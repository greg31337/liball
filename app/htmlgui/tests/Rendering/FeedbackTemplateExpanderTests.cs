using System;
using System.Globalization;
using htmlgui;

namespace htmlgui.Tests;

public class FeedbackTemplateExpanderTests
{
    [Test]
    public void ExpandsDeterminateAndIndeterminateProgressWithNativeSemantics()
    {
        UiNode determinate = FeedbackControl("progress", "Loading files");
        determinate.State.Version = 8;
        determinate.Feedback!.Severity = UiValidationSeverity.Warning;
        determinate.Feedback.LiveMode = UiAriaLiveMode.Polite;
        determinate.Feedback.HasMinimum = true;
        determinate.Feedback.Minimum = 10;
        determinate.Feedback.HasMaximum = true;
        determinate.Feedback.Maximum = 20;
        determinate.Feedback.HasValue = true;
        determinate.Feedback.Value = 12.5;
        determinate.Feedback.IsBusy = true;
        determinate.Children = new UiNode[] { LowLevelText("detail", "detail-id", " remaining") };
        UiNode indeterminate = FeedbackControl("progress", "Waiting");
        SemanticExpander expander = new SemanticExpander();
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        DomNode expandedDeterminate;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
            expandedDeterminate = expander.ExpandNode(determinate);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }

        DomNode expandedIndeterminate = expander.ExpandNode(indeterminate);

