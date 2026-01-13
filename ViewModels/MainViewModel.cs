using Labb3_DB.Commands;
using Labb3_DB.Models;
using Labb3_DB.Mongo;
using Labb3_DB.ViewModels;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Timers;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using Labb3_DB.Data;
using Microsoft.Xaml.Behaviors;

public class MainViewModel : BaseViewModel
    {
    private readonly DatabaseService _dbService;
    private Kingdom _currentKingdom;
    private PeriodicTimer _gameTick;
    private PeriodicTimer _saveTimer;
    // UI properties
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
    private string _eventsLog;
    public string EventsLog
        {
        get => _eventsLog;
        set => SetProperty(ref _eventsLog, value);
        }

    private float _happiness;
    public float Happiness
        {
        get => _happiness;
        set => SetProperty(ref _happiness, value);
        }
    // Collections ListViews
    public ObservableCollection<BuildingViewModel> Buildings { get; set; }
    public ObservableCollection<BuildingViewModel> ShopBuildings { get; set; }

    // Buttons commands
    public ICommand BuyBuildingCommand { get; }
    public ICommand SaveGameCommand { get; }
    public ICommand ResetKingdomCommand { get; }

    public MainViewModel()
        {
        _dbService = new DatabaseService();

        // Initiera collections
        Buildings = new ObservableCollection<BuildingViewModel>();
        ShopBuildings = new ObservableCollection<BuildingViewModel>();

        // Init commands
        BuyBuildingCommand = new RelayCommand(async (param) => await BuyBuilding(param));
        SaveGameCommand = new RelayCommand(async (_) => await SaveGameTimerAsync());
        ResetKingdomCommand = new RelayCommand(async (_) => await ResetKingdom());

        // Ladda data SIST - detta måste vara async
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
                Happiness = _currentKingdom.Happiness;
                LogEvent($"Kingdom {_currentKingdom.KingdomName} loaded successfully!");
                }

            var ownedBuildings = await _dbService.GetAllBuildingsAsync();

            var shopBuildings = ShopData.GetShopBuildings();

            foreach (var ownedBuilding in ownedBuildings)
                {
                var shopBuilding = shopBuildings.Find(b => b.Name == ownedBuilding.Name);
                if (shopBuilding != null)
                    {
                    shopBuilding.Count = ownedBuilding.Count;
                    shopBuilding.Level = ownedBuilding.Level;
                    }
                }

            Buildings.Clear();
            ShopBuildings.Clear();


            foreach (var building in shopBuildings)
                {
                ShopBuildings.Add(new BuildingViewModel(building));
                }

            foreach (var building in shopBuildings.Where(b => b.Count > 0))
                {
                Buildings.Add(new BuildingViewModel(building));
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
        // TODO: Implement buy logic
        }

    private async Task ResetKingdom()
        {
        var result = MessageBox.Show(
            "Are you sure you want to reset your kingdom?\n\n" +
            "This will delete ALL your progress!\n" +
            "This action cannot be undone!",
            "⚠ Confirm Reset",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning
        );

        if (result == MessageBoxResult.Yes)
            {
            try
                {
                _gameTick?.Dispose();

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


    #region Timers

    private async Task GameTick()
        {
        while (await _gameTick.WaitForNextTickAsync())
            {
            //DB update
            _currentKingdom.Gold += _currentKingdom.GoldPerSecond;
            _currentKingdom.Happiness -= 0.1f;
            _currentKingdom.Happiness = Math.Clamp(_currentKingdom.Happiness, 0, 100);


            //UI update
            Happiness = _currentKingdom.Happiness;
            Gold = _currentKingdom.Gold;
            }
        }
    private async Task SaveGameTimerAsync()
        {
        // TODO: Implement save logic
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

    #endregion
    }