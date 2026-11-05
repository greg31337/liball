using System;
using System.Collections.Generic;
using System.Globalization;

namespace pdf;

internal class PdfDocumentGraphCopier
{
    internal PdfDocument Source;
    internal PdfDocument Target;
    internal Dictionary<PdfPage, PdfPage> PageMap;
    internal Dictionary<string, string> DestinationNames;
    internal Dictionary<string, string> ImageNames;
    internal Dictionary<string, string> FormNames;
    internal Dictionary<PdfAccessibilityNode, PdfAccessibilityNode> AccessibilityMap;
    internal bool ResourcesCopied;

    internal PdfDocumentGraphCopier(PdfDocument source, PdfDocument target)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (target == null)
        {
            throw new ArgumentNullException("target");
        }

        Source = source;
        Target = target;
        PageMap = new Dictionary<PdfPage, PdfPage>();
        DestinationNames = new Dictionary<string, string>();
        ImageNames = new Dictionary<string, string>();
        FormNames = new Dictionary<string, string>();
        AccessibilityMap = new Dictionary<PdfAccessibilityNode, PdfAccessibilityNode>();
        ResourcesCopied = false;
    }

    internal PdfPage CopySinglePage(int sourcePageIndex, int targetPageIndex)
    {
        CopyPageRange(sourcePageIndex, 1, targetPageIndex, false);
        return Target._pages[targetPageIndex];
    }

    internal void CopyPageRange(int firstPageIndex, int pageCount, int targetPageIndex, bool replaceDocumentSettings)
    {
        ValidatePageRange(firstPageIndex, pageCount, targetPageIndex);
        if (Source == Target)
        {
            throw new ArgumentException("A document cannot import or merge itself.", "source");
        }

        CopyResources();
        for (int offset = 0; offset < pageCount; offset++)
        {
            PdfPage sourcePage = Source._pages[firstPageIndex + offset];
            PdfPage copiedPage = CopyPageCore(sourcePage);
            Target._pages.Insert(targetPageIndex + offset, copiedPage);
            PageMap.Add(sourcePage, copiedPage);
        }

        CopyMetadataAndAttachments(replaceDocumentSettings);
        CopyDestinations();
        CopyAccessibilityNodes();
        CopyMarkedContentNodes();
        CopyPageObjects();
        CopyFormFields();
        CopyOutlines();
        CopyNavigationMarkers();
    }

    internal void CopyPageStamp(int sourcePageIndex, int targetPageIndex, bool foreground)
    {
        ValidatePageIndex(Source, sourcePageIndex, "sourcePageIndex");
        ValidatePageIndex(Target, targetPageIndex, "targetPageIndex");
        if (Source._pages[sourcePageIndex].Canvas.MarkedContentNodes.Count > 0)
        {
            throw new InvalidOperationException("Tagged page content cannot be used as a stamp because its marked-content identifiers belong to the source page.");
        }

        if (Source != Target)
        {
            CopyResources();
        }

        PdfCanvas stamp = CopyCanvas(Source._pages[sourcePageIndex].Canvas);
        if (foreground)
        {
            Target._pages[targetPageIndex].Canvas.AppendCanvas(stamp);
        }
        else
        {
            Target._pages[targetPageIndex].Canvas.PrependCanvas(stamp);
        }
    }

    internal void ValidatePageRange(int firstPageIndex, int pageCount, int targetPageIndex)
    {
        if (firstPageIndex < 0 || firstPageIndex > Source._pages.Count)
        {
            throw new ArgumentOutOfRangeException("firstPageIndex", "First page index must be within the source document.");
        }

        if (pageCount < 0 || firstPageIndex + pageCount > Source._pages.Count)
        {
            throw new ArgumentOutOfRangeException("pageCount", "Page count must fit within the source document.");
        }

        if (targetPageIndex < 0 || targetPageIndex > Target._pages.Count)
        {
            throw new ArgumentOutOfRangeException("targetPageIndex", "Target page index must be a valid insertion position.");
        }
    }

    internal static void ValidatePageIndex(PdfDocument document, int pageIndex, string name)
    {
        if (pageIndex < 0 || pageIndex >= document._pages.Count)
        {
            throw new ArgumentOutOfRangeException(name, "Page index must be within the document.");
        }
    }

    internal PdfPage CopyPageCore(PdfPage sourcePage)
    {
        PdfPage page = new PdfPage(sourcePage.Width, sourcePage.Height);
        page.OwnerDocument = Target;
        page.Canvas = CopyCanvas(sourcePage.Canvas);
        page.Margins = new PdfPageMargins(sourcePage.Margins.Left, sourcePage.Margins.Top, sourcePage.Margins.Right, sourcePage.Margins.Bottom);
        page.MediaBox = CopyRectangle(sourcePage.MediaBox);
        page.CropBox = CopyRectangle(sourcePage.CropBox);
        page.BleedBox = CopyRectangle(sourcePage.BleedBox);
        page.TrimBox = CopyRectangle(sourcePage.TrimBox);
        page.ArtBox = CopyRectangle(sourcePage.ArtBox);
        page.HasCropBox = sourcePage.HasCropBox;
        page.HasBleedBox = sourcePage.HasBleedBox;
        page.HasTrimBox = sourcePage.HasTrimBox;
        page.HasArtBox = sourcePage.HasArtBox;
        return page;
    }

    internal void CopyResources()
    {
        if (ResourcesCopied)
        {
            return;
        }

        foreach (PdfTrueTypeFont font in Source.FontRegistry.TrueTypeFonts)
        {
            Target.FontRegistry.StoreTrueTypeFont(font);
        }

        foreach (PdfImage image in Source.Images)
        {
            string name = "Im" + (Target._imageCache._images.Count + 1).ToString(CultureInfo.InvariantCulture);
            PdfImage copied = CopyImage(image, name);
            Target._imageCache._images.Add(copied);
            ImageNames.Add(image.Name, name);
        }

        foreach (PdfFormXObject form in Source.Forms)
        {
            PdfFormXObject copied = Target.AddForm(form.Width, form.Height);
            FormNames.Add(form.Name, copied.Name);
        }

        for (int index = 0; index < Source._forms.Count; index++)
        {
            int targetIndex = Target._forms.Count - Source._forms.Count + index;
            Target._forms[targetIndex].Canvas = CopyCanvas(Source._forms[index].Canvas);
        }

        ResourcesCopied = true;
    }

    internal PdfCanvas CopyCanvas(PdfCanvas sourceCanvas)
    {
        PdfCanvas canvas = new PdfCanvas();
        canvas.UsesDeviceRgb = sourceCanvas.UsesDeviceRgb;
        canvas.UsesDeviceCmyk = sourceCanvas.UsesDeviceCmyk;
        canvas.MarkedContentDepth = sourceCanvas.MarkedContentDepth;
        string content = sourceCanvas.ToPdfString();
        foreach (KeyValuePair<string, string> name in ImageNames)
        {
            content = content.Replace("/" + name.Key + " Do", "/" + name.Value + " Do");
        }

        foreach (KeyValuePair<string, string> name in FormNames)
        {
            content = content.Replace("/" + name.Key + " Do", "/" + name.Value + " Do");
        }

        canvas._content.Append(content);
        foreach (PdfFont font in sourceCanvas.Fonts)
        {
            canvas._fonts.Add(font);
        }

        foreach (PdfGraphicsState state in sourceCanvas.GraphicsStates)
        {
            canvas._graphicsStates.Add(new PdfGraphicsState(state.Name, state.HasFillOpacity, state.FillOpacity, state.HasStrokeOpacity, state.StrokeOpacity, state.HasBlendMode, state.BlendMode));
        }

        foreach (PdfTrueTypeFontUsage usage in sourceCanvas.TrueTypeFontUsages)
        {
            PdfTrueTypeFontUsage copiedUsage = new PdfTrueTypeFontUsage(usage.Font);
            copiedUsage.Merge(usage);
            canvas._trueTypeFontUsages.Add(copiedUsage);
        }

        return canvas;
    }

    internal static PdfImage CopyImage(PdfImage image, string name)
    {
        PdfImage copied = PdfImage.CreateInternal(name, image.Width, image.Height);
        copied.RgbData = CopyBytes(image.RgbData);
        copied.Format = image.Format;
        copied.ColorSpace = image.ColorSpace;
        copied.EncodedData = CopyBytes(image.EncodedData);
        copied.IsJpeg = image.IsJpeg;
        copied.IsPng = image.IsPng;
        copied.BitsPerComponent = image.BitsPerComponent;
        copied.JpegColorSpace = image.JpegColorSpace;
        copied.PngColorSpace = image.PngColorSpace;
        copied.CompressedData = CopyBytes(image.CompressedData);
        copied.HasSoftMask = image.HasSoftMask;
        copied.SoftMaskData = CopyBytes(image.SoftMaskData);
        copied.SoftMaskBitsPerComponent = image.SoftMaskBitsPerComponent;
        copied.HasDpiMetadata = image.HasDpiMetadata;
        copied.HorizontalDpi = image.HorizontalDpi;
        copied.VerticalDpi = image.VerticalDpi;
        copied.HasInvertedCmykSamples = image.HasInvertedCmykSamples;
        copied.AlternateText = image.AlternateText;
        copied.IsDecorative = image.IsDecorative;
        return copied;
    }

    internal void CopyMetadataAndAttachments(bool replaceDocumentSettings)
    {
        if (replaceDocumentSettings || !Target.Metadata.HasValues())
        {
            Target.Metadata = CopyMetadata(Source.Metadata);
        }

        if (replaceDocumentSettings || Target.Language.Length == 0)
        {
            Target.Language = Source.Language;
        }

        if (replaceDocumentSettings)
        {
            Target.WriterOptions = CopyWriterOptions(Source.WriterOptions);
            Target.HasPdfA = Source.HasPdfA;
            Target.PdfAOptions = Source.PdfAOptions.Copy();
            Target.HasFacturX = Source.HasFacturX;
            Target.FacturXMetadata = Source.FacturXMetadata.Copy();
            Target.HasViewerPreferences = Source.HasViewerPreferences;
            Target.ViewerPreferences = Source.ViewerPreferences.Copy();
            Target.HasInitialPageMode = Source.HasInitialPageMode;
            Target.InitialPageMode = Source.InitialPageMode;
            Target.HasInitialPageLayout = Source.HasInitialPageLayout;
            Target.InitialPageLayout = Source.InitialPageLayout;
        }

        foreach (PdfAttachment attachment in Source.Attachments)
        {
            if (!HasEquivalentAttachment(attachment))
            {
                string fileName = UniqueAttachmentName(attachment.FileName);
                PdfAttachment copiedAttachment = new PdfAttachment(fileName, attachment.Data, attachment.MimeType, attachment.Description);
                copiedAttachment.IsAssociatedFile = attachment.IsAssociatedFile;
                copiedAttachment.Relationship = attachment.Relationship;
                Target._attachments.Add(copiedAttachment);
            }
        }
    }

    internal void CopyDestinations()
    {
        foreach (PdfNamedDestination destination in Source.NamedDestinations)
        {
            if (PageMap.ContainsKey(destination.Page))
            {
                string name = UniqueDestinationName(destination.Name);
                PdfNamedDestination copied = new PdfNamedDestination(name, PageMap[destination.Page], destination.X, destination.Y);
                Target._namedDestinations.Add(copied);
                DestinationNames[destination.Name] = name;
            }
        }
    }

    internal void CopyAccessibilityNodes()
    {
        foreach (PdfAccessibilityNode node in Source.AccessibilityNodes)
        {
            if (PageMap.ContainsKey(node.Page))
            {
                PdfAccessibilityNode copied = new PdfAccessibilityNode(PageMap[node.Page], node.Role, node.Description);
                copied.AlternateText = node.AlternateText;
                copied.ActualText = node.ActualText;
                copied.Language = node.Language;
                copied.OwnerDocument = Target;
                copied.HasTableHeaderScope = node.HasTableHeaderScope;
                copied.TableHeaderScope = node.TableHeaderScope;
                Target._accessibilityNodes.Add(copied);
                AccessibilityMap.Add(node, copied);
            }
        }

        foreach (PdfAccessibilityNode node in Source.AccessibilityNodes)
        {
            if (AccessibilityMap.ContainsKey(node))
            {
                PdfAccessibilityNode copied = AccessibilityMap[node];
                if (node.Parent != null && AccessibilityMap.ContainsKey(node.Parent))
                {
                    copied.Parent = AccessibilityMap[node.Parent];
                    copied.Parent._children.Add(copied);
                }
                else
                {
                    Target._structureRoots.Add(copied);
                }
            }
        }

        if (Source.HasTaggedPdf && AccessibilityMap.Count > 0)
        {
            Target.HasTaggedPdf = true;
        }

        foreach (PdfStructureLinkTarget target in Source.StructureLinkTargets)
        {
            if (AccessibilityMap.ContainsKey(target.StructureNode) && DestinationNames.ContainsKey(target.Destination.Name))
            {
                PdfNamedDestination destination = FindTargetDestination(DestinationNames[target.Destination.Name]);
                PdfAccessibilityNode node = AccessibilityMap[target.StructureNode];
                destination.SetStructureTarget(node);
                Target._structureLinkTargets.Add(new PdfStructureLinkTarget(UniqueStructureTargetName(target.Name), node, destination));
            }
        }
    }

    internal void CopyMarkedContentNodes()
    {
        foreach (KeyValuePair<PdfPage, PdfPage> entry in PageMap)
        {
            foreach (PdfAccessibilityNode node in entry.Key.Canvas.MarkedContentNodes)
            {
                if (AccessibilityMap.ContainsKey(node))
                {
                    PdfAccessibilityNode copied = AccessibilityMap[node];
                    int markedContentId = entry.Value.Canvas._markedContentNodes.Count;
                    entry.Value.Canvas._markedContentNodes.Add(copied);
                    copied._markedContentIds.Add(markedContentId);
                }
            }
        }
    }

    internal void CopyPageObjects()
    {
        foreach (KeyValuePair<PdfPage, PdfPage> entry in PageMap)
        {
            PdfPage sourcePage = entry.Key;
            PdfPage targetPage = entry.Value;
            foreach (PdfExternalLink link in sourcePage.ExternalLinks)
            {
                PdfExternalLink copiedLink = new PdfExternalLink(link.Uri, CopyRectangle(link.Rectangle));
                if (link.TagNode != null && AccessibilityMap.ContainsKey(link.TagNode))
                {
                    copiedLink.TagNode = AccessibilityMap[link.TagNode];
                }

                targetPage._externalLinks.Add(copiedLink);
            }

            foreach (PdfInternalLink link in sourcePage.InternalLinks)
            {
                if (DestinationNames.ContainsKey(link.DestinationName))
                {
                    PdfInternalLink copiedLink = new PdfInternalLink(DestinationNames[link.DestinationName], CopyRectangle(link.Rectangle));
                    if (link.StructureTarget != null && AccessibilityMap.ContainsKey(link.StructureTarget))
                    {
                        copiedLink.SetStructureTarget(AccessibilityMap[link.StructureTarget]);
                    }

                    if (link.TagNode != null && AccessibilityMap.ContainsKey(link.TagNode))
                    {
                        copiedLink.TagNode = AccessibilityMap[link.TagNode];
                    }

                    targetPage._internalLinks.Add(copiedLink);
                }
            }

            foreach (PdfAnnotation annotation in sourcePage.Annotations)
            {
                PdfAnnotation copiedAnnotation = new PdfAnnotation(annotation.Subtype, annotation.Contents, CopyRectangle(annotation.Rectangle));
                copiedAnnotation.Color = new PdfColor(annotation.Color.Mode, annotation.Color.First, annotation.Color.Second, annotation.Color.Third, annotation.Color.Fourth);
                copiedAnnotation.HasColor = annotation.HasColor;
                copiedAnnotation.Open = annotation.Open;
                copiedAnnotation.IconName = annotation.IconName;
                if (annotation.TagNode != null && AccessibilityMap.ContainsKey(annotation.TagNode))
                {
                    copiedAnnotation.TagNode = AccessibilityMap[annotation.TagNode];
                }

                targetPage._annotations.Add(copiedAnnotation);
            }
        }
    }

    internal void CopyFormFields()
    {
        bool copiedAny = false;
        foreach (PdfAcroFormField field in Source.AcroForm.Fields)
        {
            if (PageMap.ContainsKey(field.Page))
            {
                string name = UniqueFieldName(field.Name);
                PdfAcroFormField copied = CopyField(field, name, PageMap[field.Page]);
                if (field.TagNode != null && AccessibilityMap.ContainsKey(field.TagNode))
                {
                    copied.TagNode = AccessibilityMap[field.TagNode];
                }

                Target.AcroForm._fields.Add(copied);
                copiedAny = true;
            }
        }

        foreach (PdfRadioButtonGroup group in Source.AcroForm.RadioButtonGroups)
        {
            PdfRadioButtonGroup copiedGroup = new PdfRadioButtonGroup(UniqueFieldName(group.Name));
            copiedGroup.AccessibilityName = group.AccessibilityName;
            foreach (PdfAcroFormField option in group.Options)
            {
                if (PageMap.ContainsKey(option.Page))
                {
                    PdfAcroFormField copiedOption = copiedGroup.AddOption(option.ExportValue, PageMap[option.Page], option.Rectangle.X, option.Rectangle.Y, option.Rectangle.Width, option.Rectangle.Height);
                    CopyFieldValues(option, copiedOption);
                    if (option.TagNode != null && AccessibilityMap.ContainsKey(option.TagNode))
                    {
                        copiedOption.TagNode = AccessibilityMap[option.TagNode];
                    }
                }
            }

            if (copiedGroup._options.Count > 0)
            {
                if (group.SelectedValue.Length > 0)
                {
                    foreach (PdfAcroFormField option in copiedGroup.Options)
                    {
                        if (option.ExportValue == group.SelectedValue)
                        {
                            copiedGroup.SelectOption(group.SelectedValue);
                        }
                    }
                }

                Target.AcroForm._radioButtonGroups.Add(copiedGroup);
                copiedGroup.SetOwnerDocument(Target);
                copiedAny = true;
            }
        }

        if (copiedAny)
        {
            Target.HasAcroForm = true;
            Target.AcroForm.NeedAppearances = Source.AcroForm.NeedAppearances;
            Target.AcroForm.DefaultAppearance = Source.AcroForm.DefaultAppearance;
        }
    }

    internal static PdfAcroFormField CopyField(PdfAcroFormField field, string name, PdfPage page)
    {
        PdfAcroFormField copied = new PdfAcroFormField(field.FieldType, name, page, CopyRectangle(field.Rectangle));
        CopyFieldValues(field, copied);
        return copied;
    }

    internal static void CopyFieldValues(PdfAcroFormField source, PdfAcroFormField target)
    {
        target.Value = source.Value;
        target.Checked = source.Checked;
        target.HasSignaturePlaceholder = source.HasSignaturePlaceholder;
        target.SignatureContentsLength = source.SignatureContentsLength;
        target.SignatureFilter = source.SignatureFilter;
        target.SignatureSubFilter = source.SignatureSubFilter;
        target.SignatureReason = source.SignatureReason;
        target.SignatureLocation = source.SignatureLocation;
        target.SignatureContactInfo = source.SignatureContactInfo;
        target.AccessibilityName = source.AccessibilityName;
    }

    internal void CopyOutlines()
    {
        foreach (PdfOutlineItem outline in Source.Outlines)
        {
            CopyOutlineBranch(outline, null);
        }
    }

    internal void CopyOutlineBranch(PdfOutlineItem sourceOutline, PdfOutlineItem? targetParent)
    {
        PdfOutlineItem? copied = targetParent;
        if (PageMap.ContainsKey(sourceOutline.Page))
        {
            copied = new PdfOutlineItem(sourceOutline.Title, PageMap[sourceOutline.Page], sourceOutline.X, sourceOutline.Y);
            if (targetParent == null)
            {
                Target._outlines.Add(copied);
            }
            else
            {
                targetParent.AddChild(copied);
            }
        }

        foreach (PdfOutlineItem child in sourceOutline.Children)
        {
            CopyOutlineBranch(child, copied);
        }
    }

    internal void CopyNavigationMarkers()
    {
        foreach (PdfPageLabelRange range in Source.PageLabelRanges)
        {
            if (PageMap.ContainsKey(range.Page))
            {
                Target._pageLabelRanges.Add(new PdfPageLabelRange(PageMap[range.Page], range.Style, range.Prefix, range.StartNumber));
            }
        }

        foreach (PdfIndexMarker marker in Source.IndexMarkers)
        {
            if (PageMap.ContainsKey(marker.Page) && DestinationNames.ContainsKey(marker.DestinationName))
            {
                Target._indexMarkers.Add(new PdfIndexMarker(marker.Term, marker.Subterm, PageMap[marker.Page], marker.X, marker.Y, DestinationNames[marker.DestinationName]));
            }
        }

        foreach (PdfCrossReferenceMarker marker in Source.CrossReferenceMarkers)
        {
            if (PageMap.ContainsKey(marker.Page) && DestinationNames.ContainsKey(marker.DestinationName))
            {
                Target._crossReferenceMarkers.Add(new PdfCrossReferenceMarker(UniqueCrossReferenceName(marker.Name), marker.Text, PageMap[marker.Page], marker.X, marker.Y, DestinationNames[marker.DestinationName]));
            }
        }

        foreach (PdfPublishingMarker marker in Source.PublishingMarkers)
        {
            if (PageMap.ContainsKey(marker.Page) && DestinationNames.ContainsKey(marker.DestinationName))
            {
                Target._publishingMarkers.Add(new PdfPublishingMarker(marker.Kind, marker.Title, PageMap[marker.Page], marker.X, marker.Y, DestinationNames[marker.DestinationName]));
            }
        }
    }

    internal string UniqueDestinationName(string requested)
    {
        string candidate = requested;
        int suffix = 2;
        while (HasTargetDestination(candidate))
        {
            candidate = requested + " (" + suffix.ToString(CultureInfo.InvariantCulture) + ")";
            suffix++;
        }

        return candidate;
    }

    internal bool HasTargetDestination(string name)
    {
        foreach (PdfNamedDestination destination in Target.NamedDestinations)
        {
            if (destination.Name == name)
            {
                return true;
            }
        }

        return false;
    }

    internal PdfNamedDestination FindTargetDestination(string name)
    {
        foreach (PdfNamedDestination destination in Target.NamedDestinations)
        {
            if (destination.Name == name)
            {
                return destination;
            }
        }

        throw new InvalidOperationException("Copied destination was not found.");
    }

    internal string UniqueFieldName(string requested)
    {
        string candidate = requested;
        int suffix = 2;
        while (Target.AcroForm.HasField(candidate))
        {
            candidate = requested + " (" + suffix.ToString(CultureInfo.InvariantCulture) + ")";
            suffix++;
        }

        return candidate;
    }

    internal string UniqueAttachmentName(string requested)
    {
        string candidate = requested;
        string stem = requested;
        string extension = string.Empty;
        int dot = requested.LastIndexOf('.');
        if (dot > 0)
        {
            stem = requested.Substring(0, dot);
            extension = requested.Substring(dot);
        }

        int suffix = 2;
        while (HasTargetAttachment(candidate))
        {
            candidate = stem + " (" + suffix.ToString(CultureInfo.InvariantCulture) + ")" + extension;
            suffix++;
        }

        return candidate;
    }

    internal bool HasEquivalentAttachment(PdfAttachment sourceAttachment)
    {
        foreach (PdfAttachment attachment in Target.Attachments)
        {
            if (attachment.FileName == sourceAttachment.FileName && attachment.MimeType == sourceAttachment.MimeType && attachment.Description == sourceAttachment.Description && BytesEqual(attachment.Data, sourceAttachment.Data))
            {
                return true;
            }
        }

        return false;
    }

    internal bool HasTargetAttachment(string name)
    {
        foreach (PdfAttachment attachment in Target.Attachments)
        {
            if (attachment.FileName == name)
            {
                return true;
            }
        }

        return false;
    }

    internal string UniqueCrossReferenceName(string requested)
    {
        string candidate = requested;
        int suffix = 2;
        while (HasTargetCrossReference(candidate))
        {
            candidate = requested + " (" + suffix.ToString(CultureInfo.InvariantCulture) + ")";
            suffix++;
        }

        return candidate;
    }

    internal bool HasTargetCrossReference(string name)
    {
        foreach (PdfCrossReferenceMarker marker in Target.CrossReferenceMarkers)
        {
            if (marker.Name == name)
            {
                return true;
            }
        }

        return false;
    }

    internal string UniqueStructureTargetName(string requested)
    {
        string candidate = requested;
        int suffix = 2;
        while (HasTargetStructureTarget(candidate))
        {
            candidate = requested + " (" + suffix.ToString(CultureInfo.InvariantCulture) + ")";
            suffix++;
        }

        return candidate;
    }

    internal bool HasTargetStructureTarget(string name)
    {
        foreach (PdfStructureLinkTarget target in Target.StructureLinkTargets)
        {
            if (target.Name == name)
            {
                return true;
            }
        }

        return false;
    }

    internal static PdfMetadata CopyMetadata(PdfMetadata source)
    {
        PdfMetadata metadata = new PdfMetadata();
        metadata.Title = source.Title;
        metadata.Author = source.Author;
        metadata.Subject = source.Subject;
        metadata.Keywords = source.Keywords;
        metadata.Creator = source.Creator;
        metadata.Producer = source.Producer;
        metadata.HasCreationDateValue = source.HasCreationDateValue;
        metadata.CreationDate = source.CreationDate;
        metadata.HasModificationDateValue = source.HasModificationDateValue;
        metadata.ModificationDate = source.ModificationDate;
        metadata.XmpMetadata = source.XmpMetadata;
        return metadata;
    }

    internal static PdfWriterOptions CopyWriterOptions(PdfWriterOptions source)
    {
        PdfWriterOptions options = new PdfWriterOptions();
        options.Version = source.Version;
        options.Deterministic = source.Deterministic;
        options.DeterministicDocumentId = source.DeterministicDocumentId;
        options.CompressContentStreams = source.CompressContentStreams;
        return options;
    }

    internal static PdfRectangle CopyRectangle(PdfRectangle source)
    {
        return new PdfRectangle(source.X, source.Y, source.Width, source.Height);
    }

    internal static byte[] CopyBytes(byte[] source)
    {
        byte[] copied = new byte[source.Length];
        Array.Copy(source, copied, source.Length);
        return copied;
    }

    internal static bool BytesEqual(byte[] first, byte[] second)
    {
        if (first.Length != second.Length)
        {
            return false;
        }

        for (int index = 0; index < first.Length; index++)
        {
            if (first[index] != second[index])
            {
                return false;
            }
        }

        return true;
    }
}
