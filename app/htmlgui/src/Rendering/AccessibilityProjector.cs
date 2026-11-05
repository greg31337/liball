using System;
using System.Collections.Generic;

namespace htmlgui;

internal class AccessibilityProjector
{
    internal DomNode ProjectControl(UiNode node, DomNode rendered)
    {
        DomNode? target = SelectSemanticTarget(node, rendered);
        if (target == null)
        {
            if (!NeedsProjection(node))
            {
                return AppendLiveAnnouncements(node, rendered);
            }

            target = CreateSemanticHost(rendered);
            rendered = target;
        }

        List<DomAttribute> attributes = CopyAttributes(target.Attributes);
        ProjectNodeState(node, target, attributes);
        ProjectAccessibility(node, target, attributes);
        ProjectSupportFeatures(node, attributes);
        ProjectHeadingMetadata(node, target, attributes);
        ProjectLandmarkMetadata(node, target);
        target.Attributes = attributes.ToArray();
        ProjectNativeRole(node, target, attributes);
        SortAttributes(attributes);
        target.Attributes = attributes.ToArray();
        AppendNonColorStatus(node, target);
        return AppendLiveAnnouncements(node, rendered);
    }

    internal DomNode? SelectSemanticTarget(UiNode node, DomNode rendered)
    {
        if (rendered.Kind == DomNodeKind.Element && IsSemanticElement(rendered))
        {
            return rendered;
        }

        if (node.Input != null || UsesNestedSemanticHost(node.Name))
        {
            DomNode? descendant = FindSemanticDescendant(rendered.Children);
            if (descendant != null)
            {
                return descendant;
            }
        }

        if (rendered.Kind == DomNodeKind.Element)
        {
            return rendered;
        }

        return null;
    }

    internal bool UsesNestedSemanticHost(string name)
    {
        switch (name)
        {
            case "menu-button":
            case "tabs":
            case "tab-view":
            case "dialog":
            case "modal-dialog":
            case "non-modal-dialog":
            case "alert-dialog":
            case "confirm-dialog":
            case "prompt-dialog":
            case "sheet":
            case "drawer":
                return true;
            default:
                return false;
        }
    }

    internal DomNode? FindSemanticDescendant(DomNode[]? children)
    {
        if (children == null)
        {
            return null;
        }

        int index = 0;
        while (index < children.Length)
        {
            DomNode? child = children[index];
            if (child != null && !IsAccessibilityHidden(child))
            {
                if (child.Kind == DomNodeKind.Element && IsSemanticElement(child))
                {
                    return child;
                }

                DomNode? nested = FindSemanticDescendant(child.Children);
                if (nested != null)
                {
                    return nested;
                }
            }

            index++;
        }

        return null;
    }

    internal bool IsSemanticElement(DomNode node)
    {
        if (GetAttributeValue(node.Attributes, HtmlAttributes.Role) != null)
        {
            return true;
        }

        return GetImplicitRole(node) != null;
    }

    internal bool IsAccessibilityHidden(DomNode node)
    {
        string? hidden = GetAttributeValue(node.Attributes, AriaAttributes.Hidden);
        if (hidden == "true")
        {
            return true;
        }

        return GetAttributeValue(node.Attributes, HtmlAttributes.Hidden) != null;
    }

    internal bool NeedsProjection(UiNode node)
    {
        UiNodeState? state = node.State;
        if (state != null && (!state.IsVisible || !state.IsEnabled || state.IsBusy || state.HasTabIndex || state.HasPressed || state.HasSelected || state.HasExpanded || state.HasCheckState || state.HasCurrentKind))
        {
            return true;
        }

        UiAccessibility? accessibility = node.Accessibility;
        if (accessibility == null)
        {
            return false;
        }

        if (accessibility.Role != null || accessibility.Label != null || accessibility.Description != null || accessibility.HasHeadingLevel || accessibility.LiveMode != UiAriaLiveMode.Off || accessibility.HasCurrentKind || accessibility.IsHidden || accessibility.StatusText != null)
        {
            return true;
        }

        if (accessibility.Relationships != null && accessibility.Relationships.Length > 0)
        {
            return true;
        }

        return accessibility.Features != null && accessibility.Features.Length > 0;
    }

