using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace Labb3_DB.Models
    {
    public class User
        {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("userID")]
        public string? UserID { get; set; }

        [BsonElement("userName")]
        public string? Username { get; set; }
        
        [BsonElement("password")]
        public string? Password { get; set; }

        [BsonElement("savedKingdoms")]
        public List<Kingdom> SavedKingdoms { get; set; } = new List<Kingdom>();
        }
    }
