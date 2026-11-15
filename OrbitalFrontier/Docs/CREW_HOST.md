# Windows crew host

This build targets Windows 10/11 on x64.

1. Extract the entire `Orbital_Frontier_Crew_Host_Windows.zip` folder.
2. Run `OrbitalFrontier.CrewHost.exe`. Keep the console open. Allow TCP 47620 for the private LAN if Windows prompts.
3. The host opens `http://localhost:47620/` in desktop Chrome or Edge and selects **Crew LAN → Join shared crew room** first. Other players use `http://HOST-LAN-IP:47620/`. Run `ipconfig` on the host and use its active network adapter’s IPv4 address.
4. Up to four people share the same mission. Seat 1 commits to launch and can reset the mission. Each crew member walks separately, can take any nearby station, and retains individual health, oxygen, suit charge and EVA position while connected.
5. Close the console to shut down the room. The room keeps the mission in memory. A disconnected player can rejoin through the same browser tab. The host allows a new player to fill a freed seat.

The .NET runtime and browser assets are included. Players need only a browser. Game content and telemetry remain on the host’s local network. The crew host and native Unity lobby use different protocols. Use the browser host package for a mixed Windows/Linux group, or use native Unity builds for the entire group.

To play offline without a crew, open the separately delivered `Orbital_Frontier_Expedition.html`.
