# Specification Sources

Retrieved on 2026-08-25. These snapshots cover the normative version syntax and precedence rules, ecosystem range grammars, and ecosystem-specific selection behavior named in `todo.md`. HTML pages are saved as delivered by the authoritative project sites; the node-semver reference is the upstream repository's Markdown source.

| Local file | Authoritative source | Relevant scope |
| --- | --- | --- |
| `semver-2.0.0.html` | https://semver.org/ | SemVer 2.0.0 syntax, precedence, and BNF grammar |
| `node-semver-readme.md` | https://raw.githubusercontent.com/npm/node-semver/main/README.md | npm/node-semver parsing, comparison, range grammar, increments, and selection helpers |
| `maven-comparable-version.html` | https://maven.apache.org/ref/3-LATEST/maven-artifact/apidocs/org/apache/maven/artifact/versioning/ComparableVersion.html | Maven version tokenization, qualifier aliases, normalization, and ordering |
| `maven-pom-reference.html` | https://maven.apache.org/pom.html | Maven dependency version requirements and mediation behavior |
| `maven-repository-metadata.html` | https://maven.apache.org/ref/3-LATEST/maven-repository-metadata/repository-metadata.html | Maven repository and snapshot metadata model |
| `nuget-package-versioning.html` | https://learn.microsoft.com/en-us/nuget/concepts/package-versioning | NuGet version syntax, normalization, SemVer differences, and interval ranges |
| `nuget-dependency-resolution.html` | https://learn.microsoft.com/en-us/nuget/concepts/dependency-resolution | NuGet floating versions, lowest-applicable-version selection, and dependency rules |
| `nuget-target-frameworks.html` | https://learn.microsoft.com/en-us/nuget/reference/target-frameworks | NuGet target framework and dependency-group conventions |
| `go-modules-reference.html` | https://go.dev/ref/mod | Go module versions, module paths, semantic import versioning, and module selection |
| `go-x-mod-semver.html` | https://pkg.go.dev/golang.org/x/mod/semver | Go's public semver operation contract and deviations from SemVer 2.0.0 |
| `cargo-specifying-dependencies.html` | https://doc.rust-lang.org/stable/cargo/reference/specifying-dependencies.html | Cargo version-requirement syntax and prerelease rules |
| `cargo-dependency-resolver.html` | https://doc.rust-lang.org/stable/cargo/reference/resolver.html | Cargo candidate selection, feature unification, and dependency resolution behavior |
| `rubygems-version.html` | https://docs.ruby-lang.org/en/3.4/Gem/Version.html | RubyGems version syntax, normalization, prerelease detection, and ordering |
| `rubygems-requirement.html` | https://docs.ruby-lang.org/en/3.4/Gem/Requirement.html | RubyGems requirement operators and satisfaction behavior |
| `pep-0440.html` | https://peps.python.org/pep-0440/ | PEP 440 version identifiers, ordering, normalization, and specifiers |
| `python-version-specifiers.html` | https://packaging.python.org/en/latest/specifications/version-specifiers/ | Current Python packaging version and dependency-specifier specification |
| `rfc2119.txt` | https://www.rfc-editor.org/rfc/rfc2119.txt | Original definitions of normative requirement keywords used by PEP 440 |
| `rfc8174.txt` | https://www.rfc-editor.org/rfc/rfc8174.txt | Updated interpretation of RFC 2119 normative keywords |
