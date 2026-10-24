# Crew17 — Rolling Giant custom build

Public source snapshot for the Crew17 Lethal Company package.

## Upstream

This build is based on Rolling Giant by Andrew Burke. The source base is [TheSoftDiamond/lc-rolling-giant](https://github.com/TheSoftDiamond/lc-rolling-giant); the upstream license and acknowledgements are retained in `source/README.md` and `source/LICENSE.md`.

## Changes in this build

- The default-on `SpawnGuaranteedAtFivePm` setting adds one interior Rolling Giant near the main entrance when the displayed in-game clock reaches 5:00 PM on each round with interior AI nodes.
- Configured random spawn chances remain enabled. A random appearance before 5:00 PM does not cancel the scheduled appearance.
- When the same Giant is chasing a player who exits through a nearby paired facility entrance, it switches to the outdoor side and continues the chase.
- The host runs the scheduled spawn; all players should use the same package so the enemy prefab and grayscale face asset are available on every client.
- The plugin assembly version is `2.6.3.1`; the Thunderstore package version is `1.0.1`.

The relevant changes are in `source/Patches/GuaranteedSpawnPatch.cs`, `source/Patches/EnemyPatches.cs`, `source/Settings/CustomConfig.cs`, and `source/RollingGiantAI.cs`.

## Build and verification

The source compiles against the Lethal Company V81 game libraries and the Netcode patcher. A live multiplayer gameplay check is still required to verify the 5:00 PM appearance and doorway pursuit in-game.

## Source scope

The `source/` folder contains the mod's C# source and build metadata for review. This source-only mirror excludes compiled DLLs, the Unity asset bundle, game files, and decompiled game sources. It is a reviewable source snapshot, not the installable release archive.
