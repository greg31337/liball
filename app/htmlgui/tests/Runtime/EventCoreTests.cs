using System;
using System.Collections.Generic;
using htmlgui;

namespace htmlgui.Tests;

public class EventCoreTests
{
    [Test]
    public void ConvertsEverySupportedEventKindToAndFromItsCanonicalDomName()
    {
        string[] expectedNames = new string[]
        {
            "click", "dblclick", "contextmenu",
            "pointerdown", "pointerup", "pointermove", "pointerenter", "pointerleave", "pointercancel",
            "mousedown", "mouseup", "mousemove", "mouseenter", "mouseleave", "wheel",
            "keydown", "keyup", "focus", "blur", "focusin", "focusout",
            "beforeinput", "input", "change", "compositionstart", "compositionupdate", "compositionend", "selectionchange",
            "submit", "reset", "dragstart", "drag", "dragenter", "dragover", "dragleave", "drop", "dragend",
            "copy", "cut", "paste", "scroll", "resize", "visibilitychange", "toggle"
        };
        UiEventKind[] kinds = Enum.GetValues<UiEventKind>();
        EventCore core = new EventCore();
        List<string> actualNames = new List<string>();

        foreach (UiEventKind kind in kinds)
        {
            string name = core.ConvertEventKind(kind);
            actualNames.Add(name);
            Assert.That(core.ConvertEventName(name), Is.EqualTo(kind));
        }

        Assert.Multiple(() =>
        {
            Assert.That(kinds, Has.Length.EqualTo(44));
            Assert.That(actualNames, Is.EqualTo(expectedNames));
        });
    }

