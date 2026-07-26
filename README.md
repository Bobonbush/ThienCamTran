# Thiên Cẩm Trận — A 2D Metroidvania (Playable Demo)

Final project for **3D Visualization and Game Development**
Faculty of Information Technology, VNU-HCM University of Science (Advanced Program)

Instructors: Huỳnh Viết Thám · Trần Ngọc Đạt Thành · Trần Minh Triết

---

## Team

| No | Student ID | Full name |
|:--:|:----------:|-----------|
| 1 | 23125048 | Nguyễn Thư Uyên |
| 2 | 23125008 | Đặng Bảo Khoa |
| 3 | 23125074 | Thới Gia Nghi |
| 4 | 23125004 | Trần Duy Anh Dũng |

---

## About the game

**Thiên Cẩm Trận** — roughly *the Thiên Cẩm Array* — is a 2D metroidvania built in Unity, drawing
on Vietnamese folklore and history rather than the European setting the genre usually reaches for.

The game takes place in **Vệ An**, a mountain fortress city in the fictional kingdom of **Trường
Xuân**. Vệ An was built directly on top of an old warding array holding down **Thiên Cẩm**, a
sealed spirit mountain, with four sacred relics hidden at the city's corners as the anchors of that
seal. When foreigners arrived from beyond the sea mist, something came with them: the seal came
apart, the relics went missing, and the city fell in a single night of storms. What is left is a
dead city whose people die and come back, over and over. The player explores the wreckage of a
formation that was supposed to hold.

### What is in this build

This is a **playable demo**, not the full game — one complete slice taken start to finish. It
covers the opening arc: the outskirts of the ruined citadel, the tunnel network underneath, and one
boss fight to close it out. That is **11 playable rooms plus 6 cutscenes**, running roughly **1–3
hours** depending on how much optional content the player finds. 27 features are implemented and
documented in the report. The remaining arcs, bosses and endings exist only in the design document.

### The core mechanic

The player has three bars instead of the usual one or two, and they deliberately behave
differently:

- **Health** falls when hit and never regenerates on its own.
- **Mana** *rises* when the player lands a hit, and is spent on the bow or drained entirely to heal.
- **Stamina** refills by itself and is consumed by attacking, each combo hit costing more than the last.

Because healing costs a full mana bar and mana only comes from hitting enemies, a player who is low
on health cannot retreat and wait it out — they have to go back in and fight to earn the heal.

---

## Applications and tools

| Application | Used for |
|---|---|
| **Unity** | Game engine — the entire build |
| **Aseprite** | Pixel-art sprite edits |
| **Git / GitHub** | Version control |
| **LaTeX** | The accompanying project report |

---

## Unity version

> **Unity 6000.3.16f1**

Open the project with exactly this version; the packages below are pinned to it.

| Package | Version |
|---|---|
| Universal Render Pipeline (URP) | 17.3.0 |
| Input System | 1.19.0 |
| Cinemachine | 3.1.6 |
| Timeline | 1.8.12 |
| 2D Animation | 13.0.5 |
| 2D Aseprite Importer | 3.0.2 |
| 2D SpriteShape | 13.0.0 |
| 2D Tilemap Extras | 6.0.2 |
| uGUI / TextMeshPro | 2.0.0 |

---

## Running the project

1. Open the project folder in Unity **6000.3.16f1**.
2. Open `Assets/Scenes/UI/MainMenuNew.unity` (build index 0) and press Play.

### Controls

| Key | Action |
|:---:|---|
| `E` | Interact / confirm |
| `I` | Open or close the inventory book |
| `Q` | Cancel / close |
| `A` | Arrow shot (costs mana) |
| `R` | Heal (drains mana) |
| `[` / `]` | Previous / next page in the book menu |

Movement, jump, dash and attack use the standard bindings shown in-game. The game plays fully on a
gamepad as well as keyboard and mouse, and **all keyboard bindings are remappable** in the settings
menu.

---

## Links

- **Gameplay walkthrough:** https://youtu.be/TdV-JnJ259I
- **Source repository:** https://github.com/Bobonbush/Game2D
- **Full report:** `report/report_real/main.pdf`

---

## Resource references

