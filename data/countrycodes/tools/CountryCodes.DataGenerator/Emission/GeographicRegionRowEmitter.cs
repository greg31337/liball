using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using CountryCodes.DataGenerator.Staging;

namespace CountryCodes.DataGenerator.Emission;

internal static class GeographicRegionRowEmitter
{
    private const string FileName = "GeographicRegionTable.g.cs";

    internal static GeographicRegionEmissionResult Emit(
        GeneratorStagingData staging,
        string outputDirectory,
        GeneratedIndexCollector indexes)
    {
        Dictionary<string, RegionEmissionRow> regions = new Dictionary<string, RegionEmissionRow>(
            StringComparer.Ordinal);
        Dictionary<string, RegionNameEmissionRow> names =
            new Dictionary<string, RegionNameEmissionRow>(StringComparer.Ordinal);
        Dictionary<string, CountryRegionEmissionRow> mappings =
            new Dictionary<string, CountryRegionEmissionRow>(StringComparer.Ordinal);
        Dictionary<string, HashSet<string>> mappingLocales =
            new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        foreach (StagedM49Hierarchy row in staging.M49Hierarchy)
        {
            AddRegion(regions, names, row.GlobalCode, row.GlobalName, "World", null, row);
            if (row.RegionCode != null && row.RegionName != null)
            {
                AddRegion(
                    regions,
                    names,
                    row.RegionCode,
                    row.RegionName,
                    "Region",
                    row.GlobalCode.Value,
                    row);
            }

            if (row.SubregionCode != null && row.SubregionName != null)
            {
                AddRegion(
                    regions,
                    names,
                    row.SubregionCode,
                    row.SubregionName,
                    "Subregion",
                    RequireCode(row.RegionCode, row, "subregion"),
                    row);
            }

            if (row.IntermediateRegionCode != null && row.IntermediateRegionName != null)
            {
                AddRegion(
                    regions,
                    names,
                    row.IntermediateRegionCode,
                    row.IntermediateRegionName,
                    "IntermediateRegion",
                    RequireCode(row.SubregionCode, row, "intermediate region"),
                    row);
            }

            AddMapping(mappings, mappingLocales, row);
        }

        List<RegionEmissionRow> regionRows = new List<RegionEmissionRow>(regions.Values);
        List<RegionNameEmissionRow> nameRows = new List<RegionNameEmissionRow>(names.Values);
        List<CountryRegionEmissionRow> mappingRows =
            new List<CountryRegionEmissionRow>(mappings.Values);
        regionRows.Sort(CompareRegions);
        nameRows.Sort(CompareNames);
        mappingRows.Sort(CompareMappings);
        Validate(regionRows, nameRows, mappingRows, mappingLocales, staging.Countries);
        AddIndexes(regionRows, nameRows, mappingRows, indexes);

        string path = Path.Combine(outputDirectory, FileName);
        Directory.CreateDirectory(outputDirectory);
        WriteDeterministic(path, Render(regionRows, nameRows, mappingRows));
        return new GeographicRegionEmissionResult(
            regionRows.Count,
            nameRows.Count,
            mappingRows.Count,
            path);
    }

    private static void AddIndexes(
        IReadOnlyList<RegionEmissionRow> regions,
        IReadOnlyList<RegionNameEmissionRow> names,
        IReadOnlyList<CountryRegionEmissionRow> mappings,
        GeneratedIndexCollector indexes)
    {
        for (int index = 0; index < regions.Count; index += 1)
        {
            indexes.AddExact("GeographicRegionM49", regions[index].Code, index);
        }

        for (int index = 0; index < names.Count; index += 1)
        {
            indexes.AddSearch(
                "GeographicRegionName",
                names[index].SearchKey,
                index);
        }

        for (int index = 0; index < mappings.Count; index += 1)
        {
            indexes.AddExact("CountryRegionAlpha2", mappings[index].Alpha2, index);
        }
    }

