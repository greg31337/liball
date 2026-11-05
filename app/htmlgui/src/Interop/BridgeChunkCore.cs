using System;
using System.Collections.Generic;
using System.Text;

namespace htmlgui;

internal class BridgeChunkOperationResult
{
    internal bool Success;
    internal bool Applied;
    internal bool Complete;
    internal bool Duplicate;
    internal bool Backpressured;
    internal bool Expired;
    internal string Code = string.Empty;
    internal string ChunkId = string.Empty;
    internal string Checksum = string.Empty;
    internal byte[]? Data;
    internal UiBridgeEnvelope[] Envelopes = Array.Empty<UiBridgeEnvelope>();
    internal string[] ExpiredChunkIds = Array.Empty<string>();
}

internal class BridgeChunkPayloadResult
{
    internal bool Success;
    internal string Code = string.Empty;
    internal string ChunkId = string.Empty;
    internal int ChunkIndex;
    internal int ChunkCount;
    internal int Length;
    internal string Checksum = string.Empty;
    internal byte[] Data = Array.Empty<byte>();
}

internal class BridgeChunkAssembly
{
    internal string ChunkId = string.Empty;
    internal int ChunkCount;
    internal int Length;
    internal string Checksum = string.Empty;
    internal long ExpiresAtMilliseconds;
    internal byte[][] Chunks = Array.Empty<byte[]>();
    internal bool[] Received = Array.Empty<bool>();
    internal int ReceivedCount;
    internal int ReceivedBytes;
}

internal class BridgeChunkHistoryEntry
{
    internal string ChunkId = string.Empty;
    internal int Length;
    internal string Checksum = string.Empty;
    internal bool Expired;
}

internal class BridgeChunkCore
{
    internal const string ChunkLengthMember = "length";
    internal const int HardMaximumChunkCount = 4096;
    internal const int HardMaximumChunkBytes = 49152;
    internal const int HardMaximumAssemblyCount = 4096;
    internal const int HardMaximumAssemblyBytes = 67108864;

    internal BridgeSession Session;
    internal BridgeBinaryCodec BinaryCodec;
    internal List<BridgeChunkAssembly> Assemblies;
    internal List<BridgeChunkHistoryEntry> History;
    internal int MaximumChunkBytes;
    internal int MaximumAssemblyBytes;
    internal int MaximumAssemblyCount;
    internal int ExpiryMilliseconds;
    internal int BufferedBytes;

    internal BridgeChunkCore(BridgeSession session, int maximumChunkBytes, int maximumAssemblyBytes, int maximumAssemblyCount, int expiryMilliseconds)
    {
        Session = session;
        BinaryCodec = new BridgeBinaryCodec();
        Assemblies = new List<BridgeChunkAssembly>();
        History = new List<BridgeChunkHistoryEntry>();
        MaximumChunkBytes = maximumChunkBytes;
        MaximumAssemblyBytes = maximumAssemblyBytes;
        MaximumAssemblyCount = maximumAssemblyCount;
        ExpiryMilliseconds = expiryMilliseconds;
    }

