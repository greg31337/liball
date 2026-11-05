using System;

namespace LanguageCodes.DataGenerator.Model;

internal sealed class IanaLanguageSubtagRegistry
{
    internal IanaLanguageSubtagRegistry(DateOnly fileDate, IanaRegistryRecord[] records)
    {
        FileDate = fileDate;
        Records = records;
    }

    internal DateOnly FileDate { get; }

    internal IanaRegistryRecord[] Records { get; }
}