    private static void AddRegion(
        Dictionary<string, RegionEmissionRow> regions,
        Dictionary<string, RegionNameEmissionRow> names,
        StagedText code,
        StagedText name,
        string level,
        string? parentCode,
        StagedM49Hierarchy source)
    {
        RegionEmissionRow candidate = new RegionEmissionRow(code, level, parentCode);
        if (!regions.TryGetValue(code.Value, out RegionEmissionRow? existing))
        {
            regions.Add(code.Value, candidate);
        }
        else if (!string.Equals(existing.Level, level, StringComparison.Ordinal) ||
                 !string.Equals(existing.ParentCode, parentCode, StringComparison.Ordinal))
        {
            throw new EmissionException(
                $"M49 aggregate '{code.Value}' has conflicting emitted hierarchy data.");
        }
        else if (CompareOrigins(candidate.Origin, existing.Origin) < 0)
        {
            regions[code.Value] = candidate;
        }

        string locale = source.Origin.Locale ?? string.Empty;
        if (locale.Length == 0)
        {
            throw new EmissionException(
                $"M49 hierarchy row '{source.Origin.UpstreamKey}' has no locale.");
        }

        string normalizedName = NameEmissionUtility.NormalizeDisplay(name.Value);
        string key = code.Value + "\0" + locale;
        RegionNameEmissionRow nameCandidate = new RegionNameEmissionRow(
            code.Value,
            locale,
            normalizedName,
            name.Provenance);
        if (!names.TryGetValue(key, out RegionNameEmissionRow? existingName))
        {
            names.Add(key, nameCandidate);
        }
        else if (!string.Equals(existingName.Name, normalizedName, StringComparison.Ordinal))
        {
            throw new EmissionException(
                $"M49 aggregate '{code.Value}' has conflicting '{locale}' names.");
        }
        else if (CompareOrigins(nameCandidate.Origin, existingName.Origin) < 0)
        {
            names[key] = nameCandidate;
        }
    }

    private static string RequireCode(
        StagedText? value,
        StagedM49Hierarchy row,
        string context)
    {
        if (value == null)
        {
            throw new EmissionException(
                $"M49 country '{row.M49Code.Value}' has a {context} without its parent.");
        }

        return value.Value;
    }

    private static void AddMapping(
        Dictionary<string, CountryRegionEmissionRow> mappings,
        Dictionary<string, HashSet<string>> mappingLocales,
        StagedM49Hierarchy row)
    {
        CountryRegionEmissionRow candidate = new CountryRegionEmissionRow(row);
        if (!mappings.TryGetValue(row.Alpha2.Value, out CountryRegionEmissionRow? existing))
        {
            mappings.Add(row.Alpha2.Value, candidate);
        }
        else if (!existing.HasSameIdentityAndHierarchy(candidate))
        {
            throw new EmissionException(
                $"M49 country '{row.Alpha2.Value}' has locale-dependent hierarchy data.");
        }
        else if (CompareOrigins(candidate.Origin, existing.Origin) < 0)
        {
            mappings[row.Alpha2.Value] = candidate;
        }

        if (!mappingLocales.TryGetValue(row.Alpha2.Value, out HashSet<string>? locales))
        {
            locales = new HashSet<string>(StringComparer.Ordinal);
            mappingLocales.Add(row.Alpha2.Value, locales);
        }

        string locale = row.Origin.Locale ?? string.Empty;
        if (locale.Length == 0 || !locales.Add(locale))
        {
            throw new EmissionException(
                $"M49 country '{row.Alpha2.Value}' has a missing or duplicate locale row.");
        }
    }

