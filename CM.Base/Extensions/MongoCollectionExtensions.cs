using System;
using System.Diagnostics.Contracts;
using MongoDB.Driver.Builders;

namespace MongoDB.Driver
{
    public static class MongoCollectionExtensions
    {
        /// <summary>
        /// FIXME: A nicer way to do this is to find the parent class that has the BsonDiscriminatorAttribute set and use
        /// it as type T2 instead. This way, we avoid repetitions throughout the code. The lookups (i.e. the reflection)
        /// should be cached.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <param name="collection"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        public static MongoCursor<T2> FindOnly<T, T2>(this MongoCollection collection, IMongoQuery query) where T2 : T
        {
            Contract.Requires<ArgumentNullException>(query != null);

            /*
            Type rover;
            while((rover = rover.DeclaringType) != typeof(object))
            {
                if(rover.GetCustomAttributes(typeof(BsonDiscriminator), false))
                    // that's the type we're looking for!
                FIXME: NEEDS CACHING!
            }
            */

            IMongoQuery tQuery = null;
            if (typeof(T2) != typeof(T))
            {
                tQuery = Query.And(query, Query.EQ("_t", typeof(T2).Name));
            }
            else
            {
                tQuery = query;
            }

            var results = collection.FindAs<T2>(tQuery);
            return results;
        }
    }
}
