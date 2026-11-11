# RPG Toolkit implementation plan — approved first slice

Source of product decisions: [RPGToolkitDesign.md](RPGToolkitDesign.md). This plan
was approved for the first Top Down implementation slice. The toolkit and demo
assets are under `Assets/RpgToolkit`; see its README for usage and package contents.

## Outcome and boundary

Ship a first usable 2D **Top Down** template in Unity 6000.6.0f1. A developer
opens the Window-menu wizard, selects/edits/saves an illustrated preset, creates
a new character prefab, authors a Tilemap or node exploration world, enters a
separate tactical arena, and exercises a small action timeline. The output must
work after importing the package into a fresh Unity project with its dependencies.

This slice includes the agreed movement, node maps, tactical traversal, arena
transition, and action timing foundation. Combat effects, party management,
inventory UI, equipment, spells, buffs, attacks, animation state authoring,
platformer implementation, and isometric implementation come in later slices.
The wizard will show only implemented templates and choices; it will not create
nonfunctional Platformer/Isometric prefabs or advertise unfinished systems.

## Existing project and safety constraints

- `Assets/Scripts/GridCoordinate.cs` and `GridLayout.cs` supply reusable square
  coordinates, cardinal neighbors, and cell-to-local math. Their existing callers
  and serialization must remain compatible. Extract only pure tile-edge logic
  if a proven common rule exists; do not depend on `GridManager` or road prefabs.
- `Assets/Settings/InputSystem_Actions.inputactions` already has a Player/Move
  action and UI pointer actions. Inspect actual bindings before deciding whether
  a separate toolkit action map/asset is needed. Keep character input isolated
  from the existing road-grid input router and car controller.
- `Assets/Scenes/TemplatesBase.unity` is a user-created, currently untracked scene
  with a camera. Leave it untouched. Create dedicated toolkit demo scenes/assets.
- Existing tracked prefabs, SampleScene, GridManager, and catalog have user edits.
  Preserve them. Do not run a second Unity Editor on the same project while the
  live Editor is open. Avoid generated Library/Temp/IDE artifacts in Git.
- Use existing Input System, Tilemap Extras, Unity Editor APIs, and Physics2D.
  No additional package or navigation library is planned for this slice.

## Stage 1 — authoring model and character wizard

1. Add runtime preset assets (`ScriptableObject`) with a name, description,
   preview `Sprite`/`Texture2D`, Top Down movement choice, numeric settings,
   tile rule reference, and output options. Keep current health/movement state
   out of shared preset assets.
2. Add an Editor-only `Window > RPG Toolkit > Setup` window with preset selector,
   image/description preview, editable draft, Save/Save As, validation, review,
   and Create Prefab. Add a Project Settings catalog only for selecting available
   presets/images; presets remain ordinary exportable assets.
3. Generate a new prefab with physics root, collider, chosen movement component,
   explicit input adapter, and visual child. Store a copy of authored settings
   on the prefab, so later preset edits do not change it. Use Unity asset APIs,
   unique output paths, Undo where applicable, and cleanup on partial failure.
4. No scene object reference is serialized into a prefab. The world setup binds
   instantiated characters to a scene grid/arena via an explicit component.

**Check:** Save/reopen a customized preset; create two prefabs with unique names;
inspect required components and independent settings; cancel/invalid input
creates no asset; export/import preset, preview, prefab, and references.

## Stage 2 — Top Down exploration movement

1. Input adapters translate mouse, keyboard, and controller input to typed
   per-character intent events. AI destination requests enter the same movement
   consumer. Subscription lifecycle, UI suppression, pause/death gating, and
   cancellation are handled at the proper owner.
2. Add continuous free movement using Rigidbody2D and an optional destination
   follower. For click-to-move, sample Physics2D clearance into a navigation
   graph, run A*, validate collider-sized waypoint shortcuts, and replan when
   blocked. Share navigation with enemy destination requests. Expose unreachable
   results and bound replan frequency.
3. Add cell-by-cell exploration movement over the gameplay Tilemap, with
   occupancy and directional/edge validation but no combat budget or turns.
4. Create a small demo scene with obstacle and concave-route cases. The wizard
   can generate Free, Point and Click, or Grid exploration character prefabs.

**Check:** Keyboard/controller and click routes move the same actor correctly;
enemy requests follow reachable routes; collisions respect body clearance;
unreachable goals report failure; disabling input stops stale motion; no input
leaks across two instantiated characters.

## Stage 3 — tactical gameplay grid