    private static void Validate(
        IReadOnlyList<RegionEmissionRow> regions,
        IReadOnlyList<RegionNameEmissionRow> names,
        IReadOnlyList<CountryRegionEmissionRow> mappings,
        IReadOnlyDictionary<string, HashSet<string>> mappingLocales,
        IReadOnlyList<StagedCountry> countries)
    {
        Dictionary<string, RegionEmissionRow> regionsByCode =
            new Dictionary<string, RegionEmissionRow>(StringComparer.Ordinal);
        HashSet<string> countryNumericCodes = new HashSet<string>(StringComparer.Ordinal);
        Dictionary<string, StagedCountry> countriesByAlpha2 =
            new Dictionary<string, StagedCountry>(StringComparer.Ordinal);
        foreach (StagedCountry country in countries)
        {
            countryNumericCodes.Add(country.Numeric.Value);
            countriesByAlpha2.Add(country.Alpha2.Value, country);
        }

        RegionEmissionRow? previousRegion = null;
        int worldCount = 0;
        foreach (RegionEmissionRow region in regions)
        {
            if (previousRegion != null && CompareRegions(previousRegion, region) >= 0)
            {
                throw new EmissionException("M49 aggregate rows are not unique ordinal code order.");
            }

            if (countryNumericCodes.Contains(region.Code))
            {
                throw new EmissionException(
                    $"M49 aggregate '{region.Code}' collides with a current country code.");
            }

            regionsByCode.Add(region.Code, region);
            if (string.Equals(region.Level, "World", StringComparison.Ordinal))
            {
                worldCount += 1;
                if (region.ParentCode != null)
                {
                    throw new EmissionException("The M49 world aggregate must not have a parent.");
                }
            }
            else if (region.ParentCode == null)
            {
                throw new EmissionException($"M49 aggregate '{region.Code}' has no parent.");
            }

            previousRegion = region;
        }

        if (worldCount != 1 || !regionsByCode.ContainsKey("001"))
        {
            throw new EmissionException("M49 hierarchy must contain exactly one world aggregate.");
        }

        foreach (RegionEmissionRow region in regions)
        {
            if (region.ParentCode == null)
            {
                continue;
            }

            if (!regionsByCode.TryGetValue(region.ParentCode, out RegionEmissionRow? parent) ||
                LevelOrder(parent.Level) + 1 != LevelOrder(region.Level))
            {
                throw new EmissionException(
                    $"M49 aggregate '{region.Code}' has an invalid parent '{region.ParentCode}'.");
            }
        }

        HashSet<string> locales = new HashSet<string>(StringComparer.Ordinal);
        Dictionary<string, int> nameCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        RegionNameEmissionRow? previousName = null;
        foreach (RegionNameEmissionRow name in names)
        {
            if (!regionsByCode.ContainsKey(name.RegionCode))
            {
                throw new EmissionException(
                    $"M49 name references unknown aggregate '{name.RegionCode}'.");
            }

            if (name.Name.Length == 0 || name.SearchKey.Length == 0)
            {
                throw new EmissionException(
                    $"M49 aggregate '{name.RegionCode}' has an empty name or search key.");
            }

            if (previousName != null && CompareNames(previousName, name) >= 0)
            {
                throw new EmissionException("M49 aggregate names are not unique stable order.");
            }

            locales.Add(name.Locale);
            nameCounts[name.RegionCode] = nameCounts.GetValueOrDefault(name.RegionCode) + 1;
            previousName = name;
        }

        foreach (RegionEmissionRow region in regions)
        {
            if (!nameCounts.TryGetValue(region.Code, out int count) || count != locales.Count)
            {
                throw new EmissionException(
                    $"M49 aggregate '{region.Code}' does not have one name per locale.");
            }
        }

        CountryRegionEmissionRow? previousMapping = null;
        foreach (CountryRegionEmissionRow mapping in mappings)
        {
            if (previousMapping != null && CompareMappings(previousMapping, mapping) >= 0)
            {
                throw new EmissionException("M49 country mappings are not unique alpha-2 order.");
            }

            if (!countriesByAlpha2.TryGetValue(mapping.Alpha2, out StagedCountry? country) ||
                !string.Equals(country.Alpha3.Value, mapping.Alpha3, StringComparison.Ordinal) ||
                !string.Equals(country.Numeric.Value, mapping.Numeric, StringComparison.Ordinal))
            {
                throw new EmissionException(
                    $"M49 mapping '{mapping.Alpha2}' does not match the ISO-derived country row.");
            }

            ValidateMappingHierarchy(mapping, regionsByCode);
            if (!mappingLocales.TryGetValue(mapping.Alpha2, out HashSet<string>? rowLocales) ||
                rowLocales.Count != locales.Count)
            {
                throw new EmissionException(
                    $"M49 mapping '{mapping.Alpha2}' does not occur in every locale.");
            }

            previousMapping = mapping;
        }
    }

