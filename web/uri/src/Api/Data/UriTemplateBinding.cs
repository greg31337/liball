namespace uri;

/// <summary>
/// Associates a case-sensitive URI Template variable name with its value.
/// </summary>
public class UriTemplateBinding
{
    /// <summary>The case-sensitive variable name.</summary>
    public string Name;

    /// <summary>The value bound to the variable.</summary>
    public UriTemplateValue Value;

    /// <summary>Initializes a URI Template variable binding.</summary>
    /// <param name="name">The case-sensitive variable name.</param>
    /// <param name="value">The variable value.</param>
    public UriTemplateBinding(string name, UriTemplateValue value)
    {
        Name = name;
        Value = value;
    }
}
