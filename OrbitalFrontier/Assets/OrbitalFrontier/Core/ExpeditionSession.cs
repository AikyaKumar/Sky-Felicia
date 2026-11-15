using System;
using System.Collections.Generic;

namespace OrbitalFrontier.Core
{
    public enum ExpeditionPhase { Design, Ascent, Cruise, Orbit, Descent, Surface, Complete, Failed }
    public enum ExpeditionCommand {
        World = 1, Objective, Difficulty, Launcher, OrbitProfile, Transfer, Vehicle, Install, Detach, Rotate,
        Launch, Stage, Coast, Capture, Maneuver, BeginDescent, LandingAssist, LandingThrottle, Stabilize, Abort,
        Move, Face, Inspect, Repair, SwitchPower, Airlock, Sample, Downlink, ReturnDesign, SetRover, Triage, Endurance
    }

    public static class CrewStation
    {
        public const int Helm = 0, Nav = 1, Engineering = 2, Life = 3, Science = 4, Comms = 5, Hatch = 6;
        public static readonly string[] Name = { "Helm", "Navigation", "Engineering", "Life support", "Science", "Communications", "Airlock" };
        public static readonly double[] X = { -.92, .92, -1.75, 1.75, -1.75, 1.75, 0 };
        public static readonly double[] Z = { -8.35, -8.35, -3.0, -3.0, 1.05, 1.05, 6.95 };
    }

    [Serializable]
    public sealed class ExpeditionCrew
    {
        public int id, station = -1;
        public bool active, outside, rover, down;
        public double x, z, yaw, oxygen = 100, health = 100, suit = 100;
        public string message;
    }

    [Serializable]
    public sealed class ExpeditionTelemetry
    {
        public double time, timeLeft, phaseTime, progress, cruiseDays, altitude, speed, vertical, vx, vz,
            x, z, throttle, fuel, deltaV, battery, power, demand, pressure, engineTemp, radiation, distance, signalDelay,
            stored, delivered, txRemaining, orbitalPeri, orbitalApo, orbitalPeriod, passEta, soilSlope,
            touchdownV, touchdownHorizontal, touchdownSlope, lastSampleSlope, lastSampleElevation, descentElapsed, roverX, roverZ, roverYaw;
        public bool assist, dampers, charger, support, transmitter, sunlight, link, transferring, staged,
            landingBurn, repaired, captureBurn, repairFaultOccurred;
        public int fault, spares, measured, orbitalPasses, samples, reportScore, stageCount, inspectedMask, roverId = -1;
        public int[] marks = new int[2];
        public double[] slopes = new double[2], elevations = new double[2];
        public string status, finding, summary;
        public string[] log = Array.Empty<string>();
        public OrbitState orbit;
        public VectorD[] track = Array.Empty<VectorD>();
    }

    [Serializable]
    public sealed class ExpeditionView
    {
        public int phase, revision;
        public ExpeditionDesign design;
        public ExpeditionBudget budget;
        public ExpeditionTelemetry telemetry;
        public ExpeditionCrew[] crew;
    }

    public sealed partial class ExpeditionSession
    {
        public const double CabinHalfWidth = 2.2, CabinFront = -8.68, CabinAft = 7.15;
        public static readonly string[] Objectives = { "Surface science", "Orbital survey", "Systems recovery" };
        readonly ExpeditionCatalog catalog;
        readonly SurfaceGrid[] terrain;
        readonly List<string> log = new List<string>();
        readonly ExpeditionView view;
        ExpeditionWorld World => catalog.worlds[view.design.world];
        ExpeditionBudget Stats => view.budget;
        ExpeditionTelemetry T => view.telemetry;
        ExpeditionDesign D => view.design;
        SurfaceGrid Grid => terrain[D.world];
        ExpeditionPhase Phase => (ExpeditionPhase)view.phase;
        public ExpeditionView View => view;
        readonly double[] moveX = new double[4], moveZ = new double[4], moveAge = new double[4];
        double ticks, warp = 1, orbits, transferPacket, measurementCooldown, captureRemaining;
        int heldStage;
        bool secondFault;