    internal BridgeChunkOperationResult SplitBridgeMessage(string? chunkId, byte[]? data, long nowMilliseconds)
    {
        BridgeChunkOperationResult readiness = ValidateChunkReadiness(nowMilliseconds);
        if (!readiness.Success)
        {
            return readiness;
        }

        if (!IsChunkIdentifierValid(chunkId) || data == null)
        {
            return CreateChunkFailure("bridge.invalid-chunk-input");
        }

        if (data.Length > MaximumAssemblyBytes)
        {
            return CreateChunkFailure("bridge.chunk-message-too-large");
        }

        int chunkCount = CalculateChunkCount(data.Length);
        if (chunkCount > HardMaximumChunkCount)
        {
            return CreateChunkFailure("bridge.too-many-chunks");
        }

        if (!CanCreateChunkEnvelopes(chunkCount))
        {
            Session.State = UiTransportState.Faulted;
            return CreateChunkFailure("bridge.sequence-exhausted");
        }

        string checkedChunkId = chunkId ?? string.Empty;
        string checksum = StableHash.HashBytes(data);
        UiValue[] payloads = new UiValue[chunkCount];
        int payloadIndex = 0;
        while (payloadIndex < chunkCount)
        {
            int offset = payloadIndex * MaximumChunkBytes;
            int remaining = data.Length - offset;
            int chunkLength = Math.Min(MaximumChunkBytes, Math.Max(0, remaining));
            byte[] chunkData = new byte[chunkLength];
            if (chunkLength > 0)
            {
                Array.Copy(data, offset, chunkData, 0, chunkLength);
            }

            BridgeBinaryResult encoded = BinaryCodec.EncodeBridgeBinary(chunkData, MaximumChunkBytes);
            if (!encoded.Success)
            {
                return CreateChunkFailure(encoded.Code);
            }

            UiValue payload = CreateChunkPayload(checkedChunkId, payloadIndex, chunkCount, data.Length, checksum, encoded.Encoded);
            if (!IsChunkEnvelopeWithinLimit(payload, Session.NextOutboundSequence + payloadIndex))
            {
                return CreateChunkFailure("bridge.chunk-envelope-too-large");
            }

            payloads[payloadIndex] = payload;
            payloadIndex++;
        }

        UiBridgeEnvelope[] envelopes = new UiBridgeEnvelope[chunkCount];
        int index = 0;
        while (index < chunkCount)
        {
            envelopes[index] = Session.CreateSessionEnvelope(UiBridgeMessageKind.Chunk, payloads[index], Session.ReadNegotiatedVersion());
            index++;
        }

        return new BridgeChunkOperationResult
        {
            Success = true,
            Applied = true,
            Complete = true,
            ChunkId = checkedChunkId,
            Checksum = checksum,
            Data = CopyChunkBytes(data),
            Envelopes = envelopes
        };
    }

