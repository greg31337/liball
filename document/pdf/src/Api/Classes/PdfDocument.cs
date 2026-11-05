using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace pdf;

/// <summary>
/// Represents a PDF document and coordinates its pages, resources, metadata, security settings, and output.
/// </summary>
/// <remarks>
/// Resources are registered on the document before saving. Saving serializes the current object graph; callers may continue editing the document and save it again.
/// </remarks>
public class PdfDocument : IPdfDocument
{
    internal PdfImageCache _imageCache;
    internal List<PdfPage> _pages;
    internal List<PdfFormXObject> _forms;
    internal List<PdfNamedDestination> _namedDestinations;
    internal List<PdfOutlineItem> _outlines;
    internal List<PdfAttachment> _attachments;
    internal List<PdfAccessibilityNode> _accessibilityNodes;
    internal List<PdfIndexMarker> _indexMarkers;
    internal List<PdfCrossReferenceMarker> _crossReferenceMarkers;
    internal List<PdfPublishingMarker> _publishingMarkers;
    internal List<PdfPageLabelRange> _pageLabelRanges;
    internal List<PdfStructureLinkTarget> _structureLinkTargets;
    internal PdfFontRegistry FontRegistry;
    /// <summary>
    /// Exposes the images currently stored by this object.
    /// </summary>
    internal IReadOnlyList<PdfImage> Images;
    /// <summary>
    /// Exposes the pages currently stored by this object.
    /// </summary>
    internal IReadOnlyList<PdfPage> Pages;
    /// <summary>
    /// Exposes the forms currently stored by this object.
    /// </summary>
    internal IReadOnlyList<PdfFormXObject> Forms;
    /// <summary>
    /// Exposes the named destinations currently stored by this object.
    /// </summary>
    internal IReadOnlyList<PdfNamedDestination> NamedDestinations;
    /// <summary>
    /// Exposes the outlines currently stored by this object.
    /// </summary>
    internal IReadOnlyList<PdfOutlineItem> Outlines;
    /// <summary>
    /// Exposes the attachments currently stored by this object.
    /// </summary>
    internal IReadOnlyList<PdfAttachment> Attachments;
    /// <summary>
    /// Exposes the accessibility nodes currently stored by this object.
    /// </summary>
    internal IReadOnlyList<PdfAccessibilityNode> AccessibilityNodes;
    internal IReadOnlyList<PdfIndexMarker> IndexMarkers;
    internal IReadOnlyList<PdfCrossReferenceMarker> CrossReferenceMarkers;
    internal IReadOnlyList<PdfPublishingMarker> PublishingMarkers;
    internal IReadOnlyList<PdfPageLabelRange> PageLabelRanges;
    internal IReadOnlyList<PdfStructureLinkTarget> StructureLinkTargets;
    /// <summary>
    /// Stores the metadata used by this object.
    /// </summary>
    internal PdfMetadata Metadata;
    internal bool HasPdfA;
    internal PdfAPreparationOptions PdfAOptions;
    internal bool HasFacturX;
    internal PdfFacturXMetadata FacturXMetadata;
    internal bool HasTaggedPdf;
    internal List<PdfAccessibilityNode> _structureRoots;
    internal IReadOnlyList<PdfAccessibilityNode> StructureRoots;
    /// <summary>
    /// Stores the language used by this object.
    /// </summary>
    internal string Language;
    /// <summary>
    /// Stores the writer options used by this object.
    /// </summary>
    internal PdfWriterOptions WriterOptions;
    /// <summary>
    /// Indicates whether this object has acro form.
    /// </summary>
    internal bool HasAcroForm;
    /// <summary>
    /// Stores the acro form used by this object.
    /// </summary>
    internal PdfAcroForm AcroForm;
    /// <summary>
    /// Indicates whether this object has encryption.
    /// </summary>
    internal bool HasEncryption;
    /// <summary>
    /// Stores the encryption options used by this object.
    /// </summary>
    internal PdfEncryptionOptions EncryptionOptions;
    internal bool HasViewerPreferences;
    internal PdfViewerPreferences ViewerPreferences;
    internal bool HasInitialPageMode;
    internal PdfPageMode InitialPageMode;
    internal bool HasInitialPageLayout;
    internal PdfPageLayout InitialPageLayout;

    /// <summary>
    /// Initializes a new document.
    /// </summary>
    public PdfDocument()
    {
        _imageCache = new PdfImageCache();
        _pages = new List<PdfPage>();
        _forms = new List<PdfFormXObject>();
        _namedDestinations = new List<PdfNamedDestination>();
        _outlines = new List<PdfOutlineItem>();
        _attachments = new List<PdfAttachment>();
        _accessibilityNodes = new List<PdfAccessibilityNode>();
        _indexMarkers = new List<PdfIndexMarker>();
        _crossReferenceMarkers = new List<PdfCrossReferenceMarker>();
        _publishingMarkers = new List<PdfPublishingMarker>();
        _pageLabelRanges = new List<PdfPageLabelRange>();
        _structureLinkTargets = new List<PdfStructureLinkTarget>();
        FontRegistry = PdfFontRegistry.CreateStandard();
        Images = _imageCache.Images;
        Pages = _pages;
        Forms = _forms;
        NamedDestinations = _namedDestinations;
        Outlines = _outlines;
        Attachments = _attachments;
        AccessibilityNodes = _accessibilityNodes;
        IndexMarkers = _indexMarkers;
        CrossReferenceMarkers = _crossReferenceMarkers;
        PublishingMarkers = _publishingMarkers;
        PageLabelRanges = _pageLabelRanges;
        StructureLinkTargets = _structureLinkTargets;
        Metadata = new PdfMetadata();
        HasPdfA = false;
        PdfAOptions = new PdfAPreparationOptions();
        HasFacturX = false;
        FacturXMetadata = new PdfFacturXMetadata(PdfFacturXProfile.En16931);
        HasTaggedPdf = false;
        _structureRoots = new List<PdfAccessibilityNode>();
        StructureRoots = _structureRoots;
        Language = string.Empty;
        WriterOptions = new PdfWriterOptions();
        HasAcroForm = false;
        AcroForm = new PdfAcroForm();
        AcroForm.SetOwnerDocument(this);
        HasEncryption = false;
        EncryptionOptions = new PdfEncryptionOptions(string.Empty, string.Empty, new PdfPermissions());
        HasViewerPreferences = false;
        ViewerPreferences = new PdfViewerPreferences();
        HasInitialPageMode = false;
        InitialPageMode = PdfPageMode.UseNone;
        HasInitialPageLayout = false;
        InitialPageLayout = PdfPageLayout.SinglePage;
    }

    /// <summary>
    /// Returns the font registry owned by this document.
    /// </summary>
    /// <returns>The document font registry.</returns>
    public PdfFontRegistry GetFontRegistry()
    {
        return FontRegistry;
    }

    /// <summary>
    /// Loads and registers a TrueType or OpenType font from an in-memory byte array.
    /// </summary>
    /// <param name="fontData">The complete TrueType or OpenType font data.</param>
    /// <returns>The registered font, or the existing registration when the same font data was registered previously.</returns>
    /// <exception cref="ArgumentNullException">Thrown when font data is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when font data is malformed or unsupported.</exception>
    public PdfTrueTypeFont RegisterTrueTypeFont(byte[] fontData)
    {
        return FontRegistry.RegisterTrueTypeFont(fontData);
    }

    /// <summary>
    /// Loads and registers a TrueType or OpenType font from the current position of a readable stream.
    /// </summary>
    /// <param name="stream">The readable stream containing complete TrueType or OpenType font data.</param>
    /// <returns>The registered font, or the existing registration when the same font data was registered previously.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the stream is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the stream is not readable.</exception>
    /// <exception cref="InvalidDataException">Thrown when font data is malformed or unsupported.</exception>
    public PdfTrueTypeFont RegisterTrueTypeFontFromStream(Stream stream)
    {
        return FontRegistry.RegisterTrueTypeFontFromStream(stream);
    }

    /// <summary>
    /// Loads and registers a TrueType or OpenType font from a file.
    /// </summary>
    /// <param name="path">The path to a TrueType or OpenType font file.</param>
    /// <returns>The registered font, or the existing registration when the same font data was registered previously.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the path is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when font data is malformed or unsupported.</exception>
    public PdfTrueTypeFont RegisterTrueTypeFontFromFile(string path)
    {
        return FontRegistry.RegisterTrueTypeFontFromFile(path);
    }

    /// <summary>
    /// Returns the number of pages currently stored in this document.
    /// </summary>
    /// <returns>The current page count.</returns>
    public int GetPageCount()
    {
        return _pages.Count;
    }

    /// <summary>
    /// Returns the page at the requested zero-based position.
    /// </summary>
    /// <param name="index">The zero-based position of the page to return.</param>
    /// <returns>The page stored at the requested position.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the index is outside the document page range.</exception>
    public PdfPage GetPage(int index)
    {
        if (index < 0 || index >= _pages.Count)
        {
            throw new ArgumentOutOfRangeException("index", "Page index must be within the document page range.");
        }

        return _pages[index];
    }

    /// <summary>
    /// Adds a page to this document and returns the stored value.
    /// </summary>
    /// <returns>The object that was added to the owning document or collection.</returns>
    public PdfPage AddLetterPage()
    {
        return AddPage(PdfPageSize.LetterWidth, PdfPageSize.LetterHeight);
    }

    /// <summary>
    /// Adds a page to this document and returns the stored value.
    /// </summary>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    public PdfPage AddPage(double width, double height)
    {
        PdfPage page = new PdfPage(width, height);
        page.OwnerDocument = this;
        _pages.Add(page);
        return page;
    }

    /// <summary>
    /// Adds a page to this document and returns the stored value.
    /// </summary>
    /// <param name="size">The size to use for this operation.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public PdfPage AddPageWithDimensions(PdfPageDimensions size)
    {
        if (size == null)
        {
            throw new ArgumentNullException("size");
        }

        return AddPage(size.Width, size.Height);
    }

    /// <summary>
    /// Inserts a page at the requested position and returns the new value.
    /// </summary>
    /// <param name="index">The zero-based position at which to perform the operation.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    public PdfPage InsertLetterPage(int index)
    {
        return InsertPage(index, PdfPageSize.LetterWidth, PdfPageSize.LetterHeight);
    }

    /// <summary>
    /// Inserts a page at the requested position and returns the new value.
    /// </summary>
    /// <param name="index">The zero-based position at which to perform the operation.</param>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a numeric argument falls outside its supported range.</exception>
    public PdfPage InsertPage(int index, double width, double height)
    {
        if (index < 0 || index > _pages.Count)
        {
            throw new ArgumentOutOfRangeException("index", "Page index must be within the document page range.");
        }

        PdfPage page = new PdfPage(width, height);
        page.OwnerDocument = this;
        _pages.Insert(index, page);
        return page;
    }

    /// <summary>
    /// Inserts a page at the requested position and returns the new value.
    /// </summary>
    /// <param name="index">The zero-based position at which to perform the operation.</param>
    /// <param name="size">The size to use for this operation.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public PdfPage InsertPageWithDimensions(int index, PdfPageDimensions size)
    {
        if (size == null)
        {
            throw new ArgumentNullException("size");
        }

        return InsertPage(index, size.Width, size.Height);
    }

    /// <summary>
    /// Imports a copy of one page and its related document objects at the end of this document.
    /// </summary>
    /// <param name="source">The source document whose in-memory object graph is copied.</param>
    /// <param name="sourcePageIndex">The zero-based source page position.</param>
    /// <returns>The imported page owned by this document.</returns>
    public PdfPage ImportPage(PdfDocument source, int sourcePageIndex)
    {
        return InsertImportedPage(source, sourcePageIndex, _pages.Count);
    }