        public ExpeditionSession(ExpeditionCatalog catalog, SurfaceGrid[] terrain)
        {
            this.catalog = catalog;
            this.terrain = terrain;
            view = new ExpeditionView { design = new ExpeditionDesign(), crew = new ExpeditionCrew[4] };
            for (int i = 0; i < view.crew.Length; i++)
                view.crew[i] = new ExpeditionCrew { id = i, active = i == 0, x = i % 2 == 0 ? -.65 : .65, z = i < 2 ? -5.9 : -2.5 };
            LoadVehicle(0);
            UpdateBudget();
        }

        public void CrewActive(int seat, bool active)
        {
            if (seat < 0 || seat >= view.crew.Length || view.crew[seat].active == active) return;
            var crew = view.crew[seat];
            crew.active = active;
            moveAge[seat] = 0;
            if (!active && crew.outside)
            {
                if (crew.rover && view.telemetry != null) view.telemetry.roverId = -1;
                crew.outside = false;
                crew.rover = false;
                crew.x = 0;
                crew.z = 5.2;
            }
            if (view.telemetry != null) Log((active ? "Crew connected at seat " : "Crew disconnected from seat ") + (seat + 1) + ".");
        }

        public void Apply(ExpeditionCommand command, int seat, int n = 0, double a = 0, double b = 0, double c = 0)
        {
            if (seat < 0 || seat >= view.crew.Length || !view.crew[seat].active ||
                double.IsNaN(a) || double.IsInfinity(a) || double.IsNaN(b) || double.IsInfinity(b) ||
                double.IsNaN(c) || double.IsInfinity(c)) return;
            var crew = view.crew[seat];
            if (command == ExpeditionCommand.ReturnDesign && seat == 0)
            {
                SetPhase(ExpeditionPhase.Design);
                view.telemetry = null;
                return;
            }
            if (Phase == ExpeditionPhase.Design)
            {
                PlanCommand(command, seat, n);
                return;
            }
            if (Phase >= ExpeditionPhase.Complete || crew.down && command != ExpeditionCommand.Face) return;
            if (command == ExpeditionCommand.Move)
            {
                moveX[seat] = Orbit.Clamp(a, -1, 1);
                moveZ[seat] = Orbit.Clamp(b, -1, 1);
                if (crew.yaw != c) crew.yaw = c % 360;
                moveAge[seat] = 0.28;
                return;
            }
            if (command == ExpeditionCommand.Face)
            {
                crew.yaw = a % 360;
                return;
            }

            switch (command)
            {
                case ExpeditionCommand.Stage: At(crew, CrewStation.Helm, StageRocket); break;
                case ExpeditionCommand.Coast: At(crew, CrewStation.Nav, () => SetCoast(n)); break;
                case ExpeditionCommand.Capture: At(crew, CrewStation.Nav, Capture); break;
                case ExpeditionCommand.Maneuver: At(crew, CrewStation.Nav, () => TrimOrbit(a)); break;
                case ExpeditionCommand.BeginDescent: At(crew, CrewStation.Helm, Descent); break;
                case ExpeditionCommand.LandingAssist: At(crew, CrewStation.Helm, () => ChangeAssist(n != 0)); break;
                case ExpeditionCommand.LandingThrottle: At(crew, CrewStation.Helm, () => SetThrottle(a,b,c)); break;
                case ExpeditionCommand.Stabilize:
                    if (Phase == ExpeditionPhase.Descent) At(crew, CrewStation.Helm, () => T.dampers = !T.dampers);
                    break;
                case ExpeditionCommand.Abort: At(crew, CrewStation.Helm, Abort); break;
                case ExpeditionCommand.Inspect: Inspect(crew, n); break;
                case ExpeditionCommand.Repair: Repair(crew, n); break;
                case ExpeditionCommand.SwitchPower: At(crew, n == 1 ? CrewStation.Life : n == 2 ? CrewStation.Comms : CrewStation.Engineering, () => Toggle(n)); break;
                case ExpeditionCommand.Airlock: Airlock(crew); break;
                case ExpeditionCommand.Sample: Sample(crew, n); break;
                case ExpeditionCommand.Downlink: At(crew, CrewStation.Comms, Transmit); break;
                case ExpeditionCommand.SetRover: Rover(crew); break;
                case ExpeditionCommand.Triage: At(crew, CrewStation.Life, () => Treat(crew)); break;
            }
            view.revision++;
        }

