# ORBITAL FRONTIER — PLAYER QUICKSTART

**Game scope:** This is a playable *lunar orbit reconnaissance prototype*, not yet the full space sandbox (no Earth liftoff, interplanetary transfers, landing or roaming on planetary surfaces).

## First run — recommended route

1. Open `Assets/OrbitalFrontier/Scenes/MissionDesk.unity` and press Play. The **First flight guide** now appears on first run. Reopen it using the **HOW TO** button on the top bar.
2. Choose **Enter the hangar**. Leave **Aster Surveyor** selected for your first run. You can click each of the seven mounts to change components. Watch the Engineering panel: mass, delta-v, available power, cost and **FLIGHT READY**. If you accidentally remove required components, restore the preset.
3. Click **Deploy into lunar orbit**. The game starts in a 23 km x 180 km elliptical lunar orbit. It does *not* simulate launch from Earth yet.
4. Select the **Nav** station (`1`). Click **Suggest correction** and then **Execute burn**. Let the burn finish. The objective is a qualified orbit with **perilune (lowest altitude) between 46 and 92 km**. If you alter the plan manually, watch *Predicted perilune* before execution.
5. Select **Lidar** (`4`). Choose the first site, **Coarse 8x8**, then **Coast to access** if needed; when in window click **Start scan**. Repeat for all three sites. Compare each site's **90th-percentile slope** — a lower slope tends to be a better landing candidate. Return to your preferred site and perform a **Precision 24x24** scan.
6. Select **Comms** (`5`). Choose the site with your Precision map. Make sure **Earth visible** is shown, the **Radio bus** is online (Power tab `3`), and the engine is idle. Click **Transmit recommendation**. Finish the downlink and review your result out of 100.

## Controls

| Input | Action |
|---|---|
| `1` | Navigation station |
| `2` | Flight station |
| `3` | Power and thermal station |
| `4` | LiDAR station |
| `5` | Communications station |
| `Space` | Pause/unpause time (outside input fields) |
| `X` | Abort active operation |
| Drag viewport | Rotate camera around spacecraft/scene |
| Mouse wheel over viewport | Zoom |
| Orbit, Chase, Terrain | Switch cameras; Terrain requires survey result |
| ×1 / ×5 / ×25 / ×160 | Time compression, upper-right in cockpit |

**Common problems:**
- **Deploy button disabled:** Your ship is missing a required component, has insufficient engineering performance, or exceeds mission limits; inspect right-side Engineering messages.
- **Start scan disabled:** Qualify orbit first, select a site, use Coast to access, keep the LiDAR bus powered, stop thrust and other active operations.
- **Transmit disabled:** Scan all three sites, obtain a Precision result for the selected site, wait for Earth visibility, power the Radio bus and stop engine thrust.
- **The scene is empty:** Open **MissionDesk.unity**, not Unity's default Untitled scene.

**Multiplayer:** The Unity desktop build includes a basic host-authoritative LAN crew prototype; it is not internet matchmaking. In-game **Crew LAN** provides host/join controls, with TCP port 47610.

## Developer notes
- This guide was added as a new Unity runtime partial class, `Assets/OrbitalFrontier/Runtime/FrontierGame.Tutorial.cs`.
- Existing visuals, physics, mission data and scoring remain unchanged.
- The package manifest includes valid Unity audio/physics modules and excludes invalid `com.unity.modules.inputlegacy`.
- This package's C# changes were reviewed structurally but **Unity compilation and runtime were not executed** in the generation environment. If Unity reports errors, collect the first red Console message.
