using System;
using System.Collections.Frozen;

namespace sdp;

internal static class SdpRegistryData
{
    internal static readonly FrozenSet<string> MediaTypes = SdpRegistryLookup.CreateSdpNameSet("sdp-parameters-1", StringComparer.OrdinalIgnoreCase);

    internal static readonly FrozenSet<string> TransportProtocols = SdpRegistryLookup.CreateSdpNameSet("sdp-parameters-2", StringComparer.Ordinal);

    internal static readonly FrozenSet<string> BandwidthTypes = SdpRegistryLookup.CreateSdpNameSet("sdp-parameters-3", StringComparer.Ordinal);

    internal static readonly FrozenSet<string> NetworkTypes = SdpRegistryLookup.CreateSdpNameSet("sdp-parameters-4", StringComparer.Ordinal);

    internal static readonly FrozenSet<string> AddressTypes = SdpRegistryLookup.CreateSdpNameSet("sdp-parameters-5", StringComparer.Ordinal);

    internal static readonly FrozenSet<string> EncryptionMethods = SdpRegistryLookup.CreateSdpNameSet("sdp-parameters-6", StringComparer.Ordinal);

    internal static readonly FrozenSet<string> AttributeNames = SdpRegistryLookup.CreateSdpNameSet("sdp-att-field", StringComparer.OrdinalIgnoreCase);

    internal static readonly FrozenSet<string> GroupSemantics = SdpRegistryLookup.CreateSdpNameSet("sdp-parameters-13", StringComparer.Ordinal);

    internal static readonly FrozenSet<int> AssignedPayloadTypes = SdpRegistryLookup.CreateAssignedPayloadTypeSet();

    internal static readonly FrozenSet<string> RtpEncodings = SdpRegistryLookup.CreateRtpEncodingSet();

    internal static readonly FrozenSet<string> HeaderExtensionUris = SdpRegistryLookup.CreateHeaderExtensionSet();
}