        void PlanCommand(ExpeditionCommand command, int seat, int n)
        {
            switch (command)
            {
                case ExpeditionCommand.World when n >= 0 && n < catalog.worlds.Length:
                    if (n != D.world) LoadVehicle(n);
                    break;
                case ExpeditionCommand.Objective when n >= 0 && n < Objectives.Length: D.objective = n; break;
                case ExpeditionCommand.Difficulty when n >= 0 && n <= 1: D.difficulty = n; break;
                case ExpeditionCommand.Launcher when n >= 0 && n < catalog.launchers.Length: D.launcher = n; break;
                case ExpeditionCommand.OrbitProfile when n >= 0 && n <= 1: D.orbit = n; break;
                case ExpeditionCommand.Transfer when n >= 0 && n <= 1: D.route = n; break;
                case ExpeditionCommand.Endurance when n >= 0 && n <= 2: D.endurance = n; break;
                case ExpeditionCommand.Vehicle when n >= 0 && n < catalog.vehicles.Length: LoadVehicle(n); break;
                case ExpeditionCommand.Install when n >= 0 && n < catalog.parts.Length:
                    var part = catalog.parts[n];
                    D.parts[part.slot] = part.id;
                    D.rotations[part.slot] = 0;
                    break;
                case ExpeditionCommand.Detach when n >= 0 && n < D.parts.Length:
                    D.parts[n] = "";
                    break;
                case ExpeditionCommand.Rotate when n >= 0 && n < D.parts.Length && !string.IsNullOrEmpty(D.parts[n]):
                    D.rotations[n] = (D.rotations[n] + 1) % 4;
                    break;
                case ExpeditionCommand.Launch when seat == 0 && Stats.ready: Launch(); return;
                default: return;
            }
            UpdateBudget();
        }

        void LoadVehicle(int i)
        {
            D.vehicle = i;
            var vehicle = catalog.vehicles[i];
            Array.Copy(vehicle.parts, D.parts, D.parts.Length);
            Array.Clear(D.rotations, 0, D.rotations.Length);
            D.world = vehicle.world;
            D.launcher = i;
        }

        void UpdateBudget()
        {
            view.budget = ExpeditionEngineering.Evaluate(catalog, D);
            view.revision++;
        }

        void Launch()
        {
            foreach (var crew in view.crew)
            {
                crew.x = crew.id % 2 == 0 ? -.64 : .64;
                crew.z = crew.id < 2 ? -6.20 : -.40;
                crew.yaw = 0;
                crew.station = -1;
                crew.health = crew.oxygen = crew.suit = 100;
                crew.message = null;
                crew.outside = crew.rover = crew.down = false;
            }
            Array.Clear(moveAge, 0, moveAge.Length);
            log.Clear();
            secondFault = false; captureRemaining = orbits = 0;
            ticks = 0; warp = 1; heldStage = 0; measurementCooldown = 0;
            manualEast = manualSouth = 0;
            view.telemetry = new ExpeditionTelemetry {
                fuel = Stats.fuel, deltaV = Stats.reserve, battery = Stats.battery * .85,
                pressure = 101.3, engineTemp = 294, sunlight = true, charger = true, support = true,
                transmitter = true, assist = D.difficulty == 0, dampers = true, spares = (int)Stats.spares,
                timeLeft = Stats.sessionMinutes * 60, signalDelay = World.lightTime, distance = 0
            };
            SetPhase(ExpeditionPhase.Ascent);
            Log("Liftoff. Stage from the helm when the first burn completes.");
        }

