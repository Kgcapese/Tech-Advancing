﻿using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TechAdvancing
{
    class Util
    {
        internal static int Clamp(int min, int val, int max) //helper method
        {
            if (val < min)
            {
                return min;
            }
            else if (max < val)
            {
                return max;
            }
            else
            {
                return val;
            }
        }

        internal static TechLevel Clamp(TechLevel min, TechLevel val, TechLevel max) //helper method
        {
            if (val < min)
            {
                return min;
            }
            else if (max < val)
            {
                return max;
            }
            else
            {
                return val;
            }
        }


        internal static bool ColonyHasHiTechPeople()
        {
            FactionDef[] hitechfactions = new FactionDef[] { FactionDefOf.Mechanoid, FactionDefOf.Outlander, FactionDefOf.Spacer, FactionDefOf.PlayerColony };
            string[] hightechkinds = new string[] { "colonist" };

            //Debug
            //   foreach (var pawn in RimWorld.PawnsFinder.AllMaps_FreeColonists)
            //   {
            //       string techlvl = null;
            //       if (MapComponent_TA_Expose.TA_Expose_People?.ContainsKey(pawn)==true)
            //       {
            //           techlvl = ((int?)(MapComponent_TA_Expose.TA_Expose_People[pawn])?.def?.techLevel ?? -1).ToString();
            //       }
            //       LogOutput.writeLogMessage(Errorlevel.Warning, "Pawn: " + pawn?.Name + " |Faction: " + pawn?.Faction?.Name + " |DefName: " + pawn?.kindDef?.defaultFactionType?.defName  + "|Tech lvl: "+ techlvl + " |High Tech (whitelist): " + (hitechfactions.Contains(pawn?.Faction?.def) ? "yes" : "no"));    
            //}
            //   LogOutput.writeLogMessage(Errorlevel.Warning,"done");

            return RimWorld.PawnsFinder.AllMaps_FreeColonists.Any(x => hightechkinds.Contains(x.kindDef.defName.ToLowerInvariant()) || ((int?)((MapCompSaveHandler.ColonyPeople?.ContainsKey(x) == true) ? MapCompSaveHandler.ColonyPeople[x] : null)?.def?.techLevel ?? -1) >= (int)TechLevel.Industrial);
        }

        internal static TechLevel GetHighestTechlevel(params TechLevel[] t)
        {
            var max = t.Select(x=>(int)x).Max();
            return (max > (int)TechLevel.Transcendent) ? TechLevel.Transcendent : (TechLevel)max;
        }
    }
}