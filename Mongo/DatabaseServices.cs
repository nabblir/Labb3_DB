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
        private readonly IMongoCollection<Kingdom> _kingdomsCollection;
        private const string ConnectionString = "mongodb://localhost:27017";
        private const string DatabaseName = "KevinSpehling";

        public DatabaseService()
            {
            var client = new MongoClient(ConnectionString);
            _database = client.GetDatabase(DatabaseName);

            _buildingsCollection = _database.GetCollection<Building>("buildings");
            _userCollection = _database.GetCollection<User>("users");
            _kingdomsCollection = _database.GetCollection<Kingdom>("kingdoms");
            }

        #region Kingdom CRUD Operations

        /// <summary>
        /// Creates a new kingdom and adds its ID to the user's kingdom list
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

            // Insert kingdom into kingdoms collection
            await _kingdomsCollection.InsertOneAsync(kingdom);

            // Add kingdom ID to users kingdom list
            var filter = Builders<User>.Filter.Eq(u => u.UserId, userId);
            var update = Builders<User>.Update.Push(u => u.KingdomIds, kingdom.Id);
            await _userCollection.UpdateOneAsync(filter, update);

            return kingdom;
        }

        /// <summary>
        /// Gets all kingdoms for a specific user by loading from kingdoms collection
        /// </summary>
        public async Task<List<Kingdom>> GetUserKingdomsAsync(string userId)
        {
            var user = await _userCollection
                .Find(u => u.UserId == userId)
                .FirstOrDefaultAsync();

            if (user == null || user.KingdomIds.Count == 0)
                return new List<Kingdom>();

            // Fetch all kingdoms from the kingdoms collection
            var kingdoms = await _kingdomsCollection
                .Find(k => user.KingdomIds.Contains(k.Id))
                .ToListAsync();

            return kingdoms;
        }

        /// <summary>
        /// Gets a specific kingdom by ID
        /// </summary>
        public async Task<Kingdom?> GetKingdomByIdAsync(string kingdomId)
        {
            return await _kingdomsCollection
                .Find(k => k.Id == kingdomId)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Updates a specific kingdom
        /// </summary>
        public async Task<bool> UpdateKingdomAsync(string userId, Kingdom kingdom)
        {
            kingdom.LastSaved = DateTime.UtcNow;

            var result = await _kingdomsCollection.ReplaceOneAsync(
                k => k.Id == kingdom.Id,
                kingdom
            );
            return result.ModifiedCount > 0;
        }

        /// <summary>
        /// Deletes a specific kingdom and removes it from user's kingdom list
        /// </summary>
        public async Task<bool> DeleteKingdomAsync(string userId, string kingdomId)
        {
            var deleteResult = await _kingdomsCollection.DeleteOneAsync(k => k.Id == kingdomId);

            // Remove kingdom ID from users list
            var filter = Builders<User>.Filter.Eq(u => u.UserId, userId);
            var update = Builders<User>.Update.Pull(u => u.KingdomIds, kingdomId);
            await _userCollection.UpdateOneAsync(filter, update);

            return deleteResult.DeletedCount > 0;
        }

        /// <summary>
        /// Deletes all kingdoms for a user
        /// </summary>
        public async Task<bool> DeleteAllUserKingdomsAsync(string userId)
        {
            var user = await _userCollection
                .Find(u => u.UserId == userId)
                .FirstOrDefaultAsync();

            if (user == null || user.KingdomIds.Count == 0)
                return true;

            // Delete all kingdoms from kingdoms collection
            var deleteResult = await _kingdomsCollection
                .DeleteManyAsync(k => user.KingdomIds.Contains(k.Id));

            // Clear users kingdom list
            var filter = Builders<User>.Filter.Eq(u => u.UserId, userId);
            var update = Builders<User>.Update.Set(u => u.KingdomIds, new List<string>());
            await _userCollection.UpdateOneAsync(filter, update);

            return deleteResult.DeletedCount > 0;
        }

        #endregion

        #region Building CRUD Operations

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
            if (currentUser.KingdomIds.Any())
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
        /// Authenticate user and return user object with loaded kingdoms
        /// </summary>
        public async Task<User?> GetUserAsync(string username, string password)
        {
            var hashedPassword = HashPassword(password);
            var user = await _userCollection
                .Find(u => u.Username == username && u.Password == hashedPassword)
                .FirstOrDefaultAsync();

            if (user != null)
            {
                // Load kingdoms from separate collection
                user.SavedKingdoms = await GetUserKingdomsAsync(user.UserId);
            }

            return user;
        }

        /// <summary>
        /// Create a new user account
        /// </summary>
        /// <returns>Success status and message</returns>
        public async Task<(bool success, string message)> CreateUserAsync(string username, string password)
        {
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
                KingdomIds = new List<string>(),
                SavedKingdoms = new List<Kingdom>()
            };

            await _userCollection.InsertOneAsync(user);
            return (true, "User created successfully!");
        }

        /// <summary>
        /// Get user by userId and load their kingdoms
        /// </summary>
        public async Task<User?> GetUserByIdAsync(string userId)
        {
            var user = await _userCollection
                .Find(u => u.UserId == userId)
                .FirstOrDefaultAsync();

            if (user != null)
            {
                // Load kingdoms from separate collection
                user.SavedKingdoms = await GetUserKingdomsAsync(user.UserId);
            }

            return user;
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