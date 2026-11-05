using System.Collections.Generic;

namespace CurrencyCodes.DataGenerator;

internal sealed class IsoListThreeData
{
    internal IsoListThreeData(string publicationDate, IReadOnlyList<IsoListThreeRow> rows)
    {
        PublicationDate = publicationDate;
        Rows = rows;
    }

    internal string PublicationDate { get; }

    internal IReadOnlyList<IsoListThreeRow> Rows { get; }
}
