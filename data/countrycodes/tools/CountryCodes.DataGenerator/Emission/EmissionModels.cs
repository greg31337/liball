using System;

namespace CountryCodes.DataGenerator.Emission;

internal sealed class CountryRowEmissionSummary
{
    internal CountryRowEmissionSummary(
        int countryCount,
        int reservedCodeCount,
        int userAssignedRangeCount,
        int countryNameCount,
        int subdivisionCount,
        int subdivisionNameCount,
        int subdivisionAliasCount,
        int subdivisionReplacementCount,
        int historicalCountryCount,
        int countryChangeCount,
        int predecessorLinkCount,
        int successorLinkCount,
        int geographicRegionCount,
        int geographicRegionNameCount,
        int countryRegionMappingCount,
        int exactIndexCount,
        int searchIndexCount,
        int sourceRevisionCount,
        int contentHashCount,
        string aggregateContentChecksum,
        string countryTablePath,
        string reservedCodeTablePath,
        string countryNameTablePath,
        string subdivisionTablePath,
        string subdivisionNameTablePath,
        string historicalCountryTablePath,
        string countryChangeTablePath,
        string geographicRegionTablePath,
        string generatedIndexTablePath,
        string generatedMetadataPath)
    {
        CountryCount = countryCount;
        ReservedCodeCount = reservedCodeCount;
        UserAssignedRangeCount = userAssignedRangeCount;
        CountryNameCount = countryNameCount;
        SubdivisionCount = subdivisionCount;
        SubdivisionNameCount = subdivisionNameCount;
        SubdivisionAliasCount = subdivisionAliasCount;
        SubdivisionReplacementCount = subdivisionReplacementCount;
        HistoricalCountryCount = historicalCountryCount;
        CountryChangeCount = countryChangeCount;
        PredecessorLinkCount = predecessorLinkCount;
        SuccessorLinkCount = successorLinkCount;
        GeographicRegionCount = geographicRegionCount;
        GeographicRegionNameCount = geographicRegionNameCount;
        CountryRegionMappingCount = countryRegionMappingCount;
        ExactIndexCount = exactIndexCount;
        SearchIndexCount = searchIndexCount;
        SourceRevisionCount = sourceRevisionCount;
        ContentHashCount = contentHashCount;
        AggregateContentChecksum = aggregateContentChecksum;
        CountryTablePath = countryTablePath;
        ReservedCodeTablePath = reservedCodeTablePath;
        CountryNameTablePath = countryNameTablePath;
        SubdivisionTablePath = subdivisionTablePath;
        SubdivisionNameTablePath = subdivisionNameTablePath;
        HistoricalCountryTablePath = historicalCountryTablePath;
        CountryChangeTablePath = countryChangeTablePath;
        GeographicRegionTablePath = geographicRegionTablePath;
        GeneratedIndexTablePath = generatedIndexTablePath;
        GeneratedMetadataPath = generatedMetadataPath;
    }

    internal int CountryCount { get; }

    internal int ReservedCodeCount { get; }

    internal int UserAssignedRangeCount { get; }

    internal int CountryNameCount { get; }

    internal int SubdivisionCount { get; }

    internal int SubdivisionNameCount { get; }

    internal int SubdivisionAliasCount { get; }

    internal int SubdivisionReplacementCount { get; }

    internal int HistoricalCountryCount { get; }

    internal int CountryChangeCount { get; }

    internal int PredecessorLinkCount { get; }

    internal int SuccessorLinkCount { get; }

    internal int GeographicRegionCount { get; }

    internal int GeographicRegionNameCount { get; }

    internal int CountryRegionMappingCount { get; }

    internal int ExactIndexCount { get; }

    internal int SearchIndexCount { get; }

    internal int SourceRevisionCount { get; }

    internal int ContentHashCount { get; }

    internal string AggregateContentChecksum { get; }

    internal string CountryTablePath { get; }

    internal string ReservedCodeTablePath { get; }

    internal string CountryNameTablePath { get; }

    internal string SubdivisionTablePath { get; }

    internal string SubdivisionNameTablePath { get; }

    internal string HistoricalCountryTablePath { get; }

    internal string CountryChangeTablePath { get; }

    internal string GeographicRegionTablePath { get; }

    internal string GeneratedIndexTablePath { get; }

    internal string GeneratedMetadataPath { get; }
}

internal sealed class CountryNameEmissionResult
{
    internal CountryNameEmissionResult(int rowCount, string path)
    {
        RowCount = rowCount;
        Path = path;
    }

    internal int RowCount { get; }

    internal string Path { get; }
}

internal sealed class SubdivisionEmissionResult
{
    internal SubdivisionEmissionResult(
        int rowCount,
        int nameCount,
        int aliasCount,
        int replacementCount,
        string tablePath,
        string nameTablePath)
    {
        RowCount = rowCount;
        NameCount = nameCount;
        AliasCount = aliasCount;
        ReplacementCount = replacementCount;
        TablePath = tablePath;
        NameTablePath = nameTablePath;
    }

    internal int RowCount { get; }

    internal int NameCount { get; }

    internal int AliasCount { get; }

    internal int ReplacementCount { get; }

    internal string TablePath { get; }

    internal string NameTablePath { get; }
}

internal sealed class HistoricalCountryEmissionResult
{
    internal HistoricalCountryEmissionResult(
        int rowCount,
        int changeCount,
        int predecessorCount,
        int successorCount,
        string tablePath,
        string changeTablePath)
    {
        RowCount = rowCount;
        ChangeCount = changeCount;
        PredecessorCount = predecessorCount;
        SuccessorCount = successorCount;
        TablePath = tablePath;
        ChangeTablePath = changeTablePath;
    }

    internal int RowCount { get; }

    internal int ChangeCount { get; }

    internal int PredecessorCount { get; }

    internal int SuccessorCount { get; }

    internal string TablePath { get; }

    internal string ChangeTablePath { get; }
}

internal sealed class GeographicRegionEmissionResult
{
    internal GeographicRegionEmissionResult(
        int regionCount,
        int nameCount,
        int mappingCount,
        string tablePath)
    {
        RegionCount = regionCount;
        NameCount = nameCount;
        MappingCount = mappingCount;
        TablePath = tablePath;
    }

    internal int RegionCount { get; }

    internal int NameCount { get; }

    internal int MappingCount { get; }

    internal string TablePath { get; }
}

internal sealed class GeneratedIndexEmissionResult
{
    internal GeneratedIndexEmissionResult(int exactCount, int searchCount, string path)
    {
        ExactCount = exactCount;
        SearchCount = searchCount;
        Path = path;
    }

    internal int ExactCount { get; }

    internal int SearchCount { get; }

    internal string Path { get; }
}

internal sealed class GeneratedMetadataEmissionResult
{
    internal GeneratedMetadataEmissionResult(
        int sourceRevisionCount,
        int contentHashCount,
        string aggregateContentChecksum,
        string path)
    {
        SourceRevisionCount = sourceRevisionCount;
        ContentHashCount = contentHashCount;
        AggregateContentChecksum = aggregateContentChecksum;
        Path = path;
    }

    internal int SourceRevisionCount { get; }

    internal int ContentHashCount { get; }

    internal string AggregateContentChecksum { get; }

    internal string Path { get; }
}

internal sealed class EmissionException : Exception
{
    internal EmissionException(string message)
        : base(message)
    {
    }
}
