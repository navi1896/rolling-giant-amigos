# RollingGiant (Crew17)

A customized Rolling Giant package for Lethal Company V81, based on the MIT-licensed upstream Rolling Giant mod and including native compatibility enhancements for **ControlCompany**.

## Version 1.1.0

- Automatically installs and integrates with **ControlCompany (`ControlCompany-ControlCompany-1.3.3`)** via a built-in compatibility bridge, allowing the host to enter Ghost Mode (`F1`), cycle spectated players (`O` / `P`), toggle the UI (`U`), and control monsters (`F1`–`F12`).
- Fixes ControlCompany 1.3.3's Ghost Mode / Monster Mode top-right UI list rendering issue on **Lethal Company V81**.
- Adds full direct player control for the **Rolling Giant** (`F1` in Ghost Mode, both indoors and outdoors), including synced rolling audio, strangulation collision attack, manual stop-sound trigger (`LMB`), force-move while watched (`Hold RMB`), and toggling between manual and AI control (`F1`).
- Automatically enables controlling monsters added by **other enemy mods** alongside all base-game monsters directly from the Ghost Mode spawn list (`F1`–`F12`).
- Lets the Rolling Giant both **exit and re-enter** the facility through paired entrance doors (automatically when chasing a player who goes back inside, or manually by pressing `E` at a door while controlling the Giant).
- Syncs Extended Mode (`0` + `8`) group credit boosts across multiplayer clients.

## Version 1.0.1

- Guarantees one Rolling Giant inside the facility near the main entrance when the displayed in-game clock reaches 5:00 PM, once each active round.
- Keeps configured random spawns enabled; an earlier random Giant does not cancel the scheduled 5:00 PM appearance.
- If the interior Giant is already chasing a player who exits through a nearby paired entrance, the same networked Giant switches to the outdoor side and continues the chase. The transition uses the outdoor NavMesh near that exit.
- Includes the grayscale custom face asset from the previous package version.

The guaranteed appearance needs interior AI nodes on the level. The host runs the scheduled spawn. Every player should install and enable this package so the custom enemy and face are present on each client.

## Installation

Install through Thunderstore Mod Manager or r2modman; BepInEx and ControlCompany will be installed automatically as dependencies. Disable or remove other Rolling Giant packages in the same profile to prevent duplicate plugins.

## Source and credits

Reviewable C# source and build metadata: https://github.com/navi1896/rolling-giant-amigos

Based on Rolling Giant by SoftDiamond and Andrew Burke (MIT License): https://github.com/TheSoftDiamond/lc-rolling-giant.
Integrates via Harmony compatibility patches with ControlCompany: https://thunderstore.io/c/lethal-company/p/ControlCompany/ControlCompany/.
