using System;
using htmlgui;

namespace htmlgui.Tests;

public class ControlTests
{
    [Test]
    public void ControlModelsHaveSafeDefaults()
    {
        UiText text = new UiText();
        UiButton button = new UiButton();
        UiInput input = new UiInput();
        UiChoice choice = new UiChoice();
        UiMedia media = new UiMedia();
        UiFeedback feedback = new UiFeedback();
        UiDisclosure disclosure = new UiDisclosure();

        Assert.Multiple(() =>
        {
            Assert.That(text.Kind, Is.EqualTo(UiTextKind.Plain));
            Assert.That(text.Text, Is.Empty);
            Assert.That(text.HasHeadingLevel, Is.False);
            Assert.That(text.Url, Is.Null);
            Assert.That(button.Kind, Is.EqualTo(UiButtonKind.Push));
            Assert.That(button.Label, Is.Empty);
            Assert.That(button.IsEnabled, Is.True);
            Assert.That(button.IsVisible, Is.True);
            Assert.That(button.Menu, Is.Null);
            Assert.That(input.Kind, Is.EqualTo(UiInputKind.Text));
            Assert.That(input.Value.Kind, Is.EqualTo(UiValueKind.Null));
            Assert.That(input.Choices, Is.Empty);
            Assert.That(input.ErrorText, Is.Null);
            Assert.That(input.HasClearAction, Is.False);
            Assert.That(input.HasRevealAction, Is.False);
            Assert.That(input.IsRevealed, Is.False);
            Assert.That(input.EnterKeyHint, Is.Null);
            Assert.That(input.HasSpellCheck, Is.False);
            Assert.That(input.AcceptedFileTypes, Is.Empty);
            Assert.That(input.UpdateTrigger, Is.EqualTo(UiBindingTrigger.Change));
            Assert.That(choice.Value.Kind, Is.EqualTo(UiValueKind.Null));
            Assert.That(media.FallbackContent, Is.Empty);
            Assert.That(feedback.Severity, Is.EqualTo(UiValidationSeverity.Information));
            Assert.That(feedback.LiveMode, Is.EqualTo(UiAriaLiveMode.Off));
            Assert.That(disclosure.Content, Is.Empty);
            Assert.That(disclosure.IsExpanded, Is.False);
        });
    }

    [Test]
    public void PopulatedControlsRetainSemanticOptions()
    {
        UiChoice choice = new UiChoice
        {
            Key = "red",
            Label = "Red",
            Value = TextValue("#f00"),
            Url = "/colors/red",
            GroupKey = "warm",
            IsSelected = true,
            IsDisabled = true
        };
        UiInput input = new UiInput
        {
            Kind = UiInputKind.Number,
            Name = "quantity",
            Value = IntegerValue(2),
            Placeholder = "Quantity",
            HelperText = "Choose one",
            ErrorText = "Not enough",
            Prefix = "$",
            Suffix = "items",
            HasClearAction = true,
            HasRevealAction = true,
            IsRevealed = true,
            InputMode = "numeric",
            AutoComplete = "off",
            EnterKeyHint = "done",
            HasSpellCheck = true,
            SpellCheck = true,
            Minimum = IntegerValue(0),
            Maximum = IntegerValue(10),
            Step = IntegerValue(1),
            Choices = new UiChoice[] { choice },
            HasMinimumLength = true,
            MinimumLength = 0,
            HasMaximumLength = true,
            MaximumLength = 3,
            IsRequired = true,
            IsDisabled = true,
            Multiple = true,
            AcceptedFileTypes = new string[] { ".txt" },
            UpdateTrigger = UiBindingTrigger.Input
        };
        UiMenu menu = new UiMenu { Key = "actions" };
        UiButton button = new UiButton
        {
            Kind = UiButtonKind.Split,
            Label = "Save",
            CommandId = "save",
            IconKey = "disk",
            IsEnabled = false,
            IsVisible = false,
            IsBusy = true,
            HasPressed = true,
            IsPressed = false,
            Menu = menu
        };
        UiText text = new UiText { Kind = UiTextKind.Heading, Text = "Title", HasHeadingLevel = true, HeadingLevel = 2, Url = "/", AlternativeText = "Home", LocalizationKey = "title" };
        UiNode fallback = new UiNode { Kind = UiNodeKind.Text, Text = "Unsupported" };
        UiMedia media = new UiMedia { Kind = "video", Source = "/movie.mp4", MimeType = "video/mp4", AlternativeText = "Movie", FallbackContent = new UiNode[] { fallback }, ShowControls = true, AutoPlay = true, Muted = true, Loop = true };
        UiFeedback feedback = new UiFeedback { Kind = "progress", Message = "Loading", Severity = UiValidationSeverity.Warning, LiveMode = UiAriaLiveMode.Polite, HasValue = true, Value = 0, HasMinimum = true, Minimum = 0, HasMaximum = true, Maximum = 100, IsBusy = true };
        UiDisclosure disclosure = new UiDisclosure { Key = "details", Summary = "Details", Content = new UiNode[] { fallback }, IsExpanded = true, IsDisabled = true };

        Assert.Multiple(() =>
        {
            Assert.That(input.Choices[0], Is.SameAs(choice));
            Assert.That(input.Minimum?.IntegerValue, Is.Zero);
            Assert.That(input.HasMinimumLength, Is.True);
            Assert.That(input.MinimumLength, Is.Zero);
            Assert.That(input.ErrorText, Is.EqualTo("Not enough"));
            Assert.That(input.HasClearAction, Is.True);
            Assert.That(input.HasRevealAction, Is.True);
            Assert.That(input.IsRevealed, Is.True);
            Assert.That(input.EnterKeyHint, Is.EqualTo("done"));
            Assert.That(input.SpellCheck, Is.True);
            Assert.That(input.AcceptedFileTypes, Is.EqualTo(new string[] { ".txt" }));
            Assert.That(input.UpdateTrigger, Is.EqualTo(UiBindingTrigger.Input));
            Assert.That(choice.Url, Is.EqualTo("/colors/red"));
            Assert.That(button.Menu, Is.SameAs(menu));
            Assert.That(button.HasPressed, Is.True);
            Assert.That(button.IsPressed, Is.False);
            Assert.That(text.HeadingLevel, Is.EqualTo(2));
            Assert.That(media.FallbackContent[0], Is.SameAs(fallback));
            Assert.That(media.Muted, Is.True);
            Assert.That(feedback.HasValue, Is.True);
            Assert.That(feedback.Value, Is.Zero);
            Assert.That(feedback.Maximum, Is.EqualTo(100));
            Assert.That(disclosure.Content[0], Is.SameAs(fallback));
        });
    }

    private static UiValue TextValue(string value)
    {
        return new UiValue { Kind = UiValueKind.String, StringValue = value };
    }

    private static UiValue IntegerValue(long value)
    {
        return new UiValue { Kind = UiValueKind.Integer, IntegerValue = value };
    }
}
