# Expedition verification

| What was exercised | Result |
|---|---|
| Shared C# simulation | 55 checks passed. All three preset ships flew ascent, cruise, repair, orbital capture, terminal landing, EVA and downlink in the model. The test also completed orbital survey and cabin systems recovery, checked sustained pressure loss and seal replacement, verified parked-rover persistence, and reproduced an expert hard landing when the throttle was left off. |
| Original reconnaissance game | The 40 existing validation assertions passed. The Unity project retains the original survey scene and its tutorial. |
| Offline browser | Passed both surface missions through Earth acknowledgment with the browser offline. The Moon test changed the launcher and mission window, edited a mount, staged, repaired the controller, captured orbit, landed, used the airlock, drove to both science beacons, returned, and transmitted. The Mars test also exercised the atmospheric transition, then visited both Jezero science sites on foot. A separate test clicked a 3D part and started the in-game first-flight guide. |
| Browser crew host | Two Chromium pages and two further WebSocket connections occupied all four seats. The clients shared design edits and launch, displayed the same mission phase, moved independently, and released a disconnected seat. The host rejected a fifth connection. This integration test ran under Linux. |
| Windows crew host package | Self-contained Windows x64 publish completed and the archive includes the executable, its .NET runtime and the browser client. The Windows executable was not run in this Linux workspace. |
| Native Unity | Runtime, editor, desktop and Web conditional C# compilation passed with Unity 6000.0.62f1 API assemblies. Full native Play Mode, native rendering, and the native multiplayer/build smoke test still need an activated Unity installation. |

## Reproduce simulation checks

Run from the project folder:

```bash
dotnet run --project Tools/ExpeditionValidation -c Release
dotnet run --project Tools/Validation -c Release
```

Use the **First-flight guide** in-game for the interactive Moon walkthrough. To verify the Mars setup, choose Jezero and the default Selene ship, then hit the launch readiness gate with Peregrine medium selected. Switch back to Meridian heavy and fly on Guided. At Science, confirm 20 m source cells. After touchdown, verify each field record and return with suit oxygen remaining. In Expert, compare the touchdown and failure debriefs using manual throttle.

For an in-person crew check, open the same host address from two computers on the same LAN. Verify that the pilot operates the Helm while another player repairs the power controller, that the pilot can see the other avatar walking, and that the airlock and field records are shared as crew members move between ship and surface.
