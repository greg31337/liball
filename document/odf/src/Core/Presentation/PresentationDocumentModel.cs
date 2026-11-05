using System.Collections.Generic;
using System.Text;

namespace odf;

internal static class PresentationDocumentModel
{
    internal static void LoadPresentation(OdfDocument document)
    {
        if (document == null)
        {
            throw new OdfDocumentException(
                "A document is required for presentation materialization.");
        }
        OdfNode? presentation = FindPresentationBody(document);
        LoadPresentationLayouts(document);
        document.PresentationDeclarations = presentation == null
            ? []
            : LoadDeclarations(presentation);
        document.Slides = presentation == null
            ? []
            : LoadSlides(document, presentation);
        document.PresentationHandout = LoadHandout(document);
        EnrichMasterPages(document);
        PresentationShowModel.LoadFeatures(document, presentation);
        AnimationDocumentModel.LoadAnimations(document);
    }

    internal static void SynchronizePresentation(OdfDocument document)
    {
        if (document == null || document.Slides == null
            || document.PresentationDeclarations == null)
        {
            throw new OdfDocumentException(
                "Presentation collections are required for writing.");
        }
        int index = 0;
        while (index < document.StyleResources.Length)
        {
            OdfStyleResource resource = document.StyleResources[index];
            if (resource.Kind == OdfStyleResourceKind.PresentationLayout)
            {
                int placeholderIndex = 0;
                while (placeholderIndex
                    < resource.PresentationPlaceholders.Length)
                {
                    SynchronizePlaceholder(
                        resource.PresentationPlaceholders[
                            placeholderIndex]);
                    placeholderIndex = placeholderIndex + 1;
                }
            }
            index = index + 1;
        }
        index = 0;
        while (index < document.Slides.Length)
        {
            SynchronizeSlide(document, document.Slides[index]);
            index = index + 1;
        }
        SynchronizeDeclarations(document);
        SynchronizeHandout(document);
        index = 0;
        while (index < document.MasterPages.Length)
        {
            SynchronizeMaster(document.MasterPages[index]);
            index = index + 1;
        }
        ResolveRelationships(document);
        PresentationShowModel.SynchronizeFeatures(
            document,
            FindPresentationBody(document));
        AnimationDocumentModel.SynchronizeAnimations(document);
    }

    static void LoadPresentationLayouts(OdfDocument document)
    {
        int index = 0;
        while (index < document.StyleResources.Length)
        {
            OdfStyleResource resource = document.StyleResources[index];
            if (resource.Kind == OdfStyleResourceKind.PresentationLayout
                && resource.Node != null)
            {
                resource.PresentationPlaceholders = LoadPlaceholders(
                    document,
                    resource.Node,
                    resource);
            }
            else
            {
                resource.PresentationPlaceholders = [];
            }
            index = index + 1;
        }
    }

