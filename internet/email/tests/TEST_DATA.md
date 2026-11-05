# Test data policy

This policy applies to every committed message, header block, MIME part,
archive, key, certificate, byte vector, and benchmark input in this repository.
Test source code that embeds equivalent data follows the same rules.

## Directory purposes

- `tests/fixtures/` contains small, human-authored examples for focused tests.
- `tests/vectors/generated/` contains deterministic inputs and expected outputs
  produced by a committed generator or a documented mechanical procedure.
- `tests/corpus/` contains larger, stable interoperability, standards, archive,
  and malformed-message samples.
- `tests/fuzz/seeds/` contains small inputs chosen only to seed deterministic
  mutation fuzzing. Crash outputs are not committed without minimization and
  the same privacy and license review as corpus files.
- `benchmarks/data/` contains stable benchmark inputs. Benchmark results and
  machine-specific traces do not belong in this directory.

A file belongs in only one category. Tests may read across categories, but must
not silently rewrite committed data. Generated temporary files and fuzz output
go under the operating system's temporary directory and are cleaned up after
the run.

## Privacy and security

Do not commit production email, private correspondence, customer data,
credentials, access tokens, operational private keys, non-public certificates,
or data copied from a personal mailbox. Permission from one correspondent is
not sufficient when a message contains information about other people.

Prefer fully synthetic data. Use reserved examples such as `example.com`,
`.invalid` names, documentation IP address ranges, fixed timestamps, invented
message IDs, and obviously fictional message bodies. Test cryptographic keys
must be newly generated for this repository, have no operational use, and be
labelled as test-only in their manifest entry and, where possible, in the data.

Anonymized real-world structure is allowed only when synthetic reproduction is
impractical. Review the raw bytes and every decoded layer, including headers,
bodies, nested messages, attachments, archives, metadata, filenames, image
metadata, signatures, and malformed fragments. Replace rather than merely hash
names, addresses, IDs, hosts, IP addresses, and unique free text. Record the
review as `synthetic` or `anonymized-reviewed` in the directory manifest. When
in doubt, do not commit the sample.

Fixtures must be inert. Do not include live tracking URLs, network-dependent
references, real malware, active credentials, or payloads intended to execute.
Security cases use the smallest synthetic byte sequence needed to exercise the
behavior.

## Licensing and provenance

Every data file except a directory README and manifest must have one row in the
nearest `MANIFEST.tsv`. The tab-separated columns are:

1. repository-relative `path`;
2. `origin` (`synthetic`, `generated`, `standard`, or `third-party`);
3. an SPDX license identifier or a locally explained `LicenseRef-...` value;
4. a canonical `source` URL or `local`;
5. `sha256` of the committed bytes in lowercase hexadecimal;
6. `privacy_review` (`synthetic` or `anonymized-reviewed`);
7. short `notes`, including the generator command and version when applicable.

Do not copy a sample merely because it is publicly accessible. Before adding
standards or third-party material, verify that its terms allow redistribution
in this repository and preserve every required notice or attribution. Material
with unknown, incompatible, non-redistributable, or personal-use-only terms is
not committed. A URL is provenance, not a license.

For original synthetic data, the contributor must have the right to contribute
it under the repository's applicable source license. If no repository-wide
license has been declared, the manifest must use an explicit license that
permits redistribution; it must not use an assumed project license. Generated
output inherits relevant rights and attribution requirements from both its
generator and its source material.

## Review checklist

Before committing data:

1. confirm the category and use the smallest sample that preserves the case;
2. complete raw and decoded privacy review;
3. verify redistribution terms and required notices;
4. make generation deterministic where applicable;
5. calculate the SHA-256 hash after the final byte-level edit;
6. add or update the nearest manifest row;
7. run the tests or benchmarks that consume the file.

