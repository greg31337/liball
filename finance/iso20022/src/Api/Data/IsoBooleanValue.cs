namespace iso20022;

/// <summary>Represents an ISO boolean value and its source lexical form.</summary>
public class IsoBooleanValue
{
    /// <summary>The boolean value.</summary>
    public bool Value;
    /// <summary>The original lexical form, such as <c>true</c> or <c>1</c>.</summary>
    public string LexicalValue = string.Empty;
}