    static OdfSlide[] LoadSlides(
        OdfDocument document,
        OdfNode presentation)
    {
        List<OdfSlide> result = new List<OdfSlide>();
        int index = 0;
        while (index < presentation.Children.Length)
        {
            OdfNode node = presentation.Children[index];
            if (IsElement(node, OdfNamespaces.Drawing, "page"))
            {
                OdfDrawingObject? drawing = FindDrawing(document, node);
                if (drawing != null)
                {
                    result.Add(CreateSlide(
                        document,
                        drawing,
                        result.Count));
                }
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    static OdfSlide CreateSlide(
        OdfDocument document,
        OdfDrawingObject drawing,
        int index)
    {
        OdfNode node = drawing.Node ?? new OdfNode();
        OdfSlide result = new OdfSlide();
        result.Index = index;
        result.Name = drawing.Name;
        result.Id = drawing.Id;
        result.XmlId = drawing.XmlId;
        result.StyleName = drawing.StyleName;
        result.MasterPageName = drawing.MasterPageName;
        result.PresentationPageLayoutName =
            drawing.PresentationPageLayoutName;
        result.HeaderName = Read(
            node,
            OdfNamespaces.Presentation,
            "use-header-name");
        result.FooterName = Read(
            node,
            OdfNamespaces.Presentation,
            "use-footer-name");
        result.DateTimeName = Read(
            node,
            OdfNamespaces.Presentation,
            "use-date-time-name");
        result.Visibility = Read(
            node,
            OdfNamespaces.Presentation,
            "visibility");
        RefreshVisibility(result);
        result.NavigationOrder = drawing.NavigationOrder;
        result.Drawing = drawing;
        result.Node = node;
        result.Notes = LoadNotes(document, result, node);
        OdfNode? notesNode = result.Notes == null
            ? null
            : result.Notes.Node;
        result.DrawingObjects = ContainedSlideDrawings(
            document,
            node,
            notesNode,
            drawing);
        result.Placeholders = LoadSlidePlaceholders(
            document,
            node,
            notesNode);
        ResolveSlideRelationships(document, result);
        CaptureSlide(result);
        return result;
    }

    static OdfPresentationNotes? LoadNotes(
        OdfDocument document,
        OdfSlide slide,
        OdfNode slideNode)
    {
        OdfNode? node = FindDirectChild(
            slideNode,
            OdfNamespaces.Presentation,
            "notes");
        if (node == null)
        {
            return null;
        }
        OdfPresentationNotes result = new OdfPresentationNotes();
        result.StyleName = Read(
            node,
            OdfNamespaces.Drawing,
            "style-name");
        result.PresentationPageLayoutName = Read(
            node,
            OdfNamespaces.Presentation,
            "presentation-page-layout-name");
        result.PageThumbnailNode = FindDirectChild(
            node,
            OdfNamespaces.Drawing,
            "page-thumbnail");
        if (result.PageThumbnailNode != null)
        {
            result.PageNumber = Read(
                result.PageThumbnailNode,
                OdfNamespaces.Drawing,
                "page-number");
        }
        result.Slide = slide;
        result.PresentationPageLayout = FindPresentationLayout(
            document,
            result.PresentationPageLayoutName);
        result.Placeholders = LoadPlaceholders(document, node, null);
        ResolvePlaceholderAssignments(
            result.Placeholders,
            result.PresentationPageLayout);
        result.DrawingObjects = ContainedDrawings(document, node, null);
        result.TextBlocks = ContainedTextBlocks(document, node);
        result.Node = node;
        CaptureNotes(result);
        return result;
    }

    static OdfPresentationDeclaration[] LoadDeclarations(
        OdfNode presentation)
    {
        List<OdfPresentationDeclaration> result =
            new List<OdfPresentationDeclaration>();
        int index = 0;
        while (index < presentation.Children.Length)
        {
            OdfNode node = presentation.Children[index];
            OdfPresentationDeclarationKind kind = DeclarationKind(node);
            if (kind != OdfPresentationDeclarationKind.Unknown)
            {
                OdfPresentationDeclaration item =
                    new OdfPresentationDeclaration();
                item.Kind = kind;
                item.Name = Read(
                    node,
                    OdfNamespaces.Presentation,
                    "name");
                item.Source = Read(
                    node,
                    OdfNamespaces.Presentation,
                    "source");
                item.DataStyleName = Read(
                    node,
                    OdfNamespaces.Style,
                    "data-style-name");
                item.Text = TextContent(node);
                item.Node = node;
                CaptureDeclaration(item);
                result.Add(item);
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    static OdfPresentationHandout? LoadHandout(OdfDocument document)
    {
        OdfNode? node = FindElement(
            document,
            OdfNamespaces.Presentation,
            "handout-master");
        if (node == null)
        {
            return null;
        }
        OdfPresentationHandout result = new OdfPresentationHandout();
        result.StyleName = Read(
            node,
            OdfNamespaces.Drawing,
            "style-name");
        result.PresentationPageLayoutName = Read(
            node,
            OdfNamespaces.Presentation,
            "presentation-page-layout-name");
        result.HeaderName = Read(
            node,
            OdfNamespaces.Presentation,
            "use-header-name");
        result.FooterName = Read(
            node,
            OdfNamespaces.Presentation,
            "use-footer-name");
        result.DateTimeName = Read(
            node,
            OdfNamespaces.Presentation,
            "use-date-time-name");
        result.PresentationPageLayout = FindPresentationLayout(
            document,
            result.PresentationPageLayoutName);
        result.Placeholders = LoadPlaceholders(document, node, null);
        result.DrawingObjects = ContainedDrawings(document, node, null);
        result.TextBlocks = ContainedTextBlocks(document, node);
        result.Node = node;
        ResolveHandoutRelationships(document, result);
        CaptureHandout(result);
        return result;
    }

    static void EnrichMasterPages(OdfDocument document)
    {
        int index = 0;
        while (index < document.MasterPages.Length)
        {
            OdfMasterPage master = document.MasterPages[index];
            OdfNode? node = master.Node;
            if (node != null)
            {
                master.PresentationPageLayoutName = Read(
                    node,
                    OdfNamespaces.Presentation,
                    "presentation-page-layout-name");
                master.HeaderName = Read(
                    node,
                    OdfNamespaces.Presentation,
                    "use-header-name");
                master.FooterName = Read(
                    node,
                    OdfNamespaces.Presentation,
                    "use-footer-name");
                master.DateTimeName = Read(
                    node,
                    OdfNamespaces.Presentation,
                    "use-date-time-name");
                master.PresentationPageLayout = FindPresentationLayout(
                    document,
                    master.PresentationPageLayoutName);
                master.PresentationPlaceholders = LoadPlaceholders(
                    document,
                    node,
                    null);
                master.DrawingObjects = ContainedDrawings(
                    document,
                    node,
                    null);
                ResolveMasterRelationships(document, master);
            }
            CaptureMaster(master);
            index = index + 1;
        }
    }

    static OdfPresentationPlaceholder[] LoadPlaceholders(
        OdfDocument document,
        OdfNode container,
        OdfStyleResource? layout)
    {
        List<OdfPresentationPlaceholder> result =
            new List<OdfPresentationPlaceholder>();
        AddPlaceholders(document, container, layout, result, true);
        return result.ToArray();
    }

    static OdfPresentationPlaceholder[] LoadSlidePlaceholders(
        OdfDocument document,
        OdfNode container,
        OdfNode? notes)
    {
        List<OdfPresentationPlaceholder> result =
            new List<OdfPresentationPlaceholder>();
        int index = 0;
        while (index < container.Children.Length)
        {
            AddSlidePlaceholders(
                document,
                container.Children[index],
                notes,
                result);
            index = index + 1;
        }
        return result.ToArray();
    }

    static void AddSlidePlaceholders(
        OdfDocument document,
        OdfNode node,
        OdfNode? notes,
        List<OdfPresentationPlaceholder> result)
    {
        if (node == notes)
        {
            return;
        }
        if (IsPlaceholderNode(node))
        {
            result.Add(CreatePlaceholder(document, node, null));
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            AddSlidePlaceholders(
                document,
                node.Children[index],
                notes,
                result);
            index = index + 1;
        }
    }

    static void AddPlaceholders(
        OdfDocument document,
        OdfNode node,
        OdfStyleResource? layout,
        List<OdfPresentationPlaceholder> result,
        bool skipContainer)
    {
        if (!skipContainer && IsPlaceholderNode(node))
        {
            result.Add(CreatePlaceholder(document, node, layout));
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            AddPlaceholders(
                document,
                node.Children[index],
                layout,
                result,
                false);
            index = index + 1;
        }
    }

    static OdfPresentationPlaceholder CreatePlaceholder(
        OdfDocument document,
        OdfNode node,
        OdfStyleResource? layout)
    {
        OdfPresentationPlaceholder result =
            new OdfPresentationPlaceholder();
        result.ObjectName = IsElement(
            node,
            OdfNamespaces.Presentation,
            "placeholder")
            ? Read(node, OdfNamespaces.Presentation, "object")
            : Read(node, OdfNamespaces.Presentation, "class");
        result.PlaceholderValue = Read(
            node,
            OdfNamespaces.Presentation,
            "placeholder");
        OdfLexicalValue placeholder = OdfLexicalParser.ParseBoolean(
            result.PlaceholderValue);
        result.IsPlaceholderValid = result.PlaceholderValue.Length == 0
            || placeholder.IsValid;
        result.IsPlaceholder = IsElement(
                node,
                OdfNamespaces.Presentation,
                "placeholder")
            || placeholder.IsValid && placeholder.BooleanValue;
        result.UserTransformedValue = Read(
            node,
            OdfNamespaces.Presentation,
            "user-transformed");
        OdfLexicalValue transformed = OdfLexicalParser.ParseBoolean(
            result.UserTransformedValue);
        result.IsUserTransformedValid =
            result.UserTransformedValue.Length == 0 || transformed.IsValid;
        result.IsUserTransformed = transformed.IsValid
            && transformed.BooleanValue;
        result.X = Read(node, OdfNamespaces.Svg, "x");
        result.Y = Read(node, OdfNamespaces.Svg, "y");
        result.Width = Read(node, OdfNamespaces.Svg, "width");
        result.Height = Read(node, OdfNamespaces.Svg, "height");
        result.Drawing = FindDrawing(document, node);
        result.Layout = layout;
        result.Node = node;
        CapturePlaceholder(result);
        return result;
    }

    static void ResolveSlideRelationships(
        OdfDocument document,
        OdfSlide slide)
    {
        slide.MasterPage = FindMasterPage(
            document,
            slide.MasterPageName);
        slide.PresentationPageLayout = FindPresentationLayout(
            document,
            slide.PresentationPageLayoutName);
        slide.Header = FindDeclaration(
            document,
            OdfPresentationDeclarationKind.Header,
            slide.HeaderName);
        slide.Footer = FindDeclaration(
            document,
            OdfPresentationDeclarationKind.Footer,
            slide.FooterName);
        slide.DateTime = FindDeclaration(
            document,
            OdfPresentationDeclarationKind.DateTime,
            slide.DateTimeName);
        ResolvePlaceholderAssignments(
            slide.Placeholders,
            slide.PresentationPageLayout);
        List<OdfDrawingObject> navigation =
            new List<OdfDrawingObject>();
        bool valid = true;
        OdfDrawingObject? drawing = slide.Drawing;
        if (drawing != null)
        {
            int index = 0;
            while (index < drawing.References.Length)
            {
                OdfDrawingReference reference = drawing.References[index];
                if (reference.Kind
                    == OdfDrawingReferenceKind.NavigationOrder)
                {
                    if (reference.TargetObject == null
                        || !IsSlideDrawing(
                            reference.TargetObject.Node,
                            slide.Node,
                            slide.Notes == null
                                ? null
                                : slide.Notes.Node))
                    {
                        valid = false;
                    }
                    else
                    {
                        navigation.Add(reference.TargetObject);
                    }
                }
                index = index + 1;
            }
        }
        slide.NavigationObjects = navigation.ToArray();
        slide.IsNavigationOrderValid = valid
            && slide.NavigationObjects.Length
                == NavigationTokenCount(slide.NavigationOrder);
    }

    static void ResolveRelationships(OdfDocument document)
    {
        int index = 0;
        while (index < document.Slides.Length)
        {
            OdfSlide slide = document.Slides[index];
            slide.Index = index;
            ResolveSlideRelationships(document, slide);
            if (slide.Notes != null)
            {
                slide.Notes.PresentationPageLayout =
                    FindPresentationLayout(
                        document,
                        slide.Notes.PresentationPageLayoutName);
                ResolvePlaceholderAssignments(
                    slide.Notes.Placeholders,
                    slide.Notes.PresentationPageLayout);
            }
            index = index + 1;
        }
        if (document.PresentationHandout != null)
        {
            document.PresentationHandout.PresentationPageLayout =
                FindPresentationLayout(
                    document,
                    document.PresentationHandout
                        .PresentationPageLayoutName);
            ResolveHandoutRelationships(
                document,
                document.PresentationHandout);
        }
        index = 0;
        while (index < document.MasterPages.Length)
        {
            ResolveMasterRelationships(
                document,
                document.MasterPages[index]);
            index = index + 1;
        }
    }

    static void ResolveHandoutRelationships(
        OdfDocument document,
        OdfPresentationHandout handout)
    {
        handout.PresentationPageLayout = FindPresentationLayout(
            document,
            handout.PresentationPageLayoutName);
        handout.Header = FindDeclaration(
            document,
            OdfPresentationDeclarationKind.Header,
            handout.HeaderName);
        handout.Footer = FindDeclaration(
            document,
            OdfPresentationDeclarationKind.Footer,
            handout.FooterName);
        handout.DateTime = FindDeclaration(
            document,
            OdfPresentationDeclarationKind.DateTime,
            handout.DateTimeName);
        ResolvePlaceholderAssignments(
            handout.Placeholders,
            handout.PresentationPageLayout);
    }

    static void ResolveMasterRelationships(
        OdfDocument document,
        OdfMasterPage master)
    {
        master.PresentationPageLayout = FindPresentationLayout(
            document,
            master.PresentationPageLayoutName);
        master.PresentationHeader = FindDeclaration(
            document,
            OdfPresentationDeclarationKind.Header,
            master.HeaderName);
        master.PresentationFooter = FindDeclaration(
            document,
            OdfPresentationDeclarationKind.Footer,
            master.FooterName);
        master.PresentationDateTime = FindDeclaration(
            document,
            OdfPresentationDeclarationKind.DateTime,
            master.DateTimeName);
        ResolvePlaceholderAssignments(
            master.PresentationPlaceholders,
            master.PresentationPageLayout);
    }

    static void ResolvePlaceholderAssignments(
        OdfPresentationPlaceholder[] placeholders,
        OdfStyleResource? layout)
    {
        int index = 0;
        while (index < placeholders.Length)
        {
            OdfPresentationPlaceholder placeholder = placeholders[index];
            placeholder.Layout = layout;
            placeholder.LayoutPlaceholder = FindLayoutPlaceholder(
                layout,
                placeholder.ObjectName);
            index = index + 1;
        }
    }

    static OdfPresentationPlaceholder? FindLayoutPlaceholder(
        OdfStyleResource? layout,
        string objectName)
    {
        if (layout == null || objectName.Length == 0)
        {
            return null;
        }
        OdfPresentationPlaceholder? result = null;
        int count = 0;
        int index = 0;
        while (index < layout.PresentationPlaceholders.Length)
        {
            OdfPresentationPlaceholder candidate =
                layout.PresentationPlaceholders[index];
            if (candidate.ObjectName == objectName)
            {
                result = candidate;
                count = count + 1;
            }
            index = index + 1;
        }
        return count == 1 ? result : null;
    }

    static void SynchronizeSlide(
        OdfDocument document,
        OdfSlide slide)
    {
        if (slide == null || slide.Node == null || slide.Drawing == null)
        {
            throw new OdfDocumentException(
                "A presentation slide requires its drawing page element.");
        }
        slide.Drawing.Name = slide.Name;
        slide.Drawing.Id = slide.Id;
        slide.Drawing.XmlId = slide.XmlId;
        slide.Drawing.StyleName = slide.StyleName;
        if (slide.Drawing.StyleNamespace.Length == 0)
        {
            slide.Drawing.StyleNamespace = OdfNamespaces.Drawing;
        }
        slide.Drawing.MasterPageName = slide.MasterPageName;
        slide.Drawing.PresentationPageLayoutName =
            slide.PresentationPageLayoutName;
        slide.Drawing.NavigationOrder = slide.NavigationOrder;
        WriteChanged(slide.Node, OdfNamespaces.Presentation, "use-header-name", OdfNamespaces.PresentationPrefix, slide.HeaderName, slide.OriginalHeaderName);
        WriteChanged(slide.Node, OdfNamespaces.Presentation, "use-footer-name", OdfNamespaces.PresentationPrefix, slide.FooterName, slide.OriginalFooterName);
        WriteChanged(slide.Node, OdfNamespaces.Presentation, "use-date-time-name", OdfNamespaces.PresentationPrefix, slide.DateTimeName, slide.OriginalDateTimeName);
        WriteChanged(slide.Node, OdfNamespaces.Presentation, "visibility", OdfNamespaces.PresentationPrefix, slide.Visibility, slide.OriginalVisibility);
        int index = 0;
        while (index < slide.Placeholders.Length)
        {
            SynchronizePlaceholder(slide.Placeholders[index]);
            index = index + 1;
        }
        if (slide.Notes != null)
        {
            if (slide.Notes.Node == null)
            {
                slide.Notes.Node = CreateElement(
                    OdfNamespaces.Presentation,
                    "notes",
                    OdfNamespaces.PresentationPrefix,
                    slide.Node.Location.PartPath);
                StyleDocumentModel.AppendNode(
                    slide.Node,
                    slide.Notes.Node);
                slide.Notes.Slide = slide;
            }
            SynchronizeNotes(slide.Notes);
        }
        RefreshVisibility(slide);
        CaptureSlide(slide);
    }

    static void SynchronizeNotes(OdfPresentationNotes notes)
    {
        OdfNode node = notes.Node ?? new OdfNode();
        WriteChanged(node, OdfNamespaces.Drawing, "style-name", OdfNamespaces.DrawingPrefix, notes.StyleName, notes.OriginalStyleName);
        WriteChanged(node, OdfNamespaces.Presentation, "presentation-page-layout-name", OdfNamespaces.PresentationPrefix, notes.PresentationPageLayoutName, notes.OriginalPresentationPageLayoutName);
        if (notes.PageNumber.Length > 0
            && notes.PageThumbnailNode == null)
        {
            notes.PageThumbnailNode = CreateElement(
                OdfNamespaces.Drawing,
                "page-thumbnail",
                OdfNamespaces.DrawingPrefix,
                node.Location.PartPath);
            StyleDocumentModel.AppendNode(
                node,
                notes.PageThumbnailNode);
        }
        if (notes.PageThumbnailNode != null)
        {
            WriteChanged(notes.PageThumbnailNode, OdfNamespaces.Drawing, "page-number", OdfNamespaces.DrawingPrefix, notes.PageNumber, notes.OriginalPageNumber);
        }
        int index = 0;
        while (index < notes.Placeholders.Length)
        {
            SynchronizePlaceholder(notes.Placeholders[index]);
            index = index + 1;
        }
        CaptureNotes(notes);
    }

    static void SynchronizePlaceholder(
        OdfPresentationPlaceholder placeholder)
    {
        if (placeholder == null || placeholder.Node == null)
        {
            throw new OdfDocumentException(
                "A presentation placeholder requires an element.");
        }
        bool layoutPlaceholder = IsElement(
            placeholder.Node,
            OdfNamespaces.Presentation,
            "placeholder");
        WriteChanged(placeholder.Node, OdfNamespaces.Presentation, layoutPlaceholder ? "object" : "class", OdfNamespaces.PresentationPrefix, placeholder.ObjectName, placeholder.OriginalObjectName);
        WriteChanged(placeholder.Node, OdfNamespaces.Presentation, "placeholder", OdfNamespaces.PresentationPrefix, placeholder.PlaceholderValue, placeholder.OriginalPlaceholderValue);
        WriteChanged(placeholder.Node, OdfNamespaces.Presentation, "user-transformed", OdfNamespaces.PresentationPrefix, placeholder.UserTransformedValue, placeholder.OriginalUserTransformedValue);
        WriteChanged(placeholder.Node, OdfNamespaces.Svg, "x", OdfNamespaces.SvgPrefix, placeholder.X, placeholder.OriginalX);
        WriteChanged(placeholder.Node, OdfNamespaces.Svg, "y", OdfNamespaces.SvgPrefix, placeholder.Y, placeholder.OriginalY);
        WriteChanged(placeholder.Node, OdfNamespaces.Svg, "width", OdfNamespaces.SvgPrefix, placeholder.Width, placeholder.OriginalWidth);
        WriteChanged(placeholder.Node, OdfNamespaces.Svg, "height", OdfNamespaces.SvgPrefix, placeholder.Height, placeholder.OriginalHeight);
        CapturePlaceholder(placeholder);
    }

    static void SynchronizeDeclarations(OdfDocument document)
    {
        OdfNode? body = FindPresentationBody(document);
        int index = 0;
        while (index < document.PresentationDeclarations.Length)
        {
            OdfPresentationDeclaration declaration =
                document.PresentationDeclarations[index];
            if (declaration == null)
            {
                throw new OdfDocumentException(
                    "The presentation declaration collection contains null.");
            }
            if (declaration.Node == null)
            {
                if (body == null)
                {
                    throw new OdfDocumentException(
                        "A presentation body is required for declarations.");
                }
                declaration.Node = CreateElement(
                    OdfNamespaces.Presentation,
                    DeclarationElementName(declaration.Kind),
                    OdfNamespaces.PresentationPrefix,
                    body.Location.PartPath);
                StyleDocumentModel.AppendNode(body, declaration.Node);
            }
            WriteChanged(declaration.Node, OdfNamespaces.Presentation, "name", OdfNamespaces.PresentationPrefix, declaration.Name, declaration.OriginalName);
            WriteChanged(declaration.Node, OdfNamespaces.Presentation, "source", OdfNamespaces.PresentationPrefix, declaration.Source, declaration.OriginalSource);
            WriteChanged(declaration.Node, OdfNamespaces.Style, "data-style-name", OdfNamespaces.StylePrefix, declaration.DataStyleName, declaration.OriginalDataStyleName);
            if (declaration.Text != declaration.OriginalText)
            {
                SetTextContent(declaration.Node, declaration.Text);
            }
            CaptureDeclaration(declaration);
            index = index + 1;
        }
    }

    static void SynchronizeHandout(OdfDocument document)
    {
        OdfPresentationHandout? handout = document.PresentationHandout;
        if (handout == null)
        {
            return;
        }
        if (handout.Node == null)
        {
            OdfNode masterStyles = RequireMasterStyles(document);
            handout.Node = CreateElement(
                OdfNamespaces.Presentation,
                "handout-master",
                OdfNamespaces.PresentationPrefix,
                masterStyles.Location.PartPath);
            StyleDocumentModel.AppendNode(masterStyles, handout.Node);
        }
        WriteChanged(handout.Node, OdfNamespaces.Drawing, "style-name", OdfNamespaces.DrawingPrefix, handout.StyleName, handout.OriginalStyleName);
        WriteChanged(handout.Node, OdfNamespaces.Presentation, "presentation-page-layout-name", OdfNamespaces.PresentationPrefix, handout.PresentationPageLayoutName, handout.OriginalPresentationPageLayoutName);
        WriteChanged(handout.Node, OdfNamespaces.Presentation, "use-header-name", OdfNamespaces.PresentationPrefix, handout.HeaderName, handout.OriginalHeaderName);
        WriteChanged(handout.Node, OdfNamespaces.Presentation, "use-footer-name", OdfNamespaces.PresentationPrefix, handout.FooterName, handout.OriginalFooterName);
        WriteChanged(handout.Node, OdfNamespaces.Presentation, "use-date-time-name", OdfNamespaces.PresentationPrefix, handout.DateTimeName, handout.OriginalDateTimeName);
        int index = 0;
        while (index < handout.Placeholders.Length)
        {
            SynchronizePlaceholder(handout.Placeholders[index]);
            index = index + 1;
        }
        CaptureHandout(handout);
    }

    static void SynchronizeMaster(OdfMasterPage master)
    {
        if (master.Node == null)
        {
            return;
        }
        WriteChanged(master.Node, OdfNamespaces.Presentation, "presentation-page-layout-name", OdfNamespaces.PresentationPrefix, master.PresentationPageLayoutName, master.OriginalPresentationPageLayoutName);
        WriteChanged(master.Node, OdfNamespaces.Presentation, "use-header-name", OdfNamespaces.PresentationPrefix, master.HeaderName, master.OriginalHeaderName);
        WriteChanged(master.Node, OdfNamespaces.Presentation, "use-footer-name", OdfNamespaces.PresentationPrefix, master.FooterName, master.OriginalFooterName);
        WriteChanged(master.Node, OdfNamespaces.Presentation, "use-date-time-name", OdfNamespaces.PresentationPrefix, master.DateTimeName, master.OriginalDateTimeName);
        int index = 0;
        while (index < master.PresentationPlaceholders.Length)
        {
            SynchronizePlaceholder(master.PresentationPlaceholders[index]);
            index = index + 1;
        }
        CaptureMaster(master);
    }

    static OdfNode RequireMasterStyles(OdfDocument document)
    {
        int index = 0;
        while (index < document.Parts.Length)
        {
            if (document.Parts[index].MasterStyles != null)
            {
                return document.Parts[index].MasterStyles
                    ?? new OdfNode();
            }
            index = index + 1;
        }
        OdfPart? part = StyleDocumentModel.FindPart(
            document,
            document.PackageKind == OdfPackageKind.Flat
                ? ""
                : OdfPartNames.Styles);
        if (part == null || part.Root == null)
        {
            throw new OdfDocumentException(
                "A styles root is required for a handout master.");
        }
        OdfNode section = CreateElement(
            OdfNamespaces.Office,
            "master-styles",
            OdfNamespaces.OfficePrefix,
            part.Path);
        StyleDocumentModel.AppendNode(part.Root, section);
        part.MasterStyles = section;
        return section;
    }

    static OdfDrawingObject[] ContainedDrawings(
        OdfDocument document,
        OdfNode container,
        OdfDrawingObject? excluded)
    {
        List<OdfDrawingObject> result = new List<OdfDrawingObject>();
        int index = 0;
        while (index < document.DrawingObjects.Length)
        {
            OdfDrawingObject drawing = document.DrawingObjects[index];
            if (drawing != excluded
                && IsDescendantOf(drawing.Node, container))
            {
                result.Add(drawing);
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    static OdfDrawingObject[] ContainedSlideDrawings(
        OdfDocument document,
        OdfNode container,
        OdfNode? notes,
        OdfDrawingObject excluded)
    {
        List<OdfDrawingObject> result = new List<OdfDrawingObject>();
        int index = 0;
        while (index < document.DrawingObjects.Length)
        {
            OdfDrawingObject drawing = document.DrawingObjects[index];
            if (drawing != excluded
                && IsSlideDrawing(drawing.Node, container, notes))
            {
                result.Add(drawing);
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    static bool IsSlideDrawing(
        OdfNode? node,
        OdfNode? slide,
        OdfNode? notes)
    {
        return IsDescendantOf(node, slide)
            && (notes == null || !IsDescendantOf(node, notes));
    }

    static OdfTextBlock[] ContainedTextBlocks(
        OdfDocument document,
        OdfNode container)
    {
        List<OdfTextBlock> result = new List<OdfTextBlock>();
        int index = 0;
        while (index < document.TextBlocks.Length)
        {
            OdfTextBlock block = document.TextBlocks[index];
            if (IsDescendantOf(block.Node, container))
            {
                result.Add(block);
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    static bool IsDescendantOf(OdfNode? node, OdfNode? container)
    {
        if (node == null || container == null || node == container)
        {
            return false;
        }
        OdfNode? current = node.Parent;
        while (current != null)
        {
            if (current == container)
            {
                return true;
            }
            current = current.Parent;
        }
        return false;
    }

    static OdfDrawingObject? FindDrawing(
        OdfDocument document,
        OdfNode node)
    {
        int index = 0;
        while (index < document.DrawingObjects.Length)
        {
            if (document.DrawingObjects[index].Node == node)
            {
                return document.DrawingObjects[index];
            }
            index = index + 1;
        }
        return null;
    }

    static OdfMasterPage? FindMasterPage(
        OdfDocument document,
        string name)
    {
        if (name.Length == 0)
        {
            return null;
        }
        OdfMasterPage? result = null;
        int count = 0;
        int index = 0;
        while (index < document.MasterPages.Length)
        {
            if (document.MasterPages[index].Name == name)
            {
                result = document.MasterPages[index];
                count = count + 1;
            }
            index = index + 1;
        }
        return count == 1 ? result : null;
    }

    static OdfStyleResource? FindPresentationLayout(
        OdfDocument document,
        string name)
    {
        if (name.Length == 0)
        {
            return null;
        }
        OdfStyleResource? result = null;
        int count = 0;
        int index = 0;
        while (index < document.StyleResources.Length)
        {
            OdfStyleResource resource = document.StyleResources[index];
            if (resource.Kind == OdfStyleResourceKind.PresentationLayout
                && resource.Name == name)
            {
                result = resource;
                count = count + 1;
            }
            index = index + 1;
        }
        return count == 1 ? result : null;
    }

    static OdfPresentationDeclaration? FindDeclaration(
        OdfDocument document,
        OdfPresentationDeclarationKind kind,
        string name)
    {
        if (name.Length == 0)
        {
            return null;
        }
        OdfPresentationDeclaration? result = null;
        int count = 0;
        int index = 0;
        while (index < document.PresentationDeclarations.Length)
        {
            OdfPresentationDeclaration candidate =
                document.PresentationDeclarations[index];
            if (candidate.Kind == kind && candidate.Name == name)
            {
                result = candidate;
                count = count + 1;
            }
            index = index + 1;
        }
        return count == 1 ? result : null;
    }

    static OdfNode? FindPresentationBody(OdfDocument document)
    {
        return FindElement(
            document,
            OdfNamespaces.Office,
            "presentation");
    }

    static OdfNode? FindElement(
        OdfDocument document,
        string namespaceName,
        string localName)
    {
        int partIndex = 0;
        while (partIndex < document.Parts.Length)
        {
            OdfNode[] nodes = document.Parts[partIndex].Nodes;
            int index = 0;
            while (index < nodes.Length)
            {
                OdfNode? result = FindDescendant(
                    nodes[index],
                    namespaceName,
                    localName);
                if (result != null)
                {
                    return result;
                }
                index = index + 1;
            }
            partIndex = partIndex + 1;
        }
        return null;
    }

    static OdfNode? FindDescendant(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        if (IsElement(node, namespaceName, localName))
        {
            return node;
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            OdfNode? result = FindDescendant(
                node.Children[index],
                namespaceName,
                localName);
            if (result != null)
            {
                return result;
            }
            index = index + 1;
        }
        return null;
    }

    static OdfNode? FindDirectChild(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        int index = 0;
        while (index < node.Children.Length)
        {
            if (IsElement(node.Children[index], namespaceName, localName))
            {
                return node.Children[index];
            }
            index = index + 1;
        }
        return null;
    }

    static bool IsPlaceholderNode(OdfNode node)
    {
        return IsElement(
                node,
                OdfNamespaces.Presentation,
                "placeholder")
            || Read(node, OdfNamespaces.Presentation, "class").Length > 0
            || Read(
                node,
                OdfNamespaces.Presentation,
                "placeholder").Length > 0;
    }

    static OdfPresentationDeclarationKind DeclarationKind(OdfNode node)
    {
        if (IsElement(
            node,
            OdfNamespaces.Presentation,
            "header-decl"))
        {
            return OdfPresentationDeclarationKind.Header;
        }
        if (IsElement(
            node,
            OdfNamespaces.Presentation,
            "footer-decl"))
        {
            return OdfPresentationDeclarationKind.Footer;
        }
        if (IsElement(
            node,
            OdfNamespaces.Presentation,
            "date-time-decl"))
        {
            return OdfPresentationDeclarationKind.DateTime;
        }
        return OdfPresentationDeclarationKind.Unknown;
    }

    static string DeclarationElementName(
        OdfPresentationDeclarationKind kind)
    {
        if (kind == OdfPresentationDeclarationKind.Header)
        {
            return "header-decl";
        }
        if (kind == OdfPresentationDeclarationKind.Footer)
        {
            return "footer-decl";
        }
        if (kind == OdfPresentationDeclarationKind.DateTime)
        {
            return "date-time-decl";
        }
        throw new OdfDocumentException(
            "A known presentation declaration kind is required.");
    }

    static void RefreshVisibility(OdfSlide slide)
    {
        slide.IsVisibilityValid = slide.Visibility.Length == 0
            || slide.Visibility == "visible"
            || slide.Visibility == "hidden";
        slide.IsVisible = slide.Visibility.Length == 0
            || slide.Visibility == "visible";
    }

    static int NavigationTokenCount(string value)
    {
        int count = 0;
        int index = 0;
        while (index < value.Length)
        {
            while (index < value.Length
                && global::System.Char.IsWhiteSpace(value[index]))
            {
                index = index + 1;
            }
            if (index < value.Length)
            {
                count = count + 1;
            }
            while (index < value.Length
                && !global::System.Char.IsWhiteSpace(value[index]))
            {
                index = index + 1;
            }
        }
        return count;
    }

    static string TextContent(OdfNode node)
    {
        StringBuilder result = new StringBuilder();
        AppendText(node, result);
        return result.ToString();
    }

    static void AppendText(OdfNode node, StringBuilder result)
    {
        if (node.Kind == OdfNodeKind.Text)
        {
            result.Append(node.Value);
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            AppendText(node.Children[index], result);
            index = index + 1;
        }
    }

    static void SetTextContent(OdfNode node, string value)
    {
        List<OdfNode> children = new List<OdfNode>();
        int index = 0;
        while (index < node.Children.Length)
        {
            if (node.Children[index].Kind != OdfNodeKind.Text)
            {
                children.Add(node.Children[index]);
            }
            index = index + 1;
        }
        if (value.Length > 0)
        {
            OdfNode text = new OdfNode();
            text.Kind = OdfNodeKind.Text;
            text.Value = value;
            text.Parent = node;
            text.Location.PartPath = node.Location.PartPath;
            children.Insert(0, text);
        }
        node.Children = children.ToArray();
    }

    static OdfNode CreateElement(
        string namespaceName,
        string localName,
        string prefix,
        string partPath)
    {
        OdfNode result = new OdfNode();
        result.Kind = OdfNodeKind.Element;
        result.Name = StyleDocumentModel.QualifiedName(
            namespaceName,
            localName,
            prefix);
        result.Location.PartPath = partPath;
        return result;
    }

    static void WriteChanged(
        OdfNode node,
        string namespaceName,
        string localName,
        string prefix,
        string value,
        string original)
    {
        if (value == original)
        {
            return;
        }
        if (value.Length == 0)
        {
            StyleDocumentModel.RemoveAttribute(
                node,
                namespaceName,
                localName);
        }
        else
        {
            StyleDocumentModel.WriteAttribute(
                node,
                namespaceName,
                localName,
                prefix,
                value);
        }
    }

    static string Read(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        return StyleDocumentModel.ReadAttribute(
            node,
            namespaceName,
            localName);
    }

    static bool IsElement(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        return node.Kind == OdfNodeKind.Element
            && node.Name != null
            && node.Name.NamespaceName == namespaceName
            && node.Name.LocalName == localName;
    }

    static void CaptureSlide(OdfSlide slide)
    {
        slide.OriginalName = slide.Name;
        slide.OriginalId = slide.Id;
        slide.OriginalXmlId = slide.XmlId;
        slide.OriginalStyleName = slide.StyleName;
        slide.OriginalMasterPageName = slide.MasterPageName;
        slide.OriginalPresentationPageLayoutName =
            slide.PresentationPageLayoutName;
        slide.OriginalHeaderName = slide.HeaderName;
        slide.OriginalFooterName = slide.FooterName;
        slide.OriginalDateTimeName = slide.DateTimeName;
        slide.OriginalVisibility = slide.Visibility;
        slide.OriginalNavigationOrder = slide.NavigationOrder;
    }

    static void CaptureNotes(OdfPresentationNotes notes)
    {
        notes.OriginalStyleName = notes.StyleName;
        notes.OriginalPresentationPageLayoutName =
            notes.PresentationPageLayoutName;
        notes.OriginalPageNumber = notes.PageNumber;
    }

    static void CapturePlaceholder(
        OdfPresentationPlaceholder placeholder)
    {
        placeholder.OriginalObjectName = placeholder.ObjectName;
        placeholder.OriginalPlaceholderValue =
            placeholder.PlaceholderValue;
        placeholder.OriginalUserTransformedValue =
            placeholder.UserTransformedValue;
        placeholder.OriginalX = placeholder.X;
        placeholder.OriginalY = placeholder.Y;
        placeholder.OriginalWidth = placeholder.Width;
        placeholder.OriginalHeight = placeholder.Height;
    }

    static void CaptureDeclaration(
        OdfPresentationDeclaration declaration)
    {
        declaration.OriginalName = declaration.Name;
        declaration.OriginalSource = declaration.Source;
        declaration.OriginalDataStyleName = declaration.DataStyleName;
        declaration.OriginalText = declaration.Text;
    }

    static void CaptureHandout(OdfPresentationHandout handout)
    {
        handout.OriginalStyleName = handout.StyleName;
        handout.OriginalPresentationPageLayoutName =
            handout.PresentationPageLayoutName;
        handout.OriginalHeaderName = handout.HeaderName;
        handout.OriginalFooterName = handout.FooterName;
        handout.OriginalDateTimeName = handout.DateTimeName;
    }

    static void CaptureMaster(OdfMasterPage master)
    {
        master.OriginalPresentationPageLayoutName =
            master.PresentationPageLayoutName;
        master.OriginalHeaderName = master.HeaderName;
        master.OriginalFooterName = master.FooterName;
        master.OriginalDateTimeName = master.DateTimeName;
    }
}
