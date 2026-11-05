using System.Collections.Generic;

namespace args;

/// <summary>
/// Describes one parsed occurrence of an option or positional argument.
/// </summary>
public class ArgOccurrence
{
    /// <summary>
    /// The kind of symbol represented by this object.
    /// </summary>
    public ArgSymbolKind SymbolKind;
    /// <summary>
    /// The canonical symbol name associated with this object.
    /// </summary>
    public string SymbolName;
    /// <summary>
    /// The exact option alias used on the command line, or an empty string when not applicable.
    /// </summary>
    public string Alias;
    /// <summary>
    /// The zero-based source token index, or -1 when no source token exists.
    /// </summary>
    public int TokenIndex;
    /// <summary>
    /// The option associated with this object, or null when it does not apply.
    /// </summary>
    public ArgOption? Option;
    /// <summary>
    /// The positional argument associated with this object, or null when it does not apply.
    /// </summary>
    public ArgArgument? Argument;
    /// <summary>
    /// The raw text values assigned to this occurrence, in encounter order.
    /// </summary>
    public List<string> Values;
    /// <summary>
    /// Where the occurrence's value came from.
    /// </summary>
    public ArgValueSource Source;
    /// <summary>
    /// The accumulated count for a count option occurrence.
    /// </summary>
    public int Count;

    internal ArgOccurrence()
    {
        SymbolKind = ArgSymbolKind.Option;
        SymbolName = "";
        Alias = "";
        TokenIndex = -1;
        Option = null;
        Argument = null;
        Values = new List<string>();
        Source = ArgValueSource.None;
        Count = 0;
    }
}
