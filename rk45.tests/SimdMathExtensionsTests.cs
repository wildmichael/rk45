using System.Linq;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using FluentAssertions;
using Xunit;

namespace rk45.tests
{
    public class SimdMathExtensionsTests
    {
        readonly Vector256<double>[] a = new[]{
                Vector256.Create(50.0, 20, 30, 80),
                Vector256.Create(30.0, 40, 10, 20),
            };

        readonly Vector256<double>[] b = new[]{
                Vector256.Create(5.0, 15, 3, 70),
                Vector256.Create(3.0, 30, 1, 2),
            };

        [Fact]
        public void VAll_Case_All_True_Should_Return_True()
        {
            var result = a
                .Zip(b)
                .Select(x => Avx.CompareLessThanOrEqual(x.Second, x.First))
                .All();
            result.Should().BeTrue();
        }

        [Fact]
        public void Vall_Case_Not_All_True_Should_Return_False()
        {
            b[1] = Vector256.Create(3.0, 4, 100, 2);

            a
                .Zip(b)
                .Select(x => Avx.CompareLessThanOrEqual(x.Second, x.First))
                .All()
                .Should().BeFalse();
        }

        [Fact]
        public void VMin_Returns_Minimum_Element()
        {
            var result = a.Min();
            result.Should().Be(10);
        }

        [Fact]
        public void Load_Works()
        {
            var x = new[] { 1.0, 2, 3, 4, 5, 6 };
            var t = new[] { Vector256.Create(1.0, 2, 3, 4), Vector256.Create(5.0, 6, 0, 0) };

            var y = x.Load().ToArray();

            y.Should().BeEquivalentTo(t);
        }

        [Fact]
        public void Unpack_Works()
        {
            var x = new[] { Vector256.Create(1.0, 2, 3, 4), Vector256.Create(5.0, 6, 0, 0) };
            var t = new[] { 1.0, 2, 3, 4, 5, 6, 0, 0 };

            var y = x.Unpack().ToArray();

            y.Should().BeEquivalentTo(t);
        }

        [Fact]
        public void Add_Should_Add()
        {
            var x = new[] { Vector256.Create(1.0, 2, 3, 4), Vector256.Create(5.0, 6, 0, 0) };
            var y = new[] { Vector256.Create(7.0, 8, 9, 10), Vector256.Create(11.0, 12, 0, 0) };

            var z = x.Add(y).Unpack().ToArray();
            z.Should().BeEquivalentTo(8.0, 10, 12, 14, 16, 18, 0, 0);
        }

        [Fact]
        public void Mul_Should_Multiply()
        {
            var x = new[] { Vector256.Create(1.0, 2, 3, 4), Vector256.Create(5.0, 6, 0, 0) };

            var z = x.Mul(Vector256.Create(5.0)).Unpack().ToArray();
            z.Should().BeEquivalentTo(5.0, 10, 15, 20, 25, 30, 0, 0);
        }

        [Fact]
        public void MulAdd_Should_Multiply_And_Add()
        {
            var x = new[] { Vector256.Create(1.0, 2, 3, 4), Vector256.Create(5.0, 6, 0, 0) };
            var y = new[] { Vector256.Create(7.0, 8, 9, 10), Vector256.Create(11.0, 12, 0, 0) };

            var z = x.MulAdd(Vector256.Create(5.0), y).Unpack().ToArray();
            z.Should().BeEquivalentTo(12.0, 18, 24, 30, 36, 42, 0, 0);
        }

        [Fact]
        public void Div_Should_Divide()
        {
            var x = new[] { Vector256.Create(1.0, 2, 3, 4), Vector256.Create(5.0, 6, 0, 0) };
            var y = new[] { Vector256.Create(7.0, 8, 9, 10), Vector256.Create(11.0, 12, 1, 1) };

            var z = x.Div(y).Unpack().ToArray();
            z.Should().BeEquivalentTo(1.0/7, 2.0/8, 3.0/9, 4.0/10, 5.0/11, 6.0/12, 0, 0);
        }

        [Fact]
        public void VAbs_Should_Return_Absolute()
        {
            var x = new[] { Vector256.Create(1.0, -2, 3, -4), Vector256.Create(5.0, -6, 0, -0) };

            var z = x.Select(v => v.Abs()).Unpack().ToArray();
            z.Should().BeEquivalentTo(1.0, 2, 3, 4, 5, 6, 0, 0);
        }
    }
}