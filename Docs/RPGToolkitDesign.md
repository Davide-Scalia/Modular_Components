# RPG Toolkit design — iterative draft

## Capability and product intent

Give Unity developers a window-based workflow for rapidly creating 2D character
prefabs from configurable gameplay templates, with illustrated presets that can
be saved in the project and exported with their referenced assets.

This document records the design conversation. Only decisions explicitly marked
confirmed are approved product choices; recommendations remain proposals.

## Confirmed constraints

- Support 2D only for this version.
- Expose setup through Unity's Window menu.
- Generate new character prefabs; configuring existing controllers is out of scope.
- Allow customizable presets saved as project assets.
- Provide Platformer/Metroidvania, Top Down, and Isometric template families.
- Top Down and Isometric offer point-and-click, custom-grid, and free movement.
- Template images must be configurable and portable with the exported content.
- Plan through successive questions and Editor diagrams before implementation.
- Start with Top Down.
- Create a separate gameplay-grid template for navigation and spatial queries.
  Do not depend on the road placement manager, road prefabs, or placement state.
- Reuse appropriate coordinate math and tile-compatibility checks through a
  small shared foundation; placement and gameplay keep separate responsibilities.
- Gameplay-grid movement is square and four-directional; no diagonal steps.
- Keep mouse/controller input outside the components performing gameplay actions,
  communicating intent through events.
- Continuous point-and-click navigation must route around Physics2D obstacles
  and be usable by players and enemies.
- Grid navigation must calculate reachable cells under an integer MaxMovement
  budget, support weighted terrain costs, and find routes to selected destinations.
  Support budgets in the tens/hundreds without a single-digit assumption.
- In tactical combat, refill remaining movement at the start of the actor's turn.
- Allied and enemy occupants both block tactical traversal and destinations.
- Combat encounters take place in a separate arena.
- Support manually linked node maps and a layered node-map authoring mode.
  Layer zero is exactly one start node; later layers contain multiple nodes with
  authored branching connections. Dead ends are valid; routes are not forced linear.
- Combat order is managed by timing/initiative rather than a fixed player-team
  then enemy-team sequence. Other systems, actors, and items may submit actions.
- Exploration is a separate mode: node-map travel, cell-by-cell exploration
  movement (Pokemon-like), or continuous free movement. Do not model exploration
  merely as tactical combat with unlimited movement points.
- Author gameplay worlds with Unity Tilemaps, using template-specific tile rules
  (for example, platformer rules differ from top-down rules).

## Requested systems awaiting detailed design

- Party management, enemy encounter triggers, encounter lifecycle, and turn-based moves.
- Character state machines and animation integration per state.
- Weapon attacks with immediate/event timing or cast/animation-driven hit checks;
  optional nested combo settings.
- Inventory with drag/drop, slot validation, and efficient category queries.
- Spells, mana management, and spawned attack objects.
- Shared player/enemy health.
- Buffs/debuffs, damage over time, stat modifiers, and configurable stacking rules.
- Armor slots, equipment changes, stat contributions, and granted effects.
- Shared stats: Attack, Defense, Special Attack, Special Defense, Crit Rate,
  Crit Damage, and integer MaxMovement. Systems consuming these stats must
  declare that dependency.

## Proposed surfaces and workflow

- Window > RPG Toolkit > Setup: choose preset, movement, systems, settings, output.
- Project Settings > RPG Toolkit: template catalog with images and descriptions.
- Preset Inspector: edit the same authoring configuration outside the wizard.
- Presets distinguish exploration mode from combat mode. Node-map and encounter
  configuration have world/session scope; their authoring belongs outside the
  character prefab itself. A dedicated world/encounter setup section is proposed.
- Draft -> validate -> review -> create new prefab.
- Save/Save As persist explicit preset edits; Cancel discards the working draft.

## Proposed interface contract

- Inputs: template family, movement settings, optional systems, asset references,
  character name, and project output folder.
