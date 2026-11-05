using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;

namespace diameter;

internal class AccountingStoreMutation
{
    internal bool Changed;
    internal List<DiameterAccountingRecord> Records =
        new List<DiameterAccountingRecord>();
    internal DiameterAccountingStoreResult Result =
        new DiameterAccountingStoreResult();
}

internal class AccountingStoreDecodeResult
{
    internal bool Valid;
    internal List<DiameterAccountingRecord> Records =
        new List<DiameterAccountingRecord>();
    internal string Code = string.Empty;
    internal string Message = string.Empty;
}

internal class AccountingRecordStoreEngine
{
    internal List<DiameterAccountingRecord> Records =
        new List<DiameterAccountingRecord>();

    internal DiameterAccountingStoreResult LoadAccountingStoreRecord(
        long recordIdentifier)
    {
        if (recordIdentifier <= 0)
        {
            throw new ArgumentOutOfRangeException("recordIdentifier");
        }

        DiameterAccountingStoreResult result =
            new DiameterAccountingStoreResult();
        result.RecordIdentifier = recordIdentifier;
        int index = FindAccountingStoreRecordIndex(recordIdentifier);
        if (index >= 0)
        {
            result.Found = true;
            result.Record =
                AccountingRecordCloner.CloneAccountingRecord(
                    Records[index]);
            result.Code = "accounting-record-found";
            result.Message = "The accounting record was found.";
        }
        else
        {
            result.Code = "accounting-record-not-found";
            result.Message = "The accounting record was not found.";
        }

        return result;
    }

    internal AccountingStoreMutation PrepareAccountingStoreSave(
        DiameterAccountingRecord? record,
        long expectedVersion)
    {
        if (record == null)
        {
            throw new ArgumentException(
                "An accounting record is required.");
        }

        record.ValidateAccountingRecord();
        if (expectedVersion < 0)
        {
            throw new ArgumentOutOfRangeException("expectedVersion");
        }

        AccountingStoreMutation mutation =
            new AccountingStoreMutation();
        mutation.Result.RecordIdentifier = record.RecordIdentifier;
        int existingIndex = FindAccountingStoreRecordIndex(
            record.RecordIdentifier);
        if (existingIndex < 0 && expectedVersion != 0)
        {
            mutation.Result.Conflict = true;
            mutation.Result.Code = "accounting-version-conflict";
            mutation.Result.Message =
                "The accounting record does not exist at the expected version.";
            return mutation;
        }

        if (existingIndex >= 0 &&
            Records[existingIndex].Version != expectedVersion)
        {
            mutation.Result.Found = true;
            mutation.Result.Conflict = true;
            mutation.Result.Record =
                AccountingRecordCloner.CloneAccountingRecord(
                    Records[existingIndex]);
            mutation.Result.Code = "accounting-version-conflict";
            mutation.Result.Message =
                "The accounting record version changed before it was saved.";
            return mutation;
        }

        if (expectedVersion == long.MaxValue)
        {
            mutation.Result.Found = existingIndex >= 0;
            mutation.Result.Conflict = true;
            mutation.Result.Code = "accounting-version-exhausted";
            mutation.Result.Message =
                "The accounting record version cannot advance further.";
            return mutation;
        }

        mutation.Records = CloneAccountingStoreRecords();
        DiameterAccountingRecord saved =
            AccountingRecordCloner.CloneAccountingRecord(record);
        saved.Version = expectedVersion + 1;
        if (existingIndex >= 0)
        {
            mutation.Records[existingIndex] = saved;
        }
        else
        {
            mutation.Records.Add(saved);
        }

        mutation.Changed = true;
        mutation.Result.Found = existingIndex >= 0;
        mutation.Result.Saved = true;
        mutation.Result.Record =
            AccountingRecordCloner.CloneAccountingRecord(saved);
        mutation.Result.Code = "accounting-record-saved";
        mutation.Result.Message = "The accounting record was saved.";
        return mutation;
    }

