# GS1 AI table generation

`generate_gs1_ai_table.py` is the build-time-only transformation from the
checked-in `specs/GS1-Syntax-Dictionary.txt` snapshot to the static C# data
consumed by the library. It uses only the Python standard library; generated
C# has no runtime dependency on Python, the dictionary, or network access.

The generator pins the snapshot SHA-256 and fails if the source changes without
an intentional generator update. It parses every entry into these canonical
fields:

- concrete AI plus its source range/wildcard pattern;
- predefined-length and Digital Link data-attribute flags;
- ordered components with character set, minimum/maximum length, optionality,
  and ordered linter names;
- required association alternatives, exclusions, Digital Link primary-key and
  ordered qualifier alternatives;
- title and source line provenance.

Ranges are expanded to concrete AIs and sorted lexically. The generated table
stores one definition per source entry and a flattened decimal prefix trie maps
every concrete AI to its definition without runtime parsing. All arrays preserve
source order. UTF-8 input and LF output are mandatory. Unknown flags,
attributes, component forms, association forms, duplicate expanded AIs, and a
source digest mismatch fail generation.

Validate the pinned snapshot and print its deterministic counts:

```sh
python3 tools/gs1/generate_gs1_ai_table.py
```

Generate the canonical table (introduced by the next implementation task):

```sh
python3 tools/gs1/generate_gs1_ai_table.py \
  --output src/Internal/Gs1/Gs1AiTable.cs
```

Verify that a checked-in table is current without modifying it:

```sh
python3 tools/gs1/generate_gs1_ai_table.py \
  --output src/Internal/Gs1/Gs1AiTable.cs --check
```
