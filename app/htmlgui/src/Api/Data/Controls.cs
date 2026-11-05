using System;

namespace htmlgui;

/// <summary>Describes semantic text or content.</summary>
public class UiText
{
    /// <summary>The semantic presentation.</summary>
    public UiTextKind Kind;
    /// <summary>The literal or localization-key text.</summary>
    public string Text = string.Empty;
    /// <summary>Whether a heading level is supplied.</summary>
    public bool HasHeadingLevel;
    /// <summary>The heading level.</summary>
    public int HeadingLevel;
    /// <summary>The optional link or image URL.</summary>
    public string? Url;
    /// <summary>The optional accessible alternative text.</summary>
    public string? AlternativeText;
    /// <summary>The optional localization catalog key.</summary>
    public string? LocalizationKey;
}

/// <summary>Describes a push, form, toggle, split, icon, or command-backed button.</summary>
public class UiButton
{
    /// <summary>The button behavior.</summary>
    public UiButtonKind Kind;
    /// <summary>The visible label.</summary>
    public string Label = string.Empty;
    /// <summary>The optional stable command identifier.</summary>
    public string? CommandId;
    /// <summary>The optional icon key.</summary>
    public string? IconKey;
    /// <summary>Whether the button is enabled.</summary>
    public bool IsEnabled = true;
    /// <summary>Whether the button is visible.</summary>
    public bool IsVisible = true;
    /// <summary>Whether the button is busy.</summary>
    public bool IsBusy;
    /// <summary>Whether a pressed state is supplied.</summary>
    public bool HasPressed;
    /// <summary>The pressed state.</summary>
    public bool IsPressed;
    /// <summary>The optional menu opened by a split or menu button.</summary>
    public UiMenu? Menu;
}

/// <summary>Describes a text-like, numeric, temporal, file, color, hidden, or read-only input.</summary>
public class UiInput
{
    /// <summary>The input kind.</summary>
    public UiInputKind Kind;
    /// <summary>The form field name.</summary>
    public string Name = string.Empty;
    /// <summary>The current portable value.</summary>
    public UiValue Value = new UiValue();
    /// <summary>The optional placeholder.</summary>
    public string? Placeholder;
    /// <summary>The optional helper text.</summary>
    public string? HelperText;
    /// <summary>The optional current error text.</summary>
    public string? ErrorText;
    /// <summary>The optional prefix text.</summary>
    public string? Prefix;
    /// <summary>The optional suffix text.</summary>
    public string? Suffix;
    /// <summary>Whether a clear-value action is shown.</summary>
    public bool HasClearAction;
    /// <summary>Whether a password reveal action is shown.</summary>
    public bool HasRevealAction;
    /// <summary>Whether the password value is currently revealed.</summary>
    public bool IsRevealed;
    /// <summary>The optional input-mode token.</summary>
    public string? InputMode;
    /// <summary>The optional autocomplete token.</summary>
    public string? AutoComplete;
    /// <summary>The optional enter-key presentation hint.</summary>
    public string? EnterKeyHint;
    /// <summary>Whether an explicit spellcheck setting is supplied.</summary>
    public bool HasSpellCheck;
    /// <summary>Whether browser spellchecking is enabled.</summary>
    public bool SpellCheck;
    /// <summary>The optional minimum value.</summary>
    public UiValue? Minimum;
    /// <summary>The optional maximum value.</summary>
    public UiValue? Maximum;
    /// <summary>The optional step value.</summary>
    public UiValue? Step;
    /// <summary>The optional regular-expression pattern constraint.</summary>
    public string? Pattern;
    /// <summary>The choices used by select, radio, listbox, combo, or segmented inputs.</summary>
    public UiChoice[] Choices = Array.Empty<UiChoice>();
    /// <summary>Whether a minimum text length is supplied.</summary>
    public bool HasMinimumLength;
    /// <summary>The minimum text length.</summary>
    public int MinimumLength;
    /// <summary>Whether a maximum text length is supplied.</summary>
    public bool HasMaximumLength;
    /// <summary>The maximum text length.</summary>
    public int MaximumLength;
    /// <summary>Whether a value is required.</summary>
    public bool IsRequired;
    /// <summary>Whether the input is disabled.</summary>
    public bool IsDisabled;
    /// <summary>Whether multiple values or files are accepted.</summary>
    public bool Multiple;
    /// <summary>The accepted MIME types or file-name extensions for file inputs.</summary>
    public string[] AcceptedFileTypes = Array.Empty<string>();
    /// <summary>The binding update trigger.</summary>
    public UiBindingTrigger UpdateTrigger = UiBindingTrigger.Change;
}

