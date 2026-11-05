using System;

namespace pdf;

/// <summary>Stores one numbered contract clause.</summary>
public class PdfContractClause
{
    /// <summary>Stores the clause heading.</summary>
    public string Heading = string.Empty;
    /// <summary>Stores the clause body.</summary>
    public string Text = string.Empty;

    /// <summary>Initializes a contract clause.</summary>
    /// <param name="heading">The clause heading.</param>
    /// <param name="text">The clause body.</param>
    public PdfContractClause(string heading, string text)
    {
        if (heading == null) { throw new ArgumentNullException("heading"); }
        if (text == null) { throw new ArgumentNullException("text"); }
        Heading = heading;
        Text = text;
    }
}
