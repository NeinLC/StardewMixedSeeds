using System;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using System.Reflection;
using xTile.Dimensions;
using HarmonyLib;
using xTile.Format;
using Microsoft.VisualBasic;
using StardewValley.Locations;
using StardewValley.Extensions;

namespace SeedMod
{
    /// <summary>The mod entry point.</summary>
    internal sealed class ModEntry : Mod
    {        
        
        public override void Entry(IModHelper helper)
        {   
                                   
            var harmony = new Harmony(this.ModManifest.UniqueID);           

            // Gets the season for use in postfix
            harmony.Patch(
                original: AccessTools.Method(typeof(StardewValley.Crop), nameof(StardewValley.Crop.ResolveSeedId)),
                prefix: new HarmonyMethod(typeof(SeedPatch), nameof(SeedPatch.Prefix))
            );

            // Will apply changes after in game ResolveSeedID is called
            harmony.Patch(
                original: AccessTools.Method(typeof(StardewValley.Crop), nameof(StardewValley.Crop.ResolveSeedId)),
                postfix: new HarmonyMethod(typeof(SeedPatch), nameof(SeedPatch.Postfix))
            );

        }

        public class Seed
        {
            public string cropName { get; set; } = null!;
            public string seedID { get; set; } = null!;
            public double dropChance { get; set; }
        }


        public class SeedPatch
        {
            
            
            public static void Prefix(out GameLocation __state)
            {                
                __state = new GameLocation();
            }



            public static List<Seed> GetSeedList(Season season)
            {

                // Lists of seeds and assigned drop chance, numbers are ratios to each other
                // TO DO: Figure out how to call lists from a config file that can be edited easily

                List<Seed> springSeeds = new List<Seed>
                {                    
                    new Seed {cropName = "Parsnip", seedID = "472", dropChance = 8},
                    new Seed {cropName = "Green Bean", seedID = "473", dropChance = 4},
                    new Seed {cropName = "Cauliflower", seedID = "474", dropChance = 4},
                    new Seed {cropName = "Potato", seedID = "475", dropChance = 6},
                    new Seed {cropName = "Garlic", seedID = "476", dropChance = 1},
                    new Seed {cropName = "Rice", seedID = "273", dropChance = 4},
                    new Seed {cropName = "Kale", seedID = "477", dropChance = 5},
                    new Seed {cropName = "Rhubarb", seedID = "478", dropChance = 2},
                    new Seed {cropName = "Strawberry", seedID = "745", dropChance = 2},
                    new Seed {cropName = "Coffee", seedID = "433", dropChance = 0.2}
                };

                List<Seed> summerSeeds = new List<Seed>
                {
                    //new Seed {cropName = "Taro Root", seedID = "433", dropChance = 0.2},                    
                    new Seed {cropName = "Melon", seedID = "479", dropChance = 6},
                    new Seed {cropName = "Tomato", seedID = "480", dropChance = 8},
                    new Seed {cropName = "Blueberry", seedID = "481", dropChance = 6},
                    new Seed {cropName = "Hot Pepper", seedID = "482", dropChance = 8},
                    new Seed {cropName = "Wheat", seedID = "483", dropChance = 12},
                    new Seed {cropName = "Radish", seedID = "484", dropChance = 4},
                    new Seed {cropName = "Red Cabbage", seedID = "485", dropChance = 1},
                    new Seed {cropName = "Starfruit", seedID = "486", dropChance = 1},
                    //new Seed {cropName = "Pineapple", seedID = "745", dropChance = 2},
                    new Seed {cropName = "Coffee", seedID = "433", dropChance = 0.2},
                    new Seed {cropName = "Cactus Fruit", seedID = "433", dropChance = 2},
                    new Seed {cropName = "Corn", seedID = "487", dropChance = 6},                    
                    new Seed {cropName = "Hops", seedID = "302", dropChance = 4},
                    //new Seed {cropName = "Ancient Fruit", seedID = "499", dropChance = 0.2},                    
                };

                List<Seed> fallSeeds = new List<Seed>
                {                    
                    new Seed {cropName = "Wheat", seedID = "483", dropChance = 12},
                    new Seed {cropName = "Corn", seedID = "487", dropChance = 8},
                    new Seed {cropName = "Grape", seedID = "301", dropChance = 6},
                    new Seed {cropName = "Amaranth", seedID = "299", dropChance = 6},
                    new Seed {cropName = "Eggplant", seedID = "488", dropChance = 6},
                    new Seed {cropName = "Artichoke", seedID = "489", dropChance = 4},
                    new Seed {cropName = "Pumpkin", seedID = "490", dropChance = 4},
                    new Seed {cropName = "Bok Choy", seedID = "491", dropChance = 8},                    
                    new Seed {cropName = "Yam", seedID = "492", dropChance = 6},                    
                    new Seed {cropName = "Cranberries", seedID = "493", dropChance = 6},
                    new Seed {cropName = "Beet", seedID = "494", dropChance = 4},
                    //new Seed {cropName = "Ancient Fruit", seedID = "499", dropChance = 0.2},
                    //new Seed {cropName = "SweetGemBerry", seedID = "302", dropChance = 4},  
                                   
                };

                List<Seed> winterSeeds = new List<Seed>
                {                    
                    new Seed {cropName = "Powdermelons", seedID = "Powdermelon Seeds", dropChance = 1},                                      
                };

                // Return list based on the season, winter as default                
                switch (season)
                {
                    case Season.Spring: return springSeeds;
                    case Season.Summer: return summerSeeds;
                    case Season.Fall: return fallSeeds;
                    default: return winterSeeds;
                }                             

            }

            // This is just directly from the game
            public static string getRandomFlowerSeedForThisSeason(Season season)
            {
                if (season == Season.Winter)
                {
                    season = Game1.random.Choose(Season.Spring, Season.Summer, Season.Fall);
                }
                return season switch
                {
                    Season.Spring => Game1.random.Choose("427", "429"), 
                    Season.Summer => Game1.random.Choose("455", "453", "431"), 
                    Season.Fall => Game1.random.Choose("431", "425"), 
                    _ => "-1", 
                };
            }


            public static void Postfix(string itemId, GameLocation __state, ref string __result)
            {
                
                             
                GameLocation currentLocation = __state;
                

                if (itemId == "MixedFlowerSeeds")
                {
                    __result = getRandomFlowerSeedForThisSeason(currentLocation.GetSeason());

                }

                else if (itemId == "770")
                {
                    List<Seed> seedList = GetSeedList(currentLocation.GetSeason()); 

                    Random rand = new Random();
                    double totalWeight = 0;
                    string seedID = ""; 

                    // Gets a total from drop chance to compare drop chances against
                    foreach (Seed seed in seedList){
                        totalWeight += seed.dropChance;              
                    }

                    // Selects a seed ID randomly with weighted probability from drop chance
                    double r = rand.NextDouble() * totalWeight;
                    double sum = 0;
                    foreach(Seed seed in seedList)
                    {
                        if(r <= (sum = sum + seed.dropChance)){
                            seedID = seed.seedID;
                            break;
                        }
                    }
                                             
                    // For the island location in game, not dependent on season
                    // This is copied from game code
                    if (__state is IslandLocation)
                    {
                        seedID = Game1.random.Next(4) switch
                        {
                            0 => "479", 
                            1 => "833", 
                            2 => "481", 
                            _ => "478", 
                        };
                    }
                    __result = seedID;                        
                }

                else 
                {
                    __result = itemId;
                }
                         
            }
        }      
        
    }
}

