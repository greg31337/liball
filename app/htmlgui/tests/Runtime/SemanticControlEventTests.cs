using System;
using htmlgui;

namespace htmlgui.Tests;

public class SemanticControlEventTests
{
    [TestCase("Enter")]
    [TestCase("Spacebar")]
    public void SynthesizesClicksFromButtonKeyboardActivation(string key)
    {
        UiNode button = Button(UiButtonKind.Push);
        UiEventData source = KeyEvent(key, false);

        UiEventData[] events = new EventCore().SynthesizeControlEvents(button, source);

        Assert.Multiple(() =>
        {
            Assert.That(events, Has.Length.EqualTo(1));
            Assert.That(events[0].Kind, Is.EqualTo(UiEventKind.Click));
            Assert.That(events[0].TargetId, Is.EqualTo("control"));
            Assert.That(events[0].Path, Is.EqualTo(new string[] { "control", "root" }));
            Assert.That(events[0].IsTrusted, Is.True);
            Assert.That(events[0].Keyboard, Is.Null);
            Assert.That(source.Kind, Is.EqualTo(UiEventKind.KeyDown));
        });
    }

    [Test]
    public void IgnoresNonActivationRepeatsAndCompositionKeys()
    {
        UiNode button = Button(UiButtonKind.Push);
        EventCore core = new EventCore();

        UiEventData[] letter = core.SynthesizeControlEvents(button, KeyEvent("a", false));
        UiEventData[] composing = core.SynthesizeControlEvents(button, KeyEvent("Enter", true));
        UiEventData[] existingClick = core.SynthesizeControlEvents(button, ClickEvent());

        Assert.Multiple(() =>
        {
            Assert.That(letter, Is.Empty);
            Assert.That(composing, Is.Empty);
            Assert.That(existingClick, Is.Empty);
        });
    }

    [TestCase(UiButtonKind.Submit, UiEventKind.Submit)]
    [TestCase(UiButtonKind.Reset, UiEventKind.Reset)]
    public void SynthesizesFormEventsFromSemanticButtons(UiButtonKind buttonKind, UiEventKind eventKind)
    {
        UiEventData[] events = new EventCore().SynthesizeControlEvents(Button(buttonKind), ClickEvent());

        Assert.Multiple(() =>
        {
            Assert.That(events, Has.Length.EqualTo(1));
            Assert.That(events[0].Kind, Is.EqualTo(eventKind));
            Assert.That(events[0].Input, Is.Null);
        });
    }

    [TestCase(false, true)]
    [TestCase(true, false)]
    public void SynthesizesTheNextToggleButtonState(bool current, bool expected)
    {
        UiNode button = Button(UiButtonKind.Toggle);
        button.Button!.HasPressed = true;
        button.Button.IsPressed = current;

        UiEventData[] events = new EventCore().SynthesizeControlEvents(button, ClickEvent());

        Assert.Multiple(() =>
        {
            Assert.That(events[0].Kind, Is.EqualTo(UiEventKind.Toggle));
            Assert.That(events[0].Input!.Value.Kind, Is.EqualTo(UiValueKind.Boolean));
            Assert.That(events[0].Input!.Value.BooleanValue, Is.EqualTo(expected));
            Assert.That(button.Button!.IsPressed, Is.EqualTo(current));
        });
    }

    [TestCase(false, true)]
    [TestCase(true, false)]
    public void SynthesizesTheNextDisclosureState(bool current, bool expected)
    {
        UiNode disclosure = new UiNode
        {
            Identity = new UiNodeIdentity { StableId = "control" },
            Disclosure = new UiDisclosure { IsExpanded = current }
        };

        UiEventData[] events = new EventCore().SynthesizeControlEvents(disclosure, KeyEvent("Enter", false));

        Assert.Multiple(() =>
        {
            Assert.That(events[0].Kind, Is.EqualTo(UiEventKind.Toggle));
            Assert.That(events[0].Input!.Value.BooleanValue, Is.EqualTo(expected));
            Assert.That(disclosure.Disclosure!.IsExpanded, Is.EqualTo(current));
        });
    }

    [Test]
    public void UsesNodePressedStateWhenTheButtonHasNoExplicitPressedState()
    {
        UiNode button = Button(UiButtonKind.Toggle);
        button.State.HasPressed = true;
        button.State.IsPressed = true;

        UiEventData[] events = new EventCore().SynthesizeControlEvents(button, ClickEvent());

        Assert.That(events[0].Input!.Value.BooleanValue, Is.False);
    }

    [Test]
    public void DoesNotSynthesizeEventsForUnavailableControls()
    {
        UiNode disabled = Button(UiButtonKind.Push);
        disabled.State.IsEnabled = false;
        UiNode hidden = Button(UiButtonKind.Push);
        hidden.State.IsVisible = false;
        UiNode busy = Button(UiButtonKind.Push);
        busy.State.IsBusy = true;
        UiNode buttonDisabled = Button(UiButtonKind.Push);
        buttonDisabled.Button!.IsEnabled = false;
        UiNode disclosureDisabled = new UiNode { Disclosure = new UiDisclosure { IsDisabled = true } };
        EventCore core = new EventCore();

        Assert.Multiple(() =>
        {
            Assert.That(core.SynthesizeControlEvents(disabled, KeyEvent("Enter", false)), Is.Empty);
            Assert.That(core.SynthesizeControlEvents(hidden, KeyEvent("Enter", false)), Is.Empty);
            Assert.That(core.SynthesizeControlEvents(busy, KeyEvent("Enter", false)), Is.Empty);
            Assert.That(core.SynthesizeControlEvents(buttonDisabled, KeyEvent("Enter", false)), Is.Empty);
            Assert.That(core.SynthesizeControlEvents(disclosureDisabled, KeyEvent("Enter", false)), Is.Empty);
        });
    }

    [Test]
    public void RejectsNullControlsAndUnsupportedButtonKinds()
    {
        UiNode unsupported = Button(UiButtonKind.Push);
        unsupported.Button!.Kind = (UiButtonKind)999;
        EventCore core = new EventCore();

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() => core.SynthesizeControlEvents(null, ClickEvent()));
            Assert.Throws<ArgumentOutOfRangeException>(() => core.SynthesizeControlEvents(unsupported, ClickEvent()));
        });
    }

    private static UiNode Button(UiButtonKind kind)
    {
        return new UiNode
        {
            Identity = new UiNodeIdentity { StableId = "control" },
            Button = new UiButton { Kind = kind }
        };
    }

    private static UiEventData ClickEvent()
    {
        return new UiEventData
        {
            Kind = UiEventKind.Click,
            TargetId = "control",
            Path = new string[] { "control", "root" },
            IsTrusted = true
        };
    }

    private static UiEventData KeyEvent(string key, bool composing)
    {
        return new UiEventData
        {
            Kind = UiEventKind.KeyDown,
            TargetId = "control",
            Path = new string[] { "control", "root" },
            IsTrusted = true,
            Keyboard = new UiKeyboardData { Key = key, IsComposing = composing }
        };
    }
}
