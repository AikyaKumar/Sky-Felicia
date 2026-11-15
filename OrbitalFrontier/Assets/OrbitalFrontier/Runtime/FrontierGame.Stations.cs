using System;
using OrbitalFrontier.Core;
using UnityEngine;
using static OrbitalFrontier.Runtime.FrontierConsole;

namespace OrbitalFrontier.Runtime
{
    public sealed partial class FrontierGame
    {
        void DrawStation(Rect station, FlightConsole f)
        {
            ui.Card(station);
            float tabWidth = (station.width - 21) / tabNames.Length;
            for (int i = 0; i < tabNames.Length; i++)
                if (ui.Button(new Rect(station.x + 7 + i * tabWidth, station.y + 7, tabWidth - 3, 32),
                    tabNames[i], selected: panel == i, size: 13)) SwitchPanel(i);
            ui.Rule(station.x, station.y + 45, station.width);
            float x = station.x + 10, y = station.y + 54, w = station.width - 20;
            switch (panel)
            {
                case 0: DrawNavigation(x, y, w, f); break;
                case 1: DrawFlight(x, y, w, f); break;
                case 2: DrawEngineering(x, y, w, f); break;
                case 3: DrawLidar(x, y, w, f); break;
                case 4: DrawCommunications(x, y, w, f); break;
            }
        }

        void StationHeading(float x, float y, float w, string heading, string note)
        {
            ui.Text(new Rect(x, y, w, 31), heading, 25, Ink, true);
            ui.Text(new Rect(x, y + 33, w, 33), note, 13, Muted, false, false, TextAnchor.UpperLeft, true);
        }

        void Pair(float x, float y, float w, string name, string value, string unit,
            string second, string secondValue, string secondUnit)
        {
            float half = (w - 5) / 2;
            ui.Metric(new Rect(x, y, half, 44), name, value, unit);
            ui.Metric(new Rect(x + half + 5, y, half, 44), second, secondValue, secondUnit);
        }

        void DrawNavigation(float x, float y, float w, FlightConsole f)
        {
            var e = f.elements;
            StationHeading(x, y, w, "Trajectory planning", "Preview a tangential correction at the planned ignition time.");
            Pair(x, y + 67, w, "PERILUNE", N(e.peri), "km", "APOLUNE", e.bound ? N(e.apo) : "—", "km");
            Pair(x, y + 115, w, "INCLINATION", N(e.inclination), "°", "PERIOD", e.bound ? N(e.period / 60) : "—", "min");
            ui.Reading(x, y + 164, w, "Eccentricity", N(e.eccentricity, "0.000"));
            float half = (w - 6) / 2;
            ui.Text(new Rect(x, y + 191, half, 15), "BURN ΔV (m/s)", 9, Subtle, false, true);
            ui.Text(new Rect(x + half + 6, y + 191, half, 15), "START IN (s)", 9, Subtle, false, true);
            var value = ui.Field(new Rect(x, y + 209, half, 30), "planBurn", inputBurn);
            var delay = ui.Field(new Rect(x + half + 6, y + 209, half, 30), "planDelay", inputDelay);
            if ((value != inputBurn || delay != inputDelay) && !f.autoBurn)
            {
                inputBurn = value;
                inputDelay = delay;
                ApplyPlan(false);
            }
            ui.Reading(x, y + 246, w, "Predicted perilune", N(f.predicted.peri) + " km", Mint);
            ui.Reading(x, y + 271, w, "Ignition",
                f.autoBurn && f.burnAt <= f.time ? N(f.burnRemaining) + " m/s left" : "MET " + Clock(f.burnAt));
            if (ui.Button(new Rect(x, y + 305, half - 3, 32), "Suggest correction", enabled: !f.autoBurn, size: 12))
            {
                Send(Command.Suggest);
                if (!HasHost) ReadPlan();
            }
            if (ui.Button(new Rect(x + half + 5, y + 305, half + 1, 32), "Execute burn", true,
                enabled: !f.autoBurn && !f.scanning && !f.transmitting && Math.Abs(f.burn) >= .1 && Math.Abs(f.burn) <= f.deltaV))
                ApplyPlan(true);
            if (ui.Button(new Rect(x, y + 344, w, 29), "Abort operations", size: 12)) Send(Command.Stop);
        }

