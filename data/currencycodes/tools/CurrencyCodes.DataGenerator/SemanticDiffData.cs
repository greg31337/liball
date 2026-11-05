using System.Collections.Generic;

namespace CurrencyCodes.DataGenerator;

internal sealed class SemanticDiffData
{
    internal SemanticDiffData(IReadOnlyList<SemanticDiffEntry> entries)
    {
        Entries = entries;
    }

    internal IReadOnlyList<SemanticDiffEntry> Entries { get; }
}
