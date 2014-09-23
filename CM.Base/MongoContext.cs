using System;
using System.Collections.Generic;
using System.Configuration;
using System.Runtime.Serialization;
using System.Text;
using CM.Base.BusinessModels;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Wrappers;
using CodeContracts = System.Diagnostics.Contracts;

namespace CM.Base
{
    [Serializable]
    public class UniqueConstraintViolationException : Exception
    {
        public UniqueConstraintViolationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public UniqueConstraintViolationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    public class MongoContext
    {
        private static readonly Object _syncRoot = new object();

        private MongoDatabase _actualProvider = null;

        private MongoDatabase _provider
        {
            get
            {
                if (_actualProvider == null)
                {
                    _actualProvider = CreateSession(DatabaseName);
                }
                return _actualProvider;
            }
        }

        private static MongoServer _server;

        private static readonly WriteConcern PARANOID_WRITE_CONCERN_RS = new WriteConcern() { Journal = true, W = "majority", WTimeout = TimeSpan.FromSeconds(5) };
        private static readonly WriteConcern SAFE_WRITE_CONCERN_RS = PARANOID_WRITE_CONCERN_RS;

        private static readonly WriteConcern PARANOID_WRITE_CONCERN_PLAIN = new WriteConcern() { Journal = true };
        private static readonly WriteConcern SAFE_WRITE_CONCERN_PLAIN = PARANOID_WRITE_CONCERN_PLAIN;
        private static readonly WriteConcern DEFAULT_WRITE_CONCERN = WriteConcern.Acknowledged;

        private static WriteConcern PARANOID_WRITE_CONCERN = PARANOID_WRITE_CONCERN_PLAIN;
        private static WriteConcern SAFE_WRITE_CONCERN = SAFE_WRITE_CONCERN_PLAIN;

        private static readonly string _host = ConfigurationManager.AppSettings["MongoHost"] ?? "localhost";

        public string DatabaseName { get; private set; }

        /// <summary>
        /// This is not an expensive operation, because the connections are pooled
        /// </summary>
        /// <param name="databaseName"></param>
        /// <returns></returns>
        private MongoDatabase CreateSession(string databaseName)
        {
            CodeContracts.Contract.Ensures(CodeContracts.Contract.Result<MongoDatabase>() != null);

            if (_server == null)
            {
                lock (_syncRoot)
                {
                    if (_server == null)
                    {
                        string connectionString = String.Format("mongodb://{1}/{0}?safe=true", databaseName, _host);
                        MongoClient client = new MongoClient(connectionString);
                        _server = client.GetServer();

                        // If connected to a replica set, use the appropriate write concerns
                        if (_server.ReplicaSetName != null)
                        {
                            PARANOID_WRITE_CONCERN = PARANOID_WRITE_CONCERN_RS;
                            SAFE_WRITE_CONCERN = SAFE_WRITE_CONCERN_RS;
                        }

                        //var pack = new ConventionPack();
                        //pack.Add(new IgnoreExtraElementsConvention(true));
                        //ConventionRegistry.Register("sloppy",pack,t => true);
                    }
                }
            }

            // This method is thread safe and will return the same database object instance every time
            var database = _server.GetDatabase(databaseName);

            try
            {
                _server.Connect();
            }
            catch
            {
                return null;
            }

            return database;
        }

        public bool Exists<T>(ObjectId id)
            where T : IDBObject
        {
            return GetCollection<T>().Find(Query<T>.EQ(p => p.Id, id)).SetFields(Fields<T>.Include(p => p.Id)).SetLimit(1).Size() == 1;
        }

        public void AssertIdExists<T>(ObjectId id)
            where T : IDBObject
        {
            var temp = Exists<T>(id);
            if (temp == false)
                throw new ApplicationException(string.Format("Foreign key violation: Couldn't find {0}:{1}", typeof(T).Name, id.ToString()));
        }

        public MongoContext(string databaseName)
        {
            // CodeContracts.Contract.Ensures(_provider != null);
            CodeContracts.Contract.Requires<ArgumentNullException>(databaseName != null);
            CodeContracts.Contract.Requires<ArgumentException>(!string.IsNullOrWhiteSpace(databaseName));

            _actualProvider = CreateSession(databaseName);
            DatabaseName = databaseName;
        }

        public MongoDatabase Provider
        {
            get
            {
                return _provider;
            }
        }

        public T SingleById<T>(object id)
        {
            return GetCollection<T>().FindOneById(BsonValue.Create(id));
        }

        /// <summary>
        /// Inserts the given item of type T into a collection named after the type, more specifically,
        /// a collection with name typeof(T).Name.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item"></param>
        public void Insert<T>(T item) where T : class
        {
            try
            {
                var result = GetCollection<T>().Insert(item);

                if (result.Ok == false)
                {
                    // WARNING: Can no longer do that, because the logger inserts - would lead to infinite loops!
                    // _log.Error(() => string.Format("Failed to insert {0} into the database: {1}", typeof(T).Name, result.LastErrorMessage));
                    throw new ApplicationException("Database insert operation failed: " + (result.HasLastErrorMessage ? result.LastErrorMessage : "(no further info available)"));
                }
            }
            catch (MongoException ex)
            {
                if (ex.Message.Contains("duplicate key error"))
                    throw new UniqueConstraintViolationException("Duplicate Key!", ex);
                else
                    throw;
            }
        }

        public void InsertBatch<T1>(IEnumerable<T1> batch)
        {
            StringBuilder sb = new StringBuilder();
            // long totalSuccess = 0;
            bool hasError = false;
            string allErrors = string.Empty;

            try
            {
                var result = GetCollection<T1>().InsertBatch(batch);

                foreach (var rover in result)
                {
                    if (rover.Ok == false)
                    {
                        if (rover.HasLastErrorMessage)
                            sb.AppendLine(rover.LastErrorMessage);
                        hasError = true;
                    }
                }

                if (hasError == true)
                {
                    // WARNING: Can no longer do that, because the logger inserts - would lead to infinite loops!
                    // _log.Error(() => string.Format("Failed to insert {0} into the database: {1}", typeof(T).Name, result.LastErrorMessage));
                    throw new ApplicationException(string.Format("Database batch insert operation failed. Errors: {0}", sb.ToString()));
                }
            }
            catch (MongoException ex)
            {
                if (ex.Message.Contains("duplicate key error"))
                    throw new UniqueConstraintViolationException("Duplicate Key!", ex);
                else
                    throw;
            }
        }

        public long UpdateMany<T>(IMongoQuery query, IMongoUpdate update)
        {
            try
            {
                var result = GetCollection<T>().Update(query, update, UpdateFlags.Multi);
                if (result.Ok == false)
                {
                    // _log.Error(() => string.Format("Failed to update object of type {0}: {1}", typeof(T).Name, result.LastErrorMessage));
                    throw new ApplicationException("Database insert operation failed: " + (result.HasLastErrorMessage ? result.LastErrorMessage : "(no further info available)"));
                }

                return result.DocumentsAffected;
            }
            catch (MongoException ex)
            {
                // FIXME! This is no longer correct!
                if (ex.Message.Contains("duplicate key error"))
                    throw new UniqueConstraintViolationException("Duplicate Key!", ex);
                else
                    throw;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public long Update<T>(T item) where T : IDBObject
        {
            return UpdateInternal(item, item.Id);
        }

        private long UpdateInternal<T>(T item, object id)
        {
            try
            {
                var result = GetCollection<T>().Update(Query.EQ("_id", BsonValue.Create(id)), MongoDB.Driver.Builders.Update.Replace(item));
                if (result.Ok == false)
                {
                    // _log.Error(() => string.Format("Failed to update object of type {0}: {1}", typeof(T).Name, result.LastErrorMessage));
                    throw new ApplicationException("Database insert operation failed: " + (result.HasLastErrorMessage ? result.LastErrorMessage : "(no further info available)"));
                }

                return result.DocumentsAffected;
            }
            catch (MongoException ex)
            {
                // FIXME! This is no longer correct!
                if (ex.Message.Contains("duplicate key error"))
                    throw new UniqueConstraintViolationException("Duplicate Key!", ex);
                else
                    throw;
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Performs an update with a more strict write concern to ensure the update was persisted.
        /// Use this whenever data loss is irrecoverable, such as in webhooks. Do not use this method
        /// per default, because it can easily take 10 - 30ms to complete, rendering it an order of
        /// magnitude slower than normal updates
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query"></param>
        /// <param name="newItemState"></param>
        /// <returns></returns>
        public long UpdateSafe<T>(IMongoQuery query, T newItemState)
        {
            return UpdateReplaceInternal(query, newItemState, SAFE_WRITE_CONCERN);
        }

        public long Update<T>(IMongoQuery query, T newItemState)
        {
            return UpdateReplaceInternal(query, newItemState, DEFAULT_WRITE_CONCERN);
        }

        private long UpdateReplaceInternal<T>(IMongoQuery query, T newItemState, WriteConcern writeConcern)
        {
            try
            {
                var result = GetCollection<T>().Update(query, UpdateWrapper.Create(newItemState), writeConcern);
                if (result.Ok == false)
                {
                    // _log.Error(() => string.Format("Failed to update object of type {0}: {1}", typeof(T).Name, result.LastErrorMessage));
                    throw new ApplicationException("Database insert operation failed: " + (result.HasLastErrorMessage ? result.LastErrorMessage : "(no further info available)"));
                }

                return result.DocumentsAffected;
            }
            catch (MongoException ex)
            {
                // FIXME! This is no longer correct!
                if (ex.Message.Contains("duplicate key error"))
                    throw new UniqueConstraintViolationException("Duplicate Key!", ex);
                else
                    throw;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public long Update<T>(IMongoQuery query, IMongoUpdate update)
        {
            return UpdateDiffInternal<T>(query, update, DEFAULT_WRITE_CONCERN);
        }

        public long UpdateSafe<T>(IMongoQuery query, IMongoUpdate update)
        {
            return UpdateDiffInternal<T>(query, update, SAFE_WRITE_CONCERN);
        }

        private long UpdateDiffInternal<T>(IMongoQuery query, IMongoUpdate update, WriteConcern writeConcern)
        {
            try
            {
                var result = GetCollection<T>().Update(query, update, writeConcern);
                if (result.Ok == false)
                {
                    // _log.Error(() => string.Format("Failed to update object of type {0}: {1}", typeof(T).Name, result.LastErrorMessage));
                    throw new ApplicationException("Database insert operation failed: " + (result.HasLastErrorMessage ? result.LastErrorMessage : "(no further info available)"));
                }

                return result.DocumentsAffected;
            }
            catch (MongoException ex)
            {
                // FIXME! This is no longer correct!
                if (ex.Message.Contains("duplicate key error"))
                    throw new UniqueConstraintViolationException("Duplicate Key!", ex);
                else
                    throw;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public long Upsert<T>(IMongoQuery query, IMongoUpdate update)
        {
            CodeContracts.Contract.Requires<ArgumentNullException>(query != null);
            CodeContracts.Contract.Requires<ArgumentNullException>(update != null);

            var result = GetCollection<T>().Update(query, update, UpdateFlags.Upsert);
            return result.DocumentsAffected;
        }

        public T FindModifyUpsert<T>(IMongoQuery query, IMongoUpdate update, WriteConcern writeConcern)
        {
            CodeContracts.Contract.Requires<ArgumentNullException>(query != null);
            CodeContracts.Contract.Requires<ArgumentNullException>(update != null);

            var result = GetCollection<T>(writeConcern).FindAndModify(new FindAndModifyArgs { Query = query, Update = update, Upsert = true, VersionReturned = FindAndModifyDocumentVersion.Modified });
            return result.GetModifiedDocumentAs<T>();
        }

        public T FindModifyUpsert<T>(IMongoQuery query, IMongoUpdate update)
        {
            CodeContracts.Contract.Requires<ArgumentNullException>(query != null);
            CodeContracts.Contract.Requires<ArgumentNullException>(update != null);

            var result = GetCollection<T>().FindAndModify(new FindAndModifyArgs { Query = query, Update = update, Upsert = true, VersionReturned = FindAndModifyDocumentVersion.Modified });
            return result.GetModifiedDocumentAs<T>();
        }

        private T FindAndModifyBase<T>(IMongoQuery query, IMongoUpdate update, bool returnNew)
        {
            try
            {
                var temp = GetCollection<T>().FindAndModify(new FindAndModifyArgs { Query = query, Update = update, VersionReturned = returnNew ? FindAndModifyDocumentVersion.Modified : FindAndModifyDocumentVersion.Original });
                return temp.GetModifiedDocumentAs<T>();
            }
            catch (MongoException ex)
            {
                // _log.Error(() => "Error in find and modify!", ex);

                if (ex.Message.Contains("duplicate key error"))
                    throw new UniqueConstraintViolationException("Duplicate Key!", ex);
                else
                    throw;
            }
            catch (Exception)
            {
                // _log.Error(() => "Error in find and modify!", ex2);
                throw;
            }
        }

        public T ModifyAndFind<T>(IMongoQuery query, IMongoUpdate update)
        {
            return FindAndModifyBase<T>(query, update, true);
        }

        public T FindAndModify<T>(IMongoQuery query, IMongoUpdate update)
            where T : class
        {
            return FindAndModifyBase<T>(query, update, false);
        }

        /// <summary>
        /// Deletes a single item from the database
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item"></param>
        public void Delete<T>(T item)
        {
            var query = QueryWrapper.Create(item);
            var result = GetCollection<T>().Remove(query, RemoveFlags.Single);

            if (result.Ok == false)
            {
                // _log.Error(() => string.Format("Failed to delete object of type {0}: {1}", typeof(T).Name, result.LastErrorMessage));
                throw new ApplicationException(string.Format("Failed to delete object of type {0}: {1}", typeof(T).Name, result.LastErrorMessage));
            }
            //return result.Ok;
        }

        /// <summary>
        /// Deletes a single item from the database
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item"></param>
        /// <returns>The number of affected documents</returns>
        public long Delete<T>(IMongoQuery query)
        {
            var result = GetCollection<T>().Remove(query, RemoveFlags.Single);

            if (result.Ok == false)
            {
                // _log.Error(() => string.Format("Failed to delete object of type {0}: {1}", typeof(T).Name, result.LastErrorMessage));
                throw new ApplicationException(string.Format("Failed to delete object of type {0}: {1}", typeof(T).Name, result.LastErrorMessage));
            }

            return result.DocumentsAffected;
        }

        public long DeleteMany<T>(IMongoQuery query)
            where T : class
        {
            var result = GetCollection<T>().Remove(query, RemoveFlags.None);
            if (result.Ok == false)
            {
                // _log.Error(() => string.Format("Failed to delete multiple objects of type {0}: {1}", typeof(T).Name, result.LastErrorMessage));
                throw new ApplicationException(string.Format("Failed to delete multiple objects of type {0}: {1}", typeof(T).Name, result.LastErrorMessage));
            }

            return result.DocumentsAffected;
        }

        public bool TryInsert<T>(T item) where T : class
        {
            try
            {
                Insert(item);
            }
            catch (Exception)
            {
                // _log.Warn(() => "Error in TryInsert!", ex);
                return false;
            }
            return true;
        }

        public MongoCollection<T1> GetCollection<T1>(string collectionName)
        {
            CodeContracts.Contract.Ensures(CodeContracts.Contract.Result<MongoCollection<T1>>() != null);
            var result = _provider.GetCollection<T1>(collectionName);
            CodeContracts.Contract.Assume(result != null);
            return result;
        }

        public MongoCollection<T1> GetCollection<T1>()
        {
            CodeContracts.Contract.Ensures(CodeContracts.Contract.Result<MongoCollection<T1>>() != null);
            var result = _provider.GetCollection<T1>(typeof(T1).Name);
            CodeContracts.Contract.Assume(result != null);
            return result;
        }

        public MongoCollection<T1> GetCollection<T1>(WriteConcern writeConcern)
        {
            CodeContracts.Contract.Ensures(CodeContracts.Contract.Result<MongoCollection<T1>>() != null);
            var result = _provider.GetCollection<T1>(typeof(T1).Name, writeConcern);
            CodeContracts.Contract.Assume(result != null);
            return result;
        }

        public MongoCursor<T1> FindAll<T1>()
        {
            CodeContracts.Contract.Ensures(CodeContracts.Contract.Result<MongoCursor<T1>>() != null);
            var result = _provider.GetCollection<T1>(typeof(T1).Name).FindAll();
            CodeContracts.Contract.Assume(result != null);
            return result;
        }

        public MongoCursor<T1> Find<T1>(IMongoQuery query)
        {
            CodeContracts.Contract.Ensures(CodeContracts.Contract.Result<MongoCursor<T1>>() != null);
            var result = _provider.GetCollection<T1>(typeof(T1).Name).Find(query);
            CodeContracts.Contract.Assume(result != null);
            return result;
        }
    }
}