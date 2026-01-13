using Labb3_DB.Commands;
using Labb3_DB.Data;
using Labb3_DB.Models;
using Labb3_DB.Mongo;
using Labb3_DB.ViewModels;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

public class MainViewModel : BaseViewModel
    {
    private readonly DatabaseService _dbService;
    private Kingdom _currentKingdom;
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

    public ObservableCollection<BuildingViewModel> Buildings { get; set; }
    public ObservableCollection<BuildingViewModel> ShopBuildings { get; set; }

    public ICommand BuyBuildingCommand { get; }
    public ICommand SaveGameCommand { get; }
    public ICommand ResetKingdomCommand { get; }

    public MainViewModel()
        {
        _dbService = new DatabaseService();
        Buildings = new ObservableCollection<BuildingViewModel>();
        ShopBuildings = new ObservableCollection<BuildingViewModel>();

        BuyBuildingCommand = new RelayCommand(async (param) => await BuyBuilding(param));
        SaveGameCommand = new RelayCommand(async (_) => await SaveGameTimerAsync());
        ResetKingdomCommand = new RelayCommand(async (_) => await ResetKingdom());

        _ = LoadGameDataAsync();
        }

    private async Task LoadGameDataAsync()
        {
        try
            {
            await _dbService.InitializeDatabaseAsync();

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
                EventsLog = _currentKingdom.EventsLog ?? "";
                LogEvent($"Kingdom {_currentKingdom.KingdomName} loaded successfully!");
                }

            var ownedBuildings = await _dbService.GetAllBuildingsAsync();
            var shopBuildings = ShopData.GetShopBuildings();

            Buildings.Clear();
            ShopBuildings.Clear();

            foreach (var shopBuilding in shopBuildings)
                {
                var owned = ownedBuildings.FirstOrDefault(b => b.Name == shopBuilding.Name);

                if (owned != null)
                    {
                    var vm = new BuildingViewModel(owned);
                    ShopBuildings.Add(vm);
                    if (owned.Count > 0)
                        {
                        Buildings.Add(vm);
                        }
                    }
                else
                    {
                    ShopBuildings.Add(new BuildingViewModel(shopBuilding));
                    }
                }

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

        if (_currentKingdom != null)
            {
            _currentKingdom.EventsLog = EventsLog;
            }
        }

    private async Task BuyBuilding(object? parameter)
        {
        var building = parameter as BuildingViewModel;
        if (building == null)
            return;

        if (Gold < building.CurrentCost)
            {
            LogEvent($"Not enough gold to buy {building.Name}!");
            return;
            }

        if (Population + building.PopulationCost > MaxPopulation)
            {
            LogEvent($"Not enough population capacity for {building.Name}!");
            return;
            }

        Gold -= building.CurrentCost;
        _currentKingdom.Gold = Gold;

        if (building.Model.Id != null)
            {
            building.Count++;
            await _dbService.UpdateBuildingAsync(building.Model);
            LogEvent($"Bought another {building.Name}");
            }
        else
            {
            building.Count = 1;
            await _dbService.CreateBuildingAsync(building.Model);
            Buildings.Add(building);
            LogEvent($"Bought a {building.Name}");
            }

        UpdateStats(building);
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
                await _dbService.DeleteAllBuildingsAsync();

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

    private void UpdateStats(BuildingViewModel building)
        {
        if (building.HappinessDecrease != 0)
            {
            _currentKingdom.HappinessDecrease += building.HappinessDecrease;
            HappinessDecrease = _currentKingdom.HappinessDecrease;
            }

        if (building.HappinessIncrease != 0)
            {
            _currentKingdom.HappinessIncrease += building.HappinessIncrease;
            HappinessIncrease = _currentKingdom.HappinessIncrease;
            }

        if (building.BaseIncome != 0)
            {
            _currentKingdom.GoldPerSecond += building.BaseIncome;
            GoldPerSecond = _currentKingdom.GoldPerSecond;
            }

        if (building.MaxPopulation != 0)
            {
            _currentKingdom.MaxPopulation += building.MaxPopulation;
            MaxPopulation = _currentKingdom.MaxPopulation;
            }

        if (building.PopulationCost != 0)
            {
            _currentKingdom.Population += building.PopulationCost;
            Population = _currentKingdom.Population;
            }
        }

    private async Task GameTick()
        {
        while (await _gameTick.WaitForNextTickAsync())
            {
            _currentKingdom.Gold += _currentKingdom.GoldPerSecond;
            _currentKingdom.Happiness = _currentKingdom.Happiness - ( _currentKingdom.HappinessDecrease + _currentKingdom.HappinessIncrease );
            _currentKingdom.Happiness = Math.Clamp(_currentKingdom.Happiness, 0, 100);

            Happiness = _currentKingdom.Happiness;
            Gold = _currentKingdom.Gold;
            }
        }

    private async Task SaveGameTimerAsync()
        {
        while (await _saveTimer.WaitForNextTickAsync())
            {
            if (_currentKingdom?.Id != null)
                {
                _currentKingdom.Gold = Gold;
                _currentKingdom.GoldPerSecond = GoldPerSecond;
                _currentKingdom.Population = Population;
                _currentKingdom.Happiness = Happiness;
                _currentKingdom.EventsLog = EventsLog;
                await _dbService.UpdateKingdomAsync(_currentKingdom);
                }
            }
        }
    }