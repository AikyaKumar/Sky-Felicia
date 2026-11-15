# Orbital Frontier — Expedition

Design a spacecraft and fly it from Earth departure to the Moon or Mars. Work through the flight plan, stage the launch vehicle from the helm, walk to the engineering bay to diagnose faults, capture an orbit, and complete a surface or orbital science mission. The crew can share the ship with up to four players.

The [player quickstart](PLAYER_QUICKSTART.md) walks through the first guided mission and its controls.

## Open the game

- **Solo** — Open `Orbital_Frontier_Expedition.html` in desktop Chrome or Edge. It includes the C# simulation, 3D art and datasets. It runs from disk with WebGL and needs no account or server.
- **Windows crew** — Extract the complete `Orbital_Frontier_Crew_Host_Windows.zip` folder. Run `OrbitalFrontier.CrewHost.exe` and keep the console open. On the host, open `http://localhost:47620/` in a browser. Other players open `http://HOST-LAN-IP:47620/`. Everyone selects **Crew LAN → Join shared crew room**. Join from the host browser first to claim seat 1, which launches and resets the mission. Allow TCP 47620 on your private LAN if prompted by Windows. Find the host address with `ipconfig`. All landings, repairs, and crew movement run on the shared host. The Windows build includes the .NET runtime.
- **Unity** — Open this `OrbitalFrontier` project folder with Unity Hub using **Unity 6.0 LTS (6000.0.62f1)**. Open `Assets/OrbitalFrontier/Scenes/Expedition.unity` and press Play. Build Windows (Direct3D 11), Linux, or Unity Web through **Orbital Frontier → Build** after installing build support for the chosen target. The native desktop LAN lobby uses TCP 47611. Join with the host’s local IP. The browser LAN host uses TCP 47620 and a separate lobby. Unity Web supports solo play. The original lunar reconnaissance mission remains in `MissionDesk.unity`. For a hosted web publish, it also remains under `archive/survey.html`.

## First expedition

1. Select **Moon** for Hadley–Apennine or **Mars** for Jezero. Start with **First-flight guide** or select **Surface science** and **Guided**. Choose a launch class, low or high orbit, transfer profile and planned operations window. The shipyard has three presets, 36 selectable components and 15 physical mount types. Click an installed part on the 3D model to inspect its mount. Try a larger battery, different power source, faster dish, alternative instruments or a rover. Resolve the flight-readiness holds before launch.
2. Walk forward to the **Helm**. Separate both stages after cutoff. At **Navigation**, select accelerated coast. If the power controller fails, walk to **Engineering**, run diagnostics and replace it. Return to **Navigation** to capture orbit.
3. Walk to **Science** and measure the landing corridor. From **Helm**, begin terminal descent. Guided handles thrust and lateral correction. Expert starts with manual throttle, translation and dampers. You can switch guidance during the approach. The map and controls show flight speed, drift, measured slope and the landing envelope.
4. Go to the **aft airlock** and begin EVA. Check the **Surface** map for sites A and B. Walk there or take an installed rover. Dismount to collect each field record within 10 m of the beacon. Return before suit oxygen is depleted. The rover remains where you park it.
5. Return through the airlock, then transmit at **Communications**. The mission report covers objectives, resource efficiency, touchdown, science and crew. **Orbital survey** flies two acquisition periods in orbit and skips descent. **Systems recovery** adds a leaking pressure seal and requires a working cabin and a surface measurement before downlink.

### Controls

| Control | Action |
|---|---|
| WASD or arrow keys | Move in first person or while checking the map |
| Click viewport, then mouse | Look around. Escape releases the pointer. The browser also supports drag-to-look. |
| E | Use a reachable station, airlock or field beacon |
| M / V | Toggle maps / cycle first person, exterior and map |
| R | Mount or dismount an installed rover within range |
| [ / ] during manual descent | Decrease / increase throttle by 4 percentage points |
| Shipyard drag / wheel | Rotate and zoom the 3D preview |

Reachable stations can also be opened through the right-hand directory. The cabin deck map tracks each crew member. Guidance from Navigation, Engineering, Life support, Science, Communications, and Helm applies to the same simulation. Crew assignments are flexible.

## Rebuild

The Unity project and both browser modes share the C# simulation in `Assets/OrbitalFrontier/Core`. The browser build uses .NET 8 WebAssembly and Three.js. The crew host uses ASP.NET Core and WebSockets. Build tools require Python 3 and .NET SDK 8. The tested WebAssembly compiler was SDK 8.0.408 with runtime 8.0.15.

```bash
dotnet workload install wasm-tools
dotnet publish Browser/OrbitalFrontier.Browser.csproj -c Release -o Builds/Browser
python3 Tools/pack_browser.py Builds/Browser/wwwroot Builds/Orbital_Frontier_Expedition.html

# Run a crew host on a machine with ASP.NET Core 8.
FRONTIER_WEB_ROOT="$PWD/Builds/Browser/wwwroot" dotnet run --project Tools/CrewHost -c Release
```

For Windows PowerShell, set `$env:FRONTIER_WEB_ROOT = (Resolve-Path Builds/Browser/wwwroot).Path` before running the host, or copy the generated `wwwroot` beside a self-contained publish. The ready-made Windows crew package already includes these files.

Read [terrain provenance and model limits](Docs/EXPEDITION_DATA.md) and [verification results](Docs/EXPEDITION_TESTING.md). Build requirements and hardware values are simulation parameters. Native Unity still needs Play Mode and standalone verification in an activated installation. The browser game can run and host a crew without Unity.
