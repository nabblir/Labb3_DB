using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Labb3_DB.Models
    {
    /// <summary>
    /// Represents a building in the game
    /// </summary>
    public class Building : INotifyPropertyChanged
        {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("description")]
        public string Description { get; set; } = string.Empty;

        private int _count = 0;
        [BsonElement("count")]
        public int Count
            {
            get => _count;
            set
                {
                if (SetProperty(ref _count, value))
                    {
                    // Notify that dependent properties changed
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentCost)));
                    }
                }

            }

        private int _level = 1;
        [BsonElement("level")]
        public int Level
            {
            get => _level;
            set
                {
                if (SetProperty(ref _level, value))
                    {
                    // Notify that dependent properties changed
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentIncome)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IncomePerBuilding)));
                    }
                }

            }

        [BsonElement("baseCost")]
        public double BaseCost { get; set; }

        [BsonElement("baseIncome")]
        public double BaseIncome { get; set; }

        [BsonElement("happinessDecrease")]
        public float HappinessDecrease { get; set; }
        
        [BsonElement("happinessIncrease")]
        public float HappinessIncrease { get; set; }
        
        [BsonElement("costMultiplier")]
        public double CostMultiplier { get; set; } = 1.15;

        [BsonElement("populationCost")]
        public int PopulationCost { get; set; } = 0;
        
        [BsonElement("maxPopulation")]
        public int MaxPopulation { get; set; } = 0;
        
        [BsonElement("buildingType")]
        public string BuildingType { get; set; } = string.Empty; // "Production", "Housing", "Entertainment"

        [BsonIgnore]
        public double CurrentCost => BaseCost * Math.Pow(CostMultiplier, Count);

        [BsonIgnore]
        public double CurrentIncome => BaseIncome * (Level * 5) * Count;

        [BsonIgnore]
        public double IncomePerBuilding => BaseIncome * (Level * 5);

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
            {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
            }
        }
    }