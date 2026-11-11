# RPG Toolkit 2D Spell System

The spell module runs the same designer-authored spells in platformer and top-down scenes. It controls movement directly, uses swept `Physics2D` queries for collisions, and shares its step logic with the trajectory preview.

## Scene setup

1. Add one `SpellWorld2D` to the scene.
2. Add `SpellCaster2D` to a caster and assign the world.
3. Add `SpellTrajectoryPreview2D` and a world-space `LineRenderer` when aiming needs a preview.
4. Call `TryCast(spell, origin, direction)` from player input or AI. The spell system itself does not read input.
5. Put damageable objects on the spell's target layers and add an `IDamageable` component such as `Health`. Put terrain on its obstacle layers.

`Window > RPG Toolkit > Spells > Create Demo Content` creates shared spell assets plus top-down and platformer demo scenes. In either scene, press 1-4 to choose a spell, aim with the mouse, and left-click to cast.

## Authoring a spell

Create a spell with `Create > RPG Toolkit > Spells > Spell`. Assign a pooled runtime prefab containing `SpellInstance2D`, enter Damage/Speed/Lifetime/Radius directly, choose target and obstacle masks, then assign a motion asset.

Rules connect a trigger to condition and effect assets. Empty conditions always pass. `All` requires every condition; `Any` requires one. `Once` records its state on that active spell. `Repeat` can run again after its interval. ScriptableObject assets remain immutable while every cast stores its own rule timers, modifiers, targets, lifetime, and hit history.

Included building blocks:

- Ballistic and homing movement.
- Lifetime, caster-stat, and spell-stat conditions.
- Damage, spell-stat modifier, and child-spell effects.
- Spawn, tick, hit, bounce, expiry, and area-pulse triggers.

To add a behavior, derive from `SpellMotion2D`, `SpellCondition`, or `SpellEffect`. Read or change the active spell only through `SpellEvaluationContext`; never store per-cast values on the ScriptableObject.

## Stats and safety limits

Actor and spell modifiers use `(base + flat) * (1 + summed percentages)`. Applying the same modifier asset again refreshes its value and duration; different assets stack. Caster conditions choose values captured when casting or the caster's current `EntityStats` values.

Each root cast has a shared default budget of 64 spell instances and eight child generations. This bounds recursive child-spell combinations. Preview is also bounded to five seconds and 256 points, and it suppresses damage and child spawning.

Run `Window > RPG Toolkit > Spells > Run Smoke Checks` after changing the module.
