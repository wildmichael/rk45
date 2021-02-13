using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace rk45
{
    public abstract class OdeSimd : OdeBase
    {
        // helper for brevity
        private static Vector256<double> V(double d) => Vector256.Create(d);

        static readonly Vector256<double>
            VB21 = V(B21),
            VB31 = V(B31), VB32 = V(B32),
            VB41 = V(B41), VB42 = V(B42), VB43 = V(B43),
            VB51 = V(B51), VB52 = V(B52), VB53 = V(B53),
            VB54 = V(B54),
            VB61 = V(B51), VB62 = V(B52), VB63 = V(B53),
            VB64 = V(B64), VB65 = V(B65);

        static readonly Vector256<double>
            VCh1 = V(Ch1), VCh2 = V(Ch2), VCh3 = V(Ch3),
            VCh4 = V(Ch4), VCh5 = V(Ch5), VCh6 = V(Ch6);

        static readonly Vector256<double>
            VCt1 = V(Ct1), VCt2 = V(Ct2), VCt3 = V(Ct3),
            VCt4 = V(Ct4), VCt5 = V(Ct5), VCt6 = V(Ct6);

        public static IEnumerable<(double, double[])> Rk45(
            Func<Vector256<double>, IEnumerable<Vector256<double>>, IEnumerable<Vector256<double>>> func,
            double[] y0,
            double t0,
            double tEnd,
            double[] tOut,
            double h,
            double hMax,
            double[] epsilon)
        {
#if VALIDATION
            // Argument validation
            if (y0.Length < 1)
            {
                throw new ArgumentException("The initial state array must contain at least one item", nameof(y0));
            }

            if (tEnd <= t0)
            {
                throw new ArgumentException("The end time must be larger than the start time", nameof(tEnd));
            }

            tOut = tOut.OrderBy(v => v).ToArray();
            if (tOut.Length > 0 && (tOut.First() <= t0 || tOut.Last() >= tEnd))
            {
                throw new ArgumentException(
                    $"The output times must be in the range ({nameof(t0)}, {nameof(tEnd)})]",
                    nameof(tOut));
            }

            if (epsilon.Length < 1 || (epsilon.Length > 1 && epsilon.Length != y0.Length))
            {
                throw new ArgumentException(
                    $"{nameof(epsilon)} must either contain a single element "
                    + "or have the same length as {nameof(y0)}.",
                    nameof(epsilon));
            }
#endif

            // Initialization
            var y = y0.Load().ToArray();
            var t = t0;
            var veps = epsilon.Load(epsilon[0]).ToArray();

            // Output at t0
            yield return (t, y.Unpack().ToArray());

#if DENSE_OUTPUT
            // Construct enumerator for output points
            var tOutEnum = tOut.Concat(
                Enumerable.Repeat(double.NegativeInfinity, int.MaxValue)).GetEnumerator();
            tOutEnum.MoveNext();
            var tNextOut = tOutEnum.Current;
            var doOutput = false;
#endif

            // Perform iterations
            do
            {
                // Clamp h to not exceed hmax and not overshoot tend
                h = Math.Min(Math.Min(h, hMax), tEnd - t);

#if DENSE_OUTPUT
                // Clamp h in case it would overshoot next output time.
                // Note that this can (and should) be improved in such a way
                // that the step size is not adapted but the intermediate value
                // for the output instant is interpolated using a cubic Hermite
                // spline constructed from y(t), y(t+h), f(t, y(t)) and
                // f(t+h, y(t+h)).
                var dtOut = tNextOut - t;
                if (dtOut > 0 && dtOut < h)
                {
                    h = dtOut;
                    doOutput = true;
                }
#endif

                var vh = Vector256.Create(h);
                // Calculate k1 through k6
                var k1 = func(V(t + h * A1), y).Mul(vh).ToArray();
                var k2 = func(V(t + h * A2), k1.MulAdd(VB21, y)).Mul(vh).ToArray();
                var k3 = func(V(t + h * A3), k2.MulAdd(VB32, k1.MulAdd(VB31, y))).Mul(vh).ToArray();
                var k4 = func(V(t + h * A4), k3.MulAdd(VB43, k2.MulAdd(VB42, k1.MulAdd(VB41, y)))).Mul(vh).ToArray();
                var k5 = func(V(t + h * A5), k4.MulAdd(VB54, k3.MulAdd(VB53, k2.MulAdd(VB52, k1.MulAdd(VB51, y))))).Mul(vh).ToArray();
                var k6 = func(V(t + h * A6), k5.MulAdd(VB65, k4.MulAdd(VB64, k3.MulAdd(VB63, k2.MulAdd(VB62, k1.MulAdd(VB61, y)))))).Mul(vh).ToArray();

                // Calculate error estimate for each component
                var te = k1.MulAdd(VCt1,
                    k2.MulAdd(VCt2,
                        k3.MulAdd(VCt3,
                            k4.MulAdd(VCt4,
                                k5.MulAdd(VCt5,
                                    k6.Mul(VCt6)))))).Select(v => v.Abs());

                // If only single epsilon given, reduce
                if (epsilon.Length == 1)
                {
                    te = new[]{ V(te.Max()) };
                }

                // If error criterion is fulfilled, perform increment
                if (te.Zip(veps).Select(x =>
                        Avx.CompareLessThanOrEqual(x.First, x.Second)).All())
                {
                    y = k6.MulAdd(VCh6,
                        k5.MulAdd(VCh5,
                            k4.MulAdd(VCh4,
                                k3.MulAdd(VCh3,
                                    k2.MulAdd(VCh2,
                                        k1.MulAdd(VCh1, y)))))).ToArray();
                    t += h;

#if DENSE_OUTPUT
                    // If required, yield result
                    if (doOutput)
                    {
                        doOutput = false;
                        tOutEnum.MoveNext();
                        tNextOut = tOutEnum.Current;
                        yield return (t, y.Unpack().ToArray());
                    }
#endif
                }

                // Adapt step size
                h *= 0.9 * Math.Pow(veps.Div(te).Min(), 1.0/5);
            } while (t < tEnd);

            // Output at tend
            yield return (t, y.Unpack().ToArray());
        }
    }
}
