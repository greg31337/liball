namespace fix;

/// <summary>Identifies and dispatches generic FIX messages without generated message types.</summary>
public interface IMessageCracker
{
    /// <summary>Identifies and dispatches one administrative or application message.</summary>
    /// <param name="message">The message to dispatch.</param>
    /// <param name="transportDictionary">The transport dictionary used for administrative messages.</param>
    /// <param name="applicationDictionary">The application dictionary used for application messages.</param>
    /// <param name="scenario">The selected scenario, or empty to use the selected dictionary default.</param>
    /// <param name="handler">The interface-based dispatch target.</param>
    /// <returns>The dispatch context and handler decision, or an explicit failure.</returns>
    MessageCrackResult CrackMessage(FixMessage message, FixDictionary transportDictionary, FixDictionary applicationDictionary, string scenario, IMessageCrackHandler handler);
    /// <summary>Looks up one message definition by canonical name and scenario.</summary>
    /// <param name="dictionary">The dictionary to query.</param>
    /// <param name="name">The canonical message name.</param>
    /// <param name="scenario">The selected scenario, or empty to use the dictionary default.</param>
    /// <returns>The matching definition and selected identity.</returns>
    MessageLookupResult LookupMessageName(FixDictionary dictionary, string name, string scenario);
    /// <summary>Looks up message definitions by category and scenario.</summary>
    /// <param name="dictionary">The dictionary to query.</param>
    /// <param name="category">The dictionary category name.</param>
    /// <param name="scenario">The selected scenario, or empty to use the dictionary default.</param>
    /// <returns>The matching definitions and selected identity.</returns>
    MessageLookupResult LookupMessageCategory(FixDictionary dictionary, string category, string scenario);
}
