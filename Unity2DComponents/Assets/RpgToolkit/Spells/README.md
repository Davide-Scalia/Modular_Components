# RPG Toolkit 2D Spell System

## Casting and ownership

Add a SpellWorld2D to the scene and assign it on each SpellCaster2D. Call
TryCast(spell, origin, direction), or TryCast(spell, direction) to use the
caster position. Input and AI need no other runtime calls.

Each cast captures the caster's stats. Children inherit that snapshot. An
instance captures its spell base stats and fixed lifetime when it spawns;
changing caster stats or the asset later does not alter that instance's
baseline. Caster identity and position can still be used. Lifetime modifiers
are invalid and are reported by validation rather than silently applied.

SpellEvaluationContext owns per-instance age, modifiers, targets, and timers.
SpellInstance2D handles the scene object and pool lifecycle. SpellSimulation2D
is shared by live FixedUpdate movement and trajectory preview. Movement uses
swept Physics2D queries; it does not require a Rigidbody2D on the spell.
Definition, condition, effect and motion assets contain authoring data, never
mutable per-cast state.

## Rules

Rules match a trigger, evaluate All/Any conditions, and apply their effects.
Empty conditions pass. The execution mode determines what follows:

| Mode | Behavior |
| --- | --- |
| Once | First matching trigger whose conditions pass executes once per instance. |
| Every Event | Each matching trigger checks conditions and applies effects. Use for every hit or area pulse. |
| Repeat on Interval | The first passing trigger applies effects immediately and starts one timer. Later executions do not recheck the condition or require another trigger. |

OnTick conditions are polled every 0.1 seconds, starting at spawn. Scheduled
repeats and area pulses have a 0.1-second minimum, with 0.25 seconds as the
default interval. Movement still runs every physics step. Delayed frames do
not cause catch-up bursts. Spell-owned timers stop when the spell ends;
OnExpire cannot start a timer that outlives its spell.

Hit-started repeats retain the actual collision target; a missing, disabled or
dead target receives no damage. Area effects query current occupants on each
execution, deduplicating enemies with multiple colliders. Reusable result
lists have no fixed 64-collider truncation.

## Conditions and modifiers

Spell Stat Percentage thresholds use fractions: 0.5 means 50%, 1.5 means 150%.
Lifetime compares elapsed seconds with fixed lifetime times the threshold.
Damage, Speed and Radius compare their current value with captured base value
times the threshold. This also defines zero-base behavior without division.
The separate Lifetime condition compares elapsed seconds directly.

Use Greater Or Equal for reaching a timing threshold: exact equality can be
missed between polls. With a five-second spell, Lifetime >= 0.5 starts at
2.5 seconds, subject to the 0.1-second polling cadence.

Modifiers use (base + summed flat) * (1 + summed percentages).
Refresh keeps one contribution from the same modifier asset and renews its
expiry. Add accumulates applications with individual expiries. Duration zero
means until the spell ends. Existing modifier assets default to Refresh.

## Target-owned damage over time

Damage Over Time is a spell effect with a damage multiplier, critical toggle,
duration, and interval. It applies to the enemy actually hit, or each enemy
currently in an area. One DamageOverTimeReceiver on the target owns all its
active damage statuses. Damage inputs and critical stats are captured at
application, and the first tick occurs after the interval. Add a separate
Damage effect for immediate impact damage.

The status survives the projectile and caster. Reapplying the same status
asset from the same caster refreshes its duration and captured damage without
postponing an already pending tick. Different casters remain independent.
Death, disabling, or destruction clears statuses. This is a focused damage
feature; temporary actor-stat modifiers remain in EntityStats.

## Limits and pooling

Maximum Concurrent Spells defaults to 64 **per cast family**, including the
root and all active descendants. Finished or destroyed instances release
capacity exactly once. The family survives its parent. Maximum Spawn Depth
defaults to 8; the root is depth 0. Rejected scheduled child spawns can retry
on the next interval. Warnings identify rejection reasons once per reason
per family.

Maximum Inactive Per Spell on the world defaults to 128 retained inactive
objects **per spell definition**. It is a storage setting, not an active-spell
limit. A spell finishing at capacity runs its final effects before releasing
its slot, so a child requested during those final effects can be rejected.

## Preview, demos and checks

SpellTrajectoryPreview2D renders a LineRenderer using the same simulation.
Preview is bounded to five seconds / 256 points by default. It applies local
stat changes but suppresses damage, target statuses and child spawning.

Window > RPG Toolkit > Spells > Create Demo Content generates example assets
and scenes. It overwrites its generated demo content; do not run it to migrate
hand-authored demo changes. The generated timed projectile demonstrates
half-lifetime growth; the homing example applies a burn status.

Enter Play mode, then run Window > RPG Toolkit > Spells > Run Smoke Checks to verify casting,
pooling, family capacity, rule modes, percentage conditions, modifiers,
immutable stats/lifetime, target-owned damage, crowded area queries, and
preview parity. The same entry point can be invoked in Unity batch mode with
-executeMethod RpgToolkit.Spells.Editor.SpellSmokeChecks.RunFromCommandLine
without -quit. The batch runner enters Play mode and exits with a nonzero
code on failure. This exercises actual Unity destruction/disable callbacks.

## Migration and rollback

This migration updates code and assets together in one local project; there
is no supported mixed-code player rollout. Keep the migrated scripts and
assets in the same build. Existing script and asset GUIDs are preserved.

- Rule enum 0 remains Once; 1 now names Every Event; 2 is Repeat on Interval.
  Existing OnTick Repeat rules were changed from 1 to 2; other event rules
  retain 1. Authored intervals are retained, with a runtime floor of 0.1s.
- Half Lifetime now uses 0.5. The authored Slow Water Orb ten-second repeat
  interval remains unchanged and warns because its lifetime is five seconds.
- FormerlySerializedAs preserves maximumSpellsPerCast as
  maximumConcurrentSpells and maximumPoolSize as maximumInactivePerSpell.
- Removed caster-stat read-mode fields are ignored; every read uses the cast
  snapshot. Modifier mode defaults to Refresh.
- SpellCastBudget was replaced by SpellCastFamily; SpellSpawnRequest no
  longer carries a live EntityStats reference. Custom callers must rebuild.

Before editing, a full copy of this module was saved locally at:
C:/Users/kingd/AppData/Local/Temp/spell-migration-1794412300004/Spells

To roll back, stop Play mode and restore only the changed module files and
migrated assets from that backup, preserving newer unrelated work. Remove the
two newly added damage-over-time scripts and their .meta files only as part
of that explicit rollback. Restore code and data together; restoring only
old enum consumers would reinterpret migrated rules incorrectly.