    private static void ValidateMappingHierarchy(
        CountryRegionEmissionRow mapping,
        IReadOnlyDictionary<string, RegionEmissionRow> regions)
    {
        RequireMappingLevel(mapping.GlobalCode, "World", mapping.Alpha2, regions);
        if (mapping.RegionCode != null)
        {
            RequireMappingLevel(mapping.RegionCode, "Region", mapping.Alpha2, regions);
            RequireParent(mapping.RegionCode, mapping.GlobalCode, mapping.Alpha2, regions);
        }

        if (mapping.SubregionCode != null)
        {
            RequireMappingLevel(mapping.SubregionCode, "Subregion", mapping.Alpha2, regions);
            RequireParent(
                mapping.SubregionCode,
                RequireMappingParent(mapping.RegionCode, mapping.Alpha2, "subregion"),
                mapping.Alpha2,
                regions);
        }

        if (mapping.IntermediateRegionCode != null)
        {
            RequireMappingLevel(
                mapping.IntermediateRegionCode,
                "IntermediateRegion",
                mapping.Alpha2,
                regions);
            RequireParent(
                mapping.IntermediateRegionCode,
                RequireMappingParent(mapping.SubregionCode, mapping.Alpha2, "intermediate region"),
                mapping.Alpha2,
                regions);
        }
    }

    private static string RequireMappingParent(string? code, string alpha2, string context)
    {
        if (code == null)
        {
            throw new EmissionException(
                $"M49 mapping '{alpha2}' has a {context} without its parent.");
        }

        return code;
    }

    private static void RequireMappingLevel(
        string code,
        string level,
        string alpha2,
        IReadOnlyDictionary<string, RegionEmissionRow> regions)
    {
        if (!regions.TryGetValue(code, out RegionEmissionRow? region) ||
            !string.Equals(region.Level, level, StringComparison.Ordinal))
        {
            throw new EmissionException(
                $"M49 mapping '{alpha2}' references invalid {level} aggregate '{code}'.");
        }
    }

    private static void RequireParent(
        string code,
        string parentCode,
        string alpha2,
        IReadOnlyDictionary<string, RegionEmissionRow> regions)
    {
        if (!string.Equals(regions[code].ParentCode, parentCode, StringComparison.Ordinal))
        {
            throw new EmissionException(
                $"M49 mapping '{alpha2}' does not follow the parent of aggregate '{code}'.");
        }
    }

    private static int CompareRegions(RegionEmissionRow left, RegionEmissionRow right)
    {
        return string.CompareOrdinal(left.Code, right.Code);
    }

    private static int CompareNames(RegionNameEmissionRow left, RegionNameEmissionRow right)
    {
        int comparison = string.CompareOrdinal(left.RegionCode, right.RegionCode);
        return comparison == 0 ? string.CompareOrdinal(left.Locale, right.Locale) : comparison;
    }

    private static int CompareMappings(
        CountryRegionEmissionRow left,
        CountryRegionEmissionRow right)
    {
        return string.CompareOrdinal(left.Alpha2, right.Alpha2);
    }

    private static int CompareOrigins(StagingOrigin left, StagingOrigin right)
    {
        int comparison = string.CompareOrdinal(left.Locale ?? string.Empty, right.Locale ?? string.Empty);
        return comparison == 0
            ? string.CompareOrdinal(left.UpstreamKey, right.UpstreamKey)
            : comparison;
    }

