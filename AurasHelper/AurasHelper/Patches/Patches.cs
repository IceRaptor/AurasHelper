using BattleTech;
using BattleTech.UI;
using Harmony;
using System;
using System.Collections.Generic;
using UnityEngine;

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

    [HarmonyPatch(typeof(AuraCache), "AddEffectIfNotPresent")]
    public static class AuraCache_AddEffectIfNotPresent{

        public static void Postfix(AuraCache __instance, ref bool __result, AbstractActor fromActor, AbstractActor movingActor, Vector3 movingActorPos, 
            string effectCreatorId, EffectData effect, ref List<string> existingEffectIDs, EffectTriggerType triggerSource) {
            Mod.Log.Trace("AC:AEINP entered");

        }
    }

    [HarmonyPatch(typeof(AuraCache), "RemoveEffectIfPresent")]
    public static class AuraCache_RemoveEffectIfPresent {

        public static void Postfix(AuraCache __instance, ref bool __result, AbstractActor fromActor, string effectCreatorId, 
            EffectData effect, List<Effect> existingEffects, EffectTriggerType triggerSource) {
            Mod.Log.Trace("AC:REIP entered");

        }
    }

}
