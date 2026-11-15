using System;
using OrbitalFrontier.Core;
using UnityEngine;
using static OrbitalFrontier.Runtime.FrontierConsole;

namespace OrbitalFrontier.Runtime
{
    public sealed partial class FrontierGame
    {
        readonly string[] tabNames = { "Nav", "Flight", "Power", "Lidar", "Comms" };
        readonly string[] mountCodes = { "CB", "PT", "ME", "SA", "BT", "LA", "RF" };

        void OnGUI()
        {
            if (session == null)
            {
                if (dataError != null) GUI.Label(new Rect(18, 18, 620, 45), dataError);
                return;
            }
            float w = CanvasWidth, h = CanvasHeight;
            var matrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(Scale, Scale, 1));
            HandleConsoleKeys();
            ui.ResetTooltip();
            bool available = overlay == null;
            bool enabled = GUI.enabled;
            GUI.enabled = enabled && available;
            if (View.screen == 1 || View.screen == 2)
            {
                ui.Fill(new Rect(0, 60, w, viewport.y - 60), Bg);
                ui.Fill(new Rect(0, viewport.y, viewport.x, viewport.height), Bg);
                ui.Fill(new Rect(viewport.xMax, viewport.y, w - viewport.xMax, viewport.height), Bg);
                ui.Fill(new Rect(0, viewport.yMax, w, h - viewport.yMax), Bg);
            }
            DrawTop(w);
            switch (View.screen)
            {
                case 0: Menu(w, h); break;
                case 1: Hangar(w, h); break;
                case 2: Cockpit(w, h); break;
                case 3: Report(w, h); break;
            }
            if (!string.IsNullOrEmpty(ui.Tooltip))
            {
                Vector2 at = Event.current.mousePosition;
                var tip = new Rect(Mathf.Min(w - 315, at.x + 14), Mathf.Min(h - 47, at.y + 13), 301, 35);
                ui.Card(tip);
                ui.Text(new Rect(tip.x + 8, tip.y, tip.width - 16, tip.height), ui.Tooltip, 12, Muted, false, false, TextAnchor.MiddleLeft, true);
            }
            GUI.enabled = enabled;
            if (overlay != null) DrawOverlay(w, h);
            GUI.matrix = matrix;
        }

        void HandleConsoleKeys()
        {
            var e = Event.current;
            if (e.type != EventType.KeyDown || overlay != null || View.screen != 2 ||
                GUI.GetNameOfFocusedControl().StartsWith("plan", StringComparison.Ordinal)) return;
            switch (e.keyCode)
            {
                case KeyCode.Space: Send(Command.Warp, View.flight.warp == 0 ? 1 : 0); break;
                case KeyCode.X: Send(Command.Stop); break;
                case KeyCode.Alpha1: SwitchPanel(0); break;
                case KeyCode.Alpha2: SwitchPanel(1); break;
                case KeyCode.Alpha3: SwitchPanel(2); break;
                case KeyCode.Alpha4: SwitchPanel(3); break;
                case KeyCode.Alpha5: SwitchPanel(4); break;
                default: return;
            }
            e.Use();
        }

        void DrawTop(float w)
        {
            ui.Fill(new Rect(0, 0, w, 60), new Color(.046f, .052f, .083f));
            ui.Rule(0, 59, w);
            ui.Text(new Rect(21, 8, 198, 24), "ORBITAL FRONTIER", 20, Ink, true);
            ui.Text(new Rect(22, 31, 180, 17), "FLIGHT OPERATIONS", 9, Accent, false, true);
            float x = w * .50f - 176;
            int screen = View.screen;
            if (ui.Button(new Rect(x, 15, 89, 30), "Mission", selected: screen == 0, enabled: !HasHost))
            {
                if (screen == 2) Open("home");
                else Send(Command.Home);
            }
            if (ui.Button(new Rect(x + 95, 15, 88, 30), "Hangar", selected: screen == 1, enabled: !HasHost))
            {
                if (screen == 2) Open("hangar");
                else Send(Command.OpenHangar);
            }
            if (ui.Button(new Rect(x + 189, 15, 69, 30), "Atlas")) Open("atlas");
            if (ui.Button(new Rect(x + 264, 15, 77, 30), "Controls")) Open("controls");
            if (ui.Button(new Rect(x + 347, 15, 75, 30), "HOW TO", true))
            {
                tutorialPage = 0;
                Open("tutorial");
            }
            ui.Text(new Rect(w - 258, 15, 235, 30), NetStatus, 10, Mint, false, true, TextAnchor.MiddleRight);
        }

