using System;

namespace time;

/// <summary>Describes how a local date and time is resolved when a time-zone transition makes it ambiguous or invalid.</summary>
/// <remarks>Use one of the named factory methods. Normal local values always resolve to their single matching instant.</remarks>
public class ZoneResolver : IZoneResolver
{
    internal const int ModeStrict = 1;
    internal const int ModeEarlier = 2;
    internal const int ModeLater = 3;
    internal const int ModeCompatible = 4;
    internal const int ModeShiftForward = 5;

    int mode;

    internal ZoneResolver(int mode)
    {
        RequireMode(mode, "mode");

        this.mode = mode;
    }

    /// <summary>Creates a resolver that rejects ambiguous and invalid local values.</summary>
    /// <returns>A strict resolver.</returns>
    internal static ZoneResolver Strict()
    {
        return new ZoneResolver(ModeStrict);
    }

    /// <summary>Creates a resolver that chooses the earlier instant during an overlap and rejects gaps.</summary>
    /// <returns>An earlier-instant resolver.</returns>
    internal static ZoneResolver Earlier()
    {
        return new ZoneResolver(ModeEarlier);
    }

    /// <summary>Creates a resolver that chooses the later instant during an overlap and rejects gaps.</summary>
    /// <returns>A later-instant resolver.</returns>
    internal static ZoneResolver Later()
    {
        return new ZoneResolver(ModeLater);
    }

    /// <summary>Creates a resolver that chooses the earlier instant during an overlap and shifts invalid values forward across a gap.</summary>
    /// <returns>A resolver suitable for preserving common wall-clock scheduling behavior.</returns>
    internal static ZoneResolver Compatible()
    {
        return new ZoneResolver(ModeCompatible);
    }

    /// <summary>Creates a resolver that shifts invalid values forward across a gap and rejects overlaps.</summary>
    /// <returns>A gap-shifting resolver.</returns>
    internal static ZoneResolver ShiftForward()
    {
        return new ZoneResolver(ModeShiftForward);
    }

    /// <summary>Resolves a local value using this resolver's gap and overlap policy.</summary>
    /// <param name="zone">The time zone in which the local value occurs.</param>
    /// <param name="localDateTime">The local date and time to resolve.</param>
    /// <returns>One offset date-time chosen or shifted according to this resolver's policy.</returns>
    public OffsetDateTime ResolveLocal(TimeZoneRef zone, LocalDateTime localDateTime)
    {
        TimeValidation.RequireNotNull(zone, "zone");
        TimeValidation.RequireNotNull(localDateTime, "localDateTime");

        return Resolve(zone.CreateMapping(localDateTime));
    }

    internal int GetMode()
    {
        return mode;
    }

    internal OffsetDateTime Resolve(ZoneMapping mapping)
    {
        TimeValidation.RequireNotNull(mapping, "mapping");

        if (mapping.GetKind() == ZoneMapping.KindNormal)
        {
            return mapping.GetZone().ConvertInstant(mapping.GetEarlyInstant());
        }

        if (mapping.GetKind() == ZoneMapping.KindAmbiguous)
        {
            return ResolveAmbiguous(mapping);
        }

        return ResolveInvalid(mapping);
    }

    OffsetDateTime ResolveAmbiguous(ZoneMapping mapping)
    {
        if (mode == ModeEarlier || mode == ModeCompatible)
        {
            return mapping.GetZone().ConvertInstant(mapping.GetEarlyInstant());
        }

        if (mode == ModeLater)
        {
            return mapping.GetZone().ConvertInstant(mapping.GetLateInstant());
        }

        throw new InvalidOperationException("Ambiguous local time could not be resolved by this resolver.");
    }

    OffsetDateTime ResolveInvalid(ZoneMapping mapping)
    {
        if (mode == ModeShiftForward || mode == ModeCompatible)
        {
            LocalDateTime shifted = mapping.GetLocalDateTime().AddDuration(mapping.GetGapDuration());
            OffsetDateTime[] mappings = mapping.GetZone().MapLocal(shifted);
            if (mappings.Length == 0)
            {
                throw new InvalidOperationException("Shifted local time did not resolve to a valid mapping.");
            }

            return mappings[0];
        }

        throw new InvalidOperationException("Invalid local time could not be resolved by this resolver.");
    }

    static void RequireMode(int mode, string parameterName)
    {
        if (mode != ModeStrict && mode != ModeEarlier && mode != ModeLater && mode != ModeCompatible && mode != ModeShiftForward)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Zone resolver mode is not supported.");
        }
    }
}
