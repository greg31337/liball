# Benchmarks

This directory is reserved for the SDK/BCL-only benchmark harness and benchmark
definitions. Benchmarks use fixed workloads, state their warm-up and iteration
counts, and report runtime, architecture, build configuration, and input name.
Machine-specific results, traces, dumps, and profiler output are not committed
as source data.

Benchmark inputs live in `data/` and follow `tests/TEST_DATA.md`. Benchmark code
must not contain production messages, secrets, network access, or undeclared
third-party benchmark frameworks.

