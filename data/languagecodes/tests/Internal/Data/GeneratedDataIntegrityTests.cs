using System.Security.Cryptography;
using System.Text;
using LanguageCodes.DataGenerator.Model;
using LanguageCodes.DataGenerator.SourceReaders;
using languagecodes;

namespace languagecodes.Tests;

public sealed class GeneratedDataIntegrityTests
{
    [Test]
    public void SnapshotCountsMatchPinnedEdition()
    {
        Assert.Multiple(() =>
        {
            Assert.That(GeneratedLanguageEntries.Rows, Has.Length.EqualTo(8043));
            Assert.That(GeneratedLanguageEntries.Rows.Count(row => row.Alpha2Code != null), Is.EqualTo(184));
            Assert.That(
                GeneratedLanguageEntries.Rows.Count(row => row.Set2TerminologyCode != null),
                Is.EqualTo(486));
            Assert.That(GeneratedLanguageEntries.Rows.Count(row => row.Set3Code != null), Is.EqualTo(7927));
            Assert.That(GeneratedLanguageEntries.Rows.Count(row => row.Set5Code != null), Is.EqualTo(115));
            Assert.That(GeneratedRetirements.Rows, Has.Length.EqualTo(388));
            Assert.That(GeneratedLanguageNames.Rows, Has.Length.EqualTo(34628));
            Assert.That(GeneratedAliases.Rows, Has.Length.EqualTo(2659));
            Assert.That(GeneratedRelationships.Rows, Has.Length.EqualTo(548));
            Assert.That(GeneratedIanaRegistry.Rows, Has.Length.EqualTo(9296));
            Assert.That(
                GeneratedIanaRegistry.Rows.Count(row => row.RecordType == LanguageSubtagType.Language),
                Is.EqualTo(8276));
            Assert.That(
                GeneratedIanaRegistry.Rows.Count(row => row.RecordType == LanguageSubtagType.Extlang),
                Is.EqualTo(258));
            Assert.That(
                GeneratedIanaRegistry.Rows.Count(row => row.RecordType == LanguageSubtagType.Script),
                Is.EqualTo(225));
            Assert.That(
                GeneratedIanaRegistry.Rows.Count(row => row.RecordType == LanguageSubtagType.Region),
                Is.EqualTo(305));
            Assert.That(
                GeneratedIanaRegistry.Rows.Count(row => row.RecordType == LanguageSubtagType.Variant),
                Is.EqualTo(139));
            Assert.That(
                GeneratedIanaRegistry.Rows.Count(row => row.RecordType == LanguageSubtagType.Grandfathered),
                Is.EqualTo(26));
            Assert.That(
                GeneratedIanaRegistry.Rows.Count(row => row.RecordType == LanguageSubtagType.Redundant),
                Is.EqualTo(67));
        });
    }

    [Test]
    public void GeneratedKeysAreUniqueWithinTheirDomains()
    {
        Assert.Multiple(() =>
        {
            Assert.That(GeneratedLanguageEntries.Rows.Select(row => row.Key), Is.Unique);
            Assert.That(
                GeneratedLanguageEntries.Rows.Where(row => row.Alpha2Code != null).Select(row => row.Alpha2Code),
                Is.Unique);
            Assert.That(
                GeneratedLanguageEntries.Rows.Where(row => row.Set2TerminologyCode != null)
                    .Select(row => row.Set2TerminologyCode),
                Is.Unique);
            Assert.That(
                GeneratedLanguageEntries.Rows.Where(row => row.Set2BibliographicCode != null)
                    .Select(row => row.Set2BibliographicCode),
                Is.Unique);
            Assert.That(
                GeneratedLanguageEntries.Rows.Where(row => row.Set3Code != null).Select(row => row.Set3Code),
                Is.Unique);
            Assert.That(
                GeneratedLanguageEntries.Rows.Where(row => row.Set5Code != null).Select(row => row.Set5Code),
                Is.Unique);
            Assert.That(GeneratedRetirements.Rows.Select(row => row.RetiredCode), Is.Unique);
            Assert.That(
                GeneratedLanguageNames.Rows.Select(NameKey),
                Is.Unique);
            Assert.That(
                GeneratedAliases.Rows.Select(
                    row => JoinKey(
                        row.EntityKey,
                        row.Value,
                        row.Kind.ToString(),
                        row.Source.ToString(),
                        row.SourceRecordIdentifier)),
                Is.Unique);
            Assert.That(
                GeneratedRelationships.Rows.Select(
                    row => JoinKey(
                        row.ParentCode,
                        row.ChildCode,
                        row.Kind.ToString(),
                        row.Status.ToString(),
                        row.Source.ToString())),
                Is.Unique);
            Assert.That(
                GeneratedIanaRegistry.Rows.Select(row => JoinKey(row.RecordType.ToString(), row.Value)),
                Is.Unique);
        });
    }

