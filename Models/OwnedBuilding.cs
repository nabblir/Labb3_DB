using Labb3_DB.ViewModels;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Labb3_DB.Models
    {
    /// <summary>
    /// Represents a building that the player owns (stored in Kingdom.OwnedBuildings)
    /// </summary>
    public class OwnedBuilding : BaseViewModel
        {
        [BsonElement("buildingName")]
        public string BuildingName { get; set; } = string.Empty; // Reference to Building.Name

        private int _count = 0;
        [BsonElement("count")]
        public int Count
            {
            get => _count;
            set
                {
                if (SetProperty(ref _count, value))
                    {
                    OnPropertyChanged(nameof(TotalIncome));
                    OnPropertyChanged(nameof(TotalPopulationCost));
                    OnPropertyChanged(nameof(TotalMaxPopulation));
                    OnPropertyChanged(nameof(TotalHappinessIncrease));
                    OnPropertyChanged(nameof(TotalHappinessDecrease));
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
                    OnPropertyChanged(nameof(TotalIncome));
                    OnPropertyChanged(nameof(IncomePerBuilding));
                    OnPropertyChanged(nameof(TotalHappinessIncrease));
                    }
                }
            }

        // Cached calculations (recalculated when needed)
        private double _totalIncome;
        [BsonElement("totalIncome")]
        public double TotalIncome
            {
            get => _totalIncome;
            set => SetProperty(ref _totalIncome, value);
            }

        private double _incomePerBuilding;
        [BsonElement("incomePerBuilding")]
        public double IncomePerBuilding
            {
            get => _incomePerBuilding;
            set => SetProperty(ref _incomePerBuilding, value);
            }

        private int _totalPopulationCost;
        [BsonElement("totalPopulationCost")]
        public int TotalPopulationCost
            {
            get => _totalPopulationCost;
            set => SetProperty(ref _totalPopulationCost, value);
            }

        private int _totalMaxPopulation;
        [BsonElement("totalMaxPopulation")]
        public int TotalMaxPopulation
            {
            get => _totalMaxPopulation;
            set => SetProperty(ref _totalMaxPopulation, value);
            }

        private float _totalHappinessIncrease;
        [BsonElement("totalHappinessIncrease")]
        public float TotalHappinessIncrease
            {
            get => _totalHappinessIncrease;
            set => SetProperty(ref _totalHappinessIncrease, value);
            }

        private float _totalHappinessDecrease;
        [BsonElement("totalHappinessDecrease")]
        public float TotalHappinessDecrease
            {
            get => _totalHappinessDecrease;
            set => SetProperty(ref _totalHappinessDecrease, value);
            }

        /// <summary>
        /// Calculate the current cost to buy one more of this building
        /// </summary>
        public double CalculateCurrentCost(Building template)
            {
            return template.BaseCost * Math.Pow(template.CostMultiplier, Count);
            }

        /// <summary>
        /// Recalculate all totals based on the building template
        /// </summary>
        public void RecalculateTotals(Building template, bool? isChurch = false, float? currentHappiness = null)
            {
            IncomePerBuilding = template.BaseIncome * Level;
            TotalIncome = IncomePerBuilding * Count;
            TotalPopulationCost = template.PopulationCost * Count;
            TotalMaxPopulation = template.MaxPopulation * Count;
            TotalHappinessIncrease = template.HappinessIncrease * Count * Level;
            TotalHappinessDecrease = template.HappinessDecrease * Count;

            //Church check
            if (isChurch == true && currentHappiness != null)
                {
                if (currentHappiness >= 75)
                    {
                    TotalIncome = IncomePerBuilding * Count;
                    }
                else if (currentHappiness < 50)
                    {
                    TotalIncome = -( ( IncomePerBuilding * Count ) * 2 );
                    }
                else
                    {
                    TotalIncome = 0;
                    }
                }
            }
    }
}