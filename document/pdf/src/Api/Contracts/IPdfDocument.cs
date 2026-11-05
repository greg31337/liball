using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace pdf;

/// <summary>
/// Defines the operations exposed by <see cref="PdfDocument"/>.
/// </summary>
public interface IPdfDocument
{
    /// <summary>
    /// Returns the font registry owned by this document.
    /// </summary>
    /// <returns>The document font registry.</returns>
    PdfFontRegistry GetFontRegistry();

    /// <summary>
    /// Loads and registers a TrueType or OpenType font from an in-memory byte array.
    /// </summary>
    /// <param name="fontData">The complete TrueType or OpenType font data.</param>
    /// <returns>The registered font, or the existing registration when the same font data was registered previously.</returns>
    /// <exception cref="ArgumentNullException">Thrown when font data is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when font data is malformed or unsupported.</exception>
    PdfTrueTypeFont RegisterTrueTypeFont(byte[] fontData);

    /// <summary>
    /// Loads and registers a TrueType or OpenType font from the current position of a readable stream.
    /// </summary>
    /// <param name="stream">The readable stream containing complete TrueType or OpenType font data.</param>
    /// <returns>The registered font, or the existing registration when the same font data was registered previously.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the stream is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the stream is not readable.</exception>
    /// <exception cref="InvalidDataException">Thrown when font data is malformed or unsupported.</exception>
    PdfTrueTypeFont RegisterTrueTypeFontFromStream(Stream stream);

    /// <summary>
    /// Loads and registers a TrueType or OpenType font from a file.
    /// </summary>
    /// <param name="path">The path to a TrueType or OpenType font file.</param>
    /// <returns>The registered font, or the existing registration when the same font data was registered previously.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the path is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when font data is malformed or unsupported.</exception>
    PdfTrueTypeFont RegisterTrueTypeFontFromFile(string path);

    /// <summary>
    /// Returns the number of pages currently stored in this document.
    /// </summary>
    /// <returns>The current page count.</returns>
    int GetPageCount();

    /// <summary>
    /// Returns the page at the requested zero-based position.
    /// </summary>
    /// <param name="index">The zero-based position of the page to return.</param>
    /// <returns>The page stored at the requested position.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the index is outside the document page range.</exception>
    PdfPage GetPage(int index);

    /// <summary>
    /// Adds a page to this document and returns the stored value.
    /// </summary>
    /// <returns>The object that was added to the owning document or collection.</returns>
    PdfPage AddLetterPage();

    /// <summary>
    /// Adds a page to this document and returns the stored value.
    /// </summary>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    PdfPage AddPage(double width, double height);

    /// <summary>
    /// Adds a page to this document and returns the stored value.
    /// </summary>
    /// <param name="size">The size to use for this operation.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    PdfPage AddPageWithDimensions(PdfPageDimensions size);

    /// <summary>
    /// Inserts a page at the requested position and returns the new value.
    /// </summary>
    /// <param name="index">The zero-based position at which to perform the operation.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    PdfPage InsertLetterPage(int index);

    /// <summary>
    /// Inserts a page at the requested position and returns the new value.
    /// </summary>
    /// <param name="index">The zero-based position at which to perform the operation.</param>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a numeric argument falls outside its supported range.</exception>
    PdfPage InsertPage(int index, double width, double height);

    /// <summary>
    /// Inserts a page at the requested position and returns the new value.
    /// </summary>
    /// <param name="index">The zero-based position at which to perform the operation.</param>
    /// <param name="size">The size to use for this operation.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    PdfPage InsertPageWithDimensions(int index, PdfPageDimensions size);

    /// <summary>Imports a copy of one source page at the end of this document.</summary>
    /// <param name="source">The source document.</param>
    /// <param name="sourcePageIndex">The zero-based source page position.</param>
    /// <returns>The imported page.</returns>
    PdfPage ImportPage(PdfDocument source, int sourcePageIndex);

    /// <summary>Imports a copy of one source page at a requested position.</summary>
    /// <param name="source">The source document.</param>
    /// <param name="sourcePageIndex">The zero-based source page position.</param>
    /// <param name="targetPageIndex">The zero-based insertion position.</param>
    /// <returns>The imported page.</returns>
    PdfPage InsertImportedPage(PdfDocument source, int sourcePageIndex, int targetPageIndex);

    /// <summary>Places source-page content behind a target page.</summary>
    /// <param name="source">The source document.</param>
    /// <param name="sourcePageIndex">The zero-based source page position.</param>
    /// <param name="targetPageIndex">The zero-based target page position.</param>
    void StampPageBackground(PdfDocument source, int sourcePageIndex, int targetPageIndex);

    /// <summary>Places source-page content in front of a target page.</summary>
    /// <param name="source">The source document.</param>
    /// <param name="sourcePageIndex">The zero-based source page position.</param>
    /// <param name="targetPageIndex">The zero-based target page position.</param>
    void StampPageForeground(PdfDocument source, int sourcePageIndex, int targetPageIndex);

    /// <summary>Splits this document into consecutive documents.</summary>
    /// <param name="maximumPagesPerDocument">The positive maximum result size.</param>
    /// <returns>The split documents in page order.</returns>
    PdfDocument[] SplitDocument(int maximumPagesPerDocument);