    internal void ProjectSupportFeatures(UiNode node, List<DomAttribute> attributes)
    {
        UiAccessibility? accessibility = node.Accessibility;
        if (accessibility == null)
        {
            return;
        }

        UiAccessibilityFeature[]? features = accessibility.Features;
        if (features == null)
        {
            throw CreateRenderException(node, "Accessibility features cannot be null.");
        }

        ValidateSupportFeatures(node, features);
        AddSupportClass(features, UiAccessibilityFeature.ForcedColors, AccessibilityHooks.ForcedColorsClass, attributes);
        AddSupportClass(features, UiAccessibilityFeature.Zoom, AccessibilityHooks.ZoomClass, attributes);
        AddSupportClass(features, UiAccessibilityFeature.Reflow, AccessibilityHooks.ReflowClass, attributes);
        AddSupportClass(features, UiAccessibilityFeature.FocusVisible, AccessibilityHooks.FocusVisibleClass, attributes);
        AddSupportClass(features, UiAccessibilityFeature.TargetSize, AccessibilityHooks.TargetSizeClass, attributes);
        AddSupportClass(features, UiAccessibilityFeature.ReducedMotion, AccessibilityHooks.ReducedMotionClass, attributes);
        AddSupportClass(features, UiAccessibilityFeature.ReducedTransparency, AccessibilityHooks.ReducedTransparencyClass, attributes);
        AddSupportClass(features, UiAccessibilityFeature.ScreenReaderOnly, AccessibilityHooks.ScreenReaderOnlyClass, attributes);
        AddSupportClass(features, UiAccessibilityFeature.NonColorStatus, AccessibilityHooks.NonColorStatusClass, attributes);

        if (HasSupportFeature(features, UiAccessibilityFeature.TargetSize))
        {
            if (accessibility.MinimumTargetSizePixels < 24)
            {
                throw CreateRenderException(node, "Minimum accessibility target sizes cannot be less than 24 CSS pixels.");
            }

            SetAttribute(attributes, AccessibilityHooks.TargetSizeAttribute, InvariantFormatter.FormatInt32(accessibility.MinimumTargetSizePixels), false);
        }

        if (HasSupportFeature(features, UiAccessibilityFeature.Busy))
        {
            SetAttributeIfMissing(attributes, AriaAttributes.Busy, "true", false);
        }

        if (HasSupportFeature(features, UiAccessibilityFeature.NonColorStatus) && string.IsNullOrWhiteSpace(accessibility.StatusText))
        {
            throw CreateRenderException(node, "Non-color status hooks require non-empty status text.");
        }
    }

    internal void ValidateSupportFeatures(UiNode node, UiAccessibilityFeature[] features)
    {
        int index = 0;
        while (index < features.Length)
        {
            switch (features[index])
            {
                case UiAccessibilityFeature.ForcedColors:
                case UiAccessibilityFeature.Zoom:
                case UiAccessibilityFeature.Reflow:
                case UiAccessibilityFeature.FocusVisible:
                case UiAccessibilityFeature.TargetSize:
                case UiAccessibilityFeature.ReducedMotion:
                case UiAccessibilityFeature.ReducedTransparency:
                case UiAccessibilityFeature.ScreenReaderOnly:
                case UiAccessibilityFeature.Busy:
                case UiAccessibilityFeature.NonColorStatus:
                    break;
                default:
                    throw CreateRenderException(node, "The accessibility feature is not supported.");
            }

            index++;
        }
    }

    internal void AddSupportClass(UiAccessibilityFeature[] features, UiAccessibilityFeature feature, string className, List<DomAttribute> attributes)
    {
        if (HasSupportFeature(features, feature))
        {
            MergeAttributeTokens(attributes, HtmlAttributes.Class, className);
        }
    }

    internal bool HasSupportFeature(UiAccessibilityFeature[] features, UiAccessibilityFeature feature)
    {
        int index = 0;
        while (index < features.Length)
        {
            if (features[index] == feature)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal void AppendNonColorStatus(UiNode node, DomNode target)
    {
        UiAccessibility? accessibility = node.Accessibility;
        if (accessibility == null || accessibility.Features == null || !HasSupportFeature(accessibility.Features, UiAccessibilityFeature.NonColorStatus))
        {
            return;
        }

        DomNode[] children = target.Children ?? Array.Empty<DomNode>();
        int childCount = children.Length;

        int index = 0;
        while (index < childCount)
        {
            DomNode? child = children[index];
            if (child != null && child.Key == "$accessibility-status")
            {
                return;
            }

            index++;
        }

        string nodeId = GetProjectorNodeId(node);
        string token = StableHash.HashParts(new string[] { nodeId, accessibility.StatusText ?? string.Empty, "non-color-status" });
        DomNode status = new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = "$accessibility-status",
            Id = "ui-accessibility-status-" + token.Substring(0, 20),
            Name = HtmlElements.Span,
            Attributes = new DomAttribute[]
            {
                new DomAttribute { Name = HtmlAttributes.Class, Value = AccessibilityHooks.StatusTextClass }
            },
            Children = new DomNode[]
            {
                new DomNode
                {
                    Kind = DomNodeKind.Text,
                    Key = "$accessibility-status-text",
                    Id = "ui-accessibility-status-text-" + token.Substring(0, 20),
                    Text = accessibility.StatusText,
                    Version = GetProjectionVersion(node)
                }
            },
            Version = GetProjectionVersion(node)
        };
        DomNode[] result = new DomNode[childCount + 1];
        index = 0;
        while (index < childCount)
        {
            result[index] = children[index];
            index++;
        }

        result[childCount] = status;
        target.Children = result;
    }

    internal DomNode CreateSemanticHost(DomNode rendered)
    {
        return new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = rendered.Key,
            Id = rendered.Id,
            Name = HtmlElements.Division,
            Children = rendered.Children,
            EventSubscriptions = rendered.EventSubscriptions,
            StyleScope = rendered.StyleScope,
            HydrationMarker = rendered.HydrationMarker,
            Version = rendered.Version,
            SubtreeHash = rendered.SubtreeHash
        };
    }

