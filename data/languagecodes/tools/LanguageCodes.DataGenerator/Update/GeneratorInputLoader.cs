using System;
using System.IO;
using LanguageCodes.DataGenerator.Model;
using LanguageCodes.DataGenerator.Reconciliation;
using LanguageCodes.DataGenerator.SourceReaders;
using LanguageCodes.DataGenerator.Validation;

namespace LanguageCodes.DataGenerator.Update;

internal sealed class ValidatedGeneratorData
{
    internal ValidatedGeneratorData(
        ReconciledIso639Data isoData,
        Iso639Set3NameRecord[] names,
        Iso639Set3MacrolanguageMapping[] mappings,
        Iso639Set3RetirementRecord[] retirements,
        Iso639Set5HierarchyData hierarchy,
        IanaLanguageSubtagRegistry registry,
        ReconciledCodeAliasData aliases)
    {
        IsoData = isoData;
        Names = names;
        Mappings = mappings;
        Retirements = retirements;
        Hierarchy = hierarchy;
        Registry = registry;
        Aliases = aliases;
    }

    internal ReconciledIso639Data IsoData { get; }

    internal Iso639Set3NameRecord[] Names { get; }

    internal Iso639Set3MacrolanguageMapping[] Mappings { get; }

    internal Iso639Set3RetirementRecord[] Retirements { get; }

    internal Iso639Set5HierarchyData Hierarchy { get; }

    internal IanaLanguageSubtagRegistry Registry { get; }

    internal ReconciledCodeAliasData Aliases { get; }
}

internal static class GeneratorInputLoader
{
    internal static ValidatedGeneratorData LoadAndValidate(
        DataManifest manifest,
        string sourceRoot)
    {
        Iso639Set2Data set2 = Iso639Set2Reader.Read(
            ResolveSourcePath(manifest, sourceRoot, "iso-639-2-code-table"));
        Iso639Set3Record[] set3 = Iso639Set3Reader.Read(
            ResolveSourcePath(manifest, sourceRoot, "iso-639-3-code-table"));
        Iso639Set3NameRecord[] names = Iso639Set3NameIndexReader.Read(
            ResolveSourcePath(manifest, sourceRoot, "iso-639-3-name-index"));
        Iso639Set3RetirementRecord[] retirements = Iso639Set3RetirementReader.Read(
            ResolveSourcePath(manifest, sourceRoot, "iso-639-3-retirements"));
        Iso639Set3MacrolanguageMapping[] mappings = Iso639Set3MacrolanguageReader.Read(
            ResolveSourcePath(manifest, sourceRoot, "iso-639-3-macrolanguage-mappings"),
            set3,
            retirements);
        Iso639Set5Record[] set5 = Iso639Set5Reader.Read(
            ResolveSourcePath(manifest, sourceRoot, "iso-639-5-code-table"));
        Iso639Set5HierarchyData hierarchy = Iso639Set5HierarchyReader.Read(
            ResolveSourcePath(manifest, sourceRoot, "iso-639-5-hierarchy"),
            set5);
        IanaLanguageSubtagRegistry registry = IanaLanguageSubtagRegistryReader.Read(
            ResolveSourcePath(manifest, sourceRoot, "iana-language-subtag-registry"));
        ReconciledIso639Data isoData = Iso639Reconciler.Reconcile(set2, set3, set5);
        ReconciledCodeAliasData aliases = CodeAliasDeriver.Derive(isoData, retirements, registry);
        string[] violations = GeneratorIntegrityValidator.FindViolations(
            set2,
            set3,
            names,
            mappings,
            retirements,
            set5,
            hierarchy,
            registry,
            isoData,
            aliases);
        if (violations.Length > 0)
        {
            throw new InvalidDataException(
                "Generator inputs failed invariants:" + Environment.NewLine +
                string.Join(Environment.NewLine, violations));
        }

        return new ValidatedGeneratorData(
            isoData,
            names,
            mappings,
            retirements,
            hierarchy,
            registry,
            aliases);
    }

    internal static string FindSourceRoot(string manifestPath, DataManifest manifest)
    {
        string? candidate = Path.GetDirectoryName(Path.GetFullPath(manifestPath));
        while (candidate != null)
        {
            if (ContainsAllSources(candidate, manifest))
            {
                return candidate;
            }

            candidate = Path.GetDirectoryName(candidate);
        }

        throw new DirectoryNotFoundException(
            "Could not locate the source root described by the pinned manifest.");
    }

    private static string ResolveSourcePath(
        DataManifest manifest,
        string sourceRoot,
        string sourceId)
    {
        string relativePath = manifest.GetSource(sourceId).Path;
        string path = Path.GetFullPath(
            Path.Combine(sourceRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        string rootPrefix = sourceRoot.EndsWith(Path.DirectorySeparatorChar)
            ? sourceRoot
            : sourceRoot + Path.DirectorySeparatorChar;
        if (!path.StartsWith(rootPrefix, StringComparison.Ordinal) || !File.Exists(path))
        {
            throw new FileNotFoundException(
                $"Manifest source '{relativePath}' is missing or escapes its source root.",
                path);
        }

        return path;
    }

    private static bool ContainsAllSources(string root, DataManifest manifest)
    {
        foreach (DataManifestSource source in manifest.Sources)
        {
            string path = Path.Combine(
                root,
                source.Path.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path))
            {
                return false;
            }
        }

        return true;
    }
}