    /// <summary>Appends independent copies of another document's pages and related objects.</summary>
    /// <param name="source">The source document.</param>
    void MergeDocument(PdfDocument source);

    /// <summary>
    /// Adds an image to this document and returns the stored value.
    /// </summary>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="rgbData">Packed RGB bytes in row-major order, with three bytes per pixel.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    PdfImage AddImage(int width, int height, byte[] rgbData);

    /// <summary>
    /// Adds packed eight-bit grayscale samples as an image resource.
    /// </summary>
    /// <param name="width">The positive pixel width.</param>
    /// <param name="height">The positive pixel height.</param>
    /// <param name="grayscaleData">Packed grayscale bytes in row-major order, with one byte per pixel.</param>
    /// <returns>The registered image, or the existing resource when the same samples were registered previously.</returns>
    PdfImage AddGrayscaleImage(int width, int height, byte[] grayscaleData);

    /// <summary>
    /// Adds packed eight-bit RGB samples as an image resource.
    /// </summary>
    /// <param name="width">The positive pixel width.</param>
    /// <param name="height">The positive pixel height.</param>
    /// <param name="rgbData">Packed RGB bytes in row-major order, with three bytes per pixel.</param>
    /// <returns>The registered image, or the existing resource when the same samples were registered previously.</returns>
    PdfImage AddRgbImage(int width, int height, byte[] rgbData);

    /// <summary>
    /// Adds packed eight-bit RGBA samples as an image resource with an alpha soft mask.
    /// </summary>
    /// <param name="width">The positive pixel width.</param>
    /// <param name="height">The positive pixel height.</param>
    /// <param name="rgbaData">Packed RGBA bytes in row-major order, with four bytes per pixel.</param>
    /// <returns>The registered image, or the existing resource when the same samples were registered previously.</returns>
    PdfImage AddRgbaImage(int width, int height, byte[] rgbaData);

    /// <summary>
    /// Loads and adds a JPEG image from an in-memory byte array.
    /// </summary>
    /// <param name="jpegData">The complete encoded JPEG data.</param>
    /// <returns>The registered image, or the existing resource when the same JPEG data was registered previously.</returns>
    /// <exception cref="ArgumentNullException">Thrown when JPEG data is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when JPEG data is malformed or unsupported.</exception>
    PdfImage AddJpeg(byte[] jpegData);

    /// <summary>
    /// Loads and adds a JPEG image from a read-only memory span.
    /// </summary>
    /// <param name="jpegData">The complete encoded JPEG data.</param>
    /// <returns>The registered image, or the existing resource when the same JPEG data was registered previously.</returns>
    /// <exception cref="InvalidDataException">Thrown when JPEG data is malformed or unsupported.</exception>
    PdfImage AddJpegFromSpan(ReadOnlySpan<byte> jpegData);

    /// <summary>
    /// Loads and adds a JPEG image from the current position of a readable stream without taking ownership of it.
    /// </summary>
    /// <param name="stream">The readable stream containing complete encoded JPEG data.</param>
    /// <returns>The registered image, or the existing resource when the same JPEG data was registered previously.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the stream is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the stream is not readable.</exception>
    /// <exception cref="InvalidDataException">Thrown when JPEG data is malformed or unsupported.</exception>
    PdfImage AddJpegFromStream(Stream stream);

    /// <summary>
    /// Loads and adds a JPEG image from a file.
    /// </summary>
    /// <param name="path">The path to the JPEG file.</param>
    /// <returns>The registered image, or the existing resource when the same JPEG data was registered previously.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the path is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when JPEG data is malformed or unsupported.</exception>
    PdfImage AddJpegFromFile(string path);

    /// <summary>
    /// Loads and adds a PNG image from an in-memory byte array.
    /// </summary>
    /// <param name="pngData">The complete encoded PNG data.</param>
    /// <returns>The registered image, or the existing resource when the same PNG data was registered previously.</returns>
    /// <exception cref="ArgumentNullException">Thrown when PNG data is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when PNG data is malformed, animated, or unsupported.</exception>
    PdfImage AddPng(byte[] pngData);

    /// <summary>
    /// Loads and adds a PNG image from a read-only memory span.
    /// </summary>
    /// <param name="pngData">The complete encoded PNG data.</param>
    /// <returns>The registered image, or the existing resource when the same PNG data was registered previously.</returns>
    /// <exception cref="InvalidDataException">Thrown when PNG data is malformed, animated, or unsupported.</exception>
    PdfImage AddPngFromSpan(ReadOnlySpan<byte> pngData);

    /// <summary>
    /// Loads and adds a PNG image from the current position of a readable stream without taking ownership of it.
    /// </summary>
    /// <param name="stream">The readable stream containing complete encoded PNG data.</param>
    /// <returns>The registered image, or the existing resource when the same PNG data was registered previously.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the stream is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the stream is not readable.</exception>
    /// <exception cref="InvalidDataException">Thrown when PNG data is malformed, animated, or unsupported.</exception>
    PdfImage AddPngFromStream(Stream stream);

