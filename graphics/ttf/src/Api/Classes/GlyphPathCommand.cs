namespace ttf;

/// <summary>Represents one immutable normalized path command in design units.</summary>
public class GlyphPathCommand : IGlyphPathCommand
{
    GlyphPathCommandKind kind;
    double controlX;
    double controlY;
    double secondControlX;
    double secondControlY;
    double endX;
    double endY;

    internal GlyphPathCommand(SfntPathCommand command)
    {
        kind = ConvertKind(command.GetKind());

        if (kind == GlyphPathCommandKind.Quadratic ||
            kind == GlyphPathCommandKind.Cubic)
        {
            controlX = command.GetControlX();
            controlY = command.GetControlY();
        }

        if (kind == GlyphPathCommandKind.Cubic)
        {
            secondControlX = command.GetSecondControlX();
            secondControlY = command.GetSecondControlY();
        }

        if (kind != GlyphPathCommandKind.Close)
        {
            endX = command.GetEndX();
            endY = command.GetEndY();
        }
    }

    /// <summary>Gets the path operation kind.</summary>
    /// <returns>The operation kind.</returns>
    public GlyphPathCommandKind GetKind()
    {
        return kind;
    }

    /// <summary>Gets the first curve control x coordinate.</summary>
    /// <returns>The control x coordinate in design units.</returns>
    /// <exception cref="System.InvalidOperationException">The command is not a curve.</exception>
    public double GetControlX()
    {
        EnsureCurve();
        return controlX;
    }

    /// <summary>Gets the first curve control y coordinate.</summary>
    /// <returns>The control y coordinate in design units.</returns>
    /// <exception cref="System.InvalidOperationException">The command is not a curve.</exception>
    public double GetControlY()
    {
        EnsureCurve();
        return controlY;
    }

    /// <summary>Gets the second cubic control x coordinate.</summary>
    /// <returns>The second control x coordinate in design units.</returns>
    /// <exception cref="System.InvalidOperationException">The command is not cubic.</exception>
    public double GetSecondControlX()
    {
        EnsureCubic();
        return secondControlX;
    }

    /// <summary>Gets the second cubic control y coordinate.</summary>
    /// <returns>The second control y coordinate in design units.</returns>
    /// <exception cref="System.InvalidOperationException">The command is not cubic.</exception>
    public double GetSecondControlY()
    {
        EnsureCubic();
        return secondControlY;
    }

    /// <summary>Gets the command endpoint x coordinate.</summary>
    /// <returns>The endpoint x coordinate in design units.</returns>
    /// <exception cref="System.InvalidOperationException">The command closes a contour.</exception>
    public double GetEndX()
    {
        EnsureEndpoint();
        return endX;
    }

    /// <summary>Gets the command endpoint y coordinate.</summary>
    /// <returns>The endpoint y coordinate in design units.</returns>
    /// <exception cref="System.InvalidOperationException">The command closes a contour.</exception>
    public double GetEndY()
    {
        EnsureEndpoint();
        return endY;
    }

    void EnsureCurve()
    {
        if (kind != GlyphPathCommandKind.Quadratic &&
            kind != GlyphPathCommandKind.Cubic)
        {
            throw new System.InvalidOperationException(
                "Only a curve command has a control point.");
        }
    }

    void EnsureCubic()
    {
        if (kind != GlyphPathCommandKind.Cubic)
        {
            throw new System.InvalidOperationException(
                "Only a cubic command has a second control point.");
        }
    }

    void EnsureEndpoint()
    {
        if (kind == GlyphPathCommandKind.Close)
        {
            throw new System.InvalidOperationException(
                "A close command has no explicit endpoint.");
        }
    }

    static GlyphPathCommandKind ConvertKind(SfntPathCommandKind kind)
    {
        if (kind == SfntPathCommandKind.Move)
        {
            return GlyphPathCommandKind.Move;
        }

        if (kind == SfntPathCommandKind.Line)
        {
            return GlyphPathCommandKind.Line;
        }

        if (kind == SfntPathCommandKind.Quadratic)
        {
            return GlyphPathCommandKind.Quadratic;
        }

        if (kind == SfntPathCommandKind.Cubic)
        {
            return GlyphPathCommandKind.Cubic;
        }

        return GlyphPathCommandKind.Close;
    }
}
