# Custom Skeleton Merchant Shop (SKShop)

- Author: 百事#0
- This plugin replaces the **Skeleton Merchant**'s shop with a fully customizable store, allowing you to define items, prices, prefixes, and display conditions.

## Core Features
- **Fully customizable items**: Add, remove, or modify any item sold by the Skeleton Merchant. Set stack size, prefix, and price (copper/silver/gold/platinum).
- **Rich condition system**: Each item can have its own display conditions. Only items meeting the conditions will appear in the shop. Supported conditions include:
  - Boss defeats (King Slime, Eye of Cthulhu, Wall of Flesh, etc.)
  - Game progression (Hardmode, Plantera defeated, Golem defeated, Moon Lord defeated)
  - Time of day (Day, Night, Noon, Midnight)
  - Moon phases (Full, New, etc.)
  - Weather and events (Rain, Blood Moon, Solar Eclipse, Party, Sandstorm, Slime Rain, etc.)
  - Invasions (Goblin Army, Pirate Invasion, Frost Moon, etc.)
  - Player biome (Forest, Jungle, Desert, Hallow, Corruption, etc., over 20 types)
  - Player max health (<400 or ≥400)
- **Multiple shop profiles**: Configure multiple shop groups, each with enable/disable, allowed user groups, open/close chat messages, etc. Perfect for offering different items to different player ranks.
- **Auto-spawn**: Enable automatic spawning of the Skeleton Merchant when certain global conditions are met (e.g., a boss is defeated). The merchant will appear near town NPCs.
- **Manual spawn**: Admins can use `/skeleton <x> <y>` to spawn the merchant at any location. Players can also summon it by using a configured item (default: Fallen Star, ID 75) with right-click.
- **Respawn mechanism**: If the merchant is killed, it will respawn after a configurable delay at the last known location.
- **Dynamic refresh**: The shop contents can be refreshed at a configurable interval (in milliseconds) without requiring the player to reopen the shop.

## Commands
- `/skeleton <x> <y>` – Spawn a Skeleton Merchant at the given coordinates
- `/skeleton remove` – Remove the currently active Skeleton Merchant

## Configuration
The configuration file is located at `tshock/SKShop.json`  
A default configuration will be generated on first run. Refer to the comments inside the file for detailed explanation.

## Feedback & Support
- GitHub Issues: https://github.com/mcisfun517-prog/SKShop
- QQ Group: 1094232871