    /// <summary>
    /// Loads and adds a PNG image from a file.
    /// </summary>
    /// <param name="path">The path to the PNG file.</param>
    /// <returns>The registered image, or the existing resource when the same PNG data was registered previously.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the path is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when PNG data is malformed, animated, or unsupported.</exception>
    PdfImage AddPngFromFile(string path);

    /// <summary>
    /// Detects, loads, and adds an encoded JPEG or PNG image from an in-memory byte array.
    /// </summary>
    /// <param name="encodedData">The complete encoded JPEG or PNG data.</param>
    /// <returns>The registered image, or the existing resource when the same source was registered previously.</returns>
    /// <exception cref="ArgumentNullException">Thrown when encoded data is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when the encoded format is unknown or malformed.</exception>
    PdfImage AddEncodedImage(byte[] encodedData);

    /// <summary>
    /// Detects, loads, and adds an encoded JPEG or PNG image from a read-only memory span.
    /// </summary>
    /// <param name="encodedData">The complete encoded JPEG or PNG data.</param>
    /// <returns>The registered image, or the existing resource when the same source was registered previously.</returns>
    /// <exception cref="InvalidDataException">Thrown when the encoded format is unknown or malformed.</exception>
    PdfImage AddEncodedImageFromSpan(ReadOnlySpan<byte> encodedData);

    /// <summary>
    /// Detects, loads, and adds an encoded JPEG or PNG image from the current position of a readable stream without taking ownership of it.
    /// </summary>
    /// <param name="stream">The readable stream containing complete encoded JPEG or PNG data.</param>
    /// <returns>The registered image, or the existing resource when the same source was registered previously.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the stream is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the stream is not readable.</exception>
    /// <exception cref="InvalidDataException">Thrown when the encoded format is unknown or malformed.</exception>
    PdfImage AddEncodedImageFromStream(Stream stream);

    /// <summary>
    /// Detects, loads, and adds an encoded JPEG or PNG image from a file.
    /// </summary>
    /// <param name="path">The path to the encoded JPEG or PNG file.</param>
    /// <returns>The registered image, or the existing resource when the same source was registered previously.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the path is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when the encoded format is unknown or malformed.</exception>
    PdfImage AddEncodedImageFromFile(string path);

    /// <summary>
    /// Adds a form to this document and returns the stored value.
    /// </summary>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a numeric argument falls outside its supported range.</exception>
    PdfFormXObject AddForm(double width, double height);

    /// <summary>
    /// Generates and registers a reusable vector QR Code form.
    /// </summary>
    /// <param name="text">The Unicode text encoded in UTF-8 byte mode.</param>
    /// <param name="moduleSize">The width and height of one module in PDF points.</param>
    /// <param name="errorCorrection">The requested QR Code error-correction level.</param>
    /// <param name="quietZone">The number of white modules surrounding the symbol.</param>
    /// <returns>The registered reusable vector form.</returns>
    /// <exception cref="ArgumentNullException">Thrown when text is null.</exception>
    /// <exception cref="ArgumentException">Thrown when text or the error-correction level is unsupported.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a dimension is outside its supported range.</exception>
    PdfFormXObject AddQrCode(string text, double moduleSize, PdfQrErrorCorrection errorCorrection, int quietZone);

    /// <summary>
    /// Generates and registers an eight-bit monochrome QR Code image.
    /// </summary>
    /// <param name="text">The Unicode text encoded in UTF-8 byte mode.</param>
    /// <param name="modulePixels">The integer pixel width and height of one module.</param>
    /// <param name="errorCorrection">The requested QR Code error-correction level.</param>
    /// <param name="quietZone">The number of white modules surrounding the symbol.</param>
    /// <returns>The registered grayscale image resource.</returns>
    /// <exception cref="ArgumentNullException">Thrown when text is null.</exception>
    /// <exception cref="ArgumentException">Thrown when text or the error-correction level is unsupported.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a dimension is outside its supported range.</exception>
    PdfImage AddQrCodeImage(string text, int modulePixels, PdfQrErrorCorrection errorCorrection, int quietZone);

    /// <summary>
    /// Generates and registers a reusable vector business-barcode form.
    /// </summary>
    /// <param name="text">The data accepted by the selected barcode format.</param>
    /// <param name="format">The one-dimensional barcode symbology.</param>
    /// <param name="moduleWidth">The width of one narrow module in PDF points.</param>
    /// <param name="barHeight">The bar height in PDF points.</param>
    /// <param name="includeText">True to draw normalized human-readable text below the bars.</param>
    /// <returns>The registered reusable vector form.</returns>
    /// <exception cref="ArgumentNullException">Thrown when text is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the format or text is unsupported or a check digit is invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a dimension is outside its supported range.</exception>
    PdfFormXObject AddBarcode(string text, PdfBarcodeFormat format, double moduleWidth, double barHeight, bool includeText);

    /// <summary>
    /// Generates and registers an eight-bit monochrome business-barcode image.
    /// </summary>
    /// <param name="text">The data accepted by the selected barcode format.</param>
    /// <param name="format">The one-dimensional barcode symbology.</param>
    /// <param name="modulePixels">The integer pixel width of one narrow module.</param>
    /// <param name="barHeightPixels">The bar height in pixels.</param>
    /// <returns>The registered grayscale image resource.</returns>
    /// <exception cref="ArgumentNullException">Thrown when text is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the format or text is unsupported or a check digit is invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a dimension is outside its supported range.</exception>
    PdfImage AddBarcodeImage(string text, PdfBarcodeFormat format, int modulePixels, int barHeightPixels);

    /// <summary>
    /// Securely imports SVG markup as a reusable native PDF form.
    /// </summary>
    /// <param name="svgText">The complete SVG markup.</param>
    /// <returns>The registered reusable form containing native PDF drawing commands.</returns>
    /// <exception cref="ArgumentNullException">Thrown when SVG markup is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when SVG content is unsafe or outside the supported subset.</exception>
    PdfFormXObject AddSvg(string svgText);

    /// <summary>
    /// Securely imports SVG markup from a readable stream without taking ownership of it.
    /// </summary>
    /// <param name="stream">The readable stream containing complete SVG markup.</param>
    /// <returns>The registered reusable form containing native PDF drawing commands.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the stream is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the stream is not readable.</exception>
    /// <exception cref="InvalidDataException">Thrown when SVG content is unsafe or outside the supported subset.</exception>
    PdfFormXObject AddSvgFromStream(Stream stream);

    /// <summary>
    /// Securely imports SVG markup from a file as a reusable native PDF form.
    /// </summary>
    /// <param name="path">The path to the SVG file.</param>
    /// <returns>The registered reusable form containing native PDF drawing commands.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the path is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when SVG content is unsafe or outside the supported subset.</exception>
    PdfFormXObject AddSvgFromFile(string path);

    /// <summary>
    /// Renders previously loaded SVG data as a reusable native PDF form.
    /// </summary>
    /// <param name="svgData">The validated SVG data to render.</param>
    /// <returns>The registered reusable form containing native PDF drawing commands.</returns>
    /// <exception cref="ArgumentNullException">Thrown when SVG data is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when SVG content is outside the supported subset.</exception>
    PdfFormXObject AddLoadedSvg(PdfSvgData svgData);

    /// <summary>
    /// Adds a named destination to this document and returns the stored value.
    /// </summary>
    /// <param name="name">The name used to identify the value in the document.</param>
    /// <param name="page">The PDF page that receives or owns the content.</param>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="y">The vertical coordinate in PDF points from the page's bottom edge.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when an argument is invalid for this operation.</exception>
    PdfNamedDestination AddNamedDestination(string name, PdfPage page, double x, double y);

    /// <summary>
    /// Adds a named destination to this document and returns the stored value.
    /// </summary>
    /// <param name="name">The name used to identify the value in the document.</param>
    /// <param name="page">The PDF page that receives or owns the content.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    PdfNamedDestination AddNamedDestinationAtPageTop(string name, PdfPage page);

    /// <summary>
    /// Adds an outline to this document and returns the stored value.
    /// </summary>
    /// <param name="title">The title to use for this operation.</param>
    /// <param name="page">The PDF page that receives or owns the content.</param>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="y">The vertical coordinate in PDF points from the page's bottom edge.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    PdfOutlineItem AddOutline(string title, PdfPage page, double x, double y);

    /// <summary>
    /// Adds an outline to this document and returns the stored value.
    /// </summary>
    /// <param name="title">The title to use for this operation.</param>
    /// <param name="page">The PDF page that receives or owns the content.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    PdfOutlineItem AddOutlineAtPageTop(string title, PdfPage page);

    /// <summary>
    /// Adds an outline child to this document and returns the stored value.
    /// </summary>
    /// <param name="parent">The parent to use for this operation.</param>
    /// <param name="title">The title to use for this operation.</param>
    /// <param name="page">The PDF page that receives or owns the content.</param>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="y">The vertical coordinate in PDF points from the page's bottom edge.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when an argument is invalid for this operation.</exception>
    PdfOutlineItem AddOutlineChild(PdfOutlineItem parent, string title, PdfPage page, double x, double y);

    /// <summary>
    /// Adds an outline child to this document and returns the stored value.
    /// </summary>
    /// <param name="parent">The parent to use for this operation.</param>
    /// <param name="title">The title to use for this operation.</param>
    /// <param name="page">The PDF page that receives or owns the content.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    PdfOutlineItem AddOutlineChildAtPageTop(PdfOutlineItem parent, string title, PdfPage page);

    /// <summary>Adds a primary index-term marker.</summary>
    /// <param name="term">The primary index term.</param>
    /// <param name="page">The target page.</param>
    /// <param name="x">The target horizontal coordinate.</param>
    /// <param name="y">The target vertical coordinate.</param>
    /// <returns>The registered marker.</returns>
    PdfIndexMarker AddIndexMarker(string term, PdfPage page, double x, double y);

    /// <summary>Adds a secondary index-term marker.</summary>
    /// <param name="term">The primary index term.</param>
    /// <param name="subterm">The secondary index term.</param>
    /// <param name="page">The target page.</param>
    /// <param name="x">The target horizontal coordinate.</param>
    /// <param name="y">The target vertical coordinate.</param>
    /// <returns>The registered marker.</returns>
    PdfIndexMarker AddIndexSubentryMarker(string term, string subterm, PdfPage page, double x, double y);

    /// <summary>Returns the number of registered index markers.</summary>
    /// <returns>The marker count.</returns>
    int GetIndexMarkerCount();

    /// <summary>Returns a registered index marker.</summary>
    /// <param name="index">The zero-based marker position.</param>
    /// <returns>The requested marker.</returns>
    PdfIndexMarker GetIndexMarker(int index);

    /// <summary>Adds a named cross-reference marker.</summary>
    /// <param name="name">The unique marker name.</param>
    /// <param name="text">The marker display text.</param>
    /// <param name="page">The target page.</param>
    /// <param name="x">The target horizontal coordinate.</param>
    /// <param name="y">The target vertical coordinate.</param>
    /// <returns>The registered marker.</returns>
    PdfCrossReferenceMarker AddCrossReferenceMarker(string name, string text, PdfPage page, double x, double y);

    /// <summary>Returns the number of registered cross-reference markers.</summary>
    /// <returns>The marker count.</returns>
    int GetCrossReferenceMarkerCount();

    /// <summary>Returns a registered cross-reference marker.</summary>
    /// <param name="index">The zero-based marker position.</param>
    /// <returns>The requested marker.</returns>
    PdfCrossReferenceMarker GetCrossReferenceMarker(int index);

    /// <summary>Adds a link to a registered cross-reference marker.</summary>
    /// <param name="sourcePage">The page receiving the link.</param>
    /// <param name="markerName">The marker name.</param>
    /// <param name="rectangle">The clickable rectangle.</param>
    /// <returns>The stored link.</returns>
    PdfInternalLink AddCrossReferenceLink(PdfPage sourcePage, string markerName, PdfRectangle rectangle);

    /// <summary>Formats a cross-reference with its current page label.</summary>
    /// <param name="markerName">The marker name.</param>
    /// <returns>The formatted cross-reference text.</returns>
    string GetCrossReferenceText(string markerName);

    /// <summary>Adds a figure marker for generated lists.</summary>
    /// <param name="title">The figure title.</param>
    /// <param name="page">The target page.</param>
    /// <param name="x">The target horizontal coordinate.</param>
    /// <param name="y">The target vertical coordinate.</param>
    /// <returns>The registered marker.</returns>
    PdfPublishingMarker AddFigureMarker(string title, PdfPage page, double x, double y);

    /// <summary>Adds a table marker for generated lists.</summary>
    /// <param name="title">The table title.</param>
    /// <param name="page">The target page.</param>
    /// <param name="x">The target horizontal coordinate.</param>
    /// <param name="y">The target vertical coordinate.</param>
    /// <returns>The registered marker.</returns>
    PdfPublishingMarker AddTableMarker(string title, PdfPage page, double x, double y);

    /// <summary>Returns the number of registered figure and table markers.</summary>
    /// <returns>The marker count.</returns>
    int GetPublishingMarkerCount();

    /// <summary>Returns a registered figure or table marker.</summary>
    /// <param name="index">The zero-based marker position.</param>
    /// <returns>The requested marker.</returns>
    PdfPublishingMarker GetPublishingMarker(int index);

    /// <summary>Adds a page-label range beginning at an existing page.</summary>
    /// <param name="pageIndex">The zero-based first page position.</param>
    /// <param name="style">The numbering style.</param>
    /// <param name="prefix">The label prefix.</param>
    /// <param name="startNumber">The positive first generated number.</param>
    /// <returns>The registered range.</returns>
    PdfPageLabelRange AddPageLabelRange(int pageIndex, PdfPageLabelStyle style, string prefix, int startNumber);

    /// <summary>Returns the number of configured page-label ranges.</summary>
    /// <returns>The range count.</returns>
    int GetPageLabelRangeCount();

    /// <summary>Returns a configured page-label range.</summary>
    /// <param name="index">The zero-based range position.</param>
    /// <returns>The requested range.</returns>
    PdfPageLabelRange GetPageLabelRange(int index);

    /// <summary>Returns the visible label for a document page.</summary>
    /// <param name="pageIndex">The zero-based page position.</param>
    /// <returns>The configured page label.</returns>
    string GetPageLabel(int pageIndex);

    /// <summary>Creates a navigation target tied to an accessibility structure node.</summary>
    /// <param name="name">The unique target name.</param>
    /// <param name="structureNode">The registered structure node.</param>
    /// <param name="x">The target horizontal coordinate.</param>
    /// <param name="y">The target vertical coordinate.</param>
    /// <returns>The structure-aware link target.</returns>
    PdfStructureLinkTarget AddStructureLinkTarget(string name, PdfAccessibilityNode structureNode, double x, double y);

    /// <summary>Returns the number of structure-aware link targets.</summary>
    /// <returns>The target count.</returns>
    int GetStructureLinkTargetCount();

    /// <summary>Returns a structure-aware link target.</summary>
    /// <param name="index">The zero-based target position.</param>
    /// <returns>The requested target.</returns>
    PdfStructureLinkTarget GetStructureLinkTarget(int index);

    /// <summary>
    /// Sets the metadata used for subsequent PDF generation.
    /// </summary>
    /// <param name="metadata">The metadata to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when metadata is null.</exception>
    void SetMetadata(PdfMetadata metadata);

    /// <summary>
    /// Checks the document for known conditions that affect later PDF/A conversion.
    /// </summary>
    /// <returns>A result containing PDF/A preparation errors and warnings.</returns>
    PdfAPreparationResult CheckPdfAPreparation();

    /// <summary>Prepares the document for level-B PDF/A archival output.</summary>
    /// <param name="options">The profile and ICC output-intent options.</param>
    void PrepareForPdfA(PdfAPreparationOptions options);

    /// <summary>Determines whether PDF/A preparation is active.</summary>
    /// <returns>True when PDF/A output is enabled.</returns>
    bool IsPreparedForPdfA();

    /// <summary>Returns the active PDF/A profile.</summary>
    /// <returns>The configured conformance profile.</returns>
    PdfAConformance GetPdfAConformance();

    /// <summary>
    /// Sets the language used for subsequent PDF generation.
    /// </summary>
    /// <param name="language">The language to use for this operation.</param>
    void SetLanguage(string language);

    /// <summary>
    /// Sets the PDF viewer-window and full-screen fallback preferences.
    /// </summary>
    /// <param name="preferences">The viewer preferences to copy into the document.</param>
    void SetViewerPreferences(PdfViewerPreferences preferences);

    /// <summary>
    /// Controls whether a viewer title bar should display document-title metadata instead of the file name.
    /// </summary>
    /// <param name="displayDocumentTitle">True to request the document metadata title; false to request the file name.</param>
    void SetDisplayDocumentTitle(bool displayDocumentTitle);

    /// <summary>
    /// Selects the panel or presentation mode used when the document opens.
    /// </summary>
    /// <param name="mode">The initial page mode.</param>
    void SetInitialPageMode(PdfPageMode mode);

    /// <summary>
    /// Selects the page arrangement used when the document opens.
    /// </summary>
    /// <param name="layout">The initial page layout.</param>
    void SetInitialPageLayout(PdfPageLayout layout);

    /// <summary>
    /// Adds an accessibility node to this document and returns the stored value.
    /// </summary>
    /// <param name="page">The PDF page that receives or owns the content.</param>
    /// <param name="role">The role to use for this operation.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    PdfAccessibilityNode AddAccessibilityNode(PdfPage page, string role);

    /// <summary>
    /// Adds an accessibility node to this document and returns the stored value.
    /// </summary>
    /// <param name="page">The PDF page that receives or owns the content.</param>
    /// <param name="role">The role to use for this operation.</param>
    /// <param name="description">The description to use for this operation.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    PdfAccessibilityNode AddAccessibilityNodeWithDescription(PdfPage page, string role, string description);

    /// <summary>Enables structure-tree and parent-tree output.</summary>
    void EnableTaggedPdf();

    /// <summary>Determines whether tagged output is enabled.</summary>
    /// <returns>True when tagged output is enabled.</returns>
    bool IsTaggedPdfEnabled();

    /// <summary>Adds a child structure element on its parent's page.</summary>
    /// <param name="parent">The registered parent.</param>
    /// <param name="role">The standard structure role.</param>
    /// <param name="description">The optional description.</param>
    /// <returns>The child element.</returns>
    PdfAccessibilityNode AddStructureChild(PdfAccessibilityNode parent, string role, string description);

    /// <summary>Adds a child structure element on a requested page.</summary>
    /// <param name="parent">The registered parent.</param>
    /// <param name="page">The content page.</param>
    /// <param name="role">The standard structure role.</param>
    /// <param name="description">The optional description.</param>
    /// <returns>The child element.</returns>
    PdfAccessibilityNode AddStructureChildOnPage(PdfAccessibilityNode parent, PdfPage page, string role, string description);

    /// <summary>Sets structure alternate text.</summary>
    /// <param name="node">The registered element.</param>
    /// <param name="alternateText">The alternate description.</param>
    void SetStructureAlternateText(PdfAccessibilityNode node, string alternateText);

    /// <summary>Sets structure replacement text.</summary>
    /// <param name="node">The registered element.</param>
    /// <param name="actualText">The replacement text.</param>
    void SetStructureActualText(PdfAccessibilityNode node, string actualText);

    /// <summary>Sets a structure language override.</summary>
    /// <param name="node">The registered element.</param>
    /// <param name="language">The language tag.</param>
    void SetStructureLanguage(PdfAccessibilityNode node, string language);

    /// <summary>Sets tagged table-header scope.</summary>
    /// <param name="node">A TH structure element.</param>
    /// <param name="scope">The header scope.</param>
    void SetTableHeaderScope(PdfAccessibilityNode node, PdfTableHeaderScope scope);

    /// <summary>Moves an element within sibling reading order.</summary>
    /// <param name="node">The registered element.</param>
    /// <param name="position">The zero-based sibling position.</param>
    void SetStructureReadingOrder(PdfAccessibilityNode node, int position);

    /// <summary>Returns the root structure element count.</summary>
    /// <returns>The root count.</returns>
    int GetStructureRootCount();

    /// <summary>Returns a root structure element.</summary>
    /// <param name="index">The root position.</param>
    /// <returns>The root element.</returns>
    PdfAccessibilityNode GetStructureRoot(int index);

    /// <summary>Returns a structure child count.</summary>
    /// <param name="node">The parent element.</param>
    /// <returns>The child count.</returns>
    int GetStructureChildCount(PdfAccessibilityNode node);

    /// <summary>Returns a structure child.</summary>
    /// <param name="node">The parent element.</param>
    /// <param name="index">The child position.</param>
    /// <returns>The child element.</returns>
    PdfAccessibilityNode GetStructureChild(PdfAccessibilityNode node, int index);

    /// <summary>Tags an internal link annotation.</summary>
    /// <param name="node">The structure element.</param>
    /// <param name="link">The internal link.</param>
    void TagInternalLink(PdfAccessibilityNode node, PdfInternalLink link);

    /// <summary>Tags an external link annotation.</summary>
    /// <param name="node">The structure element.</param>
    /// <param name="link">The external link.</param>
    void TagExternalLink(PdfAccessibilityNode node, PdfExternalLink link);

    /// <summary>Tags a general annotation.</summary>
    /// <param name="node">The structure element.</param>
    /// <param name="annotation">The annotation.</param>
    void TagAnnotation(PdfAccessibilityNode node, PdfAnnotation annotation);

    /// <summary>Tags a form widget.</summary>
    /// <param name="node">The structure element.</param>
    /// <param name="field">The field or radio option.</param>
    void TagFormField(PdfAccessibilityNode node, PdfAcroFormField field);

    /// <summary>
    /// Checks the current document for accessibility preparation and returns the findings.
    /// </summary>
    /// <returns>The resulting accessibility preparation result.</returns>
    PdfAccessibilityPreparationResult CheckAccessibilityPreparation();

    /// <summary>Validates the syntax and object structure emitted by this document.</summary>
    /// <returns>The ordered validation findings.</returns>
    PdfValidationResult ValidatePdfStructure();

    /// <summary>Validates PDF/A preparation and emitted archival structures.</summary>
    /// <returns>The ordered validation findings.</returns>
    PdfValidationResult ValidatePdfAConformance();

    /// <summary>Validates PDF/UA preparation and emitted tagged structures.</summary>
    /// <returns>The ordered validation findings.</returns>
    PdfValidationResult ValidatePdfUaPreparation();

    /// <summary>
    /// Adds an attachment to this document and returns the stored value.
    /// </summary>
    /// <param name="fileName">The file name to use for this operation.</param>
    /// <param name="data">The binary data to copy into the PDF resource.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    PdfAttachment AddAttachment(string fileName, byte[] data);

    /// <summary>
    /// Adds an attachment to this document and returns the stored value.
    /// </summary>
    /// <param name="fileName">The file name to use for this operation.</param>
    /// <param name="data">The binary data to copy into the PDF resource.</param>
    /// <param name="mimeType">The mime type to use for this operation.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    PdfAttachment AddAttachmentWithMimeType(string fileName, byte[] data, string mimeType);

    /// <summary>
    /// Adds an attachment to this document and returns the stored value.
    /// </summary>
    /// <param name="fileName">The file name to use for this operation.</param>
    /// <param name="data">The binary data to copy into the PDF resource.</param>
    /// <param name="mimeType">The mime type to use for this operation.</param>
    /// <param name="description">The description to use for this operation.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when an argument is invalid for this operation.</exception>
    PdfAttachment AddAttachmentWithMetadata(string fileName, byte[] data, string mimeType, string description);

    /// <summary>Adds an embedded file with an explicit document relationship.</summary>
    /// <param name="fileName">The embedded file name.</param>
    /// <param name="data">The embedded bytes.</param>
    /// <param name="mimeType">The non-empty MIME type.</param>
    /// <param name="description">The file description.</param>
    /// <param name="relationship">The semantic document relationship.</param>
    /// <returns>The associated attachment.</returns>
    PdfAttachment AddAssociatedFile(string fileName, byte[] data, string mimeType, string description, PdfAssociatedFileRelationship relationship);

    /// <summary>
    /// Enables the document's interactive form dictionary and returns it for field configuration.
    /// </summary>
    /// <returns>The resulting acro form.</returns>
    PdfAcroForm UseAcroForm();

    /// <summary>
    /// Sets the acro form used for subsequent PDF generation.
    /// </summary>
    /// <param name="acroForm">The acro form to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void SetAcroForm(PdfAcroForm acroForm);

    /// <summary>
    /// Adds a text field to this document and returns the stored value.
    /// </summary>
    /// <param name="name">The name used to identify the value in the document.</param>
    /// <param name="page">The PDF page that receives or owns the content.</param>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="y">The vertical coordinate in PDF points from the page's bottom edge.</param>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    PdfAcroFormField AddTextField(string name, PdfPage page, double x, double y, double width, double height);

    /// <summary>
    /// Adds a text field to this document and returns the stored value.
    /// </summary>
    /// <param name="name">The name used to identify the value in the document.</param>
    /// <param name="page">The PDF page that receives or owns the content.</param>
    /// <param name="rectangle">The rectangle to use for this operation.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    PdfAcroFormField AddTextFieldInRectangle(string name, PdfPage page, PdfRectangle rectangle);

    /// <summary>
    /// Adds a checkbox to this document and returns the stored value.
    /// </summary>
    /// <param name="name">The name used to identify the value in the document.</param>
    /// <param name="page">The PDF page that receives or owns the content.</param>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="y">The vertical coordinate in PDF points from the page's bottom edge.</param>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    PdfAcroFormField AddCheckbox(string name, PdfPage page, double x, double y, double width, double height);

    /// <summary>
    /// Adds a checkbox to this document and returns the stored value.
    /// </summary>
    /// <param name="name">The name used to identify the value in the document.</param>
    /// <param name="page">The PDF page that receives or owns the content.</param>
    /// <param name="rectangle">The rectangle to use for this operation.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    PdfAcroFormField AddCheckboxInRectangle(string name, PdfPage page, PdfRectangle rectangle);

    /// <summary>Adds an empty mutually exclusive radio-button group.</summary>
    /// <param name="name">The unique terminal field name.</param>
    /// <returns>The registered radio-button group.</returns>
    PdfRadioButtonGroup AddRadioButtonGroup(string name);

    /// <summary>
    /// Draws current interactive form values into page content and removes the interactive fields.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the object is not in a state that permits this operation.</exception>
    void FlattenForm();

    /// <summary>
    /// Adds a signature placeholder to this document and returns the stored value.
    /// </summary>
    /// <param name="name">The name used to identify the value in the document.</param>
    /// <param name="page">The PDF page that receives or owns the content.</param>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="y">The vertical coordinate in PDF points from the page's bottom edge.</param>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    PdfAcroFormField AddSignaturePlaceholder(string name, PdfPage page, double x, double y, double width, double height);

    /// <summary>
    /// Adds a signature placeholder to this document and returns the stored value.
    /// </summary>
    /// <param name="name">The name used to identify the value in the document.</param>
    /// <param name="page">The PDF page that receives or owns the content.</param>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="y">The vertical coordinate in PDF points from the page's bottom edge.</param>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="contentsLength">The positive placeholder size in bytes. It cannot exceed <c>int.MaxValue / 2</c> because each byte is written as two hexadecimal characters.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a numeric argument falls outside its supported range.</exception>
    PdfAcroFormField AddSignaturePlaceholderWithLength(string name, PdfPage page, double x, double y, double width, double height, int contentsLength);

    /// <summary>
    /// Adds a signature placeholder to this document and returns the stored value.
    /// </summary>
    /// <param name="name">The name used to identify the value in the document.</param>
    /// <param name="page">The PDF page that receives or owns the content.</param>
    /// <param name="rectangle">The rectangle to use for this operation.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    PdfAcroFormField AddSignaturePlaceholderInRectangle(string name, PdfPage page, PdfRectangle rectangle);

    /// <summary>
    /// Selects the PDF specification version used for subsequent document output.
    /// </summary>
    /// <param name="version">The supported PDF version to write.</param>
    /// <exception cref="ArgumentException">Thrown when the version is not supported.</exception>
    /// <exception cref="InvalidOperationException">Thrown when an enabled feature cannot be represented by the selected version.</exception>
    void SetPdfVersion(PdfVersion version);

    /// <summary>
    /// Enables and configures deterministic writer mode for subsequent document output.
    /// </summary>
    void UseDeterministicWriterMode();

    /// <summary>
    /// Sets the writer options used for subsequent document output.
    /// </summary>
    /// <param name="options">The writer options to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void SetWriterOptions(PdfWriterOptions options);

    /// <summary>
    /// Enables and configures deterministic writer mode for subsequent document output.
    /// </summary>
    /// <param name="documentId">The document id to use for this operation.</param>
    /// <exception cref="ArgumentException">Thrown when an argument is invalid for this operation.</exception>
    void UseDeterministicWriterModeWithId(string documentId);

    /// <summary>
    /// Enables and configures compressed content streams for subsequent document output.
    /// </summary>
    void UseCompressedContentStreams();

    /// <summary>
    /// Enables and configures standard encryption for subsequent document output.
    /// </summary>
    /// <param name="userPassword">The user password to use for this operation.</param>
    /// <param name="ownerPassword">The owner password to use for this operation.</param>
    void UseStandardEncryption(string userPassword, string ownerPassword);

    /// <summary>
    /// Enables and configures standard encryption for subsequent document output.
    /// </summary>
    /// <param name="userPassword">The user password to use for this operation.</param>
    /// <param name="ownerPassword">The owner password to use for this operation.</param>
    /// <param name="permissions">The permissions to use for this operation.</param>
    void UseStandardEncryptionWithPermissions(string userPassword, string ownerPassword, PdfPermissions permissions);

    /// <summary>
    /// Enables and configures standard encryption with the supplied options.
    /// </summary>
    /// <param name="options">The encryption options to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void UseStandardEncryptionWithOptions(PdfEncryptionOptions options);

    /// <summary>
    /// Serializes the current PDF document to the requested destination.
    /// </summary>
    /// <param name="path">The vector path whose stored segments will be used.</param>
    void Save(string path);

    /// <summary>
    /// Serializes the current PDF document to the requested destination.
    /// </summary>
    /// <param name="stream">The writable stream that receives the PDF bytes; the method leaves ownership with the caller.</param>
    void SaveToStream(Stream stream);

    /// <summary>
    /// Serializes the current document and returns the complete PDF file as bytes.
    /// </summary>
    /// <returns>A new byte array containing the generated data.</returns>
    byte[] ToBytes();
}
