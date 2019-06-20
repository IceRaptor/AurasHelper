# AurasHelper
This mod for the [HBS BattleTech](http://battletechgame.com/) game provides some minor changes to aura behaviors. These changes allow mods to use auras more flexibly than the HBS logic would normally allow. The following changes are supported:

* Effects are only removed with all effect sources have left range. (Adds `<STATNAME>_SOURCES` statistic)
* Multiple effects that modify a statistic will be tracked and available for query. (Adds `<STATNAME>_VALUES` statistic)

## Background

To understand why these are necessary, you have to understand a little bit about how HBS effects and auras work. Every __effect__ has an ID, a target, a type, and a duration.

* The __target__ objects defines whether the effect is passive or active, and what actor is impacted by the effect. It defines how far away the effect applies for auras, and whether the effect causes graphical issues.
* The __type__ value defines the mechanical outcome of the effect. A common outcome is modifying one or more _statistics_ on the targeted actors. Statistics are integer, string, or bitmasks that are stored on many in-game objects (like actors, pilots, etc).
* The __duration__ objects define how long the effect lasts. Effects can last indefinitely, be reduced on activation, movement, or end of the round, or be removed when attacked. An effects duration includes a __stackLimit__ which defines how many of the same effect will be applied to the same actor.

Effects can be attached to components (passive effects), events in game (attacks, action sequences), be applied from design masks, and many other sources. Any actor can be targeted by multiples of the same effect from different sources.

For instance, `data/heatsinks/Gear_HeatSink_Generic_Thermal-Exchanger-II.json` applies the effect with ID `StatusEffect-Heat_GenReduction-T2`. If a 'mech has multiple Thermal Exchanger components, each component applies the effect to the parent 'mech. Because the effect description has `"stackLimit" : -1,` every component applies a unique instance of the same effectId. If you have three _Thermal Exchanger IIs_ on a 'mech, that actor will have three effects applied. Each effect will have the same ID and apply it's outcome independently.

## Stack Limit Oddities
While `"stackLimit" : -1` works as you might expect, applying the effect every time, other stackLimits behave in a non-intuitive fashion. If you have a `"stackLimit" : 3` set for an effect, when a 4th copy of the effect is applied to the actor the actor retains only 3 effect instances. Instead effect instance 4 replacing effect instance 1, the first effect is refreshed. Instead of going from [1,2,3] to [2,3,4] you keep [1,2,3] but 1 is updated with 4's values.

The likely reason for this behavior is that if they instead removed 1 and applied 4, all of the graphical and gameplay triggers would fire. This is unnecessary; if you're already playing an overheated VFX (because [1,2,3] applied it) then dropping 1 to add 4 means stopping the VFX and restarting it.

This behavior leads to an important consequence. The HBS code assumes that for a given EffectID, the applied modifier is __always the same__. However, there's nothing enforcing this constraint. If you had two new components, each can apply an effect with `effect.description.id = StatusEffect-Heat_GenReduction-T2` but have completely unique values in their `statisticData` block.

If your mod has different values for the same effectID, __the order of application matters__. Using the above example with effect instances [1,2,3,4], the value of each instance is [a,b,c,d] and the effect sets (instead of modifies a value). If the order of application is 1,2,3,4 and the stackLimit is 3, then the final value will be a. The values will be applied as a -> b -> c, then a will be refreshed _because it's the oldest_. This sets the final value to a.

:warning: Because predicting the interactions are so difficult, you should probably avoid this approach and keep every definition of an effect.description.id applying the same values.

## Auras

Auras are effects whose targetingData includes a `"specialRules" : "Aura"` block, and an `"auraEffectType" and "effectTargetType"` defining the actors to which an instance of the effect should be applied. Instead of auras being unique constructs, they are just effects that can be applied to one or more actors. A good example of auras is the [EWE in UrbanWarfare](https://github.com/caardappel-hbs/bt-dlc-designdata/blob/master/UrbanWarfare/data/upgrades/sensorTech/Gear_Sensor_Prototype_EWE.json).

By default, at the start of the following events the game engine searches for all actors with auraEffects and applies the effect to all valid targets:

* TurnDirector.CheckGameBegin
* TurnDirector.EndCurrentRound

This initialization is what causes the ECM bubble to 'pop in' after the zoom-in effect at mission start. The aura effect hasn't been applied until that point, so the VFX doesn't play.

In addition, when an actor goes through one of the following events the game engine applies their auraEffects to any valid target within range:

* AbstractActor.OnActivationBegin - after the actor is selected, but before resolving any action or movement selected.
* AbstractActor.OnActivationEnd - after all actions and movement have been completed.
* AbstractActor.OnPositionUpdate - for each 'hex' the actor moves.
* AbstractActor.CancelCreatedEffects - when the actor dies or is shutdown

Each actor has an `AuraCache` object attached to it, that receives new auraEffects or messages that auraEffects have been removed. It goes through effect processing logic to determine what should happen, and applies the effect on each actor individually.

### Aura Example
An example helps make this a bit more clear. Let's say we have a Raven, Griffin, and Crab. If the Griffin and Crab are within the aura effect range at the start of the game (TurnDirector.CheckGameBegin), then the Raven, Griffin, and Crab all receive the effect outcome before any action is taken.

The Griffin activates first and goes through `OnActivationBegin`. Because it emits no auraEffects, nothing happens. It then plots a movement path. Each time it moves a 'hex', `OnPositionUpdate` is invoked. The Griffin has no auraEffects, so it skips that part of the check. But when I leaves the range of the Raven's auraEffect, a message is sent to the Griffin's AuraCache indicating that the effect should be removed. When the Griffin finishes its movement and actions, `OnActivationEnd` is invoked and again nothing happens because there are no auraEffects emitted from the Griffin.

The Raven activates next and goes through `OnActivationBegin`. Because it emits an auraEffect (`ECMStealth_GhostEffect_Allies`), it checks for all allies within range (100m). If an ally is within range, the `ECMStealth_GhostEffect_Allies` effect is applied. This includes the Raven. Because the effect has a stackLimit = 1, any existing effect is refreshed and the new effect instance is dropped. In this case, the Raven and the Crab both receive a message about a new effect `ECMStealth_GhostEffect_Allies`, and both refresh their existing effect (from `TurnDirector.CheckGameBegin`).

The Raven then moves, each hex of which invokes `OnPositionUpdate`. When the Crab is more than 100m away, the Crab's `AuraCache` receives a message that `ECMStealth_GhostEffect_Allies` should be removed. The Crab removes the effect and loses the benefit. On each hex of movement, the Raven refreshes its initial instance of `ECMStealth_GhostEffect_Allies`.

Finally the Raven completes `OnActivationEnd`. It checks for all actors in range, of which there are none except the Raven. The Raven's `AuraCache` receives another message saying add `ECMStealth_GhostEffect_Allies`, and it refreshes the initial instance of `ECMStealth_GhostEffect_Allies` it's carried since `TurnDirector.CheckGameBegin` fired.

The Raven will only lose its initial `ECMStealth_GhostEffect_Allies` instance if it were to shutdown, die, or the component that provides the effect (EWE) were to be destroyed.

### Aura Oddities

The logic above works fine when there's a single emitter of `ECMStealth_GhostEffect_Allies`. When there are multiple emitters though, things get a bit weird. Let's say we have Raven A and B with our Crab. If the Crab is within the bubble of both A and B, and Raven A moves away, the Crab keeps `ECMStealth_GhostEffect_Allies` and it works as you'd expect. Unfortunately, HBS code __makes a special case for ECM auras__ and this does NOT occur when general aura logic is applied.

Instead of `ECMStealth_GhostEffect_Allies`, let's say Raven A and B have `Custom_Ally_Effect` instead. This is defined as a `StatisticEffect` which means it will modify a string or integer value on the target actor.

When A leaves range of the Crab, the Crab receives a message to remove `Custom_Ally_Effect`. Because there's a stack limit of 1, the only effect to remove is the initial effect from `TurnDirector.CheckGameBegin`. The end result is that once A's call to `OnPositionUpdate` is complete, `Custom_Ally_Effect` will be gone from the Crab __even though it's within the area of Raven B's aura.__

If anything activates before the Crab or Raven B, the Crab will not have the benefit of `Custom_Ally_Effect`. It only regains `Custom_Ally_Effect` once Raven B checks its aura targets, or the Crab activates and notices it is within B's range.

This is reason for one of the fixes above. AurasHelper tracks all aura emitters and makes sure the effect is only removed from the Crab once it leaves the range of __all emitters__. This allows general auras to function like you would expect (just like ECM ones do).

## Statistic Effects

Some effects modify an actors _statistics_, which are key-value pairs that are stored on the actor. These statistics are read by many systems, and are how the effect system and game mechanics get tied together in most cases. Custom statistics are possible to set as well, which provides mod developers with a convenient hook as well.

Statistic values can be integer, floats, strings or bitmasks. Mathematical operations are supported on them, and the bitmasks support a few other operations we won't discuss. For instance you can define STAT_X as an integer, then add, subtract, or set the value from harmony injections.

These operations occur when the effect is applied. When multiple effects are applied, each operation occurs in the order the effect was applied. Each unique effect applies their operation, and multiple instances of the same effect apply up to their __stackLimit__ (see above).

For instance, let's say there are EFFECT_A, EFFECT_B, and EFFECT_C. A applies a -2 modifier, B a +3, and C a -1. A and B have a stackLimit of 1, while C has a stackLimit of 3. All effects modify STAT_X. If C has all three effects in place, the final value of STAT_X would be 3-1+1+1+1=5.  

This works well, but doesn't allow logic like 'take the highest value' or 'highest +1 for each additional value'. This mod tracks all of the aura effects that modify a statistic effect and records the value that was applied (along with other information). This is exposed through the `<STATNAME>_VALUES` statistic and can be queried by mods that want to make more complex decisions upon all the values that are present.


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