    internal BridgeChunkOperationResult AcceptBridgeChunk(UiBridgeEnvelope? envelope, bool isMainFrame, long nowMilliseconds)
    {
        BridgeChunkOperationResult readiness = ValidateChunkReadiness(nowMilliseconds);
        if (!readiness.Success)
        {
            return readiness;
        }

        if (envelope == null)
        {
            return CreateChunkFailure("bridge.null-envelope");
        }

        if (envelope.Sequence < 0)
        {
            return CreateChunkFailure("bridge.invalid-sequence");
        }

        if (envelope.Sequence <= Session.LastInboundSequence)
        {
            BridgeValidationResult duplicateValidation = ValidateChunkEnvelope(envelope, isMainFrame, envelope.Sequence == 0 ? -1 : envelope.Sequence - 1);
            if (!duplicateValidation.Success)
            {
                return CreateChunkFailure(duplicateValidation.Code);
            }

            if (envelope.Kind != UiBridgeMessageKind.Chunk)
            {
                return CreateChunkFailure("bridge.unexpected-chunk-kind");
            }

            BridgeChunkPayloadResult duplicatePayload = ParseChunkPayload(envelope.Payload);
            if (!duplicatePayload.Success)
            {
                return CreateChunkFailure(duplicatePayload.Code);
            }

            Session.LastActivityMilliseconds = nowMilliseconds;
            BridgeChunkOperationResult duplicate = CreateChunkSuccess(false);
            duplicate.Duplicate = true;
            duplicate.ChunkId = duplicatePayload.ChunkId;
            duplicate.Checksum = duplicatePayload.Checksum;
            return duplicate;
        }

        if (Session.LastInboundSequence == int.MaxValue || envelope.Sequence != Session.LastInboundSequence + 1)
        {
            BridgeValidationResult gapValidation = ValidateChunkEnvelope(envelope, isMainFrame, Session.LastInboundSequence);
            if (!gapValidation.Success)
            {
                return CreateChunkFailure(gapValidation.Code);
            }

            return CreateChunkFailure("bridge.message-out-of-order");
        }

        BridgeValidationResult validation = ValidateChunkEnvelope(envelope, isMainFrame, Session.LastInboundSequence);
        if (!validation.Success)
        {
            return CreateChunkFailure(validation.Code);
        }

        if (envelope.Kind != UiBridgeMessageKind.Chunk)
        {
            return CreateChunkFailure("bridge.unexpected-chunk-kind");
        }

        BridgeChunkPayloadResult payload = ParseChunkPayload(envelope.Payload);
        if (!payload.Success)
        {
            return CreateChunkFailure(payload.Code);
        }

        BridgeChunkHistoryEntry? history = FindChunkHistory(payload.ChunkId);
        if (history != null)
        {
            if (history.Length != payload.Length || !ValueComparer.TextEquals(history.Checksum, payload.Checksum))
            {
                return CreateChunkFailure("bridge.chunk-identity-conflict");
            }

            CommitChunkEnvelope(envelope, nowMilliseconds);
            BridgeChunkOperationResult historical = history.Expired ? CreateChunkFailure("bridge.chunk-expired") : CreateChunkSuccess(false);
            historical.Applied = history.Expired;
            historical.Duplicate = !history.Expired;
            historical.Expired = history.Expired;
            historical.ChunkId = payload.ChunkId;
            historical.Checksum = payload.Checksum;
            return historical;
        }

        BridgeChunkAssembly? assembly = FindChunkAssembly(payload.ChunkId);
        bool createdAssembly = false;
        if (assembly == null)
        {
            if (Assemblies.Count >= MaximumAssemblyCount)
            {
                BridgeChunkOperationResult backpressure = CreateChunkFailure("bridge.chunk-assembly-backpressure");
                backpressure.Backpressured = true;
                return backpressure;
            }

            assembly = new BridgeChunkAssembly
            {
                ChunkId = payload.ChunkId,
                ChunkCount = payload.ChunkCount,
                Length = payload.Length,
                Checksum = payload.Checksum,
                ExpiresAtMilliseconds = CreateChunkExpiry(nowMilliseconds),
                Chunks = new byte[payload.ChunkCount][],
                Received = new bool[payload.ChunkCount]
            };
            Assemblies.Add(assembly);
            createdAssembly = true;
        }
        else if (assembly.ChunkCount != payload.ChunkCount || assembly.Length != payload.Length || !ValueComparer.TextEquals(assembly.Checksum, payload.Checksum))
        {
            return CreateChunkFailure("bridge.chunk-metadata-conflict");
        }

        if (assembly.Received[payload.ChunkIndex])
        {
            if (!ChunkBytesEqual(assembly.Chunks[payload.ChunkIndex], payload.Data))
            {
                return CreateChunkFailure("bridge.chunk-data-conflict");
            }

            CommitChunkEnvelope(envelope, nowMilliseconds);
            BridgeChunkOperationResult duplicate = CreateChunkSuccess(false);
            duplicate.Duplicate = true;
            duplicate.ChunkId = payload.ChunkId;
            duplicate.Checksum = payload.Checksum;
            return duplicate;
        }

        if (payload.Data.Length > MaximumAssemblyBytes - BufferedBytes)
        {
            if (createdAssembly)
            {
                Assemblies.Remove(assembly);
            }

            BridgeChunkOperationResult backpressure = CreateChunkFailure("bridge.chunk-buffer-backpressure");
            backpressure.Backpressured = true;
            return backpressure;
        }

        assembly.Chunks[payload.ChunkIndex] = payload.Data;
        assembly.Received[payload.ChunkIndex] = true;
        assembly.ReceivedCount++;
        assembly.ReceivedBytes += payload.Data.Length;
        BufferedBytes += payload.Data.Length;
        if (assembly.ReceivedCount < assembly.ChunkCount)
        {
            CommitChunkEnvelope(envelope, nowMilliseconds);
            BridgeChunkOperationResult partial = CreateChunkSuccess(true);
            partial.ChunkId = payload.ChunkId;
            partial.Checksum = payload.Checksum;
            return partial;
        }

        byte[] completedData = AssembleChunkBytes(assembly);
        bool validLength = assembly.ReceivedBytes == assembly.Length && completedData.Length == assembly.Length;
        bool validChecksum = validLength && ValueComparer.TextEquals(StableHash.HashBytes(completedData), assembly.Checksum);
        RemoveChunkAssembly(assembly);
        CommitChunkEnvelope(envelope, nowMilliseconds);
        if (!validLength)
        {
            BridgeChunkOperationResult lengthFailure = CreateChunkFailure("bridge.chunk-length-mismatch");
            lengthFailure.Applied = true;
            return lengthFailure;
        }

        if (!validChecksum)
        {
            BridgeChunkOperationResult checksumFailure = CreateChunkFailure("bridge.chunk-checksum-mismatch");
            checksumFailure.Applied = true;
            return checksumFailure;
        }

        AddChunkHistory(payload.ChunkId, payload.Length, payload.Checksum, false);
        BridgeChunkOperationResult complete = CreateChunkSuccess(true);
        complete.Complete = true;
        complete.ChunkId = payload.ChunkId;
        complete.Checksum = payload.Checksum;
        complete.Data = completedData;
        return complete;
    }

