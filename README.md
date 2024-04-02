This is the main code for the Find Your Own Seeds mod set on Nexus Mods for Stardew Valley.

ModEntry.cs : Contains the working code to modify mixed seeds into dropping any crop that 
  season not found through other means such as monster drops and artifact spots. 
  Works through SMAPI and uses Harmony to patch

SeedMod.csproj : Project file used in Visual Studio Code to compile everything 

content.json : A JSON file to be used by Content Patcher. This file nullifies all seed sales
  from shops aside from the random travelling cart. Additionally, towards the end, Marnie's
  Ranch is given a few relevant seeds to sell based on friendship values and season.

manifest.json : Called by Content Patcher for the content.json file and SMAPI for the mixed
  seed component, SeedMod.dll


Other notes:
- Commented out crops in ModEntry.cs will hopefully optionally be re-eneabled once a method
    for users to configure their experience has been developed.
- Marnie's Shop having a friendship mechanic to introduce seed sales is the first of other
      getting methods I hope to implement in game.



