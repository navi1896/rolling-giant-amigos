# Crew17 — Rolling Giant + Custom ControlCompany build

Public source snapshot for the Crew17 Lethal Company package (`v1.0.2`).

## Upstream

This build is based on Rolling Giant by Andrew Burke ([TheSoftDiamond/lc-rolling-giant](https://github.com/TheSoftDiamond/lc-rolling-giant)) and includes a custom integrated version of [ControlCompany](https://github.com/ControlCompany/ControlCompany) (`ControlCompany (Crew17 Edition)`). The upstream license and acknowledgements are retained in `source/README.md` and `source/LICENSE.md`.

## Changes in this build (`1.0.2` / Assembly `2.6.3.2`)

- **Integrated Custom ControlCompany (`Crew17.ControlCompany`)**: Includes a built-in custom edition of ControlCompany directly in the mod so the host can enter Ghost Mode (`F1`), cycle spectated players (`O` / `P`), toggle the UI (`U`), and control monsters (`F1`–`F12`) without installing a separate mod.
- **Direct Rolling Giant Possession (`RollingGiantEnemyController`)**:
  - The Rolling Giant is always available in the `F1` slot in Ghost Mode on every moon (both indoors and outdoors).
  - Full rolling audio and collision attack (strangulation) are synced across all clients.
  - **Left Click (LMB)**: Play the Rolling Giant stop/scare sound on demand.
  - **Right Click (Hold RMB)**: Force move even while players are looking at the Giant.
  - **Interact (`E`)**: Transition from inside the facility to outside through a nearby entrance door.
  - **`F1`**: Toggle between manual player control and automatic AI control.
- **Retained from `1.0.1`**:
  - The default-on `SpawnGuaranteedAtFivePm` setting adds one interior Rolling Giant near the main entrance when the displayed in-game clock reaches 5:00 PM on each round with interior AI nodes.
  - Configured random spawn chances remain enabled.
  - When an AI-controlled Giant is chasing a player who exits through a nearby paired facility entrance, it switches to the outdoor side and continues the chase.

The relevant changes are in `source/ControlCompanyMod/`, `source/RollingGiantAI.cs`, `source/Plugin.cs`, `source/Patches/GuaranteedSpawnPatch.cs`, `source/Patches/EnemyPatches.cs`, and `source/Settings/CustomConfig.cs`.

## Build and verification

The source compiles against the Lethal Company V81 game libraries and the Netcode patcher.

## Source scope

The `source/` folder contains the mod's C# source and build metadata for review. This source-only mirror excludes compiled DLLs, the Unity asset bundle, game files, and decompiled game sources.
