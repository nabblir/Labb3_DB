using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Labb3_DB.Models
    {
    public class ApplicationSettings
        {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("isRemembered")]
        public bool IsRemembered { get; set; } = false;

        [BsonElement("userID")]
        public string UserID { get; set; } = string.Empty;

        }
    }
