using Labb3_DB.ViewModels;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Painting.Effects;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.Diagnostics;
using LiveChartsCore.SkiaSharpView.Painting.ImageFilters;
public class StatsViewModel : BaseViewModel
    {
    private double currentGoldPerSecond;
    private float currentHappinessPerSecond;
    private float currentHappiness;
    private double currentGold;
    private MainViewModel mainViewModel;

    public ObservableCollection<double> GoldHistory { get; set; }
    public ObservableCollection<double> GoldPerSecondHistory { get; set; }
    public ObservableCollection<double> HappinessHistory { get; set; }
    public ObservableCollection<double> HappinessPerSecondHistory { get; set; }

    private PeriodicTimer? _updateStats;

    private ISeries[] _goldSeries;
    public ISeries[] GoldSeries
        {
        get => _goldSeries;
        set => SetProperty(ref _goldSeries, value);
        }

    private ISeries[] _happinessSeries;
    public ISeries[] HappinessSeries
        {
        get => _happinessSeries;
        set => SetProperty(ref _happinessSeries, value);
        }

    #region Properties
    public double CurrentGoldPerSecond
        {
        get => currentGoldPerSecond;
        set => SetProperty(ref currentGoldPerSecond, value);
        }
    public float CurrentHappinessPerSecond
        {
        get => currentHappinessPerSecond;
        set => SetProperty(ref currentHappinessPerSecond, value);
        }

    public Axis[] GoldAxes { get; set; }
    public Axis[] HappinessAxes { get; set; }
    #endregion

    public StatsViewModel(MainViewModel mainVM)
        {
        mainViewModel = mainVM;
        currentGoldPerSecond = mainViewModel.CurrentKingdom.GoldPerSecond;
        currentHappinessPerSecond = mainViewModel.GetHappinessPerSecond();
        currentHappiness = mainViewModel.CurrentKingdom.Happiness;
        currentGold = mainViewModel.Gold;

        GoldHistory = new ObservableCollection<double> { 0, 0, 0, 0, 0, 0, 0 };
        GoldPerSecondHistory = new ObservableCollection<double> { 0, 0, 0, 0, 0, 0, 0 };
        HappinessHistory = new ObservableCollection<double> { 0, 0, 0, 0, 0, 0, 0 };
        HappinessPerSecondHistory = new ObservableCollection<double> { 0, 0, 0, 0, 0, 0, 0 };

        /*
         * I could not find any good updated/current documentation for LiveCharts2, so had to resort to AI for stylizing: 
         */
        _goldSeries = new ISeries[]
        {
            new LineSeries<double>
            {
                Name = "Gold",
                Values = GoldHistory,
                Fill = new SolidColorPaint(SKColors.Gold.WithAlpha(50)),
                Stroke = new SolidColorPaint(SKColors.Gold) { StrokeThickness = 2 },
                GeometrySize = 0, 
                LineSmoothness = 0.5
            },
            new LineSeries<double>
            {
                Name = "Gold/Second",
                Values = GoldPerSecondHistory,
                Fill = null,
                Stroke = new SolidColorPaint(SKColors.Orange.WithAlpha(150)) 
                { 
                    StrokeThickness = 1,
                    PathEffect = new DashEffect(new float[] { 4, 2 })
                },
                GeometrySize = 0,
                LineSmoothness = 0.5
            }
        };

        _happinessSeries = new ISeries[]
        {
            new LineSeries<double>
            {
                Name = "Happiness",
                Values = HappinessHistory,
                Fill = new SolidColorPaint(SKColors.LawnGreen.WithAlpha(50)),
                Stroke = new SolidColorPaint(SKColors.LawnGreen) { StrokeThickness = 2 },
                GeometrySize = 0,
                LineSmoothness = 0.5
            },
            new LineSeries<double>
            {
                Name = "Happiness/Second",
                Values = HappinessPerSecondHistory,
                Fill = null,
                Stroke = new SolidColorPaint(SKColors.YellowGreen.WithAlpha(150)) 
                { 
                    StrokeThickness = 1,
                    PathEffect = new DashEffect(new float[] { 4, 2 })
                },
                GeometrySize = 0,
                LineSmoothness = 0.5
            }
        };

        GoldAxes = new Axis[]
        {
            new Axis
            {
                MinStep = 1,
                MinLimit = 0,
                LabelsPaint = new SolidColorPaint(SKColors.Gold),
                TextSize = 9,
                SeparatorsPaint = new SolidColorPaint(SKColors.White.WithAlpha(20))
                {
                    StrokeThickness = 1
                }
            }
        };

        HappinessAxes = new Axis[]
        {
            new Axis
            {
                MinStep = 20,
                MinLimit = 0,
                MaxLimit = 100,
                Labeler = value => value.ToString("F0") + "%",
                LabelsPaint = new SolidColorPaint(SKColors.LawnGreen),
                TextSize = 9,
                SeparatorsPaint = new SolidColorPaint(SKColors.White.WithAlpha(20))
                {
                    StrokeThickness = 1
                }
            }
        };

        _updateStats = new PeriodicTimer(TimeSpan.FromSeconds(1));
        _ = StatUpdate();
        }

    private async Task StatUpdate()
        {
        if (_updateStats == null)
            return;
        try
            {
            while (await _updateStats.WaitForNextTickAsync())
                {
                if (mainViewModel.CurrentKingdom == null)
                    break;

                currentGold = mainViewModel.Gold;
                currentGoldPerSecond = mainViewModel.CurrentKingdom.GoldPerSecond;
                currentHappinessPerSecond = mainViewModel.GetHappinessPerSecond();
                currentHappiness = mainViewModel.CurrentKingdom.Happiness;

                bool arrayFull = GoldHistory[GoldHistory.Count - 1] != 0;

                if (!arrayFull)
                    {
                    for (int i = 0; i < GoldHistory.Count; i++)
                        {
                        if (GoldHistory[i] == 0)
                            {
                            GoldHistory[i] = currentGold;
                            GoldPerSecondHistory[i] = currentGoldPerSecond;
                            HappinessHistory[i] = currentHappiness;
                            HappinessPerSecondHistory[i] = currentHappinessPerSecond;
                            break;
                            }
                        }
                    }
                else
                    {
                    for (int i = 0; i < GoldHistory.Count - 1; i++)
                        {
                        GoldHistory[i] = GoldHistory[i + 1];
                        GoldPerSecondHistory[i] = GoldPerSecondHistory[i + 1];
                        HappinessHistory[i] = HappinessHistory[i + 1];
                        HappinessPerSecondHistory[i] = HappinessPerSecondHistory[i + 1];
                        }
                    GoldHistory[GoldHistory.Count - 1] = currentGold;
                    GoldPerSecondHistory[GoldPerSecondHistory.Count - 1] = currentGoldPerSecond;
                    HappinessHistory[HappinessHistory.Count - 1] = currentHappiness;
                    HappinessPerSecondHistory[HappinessPerSecondHistory.Count - 1] = currentHappinessPerSecond;
                    }
                }
            }
        catch (OperationCanceledException)
            {
            // Timer was cancelled, this is expected
            }
        }
    }