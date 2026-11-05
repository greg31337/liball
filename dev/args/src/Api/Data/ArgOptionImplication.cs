namespace args;

/// <summary>
/// Describes an option and logical value supplied when another option is present.
/// </summary>
public class ArgOptionImplication
{
    /// <summary>
    /// The canonical name or alias of the option to supply.
    /// </summary>
    public string OptionName;
    /// <summary>
    /// The logical value supplied to the target option.
    /// </summary>
    public string Value;

    internal ArgOptionImplication(string optionName, string value)
    {
        OptionName = optionName;
        Value = value;
    }
}
