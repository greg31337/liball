using System;
using System.Collections.Generic;

namespace odf;

internal static class PresentationEditingModel
{
    internal static OdfSlide[] GetSlides(OdfDocument document)
    {
        RequirePresentationDocument(document);
        return (OdfSlide[])document.Slides.Clone();
    }

    internal static OdfPresentationNotes CreateSlideNotes(
        OdfDocument document,
        OdfSlide slide)
    {
        PresentationSlideEditingModel.RequireOwnedSlide(document, slide);
        if (slide.Notes != null)
        {
            throw new OdfEditException(
                "The slide already has a notes page.");
        }
        OdfNode slideNode = slide.Node ?? new OdfNode();
        OdfPresentationNotes notes = new OdfPresentationNotes();
        notes.Slide = slide;
        slide.Notes = notes;
        SynchronizePresentationAndReload(document);
        OdfSlide refreshed = PresentationSlideEditingModel.RequireSlideByNode(
            document,
            slideNode);
        return refreshed.Notes ?? throw new OdfEditException(
            "The created notes page could not be materialized.");
    }

    internal static void RemoveSlideNotes(
        OdfDocument document,
        OdfSlide slide)
    {
        PresentationSlideEditingModel.RequireOwnedSlide(document, slide);
        if (slide.Notes == null || slide.Notes.Node == null)
        {
            return;
        }
        PresentationDocumentModel.SynchronizePresentation(document);
        StyleDocumentModel.RemoveNodeFromParent(slide.Notes.Node);
        PresentationDocumentModel.LoadPresentation(document);
    }