        void Menu(float w, float h)
        {
            float x = Math.Max(33, w * .075f), top = 60 + (h - 60) * .14f;
            ui.Fill(new Rect(0, 60, 27, h - 60), new Color(.026f, .029f, .055f));
            ui.Fill(new Rect(x, top + 9, 16, 1), Accent);
            ui.Text(new Rect(x + 26, top, 450, 20), "LUNAR OPERATIONS  /  OF–01", 12, Accent, false, true);
            ui.Text(new Rect(x - 6, top + 23, 660, 147), "Orbital", 119, Ink, true);
            ui.Text(new Rect(x - 6, top + 118, 675, 147), "Frontier.", 119, Accent, true);
            ui.Text(new Rect(x, top + 269, 383, 56),
                "Engineer a spacecraft. Correct its orbit. Map the Moon and recommend a landing site.",
                21, Muted, false, false, TextAnchor.MiddleLeft, true);
            if (ui.Button(new Rect(x, top + 346, 173, 42), "Enter the hangar", true, enabled: !HasHost))
                Send(Command.OpenHangar);
            if (ui.Button(new Rect(x + 184, top + 346, 123, 42), "Mission brief")) Open("brief");
            if (ui.Button(new Rect(x + 319, top + 346, 106, 42), "Crew LAN", enabled: true)) Open("crew");
            var bottom = new Rect(x, h - 91, w - x * 2, 65);
            ui.Card(bottom);
            ui.Text(new Rect(x + 15, bottom.y + 6, 320, 22), "PLAYABLE MISSION", 10, Subtle, false, true);
            ui.Text(new Rect(x + 15, bottom.y + 27, 320, 27), "Tranquillity Survey", 22, Ink, true);
            ui.Text(new Rect(x + bottom.width * .47f, bottom.y + 6, 290, 22), "LUNAR DATA", 10, Subtle, false, true);
            ui.Text(new Rect(x + bottom.width * .47f, bottom.y + 27, 290, 27), "NASA LRO / LOLA", 19, Ink, true);
            if (PlayerPrefs.HasKey("OrbitalFrontier.LastSurveyScore"))
                ui.Text(new Rect(x + bottom.width * .70f, bottom.y + 14, 143, 38), "LAST " + PlayerPrefs.GetInt("OrbitalFrontier.LastSurveyScore") + "/100", 10, Mint, false, true);
            if (ui.Button(new Rect(bottom.xMax - 139, bottom.y + 15, 127, 33), "Planetary atlas")) Open("atlas");
        }

        void Section(string tag, string title)
        {
            ui.Fill(new Rect(15, 81, 14, 1), Accent);
            ui.Text(new Rect(38, 70, 470, 24), tag, 10, Accent, false, true);
            ui.Text(new Rect(14, 92, 602, 49), title, 39, Ink, true);
        }

        void Frame(Rect rect)
        {
            ui.Fill(new Rect(rect.x - 1, rect.y - 1, rect.width + 2, 1), Border);
            ui.Fill(new Rect(rect.x - 1, rect.yMax, rect.width + 2, 1), Border);
            ui.Fill(new Rect(rect.x - 1, rect.y, 1, rect.height), Border);
            ui.Fill(new Rect(rect.xMax, rect.y, 1, rect.height), Border);
        }

