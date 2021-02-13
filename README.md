# Runge-Kutta 4(5) Sample Implementation in .NET 5

> A small application demoing the .NET SIMD intrinsics.

## Requirements

* .NET 5 (or .NET Core 3.1 with minor modifications to the `*.csproj` files).
* A CPU that supports the FMA and AVX SIMD instruction sets.
* The unit tests in `rk45.tests/` have some NuGet dependencies which get
  automatically downloaded during the build.

## Building

Using the `dotnet` CLI tool:

```sh
$ dotnet build -c Release
```

## Running the Benchmarks

The naive implementation:

```sh
$ dotnet run -c Release -p rk45
```

The SIMD implementation:

```sh
$ dotnet run -c Release -p rk45 -- --simd
```

## Unit Tests

There is some small test coverage for the `SimdMathExtensions` class. The
tests can be run with:

```sh
$ dotnet test
```