using Labb3_DB.Commands;
using Labb3_DB.Data;
using Labb3_DB.Models;
using Labb3_DB.Mongo;
using Labb3_DB.Views;
using MaterialDesignThemes.Wpf;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Labb3_DB.ViewModels
    {
    public class MainViewModel : BaseViewModel
        {
        private readonly DatabaseService _dbService;
        private User _currentUser;
        private Kingdom _currentKingdom;
        private List<Building> _buildingTemplates;
        private PeriodicTimer? _gameTick;
        private PeriodicTimer? _saveTimer;

        #region Properties

        private string _kingdomName = string.Empty;
        public string KingdomName
            {
            get => _kingdomName;
            set => SetProperty(ref _kingdomName, value);
            }

        private double _gold;
        public double Gold
            {
            get => _gold;
            set => SetProperty(ref _gold, value);
            }

        private double _goldPerSecond;
        public double GoldPerSecond
            {
            get => _goldPerSecond;
            set => SetProperty(ref _goldPerSecond, value);
            }

        private int _population;
        public int Population
            {
            get => _population;
            set => SetProperty(ref _population, value);
            }

        private int _maxPopulation;
        public int MaxPopulation
            {
            get => _maxPopulation;
            set => SetProperty(ref _maxPopulation, value);
            }

        private string _eventsLog = string.Empty;
        public string EventsLog
            {
            get => _eventsLog;
            set => SetProperty(ref _eventsLog, value);
            }

        private float _happinessDecrease;
        public float HappinessDecrease
            {
            get => _happinessDecrease;
            set => SetProperty(ref _happinessDecrease, value);
            }

        private float _happinessIncrease;
        public float HappinessIncrease
            {
            get => _happinessIncrease;
            set => SetProperty(ref _happinessIncrease, value);
            }
        private float _happinessDisplay;
        public float HappinessDisplay
            {
            get => _happinessDisplay;
            set => SetProperty(ref _happinessDisplay, value);
            }
        private float _happiness;
        public float Happiness
            {
            get => _happiness;
            set => SetProperty(ref _happiness, value);
            }

        private bool _isLoading;
        public bool IsLoading
            {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
            }

        public ObservableCollection<BuildingViewModel> OwnedBuildings { get; set; }
        public ObservableCollection<BuildingViewModel> ShopBuildings { get; set; }
        public ObservableCollection<Kingdom> UserKingdoms { get; set; }

        #endregion

        #region Commands

        public ICommand OpenBuildingDialogCommand { get; }
        public ICommand SaveGameCommand { get; }
        public ICommand ResetKingdomCommand { get; }
        public ICommand LoadGameCommand { get; }
        public ICommand SettingsCommand { get; }
        public ICommand ExitCommand { get; }
        public ICommand BuyBuildingCommand { get; }

        #endregion

        public MainViewModel(User user, Kingdom? selectedKingdom, bool shouldCreateNew)
            {
            _dbService = new DatabaseService();
            _currentUser = user ?? throw new ArgumentNullException(nameof(user));

            OwnedBuildings = new ObservableCollection<BuildingViewModel>();
            ShopBuildings = new ObservableCollection<BuildingViewModel>();
            UserKingdoms = new ObservableCollection<Kingdom>(_currentUser.SavedKingdoms);

            // Initialize commands
            OpenBuildingDialogCommand = new RelayCommand(async building =>
            {
                if (building is BuildingViewModel bvm)
                    {
                    await OpenBuildingDialog(bvm);
                    }
            }, _ => !IsLoading);

            SaveGameCommand = new RelayCommand(async _ => await SaveGameAsync(), _ => !IsLoading && _currentKingdom != null);
            ResetKingdomCommand = new RelayCommand(async _ => await ResetKingdom(), _ => !IsLoading && _currentKingdom != null);
            LoadGameCommand = new RelayCommand(async _ => await ShowKingdomSelectionDialog(), _ => !IsLoading);
            SettingsCommand = new RelayCommand(_ => OpenSettings());
            ExitCommand = new RelayCommand(_ => Application.Current.Shutdown());
            BuyBuildingCommand = new RelayCommand(async param => await BuyBuilding(param), _ => !IsLoading && _currentKingdom != null);

            // Initialize with selected or new kingdom
            _ = InitializeAsync(selectedKingdom, shouldCreateNew);
            }

        #region Initialization

        private async Task InitializeAsync(Kingdom? selectedKingdom, bool shouldCreateNew)
            {
            try
                {
                IsLoading = true;

                // Initialize buildings collection if needed
                await _dbService.InitializeBuildingsAsync();
                _buildingTemplates = await _dbService.GetAllBuildingsAsync();

                if (shouldCreateNew)
                    {
                    await CreateNewKingdomInternal();
                    }
                else if (selectedKingdom != null)
                    {
                    _currentKingdom = selectedKingdom;
                    await LoadKingdomData(_currentKingdom);
                    }
                else
                    {
                    LogEvent("No kingdom selected.");
                    }
                }
            catch (Exception ex)
                {
                LogEvent($"ERROR: {ex.Message}");
                MessageBox.Show($"Failed to initialize: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            finally
                {
                IsLoading = false;
                }
            }

        #endregion

        #region Kingdom Management

        private async Task ShowKingdomSelectionDialog()
            {
            try
                {
                IsLoading = true;
                StopGameLoops();

                // Save current kingdom before showing dialog
                if (_currentKingdom != null)
                    {
                    await SaveGameAsync();
                    }

                // Refresh user data
                _currentUser = await _dbService.GetUserByIdAsync(_currentUser.UserId);
                UserKingdoms.Clear();
                foreach (var k in _currentUser.SavedKingdoms)
                    {
                    UserKingdoms.Add(k);
                    }

                var selectionViewModel = new KingdomSelectionViewModel(_currentUser);
                var selectionDialog = new KingdomSelectionDialog
                    {
                    DataContext = selectionViewModel
                    };

                await DialogHost.Show(selectionDialog, "MainDialogHost");

                if (selectionViewModel.ShouldCreateNew)
                    {
                    await CreateNewKingdomInternal();
                    }
                else if (selectionViewModel.Result != null)
                    {
                    _currentKingdom = selectionViewModel.Result;
                    await LoadKingdomData(_currentKingdom);
                    }
                else
                    {
                    // User cancelled reload current kingdom
                    if (_currentKingdom != null)
                        {
                        await LoadKingdomData(_currentKingdom);
                        }
                    }
                }
            catch (Exception ex)
                {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            finally
                {
                IsLoading = false;
                }
            }

        private async Task LoadKingdomData(Kingdom kingdom)
            {
            if (kingdom == null)
                return;

            // Set all stats from kingdom
            Gold = kingdom.Gold;
            KingdomName = kingdom.KingdomName;
            GoldPerSecond = kingdom.GoldPerSecond;
            Population = kingdom.Population;
            MaxPopulation = kingdom.MaxPopulation;
            Happiness = kingdom.Happiness;
            HappinessDecrease = kingdom.HappinessDecrease;
            HappinessIncrease = kingdom.HappinessIncrease;

            LogEvent($"Kingdom '{kingdom.KingdomName}' loaded!");

            OwnedBuildings.Clear();
            ShopBuildings.Clear();

            // Create ViewModels for all buildings
            foreach (var template in _buildingTemplates)
                {
                var ownedBuilding = kingdom.OwnedBuildings
                    .FirstOrDefault(owned => owned.BuildingName == template.Name);

                var viewModel = new BuildingViewModel(template, ownedBuilding);

                if (ownedBuilding != null && ownedBuilding.Count > 0)
                    {
                    OwnedBuildings.Add(viewModel);
                    }
                else
                    {
                    ShopBuildings.Add(viewModel);
                    }
                }

            RecalculateKingdomStats();

            // Start game loops
            StopGameLoops(); // Clean up any existing timers
            _gameTick = new PeriodicTimer(TimeSpan.FromSeconds(1));
            _ = GameTick();

            _saveTimer = new PeriodicTimer(TimeSpan.FromSeconds(10));
            _ = SaveGameTimerAsync();
            }

        private async Task CreateNewKingdomInternal()
            {
            try
                {
                var newKingdom = new Kingdom
                    {
                    KingdomName = $"Kingdom {_currentUser.SavedKingdoms.Count + 1}",
                    UserId = _currentUser.UserId,
                    Gold = 5,
                    GoldPerSecond = 0.5f,
                    Population = 1,
                    MaxPopulation = 5,
                    Happiness = 50,
                    HappinessDecrease = 0.01f,
                    HappinessIncrease = 0,
                    OwnedBuildings = new List<OwnedBuilding>()
                    };

                // Give starting building
                var farmTemplate = _buildingTemplates?.FirstOrDefault(b => b.Name == "Farm");
                if (farmTemplate != null)
                    {
                    var startingFarm = new OwnedBuilding
                        {
                        BuildingName = "Farm",
                        Count = 1,
                        Level = 1
                        };
                    startingFarm.RecalculateTotals(farmTemplate);
                    newKingdom.OwnedBuildings.Add(startingFarm);
                    }

                await _dbService.CreateKingdomAsync(_currentUser.UserId, newKingdom);

                // Refresh user data
                _currentUser = await _dbService.GetUserByIdAsync(_currentUser.UserId);
                _currentKingdom = _currentUser.SavedKingdoms.FirstOrDefault(k => k.Id == newKingdom.Id);

                // Update kingdoms collection
                UserKingdoms.Clear();
                foreach (var k in _currentUser.SavedKingdoms)
                    {
                    UserKingdoms.Add(k);
                    }

                LogEvent($"New kingdom '{newKingdom.KingdomName}' created!");
                await LoadKingdomData(_currentKingdom);
                }
            catch (Exception ex)
                {
                MessageBox.Show($"Error creating kingdom: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

        private async Task ResetKingdom()
            {
            if (_currentKingdom == null)
                return;

            var result = MessageBox.Show(
                $"Are you sure you want to delete '{_currentKingdom.KingdomName}'?\n\nThis will delete ALL progress for this kingdom!\nThis action cannot be undone!",
                "⚠ Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );

            if (result != MessageBoxResult.Yes)
                return;

            try
                {
                IsLoading = true;
                StopGameLoops();

                string deletedKingdomName = _currentKingdom.KingdomName;
                await _dbService.DeleteKingdomAsync(_currentUser.UserId, _currentKingdom.Id);

                // Refresh user data
                _currentUser = await _dbService.GetUserByIdAsync(_currentUser.UserId);

                // Update kingdoms collection
                UserKingdoms.Clear();
                foreach (var k in _currentUser.SavedKingdoms)
                    {
                    UserKingdoms.Add(k);
                    }

                LogEvent($"Kingdom '{deletedKingdomName}' deleted!");

                // Load another kingdom or clear the screen
                _currentKingdom = _currentUser.SavedKingdoms.FirstOrDefault();
                if (_currentKingdom != null)
                    {
                    await LoadKingdomData(_currentKingdom);
                    }
                else
                    {
                    // Clear all UI
                    OwnedBuildings.Clear();
                    ShopBuildings.Clear();
                    Gold = 0;
                    KingdomName = string.Empty;
                    Population = 0;
                    MaxPopulation = 0;
                    Happiness = 0;
                    GoldPerSecond = 0;
                    LogEvent("No kingdoms remaining. Create a new one!");
                    }
                }
            catch (Exception ex)
                {
                MessageBox.Show($"Error deleting kingdom: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            finally
                {
                IsLoading = false;
                }
            }

        #endregion

        #region Game Logic

        private void RecalculateKingdomStats()
            {
            if (_currentKingdom == null || _buildingTemplates == null)
                return;

            // Recalculate all owned buildings
            foreach (var ownedBuilding in _currentKingdom.OwnedBuildings)
                {
                var template = _buildingTemplates.FirstOrDefault(t => t.Name == ownedBuilding.BuildingName);
                if (template != null)
                    {
                    ownedBuilding.RecalculateTotals(template);

                    //Church building check
                    if (ownedBuilding.BuildingName == "Church")
                        {
                            ownedBuilding.RecalculateTotals(template, true, Happiness);
                        }
                    }
                }

            // Update kingdom totals
            GoldPerSecond = _currentKingdom.OwnedBuildings.Sum(ob => ob.TotalIncome);
            _currentKingdom.GoldPerSecond = GoldPerSecond;
            _currentKingdom.Gold += _currentKingdom.GoldPerSecond;

            Population = _currentKingdom.OwnedBuildings.Sum(ob => ob.TotalPopulationCost);
            _currentKingdom.Population = Population;

            MaxPopulation = 5 + _currentKingdom.OwnedBuildings.Sum(ob => ob.TotalMaxPopulation);
            _currentKingdom.MaxPopulation = MaxPopulation;

            HappinessIncrease = _currentKingdom.OwnedBuildings.Sum(ob => ob.TotalHappinessIncrease);
            _currentKingdom.HappinessIncrease = HappinessIncrease;

            HappinessDecrease = _currentKingdom.OwnedBuildings.Sum(ob => ob.TotalHappinessDecrease);
            _currentKingdom.HappinessDecrease = HappinessDecrease;

            _currentKingdom.Happiness += _currentKingdom.HappinessIncrease - _currentKingdom.HappinessDecrease;
            _currentKingdom.Happiness = Math.Clamp(_currentKingdom.Happiness, 0, 100);
            }

        private async Task GameTick()
            {
            if (_gameTick == null)
                return;

            try
                {
                while (await _gameTick.WaitForNextTickAsync())
                    {
                    if (_currentKingdom == null)
                        break;
                    RecalculateKingdomStats();
                    HappinessDisplay = _currentKingdom.HappinessIncrease - _currentKingdom.HappinessDecrease;
                    Happiness = _currentKingdom.Happiness;
                    Gold = _currentKingdom.Gold;
                    }
                }
            catch (OperationCanceledException)
                {
                // Timer was cancelled, this is expected
                }
            }

        private async Task SaveGameTimerAsync()
            {
            if (_saveTimer == null)
                return;

            try
                {
                while (await _saveTimer.WaitForNextTickAsync())
                    {
                    await SaveGameAsync();
                    }
                }
            catch (OperationCanceledException)
                {
                // Timer was cancelled, this is expected
                }
            }

        private async Task SaveGameAsync()
            {
            if (_currentKingdom?.Id == null)
                return;

            try
                {
                _currentKingdom.Gold = Gold;
                _currentKingdom.GoldPerSecond = GoldPerSecond;
                _currentKingdom.Population = Population;
                _currentKingdom.MaxPopulation = MaxPopulation;
                _currentKingdom.Happiness = Happiness;
                _currentKingdom.HappinessIncrease = HappinessIncrease;
                _currentKingdom.HappinessDecrease = HappinessDecrease;

                bool success = await _dbService.UpdateKingdomAsync(_currentUser.UserId, _currentKingdom);
                if (success)
                    {
                    Debug.WriteLine($"[SaveGame] Kingdom '{_currentKingdom.KingdomName}' saved at {DateTime.Now:HH:mm:ss}");
                    }
                else
                    {
                    Debug.WriteLine($"[SaveGame] Failed to save kingdom at {DateTime.Now:HH:mm:ss}");
                    }
                }
            catch (Exception ex)
                {
                Debug.WriteLine($"[SaveGame] Error: {ex.Message}");
                }
            }

        private void StopGameLoops()
            {
            _gameTick?.Dispose();
            _gameTick = null;
            _saveTimer?.Dispose();
            _saveTimer = null;
            }

        #endregion

        #region Building Operations

        private async Task BuyBuilding(object? parameter)
            {
            if (parameter is not BuildingViewModel buildingViewModel || _currentKingdom == null)
                return;

            var template = buildingViewModel.BuildingTemplate;

            if (Gold < buildingViewModel.CurrentCost)
                {
                LogEvent($"Not enough gold to buy {template.Name}!");
                return;
                }

            if (Population + template.PopulationCost > MaxPopulation)
                {
                LogEvent($"Not enough population capacity for {template.Name}!");
                return;
                }

            try
                {
                Gold -= buildingViewModel.CurrentCost;
                _currentKingdom.Gold = Gold;

                var ownedBuilding = _currentKingdom.OwnedBuildings
                    .FirstOrDefault(ob => ob.BuildingName == template.Name);

                if (ownedBuilding == null)
                    {
                    ownedBuilding = new OwnedBuilding
                        {
                        BuildingName = template.Name,
                        Count = 1,
                        Level = 1
                        };
                    ownedBuilding.RecalculateTotals(template);
                    _currentKingdom.OwnedBuildings.Add(ownedBuilding);

                    LogEvent($"Bought a {template.Name}!");
                    }
                else
                    {
                    ownedBuilding.Count++;
                    ownedBuilding.RecalculateTotals(template);

                    LogEvent($"Bought another {template.Name}!");
                    }

                RecalculateKingdomStats();
                await RefreshBuildingCollections();
                await SaveGameAsync();
                }
            catch (Exception ex)
                {
                LogEvent($"Error buying building: {ex.Message}");
                }
            }

        private async Task OpenBuildingDialog(BuildingViewModel buildingViewModel)
            {
            var template = buildingViewModel.BuildingTemplate;
            var ownedBuilding = buildingViewModel.OwnedBuilding;

            var dialogViewModel = new BuildingDetailDialogViewModel(
                template,
                ownedBuilding,
                Gold,
                Population,
                () => GetPopulation(true),
                UpdateGold,
                UpdateGameStats,
                () => GetOwnedBuilding(template.Name)
            );

            var view = new BuildingDetailDialog
                {
                DataContext = dialogViewModel
                };

            await DialogHost.Show(view, "MainDialogHost");
            await RefreshBuildingCollections();
            }

        private async Task RefreshBuildingCollections()
            {
            foreach (var template in _buildingTemplates)
                {
                var ownedBuilding = _currentKingdom?.OwnedBuildings
                    .FirstOrDefault(owned => owned.BuildingName == template.Name);

                var existingOwned = OwnedBuildings.FirstOrDefault(vm => vm.Name == template.Name);
                var existingShop = ShopBuildings.FirstOrDefault(vm => vm.Name == template.Name);

                if (ownedBuilding != null && ownedBuilding.Count > 0)
                    {
                    if (existingOwned == null)
                        {
                        if (existingShop != null)
                            {
                            ShopBuildings.Remove(existingShop);
                            }
                        OwnedBuildings.Add(new BuildingViewModel(template, ownedBuilding));
                        }
                    else
                        {
                        existingOwned.OwnedBuilding = ownedBuilding;
                        }
                    }
                else
                    {
                    if (existingShop == null)
                        {
                        if (existingOwned != null)
                            {
                            OwnedBuildings.Remove(existingOwned);
                            }
                        ShopBuildings.Add(new BuildingViewModel(template, null));
                        }
                    }
                }
            }

        #endregion

        #region Helper Methods

        private OwnedBuilding? GetOwnedBuilding(string buildingName)
            {
            return _currentKingdom?.OwnedBuildings
                .FirstOrDefault(ob => ob.BuildingName == buildingName);
            }

        private void UpdateGold(double amount)
            {
            Gold += amount;
            if (_currentKingdom != null)
                {
                _currentKingdom.Gold = Gold;
                }
            }
        /// <summary>
        /// Returns either the current population or max population based on the parameter
        /// </summary>
        /// <param name="isMaxPopulation"></param>
        /// <returns></returns>
        private int GetPopulation(bool isMaxPopulation = false)
            {
            return isMaxPopulation ? MaxPopulation : Population;
            }
        private void UpdateGameStats()
            {
            RecalculateKingdomStats();
            _ = SaveGameAsync();
            }

        private void LogEvent(string message)
            {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            EventsLog = $"[{timestamp}] {message}\n{EventsLog}";

            if (EventsLog.Length > 1000)
                {
                EventsLog = EventsLog.Substring(0, 1000);
                }
            }

        private void OpenSettings()
            {
            MessageBox.Show("Settings not implemented yet", "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
            }

        #endregion

        }
    }