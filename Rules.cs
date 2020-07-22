﻿using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace TechAdvancing
{
    /// <summary>
    /// Class storing all the Rules and helper methods.
    /// </summary>
    class Rules
    {
        public static Dictionary<TechLevel, int> researchProjectStoreTotal = new Dictionary<TechLevel, int>();
        public static Dictionary<TechLevel, int> researchProjectStoreFinished = new Dictionary<TechLevel, int>();
        public static List<ResearchProjectDef> nonIgnoredTechs = new List<ResearchProjectDef>();

        public static TechLevel baseFactionLevel = TechLevel.Undefined;

        /// <summary>
        /// Gets the final techlevel. This includes limits as a maximum and the base faction techlevel as a minimum.
        /// </summary>
        /// <returns></returns>
        internal static TechLevel GetNewTechLevel()
        {
            if (TechAdvancing_Config_Tab.b_configCheckboxNeedTechColonists)
            {
                return (TechLevel)(Math.Min((int)GetRuleTechlevel(), (int)GetLowTechTL()));
            }
            return GetRuleTechlevel();
        }

        /// <summary>
        /// Gets the max techlevel that was generated by any rule. Also takes the faction-min techlevel into account.
        /// </summary>
        /// <returns></returns>
        internal static TechLevel GetRuleTechlevel()
        {
            LogOutput.WriteLogMessage(Errorlevel.Debug, $"A: {RuleA()} | B:{RuleB()}");
            return Util.GetHighestTechlevel(TechAdvancing_Config_Tab.GetBaseTechlevel(), RuleA(), RuleB());
        }

        /// <summary>
        /// Returns the lowTech colony limit. Only if the limit is applied.
        /// </summary>
        /// <returns></returns>
        internal static TechLevel GetLowTechTL()
        {
            if (!TechAdvancing_Config_Tab.b_configCheckboxNeedTechColonists)    // if the limit is not enabled at all
            {
                return TechLevel.Archotech;
            }
            else
            {
                return (TechAdvancing.MapCompSaveHandler.ColonyPeople.Any(x => x.Value?.def?.techLevel >= TechLevel.Industrial)) ? TechLevel.Archotech : TechAdvancing_Config_Tab.maxTechLevelForTribals;
            }
        }

        internal static TechLevel RuleA()
        {
            var notResearched = researchProjectStoreTotal.Except(researchProjectStoreFinished);
            int min = notResearched.Where(x => x.Value > 0).Select(x => (int)x.Key).DefaultIfEmpty(0).Min();
            return (TechLevel)Util.Clamp(0, min - 1 + TechAdvancing_Config_Tab.conditionvalue_A, (int)TechLevel.Archotech);
        }

        internal static TechLevel RuleB()
        {
            int result = 0; //tl undef

            foreach (var tl in researchProjectStoreTotal.Where(x => x.Value > 0))
            {
                if ((float)researchProjectStoreFinished[tl.Key] / (float)tl.Value > (TechAdvancing_Config_Tab.conditionvalue_B_s / 100f))
                {
                    result = (int)tl.Key;
                }
            }
            return (TechLevel)Util.Clamp(0, result + (int)TechAdvancing_Config_Tab.conditionvalue_B, (int)TechLevel.Archotech);
        }
    }
}