    /// <summary>
    /// Imports a copy of one page and its related document objects at a requested position.
    /// </summary>
    /// <param name="source">The source document whose in-memory object graph is copied.</param>
    /// <param name="sourcePageIndex">The zero-based source page position.</param>
    /// <param name="targetPageIndex">The zero-based insertion position in this document.</param>
    /// <returns>The imported page owned by this document.</returns>
    public PdfPage InsertImportedPage(PdfDocument source, int sourcePageIndex, int targetPageIndex)
    {
        PdfDocumentGraphCopier copier = new PdfDocumentGraphCopier(source, this);
        return copier.CopySinglePage(sourcePageIndex, targetPageIndex);
    }

    /// <summary>
    /// Places a source page's drawing commands behind the existing content of a target page.
    /// </summary>
    /// <param name="source">The document containing the visual stamp.</param>
    /// <param name="sourcePageIndex">The zero-based source page position.</param>
    /// <param name="targetPageIndex">The zero-based target page position.</param>
    public void StampPageBackground(PdfDocument source, int sourcePageIndex, int targetPageIndex)
    {
        PdfDocumentGraphCopier copier = new PdfDocumentGraphCopier(source, this);
        copier.CopyPageStamp(sourcePageIndex, targetPageIndex, false);
    }

    /// <summary>
    /// Places a source page's drawing commands in front of the existing content of a target page.
    /// </summary>
    /// <param name="source">The document containing the visual stamp.</param>
    /// <param name="sourcePageIndex">The zero-based source page position.</param>
    /// <param name="targetPageIndex">The zero-based target page position.</param>
    public void StampPageForeground(PdfDocument source, int sourcePageIndex, int targetPageIndex)
    {
        PdfDocumentGraphCopier copier = new PdfDocumentGraphCopier(source, this);
        copier.CopyPageStamp(sourcePageIndex, targetPageIndex, true);
    }

    /// <summary>
    /// Splits this document into deterministic consecutive documents of at most the requested size.
    /// </summary>
    /// <param name="maximumPagesPerDocument">The positive maximum number of pages in each result.</param>
    /// <returns>Independent documents in source page order.</returns>
    public PdfDocument[] SplitDocument(int maximumPagesPerDocument)
    {
        if (maximumPagesPerDocument <= 0)
        {
            throw new ArgumentOutOfRangeException("maximumPagesPerDocument", "Maximum pages per document must be positive.");
        }

        int resultCount = _pages.Count == 0 ? 0 : (_pages.Count - 1) / maximumPagesPerDocument + 1;
        PdfDocument[] results = new PdfDocument[resultCount];
        for (int resultIndex = 0; resultIndex < resultCount; resultIndex++)
        {
            int firstPageIndex = resultIndex * maximumPagesPerDocument;
            int pageCount = maximumPagesPerDocument;
            if (firstPageIndex + pageCount > _pages.Count)
            {
                pageCount = _pages.Count - firstPageIndex;
            }

            PdfDocument result = new PdfDocument();
            PdfDocumentGraphCopier copier = new PdfDocumentGraphCopier(this, result);
            copier.CopyPageRange(firstPageIndex, pageCount, 0, true);
            results[resultIndex] = result;
        }

        return results;
    }

    /// <summary>
    /// Appends independent copies of all pages and related objects from another document.
    /// </summary>
    /// <param name="source">The source document whose object graph is merged.</param>
    public void MergeDocument(PdfDocument source)
    {
        PdfDocumentGraphCopier copier = new PdfDocumentGraphCopier(source, this);
        copier.CopyPageRange(0, source.GetPageCount(), _pages.Count, false);
    }

    /// <summary>
    /// Adds an image to this document and returns the stored value.
    /// </summary>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="rgbData">Packed RGB bytes in row-major order, with three bytes per pixel.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    public PdfImage AddImage(int width, int height, byte[] rgbData)
    {
        return _imageCache.AddRgbImage(width, height, rgbData);
    }

    /// <summary>
    /// Adds packed eight-bit grayscale samples as an image resource.
    /// </summary>
    /// <param name="width">The positive pixel width.</param>
    /// <param name="height">The positive pixel height.</param>
    /// <param name="grayscaleData">Packed grayscale bytes in row-major order, with one byte per pixel.</param>
    /// <returns>The registered image, or the existing resource when the same samples were registered previously.</returns>
    public PdfImage AddGrayscaleImage(int width, int height, byte[] grayscaleData)
    {
        return _imageCache.AddGrayscaleImage(width, height, grayscaleData);
    }

    /// <summary>
    /// Adds packed eight-bit RGB samples as an image resource.
    /// </summary>
    /// <param name="width">The positive pixel width.</param>
    /// <param name="height">The positive pixel height.</param>
    /// <param name="rgbData">Packed RGB bytes in row-major order, with three bytes per pixel.</param>
    /// <returns>The registered image, or the existing resource when the same samples were registered previously.</returns>
    public PdfImage AddRgbImage(int width, int height, byte[] rgbData)
    {
        return _imageCache.AddRgbImage(width, height, rgbData);
    }

    /// <summary>
    /// Adds packed eight-bit RGBA samples as an image resource with an alpha soft mask.
    /// </summary>
    /// <param name="width">The positive pixel width.</param>
    /// <param name="height">The positive pixel height.</param>
    /// <param name="rgbaData">Packed RGBA bytes in row-major order, with four bytes per pixel.</param>
    /// <returns>The registered image, or the existing resource when the same samples were registered previously.</returns>
    public PdfImage AddRgbaImage(int width, int height, byte[] rgbaData)
    {
        return _imageCache.AddRgbaImage(width, height, rgbaData);
    }

    /// <summary>
    /// Loads and adds a JPEG image from an in-memory byte array.
    /// </summary>
    /// <param name="jpegData">The complete encoded JPEG data.</param>
    /// <returns>The registered image, or the existing resource when the same JPEG data was registered previously.</returns>
    /// <exception cref="ArgumentNullException">Thrown when JPEG data is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when JPEG data is malformed or unsupported.</exception>
    public PdfImage AddJpeg(byte[] jpegData)
    {
        PdfJpegData loaded = new PdfJpegLoader().Load(jpegData);
        return _imageCache.AddJpegImage(loaded);
    }

    /// <summary>
    /// Loads and adds a JPEG image from a read-only memory span.
    /// </summary>
    /// <param name="jpegData">The complete encoded JPEG data.</param>
    /// <returns>The registered image, or the existing resource when the same JPEG data was registered previously.</returns>
    /// <exception cref="InvalidDataException">Thrown when JPEG data is malformed or unsupported.</exception>
    public PdfImage AddJpegFromSpan(ReadOnlySpan<byte> jpegData)
    {
        PdfJpegData loaded = new PdfJpegLoader().LoadFromSpan(jpegData);
        return _imageCache.AddJpegImage(loaded);
    }

    /// <summary>
    /// Loads and adds a JPEG image from the current position of a readable stream without taking ownership of it.
    /// </summary>
    /// <param name="stream">The readable stream containing complete encoded JPEG data.</param>
    /// <returns>The registered image, or the existing resource when the same JPEG data was registered previously.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the stream is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the stream is not readable.</exception>
    /// <exception cref="InvalidDataException">Thrown when JPEG data is malformed or unsupported.</exception>
    public PdfImage AddJpegFromStream(Stream stream)
    {
        PdfJpegData loaded = new PdfJpegLoader().LoadFromStream(stream);
        return _imageCache.AddJpegImage(loaded);
    }

    /// <summary>
    /// Loads and adds a JPEG image from a file.
    /// </summary>
    /// <param name="path">The path to the JPEG file.</param>
    /// <returns>The registered image, or the existing resource when the same JPEG data was registered previously.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the path is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when JPEG data is malformed or unsupported.</exception>
    public PdfImage AddJpegFromFile(string path)
    {
        PdfJpegData loaded = new PdfJpegLoader().LoadFromFile(path);
        return _imageCache.AddJpegImage(loaded);
    }

    /// <summary>
    /// Loads and adds a PNG image from an in-memory byte array.
    /// </summary>
    /// <param name="pngData">The complete encoded PNG data.</param>
    /// <returns>The registered image, or the existing resource when the same PNG data was registered previously.</returns>
    /// <exception cref="ArgumentNullException">Thrown when PNG data is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when PNG data is malformed, animated, or unsupported.</exception>
    public PdfImage AddPng(byte[] pngData)
    {
        PdfPngData loaded = new PdfPngLoader().Load(pngData);
        return _imageCache.AddPngImage(loaded);
    }

    /// <summary>
    /// Loads and adds a PNG image from a read-only memory span.
    /// </summary>
    /// <param name="pngData">The complete encoded PNG data.</param>
    /// <returns>The registered image, or the existing resource when the same PNG data was registered previously.</returns>
    /// <exception cref="InvalidDataException">Thrown when PNG data is malformed, animated, or unsupported.</exception>
    public PdfImage AddPngFromSpan(ReadOnlySpan<byte> pngData)
    {
        PdfPngData loaded = new PdfPngLoader().LoadFromSpan(pngData);
        return _imageCache.AddPngImage(loaded);
    }

    /// <summary>
    /// Loads and adds a PNG image from the current position of a readable stream without taking ownership of it.
    /// </summary>
    /// <param name="stream">The readable stream containing complete encoded PNG data.</param>
    /// <returns>The registered image, or the existing resource when the same PNG data was registered previously.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the stream is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the stream is not readable.</exception>
    /// <exception cref="InvalidDataException">Thrown when PNG data is malformed, animated, or unsupported.</exception>
    public PdfImage AddPngFromStream(Stream stream)
    {
        PdfPngData loaded = new PdfPngLoader().LoadFromStream(stream);
        return _imageCache.AddPngImage(loaded);
    }

    /// <summary>
    /// Loads and adds a PNG image from a file.
    /// </summary>
    /// <param name="path">The path to the PNG file.</param>
    /// <returns>The registered image, or the existing resource when the same PNG data was registered previously.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the path is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when PNG data is malformed, animated, or unsupported.</exception>
    public PdfImage AddPngFromFile(string path)
    {
        PdfPngData loaded = new PdfPngLoader().LoadFromFile(path);
        return _imageCache.AddPngImage(loaded);
    }

    /// <summary>
    /// Detects, loads, and adds an encoded JPEG or PNG image from an in-memory byte array.
    /// </summary>
    /// <param name="encodedData">The complete encoded JPEG or PNG data.</param>
    /// <returns>The registered image, or the existing resource when the same source was registered previously.</returns>
    /// <exception cref="ArgumentNullException">Thrown when encoded data is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when the encoded format is unknown or malformed.</exception>
    public PdfImage AddEncodedImage(byte[] encodedData)
    {
        if (encodedData == null)
        {
            throw new ArgumentNullException("encodedData");
        }

        return AddEncodedImageFromSpan(encodedData.AsSpan());
    }

    /// <summary>
    /// Detects, loads, and adds an encoded JPEG or PNG image from a read-only memory span.
    /// </summary>
    /// <param name="encodedData">The complete encoded JPEG or PNG data.</param>
    /// <returns>The registered image, or the existing resource when the same source was registered previously.</returns>
    /// <exception cref="InvalidDataException">Thrown when the encoded format is unknown or malformed.</exception>
    public PdfImage AddEncodedImageFromSpan(ReadOnlySpan<byte> encodedData)
    {
        if (IsPngSignature(encodedData))
        {
            return AddPngFromSpan(encodedData);
        }

        if (encodedData.Length >= 2 && encodedData[0] == 255 && encodedData[1] == 216)
        {
            return AddJpegFromSpan(encodedData);
        }

        throw new InvalidDataException("Encoded image data must use a supported JPEG or PNG signature.");
    }

    /// <summary>
    /// Detects, loads, and adds an encoded JPEG or PNG image from the current position of a readable stream without taking ownership of it.
    /// </summary>
    /// <param name="stream">The readable stream containing complete encoded JPEG or PNG data.</param>
    /// <returns>The registered image, or the existing resource when the same source was registered previously.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the stream is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the stream is not readable.</exception>
    /// <exception cref="InvalidDataException">Thrown when the encoded format is unknown or malformed.</exception>
    public PdfImage AddEncodedImageFromStream(Stream stream)
    {
        if (stream == null)
        {
            throw new ArgumentNullException("stream");
        }

        if (!stream.CanRead)
        {
            throw new ArgumentException("Encoded image stream must be readable.", "stream");
        }

        using (MemoryStream output = new MemoryStream())
        {
            stream.CopyTo(output);
            return AddEncodedImage(output.ToArray());
        }
    }