1. Add a separate scene-level gameplay-grid component backed by Unity Tilemaps.
   It reads template-specific gameplay rule assets mapping visual tiles to
   walkability, positive integer entry cost, and four-way edge compatibility.
   The first rule profile is Top Down. Missing/ambiguous rules are reported in
   the Editor. Art/autotiling is authored with Unity Tile/RuleTile assets.
2. Keep actor occupancy distinct from terrain and from road placement. All
   other actors block transit and destinations. Provide spatial occupancy lookup
   for later attack queries without putting attack decisions in the grid.
3. Use bounded Dijkstra for every cell reachable under effective integer
   `MaxMovement`, and A* with admissible Manhattan heuristic for a destination.
   Share one traversal/cost source between preview and actual movement. Revalidate
   each committed step, update occupancy atomically, charge movement once, and
   refill only on that actor's next activation.
4. Add a tactical arena demo Tilemap with normal and costly terrain, two actors,
   reachable-cell/path preview, and blocked destination feedback.

**Check:** Four cardinal steps only; cheaper detour beats a short costly path;
zero/exact/hundreds budget; allies/enemies block; no double occupancy; changing
occupancy invalidates stale paths; budget resets once on actor activation and
is never charged during exploration.

## Stage 4 — node maps and separate arena transition

1. Add a graph asset with stable node IDs, display positions, optional encounter
   references, and explicit links. A free manual editor permits arbitrary links.
   A layered editor starts with one node in layer 0, lets authors place multiple
   nodes in later layers, and defaults the link picker to the previous layer.
   Branches, merges, and dead ends are valid. Broken links are errors; unreachable
   authored nodes are warnings. A selectable destination must be connected to
   the current node. Do not require a final exit or fabricate links.
2. Add encounter definitions referencing an arena scene and spawn markers.
   A scene-level transition coordinator suspends exploration commands, loads the
   arena, binds actors/grid/timeline, and returns to the prior exploration
   context on a minimal result signal. Preserve actor identity and implemented
   stats; leave future HP/inventory persistence to their owner components.
3. Failed load/setup returns control to exploration with an actionable error.
   Ending combat cancels queued actions and clears grid occupancy before teardown.

**Check:** Author and select branching/dead-end nodes; save/reload graph;
reject dangling links; enter a separate arena and return to the prior node;
failed arena binding does not strand controls or corrupt campaign position.

## Stage 5 — action timing foundation

1. Add a scene-level timeline that owns logical time, stable ready ordering,
   action request validation, completion, and cancellation. Actor readiness
   drives turn activation and the tactical movement refill. Effects remain in
   their submitting components; no subclass per attack or item is required.
2. Expose composition-friendly timing inputs: actor velocity, action cost,
   and recovery after the action. Resolve them in one deterministic calculation
   with positive values and bounded results. Proposed initial rule:
   `nextReady = currentTime + ceil(actionCost * speedScale / velocity) + recovery`.
   Sample values are tuning defaults, not a public promise. Velocity may be
   modified by later components; only the timing resolver reads those modifiers.
3. Add a demo movement/wait action and a demo item action. An item is submitted
   by the current actor, resolves its own effect, and delays that actor's next
   readiness according to its configured timing cost. Lower-cost items offer
   faster return. This does not introduce the full inventory system.

**Check:** Different velocities/costs produce expected order; equal-ready ties
are stable; quick item returns earlier than costly item; rejected/cancelled
actions never fire twice or advance time; actor removal and arena exit clean
the queue; movement refills once when that actor becomes active.

## Integration and delivery gate

- Compile in the live Unity Editor and inspect Console before/after imports.
- Run focused EditMode checks for graph validation, weighted reachability/A*,
  timing order, and invalid configuration; PlayMode checks for input lifecycle,
  Physics2D clearance, occupancy, prefab wiring, arena entry/return, and cleanup.
- Exercise the wizard and map editor by hand, including Undo, saved presets,
  serialized references, and an export/import into a clean Unity project.
- Review only toolkit-owned diffs. Report passed checks, existing Console errors,
  and remaining limitations precisely. Preserve the user's dirty worktree.

## Assumptions for approval

- In layered mode, the link picker defaults to adjacent layers. Free manual mode
  supports arbitrary links. If skipping layers inside layered mode is required,
  the authoring UI can expose it without changing the runtime graph.
- The timing formula above is an initial tuning rule. Velocity after an attack
  can affect future readiness via action recovery/modifiers; exact balancing
  remains adjustable in assets. No full combat action effects are included here.
- A dedicated demonstration scene and test assets are created under a new
  toolkit folder, preserving the user's `TemplatesBase.unity` and SampleScene.
