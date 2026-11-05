using System;

namespace time;

internal class ZoneMapping
{
    internal const int KindNormal = 1;
    internal const int KindAmbiguous = 2;
    internal const int KindInvalid = 3;

    int kind;
    LocalDateTime localDateTime;
    TimeZoneRef zone;
    Instant earlyInstant;
    Instant lateInstant;
    Duration gapDuration;

    internal ZoneMapping(int kind, LocalDateTime localDateTime, TimeZoneRef zone, Instant earlyInstant, Instant lateInstant, Duration gapDuration)
    {
        RequireKind(kind, "kind");
        TimeValidation.RequireNotNull(localDateTime, "localDateTime");
        TimeValidation.RequireNotNull(zone, "zone");
        TimeValidation.RequireNotNull(earlyInstant, "earlyInstant");
        TimeValidation.RequireNotNull(lateInstant, "lateInstant");
        TimeValidation.RequireNotNull(gapDuration, "gapDuration");

        this.kind = kind;
        this.localDateTime = localDateTime;
        this.zone = zone;
        this.earlyInstant = earlyInstant;
        this.lateInstant = lateInstant;
        this.gapDuration = gapDuration;
    }

    internal static ZoneMapping CreateNormal(LocalDateTime localDateTime, TimeZoneRef zone, Instant instant)
    {
        TimeValidation.RequireNotNull(instant, "instant");

        return new ZoneMapping(KindNormal, localDateTime, zone, instant, instant, Duration.Zero());
    }

    internal static ZoneMapping CreateAmbiguous(LocalDateTime localDateTime, TimeZoneRef zone, Instant earlyInstant, Instant lateInstant)
    {
        TimeValidation.RequireNotNull(earlyInstant, "earlyInstant");
        TimeValidation.RequireNotNull(lateInstant, "lateInstant");

        if (earlyInstant.CompareTo(lateInstant) > 0)
        {
            throw new ArgumentException("Early instant must not be after late instant.", "earlyInstant");
        }

        return new ZoneMapping(KindAmbiguous, localDateTime, zone, earlyInstant, lateInstant, Duration.Zero());
    }

    internal static ZoneMapping CreateInvalid(LocalDateTime localDateTime, TimeZoneRef zone, Instant earlyInstant, Instant lateInstant, Duration gapDuration)
    {
        TimeValidation.RequireNotNull(earlyInstant, "earlyInstant");
        TimeValidation.RequireNotNull(lateInstant, "lateInstant");
        TimeValidation.RequireNotNull(gapDuration, "gapDuration");

        if (earlyInstant.CompareTo(lateInstant) > 0)
        {
            throw new ArgumentException("Early instant must not be after late instant.", "earlyInstant");
        }

        if (gapDuration.GetTicks() <= 0L)
        {
            throw new ArgumentOutOfRangeException("gapDuration", "Gap duration must be positive.");
        }

        return new ZoneMapping(KindInvalid, localDateTime, zone, earlyInstant, lateInstant, gapDuration);
    }

    internal int GetKind()
    {
        return kind;
    }

    internal LocalDateTime GetLocalDateTime()
    {
        return localDateTime;
    }

    internal TimeZoneRef GetZone()
    {
        return zone;
    }

    internal Instant GetEarlyInstant()
    {
        return earlyInstant;
    }

    internal Instant GetLateInstant()
    {
        return lateInstant;
    }

    internal Duration GetGapDuration()
    {
        return gapDuration;
    }

    static void RequireKind(int kind, string parameterName)
    {
        if (kind != KindNormal && kind != KindAmbiguous && kind != KindInvalid)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Zone mapping kind is not supported.");
        }
    }
}
