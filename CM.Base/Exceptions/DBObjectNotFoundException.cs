using System;
using MongoDB.Bson;

namespace CM.Base.Exceptions
{
    /// <summary>
    /// An exception that indicates that an object that was requested *by the system* was not found.
    /// This exception should be thrown only if the id was retrieved for instance from a foreign key, i.e. this is really BAD.
    /// </summary>
    [Serializable]
    public class DBObjectNotFoundException : Exception
    {
        public object Id { get; private set; }
        public Type Type { get; private set; }

        public DBObjectNotFoundException(object id, Type type)
            : this(string.Format("{0} with Id={1} does not exist.", type.Name, id))
        {
            Type = type;
            Id = id;
        }

        public DBObjectNotFoundException() { }
        public DBObjectNotFoundException(string message) : base(message) { }
        public DBObjectNotFoundException(string message, Exception inner) : base(message, inner) { }
        protected DBObjectNotFoundException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }

        public static DBObjectNotFoundException Create<T>(ObjectId id)
        {
            return new DBObjectNotFoundException(id, typeof(T));
        }
    }
}
