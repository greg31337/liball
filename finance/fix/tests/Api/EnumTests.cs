using System;
using fix;

namespace fix.Tests;

[TestFixture]
public class EnumTests
{
    [Test]
    public void EnumIntegerValuesAreStable()
    {
        Assert.Multiple(() =>
        {
            AssertValues<FixFieldType>(0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29);
            AssertValues<FixMemberKind>(0, 1, 2, 3);
            AssertValues<FixPresence>(0, 1, 2, 3, 4, 5);
            AssertValues<DecodeStatus>(0, 1, 2, 3);
            AssertValues<EncodeStatus>(0, 1, 2, 3);
            AssertValues<FrameStatus>(0, 1, 2, 3, 4);
            AssertValues<ValidationSeverity>(0, 1, 2, 3);
            AssertValues<ValidationLayer>(0, 1, 2, 4, 8, 16, 32, 64, 128, 256, 511);
            AssertValues<SessionRole>(0, 1);
            AssertValues<SessionState>(0, 1, 2, 3, 4, 5, 6, 7, 8, 9);
            AssertValues<SessionEventKind>(0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15);
            AssertValues<SessionResetMode>(0, 1, 2, 4, 8);
            AssertValues<MessageDisposition>(0, 1, 2, 3);
            AssertValues<LogDirection>(0, 1, 2);
            AssertValues<TransportState>(0, 1, 2, 3, 4, 5);
            AssertValues<OrchestraVersion>(0, 10, 11);
            AssertValues<OrchestraRuleKind>(0, 1, 2, 3);
            AssertValues<GenerationTarget>(0, 1);
            AssertValues<BinaryEncodingType>(0, 1, 2, 3, 4, 5, 6);
        });
    }

    [Test]
    public void CapabilityProviderRoundTripsEveryEnumFamily()
    {
        FixApiCapabilities expected = new FixApiCapabilities
        {
            FieldTypes = [FixFieldType.UtcTimestamp],
            MemberKinds = [FixMemberKind.Group],
            PresenceModes = [FixPresence.Conditional],
            DecodeStatuses = [DecodeStatus.Incomplete],
            EncodeStatuses = [EncodeStatus.DestinationTooSmall],
            FrameStatuses = [FrameStatus.Recovered],
            ValidationSeverities = [ValidationSeverity.Fatal],
            ValidationLayers = [ValidationLayer.All],
            SessionRoles = [SessionRole.Acceptor],
            SessionStates = [SessionState.Active],
            SessionEventKinds = [SessionEventKind.RecoveryCompleted],
            SessionResetModes = [SessionResetMode.OnSessionBoundary],
            MessageDispositions = [MessageDisposition.Reject],
            LogDirections = [LogDirection.Outgoing],
            TransportStates = [TransportState.Connected],
            OrchestraVersions = [OrchestraVersion.Version11],
            OrchestraRuleKinds = [OrchestraRuleKind.Assignment],
            GenerationTargets = [GenerationTarget.SbeSchema],
            BinaryEncodingTypes = [BinaryEncodingType.Sbe]
        };
        IFixApiCapabilitiesProvider provider = new StubCapabilitiesProvider(expected);

        FixApiCapabilities actual = provider.GetFixApiCapabilities();

        Assert.Multiple(() =>
        {
            Assert.That(actual.FieldTypes, Is.EqualTo(expected.FieldTypes));
            Assert.That(actual.MemberKinds, Is.EqualTo(expected.MemberKinds));
            Assert.That(actual.PresenceModes, Is.EqualTo(expected.PresenceModes));
            Assert.That(actual.DecodeStatuses, Is.EqualTo(expected.DecodeStatuses));
            Assert.That(actual.EncodeStatuses, Is.EqualTo(expected.EncodeStatuses));
            Assert.That(actual.FrameStatuses, Is.EqualTo(expected.FrameStatuses));
            Assert.That(actual.ValidationSeverities, Is.EqualTo(expected.ValidationSeverities));
            Assert.That(actual.ValidationLayers, Is.EqualTo(expected.ValidationLayers));
            Assert.That(actual.SessionRoles, Is.EqualTo(expected.SessionRoles));
            Assert.That(actual.SessionStates, Is.EqualTo(expected.SessionStates));
            Assert.That(actual.SessionEventKinds, Is.EqualTo(expected.SessionEventKinds));
            Assert.That(actual.SessionResetModes, Is.EqualTo(expected.SessionResetModes));
            Assert.That(actual.MessageDispositions, Is.EqualTo(expected.MessageDispositions));
            Assert.That(actual.LogDirections, Is.EqualTo(expected.LogDirections));
            Assert.That(actual.TransportStates, Is.EqualTo(expected.TransportStates));
            Assert.That(actual.OrchestraVersions, Is.EqualTo(expected.OrchestraVersions));
            Assert.That(actual.OrchestraRuleKinds, Is.EqualTo(expected.OrchestraRuleKinds));
            Assert.That(actual.GenerationTargets, Is.EqualTo(expected.GenerationTargets));
            Assert.That(actual.BinaryEncodingTypes, Is.EqualTo(expected.BinaryEncodingTypes));
        });
    }

    private static void AssertValues<TEnum>(params int[] expected) where TEnum : Enum
    {
        Array values = Enum.GetValues(typeof(TEnum));
        int[] actual = new int[values.Length];
        for (int index = 0; index < values.Length; index++)
        {
            actual[index] = Convert.ToInt32(values.GetValue(index));
        }

        Assert.That(actual, Is.EqualTo(expected), typeof(TEnum).Name);
    }

    private sealed class StubCapabilitiesProvider : IFixApiCapabilitiesProvider
    {
        private readonly FixApiCapabilities capabilities;

        public StubCapabilitiesProvider(FixApiCapabilities capabilities)
        {
            this.capabilities = capabilities;
        }

        public FixApiCapabilities GetFixApiCapabilities()
        {
            return capabilities;
        }
    }
}