        void SetPhase(ExpeditionPhase phase)
        {
            view.phase = (int)phase;
            if (T != null) T.phaseTime = 0;
            view.revision++;
        }

        public void Tick(double realSeconds)
        {
            if (T == null || Phase == ExpeditionPhase.Design || Phase >= ExpeditionPhase.Complete || realSeconds <= 0) return;
            double remaining = Math.Min(realSeconds, .25);
            while (remaining > 1e-7 && Phase < ExpeditionPhase.Complete)
            {
                double step = Math.Min(remaining, .04);
                UpdateCrew(step);
                Advance(step);
                remaining -= step;
            }
        }

        void Advance(double step)
        {
            T.time += step;
            T.phaseTime += step;
            T.timeLeft = Math.Max(0, Stats.sessionMinutes * 60 - T.time);
            if (T.timeLeft == 0) { Fail("The active operations window expired."); return; }
            switch (Phase)
            {
                case ExpeditionPhase.Ascent: Ascent(step); break;
                case ExpeditionPhase.Cruise: Cruise(step); break;
                case ExpeditionPhase.Orbit: OrbitOps(step); break;
                case ExpeditionPhase.Descent: Land(step); break;
                case ExpeditionPhase.Surface: SurfaceOps(step); break;
            }
            if (Phase >= ExpeditionPhase.Complete) return;
            Systems(step);
            if (T.captureBurn) AdvanceCapture(step);
            if (T.transferring) AdvanceDownlink(step);
            T.deltaV = SpacecraftStats.DeltaV(Stats.dry, T.fuel, Stats.isp);
        }

        void UpdateCrew(double dt)
        {
            foreach (var crew in view.crew)
            {
                if (!crew.active || crew.down) continue;
                int id = crew.id;
                if (moveAge[id] > 0)
                {
                    double a = moveX[id], b = moveZ[id];
                    double mag = Math.Sqrt(a * a + b * b);
                    if (mag > 1) { a /= mag; b /= mag; }
                    double v = crew.outside ? crew.rover ? 15.0 : World.id == "moon" ? 4.4 : 3.9 : 4.0;
                    double yaw = crew.yaw * Orbit.Rad, cos = Math.Cos(yaw), sin = Math.Sin(yaw);
                    double dx = (a * cos + b * sin) * v * dt, dz = (a * sin - b * cos) * v * dt;
                    double x = crew.x + dx, z = crew.z + dz;
                    if (crew.outside)
                    {
                        double edge = Grid.Extent - 8;
                        crew.x = Orbit.Clamp(x, -edge, edge);
                        crew.z = Orbit.Clamp(z, -edge, edge);
                        if (crew.rover)
                        {
                            T.roverX = crew.x;
                            T.roverZ = crew.z;
                            T.roverYaw = crew.yaw;
                        }
                        if (Grid.Slope(crew.x, crew.z) > 22 && mag > .2)
                            crew.health = Math.Max(0, crew.health - (crew.rover ? 2 : .75) * dt);
                    }
                    else
                    {
                        if (InteriorWalkable(x, crew.z)) crew.x = x;
                        if (InteriorWalkable(crew.x, z)) crew.z = z;
                    }
                    moveAge[id] -= dt;
                }
                if (crew.outside)
                {
                    double oxygenRate = 100 / Math.Max(45, Stats.suitSeconds);
                    crew.oxygen = Math.Max(0, crew.oxygen - oxygenRate * dt * (crew.rover ? 1.12 : 1));
                    crew.suit = Math.Max(0, crew.suit - dt * (crew.rover ? .075 : .14) * (T.sunlight ? 1 : 1.3));
                    if (crew.suit < 15) crew.health = Math.Max(0, crew.health - .34 * dt);
                }
                else
                {
                    if (T.pressure >= 73 && T.support && T.fault != 2)
                    {
                        crew.oxygen = Math.Min(100, crew.oxygen + dt * 3.4);
                        crew.suit = Math.Min(100, crew.suit + dt * 1.85);
                    }
                    if (T.pressure < 68) crew.oxygen = Math.Max(0, crew.oxygen - (68 - T.pressure) * dt * .15);
                }
                if (crew.oxygen <= 0) crew.health = Math.Max(0, crew.health - 4 * dt);
                if (crew.health <= 0)
                {
                    crew.down = true;
                    if (crew.rover)
                    {
                        T.roverId = -1;
                        crew.rover = false;
                    }
                    Log("Crew member " + (id + 1) + " incapacitated.");
                }
            }
            bool alive = false;
            foreach (var crew in view.crew) if (crew.active && !crew.down) alive = true;
            if (!alive) Fail("Crew became incapacitated.");
        }