    internal void ProjectNodeState(UiNode node, DomNode target, List<DomAttribute> attributes)
    {
        UiNodeState? state = node.State;
        if (state == null)
        {
            return;
        }

        if (!state.IsVisible)
        {
            SetAttributeIfMissing(attributes, HtmlAttributes.Hidden, string.Empty, true);
        }

        if (!state.IsEnabled)
        {
            if (GetListAttributeValue(attributes, HtmlAttributes.Disabled) == null && GetListAttributeValue(attributes, AriaAttributes.Disabled) == null && SupportsNativeDisabled(target))
            {
                SetAttribute(attributes, HtmlAttributes.Disabled, string.Empty, true);
            }
            else if (GetListAttributeValue(attributes, HtmlAttributes.Disabled) == null && GetListAttributeValue(attributes, AriaAttributes.Disabled) == null)
            {
                SetAttribute(attributes, AriaAttributes.Disabled, "true", false);
            }
        }

        if (state.IsBusy)
        {
            SetAttributeIfMissing(attributes, AriaAttributes.Busy, "true", false);
        }

        if (state.HasTabIndex)
        {
            SetAttributeIfMissing(attributes, HtmlAttributes.TabIndex, InvariantFormatter.FormatInt32(state.TabIndex), false);
        }

        if (state.HasPressed)
        {
            SetAttributeIfMissing(attributes, AriaAttributes.Pressed, FormatBoolean(state.IsPressed), false);
        }

        if (state.HasSelected && GetListAttributeValue(attributes, AriaAttributes.Selected) == null && GetListAttributeValue(attributes, HtmlAttributes.Selected) == null)
        {
            ProjectSelectedState(target, attributes, state.IsSelected);
        }

        if (state.HasExpanded && GetListAttributeValue(attributes, AriaAttributes.Expanded) == null && GetListAttributeValue(attributes, HtmlAttributes.Open) == null)
        {
            ProjectExpandedState(target, attributes, state.IsExpanded);
        }

        if (state.HasCheckState && GetListAttributeValue(attributes, AriaAttributes.Checked) == null && GetListAttributeValue(attributes, HtmlAttributes.Checked) == null && !HasProperty(target.Properties, HtmlAttributes.Checked))
        {
            ProjectCheckState(target, attributes, state.CheckState);
        }

        if (state.HasCurrentKind)
        {
            SetAttributeIfMissing(attributes, AriaAttributes.Current, FormatCurrentKind(state.CurrentKind), false);
        }
    }

    internal void ProjectAccessibility(UiNode node, DomNode target, List<DomAttribute> attributes)
    {
        UiAccessibility? accessibility = node.Accessibility;
        if (accessibility == null)
        {
            return;
        }

        if (accessibility.Label != null)
        {
            if (target.Name == HtmlElements.Image)
            {
                SetAttribute(attributes, HtmlAttributes.AlternativeText, accessibility.Label, false);
            }
            else
            {
                SetAttribute(attributes, AriaAttributes.Label, accessibility.Label, false);
            }
        }

        if (accessibility.Description != null)
        {
            SetAttribute(attributes, AriaAttributes.Description, accessibility.Description, false);
        }

        if (accessibility.LiveMode != UiAriaLiveMode.Off)
        {
            SetAttribute(attributes, AriaAttributes.Live, FormatLiveMode(accessibility.LiveMode), false);
        }

        if (accessibility.HasCurrentKind)
        {
            SetAttribute(attributes, AriaAttributes.Current, FormatCurrentKind(accessibility.CurrentKind), false);
        }

        if (accessibility.IsHidden)
        {
            SetAttribute(attributes, AriaAttributes.Hidden, "true", false);
        }

        UiAriaRelationship[]? relationships = accessibility.Relationships;
        int index = 0;
        while (relationships != null && index < relationships.Length)
        {
            UiAriaRelationship? relationship = relationships[index];
            if (relationship == null)
            {
                throw CreateRenderException(node, "Accessibility relationships cannot contain null.");
            }

            ProjectRelationship(node, attributes, relationship);
            index++;
        }

        ResolveAccessibleInputs(attributes);
    }

    internal void ResolveAccessibleInputs(List<DomAttribute> attributes)
    {
        string? labelledBy = GetListAttributeValue(attributes, AriaAttributes.LabelledBy);
        if (labelledBy != null && labelledBy.Length > 0)
        {
            RemoveAttribute(attributes, AriaAttributes.Label);
        }

        string? describedBy = GetListAttributeValue(attributes, AriaAttributes.DescribedBy);
        if (describedBy != null && describedBy.Length > 0)
        {
            RemoveAttribute(attributes, AriaAttributes.Description);
        }

        string? errorMessage = GetListAttributeValue(attributes, AriaAttributes.ErrorMessage);
        if (errorMessage != null && errorMessage.Length > 0)
        {
            MergeAttributeTokens(attributes, AriaAttributes.DescribedBy, errorMessage);
            SetAttribute(attributes, AriaAttributes.Invalid, "true", false);
            RemoveAttribute(attributes, AriaAttributes.Description);
        }
    }

    internal void MergeAttributeTokens(List<DomAttribute> attributes, string name, string values)
    {
        List<string> tokens = ReadTokens(GetListAttributeValue(attributes, name));
        List<string> additions = ReadTokens(values);
        int index = 0;
        while (index < additions.Count)
        {
            if (!ContainsToken(tokens, additions[index]))
            {
                tokens.Add(additions[index]);
            }

            index++;
        }

        SetAttribute(attributes, name, string.Join(" ", tokens), false);
    }

