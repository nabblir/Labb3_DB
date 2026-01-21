using Labb3_DB.Data;
using Labb3_DB.Models;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Policy;
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
        private readonly IMongoCollection<Kingdom> _kingdomCollection;
        private readonly IMongoCollection<Building> _buildingsCollection;
        private readonly IMongoCollection<User> _userCollection;
        private const string ConnectionString = "mongodb://localhost:27017";
        private const string DatabaseName = "KevinSpehling";
        public DatabaseService()
            {
            var client = new MongoClient(ConnectionString);
            _database = client.GetDatabase(DatabaseName);

            _kingdomCollection = _database.GetCollection<Kingdom>("kingdoms");
            _buildingsCollection = _database.GetCollection<Building>("buildings");
            _userCollection = _database.GetCollection<User>("users");

            }

        #region Kingdom CRUD Operations

        /// <summary>
        /// Creates a new kingdom
        /// </summary>
        public async Task<Kingdom> CreateKingdomAsync(Kingdom kingdom)
            {
            await _kingdomCollection.InsertOneAsync(kingdom);
            return kingdom;
            }

        /// <summary>
        /// Fetches the kingdom (there should only be one)
        /// </summary>
        public async Task<Kingdom?> GetKingdomAsync()
            {
            return await _kingdomCollection.Find(_ => true).FirstOrDefaultAsync();
            }

        /// <summary>
        /// Updates the kingdom
        /// </summary>
        public async Task<bool> UpdateKingdomAsync(Kingdom kingdom)
            {
            kingdom.LastSaved = DateTime.UtcNow;
            var result = await _kingdomCollection.ReplaceOneAsync(
                k => k.Id == kingdom.Id,
                kingdom
            );
            return result.ModifiedCount > 0;
            }

        /// <summary>
        /// Delete kingdom (Reset button)
        /// </summary>
        public async Task<bool> DeleteKingdomAsync(string id)
            {
            var result = await _kingdomCollection.DeleteOneAsync(k => k.Id == id);
            return result.DeletedCount > 0;
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
        /// Initialize the database with a new kingdom and starting buildings
        /// </summary>
        public async Task InitializeDatabaseAsync()
            {
            var existingKingdom = await GetKingdomAsync();

            if (existingKingdom != null)
                {
                return;
                }

            // Create initial kingdom
            var kingdom = new Kingdom
                {
                KingdomName = "Starship Alice",
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

            await CreateKingdomAsync(kingdom);
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
        #endregion

        #region User Authentication
        public async Task<User> GetUserAsync(string username, string password)
            {
            // This is a placeholder for user authentication logic.
            // Implement user retrieval and authentication as needed.

            return await _userCollection.Find(user => user.Username == username && user.Password == HashPassword(password)).FirstOrDefaultAsync();
            }

        public async Task CreateUserAsync(string username, string password)
            {
            User user = new User
                {
                Username = username,
                Password = HashPassword(password),
                UserID = Guid.NewGuid().ToString() // Unique ID for the user - used to link kingdoms to users
                };

            await _userCollection.InsertOneAsync(user);
            MessageBox.Show("User created successfully!");
            }
        #endregion

        #region User credentials hashing
        private string HashPassword(string password)
            {
            using (SHA256 sha256Hash = SHA256.Create())
                {
                return GetHash(sha256Hash, password);
                }
            }

        private static string GetHash(HashAlgorithm hashAlgorithm, string input)
            {

            // Convert the input string to a byte array and compute the hash.
            byte[] data = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            var sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
                {
                sBuilder.Append(data[i].ToString("x2"));
                }

            // Return the hexadecimal string.
            return sBuilder.ToString();
            }

        private static bool VerifyHash(HashAlgorithm hashAlgorithm, string input, string hash)
            {
            // Hash the input.
            var hashOfInput = GetHash(hashAlgorithm, input);

            // Create a StringComparer an compare the hashes.
            StringComparer comparer = StringComparer.OrdinalIgnoreCase;

            return comparer.Compare(hashOfInput, hash) == 0;
            }
        #endregion
        }
    }