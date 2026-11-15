# Verification and acceptance checklist

## Checks performed

| Check | Result and coverage |
|---|---|
| C# simulation and transport | Passed 40 assertions. The validation program computes circular orbital elements, checks one-revolution numerical drift and the rocket equation, confirms eclipse geometry, updates build requirements, compares terrain resolutions and completes the mission with both presets. It also tests powered subsystems, a rejected coarse-only downlink, and a bad burn ending in impact. |
| Local TCP crew | Passed at the transport layer over loopback. Three clients joined one host, a crew command reached the host, and all players received a complete state. A fifth participant was refused, a disconnect released the seat, and a client rejoined. Host-only deploy and reset were also checked. |
| Offline browser game | Passed in Chrome Headless Shell with the browser configured offline. Playwright opened the saved HTML from disk, rendered the Moon and assembly, checked archival imagery and the atlas, rotated and detached hardware, flew the correction, changed cockpit power, scanned all three sites, rendered 3D survey terrain, transmitted a precision recommendation and verified the scored debrief and replay. The run scored 96/100 and had no script errors or external requests. |
| Unity C# API compatibility | Passed with the compiler and reference assemblies from Unity 6000.0.62f1 for desktop, Unity Web conditional compilation, and editor build commands. |
| Full Unity import, Play Mode, Windows and Unity Web players | Pending. Unity Package Manager and licensing processes could not communicate through local IPC in the execution environment. Editor rendering, shader compilation, Unity JSON serialization across running desktop clients, and packaged Unity players still require verification in an activated editor. |

Run the engine and transport verification with:

```bash
dotnet run --project Tools/Validation/Validation.csproj -c Release
```

## Native Unity acceptance

1. Import with the editor version listed in the project. Open `MissionDesk`, enter Play Mode and check that the moon texture, heritage photographs, fonts and 3D spacecraft render.
2. In the hangar, inspect a mount and swap parts. Rotate solar arrays and confirm lower sunlight output. Detach an engine and confirm deployment is locked. Restore it, then deploy.
3. Fly through the five cockpit stations. Qualify the orbit and test a second run with a wrong burn. Compare coarse and precision maps, check the 3D terrain view, and verify a precision-required downlink and debrief.
4. Run two desktop builds with host and client on loopback. Repeat across two machines on a private LAN with three clients when available. Compare mission time, propulsion state, scan products and score. Disconnect a client during flight and confirm the host can continue. Rejoin and check the current telemetry.
5. Run the Windows player alongside an open mission and verify input and window resizing. Build Unity Web, serve its folder over HTTP, and run the solo mission in a WebGL 2 browser.
