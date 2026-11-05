namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgProcessPresets"/>.</summary>
public interface ISvgProcessPresets
{
    /// <summary>Creates conservative semantics-preserving cleanup defaults.</summary>
    /// <returns>An independent safe-default configuration.</returns>
    SvgProcessOptions SafeDefault();
    /// <summary>Creates deterministic canonicalization defaults.</summary>
    /// <returns>An independent canonicalization configuration.</returns>
    SvgProcessOptions Canonicalize();
    /// <summary>Creates aggressive size-oriented defaults with explicit semantic loss enabled.</summary>
    /// <returns>An independent aggressive configuration.</returns>
    SvgProcessOptions Aggressive();
    /// <summary>Creates safe sprite-isolation and construction defaults.</summary>
    /// <returns>An independent sprite configuration.</returns>
    SvgProcessOptions Sprite();
    /// <summary>Creates secure-static sanitization defaults.</summary>
    /// <returns>An independent sanitization configuration.</returns>
    SvgProcessOptions Sanitize();
    /// <summary>Creates one named preset configuration.</summary>
    /// <param name="kind">The non-custom preset kind.</param>
    /// <returns>An independent named configuration.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown for <c>Custom</c> or an unknown kind.</exception>
    SvgProcessOptions Create(SvgProcessPresetKind kind);
}