        void DrawFlight(float x, float y, float w, FlightConsole f)
        {
            StationHeading(x, y, w, "Flight control", "Rate-limited attitude hold slews the thrust vector in the orbital plane.");
            Pair(x, y + 67, w, "VELOCITY", N(f.elements.speed, "0"), "m/s", "ALTITUDE", N(f.elements.altitude), "km");
            Pair(x, y + 115, w, "THROTTLE", N(f.throttle * 100, "0"), "%", "PROPELLANT", N(f.fuel), "kg");
            ui.Reading(x, y + 164, w, "Local escape velocity", N(f.elements.escape, "0") + " m/s");
            ui.Reading(x, y + 188, w, "Attitude status", f.offAxis ? "Slewing" : "On vector");
            ui.Text(new Rect(x, y + 217, w, 24), "Attitude hold", 16, Ink, true);
            float slice = w / 4;
            for (int i = 0; i < Mission.Pointing.Length; i++)
                if (ui.Button(new Rect(x + i * slice, y + 244, slice - 3, 32), Mission.Pointing[i],
                    selected: f.pointing == i, enabled: !f.autoBurn, size: 12)) Send(Command.Pointing, i);
            ui.Text(new Rect(x, y + 282, w, 24), "Manual throttle", 16, Ink, true);
            double[] throttle = { 0, 0.25, 0.5, 1 };
            for (int i = 0; i < throttle.Length; i++)
                if (ui.Button(new Rect(x + i * slice, y + 310, slice - 3, 32), N(throttle[i] * 100, "0") + "%",
                    selected: f.commandedThrottle == throttle[i], enabled: !f.autoBurn, size: 12))
                    Send(Command.Throttle, value: throttle[i]);
            if (ui.Button(new Rect(x, y + 349, w, 29), "Abort all operations", size: 12)) Send(Command.Stop);
        }

        void DrawEngineering(float x, float y, float w, FlightConsole f)
        {
            StationHeading(x, y, w, "Electrical & thermal", "Battery reserve supplies the bus when the Moon eclipses the Sun.");
            Pair(x, y + 67, w, "GENERATION", N(f.generated,"0"), "W", "BUS LOAD", N(f.demand,"0"), "W");
            Pair(x, y + 115, w, "BATTERY", N(f.battery,"0"), "Wh", "ENGINE TEMP.", N(f.temperature,"0"), "K");
            ui.Meter(new Rect(x, y + 167, w, 6), f.battery / f.batteryCapacity);
            string[] buses = { "Solar arrays", "Lidar bus", "Radio bus" };
            bool[] online = { f.solar, f.lidar, f.radio };
            for (int i = 0; i < buses.Length; i++)
            {
                float row = y + 186 + i * 36;
                bool live = online[i];
                if (ui.Button(new Rect(x, row, w, 31), "", selected: live)) Send(Command.System, i);
                ui.Text(new Rect(x + 9, row, w - 78, 31), buses[i], 14, Ink, true);
                ui.Text(new Rect(x + w - 74, row, 66, 31), live ? "ONLINE" : "OFF", 10, live ? Mint : Muted, false, true, TextAnchor.MiddleRight);
            }
            ui.Reading(x, y + 300, w, "Avionics", f.bus ? "Powered" : "Undervoltage", f.bus ? Mint : Amber);
            ui.Reading(x, y + 324, w, "Engine", f.engineLimited ? "Thermally limited" : "Nominal");
        }

        void DrawLidar(float x, float y, float w, FlightConsole f)
        {
            StationHeading(x, y, w, "Laser altimetry", "Compare relief and slope from three NASA LOLA elevation tiles.");
            for (int i = 0; i < catalog.sites.Length; i++)
            {
                float row = y + 63 + i * 27;
                SiteButton(x, row, w, catalog.sites[i].name, f.scanned[i] == 0 ? "PENDING" : f.scanned[i] == 1 ? "COARSE" : "PRECISION",
                    f.target == i, !f.scanning, Command.Target, i);
            }
            float half = (w - 5) / 2;
            if (ui.Button(new Rect(x,y + 148,half,28),"Coarse  8×8",selected:f.scanLevel == 1,enabled:!f.scanning,size:12)) Send(Command.Resolution,1);
            if (ui.Button(new Rect(x + half + 5,y + 148,half,28),"Precision  24×24",selected:f.scanLevel == 2,enabled:!f.scanning,size:12)) Send(Command.Resolution,2);
            ui.Reading(x, y + 180, w, "Slant range", N(f.scanRange, "0") + " km");
            string access = f.scanWindow ? "In window" : f.accessEta > 0 ? (f.waiting ? "ETA " : "In ") + Clock(f.accessEta) : "Out of range";
            ui.Reading(x, y + 203, w, "Target access", access, f.scanWindow ? Mint : Amber);
            if (ui.Button(new Rect(x, y + 232, half, 31), "Coast to access", enabled: f.stage >= 1 && !f.scanning &&
                !f.autoBurn && !f.transmitting && !f.scanWindow && !f.waiting, size: 12)) Send(Command.Access);
            if (ui.Button(new Rect(x + half + 5, y + 232, half, 31), "Start scan", true,
                enabled: f.available && !f.scanning, size: 12)) Send(Command.Scan);
            ui.Meter(new Rect(x, y + 274, w, 5), f.scanProgress);
            DrawProduct(x, y + 289, w, f.target, f.products[f.target]);
        }

