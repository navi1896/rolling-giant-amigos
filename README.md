# RollingGiant (Crew17)

A customized Rolling Giant package for Lethal Company V81, based on the MIT-licensed upstream Rolling Giant mod.

## Version 1.1.5

- Bug fixes and general stability improvements for Lethal Company V81.

## Version 1.0.1

- Guarantees one Rolling Giant inside the facility near the main entrance when the displayed in-game clock reaches 5:00 PM, once each active round.
- Keeps configured random spawns enabled; an earlier random Giant does not cancel the scheduled 5:00 PM appearance.
- If the interior Giant is already chasing a player who exits through a nearby paired entrance, the same networked Giant switches to the outdoor side and continues the chase. The transition uses the outdoor NavMesh near that exit.
- Includes the grayscale custom face asset from the previous package version.

The guaranteed appearance needs interior AI nodes on the level. The host runs the scheduled spawn. Every player should install and enable this package so the custom enemy and face are present on each client.

## Installation

Install through Thunderstore Mod Manager or r2modman; BepInEx will be installed automatically as a dependency. Disable or remove other Rolling Giant packages in the same profile to prevent duplicate plugins.

## Source and credits

Reviewable C# source and build metadata: https://github.com/navi1896/rolling-giant-amigos

Based on Rolling Giant by SoftDiamond and Andrew Burke (MIT License): https://github.com/TheSoftDiamond/lc-rolling-giant.
