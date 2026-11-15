using OrbitalFrontier.Core;
using UnityEngine;
using static OrbitalFrontier.Runtime.FrontierConsole;

namespace OrbitalFrontier.Runtime
{
    public sealed partial class FrontierGame
    {
        void DrawOverlay(float w, float h)
        {
            ui.Fill(new Rect(0, 0, w, h), new Color(.015f,.017f,.032f,.85f));
            float width = overlay == "tutorial" ? 660 : 592;
            float height = overlay == "tutorial" ? 530 : overlay == "atlas" || overlay == "brief" ? 430 : overlay == "log" ? 465 : 300;
            var box = new Rect((w - width) / 2, (h - height) / 2, width, height);
            ui.Card(box);
            string title = overlay switch {
                "atlas" => "Planetary atlas", "brief" => "Tranquillity Survey", "crew" => "LAN crew",
                "controls" => "Controls and settings", "log" => "Mission log", "tutorial" => "First flight guide", _ => "End the current survey?"
            };
            ui.Title(box, title);
            if (ui.Button(new Rect(box.xMax - 45, box.y + 7, 33, 28), "×", size: 17))
            {
                overlay = null;
                return;
            }
            float x = box.x + 17, y = box.y + 55, span = box.width - 34;
            switch (overlay)
            {
                case "tutorial":
                    DrawTutorial(x, y, span);
                    break;
                case "atlas":
                    DrawAtlas(x, y, span);
                    break;
                case "brief":
                    DrawBrief(x, y, span);
                    break;
                case "crew":
                    DrawCrew(x, y, span);
                    break;
                case "log":
                    string[] lines = View.flight.log;
                    for (int i = 0; i < lines.Length; i++)
                        ui.Text(new Rect(x, y + 32 * i, span, 29), lines[i], 12, Muted, false, true);
                    break;
                case "home":
                case "hangar":
                    ui.Text(new Rect(x, y, span, 50), "Returning to the mission desk or hangar ends the current flight.", 18, Muted, false, false, TextAnchor.MiddleLeft, true);
                    if (ui.Button(new Rect(x, y + 69, 136, 37), "End survey", true))
                    {
                        string destination = overlay;
                        overlay = null;
                        Send(destination == "home" ? Command.Home : Command.OpenHangar);
                    }
                    if (ui.Button(new Rect(x + 143, y + 69, 136, 37), "Continue flight")) overlay = null;
                    break;
                default:
                    DrawControls(x, y, span);
                    break;
            }
        }

        void DrawBrief(float x, float y, float w)
        {
            ui.Text(new Rect(x, y, w, 45), "Your spacecraft is in a 23 × 180 km lunar orbit. Prepare a survey orbit and use laser altimetry to compare three candidate regions.",
                17, Muted, false, false, TextAnchor.UpperLeft, true);
            string[] titles = { "01  Hangar", "02  Trajectory planning", "03  Laser altimetry", "04  Earth downlink" };
            string[] notes = {
                "The Aster preset is ready to deploy. Swap or rotate modules to see the engineering trade-offs.",
                "Execute the suggested correction, or enter a burn. Qualify with perilune between 46 and 92 km.",
                "Coast to access and scan each target. Compare slopes and acquire a precision map of your choice.",
                "With all three sites mapped, transmit your recommendation when Earth is visible."
            };
            for (int i = 0; i < titles.Length; i++)
            {
                float yy = y + 56 + i * 54;
                ui.Text(new Rect(x, yy, w, 19), titles[i], 15, Ink, true);
                ui.Text(new Rect(x, yy + 21, w, 26), notes[i], 14, Muted, false, false, TextAnchor.UpperLeft, true);
            }
            ui.Rule(x, y + 284, w);
            ui.Text(new Rect(x, y + 294, w, 48), "Precision scans use NASA LRO/LOLA data at 16 samples per degree. The mission compares broad terrain; hazards at the scale of a single lander are below the dataset resolution.",
                13, Muted, false, false, TextAnchor.UpperLeft, true);
            if (ui.Button(new Rect(x, y + 343, 161, 27), "NASA CGI Moon Kit", size: 12))
                Application.OpenURL("https://svs.gsfc.nasa.gov/4720/");
        }

