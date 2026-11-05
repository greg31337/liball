using System.Collections.Generic;

namespace CurrencyCodes.DataGenerator;

internal sealed class CldrTerritoryCodeData
{
    internal CldrTerritoryCodeData(
        IReadOnlyList<CldrTerritoryCode> territoryCodes,
        IReadOnlyList<CldrTerritoryAlias> aliases)
    {
        TerritoryCodes = territoryCodes;
        Aliases = aliases;
    }

    internal IReadOnlyList<CldrTerritoryCode> TerritoryCodes { get; }

    internal IReadOnlyList<CldrTerritoryAlias> Aliases { get; }
}