    [Test]
    public void GeneratedEntitiesPreserveEveryCrossSetMappingAndBibliographicPair()
    {
        string repositoryRoot = FindRepositoryRoot();
        DataManifest manifest = ReadManifest(repositoryRoot);
        Iso639Set2Data set2 = Iso639Set2Reader.Read(SourcePath(repositoryRoot, manifest, "iso-639-2-code-table"));
        Iso639Set3Record[] set3 = Iso639Set3Reader.Read(
            SourcePath(repositoryRoot, manifest, "iso-639-3-code-table"));
        Iso639Set5Record[] set5 = Iso639Set5Reader.Read(
            SourcePath(repositoryRoot, manifest, "iso-639-5-code-table"));
        Dictionary<string, LanguageRow> rowsBySet2 = GeneratedLanguageEntries.Rows
            .Where(row => row.Set2TerminologyCode != null)
            .ToDictionary(row => row.Set2TerminologyCode!);
        Dictionary<string, LanguageRow> rowsBySet3 = GeneratedLanguageEntries.Rows
            .Where(row => row.Set3Code != null)
            .ToDictionary(row => row.Set3Code!);
        Dictionary<string, LanguageRow> rowsBySet5 = GeneratedLanguageEntries.Rows
            .Where(row => row.Set5Code != null)
            .ToDictionary(row => row.Set5Code!);
        List<string> violations = new List<string>();

        foreach (Iso639Set2Record source in set2.Records)
        {
            string terminologyCode = source.TerminologyCode ?? source.BibliographicCode;
            if (!rowsBySet2.TryGetValue(terminologyCode, out LanguageRow? row))
            {
                violations.Add($"Set 2 code '{terminologyCode}' has no generated entity.");
                continue;
            }

            AddMismatch(violations, terminologyCode, "Set 2 terminology", terminologyCode, row.Set2TerminologyCode);
            AddMismatch(
                violations,
                terminologyCode,
                "Set 2 bibliographic",
                source.BibliographicCode,
                row.Set2BibliographicCode);
            AddMismatch(violations, terminologyCode, "alpha-2", source.Alpha2Code, row.Alpha2Code);
        }

        foreach (Iso639Set3Record source in set3)
        {
            if (!rowsBySet3.TryGetValue(source.Id, out LanguageRow? row))
            {
                violations.Add($"Set 3 code '{source.Id}' has no generated entity.");
                continue;
            }

            AddMismatch(violations, source.Id, "Set 3", source.Id, row.Set3Code);
            AddMismatch(
                violations,
                source.Id,
                "Set 2 terminology",
                source.Part2TerminologyCode,
                row.Set2TerminologyCode);
            AddMismatch(
                violations,
                source.Id,
                "Set 2 bibliographic",
                source.Part2BibliographicCode,
                row.Set2BibliographicCode);
            AddMismatch(violations, source.Id, "alpha-2", source.Alpha2Code, row.Alpha2Code);
        }

        foreach (Iso639Set5Record source in set5)
        {
            if (!rowsBySet5.TryGetValue(source.Code, out LanguageRow? row))
            {
                violations.Add($"Set 5 code '{source.Code}' has no generated entity.");
                continue;
            }

            AddMismatch(violations, source.Code, "Set 5", source.Code, row.Set5Code);
        }

        int sourcePairCount = set2.Records.Count(
            row => row.TerminologyCode != null && row.TerminologyCode != row.BibliographicCode);
        int generatedPairCount = GeneratedLanguageEntries.Rows.Count(
            row => row.Set2TerminologyCode != null && row.Set2BibliographicCode != null &&
                row.Set2TerminologyCode != row.Set2BibliographicCode);

        Assert.Multiple(() =>
        {
            Assert.That(violations, Is.Empty, string.Join(Environment.NewLine, violations.Take(20)));
            Assert.That(sourcePairCount, Is.EqualTo(20));
            Assert.That(generatedPairCount, Is.EqualTo(sourcePairCount));
        });
    }

