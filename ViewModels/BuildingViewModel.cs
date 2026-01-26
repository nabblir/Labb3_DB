using Labb3_DB.Models;
using System;
using System.Diagnostics;

namespace Labb3_DB.ViewModels
    {
    public class BuildingViewModel : BaseViewModel
        {
        private readonly Building _buildingTemplate;
        private OwnedBuilding? _ownedBuilding;

        public BuildingViewModel(Building buildingTemplate, OwnedBuilding? ownedBuilding = null)
            {
            _buildingTemplate = buildingTemplate;
            _ownedBuilding = ownedBuilding;

            if (_ownedBuilding != null)
                {
                _ownedBuilding.PropertyChanged += (s, e) =>
                {
                    // Notify all dependent properties when owned building changes
                    OnPropertyChanged(nameof(Count));
                    OnPropertyChanged(nameof(Level));
                    OnPropertyChanged(nameof(CurrentCost));
                    OnPropertyChanged(nameof(IncomePerBuilding));
                    OnPropertyChanged(nameof(TotalIncome));
                    OnPropertyChanged(nameof(TotalMaxPopulation));
                    OnPropertyChanged(nameof(TotalPopulationCost));
                    OnPropertyChanged(nameof(TotalHappinessIncrease));
                    OnPropertyChanged(nameof(TotalHappinessDecrease));
                    OnPropertyChanged(nameof(IsOwned));
                };
                }
            }

        #region Template Properties (from Building)

        public string Name => _buildingTemplate.Name;
        public string Description => _buildingTemplate.Description;
        public string BuildingType => _buildingTemplate.BuildingType;
        public double BaseCost => _buildingTemplate.BaseCost;
        public double BaseIncome => _buildingTemplate.BaseIncome;
        public int MaxPopulation => _buildingTemplate.MaxPopulation;
        public int PopulationCost => _buildingTemplate.PopulationCost;
        public float HappinessIncrease => _buildingTemplate.HappinessIncrease;
        public float HappinessDecrease => _buildingTemplate.HappinessDecrease;

        public Building BuildingTemplate => _buildingTemplate;

        #endregion

        #region Owned Building Properties

        public OwnedBuilding? OwnedBuilding
            {
            get => _ownedBuilding;
            set
                {
                if (_ownedBuilding != null)
                    {
                    _ownedBuilding.PropertyChanged -= OnOwnedBuildingPropertyChanged;
                    }

                _ownedBuilding = value;

                if (_ownedBuilding != null)
                    {
                    _ownedBuilding.PropertyChanged += OnOwnedBuildingPropertyChanged;
                    }

                OnPropertyChanged();
                OnPropertyChanged(nameof(IsOwned));
                OnPropertyChanged(nameof(Count));
                OnPropertyChanged(nameof(Level));
                OnPropertyChanged(nameof(CurrentCost));
                OnPropertyChanged(nameof(IncomePerBuilding));
                OnPropertyChanged(nameof(TotalIncome));
                OnPropertyChanged(nameof(TotalMaxPopulation));
                OnPropertyChanged(nameof(TotalPopulationCost));
                OnPropertyChanged(nameof(TotalHappinessIncrease));
                OnPropertyChanged(nameof(TotalHappinessDecrease));
                }
            }

        private void OnOwnedBuildingPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
            {
            // Notify all dependent properties when owned building changes
            OnPropertyChanged(nameof(Count));
            OnPropertyChanged(nameof(Level));
            OnPropertyChanged(nameof(CurrentCost));
            OnPropertyChanged(nameof(IncomePerBuilding));
            OnPropertyChanged(nameof(TotalIncome));
            OnPropertyChanged(nameof(TotalMaxPopulation));
            OnPropertyChanged(nameof(TotalPopulationCost));
            OnPropertyChanged(nameof(TotalHappinessIncrease));
            OnPropertyChanged(nameof(TotalHappinessDecrease));
            OnPropertyChanged(nameof(IsOwned));
            Debug.WriteLine($"[Church] Property changed: {e.PropertyName}, TotalIncome: {_ownedBuilding?.TotalIncome}");
            }

        public bool IsOwned => _ownedBuilding != null && _ownedBuilding.Count > 0;

        public int Count => _ownedBuilding?.Count ?? 0;

        public int Level => _ownedBuilding?.Level ?? 1;

        #endregion

        #region Calculated Properties

        public double CurrentCost
            {
            get
                {
                if (_ownedBuilding != null)
                    {
                    return _ownedBuilding.CalculateCurrentCost(_buildingTemplate);
                    }
                return _buildingTemplate.BaseCost;
                }
            }

        public double IncomePerBuilding => _ownedBuilding?.IncomePerBuilding ?? ( _buildingTemplate.BaseIncome * 5 );

        public double TotalIncome => _ownedBuilding?.TotalIncome ?? 0;

        public int TotalMaxPopulation => _ownedBuilding?.TotalMaxPopulation ?? 0;

        public int TotalPopulationCost => _ownedBuilding?.TotalPopulationCost ?? 0;

        public float TotalHappinessIncrease => _ownedBuilding?.TotalHappinessIncrease ?? 0;

        public float TotalHappinessDecrease => _ownedBuilding?.TotalHappinessDecrease ?? 0;

        #endregion
        }
    }