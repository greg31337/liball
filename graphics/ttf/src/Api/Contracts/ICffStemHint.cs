namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="CffStemHint"/>.</summary>
public interface ICffStemHint
{
    /// <summary>Reports whether this is a vertical stem hint.</summary>
    /// <returns>True for a vertical stem; false for a horizontal stem.</returns>
    bool IsVertical();

    /// <summary>Gets the encoded relative stem position.</summary>
    /// <returns>The position delta in design units.</returns>
    double GetPositionDelta();

    /// <summary>Gets the encoded stem width.</summary>
    /// <returns>The width in design units.</returns>
    double GetWidth();
}
