namespace fix;

/// <summary>Defines mutable operations over the general FIX message model.</summary>
public interface IMessageOperations
{
    /// <summary>Creates a generic application message for a dictionary MsgType and scenario.</summary>
    /// <param name="dictionary">The application dictionary.</param>
    /// <param name="msgType">The exact dictionary MsgType.</param>
    /// <param name="scenario">The selected scenario, or empty to use the dictionary default.</param>
    /// <returns>The created message, matched definition, and application metadata.</returns>
    ApplicationMessageResult CreateApplicationMessage(FixDictionary dictionary, string msgType, string scenario);
    /// <summary>Identifies a generic application message in a dictionary and scenario.</summary>
    /// <param name="message">The message to identify.</param>
    /// <param name="dictionary">The application dictionary.</param>
    /// <param name="scenario">The selected scenario, or empty to use the dictionary default.</param>
    /// <returns>The inspected message, matched definition, and application metadata.</returns>
    ApplicationMessageResult IdentifyApplicationMessage(FixMessage message, FixDictionary dictionary, string scenario);
    /// <summary>Sets or adds a field in its routed message section.</summary>
    /// <param name="message">The message to edit.</param>
    /// <param name="field">The field to set.</param>
    /// <returns>The explicit edit outcome.</returns>
    MessageEditResult SetField(FixMessage message, FixField field);
    /// <summary>Removes every occurrence of a field tag.</summary>
    /// <param name="message">The message to edit.</param>
    /// <param name="tag">The tag to remove.</param>
    /// <returns>The explicit edit outcome.</returns>
    MessageEditResult RemoveField(FixMessage message, int tag);
    /// <summary>Adds a structured group to the message body.</summary>
    /// <param name="message">The message to edit.</param>
    /// <param name="group">The group to add.</param>
    /// <returns>The explicit edit outcome.</returns>
    MessageEditResult AddGroup(FixMessage message, FixGroup group);
    /// <summary>Removes every structured group with the supplied count tag.</summary>
    /// <param name="message">The message to edit.</param>
    /// <param name="countTag">The group count tag to remove.</param>
    /// <returns>The explicit edit outcome.</returns>
    MessageEditResult RemoveGroup(FixMessage message, int countTag);
    /// <summary>Creates an independent copy of a message.</summary>
    /// <param name="message">The message to copy.</param>
    /// <returns>The explicit edit outcome containing the copy.</returns>
    MessageEditResult CloneMessage(FixMessage message);
}
