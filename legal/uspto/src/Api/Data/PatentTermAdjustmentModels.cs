namespace uspto;

/// <summary>
/// Describes one event used in a patent-term-adjustment calculation.
/// </summary>
public class PatentTermAdjustmentHistoryEntry
{
    /// <summary>The date associated with the adjustment event.</summary>
    public global::System.DateTimeOffset EventDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="EventDate"/> was supplied.</summary>
    public bool HasEventDate = false;

    /// <summary>The event description supplied by the source.</summary>
    public string? Description = null;

    /// <summary>The event's sequence number, including fractional sequence values.</summary>
    public double EventSequenceNumber = 0.0;

    /// <summary>Whether <see cref="EventSequenceNumber"/> was supplied.</summary>
    public bool HasEventSequenceNumber = false;

    /// <summary>The sequence number of the event from which this entry originated.</summary>
    public double OriginatingEventSequenceNumber = 0.0;

    /// <summary>Whether <see cref="OriginatingEventSequenceNumber"/> was supplied.</summary>
    public bool HasOriginatingEventSequenceNumber = false;

    /// <summary>The applicant-attributable delay associated with this event, in days.</summary>
    public int ApplicantDelayDays = 0;

    /// <summary>Whether <see cref="ApplicantDelayDays"/> was supplied.</summary>
    public bool HasApplicantDelayDays = false;

    /// <summary>The intellectual-property-office delay associated with this event, in days.</summary>
    public int IpOfficeDelayDays = 0;

    /// <summary>Whether <see cref="IpOfficeDelayDays"/> was supplied.</summary>
    public bool HasIpOfficeDelayDays = false;

    /// <summary>The source patent-term-adjustment or extension code.</summary>
    public string? Code = null;

    /// <summary>Unmodeled history fields retained as raw JSON text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds patent-term-adjustment totals and calculation history for one application.
/// </summary>
public class PatentTermAdjustment
{
    /// <summary>The canonical requested patent application number.</summary>
    public string? ApplicationNumber = null;

    /// <summary>The total patent-term adjustment, in days.</summary>
    public int AdjustmentTotalDays = 0;

    /// <summary>Whether <see cref="AdjustmentTotalDays"/> was supplied.</summary>
    public bool HasAdjustmentTotalDays = false;

    /// <summary>The category A office delay, in days.</summary>
    public int ADelayDays = 0;

    /// <summary>Whether <see cref="ADelayDays"/> was supplied.</summary>
    public bool HasADelayDays = false;

    /// <summary>The category B office delay, in days.</summary>
    public int BDelayDays = 0;

    /// <summary>Whether <see cref="BDelayDays"/> was supplied.</summary>
    public bool HasBDelayDays = false;

    /// <summary>The category C office delay, in days.</summary>
    public int CDelayDays = 0;

    /// <summary>Whether <see cref="CDelayDays"/> was supplied.</summary>
    public bool HasCDelayDays = false;

    /// <summary>The applicant-attributable delay, in days.</summary>
    public int ApplicantDelayDays = 0;

    /// <summary>Whether <see cref="ApplicantDelayDays"/> was supplied.</summary>
    public bool HasApplicantDelayDays = false;

    /// <summary>The non-overlapping office delay, in days.</summary>
    public double NonOverlappingDelayDays = 0.0;

    /// <summary>Whether <see cref="NonOverlappingDelayDays"/> was supplied.</summary>
    public bool HasNonOverlappingDelayDays = false;

    /// <summary>The overlapping office delay, in days.</summary>
    public double OverlappingDelayDays = 0.0;

    /// <summary>Whether <see cref="OverlappingDelayDays"/> was supplied.</summary>
    public bool HasOverlappingDelayDays = false;

    /// <summary>The manual office adjustment applied to the delay calculation, in days.</summary>
    public double IpOfficeAdjustmentDelayDays = 0.0;

    /// <summary>Whether <see cref="IpOfficeAdjustmentDelayDays"/> was supplied.</summary>
    public bool HasIpOfficeAdjustmentDelayDays = false;

    /// <summary>The adjustment calculation events in source order.</summary>
    public PatentTermAdjustmentHistoryEntry[] History = new PatentTermAdjustmentHistoryEntry[0];

    /// <summary>The number of returned calculation events.</summary>
    public int Count = 0;

    /// <summary>The service request identifier associated with the response.</summary>
    public string? RequestIdentifier = null;

    /// <summary>The origin and authority of the adjustment data.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance();

    /// <summary>Unmodeled response and wrapper fields retained as raw JSON text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}
