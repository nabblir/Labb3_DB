
# Starship Alice - Kingdom Building Game

A WPF-based idle kingdom building game built with .NET 8 and MongoDB. Manage your kingdom, build structures, and grow your economy while maintaining citizen happiness and population balance.

## 🎮 Project Overview

Starship Alice is a feature-rich kingdom simulation game demonstrating advanced .NET development patterns including MVVM architecture, async/await operations, MongoDB integration, and real-time data binding. The game combines economic simulation with resource management, providing an engaging progression loop.

## ✨ Key Features

### Game Mechanics
- **8 Building Types** - Production (Farm, Woodcutter, Mine), Housing (House, Castle), Entertainment (Tavern, Brothel, Church)
- **Economic System** - Exponential cost scaling, income multiplication based on level
- **Population Management** - Track current/max population with building-based requirements
- **Happiness System** - Dynamic happiness calculation affecting building performance
- **Level Upgrades** - Buildings can be upgraded to increase income per second
- **Special Logic** - Church building has conditional income based on happiness thresholds

### Technical Features
- **User Authentication** - SHA256 password hashing with account management
- **Multi-Kingdom Support** - Create and switch between multiple game saves per user
- **Real-time Statistics** - LiveCharts visualization of gold and happiness trends
- **Auto-save System** - Game saves every 10 seconds automatically
- **Persistent Data** - All progress stored in MongoDB and survives application restart
- **Responsive UI** - Material Design implementation with async operations preventing UI freeze

## 🏗️ Architecture

### Database Design (MongoDB)


KevinSpehling (Database)
├── users (Collection)
│   ├── _id: ObjectId
│   ├── userID: string (GUID)
│   ├── userName: string
│   └── kingdomIds: string[] (references to kingdoms)
│
├── kingdoms (Collection)
│   ├── _id: ObjectId
│   ├── kingdomName: string
│   ├── userId: string (reference to user)
│   ├── gold: double
│   ├── goldPerSecond: double
│   ├── population: int
│   ├── maxPopulation: int
│   ├── happiness: float (0-100)
│   ├── happinessDecrease: float
│   ├── happinessIncrease: float
│   ├── ownedBuildings: OwnedBuilding[] (embedded)
│   ├── eventsLog: string
│   └── lastSaved: DateTime
│
└── buildings (Collection)
    ├── _id: ObjectId
    ├── name: string
    ├── description: string
    ├── buildingType: string (Production/Housing/Entertainment)
    ├── baseCost: double
    ├── costMultiplier: double (1.15)
    ├── baseIncome: double
    ├── populationCost: int
    ├── maxPopulation: int
    ├── happinessIncrease: float
    └── happinessDecrease: float

**Design Rationale**: Normalized schema with kingdoms stored in a separate collection (not embedded) to avoid data duplication and allow independent updates. Users store only kingdom IDs, which are resolved at runtime via `DatabaseService`.

### MVVM Architecture

Views (XAML + Code-behind)
    ↓
ViewModels (Business Logic + State)
    ↓
Models (Data Classes)
    ↓
Mongo/DatabaseService (Data Access)
    ↓
MongoDB

**Key ViewModels**:
- `LoginViewModel` - Authentication and user management
- `MainViewModel` - Game state, building management, kingdom operations
- `BuildingDetailDialogViewModel` - Building interaction and upgrades
- `BuildingViewModel` - Individual building presentation
- `StatsViewModel` - Real-time statistics and charting
- `KingdomSelectionViewModel` - Multi-kingdom selection

## 🔄 Async/Await Pattern

All long-running operations use `async/await`:


// Database operations are non-blocking
public async Task<User?> GetUserAsync(string username, string password)
{
    var user = await _userCollection.Find(...).FirstOrDefaultAsync();
    if (user != null)
    {
        user.SavedKingdoms = await GetUserKingdomsAsync(user.UserId);
    }
    return user;
}

// Game loop runs every 1 second
private async Task GameTick()
{
    while (await _gameTick.WaitForNextTickAsync())
    {
        RecalculateKingdomStats();
        Happiness = _currentKingdom.Happiness;
        Gold = _currentKingdom.Gold;
    }
}

// Auto-save runs every 10 seconds
private async Task SaveGameTimerAsync()
{
    while (await _saveTimer.WaitForNextTickAsync())
    {
        await SaveGameAsync();
    }
}

## 🎯 Game Balance

### Cost Formula
CurrentCost = BaseCost × CostMultiplier^Count
Example: Farm (BaseCost=20) costs: 20 → 23 → 26.45 → 30.42...

### Income Formula
IncomePerBuilding = BaseIncome × (Level × 5)
TotalIncome = IncomePerBuilding × Count
Example: Farm (BaseIncome=1) at Level 1 owns 3: 1 × 5 × 3 = 15 gold/sec

