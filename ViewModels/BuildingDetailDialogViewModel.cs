using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Labb3_DB.Commands;
using Labb3_DB.Models;
using MaterialDesignThemes.Wpf;

namespace Labb3_DB.ViewModels
    {
    public class BuildingDetailDialogViewModel : INotifyPropertyChanged
        {
        private readonly Action<double> _updateGold;
        private readonly Action _updateStats;

        public BuildingDetailDialogViewModel(Building building, double currentGold, Action<double> updateGold, Action updateStats)
            {
            SelectedBuilding = building;
            CurrentGold = currentGold;
            _updateGold = updateGold;
            _updateStats = updateStats;

            // Initialize commands
            BuyMoreBuildingCommand = new RelayCommand(_ => BuyMoreBuilding(), _ => CanBuyMore());
            UpgradeBuildingCommand = new RelayCommand(_ => UpgradeBuilding(), _ => CanUpgrade());
            SellBuildingCommand = new RelayCommand(_ => SellBuilding(), _ => CanSell());
            CloseDialogCommand = new RelayCommand(_ => CloseDialog());
            }

        #region Properties

        private Building _selectedBuilding;
        public Building SelectedBuilding
            {
            get => _selectedBuilding;
            set
                {
                _selectedBuilding = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(NextLevel));
                OnPropertyChanged(nameof(UpgradeCost));
                OnPropertyChanged(nameof(NextLevelIncome));
                OnPropertyChanged(nameof(SellValue));
                }
            }

        private double _currentGold;
        public double CurrentGold
            {
            get => _currentGold;
            set
                {
                _currentGold = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
                }
            }

        public int NextLevel => SelectedBuilding.Level + 1;

        public double UpgradeCost => CalculateUpgradeCost();

        public double NextLevelIncome => CalculateNextLevelIncome();

        public double SellValue => SelectedBuilding.BaseCost * Math.Pow(SelectedBuilding.CostMultiplier, SelectedBuilding.Count - 1) * 0.5;

        public double CurrentCost => SelectedBuilding?.CurrentCost ?? 0;

        #endregion

        #region Commands

        public ICommand BuyMoreBuildingCommand { get; }
        public ICommand UpgradeBuildingCommand { get; }
        public ICommand SellBuildingCommand { get; }
        public ICommand CloseDialogCommand { get; }

        #endregion

        #region Command Methods

        private bool CanBuyMore()
            {
            return CurrentGold >= SelectedBuilding.CurrentCost;
            }

        private void BuyMoreBuilding()
            {
            if (CurrentGold >= SelectedBuilding.CurrentCost)
                {
                double cost = SelectedBuilding.CurrentCost;
                _updateGold(-cost);
                CurrentGold -= cost;

                SelectedBuilding.Count++;

                // Notify that these depend on Count
                OnPropertyChanged(nameof(CurrentCost));
                OnPropertyChanged(nameof(UpgradeCost));
                OnPropertyChanged(nameof(SellValue));
                _updateStats();
                CommandManager.InvalidateRequerySuggested();
                }
            }

        private bool CanUpgrade()
            {
            return CurrentGold >= UpgradeCost && SelectedBuilding.Count > 0;
            }

        private void UpgradeBuilding()
            {
            if (CurrentGold >= UpgradeCost && SelectedBuilding.Count > 0)
                {
                double cost = UpgradeCost;

                _updateGold(-cost);
                CurrentGold -= cost;

                SelectedBuilding.Level++;

                // Notify that these depend on Level
                OnPropertyChanged(nameof(UpgradeCost));
                OnPropertyChanged(nameof(NextLevelIncome));
                OnPropertyChanged(nameof(CurrentCost));
                _updateStats();
                CommandManager.InvalidateRequerySuggested();
                }
            }

        private bool CanSell()
            {
            return SelectedBuilding.Count > 0;
            }

        private void SellBuilding()
            {
            if (SelectedBuilding.Count > 0)
                {
                double refund = SellValue;

                _updateGold(refund);
                CurrentGold += refund;

                SelectedBuilding.Count--;

                // Notify that these depend on Count
                OnPropertyChanged(nameof(CurrentCost));
                OnPropertyChanged(nameof(UpgradeCost));
                OnPropertyChanged(nameof(SellValue));
                _updateStats();
                CommandManager.InvalidateRequerySuggested();
                }
            }

        private void CloseDialog()
            {
            DialogHost.Close("MainDialogHost");
            }

        #endregion

        #region Helper Methods

        private double CalculateUpgradeCost()
            {
            // Cost increases exponentially with level
            return SelectedBuilding.BaseCost * Math.Pow(2, SelectedBuilding.Level) * SelectedBuilding.Count;
            }

        private double CalculateNextLevelIncome()
            {
            // Income at next level
            return SelectedBuilding.BaseIncome * ( NextLevel ) * SelectedBuilding.Count;
            }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }

        #endregion
        }
    }