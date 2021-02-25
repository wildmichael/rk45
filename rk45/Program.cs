using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace rk45
{
    class Program
    {
        static void Main(string[] args)
        {
            var problem = "masses";
            var useSimd = false;

            foreach (var a in args)
            {
                switch (a)
                {
                    case "--simd": useSimd = true; break;
                    case "masses":
                    case "decay": problem = a; break;
                    default:
                        System.Console.Error.WriteLine($"Error: Unknown argument \"{a}\".");
                        break;
                }
            }

            Func<IList<(double, double[])>> solver = ((problem, useSimd)) switch
            {
                ("decay", false) => SolveExponentialDecayNaive,
                ("decay", true) => SolveExponentialDecaySimd,
                ("masses", false) => SolveCoupledMassesNaive,
                ("masses", true) => SolveCoupledMassesSimd,
                _ => throw new InvalidOperationException(),
            };

            Console.Error.WriteLine($"Solving problem: {problem}, simd: {useSimd}");

            // Warmup
            var result = solver();

            // Run for 20 rounds
            const double nRounds = 20;
            var total = 0L;
            System.Diagnostics.Stopwatch timer = new();

            for (var i = 0; i < nRounds; ++i)
            {
                timer.Start();
                _ = solver();
                timer.Stop();
                total += timer.ElapsedMilliseconds;
                timer.Reset();
                Console.Error.Write('.');
            }

            // Print out results
            Console.Error.WriteLine($"\nAverage wall clock time: {total / nRounds} ms");

            var yNames = Enumerable.Range(0, result[0].Item2.Length)
                .Select(i => "y" + i.ToString());
            Console.WriteLine($"t\t{string.Join('\t', yNames)}");

            foreach (var (i, row) in result.Select((r, i) => (i, r)))
            {
                Console.WriteLine($"{row.Item1:G}\t"
                    + $"{string.Join('\t', row.Item2.Select(v => v.ToString("G")))}");
            }
        }

        public static IList<(double, double[])> SolveExponentialDecayNaive()
            => OdeNaive.Rk45(
                // Right-hand-side function: f(t, y) = -0.5 y
                func: (double t, IEnumerable<double> y) => y.Mul(-0.5),
                y0: new[] { 1.0, 100, 51, 35},
                t0: 0,
                tEnd: 10,
                tOut: new[] { 1.0, 2, 3, 4, 5, 6, 7, 8, 9, },
                h: 1e-5,
                hMax: 1e-2,
                epsilon: new[] { 1e-7 }).ToList();

        public static IList<(double, double[])> SolveCoupledMassesNaive()
        {
            IEnumerable<double> GetCoupledMassesRhs(double x1, double x2, double v1, double v2)
            {
                const double b1 = 0.0, b2 = 0.0;
                const double m1 = 5.0, m2 = 1.2;
                const double k1 = 1e1, k2 = 3e0;
                const double L1 = 3.0, L2 = 9.0;

                yield return v1;
                yield return v2;
                yield return (-b1 * v1 - k1 * (x1 - L1) + k2 * (x2 - x1 - L2)) / m1;
                yield return (-b2 * v2 - k2 * (x2 - x1 - L2)) / m2;
            }

            return OdeNaive.Rk45(
                func: (double t, IEnumerable<double> y) =>
                {
                    var e = y.GetEnumerator(); e.MoveNext();
                    var x1 = e.Current; e.MoveNext();
                    var x2 = e.Current; e.MoveNext();
                    var v1 = e.Current; e.MoveNext();
                    var v2 = e.Current; e.MoveNext();
                    return GetCoupledMassesRhs(x1, x2, v1, v2);
                },
                y0: new[] { 0.0, 11.0, 0.0, 0.0 },
                t0: 0,
                tEnd: 50,
                tOut: Enumerable.Range(0, 498).Select(i => (i + 1) / 10.0).ToArray(),
                h: 1e-5,
                hMax: 1e-2,
                epsilon: new[] { 1e-7 }).ToList();
        }

        static readonly Vector256<double> oneHalf = Vector256.Create(-0.5);

        public static IList<(double, double[])> SolveExponentialDecaySimd()
            => OdeSimd.Rk45(
                // Right-hand-side function: f(t, y) = -0.5 y
                func: (Vector256<double> t, IEnumerable<Vector256<double>> y) =>
                    y.Select(yi => Avx.Multiply(oneHalf, yi)),
                y0: new[] { 1.0, 100, 51, 35},
                t0: 0,
                tEnd: 10,
                tOut: new[] { 1.0, 2, 3, 4, 5, 6, 7, 8, 9, },
                h: 1e-5,
                hMax: 1e-2,
                epsilon: new[] { 1e-7 }).ToList();

        public static IList<(double, double[])> SolveCoupledMassesSimd()
        {
            // negative values!
            Vector128<double> b = Vector128.Create(0.0, 0.0);
            Vector128<double> m = Vector128.Create(5.0, 1.2);
            Vector128<double> k1 = Vector128.Create(1e1, 0.0 /* always zero */);
            Vector128<double> k2 = Vector128.Create(3e0);
            Vector128<double> L1 = Vector128.Create(3.0),
                              L2 = Vector128.Create(9.0);

            IEnumerable<Vector256<double>> GetCoupledMassesRhs(IEnumerable<Vector256<double>> y)
            {
                var y0 = y.First();
                var x = y0.GetLower();
                var v = y0.GetUpper();

                var dx1 = Avx.Subtract(Vector128.Create(x.GetElement(0)), L1);
                var dx2 = Avx.Add(Avx.HorizontalSubtract(x, x), L2);
                var tmp1 = Avx.Multiply(k2, dx2);
                var tmp2 = Fma.MultiplySubtractAdd(k1, dx1, tmp1);
                var tmp3 = Fma.MultiplySubtractNegated(b, v, tmp2);
                var result = Vector256.Create(v, Avx.Divide(tmp3, m));

                yield return result;
            }

            return OdeSimd.Rk45(
                func: (t, y) => GetCoupledMassesRhs(y),
                y0: new[] { 0.0, 11.0, 0.0, 0.0 },
                t0: 0,
                tEnd: 50,
                tOut: Enumerable.Range(0, 498).Select(i => (i + 1) / 10.0).ToArray(),
                h: 1e-5,
                hMax: 1e-2,
                epsilon: new[] { 1e-7 }).ToList();
        }
    }
}