    [Test]
    public void NormalizesCommonAndPointerDataWithoutMutatingTheInput()
    {
        string[] path = new string[] { "button", "panel", "root" };
        UiPointerData pointer = new UiPointerData
        {
            PointerType = "PEN",
            PointerId = 7,
            ClientX = 12.5,
            ClientY = -3.25,
            Button = 2,
            Buttons = 5,
            PrimaryModifier = true,
            Shift = true
        };
        UiEventData source = Event(UiEventKind.PointerDown);
        source.Path = path;
        source.IsTrusted = true;
        source.Pointer = pointer;
        source.Keyboard = new UiKeyboardData { Key = "ignored" };
        source.Input = TextInput("ignored");
        source.Clipboard = ClipboardPayload();
        source.Drag = DragPayload();

        UiEventData result = new EventCore().NormalizeEventData(source);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.SameAs(source));
            Assert.That(result.Kind, Is.EqualTo(UiEventKind.PointerDown));
            Assert.That(result.TargetId, Is.EqualTo("button"));
            Assert.That(result.Path, Is.EqualTo(path));
            Assert.That(result.Path, Is.Not.SameAs(path));
            Assert.That(result.IsTrusted, Is.True);
            Assert.That(result.Pointer, Is.Not.Null.And.Not.SameAs(pointer));
            Assert.That(result.Pointer!.PointerType, Is.EqualTo("pen"));
            Assert.That(result.Pointer.PointerId, Is.EqualTo(7));
            Assert.That(result.Pointer.ClientX, Is.EqualTo(12.5));
            Assert.That(result.Pointer.ClientY, Is.EqualTo(-3.25));
            Assert.That(result.Pointer.Button, Is.EqualTo(2));
            Assert.That(result.Pointer.Buttons, Is.EqualTo(5));
            Assert.That(result.Pointer.PrimaryModifier, Is.True);
            Assert.That(result.Pointer.Shift, Is.True);
            Assert.That(result.Keyboard, Is.Null);
            Assert.That(result.Input, Is.Null);
            Assert.That(result.Clipboard, Is.Null);
            Assert.That(result.Drag, Is.Null);
            Assert.That(pointer.PointerType, Is.EqualTo("PEN"));
        });
    }

    [TestCase(UiEventKind.MouseDown)]
    [TestCase(UiEventKind.MouseUp)]
    [TestCase(UiEventKind.MouseMove)]
    [TestCase(UiEventKind.MouseEnter)]
    [TestCase(UiEventKind.MouseLeave)]
    [TestCase(UiEventKind.Wheel)]
    public void SuppliesMouseAsThePointerTypeForMouseAndWheelEvents(UiEventKind kind)
    {
        UiEventData source = Event(kind);
        source.Pointer = new UiPointerData();

        UiEventData result = new EventCore().NormalizeEventData(source);

        Assert.That(result.Pointer!.PointerType, Is.EqualTo("mouse"));
    }

    [TestCase(double.NaN, 0.0)]
    [TestCase(double.PositiveInfinity, 0.0)]
    [TestCase(0.0, double.NegativeInfinity)]
    public void RejectsNonFinitePointerCoordinates(double clientX, double clientY)
    {
        UiEventData source = Event(UiEventKind.PointerMove);
        source.Pointer = new UiPointerData { ClientX = clientX, ClientY = clientY };

        Assert.Throws<ArgumentOutOfRangeException>(() => new EventCore().NormalizeEventData(source));
    }

    [Test]
    public void NormalizesKeyboardAliasesAndCopiesKeyboardFlags()
    {
        UiKeyboardData keyboard = new UiKeyboardData
        {
            Key = "Esc",
            Code = "Escape",
            PrimaryModifier = true,
            Alt = true,
            Shift = true,
            Repeat = true,
            IsComposing = true
        };
        UiEventData source = Event(UiEventKind.KeyDown);
        source.Keyboard = keyboard;

        UiEventData result = new EventCore().NormalizeEventData(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Keyboard, Is.Not.Null.And.Not.SameAs(keyboard));
            Assert.That(result.Keyboard!.Key, Is.EqualTo(AriaKeyNames.Escape));
            Assert.That(result.Keyboard.Code, Is.EqualTo("Escape"));
            Assert.That(result.Keyboard.PrimaryModifier, Is.True);
            Assert.That(result.Keyboard.Alt, Is.True);
            Assert.That(result.Keyboard.Shift, Is.True);
            Assert.That(result.Keyboard.Repeat, Is.True);
            Assert.That(result.Keyboard.IsComposing, Is.True);
            Assert.That(keyboard.Key, Is.EqualTo("Esc"));
        });
    }

    [TestCase(UiEventKind.CompositionStart, false, true)]
    [TestCase(UiEventKind.CompositionUpdate, false, true)]
    [TestCase(UiEventKind.CompositionEnd, true, false)]
    [TestCase(UiEventKind.Input, true, true)]
    public void NormalizesCompositionStateAndSelectionData(UiEventKind kind, bool suppliedState, bool expectedState)
    {
        UiInputData input = TextInput("hello");
        input.InputType = "insertText";
        input.Data = "o";
        input.IsComposing = suppliedState;
        input.HasSelectionStart = true;
        input.SelectionStart = 2;
        input.HasSelectionEnd = true;
        input.SelectionEnd = 4;
        UiEventData source = Event(kind);
        source.Input = input;

        UiEventData result = new EventCore().NormalizeEventData(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Input, Is.Not.Null.And.Not.SameAs(input));
            Assert.That(result.Input!.InputType, Is.EqualTo("insertText"));
            Assert.That(result.Input.Data, Is.EqualTo("o"));
            Assert.That(result.Input.IsComposing, Is.EqualTo(expectedState));
            Assert.That(result.Input.HasSelectionStart, Is.True);
            Assert.That(result.Input.SelectionStart, Is.EqualTo(2));
            Assert.That(result.Input.HasSelectionEnd, Is.True);
            Assert.That(result.Input.SelectionEnd, Is.EqualTo(4));
            Assert.That(result.Input.Value, Is.Not.SameAs(input.Value));
            Assert.That(result.Input.Value.StringValue, Is.EqualTo("hello"));
        });
    }

    [Test]
    public void ClearsSelectionNumbersThatWereNotMarkedAsPresent()
    {
        UiEventData source = Event(UiEventKind.SelectionChange);
        source.Input = new UiInputData
        {
            Value = StringValue("selected"),
            SelectionStart = 12,
            SelectionEnd = 14
        };

        UiInputData result = new EventCore().NormalizeEventData(source).Input!;

        Assert.Multiple(() =>
        {
            Assert.That(result.HasSelectionStart, Is.False);
            Assert.That(result.SelectionStart, Is.Zero);
            Assert.That(result.HasSelectionEnd, Is.False);
            Assert.That(result.SelectionEnd, Is.Zero);
        });
    }

    [TestCase(true, -1, false, 0)]
    [TestCase(false, 0, true, -1)]
    [TestCase(true, 4, true, 3)]
    public void RejectsInvalidSelectionBounds(bool hasStart, int start, bool hasEnd, int end)
    {
        UiEventData source = Event(UiEventKind.SelectionChange);
        source.Input = new UiInputData
        {
            HasSelectionStart = hasStart,
            SelectionStart = start,
            HasSelectionEnd = hasEnd,
            SelectionEnd = end
        };

        Assert.That(() => new EventCore().NormalizeEventData(source), Throws.Exception);
    }

    [TestCase(UiEventKind.Submit)]
    [TestCase(UiEventKind.Reset)]
    [TestCase(UiEventKind.Scroll)]
    [TestCase(UiEventKind.Resize)]
    [TestCase(UiEventKind.VisibilityChange)]
    [TestCase(UiEventKind.Toggle)]
    public void ConvertsFormAndObserverStyleValuesWhenPresent(UiEventKind kind)
    {
        UiEventData source = Event(kind);
        source.Input = TextInput("state");

        UiEventData result = new EventCore().NormalizeEventData(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Input, Is.Not.Null);
            Assert.That(result.Input!.Value.StringValue, Is.EqualTo("state"));
            Assert.That(result.Input.Value, Is.Not.SameAs(source.Input.Value));
        });
    }

    [TestCase(UiEventKind.Focus)]
    [TestCase(UiEventKind.Blur)]
    [TestCase(UiEventKind.FocusIn)]
    [TestCase(UiEventKind.FocusOut)]
    public void ConvertsFocusEventsUsingOnlyCommonData(UiEventKind kind)
    {
        UiEventData source = Event(kind);
        source.Pointer = new UiPointerData();
        source.Keyboard = new UiKeyboardData();
        source.Input = TextInput("ignored");
        source.Clipboard = ClipboardPayload();
        source.Drag = DragPayload();

        UiEventData result = new EventCore().NormalizeEventData(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Kind, Is.EqualTo(kind));
            Assert.That(result.TargetId, Is.EqualTo("button"));
            Assert.That(result.Pointer, Is.Null);
            Assert.That(result.Keyboard, Is.Null);
            Assert.That(result.Input, Is.Null);
            Assert.That(result.Clipboard, Is.Null);
            Assert.That(result.Drag, Is.Null);
        });
    }

    [TestCase(UiEventKind.Copy, "copy")]
    [TestCase(UiEventKind.Cut, "cut")]
    [TestCase(UiEventKind.Paste, "paste")]
    public void DerivesClipboardOperationAndDeepCopiesClipboardData(UiEventKind kind, string operation)
    {
        UiClipboardData clipboard = ClipboardPayload();
        clipboard.Operation = "untrusted-operation";
        UiEventData source = Event(kind);
        source.Clipboard = clipboard;

        UiClipboardData result = new EventCore().NormalizeEventData(source).Clipboard!;
        clipboard.Data[0].Value.StringValue = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.SameAs(clipboard));
            Assert.That(result.Operation, Is.EqualTo(operation));
            Assert.That(result.IsAllowed, Is.True);
            Assert.That(result.Data, Is.Not.SameAs(clipboard.Data));
            Assert.That(result.Data[0], Is.Not.SameAs(clipboard.Data[0]));
            Assert.That(result.Data[0].Value, Is.Not.SameAs(clipboard.Data[0].Value));
            Assert.That(result.Data[0].Key, Is.EqualTo("text/plain"));
            Assert.That(result.Data[0].Value.StringValue, Is.EqualTo("copied"));
        });
    }

    [TestCase(UiEventKind.DragStart)]
    [TestCase(UiEventKind.Drag)]
    [TestCase(UiEventKind.DragEnter)]
    [TestCase(UiEventKind.DragOver)]
    [TestCase(UiEventKind.DragLeave)]
    [TestCase(UiEventKind.Drop)]
    [TestCase(UiEventKind.DragEnd)]
    public void ConvertsEveryDragEventAndDeepCopiesItsPayload(UiEventKind kind)
    {
        UiDragData drag = DragPayload();
        UiEventData source = Event(kind);
        source.Drag = drag;

        UiDragData result = new EventCore().NormalizeEventData(source).Drag!;
        drag.Data[0].Value.StringValue = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.SameAs(drag));
            Assert.That(result.SourceId, Is.EqualTo("source"));
            Assert.That(result.TargetId, Is.EqualTo("target"));
            Assert.That(result.Effect, Is.EqualTo("copy"));
            Assert.That(result.AllowedEffect, Is.EqualTo("copyMove"));
            Assert.That(result.IsKeyboard, Is.True);
            Assert.That(result.Data[0].Value.StringValue, Is.EqualTo("dragged"));
        });
    }

    [Test]
    public void AllowsAbsentOptionalPayloadsAndNormalizesNullCollections()
    {
        UiEventData input = Event(UiEventKind.Input);
        input.Path = null!;
        UiEventData clipboard = Event(UiEventKind.Copy);
        clipboard.Clipboard = new UiClipboardData { Data = null! };
        UiEventData drag = Event(UiEventKind.Drag);
        drag.Drag = new UiDragData { Data = null! };
        EventCore core = new EventCore();

        UiEventData inputResult = core.NormalizeEventData(input);
        UiEventData clipboardResult = core.NormalizeEventData(clipboard);
        UiEventData dragResult = core.NormalizeEventData(drag);

        Assert.Multiple(() =>
        {
            Assert.That(inputResult.Path, Is.Empty);
            Assert.That(inputResult.Input, Is.Null);
            Assert.That(clipboardResult.Clipboard!.Data, Is.Empty);
            Assert.That(dragResult.Drag!.Data, Is.Empty);
        });
    }

    [Test]
    public void RejectsNullUnsupportedAndMalformedInputs()
    {
        EventCore core = new EventCore();
        UiEventData unsupported = Event(UiEventKind.Click);
        unsupported.Kind = (UiEventKind)999;
        UiEventData malformedValue = Event(UiEventKind.Input);
        malformedValue.Input = new UiInputData { Value = new UiValue { Kind = UiValueKind.String } };
        UiEventData nullEntry = Event(UiEventKind.Copy);
        nullEntry.Clipboard = new UiClipboardData { Data = new UiValueEntry[] { null! } };

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() => core.NormalizeEventData(null));
            Assert.Throws<ArgumentNullException>(() => core.ConvertEventName(null));
            Assert.Throws<ArgumentException>(() => core.ConvertEventName("CLICK"));
            Assert.Throws<ArgumentException>(() => core.ConvertEventName("unknown"));
            Assert.Throws<ArgumentOutOfRangeException>(() => core.ConvertEventKind((UiEventKind)(-1)));
            Assert.Throws<ArgumentOutOfRangeException>(() => core.NormalizeEventData(unsupported));
            Assert.Throws<ArgumentException>(() => core.NormalizeEventData(malformedValue));
            Assert.Throws<ArgumentException>(() => core.NormalizeEventData(nullEntry));
        });
    }

    private static UiEventData Event(UiEventKind kind)
    {
        return new UiEventData
        {
            Kind = kind,
            TargetId = "button",
            Path = new string[] { "button", "root" }
        };
    }

    private static UiInputData TextInput(string value)
    {
        return new UiInputData { Value = StringValue(value) };
    }

    private static UiValue StringValue(string value)
    {
        return new UiValue { Kind = UiValueKind.String, StringValue = value };
    }

    private static UiClipboardData ClipboardPayload()
    {
        return new UiClipboardData
        {
            IsAllowed = true,
            Data = new UiValueEntry[]
            {
                new UiValueEntry { Key = "text/plain", Value = StringValue("copied") }
            }
        };
    }

    private static UiDragData DragPayload()
    {
        return new UiDragData
        {
            SourceId = "source",
            TargetId = "target",
            Effect = "copy",
            AllowedEffect = "copyMove",
            IsKeyboard = true,
            Data = new UiValueEntry[]
            {
                new UiValueEntry { Key = "text/plain", Value = StringValue("dragged") }
            }
        };
    }
}
