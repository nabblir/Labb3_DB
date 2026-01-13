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

        // For UI display of totals based on count
        public double TotalIncome => _count > 0 ? _model.BaseIncome * _count : _model.BaseIncome;

        public int TotalMaxPopulation => _count > 0 ? _model.MaxPopulation * _count : _model.MaxPopulation;

        public int TotalPopulationCost => _count > 0 ? _model.PopulationCost * _count : _model.PopulationCost;

        public double TotalHappinessIncrease => _count > 0 ? _model.HappinessIncrease * _count : _model.HappinessIncrease;

        private int _count;
        public int Count
            {
            get => _count;
            set
                {
                if (SetProperty(ref _count, value))
                    {
                    _model.Count = value;
                    OnPropertyChanged(nameof(CurrentCost)); // TODO: Uppdatera även cost
                    }
                }
            }

        public Building Model => _model;

        public BuildingViewModel(Building model)
            {
            _model = model;
            _count = model.Count;
            }
        }
    }
