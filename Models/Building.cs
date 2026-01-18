using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Labb3_DB.Models
    {
    /// <summary>
    /// Represents a building template in the shop (what CAN be bought)
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

        [BsonElement("buildingType")]
        public string BuildingType { get; set; } = string.Empty; // "Production", "Housing", "Entertainment"

        [BsonElement("baseCost")]
        public double BaseCost { get; set; }

        [BsonElement("costMultiplier")]
        public double CostMultiplier { get; set; } = 1.15;

        [BsonElement("baseIncome")]
        public double BaseIncome { get; set; }

        [BsonElement("populationCost")]
        public int PopulationCost { get; set; } = 0;

        [BsonElement("maxPopulation")]
        public int MaxPopulation { get; set; } = 0;

        [BsonElement("happinessIncrease")]
        public float HappinessIncrease { get; set; }

        [BsonElement("happinessDecrease")]
        public float HappinessDecrease { get; set; }
        }
    }