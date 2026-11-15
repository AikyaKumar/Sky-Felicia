using System;
using System.Collections.Generic;

namespace OrbitalFrontier.Core
{
    [Serializable]
    public sealed class SurveyProduct
    {
        public int level, side;
        public double low, high, slope90, meanSlope, volume, cellKm;
        public double[] height, slope;
    }

    public static class Survey
    {
        public static SurveyProduct Analyze(GameCatalog catalog, SurveySite site, int level)
        {
            int stride = level == 2 ? 1 : 3;
            int n = site.side / stride;
            double spacing = catalog.moonRadius * catalog.siteDegreeStep * Orbit.Rad * stride;
            double east = spacing * Math.Cos(site.lat * Orbit.Rad);
            var product = new SurveyProduct {
                level = level, side = n,
                height = new double[n * n], slope = new double[n * n],
                low = double.PositiveInfinity, high = double.NegativeInfinity,
                cellKm = spacing / 1000, volume = n * n * 0.004
            };
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    double sum = 0;
                    for (int dy = 0; dy < stride; dy++)
                        for (int dx = 0; dx < stride; dx++)
                            sum += site.elevation[(y * stride + dy) * site.side + x * stride + dx] * 0.5;
                    double value = sum / (stride * stride);
                    product.height[y * n + x] = value;
                    product.low = Math.Min(product.low, value);
                    product.high = Math.Max(product.high, value);
                }
            }
            var interior = new List<double>();
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    int l = Math.Max(0, x - 1), r = Math.Min(n - 1, x + 1);
                    int t = Math.Max(0, y - 1), b = Math.Min(n - 1, y + 1);
                    double gx = (product.height[y * n + r] - product.height[y * n + l]) / ((r - l) * east);
                    double gy = (product.height[b * n + x] - product.height[t * n + x]) / ((b - t) * spacing);
                    double slope = Math.Atan(Math.Sqrt(gx * gx + gy * gy)) / Orbit.Rad;
                    product.slope[y * n + x] = slope;
                    if (x > 0 && x < n - 1 && y > 0 && y < n - 1)
                    {
                        interior.Add(slope);
                        product.meanSlope += slope;
                    }
                }
            }
            interior.Sort();
            product.meanSlope /= interior.Count;
            double k = (interior.Count - 1) * 0.9;
            int lo = (int)Math.Floor(k), hi = (int)Math.Ceiling(k);
            product.slope90 = interior[lo] + (interior[hi] - interior[lo]) * (k - lo);
            return product;
        }
    }
}
