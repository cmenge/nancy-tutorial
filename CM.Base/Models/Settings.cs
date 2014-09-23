using System;
using CM.Base.BusinessModels;
using MongoDB.Bson;

namespace CM.Base.Models
{
    public class Settings : IDBObject
    {
        public Settings()
        {
            BaseUrl = "http://localhost/"; // yoursite.com
        }

        public ObjectId Id { get; set; }
        public string InstanceName { get; set; }
        public DateTime FirstStartup { get { return Id.CreationTime; } }

        /// <summary>
        /// The URL where this instance is deployed to
        /// </summary>
        public string BaseUrl { get; set; }
    }
}
