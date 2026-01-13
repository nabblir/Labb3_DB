using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Labb3_DB.Models
    {
    /// <summary>
    /// Represents a building in the game
    /// </summary>
    public class Building
        {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("description")]
        public string Description { get; set; } = string.Empty;

        [BsonElement("level")]
        public int Level { get; set; } = 1;

        [BsonElement("count")]
        public int Count { get; set; } = 0;

        [BsonElement("baseCost")]
        public double BaseCost { get; set; }

        [BsonElement("baseIncome")]
        public double BaseIncome { get; set; }

        [BsonElement("costMultiplier")]
        public double CostMultiplier { get; set; } = 1.15;

        [BsonElement("populationCost")]
        public int PopulationCost { get; set; } = 0;

        [BsonElement("buildingType")]
        public string BuildingType { get; set; } = string.Empty; // "Production", "Housing", "Entertainment"

        [BsonIgnore]
        public double CurrentCost => BaseCost * Math.Pow(CostMultiplier, Count);

        [BsonIgnore]
        public double CurrentIncome => BaseIncome * Level * Count;
        }
    }