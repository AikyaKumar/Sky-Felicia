using System;

namespace OrbitalFrontier.Core
{
    public sealed partial class ExpeditionSession
    {
        double manualEast, manualSouth;
        bool arrivalReady;

        void Ascent(double dt)
        {
            double target = heldStage == 0 ? 9 : 8;
            double active = Math.Min(dt, Math.Max(0, target - T.progress));
            T.progress += active;
            T.altitude += active * (heldStage == 0 ? 5200 : 32000);
            T.speed += active * (heldStage == 0 ? 145 : 790);
            T.engineTemp = 294 + 125 * Math.Min(1, T.progress / target);
            if (T.progress >= target && !T.staged)
            {
                T.staged = true;
                Log(heldStage == 0 ? "First-stage cutoff. Separate at the helm." : "Injection corridor reached. Separate the transfer stage.");
            }
        }

        void StageRocket()
        {
            if (Phase != ExpeditionPhase.Ascent || !T.staged) return;
            T.stageCount++;
            heldStage++;
            T.staged = false;
            T.progress = 0;
            if (heldStage == 1) { Log("Booster separated. Transfer stage ignited."); return; }
            T.cruiseDays = 0;
            arrivalReady = false;
            warp = 1;
            SetPhase(ExpeditionPhase.Cruise);
            Log("Transfer stage separated. Inspect systems, then select transfer coast at Navigation.");
        }

        void SetCoast(int option)
        {
            if (Phase != ExpeditionPhase.Cruise && Phase != ExpeditionPhase.Orbit && Phase != ExpeditionPhase.Surface) return;
            warp = option == 0 ? 1 : option == 1 ? 40 : 4;
            if (Phase == ExpeditionPhase.Cruise)
                Log(warp > 1 ? "Accelerated transfer coast engaged." : "Cruise set to normal operations rate.");
            else if (Phase == ExpeditionPhase.Orbit)
                T.status = warp > 1 ? "Accelerated orbital coast." : "Orbit set to normal operations rate.";
        }

        void Cruise(double dt)
        {
            if (T.captureBurn || arrivalReady) return;
            double duration = warp > 4 ? 20 : 95;
            T.cruiseDays = Math.Min(Stats.travelDays, T.cruiseDays + (Stats.travelDays / duration) * dt);
            T.progress = T.cruiseDays / Stats.travelDays;
            T.distance = (D.world == 0 ? 384400 : 470000000) * (1 - T.progress);
            T.radiation = (D.world == 1 ? .30 : .055) * T.cruiseDays;
            if (!secondFault && T.progress >= .44)
            {
                secondFault = true;
                T.fault = 1;
                Log("Power controller fault slowed charge. Walk to Engineering to diagnose it.");
            }
            if (T.progress < 1) return;
            arrivalReady = true;
            T.progress = 1;
            T.altitude = Stats.orbitKm * 1000;
            T.distance = 0;
            T.speed = Math.Sqrt(World.mu / (World.radius + T.altitude)) + Stats.arrivalDV;
            Log("Arrival corridor reached. Start the capture burn at Navigation.");
        }

        void Capture()
        {
            if (Phase != ExpeditionPhase.Cruise || !arrivalReady || T.captureBurn || T.deltaV < Stats.arrivalDV)
            { T.status = "Capture requires the arrival corridor and the planned propellant."; return; }
            if (T.battery < 20) { T.status = "Restore the power bus before engine ignition."; return; }
            T.captureBurn = true;
            captureRemaining = Stats.arrivalDV;
            Log("Capture burn started.");
        }

        void AdvanceCapture(double dt)
        {
            double delivered = Burn(Math.Min(captureRemaining, Stats.arrivalDV * dt / 5.5));
            captureRemaining -= delivered;
            T.speed -= delivered;
            T.engineTemp += dt * 26;
            if (captureRemaining > .001 && T.fuel > .001) return;
            if (captureRemaining > .001) { Fail("Propellant depleted during capture."); return; }
            T.captureBurn = false;
            var pos = Orbit.Direction(World.latitude, World.longitude - 21) * (World.radius + Stats.orbitKm * 1000);
            var north = new VectorD(0, 1, 0);
            var east = VectorD.Cross(pos.Unit, north).Unit;
            double orbitalSpeed = Math.Sqrt(World.mu / pos.Norm);
            T.orbit = new OrbitState(pos, east * orbitalSpeed);
            T.progress = 0;
            ticks = orbits = 0;
            warp = 1;
            SetPhase(ExpeditionPhase.Orbit);
            RefreshOrbit();
            Log("Capture complete. Inspect the landing sensor at Science or collect an orbital survey pass.");
        }

