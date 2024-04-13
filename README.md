This is the main code for the Re-Mixed Seeds mod on Nexus Mods for Stardew Valley.

ModEntry.cs : Contains the working code to modify mixed seeds into dropping more crops
  at weighted probabilities, reading the information needed from various json files.
  Works through SMAPI and uses Harmony to patch

SeedMod.csproj : Project file used in Visual Studio Code to compile everything 

manifest.json : Used by SMAPI to apply the mod

config.json: Mod settings which can be adjusted by the user. Contains a section for
  overall settings, and settings for each mod seedset that can be enabled

SeedSets folder: Contains json files for each set of seeds which can be edited to
  be read by the compiled dll file