    private static int LevelOrder(string level)
    {
        if (string.Equals(level, "World", StringComparison.Ordinal))
        {
            return 0;
        }

        if (string.Equals(level, "Region", StringComparison.Ordinal))
        {
            return 1;
        }

        if (string.Equals(level, "Subregion", StringComparison.Ordinal))
        {
            return 2;
        }

        return 3;
    }

    private static string Render(
        IReadOnlyList<RegionEmissionRow> regions,
        IReadOnlyList<RegionNameEmissionRow> names,
        IReadOnlyList<CountryRegionEmissionRow> mappings)
    {
        StringBuilder builder = new StringBuilder(160_000);
        Line(builder, "// <auto-generated />");
        Line(builder, "namespace countrycodes;");
        Line(builder, string.Empty);
        RenderTypes(builder);
        Line(builder, string.Empty);
        Line(builder, "internal static class GeographicRegionTable");
        Line(builder, "{");
        RenderRegions(builder, regions);
        RenderNames(builder, names);
        RenderMappings(builder, mappings);
        Line(builder, "}");
        return builder.ToString();
    }

    private static void RenderTypes(StringBuilder builder)
    {
        Line(builder, "internal enum GeneratedGeographicRegionLevel");
        Line(builder, "{");
        Line(builder, "    World = 0,");
        Line(builder, "    Region = 1,");
        Line(builder, "    Subregion = 2,");
        Line(builder, "    IntermediateRegion = 3");
        Line(builder, "}");
        Line(builder, string.Empty);
        Line(builder, "internal class GeneratedGeographicRegionRow");
        Line(builder, "{");
        Line(builder, "    internal string M49Code;");
        Line(builder, "    internal int M49Value;");
        Line(builder, "    internal GeneratedGeographicRegionLevel Level;");
        Line(builder, "    internal bool HasParent;");
        Line(builder, "    internal string ParentM49Code;");
        Line(builder, "    internal string SourceId;");
        Line(builder, "    internal string SourceRevision;");
        Line(builder, "    internal string UpstreamKey;");
        Line(builder, "    internal string EffectiveDate;");
        Line(builder, "    internal string SourceField;");
        Line(builder, string.Empty);
        Line(builder, "    internal GeneratedGeographicRegionRow(");
        Line(builder, "        string m49Code,");
        Line(builder, "        int m49Value,");
        Line(builder, "        GeneratedGeographicRegionLevel level,");
        Line(builder, "        bool hasParent,");
        Line(builder, "        string parentM49Code,");
        Line(builder, "        string sourceId,");
        Line(builder, "        string sourceRevision,");
        Line(builder, "        string upstreamKey,");
        Line(builder, "        string effectiveDate,");
        Line(builder, "        string sourceField)");
        Line(builder, "    {");
        Line(builder, "        M49Code = m49Code;");
        Line(builder, "        M49Value = m49Value;");
        Line(builder, "        Level = level;");
        Line(builder, "        HasParent = hasParent;");
        Line(builder, "        ParentM49Code = parentM49Code;");
        Line(builder, "        SourceId = sourceId;");
        Line(builder, "        SourceRevision = sourceRevision;");
        Line(builder, "        UpstreamKey = upstreamKey;");
        Line(builder, "        EffectiveDate = effectiveDate;");
        Line(builder, "        SourceField = sourceField;");
        Line(builder, "    }");
        Line(builder, "}");
        Line(builder, string.Empty);
        RenderNameType(builder);
        Line(builder, string.Empty);
        RenderMappingType(builder);
    }

