﻿using Harmony;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TechAdvancing
{
    /// <summary>
    /// Class storing all the detours and the detour call.
    /// </summary>
    class HarmonyDetours
    {
        /// <summary>
        /// Method for performing all the detours via Harmony.
        /// </summary>
        public static void Setup()
        {
            var harmony = HarmonyInstance.Create("com.ghxx.rimworld.techadvancing");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    /// <summary>
    /// Prefix for adding the button below the progressbar of the research window. The button is used for opening the config screen.
    /// </summary>
    [HarmonyPatch(typeof(RimWorld.MainTabWindow_Research))]
    [HarmonyPatch("DrawLeftRect")]
    [HarmonyPatch(new Type[] { typeof(Rect) })]
    class TA_Research_Menu_Patch
    {
        [SuppressMessage("Codequality", "IDE0051:Remove unused private member", Justification = "Referenced at runtime by harmony")]
        static void Prefix(Rect leftOutRect)
        {
            // code for adding the techadvancing config button to the (vanilla) research screen
            Rect TA_Cfgrect = new Rect(0f, 0f, 180f, 20f);
            TA_Cfgrect.x = (leftOutRect.width - TA_Cfgrect.width) / 2f;
            TA_Cfgrect.y = leftOutRect.height - 20f;

            if (Widgets.ButtonText(TA_Cfgrect, "TAcfgmenulabel".Translate(), true, false, true))
            {
                SoundDef.Named("ResearchStart").PlayOneShotOnCamera();
                Find.WindowStack.Add((Window)new TechAdvancing_Config_Tab());
            }
        }
    }

    /// <summary>
    /// Replace research cost calc method to be able to remove cost cap, like in A18
    /// </summary>
    [HarmonyPatch(typeof(Verse.ResearchProjectDef))]
    [HarmonyPatch("CostFactor")]
    [HarmonyPatch(typeof(TechLevel))]
    class TA_ReplaceResearchProjectDef
    {
        [SuppressMessage("Codequality", "IDE0051:Remove unused private member", Justification = "Referenced at runtime by harmony")]
        static void Postfix(Verse.ResearchProjectDef __instance, ref float __result, TechLevel researcherTechLevel)
        {
            if (researcherTechLevel >= __instance.techLevel)
            {
                __result = 1f;
            }
            else
            {
                int num = __instance.techLevel - researcherTechLevel;
                __result = 1f + num * 0.5f;

                if (TechAdvancing_Config_Tab.configCheckboxDisableCostMultiplicatorCap == 0)
                {
                    __result = Mathf.Min(__result, 2);
                }

                if (TechAdvancing_Config_Tab.configCheckboxMakeHigherResearchesSuperExpensive == 1)
                {
                    __result *= (float)(TechAdvancing_Config_Tab.configCheckboxMakeHigherResearchesSuperExpensiveFac * Math.Pow(2, num));
                }
            }

            __result *= TechAdvancing_Config_Tab.ConfigChangeResearchCostFacAsFloat();
        }
    }

    /// <summary>
    /// Patch for having a method called when a pawn dies.
    /// </summary>
    [HarmonyPatch(typeof(Verse.Pawn))]
    [HarmonyPatch("Kill")]
    [HarmonyPatch(new Type[] { typeof(DamageInfo?), typeof(Hediff) })]
    class TA_OnKill_Event
    {
        [SuppressMessage("Codequality", "IDE0051:Remove unused private member", Justification = "Referenced at runtime by harmony")]
        static void Postfix(Pawn __instance, DamageInfo? dinfo, Hediff exactCulprit = null)
        {
            TechAdvancing.Event.OnKill(__instance);
        }
    }

    /// <summary>
    /// Patch for getting notified about faction changes. E.g.: when a pawn joins the colony.
    /// </summary>
    [HarmonyPatch(typeof(Verse.Pawn))]
    [HarmonyPatch("SetFaction")]
    [HarmonyPatch(new Type[] { typeof(Faction), typeof(Pawn) })]
    class TA_OnNewPawn_Event
    {
        [SuppressMessage("Codequality", "IDE0051:Remove unused private member", Justification = "Referenced at runtime by harmony")]
        static void Prefix(Pawn __instance, Faction newFaction, Pawn recruiter = null)
        {
            TechAdvancing.Event.OnNewPawn(__instance);
        }
    }

    /// <summary>
    /// Postfix Patch for getting to know the new faction.
    /// </summary>
    [HarmonyPatch(typeof(Verse.Pawn))]
    [HarmonyPatch("SetFaction")]
    [HarmonyPatch(new Type[] { typeof(Faction), typeof(Pawn) })]
    class TA_PostOnNewPawn_Event
    {
        [SuppressMessage("Codequality", "IDE0051:Remove unused private member", Justification = "Referenced at runtime by harmony")]
        static void Postfix(Faction newFaction, Pawn recruiter = null)
        {
            TechAdvancing.Event.PostOnNewPawn();
        }
    }

    /// <summary>
    /// Postfix Patch for the research manager to do the techlevel calculation
    /// </summary>
    [HarmonyPatch(typeof(RimWorld.ResearchManager))]
    [HarmonyPatch("ReapplyAllMods")]
    static class TA_ResearchManager
    {
        public static TechLevel factionDefault = TechLevel.Undefined;
        public static bool isTribe = true;
        private static bool firstNotificationHidden = false;

        public static DateTime startedAt = DateTime.Now;
        public static string facName = "";
        public static bool firstpass = true;

        [SuppressMessage("Codequality", "IDE0051:Remove unused private member", Justification = "Referenced at runtime by harmony")]
        static void Postfix()
        {
            if (Faction.OfPlayerSilentFail?.def?.techLevel == null || Faction.OfPlayer.def.techLevel == TechLevel.Undefined) // abort if our techlevel is undefined for some reason
                return;


            if (firstpass || facName != Faction.OfPlayer.def.defName)
            {
                startedAt = DateTime.Now;
                facName = Faction.OfPlayer.def.defName;
                try
                {
                    GetAndReloadTL();        //store the default value for the techlevel because we will modify it later and we need the one from right now

                    isTribe = factionDefault == TechLevel.Neolithic;
                    LoadCfgValues();
                    firstpass = false;

                    //Debug
                    LogOutput.WriteLogMessage(Errorlevel.Debug, "Con A val= " + TechAdvancing_Config_Tab.conditionvalue_A + "|||Con B Val= " + TechAdvancing_Config_Tab.conditionvalue_B);

                }
                catch (Exception ex)
                {
                    LogOutput.WriteLogMessage(Errorlevel.Error, "Caught error in Reapply All Mods: " + ex.ToString());
                }

            }

            var researchProjectStoreTotal = new Dictionary<TechLevel, int>();
            var researchProjectStoreFinished = new Dictionary<TechLevel, int>();

            for (int i = 0; i < Enum.GetValues(typeof(TechLevel)).Length; i++)
            {
                researchProjectStoreTotal.Add((TechLevel)i, 0);
                researchProjectStoreFinished.Add((TechLevel)i, 0);
            }

            foreach (var researchProjectDef in DefDatabase<ResearchProjectDef>.AllDefs)
            {
                //skip the research if it contains the disabled-tag:
                #region tagDesc                    
                /*
                    <ResearchProjectDef>
                        <defName>Firefoam</defName>
                        <label>firefoam</label>
                        <description>Allows the construction of firefoam poppers; fire-safety buildings which spread fire-retardant foam in response to encroaching flames.</description>
                        <baseCost>800</baseCost>
                        <techLevel>Industrial</techLevel>
                        <prerequisites>
                            <li>MicroelectronicsBasics</li>
                        </prerequisites>
                   !    <tags>
        Important  !        <li>ta-ignore</li>
                   !    </tags>
                        <requiredResearchBuilding>HiTechResearchBench</requiredResearchBuilding>
                        <researchViewX>7</researchViewX>
                        <researchViewY>4</researchViewY>
                    </ResearchProjectDef>

                */
                #endregion

                if (researchProjectDef.tags?.Any(x => x.defName == "ta-ignore") != true)
                {
                    researchProjectStoreTotal[researchProjectDef.techLevel]++;  //total projects for techlevel  
                    if (researchProjectDef.IsFinished)
                    {
                        researchProjectStoreFinished[researchProjectDef.techLevel]++;  //finished projects for techlevel
                        researchProjectDef.ReapplyAllMods();
                    }
                }
                else
                {
                    LogOutput.WriteLogMessage(Errorlevel.Debug, "Found ta-ignore tag in:" + researchProjectDef.defName);
                }

                if (researchProjectDef.IsFinished)
                    researchProjectDef.ReapplyAllMods();
            }

            TechAdvancing.Rules.researchProjectStoreTotal = researchProjectStoreTotal;
            TechAdvancing.Rules.researchProjectStoreFinished = researchProjectStoreFinished;

            TechLevel newLevel = TechAdvancing.Rules.GetNewTechLevel();

            if (newLevel != TechLevel.Undefined)
            {
                if (firstNotificationHidden && DateTime.Now.Subtract(TimeSpan.FromSeconds(5)) > startedAt) //hiding the notification on world start
                {
                    if (Faction.OfPlayer.def.techLevel < newLevel)
                        Find.LetterStack.ReceiveLetter("newTechLevelLetterTitle".Translate(), "newTechLevelLetterContents".Translate(isTribe ? "configTribe".Translate() : "configColony".Translate()) + " " + newLevel.ToString() + ".", LetterDefOf.PositiveEvent);
                }
                else
                {
                    firstNotificationHidden = true;
                }

                Faction.OfPlayer.def.techLevel = newLevel;
            }

            /***
            how techlevel increases:
            player researched all techs of techlevel X and below. the techlevel rises to X+1

            player researched more than 50% of the techlevel Y then the techlevel rises to Y
            **/
            RecalculateTechlevel(false);
        }

        private static void LoadCfgValues() //could be improved using just vanilla loading  // TODO obsolete?
        {
            TechAdvancing_Config_Tab.ExposeData(TA_Expose_Mode.Load);

            if (TechAdvancing_Config_Tab.baseTechlvlCfg != 1)
            {
                TechAdvancing_Config_Tab.baseFactionTechLevel = (TechAdvancing_Config_Tab.baseTechlvlCfg == 0) ? TechLevel.Neolithic : TechLevel.Industrial;
            }
        }

        internal static TechLevel GetAndReloadTL()
        {
            if (Faction.OfPlayer.def.techLevel > TechLevel.Undefined && TA_ResearchManager.factionDefault == TechLevel.Undefined)
            {
                TA_ResearchManager.factionDefault = Faction.OfPlayer.def.techLevel;
                TechAdvancing_Config_Tab.baseFactionTechLevel = Faction.OfPlayer.def.techLevel;
            }
            if (Faction.OfPlayer.def.techLevel == TechLevel.Undefined)
            {
                LogOutput.WriteLogMessage(Errorlevel.Warning, "Called without valid TL");
            }
            return Faction.OfPlayer.def.techLevel;
        }

        internal static void RecalculateTechlevel(bool showIncreaseMsg = true)
        {
            if (Faction.OfPlayerSilentFail?.def?.techLevel == null || Faction.OfPlayer.def.techLevel == TechLevel.Undefined)   // if some mod does something funky again....
                return;

            GetAndReloadTL();
            TechLevel baseNewTL = Rules.GetNewTechLevel();
            if (TechAdvancing_Config_Tab.configCheckboxNeedTechColonists == 1 && !Util.ColonyHasHiTechPeople())
            {
                Faction.OfPlayer.def.techLevel = (TechLevel)Util.Clamp((int)TechLevel.Undefined, (int)baseNewTL, (int)TechAdvancing_Config_Tab.maxTechLevelForTribals);
            }
            else
            {
                Faction.OfPlayer.def.techLevel = baseNewTL;
            }

            if (showIncreaseMsg) //used to supress the first update message| Treat as always false
            {
                Messages.Message("ConfigEditTechlevelChange".Translate() + " " + (TechLevel)Faction.OfPlayer.def.techLevel + ".", MessageTypeDefOf.PositiveEvent);
            }
        }
    }
}
