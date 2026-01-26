using Labb3_DB.Models;
using System.Collections.Generic;

namespace Labb3_DB.Data
    {
    public static class ShopData
        {
        public static List<Building> GetShopBuildings()
            {
            return new List<Building>
            {
                new Building
                {
                    Name = "Farm",
                    Description = "Grows crops\nProduces 1 gold per second",
                    BaseCost = 20,
                    BaseIncome = 1,
                    BuildingType = "Production",
                    PopulationCost = 1,
                    HappinessDecrease = 0.01f
                },
                new Building
                {
                    Name = "Woodcutter",
                    Description = "Chops wood to sell\nProduces 1.5 gold per second",
                    BaseCost = 30,
                    BaseIncome = 1.5,
                    BuildingType = "Production",
                    PopulationCost = 2,
                    HappinessDecrease = 0.05f
                },
                new Building
                {
                    Name = "Mine",
                    Description = "Extracts valuable minerals\nProduces 3 gold per second",
                    BaseCost = 50,
                    BaseIncome = 3,
                    BuildingType = "Production",
                    PopulationCost = 3,
                    HappinessDecrease = 0.15f
                },
                new Building
                {
                    Name = "House",
                    Description = "Provides housing for citizens\nIncreases population capacity by 5",
                    BaseCost = 25,
                    BaseIncome = 0,
                    BuildingType = "Housing",
                    MaxPopulation = 5
                },
                new Building
                {
                    Name = "Church",
                    Description = "Gain 5 gold per second while happiness is above 75%\nLose double the income gold per second while happiness is under 50%",
                    BaseCost = 50,
                    BaseIncome = 5,
                    BuildingType = "Entertainment",
                    PopulationCost = 5
                },
                new Building
                {
                    Name = "Tavern",
                    Description = "Keeps citizens happy\nProduces 0.25 happiness per second",
                    BaseCost = 75,
                    BaseIncome = 0,
                    HappinessIncrease = 0.25f,
                    BuildingType = "Entertainment",
                    PopulationCost = 2
                },
                new Building
                {
                    Name = "Castle",
                    Description = "The heart of your kingdom\nProduces 50 gold per second and increases population by 125",
                    BaseCost = 15000,
                    BaseIncome = 50,
                    BuildingType = "Housing",
                    MaxPopulation = 125
                },
                new Building
                {
                    Name = "Brothel",
                    Description = "Keeps citizens even more happy\nProduces 3 happiness per second and 10 gold per second",
                    BaseCost = 7500,
                    HappinessIncrease = 3,
                    BaseIncome = 10,
                    BuildingType = "Entertainment",
                    PopulationCost = 5
                }
            };
            }
        }
    }