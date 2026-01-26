using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Labb3_DB.Commands;
using Labb3_DB.Models;
using MaterialDesignThemes.Wpf;
using System.Diagnostics;
namespace Labb3_DB.ViewModels
    {
    public class BuildingDetailDialogViewModel : BaseViewModel
        {
        private readonly Building _buildingTemplate;
        private readonly Action<double> _updateGold;
        private readonly Action _updateStats;
        private readonly Func<OwnedBuilding?> _getOwnedBuilding;
        private readonly Func<int> _getCurrentMaxPopulation;
        private PeriodicTimer? _resetClickTimer;
        private bool _canClick = true;

        #region Properties

        public Building BuildingTemplate => _buildingTemplate;

        private OwnedBuilding? _ownedBuilding;
        public OwnedBuilding? OwnedBuilding
            {
            get => _ownedBuilding;
            set
                {
                _ownedBuilding = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsOwned));
                OnPropertyChanged(nameof(Count));
                OnPropertyChanged(nameof(Level));
                OnPropertyChanged(nameof(CurrentCost));
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
        private int _currentPopulation;
        public int CurrentPopulation
            {
            get => _currentPopulation;
            set
                {
                _currentPopulation = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
                }
            }
        private int _currentMaxPopulation;
        public int CurrentMaxPopulation
            {
            get => _currentMaxPopulation;
            set
                {
                _currentMaxPopulation = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
                }
            }
        public bool IsOwned => OwnedBuilding != null && OwnedBuilding.Count > 0;

        public int Count => OwnedBuilding?.Count ?? 0;

        public int Level => OwnedBuilding?.Level ?? 1;

        public int NextLevel => Level + 1;

        public double CurrentCost
            {
            get
                {
                if (OwnedBuilding != null)
                    {
                    return OwnedBuilding.CalculateCurrentCost(_buildingTemplate);
                    }
                return _buildingTemplate.BaseCost;
                }
            }

        public double UpgradeCost => CalculateUpgradeCost();

        public double NextLevelIncome => CalculateNextLevelIncome();

        public double SellValue => CalculateSellValue();

        #endregion

        #region Commands

        public ICommand BuyBuildingCommand { get; }
        public ICommand BuyMoreBuildingCommand { get; }
        public ICommand UpgradeBuildingCommand { get; }
        public ICommand SellBuildingCommand { get; }
        public ICommand CloseDialogCommand { get; }

        #endregion

        public BuildingDetailDialogViewModel(
           Building buildingTemplate,
           OwnedBuilding? ownedBuilding,
           double currentGold,
           int currentPopulation,
           Func<int> currentMaxPopulation,
           Action<double> updateGold,
           Action updateStats,
           Func<OwnedBuilding?> getOwnedBuilding)

            {
            _buildingTemplate = buildingTemplate;
            OwnedBuilding = ownedBuilding;
            CurrentGold = currentGold;
            _updateGold = updateGold;
            _updateStats = updateStats;
            _getOwnedBuilding = getOwnedBuilding;
            _getCurrentMaxPopulation = currentMaxPopulation;
            CurrentPopulation = currentPopulation;
            CurrentMaxPopulation = _getCurrentMaxPopulation();
            // Initialize commands
            BuyBuildingCommand = new RelayCommand(_ => BuyBuilding(), _ => CanBuy());
            BuyMoreBuildingCommand = new RelayCommand(_ => BuyMoreBuilding(), _ => CanBuyMore());
            UpgradeBuildingCommand = new RelayCommand(_ => UpgradeBuilding(), _ => CanUpgrade());
            SellBuildingCommand = new RelayCommand(_ => SellBuilding(), _ => CanSell());
            CloseDialogCommand = new RelayCommand(_ => CloseDialog());

            Debug.WriteLine($"BuildingDetailDialogViewModel initialized for building: {_buildingTemplate.Name}");
            Debug.WriteLine("---------------------------------------------------");
            Debug.WriteLine($"CurrentGold: {CurrentGold}, IsOwned: {IsOwned}, CurrentCost: {CurrentCost}");
            Debug.WriteLine($"Count: {Count}, Level: {Level}, UpgradeCost: {UpgradeCost}");
            Debug.WriteLine($"Population: {CurrentPopulation}/{CurrentMaxPopulation}");
            Debug.WriteLine("---------------------------------------------------");
            Debug.WriteLine($"BuyBuildingCommand CanExecute: {CanBuy()}");
            Debug.WriteLine($"BuyMoreBuildingCommand CanExecute: {CanBuyMore()}");
            Debug.WriteLine($"UpgradeBuildingCommand CanExecute: {CanUpgrade()}");
            Debug.WriteLine($"SellBuildingCommand CanExecute: {CanSell()}");
            Debug.WriteLine("---------------------------------------------------");
            }
        
        #region Command Methods

        private bool CanBuy()
            {
            return CurrentGold >= CurrentCost && !IsOwned;
            }

        private void BuyBuilding()
            {
            if (CurrentGold >= CurrentCost && _canClick && CurrentPopulation + _buildingTemplate.PopulationCost < CurrentMaxPopulation)
                {

                double cost = CurrentCost;
                _updateGold(-cost);
                CurrentGold -= cost;

                // This will be handled in MainViewModel to create new OwnedBuilding and add it to Kingdom.OwnedBuildings
                _updateStats();

                OwnedBuilding = _getOwnedBuilding();

                CommandManager.InvalidateRequerySuggested();

                //Prevent spamming the buy button, which otherwise would cause player to be able to buy more than one building at a time
                _canClick = false;
                _resetClickTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(500));
                _ = ResetClick();
                }
            }

        private async Task ResetClick()
            {
            if (_resetClickTimer == null)
                return;

            try
                {
                while (await _resetClickTimer.WaitForNextTickAsync())
                    {
                    _canClick = true;
                    _resetClickTimer.Dispose();
                    }
                }
            catch (OperationCanceledException)
                {
                // Timer was cancelled, this is expected
                }
            }

        private bool CanBuyMore()
            {
            if(_buildingTemplate.BuildingType != "Housing")
                {
                return CurrentGold >= CurrentCost && IsOwned && CurrentPopulation + _buildingTemplate.PopulationCost <= CurrentMaxPopulation;
                }
            else
                {
                return CurrentGold >= CurrentCost && IsOwned;
                }
            }

        private void BuyMoreBuilding()
            {
            if (CurrentGold >= CurrentCost && OwnedBuilding != null)
                {
                double cost = CurrentCost;
                _updateGold(-cost);
                CurrentGold -= cost;

                OwnedBuilding.Count++;
                OwnedBuilding.RecalculateTotals(_buildingTemplate);

                // Notify property changes
                OnPropertyChanged(nameof(Count));
                OnPropertyChanged(nameof(CurrentCost));
                OnPropertyChanged(nameof(UpgradeCost));
                OnPropertyChanged(nameof(SellValue));

                _updateStats();
                CommandManager.InvalidateRequerySuggested();
                }
            }

        private bool CanUpgrade()
            {
            return CurrentGold >= UpgradeCost && IsOwned;
            }

        private void UpgradeBuilding()
            {
            if (CurrentGold >= UpgradeCost && OwnedBuilding != null)
                {
                double cost = UpgradeCost;
                _updateGold(-cost);
                CurrentGold -= cost;

                OwnedBuilding.Level++;
                OwnedBuilding.RecalculateTotals(_buildingTemplate);

                // Notify property changes
                OnPropertyChanged(nameof(Level));
                OnPropertyChanged(nameof(NextLevel));
                OnPropertyChanged(nameof(UpgradeCost));
                OnPropertyChanged(nameof(NextLevelIncome));

                _updateStats();
                CommandManager.InvalidateRequerySuggested();
                }
            }

        private bool CanSell()
            {
            return IsOwned;
            }

        private void SellBuilding()
            {
            if (OwnedBuilding != null && OwnedBuilding.Count > 0)
                {
                double refund = SellValue;
                _updateGold(refund);
                CurrentGold += refund;

                OwnedBuilding.Count--;
                OwnedBuilding.RecalculateTotals(_buildingTemplate);

                // Notify property changes
                OnPropertyChanged(nameof(Count));
                OnPropertyChanged(nameof(IsOwned));
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
            if (!IsOwned)
                return 0;
            // Cost increases exponentially with level
            return _buildingTemplate.BaseCost * Math.Pow(2, Level) * Count;
            }

        private double CalculateNextLevelIncome()
            {
            if (!IsOwned)
                return _buildingTemplate.BaseIncome * 5; // Level 1 income
            return _buildingTemplate.BaseIncome * ( NextLevel * 5 ) * Count;
            }

        private double CalculateSellValue()
            {
            if (!IsOwned)
                return 0;
            // Sell
            return _buildingTemplate.BaseCost * Math.Pow(_buildingTemplate.CostMultiplier, Count - 1) * 0.5;
            }

        #endregion
        }
    }