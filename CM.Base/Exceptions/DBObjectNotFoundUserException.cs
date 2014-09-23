using System;
using MongoDB.Bson;

namespace CM.Base.Exceptions
{
    /// <summary>
    /// An exception that indicates that an object that was requested *by the user* was not found.
    /// This will be directly forwarded to the API response.
    /// </summary>
    [Serializable]
    public class DBObjectNotFoundUserException : Exception
    {
        public object Id { get; private set; }
        public Type Type { get; private set; }

        public DBObjectNotFoundUserException(object id, Type type)
            : this(string.Format("{0} with Id={1} does not exist.", type.Name, id))
        {
            Type = type;
            Id = id;
        }

        public DBObjectNotFoundUserException() { }
        public DBObjectNotFoundUserException(string message) : base(message) { }
        public DBObjectNotFoundUserException(string message, Exception inner) : base(message, inner) { }
        protected DBObjectNotFoundUserException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }

        public static DBObjectNotFoundUserException Create<T>(ObjectId id)
        {
            return new DBObjectNotFoundUserException(id, typeof(T));
        }
    }
}