    private static void RenderNameType(StringBuilder builder)
    {
        Line(builder, "internal class GeneratedGeographicRegionNameRow");
        Line(builder, "{");
        Line(builder, "    internal string RegionM49Code;");
        Line(builder, "    internal string Locale;");
        Line(builder, "    internal string Name;");
        Line(builder, "    internal string SearchKey;");
        Line(builder, "    internal string SourceId;");
        Line(builder, "    internal string SourceRevision;");
        Line(builder, "    internal string UpstreamKey;");
        Line(builder, "    internal string EffectiveDate;");
        Line(builder, "    internal string SourceField;");
        Line(builder, string.Empty);
        Line(builder, "    internal GeneratedGeographicRegionNameRow(");
        Line(builder, "        string regionM49Code,");
        Line(builder, "        string locale,");
        Line(builder, "        string name,");
        Line(builder, "        string searchKey,");
        Line(builder, "        string sourceId,");
        Line(builder, "        string sourceRevision,");
        Line(builder, "        string upstreamKey,");
        Line(builder, "        string effectiveDate,");
        Line(builder, "        string sourceField)");
        Line(builder, "    {");
        Line(builder, "        RegionM49Code = regionM49Code;");
        Line(builder, "        Locale = locale;");
        Line(builder, "        Name = name;");
        Line(builder, "        SearchKey = searchKey;");
        Line(builder, "        SourceId = sourceId;");
        Line(builder, "        SourceRevision = sourceRevision;");
        Line(builder, "        UpstreamKey = upstreamKey;");
        Line(builder, "        EffectiveDate = effectiveDate;");
        Line(builder, "        SourceField = sourceField;");
        Line(builder, "    }");
        Line(builder, "}");
    }

    private static void RenderMappingType(StringBuilder builder)
    {
        Line(builder, "internal class GeneratedCountryRegionMappingRow");
        Line(builder, "{");
        Line(builder, "    internal string CountryAlpha2;");
        Line(builder, "    internal string CountryAlpha3;");
        Line(builder, "    internal string CountryNumericText;");
        Line(builder, "    internal int CountryNumericValue;");
        Line(builder, "    internal string WorldM49Code;");
        Line(builder, "    internal bool HasRegion;");
        Line(builder, "    internal string RegionM49Code;");
        Line(builder, "    internal bool HasSubregion;");
        Line(builder, "    internal string SubregionM49Code;");
        Line(builder, "    internal bool HasIntermediateRegion;");
        Line(builder, "    internal string IntermediateRegionM49Code;");
        Line(builder, "    internal string SourceId;");
        Line(builder, "    internal string SourceRevision;");
        Line(builder, "    internal string UpstreamKey;");
        Line(builder, "    internal string EffectiveDate;");
        Line(builder, string.Empty);
        Line(builder, "    internal GeneratedCountryRegionMappingRow(");
        Line(builder, "        string countryAlpha2,");
        Line(builder, "        string countryAlpha3,");
        Line(builder, "        string countryNumericText,");
        Line(builder, "        int countryNumericValue,");
        Line(builder, "        string worldM49Code,");
        Line(builder, "        bool hasRegion,");
        Line(builder, "        string regionM49Code,");
        Line(builder, "        bool hasSubregion,");
        Line(builder, "        string subregionM49Code,");
        Line(builder, "        bool hasIntermediateRegion,");
        Line(builder, "        string intermediateRegionM49Code,");
        Line(builder, "        string sourceId,");
        Line(builder, "        string sourceRevision,");
        Line(builder, "        string upstreamKey,");
        Line(builder, "        string effectiveDate)");
        Line(builder, "    {");
        Line(builder, "        CountryAlpha2 = countryAlpha2;");
        Line(builder, "        CountryAlpha3 = countryAlpha3;");
        Line(builder, "        CountryNumericText = countryNumericText;");
        Line(builder, "        CountryNumericValue = countryNumericValue;");
        Line(builder, "        WorldM49Code = worldM49Code;");
        Line(builder, "        HasRegion = hasRegion;");
        Line(builder, "        RegionM49Code = regionM49Code;");
        Line(builder, "        HasSubregion = hasSubregion;");
        Line(builder, "        SubregionM49Code = subregionM49Code;");
        Line(builder, "        HasIntermediateRegion = hasIntermediateRegion;");
        Line(builder, "        IntermediateRegionM49Code = intermediateRegionM49Code;");
        Line(builder, "        SourceId = sourceId;");
        Line(builder, "        SourceRevision = sourceRevision;");
        Line(builder, "        UpstreamKey = upstreamKey;");
        Line(builder, "        EffectiveDate = effectiveDate;");
        Line(builder, "    }");
        Line(builder, "}");
    }