        void Hangar(float w, float h)
        {
            Section("SPACECRAFT DESIGN", "Assembly hangar");
            if (ui.Button(new Rect(w - 360, 96, 109, 32), "Mission brief")) Open("brief");
            if (ui.Button(new Rect(w - 240, 91, 222, 42), "Deploy into lunar orbit", true, enabled: View.stats.ready && !HasHost)) Send(Command.Deploy);
            var left = new Rect(14, viewport.y, 222, viewport.height);
            var right = new Rect(viewport.xMax + 12, viewport.y, w - viewport.xMax - 27, viewport.height);
            ui.Card(left);
            ui.Title(left, "Workshop", "7 MOUNTS");
            for (int i = 0; i < catalog.presets.Length; i++)
            {
                var preset = catalog.presets[i];
                if (ui.Button(new Rect(left.x + 8 + i * 104, left.y + 54, 100, 34), preset.name,
                    selected: View.design.preset == i, size: 12))
                {
                    inspected = -1;
                    Send(Command.LoadPreset, i);
                }
            }

            int hover = -1;
            for (int i = 0; i < mountCodes.Length; i++)
            {
                var r = new Rect(left.x + 8, left.y + 97 + i * 52, left.width - 16, 47);
                var p = catalog.FindPart(View.design.parts[i]);
                if (ui.Button(r, "", selected: inspected == i))
                {
                    inspected = inspected == i ? -1 : i;
                }
                if (r.Contains(Event.current.mousePosition)) hover = i;
                ui.Fill(new Rect(r.x + 8, r.y + 11, 23, 24), Border);
                ui.Text(new Rect(r.x + 8, r.y + 11, 23, 24), mountCodes[i], 9, Accent, false, true, TextAnchor.MiddleCenter);
                ui.Text(new Rect(r.x + 38, r.y + 5, r.width - 41, 20), Slot.Names[i], 14, Ink, true);
                ui.Text(new Rect(r.x + 38, r.y + 25, r.width - 42, 13), p?.name ?? "Mount empty", 9, Muted, false, true);
            }
            ui.Text(new Rect(left.x + 12, left.yMax - 43, left.width - 22, 32), "Select a mount to change hardware.", 11, Muted);

            Frame(viewport);
            var active = catalog.presets[View.design.preset];
            ViewportLabels("ASSEMBLY PREVIEW", active.name, "DRAG TO ORBIT    ·    SCROLL TO ZOOM");
            ui.Card(right);
            ui.Title(right, "Engineering", View.stats.ready ? "FLIGHT READY" : "HOLD");
            var stats = View.stats;
            float rx = right.x + 12, span = right.width - 24, half = (span - 4) / 2, y = right.y + 53;
            ui.Metric(new Rect(rx, y, half, 52), "WET MASS", N(stats.wet, "0"), "kg");
            ui.Metric(new Rect(rx + half + 4, y, half, 52), "DELTA-V", N(stats.deltaV, "0"), "m/s");
            ui.Metric(new Rect(rx, y + 56, half, 52), "POWER / SUNLIT", N(stats.power, "0"), "W");
            ui.Metric(new Rect(rx + half + 4, y + 56, half, 52), "COST / CAP", N(stats.cost, "0") + "/" + N(catalog.mission.maxCost, "0"), "cr");
            ui.Metric(new Rect(rx, y + 112, half, 52), "PROPELLANT", N(stats.fuel, "0"), "kg");
            ui.Metric(new Rect(rx + half + 4, y + 112, half, 52), "LUNAR TWR", N(stats.lunarTwr, "0.00"));
            ui.Reading(rx, y + 170, span, "CoM x / y / z", N(stats.centerX, "0.00") + " / " + N(stats.center, "0.00") + " / " + N(stats.centerZ, "0.00") + " m");
            y += 203;
            if (!stats.ready)
            {
                ui.Text(new Rect(rx, y, span, 34), string.Join(" ", stats.issues), 12, Amber, false, false, TextAnchor.UpperLeft, true);
                y += 37;
            }
            bool editing = inspected >= 0;
            int mount = editing ? inspected : hover;
            if (mount < 0)
            {
                ui.Text(new Rect(rx, y, span, 15), "HERITAGE REFERENCE  ·  NASA", 9, Accent, false, true);
                ui.Text(new Rect(rx, y + 17, span, 26), active.heritage, 20, Ink, true);
                var photo = Resources.Load<Texture2D>("Frontier/" + active.photo);
                if (photo) GUI.DrawTexture(new Rect(rx, y + 49, Mathf.Min(209, span), Mathf.Min(125, right.yMax - y - 105)),
                    photo, ScaleMode.ScaleAndCrop);
                ui.Text(new Rect(rx, right.yMax - 73, span, 42), active.historical, 12, Muted, false, false, TextAnchor.UpperLeft, true);
                if (ui.Button(new Rect(rx, right.yMax - 28, span, 21), "Apollo technical record ↗", size: 11))
                    Application.OpenURL(active.specSource);
                return;
            }
            var target = catalog.FindPart(View.design.parts[mount]);
            ui.Text(new Rect(rx, y, span, 15), "SIMULATION COMPONENT", 9, Accent, false, true);
            ui.Text(new Rect(rx, y + 17, span, 27), target?.name ?? "Empty mount", 20, Ink, true);
            ui.Text(new Rect(rx, y + 47, span, 36), target?.description ?? "Choose compatible hardware to fill this mount.", 13, Muted, false, false, TextAnchor.UpperLeft, true);
            if (target != null)
            {
                string spec = N(target.mass, "0") + " kg   ·   " + target.cost + " cr";
                if (target.thrust > 0) spec += "   ·   " + N(target.thrust, "0") + " N";
                if (target.load > 0) spec += "   ·   " + N(target.load, "0") + " W";
                if (target.capacity > 0) spec += "   ·   " + N(target.capacity, "0") + " Wh";
                ui.Text(new Rect(rx, y + 86, span, 16), spec, 10, Muted, false, true);
            }
            if (!editing) return;
            float options = y + 110;
            for (int i = 0; i < catalog.parts.Length; i++)
            {
                var candidate = catalog.parts[i];
                if (candidate.slot != mount) continue;
                if (ui.Button(new Rect(rx, options, span, 28), candidate.name,
                    selected: candidate.id == target?.id, enabled: candidate.id != target?.id, size: 12))
                    Send(Command.Install, i);
                options += 32;
            }
            if (ui.Button(new Rect(rx, options + 4, half, 30), "Rotate 90°", enabled: target != null)) Send(Command.Rotate, mount);
            if (ui.Button(new Rect(rx + half + 4, options + 4, half, 30), "Detach", enabled: target != null)) Send(Command.Detach, mount);
        }

