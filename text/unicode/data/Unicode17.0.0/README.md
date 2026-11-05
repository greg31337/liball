# Unicode 17.0.0 Data Inputs

This directory pins the Unicode Character Database input files used by the data
generator, plus version-matched Unicode emoji and security data.

Download source:

`https://www.unicode.org/Public/17.0.0/ucd/`
`https://www.unicode.org/Public/17.0.0/security/`

Download the files named in `checksums.sha256` into this directory:

```sh
while read hash file; do
  case "$hash" in ''|'#'*) continue ;; esac
  case "$file" in
    emoji/emoji-sequences.txt|emoji/emoji-zwj-sequences.txt)
      url="https://www.unicode.org/Public/17.0.0/emoji/${file#emoji/}"
      ;;
    security/*)
      url="https://www.unicode.org/Public/17.0.0/security/${file#security/}"
      ;;
    *)
      url="https://www.unicode.org/Public/17.0.0/ucd/$file"
      ;;
  esac
  mkdir -p "$(dirname "$file")"
  curl -fsSL "$url" -o "$file"
done < checksums.sha256
```

Verify the downloaded files before running the generator:

```sh
shasum -a 256 -c checksums.sha256
```

Do not update these files or checksums without updating the pinned Unicode
version and provenance metadata in the repository.
