using System;
using System.Collections.Generic;

namespace odf;

internal static class PresentationShowModel
{
    internal static void LoadFeatures(
        OdfDocument document,
        OdfNode? presentation)
    {
        document.SlideTransitions = LoadTransitions(document);
        OdfPresentationSettings settings = new OdfPresentationSettings();
        settings.IsFullScreenValid = true;
        settings.IsEndlessValid = true;
        settings.IsPauseValid = true;
        settings.IsShowLogoValid = true;
        settings.IsForceManualValid = true;
        settings.IsMouseVisibleValid = true;
        settings.IsMouseAsPenValid = true;
        settings.IsStartWithNavigatorValid = true;
        settings.IsAnimationsValid = true;
        settings.IsTransitionOnClickValid = true;
        settings.IsStayOnTopValid = true;
        settings.IsShowEndSlideValid = true;
        OdfNode? settingsNode = presentation == null
            ? null
            : FindDirectChild(
                presentation,
                OdfNamespaces.Presentation,
                "settings");
        if (settingsNode != null)
        {
            LoadSettings(settings, settingsNode);
        }
        document.PresentationSettings = settings;
        document.PresentationShows = settingsNode == null
            ? []
            : LoadShows(document, settings, settingsNode);
        ResolveSettings(document, settings);
    }

    internal static void SynchronizeFeatures(
        OdfDocument document,
        OdfNode? presentation)
    {
        if (document.SlideTransitions == null
            || document.PresentationShows == null
            || document.PresentationSettings == null)
        {
            throw new OdfDocumentException(
                "Presentation feature collections are required for writing.");
        }
        SynchronizeTransitions(document.SlideTransitions);
        OdfPresentationSettings settings = document.PresentationSettings;
        if (presentation == null)
        {
            if (SettingsHasContent(settings)
                || document.PresentationShows.Length > 0)
            {
                throw new OdfEditException(
                    "Presentation settings require a presentation body.");
            }
            return;
        }
        if (settings.Node == null
            && (SettingsHasContent(settings)
                || document.PresentationShows.Length > 0))
        {
            settings.Node = CreateElement(
                OdfNamespaces.Presentation,
                "settings",
                OdfNamespaces.PresentationPrefix,
                presentation.Location.PartPath);
            AppendChild(presentation, settings.Node);
        }
        if (settings.Node != null)
        {
            SynchronizeSettings(settings);
            SynchronizeShows(
                document,
                settings,
                document.PresentationShows);
        }
        ResolveSettings(document, settings);
    }