    internal AccountingStoreMutation PrepareAccountingStoreRemove(
        long recordIdentifier,
        long expectedVersion)
    {
        if (recordIdentifier <= 0 || expectedVersion < 0)
        {
            throw new ArgumentOutOfRangeException(
                "Accounting record identifier or version");
        }

        AccountingStoreMutation mutation =
            new AccountingStoreMutation();
        mutation.Result.RecordIdentifier = recordIdentifier;
        int existingIndex = FindAccountingStoreRecordIndex(
            recordIdentifier);
        if (existingIndex < 0)
        {
            mutation.Result.Code = "accounting-record-not-found";
            mutation.Result.Message =
                "The accounting record was not found.";
            return mutation;
        }

        mutation.Result.Found = true;
        mutation.Result.Record =
            AccountingRecordCloner.CloneAccountingRecord(
                Records[existingIndex]);
        if (Records[existingIndex].Version != expectedVersion)
        {
            mutation.Result.Conflict = true;
            mutation.Result.Code = "accounting-version-conflict";
            mutation.Result.Message =
                "The accounting record version changed before removal.";
            return mutation;
        }

        mutation.Records = CloneAccountingStoreRecords();
        mutation.Records.RemoveAt(existingIndex);
        mutation.Changed = true;
        mutation.Result.Removed = true;
        mutation.Result.Code = "accounting-record-removed";
        mutation.Result.Message = "The accounting record was removed.";
        return mutation;
    }

    internal List<DiameterAccountingRecord> ReadAccountingStoreRecords()
    {
        return CloneAccountingStoreRecords();
    }

    internal void ApplyAccountingStoreMutation(
        AccountingStoreMutation mutation)
    {
        if (mutation.Changed)
        {
            Records = CloneProvidedAccountingStoreRecords(
                mutation.Records);
        }
    }

    internal void ReplaceAccountingStoreRecords(
        List<DiameterAccountingRecord> records)
    {
        Records = CloneProvidedAccountingStoreRecords(records);
    }

