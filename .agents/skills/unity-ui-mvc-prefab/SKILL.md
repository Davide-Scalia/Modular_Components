---
name: unity-ui-mvc-prefab
description: Design or modify Unity UI components in this project using prefab-authored Views and MVC boundaries; apply whenever a task creates, changes, or reviews game UI.
metadata:
  short-description: Enforce prefab-driven Unity UI MVC
---

# Unity UI: prefab-driven MVC

Use this skill for any new or changed game UI, HUD, menu, panel, dialog, widget, or UI module in this project.

## Core rule

Do not instantiate GUI classes or `Rect`/`RectInt` values in scripts to build or lay out UI. Do not construct the visual hierarchy procedurally. The editable UI must be authored by the user as prefab assets and connected through `[SerializeField]` references (or serialized fields on a serialized configuration object when that is the existing project pattern).

If a UI element is needed, add it as a separate UI object/component in the prefab. Expose the references and view operations needed by the Controller; do not hide the visual design inside code.

## MVC ownership

- **Model:** the object/component that owns the data and state represented by the UI. It may contain or coordinate the gameplay components that compose that state. It must not create or arrange the visual UI.
- **View:** a component attached to a prefab-authored UI object. It owns all visual variations: showing/hiding, enabling/disabling, text/value presentation, sprites, layout-state changes, and view animation. Its visual dependencies are serialized references.
- **Controller:** responds to domain or input events and calls public View methods with the required state. It coordinates Model and View; it does not directly manipulate child UI components or create GUI/Rect objects.

Keep the View API intention-revealing and event-oriented (for example, `Show`, `Hide`, `SetHealth`, or `PlayWarning`) rather than exposing arbitrary implementation details. Put animation and activation logic in the View so the prefab can change without rewriting Controller logic.

## Implementation checklist

Before editing, trace the existing Model, View, Controller, prefab, and event seams and extend the owner of each invariant.

When implementing or reviewing UI:

1. Reject `new` for GUI classes and `Rect`/`RectInt` construction in scripts used by the UI.
2. Prefer serialized prefab references over `GetComponent`, hierarchy searches, runtime object creation, or hard-coded visual structure when connecting UI dependencies.
3. Keep prefab-editable choices in the prefab and its serialized fields; keep runtime state in the Model.
4. Keep visual transitions, animation, and activation/deactivation in the View.
5. Make the Controller the caller of View operations in response to events, without duplicating presentation logic.
6. Preserve null/error handling for required serialized references and avoid silently degrading to a procedurally generated UI.
7. Verify the narrow path: references survive serialization, the Controller receives the relevant event, and the View presents the expected state and transitions.

## Review boundary

This rule applies to game UI construction and presentation. It does not prohibit ordinary non-UI geometry calculations where a `Rect` is required by an external API, but such calculations must not be used to replace prefab-authored UI or its layout. If an existing system violates this rule, fix the responsible ownership seam rather than adding a second guard or duplicating presentation logic.
