using System;

namespace htmlgui;

internal class CapabilityNegotiationResult
{
    internal bool Success;
    internal string Code = string.Empty;
    internal UiTransportCapabilities Capabilities = new UiTransportCapabilities();
}

internal class BridgeHandshakePayloadResult
{
    internal bool Success;
    internal string Code = string.Empty;
    internal string Nonce = string.Empty;
    internal UiTransportCapabilities Capabilities = new UiTransportCapabilities();
}

internal class BridgeCapabilityParseResult
{
    internal bool Success;
    internal string Code = string.Empty;
    internal UiCapability Capability = new UiCapability();
}

internal class CapabilityNegotiator
{
    internal const string CapabilityNameMember = "name";
    internal const string CapabilitySupportedMember = "supported";
    internal const string CapabilityVersionMember = "version";
    internal const string SupportsBinaryMember = "supportsBinary";
    internal const string SupportsChunkingMember = "supportsChunking";
    internal const int MaximumCapabilityCount = 256;
    internal const int MaximumCapabilityTextLength = 128;

    internal CapabilityNegotiationResult NegotiateBridgeCapabilities(UiTransportCapabilities? local, UiTransportCapabilities? remote)
    {
        CapabilityNegotiationResult localResult = ValidateBridgeCapabilities(local);
        if (!localResult.Success)
        {
            return localResult;
        }

        CapabilityNegotiationResult remoteResult = ValidateBridgeCapabilities(remote);
        if (!remoteResult.Success)
        {
            return remoteResult;
        }

        UiTransportCapabilities localCapabilities = localResult.Capabilities;
        UiTransportCapabilities remoteCapabilities = remoteResult.Capabilities;
        UiCapability[] negotiatedItems = new UiCapability[localCapabilities.Capabilities.Length];
        int localIndex = 0;
        while (localIndex < localCapabilities.Capabilities.Length)
        {
            UiCapability localCapability = localCapabilities.Capabilities[localIndex];
            UiCapability? remoteCapability = FindRemoteCapability(remoteCapabilities.Capabilities, localCapability.Name);
            bool supported = false;
            string? version = localCapability.Version;
            if (remoteCapability != null)
            {
                supported = localCapability.IsSupported && remoteCapability.IsSupported && AreCapabilityVersionsCompatible(localCapability.Version, remoteCapability.Version);
                if (version == null)
                {
                    version = remoteCapability.Version;
                }
            }

            negotiatedItems[localIndex] = new UiCapability
            {
                Name = localCapability.Name,
                IsSupported = supported,
                Version = version
            };
            localIndex++;
        }

        return new CapabilityNegotiationResult
        {
            Success = true,
            Capabilities = new UiTransportCapabilities
            {
                State = UiTransportState.Ready,
                Version = Math.Min(localCapabilities.Version, remoteCapabilities.Version),
                Capabilities = negotiatedItems,
                SupportsBinary = localCapabilities.SupportsBinary && remoteCapabilities.SupportsBinary,
                SupportsChunking = localCapabilities.SupportsChunking && remoteCapabilities.SupportsChunking
            }
        };
    }