- Outputs: a new prefab and any required generated supporting assets.
- Resolve dependencies before generation and explain auto-included components.
- Validate incompatible choices and missing required references before writing.
- Use a new asset path on repeated creation; do not silently overwrite a prefab.
- Clean up only assets created by a failed generation operation.
- Scene placement is separate from prefab creation; no automatic scene modification.
- Grid-dependent prefabs report a missing scene grid clearly when instantiated.
  Scene binding strategy remains to be designed; prefab assets cannot retain
  references to arbitrary scene objects.

## Proposed data ownership

- A ScriptableObject preset owns authoring defaults, description, and image.
- Generate a configuration snapshot on the prefab. Later preset edits affect
  future generation, not previously generated prefabs.
- Each character instance owns current health/mana, inventory contents, cooldowns,
  and status effects. Never store shared mutable character state in preset assets.
- Character root owns movement physics and selected gameplay components.
- A visual child owns sprite rendering and optional animation integration.
- Encounter and party services belong to scene/session scope, not duplicated
  on every character. Their creation/binding workflow is unresolved.
- Encounter definitions point to a separate arena and spawn/setup data. Arena
  instances are scene-owned; a transition coordinator retains the campaign state
  needed to return to node exploration.
- Tilemaps supply authored tiles and art; template rule assets supply gameplay
  interpretation. Runtime occupancy and mutable encounter state remain instance data.
- Editor tooling remains editor-only; exported runtime code has no UnityEditor dependency.
- Prefer Unity facilities and installed dependencies; no new framework is proposed.

## Existing project evidence

- Unity 6000.6.0f1; URP 2D and Unity Input System installed.
- ModularGrid.GridCoordinate and GridLayout provide square and axial hex coordinates.
- GridLayout currently has four square neighbors and six hex neighbors; no
  isometric projection or diagonal square navigation is supplied there.
- GridManager owns grid generation and piece-placement occupancy/connection rules.
  Placed-piece occupancy is not equivalent to navigation blockage; a road may be walkable.
- GridInputRouter handles placement/rotation/panning and UI click suppression;
  it is not a character movement controller.
- Reuse suitable coordinate/layout logic after verifying callers; navigation
  needs its own walkability and character occupancy rules.
- GridConnectionResolver owns placed-piece registrations and depends on
  GridFootprint/GridPieceConnections. It cannot be used as the gameplay grid.
  Extract only meaningful pure coordinate/edge-compatibility operations when
  implementing; preserve existing placement semantics and serialized assets.
- No dedicated third-party 2D navigation dependency appears in Packages/manifest.json.

## Proposed movement design

### Commands and ownership

- Input System adapter translates mouse/keyboard/controller actions into typed
  per-character events (direction input or destination request).
- AI decisions submit destination requests to the same character command consumer.
- Movement validates requests and owns active movement/path state. Input knows
  nothing about terrain costs, movement budgets, damage, or Rigidbody2D details.
- Use local C# events and explicit wiring; avoid a global broadcast bus for
  per-character commands. Pair subscriptions with unsubscriptions on disable.
- UI input suppression, pause/death gating, and command cancellation must be
  consistent across input sources; disabling input cancels stale direction intent.
- Publish arrival, blockage, and movement-budget changes as outcome events.

### Gameplay grid and queries

- Gameplay grid owns cell data, edges, and actor occupancy, with scene lifetime.
- Traversal rules evaluate bounds, edge compatibility, terrain eligibility, cost,
  and occupancy for a particular actor. They do not mutate the road placement grid.
- Search consumes traversal rules; movement commits occupancy and budget changes.
- Future attack queries can reuse coordinates and spatial data, but retain their
  own range/line-of-sight/targeting rules. Walkability does not imply attackability.
- Do not implement enemy-nearby checks, damage, or attack rules in the grid owner.
- Unity Tilemap authoring is confirmed. Movement/attack tile compatibility details
  and the exact rule-asset schema remain open.

### Reachability and routes

- Ordinary BFS gives minimum steps only when all transitions have equal cost.
- Recommend bounded Dijkstra search for every reachable cell under weighted costs.
  This is the cost-aware form of the requested outward flood, using a priority queue.
