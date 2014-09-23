using System;
using MongoDB.Bson;

namespace CM.Base.BusinessModels
{
    public class Note : DBObject
    {
        public ObjectId UserId { get; set; }
        public ObjectId TenantId { get; set; }
        public string Text { get; set; }
        public string Title { get; set; }

        public DateTime Created { get { return Id.CreationTime;  } }
    }
}
    