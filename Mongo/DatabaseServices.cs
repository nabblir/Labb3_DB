using Labb3_DB.Data;
using Labb3_DB.Models;
using MongoDB.Driver;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Labb3_DB.Mongo
    {
    /// <summary>
    /// Handles communication with the MongoDB database
    /// </summary>
    public class DatabaseService
        {
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<Building> _buildingsCollection;
        private readonly IMongoCollection<User> _userCollection;
        private const string ConnectionString = "mongodb://localhost:27017";
        private const string DatabaseName = "KevinSpehling";

        public DatabaseService()
            {
            var client = new MongoClient(ConnectionString);
            _database = client.GetDatabase(DatabaseName);

            _buildingsCollection = _database.GetCollection<Building>("buildings");
            _userCollection = _database.GetCollection<User>("users");
            }

        #region Kingdom CRUD Operations (Embedded in User)

        /// <summary>
        /// Creates a new kingdom for a user
        /// </summary>
        public async Task<Kingdom> CreateKingdomAsync(string userId, Kingdom kingdom)
            {
            // Generate a unique ID for the kingdom if not set
            if (string.IsNullOrEmpty(kingdom.Id))
                {
                kingdom.Id = ObjectId.GenerateNewId().ToString();
                }

            kingdom.UserId = userId;
            kingdom.LastSaved = DateTime.UtcNow;

            var filter = Builders<User>.Filter.Eq(u => u.UserId, userId);
            var update = Builders<User>.Update.Push(u => u.SavedKingdoms, kingdom);

            await _userCollection.UpdateOneAsync(filter, update);
            return kingdom;
            }

        /// <summary>
        /// Gets all kingdoms for a specific user
        /// </summary>
        public async Task<List<Kingdom>> GetUserKingdomsAsync(string userId)
            {
            var user = await _userCollection
                .Find(u => u.UserId == userId)
                .FirstOrDefaultAsync();

            return user?.SavedKingdoms ?? new List<Kingdom>();
            }

        /// <summary>
        /// Gets a specific kingdom by ID for a user
        /// </summary>
        public async Task<Kingdom?> GetKingdomByIdAsync(string userId, string kingdomId)
            {
            var user = await _userCollection
                .Find(u => u.UserId == userId)
                .FirstOrDefaultAsync();

            return user?.SavedKingdoms.FirstOrDefault(k => k.Id == kingdomId);
            }

        /// <summary>
        /// Updates a specific kingdom for a user
        /// </summary>
        public async Task<bool> UpdateKingdomAsync(string userId, Kingdom kingdom)
            {
            kingdom.LastSaved = DateTime.UtcNow;

            var filter = Builders<User>.Filter.And(
                Builders<User>.Filter.Eq(u => u.UserId, userId),
                Builders<User>.Filter.ElemMatch(u => u.SavedKingdoms, k => k.Id == kingdom.Id)
            );

            var update = Builders<User>.Update.Set("savedKingdoms.$", kingdom);

            var result = await _userCollection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
            }

        /// <summary>
        /// Deletes a specific kingdom for a user
        /// </summary>
        public async Task<bool> DeleteKingdomAsync(string userId, string kingdomId)
            {
            var filter = Builders<User>.Filter.Eq(u => u.UserId, userId);
            var update = Builders<User>.Update.PullFilter(
                u => u.SavedKingdoms,
                k => k.Id == kingdomId
            );

            var result = await _userCollection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
            }

        /// <summary>
        /// Deletes all kingdoms for a user
        /// </summary>
        public async Task<bool> DeleteAllUserKingdomsAsync(string userId)
            {
            var filter = Builders<User>.Filter.Eq(u => u.UserId, userId);
            var update = Builders<User>.Update.Set(u => u.SavedKingdoms, new List<Kingdom>());

            var result = await _userCollection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
            }

        #endregion

        #region Building CRUD Operations (Shop Templates)

        /// <summary>
        /// Creates a new building template
        /// </summary>
        public async Task<Building> CreateBuildingAsync(Building building)
            {
            await _buildingsCollection.InsertOneAsync(building);
            return building;
            }

        /// <summary>
        /// Fetches all building templates (shop catalog)
        /// </summary>
        public async Task<List<Building>> GetAllBuildingsAsync()
            {
            return await _buildingsCollection.Find(_ => true).ToListAsync();
            }

        /// <summary>
        /// Return building template by name
        /// </summary>
        public async Task<Building?> GetBuildingByNameAsync(string name)
            {
            var filter = Builders<Building>.Filter.Eq(b => b.Name, name);
            return await _buildingsCollection.Find(filter).FirstOrDefaultAsync();
            }

        /// <summary>
        /// Get building templates by type for shop filtering
        /// </summary>
        public async Task<List<Building>> GetBuildingsByTypeAsync(string type)
            {
            return await _buildingsCollection
                .Find(b => b.BuildingType == type)
                .ToListAsync();
            }

        /// <summary>
        /// Update building template
        /// </summary>
        public async Task<bool> UpdateBuildingAsync(Building building)
            {
            var result = await _buildingsCollection.ReplaceOneAsync(
                b => b.Id == building.Id,
                building
            );
            return result.ModifiedCount > 0;
            }

        /// <summary>
        /// Delete building template
        /// </summary>
        public async Task<bool> DeleteBuildingAsync(string id)
            {
            var result = await _buildingsCollection.DeleteOneAsync(b => b.Id == id);
            return result.DeletedCount > 0;
            }

        /// <summary>
        /// Delete ALL building templates (Reset button)
        /// </summary>
        public async Task<long> DeleteAllBuildingsAsync()
            {
            var result = await _buildingsCollection.DeleteManyAsync(_ => true);
            return result.DeletedCount;
            }

        #endregion

        #region Database Initialization

        /// <summary>
        /// Initialize the database with a new kingdom for the user
        /// </summary>
        public async Task InitializeDatabaseAsync(User currentUser)
            {
            // Check if user already has kingdoms
            if (currentUser.SavedKingdoms.Any())
                {
                return;
                }

            // Create initial kingdom
            var kingdom = new Kingdom
                {
                Id = ObjectId.GenerateNewId().ToString(),
                KingdomName = "Starship Alice",
                UserId = currentUser.UserId,
                Gold = 5,
                GoldPerSecond = 0.5f,
                Population = 1,
                MaxPopulation = 5,
                Happiness = 50,
                HappinessDecrease = 0.01f,
                HappinessIncrease = 0,
                OwnedBuildings = new List<OwnedBuilding>(),
                LastSaved = DateTime.UtcNow
                };

            // Give starting building
            var startingFarm = new OwnedBuilding
                {
                BuildingName = "Farm",
                Count = 1,
                Level = 1
                };

            // Get Farm template to calculate totals
            var farmTemplate = ShopData.GetShopBuildings().FirstOrDefault(b => b.Name == "Farm");
            if (farmTemplate != null)
                {
                startingFarm.RecalculateTotals(farmTemplate);
                kingdom.OwnedBuildings.Add(startingFarm);
                }

            await CreateKingdomAsync(currentUser.UserId, kingdom);
            }

        /// <summary>
        /// Initialize building templates in the shop
        /// </summary>
        public async Task InitializeBuildingsAsync()
            {
            // Check if buildings already exist
            var existingBuildings = await GetAllBuildingsAsync();
            if (existingBuildings.Count > 0)
                {
                return;
                }

            var shopData = ShopData.GetShopBuildings();
            await _buildingsCollection.InsertManyAsync(shopData);
            }

        /// <summary>
        /// Test MongoDB connection
        /// </summary>
        public async Task<string> TestConnectionAsync()
            {
            try
                {
                await _database.RunCommandAsync((Command<BsonDocument>)"{ping:1}");
                return "Connection to MongoDB successful!";
                }
            catch (Exception ex)
                {
                return $"Connection to MongoDB failed: {ex.Message}";
                }
            }

        #endregion

        #region User Authentication

        /// <summary>
        /// Authenticate user and return user object
        /// </summary>
        public async Task<User?> GetUserAsync(string username, string password)
            {
            var hashedPassword = HashPassword(password);
            return await _userCollection
                .Find(user => user.Username == username && user.Password == hashedPassword)
                .FirstOrDefaultAsync();
            }

        /// <summary>
        /// Create a new user account
        /// </summary>
        /// <summary>
        /// Create a new user account
        /// </summary>
        /// <returns>Tuple with success status and message</returns>
        public async Task<(bool success, string message)> CreateUserAsync(string username, string password)
            {
            // Check if username already exists
            var existingUser = await _userCollection
                .Find(u => u.Username == username)
                .FirstOrDefaultAsync();

            if (existingUser != null)
                {
                return (false, "Username already exists!");
                }

            User user = new User
                {
                Username = username,
                Password = HashPassword(password),
                UserId = Guid.NewGuid().ToString(),
                SavedKingdoms = new List<Kingdom>()
                };

            await _userCollection.InsertOneAsync(user);
            return (true, "User created successfully!");
            }

        /// <summary>
        /// Get user by userId
        /// </summary>
        public async Task<User?> GetUserByIdAsync(string userId)
            {
            return await _userCollection
                .Find(u => u.UserId == userId)
                .FirstOrDefaultAsync();
            }

        #endregion

        #region Password Hashing

        private string HashPassword(string password)
            {
            using (SHA256 sha256Hash = SHA256.Create())
                {
                return GetHash(sha256Hash, password);
                }
            }

        private static string GetHash(HashAlgorithm hashAlgorithm, string input)
            {
            byte[] data = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(input));
            var sBuilder = new StringBuilder();

            for (int i = 0; i < data.Length; i++)
                {
                sBuilder.Append(data[i].ToString("x2"));
                }

            return sBuilder.ToString();
            }

        #endregion
        }
    }