        void ViewportLabels(string top, string title, string bottom)
        {
            ui.Fill(new Rect(viewport.x, viewport.y, viewport.width, 54), new Color(.04f,.047f,.074f,.84f));
            ui.Text(new Rect(viewport.x + 11, viewport.y + 4, viewport.width - 20, 17), top, 9, Muted, false, true);
            ui.Text(new Rect(viewport.x + 10, viewport.y + 22, viewport.width - 22, 26), title, 20, Ink, true);
            ui.Fill(new Rect(viewport.x, viewport.yMax - 35, viewport.width, 35), new Color(.046f,.052f,.089f,.93f));
            ui.Rule(viewport.x, viewport.yMax - 36, viewport.width);
            ui.Text(new Rect(viewport.x + 10, viewport.yMax - 34, viewport.width - 114, 32), bottom, 9, Muted, false, true);
            if (ui.Button(new Rect(viewport.xMax - 86, viewport.yMax - 30, 75, 24), "Reset view", size: 11))
                stage.Reset(viewMode);
        }

        void Cockpit(float w, float h)
        {
            FlightConsole f = View.flight;
            if (f == null) return;
            Section("LIVE LUNAR OPERATIONS", catalog.mission.name);
            ui.Text(new Rect(w - 502, 95, 181, 30), "MET " + Clock(f.time), 12, Accent, false, true, TextAnchor.MiddleRight);
            for (int i = 0; i < Mission.WarpRates.Length; i++)
            {
                int rate = Mission.WarpRates[i];
                if (ui.Button(new Rect(w - 310 + i * 58, 95, 53, 30), rate == 0 ? "Pause" : "×" + rate,
                    selected: f.warp == rate, size: 12)) Send(Command.Warp, i);
            }

            var sidebar = new Rect(14, viewport.y, 207, viewport.height);
            var right = new Rect(viewport.xMax + 12, viewport.y, w - viewport.xMax - 27, viewport.height);
            ui.Card(sidebar);
            ui.Title(sidebar, "Mission objectives");
            DrawObjectives(sidebar, f);
            Frame(viewport);
            ViewportLabels(viewMode == 2 ? "LOLA ELEVATION MODEL   ·   8× VERTICAL SCALE" : "SELENOCENTRIC FRAME",
                viewMode == 0 ? "Orbital map" : viewMode == 1 ? "Chase camera" : "Survey terrain",
                viewMode == 2 ? "GREEN  LOWER SLOPE    /    AMBER  HIGHER SLOPE" : "LIVE ORBIT   /   BURN PREVIEW   /   SURVEY TARGET");
            string[] modes = { "Orbit", "Chase", "Terrain" };
            for (int i = 0; i < modes.Length; i++)
            {
                var button = new Rect(viewport.xMax - 192 + 61 * i, viewport.y + 10, 56, 29);
                if (ui.Button(button, modes[i], selected: viewMode == i, enabled: i < 2 || f.products[f.target] != null, size: 12))
                {
                    viewMode = i;
                    stage.Reset(i);
                }
                if (i == 2 && f.products[f.target] == null) ui.Hint(button, "Capture a lidar scan of the current target to view its terrain.");
            }
            var site = catalog.sites[f.target];
            ui.Text(new Rect(viewport.x + 10, viewport.yMax - 65, viewport.width - 20, 23),
                site.name.ToUpperInvariant() + "   ·   " + Coordinates(site), 10, Amber, false, true);
            ui.Text(new Rect(viewport.x + 10, viewport.yMax - 89, viewport.width - 20, 24), f.status, 11, Mint, false, true);
            DrawStation(right, f);

            var bar = new Rect(14, Math.Min(h - 62, viewport.yMax + 13), w - 30, 43);
            ui.Card(bar);
            ui.Text(new Rect(bar.x + 10, bar.y, 113, bar.height), "EVENT LOG", 10, Accent, false, true);
            ui.Text(new Rect(bar.x + 117, bar.y, bar.width - 230, bar.height), f.log?[0] ?? "", 11, Muted, false, true);
            if (ui.Button(new Rect(bar.xMax - 103, bar.y + 7, 93, 29), "Full log", size: 12)) Open("log");
        }

