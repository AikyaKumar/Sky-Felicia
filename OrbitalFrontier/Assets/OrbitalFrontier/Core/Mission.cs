using System;
using System.Collections.Generic;

namespace OrbitalFrontier.Core
{
    [Serializable]
    public sealed class FlightConsole
    {
        public OrbitState orbit;
        public OrbitalElements elements, predicted;
        public VectorD[] trajectory, preview, markers;
        public VectorD sun, earth;
        public double time, warp, fuel, capacity, deltaV, battery, batteryCapacity,
            generated, demand, temperature, throttle, commandedThrottle, heading, exposure, downlinked,
            burn, burnAt, burnRemaining, recommendation, scanProgress, scanRange, accessEta = -1, data, transmitProgress;
        public int stage, target, scanLevel, pointing, selected, score;
        public int[] scanned;
        public SurveyProduct[] products;
        public string[] log;
        public string status, scoreDetail;
        public bool sunlit, visible, scanWindow, running, waiting, autoBurn, scanning, transmitting, solar, lidar, radio, bus,
            engineLimited, available, offAxis, assisted;
    }

    public sealed class Mission
    {
        public static readonly int[] WarpRates = { 0, 1, 5, 25, 160 };
        public static readonly string[] Pointing = { "Prograde", "Outward", "Retrograde", "Inward" };
        static readonly VectorD Sun = Orbit.Direction(6, 54);
        static readonly VectorD Earth = Orbit.Direction(0, 8);
        public const double StorageCapacity = 8;
        const double MaxBurn = 120;
        const double LightRoundTrip = 2.565;
        readonly GameCatalog catalog;
        readonly CraftStats craft;
        readonly List<string> log = new List<string>();
        readonly FlightConsole f;
        double requestedWarp = 1, plannedDelay = 18;
        readonly double initialFuel;

        public FlightConsole Console => f;

        public Mission(GameCatalog catalog, CraftDesign design, bool assisted)
        {
            this.catalog = catalog;
            craft = SpacecraftStats.Calculate(catalog, design);
            if (!craft.ready)
                throw new InvalidOperationException("Invalid spacecraft build.");
            initialFuel = craft.fuel;
            double apo = catalog.moonRadius + 180000, peri = catalog.moonRadius + 23000;
            double launchSpeed = Math.Sqrt(catalog.moonMu * (2 / apo - 2 / (apo + peri)));
            double lon = 203 * Orbit.Rad;
            var east = new VectorD(-Math.Sin(lon), 0, Math.Cos(lon));
            var north = new VectorD(0, 1, 0);
            var state = new OrbitState(Orbit.Direction(0, 203) * apo,
                (east * Math.Cos(2.0 * Orbit.Rad) - north * Math.Sin(2.0 * Orbit.Rad)) * launchSpeed);
            f = new FlightConsole {
                orbit = state, battery = craft.battery * 0.78, batteryCapacity = craft.battery,
                fuel = craft.fuel, capacity = craft.fuel, temperature = 293, selected = 0, target = 0,
                solar = true, lidar = true, radio = true, bus = true, running = true, scanLevel = 1, assisted = assisted,
                scanned = new int[catalog.sites.Length], products = new SurveyProduct[catalog.sites.Length],
                markers = new VectorD[catalog.sites.Length], sun = Sun, earth = Earth
            };
            f.burn = assisted ? SuggestedBurn() : 0;
            f.burnAt = plannedDelay;
            Log("Inserted into lunar orbit. A prograde burn raises perilune.");
            Refresh();
        }

        public void SetWarp(int index)
        {
            if (index >= 0 && index < WarpRates.Length && f.running)
            {
                f.waiting = false;
                requestedWarp = WarpRates[index];
                SetStatus(requestedWarp == 0 ? "Simulation paused." : "Time rate set to ×" + requestedWarp + ".");
                Refresh();
            }
        }

        public void SetThrottle(double value)
        {
            if (!f.running)
                return;
            if (f.autoBurn)
            {
                SetStatus("Cancel the queued or active burn before using manual throttle.");
                return;
            }
            f.waiting = false;
            f.commandedThrottle = Orbit.Clamp(value, 0, 1);
            if (f.commandedThrottle > 0)
                requestedWarp = 1;
            Refresh();
        }