    private static void RenderRegions(
        StringBuilder builder,
        IReadOnlyList<RegionEmissionRow> regions)
    {
        Line(builder, "    internal static readonly GeneratedGeographicRegionRow[] Rows =");
        Line(builder, "    {");
        foreach (RegionEmissionRow region in regions)
        {
            StagingOrigin origin = region.Origin;
            Line(builder, "        new GeneratedGeographicRegionRow(");
            Line(builder, $"            {Literal(region.Code)},");
            Line(builder, $"            {ParseNumeric(region.Code)},");
            Line(builder, $"            GeneratedGeographicRegionLevel.{region.Level},");
            Line(builder, $"            {(region.ParentCode != null ? "true" : "false")},");
            Line(builder, $"            {Literal(region.ParentCode ?? string.Empty)},");
            Line(builder, $"            {Literal(origin.SourceId)},");
            Line(builder, $"            {Literal(origin.SourceRevision)},");
            Line(builder, $"            {Literal(origin.UpstreamKey)},");
            Line(builder, $"            {Literal(origin.EffectiveDate)},");
            Line(builder, $"            {Literal(region.CodeField.Provenance.SourceField)}),");
        }

        Line(builder, "    };");
    }

    private static void RenderNames(
        StringBuilder builder,
        IReadOnlyList<RegionNameEmissionRow> names)
    {
        Line(builder, string.Empty);
        Line(builder, "    internal static readonly GeneratedGeographicRegionNameRow[] Names =");
        Line(builder, "    {");
        foreach (RegionNameEmissionRow name in names)
        {
            StagingOrigin origin = name.Origin;
            Line(builder, "        new GeneratedGeographicRegionNameRow(");
            Line(builder, $"            {Literal(name.RegionCode)},");
            Line(builder, $"            {Literal(name.Locale)},");
            Line(builder, $"            {Literal(name.Name)},");
            Line(builder, $"            {Literal(name.SearchKey)},");
            Line(builder, $"            {Literal(origin.SourceId)},");
            Line(builder, $"            {Literal(origin.SourceRevision)},");
            Line(builder, $"            {Literal(origin.UpstreamKey)},");
            Line(builder, $"            {Literal(origin.EffectiveDate)},");
            Line(builder, $"            {Literal(name.Provenance.SourceField)}),");
        }

        Line(builder, "    };");
    }

    private static void RenderMappings(
        StringBuilder builder,
        IReadOnlyList<CountryRegionEmissionRow> mappings)
    {
        Line(builder, string.Empty);
        Line(builder, "    internal static readonly GeneratedCountryRegionMappingRow[] CountryMappings =");
        Line(builder, "    {");
        foreach (CountryRegionEmissionRow mapping in mappings)
        {
            StagingOrigin origin = mapping.Origin;
            Line(builder, "        new GeneratedCountryRegionMappingRow(");
            Line(builder, $"            {Literal(mapping.Alpha2)},");
            Line(builder, $"            {Literal(mapping.Alpha3)},");
            Line(builder, $"            {Literal(mapping.Numeric)},");
            Line(builder, $"            {ParseNumeric(mapping.Numeric)},");
            Line(builder, $"            {Literal(mapping.GlobalCode)},");
            Line(builder, $"            {(mapping.RegionCode != null ? "true" : "false")},");
            Line(builder, $"            {Literal(mapping.RegionCode ?? string.Empty)},");
            Line(builder, $"            {(mapping.SubregionCode != null ? "true" : "false")},");
            Line(builder, $"            {Literal(mapping.SubregionCode ?? string.Empty)},");
            Line(builder, $"            {(mapping.IntermediateRegionCode != null ? "true" : "false")},");
            Line(builder, $"            {Literal(mapping.IntermediateRegionCode ?? string.Empty)},");
            Line(builder, $"            {Literal(origin.SourceId)},");
            Line(builder, $"            {Literal(origin.SourceRevision)},");
            Line(builder, $"            {Literal(origin.UpstreamKey)},");
            Line(builder, $"            {Literal(origin.EffectiveDate)}),");
        }

        Line(builder, "    };");
    }

