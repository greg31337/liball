# Sobol direction-number generator

This development tool converts the downloaded Joe–Kuo D(6) parameter table into the checked-in `src/Quasi/SobolDirectionNumbers.cs`. The library never reads the source data or runs this tool at runtime.

Upstream data: <https://web.maths.unsw.edu.au/~fkuo/sobol/new-joe-kuo-6.21201>

Pinned input SHA-256: `68eedd2a4e3b659b9695e7aff0f8ac68718bcf620730fc3d3a8c65df2a067441`

Generated output SHA-256: `e9b55b2a9a9caf53fb6f21da8d97ec36304921df8fab667079290489735f4058`

Run from the repository root:

```sh
dotnet run --project tools/GenerateSobolDirectionNumbers/GenerateSobolDirectionNumbers.csproj -- specs/sobol-new-joe-kuo-6.21201.txt src/Quasi/SobolDirectionNumbers.cs
```

The generator verifies the pinned checksum and the complete dimension, polynomial, degree, and initial-value structure before writing UTF-8 source with deterministic line endings. Re-running the command with the same input must produce byte-identical output.