        public void SetPointing(int index)
        {
            if (!f.running || f.autoBurn || index < 0 || index >= Pointing.Length)
                return;
            f.pointing = index;
            SetStatus("Guidance: " + Pointing[index] + ".");
            Refresh();
        }

        public void SetPlan(double deltaV, double delay)
        {
            if (!f.running || f.autoBurn)
                return;
            f.burn = Orbit.Clamp(deltaV, -MaxBurn, MaxBurn);
            plannedDelay = Orbit.Clamp(delay, 0, 1800);
            f.burnAt = f.time + plannedDelay;
            Refresh();
        }

        public void SuggestPlan()
        {
            if (f.autoBurn || !f.running)
                return;
            f.burn = SuggestedBurn();
            f.burnAt = f.time + plannedDelay;
            SetStatus("Guidance calculated a " + f.burn.ToString("0.0") + " m/s correction.");
            Refresh();
        }

        double SuggestedBurn()
        {
            OrbitState at = Orbit.Coast(f.orbit, catalog.moonMu, plannedDelay);
            return Math.Round(Orbit.Correction(at, catalog.moonMu,
                catalog.moonRadius + 1000 * catalog.mission.targetPeriKm), 1);
        }

        public void ExecutePlan()
        {
            if (!f.running)
                return;
            if (f.autoBurn || f.scanning || f.transmitting || Math.Abs(f.burn) < 0.1 || Math.Abs(f.burn) > f.deltaV)
            {
                SetStatus("Burn unavailable. Check Δv reserve and active operations.");
                return;
            }
            f.waiting = false;
            f.commandedThrottle = 0;
            f.pointing = f.burn > 0 ? 0 : 2;
            f.burnAt = f.time + plannedDelay;
            f.burnRemaining = Math.Abs(f.burn);
            f.autoBurn = true;
            requestedWarp = plannedDelay > 5 ? 25 : 1;
            Log("Maneuver queued at MET +" + plannedDelay.ToString("0") + " s.");
            Refresh();
        }

        public void AllStop()
        {
            if (!f.running)
                return;
            f.autoBurn = false;
            f.burnRemaining = 0;
            f.commandedThrottle = 0;
            f.waiting = false;
            f.scanning = false;
            f.transmitting = false;
            requestedWarp = 1;
            Log("Operations stopped.");
            Refresh();
        }

        public void ToggleSystem(int system)
        {
            if (!f.running)
                return;
            if (system == 0) f.solar = !f.solar;
            if (system == 1)
            {
                f.lidar = !f.lidar;
                if (!f.lidar) f.scanning = false;
            }
            if (system == 2)
            {
                f.radio = !f.radio;
                if (!f.radio) f.transmitting = false;
            }
            Log("Power buses updated.");
            Refresh();
        }

        public void Target(int index)
        {
            if (!f.running || f.scanning || index < 0 || index >= catalog.sites.Length)
                return;
            f.waiting = false;
            if (requestedWarp > 25) requestedWarp = 1;
            f.target = index;
            f.scanProgress = 0;
            Refresh();
        }

        public void SelectResolution(int level)
        {
            if (f.running && !f.scanning && (level == 1 || level == 2))
            {
                f.scanLevel = level;
                Refresh();
            }
        }

        public void CoastToAccess()
        {
            if (!f.running || f.scanning || f.transmitting || f.autoBurn || f.commandedThrottle > 0)
                return;
            if (f.stage == 0)
            {
                SetStatus("Qualify the orbit before beginning the survey.");
                return;
            }
            if (f.scanWindow)
            {
                requestedWarp = 1;
                Refresh();
                SetStatus("Target already within lidar range.");
                return;
            }
            if (!f.elements.bound || f.elements.peri < 5)
            {
                SetStatus("Restore a safe lunar orbit for a survey pass.");
                return;
            }
            if (PredictAccess() < 0)
            {
                SetStatus("No pass next orbit. Select another site or adjust the orbit.");
                return;
            }
            f.waiting = true;
            requestedWarp = 160;
            Refresh();
            SetStatus("Coasting to " + catalog.sites[f.target].name + ".");
        }