    internal void ProjectHeadingMetadata(UiNode node, DomNode target, List<DomAttribute> attributes)
    {
        UiAccessibility? accessibility = node.Accessibility;
        if (accessibility == null || !accessibility.HasHeadingLevel)
        {
            return;
        }

        int level = accessibility.HeadingLevel;
        if (level < 1 || level > 6)
        {
            throw CreateRenderException(node, "Accessible heading levels must be between one and six.");
        }

        string? requestedRole = accessibility.Role;
        if (IsNativeHeading(target.Name) || IsGenericSemanticContainer(target.Name))
        {
            target.Name = GetHeadingElement(level);
            RemoveAttribute(attributes, AriaAttributes.Level);
            if (requestedRole == null || requestedRole == AriaRoles.Heading)
            {
                RemoveAttribute(attributes, HtmlAttributes.Role);
            }

            return;
        }

        if (requestedRole == null)
        {
            SetAttribute(attributes, HtmlAttributes.Role, AriaRoles.Heading, false);
        }

        SetAttribute(attributes, AriaAttributes.Level, InvariantFormatter.FormatInt32(level), false);
    }

    internal bool IsNativeHeading(string name)
    {
        switch (name)
        {
            case HtmlElements.Heading1:
            case HtmlElements.Heading2:
            case HtmlElements.Heading3:
            case HtmlElements.Heading4:
            case HtmlElements.Heading5:
            case HtmlElements.Heading6:
                return true;
            default:
                return false;
        }
    }

    internal string GetHeadingElement(int level)
    {
        switch (level)
        {
            case 1:
                return HtmlElements.Heading1;
            case 2:
                return HtmlElements.Heading2;
            case 3:
                return HtmlElements.Heading3;
            case 4:
                return HtmlElements.Heading4;
            case 5:
                return HtmlElements.Heading5;
            case 6:
                return HtmlElements.Heading6;
            default:
                throw new ArgumentOutOfRangeException("level", level, "Heading levels must be between one and six.");
        }
    }

    internal void ProjectLandmarkMetadata(UiNode node, DomNode target)
    {
        UiAccessibility? accessibility = node.Accessibility;
        if (accessibility == null || accessibility.Role == null || !IsGenericSemanticContainer(target.Name))
        {
            return;
        }

        switch (accessibility.Role)
        {
            case AriaRoles.Banner:
                target.Name = HtmlElements.Header;
                break;
            case AriaRoles.Complementary:
                target.Name = HtmlElements.Aside;
                break;
            case AriaRoles.ContentInfo:
                target.Name = HtmlElements.Footer;
                break;
            case AriaRoles.Form:
                target.Name = HtmlElements.Form;
                break;
            case AriaRoles.Main:
                target.Name = HtmlElements.Main;
                break;
            case AriaRoles.Navigation:
                target.Name = HtmlElements.Navigation;
                break;
            case AriaRoles.Region:
                target.Name = HtmlElements.Section;
                break;
            case AriaRoles.Search:
                target.Name = HtmlElements.Search;
                break;
        }
    }

    internal bool IsGenericSemanticContainer(string name)
    {
        switch (name)
        {
            case HtmlElements.Division:
            case HtmlElements.Span:
            case HtmlElements.Section:
            case HtmlElements.Header:
            case HtmlElements.Footer:
            case HtmlElements.Form:
            case HtmlElements.Main:
            case HtmlElements.Navigation:
            case HtmlElements.Search:
            case HtmlElements.Aside:
                return true;
            default:
                return false;
        }
    }

    internal void ProjectRelationship(UiNode node, List<DomAttribute> attributes, UiAriaRelationship relationship)
    {
        if (!IsRelationshipName(relationship.Name))
        {
            throw CreateRenderException(node, "Accessibility relationships require a supported ARIA ID-reference attribute.");
        }

        string[]? targetIds = relationship.TargetIds;
        if (targetIds == null || targetIds.Length == 0)
        {
            throw CreateRenderException(node, "Accessibility relationships require at least one target identifier.");
        }

        List<string> tokens = ReadTokens(GetListAttributeValue(attributes, relationship.Name));
        int index = 0;
        while (index < targetIds.Length)
        {
            string? targetId = targetIds[index];
            if (string.IsNullOrWhiteSpace(targetId) || ContainsHtmlWhiteSpace(targetId))
            {
                throw CreateRenderException(node, "Accessibility relationship target identifiers cannot be empty or contain whitespace.");
            }

            if (!ContainsToken(tokens, targetId))
            {
                tokens.Add(targetId);
            }

            index++;
        }

        SetAttribute(attributes, relationship.Name, string.Join(" ", tokens), false);
    }

    internal bool IsRelationshipName(string name)
    {
        switch (name)
        {
            case AriaAttributes.ActiveDescendant:
            case AriaAttributes.Controls:
            case AriaAttributes.DescribedBy:
            case AriaAttributes.Details:
            case AriaAttributes.ErrorMessage:
            case AriaAttributes.FlowTo:
            case AriaAttributes.LabelledBy:
            case AriaAttributes.Owns:
                return true;
            default:
                return false;
        }
    }