    [Test]
    public void GeneratedNamesPreserveEveryIsoSourceName()
    {
        string repositoryRoot = FindRepositoryRoot();
        DataManifest manifest = ReadManifest(repositoryRoot);
        Iso639Set2Data set2 = Iso639Set2Reader.Read(SourcePath(repositoryRoot, manifest, "iso-639-2-code-table"));
        Iso639Set3Record[] set3 = Iso639Set3Reader.Read(
            SourcePath(repositoryRoot, manifest, "iso-639-3-code-table"));
        Iso639Set3NameRecord[] set3Names = Iso639Set3NameIndexReader.Read(
            SourcePath(repositoryRoot, manifest, "iso-639-3-name-index"));
        Iso639Set5Record[] set5 = Iso639Set5Reader.Read(
            SourcePath(repositoryRoot, manifest, "iso-639-5-code-table"));
        Dictionary<string, LanguageRow> rowsBySet2 = GeneratedLanguageEntries.Rows
            .Where(row => row.Set2TerminologyCode != null)
            .ToDictionary(row => row.Set2TerminologyCode!);
        Dictionary<string, LanguageRow> rowsBySet3 = GeneratedLanguageEntries.Rows
            .Where(row => row.Set3Code != null)
            .ToDictionary(row => row.Set3Code!);
        Dictionary<string, LanguageRow> rowsBySet5 = GeneratedLanguageEntries.Rows
            .Where(row => row.Set5Code != null)
            .ToDictionary(row => row.Set5Code!);
        HashSet<string> generatedNames = GeneratedLanguageNames.Rows.Select(NameKey).ToHashSet(StringComparer.Ordinal);
        List<string> missing = new List<string>();

        foreach (Iso639Set2Record source in set2.Records)
        {
            string terminologyCode = source.TerminologyCode ?? source.BibliographicCode;
            LanguageRow entity = rowsBySet2[terminologyCode];
            AddMissingNames(
                missing,
                generatedNames,
                entity.Key,
                source.EnglishNames,
                "en",
                LanguageNameKind.English,
                ReconciledSource.Iso639Set2);
            AddMissingNames(
                missing,
                generatedNames,
                entity.Key,
                source.FrenchNames,
                "fr",
                LanguageNameKind.French,
                ReconciledSource.Iso639Set2);
        }

        foreach (Iso639Set3Record source in set3)
        {
            AddMissingName(
                missing,
                generatedNames,
                rowsBySet3[source.Id].Key,
                source.ReferenceName,
                "en",
                LanguageNameKind.Reference,
                ReconciledSource.Iso639Set3);
        }

        foreach (Iso639Set3NameRecord source in set3Names)
        {
            string entityKey = rowsBySet3[source.Id].Key;
            AddMissingName(
                missing,
                generatedNames,
                entityKey,
                source.PrintName,
                "en",
                LanguageNameKind.Print,
                ReconciledSource.Iso639Set3);
            AddMissingName(
                missing,
                generatedNames,
                entityKey,
                source.InvertedName,
                "en",
                LanguageNameKind.Inverted,
                ReconciledSource.Iso639Set3);
        }

        foreach (Iso639Set5Record source in set5)
        {
            LanguageRow entity = rowsBySet5[source.Code];
            AddMissingNames(
                missing,
                generatedNames,
                entity.Key,
                source.EnglishNames,
                "en",
                LanguageNameKind.English,
                ReconciledSource.Iso639Set5);
            AddMissingNames(
                missing,
                generatedNames,
                entity.Key,
                source.FrenchNames,
                "fr",
                LanguageNameKind.French,
                ReconciledSource.Iso639Set5);
        }

        Assert.That(missing, Is.Empty, string.Join(Environment.NewLine, missing.Take(20)));
    }