        void DrawCommunications(float x, float y, float w, FlightConsole f)
        {
            StationHeading(x, y, w, "Earth downlink", "Select the lowest-slope candidate and transmit its precision map.");
            ui.Reading(x, y + 63, w, "Line of sight", f.visible ? "Earth visible" : "Moon occultation", f.visible ? Mint : Amber);
            ui.Reading(x, y + 86, w, "Science storage", N(f.data, "0.00") + " / " + Mission.StorageCapacity + " MB");
            for (int i = 0; i < catalog.sites.Length; i++)
                SiteButton(x, y + 116 + i * 27, w, catalog.sites[i].name,
                    f.products[i] == null ? "—" : N(f.products[i].slope90, "0.00") + "°",
                    f.selected == i, !f.transmitting, Command.Recommend, i);
            bool ready = f.stage >= 2 && f.scanned[f.selected] == 2 && f.radio && f.bus && f.visible &&
                !f.transmitting && !f.scanning && !f.autoBurn && f.commandedThrottle == 0;
            if (ui.Button(new Rect(x, y + 203, w, 37), f.transmitting ? "Transmitting…" : "Transmit recommendation",
                true, enabled: ready, size: 14)) Send(Command.Transmit);
            ui.Meter(new Rect(x, y + 251, w, 5), f.transmitProgress);
            DrawProduct(x, y + 270, w, f.selected, f.products[f.selected]);
        }

        void SiteButton(float x, float y, float w, string label, string status, bool selected, bool enabled, Command command, int index)
        {
            if (ui.Button(new Rect(x, y, w, 24), "", selected: selected, enabled: enabled)) Send(command, index);
            ui.Text(new Rect(x + 6, y, w - 95, 24), label, 13, Ink);
            ui.Text(new Rect(x + w - 95, y, 88, 24), status, 9, status == "PENDING" ? Muted : Mint, false, true, TextAnchor.MiddleRight);
        }

        void DrawProduct(float x, float y, float w, int site, SurveyProduct product)
        {
            if (product == null) return;
            if (drawnSite != site || drawnProduct?.level != product.level || drawnProduct == null || !surveyImage)
            {
                drawnSite = site;
                drawnProduct = product;
                if (surveyImage) Destroy(surveyImage);
                var n = product.side;
                surveyImage = new Texture2D(n, n, TextureFormat.RGBA32, false)
                    { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "LOLA slope tile" };
                var pixels = new Color[n * n];
                for (int j = 0; j < n; j++)
                    for (int i = 0; i < n; i++)
                        pixels[(n - 1 - j) * n + i] = Color.Lerp(new Color(.15f,.47f,.46f),
                            new Color(.98f,.67f,.21f), (float)(product.slope[j * n + i] / 15));
                surveyImage.SetPixels(pixels);
                surveyImage.Apply(false, true);
            }
            GUI.DrawTexture(new Rect(x, y, 84, 84), surveyImage);
            float xx = x + 94, width = w - 94;
            ui.Reading(xx, y, width, "90th pct. slope", N(product.slope90, "0.00") + "°", Mint);
            ui.Reading(xx, y + 21, width, "Cell spacing", N(product.cellKm, "0.00") + " km");
            ui.Reading(xx, y + 42, width, "Relief", N(product.high - product.low, "0") + " m");
            ui.Reading(xx, y + 63, width, "Volume", N(product.volume, "0.00") + " MB");
            ui.Hint(new Rect(x, y, 84, 84), "LOLA slope. Green cells are flatter. Orange cells are steeper.");
        }

        void Report(float w, float h)
        {
            ui.Fill(new Rect(0, 60, w, h - 60), Bg);
            FlightConsole f = View.flight;
            bool success = f.stage == 3;
            Section("MISSION DEBRIEF", success ? "Survey complete" : "Mission ended");
            float half = (w - 45) / 2;
            var summary = new Rect(14, 149, half, Mathf.Min(448, h - 174));
            var log = new Rect(summary.xMax + 12, summary.y, half, summary.height);
            ui.Card(summary);
            ui.Title(summary, success ? "Earth acknowledgement received" : "Flight terminated");
            if (success)
            {
                string site = catalog.sites[f.selected].name;
                ui.Text(new Rect(27, 209, half - 24, 40), site, 29, Ink, true);
                ui.Text(new Rect(25, 245, half - 24, 80), f.score + " / 100", 56, Accent, true);
                ui.Text(new Rect(27, 323, half - 25, 39), f.scoreDetail, 13, Muted, false, false, TextAnchor.UpperLeft, true);
                Pair(28, 368, half - 28, "DATA DELIVERED", N(f.downlinked, "0.00"), "MB", "CANDIDATE SLOPE", N(f.recommendation, "0.00"), "°");
                Pair(28, 418, half - 28, "PROPELLANT LEFT", N(f.fuel), "kg", "MISSION TIME", Clock(f.time), "");
            }
            else
            {
                ui.Text(new Rect(27, 209, half - 25, 50), "Loss of vehicle", 33, Ink, true);
                ui.Text(new Rect(27, 270, half - 25, 52), f.scoreDetail, 17, Amber, false, false, TextAnchor.UpperLeft, true);
            }
            if (ui.Button(new Rect(27, summary.yMax - 53, 177, 36), "Fly another mission", true, enabled: !HasHost))
                Send(Command.OpenHangar);
            ui.Card(log);
            ui.Title(log, "Mission record");
            for (int i = 0; i < f.log.Length; i++)
                ui.Text(new Rect(log.x + 13, log.y + 52 + 32 * i, half - 25, 29), f.log[i], 11, Muted, false, true, TextAnchor.MiddleLeft, true);
        }
    }
}