    static OdfSlideTransition[] LoadTransitions(OdfDocument document)
    {
        List<OdfSlideTransition> result =
            new List<OdfSlideTransition>();
        int index = 0;
        while (index < document.Slides.Length)
        {
            OdfSlide slide = document.Slides[index];
            slide.Transition = null;
            OdfStyle? style = FindDrawingPageStyle(
                document,
                slide.StyleName);
            OdfNode? properties = style == null
                ? null
                : style.DrawingPagePropertiesNode;
            if (style != null && properties != null
                && HasTransition(properties))
            {
                OdfSlideTransition transition = CreateTransition(
                    slide,
                    style,
                    properties);
                slide.Transition = transition;
                result.Add(transition);
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    static OdfSlideTransition CreateTransition(
        OdfSlide slide,
        OdfStyle style,
        OdfNode node)
    {
        OdfSlideTransition result = new OdfSlideTransition();
        result.Type = Read(node, OdfNamespaces.Smil, "type");
        result.Subtype = Read(node, OdfNamespaces.Smil, "subtype");
        result.Direction = Read(node, OdfNamespaces.Smil, "direction");
        result.FadeColor = Read(node, OdfNamespaces.Smil, "fadeColor");
        result.TransitionType = Read(
            node,
            OdfNamespaces.Presentation,
            "transition-type");
        result.TransitionStyle = Read(
            node,
            OdfNamespaces.Presentation,
            "transition-style");
        result.Speed = Read(
            node,
            OdfNamespaces.Presentation,
            "transition-speed");
        result.Duration = Read(
            node,
            OdfNamespaces.Presentation,
            "duration");
        result.Kind = ParseTransitionKind(
            result.Type,
            result.TransitionStyle);
        result.IsDurationValid = result.Duration.Length == 0
            || OdfLexicalParser.ParseDuration(result.Duration).IsValid;
        result.IsRehearsalTiming = result.TransitionType == "automatic"
            && result.Duration.Length > 0
            && result.IsDurationValid;
        OdfNode? sound = FindDirectChild(
            node,
            OdfNamespaces.Presentation,
            "sound");
        if (sound != null)
        {
            result.SoundHref = Read(sound, OdfNamespaces.XLink, "href");
            result.SoundType = Read(sound, OdfNamespaces.XLink, "type");
            result.SoundShow = Read(sound, OdfNamespaces.XLink, "show");
            result.SoundActuate = Read(
                sound,
                OdfNamespaces.XLink,
                "actuate");
            result.SoundPlayFullValue = Read(
                sound,
                OdfNamespaces.Presentation,
                "play-full");
            result.SoundNode = sound;
        }
        RefreshTransitionSound(result);
        result.Slide = slide;
        result.Style = style;
        result.Node = node;
        CaptureTransition(result);
        return result;
    }

    static bool HasTransition(OdfNode node)
    {
        return Read(node, OdfNamespaces.Smil, "type").Length > 0
            || Read(node, OdfNamespaces.Smil, "subtype").Length > 0
            || Read(node, OdfNamespaces.Smil, "direction").Length > 0
            || Read(node, OdfNamespaces.Smil, "fadeColor").Length > 0
            || Read(node, OdfNamespaces.Presentation, "transition-type")
                .Length > 0
            || Read(node, OdfNamespaces.Presentation, "transition-style")
                .Length > 0
            || Read(node, OdfNamespaces.Presentation, "transition-speed")
                .Length > 0
            || Read(node, OdfNamespaces.Presentation, "duration")
                .Length > 0
            || FindDirectChild(
                node,
                OdfNamespaces.Presentation,
                "sound") != null;
    }

    static OdfStyle? FindDrawingPageStyle(
        OdfDocument document,
        string name)
    {
        OdfStyle? result = null;
        int matches = 0;
        int index = 0;
        while (index < document.Styles.Length)
        {
            OdfStyle candidate = document.Styles[index];
            if (candidate.Name == name
                && candidate.Family == OdfStyleFamily.DrawingPage)
            {
                result = candidate;
                matches = matches + 1;
            }
            index = index + 1;
        }
        return matches == 1 ? result : null;
    }

    static void SynchronizeTransitions(OdfSlideTransition[] transitions)
    {
        int index = 0;
        while (index < transitions.Length)
        {
            OdfSlideTransition item = transitions[index];
            if (item == null || item.Node == null)
            {
                throw new OdfEditException(
                    "A slide transition requires drawing-page properties.");
            }
            if (item.Kind != item.OriginalKind
                && item.Type == item.OriginalType)
            {
                item.Type = FormatTransitionKind(item.Kind);
            }
            WriteChanged(item.Node, OdfNamespaces.Smil, "type", OdfNamespaces.SmilPrefix, item.Type, item.OriginalType);
            WriteChanged(item.Node, OdfNamespaces.Smil, "subtype", OdfNamespaces.SmilPrefix, item.Subtype, item.OriginalSubtype);
            WriteChanged(item.Node, OdfNamespaces.Smil, "direction", OdfNamespaces.SmilPrefix, item.Direction, item.OriginalDirection);
            WriteChanged(item.Node, OdfNamespaces.Smil, "fadeColor", OdfNamespaces.SmilPrefix, item.FadeColor, item.OriginalFadeColor);
            WriteChanged(item.Node, OdfNamespaces.Presentation, "transition-type", OdfNamespaces.PresentationPrefix, item.TransitionType, item.OriginalTransitionType);
            WriteChanged(item.Node, OdfNamespaces.Presentation, "transition-style", OdfNamespaces.PresentationPrefix, item.TransitionStyle, item.OriginalTransitionStyle);
            WriteChanged(item.Node, OdfNamespaces.Presentation, "transition-speed", OdfNamespaces.PresentationPrefix, item.Speed, item.OriginalSpeed);
            WriteChanged(item.Node, OdfNamespaces.Presentation, "duration", OdfNamespaces.PresentationPrefix, item.Duration, item.OriginalDuration);
            SynchronizeTransitionSound(item);
            item.Kind = ParseTransitionKind(
                item.Type,
                item.TransitionStyle);
            item.IsDurationValid = item.Duration.Length == 0
                || OdfLexicalParser.ParseDuration(item.Duration).IsValid;
            item.IsRehearsalTiming =
                item.TransitionType == "automatic"
                && item.Duration.Length > 0
                && item.IsDurationValid;
            CaptureTransition(item);
            index = index + 1;
        }
    }

    static void SynchronizeTransitionSound(OdfSlideTransition item)
    {
        OdfNode? sound = item.SoundNode;
        if (sound == null && (item.SoundHref.Length > 0
                || item.SoundType.Length > 0
                || item.SoundShow.Length > 0
                || item.SoundActuate.Length > 0
                || item.SoundPlayFullValue.Length > 0
                || item.SoundPlayFull))
        {
            OdfNode parent = item.Node ?? new OdfNode();
            sound = CreateElement(
                OdfNamespaces.Presentation,
                "sound",
                OdfNamespaces.PresentationPrefix,
                parent.Location.PartPath);
            AppendChild(parent, sound);
            item.SoundNode = sound;
        }
        if (sound == null)
        {
            return;
        }
        WriteChanged(sound, OdfNamespaces.XLink, "href", OdfNamespaces.XLinkPrefix, item.SoundHref, item.OriginalSoundHref);
        WriteChanged(sound, OdfNamespaces.XLink, "type", OdfNamespaces.XLinkPrefix, item.SoundType, item.OriginalSoundType);
        WriteChanged(sound, OdfNamespaces.XLink, "show", OdfNamespaces.XLinkPrefix, item.SoundShow, item.OriginalSoundShow);
        WriteChanged(sound, OdfNamespaces.XLink, "actuate", OdfNamespaces.XLinkPrefix, item.SoundActuate, item.OriginalSoundActuate);
        string playFullValue = BooleanValue(
            item.SoundPlayFullValue,
            item.SoundPlayFull,
            item.OriginalSoundPlayFullValue);
        item.SoundPlayFullValue = playFullValue;
        WriteChanged(sound, OdfNamespaces.Presentation, "play-full", OdfNamespaces.PresentationPrefix, playFullValue, item.OriginalSoundPlayFullValue);
        RefreshTransitionSound(item);
    }

    static void RefreshTransitionSound(OdfSlideTransition item)
    {
        OdfLexicalValue playFull = OdfLexicalParser.ParseBoolean(
            item.SoundPlayFullValue);
        item.IsSoundPlayFullValid =
            item.SoundPlayFullValue.Length == 0 || playFull.IsValid;
        item.SoundPlayFull = playFull.IsValid
            && playFull.BooleanValue;
    }

    static void CaptureTransition(OdfSlideTransition item)
    {
        item.OriginalKind = item.Kind;
        item.OriginalType = item.Type;
        item.OriginalSubtype = item.Subtype;
        item.OriginalDirection = item.Direction;
        item.OriginalFadeColor = item.FadeColor;
        item.OriginalTransitionType = item.TransitionType;
        item.OriginalTransitionStyle = item.TransitionStyle;
        item.OriginalSpeed = item.Speed;
        item.OriginalDuration = item.Duration;
        item.OriginalSoundHref = item.SoundHref;
        item.OriginalSoundType = item.SoundType;
        item.OriginalSoundShow = item.SoundShow;
        item.OriginalSoundActuate = item.SoundActuate;
        item.OriginalSoundPlayFullValue = item.SoundPlayFullValue;
    }

    static void LoadSettings(
        OdfPresentationSettings settings,
        OdfNode node)
    {
        settings.StartPage = ReadPresentation(node, "start-page");
        settings.ShowName = ReadPresentation(node, "show");
        settings.FullScreenValue = ReadPresentation(node, "full-screen");
        settings.EndlessValue = ReadPresentation(node, "endless");
        settings.Pause = ReadPresentation(node, "pause");
        settings.ShowLogoValue = ReadPresentation(node, "show-logo");
        settings.ForceManualValue = ReadPresentation(node, "force-manual");
        settings.MouseVisibleValue = ReadPresentation(node, "mouse-visible");
        settings.MouseAsPenValue = ReadPresentation(node, "mouse-as-pen");
        settings.StartWithNavigatorValue = ReadPresentation(
            node,
            "start-with-navigator");
        settings.AnimationsValue = ReadPresentation(node, "animations");
        settings.TransitionOnClickValue = ReadPresentation(
            node,
            "transition-on-click");
        settings.StayOnTopValue = ReadPresentation(node, "stay-on-top");
        settings.ShowEndSlideValue = ReadPresentation(
            node,
            "show-end-of-presentation-slide");
        settings.Node = node;
        RefreshSettings(settings);
        CaptureSettings(settings);
    }

    static OdfPresentationShow[] LoadShows(
        OdfDocument document,
        OdfPresentationSettings settings,
        OdfNode settingsNode)
    {
        List<OdfPresentationShow> result =
            new List<OdfPresentationShow>();
        List<OdfNode> originalNodes = new List<OdfNode>();
        int index = 0;
        while (index < settingsNode.Children.Length)
        {
            OdfNode node = settingsNode.Children[index];
            if (IsElement(node, OdfNamespaces.Presentation, "show"))
            {
                OdfPresentationShow show = new OdfPresentationShow();
                show.Name = ReadPresentation(node, "name");
                show.Pages = ReadPresentation(node, "pages");
                show.SlideNames = ParsePageNames(show.Pages);
                show.Node = node;
                ResolveShow(document, show);
                CaptureShow(show);
                result.Add(show);
                originalNodes.Add(node);
            }
            index = index + 1;
        }
        settings.OriginalShowNodes = originalNodes.ToArray();
        return result.ToArray();
    }

    static void SynchronizeSettings(OdfPresentationSettings settings)
    {
        OdfNode node = settings.Node ?? new OdfNode();
        if (settings.StartSlide != settings.OriginalStartSlide)
        {
            settings.StartPage = settings.StartSlide == null
                ? ""
                : settings.StartSlide.Name;
        }
        if (settings.SelectedShow != settings.OriginalSelectedShow)
        {
            settings.ShowName = settings.SelectedShow == null
                ? ""
                : settings.SelectedShow.Name;
        }
        else if (settings.SelectedShow != null
            && settings.SelectedShow.Name
                != settings.SelectedShow.OriginalName)
        {
            settings.ShowName = settings.SelectedShow.Name;
        }
        settings.FullScreenValue = BooleanValue(settings.FullScreenValue, settings.FullScreen, settings.OriginalFullScreenValue);
        settings.EndlessValue = BooleanValue(settings.EndlessValue, settings.Endless, settings.OriginalEndlessValue);
        settings.ShowLogoValue = BooleanValue(settings.ShowLogoValue, settings.ShowLogo, settings.OriginalShowLogoValue);
        settings.ForceManualValue = BooleanValue(settings.ForceManualValue, settings.ForceManual, settings.OriginalForceManualValue);
        settings.MouseVisibleValue = BooleanValue(settings.MouseVisibleValue, settings.MouseVisible, settings.OriginalMouseVisibleValue);
        settings.MouseAsPenValue = BooleanValue(settings.MouseAsPenValue, settings.MouseAsPen, settings.OriginalMouseAsPenValue);
        settings.StartWithNavigatorValue = BooleanValue(settings.StartWithNavigatorValue, settings.StartWithNavigator, settings.OriginalStartWithNavigatorValue);
        settings.AnimationsValue = EnabledValue(settings.AnimationsValue, settings.AnimationsEnabled, settings.OriginalAnimationsValue);
        settings.TransitionOnClickValue = EnabledValue(settings.TransitionOnClickValue, settings.TransitionOnClickEnabled, settings.OriginalTransitionOnClickValue);
        settings.StayOnTopValue = BooleanValue(settings.StayOnTopValue, settings.StayOnTop, settings.OriginalStayOnTopValue);
        settings.ShowEndSlideValue = BooleanValue(settings.ShowEndSlideValue, settings.ShowEndSlide, settings.OriginalShowEndSlideValue);
        WritePresentationChanged(node, "start-page", settings.StartPage, settings.OriginalStartPage);
        WritePresentationChanged(node, "show", settings.ShowName, settings.OriginalShowName);
        WritePresentationChanged(node, "full-screen", settings.FullScreenValue, settings.OriginalFullScreenValue);
        WritePresentationChanged(node, "endless", settings.EndlessValue, settings.OriginalEndlessValue);
        WritePresentationChanged(node, "pause", settings.Pause, settings.OriginalPause);
        WritePresentationChanged(node, "show-logo", settings.ShowLogoValue, settings.OriginalShowLogoValue);
        WritePresentationChanged(node, "force-manual", settings.ForceManualValue, settings.OriginalForceManualValue);
        WritePresentationChanged(node, "mouse-visible", settings.MouseVisibleValue, settings.OriginalMouseVisibleValue);
        WritePresentationChanged(node, "mouse-as-pen", settings.MouseAsPenValue, settings.OriginalMouseAsPenValue);
        WritePresentationChanged(node, "start-with-navigator", settings.StartWithNavigatorValue, settings.OriginalStartWithNavigatorValue);
        WritePresentationChanged(node, "animations", settings.AnimationsValue, settings.OriginalAnimationsValue);
        WritePresentationChanged(node, "transition-on-click", settings.TransitionOnClickValue, settings.OriginalTransitionOnClickValue);
        WritePresentationChanged(node, "stay-on-top", settings.StayOnTopValue, settings.OriginalStayOnTopValue);
        WritePresentationChanged(node, "show-end-of-presentation-slide", settings.ShowEndSlideValue, settings.OriginalShowEndSlideValue);
        RefreshSettings(settings);
        CaptureSettings(settings);
    }

    static void SynchronizeShows(
        OdfDocument document,
        OdfPresentationSettings settings,
        OdfPresentationShow[] shows)
    {
        OdfNode parent = settings.Node ?? new OdfNode();
        int index = 0;
        while (index < settings.OriginalShowNodes.Length)
        {
            if (!ShowRetainsNode(
                shows,
                settings.OriginalShowNodes[index]))
            {
                RemoveChild(parent, settings.OriginalShowNodes[index]);
            }
            index = index + 1;
        }
        List<OdfNode> currentNodes = new List<OdfNode>();
        index = 0;
        while (index < shows.Length)
        {
            OdfPresentationShow show = shows[index];
            if (show == null)
            {
                throw new OdfEditException(
                    "A custom-show collection cannot contain null values.");
            }
            if (show.Node == null)
            {
                show.Node = CreateElement(
                    OdfNamespaces.Presentation,
                    "show",
                    OdfNamespaces.PresentationPrefix,
                    parent.Location.PartPath);
                AppendChild(parent, show.Node);
            }
            if (!SlideArraysMatch(show.Slides, show.OriginalSlides))
            {
                show.SlideNames = NamesFromSlides(show.Slides);
                show.Pages = JoinPageNames(show.SlideNames);
            }
            else if (!StringArraysMatch(
                show.SlideNames,
                show.OriginalSlideNames))
            {
                show.Pages = JoinPageNames(show.SlideNames);
            }
            WritePresentationChanged(show.Node, "name", show.Name, show.OriginalName);
            WritePresentationChanged(show.Node, "pages", show.Pages, show.OriginalPages);
            show.SlideNames = ParsePageNames(show.Pages);
            ResolveShow(document, show);
            CaptureShow(show);
            currentNodes.Add(show.Node);
            index = index + 1;
        }
        settings.OriginalShowNodes = currentNodes.ToArray();
    }

    static void ResolveSettings(
        OdfDocument document,
        OdfPresentationSettings settings)
    {
        settings.StartSlide = FindSlide(document, settings.StartPage);
        settings.SelectedShow = FindShow(document, settings.ShowName);
        settings.OriginalStartSlide = settings.StartSlide;
        settings.OriginalSelectedShow = settings.SelectedShow;
    }

    static void ResolveShow(
        OdfDocument document,
        OdfPresentationShow show)
    {
        List<OdfSlide> slides = new List<OdfSlide>();
        bool valid = show.SlideNames.Length > 0;
        int index = 0;
        while (index < show.SlideNames.Length)
        {
            OdfSlide? slide = FindSlide(
                document,
                show.SlideNames[index]);
            if (slide == null)
            {
                valid = false;
            }
            else
            {
                slides.Add(slide);
            }
            index = index + 1;
        }
        show.Slides = slides.ToArray();
        show.IsPageListValid = valid
            && show.Slides.Length == show.SlideNames.Length;
    }

    static OdfSlide? FindSlide(OdfDocument document, string name)
    {
        if (name.Length == 0)
        {
            return null;
        }
        OdfSlide? result = null;
        int matches = 0;
        int index = 0;
        while (index < document.Slides.Length)
        {
            if (document.Slides[index].Name == name)
            {
                result = document.Slides[index];
                matches = matches + 1;
            }
            index = index + 1;
        }
        return matches == 1 ? result : null;
    }

    static OdfPresentationShow? FindShow(
        OdfDocument document,
        string name)
    {
        if (name.Length == 0)
        {
            return null;
        }
        OdfPresentationShow? result = null;
        int matches = 0;
        int index = 0;
        while (index < document.PresentationShows.Length)
        {
            if (document.PresentationShows[index].Name == name)
            {
                result = document.PresentationShows[index];
                matches = matches + 1;
            }
            index = index + 1;
        }
        return matches == 1 ? result : null;
    }

    static string[] ParsePageNames(string pages)
    {
        if (pages.Length == 0)
        {
            return [];
        }
        string[] values = pages.Split(',');
        int index = 0;
        while (index < values.Length)
        {
            values[index] = values[index].Trim();
            index = index + 1;
        }
        return values;
    }

    static string[] NamesFromSlides(OdfSlide[] slides)
    {
        string[] result = new string[slides.Length];
        int index = 0;
        while (index < slides.Length)
        {
            if (slides[index] == null)
            {
                throw new OdfEditException(
                    "A custom show cannot contain a null slide.");
            }
            result[index] = slides[index].Name;
            index = index + 1;
        }
        return result;
    }

    static string JoinPageNames(string[] names)
    {
        return string.Join(", ", names);
    }

    static bool ShowRetainsNode(
        OdfPresentationShow[] shows,
        OdfNode node)
    {
        int index = 0;
        while (index < shows.Length)
        {
            if (shows[index] != null && shows[index].Node == node)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static bool SlideArraysMatch(OdfSlide[] first, OdfSlide[] second)
    {
        if (first.Length != second.Length)
        {
            return false;
        }
        int index = 0;
        while (index < first.Length)
        {
            if (first[index] != second[index])
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static bool StringArraysMatch(string[] first, string[] second)
    {
        if (first.Length != second.Length)
        {
            return false;
        }
        int index = 0;
        while (index < first.Length)
        {
            if (first[index] != second[index])
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static void CaptureShow(OdfPresentationShow show)
    {
        show.OriginalName = show.Name;
        show.OriginalPages = show.Pages;
        show.OriginalSlideNames = CopyStrings(show.SlideNames);
        show.OriginalSlides = CopySlides(show.Slides);
    }

    static string[] CopyStrings(string[] source)
    {
        string[] result = new string[source.Length];
        Array.Copy(source, result, source.Length);
        return result;
    }

    static OdfSlide[] CopySlides(OdfSlide[] source)
    {
        OdfSlide[] result = new OdfSlide[source.Length];
        Array.Copy(source, result, source.Length);
        return result;
    }

    static void RefreshSettings(OdfPresentationSettings settings)
    {
        OdfLexicalValue fullScreen = OdfLexicalParser.ParseBoolean(
            settings.FullScreenValue);
        settings.FullScreen = fullScreen.IsValid
            && fullScreen.BooleanValue;
        settings.IsFullScreenValid = settings.FullScreenValue.Length == 0
            || fullScreen.IsValid;
        OdfLexicalValue endless = OdfLexicalParser.ParseBoolean(
            settings.EndlessValue);
        settings.Endless = endless.IsValid && endless.BooleanValue;
        settings.IsEndlessValid = settings.EndlessValue.Length == 0
            || endless.IsValid;
        settings.IsPauseValid = settings.Pause.Length == 0
            || OdfLexicalParser.ParseDuration(settings.Pause).IsValid;
        OdfLexicalValue showLogo = OdfLexicalParser.ParseBoolean(
            settings.ShowLogoValue);
        settings.ShowLogo = showLogo.IsValid && showLogo.BooleanValue;
        settings.IsShowLogoValid = settings.ShowLogoValue.Length == 0
            || showLogo.IsValid;
        OdfLexicalValue forceManual = OdfLexicalParser.ParseBoolean(
            settings.ForceManualValue);
        settings.ForceManual = forceManual.IsValid
            && forceManual.BooleanValue;
        settings.IsForceManualValid =
            settings.ForceManualValue.Length == 0
            || forceManual.IsValid;
        OdfLexicalValue mouseVisible = OdfLexicalParser.ParseBoolean(
            settings.MouseVisibleValue);
        settings.MouseVisible = mouseVisible.IsValid
            && mouseVisible.BooleanValue;
        settings.IsMouseVisibleValid =
            settings.MouseVisibleValue.Length == 0
            || mouseVisible.IsValid;
        OdfLexicalValue mouseAsPen = OdfLexicalParser.ParseBoolean(
            settings.MouseAsPenValue);
        settings.MouseAsPen = mouseAsPen.IsValid
            && mouseAsPen.BooleanValue;
        settings.IsMouseAsPenValid =
            settings.MouseAsPenValue.Length == 0
            || mouseAsPen.IsValid;
        OdfLexicalValue navigator = OdfLexicalParser.ParseBoolean(
            settings.StartWithNavigatorValue);
        settings.StartWithNavigator = navigator.IsValid
            && navigator.BooleanValue;
        settings.IsStartWithNavigatorValid =
            settings.StartWithNavigatorValue.Length == 0
            || navigator.IsValid;
        settings.AnimationsEnabled = settings.AnimationsValue == "enabled";
        settings.IsAnimationsValid = settings.AnimationsValue.Length == 0
            || settings.AnimationsValue == "enabled"
            || settings.AnimationsValue == "disabled";
        settings.TransitionOnClickEnabled =
            settings.TransitionOnClickValue == "enabled";
        settings.IsTransitionOnClickValid =
            settings.TransitionOnClickValue.Length == 0
            || settings.TransitionOnClickValue == "enabled"
            || settings.TransitionOnClickValue == "disabled";
        OdfLexicalValue stayOnTop = OdfLexicalParser.ParseBoolean(
            settings.StayOnTopValue);
        settings.StayOnTop = stayOnTop.IsValid
            && stayOnTop.BooleanValue;
        settings.IsStayOnTopValid = settings.StayOnTopValue.Length == 0
            || stayOnTop.IsValid;
        OdfLexicalValue showEndSlide = OdfLexicalParser.ParseBoolean(
            settings.ShowEndSlideValue);
        settings.ShowEndSlide = showEndSlide.IsValid
            && showEndSlide.BooleanValue;
        settings.IsShowEndSlideValid =
            settings.ShowEndSlideValue.Length == 0
            || showEndSlide.IsValid;
    }

    static void CaptureSettings(OdfPresentationSettings settings)
    {
        settings.OriginalStartPage = settings.StartPage;
        settings.OriginalShowName = settings.ShowName;
        settings.OriginalFullScreenValue = settings.FullScreenValue;
        settings.OriginalEndlessValue = settings.EndlessValue;
        settings.OriginalPause = settings.Pause;
        settings.OriginalShowLogoValue = settings.ShowLogoValue;
        settings.OriginalForceManualValue = settings.ForceManualValue;
        settings.OriginalMouseVisibleValue = settings.MouseVisibleValue;
        settings.OriginalMouseAsPenValue = settings.MouseAsPenValue;
        settings.OriginalStartWithNavigatorValue =
            settings.StartWithNavigatorValue;
        settings.OriginalAnimationsValue = settings.AnimationsValue;
        settings.OriginalTransitionOnClickValue =
            settings.TransitionOnClickValue;
        settings.OriginalStayOnTopValue = settings.StayOnTopValue;
        settings.OriginalShowEndSlideValue = settings.ShowEndSlideValue;
    }

    static bool SettingsHasContent(OdfPresentationSettings settings)
    {
        return settings.StartPage.Length > 0
            || settings.ShowName.Length > 0
            || settings.FullScreenValue.Length > 0
            || settings.EndlessValue.Length > 0
            || settings.Pause.Length > 0
            || settings.ShowLogoValue.Length > 0
            || settings.ForceManualValue.Length > 0
            || settings.MouseVisibleValue.Length > 0
            || settings.MouseAsPenValue.Length > 0
            || settings.StartWithNavigatorValue.Length > 0
            || settings.AnimationsValue.Length > 0
            || settings.TransitionOnClickValue.Length > 0
            || settings.StayOnTopValue.Length > 0
            || settings.ShowEndSlideValue.Length > 0
            || settings.StartSlide != settings.OriginalStartSlide
            || settings.SelectedShow != settings.OriginalSelectedShow
            || BooleanChanged(
                settings.FullScreen,
                settings.OriginalFullScreenValue)
            || BooleanChanged(
                settings.Endless,
                settings.OriginalEndlessValue)
            || BooleanChanged(
                settings.ShowLogo,
                settings.OriginalShowLogoValue)
            || BooleanChanged(
                settings.ForceManual,
                settings.OriginalForceManualValue)
            || BooleanChanged(
                settings.MouseVisible,
                settings.OriginalMouseVisibleValue)
            || BooleanChanged(
                settings.MouseAsPen,
                settings.OriginalMouseAsPenValue)
            || BooleanChanged(
                settings.StartWithNavigator,
                settings.OriginalStartWithNavigatorValue)
            || settings.AnimationsEnabled
                != (settings.OriginalAnimationsValue == "enabled")
            || settings.TransitionOnClickEnabled
                != (settings.OriginalTransitionOnClickValue == "enabled")
            || BooleanChanged(
                settings.StayOnTop,
                settings.OriginalStayOnTopValue)
            || BooleanChanged(
                settings.ShowEndSlide,
                settings.OriginalShowEndSlideValue);
    }

    static bool BooleanChanged(bool value, string originalValue)
    {
        OdfLexicalValue original = OdfLexicalParser.ParseBoolean(
            originalValue);
        return value != (original.IsValid && original.BooleanValue);
    }

    static string BooleanValue(
        string value,
        bool booleanValue,
        string originalValue)
    {
        OdfLexicalValue original = OdfLexicalParser.ParseBoolean(
            originalValue);
        bool originalBoolean = original.IsValid && original.BooleanValue;
        if (booleanValue != originalBoolean && value == originalValue)
        {
            return booleanValue ? "true" : "false";
        }
        return value;
    }

    static string EnabledValue(
        string value,
        bool enabled,
        string originalValue)
    {
        bool originalEnabled = originalValue == "enabled";
        if (enabled != originalEnabled && value == originalValue)
        {
            return enabled ? "enabled" : "disabled";
        }
        return value;
    }

    static OdfTransitionKind ParseTransitionKind(
        string type,
        string legacyStyle)
    {
        switch (type)
        {
            case "none": return OdfTransitionKind.None;
            case "fade": return OdfTransitionKind.Fade;
            case "barWipe": return OdfTransitionKind.BarWipe;
            case "boxWipe": return OdfTransitionKind.BoxWipe;
            case "fourBoxWipe": return OdfTransitionKind.FourBoxWipe;
            case "barnDoorWipe": return OdfTransitionKind.BarnDoorWipe;
            case "diagonalWipe": return OdfTransitionKind.DiagonalWipe;
            case "bowTieWipe": return OdfTransitionKind.BowTieWipe;
            case "miscDiagonalWipe": return OdfTransitionKind.MiscDiagonalWipe;
            case "veeWipe": return OdfTransitionKind.VeeWipe;
            case "barnVeeWipe": return OdfTransitionKind.BarnVeeWipe;
            case "zigZagWipe": return OdfTransitionKind.ZigZagWipe;
            case "barnZigZagWipe": return OdfTransitionKind.BarnZigZagWipe;
            case "irisWipe": return OdfTransitionKind.IrisWipe;
            case "triangleWipe": return OdfTransitionKind.TriangleWipe;
            case "arrowHeadWipe": return OdfTransitionKind.ArrowHeadWipe;
            case "pentagonWipe": return OdfTransitionKind.PentagonWipe;
            case "hexagonWipe": return OdfTransitionKind.HexagonWipe;
            case "ellipseWipe": return OdfTransitionKind.EllipseWipe;
            case "eyeWipe": return OdfTransitionKind.EyeWipe;
            case "roundRectWipe": return OdfTransitionKind.RoundRectWipe;
            case "starWipe": return OdfTransitionKind.StarWipe;
            case "miscShapeWipe": return OdfTransitionKind.MiscShapeWipe;
            case "clockWipe": return OdfTransitionKind.ClockWipe;
            case "pinWheelWipe": return OdfTransitionKind.PinWheelWipe;
            case "singleSweepWipe": return OdfTransitionKind.SingleSweepWipe;
            case "fanWipe": return OdfTransitionKind.FanWipe;
            case "doubleFanWipe": return OdfTransitionKind.DoubleFanWipe;
            case "doubleSweepWipe": return OdfTransitionKind.DoubleSweepWipe;
            case "saloonDoorWipe": return OdfTransitionKind.SaloonDoorWipe;
            case "windshieldWipe": return OdfTransitionKind.WindshieldWipe;
            case "snakeWipe": return OdfTransitionKind.SnakeWipe;
            case "spiralWipe": return OdfTransitionKind.SpiralWipe;
            case "parallelSnakesWipe": return OdfTransitionKind.ParallelSnakesWipe;
            case "boxSnakesWipe": return OdfTransitionKind.BoxSnakesWipe;
            case "waterfallWipe": return OdfTransitionKind.WaterfallWipe;
            case "pushWipe": return OdfTransitionKind.PushWipe;
            case "slideWipe": return OdfTransitionKind.SlideWipe;
            case "sweepWipe": return OdfTransitionKind.SweepWipe;
        }
        if (legacyStyle == "none")
        {
            return OdfTransitionKind.None;
        }
        if (legacyStyle.StartsWith(
                "fade",
                global::System.StringComparison.Ordinal)
            || legacyStyle == "dissolve")
        {
            return OdfTransitionKind.Fade;
        }
        return OdfTransitionKind.Unknown;
    }

    internal static string FormatTransitionKind(OdfTransitionKind kind)
    {
        switch (kind)
        {
            case OdfTransitionKind.None: return "none";
            case OdfTransitionKind.Fade: return "fade";
            case OdfTransitionKind.BarWipe: return "barWipe";
            case OdfTransitionKind.BoxWipe: return "boxWipe";
            case OdfTransitionKind.FourBoxWipe: return "fourBoxWipe";
            case OdfTransitionKind.BarnDoorWipe: return "barnDoorWipe";
            case OdfTransitionKind.DiagonalWipe: return "diagonalWipe";
            case OdfTransitionKind.BowTieWipe: return "bowTieWipe";
            case OdfTransitionKind.MiscDiagonalWipe: return "miscDiagonalWipe";
            case OdfTransitionKind.VeeWipe: return "veeWipe";
            case OdfTransitionKind.BarnVeeWipe: return "barnVeeWipe";
            case OdfTransitionKind.ZigZagWipe: return "zigZagWipe";
            case OdfTransitionKind.BarnZigZagWipe: return "barnZigZagWipe";
            case OdfTransitionKind.IrisWipe: return "irisWipe";
            case OdfTransitionKind.TriangleWipe: return "triangleWipe";
            case OdfTransitionKind.ArrowHeadWipe: return "arrowHeadWipe";
            case OdfTransitionKind.PentagonWipe: return "pentagonWipe";
            case OdfTransitionKind.HexagonWipe: return "hexagonWipe";
            case OdfTransitionKind.EllipseWipe: return "ellipseWipe";
            case OdfTransitionKind.EyeWipe: return "eyeWipe";
            case OdfTransitionKind.RoundRectWipe: return "roundRectWipe";
            case OdfTransitionKind.StarWipe: return "starWipe";
            case OdfTransitionKind.MiscShapeWipe: return "miscShapeWipe";
            case OdfTransitionKind.ClockWipe: return "clockWipe";
            case OdfTransitionKind.PinWheelWipe: return "pinWheelWipe";
            case OdfTransitionKind.SingleSweepWipe: return "singleSweepWipe";
            case OdfTransitionKind.FanWipe: return "fanWipe";
            case OdfTransitionKind.DoubleFanWipe: return "doubleFanWipe";
            case OdfTransitionKind.DoubleSweepWipe: return "doubleSweepWipe";
            case OdfTransitionKind.SaloonDoorWipe: return "saloonDoorWipe";
            case OdfTransitionKind.WindshieldWipe: return "windshieldWipe";
            case OdfTransitionKind.SnakeWipe: return "snakeWipe";
            case OdfTransitionKind.SpiralWipe: return "spiralWipe";
            case OdfTransitionKind.ParallelSnakesWipe: return "parallelSnakesWipe";
            case OdfTransitionKind.BoxSnakesWipe: return "boxSnakesWipe";
            case OdfTransitionKind.WaterfallWipe: return "waterfallWipe";
            case OdfTransitionKind.PushWipe: return "pushWipe";
            case OdfTransitionKind.SlideWipe: return "slideWipe";
            case OdfTransitionKind.SweepWipe: return "sweepWipe";
        }
        return "";
    }

    static string ReadPresentation(OdfNode node, string localName)
    {
        return Read(node, OdfNamespaces.Presentation, localName);
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

    static void WritePresentationChanged(
        OdfNode node,
        string localName,
        string value,
        string original)
    {
        WriteChanged(
            node,
            OdfNamespaces.Presentation,
            localName,
            OdfNamespaces.PresentationPrefix,
            value,
            original);
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

    static OdfNode? FindDirectChild(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        int index = 0;
        while (index < node.Children.Length)
        {
            if (IsElement(
                node.Children[index],
                namespaceName,
                localName))
            {
                return node.Children[index];
            }
            index = index + 1;
        }
        return null;
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

    static void AppendChild(OdfNode parent, OdfNode child)
    {
        OdfNode[] children = new OdfNode[parent.Children.Length + 1];
        Array.Copy(parent.Children, children, parent.Children.Length);
        children[children.Length - 1] = child;
        child.Parent = parent;
        parent.Children = children;
        parent.WasEmptyElement = false;
    }

    static void RemoveChild(OdfNode parent, OdfNode child)
    {
        List<OdfNode> children = new List<OdfNode>();
        int index = 0;
        while (index < parent.Children.Length)
        {
            if (parent.Children[index] != child)
            {
                children.Add(parent.Children[index]);
            }
            index = index + 1;
        }
        child.Parent = null;
        parent.Children = children.ToArray();
    }
}
