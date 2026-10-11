# Benchmarks

Micro-benchmarks with [BenchmarkDotNet](https://benchmarkdotnet.org/). They are built by CI but never run there: timings on shared runners are not reliable.

```bash
# Always in Release
dotnet run -c Release --project benchmarks/LearnHub.Benchmarks

# Pick a benchmark and use the short job while exploring
dotnet run -c Release --project benchmarks/LearnHub.Benchmarks -- --filter "*SlugGenerator*" --job short
```

Results are written to `BenchmarkDotNet.Artifacts/` (ignored by Git).
