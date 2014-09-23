using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using CM.Base.BusinessModels;
using CM.Base.Exceptions;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using CodeContract = System.Diagnostics.Contracts.Contract;

namespace CM.Base.DBServices
{
    public class DBServiceBase<T>
        where T : class, IDBObject
    {
        protected readonly MongoContext _db;

        public DBServiceBase(MongoContext db)
        {
            CodeContract.Requires<ArgumentNullException>(db != null);
            _db = db;
        }

        protected MongoCursor<T> Find(ObjectId? fromId, IEnumerable<IMongoQuery> extraCriteria)
        {
            CodeContract.Requires<ArgumentNullException>(extraCriteria != null);
            CodeContract.Ensures(CodeContract.Result<MongoCursor<T>>() != null);

            var criteria = new List<IMongoQuery>();
            criteria.AddRange(extraCriteria.WhereNotNull());

            if (fromId != null)
                criteria.Add(Query<T>.LTE(p => p.Id, fromId));

            if (typeof(ISoftDelete).IsAssignableFrom(typeof(T)))
                criteria.Add(Query.EQ("DeletedAt", (DateTime?)BsonNull.Value));

            IMongoQuery query;
            if (criteria.Count != 1)
                query = Query.And(criteria);
            else
                query = criteria.Single();

            return _db.Find<T>(query).SetSortOrder(SortBy<T>.Descending(p => p.Id));
        }

        protected MongoCursor<T> Find(ObjectId? fromId, params IMongoQuery[] extraCriteria)
        {
            CodeContract.Ensures(CodeContract.Result<MongoCursor<T>>() != null);
            return Find(fromId, extraCriteria.AsEnumerable());
        }

        public MongoCursor<T> All(ObjectId? fromId = null)
        {
            CodeContract.Ensures(CodeContract.Result<MongoCursor<T>>() != null);
            return Find(fromId);
        }

        public T SingleByIdOrDefault(ObjectId id)
        {
            var results = _db.Find<T>(Query.And(Query<T>.EQ(p => p.Id, id)));
            var result = results.SingleOrDefault();
            return result; 
        }

        public T SingleById(ObjectId id)
        {
            CodeContract.Requires<ArgumentException>(id != ObjectId.Empty);
            CodeContract.Ensures(CodeContract.Result<T>() != null);
            var result = SingleByIdOrDefault(id);
            if (result == null)
                throw DBObjectNotFoundException.Create<T>(id);
            return result;
        }

        public T SingleByIdFromUser(string id)
        {
            ObjectId objectId;
            if (string.IsNullOrWhiteSpace(id) || !ObjectId.TryParse(id, out objectId))
            {
                string message = string.Format("'{0}' is not a valid ObjectId. Expected a 24 digit hexadecimal string.", id);
                throw new ValidationException(message);
            }
            return SingleByIdFromUser(objectId);
        }

        public T SingleByIdFromUser(ObjectId id)
        {
            if (id == ObjectId.Empty)
                throw new ValidationException("An empty id is invalid for persistent objects");
            var result = SingleByIdOrDefault(id);
            if (result == null)
                throw DBObjectNotFoundUserException.Create<T>(id);
            return result;
        }

        public virtual void Update(T item)
        {
            CodeContract.Requires<ArgumentNullException>(item != null);
            Validate(item);
            var updated = _db.Update(item);
            if (updated != 1)
                throw new Exception(string.Format("Failed to update object {0} {1}", item.GetType().Name, item.Id));
        }

        public virtual T Create(T item)
        {
            CodeContract.Requires<ArgumentNullException>(item != null);
            Validate(item);
            _db.Insert(item);
            return item;
        }

        protected virtual void Validate(T item)
        {
        }

        /// <summary>
        /// Deletes a single item iff it belongs to entityContext. Will verify the owner 
        /// by re-fetching the element from the database.
        /// </summary>
        /// <param name="entityContext"></param>
        /// <param name="item"></param>
        protected void DeleteHard(T item)
        {
            CodeContract.Requires<ArgumentNullException>(item != null);
            _db.Delete(item);
        }

        protected void DeleteHard(string id)
        {
            CodeContract.Requires<ArgumentException>(string.IsNullOrWhiteSpace(id) == false);
            var item = SingleById(ObjectId.Parse(id));
            _db.Delete(item);
        }

        public void AssertExistence(ObjectId id)
        {
            SingleById(id);
        }
    }
}