    internal static OdfStyleResource[] GetPresentationLayouts(
        OdfDocument document)
    {
        RequirePresentationDocument(document);
        List<OdfStyleResource> result = new List<OdfStyleResource>();
        int index = 0;
        while (index < document.StyleResources.Length)
        {
            OdfStyleResource resource = document.StyleResources[index];
            if (resource.Kind == OdfStyleResourceKind.PresentationLayout)
            {
                result.Add(resource);
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    internal static OdfStyleResource CreatePresentationLayout(
        OdfDocument document,
        string name,
        OdfPresentationPlaceholder[] placeholders)
    {
        RequirePresentationDocument(document);
        if (name == null || name.Length == 0)
        {
            throw new OdfEditException(
                "A presentation layout requires a name.");
        }
        if (placeholders == null)
        {
            throw new OdfEditException(
                "A presentation layout requires placeholders.");
        }
        RequireUniqueLayoutName(document, name);
        OdfNode[] content = new OdfNode[placeholders.Length];
        int index = 0;
        while (index < placeholders.Length)
        {
            if (placeholders[index] == null)
            {
                throw new OdfEditException(
                    "A presentation layout placeholder cannot be null.");
            }
            content[index] = CreateLayoutPlaceholder(
                placeholders[index]);
            index = index + 1;
        }
        OdfStyleResource layout = new OdfStyleResource();
        layout.Kind = OdfStyleResourceKind.PresentationLayout;
        layout.Name = name;
        layout.Content = content;
        AppendStyleResource(document, layout);
        ReloadStyleResources(document);
        return FindLayoutByName(document, name);
    }

    internal static void RemovePresentationLayout(
        OdfDocument document,
        OdfStyleResource layout)
    {
        RequireOwnedLayout(document, layout);
        string name = layout.Name;
        int index = 0;
        while (index < document.Slides.Length)
        {
            OdfSlide slide = document.Slides[index];
            if (slide.PresentationPageLayoutName == name)
            {
                slide.PresentationPageLayoutName = "";
                slide.PresentationPageLayout = null;
            }
            if (slide.Notes != null
                && slide.Notes.PresentationPageLayoutName == name)
            {
                slide.Notes.PresentationPageLayoutName = "";
                slide.Notes.PresentationPageLayout = null;
            }
            index = index + 1;
        }
        index = 0;
        while (index < document.MasterPages.Length)
        {
            OdfMasterPage master = document.MasterPages[index];
            if (master.PresentationPageLayoutName == name)
            {
                master.PresentationPageLayoutName = "";
                master.PresentationPageLayout = null;
            }
            index = index + 1;
        }
        if (document.PresentationHandout != null
            && document.PresentationHandout
                .PresentationPageLayoutName == name)
        {
            document.PresentationHandout.PresentationPageLayoutName = "";
            document.PresentationHandout.PresentationPageLayout = null;
        }
        PresentationDocumentModel.SynchronizePresentation(document);
        List<OdfStyleResource> retained =
            new List<OdfStyleResource>();
        index = 0;
        while (index < document.StyleResources.Length)
        {
            if (document.StyleResources[index] != layout)
            {
                retained.Add(document.StyleResources[index]);
            }
            index = index + 1;
        }
        document.StyleResources = retained.ToArray();
        ReloadStyleResources(document);
    }

    internal static OdfSlide AssignSlideLayout(
        OdfDocument document,
        OdfSlide slide,
        OdfStyleResource layout)
    {
        PresentationSlideEditingModel.RequireOwnedSlide(document, slide);
        RequireOwnedLayout(document, layout);
        OdfNode node = slide.Node ?? new OdfNode();
        slide.PresentationPageLayoutName = layout.Name;
        slide.PresentationPageLayout = layout;
        SynchronizePresentationAndReload(document);
        return PresentationSlideEditingModel.RequireSlideByNode(
            document,
            node);
    }

    internal static OdfPresentationNotes AssignNotesLayout(
        OdfDocument document,
        OdfPresentationNotes notes,
        OdfStyleResource layout)
    {
        RequireOwnedNotes(document, notes);
        RequireOwnedLayout(document, layout);
        OdfNode node = notes.Node ?? new OdfNode();
        notes.PresentationPageLayoutName = layout.Name;
        notes.PresentationPageLayout = layout;
        SynchronizePresentationAndReload(document);
        return FindNotesByNode(document, node);
    }

    internal static OdfPresentationShow[] GetPresentationShows(
        OdfDocument document)
    {
        RequirePresentationDocument(document);
        return (OdfPresentationShow[])document.PresentationShows.Clone();
    }

    internal static OdfPresentationShow CreatePresentationShow(
        OdfDocument document,
        string name,
        OdfSlide[] slides)
    {
        RequirePresentationDocument(document);
        if (name == null || name.Length == 0)
        {
            throw new OdfEditException(
                "A custom presentation show requires a name.");
        }
        RequireUniqueShowName(document, name, null);
        ValidateShowSlides(document, slides);
        OdfPresentationShow show = new OdfPresentationShow();
        show.Name = name;
        show.Slides = (OdfSlide[])slides.Clone();
        AppendShow(document, show);
        SynchronizePresentationAndReload(document);
        return FindShowByName(document, name);
    }

    internal static OdfPresentationShow SetPresentationShowSlides(
        OdfDocument document,
        OdfPresentationShow show,
        OdfSlide[] slides)
    {
        RequireOwnedShow(document, show);
        ValidateShowSlides(document, slides);
        string name = show.Name;
        show.Slides = (OdfSlide[])slides.Clone();
        SynchronizePresentationAndReload(document);
        return FindShowByName(document, name);
    }

    internal static void RemovePresentationShow(
        OdfDocument document,
        OdfPresentationShow show)
    {
        RequireOwnedShow(document, show);
        List<OdfPresentationShow> retained =
            new List<OdfPresentationShow>();
        int index = 0;
        while (index < document.PresentationShows.Length)
        {
            if (document.PresentationShows[index] != show)
            {
                retained.Add(document.PresentationShows[index]);
            }
            index = index + 1;
        }
        document.PresentationShows = retained.ToArray();
        if (document.PresentationSettings.SelectedShow == show
            || document.PresentationSettings.ShowName == show.Name)
        {
            document.PresentationSettings.SelectedShow = null;
            document.PresentationSettings.ShowName = "";
        }
        SynchronizePresentationAndReload(document);
    }

    internal static OdfPresentationSettings SetPresentationSettings(
        OdfDocument document,
        OdfPresentationSettings settings)
    {
        RequirePresentationDocument(document);
        if (settings == null)
        {
            throw new OdfEditException(
                "Presentation playback settings are required.");
        }
        if (settings.StartSlide != null)
        {
            PresentationSlideEditingModel.RequireOwnedSlide(
                document,
                settings.StartSlide);
        }
        if (settings.SelectedShow != null)
        {
            RequireOwnedShow(document, settings.SelectedShow);
        }
        CopySettings(settings, document.PresentationSettings);
        SynchronizePresentationAndReload(document);
        return document.PresentationSettings;
    }

    internal static OdfSlideTransition SetSlideTransition(
        OdfDocument document,
        OdfSlide slide,
        OdfSlideTransition transition)
    {
        PresentationSlideEditingModel.RequireOwnedSlide(document, slide);
        if (transition == null)
        {
            throw new OdfEditException(
                "A transition definition is required.");
        }
        OdfNode slideNode = slide.Node ?? new OdfNode();
        OdfStyle style = CreateTransitionStyle(
            document,
            slide,
            transition,
            false);
        slide.StyleName = style.Name;
        SynchronizeStylesAndPresentation(document);
        OdfSlide refreshed = PresentationSlideEditingModel.RequireSlideByNode(
            document,
            slideNode);
        return refreshed.Transition ?? throw new OdfEditException(
            "The assigned transition could not be materialized.");
    }

    internal static void RemoveSlideTransition(
        OdfDocument document,
        OdfSlide slide)
    {
        PresentationSlideEditingModel.RequireOwnedSlide(document, slide);
        if (slide.Transition == null)
        {
            return;
        }
        OdfStyle style = CreateTransitionStyle(
            document,
            slide,
            new OdfSlideTransition(),
            true);
        slide.StyleName = style.Name;
        SynchronizeStylesAndPresentation(document);
    }

    internal static OdfAnimation[] GetAnimations(OdfDocument document)
    {
        RequirePresentationDocument(document);
        return (OdfAnimation[])document.Animations.Clone();
    }

    internal static OdfAnimation CreateAnimation(
        OdfDocument document,
        OdfSlide slide,
        OdfAnimation? parent,
        OdfAnimationKind kind)
    {
        PresentationSlideEditingModel.RequireOwnedSlide(document, slide);
        string name = AnimationDocumentModel.AnimationName(kind);
        if (name.Length == 0)
        {
            throw new OdfEditException(
                "A recognized animation kind is required.");
        }
        OdfNode container = slide.Node ?? new OdfNode();
        if (parent != null)
        {
            RequireOwnedAnimation(document, parent);
            if (parent.Slide != slide
                || parent.Node == null
                || !IsTimingContainer(parent.Kind))
            {
                throw new OdfEditException(
                    "A nested animation requires a timing-container parent on the same slide.");
            }
            container = parent.Node;
        }
        OdfNode node = CreateElement(
            OdfNamespaces.Animation,
            name,
            OdfNamespaces.AnimationPrefix,
            container.Location.PartPath);
        StyleDocumentModel.AppendNode(container, node);
        PresentationDocumentModel.LoadPresentation(document);
        return FindAnimationByNode(document, node);
    }

    internal static void RemoveAnimation(
        OdfDocument document,
        OdfAnimation animation)
    {
        RequireOwnedAnimation(document, animation);
        PresentationDocumentModel.SynchronizePresentation(document);
        OdfNode node = animation.Node ?? new OdfNode();
        StyleDocumentModel.RemoveNodeFromParent(node);
        PresentationDocumentModel.LoadPresentation(document);
    }

    internal static void ApplyPresentationChanges(OdfDocument document)
    {
        RequirePresentationDocument(document);
        StyleResourceDocumentModel.SynchronizeStyleResources(document);
        StyleDocumentModel.SynchronizeStyles(document);
        PresentationDocumentModel.SynchronizePresentation(document);
        TextDocumentModel.SynchronizeTextBlocks(document);
        DrawingDocumentModel.SynchronizeDrawings(
            document,
            document.SaveOptions);
        StyleDocumentModel.LoadStyles(document);
        StyleResourceDocumentModel.LoadStyleResources(document);
        PresentationSlideEditingModel.RefreshPresentation(document);
    }

    static void SynchronizePresentationAndReload(OdfDocument document)
    {
        PresentationDocumentModel.SynchronizePresentation(document);
        DrawingDocumentModel.SynchronizeDrawings(
            document,
            document.SaveOptions);
        PresentationDocumentModel.LoadPresentation(document);
    }

    static void ReloadStyleResources(OdfDocument document)
    {
        StyleResourceDocumentModel.SynchronizeStyleResources(document);
        DrawingDocumentModel.SynchronizeDrawings(
            document,
            document.SaveOptions);
        StyleResourceDocumentModel.LoadStyleResources(document);
        PresentationDocumentModel.LoadPresentation(document);
    }

    static void SynchronizeStylesAndPresentation(OdfDocument document)
    {
        StyleDocumentModel.SynchronizeStyles(document);
        PresentationDocumentModel.SynchronizePresentation(document);
        DrawingDocumentModel.SynchronizeDrawings(
            document,
            document.SaveOptions);
        StyleDocumentModel.LoadStyles(document);
        PresentationDocumentModel.LoadPresentation(document);
    }

    static OdfStyle CreateTransitionStyle(
        OdfDocument document,
        OdfSlide slide,
        OdfSlideTransition transition,
        bool remove)
    {
        OdfStyle? source = FindDrawingPageStyle(
            document,
            slide.StyleName);
        List<OdfStyleProperty> properties =
            new List<OdfStyleProperty>();
        List<OdfNode> children = new List<OdfNode>();
        if (source != null)
        {
            int index = 0;
            while (index < source.DrawingPageProperties.Length)
            {
                OdfStyleProperty property =
                    source.DrawingPageProperties[index];
                if (!IsTransitionProperty(property))
                {
                    properties.Add(property);
                }
                index = index + 1;
            }
            index = 0;
            while (index < source.DrawingPagePropertyChildren.Length)
            {
                OdfNode child = source.DrawingPagePropertyChildren[index];
                if (!IsElement(
                    child,
                    OdfNamespaces.Presentation,
                    "sound"))
                {
                    children.Add(child);
                }
                index = index + 1;
            }
        }
        if (!remove)
        {
            AddTransitionProperties(properties, transition);
            OdfNode? sound = CreateTransitionSound(
                transition,
                slide.Node == null ? "" : slide.Node.Location.PartPath);
            if (sound != null)
            {
                children.Add(sound);
            }
        }
        OdfStyle definition = new OdfStyle();
        definition.Name = UniqueTransitionStyleName(document);
        definition.Family = OdfStyleFamily.DrawingPage;
        definition.IsAutomatic = true;
        definition.PartPath = slide.Node == null
            ? ""
            : slide.Node.Location.PartPath;
        definition.DrawingPageProperties = properties.ToArray();
        definition.DrawingPagePropertyChildren = children.ToArray();
        return StyleMaintenanceModel.CreateStyle(document, definition);
    }

    static void AddTransitionProperties(
        List<OdfStyleProperty> properties,
        OdfSlideTransition transition)
    {
        string type = transition.Type;
        if (type.Length == 0)
        {
            type = PresentationShowModel.FormatTransitionKind(
                transition.Kind);
        }
        AddProperty(properties, OdfNamespaces.Smil, "type", OdfNamespaces.SmilPrefix, type);
        AddProperty(properties, OdfNamespaces.Smil, "subtype", OdfNamespaces.SmilPrefix, transition.Subtype);
        AddProperty(properties, OdfNamespaces.Smil, "direction", OdfNamespaces.SmilPrefix, transition.Direction);
        AddProperty(properties, OdfNamespaces.Smil, "fadeColor", OdfNamespaces.SmilPrefix, transition.FadeColor);
        AddProperty(properties, OdfNamespaces.Presentation, "transition-type", OdfNamespaces.PresentationPrefix, transition.TransitionType);
        AddProperty(properties, OdfNamespaces.Presentation, "transition-style", OdfNamespaces.PresentationPrefix, transition.TransitionStyle);
        AddProperty(properties, OdfNamespaces.Presentation, "transition-speed", OdfNamespaces.PresentationPrefix, transition.Speed);
        AddProperty(properties, OdfNamespaces.Presentation, "duration", OdfNamespaces.PresentationPrefix, transition.Duration);
    }

    static void AddProperty(
        List<OdfStyleProperty> properties,
        string namespaceName,
        string localName,
        string prefix,
        string value)
    {
        if (value.Length == 0)
        {
            return;
        }
        OdfStyleProperty property = new OdfStyleProperty();
        property.Name.NamespaceName = namespaceName;
        property.Name.LocalName = localName;
        property.Name.Prefix = prefix;
        property.LexicalValue = value;
        properties.Add(property);
    }

    static OdfNode? CreateTransitionSound(
        OdfSlideTransition transition,
        string partPath)
    {
        if (transition.SoundHref.Length == 0
            && transition.SoundType.Length == 0
            && transition.SoundShow.Length == 0
            && transition.SoundActuate.Length == 0
            && transition.SoundPlayFullValue.Length == 0
            && !transition.SoundPlayFull)
        {
            return null;
        }
        OdfNode sound = CreateElement(
            OdfNamespaces.Presentation,
            "sound",
            OdfNamespaces.PresentationPrefix,
            partPath);
        WriteAttribute(sound, OdfNamespaces.XLink, "href", OdfNamespaces.XLinkPrefix, transition.SoundHref);
        WriteAttribute(sound, OdfNamespaces.XLink, "type", OdfNamespaces.XLinkPrefix, transition.SoundType);
        WriteAttribute(sound, OdfNamespaces.XLink, "show", OdfNamespaces.XLinkPrefix, transition.SoundShow);
        WriteAttribute(sound, OdfNamespaces.XLink, "actuate", OdfNamespaces.XLinkPrefix, transition.SoundActuate);
        string playFull = transition.SoundPlayFullValue;
        if (playFull.Length == 0 && transition.SoundPlayFull)
        {
            playFull = "true";
        }
        WriteAttribute(sound, OdfNamespaces.Presentation, "play-full", OdfNamespaces.PresentationPrefix, playFull);
        return sound;
    }

    static bool IsTransitionProperty(OdfStyleProperty property)
    {
        string namespaceName = property.Name.NamespaceName;
        string localName = property.Name.LocalName;
        if (namespaceName == OdfNamespaces.Smil)
        {
            return localName == "type"
                || localName == "subtype"
                || localName == "direction"
                || localName == "fadeColor";
        }
        return namespaceName == OdfNamespaces.Presentation
            && (localName == "transition-type"
                || localName == "transition-style"
                || localName == "transition-speed"
                || localName == "duration");
    }

    static OdfStyle? FindDrawingPageStyle(
        OdfDocument document,
        string name)
    {
        int index = 0;
        while (index < document.Styles.Length)
        {
            OdfStyle style = document.Styles[index];
            if (style.Name == name
                && style.Family == OdfStyleFamily.DrawingPage)
            {
                return style;
            }
            index = index + 1;
        }
        return null;
    }

    static string UniqueTransitionStyleName(OdfDocument document)
    {
        string result = "PresentationTransition";
        int number = 2;
        while (FindDrawingPageStyle(document, result) != null)
        {
            result = "PresentationTransition" + number.ToString(
                global::System.Globalization.CultureInfo.InvariantCulture);
            number = number + 1;
        }
        return result;
    }

    static OdfNode CreateLayoutPlaceholder(
        OdfPresentationPlaceholder placeholder)
    {
        OdfNode result = CreateElement(
            OdfNamespaces.Presentation,
            "placeholder",
            OdfNamespaces.PresentationPrefix,
            "");
        WriteAttribute(result, OdfNamespaces.Presentation, "object", OdfNamespaces.PresentationPrefix, placeholder.ObjectName);
        WriteAttribute(result, OdfNamespaces.Svg, "x", OdfNamespaces.SvgPrefix, placeholder.X);
        WriteAttribute(result, OdfNamespaces.Svg, "y", OdfNamespaces.SvgPrefix, placeholder.Y);
        WriteAttribute(result, OdfNamespaces.Svg, "width", OdfNamespaces.SvgPrefix, placeholder.Width);
        WriteAttribute(result, OdfNamespaces.Svg, "height", OdfNamespaces.SvgPrefix, placeholder.Height);
        return result;
    }

    static void WriteAttribute(
        OdfNode node,
        string namespaceName,
        string localName,
        string prefix,
        string value)
    {
        if (value.Length > 0)
        {
            StyleDocumentModel.WriteAttribute(
                node,
                namespaceName,
                localName,
                prefix,
                value);
        }
    }

    static void CopySettings(
        OdfPresentationSettings source,
        OdfPresentationSettings target)
    {
        target.StartPage = source.StartPage;
        target.ShowName = source.ShowName;
        target.FullScreenValue = source.FullScreenValue;
        target.FullScreen = source.FullScreen;
        target.EndlessValue = source.EndlessValue;
        target.Endless = source.Endless;
        target.Pause = source.Pause;
        target.ShowLogoValue = source.ShowLogoValue;
        target.ShowLogo = source.ShowLogo;
        target.ForceManualValue = source.ForceManualValue;
        target.ForceManual = source.ForceManual;
        target.MouseVisibleValue = source.MouseVisibleValue;
        target.MouseVisible = source.MouseVisible;
        target.MouseAsPenValue = source.MouseAsPenValue;
        target.MouseAsPen = source.MouseAsPen;
        target.StartWithNavigatorValue = source.StartWithNavigatorValue;
        target.StartWithNavigator = source.StartWithNavigator;
        target.AnimationsValue = source.AnimationsValue;
        target.AnimationsEnabled = source.AnimationsEnabled;
        target.TransitionOnClickValue = source.TransitionOnClickValue;
        target.TransitionOnClickEnabled = source.TransitionOnClickEnabled;
        target.StayOnTopValue = source.StayOnTopValue;
        target.StayOnTop = source.StayOnTop;
        target.ShowEndSlideValue = source.ShowEndSlideValue;
        target.ShowEndSlide = source.ShowEndSlide;
        target.StartSlide = source.StartSlide;
        target.SelectedShow = source.SelectedShow;
    }

    static void ValidateShowSlides(
        OdfDocument document,
        OdfSlide[] slides)
    {
        if (slides == null)
        {
            throw new OdfEditException(
                "A custom show slide collection cannot be null.");
        }
        int index = 0;
        while (index < slides.Length)
        {
            PresentationSlideEditingModel.RequireOwnedSlide(
                document,
                slides[index]);
            index = index + 1;
        }
    }

    static void RequireUniqueShowName(
        OdfDocument document,
        string name,
        OdfPresentationShow? excluded)
    {
        int index = 0;
        while (index < document.PresentationShows.Length)
        {
            OdfPresentationShow candidate =
                document.PresentationShows[index];
            if (candidate != excluded && candidate.Name == name)
            {
                throw new OdfEditException(
                    "A custom presentation show already uses the name.");
            }
            index = index + 1;
        }
    }

    static void RequireUniqueLayoutName(
        OdfDocument document,
        string name)
    {
        int index = 0;
        while (index < document.StyleResources.Length)
        {
            OdfStyleResource resource = document.StyleResources[index];
            if (resource.Kind == OdfStyleResourceKind.PresentationLayout
                && resource.Name == name)
            {
                throw new OdfEditException(
                    "A presentation layout already uses the name.");
            }
            index = index + 1;
        }
    }

    static void RequireOwnedLayout(
        OdfDocument document,
        OdfStyleResource layout)
    {
        RequirePresentationDocument(document);
        int index = 0;
        while (index < document.StyleResources.Length)
        {
            if (document.StyleResources[index] == layout
                && layout.Kind
                    == OdfStyleResourceKind.PresentationLayout)
            {
                return;
            }
            index = index + 1;
        }
        throw new OdfEditException(
            "The presentation layout does not belong to the document.");
    }

    static void RequireOwnedNotes(
        OdfDocument document,
        OdfPresentationNotes notes)
    {
        RequirePresentationDocument(document);
        int index = 0;
        while (index < document.Slides.Length)
        {
            if (document.Slides[index].Notes == notes)
            {
                return;
            }
            index = index + 1;
        }
        throw new OdfEditException(
            "The notes page does not belong to the document.");
    }

    static void RequireOwnedShow(
        OdfDocument document,
        OdfPresentationShow show)
    {
        RequirePresentationDocument(document);
        int index = 0;
        while (index < document.PresentationShows.Length)
        {
            if (document.PresentationShows[index] == show)
            {
                return;
            }
            index = index + 1;
        }
        throw new OdfEditException(
            "The custom show does not belong to the document.");
    }

    static void RequireOwnedAnimation(
        OdfDocument document,
        OdfAnimation animation)
    {
        RequirePresentationDocument(document);
        int index = 0;
        while (index < document.Animations.Length)
        {
            if (document.Animations[index] == animation)
            {
                return;
            }
            index = index + 1;
        }
        throw new OdfEditException(
            "The animation does not belong to the document.");
    }

    static void RequirePresentationDocument(OdfDocument document)
    {
        if (document == null
            || document.Slides == null
            || document.StyleResources == null
            || document.PresentationShows == null
            || document.Animations == null)
        {
            throw new OdfEditException(
                "A materialized presentation document is required.");
        }
        if (document.Kind != OdfDocumentKind.Presentation
            && document.Kind != OdfDocumentKind.PresentationTemplate)
        {
            throw new OdfEditException(
                "Presentation operations require a presentation document.");
        }
    }

    static void AppendStyleResource(
        OdfDocument document,
        OdfStyleResource resource)
    {
        OdfStyleResource[] resources =
            new OdfStyleResource[document.StyleResources.Length + 1];
        Array.Copy(
            document.StyleResources,
            resources,
            document.StyleResources.Length);
        resources[resources.Length - 1] = resource;
        document.StyleResources = resources;
    }

    static void AppendShow(
        OdfDocument document,
        OdfPresentationShow show)
    {
        OdfPresentationShow[] shows =
            new OdfPresentationShow[document.PresentationShows.Length + 1];
        Array.Copy(
            document.PresentationShows,
            shows,
            document.PresentationShows.Length);
        shows[shows.Length - 1] = show;
        document.PresentationShows = shows;
    }

    static OdfStyleResource FindLayoutByName(
        OdfDocument document,
        string name)
    {
        int index = 0;
        while (index < document.StyleResources.Length)
        {
            OdfStyleResource resource = document.StyleResources[index];
            if (resource.Kind == OdfStyleResourceKind.PresentationLayout
                && resource.Name == name)
            {
                return resource;
            }
            index = index + 1;
        }
        throw new OdfEditException(
            "The created presentation layout could not be materialized.");
    }

    static OdfPresentationShow FindShowByName(
        OdfDocument document,
        string name)
    {
        int index = 0;
        while (index < document.PresentationShows.Length)
        {
            if (document.PresentationShows[index].Name == name)
            {
                return document.PresentationShows[index];
            }
            index = index + 1;
        }
        throw new OdfEditException(
            "The custom show could not be materialized.");
    }

    static OdfPresentationNotes FindNotesByNode(
        OdfDocument document,
        OdfNode node)
    {
        int index = 0;
        while (index < document.Slides.Length)
        {
            OdfPresentationNotes? notes = document.Slides[index].Notes;
            if (notes != null && notes.Node == node)
            {
                return notes;
            }
            index = index + 1;
        }
        throw new OdfEditException(
            "The edited notes page could not be materialized.");
    }

    static OdfAnimation FindAnimationByNode(
        OdfDocument document,
        OdfNode node)
    {
        int index = 0;
        while (index < document.Animations.Length)
        {
            if (document.Animations[index].Node == node)
            {
                return document.Animations[index];
            }
            index = index + 1;
        }
        throw new OdfEditException(
            "The created animation could not be materialized.");
    }

    static bool IsTimingContainer(OdfAnimationKind kind)
    {
        return kind == OdfAnimationKind.Sequence
            || kind == OdfAnimationKind.Parallel
            || kind == OdfAnimationKind.Iterate;
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
        result.WasEmptyElement = true;
        return result;
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
}
