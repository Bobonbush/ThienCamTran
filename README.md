# Game2D

## Inventory, Item, and Chest Setup

### Controls

- Press `E` near a chest to open it.
- Press `E` near a dropped item to pick it up.
- Press `I` to open or close the player inventory UI.

### Player

The player must have these components:

- `PlayerController`
- `Inventory`
- `InventoryUI`

`PlayerController` requires `Inventory` automatically. If `InventoryUI` is missing at runtime, the controller adds it automatically.

### Item Prefab

Create an item prefab with:

- `Item`
- `Collider2D`
- Optional `Rigidbody2D` if the item should fall or pop out of a chest.

Recommended setup:

- `Collider2D`: keep `Is Trigger` off if the item should stand on the ground.
- `Rigidbody2D`: `Body Type = Dynamic`, `Gravity Scale = 1`.
- `Item > Item Name`: display name stored in inventory.
- `Item > Amount`: amount added when picked up.
- `Item > Ignore Player Collision`: enabled, so the player can walk through the item and press `E`.

### Chest / Item Container

Create a chest object with:

- `ItemContainer`
- `Collider2D`

Setup:

- `Collider2D`: `Is Trigger` is set by `ItemContainer`.
- `ItemContainer > Item Prefabs`: drag item prefabs that have the `Item` component.
- Optional: create a child `DropPoint` at the chest mouth and assign it to `Drop Point`.
- Tune `Burst Force`, `Upward Force`, `Spread X`, and `Spread Y` to control how items pop out.

Runtime flow:

1. Player presses `E` near the chest.
2. `ItemContainer` instantiates every `Item` prefab in `Item Prefabs`.
3. Dropped items can be picked up with `E`.
4. Picked items are stored in the player's `Inventory`.

### Inventory UI

`InventoryUI` builds a simple panel automatically if one is not assigned in the inspector.

- Press `I` to toggle the panel.
- Empty inventory shows `Empty`.
- Items show as `Item Name xAmount`.

No manual Canvas setup is required for the default UI. If the scene already has a `Canvas`, `InventoryUI` uses it. Otherwise, it creates an `InventoryCanvas`.