    internal void ProjectNativeRole(UiNode node, DomNode target, List<DomAttribute> attributes)
    {
        string? requestedRole = null;
        if (node.Accessibility != null)
        {
            requestedRole = node.Accessibility.Role;
        }

        string? implicitRole = GetImplicitRole(target);
        if (requestedRole != null)
        {
            if (string.IsNullOrWhiteSpace(requestedRole))
            {
                throw CreateRenderException(node, "A supplied accessibility role cannot be empty.");
            }

            if (requestedRole == implicitRole)
            {
                RemoveAttribute(attributes, HtmlAttributes.Role);
            }
            else
            {
                SetAttribute(attributes, HtmlAttributes.Role, requestedRole, false);
            }

            return;
        }

        string? renderedRole = GetListAttributeValue(attributes, HtmlAttributes.Role);
        if (renderedRole != null && renderedRole == implicitRole)
        {
            RemoveAttribute(attributes, HtmlAttributes.Role);
        }
    }

    internal string? GetImplicitRole(DomNode node)
    {
        switch (node.Name)
        {
            case HtmlElements.Button:
            case HtmlElements.Summary:
                return AriaRoles.Button;
            case HtmlElements.Input:
                return GetInputRole(node);
            case HtmlElements.TextArea:
                return AriaRoles.TextBox;
            case HtmlElements.Select:
                if (GetAttributeValue(node.Attributes, HtmlAttributes.Multiple) != null)
                {
                    return AriaRoles.ListBox;
                }

                return AriaRoles.ComboBox;
            case HtmlElements.Option:
                return AriaRoles.Option;
            case HtmlElements.Anchor:
                if (GetAttributeValue(node.Attributes, HtmlAttributes.HyperlinkReference) != null)
                {
                    return AriaRoles.Link;
                }

                return null;
            case HtmlElements.Image:
                if (GetAttributeValue(node.Attributes, HtmlAttributes.AlternativeText) == string.Empty)
                {
                    return AriaRoles.Presentation;
                }

                return AriaRoles.Image;
            case HtmlElements.Navigation:
                return AriaRoles.Navigation;
            case HtmlElements.Search:
                return AriaRoles.Search;
            case HtmlElements.Main:
                return AriaRoles.Main;
            case HtmlElements.Aside:
                return AriaRoles.Complementary;
            case HtmlElements.Header:
                return AriaRoles.Banner;
            case HtmlElements.Footer:
                return AriaRoles.ContentInfo;
            case HtmlElements.Form:
                if (HasAccessibleName(node.Attributes))
                {
                    return AriaRoles.Form;
                }

                return null;
            case HtmlElements.Section:
                if (HasAccessibleName(node.Attributes))
                {
                    return AriaRoles.Region;
                }

                return null;
            case HtmlElements.OrderedList:
            case HtmlElements.UnorderedList:
                return AriaRoles.List;
            case HtmlElements.ListItem:
                return AriaRoles.ListItem;
            case HtmlElements.Table:
                return AriaRoles.Table;
            case HtmlElements.TableRow:
                return AriaRoles.Row;
            case HtmlElements.TableHeader:
                return AriaRoles.ColumnHeader;
            case HtmlElements.TableData:
                return AriaRoles.Cell;
            case HtmlElements.Progress:
                return AriaRoles.ProgressBar;
            case HtmlElements.Output:
                return AriaRoles.Status;
            case HtmlElements.HorizontalRule:
                return AriaRoles.Separator;
            case HtmlElements.Dialog:
                return AriaRoles.Dialog;
            case HtmlElements.Details:
            case HtmlElements.FieldSet:
                return AriaRoles.Group;
            case HtmlElements.Heading1:
            case HtmlElements.Heading2:
            case HtmlElements.Heading3:
            case HtmlElements.Heading4:
            case HtmlElements.Heading5:
            case HtmlElements.Heading6:
                return AriaRoles.Heading;
            case HtmlElements.Article:
                return HtmlElements.Article;
            case HtmlElements.Meter:
                return HtmlElements.Meter;
            default:
                return null;
        }
    }

    internal string? GetInputRole(DomNode node)
    {
        string? type = GetAttributeValue(node.Attributes, HtmlAttributes.Type);
        switch (type)
        {
            case HtmlInputTypes.Hidden:
            case HtmlInputTypes.Color:
            case HtmlInputTypes.Date:
            case HtmlInputTypes.Time:
            case HtmlInputTypes.Month:
            case HtmlInputTypes.Week:
            case HtmlInputTypes.LocalDateTime:
            case HtmlInputTypes.File:
                return null;
            case HtmlInputTypes.Checkbox:
                return AriaRoles.Checkbox;
            case HtmlInputTypes.Radio:
                return AriaRoles.Radio;
            case HtmlInputTypes.Range:
                return AriaRoles.Slider;
            case HtmlInputTypes.Number:
                return AriaRoles.SpinButton;
            case HtmlInputTypes.Search:
                return AriaRoles.SearchBox;
            case HtmlInputTypes.Button:
            case HtmlInputTypes.Submit:
            case HtmlInputTypes.Reset:
                return AriaRoles.Button;
            default:
                return AriaRoles.TextBox;
        }
    }

    internal bool HasAccessibleName(DomAttribute[]? attributes)
    {
        string? label = GetAttributeValue(attributes, AriaAttributes.Label);
        if (label != null && label.Length > 0)
        {
            return true;
        }

        string? labelledBy = GetAttributeValue(attributes, AriaAttributes.LabelledBy);
        if (labelledBy != null && labelledBy.Length > 0)
        {
            return true;
        }

        string? title = GetAttributeValue(attributes, HtmlAttributes.Title);
        return title != null && title.Length > 0;
    }

