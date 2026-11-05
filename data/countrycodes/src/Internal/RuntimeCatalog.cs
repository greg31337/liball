using System;
using System.Threading;

namespace countrycodes;

internal class RuntimeCatalogState
{
    internal GeneratedCountryRow[] Countries;
    internal GeneratedReservedCodeRow[] ReservedCodes;
    internal GeneratedUserAssignedRange[] UserAssignedRanges;
    internal GeneratedSubdivisionRow[] Subdivisions;
    internal GeneratedSubdivisionAliasRow[] SubdivisionAliases;
    internal GeneratedSubdivisionReplacementRow[] SubdivisionReplacements;
    internal GeneratedHistoricalCountryRow[] HistoricalCountries;
    internal GeneratedCountryChangeRow[] CountryChanges;
    internal GeneratedCountryChangeLinkRow[] CountryChangePredecessors;
    internal GeneratedCountryChangeLinkRow[] CountryChangeSuccessors;
    internal GeneratedCountryNameRow[] CountryNames;
    internal GeneratedSubdivisionNameRow[] SubdivisionNames;
    internal GeneratedGeographicRegionRow[] Regions;
    internal GeneratedGeographicRegionNameRow[] RegionNames;
    internal GeneratedCountryRegionMappingRow[] CountryRegionMappings;
    internal GeneratedExactIndexRow[] ExactIndexRows;
    internal GeneratedSearchIndexRow[] SearchIndexRows;
    internal string DataIdentity;

    internal RuntimeCatalogState()
    {
        Countries = (GeneratedCountryRow[])CountryTable.Rows.Clone();
        ReservedCodes = (GeneratedReservedCodeRow[])ReservedCodeTable.Rows.Clone();
        UserAssignedRanges =
            (GeneratedUserAssignedRange[])ReservedCodeTable.UserAssignedRanges.Clone();
        Subdivisions = (GeneratedSubdivisionRow[])SubdivisionTable.Rows.Clone();
        SubdivisionAliases =
            (GeneratedSubdivisionAliasRow[])SubdivisionTable.Aliases.Clone();
        SubdivisionReplacements =
            (GeneratedSubdivisionReplacementRow[])SubdivisionTable.Replacements.Clone();
        HistoricalCountries =
            (GeneratedHistoricalCountryRow[])HistoricalCountryTable.Rows.Clone();
        CountryChanges = (GeneratedCountryChangeRow[])CountryChangeTable.Rows.Clone();
        CountryChangePredecessors =
            (GeneratedCountryChangeLinkRow[])CountryChangeTable.Predecessors.Clone();
        CountryChangeSuccessors =
            (GeneratedCountryChangeLinkRow[])CountryChangeTable.Successors.Clone();
        CountryNames = (GeneratedCountryNameRow[])CountryNameTable.Rows.Clone();
        SubdivisionNames =
            (GeneratedSubdivisionNameRow[])SubdivisionNameTable.Rows.Clone();
        Regions =
            (GeneratedGeographicRegionRow[])GeographicRegionTable.Rows.Clone();
        RegionNames =
            (GeneratedGeographicRegionNameRow[])GeographicRegionTable.Names.Clone();
        CountryRegionMappings =
            (GeneratedCountryRegionMappingRow[])GeographicRegionTable.CountryMappings.Clone();
        ExactIndexRows =
            (GeneratedExactIndexRow[])GeneratedIndexTable.ExactRows.Clone();
        SearchIndexRows =
            (GeneratedSearchIndexRow[])GeneratedIndexTable.SearchRows.Clone();
        DataIdentity = GeneratedDataMetadata.AggregateContentChecksum;
    }
}

internal static class RuntimeCatalog
{
    internal static readonly Lazy<RuntimeCatalogState> PublishedState =
        new Lazy<RuntimeCatalogState>(
            () => new RuntimeCatalogState(),
            LazyThreadSafetyMode.ExecutionAndPublication);

    internal static RuntimeCatalogState GetState()
    {
        return PublishedState.Value;
    }
}