        double Burn(double deltaV)
        {
            if (deltaV <= 0 || T.fuel <= 0 || Stats.isp <= 0) return 0;
            double exhaust = Stats.isp * SpacecraftStats.StandardGravity;
            double wet = Stats.dry + T.fuel;
            double used = Math.Min(T.fuel, wet * (1 - Math.Exp(-deltaV / exhaust)));
            T.fuel -= used;
            return exhaust * Math.Log(wet / (wet - used));
        }

        void OrbitOps(double dt)
        {
            double rate = warp == 40 ? 110 : warp > 1 ? 70 : 25;
            double angularTime = dt * rate;
            orbits += angularTime;
            ticks += dt;
            T.orbit = Orbit.Coast(T.orbit, World.mu, angularTime);
            var elements = Orbit.Describe(T.orbit, World.mu, World.radius);
            T.altitude = elements.altitude * 1000;
            T.speed = elements.speed;
            var target = Orbit.Direction(World.latitude, World.longitude + T.time / World.spin * 360);
            var spacecraft = T.orbit.p.Unit;
            double angle = Math.Acos(Orbit.Clamp(VectorD.Dot(target, spacecraft), -1, 1)) / Orbit.Rad;
            T.sunlight = !Orbit.IsOccluded(T.orbit.p, Orbit.Direction(8, 75), World.radius);
            T.link = !Orbit.IsOccluded(T.orbit.p, Orbit.Direction(12, 12), World.radius);
            T.progress = Math.Max(0, 1 - angle / 40);
            double period = Math.Max(1, elements.period);
            T.passEta = T.progress > .16 ? 0 : Math.Max(0, period - (orbits % period)) / rate;
            if (ticks >= .5) { ticks = 0; RefreshOrbit(); }
            if (elements.altitude <= 0) Fail("Orbit intersects the reference surface.");
        }

        void RefreshOrbit()
        {
            var e = Orbit.Describe(T.orbit, World.mu, World.radius);
            T.orbitalPeri = e.peri;
            T.orbitalApo = e.apo;
            T.orbitalPeriod = e.period;
            T.track = Orbit.Shape(T.orbit, World.mu, 96);
        }

        void TrimOrbit(double a)
        {
            if (Phase != ExpeditionPhase.Orbit) return;
            double wanted = Orbit.Clamp(a, -35, 35);
            if (Math.Abs(wanted) < .2) return;
            double speed = Burn(Math.Abs(wanted));
            T.orbit.v += Orbit.AlongTrack(T.orbit) * (speed * Math.Sign(wanted));
            RefreshOrbit();
            Log("Trim delivered " + speed.ToString("0.0") + " m/s. Check approach and perilune.");
        }

        void Descent()
        {
            if (Phase != ExpeditionPhase.Orbit || D.objective == 1) return;
            if (T.measured < 1) { T.status = "Measure the landing corridor at Science first."; return; }
            if (T.orbitalPeri < 25 || T.deltaV < Stats.terminalDV + 90 || T.battery < 25)
            { T.status = "Descent needs a safe orbit, propellant reserve and a live bus."; return; }
            if (D.objective == 2 && !T.repaired) { T.status = "Finish the power controller repair before committing to descent."; return; }
            Burn(Stats.terminalDV);
            T.x = -184; T.z = 82;
            T.altitude = World.startingAlt;
            T.vertical = World.startingVertical;
            T.vx = World.startingHorizontal;
            T.vz = -8;
            T.speed = Math.Sqrt(T.vertical*T.vertical + T.vx*T.vx + T.vz*T.vz);
            T.throttle = 0;
            T.phaseTime = 0;
            T.descentElapsed = 0;
            T.landingBurn = true;
            warp = 1;
            manualEast = manualSouth = 0;
            SetPhase(ExpeditionPhase.Descent);
            Log(D.world == 1
                ? "Entry equipment cleared atmospheric braking. Terminal powered descent begins."
                : "Deorbit maneuver complete. Terminal powered descent begins.");
        }

        void ChangeAssist(bool enabled)
        {
            if (Phase != ExpeditionPhase.Descent) return;
            T.assist = enabled;
            T.status = enabled ? "Descent guidance engaged." : "Manual descent. Monitor vertical and lateral speed.";
        }

        void SetThrottle(double throttle, double east, double south)
        {
            if (Phase != ExpeditionPhase.Descent) return;
            T.assist = false;
            T.throttle = Orbit.Clamp(throttle, 0, 1);
            manualEast = Orbit.Clamp(east, -1, 1);
            manualSouth = Orbit.Clamp(south, -1, 1);
        }

