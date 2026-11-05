namespace LanguageCodes.DataGenerator.Model;

internal sealed class Iso639Set3MacrolanguageMapping
{
    internal Iso639Set3MacrolanguageMapping(
        string macrolanguageId,
        string individualLanguageId,
        Iso639Set3MappingStatus status)
    {
        MacrolanguageId = macrolanguageId;
        IndividualLanguageId = individualLanguageId;
        Status = status;
    }

    internal string MacrolanguageId { get; }

    internal string IndividualLanguageId { get; }

    internal Iso639Set3MappingStatus Status { get; }
}