    /// <summary>
    /// Detects, loads, and adds an encoded JPEG or PNG image from a file.
    /// </summary>
    /// <param name="path">The path to the encoded JPEG or PNG file.</param>
    /// <returns>The registered image, or the existing resource when the same source was registered previously.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the path is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when the encoded format is unknown or malformed.</exception>
    public PdfImage AddEncodedImageFromFile(string path)
    {
        if (path == null)
        {
            throw new ArgumentNullException("path");
        }

        return AddEncodedImage(File.ReadAllBytes(path));
    }

    internal static bool IsPngSignature(ReadOnlySpan<byte> encodedData)
    {
        if (encodedData.Length < 8)
        {
            return false;
        }

        return encodedData[0] == 137 && encodedData[1] == 80 && encodedData[2] == 78 && encodedData[3] == 71 && encodedData[4] == 13 && encodedData[5] == 10 && encodedData[6] == 26 && encodedData[7] == 10;
    }

    /// <summary>
    /// Adds a form to this document and returns the stored value.
    /// </summary>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a numeric argument falls outside its supported range.</exception>
    public PdfFormXObject AddForm(double width, double height)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException("width", "Form width must be positive.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException("height", "Form height must be positive.");
        }

        PdfFormXObject form = new PdfFormXObject("Fm" + (_forms.Count + 1).ToString(CultureInfo.InvariantCulture), width, height);
        _forms.Add(form);
        return form;
    }

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
    public PdfFormXObject AddQrCode(string text, double moduleSize, PdfQrErrorCorrection errorCorrection, int quietZone)
    {
        return PdfBarcodeResourceBuilder.CreateQrForm(this, text, moduleSize, errorCorrection, quietZone);
    }

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
    public PdfImage AddQrCodeImage(string text, int modulePixels, PdfQrErrorCorrection errorCorrection, int quietZone)
    {
        return PdfBarcodeResourceBuilder.CreateQrImage(this, text, modulePixels, errorCorrection, quietZone);
    }

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
    public PdfFormXObject AddBarcode(string text, PdfBarcodeFormat format, double moduleWidth, double barHeight, bool includeText)
    {
        return PdfBarcodeResourceBuilder.CreateLinearForm(this, text, format, moduleWidth, barHeight, includeText);
    }

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
    public PdfImage AddBarcodeImage(string text, PdfBarcodeFormat format, int modulePixels, int barHeightPixels)
    {
        return PdfBarcodeResourceBuilder.CreateLinearImage(this, text, format, modulePixels, barHeightPixels);
    }

    /// <summary>
    /// Securely imports SVG markup as a reusable native PDF form.
    /// </summary>
    /// <param name="svgText">The complete SVG markup.</param>
    /// <returns>The registered reusable form containing native PDF drawing commands.</returns>
    /// <exception cref="ArgumentNullException">Thrown when SVG markup is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when SVG content is unsafe or outside the supported subset.</exception>
    public PdfFormXObject AddSvg(string svgText)
    {
        PdfSvgLoader loader = new PdfSvgLoader();
        return AddLoadedSvg(loader.Load(svgText));
    }

    /// <summary>
    /// Securely imports SVG markup from a readable stream without taking ownership of it.
    /// </summary>
    /// <param name="stream">The readable stream containing complete SVG markup.</param>
    /// <returns>The registered reusable form containing native PDF drawing commands.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the stream is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the stream is not readable.</exception>
    /// <exception cref="InvalidDataException">Thrown when SVG content is unsafe or outside the supported subset.</exception>
    public PdfFormXObject AddSvgFromStream(Stream stream)
    {
        PdfSvgLoader loader = new PdfSvgLoader();
        return AddLoadedSvg(loader.LoadFromStream(stream));
    }

    /// <summary>
    /// Securely imports SVG markup from a file as a reusable native PDF form.
    /// </summary>
    /// <param name="path">The path to the SVG file.</param>
    /// <returns>The registered reusable form containing native PDF drawing commands.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the path is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when SVG content is unsafe or outside the supported subset.</exception>
    public PdfFormXObject AddSvgFromFile(string path)
    {
        PdfSvgLoader loader = new PdfSvgLoader();
        return AddLoadedSvg(loader.LoadFromFile(path));
    }

    /// <summary>
    /// Renders previously loaded SVG data as a reusable native PDF form.
    /// </summary>
    /// <param name="svgData">The validated SVG data to render.</param>
    /// <returns>The registered reusable form containing native PDF drawing commands.</returns>
    /// <exception cref="ArgumentNullException">Thrown when SVG data is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when SVG content is outside the supported subset.</exception>
    public PdfFormXObject AddLoadedSvg(PdfSvgData svgData)
    {
        if (svgData == null)
        {
            throw new ArgumentNullException("svgData");
        }

        PdfFormXObject form = new PdfFormXObject("Fm" + (_forms.Count + 1).ToString(CultureInfo.InvariantCulture), svgData.Width, svgData.Height);
        PdfSvgRenderer renderer = new PdfSvgRenderer();
        renderer.Render(svgData, form.Canvas);
        _forms.Add(form);
        return form;
    }

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
    public PdfNamedDestination AddNamedDestination(string name, PdfPage page, double x, double y)
    {
        ValidateDestinationName(name, "name");

        if (page == null)
        {
            throw new ArgumentNullException("page");
        }

        if (!ContainsPage(page))
        {
            throw new ArgumentException("Named destination page must belong to the document.", "page");
        }

        if (HasNamedDestination(name))
        {
            throw new ArgumentException("A named destination with the same name already exists.", "name");
        }

        PdfNamedDestination destination = new PdfNamedDestination(name, page, x, y);
        _namedDestinations.Add(destination);
        return destination;
    }

    /// <summary>
    /// Adds a named destination to this document and returns the stored value.
    /// </summary>
    /// <param name="name">The name used to identify the value in the document.</param>
    /// <param name="page">The PDF page that receives or owns the content.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public PdfNamedDestination AddNamedDestinationAtPageTop(string name, PdfPage page)
    {
        if (page == null)
        {
            throw new ArgumentNullException("page");
        }

        return AddNamedDestination(name, page, page.GetContentLeft(), page.GetContentTop());
    }

    /// <summary>
    /// Adds an outline to this document and returns the stored value.
    /// </summary>
    /// <param name="title">The title to use for this operation.</param>
    /// <param name="page">The PDF page that receives or owns the content.</param>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="y">The vertical coordinate in PDF points from the page's bottom edge.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    public PdfOutlineItem AddOutline(string title, PdfPage page, double x, double y)
    {
        ValidateOutlineTitle(title);
        ValidateDocumentPage(page, "page", "Outline page must belong to the document.");

        PdfOutlineItem item = new PdfOutlineItem(title, page, x, y);
        _outlines.Add(item);
        return item;
    }

    /// <summary>
    /// Adds an outline to this document and returns the stored value.
    /// </summary>
    /// <param name="title">The title to use for this operation.</param>
    /// <param name="page">The PDF page that receives or owns the content.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public PdfOutlineItem AddOutlineAtPageTop(string title, PdfPage page)
    {
        if (page == null)
        {
            throw new ArgumentNullException("page");
        }

        return AddOutline(title, page, page.GetContentLeft(), page.GetContentTop());
    }

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
    public PdfOutlineItem AddOutlineChild(PdfOutlineItem parent, string title, PdfPage page, double x, double y)
    {
        if (parent == null)
        {
            throw new ArgumentNullException("parent");
        }

        if (!ContainsOutlineItem(parent))
        {
            throw new ArgumentException("Parent outline item must belong to the document.", "parent");
        }

        ValidateOutlineTitle(title);
        ValidateDocumentPage(page, "page", "Outline page must belong to the document.");

        PdfOutlineItem item = new PdfOutlineItem(title, page, x, y);
        parent.AddChild(item);
        return item;
    }

    /// <summary>
    /// Adds an outline child to this document and returns the stored value.
    /// </summary>
    /// <param name="parent">The parent to use for this operation.</param>
    /// <param name="title">The title to use for this operation.</param>
    /// <param name="page">The PDF page that receives or owns the content.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public PdfOutlineItem AddOutlineChildAtPageTop(PdfOutlineItem parent, string title, PdfPage page)
    {
        if (page == null)
        {
            throw new ArgumentNullException("page");
        }

        return AddOutlineChild(parent, title, page, page.GetContentLeft(), page.GetContentTop());
    }

    /// <summary>
    /// Adds a primary index-term marker at an explicit page position.
    /// </summary>
    /// <param name="term">The non-empty primary index term.</param>
    /// <param name="page">The target page owned by this document.</param>
    /// <param name="x">The horizontal target coordinate.</param>
    /// <param name="y">The vertical target coordinate.</param>
    /// <returns>The registered index marker.</returns>
    public PdfIndexMarker AddIndexMarker(string term, PdfPage page, double x, double y)
    {
        return CreateIndexMarker(term, string.Empty, page, x, y);
    }

    /// <summary>
    /// Adds a secondary index-term marker at an explicit page position.
    /// </summary>
    /// <param name="term">The non-empty primary index term.</param>
    /// <param name="subterm">The non-empty secondary index term.</param>
    /// <param name="page">The target page owned by this document.</param>
    /// <param name="x">The horizontal target coordinate.</param>
    /// <param name="y">The vertical target coordinate.</param>
    /// <returns>The registered index marker.</returns>
    public PdfIndexMarker AddIndexSubentryMarker(string term, string subterm, PdfPage page, double x, double y)
    {
        ValidateRequiredText(subterm, "subterm", "Index subterm cannot be empty.");
        return CreateIndexMarker(term, subterm, page, x, y);
    }

    /// <summary>Returns the number of registered index markers.</summary>
    /// <returns>The index marker count.</returns>
    public int GetIndexMarkerCount()
    {
        return _indexMarkers.Count;
    }

    /// <summary>Returns a registered index marker.</summary>
    /// <param name="index">The zero-based marker position.</param>
    /// <returns>The requested index marker.</returns>
    public PdfIndexMarker GetIndexMarker(int index)
    {
        if (index < 0 || index >= _indexMarkers.Count)
        {
            throw new ArgumentOutOfRangeException("index", "Index marker position is outside the registered range.");
        }

        return _indexMarkers[index];
    }

    /// <summary>
    /// Adds a named cross-reference marker at an explicit page position.
    /// </summary>
    /// <param name="name">The unique non-empty marker name.</param>
    /// <param name="text">The non-empty display text.</param>
    /// <param name="page">The target page owned by this document.</param>
    /// <param name="x">The horizontal target coordinate.</param>
    /// <param name="y">The vertical target coordinate.</param>
    /// <returns>The registered cross-reference marker.</returns>
    public PdfCrossReferenceMarker AddCrossReferenceMarker(string name, string text, PdfPage page, double x, double y)
    {
        ValidateRequiredText(name, "name", "Cross-reference marker name cannot be empty.");
        ValidateRequiredText(text, "text", "Cross-reference marker text cannot be empty.");
        ValidateDocumentPage(page, "page", "Cross-reference marker page must belong to the document.");
        if (FindCrossReferenceMarker(name) != null)
        {
            throw new ArgumentException("A cross-reference marker with the same name already exists.", "name");
        }

        string destinationName = CreateNavigationDestinationName("xref");
        AddNamedDestination(destinationName, page, x, y);
        PdfCrossReferenceMarker marker = new PdfCrossReferenceMarker(name, text, page, x, y, destinationName);
        _crossReferenceMarkers.Add(marker);
        return marker;
    }

    /// <summary>Returns the number of registered cross-reference markers.</summary>
    /// <returns>The cross-reference marker count.</returns>
    public int GetCrossReferenceMarkerCount()
    {
        return _crossReferenceMarkers.Count;
    }

    /// <summary>Returns a registered cross-reference marker.</summary>
    /// <param name="index">The zero-based marker position.</param>
    /// <returns>The requested cross-reference marker.</returns>
    public PdfCrossReferenceMarker GetCrossReferenceMarker(int index)
    {
        if (index < 0 || index >= _crossReferenceMarkers.Count)
        {
            throw new ArgumentOutOfRangeException("index", "Cross-reference marker position is outside the registered range.");
        }

        return _crossReferenceMarkers[index];
    }

    /// <summary>
    /// Adds a link annotation from a source page to a registered cross-reference marker.
    /// </summary>
    /// <param name="sourcePage">The page receiving the link annotation.</param>
    /// <param name="markerName">The registered cross-reference marker name.</param>
    /// <param name="rectangle">The clickable rectangle.</param>
    /// <returns>The stored internal link.</returns>
    public PdfInternalLink AddCrossReferenceLink(PdfPage sourcePage, string markerName, PdfRectangle rectangle)
    {
        ValidateDocumentPage(sourcePage, "sourcePage", "Cross-reference source page must belong to the document.");
        ValidateRequiredText(markerName, "markerName", "Cross-reference marker name cannot be empty.");
        if (rectangle == null)
        {
            throw new ArgumentNullException("rectangle");
        }

        PdfCrossReferenceMarker? marker = FindCrossReferenceMarker(markerName);
        if (marker == null)
        {
            throw new ArgumentException("Cross-reference marker is not registered on the document.", "markerName");
        }

        return sourcePage.AddInternalLinkInRectangle(marker.DestinationName, rectangle);
    }

    /// <summary>Formats a cross-reference marker with its current page label.</summary>
    /// <param name="markerName">The registered marker name.</param>
    /// <returns>The marker text followed by its page label.</returns>
    public string GetCrossReferenceText(string markerName)
    {
        ValidateRequiredText(markerName, "markerName", "Cross-reference marker name cannot be empty.");
        PdfCrossReferenceMarker? marker = FindCrossReferenceMarker(markerName);
        if (marker == null)
        {
            throw new ArgumentException("Cross-reference marker is not registered on the document.", "markerName");
        }

        return marker.Text + ", page " + GetPageLabel(GetPageIndex(marker.Page));
    }

    /// <summary>
    /// Adds a figure marker for generated lists of figures.
    /// </summary>
    /// <param name="title">The non-empty figure title.</param>
    /// <param name="page">The target page owned by this document.</param>
    /// <param name="x">The horizontal target coordinate.</param>
    /// <param name="y">The vertical target coordinate.</param>
    /// <returns>The registered publishing marker.</returns>
    public PdfPublishingMarker AddFigureMarker(string title, PdfPage page, double x, double y)
    {
        return CreatePublishingMarker(PdfPublishingMarkerKind.Figure, title, page, x, y);
    }

    /// <summary>
    /// Adds a table marker for generated lists of tables.
    /// </summary>
    /// <param name="title">The non-empty table title.</param>
    /// <param name="page">The target page owned by this document.</param>
    /// <param name="x">The horizontal target coordinate.</param>
    /// <param name="y">The vertical target coordinate.</param>
    /// <returns>The registered publishing marker.</returns>
    public PdfPublishingMarker AddTableMarker(string title, PdfPage page, double x, double y)
    {
        return CreatePublishingMarker(PdfPublishingMarkerKind.Table, title, page, x, y);
    }

    /// <summary>Returns the number of registered figure and table markers.</summary>
    /// <returns>The publishing marker count.</returns>
    public int GetPublishingMarkerCount()
    {
        return _publishingMarkers.Count;
    }

    /// <summary>Returns a registered figure or table marker.</summary>
    /// <param name="index">The zero-based marker position.</param>
    /// <returns>The requested publishing marker.</returns>
    public PdfPublishingMarker GetPublishingMarker(int index)
    {
        if (index < 0 || index >= _publishingMarkers.Count)
        {
            throw new ArgumentOutOfRangeException("index", "Publishing marker position is outside the registered range.");
        }

        return _publishingMarkers[index];
    }

    /// <summary>
    /// Adds a page-label range beginning at the requested existing page.
    /// </summary>
    /// <param name="pageIndex">The zero-based first page position.</param>
    /// <param name="style">The numbering style.</param>
    /// <param name="prefix">The text placed before the generated number.</param>
    /// <param name="startNumber">The positive first generated number.</param>
    /// <returns>The registered page-label range.</returns>
    public PdfPageLabelRange AddPageLabelRange(int pageIndex, PdfPageLabelStyle style, string prefix, int startNumber)
    {
        if (pageIndex < 0 || pageIndex >= _pages.Count)
        {
            throw new ArgumentOutOfRangeException("pageIndex", "Page-label range must begin on an existing page.");
        }

        ValidatePageLabelStyle(style);
        if (prefix == null)
        {
            throw new ArgumentNullException("prefix");
        }

        if (startNumber <= 0)
        {
            throw new ArgumentOutOfRangeException("startNumber", "Page-label start number must be positive.");
        }

        if (style == PdfPageLabelStyle.None && prefix.Length == 0)
        {
            throw new ArgumentException("A prefix-only page-label range requires a non-empty prefix.", "prefix");
        }

        PdfPage page = _pages[pageIndex];
        foreach (PdfPageLabelRange existingRange in _pageLabelRanges)
        {
            if (existingRange.Page == page)
            {
                throw new ArgumentException("A page-label range already begins on this page.", "pageIndex");
            }
        }

        PdfPageLabelRange range = new PdfPageLabelRange(page, style, prefix, startNumber);
        _pageLabelRanges.Add(range);
        return range;
    }

    /// <summary>Returns the number of configured page-label ranges.</summary>
    /// <returns>The page-label range count.</returns>
    public int GetPageLabelRangeCount()
    {
        return _pageLabelRanges.Count;
    }

    /// <summary>Returns a configured page-label range.</summary>
    /// <param name="index">The zero-based range position.</param>
    /// <returns>The requested page-label range.</returns>
    public PdfPageLabelRange GetPageLabelRange(int index)
    {
        if (index < 0 || index >= _pageLabelRanges.Count)
        {
            throw new ArgumentOutOfRangeException("index", "Page-label range position is outside the configured range.");
        }

        return _pageLabelRanges[index];
    }

    /// <summary>Returns the visible label for the requested document page.</summary>
    /// <param name="pageIndex">The zero-based page position.</param>
    /// <returns>The configured label, or ordinary decimal numbering when no range applies.</returns>
    public string GetPageLabel(int pageIndex)
    {
        if (pageIndex < 0 || pageIndex >= _pages.Count)
        {
            throw new ArgumentOutOfRangeException("pageIndex", "Page-label position must identify an existing page.");
        }

        PdfPageLabelRange? selectedRange = null;
        int selectedIndex = -1;
        foreach (PdfPageLabelRange range in _pageLabelRanges)
        {
            int rangeIndex = GetPageIndex(range.Page);
            if (rangeIndex <= pageIndex && rangeIndex > selectedIndex)
            {
                selectedRange = range;
                selectedIndex = rangeIndex;
            }
        }

        if (selectedRange == null)
        {
            return (pageIndex + 1).ToString(CultureInfo.InvariantCulture);
        }

        if (selectedRange.Style == PdfPageLabelStyle.None)
        {
            return selectedRange.Prefix;
        }

        int value = selectedRange.StartNumber + pageIndex - selectedIndex;
        return selectedRange.Prefix + FormatPageLabelNumber(value, selectedRange.Style);
    }

    /// <summary>
    /// Creates a named navigation target tied to an accessibility structure node.
    /// </summary>
    /// <param name="name">The unique non-empty target name.</param>
    /// <param name="structureNode">The registered accessibility structure node.</param>
    /// <param name="x">The horizontal target coordinate.</param>
    /// <param name="y">The vertical target coordinate.</param>
    /// <returns>The structure-aware link target.</returns>
    public PdfStructureLinkTarget AddStructureLinkTarget(string name, PdfAccessibilityNode structureNode, double x, double y)
    {
        ValidateRequiredText(name, "name", "Structure link target name cannot be empty.");
        if (structureNode == null)
        {
            throw new ArgumentNullException("structureNode");
        }

        if (!ContainsAccessibilityNode(structureNode))
        {
            throw new ArgumentException("Structure link target node must belong to the document.", "structureNode");
        }

        foreach (PdfStructureLinkTarget existingTarget in _structureLinkTargets)
        {
            if (existingTarget.Name == name)
            {
                throw new ArgumentException("A structure link target with the same name already exists.", "name");
            }
        }

        string destinationName = CreateNavigationDestinationName("structure");
        PdfNamedDestination destination = AddNamedDestination(destinationName, structureNode.Page, x, y);
        destination.SetStructureTarget(structureNode);
        PdfStructureLinkTarget target = new PdfStructureLinkTarget(name, structureNode, destination);
        _structureLinkTargets.Add(target);
        return target;
    }

    /// <summary>Returns the number of registered structure-aware link targets.</summary>
    /// <returns>The structure link target count.</returns>
    public int GetStructureLinkTargetCount()
    {
        return _structureLinkTargets.Count;
    }

    /// <summary>Returns a registered structure-aware link target.</summary>
    /// <param name="index">The zero-based target position.</param>
    /// <returns>The requested structure link target.</returns>
    public PdfStructureLinkTarget GetStructureLinkTarget(int index)
    {
        if (index < 0 || index >= _structureLinkTargets.Count)
        {
            throw new ArgumentOutOfRangeException("index", "Structure link target position is outside the registered range.");
        }

        return _structureLinkTargets[index];
    }

    /// <summary>
    /// Sets the metadata used for subsequent PDF generation.
    /// </summary>
    /// <param name="metadata">The metadata to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when metadata is null.</exception>
    public void SetMetadata(PdfMetadata metadata)
    {
        if (metadata == null)
        {
            throw new ArgumentNullException("metadata");
        }

        Metadata = metadata;
    }

    /// <summary>
    /// Checks the document for known conditions that affect later PDF/A conversion.
    /// </summary>
    /// <returns>A result containing PDF/A preparation errors and warnings.</returns>
    public PdfAPreparationResult CheckPdfAPreparation()
    {
        return new PdfAPreparationChecker().Check(this);
    }

    /// <summary>
    /// Prepares the document for level-B PDF/A output and enables archival validation during every save.
    /// </summary>
    /// <param name="options">The conformance profile and ICC output-intent data to copy.</param>
    public void PrepareForPdfA(PdfAPreparationOptions options)
    {
        PdfAConfiguration.Prepare(this, options);
    }

    /// <summary>
    /// Determines whether archival PDF/A output has been enabled.
    /// </summary>
    /// <returns>True when the document is prepared for PDF/A output.</returns>
    public bool IsPreparedForPdfA()
    {
        return HasPdfA;
    }

    /// <summary>
    /// Returns the active PDF/A conformance profile.
    /// </summary>
    /// <returns>The configured archival profile.</returns>
    /// <exception cref="InvalidOperationException">Thrown when PDF/A preparation has not been enabled.</exception>
    public PdfAConformance GetPdfAConformance()
    {
        if (!HasPdfA)
        {
            throw new InvalidOperationException("The document has not been prepared for PDF/A output.");
        }

        return PdfAOptions.Conformance;
    }

    /// <summary>
    /// Sets the language used for subsequent PDF generation.
    /// </summary>
    /// <param name="language">The language to use for this operation.</param>
    public void SetLanguage(string language)
    {
        ValidateLanguage(language);
        Language = language;
    }

    /// <summary>
    /// Sets the PDF viewer-window and full-screen fallback preferences.
    /// </summary>
    /// <param name="preferences">The viewer preferences to copy into the document.</param>
    /// <exception cref="ArgumentNullException">Thrown when preferences is null.</exception>
    /// <exception cref="ArgumentException">Thrown when an enum value is unsupported.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a preference requires a later PDF version.</exception>
    public void SetViewerPreferences(PdfViewerPreferences preferences)
    {
        if (preferences == null)
        {
            throw new ArgumentNullException("preferences");
        }

        ValidateNonFullScreenPageMode(preferences.NonFullScreenPageMode);
        ValidateReadingDirection(preferences.ReadingDirection);
        ValidatePageModeVersion(preferences.NonFullScreenPageMode, WriterOptions.Version);
        ViewerPreferences = preferences.Copy();
        HasViewerPreferences = true;
    }

    /// <summary>
    /// Controls whether a viewer title bar should display document-title metadata instead of the file name.
    /// </summary>
    /// <param name="displayDocumentTitle">True to request the document metadata title; false to request the file name.</param>
    public void SetDisplayDocumentTitle(bool displayDocumentTitle)
    {
        PdfViewerPreferences copied = ViewerPreferences.Copy();
        copied.DisplayDocumentTitle = displayDocumentTitle;
        ViewerPreferences = copied;
        HasViewerPreferences = true;
    }

    /// <summary>
    /// Selects the panel or presentation mode used when the document opens.
    /// </summary>
    /// <param name="mode">The initial page mode.</param>
    /// <exception cref="ArgumentException">Thrown when mode is unsupported.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the mode requires a later PDF version.</exception>
    public void SetInitialPageMode(PdfPageMode mode)
    {
        ValidatePageMode(mode);
        ValidatePageModeVersion(mode, WriterOptions.Version);
        InitialPageMode = mode;
        HasInitialPageMode = true;
    }

    /// <summary>
    /// Selects the page arrangement used when the document opens.
    /// </summary>
    /// <param name="layout">The initial page layout.</param>
    /// <exception cref="ArgumentException">Thrown when layout is unsupported.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the layout requires a later PDF version.</exception>
    public void SetInitialPageLayout(PdfPageLayout layout)
    {
        ValidatePageLayout(layout);
        ValidatePageLayoutVersion(layout, WriterOptions.Version);
        InitialPageLayout = layout;
        HasInitialPageLayout = true;
    }

    /// <summary>
    /// Adds an accessibility node to this document and returns the stored value.
    /// </summary>
    /// <param name="page">The PDF page that receives or owns the content.</param>
    /// <param name="role">The role to use for this operation.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    public PdfAccessibilityNode AddAccessibilityNode(PdfPage page, string role)
    {
        return AddAccessibilityNodeWithDescription(page, role, string.Empty);
    }

    /// <summary>
    /// Adds an accessibility node to this document and returns the stored value.
    /// </summary>
    /// <param name="page">The PDF page that receives or owns the content.</param>
    /// <param name="role">The role to use for this operation.</param>
    /// <param name="description">The description to use for this operation.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    public PdfAccessibilityNode AddAccessibilityNodeWithDescription(PdfPage page, string role, string description)
    {
        ValidateDocumentPage(page, "page", "Accessibility node page must belong to the document.");
        if (HasTaggedPdf)
        {
            ValidateStructureRole(role);
        }

        PdfAccessibilityNode node = new PdfAccessibilityNode(page, role, description);
        node.OwnerDocument = this;
        _accessibilityNodes.Add(node);
        _structureRoots.Add(node);
        return node;
    }

    /// <summary>
    /// Enables logical structure-tree and parent-tree output for this document.
    /// </summary>
    public void EnableTaggedPdf()
    {
        foreach (PdfAccessibilityNode node in _accessibilityNodes)
        {
            ValidateStructureRole(node.Role);
        }

        HasTaggedPdf = true;
    }

    /// <summary>
    /// Determines whether tagged-PDF serialization is enabled.
    /// </summary>
    /// <returns>True when structure output is enabled.</returns>
    public bool IsTaggedPdfEnabled()
    {
        return HasTaggedPdf;
    }

    /// <summary>
    /// Adds a child structure element on the same page as its parent.
    /// </summary>
    /// <param name="parent">The registered parent structure element.</param>
    /// <param name="role">The standard PDF structure role.</param>
    /// <param name="description">The optional developer-readable description.</param>
    /// <returns>The registered child in reading order.</returns>
    public PdfAccessibilityNode AddStructureChild(PdfAccessibilityNode parent, string role, string description)
    {
        ValidateStructureNode(parent, "parent");
        return AddStructureChildOnPage(parent, parent.Page, role, description);
    }

    /// <summary>
    /// Adds a child structure element on a requested document page.
    /// </summary>
    /// <param name="parent">The registered parent structure element.</param>
    /// <param name="page">The page containing the child's content.</param>
    /// <param name="role">The standard PDF structure role.</param>
    /// <param name="description">The optional developer-readable description.</param>
    /// <returns>The registered child in reading order.</returns>
    public PdfAccessibilityNode AddStructureChildOnPage(PdfAccessibilityNode parent, PdfPage page, string role, string description)
    {
        ValidateStructureNode(parent, "parent");
        ValidateDocumentPage(page, "page", "Structure child page must belong to the document.");
        ValidateStructureRole(role);
        if (description == null)
        {
            throw new ArgumentNullException("description");
        }

        PdfAccessibilityNode child = new PdfAccessibilityNode(page, role, description);
        child.Parent = parent;
        child.OwnerDocument = this;
        parent._children.Add(child);
        _accessibilityNodes.Add(child);
        return child;
    }

    /// <summary>
    /// Sets alternate text on a registered structure element.
    /// </summary>
    /// <param name="node">The structure element.</param>
    /// <param name="alternateText">The non-empty alternate description.</param>
    public void SetStructureAlternateText(PdfAccessibilityNode node, string alternateText)
    {
        ValidateStructureNode(node, "node");
        ValidateRequiredText(alternateText, "alternateText", "Structure alternate text cannot be empty.");
        node.AlternateText = alternateText;
    }

    /// <summary>
    /// Sets replacement text on a registered structure element.
    /// </summary>
    /// <param name="node">The structure element.</param>
    /// <param name="actualText">The non-empty replacement text.</param>
    public void SetStructureActualText(PdfAccessibilityNode node, string actualText)
    {
        ValidateStructureNode(node, "node");
        ValidateRequiredText(actualText, "actualText", "Structure actual text cannot be empty.");
        node.ActualText = actualText;
    }

    /// <summary>
    /// Sets a language override on a registered structure element.
    /// </summary>
    /// <param name="node">The structure element.</param>
    /// <param name="language">The language tag.</param>
    public void SetStructureLanguage(PdfAccessibilityNode node, string language)
    {
        ValidateStructureNode(node, "node");
        ValidateLanguage(language);
        node.Language = language;
    }

    /// <summary>
    /// Sets the row or column scope of a tagged table-header element.
    /// </summary>
    /// <param name="node">A registered <c>TH</c> structure element.</param>
    /// <param name="scope">The table-header scope.</param>
    public void SetTableHeaderScope(PdfAccessibilityNode node, PdfTableHeaderScope scope)
    {
        ValidateStructureNode(node, "node");
        if (node.Role != "TH")
        {
            throw new InvalidOperationException("Table-header scope can only be assigned to a TH structure element.");
        }

        ValidateTableHeaderScope(scope);
        node.HasTableHeaderScope = true;
        node.TableHeaderScope = scope;
    }

    /// <summary>
    /// Moves a structure element to a zero-based position among its siblings.
    /// </summary>
    /// <param name="node">The registered structure element.</param>
    /// <param name="position">The zero-based reading-order position.</param>
    public void SetStructureReadingOrder(PdfAccessibilityNode node, int position)
    {
        ValidateStructureNode(node, "node");
        List<PdfAccessibilityNode> siblings = _structureRoots;
        if (node.Parent != null)
        {
            siblings = node.Parent._children;
        }

        if (position < 0 || position >= siblings.Count)
        {
            throw new ArgumentOutOfRangeException("position", "Reading-order position must be within the sibling collection.");
        }

        siblings.Remove(node);
        siblings.Insert(position, node);
    }

    /// <summary>Returns the number of root structure elements.</summary>
    /// <returns>The root count.</returns>
    public int GetStructureRootCount()
    {
        return _structureRoots.Count;
    }

    /// <summary>Returns a root structure element.</summary>
    /// <param name="index">The zero-based root position.</param>
    /// <returns>The root element.</returns>
    public PdfAccessibilityNode GetStructureRoot(int index)
    {
        if (index < 0 || index >= _structureRoots.Count)
        {
            throw new ArgumentOutOfRangeException("index", "Structure root position is outside the registered range.");
        }

        return _structureRoots[index];
    }

    /// <summary>Returns the number of child structure elements.</summary>
    /// <param name="node">The registered structure element.</param>
    /// <returns>The child count.</returns>
    public int GetStructureChildCount(PdfAccessibilityNode node)
    {
        ValidateStructureNode(node, "node");
        return node._children.Count;
    }

    /// <summary>Returns a child structure element.</summary>
    /// <param name="node">The registered parent structure element.</param>
    /// <param name="index">The zero-based child position.</param>
    /// <returns>The child element.</returns>
    public PdfAccessibilityNode GetStructureChild(PdfAccessibilityNode node, int index)
    {
        ValidateStructureNode(node, "node");
        if (index < 0 || index >= node._children.Count)
        {
            throw new ArgumentOutOfRangeException("index", "Structure child position is outside the registered range.");
        }

        return node._children[index];
    }

    /// <summary>Associates an internal-link annotation with a structure element.</summary>
    /// <param name="node">The structure element.</param>
    /// <param name="link">The document-owned internal link.</param>
    public void TagInternalLink(PdfAccessibilityNode node, PdfInternalLink link)
    {
        ValidateStructureNode(node, "node");
        ValidateInternalLinkForTag(node, link);
        link.TagNode = node;
    }

    /// <summary>Associates an external-link annotation with a structure element.</summary>
    /// <param name="node">The structure element.</param>
    /// <param name="link">The document-owned external link.</param>
    public void TagExternalLink(PdfAccessibilityNode node, PdfExternalLink link)
    {
        ValidateStructureNode(node, "node");
        ValidateExternalLinkForTag(node, link);
        link.TagNode = node;
    }

    /// <summary>Associates a general annotation with a structure element.</summary>
    /// <param name="node">The structure element.</param>
    /// <param name="annotation">The document-owned annotation.</param>
    public void TagAnnotation(PdfAccessibilityNode node, PdfAnnotation annotation)
    {
        ValidateStructureNode(node, "node");
        ValidateAnnotationForTag(node, annotation);
        annotation.TagNode = node;
    }

    /// <summary>Associates a form widget with a structure element.</summary>
    /// <param name="node">The structure element.</param>
    /// <param name="field">The document-owned form field or radio option.</param>
    public void TagFormField(PdfAccessibilityNode node, PdfAcroFormField field)
    {
        ValidateStructureNode(node, "node");
        ValidateFormFieldForTag(node, field);
        field.TagNode = node;
    }

    /// <summary>
    /// Checks the current document for accessibility preparation and returns the findings.
    /// </summary>
    /// <returns>The resulting accessibility preparation result.</returns>
    public PdfAccessibilityPreparationResult CheckAccessibilityPreparation()
    {
        return new PdfAccessibilityPreparationChecker().Check(this);
    }

    /// <summary>Validates the syntax and object structure emitted by this document.</summary>
    /// <returns>The ordered validation findings.</returns>
    public PdfValidationResult ValidatePdfStructure()
    {
        return new PdfStructureValidator().ValidateDocument(this);
    }

    /// <summary>Validates PDF/A preparation and emitted archival structures.</summary>
    /// <returns>The ordered validation findings.</returns>
    public PdfValidationResult ValidatePdfAConformance()
    {
        return new PdfConformanceValidator().ValidatePdfA(this);
    }

    /// <summary>Validates PDF/UA preparation and emitted tagged structures.</summary>
    /// <returns>The ordered validation findings.</returns>
    public PdfValidationResult ValidatePdfUaPreparation()
    {
        return new PdfConformanceValidator().ValidatePdfUa(this);
    }

    /// <summary>
    /// Adds an attachment to this document and returns the stored value.
    /// </summary>
    /// <param name="fileName">The file name to use for this operation.</param>
    /// <param name="data">The binary data to copy into the PDF resource.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    public PdfAttachment AddAttachment(string fileName, byte[] data)
    {
        return AddAttachmentWithMetadata(fileName, data, string.Empty, string.Empty);
    }

    /// <summary>
    /// Adds an attachment to this document and returns the stored value.
    /// </summary>
    /// <param name="fileName">The file name to use for this operation.</param>
    /// <param name="data">The binary data to copy into the PDF resource.</param>
    /// <param name="mimeType">The mime type to use for this operation.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    public PdfAttachment AddAttachmentWithMimeType(string fileName, byte[] data, string mimeType)
    {
        return AddAttachmentWithMetadata(fileName, data, mimeType, string.Empty);
    }

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
    public PdfAttachment AddAttachmentWithMetadata(string fileName, byte[] data, string mimeType, string description)
    {
        ValidateAttachment(fileName, mimeType, description);

        if (data == null)
        {
            throw new ArgumentNullException("data");
        }

        if (HasAttachment(fileName))
        {
            throw new ArgumentException("An attachment with the same file name already exists.", "fileName");
        }

        PdfAttachment attachment = new PdfAttachment(fileName, data, mimeType, description);
        PdfAConfiguration.ConfigureNewAttachment(this, attachment);
        _attachments.Add(attachment);
        return attachment;
    }

    /// <summary>
    /// Adds an embedded file with an explicit semantic relationship to the document.
    /// </summary>
    /// <param name="fileName">The non-empty embedded file name.</param>
    /// <param name="data">The binary data copied into the embedded-file stream.</param>
    /// <param name="mimeType">The non-empty MIME type written as the embedded-file subtype.</param>
    /// <param name="description">The optional human-readable file description.</param>
    /// <param name="relationship">The semantic relationship to write on the file specification.</param>
    /// <returns>The associated attachment.</returns>
    public PdfAttachment AddAssociatedFile(string fileName, byte[] data, string mimeType, string description, PdfAssociatedFileRelationship relationship)
    {
        PdfAConfiguration.ValidateRelationship(relationship);
        if (mimeType == null)
        {
            throw new ArgumentNullException("mimeType");
        }

        if (mimeType.Length == 0)
        {
            throw new ArgumentException("Associated files require a MIME type.", "mimeType");
        }

        PdfAttachment attachment = AddAttachmentWithMetadata(fileName, data, mimeType, description);
        attachment.IsAssociatedFile = true;
        attachment.Relationship = relationship;
        return attachment;
    }

    /// <summary>
    /// Enables the document's interactive form dictionary and returns it for field configuration.
    /// </summary>
    /// <returns>The resulting acro form.</returns>
    public PdfAcroForm UseAcroForm()
    {
        AcroForm.SetOwnerDocument(this);
        HasAcroForm = true;
        return AcroForm;
    }

    /// <summary>
    /// Sets the acro form used for subsequent PDF generation.
    /// </summary>
    /// <param name="acroForm">The acro form to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void SetAcroForm(PdfAcroForm acroForm)
    {
        if (acroForm == null)
        {
            throw new ArgumentNullException("acroForm");
        }

        acroForm.SetOwnerDocument(this);
        AcroForm = acroForm;
        HasAcroForm = true;
    }

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
    public PdfAcroFormField AddTextField(string name, PdfPage page, double x, double y, double width, double height)
    {
        ValidateFormField(name, page, x, y, width, height);

        PdfAcroForm acroForm = UseAcroForm();
        acroForm.SetNeedAppearances(false);
        PdfAcroFormField field = new PdfAcroFormField("Tx", name, page, new PdfRectangle(x, y, width, height));
        acroForm.AddField(field);
        return field;
    }

    /// <summary>
    /// Adds a text field to this document and returns the stored value.
    /// </summary>
    /// <param name="name">The name used to identify the value in the document.</param>
    /// <param name="page">The PDF page that receives or owns the content.</param>
    /// <param name="rectangle">The rectangle to use for this operation.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public PdfAcroFormField AddTextFieldInRectangle(string name, PdfPage page, PdfRectangle rectangle)
    {
        if (rectangle == null)
        {
            throw new ArgumentNullException("rectangle");
        }

        return AddTextField(name, page, rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
    }

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
    public PdfAcroFormField AddCheckbox(string name, PdfPage page, double x, double y, double width, double height)
    {
        ValidateFormField(name, page, x, y, width, height);

        PdfAcroForm acroForm = UseAcroForm();
        acroForm.SetNeedAppearances(false);
        PdfAcroFormField field = new PdfAcroFormField("Btn", name, page, new PdfRectangle(x, y, width, height));
        acroForm.AddField(field);
        return field;
    }

    /// <summary>
    /// Adds a checkbox to this document and returns the stored value.
    /// </summary>
    /// <param name="name">The name used to identify the value in the document.</param>
    /// <param name="page">The PDF page that receives or owns the content.</param>
    /// <param name="rectangle">The rectangle to use for this operation.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public PdfAcroFormField AddCheckboxInRectangle(string name, PdfPage page, PdfRectangle rectangle)
    {
        if (rectangle == null)
        {
            throw new ArgumentNullException("rectangle");
        }

        return AddCheckbox(name, page, rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
    }

    /// <summary>
    /// Adds an empty mutually exclusive radio-button group to this document.
    /// </summary>
    /// <param name="name">The unique terminal field name.</param>
    /// <returns>The registered radio-button group.</returns>
    public PdfRadioButtonGroup AddRadioButtonGroup(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        PdfAcroFormField.ValidateFieldName(name);
        if (AcroForm.HasField(name))
        {
            throw new ArgumentException("A form field with the same name already exists.", "name");
        }

        PdfRadioButtonGroup group = new PdfRadioButtonGroup(name);
        PdfAcroForm acroForm = UseAcroForm();
        acroForm.SetNeedAppearances(false);
        acroForm.AddRadioButtonGroup(group);
        return group;
    }

    /// <summary>
    /// Draws current interactive form values into page content and removes the interactive fields.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the object is not in a state that permits this operation.</exception>
    public void FlattenForm()
    {
        if (!HasAcroForm)
        {
            return;
        }

        foreach (PdfAcroFormField field in AcroForm.Fields)
        {
            if (!ContainsPage(field.Page))
            {
                throw new InvalidOperationException("Form field page does not belong to the document.");
            }

            FlattenFormField(field);
        }

        foreach (PdfRadioButtonGroup group in AcroForm.RadioButtonGroups)
        {
            foreach (PdfAcroFormField option in group.Options)
            {
                if (!ContainsPage(option.Page))
                {
                    throw new InvalidOperationException("Radio-button option page does not belong to the document.");
                }

                FlattenRadioButtonOption(option);
            }
        }

        AcroForm.ClearFields();
        AcroForm.SetNeedAppearances(false);
        HasAcroForm = false;
    }

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
    public PdfAcroFormField AddSignaturePlaceholder(string name, PdfPage page, double x, double y, double width, double height)
    {
        return AddSignaturePlaceholderWithLength(name, page, x, y, width, height, 8192);
    }

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
    public PdfAcroFormField AddSignaturePlaceholderWithLength(string name, PdfPage page, double x, double y, double width, double height, int contentsLength)
    {
        ValidateFormField(name, page, x, y, width, height);
        if (contentsLength <= 0 || contentsLength > int.MaxValue / 2)
        {
            throw new ArgumentOutOfRangeException("contentsLength", "Signature contents length must be positive and small enough to encode as hexadecimal text.");
        }

        PdfAcroForm acroForm = UseAcroForm();
        PdfAcroFormField field = new PdfAcroFormField("Sig", name, page, new PdfRectangle(x, y, width, height));
        field.ConfigureSignaturePlaceholder(contentsLength);
        acroForm.AddField(field);
        return field;
    }

    /// <summary>
    /// Adds a signature placeholder to this document and returns the stored value.
    /// </summary>
    /// <param name="name">The name used to identify the value in the document.</param>
    /// <param name="page">The PDF page that receives or owns the content.</param>
    /// <param name="rectangle">The rectangle to use for this operation.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public PdfAcroFormField AddSignaturePlaceholderInRectangle(string name, PdfPage page, PdfRectangle rectangle)
    {
        if (rectangle == null)
        {
            throw new ArgumentNullException("rectangle");
        }

        return AddSignaturePlaceholder(name, page, rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
    }

    /// <summary>
    /// Selects the PDF specification version used for subsequent document output.
    /// </summary>
    /// <param name="version">The supported PDF version to write.</param>
    /// <exception cref="ArgumentException">Thrown when the version is not supported.</exception>
    /// <exception cref="InvalidOperationException">Thrown when an enabled feature cannot be represented by the selected version.</exception>
    public void SetPdfVersion(PdfVersion version)
    {
        ValidatePdfVersion(version);
        ValidatePdfAVersionCompatibility(version);
        if (version == PdfVersion.Pdf20 && HasEncryption)
        {
            throw new InvalidOperationException("The available standard encryption handler cannot be used with PDF 2.0.");
        }

        ValidateViewSettingsForVersion(version);

        WriterOptions.Version = version;
    }

    /// <summary>
    /// Enables and configures deterministic writer mode for subsequent document output.
    /// </summary>
    public void UseDeterministicWriterMode()
    {
        WriterOptions.Deterministic = true;
    }

    /// <summary>
    /// Sets the writer options used for subsequent document output.
    /// </summary>
    /// <param name="options">The writer options to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void SetWriterOptions(PdfWriterOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        ValidatePdfVersion(options.Version);
        ValidatePdfAVersionCompatibility(options.Version);
        if (options.Version == PdfVersion.Pdf20 && HasEncryption)
        {
            throw new InvalidOperationException("The available standard encryption handler cannot be used with PDF 2.0.");
        }

        ValidateViewSettingsForVersion(options.Version);
        ValidateWriterDocumentId(options);

        WriterOptions = options;
    }

    /// <summary>
    /// Enables and configures deterministic writer mode for subsequent document output.
    /// </summary>
    /// <param name="documentId">The document id to use for this operation.</param>
    /// <exception cref="ArgumentException">Thrown when an argument is invalid for this operation.</exception>
    public void UseDeterministicWriterModeWithId(string documentId)
    {
        if (!IsValidDocumentId(documentId))
        {
            throw new ArgumentException("Deterministic document ID must contain exactly 32 hexadecimal characters.", "documentId");
        }

        WriterOptions.Deterministic = true;
        WriterOptions.DeterministicDocumentId = documentId.ToUpperInvariant();
    }

    /// <summary>
    /// Enables and configures compressed content streams for subsequent document output.
    /// </summary>
    public void UseCompressedContentStreams()
    {
        WriterOptions.CompressContentStreams = true;
    }

    /// <summary>
    /// Enables and configures standard encryption for subsequent document output.
    /// </summary>
    /// <param name="userPassword">The user password to use for this operation.</param>
    /// <param name="ownerPassword">The owner password to use for this operation.</param>
    public void UseStandardEncryption(string userPassword, string ownerPassword)
    {
        UseStandardEncryptionWithPermissions(userPassword, ownerPassword, new PdfPermissions());
    }

    /// <summary>
    /// Enables and configures standard encryption for subsequent document output.
    /// </summary>
    /// <param name="userPassword">The user password to use for this operation.</param>
    /// <param name="ownerPassword">The owner password to use for this operation.</param>
    /// <param name="permissions">The permissions to use for this operation.</param>
    public void UseStandardEncryptionWithPermissions(string userPassword, string ownerPassword, PdfPermissions permissions)
    {
        ValidateEncryptionVersionCompatibility();
        EncryptionOptions = new PdfEncryptionOptions(userPassword, ownerPassword, permissions);
        HasEncryption = true;
    }

    /// <summary>
    /// Enables and configures standard encryption with the supplied options.
    /// </summary>
    /// <param name="options">The encryption options to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void UseStandardEncryptionWithOptions(PdfEncryptionOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        ValidateEncryptionVersionCompatibility();
        options.Validate();

        EncryptionOptions = options;
        HasEncryption = true;
    }

    /// <summary>
    /// Serializes the current PDF document to the requested destination.
    /// </summary>
    /// <param name="path">The vector path whose stored segments will be used.</param>
    public void Save(string path)
    {
        EnsureCanSave();
        if (path == null)
        {
            throw new ArgumentNullException("path");
        }

        if (path.Length == 0)
        {
            throw new ArgumentException("Output path cannot be empty.", "path");
        }

        string? directory = Path.GetDirectoryName(path);
        if (directory == null || directory.Length == 0)
        {
            directory = ".";
        }

        Directory.CreateDirectory(directory);
        PdfWriter.Write(path, this);
    }

    /// <summary>
    /// Serializes the current PDF document to the requested destination.
    /// </summary>
    /// <param name="stream">The writable stream that receives the PDF bytes; the method leaves ownership with the caller.</param>
    public void SaveToStream(Stream stream)
    {
        EnsureCanSave();
        if (stream == null)
        {
            throw new ArgumentNullException("stream");
        }

        if (!stream.CanWrite)
        {
            throw new ArgumentException("Output stream must be writable.", "stream");
        }

        PdfWriter.Write(stream, this);
    }

    /// <summary>
    /// Serializes the current document and returns the complete PDF file as bytes.
    /// </summary>
    /// <returns>A new byte array containing the generated data.</returns>
    public byte[] ToBytes()
    {
        EnsureCanSave();

        using (MemoryStream output = new MemoryStream())
        {
            PdfWriter.Write(output, this);
            return output.ToArray();
        }
    }

    internal void EnsureCanSave()
    {
        if (_pages.Count == 0)
        {
            throw new InvalidOperationException("A PDF document must contain at least one page.");
        }

        if (HasEncryption)
        {
            EncryptionOptions.Validate();
        }

        ValidateCanvasStates();
        PdfAConfiguration.ValidatePreparedDocument(this);
        PdfTaggedDocumentConfiguration.Validate(this);
        ValidateVersionCompatibility();
    }

    internal void ValidateCanvasStates()
    {
        foreach (PdfPage page in _pages)
        {
            page.Canvas.ValidateCompleteState();
        }

        foreach (PdfFormXObject form in _forms)
        {
            form.Canvas.ValidateCompleteState();
        }
    }

    internal void ValidateVersionCompatibility()
    {
        ValidatePdfVersion(WriterOptions.Version);
        ValidateWriterDocumentId(WriterOptions);
        if (WriterOptions.Version == PdfVersion.Pdf20 && HasEncryption)
        {
            throw new InvalidOperationException("The available standard encryption handler cannot be used with PDF 2.0.");
        }

        ValidateViewSettingsForVersion(WriterOptions.Version);
    }

    internal void ValidateWriterDocumentId(PdfWriterOptions options)
    {
        if (options.Deterministic && !IsValidDocumentId(options.DeterministicDocumentId))
        {
            throw new ArgumentException("Deterministic document ID must contain exactly 32 hexadecimal characters.", "options");
        }
    }

    internal void ValidateViewSettingsForVersion(PdfVersion version)
    {
        if (HasInitialPageMode)
        {
            ValidatePageModeVersion(InitialPageMode, version);
        }

        if (HasInitialPageLayout)
        {
            ValidatePageLayoutVersion(InitialPageLayout, version);
        }

        if (HasViewerPreferences)
        {
            ValidateNonFullScreenPageMode(ViewerPreferences.NonFullScreenPageMode);
            ValidateReadingDirection(ViewerPreferences.ReadingDirection);
            ValidatePageModeVersion(ViewerPreferences.NonFullScreenPageMode, version);
        }
    }

    internal static void ValidatePageMode(PdfPageMode mode)
    {
        if (mode == PdfPageMode.UseNone || mode == PdfPageMode.UseOutlines || mode == PdfPageMode.UseThumbnails || mode == PdfPageMode.FullScreen || mode == PdfPageMode.UseOptionalContent || mode == PdfPageMode.UseAttachments)
        {
            return;
        }

        throw new ArgumentException("Page mode is not supported.", "mode");
    }

    internal static void ValidateNonFullScreenPageMode(PdfPageMode mode)
    {
        if (mode == PdfPageMode.UseNone || mode == PdfPageMode.UseOutlines || mode == PdfPageMode.UseThumbnails || mode == PdfPageMode.UseOptionalContent)
        {
            return;
        }

        throw new ArgumentException("Non-full-screen page mode is not supported.", "NonFullScreenPageMode");
    }

    internal static void ValidatePageLayout(PdfPageLayout layout)
    {
        if (layout == PdfPageLayout.SinglePage || layout == PdfPageLayout.OneColumn || layout == PdfPageLayout.TwoColumnLeft || layout == PdfPageLayout.TwoColumnRight || layout == PdfPageLayout.TwoPageLeft || layout == PdfPageLayout.TwoPageRight)
        {
            return;
        }

        throw new ArgumentException("Page layout is not supported.", "layout");
    }

    internal static void ValidateReadingDirection(PdfReadingDirection direction)
    {
        if (direction == PdfReadingDirection.LeftToRight || direction == PdfReadingDirection.RightToLeft)
        {
            return;
        }

        throw new ArgumentException("Reading direction is not supported.", "ReadingDirection");
    }

    internal static void ValidatePageModeVersion(PdfPageMode mode, PdfVersion version)
    {
        if (mode == PdfPageMode.UseAttachments && (int)version < (int)PdfVersion.Pdf16)
        {
            throw new InvalidOperationException("The attachments page mode requires PDF 1.6 or later.");
        }

        if (mode == PdfPageMode.UseOptionalContent && (int)version < (int)PdfVersion.Pdf15)
        {
            throw new InvalidOperationException("The optional-content page mode requires PDF 1.5 or later.");
        }
    }

    internal static void ValidatePageLayoutVersion(PdfPageLayout layout, PdfVersion version)
    {
        if ((layout == PdfPageLayout.TwoPageLeft || layout == PdfPageLayout.TwoPageRight) && (int)version < (int)PdfVersion.Pdf15)
        {
            throw new InvalidOperationException("Two-page layouts require PDF 1.5 or later.");
        }
    }

    internal void ValidateEncryptionVersionCompatibility()
    {
        if (HasPdfA)
        {
            throw new InvalidOperationException("PDF/A output cannot be encrypted.");
        }

        ValidatePdfVersion(WriterOptions.Version);
        if (WriterOptions.Version == PdfVersion.Pdf20)
        {
            throw new InvalidOperationException("The available standard encryption handler cannot be used with PDF 2.0.");
        }
    }

    internal void ValidatePdfAVersionCompatibility(PdfVersion version)
    {
        if (!HasPdfA)
        {
            return;
        }

        if (version != PdfAConfiguration.GetRequiredVersion(PdfAOptions.Conformance))
        {
            throw new InvalidOperationException("The requested PDF version does not match the prepared PDF/A profile.");
        }
    }

    internal static void ValidatePdfVersion(PdfVersion version)
    {
        if (version == PdfVersion.Pdf14)
        {
            return;
        }

        if (version == PdfVersion.Pdf15)
        {
            return;
        }

        if (version == PdfVersion.Pdf16)
        {
            return;
        }

        if (version == PdfVersion.Pdf17)
        {
            return;
        }

        if (version == PdfVersion.Pdf20)
        {
            return;
        }

        throw new ArgumentException("PDF version is not supported.", "version");
    }

    internal static bool IsValidDocumentId(string value)
    {
        if (value == null)
        {
            return false;
        }

        if (value.Length != 32)
        {
            return false;
        }

        foreach (char character in value)
        {
            if (!IsHexCharacter(character))
            {
                return false;
            }
        }

        return true;
    }

    internal static bool IsHexCharacter(char character)
    {
        if (character >= '0' && character <= '9')
        {
            return true;
        }

        if (character >= 'a' && character <= 'f')
        {
            return true;
        }

        if (character >= 'A' && character <= 'F')
        {
            return true;
        }

        return false;
    }

    internal bool ContainsPage(PdfPage page)
    {
        foreach (PdfPage existingPage in _pages)
        {
            if (existingPage == page)
            {
                return true;
            }
        }

        return false;
    }

    internal int GetPageIndex(PdfPage page)
    {
        for (int index = 0; index < _pages.Count; index++)
        {
            if (_pages[index] == page)
            {
                return index;
            }
        }

        throw new InvalidOperationException("Navigation target page does not belong to the document.");
    }

    internal PdfIndexMarker CreateIndexMarker(string term, string subterm, PdfPage page, double x, double y)
    {
        ValidateRequiredText(term, "term", "Index term cannot be empty.");
        ValidateDocumentPage(page, "page", "Index marker page must belong to the document.");
        string destinationName = CreateNavigationDestinationName("index");
        AddNamedDestination(destinationName, page, x, y);
        PdfIndexMarker marker = new PdfIndexMarker(term, subterm, page, x, y, destinationName);
        _indexMarkers.Add(marker);
        return marker;
    }

    internal PdfPublishingMarker CreatePublishingMarker(PdfPublishingMarkerKind kind, string title, PdfPage page, double x, double y)
    {
        PdfGeneratedList.ValidateKind(kind);
        ValidateRequiredText(title, "title", "Publishing marker title cannot be empty.");
        ValidateDocumentPage(page, "page", "Publishing marker page must belong to the document.");
        string prefix = "figure";
        if (kind == PdfPublishingMarkerKind.Table)
        {
            prefix = "table";
        }

        string destinationName = CreateNavigationDestinationName(prefix);
        AddNamedDestination(destinationName, page, x, y);
        PdfPublishingMarker marker = new PdfPublishingMarker(kind, title, page, x, y, destinationName);
        _publishingMarkers.Add(marker);
        return marker;
    }

    internal string CreateNavigationDestinationName(string prefix)
    {
        int index = 1;
        while (true)
        {
            string name = prefix + "-" + index.ToString(CultureInfo.InvariantCulture);
            if (!HasNamedDestination(name))
            {
                return name;
            }

            index++;
        }
    }

    internal PdfCrossReferenceMarker? FindCrossReferenceMarker(string name)
    {
        foreach (PdfCrossReferenceMarker marker in _crossReferenceMarkers)
        {
            if (marker.Name == name)
            {
                return marker;
            }
        }

        return null;
    }

    internal bool ContainsAccessibilityNode(PdfAccessibilityNode node)
    {
        foreach (PdfAccessibilityNode existingNode in _accessibilityNodes)
        {
            if (existingNode == node)
            {
                return true;
            }
        }

        return false;
    }

    internal static void ValidateRequiredText(string value, string parameterName, string message)
    {
        if (value == null)
        {
            throw new ArgumentNullException(parameterName);
        }

        if (value.Length == 0)
        {
            throw new ArgumentException(message, parameterName);
        }
    }

    internal void ValidateStructureNode(PdfAccessibilityNode node, string parameterName)
    {
        if (node == null)
        {
            throw new ArgumentNullException(parameterName);
        }

        if (!ContainsAccessibilityNode(node))
        {
            throw new ArgumentException("Structure element must belong to the document.", parameterName);
        }
    }

    internal static void ValidateStructureRole(string role)
    {
        ValidateRequiredText(role, "role", "Structure role cannot be empty.");
        if (IsStandardStructureRole(role))
        {
            return;
        }

        throw new ArgumentException("Structure role is not a standard PDF role.", "role");
    }

    internal static bool IsStandardStructureRole(string role)
    {
        return role == "Document" || role == "Part" || role == "Art" || role == "Sect" || role == "Div" || role == "BlockQuote" || role == "Caption" || role == "TOC" || role == "TOCI" || role == "Index" || role == "NonStruct" || role == "Private" || role == "P" || role == "H" || role == "H1" || role == "H2" || role == "H3" || role == "H4" || role == "H5" || role == "H6" || role == "L" || role == "LI" || role == "Lbl" || role == "LBody" || role == "Table" || role == "TR" || role == "TH" || role == "TD" || role == "THead" || role == "TBody" || role == "TFoot" || role == "Span" || role == "Quote" || role == "Note" || role == "Reference" || role == "BibEntry" || role == "Code" || role == "Link" || role == "Annot" || role == "Ruby" || role == "RB" || role == "RT" || role == "RP" || role == "Warichu" || role == "WT" || role == "WP" || role == "Figure" || role == "Formula" || role == "Form";
    }

    internal static void ValidateTableHeaderScope(PdfTableHeaderScope scope)
    {
        if (scope == PdfTableHeaderScope.Row || scope == PdfTableHeaderScope.Column || scope == PdfTableHeaderScope.Both)
        {
            return;
        }

        throw new ArgumentException("Table-header scope is not supported.", "scope");
    }

    internal void ValidateInternalLinkForTag(PdfAccessibilityNode node, PdfInternalLink link)
    {
        if (link == null)
        {
            throw new ArgumentNullException("link");
        }

        foreach (PdfPage page in Pages)
        {
            foreach (PdfInternalLink existingLink in page.InternalLinks)
            {
                if (existingLink == link)
                {
                    if (page != node.Page)
                    {
                        throw new ArgumentException("Tagged link and structure element must belong to the same page.", "link");
                    }

                    return;
                }
            }
        }

        throw new ArgumentException("Internal link must belong to the document.", "link");
    }

    internal void ValidateExternalLinkForTag(PdfAccessibilityNode node, PdfExternalLink link)
    {
        if (link == null)
        {
            throw new ArgumentNullException("link");
        }

        foreach (PdfPage page in Pages)
        {
            foreach (PdfExternalLink existingLink in page.ExternalLinks)
            {
                if (existingLink == link)
                {
                    if (page != node.Page)
                    {
                        throw new ArgumentException("Tagged link and structure element must belong to the same page.", "link");
                    }

                    return;
                }
            }
        }

        throw new ArgumentException("External link must belong to the document.", "link");
    }

    internal void ValidateAnnotationForTag(PdfAccessibilityNode node, PdfAnnotation annotation)
    {
        if (annotation == null)
        {
            throw new ArgumentNullException("annotation");
        }

        foreach (PdfPage page in Pages)
        {
            foreach (PdfAnnotation existingAnnotation in page.Annotations)
            {
                if (existingAnnotation == annotation)
                {
                    if (page != node.Page)
                    {
                        throw new ArgumentException("Tagged annotation and structure element must belong to the same page.", "annotation");
                    }

                    return;
                }
            }
        }

        throw new ArgumentException("Annotation must belong to the document.", "annotation");
    }

    internal void ValidateFormFieldForTag(PdfAccessibilityNode node, PdfAcroFormField field)
    {
        if (field == null)
        {
            throw new ArgumentNullException("field");
        }

        foreach (PdfAcroFormField existingField in AcroForm.Fields)
        {
            if (existingField == field)
            {
                if (field.Page != node.Page)
                {
                    throw new ArgumentException("Tagged field and structure element must belong to the same page.", "field");
                }

                return;
            }
        }

        foreach (PdfRadioButtonGroup group in AcroForm.RadioButtonGroups)
        {
            foreach (PdfAcroFormField option in group.Options)
            {
                if (option == field)
                {
                    if (field.Page != node.Page)
                    {
                        throw new ArgumentException("Tagged field and structure element must belong to the same page.", "field");
                    }

                    return;
                }
            }
        }

        throw new ArgumentException("Form field must belong to the document.", "field");
    }

    internal static void ValidatePageLabelStyle(PdfPageLabelStyle style)
    {
        if (style == PdfPageLabelStyle.None || style == PdfPageLabelStyle.Decimal || style == PdfPageLabelStyle.UpperRoman || style == PdfPageLabelStyle.LowerRoman || style == PdfPageLabelStyle.UpperLetters || style == PdfPageLabelStyle.LowerLetters)
        {
            return;
        }

        throw new ArgumentException("Page-label style is not supported.", "style");
    }

    internal static string FormatPageLabelNumber(int value, PdfPageLabelStyle style)
    {
        if (style == PdfPageLabelStyle.Decimal)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        if (style == PdfPageLabelStyle.UpperRoman)
        {
            return FormatRomanPageLabel(value, true);
        }

        if (style == PdfPageLabelStyle.LowerRoman)
        {
            return FormatRomanPageLabel(value, false);
        }

        if (style == PdfPageLabelStyle.UpperLetters)
        {
            return FormatLetterPageLabel(value, true);
        }

        return FormatLetterPageLabel(value, false);
    }

    internal static string FormatRomanPageLabel(int value, bool upperCase)
    {
        int[] values = new int[] { 1000, 900, 500, 400, 100, 90, 50, 40, 10, 9, 5, 4, 1 };
        string[] symbols = new string[] { "M", "CM", "D", "CD", "C", "XC", "L", "XL", "X", "IX", "V", "IV", "I" };
        string result = string.Empty;
        for (int index = 0; index < values.Length; index++)
        {
            while (value >= values[index])
            {
                result += symbols[index];
                value -= values[index];
            }
        }

        if (!upperCase)
        {
            return result.ToLowerInvariant();
        }

        return result;
    }

    internal static string FormatLetterPageLabel(int value, bool upperCase)
    {
        string result = string.Empty;
        int letterIndex = (value - 1) % 26;
        int repetitionCount = (value - 1) / 26 + 1;
        char letter = (char)('A' + letterIndex);
        for (int index = 0; index < repetitionCount; index++)
        {
            result += letter.ToString();
        }

        if (!upperCase)
        {
            return result.ToLowerInvariant();
        }

        return result;
    }

    internal bool ContainsOutlineItem(PdfOutlineItem item)
    {
        return ContainsOutlineItemInList(_outlines, item);
    }

    internal bool ContainsOutlineItemInList(IReadOnlyList<PdfOutlineItem> items, PdfOutlineItem item)
    {
        foreach (PdfOutlineItem existingItem in items)
        {
            if (existingItem == item)
            {
                return true;
            }

            if (ContainsOutlineItemInList(existingItem.Children, item))
            {
                return true;
            }
        }

        return false;
    }

    internal bool HasNamedDestination(string name)
    {
        foreach (PdfNamedDestination destination in _namedDestinations)
        {
            if (destination.Name == name)
            {
                return true;
            }
        }

        return false;
    }

    internal bool HasAttachment(string fileName)
    {
        foreach (PdfAttachment attachment in _attachments)
        {
            if (attachment.FileName == fileName)
            {
                return true;
            }
        }

        return false;
    }

    internal static void ValidateDestinationName(string name, string parameterName)
    {
        if (name == null)
        {
            throw new ArgumentNullException(parameterName);
        }

        if (name.Length == 0)
        {
            throw new ArgumentException("Destination name cannot be empty.", parameterName);
        }
    }

    internal void ValidateDocumentPage(PdfPage page, string parameterName, string message)
    {
        if (page == null)
        {
            throw new ArgumentNullException(parameterName);
        }

        if (!ContainsPage(page))
        {
            throw new ArgumentException(message, parameterName);
        }
    }

    internal static void ValidateOutlineTitle(string title)
    {
        if (title == null)
        {
            throw new ArgumentNullException("title");
        }

        if (title.Length == 0)
        {
            throw new ArgumentException("Outline title cannot be empty.", "title");
        }
    }

    internal static void ValidateAttachment(string fileName, string mimeType, string description)
    {
        if (fileName == null)
        {
            throw new ArgumentNullException("fileName");
        }

        if (fileName.Length == 0)
        {
            throw new ArgumentException("Attachment file name cannot be empty.", "fileName");
        }

        if (mimeType == null)
        {
            throw new ArgumentNullException("mimeType");
        }

        if (description == null)
        {
            throw new ArgumentNullException("description");
        }
    }

    internal static void ValidateLanguage(string language)
    {
        if (language == null)
        {
            throw new ArgumentNullException("language");
        }

        if (language.Length == 0)
        {
            throw new ArgumentException("Document language cannot be empty.", "language");
        }

        foreach (char character in language)
        {
            if (!IsLanguageCharacter(character))
            {
                throw new ArgumentException("Document language must use a simple language tag.", "language");
            }
        }
    }

    internal static bool IsLanguageCharacter(char character)
    {
        if (character >= 'a' && character <= 'z')
        {
            return true;
        }

        if (character >= 'A' && character <= 'Z')
        {
            return true;
        }

        if (character >= '0' && character <= '9')
        {
            return true;
        }

        if (character == '-')
        {
            return true;
        }

        return false;
    }

    internal void ValidateFormField(string name, PdfPage page, double x, double y, double width, double height)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        PdfAcroFormField.ValidateFieldName(name);

        ValidateDocumentPage(page, "page", "Form field page must belong to the document.");

        if (AcroForm.HasField(name))
        {
            throw new ArgumentException("A form field with the same name already exists.", "name");
        }

        if (double.IsNaN(x) || double.IsInfinity(x) || double.IsNaN(y) || double.IsInfinity(y))
        {
            throw new ArgumentOutOfRangeException("x", "Form field coordinates must be finite.");
        }

        if (double.IsNaN(width) || double.IsInfinity(width) || width <= 0)
        {
            throw new ArgumentOutOfRangeException("width", "Form field width must be positive.");
        }

        if (double.IsNaN(height) || double.IsInfinity(height) || height <= 0)
        {
            throw new ArgumentOutOfRangeException("height", "Form field height must be positive.");
        }
    }

    internal static void FlattenFormField(PdfAcroFormField field)
    {
        if (field.FieldType == "Tx")
        {
            FlattenTextField(field);
        }
        else if (field.FieldType == "Btn")
        {
            FlattenCheckbox(field);
        }
        else if (field.FieldType == "Sig")
        {
            FlattenSignatureField(field);
        }
    }

    internal static void FlattenTextField(PdfAcroFormField field)
    {
        PdfRectangle rectangle = field.Rectangle;
        double fontSize = 10;
        double textY = rectangle.Y + (rectangle.Height - fontSize) / 2;

        field.Page.Canvas.StrokeRectangle(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
        if (field.Value.Length > 0)
        {
            field.Page.Canvas.DrawText(field.Value, rectangle.X + 2, textY, fontSize);
        }
    }

    internal static void FlattenCheckbox(PdfAcroFormField field)
    {
        PdfRectangle rectangle = field.Rectangle;

        field.Page.Canvas.StrokeRectangle(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
        if (field.Checked)
        {
            field.Page.Canvas.DrawLine(rectangle.X, rectangle.Y, rectangle.X + rectangle.Width, rectangle.Y + rectangle.Height);
            field.Page.Canvas.DrawLine(rectangle.X + rectangle.Width, rectangle.Y, rectangle.X, rectangle.Y + rectangle.Height);
        }
    }

    internal static void FlattenRadioButtonOption(PdfAcroFormField field)
    {
        PdfRectangle rectangle = field.Rectangle;
        double radius = Math.Min(rectangle.Width, rectangle.Height) / 2;
        double centerX = rectangle.X + rectangle.Width / 2;
        double centerY = rectangle.Y + rectangle.Height / 2;
        field.Page.Canvas.StrokeCircle(centerX, centerY, radius);
        if (field.Checked)
        {
            field.Page.Canvas.FillCircle(centerX, centerY, radius / 2);
        }
    }

    internal static void FlattenSignatureField(PdfAcroFormField field)
    {
        PdfRectangle rectangle = field.Rectangle;
        string label = field.AccessibilityName;
        if (label.Length == 0)
        {
            label = "Signature";
        }

        field.Page.Canvas.StrokeRectangle(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
        field.Page.Canvas.DrawText(label, rectangle.X + 2, rectangle.Y + (rectangle.Height - 10) / 2, 10);
    }
}