    internal bool SupportsNativeDisabled(DomNode node)
    {
        switch (node.Name)
        {
            case HtmlElements.Button:
            case HtmlElements.Input:
            case HtmlElements.Select:
            case HtmlElements.TextArea:
            case HtmlElements.FieldSet:
                return true;
            default:
                return false;
        }
    }

    internal DomNode AppendLiveAnnouncements(UiNode node, DomNode rendered)
    {
        UiLiveAnnouncement[] nodeAnnouncements = node.Announcements ?? Array.Empty<UiLiveAnnouncement>();
        UiLiveAnnouncement[] accessibilityAnnouncements = Array.Empty<UiLiveAnnouncement>();
        if (node.Accessibility != null)
        {
            accessibilityAnnouncements = node.Accessibility.Announcements ?? Array.Empty<UiLiveAnnouncement>();
        }

        int nodeCount = nodeAnnouncements.Length;
        int accessibilityCount = accessibilityAnnouncements.Length;

        if (nodeCount == 0 && accessibilityCount == 0)
        {
            return rendered;
        }

        List<string> identifiers = new List<string>();
        List<DomNode> additions = new List<DomNode>();
        int index = 0;
        while (index < nodeCount)
        {
            UiLiveAnnouncement? announcement = nodeAnnouncements[index];
            AddLiveAnnouncement(node, rendered, announcement, identifiers, additions);
            index++;
        }

        index = 0;
        while (index < accessibilityCount)
        {
            UiLiveAnnouncement? announcement = accessibilityAnnouncements[index];
            AddLiveAnnouncement(node, rendered, announcement, identifiers, additions);
            index++;
        }

        if (additions.Count == 0)
        {
            return rendered;
        }

        if (rendered.Kind == DomNodeKind.Fragment)
        {
            rendered.Children = AppendNodeLists(rendered.Children, additions);
            return rendered;
        }

        List<DomNode> children = new List<DomNode>();
        children.Add(rendered);
        index = 0;
        while (index < additions.Count)
        {
            children.Add(additions[index]);
            index++;
        }

        return new DomNode
        {
            Kind = DomNodeKind.Fragment,
            Key = rendered.Key,
            Id = rendered.Id,
            Children = children.ToArray(),
            Version = rendered.Version
        };
    }

    internal void AddLiveAnnouncement(UiNode node, DomNode rendered, UiLiveAnnouncement? announcement, List<string> identifiers, List<DomNode> additions)
    {
        if (announcement == null || string.IsNullOrWhiteSpace(announcement.Id))
        {
            throw CreateRenderException(node, "Live announcements require non-empty identifiers.");
        }

        if (ContainsToken(identifiers, announcement.Id))
        {
            throw CreateRenderException(node, "Live announcement identifiers must be unique across accessibility inputs.");
        }

        identifiers.Add(announcement.Id);
        FormatLiveMode(announcement.Mode);
        if (!ContainsLiveAnnouncement(rendered, announcement.Id))
        {
            additions.Add(CreateLiveAnnouncementNode(node, announcement));
        }
    }

