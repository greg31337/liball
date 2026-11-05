namespace ttf;

/// <summary>Defines shared policy, limits, and cancellation for one operation.</summary>
public interface IFontOperationOptions
{
    /// <summary>Gets the strict, compatible, or inspect-only policy.</summary>
    /// <returns>The selected operation mode.</returns>
    FontLoadMode GetMode();

    /// <summary>Sets the strict, compatible, or inspect-only policy.</summary>
    /// <param name="value">The operation mode.</param>
    void SetMode(FontLoadMode value);

    /// <summary>Gets the immutable resource limits.</summary>
    /// <returns>The selected hard limits.</returns>
    FontLimits GetLimits();

    /// <summary>Sets the immutable resource limits.</summary>
    /// <param name="value">The hard limits.</param>
    void SetLimits(FontLimits value);

    /// <summary>Gets the cancellation token observed by the operation.</summary>
    /// <returns>The selected token.</returns>
    System.Threading.CancellationToken GetCancellationToken();

    /// <summary>Sets the cancellation token observed by the operation.</summary>
    /// <param name="value">The cancellation token.</param>
    void SetCancellationToken(System.Threading.CancellationToken value);
}
