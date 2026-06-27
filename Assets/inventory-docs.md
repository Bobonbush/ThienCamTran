# Inventory / Item / Chest

## Controls

- `E`: open a nearby chest or pick up a nearby dropped item.
- `I`: open or close the player inventory.

## Player Setup

The player should have:

- `PlayerController`
- `Inventory`
- `InventoryUI`

`PlayerController` requires `Inventory`. At runtime, if `InventoryUI` is missing, `PlayerController` adds it automatically.

## Item Prefab Setup

Every pickup prefab needs:

- `Item`
- `Collider2D`
- Optional `Rigidbody2D` if the item should fall or pop out of a chest.

Recommended values:

- `Collider2D`: keep `Is Trigger` off if the item should collide with the ground.
- `Rigidbody2D`: `Body Type = Dynamic`, `Gravity Scale = 1`.
- `Item > Item Name`: the display name stored in inventory.
- `Item > Amount`: how many units are added when picked up.
- `Item > Ignore Player Collision`: enabled, so the player can walk through the item and press `E`.

## Chest / ItemContainer Setup

Every chest object needs:

- `ItemContainer`
- `Collider2D`

Setup:

- Drag item prefabs with the `Item` component into `ItemContainer > Item Prefabs`.
- Optional: create a child object named `DropPoint` at the chest mouth and assign it to `Drop Point`.
- Tune `Burst Force`, `Upward Force`, `Spread X`, and `Spread Y` to control how items pop out.

Runtime flow:

1. Player walks near a chest and presses `E`.
2. `ItemContainer` instantiates every prefab in `Item Prefabs`.
3. Items pop out into the scene.
4. Player walks near an item and presses `E`.
5. The `Item` is stored in the player's `Inventory`.

## Inventory UI

Press `I` to toggle the inventory panel.

`InventoryUI` creates a default panel automatically if no UI references are assigned:

- If the scene already has a `Canvas`, it uses that canvas.
- If no canvas exists, it creates an `InventoryCanvas`.
- Empty inventory shows `Empty`.
- Items display as `Item Name xAmount`.

Manual UI is optional. To use a custom panel, assign `Panel Root` and `Content Root` in `InventoryUI`.