    [Test]
    public void GeneratedReferencesResolveToGeneratedRows()
    {
        HashSet<string> entityKeys = GeneratedLanguageEntries.Rows.Select(row => row.Key)
            .ToHashSet(StringComparer.Ordinal);
        HashSet<string> activeCodes = GeneratedLanguageEntries.Rows
            .SelectMany(row => new[]
            {
                row.Alpha2Code,
                row.Set2TerminologyCode,
                row.Set2BibliographicCode,
                row.Set3Code,
                row.Set5Code
            })
            .Where(code => code != null)
            .Select(code => code!)
            .ToHashSet(StringComparer.Ordinal);
        HashSet<string> retiredCodes = GeneratedRetirements.Rows.Select(row => row.RetiredCode)
            .ToHashSet(StringComparer.Ordinal);
        HashSet<string> set5Codes = GeneratedLanguageEntries.Rows
            .Where(row => row.Set5Code != null)
            .Select(row => row.Set5Code!)
            .ToHashSet(StringComparer.Ordinal);
        List<string> violations = new List<string>();

        foreach (NameRow row in GeneratedLanguageNames.Rows)
        {
            if (!entityKeys.Contains(row.EntityKey))
            {
                violations.Add($"Name '{row.Text}' references missing entity '{row.EntityKey}'.");
            }
        }

        foreach (AliasRow row in GeneratedAliases.Rows)
        {
            if (row.EntityKey != null && !entityKeys.Contains(row.EntityKey))
            {
                violations.Add($"Alias '{row.Value}' references missing entity '{row.EntityKey}'.");
            }
        }

        foreach (RetirementRow row in GeneratedRetirements.Rows)
        {
            foreach (string replacement in row.ReplacementCodes)
            {
                if (!activeCodes.Contains(replacement) && !retiredCodes.Contains(replacement))
                {
                    violations.Add($"Retirement '{row.RetiredCode}' references missing code '{replacement}'.");
                }
            }
        }

        foreach (RelationshipRow row in GeneratedRelationships.Rows)
        {
            if (row.Kind == LanguageRelationshipKind.Set5ParentChild)
            {
                if (!set5Codes.Contains(row.ParentCode) || !set5Codes.Contains(row.ChildCode))
                {
                    violations.Add($"Set 5 relationship '{row.ParentCode}:{row.ChildCode}' is dangling.");
                }
            }
            else
            {
                if (!activeCodes.Contains(row.ParentCode))
                {
                    violations.Add($"Macrolanguage parent '{row.ParentCode}' is missing.");
                }

                bool childExists = row.Status == ReconciledRelationshipStatus.Active
                    ? activeCodes.Contains(row.ChildCode)
                    : retiredCodes.Contains(row.ChildCode);
                if (!childExists)
                {
                    violations.Add($"Macrolanguage member '{row.ChildCode}' is missing for status '{row.Status}'.");
                }
            }
        }

        Assert.That(violations, Is.Empty, string.Join(Environment.NewLine, violations.Take(20)));
    }

    [Test]
    public void Set5HierarchyIsAcyclicAndCoversEverySet5Code()
    {
        string[] codes = GeneratedLanguageEntries.Rows
            .Where(row => row.Set5Code != null)
            .Select(row => row.Set5Code!)
            .ToArray();
        Dictionary<string, int> indegree = codes.ToDictionary(code => code, _ => 0, StringComparer.Ordinal);
        Dictionary<string, List<string>> children = codes.ToDictionary(
            code => code,
            _ => new List<string>(),
            StringComparer.Ordinal);
        RelationshipRow[] edges = GeneratedRelationships.Rows
            .Where(row => row.Kind == LanguageRelationshipKind.Set5ParentChild)
            .ToArray();

        foreach (RelationshipRow edge in edges)
        {
            children[edge.ParentCode].Add(edge.ChildCode);
            indegree[edge.ChildCode]++;
        }

        Queue<string> pending = new Queue<string>(indegree.Where(item => item.Value == 0).Select(item => item.Key));
        int visited = 0;
        while (pending.Count > 0)
        {
            string code = pending.Dequeue();
            visited++;
            foreach (string child in children[code])
            {
                indegree[child]--;
                if (indegree[child] == 0)
                {
                    pending.Enqueue(child);
                }
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(edges, Is.Not.Empty);
            Assert.That(visited, Is.EqualTo(codes.Length), "The Set 5 hierarchy contains a cycle.");
            Assert.That(indegree.Values, Has.All.EqualTo(0));
        });
    }