    internal CapabilityNegotiationResult ValidateBridgeCapabilities(UiTransportCapabilities? source)
    {
        if (source == null)
        {
            return CreateCapabilityFailure("bridge.null-capabilities");
        }

        if (source.Version < BridgeProtocolVersions.MinimumSupported || source.Version > BridgeProtocolVersions.Current)
        {
            return CreateCapabilityFailure("bridge.unsupported-capability-version");
        }

        UiCapability[]? sourceItems = source.Capabilities;
        if (sourceItems == null || sourceItems.Length > MaximumCapabilityCount)
        {
            return CreateCapabilityFailure("bridge.invalid-capabilities");
        }

        UiCapability[] copiedItems = new UiCapability[sourceItems.Length];
        int index = 0;
        while (index < sourceItems.Length)
        {
            UiCapability capability = sourceItems[index];
            if (capability == null || string.IsNullOrWhiteSpace(capability.Name) || capability.Name.Length > MaximumCapabilityTextLength)
            {
                return CreateCapabilityFailure("bridge.invalid-capability");
            }

            if (capability.Version != null && (string.IsNullOrWhiteSpace(capability.Version) || capability.Version.Length > MaximumCapabilityTextLength))
            {
                return CreateCapabilityFailure("bridge.invalid-capability");
            }

            int previousIndex = 0;
            while (previousIndex < index)
            {
                if (ValueComparer.TextEquals(copiedItems[previousIndex].Name, capability.Name))
                {
                    return CreateCapabilityFailure("bridge.duplicate-capability");
                }

                previousIndex++;
            }

            copiedItems[index] = new UiCapability
            {
                Name = capability.Name,
                IsSupported = capability.IsSupported,
                Version = capability.Version
            };
            index++;
        }

        SortCapabilities(copiedItems);
        return new CapabilityNegotiationResult
        {
            Success = true,
            Capabilities = new UiTransportCapabilities
            {
                State = source.State,
                Version = source.Version,
                Capabilities = copiedItems,
                SupportsBinary = source.SupportsBinary,
                SupportsChunking = source.SupportsChunking
            }
        };
    }

    internal UiValue CreateBridgeHandshakePayload(string nonce, UiTransportCapabilities capabilities)
    {
        UiValue[] capabilityValues = new UiValue[capabilities.Capabilities.Length];
        int index = 0;
        while (index < capabilities.Capabilities.Length)
        {
            UiCapability capability = capabilities.Capabilities[index];
            UiValue versionValue = new UiValue();
            if (capability.Version != null)
            {
                versionValue.Kind = UiValueKind.String;
                versionValue.StringValue = capability.Version;
            }

            capabilityValues[index] = new UiValue
            {
                Kind = UiValueKind.Object,
                ObjectValue = new UiValueEntry[]
                {
                    new UiValueEntry { Key = CapabilityNameMember, Value = new UiValue { Kind = UiValueKind.String, StringValue = capability.Name } },
                    new UiValueEntry { Key = CapabilitySupportedMember, Value = new UiValue { Kind = UiValueKind.Boolean, BooleanValue = capability.IsSupported } },
                    new UiValueEntry { Key = CapabilityVersionMember, Value = versionValue }
                }
            };
            index++;
        }

        return new UiValue
        {
            Kind = UiValueKind.Object,
            ObjectValue = new UiValueEntry[]
            {
                new UiValueEntry { Key = BridgeProtocolNames.Nonce, Value = new UiValue { Kind = UiValueKind.String, StringValue = nonce } },
                new UiValueEntry { Key = BridgeProtocolNames.Version, Value = new UiValue { Kind = UiValueKind.Integer, IntegerValue = capabilities.Version } },
                new UiValueEntry { Key = BridgeProtocolNames.Capabilities, Value = new UiValue { Kind = UiValueKind.Array, ArrayValue = capabilityValues } },
                new UiValueEntry { Key = SupportsBinaryMember, Value = new UiValue { Kind = UiValueKind.Boolean, BooleanValue = capabilities.SupportsBinary } },
                new UiValueEntry { Key = SupportsChunkingMember, Value = new UiValue { Kind = UiValueKind.Boolean, BooleanValue = capabilities.SupportsChunking } }
            }
        };
    }

