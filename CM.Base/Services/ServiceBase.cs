using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using CM.Base.BusinessModels;
using CM.Base.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace CM.Base.Services
{
    public abstract class ServiceBase
    {
        private static readonly ILogger _log = LogManager.GetLogger();

        protected MongoContext DB { get; private set; }

        public ServiceBase(MongoContext db)
        {
            Contract.Requires<ArgumentNullException>(db != null, "db");
            Contract.Ensures(DB != null);

            DB = db;
        }

        public MongoCursor<T> Find<T>(IMongoQuery query) where T : DBObject
        {
            var result = DB.GetCollection<T>().Find(query);
            return result;
        }
    }

    public abstract class ServiceBase<T> : ServiceBase
        where T : DBObject
    {
        private static readonly ILogger _log = LogManager.GetLogger();

        protected MongoCollection<T> Collection { get; private set; }

        public ServiceBase(MongoContext db)
            : base(db)
        {
            Contract.Requires<ArgumentNullException>(db != null, "mongoContext");
            Contract.Ensures(DB != null);

            Collection = DB.GetCollection<T>();
        }

        public virtual void Insert(T item)
        {
            Contract.Requires<ArgumentNullException>(item != null);
            DB.Insert(item);
        }

        /// <summary>
        /// FIXME: Insert and TryInsert should use the same method internally so we don't need to override two methods in derived classes,
        /// commenting the entire method out for now
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        //public virtual bool TryInsert(T item)
        //{
        //    Contract.Requires<ArgumentNullException>(item != null);
        //    var success = DB.TryInsert(item);
        //    return success;
        //}

        public virtual void Update(T item)
        {
            Contract.Requires<ArgumentNullException>(item != null);
            DB.Update(item);
        }

        //public virtual void Update(Guid id, T item)
        //{
        //    Contract.Requires<ArgumentNullException>(item != null);
        //    item.ForceId(id);
        //    DB.Update(item);
        //}

        //public virtual bool TryUpdate(T item)
        //{
        //    Contract.Requires<ArgumentNullException>(item != null);
        //    var success = DB.TryUpdate(item);
        //    return success;
        //}

        public virtual void Delete(T item)
        {
            Contract.Requires<ArgumentNullException>(item != null);
            DB.Delete(item);
        }

        /// <summary>
        /// Queries the db for a single document in collection 'T'
        /// </summary>
        /// <param name="id">primary key</param>
        /// <returns>Single document with primary key 'id' in collection 'T'</returns>
        public T SingleById(ObjectId id)
        {
            Contract.Requires<ArgumentException>(id != ObjectId.Empty);

            var result = Collection.FindOneById(id);
            return result;
        }

        /// <summary>
        /// Queries the db strictly for a single document in collection 'T'
        /// THROWS on null result
        /// </summary>
        /// <param name="id">primary key</param>
        /// <returns>Single document with primary key 'id' in collection 'T'</returns>
        /// <exception cref="NotExistentException">Thrown on null result</exception>
        public T SingleByIdFromUser(string id)
        {
            var result = SingleById(ObjectId.Parse(id));
            if (result == null)
            {
                _log.Error("Unable to fetch single '{0}'", id);
                throw new Exception("Single Failed");
            }

            return result;
        }

        /// <summary>
        /// Queries the db strictly for a single document in collection 'T'
        /// THROWS on null result
        /// </summary>
        /// <param name="id">primary key</param>
        /// <returns>Single document with primary key 'id' in collection 'T'</returns>
        /// <exception cref="NotExistentException">Thrown on null result</exception>
        public T SingleByIdStrict(ObjectId id)
        {
            Contract.Requires<ArgumentException>(id != ObjectId.Empty);

            var result = SingleById(id);
            if (result == null)
            {
                _log.Error("Unable to fetch single '{0}'", id);
                throw new Exception("Single Failed");
            }

            return result;
        }

        public MongoCursor<T> Find(IMongoQuery query)
        {
            Contract.Requires<ArgumentNullException>(query != null);

            var result = Collection.Find(query);
            return result;
        }

        public MongoCursor<T2> FindOnly<T2>(IMongoQuery query) where T2 : T
        {
            Contract.Requires<ArgumentNullException>(query != null);

            var result = Collection.FindOnly<T, T2>(query);
            return result;
        }

        public MongoCursor<T> FindAll()
        {
            return Collection.FindAll();
        }

        public MongoCursor<T> FindIn(IEnumerable<ObjectId> ids)
        {
            Contract.Requires<ArgumentNullException>(ids != null);
            var result = Collection.Find(Query.In("_id", new BsonArray(ids)));
            return result;
        }
    }
}
