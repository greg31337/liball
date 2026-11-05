using NUnit.Framework;

namespace time.Tests;

[TestFixture]
public class TimeZoneRefTests
{
    [Test]
    public void Constructor_StoresTimeZoneInfo()
    {
        TimeZoneRef zone = new TimeZoneRef(TimeZoneInfo.Utc);

        Assert.That(zone.GetTimeZoneInfo(), Is.SameAs(TimeZoneInfo.Utc));
        Assert.That(zone.GetId(), Is.EqualTo(TimeZoneInfo.Utc.Id));
        Assert.That(zone.GetDisplayName(), Is.EqualTo(TimeZoneInfo.Utc.DisplayName));
    }

    [Test]
    public void Constructor_RequiresTimeZoneInfo()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new TimeZoneRef(null!))!;

        Assert.That(exception.ParamName, Is.EqualTo("timeZoneInfo"));
    }

    [Test]
    public void Utc_ReturnsBuiltInUtcZone()
    {
        TimeZoneRef zone = TimeZoneRef.Utc();

        Assert.That(zone.GetTimeZoneInfo(), Is.SameAs(TimeZoneInfo.Utc));
        Assert.That(zone.GetId(), Is.EqualTo(TimeZoneInfo.Utc.Id));
    }

    [Test]
    public void Local_ReturnsSystemLocalZone()
    {
        TimeZoneRef zone = TimeZoneRef.Local();

        Assert.That(zone.GetTimeZoneInfo(), Is.SameAs(TimeZoneInfo.Local));
        Assert.That(zone.GetId(), Is.EqualTo(TimeZoneInfo.Local.Id));
    }

    [TestCase(0, "UTC")]
    [TestCase(19800, "UTC+05:30")]
    [TestCase(-28800, "UTC-08:00")]
    [TestCase(50400, "UTC+14:00")]
    [TestCase(-50400, "UTC-14:00")]
    public void FixedOffsetFactory_CreatesStableTransitionFreeTimeZoneInfo(int totalSeconds, string expectedId)
    {
        Offset offset = Offset.FromSeconds(totalSeconds);

        TimeZoneRef zone = TimeApi.Factory.CreateFixedOffsetTimeZone(offset);
        TimeZoneInfo zoneInfo = zone.GetTimeZoneInfo();

        Assert.That(zone.GetId(), Is.EqualTo(expectedId));
        Assert.That(zone.GetDisplayName(), Is.EqualTo(expectedId));
        Assert.That(zoneInfo.StandardName, Is.EqualTo(expectedId));
        Assert.That(zoneInfo.BaseUtcOffset, Is.EqualTo(TimeSpan.FromSeconds(totalSeconds)));
        Assert.That(zoneInfo.SupportsDaylightSavingTime, Is.False);
        Assert.That(zoneInfo.GetAdjustmentRules(), Is.Empty);
    }

    [Test]
    public void FixedOffsetFactory_RequiresOffset()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => TimeApi.Factory.CreateFixedOffsetTimeZone(null!))!;

        Assert.That(exception.ParamName, Is.EqualTo("offset"));
    }

    [TestCase(1)]
    [TestCase(-1)]
    [TestCase(19845)]
    [TestCase(-19845)]
    public void FixedOffsetFactory_RejectsSecondPrecisionUnsupportedByTimeZoneInfo(int totalSeconds)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => TimeApi.Factory.CreateFixedOffsetTimeZone(Offset.FromSeconds(totalSeconds)))!;

        Assert.That(exception, Is.Not.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(exception.ParamName, Is.EqualTo("offset"));
    }

    [TestCase(50460)]
    [TestCase(-50460)]
    [TestCase(64800)]
    [TestCase(-64800)]
    public void FixedOffsetFactory_RejectsOffsetsBeyondTimeZoneInfoRange(int totalSeconds)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => TimeApi.Factory.CreateFixedOffsetTimeZone(Offset.FromSeconds(totalSeconds)))!;

        Assert.That(exception.ParamName, Is.EqualTo("offset"));
    }

    [TestCase(0, "UTC")]
    [TestCase(330, "UTC+05:30")]
    [TestCase(-480, "UTC-08:00")]
    public void FixedOffsetIdentity_RoundTripsThroughZoneIdLookup(int totalMinutes, string expectedId)
    {
        TimeZoneRef created = TimeApi.Factory.CreateFixedOffsetTimeZone(Offset.FromMinutes(totalMinutes));
        TimeZoneRef found = TimeApi.Factory.CreateTimeZoneFromSystemId(created.GetId());

        Assert.That(created.GetId(), Is.EqualTo(expectedId));
        Assert.That(found.GetId(), Is.EqualTo(expectedId));
        Assert.That(found.GetTimeZoneInfo().BaseUtcOffset, Is.EqualTo(created.GetTimeZoneInfo().BaseUtcOffset));
        Assert.That(found.GetTimeZoneInfo().GetAdjustmentRules(), Is.Empty);
    }

    [Test]
    public void FixedOffsetZone_ConvertsPositiveAndNegativeOffsets()
    {
        Instant instant = Instant.Parse("2024-02-29T12:34:56.1234567Z");
        TimeZoneRef positive = TimeApi.Factory.CreateFixedOffsetTimeZone(Offset.FromMinutes(330));
        TimeZoneRef negative = TimeApi.Factory.CreateFixedOffsetTimeZone(Offset.FromHours(-8));

        OffsetDateTime positiveValue = positive.ConvertInstant(instant);
        OffsetDateTime negativeValue = negative.ConvertInstant(instant);

        Assert.That(positive.GetOffset(instant).GetTotalSeconds(), Is.EqualTo(19800));
        Assert.That(positiveValue.ToIsoString(), Is.EqualTo("2024-02-29T18:04:56.1234567+05:30"));
        Assert.That(positiveValue.GetInstant().EqualsValue(instant), Is.True);
        Assert.That(negative.GetOffset(instant).GetTotalSeconds(), Is.EqualTo(-28800));
        Assert.That(negativeValue.ToIsoString(), Is.EqualTo("2024-02-29T04:34:56.1234567-08:00"));
        Assert.That(negativeValue.GetInstant().EqualsValue(instant), Is.True);
    }

    [Test]
    public void FixedOffsetZone_MapsEveryLocalTimeNormally()
    {
        TimeZoneRef zone = TimeApi.Factory.CreateFixedOffsetTimeZone(Offset.FromMinutes(-210));
        LocalDateTime localDateTime = LocalDateTime.Create(2024, 2, 29, 12, 34, 56, 789, 123, 4);

        OffsetDateTime[] mappings = zone.MapLocal(localDateTime);

        Assert.That(zone.IsInvalidLocal(localDateTime), Is.False);
        Assert.That(zone.IsAmbiguousLocal(localDateTime), Is.False);
        Assert.That(zone.GetAmbiguousOffsets(localDateTime), Is.Empty);
        Assert.That(mappings.Length, Is.EqualTo(1));
        Assert.That(mappings[0].GetLocalDateTime().EqualsValue(localDateTime), Is.True);
        Assert.That(mappings[0].GetOffset().GetTotalSeconds(), Is.EqualTo(-12600));
        Assert.That(mappings[0].GetInstant().ToIsoString(), Is.EqualTo("2024-02-29T16:04:56.7891234Z"));
        Assert.That(zone.ResolveLocal(localDateTime, ZoneResolver.Strict()).EqualsValue(mappings[0]), Is.True);
    }

    [Test]
    public void FixedOffsetZone_HasNoTransitions()
    {
        TimeZoneRef zone = TimeApi.Factory.CreateFixedOffsetTimeZone(Offset.FromMinutes(345));
        Instant instant = Instant.Parse("2024-06-01T00:00:00Z");

        Assert.That(zone.GetNextTransition(instant), Is.Null);
        Assert.That(zone.GetPreviousTransition(instant), Is.Null);
    }

    [Test]
    public void FixedOffsetZone_FormatsAndParsesZonedDateTimeWithStableIdentity()
    {
        Instant instant = Instant.Parse("2024-02-29T12:00:00Z");
        TimeZoneRef zone = TimeApi.Factory.CreateFixedOffsetTimeZone(Offset.FromMinutes(330));
        ZonedDateTime zoned = TimeApi.Factory.CreateZonedDateTimeFromInstant(instant, zone);

        string text = zoned.ToIsoString();
        ZonedDateTime parsed = TimeApi.Factory.ParseZonedDateTime(text);

        Assert.That(text, Is.EqualTo("2024-02-29T17:30:00+05:30[UTC+05:30]"));
        Assert.That(parsed.EqualsValue(zoned), Is.True);
        Assert.That(parsed.GetZoneId(), Is.EqualTo("UTC+05:30"));
    }

    [Test]
    public void FromSystemId_FindsUtcZone()
    {
        TimeZoneRef zone = TimeZoneRef.FromSystemId(TimeZoneInfo.Utc.Id);

        Assert.That(zone.GetId(), Is.EqualTo(TimeZoneInfo.Utc.Id));
        Assert.That(zone.GetTimeZoneInfo().BaseUtcOffset, Is.EqualTo(TimeSpan.Zero));
    }

    [Test]
    public void FromSystemId_FindsLocalZoneById()
    {
        TimeZoneRef zone = TimeZoneRef.FromSystemId(TimeZoneInfo.Local.Id);

        Assert.That(zone.GetId(), Is.EqualTo(TimeZoneInfo.Local.Id));
    }

    [Test]
    public void FromSystemId_RequiresId()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => TimeZoneRef.FromSystemId(null!))!;

        Assert.That(exception.ParamName, Is.EqualTo("id"));
    }

    [Test]
    public void FromSystemId_RejectsUnknownSystemId()
    {
        Assert.Throws<TimeZoneNotFoundException>(() => TimeZoneRef.FromSystemId("time-library/unknown-zone"));
    }

    [Test]
    public void GetAvailableSystemZones_ReturnsWrappedSystemZones()
    {
        TimeZoneRef[] zones = TimeZoneRef.GetAvailableSystemZones();

        Assert.That(zones.Length, Is.GreaterThan(0));
        foreach (TimeZoneRef zone in zones)
        {
            Assert.That(zone, Is.Not.Null);
            Assert.That(zone.GetTimeZoneInfo(), Is.Not.Null);
            Assert.That(zone.GetId(), Is.Not.Empty);
        }
    }

    [Test]
    public void GetAvailableSystemZones_CanRoundTripFirstAvailableZoneById()
    {
        TimeZoneRef[] zones = TimeZoneRef.GetAvailableSystemZones();
        TimeZoneRef first = zones[0];
        TimeZoneRef found = TimeZoneRef.FromSystemId(first.GetId());

        Assert.That(found.GetId(), Is.EqualTo(first.GetId()));
    }

    [Test]
    public void GetOffset_ReturnsZeroForUtcZone()
    {
        TimeZoneRef zone = TimeZoneRef.Utc();

        Assert.That(zone.GetOffset(Instant.UnixEpoch()).EqualsValue(Offset.Zero()), Is.True);
        Assert.That(zone.GetOffset(Instant.FromUtcTicks(TimeConstants.MinUtcTicks)).EqualsValue(Offset.Zero()), Is.True);
        Assert.That(zone.GetOffset(Instant.FromUtcTicks(TimeConstants.MaxUtcTicks)).EqualsValue(Offset.Zero()), Is.True);
    }

    [Test]
    public void GetOffset_RequiresInstant()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => TimeZoneRef.Utc().GetOffset(null!))!;

        Assert.That(exception.ParamName, Is.EqualTo("instant"));
    }

    [Test]
    public void GetOffset_MatchesTimeZoneInfoForLocalZone()
    {
        Instant instant = Instant.Parse("2024-02-29T12:34:56Z");
        TimeZoneRef zone = TimeZoneRef.Local();
        TimeSpan expected = TimeZoneInfo.Local.GetUtcOffset(instant.ToDateTimeOffset());

        Assert.That(zone.GetOffset(instant).ToTimeSpan(), Is.EqualTo(expected));
    }

    [Test]
    public void GetOffset_MatchesTimeZoneInfoForFirstAvailableSystemZone()
    {
        TimeZoneRef[] zones = TimeZoneRef.GetAvailableSystemZones();
        TimeZoneRef zone = zones[0];
        Instant instant = Instant.Parse("2024-02-29T12:34:56Z");
        TimeSpan expected = zone.GetTimeZoneInfo().GetUtcOffset(instant.ToDateTimeOffset());

        Assert.That(zone.GetOffset(instant).ToTimeSpan(), Is.EqualTo(expected));
    }

    [Test]
    public void ConvertInstant_ReturnsUtcOffsetDateTimeForUtcZone()
    {
        Instant instant = Instant.Parse("2024-02-29T12:34:56.1234567Z");
        OffsetDateTime converted = TimeZoneRef.Utc().ConvertInstant(instant);

        Assert.That(converted.GetLocalDateTime().ToIsoString(), Is.EqualTo("2024-02-29T12:34:56.1234567"));
        Assert.That(converted.GetOffset().EqualsValue(Offset.Zero()), Is.True);
        Assert.That(converted.GetInstant().EqualsValue(instant), Is.True);
    }

    [Test]
    public void ConvertInstant_RequiresInstant()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => TimeZoneRef.Utc().ConvertInstant(null!))!;

        Assert.That(exception.ParamName, Is.EqualTo("instant"));
    }

    [Test]
    public void ConvertInstant_MatchesTimeZoneInfoForLocalZone()
    {
        Instant instant = Instant.Parse("2024-02-29T12:34:56Z");
        TimeZoneRef zone = TimeZoneRef.Local();
        OffsetDateTime converted = zone.ConvertInstant(instant);
        TimeSpan expectedOffset = TimeZoneInfo.Local.GetUtcOffset(instant.ToDateTimeOffset());
        DateTime expectedLocal = TimeZoneInfo.ConvertTime(instant.ToDateTimeOffset(), TimeZoneInfo.Local).DateTime;

        Assert.That(converted.GetOffset().ToTimeSpan(), Is.EqualTo(expectedOffset));
        Assert.That(converted.GetLocalDateTime().ToDateTimeUnspecified(), Is.EqualTo(expectedLocal));
        Assert.That(converted.GetInstant().EqualsValue(instant), Is.True);
    }

    [Test]
    public void LocalMapping_RequiresLocalDateTime()
    {
        TimeZoneRef zone = TimeZoneRef.Utc();

        ArgumentNullException invalidException = Assert.Throws<ArgumentNullException>(() => zone.IsInvalidLocal(null!))!;
        ArgumentNullException ambiguousException = Assert.Throws<ArgumentNullException>(() => zone.IsAmbiguousLocal(null!))!;
        ArgumentNullException offsetsException = Assert.Throws<ArgumentNullException>(() => zone.GetAmbiguousOffsets(null!))!;
        ArgumentNullException mapException = Assert.Throws<ArgumentNullException>(() => zone.MapLocal(null!))!;

        Assert.That(invalidException.ParamName, Is.EqualTo("localDateTime"));
        Assert.That(ambiguousException.ParamName, Is.EqualTo("localDateTime"));
        Assert.That(offsetsException.ParamName, Is.EqualTo("localDateTime"));
        Assert.That(mapException.ParamName, Is.EqualTo("localDateTime"));
    }

    [Test]
    public void MapLocal_ReturnsSingleZeroOffsetMappingForUtcZone()
    {
        LocalDateTime localDateTime = LocalDateTime.Create(2024, 2, 29, 12, 34, 56);
        TimeZoneRef zone = TimeZoneRef.Utc();
        OffsetDateTime[] mappings = zone.MapLocal(localDateTime);

        Assert.That(zone.IsInvalidLocal(localDateTime), Is.False);
        Assert.That(zone.IsAmbiguousLocal(localDateTime), Is.False);
        Assert.That(zone.GetAmbiguousOffsets(localDateTime).Length, Is.EqualTo(0));
        Assert.That(mappings.Length, Is.EqualTo(1));
        Assert.That(mappings[0].GetLocalDateTime().EqualsValue(localDateTime), Is.True);
        Assert.That(mappings[0].GetOffset().EqualsValue(Offset.Zero()), Is.True);
    }

    [Test]
    public void MapLocal_ReturnsNormalMappingWithResolvedOffset()
    {
        TimeZoneRef zone = new TimeZoneRef(CreateTestDstZone());
        LocalDateTime winter = LocalDateTime.Create(2024, 1, 15, 12, 0, 0);
        LocalDateTime summer = LocalDateTime.Create(2024, 6, 15, 12, 0, 0);

        OffsetDateTime[] winterMappings = zone.MapLocal(winter);
        OffsetDateTime[] summerMappings = zone.MapLocal(summer);

        Assert.That(winterMappings.Length, Is.EqualTo(1));
        Assert.That(winterMappings[0].GetOffset().GetTotalSeconds(), Is.EqualTo(-18000));
        Assert.That(winterMappings[0].GetInstant().ToIsoString(), Is.EqualTo("2024-01-15T17:00:00Z"));
        Assert.That(summerMappings.Length, Is.EqualTo(1));
        Assert.That(summerMappings[0].GetOffset().GetTotalSeconds(), Is.EqualTo(-14400));
        Assert.That(summerMappings[0].GetInstant().ToIsoString(), Is.EqualTo("2024-06-15T16:00:00Z"));
    }

    [Test]
    public void MapLocal_ReturnsNoMappingsForInvalidLocalTime()
    {
        TimeZoneRef zone = new TimeZoneRef(CreateTestDstZone());
        LocalDateTime invalid = LocalDateTime.Create(2024, 3, 10, 2, 30, 0);

        Assert.That(zone.IsInvalidLocal(invalid), Is.True);
        Assert.That(zone.IsAmbiguousLocal(invalid), Is.False);
        Assert.That(zone.GetAmbiguousOffsets(invalid).Length, Is.EqualTo(0));
        Assert.That(zone.MapLocal(invalid).Length, Is.EqualTo(0));
    }

    [Test]
    public void MapLocal_ReturnsTwoMappingsForAmbiguousLocalTime()
    {
        TimeZoneRef zone = new TimeZoneRef(CreateTestDstZone());
        LocalDateTime ambiguous = LocalDateTime.Create(2024, 11, 3, 1, 30, 0);
        Offset[] offsets = zone.GetAmbiguousOffsets(ambiguous);
        OffsetDateTime[] mappings = zone.MapLocal(ambiguous);

        Assert.That(zone.IsInvalidLocal(ambiguous), Is.False);
        Assert.That(zone.IsAmbiguousLocal(ambiguous), Is.True);
        Assert.That(offsets.Length, Is.EqualTo(2));
        Assert.That(ContainsOffset(offsets, -18000), Is.True);
        Assert.That(ContainsOffset(offsets, -14400), Is.True);
        Assert.That(mappings.Length, Is.EqualTo(2));
        Assert.That(ContainsMappingOffset(mappings, -18000), Is.True);
        Assert.That(ContainsMappingOffset(mappings, -14400), Is.True);
        Assert.That(mappings[0].GetLocalDateTime().EqualsValue(ambiguous), Is.True);
        Assert.That(mappings[1].GetLocalDateTime().EqualsValue(ambiguous), Is.True);
        Assert.That(mappings[0].GetInstant().EqualsValue(mappings[1].GetInstant()), Is.False);
        Assert.That(mappings[0].GetInstant().CompareTo(mappings[1].GetInstant()), Is.LessThan(0));
    }

    [Test]
    public void UtcZone_MapsAndResolvesNormalLocalTimeThroughZoneMapping()
    {
        TimeZoneRef zone = TimeZoneRef.Utc();
        LocalDateTime localDateTime = LocalDateTime.Create(2024, 2, 29, 12, 34, 56);
        OffsetDateTime[] mappings = zone.MapLocal(localDateTime);
        ZoneMapping mapping = ZoneMapping.CreateNormal(localDateTime, zone, mappings[0].GetInstant());
        OffsetDateTime resolved = ZoneResolver.Strict().Resolve(mapping);

        Assert.That(mappings.Length, Is.EqualTo(1));
        Assert.That(mapping.GetKind(), Is.EqualTo(ZoneMapping.KindNormal));
        Assert.That(mapping.GetZone(), Is.SameAs(zone));
        Assert.That(mapping.GetEarlyInstant().EqualsValue(mapping.GetLateInstant()), Is.True);
        Assert.That(mapping.GetGapDuration().EqualsValue(Duration.Zero()), Is.True);
        Assert.That(resolved.GetLocalDateTime().EqualsValue(localDateTime), Is.True);
        Assert.That(resolved.GetOffset().EqualsValue(Offset.Zero()), Is.True);
        Assert.That(resolved.GetInstant().ToIsoString(), Is.EqualTo("2024-02-29T12:34:56Z"));
    }

    [Test]
    public void LocalZone_NormalMappingMatchesTimeZoneInfoAndStrictResolver()
    {
        TimeZoneRef zone = TimeZoneRef.Local();
        LocalDateTime localDateTime = FindNormalLocalDateTime(TimeZoneInfo.Local);
        OffsetDateTime[] mappings = zone.MapLocal(localDateTime);
        ZoneMapping mapping = ZoneMapping.CreateNormal(localDateTime, zone, mappings[0].GetInstant());
        OffsetDateTime resolved = zone.ResolveLocal(localDateTime, ZoneResolver.Strict());
        OffsetDateTime mappingResolved = ZoneResolver.Strict().Resolve(mapping);
        TimeSpan expectedOffset = TimeZoneInfo.Local.GetUtcOffset(localDateTime.ToDateTimeUnspecified());

        Assert.That(mappings.Length, Is.EqualTo(1));
        Assert.That(mapping.GetKind(), Is.EqualTo(ZoneMapping.KindNormal));
        Assert.That(mapping.GetLocalDateTime().EqualsValue(localDateTime), Is.True);
        Assert.That(resolved.EqualsValue(mappingResolved), Is.True);
        Assert.That(resolved.GetLocalDateTime().EqualsValue(localDateTime), Is.True);
        Assert.That(resolved.GetOffset().ToTimeSpan(), Is.EqualTo(expectedOffset));
    }

    [Test]
    public void SystemDstZone_MapsAndResolvesInvalidAndAmbiguousLocalTimes()
    {
        SystemDstCase dstCase = FindSystemDstCase();
        TimeZoneRef zone = new TimeZoneRef(dstCase.zoneInfo);
        LocalDateTime invalid = LocalDateTime.FromDateTime(dstCase.invalidLocal);
        LocalDateTime ambiguous = LocalDateTime.FromDateTime(dstCase.ambiguousLocal);

        Assert.That(zone.IsInvalidLocal(invalid), Is.True);
        Assert.That(zone.MapLocal(invalid).Length, Is.EqualTo(0));
        Assert.Throws<InvalidOperationException>(() => zone.ResolveLocal(invalid, ZoneResolver.Strict()));

        OffsetDateTime shifted = zone.ResolveLocal(invalid, ZoneResolver.ShiftForward());
        OffsetDateTime compatibleInvalid = zone.ResolveLocal(invalid, ZoneResolver.Compatible());

        Assert.That(compatibleInvalid.EqualsValue(shifted), Is.True);
        Assert.That(shifted.GetLocalDateTime().CompareTo(invalid), Is.GreaterThan(0));
        Assert.That(zone.IsInvalidLocal(shifted.GetLocalDateTime()), Is.False);

        Assert.That(zone.IsAmbiguousLocal(ambiguous), Is.True);
        OffsetDateTime[] ambiguousMappings = zone.MapLocal(ambiguous);
        Assert.That(ambiguousMappings.Length, Is.EqualTo(2));
        Assert.Throws<InvalidOperationException>(() => zone.ResolveLocal(ambiguous, ZoneResolver.Strict()));

        OffsetDateTime earlier = zone.ResolveLocal(ambiguous, ZoneResolver.Earlier());
        OffsetDateTime later = zone.ResolveLocal(ambiguous, ZoneResolver.Later());
        OffsetDateTime compatibleAmbiguous = zone.ResolveLocal(ambiguous, ZoneResolver.Compatible());
        ZoneMapping mapping = ZoneMapping.CreateAmbiguous(ambiguous, zone, earlier.GetInstant(), later.GetInstant());

        Assert.That(earlier.GetLocalDateTime().EqualsValue(ambiguous), Is.True);
        Assert.That(later.GetLocalDateTime().EqualsValue(ambiguous), Is.True);
        Assert.That(earlier.GetInstant().CompareTo(later.GetInstant()), Is.LessThan(0));
        Assert.That(compatibleAmbiguous.EqualsValue(earlier), Is.True);
        Assert.That(ZoneResolver.Earlier().Resolve(mapping).EqualsValue(earlier), Is.True);
        Assert.That(ZoneResolver.Later().Resolve(mapping).EqualsValue(later), Is.True);
    }

    [Test]
    public void Transitions_RequireInstant()
    {
        TimeZoneRef zone = TimeZoneRef.Utc();

        ArgumentNullException nextException = Assert.Throws<ArgumentNullException>(() => zone.GetNextTransition(null!))!;
        ArgumentNullException previousException = Assert.Throws<ArgumentNullException>(() => zone.GetPreviousTransition(null!))!;

        Assert.That(nextException.ParamName, Is.EqualTo("instant"));
        Assert.That(previousException.ParamName, Is.EqualTo("instant"));
    }

    [Test]
    public void Transitions_ReturnNullForUtcZone()
    {
        TimeZoneRef zone = TimeZoneRef.Utc();
        Instant instant = Instant.Parse("2024-01-01T00:00:00Z");

        Assert.That(zone.GetNextTransition(instant), Is.Null);
        Assert.That(zone.GetPreviousTransition(instant), Is.Null);
    }

    [Test]
    public void GetNextTransition_FindsCustomDstTransitions()
    {
        TimeZoneRef zone = new TimeZoneRef(CreateTestDstZone());
        Instant beforeSpring = Instant.Parse("2024-01-01T00:00:00Z");
        Instant beforeFall = Instant.Parse("2024-06-01T00:00:00Z");

        Assert.That(zone.GetNextTransition(beforeSpring)!.ToIsoString(), Is.EqualTo("2024-03-10T07:00:00Z"));
        Assert.That(zone.GetNextTransition(beforeFall)!.ToIsoString(), Is.EqualTo("2024-11-03T06:00:00Z"));
    }

    [Test]
    public void GetPreviousTransition_FindsCustomDstTransitions()
    {
        TimeZoneRef zone = new TimeZoneRef(CreateTestDstZone());
        Instant afterSpring = Instant.Parse("2024-06-01T00:00:00Z");
        Instant afterFall = Instant.Parse("2024-12-01T00:00:00Z");

        Assert.That(zone.GetPreviousTransition(afterSpring)!.ToIsoString(), Is.EqualTo("2024-03-10T07:00:00Z"));
        Assert.That(zone.GetPreviousTransition(afterFall)!.ToIsoString(), Is.EqualTo("2024-11-03T06:00:00Z"));
    }

    [Test]
    public void Transitions_ReturnNullWhenNoTransitionIsFoundWithinSearchWindow()
    {
        TimeZoneRef zone = new TimeZoneRef(CreateTestDstZone());
        Instant afterRuleWindow = Instant.Parse("2025-01-01T00:00:00Z");

        Assert.That(zone.GetNextTransition(afterRuleWindow), Is.Null);
    }

    [Test]
    public void TransitionSearches_ClampSafelyAtSupportedInstantRangeEdges()
    {
        TimeZoneRef zone = TimeApi.Factory.CreateUtcTimeZone();
        Instant minimum = TimeApi.Factory.CreateInstantFromUtcTicks(DateTime.MinValue.Ticks);
        Instant nearMinimum = TimeApi.Factory.CreateInstantFromUtcTicks(DateTime.MinValue.AddMinutes(30).Ticks);
        Instant nearMaximum = TimeApi.Factory.CreateInstantFromUtcTicks(DateTime.MaxValue.AddMinutes(-30).Ticks);
        Instant maximum = TimeApi.Factory.CreateInstantFromUtcTicks(DateTime.MaxValue.Ticks);

        Assert.That(zone.GetPreviousTransition(minimum), Is.Null);
        Assert.That(zone.GetPreviousTransition(nearMinimum), Is.Null);
        Assert.That(zone.GetNextTransition(nearMaximum), Is.Null);
        Assert.That(zone.GetNextTransition(maximum), Is.Null);
    }

    [Test]
    public void GapMapping_ClampsValidTimeSearchesAtSupportedLocalRangeEdges()
    {
        TimeZoneRef minimumZone = new TimeZoneRef(CreateMinimumEdgeGapZone());
        TimeZoneRef maximumZone = new TimeZoneRef(CreateMaximumEdgeGapZone());
        LocalDateTime minimumGap = LocalDateTime.Create(1, 1, 1, 0, 45, 0);
        LocalDateTime maximumGap = LocalDateTime.Create(9999, 12, 31, 22, 0, 0);

        Assert.That(maximumZone.IsInvalidLocal(maximumGap), Is.True);
        Assert.That(minimumZone.IsInvalidLocal(minimumGap), Is.True);
        Assert.That(minimumZone.ResolveLocal(minimumGap, ZoneResolver.ShiftForward()).GetLocalDateTime().ToIsoString(), Is.EqualTo("0001-01-01T02:45:00"));
        Assert.Throws<ArgumentOutOfRangeException>(() => maximumZone.ResolveLocal(maximumGap, ZoneResolver.ShiftForward()));
    }

    [Test]
    public void NonHourTransitions_MapResolveAndSearchAtTickPrecision()
    {
        TimeZoneRef zone = new TimeZoneRef(CreateNonHourDstZone());
        LocalDateTime invalid = LocalDateTime.Create(2024, 3, 10, 2, 30, 0);
        LocalDateTime ambiguous = LocalDateTime.Create(2024, 11, 3, 2, 0, 0);

        OffsetDateTime shifted = zone.ResolveLocal(invalid, ZoneResolver.ShiftForward());
        OffsetDateTime earlier = zone.ResolveLocal(ambiguous, ZoneResolver.Earlier());
        OffsetDateTime later = zone.ResolveLocal(ambiguous, ZoneResolver.Later());

        Assert.That(zone.MapLocal(invalid), Is.Empty);
        Assert.That(shifted.GetLocalDateTime().ToIsoString(), Is.EqualTo("2024-03-10T03:00:00"));
        Assert.That(shifted.GetOffset().ToIsoString(), Is.EqualTo("+10:30"));
        Assert.That(earlier.GetInstant().ToIsoString(), Is.EqualTo("2024-11-02T15:30:00Z"));
        Assert.That(later.GetInstant().ToIsoString(), Is.EqualTo("2024-11-02T16:00:00Z"));
        Assert.That(earlier.GetOffset().ToIsoString(), Is.EqualTo("+10:30"));
        Assert.That(later.GetOffset().ToIsoString(), Is.EqualTo("+10:00"));

        Assert.That(zone.GetNextTransition(Instant.Parse("2024-03-01T00:00:00Z"))!.ToIsoString(), Is.EqualTo("2024-03-09T16:15:00Z"));
        Assert.That(zone.GetPreviousTransition(Instant.Parse("2024-04-01T00:00:00Z"))!.ToIsoString(), Is.EqualTo("2024-03-09T16:15:00Z"));
        Assert.That(zone.GetNextTransition(Instant.Parse("2024-10-01T00:00:00Z"))!.ToIsoString(), Is.EqualTo("2024-11-02T15:45:00Z"));
        Assert.That(zone.GetPreviousTransition(Instant.Parse("2024-12-01T00:00:00Z"))!.ToIsoString(), Is.EqualTo("2024-11-02T15:45:00Z"));
    }

    static TimeZoneInfo CreateTestDstZone()
    {
        TimeZoneInfo.TransitionTime daylightStart = TimeZoneInfo.TransitionTime.CreateFixedDateRule(
            new DateTime(1, 1, 1, 2, 0, 0),
            3,
            10);
        TimeZoneInfo.TransitionTime daylightEnd = TimeZoneInfo.TransitionTime.CreateFixedDateRule(
            new DateTime(1, 1, 1, 2, 0, 0),
            11,
            3);
        TimeZoneInfo.AdjustmentRule rule = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(
            new DateTime(2024, 1, 1),
            new DateTime(2024, 12, 31),
            TimeSpan.FromHours(1),
            daylightStart,
            daylightEnd);

        return TimeZoneInfo.CreateCustomTimeZone(
            "time-library-test-dst",
            TimeSpan.FromHours(-5),
            "Time Library Test DST",
            "TLT",
            "TLT Daylight",
            new TimeZoneInfo.AdjustmentRule[] { rule });
    }

    static TimeZoneInfo CreateNonHourDstZone()
    {
        TimeZoneInfo.TransitionTime daylightStart = TimeZoneInfo.TransitionTime.CreateFixedDateRule(
            new DateTime(1, 1, 1, 2, 15, 0),
            3,
            10);
        TimeZoneInfo.TransitionTime daylightEnd = TimeZoneInfo.TransitionTime.CreateFixedDateRule(
            new DateTime(1, 1, 1, 2, 15, 0),
            11,
            3);
        TimeZoneInfo.AdjustmentRule rule = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(
            new DateTime(2024, 1, 1),
            new DateTime(2024, 12, 31),
            TimeSpan.FromMinutes(30),
            daylightStart,
            daylightEnd);

        return TimeZoneInfo.CreateCustomTimeZone(
            "time-library-test-non-hour-dst",
            TimeSpan.FromHours(10),
            "Time Library Non-Hour DST",
            "TLNH Standard",
            "TLNH Daylight",
            new TimeZoneInfo.AdjustmentRule[] { rule });
    }

    static TimeZoneInfo CreateMinimumEdgeGapZone()
    {
        TimeZoneInfo.TransitionTime daylightStart = TimeZoneInfo.TransitionTime.CreateFixedDateRule(
            new DateTime(1, 1, 1, 0, 30, 0),
            1,
            1);
        TimeZoneInfo.TransitionTime daylightEnd = TimeZoneInfo.TransitionTime.CreateFixedDateRule(
            new DateTime(1, 1, 1, 0, 0, 0),
            7,
            1);
        TimeZoneInfo.AdjustmentRule rule = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(
            new DateTime(1, 1, 1),
            new DateTime(1, 12, 31),
            TimeSpan.FromHours(2),
            daylightStart,
            daylightEnd);

        return TimeZoneInfo.CreateCustomTimeZone(
            "time-library-test-minimum-edge-gap",
            TimeSpan.Zero,
            "Time Library Minimum Edge Gap",
            "TL Minimum Standard",
            "TL Minimum Daylight",
            new TimeZoneInfo.AdjustmentRule[] { rule });
    }

    static TimeZoneInfo CreateMaximumEdgeGapZone()
    {
        TimeZoneInfo.TransitionTime daylightStart = TimeZoneInfo.TransitionTime.CreateFixedDateRule(
            new DateTime(1, 1, 1, 21, 30, 0),
            12,
            31);
        TimeZoneInfo.TransitionTime daylightEnd = TimeZoneInfo.TransitionTime.CreateFixedDateRule(
            new DateTime(1, 1, 1, 0, 0, 0),
            7,
            1);
        TimeZoneInfo.AdjustmentRule rule = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(
            new DateTime(9999, 1, 1),
            new DateTime(9999, 12, 31),
            TimeSpan.FromHours(2),
            daylightStart,
            daylightEnd);

        return TimeZoneInfo.CreateCustomTimeZone(
            "time-library-test-maximum-edge-gap",
            TimeSpan.Zero,
            "Time Library Maximum Edge Gap",
            "TL Maximum Standard",
            "TL Maximum Daylight",
            new TimeZoneInfo.AdjustmentRule[] { rule });
    }

    static LocalDateTime FindNormalLocalDateTime(TimeZoneInfo zoneInfo)
    {
        DateTime candidate = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Unspecified);
        int daysChecked = 0;
        while (daysChecked < 366)
        {
            if (!zoneInfo.IsInvalidTime(candidate) && !zoneInfo.IsAmbiguousTime(candidate))
            {
                return LocalDateTime.FromDateTime(candidate);
            }

            candidate = candidate.AddDays(1);
            daysChecked = daysChecked + 1;
        }

        Assert.Ignore("No normal local time was available for the local system zone.");
        return LocalDateTime.FromDateTime(DateTime.MinValue);
    }

    static SystemDstCase FindSystemDstCase()
    {
        foreach (TimeZoneInfo zoneInfo in TimeZoneInfo.GetSystemTimeZones())
        {
            if (zoneInfo.SupportsDaylightSavingTime)
            {
                DateTime invalid = FindTransitionLocalTime(zoneInfo, true);
                DateTime ambiguous = FindTransitionLocalTime(zoneInfo, false);
                if (invalid != DateTime.MinValue && ambiguous != DateTime.MinValue)
                {
                    return new SystemDstCase(zoneInfo, invalid, ambiguous);
                }
            }
        }

        Assert.Ignore("No DST-observing system zone with invalid and ambiguous local times was available.");
        return new SystemDstCase(TimeZoneInfo.Utc, DateTime.MinValue, DateTime.MinValue);
    }

    static DateTime FindTransitionLocalTime(TimeZoneInfo zoneInfo, bool invalid)
    {
        TimeZoneInfo.AdjustmentRule[] rules = zoneInfo.GetAdjustmentRules();
        foreach (TimeZoneInfo.AdjustmentRule rule in rules)
        {
            int startYear = rule.DateStart.Year;
            if (startYear < 2020)
            {
                startYear = 2020;
            }

            int endYear = rule.DateEnd.Year;
            if (endYear > 2030)
            {
                endYear = 2030;
            }

            int year = startYear;
            while (year <= endYear)
            {
                DateTime found = ProbeTransition(zoneInfo, GetTransitionDateTime(year, rule.DaylightTransitionStart), invalid);
                if (found != DateTime.MinValue)
                {
                    return found;
                }

                found = ProbeTransition(zoneInfo, GetTransitionDateTime(year, rule.DaylightTransitionEnd), invalid);
                if (found != DateTime.MinValue)
                {
                    return found;
                }

                year = year + 1;
            }
        }

        return DateTime.MinValue;
    }

    static DateTime ProbeTransition(TimeZoneInfo zoneInfo, DateTime transition, bool invalid)
    {
        DateTime candidate = transition.AddHours(-3);
        int probes = 0;
        while (probes <= 24)
        {
            if (invalid && zoneInfo.IsInvalidTime(candidate))
            {
                return candidate;
            }

            if (!invalid && zoneInfo.IsAmbiguousTime(candidate))
            {
                return candidate;
            }

            candidate = candidate.AddMinutes(15);
            probes = probes + 1;
        }

        return DateTime.MinValue;
    }

    static DateTime GetTransitionDateTime(int year, TimeZoneInfo.TransitionTime transition)
    {
        int month = transition.Month;
        int day = transition.Day;

        if (!transition.IsFixedDateRule)
        {
            DateTime firstOfMonth = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Unspecified);
            int daysToTransitionDay = ((int)transition.DayOfWeek - (int)firstOfMonth.DayOfWeek + 7) % 7;
            day = 1 + daysToTransitionDay + ((transition.Week - 1) * 7);
            int daysInMonth = DateTime.DaysInMonth(year, month);
            if (day > daysInMonth)
            {
                day = day - 7;
            }
        }

        return new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Unspecified).Add(transition.TimeOfDay.TimeOfDay);
    }

    static bool ContainsOffset(Offset[] offsets, int totalSeconds)
    {
        foreach (Offset offset in offsets)
        {
            if (offset.GetTotalSeconds() == totalSeconds)
            {
                return true;
            }
        }

        return false;
    }

    static bool ContainsMappingOffset(OffsetDateTime[] mappings, int totalSeconds)
    {
        foreach (OffsetDateTime mapping in mappings)
        {
            if (mapping.GetOffset().GetTotalSeconds() == totalSeconds)
            {
                return true;
            }
        }

        return false;
    }

    class SystemDstCase
    {
        public TimeZoneInfo zoneInfo;
        public DateTime invalidLocal;
        public DateTime ambiguousLocal;

        public SystemDstCase(TimeZoneInfo zoneInfo, DateTime invalidLocal, DateTime ambiguousLocal)
        {
            this.zoneInfo = zoneInfo;
            this.invalidLocal = invalidLocal;
            this.ambiguousLocal = ambiguousLocal;
        }
    }
}
