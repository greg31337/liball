namespace ttf;

/// <summary>Represents one immutable adjustment applied to a justified line.</summary>
public class TextJustificationApplication : ITextJustificationApplication
{
    AppliedJustification application;

    internal TextJustificationApplication(AppliedJustification application)
    {
        this.application = application;
    }

    /// <summary>Returns the mechanism that supplied the expansion.</summary>
    /// <returns>The application kind.</returns>
    public TextJustificationApplicationKind GetKind()
    {
        AppliedJustificationKind kind = application.GetKind();
        if (kind == AppliedJustificationKind.Space)
        {
            return TextJustificationApplicationKind.Space;
        }
        if (kind == AppliedJustificationKind.Tracking)
        {
            return TextJustificationApplicationKind.Tracking;
        }
        if (kind == AppliedJustificationKind.Jstf)
        {
            return TextJustificationApplicationKind.Jstf;
        }
        return TextJustificationApplicationKind.Kashida;
    }

    /// <summary>Returns the adjusted source-cluster boundary.</summary>
    /// <returns>The source-cluster boundary.</returns>
    public int GetClusterBoundary()
    {
        return application.GetClusterBoundary();
    }

    /// <summary>Returns the expansion applied at the boundary.</summary>
    /// <returns>The positive expansion in layout units.</returns>
    public double GetExpansion()
    {
        return application.GetExpansion();
    }
}