    internal int FindAccountingStoreRecordIndex(long recordIdentifier)
    {
        int index = 0;
        while (index < Records.Count)
        {
            if (Records[index].RecordIdentifier == recordIdentifier)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal List<DiameterAccountingRecord> CloneAccountingStoreRecords()
    {
        return CloneProvidedAccountingStoreRecords(Records);
    }

    internal List<DiameterAccountingRecord>
        CloneProvidedAccountingStoreRecords(
            List<DiameterAccountingRecord> source)
    {
        List<DiameterAccountingRecord> result =
            new List<DiameterAccountingRecord>();
        int index = 0;
        while (index < source.Count)
        {
            result.Add(AccountingRecordCloner.CloneAccountingRecord(
                source[index]));
            index = index + 1;
        }

        return result;
    }
}

internal static class AccountingRecordStoreFileCodec
{
    internal const uint Magic = 1145651533U;
    internal const uint FormatVersion = 1U;
    internal const int HeaderLength = 12;
    internal const int HashLength = 32;
    internal static readonly JsonSerializerOptions JsonOptions =
        CreateAccountingStoreJsonOptions();

    internal static JsonSerializerOptions CreateAccountingStoreJsonOptions()
    {
        JsonSerializerOptions result = new JsonSerializerOptions();
        result.IncludeFields = true;
        return result;
    }

    internal static byte[] EncodeAccountingStoreFile(
        List<DiameterAccountingRecord> records)
    {
        byte[] payload = JsonSerializer.SerializeToUtf8Bytes(
            records,
            JsonOptions);
        byte[] hash = SHA256.HashData(payload);
        byte[] result = new byte[
            HeaderLength + payload.Length + HashLength];
        NetworkByteOrder.WriteUInt32(result, Magic);
        NetworkByteOrder.WriteUInt32(
            result.AsSpan(4),
            FormatVersion);
        NetworkByteOrder.WriteUInt32(
            result.AsSpan(8),
            (uint)payload.Length);
        Array.Copy(payload, 0, result, HeaderLength, payload.Length);
        Array.Copy(
            hash,
            0,
            result,
            HeaderLength + payload.Length,
            HashLength);
        return result;
    }

    internal static AccountingStoreDecodeResult DecodeAccountingStoreFile(
        byte[] data)
    {
        AccountingStoreDecodeResult result =
            new AccountingStoreDecodeResult();
        if (data == null || data.Length < HeaderLength + HashLength)
        {
            SetAccountingStoreDecodeFailure(
                result,
                "accounting-store-truncated",
                "The accounting store file is truncated.");
            return result;
        }

        uint magic = NetworkByteOrder.ReadUInt32(data);
        uint version = NetworkByteOrder.ReadUInt32(data.AsSpan(4));
        uint payloadLengthValue =
            NetworkByteOrder.ReadUInt32(data.AsSpan(8));
        if (magic != Magic || version != FormatVersion ||
            payloadLengthValue > int.MaxValue)
        {
            SetAccountingStoreDecodeFailure(
                result,
                "accounting-store-header-invalid",
                "The accounting store header is invalid or unsupported.");
            return result;
        }

        int payloadLength = (int)payloadLengthValue;
        if (payloadLength != data.Length - HeaderLength - HashLength)
        {
            SetAccountingStoreDecodeFailure(
                result,
                "accounting-store-length-invalid",
                "The accounting store payload length is inconsistent.");
            return result;
        }

        byte[] payload = new byte[payloadLength];
        byte[] actualHash = new byte[HashLength];
        Array.Copy(data, HeaderLength, payload, 0, payloadLength);
        Array.Copy(
            data,
            HeaderLength + payloadLength,
            actualHash,
            0,
            HashLength);
        byte[] expectedHash = SHA256.HashData(payload);
        if (!CryptographicOperations.FixedTimeEquals(
            expectedHash,
            actualHash))
        {
            SetAccountingStoreDecodeFailure(
                result,
                "accounting-store-checksum-invalid",
                "The accounting store checksum does not match its payload.");
            return result;
        }

        List<DiameterAccountingRecord>? records =
            JsonSerializer.Deserialize<List<DiameterAccountingRecord>>(
                payload,
                JsonOptions);
        if (records == null)
        {
            SetAccountingStoreDecodeFailure(
                result,
                "accounting-store-payload-invalid",
                "The accounting store payload is invalid.");
            return result;
        }

        int index = 0;
        while (index < records.Count)
        {
            DiameterAccountingRecord record = records[index];
            if (record == null ||
                !IsAccountingStoreRecordShapeValid(record))
            {
                SetAccountingStoreDecodeFailure(
                    result,
                    "accounting-store-record-invalid",
                    "The accounting store contains an invalid record.");
                return result;
            }

            index = index + 1;
        }

        result.Valid = true;
        result.Records = records;
        return result;
    }

    internal static bool IsAccountingStoreRecordShapeValid(
        DiameterAccountingRecord record)
    {
        if (record.RecordIdentifier <= 0 || record.Version < 0 ||
            record.Request == null || record.RouteIdentifier == null ||
            record.Code == null ||
            record.Message == null || record.AttemptCount < 0)
        {
            return false;
        }

        DiameterAccountingRequest request = record.Request;
        return request.SessionId != null && request.SessionId.Length > 0 &&
            request.DestinationRealm != null &&
            request.UserName != null &&
            request.AccountingSessionId != null &&
            request.AccountingMultiSessionId != null &&
            request.RouteIdentifier != null &&
            request.CorrelationText != null &&
            request.ServiceAvps != null &&
            request.ApplicationState != null &&
            (record.OutboundMessage == null ||
            record.OutboundMessage.Avps != null) &&
            (record.LastAnswer == null || record.LastAnswer.Avps != null);
    }

    internal static void SetAccountingStoreDecodeFailure(
        AccountingStoreDecodeResult result,
        string code,
        string message)
    {
        result.Code = code;
        result.Message = message;
    }
}