/// <summary>Describes one selectable choice.</summary>
public class UiChoice
{
    /// <summary>The stable choice key.</summary>
    public string Key = string.Empty;
    /// <summary>The visible label.</summary>
    public string Label = string.Empty;
    /// <summary>The portable choice value.</summary>
    public UiValue Value = new UiValue();
    /// <summary>The optional navigation URL.</summary>
    public string? Url;
    /// <summary>The optional group key.</summary>
    public string? GroupKey;
    /// <summary>Whether the choice is selected.</summary>
    public bool IsSelected;
    /// <summary>Whether the choice is disabled.</summary>
    public bool IsDisabled;
}

/// <summary>Describes safe opt-in audio, video, image, or embedded media.</summary>
public class UiMedia
{
    /// <summary>The media kind token.</summary>
    public string Kind = string.Empty;
    /// <summary>The source URL.</summary>
    public string Source = string.Empty;
    /// <summary>The optional MIME type.</summary>
    public string? MimeType;
    /// <summary>The optional accessible alternative text.</summary>
    public string? AlternativeText;
    /// <summary>Fallback content.</summary>
    public UiNode[] FallbackContent = Array.Empty<UiNode>();
    /// <summary>Whether native controls are shown.</summary>
    public bool ShowControls;
    /// <summary>Whether playback starts automatically.</summary>
    public bool AutoPlay;
    /// <summary>Whether playback is muted.</summary>
    public bool Muted;
    /// <summary>Whether playback loops.</summary>
    public bool Loop;
}

/// <summary>Describes progress, status, alert, placeholder, or other feedback content.</summary>
public class UiFeedback
{
    /// <summary>The feedback kind token.</summary>
    public string Kind = string.Empty;
    /// <summary>The displayed message.</summary>
    public string Message = string.Empty;
    /// <summary>The validation or status severity.</summary>
    public UiValidationSeverity Severity;
    /// <summary>The live-region behavior.</summary>
    public UiAriaLiveMode LiveMode;
    /// <summary>Whether a numeric value is supplied.</summary>
    public bool HasValue;
    /// <summary>The current numeric value.</summary>
    public double Value;
    /// <summary>Whether a minimum is supplied.</summary>
    public bool HasMinimum;
    /// <summary>The minimum numeric value.</summary>
    public double Minimum;
    /// <summary>Whether a maximum is supplied.</summary>
    public bool HasMaximum;
    /// <summary>The maximum numeric value.</summary>
    public double Maximum;
    /// <summary>Whether the feedback represents pending work.</summary>
    public bool IsBusy;
}

/// <summary>Describes a collapsible region or native disclosure.</summary>
public class UiDisclosure
{
    /// <summary>The stable disclosure key.</summary>
    public string Key = string.Empty;
    /// <summary>The visible summary.</summary>
    public string Summary = string.Empty;
    /// <summary>The disclosed content.</summary>
    public UiNode[] Content = Array.Empty<UiNode>();
    /// <summary>Whether the region is expanded.</summary>
    public bool IsExpanded;
    /// <summary>Whether interaction is disabled.</summary>
    public bool IsDisabled;
}