        void DrawAtlas(float x, float y, float w)
        {
            for (int i = 0; i < catalog.bodies.Length; i++)
                if (ui.Button(new Rect(x + 104 * i, y, 98, 31), catalog.bodies[i].name, selected: atlas == i))
                    atlas = i;
            var body = catalog.bodies[atlas];
            Pair(x, y + 43, w, "MEAN RADIUS", N(body.radius), "km", "MASS", body.mass, "");
            Pair(x, y + 91, w, "SURFACE GRAVITY", N(body.gravity, "0.00"), "m/s²", "ESCAPE SPEED", N(body.escape, "0.00"), "km/s");
            Pair(x, y + 139, w, "SIDEREAL ROTATION", body.rotation, "", "ORBITAL PERIOD", body.period, "");
            ui.Reading(x, y + 195, w, "Solar distance", body.distance);
            ui.Text(new Rect(x, y + 222, w, 28), body.air + "  ·  " + body.temperature, 13, Muted, false, false, TextAnchor.UpperLeft, true);
            ui.Text(new Rect(x, y + 268, w, 45), body.notes, 15, Ink, false, false, TextAnchor.UpperLeft, true);
            if (ui.Button(new Rect(x, y + 326, 144, 28), "NASA fact sheet", size: 12)) Application.OpenURL(body.source);
        }

        void DrawControls(float x, float y, float w)
        {
            ui.Text(new Rect(x, y, w, 88),
                "Drag and scroll in the 3D viewport to orbit and zoom. Choose Orbit or Chase in flight. Capture a lidar scan to unlock Terrain.\n1–5 switch cockpit stations. Space toggles time pause. X aborts active operations.",
                17, Muted, false, false, TextAnchor.UpperLeft, true);
            if (ui.Button(new Rect(x, y + 97, 151, 31), motion ? "Motion: enabled" : "Motion: reduced"))
                motion = !motion;
            if (View.screen <= 1 && !HasHost)
            {
                if (ui.Button(new Rect(x, y + 137, 151, 31), "Guided setup", selected: View.assisted)) SetAssist(true);
                if (ui.Button(new Rect(x + 158, y + 137, 151, 31), "Manual setup", selected: !View.assisted)) SetAssist(false);
            }
            if (ui.Button(new Rect(x, y + 181, 151, 31), "Crew LAN")) Open("crew");
            if (CrewOnline) ui.Text(new Rect(x + 160, y + 181, w - 162, 32), NetStatus, 11, Mint, false, true);
        }

        void SetAssist(bool assisted)
        {
            if (View.screen == 0) Send(Command.OpenHangar);
            Send(Command.Assistance, assisted ? 1 : 0);
        }

        void DrawCrew(float x, float y, float w)
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            ui.Text(new Rect(x, y, w, 55), "Create one shared spacecraft for up to four players on the same local network. The host advances the simulation. Crew can switch stations freely.",
                15, Muted, false, false, TextAnchor.UpperLeft, true);
            if (network.Hosting || network.Joined || network.Joining)
            {
                ui.Text(new Rect(x, y + 64, w, 27), network.Joining ? "CONNECTING…" : NetStatus, 13, Mint, false, true);
                ui.Text(new Rect(x, y + 98, w, 32), network.Hosting ? "Crews join using the host’s local IPv4 address on TCP " + ListenPort + "." :
                    "On reconnect, the host sends its current hangar or mission state.", 14, Muted);
                if (ui.Button(new Rect(x, y + 166, 156, 34), network.Hosting ? "Close LAN room" : "Leave room"))
                {
                    network.Leave();
                    remote = null;
                    lastScreen = -1;
                }
            }
            else
            {
                if (ui.Button(new Rect(x, y + 62, 156, 34), "Host LAN session", true))
                    network.StartHost(ListenPort);
                ui.Text(new Rect(x, y + 108, w, 17), "HOST IP ADDRESS", 10, Subtle, false, true);
                joinAddress = ui.Field(new Rect(x, y + 129, 229, 32), "lanAddress", joinAddress);
                if (ui.Button(new Rect(x + 237, y + 129, 115, 32), "Join crew", enabled: !string.IsNullOrWhiteSpace(joinAddress)))
                {
                    awaitingFirst = true;
                    network.StartJoin(joinAddress.Trim(), ListenPort);
                }
                ui.Text(new Rect(x, y + 176, w, 51), network.Error ?? ("Use 127.0.0.1 for a second desktop instance. Default TCP port " + ListenPort + "."),
                    13, network.Error == null ? Muted : Amber, false, false, TextAnchor.UpperLeft, true);
            }
#else
            ui.Text(new Rect(x, y, w, 54), "LAN crew sessions are available in the desktop build. Every station is available to one pilot in this build.",
                17, Muted, false, false, TextAnchor.UpperLeft, true);
#endif
        }
    }
}
