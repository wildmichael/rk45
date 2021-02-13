using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace rk45
{
    public static class SimdMathExtensions
    {
        private static readonly Vector256<double> NegZero = Vector256.Create(-0.0);

        private static readonly int TrueMask = Avx.MoveMask(Vector256<double>.AllBitsSet);

        // Loads an array of doubles into an array of vectors.
        public static IEnumerable<Vector256<double>> Load(this double[] @this, double padding = 0)
        {
            var c = Vector256<double>.Count;
            var n = @this.Length - c + 1;
            var i = 0;
            for (; i < n; i += c)
            {
                yield return LoadInternal(@this, i);
            }

            // add one last vector with padding if necessary
            if (i < @this.Length)
            {
                var tail = new double[c];
                var j = 0;
                for (; i < @this.Length; ++i, ++j)
                {
                    tail[j] = @this[i];
                }

                for (; j < Vector256<double>.Count; ++j)
                {
                    tail[j] = padding;
                }

                yield return LoadInternal(tail, 0);
            }
        }

        private unsafe static Vector256<double> LoadInternal(double[] a, int i)
        {
            fixed (double* ap = a)
            {
                return Avx.LoadVector256(ap + i);
            }
        }

        // Reverse of Load()
        public static IEnumerable<T> Unpack<T>(this IEnumerable<Vector256<T>> @this)
            where T: struct
        {
            foreach (var v in @this)
            {
                for (var i = 0; i < Vector256<T>.Count; ++i)
                {
                    yield return v.GetElement(i);
                }
            }
        }

        // Element-wise addition.
        public static IEnumerable<Vector256<double>> Add(
            this IEnumerable<Vector256<double>> @this,
            IEnumerable<Vector256<double>> other)
            => @this.Zip(other).Select(ab => Avx.Add(ab.First, ab.Second));

        // Element-wise multiplication.
        public static IEnumerable<Vector256<double>> Mul(
            this IEnumerable<Vector256<double>> @this,
            Vector256<double> other)
            => @this.Select(v => Avx.Multiply(v, other));

        // Multipyly-Add: a*@this[i]+c[i]
        public static IEnumerable<Vector256<double>> MulAdd(
            this IEnumerable<Vector256<double>> @this,
            Vector256<double> a,
            IEnumerable<Vector256<double>> c)
            => @this.Zip(c).Select(v => Fma.MultiplyAdd(a, v.First, v.Second));

        // Element-wise division.
        public static IEnumerable<Vector256<double>> Div(
            this IEnumerable<Vector256<double>> @this,
            IEnumerable<Vector256<double>> other)
            => @this.Zip(other).Select(ab => Avx.Divide(ab.First, ab.Second));

        // Element-wise absolute value.
        // see https://stackoverflow.com/a/5987631/159834
        public static Vector256<double> Abs(this Vector256<double> v)
            => Avx.AndNot(NegZero, v);

        // True if all values have all bits set (SIMD-variant of true), false otherwise
        // see https://habr.com/en/post/467689/
        public static bool All(this IEnumerable<Vector256<double>> @this)
            => @this.Select(v => Avx.MoveMask(v)).All(i => i == TrueMask);

        // Minimum value.
        public static double Min(this IEnumerable<Vector256<double>> @this)
        {
            var tmp = @this.Aggregate((x, y) => Avx.Min(x, y));
            var tmp1 = Avx.Min(tmp.GetLower(), tmp.GetUpper());
            var result = Math.Min(tmp1.GetElement(0), tmp1.GetElement(1));
            return result;
        }

        // Maximum value.
        public static double Max(this IEnumerable<Vector256<double>> @this)
        {
            var tmp = @this.Aggregate((x, y) => Avx.Max(x, y));
            var tmp1 = Avx.Max(tmp.GetLower(), tmp.GetUpper());
            var result = Math.Max(tmp1.GetElement(0), tmp1.GetElement(1));
            return result;
        }
    }
}
