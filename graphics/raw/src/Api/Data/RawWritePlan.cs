namespace raw;

/// <summary>Reports the decoded stored-sample guarantee planned or performed for writer output.</summary>
public class RawPixelValidationReport
{
    /// <summary>The identity or bounded-error guarantee.</summary>
    public RawPixelValidationKind Kind;
    /// <summary>Whether output must be reopened and decoded for this guarantee.</summary>
    public bool Required;
    /// <summary>Whether reopen and decoded-sample comparison has been performed.</summary>
    public bool Performed;
    /// <summary>Whether the performed comparison passed.</summary>
    public bool Passed;
    /// <summary>The expected or compared stored-sample count.</summary>
    public long SampleCount;
    /// <summary>The largest observed absolute stored-sample error, or <c>-1</c> before comparison.</summary>
    public int MaximumAbsoluteError;
    /// <summary>The permitted maximum absolute stored-sample error.</summary>
    public int AllowedMaximumAbsoluteError;
    /// <summary>A deterministic explanation of the validation guarantee or result.</summary>
    public string Details;

    /// <summary>Creates a decoded stored-sample validation report.</summary>
    /// <param name="kind">The numeric guarantee.</param>
    /// <param name="required">Whether post-write reopen validation is required.</param>
    /// <param name="performed">Whether comparison was performed.</param>
    /// <param name="passed">Whether the performed comparison passed.</param>
    /// <param name="sampleCount">The expected or compared sample count.</param>
    /// <param name="maximumAbsoluteError">The observed maximum error, or <c>-1</c>.</param>
    /// <param name="allowedMaximumAbsoluteError">The permitted maximum error.</param>
    /// <param name="details">The deterministic explanation.</param>
    public RawPixelValidationReport(RawPixelValidationKind kind, bool required, bool performed, bool passed, long sampleCount, int maximumAbsoluteError, int allowedMaximumAbsoluteError, string details)
    {
        Kind = kind;
        Required = required;
        Performed = performed;
        Passed = passed;
        SampleCount = sampleCount;
        MaximumAbsoluteError = maximumAbsoluteError;
        AllowedMaximumAbsoluteError = allowedMaximumAbsoluteError;
        Details = details == null ? string.Empty : details;
    }

    /// <summary>Creates the default report for a write without decoded pixel validation.</summary>
    /// <returns>A populated no-validation report.</returns>
    internal static RawPixelValidationReport CreateNone()
    {
        return new RawPixelValidationReport(RawPixelValidationKind.None, false, false, false, 0, -1, 0, "No decoded pixel validation is requested.");
    }
}

/// <summary>Describes one block-level action in a dry-run write plan.</summary>
public class RawWriteChange
{
    /// <summary>The kind of block action.</summary>
    public RawWriteChangeKind Kind;
    /// <summary>A stable block or metadata location.</summary>
    public string Location;
    /// <summary>A human-readable description of the action.</summary>
    public string Description;
    /// <summary>The original byte offset, or <c>-1</c> for a new block.</summary>
    public long OriginalOffset;
    /// <summary>The original byte length, or <c>0</c> for a new block.</summary>
    public long OriginalLength;
    /// <summary>The planned byte offset, or <c>-1</c> when not yet assigned or dropped.</summary>
    public long PlannedOffset;
    /// <summary>The planned byte length, or <c>0</c> for a dropped block.</summary>
    public long PlannedLength;
    /// <summary>Whether this action requires explicit data-loss consent.</summary>
    public bool RequiresConsent;

    /// <summary>Creates a block-level write action.</summary>
    /// <param name="kind">The action kind.</param>
    /// <param name="location">The stable block location.</param>
    /// <param name="description">The human-readable description.</param>
    /// <param name="originalOffset">The original offset, or <c>-1</c>.</param>
    /// <param name="originalLength">The original length.</param>
    /// <param name="plannedOffset">The planned offset, or <c>-1</c>.</param>
    /// <param name="plannedLength">The planned length.</param>
    /// <param name="requiresConsent">Whether explicit consent is required.</param>
    public RawWriteChange(RawWriteChangeKind kind, string location, string description, long originalOffset, long originalLength, long plannedOffset, long plannedLength, bool requiresConsent)
    {
        Kind = kind;
        Location = location == null ? string.Empty : location;
        Description = description == null ? string.Empty : description;
        OriginalOffset = originalOffset;
        OriginalLength = originalLength;
        PlannedOffset = plannedOffset;
        PlannedLength = plannedLength;
        RequiresConsent = requiresConsent;
    }
}

/// <summary>Reports the deterministic outcome of planning a write without committing output.</summary>
public class RawWritePlan
{
    /// <summary>Whether layout and compatibility checks permit the planned write.</summary>
    public bool CanWrite;
    /// <summary>The predicted output size in bytes, or <c>-1</c> when unavailable.</summary>
    public long PredictedSize;
    /// <summary>The selected write profile name, or an empty string.</summary>
    public string ProfileName;
    /// <summary>Application/version compatibility evidence for the selected write profile.</summary>
    public RawWriteCompatibilityEntry[] Compatibility;
    /// <summary>Source authenticity, signature, digest, or original-decision blocks retained opaquely by the plan.</summary>
    public RawPayloadBlock[] AuthenticityBlocks;
    /// <summary>Whether caller edits make retained authenticity data no longer describe the rewritten file.</summary>
    public bool AuthenticityInvalidated;
    /// <summary>The decoded stored-sample identity or error-bound guarantee for this write.</summary>
    public RawPixelValidationReport PixelValidation;
    /// <summary>Block-level actions in deterministic output order.</summary>
    public RawWriteChange[] Changes;
    /// <summary>Planning and compatibility diagnostics.</summary>
    public RawDiagnostic[] Diagnostics;