    internal BridgeHandshakePayloadResult ParseBridgeHandshakePayload(UiValue? payload)
    {
        if (payload == null || payload.Kind != UiValueKind.Object || payload.ObjectValue == null)
        {
            return CreateHandshakeFailure("bridge.invalid-handshake-payload");
        }

        UiValueEntry[] entries = payload.ObjectValue;
        string? nonce = null;
        int version = 0;
        UiCapability[]? capabilities = null;
        bool supportsBinary = false;
        bool supportsChunking = false;
        bool hasVersion = false;
        bool hasCapabilities = false;
        bool hasSupportsBinary = false;
        bool hasSupportsChunking = false;
        int index = 0;
        while (index < entries.Length)
        {
            UiValueEntry entry = entries[index];
            if (entry == null || entry.Value == null)
            {
                return CreateHandshakeFailure("bridge.invalid-handshake-payload");
            }

            if (ValueComparer.TextEquals(entry.Key, BridgeProtocolNames.Nonce))
            {
                if (nonce != null || entry.Value.Kind != UiValueKind.String || string.IsNullOrWhiteSpace(entry.Value.StringValue))
                {
                    return CreateHandshakeFailure("bridge.invalid-handshake-nonce");
                }

                nonce = entry.Value.StringValue;
            }
            else if (ValueComparer.TextEquals(entry.Key, BridgeProtocolNames.Version))
            {
                if (hasVersion || entry.Value.Kind != UiValueKind.Integer || entry.Value.IntegerValue < int.MinValue || entry.Value.IntegerValue > int.MaxValue)
                {
                    return CreateHandshakeFailure("bridge.invalid-handshake-version");
                }

                version = (int)entry.Value.IntegerValue;
                hasVersion = true;
            }
            else if (ValueComparer.TextEquals(entry.Key, BridgeProtocolNames.Capabilities))
            {
                if (hasCapabilities || entry.Value.Kind != UiValueKind.Array || entry.Value.ArrayValue == null)
                {
                    return CreateHandshakeFailure("bridge.invalid-handshake-capabilities");
                }

                BridgeHandshakePayloadResult capabilityResult = ParseHandshakeCapabilities(entry.Value.ArrayValue);
                if (!capabilityResult.Success)
                {
                    return capabilityResult;
                }

                capabilities = capabilityResult.Capabilities.Capabilities;
                hasCapabilities = true;
            }
            else if (ValueComparer.TextEquals(entry.Key, SupportsBinaryMember))
            {
                if (hasSupportsBinary || entry.Value.Kind != UiValueKind.Boolean)
                {
                    return CreateHandshakeFailure("bridge.invalid-handshake-capabilities");
                }

                supportsBinary = entry.Value.BooleanValue;
                hasSupportsBinary = true;
            }
            else if (ValueComparer.TextEquals(entry.Key, SupportsChunkingMember))
            {
                if (hasSupportsChunking || entry.Value.Kind != UiValueKind.Boolean)
                {
                    return CreateHandshakeFailure("bridge.invalid-handshake-capabilities");
                }

                supportsChunking = entry.Value.BooleanValue;
                hasSupportsChunking = true;
            }
            else
            {
                return CreateHandshakeFailure("bridge.unknown-handshake-member");
            }

            index++;
        }

        if (nonce == null || !hasVersion || !hasCapabilities || capabilities == null || !hasSupportsBinary || !hasSupportsChunking)
        {
            return CreateHandshakeFailure("bridge.incomplete-handshake-payload");
        }

        UiTransportCapabilities parsedCapabilities = new UiTransportCapabilities
        {
            State = UiTransportState.Handshaking,
            Version = version,
            Capabilities = capabilities,
            SupportsBinary = supportsBinary,
            SupportsChunking = supportsChunking
        };
        CapabilityNegotiationResult validation = ValidateBridgeCapabilities(parsedCapabilities);
        if (!validation.Success)
        {
            return CreateHandshakeFailure(validation.Code);
        }

        return new BridgeHandshakePayloadResult
        {
            Success = true,
            Nonce = nonce,
            Capabilities = validation.Capabilities
        };
    }

    internal BridgeHandshakePayloadResult ParseHandshakeCapabilities(UiValue[] values)
    {
        if (values.Length > MaximumCapabilityCount)
        {
            return CreateHandshakeFailure("bridge.invalid-handshake-capabilities");
        }

        UiCapability[] capabilities = new UiCapability[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            BridgeCapabilityParseResult parsed = ParseHandshakeCapability(values[index]);
            if (!parsed.Success)
            {
                return CreateHandshakeFailure(parsed.Code);
            }

            capabilities[index] = parsed.Capability;
            index++;
        }

        return new BridgeHandshakePayloadResult
        {
            Success = true,
            Capabilities = new UiTransportCapabilities { Capabilities = capabilities }
        };
    }