        void Land(double dt)
        {
            if (D.world == 1 && T.descentElapsed < 5)
            {
                T.descentElapsed += dt;
                T.progress = T.descentElapsed / 5;
                T.engineTemp += dt * (79 / Math.Max(1, Stats.shield));
                return;
            }
            T.descentElapsed += dt;
            double floor = Grid.Height(T.x, T.z);
            double agl = T.altitude - floor;
            T.soilSlope = Grid.Slope(T.x, T.z);
            double wet = Stats.dry + T.fuel;
            double aMax = Stats.thrust / wet * (T.engineTemp > 1030 ? .73 : 1);
            double maxDecel = Math.Max(.3, aMax - World.gravity);
            if (T.assist)
            {
                double blendDistance = Math.Max(12, maxDecel * 1.7);
                double goal = -Math.Min(23, .78 + Math.Max(0, agl - .8) * .45);
                double brake = Math.Sqrt(.6 + 2 * maxDecel * .37 * Math.Max(0, agl - blendDistance));
                if (agl > blendDistance) goal = Math.Max(goal, -brake - blendDistance * .37);
                double total = World.gravity + (goal - T.vertical) * (agl < 40 ? 2.4 : 1.35);
                T.throttle = Orbit.Clamp(total / aMax, 0, 1);
            }
            double east = T.assist || T.dampers ? Orbit.Clamp(-T.vx * (T.assist ? .59 : .45) + (T.assist ? -T.x * .017 : 0), -Stats.rcs, Stats.rcs) : 0;
            double south = T.assist || T.dampers ? Orbit.Clamp(-T.vz * (T.assist ? .59 : .45) + (T.assist ? -T.z * .017 : 0), -Stats.rcs, Stats.rcs) : 0;
            if (!T.assist) { east += manualEast * Stats.rcs; south += manualSouth * Stats.rcs; }
            double acceleration = aMax * (T.battery > 10 ? T.throttle : 0);
            double propellant = Math.Min(T.fuel, acceleration * wet / (Stats.isp * SpacecraftStats.StandardGravity) * dt);
            T.fuel -= propellant;
            if (propellant <= 0) { acceleration = 0; east = 0; south = 0; }
            T.vertical += (acceleration - World.gravity - (D.world == 1 ? .0034 * T.vertical : 0)) * dt;
            T.vx += east * dt; T.vz += south * dt;
            double edge = Grid.Extent - 8;
            T.x = Orbit.Clamp(T.x + T.vx * dt, -edge, edge);
            T.z = Orbit.Clamp(T.z + T.vz * dt, -edge, edge);
            T.altitude += T.vertical * dt;
            T.speed = Math.Sqrt(T.vertical*T.vertical + T.vx*T.vx + T.vz*T.vz);
            T.engineTemp += T.throttle * dt * 5.5;
            if (T.altitude > 4200) { Fail("The lander climbed beyond the terminal descent envelope."); return; }
            double ground = Grid.Height(T.x, T.z);
            if (T.altitude <= ground)
            {
                T.touchdownV = Math.Abs(T.vertical);
                T.touchdownHorizontal = Math.Sqrt(T.vx*T.vx + T.vz*T.vz);
                T.touchdownSlope = Grid.Slope(T.x, T.z);
                if (T.touchdownV > World.landingLimit * Stats.gear || T.touchdownHorizontal > 2.7 * Stats.gear ||
                    T.touchdownSlope > Stats.slopeLimit)
                {
                    Fail("Hard landing — vertical " + T.touchdownV.ToString("0.0") + " m/s, lateral " +
                        T.touchdownHorizontal.ToString("0.0") + " m/s, slope " + T.touchdownSlope.ToString("0.0") + "°.");
                    return;
                }
                T.altitude = ground;
                T.throttle = T.vx = T.vz = T.vertical = T.speed = 0;
                T.link = true;
                T.sunlight = true;
                T.roverX = T.x + 4;
                T.roverZ = T.z + 10;
                T.roverYaw = 0;
                T.roverId = -1;
                T.landingBurn = false;
                SetPhase(ExpeditionPhase.Surface);
                Log("Touchdown. Walk aft to the airlock for EVA.");
            }
        }

        void Abort()
        {
            if (Phase != ExpeditionPhase.Descent) return;
            if (T.altitude - Grid.Height(T.x, T.z) < 85 || T.deltaV < 160)
            { T.status = "Abort envelope requires at least 85 m AGL and 160 m/s reserve."; return; }
            Burn(155);
            T.throttle = T.vx = T.vz = T.vertical = 0;
            T.landingBurn = false;
            SetPhase(ExpeditionPhase.Orbit);
            Log("Terminal run aborted. Lander returned to the pre-descent orbit state.");
        }
    }
}
