using System.Collections.Generic;
using System.Linq;

namespace rk45
{
    public static class MathExtensions
    {
        public static IEnumerable<double> Add(this IEnumerable<double> @this, IEnumerable<double> other)
            => @this.Zip(other).Select(ab => ab.First + ab.Second);

        public static IEnumerable<double> Mul(this IEnumerable<double> @this, double other)
            => @this.Select(v => v * other);

        public static IEnumerable<double> Div(this IEnumerable<double> @this, IEnumerable<double> other)
            => @this.Zip(other).Select(ab => ab.First / ab.Second);
    }
}
