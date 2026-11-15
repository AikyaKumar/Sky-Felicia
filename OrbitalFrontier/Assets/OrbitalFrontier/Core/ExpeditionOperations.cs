using System;

namespace OrbitalFrontier.Core
{
    public sealed partial class ExpeditionSession
    {
        void Systems(double dt)
        {
            double ambient = T.sunlight ? 288 : 243;
            T.engineTemp += (ambient - T.engineTemp) * Math.Min(1, dt * .025);
            double sunlight = T.sunlight ? 1 : 0;
            if (D.world == 1 && Phase == ExpeditionPhase.Surface) sunlight *= .69;
            double solarDistance = World.starDistance > 1 && Phase >= ExpeditionPhase.Orbit ? World.starDistance : 1;
            double factor = Stats.chargeRate > 0 ? 1 : sunlight / (solarDistance * solarDistance);
            T.power = T.charger ? Stats.generation * factor * (T.fault == 1 ? .13 : 1) : 0;
            double engineDraw = T.captureBurn || T.landingBurn ? 100 : 0;
            T.demand = Stats.load + (T.transferring && T.txRemaining > T.signalDelay ? Stats.peak - Stats.load - Stats.draw : 0) + engineDraw;
            if (!T.support) T.demand -= catalog.Get(D.parts[ExpeditionSlots.Life])?.draw ?? 0;
            measurementCooldown = Math.Max(0, measurementCooldown - dt);
            if (measurementCooldown > 0) T.demand += Stats.draw;
            double rate = Phase == ExpeditionPhase.Cruise ? warp > 4 ? 125 : 28 : Phase == ExpeditionPhase.Orbit ? warp > 4 ? 42 : 19 : 1;
            double wattHours = (T.power - T.demand) * dt * rate / 3600;
            T.battery = Orbit.Clamp(T.battery + wattHours, 0, Stats.battery);
            T.pressure = Orbit.Clamp(T.pressure + (T.support && T.battery > 7 && T.fault != 2 ? .94 : T.fault == 2 ? -2.15 : -.52) * dt, 0, 101.3);
            if (Phase == ExpeditionPhase.Cruise || Phase == ExpeditionPhase.Ascent) T.link = true;
        }

        void Toggle(int n)
        {
            switch (n)
            {
                case 0: T.charger = !T.charger; Log(T.charger ? "Generation breaker closed." : "Generation breaker opened."); break;
                case 1: T.support = !T.support; Log(T.support ? "Life-support blower online." : "Life-support blower shut off."); break;
                case 2: T.transmitter = !T.transmitter; Log(T.transmitter ? "Radio online." : "Radio offline."); break;
            }
        }

        void Inspect(ExpeditionCrew crew, int n)
        {
            At(crew, n, () => {
                T.inspectedMask |= 1 << n;
                switch (n)
                {
                    case CrewStation.Engineering:
                        T.finding = T.fault == 1 ? "Isolation test: charge controller output degraded to 13%. Replace its line unit." :
                            "Power bus checked. Continuity normal.";
                        break;
                    case CrewStation.Life:
                        T.finding = T.fault == 2 ? "Isolation test: port regulator seal leaking. Fit a replacement." :
                            "Oxygen loop checked. Cabin pressure " + T.pressure.ToString("0.0") + " kPa.";
                        break;
                    case CrewStation.Science:
                        MeasureOrbit();
                        break;
                    default:
                        T.finding = CrewStation.Name[n] + " self-test complete.";
                        break;
                }
                crew.message = T.finding;
                Log(T.finding);
            });
        }

