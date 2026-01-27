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
        public string UserId { get; set; } = string.Empty;

        [BsonElement("userName")]
        public string Username { get; set; } = string.Empty;

        [BsonElement("password")]
        public string Password { get; set; } = string.Empty;

        [BsonElement("kingdomIds")]
        public List<string> KingdomIds { get; set; } = new List<string>();

        [BsonIgnore]
        public List<Kingdom> SavedKingdoms { get; set; } = new List<Kingdom>();
        }
    }