- Recommend A* for a selected destination, with four-way Manhattan distance
  multiplied by minimum legal step cost as its admissible heuristic.
- Costs must be positive integers; budget may be zero. Normal terrain cost 10,
  difficult terrain cost 20, and MaxMovement 100 are illustrative defaults only.
- Range preview and actual routing must use identical traversal/cost rules.
- Recheck the next step before committing it when obstacles or occupants change;
  never spend budget twice or allow two actors to commit the same cell.
- Refill remaining movement from effective MaxMovement at the start of the actor's
  turn. MaxMovement and remaining budget are distinct values.
- Other actors block both traversal and destinations, regardless of allegiance.
  The moving actor's own starting cell is not an obstacle to its search.
- Movement interruption/refund rules and mid-turn MaxMovement changes remain open.

### Tilemap and template rule assets

- Recommend keeping Unity Tile/RuleTile visual authoring separate from gameplay
  rule assets, so art/autotiling changes do not silently change navigation rules.
- RuleTile is Unity's neighbor-based visual tile system; it is not itself the
  movement, budget, or attack eligibility model.
- A template's rule asset maps authored tiles to relevant gameplay properties.
  Top-down examples: walkability, positive integer entry cost, edge compatibility,
  and sight blocking. Platformer examples: solid ground, one-way platforms, hazards,
  and climbable surfaces. These fields are proposed, not a finalized schema.
- Keep platformer physics semantics outside top-down traversal checks. Shared
  coordinates and data checks do not require one universal movement rule class.
- Missing gameplay rules should be identified during authoring validation;
  decorative layers should be explicitly excluded from navigation interpretation.
- Gameplay rules and Physics2D colliders must agree where both govern traversal;
  diagnose conflicts rather than letting a preview promise an impossible path.

## Proposed exploration and encounter design

- Node exploration owns a graph of destinations and permitted links. A selected
  available node requests entry into its content/encounter. Nodes are not tactical
  grid cells and do not consume the tactical movement budget.
- Grid exploration moves actors cell by cell using traversal and occupancy checks,
  but has its own control flow without tactical turns or movement-point spending.
- Continuous exploration uses direct input or destination navigation as configured.
- Mode choices are template/preset settings; supporting all modes does not imply
  runtime switching between every exploration mode within one game.
- Proposed lifecycle: Exploring -> EnteringEncounter -> InCombat -> Resolving ->
  ReturningToExploration. Load/setup failure returns to exploration with an error;
  defeat/escape behavior remains to be specified.
- Encounter lifecycle owns the transition. It suspends exploration commands,
  cancels active movement safely, prepares combat placement, and enables combat
  input only after valid setup. Character state machines remain actor-local.
- A shared party/character state can preserve HP, inventory, and effects across
  map/arena representations; which values persist/reset is an upcoming decision.
- Encounter placement is confirmed as a separate arena. Do not create arena or
  campaign scene infrastructure as a side effect of character-prefab generation.

### Node map authoring

- Support free manual node placement with explicit links, plus an optional layered
  layout for authored progression routes.
- Layer 0 contains exactly one start node. Each later layer may contain multiple
  nodes and authored links from the preceding layer. A layer can branch, merge,
  or contain dead ends; never synthesize a single linear route.
- Dead ends are valid. Validate dangling references and that reachable
  destinations have a valid route from the start. Unreachable nodes should be
  highlighted as authoring warnings, not deleted or silently repaired.
- Whether layered connections may skip a layer remains open. Free manual maps
  can use arbitrary authored links; layered mode should default to previous-layer
  links while preserving explicit branching.
- Layers are an authoring aid. Runtime traversal uses an ordinary node graph so
  manually authored and layered maps share destination-selection behavior.
- Proposed node data: stable node identity, display position, node/encounter
  reference, and explicit outgoing links. Encounter metadata stays with the
  encounter definition, not duplicated on each edge.
- Hand-authoring UI should allow creating/dragging nodes and selecting links
  from the preceding layer. Layout lines are visual grouping, not movement lanes.