    [Test]
    public void ManifestHashesLengthsHeadersAndVersionsMatchPinnedFiles()
    {
        string repositoryRoot = FindRepositoryRoot();
        DataManifest manifest = ReadManifest(repositoryRoot);
        List<string> violations = new List<string>();
        foreach (DataManifestSource source in manifest.Sources)
        {
            string path = SourcePath(repositoryRoot, source);
            if (!File.Exists(path))
            {
                violations.Add($"Manifest source '{source.Id}' is missing at '{source.Path}'.");
                continue;
            }

            byte[] bytes = File.ReadAllBytes(path);
            if (bytes.LongLength != source.ByteLength)
            {
                violations.Add(
                    $"Manifest source '{source.Id}' length is {bytes.LongLength}, expected {source.ByteLength}.");
            }

            string sha256 = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
            if (sha256 != source.Sha256)
            {
                violations.Add($"Manifest source '{source.Id}' hash is '{sha256}', expected '{source.Sha256}'.");
            }

            byte[] expectedHeader = Encoding.UTF8.GetBytes(source.ExpectedHeader);
            if (bytes.Length < expectedHeader.Length ||
                !bytes.AsSpan(0, expectedHeader.Length).SequenceEqual(expectedHeader))
            {
                violations.Add($"Manifest source '{source.Id}' does not have its expected header.");
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(manifest.Sources, Has.Length.EqualTo(20));
            Assert.That(manifest.ManifestVersion, Is.EqualTo(GeneratedDataMetadata.ManifestVersion));
            Assert.That(
                manifest.GeneratorSchemaVersion,
                Is.EqualTo(GeneratedDataMetadata.GeneratorSchemaVersion));
            Assert.That(
                manifest.RetrievalDate.ToString("yyyy-MM-dd"),
                Is.EqualTo(GeneratedDataMetadata.RetrievalDate));
            Assert.That(
                manifest.GetSource("iso-639-2-code-table").UpstreamDate,
                Is.EqualTo(GeneratedDataMetadata.Iso639Set2UpstreamDate));
            Assert.That(
                manifest.GetSource("iso-639-3-code-table").UpstreamDate,
                Is.EqualTo(GeneratedDataMetadata.Iso639Set3CoreUpstreamDate));
            Assert.That(
                manifest.GetSource("iso-639-3-name-index").UpstreamDate,
                Is.EqualTo(GeneratedDataMetadata.Iso639Set3NamesUpstreamDate));
            Assert.That(
                manifest.GetSource("iso-639-3-macrolanguage-mappings").UpstreamDate,
                Is.EqualTo(GeneratedDataMetadata.Iso639Set3MacrolanguagesUpstreamDate));
            Assert.That(
                manifest.GetSource("iso-639-3-retirements").UpstreamDate,
                Is.EqualTo(GeneratedDataMetadata.Iso639Set3RetirementsUpstreamDate));
            Assert.That(
                manifest.GetSource("iso-639-5-code-table").UpstreamDate,
                Is.EqualTo(GeneratedDataMetadata.Iso639Set5UpstreamDate));
            Assert.That(
                manifest.GetSource("iso-639-5-hierarchy").UpstreamDate,
                Is.EqualTo(GeneratedDataMetadata.Iso639Set5HierarchyUpstreamDate));
            Assert.That(
                manifest.GetSource("iana-language-subtag-registry").UpstreamDate,
                Is.EqualTo(GeneratedDataMetadata.IanaRegistryUpstreamDate));
            Assert.That(violations, Is.Empty, string.Join(Environment.NewLine, violations));
        });
    }

    private static void AddMismatch(
        List<string> violations,
        string identifier,
        string field,
        string? expected,
        string? actual)
    {
        if (expected != actual)
        {
            violations.Add($"'{identifier}' {field} is '{actual ?? "<null>"}', expected '{expected ?? "<null>"}'.");
        }
    }

    private static void AddMissingNames(
        List<string> missing,
        HashSet<string> generatedNames,
        string entityKey,
        string[] names,
        string language,
        LanguageNameKind kind,
        ReconciledSource source)
    {
        foreach (string name in names)
        {
            AddMissingName(missing, generatedNames, entityKey, name, language, kind, source);
        }
    }

    private static void AddMissingName(
        List<string> missing,
        HashSet<string> generatedNames,
        string entityKey,
        string text,
        string language,
        LanguageNameKind kind,
        ReconciledSource source)
    {
        string key = NameKey(entityKey, text, language, kind, source);
        if (!generatedNames.Contains(key))
        {
            missing.Add($"Generated names omit '{text}' ({kind}, {source}) for '{entityKey}'.");
        }
    }

    private static string NameKey(NameRow row)
    {
        return NameKey(row.EntityKey, row.Text, row.NameLanguage, row.Kind, row.Source);
    }

    private static string NameKey(
        string entityKey,
        string text,
        string language,
        LanguageNameKind kind,
        ReconciledSource source)
    {
        return JoinKey(entityKey, kind.ToString(), source.ToString(), language, text);
    }

    private static string JoinKey(params string?[] values)
    {
        return string.Join('\0', values);
    }

    private static DataManifest ReadManifest(string repositoryRoot)
    {
        return DataManifestReader.Read(Path.Combine(repositoryRoot, "specs", "data-manifest.json"));
    }

    private static string SourcePath(string repositoryRoot, DataManifest manifest, string sourceId)
    {
        return SourcePath(repositoryRoot, manifest.GetSource(sourceId));
    }

    private static string SourcePath(string repositoryRoot, DataManifestSource source)
    {
        return Path.Combine(repositoryRoot, source.Path.Replace('/', Path.DirectorySeparatorChar));
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "specs", "data-manifest.json")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the repository root from the test directory.");
    }
}
