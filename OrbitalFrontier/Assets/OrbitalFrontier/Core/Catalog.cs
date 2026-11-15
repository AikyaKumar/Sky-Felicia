using System;
using System.Collections.Generic;

namespace OrbitalFrontier.Core
{
    [Serializable]
    public sealed class Part
    {
        public string id, name, description;
        public int slot, cost;
        public double mass, fuel, thrust, isp, gen, capacity, load, linkRate;
    }

    [Serializable]
    public sealed class CraftPreset
    {
        public string name, role, heritage, photo, source, description, historical, specSource;
        public string[] parts;
    }

    [Serializable]
    public sealed class SurveySite
    {
        public string name, note;
        public double lat, lon;
        public int side;
        // Signed half-metres relative to the 1,737.4 km lunar datum.
        public int[] elevation;
    }

    [Serializable]
    public sealed class BodyFact
    {
        public string name, mass, rotation, period, distance, temperature, air, notes, source;
        public double radius, gravity, escape;
    }

    [Serializable]
    public sealed class MissionDefinition
    {
        public string name, duration;
        public double targetPeriKm, targetLowerKm, targetUpperKm, maxCost, parCost, scanAltKm, maxSlantKm;
    }

    [Serializable]
    public sealed class GameCatalog
    {
        public Part[] parts;
        public CraftPreset[] presets;
        public SurveySite[] sites;
        public BodyFact[] bodies;
        public double moonRadius, moonMu, moonDay, siteDegreeStep;
        public MissionDefinition mission;

        public Part FindPart(string id)
        {
            foreach (Part part in parts)
                if (part.id == id)
                    return part;
            return null;
        }
    }

    public static class Slot
    {
        public const int Bus = 0, Tank = 1, Engine = 2, Solar = 3, Battery = 4, Lidar = 5, Antenna = 6;
        public static readonly string[] Names =
            { "Command bus", "Propellant tank", "Main engine", "Solar array", "Battery", "Laser altimeter", "Antenna" };
    }

    [Serializable]
    public sealed class CraftDesign
    {
        public string[] parts = new string[7];
        public int[] angles = new int[7];
        public int preset;

        public void Load(CraftPreset choice, int index)
        {
            Array.Copy(choice.parts, parts, parts.Length);
            Array.Clear(angles, 0, angles.Length);
            preset = index;
        }

        public void Install(Part part)
        {
            parts[part.slot] = part.id;
            angles[part.slot] = 0;
        }

        public void Detach(int slot)
        {
            parts[slot] = null;
            angles[slot] = 0;
        }

        public void Rotate(int slot)
        {
            angles[slot] = (angles[slot] + 1) % 4;
        }

        public CraftDesign Copy()
        {
            return new CraftDesign { parts = (string[])parts.Clone(), angles = (int[])angles.Clone(), preset = preset };
        }
    }

    [Serializable]
    public sealed class CraftStats
    {
        public double dry, wet, cost, thrust, isp, fuel, deltaV, lunarTwr, power, battery, idle, scan, radio,
            rate, center, centerX, centerZ, arrayFactor;
        public string[] issues;
        public bool ready;
    }

    public static class SpacecraftStats
    {
        public const double StandardGravity = 9.80665;
        static readonly double[] AxialStations = { 0.65, -0.42, -1.65, 0.15, -0.44, 0.85, 0.85 };
        static readonly double[] RadialStations = { 0, 0, 0, 0, 0.52, 0.76, 0.90 };

        public static CraftStats Calculate(GameCatalog catalog, CraftDesign design)
        {
            var result = new CraftStats();
            var issues = new List<string>();
            for (int i = 0; i < Slot.Names.Length; i++)
            {
                Part part = catalog.FindPart(design.parts[i]);
                if (part == null || part.slot != i)
                {
                    issues.Add("Install a " + Slot.Names[i].ToLowerInvariant() + ".");
                    continue;
                }
                result.dry += part.mass;
                result.cost += part.cost;
                result.fuel += part.fuel;
                result.thrust += part.thrust;
                result.isp += part.isp;
                result.battery += part.capacity;
                double mass = part.mass + part.fuel;
                result.center += mass * AxialStations[i];
                double angle = design.angles[i] * Math.PI / 2;
                result.centerX += mass * RadialStations[i] * Math.Cos(angle);
                result.centerZ += mass * RadialStations[i] * Math.Sin(angle);
                if (i == Slot.Solar)
                {
                    result.arrayFactor = design.angles[i] % 2 == 0 ? 1 : 0.72;
                    result.power = part.gen * result.arrayFactor;
                }
                if (i == Slot.Lidar)
                    result.scan = part.load;
                else if (i == Slot.Antenna)
                {
                    result.radio = part.load;
                    result.rate = part.linkRate;
                }
                else
                    result.idle += part.load;
            }

            result.wet = result.dry + result.fuel;
            if (result.wet > 0)
            {
                result.center /= result.wet;
                result.centerX /= result.wet;
                result.centerZ /= result.wet;
                result.lunarTwr = result.thrust / (result.wet * catalog.moonMu / (catalog.moonRadius * catalog.moonRadius));
            }
            result.deltaV = DeltaV(result.dry, result.fuel, result.isp);
            if (result.cost > catalog.mission.maxCost)
                issues.Add("Reduce build cost to the mission budget.");
            if (result.power < result.idle && issues.Count == 0)
                issues.Add("Solar output must cover idle demand in sunlight.");
            if (result.deltaV < 85 && issues.Count == 0)
                issues.Add("Carry at least 85 m/s of maneuver reserve.");
            result.issues = issues.ToArray();
            result.ready = issues.Count == 0;
            return result;
        }

        public static double DeltaV(double dry, double propellant, double isp)
        {
            return dry > 0 && propellant > 0 ? isp * StandardGravity * Math.Log((dry + propellant) / dry) : 0;
        }
    }
}