    internal BridgeChunkOperationResult SweepExpiredBridgeChunks(long nowMilliseconds)
    {
        BridgeChunkOperationResult readiness = ValidateChunkReadiness(nowMilliseconds);
        if (!readiness.Success)
        {
            return readiness;
        }

        int expiredCount = 0;
        int countIndex = 0;
        while (countIndex < Assemblies.Count)
        {
            if (nowMilliseconds >= Assemblies[countIndex].ExpiresAtMilliseconds)
            {
                expiredCount++;
            }

            countIndex++;
        }

        if (expiredCount == 0)
        {
            return CreateChunkSuccess(false);
        }

        string[] expiredIds = new string[expiredCount];
        int outputIndex = 0;
        int assemblyIndex = 0;
        while (assemblyIndex < Assemblies.Count)
        {
            BridgeChunkAssembly assembly = Assemblies[assemblyIndex];
            if (nowMilliseconds >= assembly.ExpiresAtMilliseconds)
            {
                expiredIds[outputIndex] = assembly.ChunkId;
                outputIndex++;
                AddChunkHistory(assembly.ChunkId, assembly.Length, assembly.Checksum, true);
                BufferedBytes -= assembly.ReceivedBytes;
                Assemblies.RemoveAt(assemblyIndex);
            }
            else
            {
                assemblyIndex++;
            }
        }

        BridgeChunkOperationResult result = CreateChunkSuccess(true);
        result.Expired = true;
        result.ExpiredChunkIds = expiredIds;
        return result;
    }

    internal BridgeChunkPayloadResult ParseChunkPayload(UiValue? payload)
    {
        if (payload == null || payload.Kind != UiValueKind.Object || payload.ObjectValue == null)
        {
            return CreateChunkPayloadFailure("bridge.invalid-chunk-payload");
        }

        UiValueEntry[] entries = payload.ObjectValue;
        string? chunkId = null;
        string? encodedData = null;
        string? checksum = null;
        int chunkIndex = 0;
        int chunkCount = 0;
        int length = 0;
        bool hasChunkIndex = false;
        bool hasChunkCount = false;
        bool hasLength = false;
        bool hasChunkData = false;
        int index = 0;
        while (index < entries.Length)
        {
            UiValueEntry entry = entries[index];
            if (entry == null || entry.Value == null)
            {
                return CreateChunkPayloadFailure("bridge.invalid-chunk-payload");
            }

            if (ValueComparer.TextEquals(entry.Key, BridgeProtocolNames.ChunkId))
            {
                if (chunkId != null || entry.Value.Kind != UiValueKind.String || !IsChunkIdentifierValid(entry.Value.StringValue))
                {
                    return CreateChunkPayloadFailure("bridge.invalid-chunk-id");
                }

                chunkId = entry.Value.StringValue;
            }
            else if (ValueComparer.TextEquals(entry.Key, BridgeProtocolNames.ChunkIndex))
            {
                if (hasChunkIndex || !ReadChunkInteger(entry.Value, 0, int.MaxValue))
                {
                    return CreateChunkPayloadFailure("bridge.invalid-chunk-index");
                }

                chunkIndex = (int)entry.Value.IntegerValue;
                hasChunkIndex = true;
            }
            else if (ValueComparer.TextEquals(entry.Key, BridgeProtocolNames.ChunkCount))
            {
                if (hasChunkCount || !ReadChunkInteger(entry.Value, 1, HardMaximumChunkCount))
                {
                    return CreateChunkPayloadFailure("bridge.invalid-chunk-count");
                }

                chunkCount = (int)entry.Value.IntegerValue;
                hasChunkCount = true;
            }
            else if (ValueComparer.TextEquals(entry.Key, ChunkLengthMember))
            {
                if (hasLength || !ReadChunkInteger(entry.Value, 0, MaximumAssemblyBytes))
                {
                    return CreateChunkPayloadFailure("bridge.invalid-chunk-length");
                }

                length = (int)entry.Value.IntegerValue;
                hasLength = true;
            }
            else if (ValueComparer.TextEquals(entry.Key, BridgeProtocolNames.ChunkData))
            {
                if (hasChunkData || entry.Value.Kind != UiValueKind.String || entry.Value.StringValue == null)
                {
                    return CreateChunkPayloadFailure("bridge.invalid-chunk-data");
                }

                encodedData = entry.Value.StringValue;
                hasChunkData = true;
            }
            else if (ValueComparer.TextEquals(entry.Key, BridgeProtocolNames.Checksum))
            {
                if (checksum != null || entry.Value.Kind != UiValueKind.String || !IsChecksumValid(entry.Value.StringValue))
                {
                    return CreateChunkPayloadFailure("bridge.invalid-chunk-checksum");
                }

                checksum = entry.Value.StringValue;
            }
            else
            {
                return CreateChunkPayloadFailure("bridge.unknown-chunk-member");
            }

            index++;
        }

        if (chunkId == null || !hasChunkIndex || !hasChunkCount || !hasLength || !hasChunkData || encodedData == null || checksum == null)
        {
            return CreateChunkPayloadFailure("bridge.incomplete-chunk-payload");
        }

        if (chunkIndex >= chunkCount || chunkCount != CalculateChunkCount(length))
        {
            return CreateChunkPayloadFailure("bridge.invalid-chunk-shape");
        }

        BridgeBinaryResult decoded = BinaryCodec.DecodeBridgeBinary(encodedData, MaximumChunkBytes);
        if (!decoded.Success)
        {
            return CreateChunkPayloadFailure(decoded.Code);
        }

        int expectedLength = ReadExpectedChunkLength(chunkIndex, chunkCount, length);
        if (decoded.Data.Length != expectedLength)
        {
            return CreateChunkPayloadFailure("bridge.invalid-chunk-length");
        }

        return new BridgeChunkPayloadResult
        {
            Success = true,
            ChunkId = chunkId,
            ChunkIndex = chunkIndex,
            ChunkCount = chunkCount,
            Length = length,
            Checksum = checksum,
            Data = decoded.Data
        };
    }

    internal UiValue CreateChunkPayload(string chunkId, int chunkIndex, int chunkCount, int length, string checksum, string encodedData)
    {
        return new UiValue
        {
            Kind = UiValueKind.Object,
            ObjectValue = new UiValueEntry[]
            {
                new UiValueEntry { Key = BridgeProtocolNames.ChunkId, Value = new UiValue { Kind = UiValueKind.String, StringValue = chunkId } },
                new UiValueEntry { Key = BridgeProtocolNames.ChunkIndex, Value = new UiValue { Kind = UiValueKind.Integer, IntegerValue = chunkIndex } },
                new UiValueEntry { Key = BridgeProtocolNames.ChunkCount, Value = new UiValue { Kind = UiValueKind.Integer, IntegerValue = chunkCount } },
                new UiValueEntry { Key = ChunkLengthMember, Value = new UiValue { Kind = UiValueKind.Integer, IntegerValue = length } },
                new UiValueEntry { Key = BridgeProtocolNames.ChunkData, Value = new UiValue { Kind = UiValueKind.String, StringValue = encodedData } },
                new UiValueEntry { Key = BridgeProtocolNames.Checksum, Value = new UiValue { Kind = UiValueKind.String, StringValue = checksum } }
            }
        };
    }