    internal BridgeCapabilityParseResult ParseHandshakeCapability(UiValue value)
    {
        if (value == null || value.Kind != UiValueKind.Object || value.ObjectValue == null)
        {
            return CreateCapabilityParseFailure("bridge.invalid-handshake-capability");
        }

        UiValueEntry[] entries = value.ObjectValue;
        string? name = null;
        string? version = null;
        bool supported = false;
        bool hasSupported = false;
        bool hasVersion = false;
        int index = 0;
        while (index < entries.Length)
        {
            UiValueEntry entry = entries[index];
            if (entry == null || entry.Value == null)
            {
                return CreateCapabilityParseFailure("bridge.invalid-handshake-capability");
            }

            if (ValueComparer.TextEquals(entry.Key, CapabilityNameMember))
            {
                if (name != null || entry.Value.Kind != UiValueKind.String || string.IsNullOrWhiteSpace(entry.Value.StringValue))
                {
                    return CreateCapabilityParseFailure("bridge.invalid-handshake-capability");
                }

                name = entry.Value.StringValue;
            }
            else if (ValueComparer.TextEquals(entry.Key, CapabilitySupportedMember))
            {
                if (hasSupported || entry.Value.Kind != UiValueKind.Boolean)
                {
                    return CreateCapabilityParseFailure("bridge.invalid-handshake-capability");
                }

                supported = entry.Value.BooleanValue;
                hasSupported = true;
            }
            else if (ValueComparer.TextEquals(entry.Key, CapabilityVersionMember))
            {
                if (hasVersion || (entry.Value.Kind != UiValueKind.Null && entry.Value.Kind != UiValueKind.String))
                {
                    return CreateCapabilityParseFailure("bridge.invalid-handshake-capability");
                }

                if (entry.Value.Kind == UiValueKind.String)
                {
                    if (string.IsNullOrWhiteSpace(entry.Value.StringValue))
                    {
                        return CreateCapabilityParseFailure("bridge.invalid-handshake-capability");
                    }

                    version = entry.Value.StringValue;
                }

                hasVersion = true;
            }
            else
            {
                return CreateCapabilityParseFailure("bridge.invalid-handshake-capability");
            }

            index++;
        }

        if (name == null || !hasSupported || !hasVersion)
        {
            return CreateCapabilityParseFailure("bridge.invalid-handshake-capability");
        }

        return new BridgeCapabilityParseResult
        {
            Success = true,
            Capability = new UiCapability { Name = name, IsSupported = supported, Version = version }
        };
    }

    internal UiCapability? FindRemoteCapability(UiCapability[] capabilities, string name)
    {
        int index = 0;
        while (index < capabilities.Length)
        {
            if (ValueComparer.TextEquals(capabilities[index].Name, name))
            {
                return capabilities[index];
            }

            index++;
        }

        return null;
    }

    internal bool AreCapabilityVersionsCompatible(string? localVersion, string? remoteVersion)
    {
        return localVersion == null || remoteVersion == null || ValueComparer.TextEquals(localVersion, remoteVersion);
    }

    internal void SortCapabilities(UiCapability[] capabilities)
    {
        int index = 1;
        while (index < capabilities.Length)
        {
            UiCapability item = capabilities[index];
            int insertionIndex = index;
            while (insertionIndex > 0 && string.CompareOrdinal(capabilities[insertionIndex - 1].Name, item.Name) > 0)
            {
                capabilities[insertionIndex] = capabilities[insertionIndex - 1];
                insertionIndex--;
            }

            capabilities[insertionIndex] = item;
            index++;
        }
    }

    internal CapabilityNegotiationResult CreateCapabilityFailure(string code)
    {
        return new CapabilityNegotiationResult { Code = code };
    }

    internal BridgeHandshakePayloadResult CreateHandshakeFailure(string code)
    {
        return new BridgeHandshakePayloadResult { Code = code };
    }

    internal BridgeCapabilityParseResult CreateCapabilityParseFailure(string code)
    {
        return new BridgeCapabilityParseResult { Code = code };
    }
}
