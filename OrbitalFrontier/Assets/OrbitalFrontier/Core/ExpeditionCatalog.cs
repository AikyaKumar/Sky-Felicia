using System;
using System.Collections.Generic;

namespace OrbitalFrontier.Core
{
    [Serializable]
    public sealed class ExpeditionPart
    {
        public string id, name, description;
        public int slot;
        public double mass, cost, limit, draw, fuel, thrust, isp, generation, steady, wattHours, science, mb,
            rate, gear, oxygen, days;
    }

    [Serializable]
    public sealed class FieldPoint
    {
        public string name, observation, source;
        public double x, z;
    }

    [Serializable]
    public sealed class ExpeditionWorld
    {
        public string id, name, body, tag, subtitle, color, source, research, credit, description, heightFile, texture, rasterWindow;
        public int mapSize;
        public double latitude, longitude, datumHeight, spacing, gravity, radius, mu, spin, pressure, starDistance,
            budget, orbitLow, orbitHigh, days, fastDays, lightTime, arrivalDV, terminalDV, landingLimit,
            startingAlt, startingVertical, startingHorizontal;
        public FieldPoint[] points;
    }

    [Serializable]
    public sealed class Launcher
    {
        public string name, heritage;
        public double cost, moon, mars, highPenalty, fastPenalty;
    }

    [Serializable]
    public sealed class ExpeditionVehicle
    {
        public string name, description, photo;
        public string[] parts;
        public int world;
    }

    [Serializable]
    public sealed class ExpeditionCatalog
    {
        public ExpeditionWorld[] worlds;
        public ExpeditionVehicle[] vehicles;
        public ExpeditionPart[] parts;
        public Launcher[] launchers;