        public static bool InteriorWalkable(double x, double z)
        {
            if (Math.Abs(x) > CabinHalfWidth || z < -7.88 || z > CabinAft) return false;
            if (Math.Abs(x) <= 1.23) return true;
            return !((z > -4.55 && z < -4.1) || (z > 4.10 && z < 4.53) ||
                Math.Abs(z + 3.0) < .89 || Math.Abs(z - 1.05) < .89);
        }

        bool InReach(ExpeditionCrew crew, int station) =>
            !crew.outside && station >= 0 && station <= CrewStation.Hatch &&
            Math.Pow(crew.x - CrewStation.X[station], 2) + Math.Pow(crew.z - CrewStation.Z[station], 2) <= 2.35 * 2.35;

        void At(ExpeditionCrew crew, int station, Action action)
        {
            if (!InReach(crew, station))
            {
                crew.message = "Approach the " + CrewStation.Name[station].ToLowerInvariant() + " console.";
                return;
            }
            crew.station = station;
            crew.message = null;
            action();
        }

        void Log(string s)
        {
            T.status = s;
            log.Insert(0, TimeSpan.FromSeconds(T.time).ToString(@"mm\:ss") + "  " + s);
            if (log.Count > 10) log.RemoveAt(10);
            T.log = log.ToArray();
            view.revision++;
        }

        void Fail(string reason)
        {
            if (Phase >= ExpeditionPhase.Complete) return;
            T.summary = reason;
            SetPhase(ExpeditionPhase.Failed);
            Log(reason);
        }

        void Complete()
        {
            if (Phase >= ExpeditionPhase.Complete) return;
            double science = 25 + Math.Min(15, Stats.science * .82);
            double care = 0; int members = 0;
            foreach (var crew in view.crew)
                if (crew.active) { care += crew.health; members++; }
            double land = D.objective == 1 ? 20 : 20 * Math.Max(0, 1 - T.touchdownV / (World.landingLimit * Stats.gear * 1.56));
            double fuel = 15 * Math.Min(1, (T.fuel / Stats.fuel) * 2.6);
            double cost = 10 * Orbit.Clamp((Stats.budget - Stats.cost) / (Stats.budget * .22), 0, 1);
            double systems = (T.repaired || T.fault == 0 ? 5 : 0) + (T.inspectedMask != 0 ? 5 : 0) + (members == 0 ? 0 : care / members * .05);
            T.reportScore = (int)Math.Round(Orbit.Clamp(science + land + fuel + cost + systems, 0, 100));
            T.summary = "Science " + (int)science + "/40 · " + (D.objective == 1 ? "Orbit" : "Touchdown") +
                " " + (int)land + "/20 · Fuel " + (int)fuel + "/15 · Cost " + (int)cost + "/10 · Crew and systems " + (int)systems + "/15";
            SetPhase(ExpeditionPhase.Complete);
            Log("Earth acknowledged the science products. Expedition complete.");
        }
    }
}
