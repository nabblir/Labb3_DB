using Labb3_DB.Commands;
using Labb3_DB.Data;
using Labb3_DB.Models;
using Labb3_DB.Mongo;
using Labb3_DB.ViewModels;
using Labb3_DB.Views;
using MaterialDesignThemes.Wpf;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace Labb3_DB.ViewModels
    {
    public class MainViewModel : BaseViewModel
        {
        private readonly DatabaseService _dbService;
        private Kingdom _currentKingdom;
        private List<Building> _buildingTemplates;
        private PeriodicTimer _gameTick;
        private PeriodicTimer _saveTimer;

        private string _kingdomName;
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

        private string _eventsLog;
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

        private float _happiness;
        public float Happiness
            {
            get => _happiness;
            set => SetProperty(ref _happiness, value);
            }

        public ObservableCollection<BuildingViewModel> OwnedBuildings { get; set; }
        public ObservableCollection<BuildingViewModel> ShopBuildings { get; set; }

        public ICommand OpenBuildingDialogCommand { get; }
        public ICommand SaveGameCommand { get; }
        public ICommand ResetKingdomCommand { get; }
        public ICommand LoadGameCommand { get; }
        public ICommand SettingsCommand { get; }
        public ICommand ExitCommand { get; }
        public ICommand BuyBuildingCommand { get; }

        public MainViewModel()
            {
            _dbService = new DatabaseService();
            OwnedBuildings = new ObservableCollection<BuildingViewModel>();
            ShopBuildings = new ObservableCollection<BuildingViewModel>();

            OpenBuildingDialogCommand = new RelayCommand(async (building) =>
            {
                if (building is BuildingViewModel bvm)
                    {
                    await OpenBuildingDialog(bvm);
                    }
            });

            SaveGameCommand = new RelayCommand(async (_) => await SaveGameAsync());
            ResetKingdomCommand = new RelayCommand(async (_) => await ResetKingdom());
            LoadGameCommand = new RelayCommand(async (_) => await LoadGameDataAsync());
            SettingsCommand = new RelayCommand(_ => OpenSettings());
            ExitCommand = new RelayCommand(_ => Application.Current.Shutdown());
            BuyBuildingCommand = new RelayCommand(async (param) => await BuyBuilding(param));

            SaveGameCommand = new RelayCommand(async (_) => await SaveGameAsync());
            ResetKingdomCommand = new RelayCommand(async (_) => await ResetKingdom());
            _ = LoadGameDataAsync();
            }

        private async Task LoadGameDataAsync()
            {
            try
                {
                // Initialize database and buildings collection
                await _dbService.InitializeDatabaseAsync();
                await _dbService.InitializeBuildingsAsync();

                // Load kingdom
                _currentKingdom = await _dbService.GetKingdomAsync();

                if (_currentKingdom != null)
                    {
                    Gold = _currentKingdom.Gold;
                    KingdomName = _currentKingdom.KingdomName;
                    GoldPerSecond = _currentKingdom.GoldPerSecond;
                    Population = _currentKingdom.Population;
                    MaxPopulation = _currentKingdom.MaxPopulation;
                    Happiness = _currentKingdom.Happiness;
                    HappinessDecrease = _currentKingdom.HappinessDecrease;
                    HappinessIncrease = _currentKingdom.HappinessIncrease;
                    EventsLog = "";
                    LogEvent($"Kingdom {_currentKingdom.KingdomName} loaded successfully!");
                    }

                // Load building templates from database
                _buildingTemplates = await _dbService.GetAllBuildingsAsync();

                OwnedBuildings.Clear();
                ShopBuildings.Clear();

                // Create ViewModels for all buildings
                foreach (var template in _buildingTemplates)
                    {
                    var ownedBuilding = _currentKingdom?.OwnedBuildings
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
                _gameTick = new PeriodicTimer(TimeSpan.FromSeconds(1));
                _ = GameTick();

                _saveTimer = new PeriodicTimer(TimeSpan.FromSeconds(10));
                _ = SaveGameTimerAsync();
                }
            catch (Exception ex)
                {
                LogEvent($"ERROR: {ex.Message}");
                MessageBox.Show($"Failed to load game: {ex.Message}");
                }
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

        private async Task ResetKingdom()
            {
            var result = MessageBox.Show(
                "Are you sure you want to reset your kingdom?\n\nThis will delete ALL your progress!\nThis action cannot be undone!",
                "⚠ Confirm Reset",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );

            if (result == MessageBoxResult.Yes)
                {
                try
                    {
                    _gameTick?.Dispose();
                    _saveTimer?.Dispose();

                    if (_currentKingdom?.Id != null)
                        {
                        await _dbService.DeleteKingdomAsync(_currentKingdom.Id);
                        }

                    LogEvent("Kingdom reset! Restarting application...");
                    await Task.Delay(1000);

                    System.Diagnostics.Process.Start(
                        System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName
                    );
                    Application.Current.Shutdown();
                    }
                catch (Exception ex)
                    {
                    MessageBox.Show($"Error resetting kingdom: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }

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
                    }
                }

            // Update kingdom totals
            GoldPerSecond = _currentKingdom.OwnedBuildings.Sum(ownedBuilding => ownedBuilding.TotalIncome);
            _currentKingdom.GoldPerSecond = GoldPerSecond;

            Population = _currentKingdom.OwnedBuildings.Sum(ownedBuilding => ownedBuilding.TotalPopulationCost);
            _currentKingdom.Population = Population;

            MaxPopulation = 5 + _currentKingdom.OwnedBuildings.Sum(ownedBuilding => ownedBuilding.TotalMaxPopulation);
            _currentKingdom.MaxPopulation = MaxPopulation;

            HappinessIncrease = _currentKingdom.OwnedBuildings.Sum(ownedBuilding => ownedBuilding.TotalHappinessIncrease);
            _currentKingdom.HappinessIncrease = HappinessIncrease;

            HappinessDecrease = _currentKingdom.OwnedBuildings.Sum(ownedBuilding => ownedBuilding.TotalHappinessDecrease);
            _currentKingdom.HappinessDecrease = HappinessDecrease;
            }

        private async Task GameTick()
            {
            while (await _gameTick.WaitForNextTickAsync())
                {
                _currentKingdom.Gold += _currentKingdom.GoldPerSecond;
                _currentKingdom.Happiness += _currentKingdom.HappinessIncrease - _currentKingdom.HappinessDecrease;
                _currentKingdom.Happiness = Math.Clamp(_currentKingdom.Happiness, 0, 100);

                Happiness = _currentKingdom.Happiness;
                Gold = _currentKingdom.Gold;
                }
            }

        private async Task SaveGameTimerAsync()
            {
            while (await _saveTimer.WaitForNextTickAsync())
                {
                await SaveGameAsync();
                }
            }

        private async Task SaveGameAsync()
            {
            if (_currentKingdom?.Id != null)
                {
                _currentKingdom.Gold = Gold;
                _currentKingdom.GoldPerSecond = GoldPerSecond;
                _currentKingdom.Population = Population;
                _currentKingdom.MaxPopulation = MaxPopulation;
                _currentKingdom.Happiness = Happiness;
                _currentKingdom.HappinessIncrease = HappinessIncrease;
                _currentKingdom.HappinessDecrease = HappinessDecrease;

                await _dbService.UpdateKingdomAsync(_currentKingdom);
                Debug.WriteLine($"[SaveGame] Kingdom saved at {DateTime.Now:HH:mm:ss}");
                }
            }

        private async Task BuyBuilding(object? parameter)
            {
            var buildingViewModel = parameter as BuildingViewModel;
            if (buildingViewModel == null)
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

            // Move building from shop to owned
            await RefreshBuildingCollections();

            // Save
            await SaveGameAsync();
            }

        private async Task OpenBuildingDialog(BuildingViewModel buildingViewModel)
            {
            var template = buildingViewModel.BuildingTemplate;
            var ownedBuilding = buildingViewModel.OwnedBuilding;


            var dialogViewModel = new BuildingDetailDialogViewModel(
                template,
                ownedBuilding,
                Gold,
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

        private OwnedBuilding? GetOwnedBuilding(string buildingName)
            {
            return _currentKingdom?.OwnedBuildings
                .FirstOrDefault(ownedBuilding => ownedBuilding.BuildingName == buildingName);
            }

        private void UpdateGold(double amount)
            {
            Gold += amount;
            _currentKingdom.Gold = Gold;
            }

        private void UpdateGameStats()
            {
            RecalculateKingdomStats();

            // Save immediately after purchase/upgrade/sell
            _ = SaveGameAsync();
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

        private void OpenSettings()
            {
            MessageBox.Show("Settings not implemented yet", "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }