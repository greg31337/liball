using System;
using System.Collections.Generic;

namespace htmlgui;

internal class FeedbackTemplateExpander
{
    internal DomNode ExpandFeedback(UiNode node, DomNode[] logicalChildren)
    {
        UiFeedback? feedback = node.Feedback;
        if (feedback == null)
        {
            if (IsLiveAnnouncementName(node.Name))
            {
                return ExpandLiveAnnouncements(node);
            }

            throw CreateRenderException(node, "A feedback control requires a feedback description.");
        }

        DomNode result;
        switch (feedback.Kind)
        {
            case "progress":
                result = ExpandProgress(node, feedback, logicalChildren);
                break;
            case "meter":
                result = ExpandMeter(node, feedback, logicalChildren);
                break;
            case "spinner":
                result = ExpandSpinner(node, feedback, logicalChildren);
                break;
            case "skeleton":
                result = ExpandSkeleton(node, feedback, logicalChildren);
                break;
            case "empty":
            case "empty-state":
                result = ExpandState(node, feedback, logicalChildren, "empty-state", AriaRoles.Status, false);
                break;
            case "error":
            case "error-state":
                result = ExpandState(node, feedback, logicalChildren, "error-state", AriaRoles.Alert, false);
                break;
            case "loading":
            case "loading-state":
                result = ExpandState(node, feedback, logicalChildren, "loading-state", AriaRoles.Status, true);
                break;
            case "alert":
                result = ExpandMessage(node, feedback, logicalChildren, "alert", AriaRoles.Alert, HtmlElements.Division);
                break;
            case "toast":
                result = ExpandToast(node, feedback, logicalChildren);
                break;
            case "status":
                result = ExpandMessage(node, feedback, logicalChildren, "status", AriaRoles.Status, HtmlElements.Output);
                break;
            case "live-announcement":
            case "live-announcements":
                result = ExpandLiveAnnouncements(node);
                break;
            default:
                throw CreateRenderException(node, "The feedback kind is not supported.");
        }

        if (feedback.Kind != "live-announcement" && feedback.Kind != "live-announcements")
        {
            result.Children = AppendAnnouncements(node, result.Children);
        }

        return result;
    }

    internal bool IsFeedbackControlName(string name)
    {
        switch (name)
        {
            case "progress":
            case "meter":
            case "spinner":
            case "skeleton":
            case "empty":
            case "empty-state":
            case "error":
            case "error-state":
            case "loading":
            case "loading-state":
            case "alert":
            case "toast":
            case "status":
            case "live-announcement":
            case "live-announcements":
                return true;
            default:
                return false;
        }
    }

    internal bool IsLiveAnnouncementName(string name)
    {
        return name == "live-announcement" || name == "live-announcements";
    }