    /// <summary>Creates a dry-run write plan.</summary>
    /// <param name="canWrite">Whether the planned write is permitted.</param>
    /// <param name="predictedSize">The predicted output size, or <c>-1</c>.</param>
    /// <param name="profileName">The selected profile name.</param>
    /// <param name="changes">The planned changes, or <c>null</c> for an empty set.</param>
    /// <param name="diagnostics">The diagnostics, or <c>null</c> for an empty set.</param>
    public RawWritePlan(bool canWrite, long predictedSize, string? profileName, RawWriteChange[]? changes, RawDiagnostic[]? diagnostics)
    {
        CanWrite = canWrite;
        PredictedSize = predictedSize;
        ProfileName = profileName == null ? string.Empty : profileName;
        Compatibility = System.Array.Empty<RawWriteCompatibilityEntry>();
        AuthenticityBlocks = System.Array.Empty<RawPayloadBlock>();
        AuthenticityInvalidated = false;
        PixelValidation = RawPixelValidationReport.CreateNone();
        Changes = changes == null ? System.Array.Empty<RawWriteChange>() : changes;
        Diagnostics = diagnostics == null ? System.Array.Empty<RawDiagnostic>() : diagnostics;
    }

    /// <summary>Creates a dry-run write plan with selected-profile compatibility evidence.</summary>
    /// <param name="canWrite">Whether the planned write is permitted.</param>
    /// <param name="predictedSize">The predicted output size, or <c>-1</c>.</param>
    /// <param name="profileName">The selected profile name.</param>
    /// <param name="compatibility">Application/version compatibility evidence for the profile.</param>
    /// <param name="changes">The planned changes, or <c>null</c> for an empty set.</param>
    /// <param name="diagnostics">The diagnostics, or <c>null</c> for an empty set.</param>
    /// <returns>The populated write plan.</returns>
    internal static RawWritePlan CreateWithCompatibility(bool canWrite, long predictedSize, string profileName, RawWriteCompatibilityEntry[] compatibility, RawWriteChange[] changes, RawDiagnostic[]? diagnostics)
    {
        RawWritePlan plan = new RawWritePlan(canWrite, predictedSize, profileName, changes, diagnostics);
        plan.Compatibility = compatibility == null ? System.Array.Empty<RawWriteCompatibilityEntry>() : compatibility;
        return plan;
    }

    /// <summary>Returns whether any planned change requires explicit data-loss consent.</summary>
    /// <returns><c>true</c> when consent is required; otherwise <c>false</c>.</returns>
    internal bool RequiresDataLossConsent()
    {
        int index = 0;
        while (index < Changes.Length)
        {
            RawWriteChange change = Changes[index];
            if (change != null && change.RequiresConsent)
            {
                return true;
            }
            index++;
        }
        return false;
    }

    /// <summary>Returns whether the plan contains an error diagnostic.</summary>
    /// <returns><c>true</c> when an error is present; otherwise <c>false</c>.</returns>
    internal bool HasErrors()
    {
        int index = 0;
        while (index < Diagnostics.Length)
        {
            RawDiagnostic diagnostic = Diagnostics[index];
            if (diagnostic != null && diagnostic.Severity == RawSeverity.Error)
            {
                return true;
            }
            index++;
        }
        return false;
    }

    /// <summary>Returns whether the plan contains a warning diagnostic.</summary>
    /// <returns><c>true</c> when a warning is present; otherwise <c>false</c>.</returns>
    internal bool HasWarnings()
    {
        int index = 0;
        while (index < Diagnostics.Length)
        {
            RawDiagnostic diagnostic = Diagnostics[index];
            if (diagnostic != null && diagnostic.Severity == RawSeverity.Warning)
            {
                return true;
            }
            index++;
        }
        return false;
    }

    /// <summary>Returns whether the selected profile has a passed application/version compatibility result.</summary>
    /// <returns><c>true</c> when confirmed compatibility evidence is present; otherwise <c>false</c>.</returns>
    internal bool HasConfirmedCompatibility()
    {
        int index = 0;
        while (index < Compatibility.Length)
        {
            if (Compatibility[index] != null && Compatibility[index].ProfileId == ProfileName && Compatibility[index].IsConfirmed())
            {
                return true;
            }
            index++;
        }
        return false;
    }

    /// <summary>Validates profile evidence, layout, diagnostics, and post-write guarantees for this plan.</summary>
    /// <param name="options">Validation mode, subsystem selection, and limits.</param>
    /// <returns>Machine-readable profile and writer conformance findings.</returns>
    internal RawValidationReport Validate(RawValidationOptions options)
    {
        return RawCodec.Validate(this, options);
    }
}
