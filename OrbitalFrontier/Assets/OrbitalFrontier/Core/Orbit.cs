using System;

namespace OrbitalFrontier.Core
{
    [Serializable]
    public struct VectorD
    {
        public double x, y, z;
        public VectorD(double x, double y, double z) { this.x = x; this.y = y; this.z = z; }
        public double Norm => Math.Sqrt(Dot(this, this));
        public VectorD Unit => Norm > 1e-12 ? this / Norm : new VectorD();
        public static VectorD operator +(VectorD a, VectorD b) => new VectorD(a.x + b.x, a.y + b.y, a.z + b.z);
        public static VectorD operator -(VectorD a, VectorD b) => new VectorD(a.x - b.x, a.y - b.y, a.z - b.z);
        public static VectorD operator *(VectorD a, double b) => new VectorD(a.x * b, a.y * b, a.z * b);
        public static VectorD operator /(VectorD a, double b) => a * (1 / b);
        public static double Dot(VectorD a, VectorD b) => a.x * b.x + a.y * b.y + a.z * b.z;
        public static VectorD Cross(VectorD a, VectorD b) =>
            new VectorD(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x);
    }

    [Serializable]
    public struct OrbitState
    {
        public VectorD p, v;
        public OrbitState(VectorD position, VectorD velocity) { p = position; v = velocity; }
    }

    [Serializable]
    public struct OrbitalElements
    {
        public double altitude, speed, peri, apo, eccentricity, period, inclination, escape;
        public bool bound;
    }

    public static class Orbit
    {
        public const double Rad = Math.PI / 180;

        public static double Clamp(double v, double low, double high) => Math.Max(low, Math.Min(high, v));

        public static VectorD Direction(double latitude, double longitude)
        {
            double lat = latitude * Rad, lon = longitude * Rad;
            return new VectorD(Math.Cos(lat) * Math.Cos(lon), Math.Sin(lat), Math.Cos(lat) * Math.Sin(lon));
        }

        static VectorD Gravity(VectorD p, double mu)
        {
            double r = p.Norm;
            return p * (-mu / (r * r * r));
        }

        public static OrbitState Step(OrbitState state, double mu, double dt, VectorD thrustAcceleration)
        {
            VectorD first = Gravity(state.p, mu) + thrustAcceleration;
            VectorD p = state.p + state.v * dt + first * (0.5 * dt * dt);
            VectorD v = state.v + (first + Gravity(p, mu) + thrustAcceleration) * (dt * 0.5);
            return new OrbitState(p, v);
        }

        public static OrbitState Coast(OrbitState state, double mu, double seconds)
        {
            while (seconds > 1e-8)
            {
                double dt = Math.Min(5.0, seconds);
                state = Step(state, mu, dt, new VectorD());
                seconds -= dt;
            }
            return state;
        }

        public static VectorD AlongTrack(OrbitState state)
        {
            VectorD radial = state.p.Unit;
            return (state.v - radial * VectorD.Dot(radial, state.v)).Unit;
        }

        public static double Correction(OrbitState state, double mu, double desiredRadius)
        {
            double r = state.p.Norm;
            if (r <= desiredRadius + 100)
                return 0;
            double vr = VectorD.Dot(state.p.Unit, state.v);
            double ratio = r / desiredRadius;
            double target = Math.Sqrt((vr * vr + 2 * mu * (1 / desiredRadius - 1 / r)) / (ratio * ratio - 1));
            return target - VectorD.Dot(AlongTrack(state), state.v);
        }

        public static OrbitalElements Describe(OrbitState state, double mu, double radius)
        {
            double r = state.p.Norm, speed = state.v.Norm;
            VectorD h = VectorD.Cross(state.p, state.v);
            double e = (VectorD.Cross(state.v, h) / mu - state.p.Unit).Norm;
            double semilatus = VectorD.Dot(h, h) / mu;
            bool bound = e < 1 - 1e-10;
            double a = bound ? semilatus / (1 - e * e) : 0;
            return new OrbitalElements {
                altitude = (r - radius) / 1000, speed = speed, eccentricity = e,
                peri = (semilatus / (1 + e) - radius) / 1000,
                apo = bound ? (a * (1 + e) - radius) / 1000 : 0,
                period = bound ? 2 * Math.PI * Math.Sqrt(a * a * a / mu) : 0,
                inclination = Math.Acos(Clamp(-h.Unit.y, -1, 1)) / Rad,
                escape = Math.Sqrt(2 * mu / r), bound = bound
            };
        }

        public static VectorD[] Shape(OrbitState state, double mu, int count)
        {
            VectorD h = VectorD.Cross(state.p, state.v);
            VectorD eccentricity = VectorD.Cross(state.v, h) / mu - state.p.Unit;
            double e = eccentricity.Norm;
            VectorD u = e > 1e-7 ? eccentricity / e : state.p.Unit;
            VectorD w = VectorD.Cross(h.Unit, u);
            double p = VectorD.Dot(h, h) / mu;
            double extent = e < 1 ? Math.PI : Math.Acos(-1 / e) * 0.93;
            var points = new VectorD[count];
            for (int i = 0; i < points.Length; i++)
            {
                double angle = -extent + 2 * extent * i / (count - 1);
                double cos = Math.Cos(angle), sin = Math.Sin(angle);
                points[i] = (u * cos + w * sin) * (p / (1 + e * cos));
            }
            return points;
        }

        public static bool IsOccluded(VectorD observer, VectorD direction, double radius)
        {
            double distance = VectorD.Dot(observer, direction);
            return distance < 0 && VectorD.Dot(observer, observer) - distance * distance < radius * radius;
        }
    }
}
