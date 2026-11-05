namespace iso20022;

/// <summary>Converts ISO 20022 generic message documents between XML-oriented models and JSON.</summary>
public interface IMessageJsonConverter
{
    /// <summary>Writes a generic message document in the ISO 20022 JSON object form.</summary>
    /// <param name="document">The generic message document.</param>
    /// <returns>The conversion result.</returns>
    MessageJsonResult WriteGenericDocument(MessageDocument? document);
    /// <summary>Reads ISO 20022 JSON into a generic message document and reconstructed XML.</summary>
    /// <param name="json">The JSON text.</param>
    /// <returns>The conversion result.</returns>
    MessageJsonResult ReadGenericText(string? json);
}
