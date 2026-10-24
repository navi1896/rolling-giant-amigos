# Crew17 — Rolling Giant custom build

Public source snapshot for the Crew17 Lethal Company package.

## Upstream

This build is based on Rolling Giant by Andrew Burke. The source base used here is [TheSoftDiamond/lc-rolling-giant](https://github.com/TheSoftDiamond/lc-rolling-giant). The upstream README and license are retained in `source/README.md` and `source/LICENSE.md`.

## Changes in this build

- Adds a default-on `SpawnGuaranteedAtFivePm` setting that schedules one exterior Rolling Giant at 5:00 PM in-game on each active moon round. The Company level is excluded.
- Runs the scheduled spawn on the server and network-spawns the enemy for players running the mod.
- Suppresses the mod's random enemy spawn-pool entries while the scheduled-spawn setting is enabled.
- Updates the embedded plugin version to `2.6.3.1`.

The relevant changes are in `source/Patches/GuaranteedSpawnPatch.cs`, `source/Patches/EnemyPatches.cs`, `source/Settings/CustomConfig.cs`, and `source/Plugin.cs`.

## Source scope

The `source/` folder contains C# source and build metadata for review. This source-only mirror excludes compiled DLLs, the Unity asset bundle, game files, and the decompiled game sources. The mod loads its Unity assets from the packaged asset bundle, so this repository is a reviewable source snapshot rather than a complete release archive.