### Combat action timing and turn order

- A scene-level CombatTimeline owns combat clock, action queue, ready order,
  and completion/cancellation. It does not own health, inventory, attack effects,
  or actor inheritance.
- Actors and systems submit action intents through one narrow request boundary.
  An intent identifies its acting source/target context and action data; an
  external item or companion system can submit without deriving a new actor class.
- Components provide timing inputs/modifiers by composition. A resolver computes
  the action's schedule from current velocity, action delay, and post-attack
  recovery/velocity. The timeline alone chooses what becomes ready next.
- Prefer an absolute logical time queue with a stable tie-break sequence. Advance
  to the next ready action, resolve its effect, then schedule its recovery/next
  availability. Avoid Update polling and round-robin team phases.
- Separate calculation of timing from action effects. Use a small action definition
  plus resolver/handler registration or a typed completion callback at a meaningful
  boundary; do not require a subclass per move, item, or external system.
- Item use is submitted as an action by the current action taker. Its configured
  timing cost delays that actor's next readiness on the shared timeline; lower-cost
  items can therefore be more tempo-efficient. It does not create an independent
  turn-taking actor. The item system owns its effect; inventory/gameplay still owns
  availability and consumption. Items used outside combat do not enter this timeline.
- Define one authoritative action lifecycle: requested -> accepted/queued -> ready
  -> resolved or cancelled. Rejected actions do not advance time. Resolve an action
  at most once; define target loss/death and combat end cleanup before implementation.
- Exact velocity formula, action interruption, simultaneous-ready behavior, and
  how much an item delays the acting source remain open decisions.

### Continuous navigation

- Propose a small Physics2D-based navigation implementation first; library selection
  is not finalized. No external package installation is needed for planning.
- A navigation sampling grid describes free-space clearance independently of the
  tactical gameplay grid and its movement budgets. A* plans a route, then validated
  waypoint shortcuts and a Rigidbody2D follower produce continuous movement.
- Clearance and segment casts must account for the actor's collider size, not
  merely a point. Tactical four-direction restrictions do not constrain this
  continuous navigation representation.
- Route planning handles detours; local obstacle steering alone can get trapped
  in concave/U-shaped obstacles. Replan on relevant changes or blocked progress,
  with bounded update frequency rather than full searches every frame.
- Plan using estimated travel time if terrain changes continuous movement speed.
  Optimality is relative to the sampled graph/costs; do not promise a globally
  fastest continuous route, especially around moving actors.
- Surface unreachable destinations and lost targets explicitly. Crowded-agent
  coordination and large-scene navigation performance need later validation.

## Non-goals for this planning iteration

- No gameplay implementation, asset migration, controller removal, or scene changes.
- No 3D support or integration with an existing character controller.
- No runtime claim or live Editor validation based only on filesystem inspection.

## Next open decisions

1. Layered links: may a route skip an intermediate layer, or only connect adjacent layers?
2. Timeline formula: how should velocity, action time, and post-attack recovery combine?
3. Transition persistence, defeat/escape, and exploration encounter trigger rules.
4. Tile compatibility semantics for movement versus attacks; interruption/refund rules.
5. Free movement acceleration, platformer settings, and remaining RPG systems in later iterations.

## Acceptance direction and handoff

Eventually prove: save/reload a custom preset; generate a new prefab with explained
dependencies; instantiate it in a compatible test scene; verify chosen movement;
verify two instances keep independent state; export/import with image references intact.

Movement proofs must include a cheaper detour beating a shorter expensive route,
exact-budget reachability, zero budget, blocked/unreachable destinations, no diagonal
tactical steps, changing occupancy, and Physics2D clearance around a concave obstacle.
Also verify equivalent player/AI destination commands and no cross-character event leakage.
Verify ally/enemy blockage, refill exactly once per actor turn, no tactical budget
spending in exploration, template-specific tile interpretation, and suppression
of stale exploration input while entering combat.

Current handoff: continue product clarification. This is not a finalized
implementation specification. No runtime tests are applicable to this document change.