        public void BeginScan()
        {
            if (!f.running || f.scanning)
                return;
            if (f.stage == 0 || !f.scanWindow || !f.lidar || !f.bus || f.autoBurn || f.transmitting ||
                f.commandedThrottle > 0)
            {
                SetStatus("Lidar needs a qualified orbit, target access, power and an idle engine.");
                return;
            }
            if (f.scanned[f.target] >= f.scanLevel)
            {
                SetStatus("This site already has a product at that resolution.");
                return;
            }
            f.waiting = false;
            f.scanProgress = 0;
            f.scanning = true;
            requestedWarp = 5;
            Log("Lidar scanning " + catalog.sites[f.target].name + ".");
            Refresh();
        }

        public void Recommend(int index)
        {
            if (!f.running || f.transmitting || index < 0 || index >= f.products.Length)
                return;
            f.selected = index;
            Refresh();
        }

        public void BeginDownlink()
        {
            if (!f.running || f.transmitting)
                return;
            if (f.stage < 2 || f.scanned[f.selected] != 2 || !f.visible || !f.radio || !f.bus ||
                f.scanning || f.autoBurn || f.commandedThrottle > 0)
            {
                SetStatus("Downlink needs all three surveys, a precision map for your site, Earth line of sight and radio power.");
                return;
            }
            f.transmitProgress = 0;
            f.waiting = false;
            f.transmitting = true;
            requestedWarp = 5;
            Log("Transmitting candidate " + catalog.sites[f.selected].name + ".");
            Refresh();
        }

        public void Tick(double realSeconds)
        {
            if (!f.running)
                return;
            double remaining = Math.Min(0.25, Math.Max(0, realSeconds)) * CurrentRate();
            while (remaining > 1e-8 && f.running)
            {
                double dt = Math.Min(remaining, f.autoBurn || f.commandedThrottle > 0 ? 0.4 : 4.0);
                if (f.autoBurn && f.time < f.burnAt)
                    dt = Math.Min(dt, f.burnAt - f.time);
                double before = CurrentRate();
                Advance(dt);
                remaining -= dt;
                if (CurrentRate() < before)
                    break;
            }
            Refresh(false);
        }

        double CurrentRate()
        {
            if (f.autoBurn && f.time + 1e-8 >= f.burnAt) return Math.Min(requestedWarp, 5);
            if (f.commandedThrottle > 0) return Math.Min(requestedWarp, 5);
            if (f.scanning || f.transmitting) return Math.Min(requestedWarp, 5);
            return requestedWarp;
        }

        public void Refresh(bool rebuildShape = true)
        {
            f.warp = CurrentRate();
            f.elements = Orbit.Describe(f.orbit, catalog.moonMu, catalog.moonRadius);
            f.deltaV = SpacecraftStats.DeltaV(craft.dry, f.fuel, craft.isp);
            f.sunlit = !Orbit.IsOccluded(f.orbit.p, Sun, catalog.moonRadius);
            f.visible = !Orbit.IsOccluded(f.orbit.p, Earth, catalog.moonRadius);
            f.scanWindow = CanSeeSite(f.orbit.p, f.time, f.target, out f.scanRange);
            f.exposure = f.sunlit && f.solar ? 1 : 0;
            f.generated = craft.power * f.exposure;
            f.demand = craft.idle + (f.scanning ? craft.scan : 0) + (f.transmitting ? craft.radio : 0);
            f.available = f.stage >= 1 && f.scanWindow && f.lidar && f.bus && !f.autoBurn && !f.transmitting &&
                f.commandedThrottle == 0 && f.scanned[f.target] < f.scanLevel;
            f.recommendation = f.products[f.selected]?.slope90 ?? 0;
            for (int i = 0; i < f.markers.Length; i++)
                f.markers[i] = SiteDirection(i, f.time);
            if (rebuildShape)
                RefreshTrajectory();
        }

        public void RefreshTrajectory()
        {
            f.trajectory = Orbit.Shape(f.orbit, catalog.moonMu, 144);
            f.accessEta = PredictAccess();
            f.preview = Array.Empty<VectorD>();
            f.predicted = f.elements;
            if (f.stage == 0 || f.autoBurn || Math.Abs(f.burn) > 0.1)
            {
                double ahead = f.autoBurn ? Math.Max(0, f.burnAt - f.time) : plannedDelay;
                if (!f.autoBurn) f.burnAt = f.time + ahead;
                OrbitState future = Orbit.Coast(f.orbit, catalog.moonMu, ahead);
                double burn = f.autoBurn ? Math.Sign(f.burn) * f.burnRemaining : f.burn;
                future.v += Orbit.AlongTrack(future) * burn;
                f.predicted = Orbit.Describe(future, catalog.moonMu, catalog.moonRadius);
                f.preview = Orbit.Shape(future, catalog.moonMu, 144);
            }
        }

