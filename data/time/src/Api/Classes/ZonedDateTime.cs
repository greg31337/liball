using System;

namespace time;

/// <summary>Represents an instant viewed through a named time zone, including its derived local fields and offset.</summary>
/// <remarks>The instant and zone are authoritative. Duration arithmetic follows the timeline; period arithmetic changes local fields and uses an explicit resolver for transitions.</remarks>
public class ZonedDateTime : IZonedDateTime
{
    Instant instant;
    TimeZoneRef zone;
    OffsetDateTime offsetDateTime;

    internal ZonedDateTime(Instant instant, TimeZoneRef zone)
    {
        TimeValidation.RequireNotNull(instant, "instant");
        TimeValidation.RequireNotNull(zone, "zone");

        this.instant = instant;
        this.zone = zone;
        offsetDateTime = zone.ConvertInstant(instant);
    }

    /// <summary>Views an exact instant in a time zone.</summary><param name="instant">The timeline point.</param><param name="zone">The zone whose rules compute local fields.</param><returns>The zoned representation.</returns>
    internal static ZonedDateTime CreateFromInstant(Instant instant, TimeZoneRef zone)
    {
        return new ZonedDateTime(instant, zone);
    }

    /// <summary>Maps a floating local date-time into a time zone.</summary><param name="localDateTime">The requested wall-clock value.</param><param name="zone">The zone to apply.</param><param name="resolver">The policy for gaps and overlaps.</param><returns>The resolved zoned value.</returns>
    internal static ZonedDateTime CreateFromLocal(LocalDateTime localDateTime, TimeZoneRef zone, IZoneResolver resolver)
    {
        TimeValidation.RequireNotNull(localDateTime, "localDateTime");
        TimeValidation.RequireNotNull(zone, "zone");
        TimeValidation.RequireNotNull(resolver, "resolver");

        OffsetDateTime resolved = zone.ResolveLocal(localDateTime, resolver);
        return new ZonedDateTime(resolved.GetInstant(), zone);
    }

    /// <summary>Parses ISO offset date-time text followed by a bracketed system zone ID.</summary>
    /// <remarks>The parsed offset must agree with the zone's offset at the represented instant.</remarks><param name="text">Text such as <c>2024-01-01T09:00:00-05:00[America/New_York]</c>.</param><returns>The parsed zoned value.</returns><exception cref="FormatException">The structure, offset, or zone relationship is invalid.</exception>
    internal static ZonedDateTime Parse(string text)
    {
        TimeValidation.RequireNotNull(text, "text");

        if (text.Length == 0)
        {
            throw new FormatException("Zoned date-time text must not be empty.");
        }

        int openBracket = text.LastIndexOf('[');
        if (openBracket <= 0 || text[text.Length - 1] != ']')
        {
            throw new FormatException("Zoned date-time text must end with a bracketed time zone ID.");
        }

        int zoneIdLength = text.Length - openBracket - 2;
        if (zoneIdLength <= 0)
        {
            throw new FormatException("Zoned date-time text must include a time zone ID.");
        }

        OffsetDateTime parsedOffsetDateTime = OffsetDateTime.Parse(text.Substring(0, openBracket));
        TimeZoneRef parsedZone = TimeZoneRef.FromSystemId(text.Substring(openBracket + 1, zoneIdLength));
        ZonedDateTime result = CreateFromInstant(parsedOffsetDateTime.GetInstant(), parsedZone);

        if (!result.GetLocalDateTime().EqualsValue(parsedOffsetDateTime.GetLocalDateTime())
            || !result.GetOffset().EqualsValue(parsedOffsetDateTime.GetOffset()))
        {
            throw new FormatException("Zoned date-time offset and local time do not match the time zone.");
        }

        return result;
    }

    /// <summary>Gets the authoritative timeline point.</summary><returns>The stored instant.</returns>
    public Instant GetInstant()
    {
        return instant;
    }

    /// <summary>Gets the time-zone reference.</summary><returns>The zone used for local conversion.</returns>
    public TimeZoneRef GetZone()
    {
        return zone;
    }

    /// <summary>Gets the platform time-zone identifier.</summary><returns>The zone ID.</returns>
    public string GetZoneId()
    {
        return zone.GetId();
    }

    /// <summary>Gets the zone offset in effect at this instant.</summary><returns>The derived offset.</returns>
    public Offset GetOffset()
    {
        return offsetDateTime.GetOffset();
    }