    internal BridgeValidationResult ValidateChunkEnvelope(UiBridgeEnvelope envelope, bool isMainFrame, int lastSequence)
    {
        UiBridgeOptions? options = Session.Options;
        if (options == null)
        {
            return Session.Validator.CreateBridgeValidationFailure("bridge.null-options", "Bridge options are required.");
        }

        UiBridgeOptions validationOptions = new UiBridgeOptions
        {
            Version = Session.ReadNegotiatedVersion(),
            Origin = options.Origin,
            RequireMainFrame = options.RequireMainFrame,
            MaximumMessageLength = options.MaximumMessageLength,
            MaximumPendingRequests = options.MaximumPendingRequests,
            TimeoutMilliseconds = options.TimeoutMilliseconds
        };
        return Session.Validator.ValidateBridgeEnvelope(envelope, validationOptions, Session.Session, isMainFrame, lastSequence);
    }

    internal BridgeChunkOperationResult ValidateChunkReadiness(long nowMilliseconds)
    {
        if (Session == null || Session.State != UiTransportState.Ready)
        {
            return CreateChunkFailure("bridge.invalid-state");
        }

        if (!Session.IsActiveTimestampValid(nowMilliseconds))
        {
            return CreateChunkFailure("bridge.invalid-time");
        }

        if (MaximumChunkBytes < 1 || MaximumChunkBytes > HardMaximumChunkBytes || MaximumAssemblyBytes < MaximumChunkBytes || MaximumAssemblyBytes > HardMaximumAssemblyBytes || MaximumAssemblyCount < 1 || MaximumAssemblyCount > HardMaximumAssemblyCount || ExpiryMilliseconds < 1)
        {
            return CreateChunkFailure("bridge.invalid-chunk-configuration");
        }

        UiBridgeOptions? options = Session.Options;
        if (options == null || MaximumChunkBytes > options.MaximumMessageLength)
        {
            return CreateChunkFailure("bridge.invalid-chunk-configuration");
        }

        if (Session.NegotiatedCapabilities == null || !Session.NegotiatedCapabilities.SupportsChunking)
        {
            return CreateChunkFailure("bridge.chunking-unsupported");
        }

        return CreateChunkSuccess(false);
    }

    internal int CalculateChunkCount(int length)
    {
        if (length == 0)
        {
            return 1;
        }

        return (length - 1) / MaximumChunkBytes + 1;
    }

    internal int ReadExpectedChunkLength(int chunkIndex, int chunkCount, int length)
    {
        if (chunkIndex < chunkCount - 1)
        {
            return MaximumChunkBytes;
        }

        return length - MaximumChunkBytes * (chunkCount - 1);
    }

    internal bool ReadChunkInteger(UiValue value, int minimum, int maximum)
    {
        return value.Kind == UiValueKind.Integer && value.IntegerValue >= minimum && value.IntegerValue <= maximum;
    }

    internal bool IsChunkIdentifierValid(string? chunkId)
    {
        return !string.IsNullOrWhiteSpace(chunkId) && chunkId.Length <= BridgeSession.MaximumIdentifierLength;
    }

