# Grid hotbar setup

1. Create an asset with **Assets > Create > Modular Grid > Hotbar Catalog**.
2. Add one item per placeable object. Assign its `GridFootprint` prefab and an optional icon sprite.
3. Author the Canvas, EventSystem, ScrollRect, viewport, content, scrollbar, and slot objects in a prefab. Add `GridHotbarView` and `GridHotbarController` to its root.
4. Add `GridHotbarSlotView` to every slot and assign its Button, background Image, icon Image, and icon RectTransform. Assign all slots to the view list. To support another catalog graphic, duplicate a prefab slot and add its reference to the list.
5. Assign the catalog and the scene's `GridPlacementController` to `GridHotbarController`. The scripts report missing references and never create missing UI objects.

Drag the bar or its scrollbar to move through the catalog. Hovering highlights a slot; clicking selects it and calls `GridPlacementController.BeginPlacement` with that item's footprint.