### Upgrade Cost Formula
UpgradeCost = BaseCost × 2^Level × Count
Encourages strategic upgrades - expensive but worth it for income multiplication.

## 📊 Data Flow Example: Buying a Building

1. **User clicks "Purchase"** in BuildingDetailDialog
2. **Validation** - Check gold, population capacity
3. **Update ViewModel** - Deduct gold, update UI
4. **Update Model** - Increment building count, recalculate totals
5. **Update Kingdom** - Trigger stats recalculation
6. **Persist to DB** - `SaveGameAsync()` updates MongoDB
7. **Refresh UI** - All bindings update automatically

private async Task BuyMoreBuilding()
{
    if (CurrentGold >= CurrentCost && OwnedBuilding != null)
    {
        double cost = CurrentCost;
        _updateGold(-cost);              // Update parent ViewModel
        CurrentGold -= cost;             // Update local state
        
        OwnedBuilding.Count++;           // Modify model
        OwnedBuilding.RecalculateTotals(_buildingTemplate);
        
        OnPropertyChanged(nameof(Count));
        OnPropertyChanged(nameof(CurrentCost));
        
        _updateStats();                  // Recalculate all stats
        CommandManager.InvalidateRequerySuggested();
    }
}

## 🛠️ CRUD Operations

### Create
- Create User Account (RegisterAsync) → Insert into `users` collection
- Create Kingdom → Insert into `kingdoms`, add ID to user's `kingdomIds`
- Create Building (template) → Insert into `buildings`
- Create OwnedBuilding → Embedded in kingdom's `ownedBuildings`

### Read
- GetUserAsync → Query users by username/password
- GetUserKingdomsAsync → Query kingdoms by user ID
- GetAllBuildingsAsync → Query all building templates
- GetKingdomByIdAsync → Query single kingdom by ID

### Update
- UpdateKingdomAsync → Replace entire kingdom document
- UpdateBuildingAsync → Replace building template
- Auto-save updates kingdom stats every 10 seconds

### Delete
- DeleteKingdomAsync → Delete from `kingdoms` collection + remove ID from user
- DeleteAllUserKingdomsAsync → Batch delete all user's kingdoms
- DeleteBuildingAsync → Delete building template

## 🎨 UI Implementation

### Material Design
Uses `MaterialDesignInXaml` for professional, modern appearance:
- Cards for grouping related information
- Dialogs for modal interactions
- Icons from Material Design library
- Color scheme: Dark theme (#2C3E50, #34495E)

### Binding System
All UI updates through WPF data binding:
<TextBlock Text="{Binding Gold, StringFormat={}{0:F0}}" />
<Button Command="{Binding BuyBuildingCommand}" CommandParameter="{Binding}" />

### Real-time Updates
- `PeriodicTimer` every 1 second updates game state
- ObservableCollection automatically updates UI when items change
- PropertyChanged events trigger binding updates
- No manual UI refresh needed - all data-driven

## 📈 Statistics System

Real-time charting with LiveCharts showing:
- **Gold Over Time** - Total gold accumulated
- **Gold Per Second** - Income rate (dashed line)
- **Happiness Over Time** - Happiness levels 0-100%
- **Happiness Change Per Second** - Net change rate

Data stored in 7-element rolling array, updated every second.

## ⚙️ Key Classes

### Models
- `User` - Account info + kingdom references (legacy migration support)
- `Kingdom` - Game state (gold, population, buildings, happiness)
- `Building` - Building template (cost, income, effects)
- `OwnedBuilding` - Player's building instance (count, level, totals)

### ViewModels
- All inherit from `BaseViewModel` implementing `INotifyPropertyChanged`
- Commands use `RelayCommand` pattern for MVVM compliance
- Async operations prevent UI blocking

### Services
- `DatabaseService` - Centralized MongoDB operations
- All methods are `async Task`
- Full error handling and logging

## 🔐 Security

- **Password Hashing** - SHA256 with no salt (acceptable for game context)
- **Input Validation** - Username min 3 chars, password min 6 chars
- **Database Connection** - Local MongoDB only (development)
- **Error Messages** - User-friendly without exposing sensitive info



## 🚀 Getting Started

### Prerequisites
- .NET 8 SDK
- MongoDB Community Edition running on `mongodb://localhost:27017`

### Installation
git clone https://github.com/nabblir/Labb3_DB.git
cd Labb3_DB
dotnet restore
dotnet run

### First Time
1. Create account (username min 3 chars, password min 6 chars)
2. Automatically get first kingdom "Starship Alice" with 1 Farm
3. Start building and growing your economy!