        void MeasureOrbit()
        {
            if (Phase != ExpeditionPhase.Orbit)
            { T.finding = "Instrument self-test complete. Acquire from orbit."; return; }
            if (T.battery < 15)
            { T.finding = "The instrument needs a live electrical bus."; return; }
            if (D.objective == 1)
            {
                if (T.progress < .10 && T.orbitalPasses == 0)
                { T.finding = "Target below the sensor window. Coast and recapture the region."; return; }
                if (measurementCooldown > 0)
                { T.finding = "Instrument is integrating the previous product."; return; }
                if (T.orbitalPasses == 2)
                { T.finding = "Both orbital products stored. Downlink from Communications."; return; }
                if (T.stored + Stats.packet > Stats.storage)
                { T.finding = "Science recorder full. Downlink stored measurements."; return; }
                T.orbitalPasses++;
                T.stored += Stats.packet;
                T.measured = 1;
                measurementCooldown = 5;
                T.finding = "Orbital product " + T.orbitalPasses + "/2 — footprint elevation and slope saved.";
                return;
            }
            T.measured = 1;
            T.soilSlope = Grid.Slope(0, 0);
            measurementCooldown = 1;
            T.battery = Math.Max(0, T.battery - 1.6);
            T.finding = "Landing corridor checked. Site slope " + T.soilSlope.ToString("0.0") +
                "°, grid " + Grid.Spacing.ToString("0") + " m.";
        }

        void Repair(ExpeditionCrew crew, int target)
        {
            At(crew, target, () => {
                int matching = target == CrewStation.Engineering ? 1 : target == CrewStation.Life ? 2 : -1;
                if (T.fault != matching) { crew.message = "No fault found at this console."; return; }
                if ((T.inspectedMask & (1 << target)) == 0)
                { crew.message = "Run the diagnostic before replacing hardware."; return; }
                if (T.spares < 1) { crew.message = "The service locker has no spare units."; return; }
                T.spares--;
                T.fault = 0;
                T.repaired = true;
                Log(target == CrewStation.Engineering ? "Charge controller replaced. Generation restored." :
                    "Port seal replaced. Cabin pressure recovering.");
            });
        }

        void SurfaceOps(double dt)
        {
            T.progress = 0;
            T.link = true;
            T.sunlight = true;
            if ((D.objective == 2 || D.difficulty == 1) &&
                T.phaseTime > (D.objective == 2 ? 10 : 75) && !T.repairFaultOccurred)
            {
                // If the power fault is still live, wait to avoid hiding its diagnosis.
                if (T.fault == 0)
                {
                    T.repairFaultOccurred = true;
                    T.fault = 2;
                    T.inspectedMask &= ~(1 << CrewStation.Life);
                    Log("Cabin pressure decay detected. Isolate the fault at Life support.");
                }
            }
        }

        void Airlock(ExpeditionCrew crew)
        {
            if (Phase != ExpeditionPhase.Surface) { crew.message = "Airlock remains interlocked until touchdown."; return; }
            double hatchZ = T.z + 9.8;
            if (crew.outside)
            {
                if (Distance(crew.x, crew.z, T.x, hatchZ) > 6)
                { crew.message = "Return to the aft airlock at the lander."; return; }
                crew.outside = false;
                crew.station = CrewStation.Hatch;
                if(crew.rover) T.roverId = -1;
                crew.rover = false;
                crew.x = 0;
                crew.z = 5.45;
                crew.yaw = 180;
                moveAge[crew.id] = 0;
                crew.message = "Airlock sealed. Suit charging.";
                Log("Crew " + (crew.id + 1) + " returned to the cabin.");
                return;
            }
            At(crew, CrewStation.Hatch, () => {
                if (T.pressure < 65 || crew.oxygen < 24 || crew.suit < 18)
                { crew.message = "Prepare the suit and pressurize the lock before EVA."; return; }
                crew.outside = true;
                crew.x = T.x;
                crew.z = hatchZ;
                crew.yaw = 0;
                crew.station = -1;
                moveAge[crew.id] = 0;
                crew.message = "EVA active. Use the surface chart to locate the science beacons.";
                Log("Crew " + (crew.id + 1) + " began an EVA.");
            });
        }

        void Rover(ExpeditionCrew crew)
        {
            if (!crew.outside || string.IsNullOrEmpty(D.parts[ExpeditionSlots.Rover]))
            { crew.message = "The current assembly carries no rover for this EVA."; return; }
            if (crew.rover)
            {
                crew.rover = false;
                T.roverId = -1;
                crew.message = "Dismounted. The rover remains here.";
                return;
            }
            if (T.roverId >= 0 || Distance(crew.x, crew.z, T.roverX, T.roverZ) > 7)
            { crew.message = "Approach the parked rover when its seat is free."; return; }
            crew.x = T.roverX;
            crew.z = T.roverZ;
            crew.rover = true;
            T.roverId = crew.id;
            crew.message = "Rover engaged.";
        }