        void DrawObjectives(Rect box, FlightConsole f)
        {
            int done = 0;
            foreach (int level in f.scanned) if (level > 0) done++;
            string[] labels = { "Qualify the orbit", "Map three sites", "Choose a candidate", "Downlink the map" };
            string[] values = { "Perilune 46–92 km", done + " / 3 products", "Compare slope, then precision", "Earth line of sight required" };
            bool[] states = { f.stage >= 1, f.stage >= 2, f.stage >= 2 && f.scanned[f.selected] == 2, f.stage == 3 };
            bool active = true;
            for (int i = 0; i < labels.Length; i++)
            {
                float y = box.y + 52 + i * 53;
                ui.Text(new Rect(box.x + 10, y + 2, 22, 22), states[i] ? "✓" : "0" + (i + 1), 10, states[i] ? Mint : Subtle, false, true);
                ui.Text(new Rect(box.x + 35, y, box.width - 41, 24), labels[i], 15, !states[i] && active ? Accent : Ink, true);
                ui.Text(new Rect(box.x + 35, y + 25, box.width - 41, 16), values[i], 11, Muted);
                ui.Rule(box.x + 10, y + 46, box.width - 20);
                if (!states[i]) active = false;
            }
            float yy = box.yMax - 191;
            ui.Text(new Rect(box.x + 10, yy, box.width - 18, 22), "SPACECRAFT STATE", 9, Subtle, false, true);
            ui.Reading(box.x + 10, yy + 21, box.width - 20, "Altitude", N(f.elements.altitude) + " km");
            ui.Reading(box.x + 10, yy + 44, box.width - 20, "Speed", N(f.elements.speed, "0") + " m/s");
            ui.Reading(box.x + 10, yy + 67, box.width - 20, "Reserve", N(f.deltaV, "0") + " m/s");
            ui.Reading(box.x + 10, yy + 90, box.width - 20, "Sunlight", f.sunlit ? "Day side" : "Eclipse");
            ui.Reading(box.x + 10, yy + 113, box.width - 20, "Earth link", f.visible ? "Clear" : "Blocked");
            if (ui.Button(new Rect(box.x + 10, box.yMax - 39, box.width - 20, 28), "Mission brief", size: 12)) Open("brief");
        }

        static string Coordinates(SurveySite site)
        {
            return N(Math.Abs(site.lat), "0.00") + "°" + (site.lat < 0 ? " S" : " N") +
                "   " + N(Math.Abs(site.lon), "0.00") + "°" + (site.lon < 0 ? " W" : " E");
        }
    }
}