    private static string ParseNumeric(string value)
    {
        return int.Parse(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
    }

    private static string Literal(string value)
    {
        return "\"" + value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Replace("\t", "\\t", StringComparison.Ordinal) + "\"";
    }

    private static void Line(StringBuilder builder, string value)
    {
        builder.Append(value);
        builder.Append('\n');
    }

    private static void WriteDeterministic(string path, string content)
    {
        File.WriteAllText(path, content, new UTF8Encoding(false));
    }

    private sealed class RegionEmissionRow
    {
        internal RegionEmissionRow(StagedText code, string level, string? parentCode)
        {
            CodeField = code;
            Level = level;
            ParentCode = parentCode;
        }

        internal StagedText CodeField { get; }
        internal string Code => CodeField.Value;
        internal string Level { get; }
        internal string? ParentCode { get; }
        internal StagingOrigin Origin => CodeField.Provenance.Origin;
    }

    private sealed class RegionNameEmissionRow
    {
        internal RegionNameEmissionRow(
            string regionCode,
            string locale,
            string name,
            FieldProvenance provenance)
        {
            RegionCode = regionCode;
            Locale = locale;
            Name = name;
            SearchKey = NameEmissionUtility.CreateSearchKey(name);
            Provenance = provenance;
        }

        internal string RegionCode { get; }
        internal string Locale { get; }
        internal string Name { get; }
        internal string SearchKey { get; }
        internal FieldProvenance Provenance { get; }
        internal StagingOrigin Origin => Provenance.Origin;
    }

    private sealed class CountryRegionEmissionRow
    {
        internal CountryRegionEmissionRow(StagedM49Hierarchy row)
        {
            Alpha2 = row.Alpha2.Value;
            Alpha3 = row.Alpha3.Value;
            Numeric = row.M49Code.Value;
            GlobalCode = row.GlobalCode.Value;
            RegionCode = row.RegionCode?.Value;
            SubregionCode = row.SubregionCode?.Value;
            IntermediateRegionCode = row.IntermediateRegionCode?.Value;
            Origin = row.Origin;
        }

        internal string Alpha2 { get; }
        internal string Alpha3 { get; }
        internal string Numeric { get; }
        internal string GlobalCode { get; }
        internal string? RegionCode { get; }
        internal string? SubregionCode { get; }
        internal string? IntermediateRegionCode { get; }
        internal StagingOrigin Origin { get; }

        internal bool HasSameIdentityAndHierarchy(CountryRegionEmissionRow other)
        {
            return string.Equals(Alpha2, other.Alpha2, StringComparison.Ordinal) &&
                   string.Equals(Alpha3, other.Alpha3, StringComparison.Ordinal) &&
                   string.Equals(Numeric, other.Numeric, StringComparison.Ordinal) &&
                   string.Equals(GlobalCode, other.GlobalCode, StringComparison.Ordinal) &&
                   string.Equals(RegionCode, other.RegionCode, StringComparison.Ordinal) &&
                   string.Equals(SubregionCode, other.SubregionCode, StringComparison.Ordinal) &&
                   string.Equals(
                       IntermediateRegionCode,
                       other.IntermediateRegionCode,
                       StringComparison.Ordinal);
        }
    }
}