        void Sample(ExpeditionCrew crew, int index)
        {
            if (Phase != ExpeditionPhase.Surface || !crew.outside || crew.rover) return;
            if (index < 0 || index >= World.points.Length) return;
            var point = World.points[index];
            if (Distance(crew.x, crew.z, point.x, point.z) > 10)
            { crew.message = "Walk within 10 m of the survey beacon to measure."; return; }
            if (T.marks[index] != 0)
            { crew.message = "That field record is already stored."; return; }
            if (T.stored + Stats.packet > Stats.storage)
            { crew.message = "Recorder full. Downlink science to free storage."; return; }
            T.samples++;
            T.marks[index] = 1;
            T.lastSampleElevation = World.datumHeight + Grid.Height(crew.x, crew.z);
            T.lastSampleSlope = Grid.Slope(crew.x, crew.z);
            T.slopes[index] = T.lastSampleSlope;
            T.elevations[index] = T.lastSampleElevation;
            T.stored += Stats.packet;
            crew.suit = Math.Max(0, crew.suit - Stats.draw * .014);
            T.finding = point.observation;
            crew.message = "Field record saved: " + T.lastSampleSlope.ToString("0.0") + "° slope.";
            Log(point.name + " measured. Return to the ship for downlink.");
        }

        void Treat(ExpeditionCrew crew)
        {
            if (T.pressure < 70 || T.battery < 15 || !T.support)
            { crew.message = "Restore pressure and power before treating the crew."; return; }
            if (crew.health >= 99 && crew.suit >= 95 && crew.oxygen >= 95)
            { crew.message = "Suit and vital signs are nominal."; return; }
            crew.health = Math.Min(100,crew.health + 23);
            crew.suit = Math.Min(100,crew.suit + 34);
            crew.oxygen = Math.Min(100,crew.oxygen + 42);
            T.battery -= 5;
            crew.message = "First aid applied. Suit replenished.";
        }

        void Transmit()
        {
            int measurements = D.objective == 1 ? T.orbitalPasses : T.samples;
            int required = D.objective == 2 ? 1 : 2;
            if (measurements < required || T.stored <= 0 || T.transferring || !T.transmitter || !T.link || T.battery < 15)
            { T.status = "Downlink needs the required records, a clear Earth link, power and a live transmitter."; return; }
            if (D.objective == 2 && Phase == ExpeditionPhase.Surface && (!T.repairFaultOccurred || T.fault != 0 || T.pressure < 70))
            {
                T.status = "Recovery requires a repaired pressure seal and a pressurized cabin.";
                return;
            }
            if (Phase == ExpeditionPhase.Surface)
                foreach (var crew in view.crew)
                    if (crew.active && crew.outside && !crew.down)
                    { T.status = "Return all EVA crew to the cabin before the final downlink."; return; }
            transferPacket = T.stored;
            T.txRemaining = T.signalDelay + transferPacket / Math.Max(.01, Stats.bandwidth);
            T.transferring = true;
            Log("Science downlink started. Awaiting Earth acknowledgment.");
        }

        void AdvanceDownlink(double dt)
        {
            if (T.txRemaining > T.signalDelay && (!T.link || !T.transmitter || T.battery < 10))
            {
                T.transferring = false;
                Log("The downlink broke before all data cleared the ship.");
                return;
            }
            // Educational time compression keeps the simulated light-delay figure visible.
            double compression = D.world == 0 ? 2.5 : T.txRemaining > T.signalDelay ? 8 : 105;
            T.txRemaining = Math.Max(0, T.txRemaining - dt * compression);
            if (T.txRemaining > 0) return;
            T.transferring = false;
            T.delivered += transferPacket;
            T.stored -= transferPacket;
            Complete();
        }

        public static double Distance(double x, double z, double a, double b) => Math.Sqrt((x - a) * (x - a) + (z - b) * (z - b));
    }
}
