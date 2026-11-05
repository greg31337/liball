using System;

namespace CurrencyCodes.DataGenerator;

internal sealed class CldrCurrencyUsage
{
    internal CldrCurrencyUsage(
        string territoryCode,
        string currencyCode,
        IsoPartialDate sourceStartDate,
        IsoPartialDate sourceEndDate,
        bool hasInclusiveStartDate,
        DateOnly inclusiveStartDate,
        bool hasInclusiveEndDate,
        DateOnly inclusiveEndDate,
        bool tender,
        bool tenderExplicit,
        string? startTimeZone,
        string? endTimeZone)
    {
        TerritoryCode = territoryCode;
        CurrencyCode = currencyCode;
        SourceStartDate = sourceStartDate;
        SourceEndDate = sourceEndDate;
        HasInclusiveStartDate = hasInclusiveStartDate;
        InclusiveStartDate = inclusiveStartDate;
        HasInclusiveEndDate = hasInclusiveEndDate;
        InclusiveEndDate = inclusiveEndDate;
        Tender = tender;
        TenderExplicit = tenderExplicit;
        StartTimeZone = startTimeZone;
        EndTimeZone = endTimeZone;
    }

    internal string TerritoryCode { get; }

    internal string CurrencyCode { get; }

    internal IsoPartialDate SourceStartDate { get; }

    internal IsoPartialDate SourceEndDate { get; }

    internal bool HasInclusiveStartDate { get; }

    internal DateOnly InclusiveStartDate { get; }

    internal bool HasInclusiveEndDate { get; }

    internal DateOnly InclusiveEndDate { get; }

    internal bool Tender { get; }

    internal bool TenderExplicit { get; }

    internal string? StartTimeZone { get; }

    internal string? EndTimeZone { get; }
}