        public ExpeditionPart Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var part in parts)
                if (part.id == id) return part;
            return null;
        }
    }

    public static class ExpeditionSlots
    {
        public const int Hull = 0, Tank = 1, Engine = 2, Generation = 3, Battery = 4, Altimeter = 5,
            Radio = 6, Landing = 7, Life = 8, Entry = 9, Science = 10, Recorder = 11, Spares = 12, Rcs = 13, Rover = 14;
        public static readonly string[] Names = {
            "Pressure hull", "Tank", "Main engine", "Power generator", "Battery bank", "Landing sensor", "Antenna",
            "Landing gear", "Life support", "Entry shield", "Field instrument", "Data recorder", "Spares", "Attitude control", "Rover"
        };
    }

    [Serializable]
    public sealed class ExpeditionDesign
    {
        public int world, objective, difficulty, launcher, orbit, route, vehicle, endurance = 1;
        public string[] parts = new string[15];
        public int[] rotations = new int[15];

        public ExpeditionDesign Copy() => new ExpeditionDesign {
            world = world, objective = objective, difficulty = difficulty, launcher = launcher, orbit = orbit,
            route = route, vehicle = vehicle, endurance=endurance, parts = (string[])parts.Clone(), rotations = (int[])rotations.Clone()
        };
    }

    [Serializable]
    public sealed class ExpeditionBudget
    {
        public bool ready;
        public string[] issues, notes;
        public double dry, wet, capacity, hullLimit, cost, budget, fuel, reserve, plannedDV, arrivalDV, terminalDV,
            thrust, isp, twr, generation, peak, battery, load, storage, packet, draw, bandwidth, lifeDays,
            travelDays, sessionMinutes, science, gear, slopeLimit, suitSeconds, spares, rcs, orbitKm, centerY,
            shield, chargeRate, destinationGeneration;
    }

    public static class ExpeditionEngineering
    {
        static readonly double[] Axial = { .2, -.34, -2.2, 0, -.82, 1.45, .64, -1.3, .64, -1.68, .73, .32, -.54, .88, -.4 };

        public static ExpeditionBudget Evaluate(ExpeditionCatalog catalog, ExpeditionDesign d)
        {
            var w = catalog.worlds[d.world];
            var launcher = catalog.launchers[d.launcher];
            var stats = new ExpeditionBudget();
            var issues = new List<string>();
            var notes = new List<string>();
            double massMoment = 0;
            bool surface = d.objective != 1;
            for (int i = 0; i < d.parts.Length; i++)
            {
                var part = catalog.Get(d.parts[i]);
                if (part == null || part.slot != i)
                {
                    bool optional = i == ExpeditionSlots.Entry && (d.world == 0 || !surface) ||
                                    !surface && (i == ExpeditionSlots.Landing || i == ExpeditionSlots.Life) ||
                                    i == ExpeditionSlots.Rover;
                    if (!optional) issues.Add("Install " + ExpeditionSlots.Names[i].ToLowerInvariant() + ".");
                    continue;
                }
                stats.dry += part.mass;
                stats.cost += part.cost;
                stats.fuel += part.fuel;
                massMoment += (part.mass + part.fuel) * Axial[i];
                stats.thrust += part.thrust;
                stats.isp += part.isp;
                stats.battery += part.wattHours;
                stats.lifeDays += part.days;
                stats.suitSeconds += part.oxygen;
                if (i == ExpeditionSlots.Hull) stats.hullLimit = part.limit;
                if (i == ExpeditionSlots.Rcs) stats.rcs = part.limit;
                if (i == ExpeditionSlots.Entry) stats.shield = part.limit;
                if (i == ExpeditionSlots.Spares) stats.spares = part.limit;
                if (i == ExpeditionSlots.Recorder) stats.storage = part.limit;
                if (i == ExpeditionSlots.Landing) { stats.gear = part.gear; stats.slopeLimit = part.limit; }
                if (i == ExpeditionSlots.Radio) stats.bandwidth = part.rate;
                if (i == ExpeditionSlots.Science)
                {
                    stats.science = part.science;
                    stats.packet = part.mb;
                    stats.draw = part.draw;
                }
                if (i == ExpeditionSlots.Generation)
                {
                    stats.chargeRate = part.steady;
                    stats.generation = part.generation * (part.steady > 0 ? 1 : d.rotations[i] % 2 == 0 ? 1 : .68);
                }
                if (i == ExpeditionSlots.Hull || i == ExpeditionSlots.Engine || i == ExpeditionSlots.Life)
                    stats.load += part.draw;
                if (i == ExpeditionSlots.Science || i == ExpeditionSlots.Altimeter || i == ExpeditionSlots.Radio)
                    stats.peak += part.draw;
            }
            stats.wet = stats.dry + stats.fuel;
            stats.centerY = stats.wet > 0 ? massMoment / stats.wet : 0;
            stats.budget = w.budget * (d.difficulty != 0 ? .94 : 1);
            stats.orbitKm = d.orbit == 0 ? w.orbitLow : w.orbitHigh;
            stats.cost += launcher.cost + (d.route == 1 ? 95 : 0) + (d.orbit == 1 ? 70 : 0);
            stats.capacity = (d.world == 0 ? launcher.moon : launcher.mars) *
                (1 - (d.orbit == 1 ? launcher.highPenalty : 0)) * (1 - (d.route == 1 ? launcher.fastPenalty : 0));
            stats.twr = stats.wet > 0 ? stats.thrust / (w.gravity * stats.wet) : 0;
            stats.reserve = SpacecraftStats.DeltaV(stats.dry, stats.fuel, stats.isp);
            stats.arrivalDV = w.arrivalDV + (d.route == 1 ? 140 : 0) + (d.orbit == 1 ? 72 : 0);
            stats.terminalDV = surface ? w.terminalDV + (d.orbit == 1 ? d.world == 0 ? 44 : 29 : 0) : 0;
            stats.plannedDV = stats.arrivalDV + stats.terminalDV + (surface ? 170 : 30);
            stats.travelDays = d.route == 1 ? w.fastDays : w.days;
            stats.bandwidth *= d.world == 0 ? 1 : .41;
            stats.peak += stats.load;
            stats.sessionMinutes = d.endurance == 0 ? 14 : d.endurance == 2 ? 36 : 24;
            if (d.difficulty == 1) stats.sessionMinutes *= .82;
            stats.cost += d.endurance == 0 ? 0 : d.endurance == 2 ? 175 : 80;
            if (stats.capacity <= 0) issues.Add("Choose a launcher rated for this destination.");
            else if (stats.wet > stats.capacity) issues.Add("Reduce wet mass or choose a higher-capacity launcher.");
            if (stats.wet > stats.hullLimit && stats.hullLimit > 0) issues.Add("Wet mass exceeds the pressure hull rating.");
            if (stats.cost > stats.budget) issues.Add("The design exceeds the mission budget.");
            if (stats.reserve < stats.plannedDV && stats.isp > 0) issues.Add("Add enough propellant for capture and landing reserves.");
            if (surface && stats.twr < 1.70 && stats.thrust > 0) issues.Add("Surface, wet TWR must reach 1.70.");
            if (surface && stats.lifeDays < stats.travelDays + 2) issues.Add("Crew provisions must cover cruise plus a two-day reserve.");
            int products = d.objective == 2 ? 1 : 2;
            if (stats.storage < products * stats.packet + .1 && stats.packet > 0)
                issues.Add("Add storage for the required science products.");
            double illumination = w.starDistance * w.starDistance;
            stats.destinationGeneration = stats.chargeRate > 0 ? stats.generation : stats.generation * (d.world == 1 && surface ? .69 : 1) / illumination;
            if (stats.destinationGeneration < stats.load) notes.Add("Idle load exceeds destination generation. Battery endurance matters.");
            else if (stats.peak > stats.destinationGeneration) notes.Add("Active instruments draw from the battery at the destination.");
            if (d.orbit == 1) notes.Add("High orbit increases coverage and link windows while reducing delivery capacity.");
            if (d.route == 1) notes.Add("Fast transfer saves cruise time and raises injection and capture demands.");
            stats.issues = issues.ToArray();
            stats.notes = notes.ToArray();
            stats.ready = issues.Count == 0;
            return stats;
        }
    }

    public sealed class SurfaceGrid
    {
        readonly short[] heights;
        public double Extent { get; }
        public int Side { get; }
        public double Spacing { get; }

        public SurfaceGrid(ExpeditionWorld region, byte[] bytes)
        {
            Side = region.mapSize;
            Spacing = region.spacing;
            if (Side < 2 || Spacing <= 0 || bytes == null || bytes.Length != Side * Side * 2)
                throw new ArgumentException("Terrain array does not match " + region.name + ".");
            Extent = (Side - 1) * Spacing * .5;
            heights = new short[Side * Side];
            for (int i = 0; i < heights.Length; i++)
                heights[i] = unchecked((short)(bytes[i * 2] | bytes[i * 2 + 1] << 8));
        }

        public double Height(double east, double south)
        {
            double x = Orbit.Clamp((east + Extent) / Spacing, 0, Side - 1.0000001);
            double z = Orbit.Clamp((south + Extent) / Spacing, 0, Side - 1.0000001);
            int a = (int)x, b = (int)z, i = b * Side + a;
            double fx = x - a, fz = z - b;
            double near = heights[i] * (1 - fx) + heights[i + 1] * fx;
            double far = heights[i + Side] * (1 - fx) + heights[i + Side + 1] * fx;
            return .25 * (near * (1 - fz) + far * fz);
        }

        public double Slope(double x, double z)
        {
            double step = Spacing * .5;
            double sx = (Height(x + step, z) - Height(x - step, z)) / Spacing;
            double sz = (Height(x, z + step) - Height(x, z - step)) / Spacing;
            return Math.Atan(Math.Sqrt(sx * sx + sz * sz)) / Orbit.Rad;
        }
    }
}