> **Notice.** Every art, audio and font asset in this build was created by someone else and remains
> the property of its respective copyright holder. This is an unpublished student project: it is
> not sold, advertised, monetised or publicly distributed. No AI-generated art, audio or text is
> used, and no asset pack is redistributed as a standalone download.
>
> **Placeholders.** The character, enemy, boss and NPC sprites are taken from *Momodora: Reverie
> Under the Moonlight* and *Momodora: Moonlit Farewell*, © 2010–2025 Guilherme Melo Martins, the
> property of [Bombservice](https://www.bombservice.com). **No licence to them is held or claimed**,
> and this project is not affiliated with, authorised by or endorsed by Bombservice. They are
> temporary placeholders and will be replaced with original artwork before any public or commercial
> release, and removed on request by the rights holder.
>
> The complete notice, including the licence audit, is in Section 8 of `report/report_real/main.pdf`.

### Art

| Asset | Author | Licence |
|---|---|---|
| [Kyrise's Free 16x16 RPG Icon Pack](https://kyrise.itch.io/kyrises-free-16x16-rpg-icon-pack) | Kyrise | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) |
| [Slash hit 01 Animation VFX](https://opengameart.org/content/slash-hit-01-animation-vfx) | GustavoPlima | [CC BY 3.0](http://creativecommons.org/licenses/by/3.0/legalcode) |
| [Platformer/Metroidvania Asset Pack](https://o-lobster.itch.io/platformmetroidvania-pixel-art-asset-pack) | [o_lobster](https://o-lobster.itch.io/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) |
| [Monsters Creatures Fantasy](https://luizmelo.itch.io/monsters-creatures-fantasy) | LuizMelo | [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/) |
| [Stabby Spikes](https://froggu999.itch.io/stabby-spikes) | froggu999 | [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/) |
| [Pixel Art Door Pack – ANIMATED](https://karsiori.itch.io/pixel-art-door-pack-animated) | KARSIORI STUDIO | CC0 |
| [Free Pixel Food!](https://henrysoftware.itch.io/pixel-food) | Henry Software | [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/) |
| [Fantasy Knight – Free Pixelart Animated Character](https://aamatniekss.itch.io/fantasy-knight-free-pixelart-animated-character) | aamatniekss | — |
| [Free Pixelart Tileset – Cute Forest](https://aamatniekss.itch.io/free-pixelart-tileset-cute-forest) | aamatniekss | — |
| [Simple Table and Chairs](https://bradfrey.itch.io/simple-table-and-chairs) | Brad Frey | — |
| [Fireball Pixel Art](https://bontt.itch.io/fireball-pixel-art) | Bont | — |
| [Wood Set](https://nyknck.itch.io/wood-set) | @nyk_nck | — |
| [FREE: Chest Animations](https://admurin.itch.io/free-chest-animations) | [Admurin](https://admurin.itch.io/) | — |
| [Medieval House Set – Statues](https://odgardian.itch.io/medieval-house-set-statues) | Odgardian | — |
| [Training Dummy sprite](https://silentshadow51.itch.io/dummy-sprite) | SilentShadow | — |
| [Far-East styled COINS](https://salgueiroazul.itch.io/far-east-styled-coins) | SalgueiroAzul | — |
| [Torch 32x32 Animated](https://rone3190.itch.io/torch-32x32-animated) | rone3190 | — |
| [Fantasy Rings Sprite Pack](https://pine-druid.itch.io/ring-sprites) | Pine Druid | — |
| [Pine Forest Parallax Background](https://lazyteastudios.itch.io/pine-forest-parallax-background) | LazyTeaStudios | — |
| [STYLED pixel art wood planks tileset](https://ipixl.itch.io/styled-pixel-art-wood-planks-tileset) | iPixl | — |
| [Free Bridges Pixel Art Assets](https://free-game-assets.itch.io/free-bridges-top-down-pixel-art-asset-pack) | Free Game Assets | [CraftPix licence](https://craftpix.net/file-licenses/) |

Where a licence column reads "—", the licence could not be confirmed from the creator's own page,
so none is stated rather than one being assumed.

### Audio

| Asset | Author | Licence |
|---|---|---|
| [Legendary JRPG Battle Music Pack](https://youfulca.itch.io/legendary-jrpg-battle-music-pack) | [YouFulca / Wingless Seraph](https://wingless-seraph.net/) | [Their terms of use](https://wingless-seraph.net/en/material-riyoukiyaku_eng.html) |
| [RPG Essentials SFX – Free!](https://leohpaz.itch.io/rpg-essentials-sfx-free) | Leohpaz | — |

### Fonts

| Font | Author | Licence |
|---|---|---|
| [EB Garamond](https://github.com/georgd/EB-Garamond) | © 2010–2013 Georg A. Duffner, © 2013 Siva Kalyan | [SIL OFL 1.1](https://openfontlicense.org/open-font-license-official-text/) |
| [Xarrovv](https://dimka.com/fonts/xarrovv) | © Dmitri Zdorov | [SIL OFL](https://openfontlicense.org/open-font-license-official-text/) |
| [m5x7](https://managore.itch.io/m5x7) | Daniel Linssen (managore) | [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/) |
| [DeArPix](https://fonttiengviet.com/font/font-pixel-tieng-viet-dearpix-1-94/) | © ygygfu 2026 | [SIL OFL](https://openfontlicense.org/open-font-license-official-text/) |

"EB Garamond" and "Xarrovv" are Reserved Font Names. None of the OFL fonts is modified by this
project.

### Design influences

*Hollow Knight* (Team Cherry) — map structure and connected-room layout ·
*Nine Sols* (Red Candle Games) — combat pacing ·
*Momodora* series (Bombservice) — movement feel and tone ·
*Dark Souls* series (FromSoftware) — checkpoint design and environmental storytelling.

---

## Developer notes

Setup documentation for the inventory, item and chest systems.

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

# Credits and Third-Party Notices

This game uses art, audio and fonts created by other people. Everyone whose work appears in
this build is credited below.

*Last reviewed: July 2026.*

---

## Disclaimer

**1. Nature of this work.** This project is a student assignment produced for academic
assessment. This build is not published, distributed to the public, sold, licensed,
advertised or monetised. Distribution is limited to submission for assessment and private
playtesting by a small number of people known to the authors.

**2. Third-party materials.** The authors are not the creators of the art, audio or font
assets in this build. All such materials remain the property of their respective copyright
holders. No ownership of any third-party asset is claimed, and no third-party asset is
presented as the authors' own work.

**3. Placeholder assets.** Some assets in this build are temporary placeholders used without
a licence from the rights holder. They are set out under *Copyright acknowledgement* below.
Their inclusion is not a claim of right and is not intended to deprive any rights holder of
revenue or recognition. They will be removed and replaced before any public or commercial
release.

**4. Good-faith effort.** The authors have tried to identify the licence governing each
asset. Where a licence could not be confirmed from the creator's own page, no licence is
stated rather than one being assumed. Nothing in this file is a legal opinion or a warranty
that the project is free of infringement.

**5. Restrictions observed.** Several of the licences below prohibit resale or redistribution
of the asset packs themselves, and several prohibit use in AI generation or AI training. No
asset pack is redistributed, resold or offered as a standalone download, and no art, audio
or text in this project was generated using AI.

**6. Corrections.** If any rights holder listed here objects to the use of their work in this
project, the authors will remove it on request.

---

## Copyright acknowledgement

> Momodora ©2010-2025 Guilherme Melo Martins

That is the copyright notice published by **[Bombservice](https://www.bombservice.com)**
(rdein), reproduced as it appears on their site. Momodora and all related characters,
artwork and assets are their property. Character, enemy, boss and NPC sprites in this build
were taken from
[*Momodora: Reverie Under the Moonlight*](https://www.spriters-resource.com/pc_computer/momodorareverieunderthemoonlight/)
and [*Momodora: Moonlit Farewell*](https://www.spriters-resource.com/pc_computer/momodoramoonlitfarewell/)
([boss sprites](https://www.spriters-resource.com/pc_computer/momodoramoonlitfarewell/asset/214812/)).

**This project is not affiliated with, authorised by, sponsored by, or endorsed by
Bombservice.** No licence to these sprites is held or claimed, and no ownership of them is
asserted. They were obtained from The Spriters Resource, a fan archive which is not the
rights holder and cannot grant a licence; its
[terms of use](https://www.spriters-resource.com/page/tou/) are understood to state that
hosted assets remain the property of the originating companies and are not for commercial
use.

The sprites are used as temporary placeholders during development of an unreleased academic
project, and will be replaced with original artwork before any public or commercial release.
They will be removed on request by the rights holder.

Bombservice publishes no fan-content policy, asset-usage guidance or attribution
requirement that we could find, so the wording of this acknowledgement is our own and does
not represent their stated wishes.

Please support the original games. They are excellent, and a direct influence on this
project.

---

## Attribution

### Art

["Kyrise's Free 16x16 RPG Icon Pack"](https://kyrise.itch.io/kyrises-free-16x16-rpg-icon-pack)
© 2018 by Kyrise is licensed under
[CC BY 4.0](https://creativecommons.org/licenses/by/4.0/).

["Slash hit 01 Animation VFX"](https://opengameart.org/content/slash-hit-01-animation-vfx) by
GustavoPlima is licensed under
[CC BY 3.0](http://creativecommons.org/licenses/by/3.0/legalcode).

["Monsters Creatures Fantasy"](https://luizmelo.itch.io/monsters-creatures-fantasy) by
LuizMelo is licensed under
[CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/).

["Stabby Spikes"](https://froggu999.itch.io/stabby-spikes) by froggu999 is licensed under
[CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/).

["Pixel Art Door Pack - ANIMATED"](https://karsiori.itch.io/pixel-art-door-pack-animated) by
KARSIORI STUDIO is licensed under CC0.

["Free Pixel Food!"](https://henrysoftware.itch.io/pixel-food) by Henry Software is licensed
under [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/).

["Fantasy Knight - Free Pixelart Animated Character"](https://aamatniekss.itch.io/fantasy-knight-free-pixelart-animated-character)
by aamatniekss.

["Free Pixelart Tileset - Cute Forest"](https://aamatniekss.itch.io/free-pixelart-tileset-cute-forest)
by aamatniekss.

["Simple Table and Chairs"](https://bradfrey.itch.io/simple-table-and-chairs) by Brad Frey.

["Fireball Pixel Art"](https://bontt.itch.io/fireball-pixel-art) by Bont.

["Wood Set"](https://nyknck.itch.io/wood-set) by @nyk_nck.

["FREE: Chest Animations"](https://admurin.itch.io/free-chest-animations) by
[Admurin](https://admurin.itch.io/).

["Medieval House Set - Statues"](https://odgardian.itch.io/medieval-house-set-statues) by
Odgardian.

["Training Dummy sprite"](https://silentshadow51.itch.io/dummy-sprite) by SilentShadow.

["Far-East styled COINS"](https://salgueiroazul.itch.io/far-east-styled-coins) by
SalgueiroAzul.

["Torch 32x32 Animated"](https://rone3190.itch.io/torch-32x32-animated) by rone3190.

["Fantasy Rings Sprite Pack"](https://pine-druid.itch.io/ring-sprites) by Pine Druid.

["Pine Forest Parallax Background"](https://lazyteastudios.itch.io/pine-forest-parallax-background)
by LazyTeaStudios.

["STYLED pixel art wood planks tileset"](https://ipixl.itch.io/styled-pixel-art-wood-planks-tileset)
by iPixl.

["Free Bridges Pixel Art Assets"](https://free-game-assets.itch.io/free-bridges-top-down-pixel-art-asset-pack)
by Free Game Assets ([CraftPix](https://craftpix.net/file-licenses/)).

["PLATFORMER/METROIDVANIA ASSET PACK"](https://o-lobster.itch.io/platformmetroidvania-pixel-art-asset-pack)
by [o_lobster](https://o-lobster.itch.io/) is licensed under
[CC BY 4.0](https://creativecommons.org/licenses/by/4.0/).

### Audio

Music: ["Legendary JRPG Battle Music Pack"](https://youfulca.itch.io/legendary-jrpg-battle-music-pack)
by [YouFulca / Wingless Seraph](https://wingless-seraph.net/), used under their
[terms of use](https://wingless-seraph.net/en/material-riyoukiyaku_eng.html).

Sound effects: ["RPG Essentials SFX - Free!"](https://leohpaz.itch.io/rpg-essentials-sfx-free)
by Leohpaz.

### Fonts

["EB Garamond"](https://github.com/georgd/EB-Garamond) by Georg Duffner is licensed under the
[SIL Open Font License, Version 1.1](https://openfontlicense.org/open-font-license-official-text/).

> Copyright 2010-2013, Georg A. Duffner, 2013 Siva Kalyan.
> Reserved Font Name: "EB Garamond".

["Xarrovv"](https://dimka.com/fonts/xarrovv) by Dmitri Zdorov is licensed under the
[SIL Open Font License](https://openfontlicense.org/open-font-license-official-text/).

> Copyright Dmitri Zdorov.

["m5x7"](https://managore.itch.io/m5x7) by Daniel Linssen (managore) is licensed under
[CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/).

["DeArPix"](https://fonttiengviet.com/font/font-pixel-tieng-viet-dearpix-1-94/) by ygygfu is
licensed under the
[SIL Open Font License](https://openfontlicense.org/open-font-license-official-text/).

> Copyright ygygfu 2026.

 The OFL fonts above are unmodified.

---

## Tools

Unity (Universal Render Pipeline, Input System, Cinemachine, TextMeshPro), Aseprite, Git.

## Inspired by

*Hollow Knight* by Team Cherry, *Nine Sols* by Red Candle Games, the *Momodora* series by
Bombservice, and the *Dark Souls* series by FromSoftware.
