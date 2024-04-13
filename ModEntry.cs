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
using System.Text.Json;
using Newtonsoft.Json.Linq;
using StardewValley.Menus;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Net;
using StardewValley.GameData.Shops;

namespace SeedMod
{
    enum MixType
    {
        Crops,
        Flowers
    }

    /// <summary>The mod entry point.</summary>
    internal sealed class ModEntry : Mod
    {

        // Used to hold the mod folder directory        
        public static string path= "";     
        
        public override void Entry(IModHelper helper)
        { 

            // Gets the path using SMAPI helper                        
            path = this.Helper.DirectoryPath;
                                               
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

        // Hold the mod setting information
        public class ModConfig
        {
            public bool RandomizeWinter { get; set; }
            public bool TrellisEnabled { get; set; }
            public bool FlowersInFlowerMix { get; set; }
            public bool RandomizeGingerIsland { get; set; }
        }     

        // Hold information settings for each modset of seeds        
        public class SeedSet
        {
            public string name {get; set;}
            public bool enabled {get; set;}
            public double dropChance {get; set;}               
        }

        // Holds seed information
        public class Seed
        {
            public string cropName { get; set; } = null!;
            public string seedID { get; set; } = null!;
            public double dropChance { get; set; }
            public bool enabled { get; set; }
            public bool isTrellis {get; set;}
            public bool isFlower {get; set;}
        }


        public class SeedPatch
        {

            // Gets the config for the game from config.json
            static string ConfigPath = Path.Combine(path, "config.json");                     
            static JObject configJson = JObject.Parse(File.ReadAllText(ConfigPath));
            static JToken jConfig = configJson["ModSettings"];
            static ModConfig config = jConfig.ToObject<ModConfig>(); 


            // Gets the game location for use in postfix
            public static void Prefix(out GameLocation __state)
            {                
                __state = new GameLocation();
            }


            // Method to get the seed sets in config
            public static string GetSeedSet()
            {
                //Create a seed set list to get all possible seed sets               
                List<SeedSet> seedSets = new List<SeedSet>();                               

                // serialize JSON results into .NET objects (seed sets)
                IList<JToken> results = configJson["SeedSets"].Children().ToList();

                foreach (JToken result in results)
                {
                    // JToken.ToObject is a helper method that uses JsonSerializer internally
                    SeedSet seedSet = result.ToObject<SeedSet>();
                    if (seedSet.enabled)
                    {
                        seedSets.Add(seedSet);
                    }                    
                } 

                // Call get weighted set to determine which set will be pulled from              
                string seedSetName = GetWeightedSet(seedSets);
                return seedSetName;   
            }

            // Returns a seed set name to pull a list from
            public static string GetWeightedSet(List<SeedSet> list)
            {
                Random rand = new Random();
                double totalWeight = 0;
                string setName = ""; 

                // Gets a total from drop chance to compare drop chances against
                foreach (SeedSet item in list){
                    totalWeight += item.dropChance;              
                }

                // Selects a seed set randomly with weighted probability from drop chance
                double r = rand.NextDouble() * totalWeight;
                double sum = 0;
                foreach(SeedSet set in list)
                {
                    if(r <= (sum = sum + set.dropChance)){
                        setName = set.name;                        
                        return setName;
                    }
                }

                return setName;                   
            }

            // Returns a string Seed ID after selecting a seed using the drop chance ratios
            public static string GetWeightedSeed(List<Seed> seedList)
            {
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
                return seedID;               
            }


            // Returns the list of seeds for the game season
            public static List<Seed> GetSeedList(Season season, MixType mix)
            {             
                // Get season name as a string to navigate json file
                string seasonName = season.ToString();

                //Create a seed list to store seeds                
                List<Seed> seeds = new List<Seed>();

                //Counter variable for tries
                int attempts = 5;

                // Limit the number of tries before returning blank list, in case
                // user restrictions make seed finding difficult
                while (seeds.Count == 0 && attempts > 0)
                {
                    // Get the seedset this seed will pull from
                    string seedSet = GetSeedSet() + ".json";

                    //Get the path to the json file being used                
                    string seedPath = Path.Combine(path, "SeedSets", seedSet);       
                    
                    //Read the json file information, store as json
                    JObject json = JObject.Parse(File.ReadAllText(seedPath));                    

                    // serialize JSON results into .NET objects, gets seeds for that seaon
                    IList<JToken> results = json[seasonName].Children().ToList();
                    
                    foreach (JToken result in results)
                    {
                        // JToken.ToObject is a helper method that uses JsonSerializer internally
                        Seed seed = result.ToObject<Seed>();
                        if (seed.enabled)
                        {
                            //Excludes trellis crops if trellis is disabled
                            if (!seed.isTrellis || config.TrellisEnabled)
                            { 
                                // For regular mixed seeds                                                               
                                if (mix == MixType.Crops && (!(seed.isFlower) || !(config.FlowersInFlowerMix)))
                                {
                                    seeds.Add(seed);
                                }

                                // GetSeedList only called when FlowersinFlowerMix enabled (for mixed flower seeds)
                                if (mix == MixType.Flowers && seed.isFlower)
                                {
                                    seeds.Add(seed);
                                }
                            }                        
                        }                    
                    } 
                    //Decrement attempts to be made
                    attempts--;           
                }                
                return seeds;  
            }


            // The vanilla mixed flower seeds method, copied from the game
            public static string getVanillaFlowerMix(Season season)
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

            // This method can edit the result of the game method ResolveSeedId
            public static void Postfix(string itemId, GameLocation __state, ref string __result)
            {  
                // Gets the ingame season                             
                GameLocation currentLocation = __state;
                Season season = currentLocation.GetSeason();               

                // Randomize seed season for winter or ginger island if config settings apply
                if ((__state is IslandLocation && config.RandomizeGingerIsland) || (season == Season.Winter && config.RandomizeWinter))
                {
                    season = Game1.random.Choose(Season.Spring, Season.Summer, Season.Fall);                        
                }  

                // Gets seeds for Mixed Flower Seeds
                if (itemId == "MixedFlowerSeeds")
                {
                    if(config.FlowersInFlowerMix)
                    {                        
                        // Get a flower seedlist for the current location's season
                        List<Seed> seedList = GetSeedList(season, MixType.Flowers); 

                        // if seedlist isn't empty (prevent null return errors)
                        if (seedList.Count > 0)
                        {
                            // Get a weighted seed selection and set result
                            string seedID = GetWeightedSeed(seedList);
                            __result = seedID;
                        }                 
                    }
                    // Use vanilla forumla if FlowersinFlowerMixed disabled
                    else
                    {
                        __result = getVanillaFlowerMix(season);
                    }
                }

                // Gets seeds for mixed seeds
                else if (itemId == "770")
                {                                     

                    // Get a crops seedlist for the current location's season
                    List<Seed> seedList = GetSeedList(season, MixType.Crops); 

                    // if seedlist isn't empty
                    if (seedList.Count > 0)
                    {
                        // Select a weighted seed for the seedID
                        string seedID = GetWeightedSeed(seedList);
                                                
                        // Vanilla ginger island if ginger island not randomized
                        if (__state is IslandLocation && !config.RandomizeGingerIsland)
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
                        //TODO: Log error message?
                        //Will not change result if functions failed, default to vanilla
                    } 
                }

                // Anything not a mixed seed just returns its ID
                else 
                {
                    __result = itemId;
                }
                         
            }
        }      
        
    }
}

