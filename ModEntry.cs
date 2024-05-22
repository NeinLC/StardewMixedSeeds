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
using StardewValley.Buildings;
using System.ComponentModel;
using StardewValley.GameData;
using System.Data;
using Newtonsoft.Json;

namespace SeedMod
{    
    public enum MixType
    {
        Seasonal,
        All
    }

    public interface IGenericModConfigMenuApi
    {
        void Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false);
        void AddSectionTitle(IManifest mod, Func<string> text, Func<string> tooltip = null);
        void AddBoolOption(IManifest mod, Func<bool> getValue, Action<bool> setValue, Func<string> name, Func<string> tooltip = null, string fieldId = null);
        void AddNumberOption(IManifest mod, Func<int> getValue, Action<int> setValue, Func<string> name, Func<string> tooltip = null, int? min = null, int? max = null, int? interval = null, Func<int, string> formatValue = null, string fieldId = null);
    }

    public class MixedSeedModApi
    {       

        public List<Seed> GetCropSeeds()
        {
            if (!SeedLists.AllCropList.Any())
            {
                SeedPatch.CreateSeedLists(MixType.All);
            }
            return SeedLists.AllCropList;
        }

        public List<Seed> GetFlowerSeeds()
        {
            if (!SeedLists.AllCropList.Any())
            {
                SeedPatch.CreateSeedLists(MixType.All);
            }
            return SeedLists.AllFlowerList;
        }

        public List<SeedSet> GetSeedSets()
        {
            return SeedPatch.GetSeedSets();
        }

        List<Seed> cropSeeds = SeedLists.AllCropList;
        List<Seed> flowerSeeds = SeedLists.AllFlowerList; 
        List<SeedSet> seedSets = SeedPatch.GetSeedSets();     
    }

    

    /// <summary>The mod entry point.</summary>
    internal sealed class ModEntry : Mod
    {

        private ModConfig Config;

        // Used to hold the directory which will be used by these methods        
        public static string path= ""; 

         
           
        
        public override void Entry(IModHelper helper)
        {

            // Gets the path using SMAPI helper                        
            path = this.Helper.DirectoryPath;  

            // Reading config with smapi
            // this.Config = this.Helper.ReadConfig<ModConfig>();            
            
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;               
                                               
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

               

        
        //TODO: May eventually split the SeedSets and ModSettings into different files
        //TODO: check if other mods install successfully before giving toggle?
        //TODO: also check auto-enable installed expensions
        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            
            // get Generic Mod Config Menu's API (if it's installed)
            var configMenu = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;


            // Getting ModConfig from json
            string ConfigPath = Path.Combine(path, "config.json");                     
            JObject configJson = JObject.Parse(File.ReadAllText(ConfigPath));
            JToken jConfig = configJson["ModSettings"];
            JToken jSeedSettings = configJson["SeedSets"];
            Config = jConfig.ToObject<ModConfig>(); 

            // Create Object list to hold SeedSet settings               
            List<SeedSet> seedSets = new List<SeedSet>();      
            IList<JToken> results = jSeedSettings.Children().ToList();
            foreach (JToken result in results)
            {
                SeedSet seedSet = result.ToObject<SeedSet>();
                seedSets.Add(seedSet);

                // If the mod the seedSet is loaded from isn't loaded, disable it
                if (!(this.Helper.ModRegistry.IsLoaded(seedSet.UniqueID) || seedSet.name == "Vanilla"))
                {
                    seedSet.enabled = false;
                }                            
            }

            // Save changes for seedsets disabled by not being loaded
            string seedSetJson = JsonConvert.SerializeObject(seedSets, Formatting.Indented);         
            string modSetStr = jConfig.ToString();
            
            string configStr = "{\n\t\"ModSettings\" :\n" 
                + modSetStr + ",\n" + "\n\"SeedSets\" :"
                + seedSetJson + "\n}";
            File.WriteAllText(ConfigPath, configStr);



            // register mod
            configMenu.Register(               

                mod: this.ModManifest,
                // TODO: Edit possibly                
                reset: () => {
                    this.Config = new ModConfig();
                    foreach (SeedSet seedSet in seedSets)
                    {
                        seedSet.dropChance = 50;                                    
                    }
                },
                save: () => {
                    // Commit all changes to the jConfig
                    jConfig["RandomizeWinter"] = Config.RandomizeWinter;
                    jConfig["TrellisEnabled"] = Config.TrellisEnabled;
                    jConfig["FlowersInFlowerMix"] = Config.FlowersInFlowerMix;
                    jConfig["RandomizeGingerIsland"] = Config.RandomizeGingerIsland;
                    jConfig["EnableYearRequirements"] = Config.EnableYearRequirements;

                    string seedSetJson = JsonConvert.SerializeObject(seedSets, Formatting.Indented);              

                    string modSetStr = jConfig.ToString();
                    
                    string configStr = "{\n\t\"ModSettings\" :\n" 
                        + modSetStr + ",\n" + "\n\"SeedSets\" :"
                        + seedSetJson + "\n}";
                    File.WriteAllText(ConfigPath, configStr);                            
                }              
            );

            configMenu.AddSectionTitle( mod: this.ModManifest, text: () => "General Configuration:");   

            // add some config options
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Randomize Winter",
                tooltip: () => "In winter, randomize seeds between Spring, Summer, and Fall sets (as in Vanilla)",
                getValue: () => Config.RandomizeWinter,
                setValue: value => Config.RandomizeWinter = value
            );
            
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Enable Trellis Crops",
                tooltip: () => "Allow trellis crops to drop from mixed seeds",
                getValue: () => Config.TrellisEnabled,
                setValue: value => Config.TrellisEnabled = value
            );

            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Flowers in Flower Mix",
                tooltip: () => "Flower crops will drop from Flower Mixed Seeds instead of regular mixed seeds",
                getValue: () => Config.FlowersInFlowerMix,
                setValue: value => Config.FlowersInFlowerMix = value
            );

            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Randomize Ginger Island",
                tooltip: () => "Mixed seeds on Ginger Island drop any seed from Spring, Summer, or Fall",
                getValue: () => Config.RandomizeGingerIsland,
                setValue: value => Config.RandomizeGingerIsland = value
            );

            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Enable Year Requirements",
                tooltip: () => "Crops with year requirments in shops won't drop until year requirement is filled",
                getValue: () => Config.FlowersInFlowerMix,
                setValue: value => Config.FlowersInFlowerMix = value
            );


            configMenu.AddSectionTitle( mod: this.ModManifest, text: () => "Seedpack Options:");  
            foreach (SeedSet seedSet in seedSets)
            {
                // Only show options for Seeds from loaded mods
                if (this.Helper.ModRegistry.IsLoaded(seedSet.UniqueID) || seedSet.name == "Vanilla")
                {
                    configMenu.AddBoolOption(
                        mod: this.ModManifest,
                        name: () => $"Enable {seedSet.name}",                    
                        getValue: () => seedSet.enabled,
                        setValue: value => seedSet.enabled = value
                    );                   

                    configMenu.AddNumberOption(
                        mod: this.ModManifest, 
                        name: () => "Drop Rate: ",
                        getValue: () => (int)seedSet.dropChance,
                        min: 1,
                        max: 50,
                        setValue: value => seedSet.dropChance = value            
                    );                      
                }
                
            }
        }

        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            // Initialize a total seed list once for the game
            SeedLists.AllCropList.Clear(); SeedLists.AllFlowerList.Clear();
            SeedPatch.CreateSeedLists(MixType.All);
            SeedPatch.CreateSeedLists(MixType.Seasonal, Season.Spring);
        }        

        public override object GetApi()
        {
            return new MixedSeedModApi();
        }    
        
    }


        

        public class ModConfig
        {
            public bool RandomizeWinter { get; set; } = true;
            public bool TrellisEnabled { get; set; } = true;
            public bool FlowersInFlowerMix { get; set; } = true;
            public bool RandomizeGingerIsland { get; set; } = true;
            public bool EnableYearRequirements { get; set; } = true;            
            public ModConfig (bool rWinter = true, bool trellis = true, bool flowers = true, bool rGinger = true, bool yearReq = true)
            {
                RandomizeWinter = rWinter;
                TrellisEnabled = trellis;
                FlowersInFlowerMix = flowers;
                RandomizeGingerIsland = rGinger;
                EnableYearRequirements = yearReq;
            }
        }     

        
        public class SeedSet
        {
            public string name { get; set; }
            public string UniqueID { get; set; }
            public bool enabled { get; set; }
            public double dropChance { get; set; }               
        }

        public class Seed
        {
            public string cropName { get; set; } = null!;
            public string seedID { get; set; } = null!;
            public double dropChance { get; set; }
            public bool enabled { get; set; }
            public bool isTrellis { get; set; } = false;
            public bool isFlower { get; set; } = false;  
            public int minYear { get; set; } = 1;
        }

        
        // Empty lists which store seeds
        public class SeedLists
        {
            // Stores what season the list contains seeds for, used to compare to curent ingame season
            static public Season ListSeason;

            // Seasonal seedlist, used for regular outdoor planting
            static public List<Seed> CropList = new List<Seed>();
            static public List<Seed> FlowerList = new List<Seed>();

            // All seasons seedlist, used in particular circumstances where season is irrelevant/randomized
            static public List<Seed> AllCropList = new List<Seed>();
            static public List<Seed> AllFlowerList = new List<Seed>();

        }





        public class SeedPatch
        {       

            // Gets the game location for use in postfix
            public static void Prefix(out GameLocation __state)
            {                
                __state = new GameLocation();
            }

            // Gets the config for the game from config.json
            static string ConfigPath = Path.Combine(ModEntry.path, "config.json");                     
            static JObject configJson = JObject.Parse(File.ReadAllText(ConfigPath));
            static JToken jConfig = configJson["ModSettings"];
            static ModConfig config = jConfig.ToObject<ModConfig>();

            // Gets the list of enabled seedsets
            public static List<SeedSet> GetSeedSets()
            {
                List<SeedSet> seedSetList = new List<SeedSet>();
                                               
                // serialize JSON results into .NET objects (seed sets)
                IList<JToken> results = configJson["SeedSets"].Children().ToList();        

                foreach (JToken result in results)
                {
                    // Get the SeedSet object out of the json
                    SeedSet seedSet = result.ToObject<SeedSet>();
                    if (seedSet.enabled)
                    {
                        seedSetList.Add(seedSet);
                    }                    
                }
                return seedSetList;

            }

            // Method used to create the seedlists, with behaviors for seasonal and comprehensive lists
            public static void CreateSeedLists(MixType type, Season season = Season.Spring)
            { 
                List<SeedSet> seedSets = GetSeedSets();

                foreach (SeedSet seedSet in seedSets)
                {
                    if (type == MixType.Seasonal)
                    {
                        AddSeedsToLists(seedSet, season, MixType.Seasonal);
                    }                        

                    else if (type == MixType.All)
                    {
                        AddSeedsToLists(seedSet, Season.Spring, MixType.All);
                        AddSeedsToLists(seedSet, Season.Summer, MixType.All);
                        AddSeedsToLists(seedSet, Season.Fall, MixType.All);
                        AddSeedsToLists(seedSet, Season.Winter, MixType.All);                                                       
                    }

                }  
            }

            // Adds seeds from seedsets to the lists
            public static void AddSeedsToLists(SeedSet seedSet, Season season, MixType type)
            {
                //Get the path to the json file being used                
                string seedPath = Path.Combine(ModEntry.path, "SeedSets", seedSet.name + ".json");       
                
                //Read the json file information, store as json
                JObject json = JObject.Parse(File.ReadAllText(seedPath));

                // Try catch block to prevent errors, such as no entries found
                try
                {
                    // serialize JSON results into .NET objects, gets seeds for that season
                    IList<JToken> results = json[season.ToString()].Children().ToList();
                    
                    foreach (JToken result in results)
                    {
                        // Turns the JToken into a seed object
                        Seed seed = result.ToObject<Seed>();
                        if (seed.enabled && (!config.EnableYearRequirements || seed.minYear <= Game1.year))
                        {                                                     
                            //Excludes trellis crops if trellis is disabled
                            if (!seed.isTrellis || config.TrellisEnabled)
                            {
                                // TODO: can modify later to only do this if we care about seedset dropchance 
                                seed.dropChance *= seedSet.dropChance;

                                // For regular mixed seeds                                                               
                                if (!seed.isFlower || !config.FlowersInFlowerMix)
                                {
                                    if (type == MixType.Seasonal) { SeedLists.CropList.Add(seed); }
                                    else { SeedLists.AllCropList.Add(seed); }
                                }

                                // GetSeedList only called when FlowersinFlowerMix enabled (for mixed flower seeds)
                                else if (seed.isFlower)
                                {
                                    if (type == MixType.Seasonal) { SeedLists.FlowerList.Add(seed); }
                                    else { SeedLists.AllFlowerList.Add(seed); }                                
                                }
                            }                                                
                        }                    
                    } 

                }
                catch {
                    //TODO: figure out how to log error with SMAPI
                }               
            }

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



            // Modifies the result from stardew's ResolveSeedID method
            public static void Postfix(string itemId, GameLocation __state, ref string __result)
            {  
                // Gets the ingame season                             
                GameLocation currentLocation = __state;
                Season season = currentLocation.GetSeason();

                if (season != SeedLists.ListSeason)
                {
                    SeedLists.CropList.Clear(); SeedLists.FlowerList.Clear();
                    CreateSeedLists(MixType.Seasonal, season);
                    SeedLists.ListSeason = season;
                }

                // Gets seeds for Mixed Flower Seeds
                if (itemId == "MixedFlowerSeeds")
                {
                    if(config.FlowersInFlowerMix)
                    {
                        // Pull from full lists for these
                        if ((currentLocation is IslandLocation && config.RandomizeGingerIsland) || (season == Season.Winter && config.RandomizeWinter))
                        {
                            if (SeedLists.AllFlowerList.Any())
                            {
                                __result = GetWeightedSeed(SeedLists.AllFlowerList);
                            }                 
                        }

                        else if (SeedLists.FlowerList.Any())
                        {
                            // Get a weighted seed selection and set result
                            __result = GetWeightedSeed(SeedLists.FlowerList);
                        }
                    }                   
                }

                // Gets seeds for mixed seeds
                else if (itemId == "770")
                {
                    // Pull from full lists for these
                    if ((currentLocation is IslandLocation && config.RandomizeGingerIsland) || (season == Season.Winter && config.RandomizeWinter))
                    {
                        if (SeedLists.AllCropList.Any())
                        {
                            __result = GetWeightedSeed(SeedLists.AllCropList);  
                        }       
                    }

                    else if (!(currentLocation is IslandLocation) && SeedLists.CropList.Any())
                    {
                        __result = GetWeightedSeed(SeedLists.CropList);
                    }              
                }          
                      
                // Implicit Else, the regular game method runs: return itemID                 
                         
            }
        }  
}
