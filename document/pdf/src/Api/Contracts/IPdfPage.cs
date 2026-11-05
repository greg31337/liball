using System;
using System.Collections.Generic;

namespace pdf;

/// <summary>
/// Defines the operations exposed by <see cref="PdfPage"/>.
/// </summary>
public interface IPdfPage
{
    /// <summary>
    /// Returns the canvas that records drawing commands for this page.
    /// </summary>
    /// <returns>The page's drawing canvas.</returns>
    PdfCanvas GetCanvas();

    /// <summary>
    /// Returns this page's dimensions in PDF points.
    /// </summary>
    /// <returns>A new dimensions value containing the page width and height.</returns>
    PdfPageDimensions GetDimensions();

    /// <summary>
    /// Sets the physical medium boundary and updates the page dimensions.
    /// </summary>
    /// <param name="box">The positive finite media rectangle in default user space.</param>
    void SetMediaBox(PdfRectangle box);

    /// <summary>
    /// Sets the visible page boundary.
    /// </summary>
    /// <param name="box">The positive finite crop rectangle contained by the media box.</param>
    void SetCropBox(PdfRectangle box);

    /// <summary>
    /// Sets the production bleed boundary.
    /// </summary>
    /// <param name="box">The positive finite bleed rectangle contained by the media box.</param>
    void SetBleedBox(PdfRectangle box);

    /// <summary>
    /// Sets the finished-page trim boundary.
    /// </summary>
    /// <param name="box">The positive finite trim rectangle contained by the media box.</param>
    void SetTrimBox(PdfRectangle box);

    /// <summary>
    /// Sets the meaningful-content art boundary.
    /// </summary>
    /// <param name="box">The positive finite art rectangle contained by the media box.</param>
    void SetArtBox(PdfRectangle box);

    /// <summary>
    /// Returns a copy of the physical media boundary.
    /// </summary>
    /// <returns>The configured media box.</returns>
    PdfRectangle GetMediaBox();

    /// <summary>
    /// Returns a copy of the effective crop boundary, which defaults to the media box.
    /// </summary>
    /// <returns>The effective crop box.</returns>
    PdfRectangle GetCropBox();

    /// <summary>
    /// Returns a copy of the effective bleed boundary, which defaults to the crop box.
    /// </summary>
    /// <returns>The effective bleed box.</returns>
    PdfRectangle GetBleedBox();

    /// <summary>
    /// Returns a copy of the effective trim boundary, which defaults to the crop box.
    /// </summary>
    /// <returns>The effective trim box.</returns>
    PdfRectangle GetTrimBox();

    /// <summary>
    /// Returns a copy of the effective art boundary, which defaults to the crop box.
    /// </summary>
    /// <returns>The effective art box.</returns>
    PdfRectangle GetArtBox();

    /// <summary>
    /// Sets the uniform margins used for subsequent PDF generation.
    /// </summary>
    /// <param name="margin">The margin to use for this operation.</param>
    void SetUniformMargins(double margin);

    /// <summary>
    /// Sets the margins used for subsequent PDF generation.
    /// </summary>
    /// <param name="left">The left in PDF points.</param>
    /// <param name="top">The top in PDF points.</param>
    /// <param name="right">The right in PDF points.</param>
    /// <param name="bottom">The bottom in PDF points.</param>
    void SetMargins(double left, double top, double right, double bottom);

    /// <summary>
    /// Returns the content area calculated from the object's current state.
    /// </summary>
    /// <returns>The resulting rectangle.</returns>
    PdfRectangle GetContentArea();

    /// <summary>
    /// Returns the content left calculated from the object's current state.
    /// </summary>
    /// <returns>The calculated value in PDF points unless stated otherwise.</returns>
    double GetContentLeft();

    /// <summary>
    /// Returns the content right calculated from the object's current state.
    /// </summary>
    /// <returns>The calculated value in PDF points unless stated otherwise.</returns>
    double GetContentRight();

    /// <summary>
    /// Returns the content top calculated from the object's current state.
    /// </summary>
    /// <returns>The calculated value in PDF points unless stated otherwise.</returns>
    double GetContentTop();

    /// <summary>
    /// Returns the content bottom calculated from the object's current state.
    /// </summary>
    /// <returns>The calculated value in PDF points unless stated otherwise.</returns>
    double GetContentBottom();

    /// <summary>
    /// Returns the content width calculated from the object's current state.
    /// </summary>
    /// <returns>The calculated value in PDF points unless stated otherwise.</returns>
    double GetContentWidth();

    /// <summary>
    /// Returns the content height calculated from the object's current state.
    /// </summary>
    /// <returns>The calculated value in PDF points unless stated otherwise.</returns>
    double GetContentHeight();

    /// <summary>
    /// Adds an internal link to this page and returns the stored value.
    /// </summary>
    /// <param name="destinationName">The destination name to use for this operation.</param>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="y">The vertical coordinate in PDF points from the page's bottom edge.</param>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    PdfInternalLink AddInternalLink(string destinationName, double x, double y, double width, double height);

    /// <summary>
    /// Adds an internal link to this page and returns the stored value.
    /// </summary>
    /// <param name="destinationName">The destination name to use for this operation.</param>
    /// <param name="rectangle">The rectangle to use for this operation.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    PdfInternalLink AddInternalLinkInRectangle(string destinationName, PdfRectangle rectangle);

    /// <summary>Adds an internal link tied to an accessibility structure node.</summary>
    /// <param name="target">The structure-aware navigation target.</param>
    /// <param name="rectangle">The clickable rectangle.</param>
    /// <returns>The stored structure-aware link.</returns>
    PdfInternalLink AddStructureInternalLink(PdfStructureLinkTarget target, PdfRectangle rectangle);

    /// <summary>
    /// Adds an external link to this page and returns the stored value.
    /// </summary>
    /// <param name="uri">The absolute or relative URI opened by the link.</param>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="y">The vertical coordinate in PDF points from the page's bottom edge.</param>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    PdfExternalLink AddExternalLink(string uri, double x, double y, double width, double height);

    /// <summary>
    /// Adds an external link to this page and returns the stored value.
    /// </summary>
    /// <param name="uri">The absolute or relative URI opened by the link.</param>
    /// <param name="rectangle">The rectangle to use for this operation.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    PdfExternalLink AddExternalLinkInRectangle(string uri, PdfRectangle rectangle);

    /// <summary>
    /// Adds a text annotation to this page and returns the stored value.
    /// </summary>
    /// <param name="contents">The contents to use for this operation.</param>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="y">The vertical coordinate in PDF points from the page's bottom edge.</param>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    PdfAnnotation AddTextAnnotation(string contents, double x, double y, double width, double height);

    /// <summary>
    /// Adds a text annotation to this page and returns the stored value.
    /// </summary>
    /// <param name="contents">The contents to use for this operation.</param>
    /// <param name="rectangle">The rectangle to use for this operation.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    PdfAnnotation AddTextAnnotationInRectangle(string contents, PdfRectangle rectangle);

    /// <summary>
    /// Adds a square annotation to this page and returns the stored value.
    /// </summary>
    /// <param name="contents">The contents to use for this operation.</param>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="y">The vertical coordinate in PDF points from the page's bottom edge.</param>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    PdfAnnotation AddSquareAnnotation(string contents, double x, double y, double width, double height);

    /// <summary>
    /// Adds a square annotation to this page and returns the stored value.
    /// </summary>
    /// <param name="contents">The contents to use for this operation.</param>
    /// <param name="rectangle">The rectangle to use for this operation.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    PdfAnnotation AddSquareAnnotationInRectangle(string contents, PdfRectangle rectangle);
}
