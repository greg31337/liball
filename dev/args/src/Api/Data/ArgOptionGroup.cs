using System.Collections.Generic;

namespace args;

/// <summary>
/// Describes a named set of options constrained by a command.
/// </summary>
public class ArgOptionGroup
{
    /// <summary>
    /// The name used in validation errors, help, and diagnostics.
    /// </summary>
    public string Name;
    /// <summary>
    /// The canonical names or aliases of options belonging to this group.
    /// </summary>
    public List<string> OptionNames;
    /// <summary>
    /// Whether no more than one group option may be present.
    /// </summary>
    public bool IsMutuallyExclusive;
    /// <summary>
    /// Whether at least one group option must be present.
    /// </summary>
    public bool IsAtLeastOneRequired;
    /// <summary>
    /// The command that owns this group.
    /// </summary>
    public ArgCommand ParentCommand;

    internal ArgOptionGroup(string name, string[] optionNames, bool isMutuallyExclusive, bool isAtLeastOneRequired, ArgCommand parentCommand)
    {
        Name = name;
        OptionNames = new List<string>();
        int index = 0;
        while (index < optionNames.Length)
        {
            OptionNames.Add(optionNames[index]);
            index = index + 1;
        }

        IsMutuallyExclusive = isMutuallyExclusive;
        IsAtLeastOneRequired = isAtLeastOneRequired;
        ParentCommand = parentCommand;
    }
}
