# State persistence

`RandomState` captures a deterministic engine's exact position, including any
partially consumed boolean reservoir. It carries the named algorithm,
algorithm version, transform version, and integrity-protected payload.

## Save and resume

```csharp
using random;

RandomFactory factory = new RandomFactory();
RandomGenerator generator = factory.CreateXoshiro256StarStar(
    123UL,
    RandomMathMode.Portable);

generator.NextUInt64();
RandomState state = generator.ExportState();
string encoded = state.ToBase64();

RandomState decoded = factory.CreateStateFromBase64(encoded);
RandomGenerator resumed = factory.RestoreGenerator(
    decoded,
    RandomMathMode.Portable);
```

`ToBytes()` and `ToBase64()` use the same canonical envelope. Decoding validates
the magic, lengths, versions, algorithm, payload, and SHA-256 digest before a
state is accepted.

Persist `RandomMathMode` beside the state. It is intentionally not encoded in
engine state. Restoring Portable state as System mode preserves raw engine
position but changes the distribution replay contract.

## Restore versus reset

- `RestoreState(state)` replaces the current position after validating that the
  state belongs to the same named algorithm.
- `Reset()` returns to the original factory position—or to the state supplied
  when `RestoreGenerator` created the generator—not to the most recent restored state.
- `Clone()` copies the current and reset positions, but it is for branching a
  replay, not for assigning parallel streams.

Invalid imports and wrong-algorithm restores fail before changing the target
generator.

## Security boundary

The state digest detects accidental corruption. It does not authenticate the
state and provides no secrecy. An attacker who can replace a state can control
future deterministic output; anyone who obtains it can reproduce the stream.
Protect state files with the same authorization and integrity controls as the
simulation or test data they influence.

`SecureRandomGenerator` deliberately has no state export/import API.

## Cross-platform replay

Raw words and non-math conversions resume exactly for a supported named engine
and transform version. Distribution replay is exact across supported platforms
only in Portable mode. In System mode, even the state reached after a
distribution call is not a portable checkpoint because platform math may alter
last bits or rejection paths.