        double PredictAccess()
        {
            if (!f.elements.bound || f.elements.peri < 5) return -1;
            if (f.scanWindow) return 0;
            // Forecast along the unpowered orbit with a rotating lunar map.
            OrbitState next = f.orbit;
            for (double ahead = 15; ahead < f.elements.period + 30; ahead += 15)
            {
                next = Orbit.Coast(next, catalog.moonMu, 15);
                if (CanSeeSite(next.p, f.time + ahead, f.target, out _)) return ahead;
            }
            return -1;
        }

        VectorD SiteDirection(int i, double time)
        {
            var site = catalog.sites[i];
            return Orbit.Direction(site.lat, site.lon + time / catalog.moonDay * 360);
        }

        bool CanSeeSite(VectorD vessel, double time, int site, out double slantKm)
        {
            VectorD normal = SiteDirection(site, time);
            VectorD target = normal * catalog.moonRadius;
            VectorD towardShip = vessel - target;
            slantKm = towardShip.Norm / 1000;
            double altitude = (vessel.Norm - catalog.moonRadius) / 1000;
            return altitude <= catalog.mission.scanAltKm && altitude > 0 &&
                slantKm <= catalog.mission.maxSlantKm && VectorD.Dot(normal, towardShip.Unit) >= 0.16;
        }

        void Advance(double dt)
        {
            if (f.waiting && CanSeeSite(f.orbit.p, f.time, f.target, out _))
            {
                f.waiting = false;
                requestedWarp = 1;
                Log("Lidar access acquired. Begin the survey.");
            }

            bool burnStarted = f.autoBurn && f.time + 1e-8 >= f.burnAt;
            double wantedHeading = f.pointing * Math.PI / 2;
            double turn = Math.Atan2(Math.Sin(wantedHeading - f.heading), Math.Cos(wantedHeading - f.heading));
            double headingStep = Orbit.Clamp(turn, -0.46 * dt, 0.46 * dt);
            f.heading += headingStep;
            f.offAxis = Math.Abs(turn) > 0.02;
            f.engineLimited = f.temperature > 565;
            double manual = f.commandedThrottle;
            if (burnStarted)
            {
                requestedWarp = 5;
                manual = f.offAxis ? 0 : 1;
            }
            double throttle = f.bus && f.fuel > 0 ? manual * (f.engineLimited ? 0.46 : 1) : 0;
            double mass = craft.dry + f.fuel;
            double exhaust = craft.isp * SpacecraftStats.StandardGravity;
            double consumption = craft.thrust * throttle / exhaust * dt;
            if (burnStarted)
                consumption = Math.Min(consumption, mass * (1 - Math.Exp(-f.burnRemaining / exhaust)));
            consumption = Math.Min(f.fuel, consumption);
            VectorD thrust = Orbit.AlongTrack(f.orbit) * Math.Cos(f.heading) + f.orbit.p.Unit * Math.Sin(f.heading);
            double delivered = consumption > 0 ? exhaust * Math.Log(mass / (mass - consumption)) : 0;
            f.throttle = consumption * exhaust / (craft.thrust * dt);
            f.orbit = Orbit.Step(f.orbit, catalog.moonMu, dt, thrust * (delivered / dt));
            f.fuel -= consumption;
            f.time += dt;
            f.temperature += (f.throttle * 10.5 - (f.temperature - 283) * 0.021) * dt;
            double wattSeconds = (f.generated - f.demand) * dt - 28 * Math.Abs(headingStep);
            f.battery = Orbit.Clamp(f.battery + wattSeconds / 3600, 0, f.batteryCapacity);
            f.bus = f.battery > 0.025 || f.generated >= craft.idle;

            if (burnStarted)
            {
                f.burnRemaining = Math.Max(0, f.burnRemaining - delivered);
                if (f.burnRemaining < 0.001 || f.fuel < 0.0001)
                {
                    bool complete = f.burnRemaining < 0.001;
                    f.autoBurn = false;
                    f.burn = 0;
                    requestedWarp = 1;
                    Log(complete ? "Maneuver complete. Verify the new perilune." : "Maneuver interrupted by propellant depletion.");
                }
            }

            Refresh(false);
            if (f.elements.altitude <= 0)
            {
                f.running = false;
                f.stage = -1;
                f.scoreDetail = "Lunar impact. Raise perilune before the spacecraft reaches the surface.";
                Log("Mission ended at surface impact.");
                return;
            }

            if (f.stage == 0 && !f.autoBurn && f.commandedThrottle == 0 && f.elements.bound &&
                f.elements.peri >= catalog.mission.targetLowerKm && f.elements.peri <= catalog.mission.targetUpperKm)
            {
                f.stage = 1;
                Log("Survey orbit qualified. Acquire the first candidate.");
            }
            if (f.scanning) AdvanceScan(dt);
            if (f.transmitting) AdvanceTransmission(dt);
        }

