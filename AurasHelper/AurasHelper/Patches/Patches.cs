using BattleTech;
using Harmony;
using System;
using System.Collections.Generic;
using UnityEngine;
using us.frostraptor.modUtils;

namespace AurasHelper {

    [HarmonyPatch(typeof(AuraCache), "UpdateAura")]
    [HarmonyPatch(new Type[] { typeof(AbstractActor), typeof(AbstractActor), typeof(Vector3), typeof(Ability), typeof(float), typeof(EffectTriggerType), typeof(bool) })]
    public static class AuraCache_UpdateAura_Ability {

        public static void Postfix(AuraCache __instance, AbstractActor fromActor, AbstractActor movingActor, Vector3 movingActorPos,
            Ability auraAbility, float distSquared, EffectTriggerType triggerSource, bool skipECMCheck) {
            Mod.Log.Trace("AC:UA:A entered");

        }
    }

    [HarmonyPatch(typeof(AuraCache), "UpdateAura")]
    [HarmonyPatch(new Type[] { typeof(AbstractActor), typeof(AbstractActor), typeof(Vector3), typeof(MechComponent), typeof(float), typeof(EffectTriggerType), typeof(bool)})]
    public static class AuraCache_UpdateAura_MechComponent {

        public static void Postfix(AuraCache __instance, AbstractActor fromActor, AbstractActor movingActor, Vector3 movingActorPos,
            MechComponent auraComponent, float distSquared, EffectTriggerType triggerSource, bool skipECMCheck) {
            Mod.Log.Trace("AC:UA:MC entered");

        }
    }

    [HarmonyPatch(typeof(AuraCache), "AuraConditionsPassed")]
    [HarmonyPatch(new Type[] { typeof(AbstractActor), typeof(Ability), typeof(EffectData), typeof(float), typeof(EffectTriggerType) })]
    public static class AuraCache_AuraConditionsPassed_Ability {
        public static void Postfix(AuraCache __instance, bool __result, AbstractActor fromActor, float distSquared) {
            Mod.Log.Trace($"-- AuraCache:AuraConditionsPassed:Ability result: {__result} for actor: {CombatantUtils.Label(fromActor)} at range: {distSquared}");
        }
    }

    [HarmonyPatch(typeof(AuraCache), "AuraConditionsPassed")]
    [HarmonyPatch(new Type[] { typeof(AbstractActor), typeof(MechComponent), typeof(EffectData), typeof(float), typeof(EffectTriggerType) })]
    public static class AuraCache_AuraConditionsPassed_MechComponent {
        public static void Postfix(AuraCache __instance, bool __result, AbstractActor fromActor, float distSquared) {
            Mod.Log.Trace($"-- AuraCache:AuraConditionsPassed:MechComponent result: {__result} for actor: {CombatantUtils.Label(fromActor)} at range: {distSquared}");
        }
    }

    [HarmonyPatch(typeof(AuraCache), "AddEffectIfNotPresent")]
    public static class AuraCache_AddEffectIfNotPresent{

        public static void Prefix(AuraCache __instance, ref bool __result, AbstractActor fromActor, AbstractActor movingActor, Vector3 movingActorPos, 
            string effectCreatorId, EffectData effect, ref List<string> existingEffectIDs, EffectTriggerType triggerSource) {
            Mod.Log.Trace("AC:AEINP:pre entered");

            Traverse ownerT = Traverse.Create(__instance).Property("Owner");
            AbstractActor Owner = ownerT.GetValue<AbstractActor>();

            // 1. When the same effectId is added, record every actor that contributes the same effect id. Only remove the effect if all actors have removed the effect
            string sourcesStat = $"{effect.Description.Id}_AH_SOURCES";
            string sourceValue = CombatantUtils.Label(fromActor);
            Mod.Log.Debug($"  sourceValue: ({sourceValue})");
            if (!Owner.StatCollection.ContainsStatistic(sourcesStat)) {
                Owner.StatCollection.AddStatistic<string>(sourcesStat, "");
                Owner.StatCollection.Set<string>(sourcesStat, sourceValue);
                //Mod.Log.Debug($"  new sources statistic: ({sourcesStat}) value: ({sourceValue})");
            } else {
                string statSources = Owner.StatCollection.GetStatistic(sourcesStat).Value<string>();

                HashSet<string> sources = new HashSet<string>();
                foreach (string value in statSources.Split(',')) {
                    if (value != null && !value.Equals(sourceValue)) {
                        sources.Add(value);
                    }
                }
                sources.Add(sourceValue);

                string newValue = string.Join(",", new List<string>(sources).ToArray());
                Owner.StatCollection.Set<string>(sourcesStat, newValue);
                //Mod.Log.Debug($"  sources statistic: ({sourcesStat}) value: ({newValue})");

            }

            // 2. When multiple effects add to the same statistic, record each value as an array
            if (effect.effectType == EffectType.StatisticEffect) {
                Mod.Log.Debug($"Tracking statEffect vs statistic: {effect.statisticData.statName} fromActor: {CombatantUtils.Label(fromActor)} effectCreatorId: {effectCreatorId} " +
                    $"vs. movingActor: {CombatantUtils.Label(movingActor)}");

                // Create a tracking stat, denoting effectId:fromActorGUID:strength
                string valuesStat = $"{effect.Description.Id}_AH_VALUES";
                string effectValue = $"{effect.Description.Id}:{CombatantUtils.Label(fromActor)}:{effectCreatorId}:{effect.statisticData.modValue}";
                Mod.Log.Debug($"  effectValue: ({effectValue})");
                if (!Owner.StatCollection.ContainsStatistic(valuesStat)) {
                    Owner.StatCollection.AddStatistic<string>(valuesStat, "");
                    Owner.StatCollection.Set<string>(valuesStat, effectValue);
                    //Mod.Log.Debug($"  new values statistic: ({valuesStat}) value: ({effectValue})");
                } else {
                    string statValues = Owner.StatCollection.GetStatistic(valuesStat).Value<string>();

                    HashSet<string> values = new HashSet<string>();
                    foreach (string value in statValues.Split(',')) {
                        if (value != null && !value.Equals(effectValue)) {
                            values.Add(value);
                        }
                    }
                    values.Add(effectValue);

                    string newValue = string.Join(",", new List<string>(values).ToArray());
                    Owner.StatCollection.Set<string>(valuesStat, newValue);
                    //Mod.Log.Debug($"  values statistic: ({valuesStat}) value: ({newValue})");
                }

            }

        }
    }

