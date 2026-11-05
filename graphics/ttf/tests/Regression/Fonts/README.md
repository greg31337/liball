# Minimized font regressions

This directory retains only redistributable, minimized font files associated
with independently confirmed sanitizer or parser regressions. Metadata belongs
in `manifest.json`; see the OpenType validation tool documentation for the
required provenance, minimization, integrity, and expected-result fields.

No qualifying sanitizer, deterministic malformed-input, or fuzz-target failure
has been discovered in the current corpus. There is therefore no legitimate
artifact to minimize or retain, and the manifest is intentionally empty.