        void AdvanceScan(double dt)
        {
            if (!f.scanWindow || !f.bus)
            {
                f.scanning = false;
                Log("Scan interrupted. Reacquire the target or restore power.");
                return;
            }
            double duration = f.scanLevel == 2 ? (craft.scan > 400 ? 22 : 32) : 12;
            f.scanProgress = Math.Min(1, f.scanProgress + dt / duration);
            if (f.scanProgress < 1) return;
            f.products[f.target] = Survey.Analyze(catalog, catalog.sites[f.target], f.scanLevel);
            f.scanned[f.target] = f.scanLevel;
            f.scanning = false;
            f.data = 0;
            int complete = 0;
            foreach (var product in f.products)
                if (product != null) { f.data += product.volume; complete++; }
            requestedWarp = 1;
            Log("Stored " + catalog.sites[f.target].name + " survey.");
            if (complete == f.products.Length && f.stage == 1)
            {
                f.stage = 2;
                Log("All sites surveyed. Choose a candidate and acquire its precision map.");
            }
        }

        void AdvanceTransmission(double dt)
        {
            if (!f.visible || !f.bus)
            {
                f.transmitting = false;
                Log("Downlink interrupted. Restore the link and retransmit.");
                return;
            }
            double transportDuration = Math.Max(4, f.data / craft.rate) + LightRoundTrip;
            f.transmitProgress = Math.Min(1, f.transmitProgress + dt / transportDuration);
            if (f.transmitProgress >= 1) Finish();
        }

        void Finish()
        {
            f.transmitting = false;
            f.downlinked = f.data;
            f.running = false;
            f.stage = 3;
            int orbit = (int)Math.Round(18 * Math.Max(0, 1 - Math.Abs(f.elements.peri - catalog.mission.targetPeriKm) / 50));
            int fuel = (int)Math.Round(15 * f.fuel / initialFuel);
            double cap = catalog.mission.maxCost, parCost = catalog.mission.parCost;
            int design = (int)Math.Round(10 * Orbit.Clamp((cap - craft.cost) / Math.Max(1, cap - parCost), 0, 1));
            var product = f.products[f.selected];
            int terrain = (int)Math.Round(25 * Orbit.Clamp(1 - (product.slope90 - 1.25) / 18, 0, 1));
            int precise = 0;
            foreach (int level in f.scanned)
                if (level == 2) precise++;
            int mapping = 26 + (int)Math.Round(6.0 * precise / f.scanned.Length);
            f.score = orbit + fuel + design + terrain + mapping;
            f.scoreDetail = "Orbit " + orbit + "/18   •   Fuel " + fuel + "/15   •   Build " + design +
                "/10   •   Terrain " + terrain + "/25   •   Mapping " + mapping + "/32";
            Log("Earth station acknowledged the science packet. Mission complete.");
        }

        void Log(string message)
        {
            int hours = (int)f.time / 3600, minutes = (int)f.time / 60 % 60, seconds = (int)f.time % 60;
            log.Insert(0, hours.ToString("00") + ":" + minutes.ToString("00") + ":" + seconds.ToString("00") + "  " + message);
            if (log.Count > 10) log.RemoveAt(log.Count - 1);
            f.log = log.ToArray();
            SetStatus(message);
        }

        void SetStatus(string message) { f.status = message; }
    }
}