    [HarmonyPatch(typeof(AuraCache), "RemoveEffectIfPresent")]
    public static class AuraCache_RemoveEffectIfPresent {

        public static bool Prefix(AuraCache __instance, AbstractActor fromActor, string effectCreatorId, 
            EffectData effect, List<Effect> existingEffects, EffectTriggerType triggerSource) {

            Mod.Log.Trace("AC:REIP entered");

            bool allowMethod = true;

            Traverse ownerT = Traverse.Create(__instance).Property("Owner");
            AbstractActor Owner = ownerT.GetValue<AbstractActor>();

            // 1. When the same effectId is added, record every actor that contributes the same effect id. Only remove the effect if all actors have removed the effect
            string sourcesStat = $"{effect.Description.Id}_AH_SOURCES";
            string sourceValue = CombatantUtils.Label(fromActor);
            if (Owner.StatCollection.ContainsStatistic(sourcesStat)) {
                Mod.Log.Debug($"  Removing effectSource: {sourceValue} from Owner: ({CombatantUtils.Label(Owner)}");
                string statSources = Owner.StatCollection.GetStatistic(sourcesStat).Value<string>();
                Mod.Log.Debug($"  -- statSources: ({statSources})");

                HashSet<string> newValues = new HashSet<string>();
                foreach (string value in statSources.Split(',')) {
                    if (value != null && value != "") { newValues.Add(value); }
                }
                if (newValues.Contains(sourceValue)) { newValues.Remove(sourceValue); }

                if (newValues.Count > 0) {
                    string newValue = string.Join(",", new List<string>(newValues).ToArray());
                    Mod.Log.Debug($"  changing effectSources on actor: ({CombatantUtils.Label(Owner)}) from: ({statSources}) to: ({newValue})");
                    Owner.StatCollection.Set(sourcesStat, newValue);
                    allowMethod = false;
                } else {
                    Mod.Log.Debug($"  No effectSources remaining on actor: ({CombatantUtils.Label(Owner)}), removing statistic: {sourcesStat}");
                    Owner.StatCollection.RemoveStatistic(sourcesStat);
                }
            }

            // 2. When multiple effects add to the same statistic, record each value as an array
            if (effect != null && effect.effectType == EffectType.StatisticEffect) {
                string valuesStat = $"{effect.Description.Id}_AH_VALUES";
                string effectValue = $"{effect.Description.Id}:{CombatantUtils.Label(fromActor)}:{effectCreatorId}:{effect.statisticData.modValue}";
                if (Owner.StatCollection.ContainsStatistic(valuesStat)) {
                    Mod.Log.Debug($"  Removing effectValue: {effectValue} from Owner: ({CombatantUtils.Label(Owner)}");
                    string statValues = Owner.StatCollection.GetStatistic(valuesStat).Value<string>();
                    Mod.Log.Debug($"  -- statValues: ({statValues})");

                    HashSet<string> newValues = new HashSet<string>();
                    foreach (string value in statValues.Split(',')) {
                        if (value != null || value != "") { newValues.Add(value); }
                    }
                    if (newValues.Contains(effectValue)) { newValues.Remove(effectValue); }

                    if (newValues.Count > 0) {
                        string newValue = string.Join(",", new List<string>(newValues).ToArray());
                        Mod.Log.Debug($"  changing statValues from: ({statValues}) to: ({newValue})");
                        Owner.StatCollection.Set(valuesStat, newValue);
                        allowMethod = false;
                    } else {
                        Mod.Log.Debug($"  No effects remaining on actor: ({CombatantUtils.Label(Owner)}), removing all values");
                        Owner.StatCollection.RemoveStatistic(valuesStat);
                    }
                }

            }

            return allowMethod;
        }
    }

    [HarmonyPatch(typeof(AbstractActor), "OnMoveComplete")]
    public static class AbstractActor_OnMoveComplete {

        public static void Prefix(AbstractActor __instance) {

            Mod.Log.Debug($" OnMoveComplete for Actor: {CombatantUtils.Label(__instance)}");
            foreach (KeyValuePair<string, Statistic> kvp in __instance.StatCollection) {
                if (kvp.Key.EndsWith("_AH_SOURCES") || kvp.Key.EndsWith("_AH_VALUES")) {
                    Mod.Log.Debug($" -- stat: ({kvp.Key}) has value: ({kvp.Value.Value<string>()})");
                }
            }

            foreach (AbstractActor unit in __instance.team.units) {
                if (unit.GUID != __instance.GUID) {
                    Mod.Log.Debug($" -- friendly actor: {CombatantUtils.Label(unit)}");
                    foreach (KeyValuePair<string, Statistic> kvp in unit.StatCollection) {
                        if (kvp.Key.EndsWith("_AH_SOURCES") || kvp.Key.EndsWith("_AH_VALUES")) {
                            Mod.Log.Debug($" -- stat: ({kvp.Key}) has value: ({kvp.Value.Value<string>()})");
                        }
                    }
                }
            }
        }
    }

}
