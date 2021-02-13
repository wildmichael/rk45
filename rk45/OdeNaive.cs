using System;
using System.Collections.Generic;
using System.Linq;

namespace rk45
{
    public abstract class OdeNaive : OdeBase
    {
        public static IEnumerable<(double, double[])> Rk45(
            Func<double, IEnumerable<double>, IEnumerable<double>> func,
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
            var y = y0.ToArray();
            var t = t0;

            // Output at t0
            yield return (t, y);

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

                // Calculate k1 through k6
                var k1 = func(t + h * A1, y).Mul(h).ToArray();
                var k2 = func(t + h * A2, y.Add(k1.Mul(B21))).Mul(h).ToArray();
                var k3 = func(t + h * A3, y.Add(k1.Mul(B31)).Add(k2.Mul(B32))).Mul(h).ToArray();
                var k4 = func(t + h * A4, y.Add(k1.Mul(B41)).Add(k2.Mul(B42)).Add(k3.Mul(B43))).Mul(h).ToArray();
                var k5 = func(t + h * A5, y.Add(k1.Mul(B51)).Add(k2.Mul(B52)).Add(k3.Mul(B53)).Add(k4.Mul(B54))).Mul(h).ToArray();
                var k6 = func(t + h * A6, y.Add(k1.Mul(B61)).Add(k2.Mul(B62)).Add(k3.Mul(B63)).Add(k4.Mul(B64)).Add(k5.Mul(B65))).Mul(h).ToArray();

                // Calculate error estimate for each component
                var te = k1.Mul(Ct1).Add(
                    k2.Mul(Ct2)).Add(
                        k3.Mul(Ct3)).Add(
                            k4.Mul(Ct4)).Add(
                                k5.Mul(Ct5)).Add(
                                    k6.Mul(Ct6)).Select(Math.Abs);

                // If only single epsilon given, reduce
                if (epsilon.Length == 1)
                {
                    te = new[] { te.Max() };
                }

                // If error criterion is fulfilled, perform increment
                if (te.Zip(epsilon).All(x => x.First <= x.Second))
                {
                    y = y.Add(
                        k1.Mul(Ch1)).Add(
                            k2.Mul(Ch2)).Add(
                                k3.Mul(Ch3)).Add(
                                    k4.Mul(Ch4)).Add(
                                        k5.Mul(Ch5)).Add(
                                            k6.Mul(Ch6)).ToArray();
                    t += h;

#if DENSE_OUTPUT
                    // If required, yield result
                    if (doOutput)
                    {
                        doOutput = false;
                        tOutEnum.MoveNext();
                        tNextOut = tOutEnum.Current;
                        yield return (t, y);
                    }
#endif
                }

                // Adapt step size
                h *= 0.9 * Math.Pow(epsilon.Div(te).Min(), 1.0 / 5);
            } while (t < tEnd);

            // Output at tend
            yield return (t, y);
        }
    }
}
