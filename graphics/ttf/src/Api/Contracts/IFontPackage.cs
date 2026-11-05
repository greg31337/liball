namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="FontPackage"/>.</summary>
public interface IFontPackage
{
    /// <summary>Gets the kind of the original loaded container.</summary>
    /// <returns>The original container kind.</returns>
    FontContainerKind GetSourceKind();

    /// <summary>Gets immutable provenance for the original and reconstructed containers.</summary>
    /// <returns>The package provenance.</returns>
    FontContainerProvenance GetProvenance();

    /// <summary>Gets all retained diagnostics in deterministic package and face order.</summary>
    /// <returns>A new array containing the retained diagnostics.</returns>
    FontDiagnostic[] GetDiagnostics();

    /// <summary>Copies the complete original input container bytes.</summary>
    /// <returns>A new array containing the original bytes.</returns>
    byte[] CopyOriginalBytes();

    /// <summary>Copies one face as a standalone SFNT font.</summary>
    /// <param name="faceIndex">The zero-based face index.</param>
    /// <returns>A new array containing the standalone SFNT bytes.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>faceIndex</c> is outside the package face range.
    /// </exception>
    /// <exception cref="System.InvalidOperationException">
    /// The selected inspect-only face is not structurally valid for reconstruction.
    /// </exception>
    byte[] CopySfntBytes(int faceIndex);
}
