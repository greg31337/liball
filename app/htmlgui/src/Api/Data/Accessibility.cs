namespace htmlgui;

/// <summary>Describes explicit accessible semantics for a node.</summary>
public class UiAccessibility
{
    /// <summary>The optional ARIA role.</summary>
    public string? Role;
    /// <summary>The optional direct accessible label.</summary>
    public string? Label;
    /// <summary>The optional description.</summary>
    public string? Description;
    /// <summary>Whether an accessible heading level is supplied.</summary>
    public bool HasHeadingLevel;
    /// <summary>The accessible heading level.</summary>
    public int HeadingLevel;
    /// <summary>The ID-based relationships.</summary>
    public UiAriaRelationship[] Relationships = System.Array.Empty<UiAriaRelationship>();
    /// <summary>The live-region mode.</summary>
    public UiAriaLiveMode LiveMode;
    /// <summary>Whether a current-item kind is supplied.</summary>
    public bool HasCurrentKind;
    /// <summary>The current-item kind.</summary>
    public UiAriaCurrentKind CurrentKind;
    /// <summary>Whether the node is hidden from accessibility APIs.</summary>
    public bool IsHidden;
    /// <summary>The opt-in accessibility presentation and environment hooks.</summary>
    public UiAccessibilityFeature[] Features = System.Array.Empty<UiAccessibilityFeature>();
    /// <summary>The minimum target size in CSS pixels when the target-size feature is enabled.</summary>
    public int MinimumTargetSizePixels = 24;
    /// <summary>The textual indicator used when the non-color-status feature is enabled.</summary>
    public string? StatusText;
    /// <summary>The pending announcements.</summary>
    public UiLiveAnnouncement[] Announcements = System.Array.Empty<UiLiveAnnouncement>();
    /// <summary>The optional validation report.</summary>
    public UiAccessibilityReport? Report;
}

/// <summary>Describes one ARIA ID-reference relationship.</summary>
public class UiAriaRelationship
{
    /// <summary>The relationship attribute name.</summary>
    public string Name = string.Empty;
    /// <summary>The referenced node identifiers.</summary>
    public string[] TargetIds = System.Array.Empty<string>();
}

/// <summary>Describes a queued live-region announcement.</summary>
public class UiLiveAnnouncement
{
    /// <summary>The stable announcement identifier.</summary>
    public string Id = string.Empty;
    /// <summary>The announcement text.</summary>
    public string Message = string.Empty;
    /// <summary>The live-region mode.</summary>
    public UiAriaLiveMode Mode = UiAriaLiveMode.Polite;
    /// <summary>Whether the entire region is announced.</summary>
    public bool Atomic;
}

/// <summary>Describes accessibility validation diagnostics.</summary>
public class UiAccessibilityReport
{
    /// <summary>The ordered diagnostics.</summary>
    public UiDiagnostic[] Diagnostics = System.Array.Empty<UiDiagnostic>();
    /// <summary>Whether validation succeeded.</summary>
    public bool IsValid = true;
}
