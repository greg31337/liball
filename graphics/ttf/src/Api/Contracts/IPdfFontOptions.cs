namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="PdfFontOptions"/>.</summary>
public interface IPdfFontOptions
{
    /// <summary>Gets the requested PDF font kind.</summary>
    /// <returns>The resource and embedded program strategy.</returns>
    PdfFontKind GetFontKind();

    /// <summary>Sets the requested PDF font kind.</summary>
    /// <param name="value">The resource and embedded program strategy.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>value</c> is undefined.
    /// </exception>
    void SetFontKind(PdfFontKind value);

    /// <summary>Gets the target PDF language version.</summary>
    /// <returns>The target version.</returns>
    PdfVersion GetVersion();

    /// <summary>Sets the target PDF language version.</summary>
    /// <param name="value">The target version.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>value</c> is undefined.
    /// </exception>
    void SetVersion(PdfVersion value);

    /// <summary>Gets the PDF writing mode.</summary>
    /// <returns>The horizontal or vertical writing mode.</returns>
    PdfWritingMode GetWritingMode();

    /// <summary>Sets the PDF writing mode.</summary>
    /// <param name="value">The horizontal or vertical writing mode.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>value</c> is undefined.
    /// </exception>
    void SetWritingMode(PdfWritingMode value);

    /// <summary>Gets the requested embedding mode.</summary>
    /// <returns>Subset or full-font embedding.</returns>
    PdfFontEmbeddingMode GetEmbeddingMode();

    /// <summary>Sets the requested embedding mode.</summary>
    /// <param name="value">Subset or full-font embedding.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>value</c> is undefined.
    /// </exception>
    void SetEmbeddingMode(PdfFontEmbeddingMode value);

    /// <summary>Gets the embedding-right response policy.</summary>
    /// <returns>The rights policy.</returns>
    PdfFontRightsPolicy GetRightsPolicy();

    /// <summary>Sets the embedding-right response policy.</summary>
    /// <param name="value">The rights policy.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>value</c> is undefined.
    /// </exception>
    void SetRightsPolicy(PdfFontRightsPolicy value);

    /// <summary>Gets the requested PDF/A compatibility profile.</summary>
    /// <returns>The archival profile.</returns>
    PdfArchivalProfile GetArchivalProfile();

    /// <summary>Sets the PDF/A compatibility profile to assess.</summary>
    /// <param name="value">The archival profile.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>value</c> is undefined.
    /// </exception>
    void SetArchivalProfile(PdfArchivalProfile value);

    /// <summary>Gets the requested PDF/UA compatibility profile.</summary>
    /// <returns>The accessibility profile.</returns>
    PdfAccessibilityProfile GetAccessibilityProfile();

    /// <summary>Sets the PDF/UA compatibility profile to assess.</summary>
    /// <param name="value">The accessibility profile.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>value</c> is undefined.
    /// </exception>
    void SetAccessibilityProfile(PdfAccessibilityProfile value);

    /// <summary>Gets the document-local CID allocation mode.</summary>
    /// <returns>The CID allocation mode.</returns>
    PdfCidAllocationMode GetCidAllocationMode();

    /// <summary>Sets the document-local CID allocation mode.</summary>
    /// <param name="value">The CID allocation mode.</param>
    void SetCidAllocationMode(PdfCidAllocationMode value);

    /// <summary>Gets the caller-supplied subset-prefix collision override.</summary>
    /// <returns>Six uppercase letters, or null for deterministic generation.</returns>
    string? GetSubsetPrefixOverride();

    /// <summary>Sets or clears the subset-prefix collision override.</summary>
    /// <param name="value">Six uppercase ASCII letters, or null.</param>
    void SetSubsetPrefixOverride(string? value);

    /// <summary>Gets the embedded-font hint retention policy.</summary>
    /// <returns>The hint policy.</returns>
    FontSubsetHintPolicy GetHintPolicy();

    /// <summary>Sets the embedded-font hint retention policy.</summary>
    /// <param name="value">The hint policy.</param>
    void SetHintPolicy(FontSubsetHintPolicy value);

    /// <summary>Gets the embedded-font layout retention policy.</summary>
    /// <returns>The layout policy.</returns>
    FontSubsetLayoutPolicy GetLayoutPolicy();

    /// <summary>Sets the embedded-font layout retention policy.</summary>
    /// <param name="value">The layout policy.</param>
    void SetLayoutPolicy(FontSubsetLayoutPolicy value);

    /// <summary>Gets the embedded-font vertical-table retention policy.</summary>
    /// <returns>The vertical policy.</returns>
    FontSubsetVerticalPolicy GetVerticalPolicy();

    /// <summary>Sets the embedded-font vertical-table retention policy.</summary>
    /// <param name="value">The vertical policy.</param>
    void SetVerticalPolicy(FontSubsetVerticalPolicy value);

    /// <summary>Gets the embedded-font color-table retention policy.</summary>
    /// <returns>The color policy.</returns>
    FontSubsetColorPolicy GetColorPolicy();

    /// <summary>Sets the embedded-font color-table retention policy.</summary>
    /// <param name="value">The color policy.</param>
    void SetColorPolicy(FontSubsetColorPolicy value);

    /// <summary>Gets the embedded-font metadata retention policy.</summary>
    /// <returns>The metadata policy.</returns>
    FontSubsetMetadataPolicy GetMetadataPolicy();

    /// <summary>Sets the embedded-font metadata retention policy.</summary>
    /// <param name="value">The metadata policy.</param>
    void SetMetadataPolicy(FontSubsetMetadataPolicy value);

    /// <summary>Gets the restricted simple-font availability policy.</summary>
    /// <returns>The simple-font policy.</returns>
    PdfSimpleFontPolicy GetSimpleFontPolicy();

    /// <summary>Sets the restricted simple-font availability policy.</summary>
    /// <param name="value">The simple-font policy.</param>
    void SetSimpleFontPolicy(PdfSimpleFontPolicy value);

    /// <summary>Gets the variable-font static-instance policy.</summary>
    /// <returns>The static-instance policy.</returns>
    PdfStaticInstancePolicy GetStaticInstancePolicy();

    /// <summary>Sets the variable-font static-instance policy.</summary>
    /// <param name="value">The static-instance policy.</param>
    void SetStaticInstancePolicy(PdfStaticInstancePolicy value);

    /// <summary>Gets the target-version mismatch policy.</summary>
    /// <returns>The version compatibility policy.</returns>
    PdfVersionCompatibilityPolicy GetVersionCompatibilityPolicy();

    /// <summary>Sets the target-version mismatch policy.</summary>
    /// <param name="value">The version compatibility policy.</param>
    void SetVersionCompatibilityPolicy(PdfVersionCompatibilityPolicy value);

    /// <summary>Gets the requested-profile finding policy.</summary>
    /// <returns>The profile compatibility policy.</returns>
    PdfProfileCompatibilityPolicy GetProfileCompatibilityPolicy();

    /// <summary>Sets the requested-profile finding policy.</summary>
    /// <param name="value">The profile compatibility policy.</param>
    void SetProfileCompatibilityPolicy(PdfProfileCompatibilityPolicy value);

    /// <summary>Gets shared policy, limits, and cancellation controls.</summary>
    /// <returns>An independent mutable copy.</returns>
    FontOperationOptions GetOperationOptions();

    /// <summary>Sets shared operation controls from an owned snapshot.</summary>
    /// <param name="value">The operation controls.</param>
    void SetOperationOptions(FontOperationOptions value);
}
