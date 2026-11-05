namespace ttf;

/// <summary>Controls shared policy, resource limits, and cancellation.</summary>
/// <remarks>
/// The default uses compatible policy and compatible finite limits. Operations
/// combine these limits with the limits retained by each loaded face and apply
/// the stricter value. Option-bearing services snapshot this object.
/// </remarks>
public class FontOperationOptions : IFontOperationOptions
{
    FontLoadMode mode;
    FontLimits limits;
    System.Threading.CancellationToken cancellationToken;

    /// <summary>Creates compatible, finitely bounded operation controls.</summary>
    public FontOperationOptions()
    {
        mode = FontLoadMode.Compatible;
        limits = new FontLimits();
        cancellationToken = System.Threading.CancellationToken.None;
    }

    /// <summary>Gets the strict, compatible, or inspect-only policy.</summary>
    /// <returns>The selected operation mode.</returns>
    public FontLoadMode GetMode() { return mode; }

    /// <summary>Sets the strict, compatible, or inspect-only policy.</summary>
    /// <param name="value">The operation mode.</param>
    public void SetMode(FontLoadMode value)
    {
        ValidateMode(value);
        mode = value;
    }

    /// <summary>Gets the immutable resource limits.</summary>
    /// <returns>The selected hard limits.</returns>
    public FontLimits GetLimits() { return limits; }

    /// <summary>Sets the immutable resource limits.</summary>
    /// <param name="value">The hard limits.</param>
    /// <exception cref="System.ArgumentNullException"><c>value</c> is null.</exception>
    public void SetLimits(FontLimits value)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException("value");
        }

        limits = value;
    }

    /// <summary>Gets the cancellation token observed by the operation.</summary>
    /// <returns>The selected token.</returns>
    public System.Threading.CancellationToken GetCancellationToken()
    {
        return cancellationToken;
    }

    /// <summary>Sets the cancellation token observed by the operation.</summary>
    /// <param name="value">The cancellation token.</param>
    public void SetCancellationToken(
        System.Threading.CancellationToken value)
    {
        cancellationToken = value;
    }

    internal FontOperationOptions Copy()
    {
        FontOperationOptions copy = new FontOperationOptions();
        copy.mode = mode;
        copy.limits = limits;
        copy.cancellationToken = cancellationToken;
        return copy;
    }

    static void ValidateMode(FontLoadMode value)
    {
        if (value != FontLoadMode.Strict &&
            value != FontLoadMode.Compatible &&
            value != FontLoadMode.InspectOnly)
        {
            throw new System.ArgumentOutOfRangeException("value");
        }
    }
}
