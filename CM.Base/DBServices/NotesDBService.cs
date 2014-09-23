using System;
using CM.Base.BusinessModels;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace CM.Base.DBServices
{
    public class NotesDBService : DBServiceBase<Note>
    {
        public NotesDBService(MongoContext db)
            : base(db)
        {
        }

        public MongoCursor<Note> All()
        {
            return _db.FindAll<Note>();
        }

        public void Delete(ObjectId noteId)
        {
            var deleted = _db.Delete<Note>(Query<Note>.EQ(p => p.Id, noteId));
            if (deleted != 1)
                throw new Exception(string.Format("User id {0} not found", noteId));
        }

        public Note SingleByIdUnauthorized(ObjectId noteId)
        {
            return _db.SingleById<Note>(noteId);
        }
    }
}
