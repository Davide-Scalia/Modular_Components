# RPG prefab UI setup

Author the Canvas, EventSystem, node graphics, and buttons in the scene or prefab before entering Play Mode. The link line is the sole procedural UI exception.

## Arena UI

1. Add `ArenaDemoControls` to the prefab root and assign its model references, Canvas, TextMeshProUGUI labels, and Wait/Item/Retreat buttons.
2. Add `ArenaAttackSlotView` to each authored attack button. Assign that slot's Button, Image, and TextMeshProUGUI, then add the slot view to `ArenaDemoControls.attackSlots`.
3. Add one already-configured `HealthVisual` per actor and list them in actor order. Its red and yellow fill Images stay owned by the health-bar prefab.
4. Author inactive, non-raycast Image objects for cell highlights and add them to `overlays`. Duplicate and assign another Image whenever the arena needs a larger visible overlay pool.
5. Keep an EventSystem in the scene or parent prefab.

## Node map UI

1. Put `MapNode` and `NodeMapNodeView` on each node GameObject in the scene. Configure its label, encounter, and outgoing `MapNode` references on `MapNode`; assign its Button, Image, and TextMeshProUGUI on the view.
2. Put `NodeMapNavigator` on a scene object, assign every scene node to its Nodes list, and assign one of those nodes as Start Node. Put `NodeMapPresenter` on a scene object and assign Navigator, Canvas, EventSystem, and Loot Label. Assign both Navigator and Presenter to the scene's `EncounterCoordinator`.
3. Add a `NodeMapLinkView` RectTransform for every connection and drag the source and target `NodeMapNodeView` components into its From and To fields. This is the sole procedural-UI exception: it creates its own non-raycast Image line and continuously connects the centers of the two scene nodes. Thickness and color remain Inspector-editable.
4. Author the Canvas, panel, title, background, and EventSystem in the prefab; none are generated at runtime.

Missing required references produce a clear Console error and disable the owning view instead of generating fallback UI.

The navigator checks outgoing scene-node references and the presenter binds each node's Button to its encounter. Set the optional loot pool and destination scene on that node's `EncounterDefinition` asset; include the destination in Build Settings. A blank destination grants loot immediately. Destination scenes load additively so scene node references survive; the coordinator hides map UI and cameras, then restores them after the encounter scene unloads. A noncombat destination can call `EncounterCoordinator.ReturnToMap` to return.
