# AurasHelper
This mod for the [HBS BattleTech](http://battletechgame.com/) game provides some minor changes to aura behaviors. These changes allow mods to use auras more flexibly than the HBS logic would normally allow. The following changes are supported:

* Effects are only removed with all effect sources have left range.
* Multiple effects that modify a statistic will be tracked and available for query.

## Background

To understand why these are necessary, you have to understand a little bit about how HBS effects and auras work. Every __effect__ has an ID, a target, a type, and a duration.

* The __target__ objects defines whether the effect is passive or active, and what actor is impacted by the effect. It defines how far away the effect applies for auras, and whether the effect causes graphical issues.
* The __type__ value defines the mechanical outcome of the effect. A common outcome is modifying one or more _statistics_ on the targeted actors. Statistics are integer, string, or bitmasks that are stored on many in-game objects (like actors, pilots, etc).
* The __duration__ objects define how long the effect lasts. Effects can last indefinitely, be reduced on activation, movement, or end of the round, or be removed when attacked. An effects duration includes a __stackLimit__ which defines how many of the same effect will be applied to the same actor.

Effects can be attached to components (passive effects), events in game (attacks, action sequences), be applied from design masks, and many other sources. Any actor can be targeted by multiples of the same effect from different sources.

For instance, `data/heatsinks/Gear_HeatSink_Generic_Thermal-Exchanger-II.json` applies the effect with ID `StatusEffect-Heat_GenReduction-T2`. If a 'mech has multiple Thermal Exchanger components, each component applies the effect to the parent 'mech. Because the effect description has `"stackLimit" : -1,` every component applies a unique instance of the same effectId. If you have three _Thermal Exchanger IIs_ on a 'mech, that actor will have three effects applied. Each effect will have the same ID and apply it's outcome independently.

### Stack Limit Oddities
While `"stackLimit" : -1` works as you might expect, applying the effect every time, other stackLimits behave in a non-intuitive fashion. If you have a `"stackLimit" : 3` set for an effect, when a 4th copy of the effect is applied to the actor the actor retains only 3 effect instances. Instead effect instance 4 replacing effect instance 1, the first effect is refreshed. Instead of going from [1,2,3] to [2,3,4] you keep [1,2,3] but 1 is updated with 4's values.

The likely reason for this behavior is that if they instead removed 1 and applied 4, all of the graphical and gameplay triggers would fire. This is unnecessary; if you're already playing an overheated VFX (because [1,2,3] applied it) then dropping 1 to add 4 means stopping the VFX and restarting it.

This behavior leads to an important consequence. The HBS code assumes that for a given EffectID, the applied modifier is __always the same__. However, there's nothing enforcing this constraint. If you had two new components, each can apply an effect with `effect.description.id = StatusEffect-Heat_GenReduction-T2` but have completely unique values in their `statisticData` block.

If your mod has different values for the same effectID, __the order of application matters__. Using the above example with effect instances [1,2,3,4], the value of each instance is [a,b,c,d] and the effect sets (instead of modifies a value). If the order of application is 1,2,3,4 and the stackLimit is 3, then the final value will be a. The values will be applied as a -> b -> c, then a will be refreshed _because it's the oldest_. This sets the final value to a.

:warning: Because predicting the interactions are so difficult, you should probably avoid this approach and keep every definition of an effect.description.id applying the same values.

## Design Notes

If an Aura effect uses AuraEffectType other than NotSet, has to be the HBS style or the static doesn't appear to be applied

AuraCache appears to be owned by an abstract actor
AuraCache::GetBestECMState appears to do a huge chunk of the calculation
`actors[i].Combat.MessageCenter.PublishMessage(new StealthChangedMessage(actors[i].GUID, actors[i].StealthPipsCurrent));`

These warnings don't do anything; effect will still be applied.
```
2019-06-09T08:44:18 FYLS [DEBUG] InitEffects for LV_ECM_SHIELD has an unsupported effectTargetType: AlliesWithinRange
2019-06-09T08:44:18 FYLS [DEBUG] InitEffects for LV_ECM_JAM has an unsupported effectTargetType: EnemiesWithinRange
```

Ugh
This sucks.
I think you're only going to be able to have 1 stack period
If you set stacks = 5, it add the effect for every space you move in the aura

If stacks = 1, and aura will be removed when you leave an overlapping bubble

targetingData has 'alsoEffectsCreator'...

```
BT.AuraCache.UpdateAuras invoked from:
    AC.UpdateAllAuras
        TurnDirector.CheckGameBegin
	TurnDirector.EndCurrentRound
    AC.UpdateAurasToActor
	AbstractActor.CancelCreatedEffects - called on death, shutdown
	AbstractActor.OnActivationBegin -
	AbstractActor.OnActivationEnd -
	AbstractActor.OnPositionUpdate - on each 'move'
	AbstractActor.RestartCreatedEffects - called on restart
	AbstractActor.CancelCreatedEffects - Called on DamageComponent
	AbstractActor.RestartPassiveEffects - if functional, checks all component status effects and updates aura. But never called b/c always passes performAuraRefresh = false
```

Iterates over auraAbility.Def.EffectData
```
    - if auraAbility.Def.EffectData shouldAffectThisActor
	- If !skipEcmCheck && auraAbility.Def.EffectData.targetingData.auraAffectType == ECM_GHOST or ECM_GENERAL
	    - Owner.Combat.FlagECMStateNeedsRefreshing
	- if auraConditionsPassed
	   - this.addEffectIfNotPresent
	- else this.RemoveEffectIfPresent
```

AuraConditionsPassed - true if:
```
    - owner.IsOperational
    - fromActor.IsOperational
    - is within square of distance
    - if type is ECM, !Owner.IsSensorLocked
```
- OnPositionUpdate: resets distances in AuraCache (UpdateAllAura -> AuraCache.ResetForFullRebuild) then updates all auras
