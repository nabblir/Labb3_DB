using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Labb3_DB.Models
    {
    /// <summary>
    /// Represents a kingdom in the game
    /// </summary>
    public class Kingdom
        {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("kingdomName")]
        public string KingdomName { get; set; } = "Starship Alice";

        [BsonElement("gold")]
        public double Gold { get; set; } = 100;

        [BsonElement("goldPerSecond")]
        public double GoldPerSecond { get; set; } = 0;

        [BsonElement("population")]
        public int Population { get; set; } = 10;
        
        [BsonElement("maxPopulation")]
        public int MaxPopulation { get; set; } = 10;

        [BsonElement("happiness")]
        public float Happiness { get; set; } = 100f;

        [BsonElement("happinessDecrease")]
        public float HappinessDecrease { get; set; } = 100f;

        [BsonElement("happinessIncrease")]
        public float HappinessIncrease { get; set; } = 100f;

        [BsonElement("lastSaved")]
        public DateTime LastSaved { get; set; } = DateTime.UtcNow;

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        [BsonElement("eventsLog")]
        public string EventsLog { get; set; } = string.Empty;
        }

    }