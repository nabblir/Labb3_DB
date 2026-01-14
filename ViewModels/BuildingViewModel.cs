using Labb3_DB.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Labb3_DB.ViewModels
{
    public class BuildingViewModel : BaseViewModel
        {
        private readonly Building _model;

        public string Name => _model.Name;
        public string Description => _model.Description;
        public double CurrentCost => _model.CurrentCost;

        public string BuildingType => _model.BuildingType;

        public float HappinessDecrease => _model.HappinessDecrease;
        public float HappinessIncrease => _model.HappinessIncrease;
        public double BaseIncome => _model.BaseIncome;
        public int MaxPopulation => _model.MaxPopulation;

        public int PopulationCost => _model.PopulationCost;

        public int Level => _model.Level;

        public double IncomePerBuilding => _model.BaseIncome * (Level * 5);

        public double TotalIncome => _model.Count > 0 ? _model.BaseIncome * (Level * 5) * _model.Count : _model.BaseIncome;

        public int TotalMaxPopulation => _model.Count > 0 ? _model.MaxPopulation * _model.Count : _model.MaxPopulation;

        public int TotalPopulationCost => _model.Count > 0 ? _model.PopulationCost * _model.Count : _model.PopulationCost;

        public double TotalHappinessIncrease => _model.Count > 0 ? _model.HappinessIncrease * _model.Count : _model.HappinessIncrease;

        public int Count => _model.Count;

        public Building Model => _model;

        public BuildingViewModel(Building model)
            {
            _model = model;
            // Listen to model changes
            _model.PropertyChanged += (s, e) =>
                {
                if (e.PropertyName == nameof(Building.Count))
                    {
                    OnPropertyChanged(nameof(Count));
                    OnPropertyChanged(nameof(TotalIncome));
                    OnPropertyChanged(nameof(TotalMaxPopulation));
                    OnPropertyChanged(nameof(TotalPopulationCost));
                    OnPropertyChanged(nameof(TotalHappinessIncrease));
                    OnPropertyChanged(nameof(CurrentCost));
                    }
                };
            }
        }
    }
