namespace pdf;

/// <summary>Defines operations exposed by <see cref="PdfBadge"/>.</summary>
public interface IPdfBadge
{
    /// <summary>Sets the badge title.</summary>
    /// <param name="title">The title text.</param>
    void SetTitle(string title);
    /// <summary>Sets the represented organization.</summary>
    /// <param name="organization">The organization text.</param>
    void SetOrganization(string organization);
    /// <summary>Sets the badge identifier.</summary>
    /// <param name="identifier">The identifier text.</param>
    void SetIdentifier(string identifier);
    /// <summary>Builds the badge.</summary>
    /// <returns>The built document.</returns>
    PdfDocument Build();
}
