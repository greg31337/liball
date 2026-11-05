using System;

namespace html;
/// <summary>Reports that HTML serialization stopped before exceeding its configured output-character limit.</summary>
/// <remarks><para>No characters beyond the configured ceiling are written. A caller-provided writer may already contain the safe prefix produced before the limit was reached; string-returning overloads do not return partial output.</para><para>Use <see cref = "GetMaximumOutputCharacters"/> to inspect the limit that ended the operation.</para></remarks>
public class HtmlSerializationLimitException : System.Exception
{
    internal int maximum;
    internal HtmlSerializationLimitException(int maximum) : base("An HTML serialization resource limit was exceeded.")
    {
        this.maximum = maximum;
    }

    /// <summary>Gets the configured output ceiling that serialization attempted to exceed.</summary>
    /// <returns>The maximum number of output characters allowed for the failed operation.</returns>
    public int GetMaximumOutputCharacters()
    {
        return this.maximum;
    }

    /// <summary>Gets the stable machine-readable code for this serialization failure.</summary>
    /// <returns><see cref = "HtmlDiagnosticCode.SerializationLimitExceeded"/>.</returns>
    public HtmlDiagnosticCode GetCode()
    {
        return HtmlDiagnosticCode.SerializationLimitExceeded;
    }
}