    internal bool IsChecksumValid(string? checksum)
    {
        if (checksum == null || checksum.Length != 64)
        {
            return false;
        }

        int index = 0;
        while (index < checksum.Length)
        {
            char value = checksum[index];
            bool digit = value >= '0' && value <= '9';
            bool lowerHex = value >= 'a' && value <= 'f';
            if (!digit && !lowerHex)
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal bool CanCreateChunkEnvelopes(int count)
    {
        return count >= 0 && Session.NextOutboundSequence <= int.MaxValue - count;
    }

    internal bool IsChunkEnvelopeWithinLimit(UiValue payload, int sequence)
    {
        UiBridgeOptions? options = Session.Options;
        if (options == null)
        {
            return false;
        }

        UiBridgeEnvelope candidate = new UiBridgeEnvelope
        {
            Version = Session.ReadNegotiatedVersion(),
            Session = Session.Session,
            Sequence = sequence,
            Origin = Session.ReadLocalOrigin(),
            Kind = UiBridgeMessageKind.Chunk,
            Payload = payload
        };
        string json = new BridgeCodec().SerializeBridgeEnvelope(candidate, int.MaxValue);
        return Encoding.UTF8.GetByteCount(json) <= options.MaximumMessageLength;
    }

    internal long CreateChunkExpiry(long nowMilliseconds)
    {
        if (nowMilliseconds > long.MaxValue - ExpiryMilliseconds)
        {
            return long.MaxValue;
        }

        return nowMilliseconds + ExpiryMilliseconds;
    }

    internal void CommitChunkEnvelope(UiBridgeEnvelope envelope, long nowMilliseconds)
    {
        Session.LastInboundSequence = envelope.Sequence;
        Session.LastActivityMilliseconds = nowMilliseconds;
    }

    internal BridgeChunkAssembly? FindChunkAssembly(string chunkId)
    {
        int index = 0;
        while (index < Assemblies.Count)
        {
            if (ValueComparer.TextEquals(Assemblies[index].ChunkId, chunkId))
            {
                return Assemblies[index];
            }

            index++;
        }

        return null;
    }

    internal BridgeChunkHistoryEntry? FindChunkHistory(string chunkId)
    {
        int index = 0;
        while (index < History.Count)
        {
            if (ValueComparer.TextEquals(History[index].ChunkId, chunkId))
            {
                return History[index];
            }

            index++;
        }

        return null;
    }

    internal void AddChunkHistory(string chunkId, int length, string checksum, bool expired)
    {
        if (History.Count >= MaximumAssemblyCount)
        {
            History.RemoveAt(0);
        }

        History.Add(new BridgeChunkHistoryEntry
        {
            ChunkId = chunkId,
            Length = length,
            Checksum = checksum,
            Expired = expired
        });
    }

    internal byte[] AssembleChunkBytes(BridgeChunkAssembly assembly)
    {
        byte[] data = new byte[assembly.ReceivedBytes];
        int offset = 0;
        int index = 0;
        while (index < assembly.Chunks.Length)
        {
            byte[] chunk = assembly.Chunks[index] ?? Array.Empty<byte>();
            if (chunk.Length > 0)
            {
                Array.Copy(chunk, 0, data, offset, chunk.Length);
                offset += chunk.Length;
            }

            index++;
        }

        return data;
    }

    internal void RemoveChunkAssembly(BridgeChunkAssembly assembly)
    {
        BufferedBytes -= assembly.ReceivedBytes;
        Assemblies.Remove(assembly);
    }

    internal bool ChunkBytesEqual(byte[]? left, byte[] right)
    {
        if (left == null || left.Length != right.Length)
        {
            return false;
        }

        int index = 0;
        while (index < left.Length)
        {
            if (left[index] != right[index])
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal byte[] CopyChunkBytes(byte[] source)
    {
        byte[] copy = new byte[source.Length];
        Array.Copy(source, copy, source.Length);
        return copy;
    }

    internal BridgeChunkPayloadResult CreateChunkPayloadFailure(string code)
    {
        return new BridgeChunkPayloadResult { Code = code };
    }

    internal BridgeChunkOperationResult CreateChunkSuccess(bool applied)
    {
        return new BridgeChunkOperationResult { Success = true, Applied = applied };
    }

    internal BridgeChunkOperationResult CreateChunkFailure(string code)
    {
        return new BridgeChunkOperationResult { Code = code };
    }
}
