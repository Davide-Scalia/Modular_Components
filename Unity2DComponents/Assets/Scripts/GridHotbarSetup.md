# Grid hotbar setup

1. Create an asset with **Assets > Create > Modular Grid > Hotbar Catalog**.
2. Add one item per placeable object. Assign its `GridFootprint` prefab and an optional icon sprite.
3. Add a panel under a Canvas, size it to the desired hotbar width and height, and add `GridHotbarView` and `GridHotbarController` to the panel. The view builds its horizontal scroll area and item slots at runtime.
4. Assign the catalog and the scene's `GridPlacementController` to `GridHotbarController`.
5. Make sure the Canvas has a `GraphicRaycaster`. The view creates an Input System UI EventSystem if the scene does not already have one.

Drag the bar or its scrollbar to move through the catalog. Hovering highlights a slot; clicking selects it and calls `GridPlacementController.BeginPlacement` with that item's footprint.
