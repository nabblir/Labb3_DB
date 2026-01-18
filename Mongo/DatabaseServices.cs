using Labb3_DB.Data;
using Labb3_DB.Models;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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

        private const string ConnectionString = "mongodb://localhost:27017";
        private const string DatabaseName = "KevinSpehling";

        public DatabaseService()
            {
            var client = new MongoClient(ConnectionString);
            _database = client.GetDatabase(DatabaseName);

            _kingdomCollection = _database.GetCollection<Kingdom>("kingdoms");
            _buildingsCollection = _database.GetCollection<Building>("buildings");
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
        }
    }