        Assert.Multiple(() =>
        {
            Assert.That(expandedDeterminate.Name, Is.EqualTo(HtmlElements.Progress));
            Assert.That(expandedDeterminate.Key, Is.EqualTo("feedback"));
            Assert.That(expandedDeterminate.Id, Is.EqualTo("feedback-id"));
            Assert.That(expandedDeterminate.Version, Is.EqualTo(8));
            Assert.That(AttributeValue(expandedDeterminate, AriaAttributes.ValueMinimum), Is.EqualTo("10"));
            Assert.That(AttributeValue(expandedDeterminate, HtmlAttributes.Maximum), Is.EqualTo("20"));
            Assert.That(AttributeValue(expandedDeterminate, HtmlAttributes.Value), Is.EqualTo("12.5"));
            Assert.That(AttributeValue(expandedDeterminate, AriaAttributes.Busy), Is.EqualTo("true"));
            Assert.That(AttributeValue(expandedDeterminate, AriaAttributes.Live), Is.EqualTo("polite"));
            Assert.That(AttributeValue(expandedDeterminate, "data-severity"), Is.EqualTo("warning"));
            Assert.That(expandedDeterminate.Children[0].Text, Is.EqualTo("Loading files"));
            Assert.That(expandedDeterminate.Children[1].Text, Is.EqualTo(" remaining"));
            Assert.That(expandedIndeterminate.Name, Is.EqualTo(HtmlElements.Progress));
            Assert.That(HasAttribute(expandedIndeterminate, HtmlAttributes.Value), Is.False);
            Assert.That(HasAttribute(expandedIndeterminate, HtmlAttributes.Maximum), Is.False);
        });
    }

    [Test]
    public void ExpandsMeterWithNativeMinimumMaximumAndValue()
    {
        UiNode node = FeedbackControl("meter", "Storage used");
        node.Feedback!.HasMinimum = true;
        node.Feedback.Minimum = -10;
        node.Feedback.HasMaximum = true;
        node.Feedback.Maximum = 10;
        node.Feedback.HasValue = true;
        node.Feedback.Value = -2.5;

        DomNode result = new SemanticExpander().ExpandNode(node);

        Assert.Multiple(() =>
        {
            Assert.That(result.Name, Is.EqualTo(HtmlElements.Meter));
            Assert.That(AttributeValue(result, HtmlAttributes.Minimum), Is.EqualTo("-10"));
            Assert.That(AttributeValue(result, HtmlAttributes.Maximum), Is.EqualTo("10"));
            Assert.That(AttributeValue(result, HtmlAttributes.Value), Is.EqualTo("-2.5"));
            Assert.That(result.Children[0].Text, Is.EqualTo("Storage used"));
        });
    }

    [TestCase("spinner", HtmlElements.Division, AriaRoles.Status, "true", "")]
    [TestCase("skeleton", HtmlElements.Division, "", "", "true")]
    [TestCase("empty-state", HtmlElements.Section, AriaRoles.Status, "", "")]
    [TestCase("error-state", HtmlElements.Section, AriaRoles.Alert, "", "")]
    [TestCase("loading-state", HtmlElements.Section, AriaRoles.Status, "true", "")]
    [TestCase("alert", HtmlElements.Division, AriaRoles.Alert, "", "")]
    [TestCase("toast", HtmlElements.Division, AriaRoles.Status, "", "")]
    [TestCase("status", HtmlElements.Output, "", "", "")]
    public void ExpandsEveryMessageAndPlaceholderFeedbackKind(
        string kind,
        string elementName,
        string role,
        string busy,
        string hidden)
    {
        UiNode node = FeedbackControl(kind, "Feedback message");
        node.Feedback!.LiveMode = UiAriaLiveMode.Polite;

        DomNode result = new SemanticExpander().ExpandNode(node);

        Assert.Multiple(() =>
        {
            Assert.That(result.Name, Is.EqualTo(elementName));
            Assert.That(AttributeValue(result, HtmlAttributes.Class), Is.EqualTo("htmlgui-" + kind));
            Assert.That(OptionalAttributeValue(result, HtmlAttributes.Role), Is.EqualTo(role));
            Assert.That(OptionalAttributeValue(result, AriaAttributes.Busy), Is.EqualTo(busy));
            Assert.That(OptionalAttributeValue(result, AriaAttributes.Hidden), Is.EqualTo(hidden));
            Assert.That(result.Children[0].Text, Is.EqualTo("Feedback message"));
        });
    }

    [TestCase("empty", "htmlgui-empty-state", AriaRoles.Status)]
    [TestCase("error", "htmlgui-error-state", AriaRoles.Alert)]
    [TestCase("loading", "htmlgui-loading-state", AriaRoles.Status)]
    public void SupportsConciseStateAliases(string kind, string className, string role)
    {
        UiNode node = FeedbackControl(kind, "State");

        DomNode result = new SemanticExpander().ExpandNode(node);

        Assert.Multiple(() =>
        {
            Assert.That(AttributeValue(result, HtmlAttributes.Class), Is.EqualTo(className));
            Assert.That(AttributeValue(result, HtmlAttributes.Role), Is.EqualTo(role));
        });
    }

    [Test]
    public void ErrorAndAssertiveToastsUseAlertWhilePoliteToastsUseStatus()
    {
        UiNode polite = FeedbackControl("toast", "Saved");
        polite.Feedback!.LiveMode = UiAriaLiveMode.Polite;
        UiNode assertive = FeedbackControl("toast", "Disconnected");
        assertive.Feedback!.LiveMode = UiAriaLiveMode.Assertive;
        UiNode error = FeedbackControl("toast", "Failed");
        error.Feedback!.Severity = UiValidationSeverity.Error;
        SemanticExpander expander = new SemanticExpander();

        DomNode expandedPolite = expander.ExpandNode(polite);
        DomNode expandedAssertive = expander.ExpandNode(assertive);
        DomNode expandedError = expander.ExpandNode(error);

        Assert.Multiple(() =>
        {
            Assert.That(AttributeValue(expandedPolite, HtmlAttributes.Role), Is.EqualTo(AriaRoles.Status));
            Assert.That(AttributeValue(expandedAssertive, HtmlAttributes.Role), Is.EqualTo(AriaRoles.Alert));
            Assert.That(AttributeValue(expandedAssertive, AriaAttributes.Live), Is.EqualTo("assertive"));
            Assert.That(AttributeValue(expandedError, HtmlAttributes.Role), Is.EqualTo(AriaRoles.Alert));
        });
    }

    [Test]
    public void ExpandsDedicatedToastPayloadWithVisibilityDurationAndLiveSemantics()
    {
        UiNode open = new UiNode
        {
            Kind = UiNodeKind.Control,
            Identity = new UiNodeIdentity { AuthorKey = "toast", SiblingIdentity = "toast", StableId = "toast-id" },
            Toast = new UiToast
            {
                Key = "save-toast",
                Message = "Changes saved",
                Severity = UiValidationSeverity.Warning,
                LiveMode = UiAriaLiveMode.Polite,
                State = new UiOverlayState { IsOpen = true },
                HasDurationMilliseconds = true,
                DurationMilliseconds = 5000
            }
        };
        UiNode closed = new UiNode
        {
            Kind = UiNodeKind.Control,
            Identity = new UiNodeIdentity { AuthorKey = "closed", SiblingIdentity = "closed", StableId = "closed-id" },
            Toast = new UiToast
            {
                Key = "error-toast",
                Message = "Save failed",
                Severity = UiValidationSeverity.Error,
                LiveMode = UiAriaLiveMode.Assertive
            }
        };
        SemanticExpander expander = new SemanticExpander();

        DomNode expandedOpen = expander.ExpandNode(open);
        DomNode expandedClosed = expander.ExpandNode(closed);

        Assert.Multiple(() =>
        {
            Assert.That(expandedOpen.Name, Is.EqualTo(HtmlElements.Division));
            Assert.That(AttributeValue(expandedOpen, HtmlAttributes.Class), Is.EqualTo("htmlgui-toast"));
            Assert.That(AttributeValue(expandedOpen, HtmlAttributes.Role), Is.EqualTo(AriaRoles.Status));
            Assert.That(AttributeValue(expandedOpen, AriaAttributes.Live), Is.EqualTo("polite"));
            Assert.That(AttributeValue(expandedOpen, "data-severity"), Is.EqualTo("warning"));
            Assert.That(AttributeValue(expandedOpen, "data-toast-key"), Is.EqualTo("save-toast"));
            Assert.That(AttributeValue(expandedOpen, "data-duration-milliseconds"), Is.EqualTo("5000"));
            Assert.That(HasAttribute(expandedOpen, HtmlAttributes.Hidden), Is.False);
            Assert.That(expandedOpen.Children[0].Text, Is.EqualTo("Changes saved"));
            Assert.That(AttributeValue(expandedClosed, HtmlAttributes.Role), Is.EqualTo(AriaRoles.Alert));
            Assert.That(HasAttribute(expandedClosed, HtmlAttributes.Hidden), Is.True);
        });
    }

    [Test]
    public void ToastAutoDismissalAndVisibilityChangesPreserveIdentity()
    {
        UiNode node = new UiNode
        {
            Kind = UiNodeKind.Control,
            Identity = new UiNodeIdentity { AuthorKey = "toast", SiblingIdentity = "toast", StableId = "toast-id" },
            Toast = new UiToast
            {
                Key = "session-toast",
                Message = "Session refreshed",
                State = new UiOverlayState { IsOpen = true },
                HasDurationMilliseconds = true,
                DurationMilliseconds = 1000
            }
        };
        SemanticExpander expander = new SemanticExpander();

        DomNode open = expander.ExpandNode(node);
        string messageId = open.Children[0].Id;
        node.Toast.State.IsOpen = false;
        node.Toast.DurationMilliseconds = 2500;
        DomNode dismissed = expander.ExpandNode(node);

        Assert.Multiple(() =>
        {
            Assert.That(HasAttribute(open, HtmlAttributes.Hidden), Is.False);
            Assert.That(AttributeValue(open, "data-duration-milliseconds"), Is.EqualTo("1000"));
            Assert.That(HasAttribute(dismissed, HtmlAttributes.Hidden), Is.True);
            Assert.That(AttributeValue(dismissed, "data-duration-milliseconds"), Is.EqualTo("2500"));
            Assert.That(dismissed.Id, Is.EqualTo(open.Id));
            Assert.That(dismissed.Children[0].Id, Is.EqualTo(messageId));
            Assert.That(dismissed.Children[0].Text, Is.EqualTo("Session refreshed"));
        });
    }

    [Test]
    public void ExpandsOrderedLiveAnnouncementsWithStableIdsAndModes()
    {
        UiNode node = new UiNode
        {
            Kind = UiNodeKind.Control,
            Name = "live-announcements",
            Identity = new UiNodeIdentity { AuthorKey = "announcer", SiblingIdentity = "announcer", StableId = "announcer-id" },
            Announcements = new UiLiveAnnouncement[]
            {
                new UiLiveAnnouncement { Id = "saved", Message = "Saved", Mode = UiAriaLiveMode.Polite, Atomic = true },
                new UiLiveAnnouncement { Id = "failed", Message = "Failed", Mode = UiAriaLiveMode.Assertive },
                new UiLiveAnnouncement { Id = "silent", Message = "Silent", Mode = UiAriaLiveMode.Off }
            }
        };
        SemanticExpander expander = new SemanticExpander();

        DomNode first = expander.ExpandNode(node);
        DomNode second = expander.ExpandNode(node);

        Assert.Multiple(() =>
        {
            Assert.That(first.Name, Is.EqualTo(HtmlElements.Division));
            Assert.That(AttributeValue(first, HtmlAttributes.Class), Is.EqualTo("htmlgui-live-announcements"));
            Assert.That(Array.ConvertAll(first.Children, child => child.Key), Is.EqualTo(new string[] { "saved", "failed", "silent" }));
            Assert.That(AttributeValue(first.Children[0], AriaAttributes.Live), Is.EqualTo("polite"));
            Assert.That(AttributeValue(first.Children[0], AriaAttributes.Atomic), Is.EqualTo("true"));
            Assert.That(AttributeValue(first.Children[0], HtmlAttributes.Role), Is.EqualTo(AriaRoles.Status));
            Assert.That(first.Children[0].Children[0].Text, Is.EqualTo("Saved"));
            Assert.That(AttributeValue(first.Children[1], AriaAttributes.Live), Is.EqualTo("assertive"));
            Assert.That(AttributeValue(first.Children[1], HtmlAttributes.Role), Is.EqualTo(AriaRoles.Alert));
            Assert.That(AttributeValue(first.Children[2], AriaAttributes.Live), Is.EqualTo("off"));
            Assert.That(first.Children[0].Id, Is.EqualTo(second.Children[0].Id));
            Assert.That(first.Children[1].Id, Is.EqualTo(second.Children[1].Id));
            Assert.That(first.Children[0], Is.Not.SameAs(second.Children[0]));
        });
    }

    [Test]
    public void ExpandsStandaloneAnnouncementsWithoutAControlName()
    {
        UiNode node = new UiNode
        {
            Kind = UiNodeKind.Control,
            Identity = new UiNodeIdentity { AuthorKey = "live", SiblingIdentity = "live", StableId = "live-id" },
            Announcements = new UiLiveAnnouncement[]
            {
                new UiLiveAnnouncement { Id = "ready", Message = "Ready", Mode = UiAriaLiveMode.Polite }
            }
        };

        DomNode result = new SemanticExpander().ExpandNode(node);

        Assert.Multiple(() =>
        {
            Assert.That(AttributeValue(result, HtmlAttributes.Class), Is.EqualTo("htmlgui-live-announcements"));
            Assert.That(result.Children, Has.Length.EqualTo(1));
            Assert.That(result.Children[0].Key, Is.EqualTo("ready"));
            Assert.That(result.Children[0].Children[0].Text, Is.EqualTo("Ready"));
        });
    }

    [Test]
    public void AppendsAnnouncementsAfterFeedbackMessageAndLogicalChildren()
    {
        UiNode node = FeedbackControl("status", "Upload complete");
        node.Children = new UiNode[] { LowLevelText("detail", "detail-id", " at noon") };
        node.Announcements = new UiLiveAnnouncement[]
        {
            new UiLiveAnnouncement { Id = "upload", Message = "Upload completed", Atomic = true }
        };

        DomNode result = new SemanticExpander().ExpandNode(node);

        Assert.Multiple(() =>
        {
            Assert.That(result.Children, Has.Length.EqualTo(3));
            Assert.That(result.Children[0].Text, Is.EqualTo("Upload complete"));
            Assert.That(result.Children[1].Text, Is.EqualTo(" at noon"));
            Assert.That(result.Children[2].Key, Is.EqualTo("upload"));
            Assert.That(result.Children[2].Children[0].Text, Is.EqualTo("Upload completed"));
        });
    }

    [TestCase(UiValidationSeverity.Information, "information", UiAriaLiveMode.Off, "off")]
    [TestCase(UiValidationSeverity.Warning, "warning", UiAriaLiveMode.Polite, "polite")]
    [TestCase(UiValidationSeverity.Error, "error", UiAriaLiveMode.Assertive, "assertive")]
    public void ProjectsEverySeverityAndLiveRegionMode(UiValidationSeverity severity, string severityToken, UiAriaLiveMode mode, string liveToken)
    {
        UiNode node = FeedbackControl("status", "State changed");
        node.Feedback!.Severity = severity;
        node.Feedback.LiveMode = mode;
        node.Feedback.IsBusy = true;

        DomNode result = new SemanticExpander().ExpandNode(node);

        Assert.Multiple(() =>
        {
            Assert.That(AttributeValue(result, "data-severity"), Is.EqualTo(severityToken));
            Assert.That(AttributeValue(result, AriaAttributes.Live), Is.EqualTo(liveToken));
            Assert.That(AttributeValue(result, AriaAttributes.Busy), Is.EqualTo("true"));
        });
    }

    [Test]
    public void SupportsDefaultFeedbackRangeAndEveryLiveAnnouncementEntryPath()
    {
        UiNode ranged = FeedbackControl("progress", "Half complete");
        ranged.Feedback!.HasValue = true;
        ranged.Feedback.Value = 0.5;
        UiNode noPayload = new UiNode
        {
            Kind = UiNodeKind.Control,
            Name = "live-announcement",
            Identity = new UiNodeIdentity { AuthorKey = "empty-live", StableId = "empty-live-id" }
        };
        UiNode payload = FeedbackControl("live-announcements", "Ignored wrapper message");
        payload.Announcements = new UiLiveAnnouncement[]
        {
            new UiLiveAnnouncement { Id = "first", Message = "First", Mode = UiAriaLiveMode.Polite },
            new UiLiveAnnouncement { Id = "second", Message = "Second", Mode = UiAriaLiveMode.Assertive }
        };
        UiNode singularPayload = FeedbackControl("live-announcement", "Ignored wrapper message");
        singularPayload.Announcements = new UiLiveAnnouncement[]
        {
            new UiLiveAnnouncement { Id = "single", Message = "Single", Mode = UiAriaLiveMode.Off }
        };
        UiNode nullAnnouncements = FeedbackControl("status", "No announcements");
        nullAnnouncements.Announcements = null!;
        SemanticExpander expander = new SemanticExpander();

        DomNode expandedRange = expander.ExpandNode(ranged);
        DomNode expandedNoPayload = expander.ExpandNode(noPayload);
        DomNode expandedPayload = expander.ExpandNode(payload);
        DomNode expandedSingularPayload = expander.ExpandNode(singularPayload);
        DomNode expandedNullAnnouncements = expander.ExpandNode(nullAnnouncements);

        Assert.Multiple(() =>
        {
            Assert.That(AttributeValue(expandedRange, HtmlAttributes.Value), Is.EqualTo("0.5"));
            Assert.That(HasAttribute(expandedRange, HtmlAttributes.Minimum), Is.False);
            Assert.That(HasAttribute(expandedRange, HtmlAttributes.Maximum), Is.False);
            Assert.That(expandedNoPayload.Children, Is.Empty);
            Assert.That(expandedPayload.Children, Has.Length.EqualTo(2));
            Assert.That(expandedPayload.Children[0].Key, Is.EqualTo("first"));
            Assert.That(expandedPayload.Children[1].Key, Is.EqualTo("second"));
            Assert.That(expandedSingularPayload.Children, Has.Length.EqualTo(1));
            Assert.That(expandedSingularPayload.Children[0].Key, Is.EqualTo("single"));
            Assert.That(expandedNullAnnouncements.Children, Has.Length.EqualTo(1));
        });
    }

    [Test]
    public void DedicatedToastOmitsOptionalKeyAndDurationAndAppendsAnnouncements()
    {
        UiNode node = new UiNode
        {
            Kind = UiNodeKind.Control,
            Identity = new UiNodeIdentity { AuthorKey = "toast", StableId = "toast-id" },
            Toast = new UiToast
            {
                Message = "Connected",
                Action = new UiButton { Kind = UiButtonKind.Command, Label = "Retry", CommandId = "connection.retry" },
                State = new UiOverlayState { IsOpen = true }
            },
            Announcements = new UiLiveAnnouncement[]
            {
                new UiLiveAnnouncement { Id = "connection", Message = "Connection restored", Mode = UiAriaLiveMode.Polite }
            }
        };
        node.State.IsEnabled = false;
        node.State.Version = 3;

        SemanticExpander expander = new SemanticExpander();
        DomNode result = expander.ExpandNode(node);
        DomNode repeated = expander.ExpandNode(node);

        Assert.Multiple(() =>
        {
            Assert.That(HasAttribute(result, "data-toast-key"), Is.False);
            Assert.That(HasAttribute(result, "data-duration-milliseconds"), Is.False);
            Assert.That(result.Children, Has.Length.EqualTo(3));
            Assert.That(AttributeValue(result.Children[1], "data-command-id"), Is.EqualTo("connection.retry"));
            Assert.That(result.Children[1].Children[0].Text, Is.EqualTo("Retry"));
            Assert.That(HasAttribute(result.Children[1], HtmlAttributes.Disabled), Is.True);
            Assert.That(result.Children[1].Version, Is.EqualTo(3));
            Assert.That(result.Children[1].Id, Is.EqualTo(repeated.Children[1].Id));
            Assert.That(result.Children[2].Key, Is.EqualTo("connection"));
        });
    }

    [Test]
    public void RejectsInvalidRangesEnumsKindsPayloadsAndAnnouncements()
    {
        UiNode reversedRange = RangedFeedback(10, 5, 7);
        UiNode belowRange = RangedFeedback(0, 10, -1);
        UiNode notFinite = RangedFeedback(0, 10, double.NaN);
        UiNode nonFiniteMinimum = RangedFeedback(double.NegativeInfinity, 10, 0);
        UiNode nonFiniteMaximum = RangedFeedback(0, double.PositiveInfinity, 0);
        UiNode aboveRange = RangedFeedback(0, 10, 11);
        UiNode invalidSeverity = FeedbackControl("status", "Invalid severity");
        invalidSeverity.Feedback!.Severity = (UiValidationSeverity)999;
        UiNode invalidLiveMode = FeedbackControl("status", "Invalid live mode");
        invalidLiveMode.Feedback!.LiveMode = (UiAriaLiveMode)999;
        UiNode unsupported = FeedbackControl("unknown-feedback", "Unknown");
        UiNode invalidToastDuration = new UiNode
        {
            Kind = UiNodeKind.Control,
            Toast = new UiToast { HasDurationMilliseconds = true, DurationMilliseconds = 0 }
        };
        UiNode missing = new UiNode
        {
            Kind = UiNodeKind.Control,
            Name = "progress",
            Identity = new UiNodeIdentity { AuthorKey = "missing", StableId = "missing-id" }
        };
        UiNode malformedAnnouncements = FeedbackControl("status", "Announcements");
        malformedAnnouncements.Announcements = new UiLiveAnnouncement[]
        {
            new UiLiveAnnouncement { Id = "same", Message = "First" },
            new UiLiveAnnouncement { Id = "same", Message = "Second" }
        };
        UiNode unnamedAnnouncement = new UiNode
        {
            Kind = UiNodeKind.Control,
            Name = "live-announcement",
            Identity = new UiNodeIdentity { AuthorKey = "live", StableId = "live-id" },
            Announcements = new UiLiveAnnouncement[] { new UiLiveAnnouncement { Id = " " } }
        };
        UiNode nullAnnouncement = FeedbackControl("status", "Null announcement");
        nullAnnouncement.Announcements = new UiLiveAnnouncement[] { null! };
        UiNode invalidAnnouncementMode = FeedbackControl("status", "Invalid announcement mode");
        invalidAnnouncementMode.Announcements = new UiLiveAnnouncement[]
        {
            new UiLiveAnnouncement { Id = "invalid-mode", Mode = (UiAriaLiveMode)999 }
        };
        UiNode nullToastState = new UiNode
        {
            Kind = UiNodeKind.Control,
            Identity = new UiNodeIdentity { StableId = "toast-id" },
            Toast = new UiToast { State = null! }
        };
        UiNode invalidToastAction = new UiNode
        {
            Kind = UiNodeKind.Control,
            Identity = new UiNodeIdentity { StableId = "toast-action-id" },
            Toast = new UiToast { Action = new UiButton(), State = new UiOverlayState { IsOpen = true } }
        };
        SemanticExpander expander = new SemanticExpander();

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => expander.ExpandNode(reversedRange))!.ParamName, Is.EqualTo("maximum"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => expander.ExpandNode(belowRange))!.ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => expander.ExpandNode(notFinite))!.ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => expander.ExpandNode(nonFiniteMinimum))!.ParamName, Is.EqualTo("minimum"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => expander.ExpandNode(nonFiniteMaximum))!.ParamName, Is.EqualTo("maximum"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => expander.ExpandNode(aboveRange))!.ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => expander.ExpandNode(invalidSeverity))!.ParamName, Is.EqualTo("severity"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => expander.ExpandNode(invalidLiveMode))!.ParamName, Is.EqualTo("liveMode"));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(unsupported));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => expander.ExpandNode(invalidToastDuration))!.ParamName, Is.EqualTo("durationMilliseconds"));
            Assert.That(Assert.Throws<UiRenderException>(() => expander.ExpandNode(missing))!.NodeId, Is.EqualTo("missing-id"));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(malformedAnnouncements));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(unnamedAnnouncement));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(nullAnnouncement));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => expander.ExpandNode(invalidAnnouncementMode))!.ParamName, Is.EqualTo("liveMode"));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(nullToastState));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(invalidToastAction));
        });
    }

    [Test]
    public void RepeatedExpansionIsFreshStableAndDoesNotMutateFeedbackModels()
    {
        UiNode node = FeedbackControl("loading-state", "Original");
        UiFeedback sourceFeedback = node.Feedback!;
        UiLiveAnnouncement sourceAnnouncement = new UiLiveAnnouncement { Id = "loading", Message = "Still loading" };
        node.Announcements = new UiLiveAnnouncement[] { sourceAnnouncement };
        SemanticExpander expander = new SemanticExpander();

        DomNode first = expander.ExpandNode(node);
        DomNode second = expander.ExpandNode(node);
        first.Children[0].Text = "Changed output";
        first.Children[1].Children[0].Text = "Changed announcement";

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(first.Attributes, Is.Not.SameAs(second.Attributes));
            Assert.That(first.Children, Is.Not.SameAs(second.Children));
            Assert.That(first.Children[0].Id, Is.EqualTo(second.Children[0].Id));
            Assert.That(first.Children[1].Id, Is.EqualTo(second.Children[1].Id));
            Assert.That(sourceFeedback.Message, Is.EqualTo("Original"));
            Assert.That(sourceAnnouncement.Message, Is.EqualTo("Still loading"));
            Assert.That(second.Children[0].Text, Is.EqualTo("Original"));
            Assert.That(second.Children[1].Children[0].Text, Is.EqualTo("Still loading"));
        });
    }

    private static UiNode FeedbackControl(string kind, string message)
    {
        return new UiNode
        {
            Kind = UiNodeKind.Control,
            Identity = new UiNodeIdentity { AuthorKey = "feedback", SiblingIdentity = "feedback", StableId = "feedback-id" },
            Feedback = new UiFeedback { Kind = kind, Message = message }
        };
    }

    private static UiNode RangedFeedback(double minimum, double maximum, double value)
    {
        UiNode node = FeedbackControl("meter", "Range");
        node.Feedback!.HasMinimum = true;
        node.Feedback.Minimum = minimum;
        node.Feedback.HasMaximum = true;
        node.Feedback.Maximum = maximum;
        node.Feedback.HasValue = true;
        node.Feedback.Value = value;
        return node;
    }

    private static UiNode LowLevelText(string key, string id, string text)
    {
        return new UiNode
        {
            Kind = UiNodeKind.Text,
            Identity = new UiNodeIdentity { AuthorKey = key, SiblingIdentity = key, StableId = id },
            Text = text
        };
    }

    private static string AttributeValue(DomNode node, string name)
    {
        int index = 0;
        while (index < node.Attributes.Length)
        {
            if (node.Attributes[index].Name == name)
            {
                return node.Attributes[index].Value;
            }

            index++;
        }

        throw new AssertionException("Attribute was not found: " + name);
    }

    private static string OptionalAttributeValue(DomNode node, string name)
    {
        int index = 0;
        while (index < node.Attributes.Length)
        {
            if (node.Attributes[index].Name == name)
            {
                return node.Attributes[index].Value;
            }

            index++;
        }

        return string.Empty;
    }

    private static bool HasAttribute(DomNode node, string name)
    {
        int index = 0;
        while (index < node.Attributes.Length)
        {
            if (node.Attributes[index].Name == name)
            {
                return true;
            }

            index++;
        }

        return false;
    }
}
