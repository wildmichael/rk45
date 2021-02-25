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
            // Switch from naive to SIMD implementation when --simd is passed
            Func<IList<(double, double[])>> solver = args.Length switch
            {
                > 0 when args[0] == "--simd" => SolveExponentialDecaySimd,
                _ => SolveExponentialDecayNaive,
            };

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
    }
}
