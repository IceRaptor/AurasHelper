# AurasHelper
This mod for the [HBS BattleTech](http://battletechgame.com/) game changes statistic based auras to more closely match expected behaviors.





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