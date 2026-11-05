using System;

namespace currencycodes;

internal class CurrencyUsageRow
{
    internal string TerritoryKey;
    internal string CurrencyCode;
    internal PartialDate SourceStartDate;
    internal PartialDate SourceEndDate;
    internal bool HasInclusiveStartDate;
    internal DateOnly InclusiveStartDate;
    internal bool HasInclusiveEndDate;
    internal DateOnly InclusiveEndDate;
    internal bool IsTender;
    internal bool IsCurrent;
    internal bool FromIsoListOne;
    internal bool FromIsoListThree;
    internal bool FromCldr;

    internal CurrencyUsageRow(
        string territoryKey,
        string currencyCode,
        PartialDate sourceStartDate,
        PartialDate sourceEndDate,
        bool hasInclusiveStartDate,
        DateOnly inclusiveStartDate,
        bool hasInclusiveEndDate,
        DateOnly inclusiveEndDate,
        bool isTender,
        bool isCurrent,
        bool fromIsoListOne,
        bool fromIsoListThree,
        bool fromCldr)
    {
        TerritoryKey = territoryKey;
        CurrencyCode = currencyCode;
        SourceStartDate = sourceStartDate;
        SourceEndDate = sourceEndDate;
        HasInclusiveStartDate = hasInclusiveStartDate;
        InclusiveStartDate = inclusiveStartDate;
        HasInclusiveEndDate = hasInclusiveEndDate;
        InclusiveEndDate = inclusiveEndDate;
        IsTender = isTender;
        IsCurrent = isCurrent;
        FromIsoListOne = fromIsoListOne;
        FromIsoListThree = fromIsoListThree;
        FromCldr = fromCldr;
    }
}
