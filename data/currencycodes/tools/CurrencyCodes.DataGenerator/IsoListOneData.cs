using System.Collections.Generic;

namespace CurrencyCodes.DataGenerator;

internal sealed class IsoListOneData
{
    internal IsoListOneData(string publicationDate, IReadOnlyList<IsoListOneRow> rows)
    {
        PublicationDate = publicationDate;
        Rows = rows;
    }

    internal string PublicationDate { get; }

    internal IReadOnlyList<IsoListOneRow> Rows { get; }
}
