using System;
using System.Text.Json;
using LanguageCodes.DataGenerator.Emission;
using LanguageCodes.DataGenerator.Model;
using LanguageCodes.DataGenerator.Reconciliation;
using LanguageCodes.DataGenerator.SourceReaders;
using LanguageCodes.DataGenerator.Update;
using LanguageCodes.DataGenerator.Validation;

namespace LanguageCodes.DataGenerator;

internal static class Program
{
    internal static int Main(string[] arguments)
    {
        if (arguments.Length == 0)
        {
            Console.WriteLine("LanguageCodes data generator");
            return 0;
        }

        if (arguments.Length == 3 && arguments[0] == "fetch-updates")
        {
            try
            {
                StagingFetchResult result = StagingFetchCommand.Execute(
                    arguments[1],
                    arguments[2]);
                Console.WriteLine(
                    $"Fetched and validated {result.SourceCount} sources " +
                    $"({result.ByteCount} bytes) into {result.StagingPath}.");
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine($"Staging fetch failed: {exception.Message}");
                return 1;
            }
        }

        if (arguments.Length == 3 && arguments[0] == "diff-updates")
        {
            try
            {
                SemanticDiffReport report = SemanticDiffCommand.Execute(
                    arguments[1],
                    arguments[2]);
                using Utf8JsonWriter writer = new Utf8JsonWriter(
                    Console.OpenStandardOutput(),
                    new JsonWriterOptions { Indented = true });
                SemanticDiffCommand.Write(writer, report);
                Console.WriteLine();
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine($"Semantic diff failed: {exception.Message}");
                return 1;
            }
        }

        if (arguments.Length == 4 && arguments[0] == "accept-updates")
        {
            try
            {
                AcceptUpdatesResult result = AcceptUpdatesCommand.Execute(
                    arguments[1],
                    arguments[2],
                    arguments[3]);
                Console.WriteLine(
                    $"Accepted {result.SourceCount} sources ({result.ChangedSourceCount} changed), " +
                    $"regenerated {result.GeneratedFileCount} tables " +
                    $"({result.ChangedGeneratedFileCount} changed), and atomically updated " +
                    $"{result.ChangedFileCount} files.");
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine($"Update acceptance failed: {exception.Message}");
                return 1;
            }
        }

        if (arguments.Length == 3 && arguments[0] == "verify-generation")
        {
            try
            {
                VerifyGenerationResult result = VerifyGenerationCommand.Execute(
                    arguments[1],
                    arguments[2]);
                Console.WriteLine(
                    $"Verified {result.SourceCount} pinned sources reproduce " +
                    $"{result.GeneratedFileCount} generated files byte for byte.");
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine($"Generation verification failed: {exception.Message}");
                return 1;
            }
        }

        if (arguments.Length == 2 && arguments[0] == "read-iso-639-2")
        {
            Iso639Set2Data data = Iso639Set2Reader.Read(arguments[1]);
            Console.WriteLine(
                $"Read {data.Records.Length} ISO 639 Set 1/2 records and {data.ReservedRanges.Length} reserved ranges.");
            return 0;
        }

        if (arguments.Length == 2 && arguments[0] == "read-iso-639-3")
        {
            Iso639Set3Record[] records = Iso639Set3Reader.Read(arguments[1]);
            Console.WriteLine($"Read {records.Length} ISO 639 Set 3 records.");
            return 0;
        }

        if (arguments.Length == 2 && arguments[0] == "read-iso-639-3-names")
        {
            Iso639Set3NameRecord[] records = Iso639Set3NameIndexReader.Read(arguments[1]);
            Console.WriteLine($"Read {records.Length} ISO 639 Set 3 name pairs.");
            return 0;
        }

        if (arguments.Length == 4 && arguments[0] == "read-iso-639-3-macrolanguages")
        {
            Iso639Set3Record[] activeRecords = Iso639Set3Reader.Read(arguments[2]);
            Iso639Set3RetirementRecord[] retiredRecords = Iso639Set3RetirementReader.Read(arguments[3]);
            Iso639Set3MacrolanguageMapping[] mappings = Iso639Set3MacrolanguageReader.Read(
                arguments[1],
                activeRecords,
                retiredRecords);
            Console.WriteLine($"Read and validated {mappings.Length} ISO 639 Set 3 macrolanguage mappings.");
            return 0;
        }

        if (arguments.Length == 2 && arguments[0] == "read-iso-639-3-retirements")
        {
            Iso639Set3RetirementRecord[] records = Iso639Set3RetirementReader.Read(arguments[1]);
            int replacementCount = 0;
            foreach (Iso639Set3RetirementRecord record in records)
            {
                replacementCount += record.ReplacementCodes.Length;
            }

            Console.WriteLine(
                $"Read {records.Length} ISO 639 Set 3 retirement records with {replacementCount} replacement targets.");
            return 0;
        }

        if (arguments.Length == 2 && arguments[0] == "read-iso-639-5")
        {
            Iso639Set5Record[] records = Iso639Set5Reader.Read(arguments[1]);
            int nameCount = 0;
            foreach (Iso639Set5Record record in records)
            {
                nameCount += record.EnglishNames.Length + record.FrenchNames.Length;
            }

            Console.WriteLine($"Read {records.Length} ISO 639 Set 5 records with {nameCount} official names.");
            return 0;
        }

        if (arguments.Length == 3 && arguments[0] == "read-iso-639-5-hierarchy")
        {
            Iso639Set5Record[] records = Iso639Set5Reader.Read(arguments[2]);
            Iso639Set5HierarchyData hierarchy = Iso639Set5HierarchyReader.Read(arguments[1], records);
            int languageGroupCount = 0;
            int remainderGroupCount = 0;
            foreach (Iso639Set5HierarchyEntry entry in hierarchy.Entries)
            {
                if (entry.GroupKind == Iso639Set5GroupKind.LanguageGroup)
                {
                    languageGroupCount++;
                }
                else if (entry.GroupKind == Iso639Set5GroupKind.RemainderGroup)
                {
                    remainderGroupCount++;
                }
            }

            Console.WriteLine(
                $"Read {hierarchy.Entries.Length} ISO 639 Set 5 hierarchy entries and " +
                $"{hierarchy.Relationships.Length} relationships " +
                $"({languageGroupCount} language groups, {remainderGroupCount} remainder groups).");
            return 0;
        }

        if (arguments.Length == 2 && arguments[0] == "read-iana-registry")
        {
            IanaLanguageSubtagRegistry registry = IanaLanguageSubtagRegistryReader.Read(arguments[1]);
            Console.WriteLine(
                $"Read {registry.Records.Length} IANA Language Subtag Registry records " +
                $"from {registry.FileDate:yyyy-MM-dd}.");
            return 0;
        }

        if (arguments.Length == 4 && arguments[0] == "reconcile-iso")
        {
            Iso639Set2Data set2Data = Iso639Set2Reader.Read(arguments[1]);
            Iso639Set3Record[] set3Records = Iso639Set3Reader.Read(arguments[2]);
            Iso639Set5Record[] set5Records = Iso639Set5Reader.Read(arguments[3]);
            ReconciledIso639Data data = Iso639Reconciler.Reconcile(set2Data, set3Records, set5Records);
            int multiSetEntityCount = 0;
            foreach (ReconciledIso639Entity entity in data.Entities)
            {
                int membershipCount = (entity.InSet1 ? 1 : 0) + (entity.InSet2 ? 1 : 0) +
                    (entity.InSet3 ? 1 : 0) + (entity.InSet5 ? 1 : 0);
                if (membershipCount > 1)
                {
                    multiSetEntityCount++;
                }
            }

            Console.WriteLine(
                $"Reconciled {data.Entities.Length} ISO 639 entities, including " +
                $"{multiSetEntityCount} entities present in multiple Sets.");
            return 0;
        }

        if (arguments.Length == 6 && arguments[0] == "derive-aliases")
        {
            Iso639Set2Data set2Data = Iso639Set2Reader.Read(arguments[1]);
            Iso639Set3Record[] set3Records = Iso639Set3Reader.Read(arguments[2]);
            Iso639Set5Record[] set5Records = Iso639Set5Reader.Read(arguments[3]);
            Iso639Set3RetirementRecord[] retirements = Iso639Set3RetirementReader.Read(arguments[4]);
            IanaLanguageSubtagRegistry registry = IanaLanguageSubtagRegistryReader.Read(arguments[5]);
            ReconciledIso639Data isoData = Iso639Reconciler.Reconcile(set2Data, set3Records, set5Records);
            ReconciledCodeAliasData aliasData = CodeAliasDeriver.Derive(isoData, retirements, registry);
            int unresolvedAliasCount = 0;
            int provenanceCount = 0;
            bool[] encounteredKinds = new bool[(int)ReconciledCodeAliasKind.IanaPreferred + 1];
            foreach (ReconciledCodeAlias alias in aliasData.Aliases)
            {
                encounteredKinds[(int)alias.Kind] = true;
                provenanceCount += alias.Provenance.Length;
                if (alias.EntityKey == null)
                {
                    unresolvedAliasCount++;
                }
            }

            int aliasKindCount = 0;
            foreach (bool encounteredKind in encounteredKinds)
            {
                if (encounteredKind)
                {
                    aliasKindCount++;
                }
            }

            Console.WriteLine(
                $"Derived {aliasData.Aliases.Length} typed code aliases across {aliasKindCount} kinds " +
                $"with {provenanceCount} provenance links; " +
                $"{unresolvedAliasCount} have no unique conceptual entity.");
            return 0;
        }

        if (arguments.Length == 9 && arguments[0] == "validate-data")
        {
            Iso639Set2Data set2Data = Iso639Set2Reader.Read(arguments[1]);
            Iso639Set3Record[] set3Records = Iso639Set3Reader.Read(arguments[2]);
            Iso639Set3NameRecord[] set3Names = Iso639Set3NameIndexReader.Read(arguments[3]);
            Iso639Set3RetirementRecord[] retirements = Iso639Set3RetirementReader.Read(arguments[5]);
            Iso639Set3MacrolanguageMapping[] mappings = Iso639Set3MacrolanguageReader.Read(
                arguments[4],
                set3Records,
                retirements);
            Iso639Set5Record[] set5Records = Iso639Set5Reader.Read(arguments[6]);
            Iso639Set5HierarchyData hierarchy = Iso639Set5HierarchyReader.Read(arguments[7], set5Records);
            IanaLanguageSubtagRegistry registry = IanaLanguageSubtagRegistryReader.Read(arguments[8]);
            ReconciledIso639Data isoData = Iso639Reconciler.Reconcile(set2Data, set3Records, set5Records);
            ReconciledCodeAliasData aliasData = CodeAliasDeriver.Derive(isoData, retirements, registry);
            string[] violations = GeneratorIntegrityValidator.FindViolations(
                set2Data,
                set3Records,
                set3Names,
                mappings,
                retirements,
                set5Records,
                hierarchy,
                registry,
                isoData,
                aliasData);
            if (violations.Length > 0)
            {
                foreach (string violation in violations)
                {
                    Console.Error.WriteLine(violation);
                }

                Console.Error.WriteLine($"Data integrity validation failed with {violations.Length} violations.");
                return 1;
            }

            Console.WriteLine(
                $"Validated {isoData.Entities.Length} reconciled entities and " +
                $"{aliasData.Aliases.Length} typed aliases with no integrity violations.");
            return 0;
        }

        if (arguments.Length == 11 && arguments[0] == "generate-tables")
        {
            Iso639Set2Data set2Data = Iso639Set2Reader.Read(arguments[2]);
            Iso639Set3Record[] set3Records = Iso639Set3Reader.Read(arguments[3]);
            Iso639Set3NameRecord[] set3Names = Iso639Set3NameIndexReader.Read(arguments[4]);
            Iso639Set3RetirementRecord[] retirements = Iso639Set3RetirementReader.Read(arguments[6]);
            Iso639Set3MacrolanguageMapping[] mappings = Iso639Set3MacrolanguageReader.Read(
                arguments[5],
                set3Records,
                retirements);
            Iso639Set5Record[] set5Records = Iso639Set5Reader.Read(arguments[7]);
            Iso639Set5HierarchyData hierarchy = Iso639Set5HierarchyReader.Read(arguments[8], set5Records);
            IanaLanguageSubtagRegistry registry = IanaLanguageSubtagRegistryReader.Read(arguments[9]);
            DataManifest manifest = DataManifestReader.Read(arguments[10]);
            ReconciledIso639Data isoData = Iso639Reconciler.Reconcile(set2Data, set3Records, set5Records);
            ReconciledCodeAliasData aliasData = CodeAliasDeriver.Derive(isoData, retirements, registry);
            string[] violations = GeneratorIntegrityValidator.FindViolations(
                set2Data,
                set3Records,
                set3Names,
                mappings,
                retirements,
                set5Records,
                hierarchy,
                registry,
                isoData,
                aliasData);
            if (violations.Length > 0)
            {
                foreach (string violation in violations)
                {
                    Console.Error.WriteLine(violation);
                }

                Console.Error.WriteLine($"Data integrity validation failed with {violations.Length} violations.");
                return 1;
            }

            GeneratedSourceFile[] files = RuntimeTableGenerator.CreateFiles(
                isoData,
                set3Names,
                mappings,
                retirements,
                hierarchy,
                registry,
                aliasData,
                manifest);
            EmissionFileResult[] results = DeterministicSourceEmitter.Write(arguments[1], files);
            int changedCount = 0;
            int byteCount = 0;
            foreach (EmissionFileResult result in results)
            {
                byteCount += result.ByteCount;
                if (result.Changed)
                {
                    changedCount++;
                }
            }

            Console.WriteLine(
                $"Generated {results.Length} runtime table files ({byteCount} bytes); " +
                $"{changedCount} files changed.");
            return 0;
        }

        Console.Error.WriteLine(
            "Usage: LanguageCodes.DataGenerator " +
            "[fetch-updates <manifest-path> <staging-directory> | " +
            "diff-updates <manifest-path> <staging-directory> | " +
            "accept-updates <manifest-path> <staging-directory> <output-directory> | " +
            "verify-generation <manifest-path> <output-directory> | " +
            "read-iso-639-2 <path> | read-iso-639-3 <path> | read-iso-639-3-names <path> | " +
            "read-iso-639-3-macrolanguages <mappings-path> <core-path> <retirements-path> | " +
            "read-iso-639-3-retirements <path> | read-iso-639-5 <path> | " +
            "read-iso-639-5-hierarchy <html-path> <tsv-path> | read-iana-registry <path> | " +
            "reconcile-iso <set2-path> <set3-path> <set5-path> | " +
            "derive-aliases <set2-path> <set3-path> <set5-path> <retirements-path> <iana-path> | " +
            "validate-data <set2-path> <set3-path> <names-path> <mappings-path> " +
            "<retirements-path> <set5-path> <hierarchy-path> <iana-path> | " +
            "generate-tables <output-dir> <set2-path> <set3-path> <names-path> <mappings-path> " +
            "<retirements-path> <set5-path> <hierarchy-path> <iana-path> <manifest-path>]");
        return 2;
    }
}