    internal DomNode ExpandProgress(UiNode node, UiFeedback feedback, DomNode[] logicalChildren)
    {
        ValidateRange(feedback);
        List<DomAttribute> attributes = CreateCommonAttributes(feedback, "progress", string.Empty, feedback.IsBusy);
        if (feedback.HasMinimum)
        {
            attributes.Add(new DomAttribute { Name = AriaAttributes.ValueMinimum, Value = InvariantFormatter.FormatDouble(feedback.Minimum) });
        }

        if (feedback.HasMaximum)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.Maximum, Value = InvariantFormatter.FormatDouble(feedback.Maximum) });
        }

        if (feedback.HasValue)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.Value, Value = InvariantFormatter.FormatDouble(feedback.Value) });
        }

        SortAttributes(attributes);
        return CreateElement(node, HtmlElements.Progress, attributes.ToArray(), PrependMessage(node, feedback.Message, logicalChildren));
    }

    internal DomNode ExpandMeter(UiNode node, UiFeedback feedback, DomNode[] logicalChildren)
    {
        ValidateRange(feedback);
        List<DomAttribute> attributes = CreateCommonAttributes(feedback, "meter", string.Empty, feedback.IsBusy);
        if (feedback.HasMinimum)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.Minimum, Value = InvariantFormatter.FormatDouble(feedback.Minimum) });
        }

        if (feedback.HasMaximum)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.Maximum, Value = InvariantFormatter.FormatDouble(feedback.Maximum) });
        }

        if (feedback.HasValue)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.Value, Value = InvariantFormatter.FormatDouble(feedback.Value) });
        }

        SortAttributes(attributes);
        return CreateElement(node, HtmlElements.Meter, attributes.ToArray(), PrependMessage(node, feedback.Message, logicalChildren));
    }

    internal DomNode ExpandSpinner(UiNode node, UiFeedback feedback, DomNode[] logicalChildren)
    {
        List<DomAttribute> attributes = CreateCommonAttributes(feedback, "spinner", AriaRoles.Status, true);
        SortAttributes(attributes);
        return CreateElement(node, HtmlElements.Division, attributes.ToArray(), PrependMessage(node, feedback.Message, logicalChildren));
    }

    internal DomNode ExpandSkeleton(UiNode node, UiFeedback feedback, DomNode[] logicalChildren)
    {
        List<DomAttribute> attributes = new List<DomAttribute>();
        attributes.Add(new DomAttribute { Name = AriaAttributes.Hidden, Value = "true" });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-skeleton" });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "severity", Value = GetSeverityToken(feedback.Severity) });
        SortAttributes(attributes);
        return CreateElement(node, HtmlElements.Division, attributes.ToArray(), PrependMessage(node, feedback.Message, logicalChildren));
    }

    internal DomNode ExpandState(UiNode node, UiFeedback feedback, DomNode[] logicalChildren, string classToken, string role, bool forceBusy)
    {
        List<DomAttribute> attributes = CreateCommonAttributes(feedback, classToken, role, forceBusy || feedback.IsBusy);
        SortAttributes(attributes);
        return CreateElement(node, HtmlElements.Section, attributes.ToArray(), PrependMessage(node, feedback.Message, logicalChildren));
    }

    internal DomNode ExpandMessage(UiNode node, UiFeedback feedback, DomNode[] logicalChildren, string classToken, string role, string elementName)
    {
        List<DomAttribute> attributes = CreateCommonAttributes(feedback, classToken, role, feedback.IsBusy);
        SortAttributes(attributes);
        return CreateElement(node, elementName, attributes.ToArray(), PrependMessage(node, feedback.Message, logicalChildren));
    }

    internal DomNode ExpandToast(UiNode node, UiFeedback feedback, DomNode[] logicalChildren)
    {
        string role = AriaRoles.Status;
        if (feedback.LiveMode == UiAriaLiveMode.Assertive || feedback.Severity == UiValidationSeverity.Error)
        {
            role = AriaRoles.Alert;
        }

        return ExpandMessage(node, feedback, logicalChildren, "toast", role, HtmlElements.Division);
    }

    internal DomNode ExpandToastOverlay(UiNode node, DomNode[] logicalChildren)
    {
        UiToast? toast = node.Toast;
        if (toast == null || toast.State == null)
        {
            throw CreateRenderException(node, "A toast control requires a toast description and state.");
        }

        UiFeedback feedback = new UiFeedback
        {
            Kind = "toast",
            Message = toast.Message,
            Severity = toast.Severity,
            LiveMode = toast.LiveMode
        };
        DomNode[] toastChildren = logicalChildren;
        if (toast.Action != null)
        {
            toastChildren = new DomNode[logicalChildren.Length + 1];
            Array.Copy(logicalChildren, toastChildren, logicalChildren.Length);
            toastChildren[logicalChildren.Length] = ExpandToastAction(node, toast);
        }

        DomNode result = ExpandToast(node, feedback, toastChildren);
        List<DomAttribute> attributes = new List<DomAttribute>();
        int index = 0;
        while (index < result.Attributes.Length)
        {
            attributes.Add(result.Attributes[index]);
            index++;
        }

        if (toast.Key.Length > 0)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "toast-key", Value = toast.Key });
        }

        if (!toast.State.IsOpen)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.Hidden, Value = string.Empty, IsBoolean = true });
        }

        if (toast.HasDurationMilliseconds)
        {
            if (toast.DurationMilliseconds < 1)
            {
                throw new ArgumentOutOfRangeException("durationMilliseconds", toast.DurationMilliseconds, "Toast durations must be positive.");
            }

            attributes.Add(new DomAttribute
            {
                Name = HtmlAttributes.DataPrefix + "duration-milliseconds",
                Value = InvariantFormatter.FormatInt32(toast.DurationMilliseconds)
            });
        }

        SortAttributes(attributes);
        result.Attributes = attributes.ToArray();
        result.Children = AppendAnnouncements(node, result.Children);
        return result;
    }

    internal DomNode ExpandToastAction(UiNode node, UiToast toast)
    {
        UiButton? action = toast.Action;
        if (action == null)
        {
            throw CreateRenderException(node, "A toast action requires a button description.");
        }

        UiNodeState state = new UiNodeState();
        state.Version = GetNodeVersion(node);
        if (node.State != null)
        {
            state.IsEnabled = node.State.IsEnabled;
            state.IsVisible = node.State.IsVisible;
            state.IsBusy = node.State.IsBusy;
        }

        string actionIdentity = toast.Key;
        if (actionIdentity.Length == 0)
        {
            actionIdentity = action.CommandId ?? action.Label;
        }

        UiNode actionNode = new UiNode
        {
            Kind = UiNodeKind.Control,
            Name = "button",
            Button = action,
            Identity = new UiNodeIdentity
            {
                AuthorKey = "$toast-action",
                SiblingIdentity = "$toast-action",
                StableId = CreateSyntheticId(node, "toast-action", actionIdentity)
            },
            State = state
        };
        return new ButtonTemplateExpander().ExpandButton(actionNode, Array.Empty<DomNode>());
    }

    internal DomNode ExpandLiveAnnouncements(UiNode node)
    {
        DomNode[] announcements = CreateAnnouncementNodes(node);
        return CreateElement(
            node,
            HtmlElements.Division,
            new DomAttribute[] { new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-live-announcements" } },
            announcements);
    }

    internal List<DomAttribute> CreateCommonAttributes(UiFeedback feedback, string classToken, string role, bool isBusy)
    {
        List<DomAttribute> attributes = new List<DomAttribute>();
        attributes.Add(new DomAttribute { Name = AriaAttributes.Live, Value = GetLiveModeToken(feedback.LiveMode) });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-" + classToken });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "severity", Value = GetSeverityToken(feedback.Severity) });
        if (role.Length > 0)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.Role, Value = role });
        }

        if (isBusy)
        {
            attributes.Add(new DomAttribute { Name = AriaAttributes.Busy, Value = "true" });
        }

        return attributes;
    }

    internal DomNode[] AppendAnnouncements(UiNode node, DomNode[] children)
    {
        DomNode[] announcements = CreateAnnouncementNodes(node);
        if (announcements.Length == 0)
        {
            return children;
        }

        DomNode[] result = new DomNode[children.Length + announcements.Length];
        Array.Copy(children, result, children.Length);
        Array.Copy(announcements, 0, result, children.Length, announcements.Length);
        return result;
    }

    internal DomNode[] CreateAnnouncementNodes(UiNode node)
    {
        UiLiveAnnouncement[]? announcements = node.Announcements;
        if (announcements == null || announcements.Length == 0)
        {
            return Array.Empty<DomNode>();
        }

        DomNode[] result = new DomNode[announcements.Length];
        Dictionary<string, string> ids = new Dictionary<string, string>(StringComparer.Ordinal);
        int index = 0;
        while (index < announcements.Length)
        {
            UiLiveAnnouncement? announcement = announcements[index];
            if (announcement == null || string.IsNullOrWhiteSpace(announcement.Id))
            {
                throw CreateRenderException(node, "Live announcements require non-empty identifiers.");
            }

            if (ids.ContainsKey(announcement.Id))
            {
                throw CreateRenderException(node, "Live announcement identifiers must be unique.");
            }

            ids.Add(announcement.Id, announcement.Id);
            string role = AriaRoles.Status;
            if (announcement.Mode == UiAriaLiveMode.Assertive)
            {
                role = AriaRoles.Alert;
            }

            List<DomAttribute> attributes = new List<DomAttribute>();
            attributes.Add(new DomAttribute { Name = AriaAttributes.Atomic, Value = FormatBoolean(announcement.Atomic) });
            attributes.Add(new DomAttribute { Name = AriaAttributes.Live, Value = GetLiveModeToken(announcement.Mode) });
            attributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-live-announcement" });
            attributes.Add(new DomAttribute { Name = HtmlAttributes.Role, Value = role });
            SortAttributes(attributes);
            result[index] = new DomNode
            {
                Kind = DomNodeKind.Element,
                Key = announcement.Id,
                Id = CreateSyntheticId(node, "announcement", announcement.Id),
                Name = HtmlElements.Division,
                Attributes = attributes.ToArray(),
                Children = new DomNode[] { CreateText(node, "$announcement-text:" + announcement.Id, "announcement-text", announcement.Id, announcement.Message) },
                Version = GetNodeVersion(node)
            };
            index++;
        }

        return result;
    }

    internal void ValidateRange(UiFeedback feedback)
    {
        double minimum = 0.0;
        double maximum = 1.0;
        if (feedback.HasMinimum)
        {
            ValidateFinite(feedback.Minimum, "minimum");
            minimum = feedback.Minimum;
        }

        if (feedback.HasMaximum)
        {
            ValidateFinite(feedback.Maximum, "maximum");
            maximum = feedback.Maximum;
        }

        if (maximum <= minimum)
        {
            throw new ArgumentOutOfRangeException("maximum", maximum, "The maximum must be greater than the minimum.");
        }

        if (feedback.HasValue)
        {
            ValidateFinite(feedback.Value, "value");
            if (feedback.Value < minimum || feedback.Value > maximum)
            {
                throw new ArgumentOutOfRangeException("value", feedback.Value, "The value must be within the supplied range.");
            }
        }
    }

    internal void ValidateFinite(double value, string parameterName)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Feedback numeric values must be finite.");
        }
    }

    internal string GetSeverityToken(UiValidationSeverity severity)
    {
        switch (severity)
        {
            case UiValidationSeverity.Information:
                return "information";
            case UiValidationSeverity.Warning:
                return "warning";
            case UiValidationSeverity.Error:
                return "error";
            default:
                throw new ArgumentOutOfRangeException("severity", severity, "The feedback severity is not supported.");
        }
    }

    internal string GetLiveModeToken(UiAriaLiveMode mode)
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
                throw new ArgumentOutOfRangeException("liveMode", mode, "The live-region mode is not supported.");
        }
    }

    internal string FormatBoolean(bool value)
    {
        if (value)
        {
            return "true";
        }

        return "false";
    }

    internal DomNode[] PrependMessage(UiNode node, string message, DomNode[] logicalChildren)
    {
        DomNode[] children = new DomNode[logicalChildren.Length + 1];
        children[0] = CreateText(node, "$feedback-message", "feedback-message", string.Empty, message);
        Array.Copy(logicalChildren, 0, children, 1, logicalChildren.Length);
        return children;
    }

    internal DomNode CreateText(UiNode node, string key, string suffix, string discriminator, string text)
    {
        return new DomNode
        {
            Kind = DomNodeKind.Text,
            Key = key,
            Id = CreateSyntheticId(node, suffix, discriminator),
            Text = text,
            Version = GetNodeVersion(node)
        };
    }

    internal DomNode CreateElement(UiNode node, string name, DomAttribute[] attributes, DomNode[] children)
    {
        return new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = GetNodeKey(node),
            Id = GetNodeId(node),
            Name = name,
            Attributes = attributes,
            Children = children,
            Version = GetNodeVersion(node)
        };
    }

    internal string CreateSyntheticId(UiNode node, string suffix, string discriminator)
    {
        string token = StableHash.HashParts(new string[] { GetNodeId(node), GetNodeKey(node), "feedback", suffix, discriminator });
        return "ui-feedback-" + token.Substring(0, 20);
    }

    internal string GetNodeKey(UiNode node)
    {
        if (node.Identity == null)
        {
            return string.Empty;
        }

        if (node.Identity.AuthorKey.Length > 0)
        {
            return node.Identity.AuthorKey;
        }

        return node.Identity.SiblingIdentity;
    }

    internal string GetNodeId(UiNode node)
    {
        if (node.Identity == null)
        {
            return string.Empty;
        }

        return node.Identity.StableId;
    }

    internal int GetNodeVersion(UiNode node)
    {
        if (node.State == null)
        {
            return 0;
        }

        return node.State.Version;
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

    internal UiRenderException CreateRenderException(UiNode node, string message)
    {
        string nodeId = GetNodeId(node);
        if (nodeId.Length == 0)
        {
            nodeId = "unknown";
        }

        return new UiRenderException(message, nodeId, "dom");
    }
}