    /// <summary>Gets local calendar and clock fields computed by the zone.</summary><returns>The derived local date-time.</returns>
    public LocalDateTime GetLocalDateTime()
    {
        return offsetDateTime.GetLocalDateTime();
    }

    /// <summary>Gets the derived local date.</summary><returns>The date in this zone.</returns>
    public LocalDate GetDate()
    {
        return offsetDateTime.GetDate();
    }

    /// <summary>Gets the derived local time.</summary><returns>The time of day in this zone.</returns>
    public LocalTime GetTime()
    {
        return offsetDateTime.GetTime();
    }

    /// <summary>Changes the zone while preserving the instant.</summary><param name="targetZone">The zone used for the new local representation.</param><returns>The same timeline point in the target zone.</returns>
    public ZonedDateTime WithZone(TimeZoneRef targetZone)
    {
        TimeValidation.RequireNotNull(targetZone, "targetZone");

        return CreateFromInstant(instant, targetZone);
    }

    /// <summary>Drops the zone rules while preserving current local fields and offset.</summary><returns>An offset date-time representing the same instant.</returns>
    public OffsetDateTime ToOffsetDateTime()
    {
        return offsetDateTime;
    }

    /// <summary>Adds exact elapsed time on the UTC timeline, then recalculates zone fields.</summary><param name="duration">The duration to add.</param><returns>The resulting instant in the same zone.</returns>
    public ZonedDateTime AddDuration(Duration duration)
    {
        TimeValidation.RequireNotNull(duration, "duration");

        return CreateFromInstant(instant.AddDuration(duration), zone);
    }

    /// <summary>Subtracts exact elapsed time on the UTC timeline, then recalculates zone fields.</summary><param name="duration">The duration to subtract.</param><returns>The resulting instant in the same zone.</returns>
    public ZonedDateTime SubtractDuration(Duration duration)
    {
        TimeValidation.RequireNotNull(duration, "duration");

        return CreateFromInstant(instant.SubtractDuration(duration), zone);
    }

    /// <summary>Adds calendar fields to local time and resolves the result in the same zone.</summary><param name="period">The period to add.</param><param name="resolver">The policy for gaps and overlaps.</param><returns>The wall-clock arithmetic result.</returns>
    public ZonedDateTime AddPeriod(Period period, IZoneResolver resolver)
    {
        TimeValidation.RequireNotNull(period, "period");
        TimeValidation.RequireNotNull(resolver, "resolver");

        return CreateFromLocal(GetLocalDateTime().AddPeriod(period), zone, resolver);
    }

    /// <summary>Subtracts calendar fields from local time and resolves the result in the same zone.</summary><param name="period">The period to subtract.</param><param name="resolver">The policy for gaps and overlaps.</param><returns>The wall-clock arithmetic result.</returns>
    public ZonedDateTime SubtractPeriod(Period period, IZoneResolver resolver)
    {
        TimeValidation.RequireNotNull(period, "period");
        TimeValidation.RequireNotNull(resolver, "resolver");

        return CreateFromLocal(GetLocalDateTime().SubtractPeriod(period), zone, resolver);
    }

    /// <summary>Gets the earliest valid instant on this value's local date.</summary><remarks>If midnight is skipped, the first valid local time after the gap is used.</remarks><returns>The start of the local day in the same zone.</returns>
    public ZonedDateTime StartOfDay()
    {
        return CreateFromLocal(LocalDateTime.Create(GetDate(), LocalTime.Midnight()), zone, ZoneResolver.Compatible());
    }

    /// <summary>Orders values by their UTC instant, ignoring zones.</summary><param name="other">The value to compare.</param><returns>A timeline comparison result.</returns>
    public int CompareInstant(ZonedDateTime other)
    {
        TimeValidation.RequireNotNull(other, "other");

        return instant.CompareTo(other.GetInstant());
    }

    /// <summary>Tests equality of both instant and zone ID.</summary><param name="other">The value to compare, or null.</param><returns><c>true</c> when the timeline point and zone identity match.</returns>
    public bool EqualsValue(ZonedDateTime? other)
    {
        if (other == null)
        {
            return false;
        }

        return instant.EqualsValue(other.GetInstant())
            && GetZoneId() == other.GetZoneId()
            && offsetDateTime.EqualsValue(other.ToOffsetDateTime());
    }

    /// <summary>Formats local fields, offset, and bracketed zone ID.</summary><returns>Invariant zoned date-time text.</returns>
    public string ToIsoString()
    {
        return offsetDateTime.ToIsoString() + "[" + zone.GetId() + "]";
    }
}
