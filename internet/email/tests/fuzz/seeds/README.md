# Fuzz seeds

Store minimal deterministic mutation-fuzzing seeds here. Seeds are starting
inputs, not an ever-growing collection of random outputs. Minimize a discovered
case, give it a stable descriptive name, and move it to focused fixtures when a
normal regression test represents it better.

All seed files require an entry in `MANIFEST.tsv` and must follow
`tests/TEST_DATA.md`.