    internal bool ContainsLiveAnnouncement(DomNode node, string identifier)
    {
        if (node.Key == identifier && HasClassToken(node.Attributes, "htmlgui-live-announcement"))
        {
            return true;
        }

        DomNode[]? children = node.Children;
        int index = 0;
        while (children != null && index < children.Length)
        {
            DomNode? child = children[index];
            if (child != null && ContainsLiveAnnouncement(child, identifier))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal bool HasClassToken(DomAttribute[]? attributes, string token)
    {
        string? value = GetAttributeValue(attributes, HtmlAttributes.Class);
        List<string> tokens = ReadTokens(value);
        return ContainsToken(tokens, token);
    }

    internal DomNode CreateLiveAnnouncementNode(UiNode node, UiLiveAnnouncement announcement)
    {
        string role = AriaRoles.Status;
        if (announcement.Mode == UiAriaLiveMode.Assertive)
        {
            role = AriaRoles.Alert;
        }

        List<DomAttribute> attributes = new List<DomAttribute>();
        attributes.Add(new DomAttribute { Name = AriaAttributes.Atomic, Value = FormatBoolean(announcement.Atomic) });
        attributes.Add(new DomAttribute { Name = AriaAttributes.Live, Value = FormatLiveMode(announcement.Mode) });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-live-announcement" });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Role, Value = role });
        SortAttributes(attributes);
        string nodeId = GetProjectorNodeId(node);
        string token = StableHash.HashParts(new string[] { nodeId, announcement.Id, "accessibility-announcement" });
        return new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = announcement.Id,
            Id = "ui-accessibility-" + token.Substring(0, 20),
            Name = HtmlElements.Division,
            Attributes = attributes.ToArray(),
            Children = new DomNode[]
            {
                new DomNode
                {
                    Kind = DomNodeKind.Text,
                    Key = "$announcement-text",
                    Id = "ui-accessibility-text-" + token.Substring(0, 20),
                    Text = announcement.Message,
                    Version = GetProjectionVersion(node)
                }
            },
            Version = GetProjectionVersion(node)
        };
    }

    internal int GetProjectionVersion(UiNode node)
    {
        if (node.State == null)
        {
            return 0;
        }

        return node.State.Version;
    }

    internal string GetProjectorNodeId(UiNode node)
    {
        if (node.Identity != null && node.Identity.StableId.Length > 0)
        {
            return node.Identity.StableId;
        }

        return "unknown";
    }

    internal DomNode[] AppendNodeLists(DomNode[]? existing, List<DomNode> additions)
    {
        DomNode[] existingNodes = existing ?? Array.Empty<DomNode>();
        int existingCount = existingNodes.Length;

        DomNode[] result = new DomNode[existingCount + additions.Count];
        int index = 0;
        while (index < existingCount)
        {
            result[index] = existingNodes[index];
            index++;
        }

        int additionIndex = 0;
        while (additionIndex < additions.Count)
        {
            result[index] = additions[additionIndex];
            index++;
            additionIndex++;
        }

        return result;
    }

    internal void ProjectSelectedState(DomNode target, List<DomAttribute> attributes, bool selected)
    {
        if (target.Name == HtmlElements.Option)
        {
            SetBooleanAttribute(attributes, HtmlAttributes.Selected, selected);
            SetBooleanProperty(target, HtmlAttributes.Selected, selected);
            RemoveAttribute(attributes, AriaAttributes.Selected);
            return;
        }

        SetAttribute(attributes, AriaAttributes.Selected, FormatBoolean(selected), false);
    }

    internal void ProjectExpandedState(DomNode target, List<DomAttribute> attributes, bool expanded)
    {
        if (target.Name == HtmlElements.Details)
        {
            SetBooleanAttribute(attributes, HtmlAttributes.Open, expanded);
            SetBooleanProperty(target, HtmlAttributes.Open, expanded);
            RemoveAttribute(attributes, AriaAttributes.Expanded);
            return;
        }

        SetAttribute(attributes, AriaAttributes.Expanded, FormatBoolean(expanded), false);
    }

    internal void ProjectCheckState(DomNode target, List<DomAttribute> attributes, UiCommandCheckState state)
    {
        string? type = GetListAttributeValue(attributes, HtmlAttributes.Type);
        string? role = GetListAttributeValue(attributes, HtmlAttributes.Role);
        bool nativeRole = role == null || role == GetImplicitRole(target);
        if (nativeRole && target.Name == HtmlElements.Input && (type == HtmlInputTypes.Checkbox || type == HtmlInputTypes.Radio) && (type == HtmlInputTypes.Checkbox || state != UiCommandCheckState.Mixed))
        {
            bool isChecked = state == UiCommandCheckState.Checked;
            SetBooleanAttribute(attributes, HtmlAttributes.Checked, isChecked);
            SetBooleanProperty(target, HtmlAttributes.Checked, isChecked);
            if (type == HtmlInputTypes.Checkbox)
            {
                SetBooleanProperty(target, "indeterminate", state == UiCommandCheckState.Mixed);
            }

            RemoveAttribute(attributes, AriaAttributes.Checked);
            return;
        }

        SetAttribute(attributes, AriaAttributes.Checked, FormatCheckState(state), false);
    }

    internal void SetBooleanAttribute(List<DomAttribute> attributes, string name, bool value)
    {
        if (value)
        {
            SetAttribute(attributes, name, string.Empty, true);
        }
        else
        {
            RemoveAttribute(attributes, name);
        }
    }

    internal void SetBooleanProperty(DomNode target, string name, bool value)
    {
        List<DomProperty> properties = CopyProperties(target.Properties);
        int index = FindProperty(properties, name);
        DomProperty property = new DomProperty
        {
            Name = name,
            Value = new UiValue { Kind = UiValueKind.Boolean, BooleanValue = value }
        };
        if (index < 0)
        {
            properties.Add(property);
        }
        else
        {
            properties[index] = property;
        }

        SortProperties(properties);
        target.Properties = properties.ToArray();
    }

    internal string FormatBoolean(bool value)
    {
        if (value)
        {
            return "true";
        }

        return "false";
    }

    internal string FormatCheckState(UiCommandCheckState state)
    {
        switch (state)
        {
            case UiCommandCheckState.Unchecked:
                return "false";
            case UiCommandCheckState.Checked:
                return "true";
            case UiCommandCheckState.Mixed:
                return "mixed";
            default:
                throw new ArgumentOutOfRangeException("state", state, "The accessibility check state is not supported.");
        }
    }

    internal string FormatCurrentKind(UiAriaCurrentKind kind)
    {
        switch (kind)
        {
            case UiAriaCurrentKind.None:
                return "false";
            case UiAriaCurrentKind.Page:
                return "page";
            case UiAriaCurrentKind.Step:
                return "step";
            case UiAriaCurrentKind.Location:
                return "location";
            case UiAriaCurrentKind.Date:
                return "date";
            case UiAriaCurrentKind.Time:
                return "time";
            case UiAriaCurrentKind.True:
                return "true";
            default:
                throw new ArgumentOutOfRangeException("kind", kind, "The ARIA current-item kind is not supported.");
        }
    }

    internal string FormatLiveMode(UiAriaLiveMode mode)
    {
        switch (mode)
        {
            case UiAriaLiveMode.Off:
                return "off";
            case UiAriaLiveMode.Polite:
                return "polite";
            case UiAriaLiveMode.Assertive:
                return "assertive";
            default:
                throw new ArgumentOutOfRangeException("mode", mode, "The ARIA live mode is not supported.");
        }
    }

    internal bool ContainsHtmlWhiteSpace(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == ' ' || character == '\t' || character == '\r' || character == '\n' || character == '\f')
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal List<string> ReadTokens(string? value)
    {
        List<string> result = new List<string>();
        if (value == null || value.Length == 0)
        {
            return result;
        }

        string[] tokens = value.Split(new char[] { ' ', '\t', '\r', '\n', '\f' }, StringSplitOptions.RemoveEmptyEntries);
        int index = 0;
        while (index < tokens.Length)
        {
            if (!ContainsToken(result, tokens[index]))
            {
                result.Add(tokens[index]);
            }

            index++;
        }

        return result;
    }

    internal bool ContainsToken(List<string> tokens, string candidate)
    {
        int index = 0;
        while (index < tokens.Count)
        {
            if (tokens[index] == candidate)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal List<DomAttribute> CopyAttributes(DomAttribute[]? supplied)
    {
        List<DomAttribute> result = new List<DomAttribute>();
        if (supplied == null)
        {
            return result;
        }

        int index = 0;
        while (index < supplied.Length)
        {
            DomAttribute? attribute = supplied[index];
            if (attribute != null)
            {
                result.Add(attribute);
            }

            index++;
        }

        return result;
    }

    internal List<DomProperty> CopyProperties(DomProperty[]? supplied)
    {
        List<DomProperty> result = new List<DomProperty>();
        if (supplied == null)
        {
            return result;
        }

        int index = 0;
        while (index < supplied.Length)
        {
            DomProperty? property = supplied[index];
            if (property != null)
            {
                result.Add(property);
            }

            index++;
        }

        return result;
    }

    internal string? GetAttributeValue(DomAttribute[]? attributes, string name)
    {
        if (attributes == null)
        {
            return null;
        }

        int index = 0;
        while (index < attributes.Length)
        {
            DomAttribute? attribute = attributes[index];
            if (attribute != null && attribute.Name == name)
            {
                return attribute.Value;
            }

            index++;
        }

        return null;
    }

    internal string? GetListAttributeValue(List<DomAttribute> attributes, string name)
    {
        int index = FindAttribute(attributes, name);
        if (index < 0)
        {
            return null;
        }

        return attributes[index].Value;
    }

    internal int FindAttribute(List<DomAttribute> attributes, string name)
    {
        int index = 0;
        while (index < attributes.Count)
        {
            if (attributes[index].Name == name)
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    internal void SetAttribute(List<DomAttribute> attributes, string name, string value, bool isBoolean)
    {
        int index = FindAttribute(attributes, name);
        DomAttribute attribute = new DomAttribute { Name = name, Value = value, IsBoolean = isBoolean };
        if (index < 0)
        {
            attributes.Add(attribute);
        }
        else
        {
            attributes[index] = attribute;
        }
    }

    internal void RemoveAttribute(List<DomAttribute> attributes, string name)
    {
        int index = attributes.Count - 1;
        while (index >= 0)
        {
            if (attributes[index].Name == name)
            {
                attributes.RemoveAt(index);
            }

            index--;
        }
    }

    internal int FindProperty(List<DomProperty> properties, string name)
    {
        int index = 0;
        while (index < properties.Count)
        {
            if (properties[index].Name == name)
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    internal bool HasProperty(DomProperty[]? properties, string name)
    {
        if (properties == null)
        {
            return false;
        }

        int index = 0;
        while (index < properties.Length)
        {
            DomProperty? property = properties[index];
            if (property != null && property.Name == name)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal void SetAttributeIfMissing(List<DomAttribute> attributes, string name, string value, bool isBoolean)
    {
        if (FindAttribute(attributes, name) < 0)
        {
            attributes.Add(new DomAttribute { Name = name, Value = value, IsBoolean = isBoolean });
        }
    }

    internal void SortAttributes(List<DomAttribute> attributes)
    {
        int index = 1;
        while (index < attributes.Count)
        {
            DomAttribute candidate = attributes[index];
            int insertionIndex = index - 1;
            while (insertionIndex >= 0 && string.CompareOrdinal(attributes[insertionIndex].Name, candidate.Name) > 0)
            {
                attributes[insertionIndex + 1] = attributes[insertionIndex];
                insertionIndex--;
            }

            attributes[insertionIndex + 1] = candidate;
            index++;
        }
    }

    internal void SortProperties(List<DomProperty> properties)
    {
        int index = 1;
        while (index < properties.Count)
        {
            DomProperty candidate = properties[index];
            int insertionIndex = index - 1;
            while (insertionIndex >= 0 && string.CompareOrdinal(properties[insertionIndex].Name, candidate.Name) > 0)
            {
                properties[insertionIndex + 1] = properties[insertionIndex];
                insertionIndex--;
            }

            properties[insertionIndex + 1] = candidate;
            index++;
        }
    }

    internal UiRenderException CreateRenderException(UiNode node, string message)
    {
        string nodeId = "unknown";
        if (node.Identity != null && node.Identity.StableId.Length > 0)
        {
            nodeId = node.Identity.StableId;
        }

        return new UiRenderException(message, nodeId, "dom");
    }
}
