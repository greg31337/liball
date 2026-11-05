using System;
using System.Collections.Generic;
using System.Text;

namespace iso20022;

internal static class ArtifactManifestCodec
{
    internal const string Magic = "ISO20022-ARTIFACT-1";
    internal const int FieldCount = 45;

    internal static ArtifactManifestEncodeResult EncodeManifest(ArtifactManifest manifest)
    {
        ArtifactManifestEncodeResult result = new ArtifactManifestEncodeResult();
        if (manifest.Coordinate == null || manifest.Hash == null || manifest.Hash.Value == null ||
            manifest.Coordinate.Edition == null || manifest.Coordinate.Identifier == null ||
            manifest.Coordinate.Version == null || manifest.OriginUri == null ||
            manifest.HashAlgorithm == null || manifest.Hash.LexicalValue == null ||
            manifest.MediaType == null || manifest.LicenseNotice == null)
        {
            result.ErrorCode = "artifact.store.manifest_invalid";
            result.ErrorMessage = "The artifact manifest contains a missing required value.";
            return result;
        }

        if (manifest.RetrievedAt != null &&
            (manifest.RetrievedAt.Date == null || manifest.RetrievedAt.Time == null ||
            manifest.RetrievedAt.LexicalValue == null ||
            manifest.RetrievedAt.Date.LexicalValue == null ||
            manifest.RetrievedAt.Time.FractionalSecondDigits == null ||
            manifest.RetrievedAt.Time.LexicalValue == null))
        {
            result.ErrorCode = "artifact.store.manifest_invalid";
            result.ErrorMessage = "The artifact retrieval time contains a missing required value.";
            return result;
        }

        if (manifest.PublicationDate != null && manifest.PublicationDate.LexicalValue == null)
        {
            result.ErrorCode = "artifact.store.manifest_invalid";
            result.ErrorMessage = "The artifact publication date contains a missing lexical value.";
            return result;
        }

        if ((manifest.RetrievedAt != null &&
            ((int)manifest.RetrievedAt.LexicalMode < 0 || (int)manifest.RetrievedAt.LexicalMode > 1 ||
            (int)manifest.RetrievedAt.Date.LexicalMode < 0 ||
            (int)manifest.RetrievedAt.Date.LexicalMode > 1 ||
            (int)manifest.RetrievedAt.Time.LexicalMode < 0 ||
            (int)manifest.RetrievedAt.Time.LexicalMode > 1)) ||
            (manifest.PublicationDate != null &&
            ((int)manifest.PublicationDate.Kind < 0 || (int)manifest.PublicationDate.Kind > 5)))
        {
            result.ErrorCode = "artifact.store.manifest_invalid";
            result.ErrorMessage = "The artifact manifest contains an invalid lexical-mode value.";
            return result;
        }

        IsoDateTimeValue retrievedAt = manifest.RetrievedAt ?? new IsoDateTimeValue();
        IsoPartialDateValue publicationDate = manifest.PublicationDate ?? new IsoPartialDateValue();
        List<string> lines = new List<string>();
        lines.Add(Magic);
        lines.Add(IntegerLexical.FormatInt32((int)manifest.Coordinate.Kind));
        lines.Add(EncodeString(manifest.Coordinate.Edition));
        lines.Add(EncodeString(manifest.Coordinate.Identifier));
        lines.Add(EncodeString(manifest.Coordinate.Version));
        lines.Add(EncodeString(manifest.OriginUri));
        lines.Add(FormatBoolean(manifest.RetrievedAt != null));
        AddDateFields(lines, retrievedAt.Date);
        AddTimeFields(lines, retrievedAt.Time);
        lines.Add(IntegerLexical.FormatInt32((int)retrievedAt.LexicalMode));
        lines.Add(EncodeString(retrievedAt.LexicalValue));
        lines.Add(FormatBoolean(manifest.PublicationDate != null));
        AddPartialDateFields(lines, publicationDate);
        lines.Add(EncodeString(manifest.HashAlgorithm));
        lines.Add(BinaryLexical.FormatBase64(manifest.Hash.Value));
        lines.Add(IntegerLexical.FormatInt32((int)manifest.Hash.Encoding));
        lines.Add(EncodeString(manifest.Hash.LexicalValue));
        lines.Add(EncodeString(manifest.MediaType));
        lines.Add(EncodeString(manifest.LicenseNotice));
        lines.Add(FormatBoolean(manifest.IsApproved));
        result.Success = true;
        result.Lines = lines.ToArray();
        return result;
    }

    internal static ArtifactManifestDecodeResult DecodeManifest(string[] lines)
    {
        ArtifactManifestDecodeResult result = new ArtifactManifestDecodeResult();
        if (lines.Length != FieldCount || lines[0] != Magic)
        {
            result.ErrorCode = "artifact.store.manifest_invalid";
            result.ErrorMessage = "The stored artifact manifest has an unsupported structure.";
            return result;
        }

        ArtifactManifestTextReader reader = new ArtifactManifestTextReader();
        reader.Lines = lines;
        reader.Index = 1;
        int artifactKind = reader.ReadInteger();
        string edition = reader.ReadEncodedString();
        string identifier = reader.ReadEncodedString();
        string version = reader.ReadEncodedString();
        string originUri = reader.ReadEncodedString();
        bool hasRetrievedAt = reader.ReadBoolean();
        IsoDateValue retrievedDate = ReadDateFields(reader);
        IsoTimeValue retrievedTime = ReadTimeFields(reader);
        int retrievedMode = reader.ReadInteger();
        string retrievedLexical = reader.ReadEncodedString();
        bool hasPublicationDate = reader.ReadBoolean();
        IsoPartialDateValue publicationDate = ReadPartialDateFields(reader);
        string hashAlgorithm = reader.ReadEncodedString();
        byte[] hash = reader.ReadEncodedBytes();
        int hashEncoding = reader.ReadInteger();
        string hashLexical = reader.ReadEncodedString();
        string mediaType = reader.ReadEncodedString();
        string licenseNotice = reader.ReadEncodedString();
        bool isApproved = reader.ReadBoolean();
        if (!reader.Success || reader.Index != lines.Length ||
            artifactKind < 0 || artifactKind > 5 ||
            retrievedMode < 0 || retrievedMode > 1 ||
            hashEncoding < 0 || hashEncoding > 2 ||
            publicationDate.Kind < PartialDateKind.Unknown || publicationDate.Kind > PartialDateKind.MonthDay)
        {
            result.ErrorCode = "artifact.store.manifest_invalid";
            result.ErrorMessage = "The stored artifact manifest contains an invalid field value.";
            return result;
        }

        ArtifactManifest manifest = new ArtifactManifest();
        manifest.Coordinate = new ArtifactCoordinate();
        manifest.Coordinate.Kind = (ArtifactKind)artifactKind;
        manifest.Coordinate.Edition = edition;
        manifest.Coordinate.Identifier = identifier;
        manifest.Coordinate.Version = version;
        manifest.OriginUri = originUri;
        if (hasRetrievedAt)
        {
            manifest.RetrievedAt = new IsoDateTimeValue();
            manifest.RetrievedAt.Date = retrievedDate;
            manifest.RetrievedAt.Time = retrievedTime;
            manifest.RetrievedAt.LexicalMode = (DateTimeLexicalMode)retrievedMode;
            manifest.RetrievedAt.LexicalValue = retrievedLexical;
        }

        if (hasPublicationDate)
        {
            manifest.PublicationDate = publicationDate;
        }

        manifest.Hash = new IsoBinaryValue();
        manifest.Hash.Value = hash;
        manifest.Hash.Encoding = (BinaryEncodingKind)hashEncoding;
        manifest.Hash.LexicalValue = hashLexical;
        manifest.HashAlgorithm = hashAlgorithm;
        manifest.MediaType = mediaType;
        manifest.LicenseNotice = licenseNotice;
        manifest.IsApproved = isApproved;
        result.Success = true;
        result.Manifest = manifest;
        return result;
    }

    internal static void AddDateFields(List<string> lines, IsoDateValue value)
    {
        lines.Add(IntegerLexical.FormatInt32(value.Year));
        lines.Add(IntegerLexical.FormatInt32(value.Month));
        lines.Add(IntegerLexical.FormatInt32(value.Day));
        lines.Add(FormatBoolean(value.HasTimezone));
        lines.Add(IntegerLexical.FormatInt32(value.TimezoneOffsetMinutes));
        lines.Add(FormatBoolean(value.IsUtc));
        lines.Add(FormatBoolean(value.IsNegativeTimezoneZero));
        lines.Add(IntegerLexical.FormatInt32((int)value.LexicalMode));
        lines.Add(EncodeString(value.LexicalValue));
    }

    internal static void AddTimeFields(List<string> lines, IsoTimeValue value)
    {
        lines.Add(IntegerLexical.FormatInt32(value.Hour));
        lines.Add(IntegerLexical.FormatInt32(value.Minute));
        lines.Add(IntegerLexical.FormatInt32(value.Second));
        lines.Add(EncodeString(value.FractionalSecondDigits));
        lines.Add(FormatBoolean(value.HasTimezone));
        lines.Add(IntegerLexical.FormatInt32(value.TimezoneOffsetMinutes));
        lines.Add(FormatBoolean(value.IsUtc));
        lines.Add(FormatBoolean(value.IsNegativeTimezoneZero));
        lines.Add(IntegerLexical.FormatInt32((int)value.LexicalMode));
        lines.Add(EncodeString(value.LexicalValue));
    }

    internal static void AddPartialDateFields(List<string> lines, IsoPartialDateValue value)
    {
        lines.Add(IntegerLexical.FormatInt32((int)value.Kind));
        lines.Add(IntegerLexical.FormatInt32(value.Year));
        lines.Add(IntegerLexical.FormatInt32(value.Month));
        lines.Add(IntegerLexical.FormatInt32(value.Day));
        lines.Add(FormatBoolean(value.HasTimezone));
        lines.Add(IntegerLexical.FormatInt32(value.TimezoneOffsetMinutes));
        lines.Add(FormatBoolean(value.IsUtc));
        lines.Add(FormatBoolean(value.IsNegativeTimezoneZero));
        lines.Add(EncodeString(value.LexicalValue));
    }

    internal static IsoDateValue ReadDateFields(ArtifactManifestTextReader reader)
    {
        IsoDateValue value = new IsoDateValue();
        value.Year = reader.ReadInteger();
        value.Month = reader.ReadInteger();
        value.Day = reader.ReadInteger();
        value.HasTimezone = reader.ReadBoolean();
        value.TimezoneOffsetMinutes = reader.ReadInteger();
        value.IsUtc = reader.ReadBoolean();
        value.IsNegativeTimezoneZero = reader.ReadBoolean();
        int lexicalMode = reader.ReadInteger();
        if (lexicalMode < 0 || lexicalMode > 1)
        {
            reader.Success = false;
        }

        value.LexicalMode = (DateTimeLexicalMode)lexicalMode;
        value.LexicalValue = reader.ReadEncodedString();
        return value;
    }

    internal static IsoTimeValue ReadTimeFields(ArtifactManifestTextReader reader)
    {
        IsoTimeValue value = new IsoTimeValue();
        value.Hour = reader.ReadInteger();
        value.Minute = reader.ReadInteger();
        value.Second = reader.ReadInteger();
        value.FractionalSecondDigits = reader.ReadEncodedString();
        value.HasTimezone = reader.ReadBoolean();
        value.TimezoneOffsetMinutes = reader.ReadInteger();
        value.IsUtc = reader.ReadBoolean();
        value.IsNegativeTimezoneZero = reader.ReadBoolean();
        int lexicalMode = reader.ReadInteger();
        if (lexicalMode < 0 || lexicalMode > 1)
        {
            reader.Success = false;
        }

        value.LexicalMode = (DateTimeLexicalMode)lexicalMode;
        value.LexicalValue = reader.ReadEncodedString();
        return value;
    }

    internal static IsoPartialDateValue ReadPartialDateFields(ArtifactManifestTextReader reader)
    {
        IsoPartialDateValue value = new IsoPartialDateValue();
        value.Kind = (PartialDateKind)reader.ReadInteger();
        value.Year = reader.ReadInteger();
        value.Month = reader.ReadInteger();
        value.Day = reader.ReadInteger();
        value.HasTimezone = reader.ReadBoolean();
        value.TimezoneOffsetMinutes = reader.ReadInteger();
        value.IsUtc = reader.ReadBoolean();
        value.IsNegativeTimezoneZero = reader.ReadBoolean();
        value.LexicalValue = reader.ReadEncodedString();
        return value;
    }

    internal static string EncodeString(string value)
    {
        return BinaryLexical.FormatBase64(Encoding.UTF8.GetBytes(value));
    }

    internal static string FormatBoolean(bool value)
    {
        return value ? "1" : "0";
    }
}

internal class ArtifactManifestTextReader
{
    internal string[] Lines = [];
    internal int Index;
    internal bool Success = true;

    internal int ReadInteger()
    {
        if (!Success || Index >= Lines.Length)
        {
            Success = false;
            return 0;
        }

        IntegerLexicalResult parsed = IntegerLexical.ParseInt32(Lines[Index].AsSpan());
        Index++;
        if (!parsed.Success)
        {
            Success = false;
            return 0;
        }

        return parsed.Value;
    }

    internal bool ReadBoolean()
    {
        if (!Success || Index >= Lines.Length)
        {
            Success = false;
            return false;
        }

        string text = Lines[Index];
        Index++;
        if (text == "1")
        {
            return true;
        }

        if (text != "0")
        {
            Success = false;
        }

        return false;
    }

    internal string ReadEncodedString()
    {
        byte[] bytes = ReadEncodedBytes();
        if (!Success)
        {
            return string.Empty;
        }

        return Encoding.UTF8.GetString(bytes);
    }

    internal byte[] ReadEncodedBytes()
    {
        if (!Success || Index >= Lines.Length)
        {
            Success = false;
            return [];
        }

        IsoBinaryValue? decoded = BinaryLexical.ParseBase64(Lines[Index].AsSpan());
        Index++;
        if (decoded == null)
        {
            Success = false;
            return [];
        }

        return decoded.Value;
    }
}

internal class ArtifactManifestEncodeResult
{
    internal bool Success;
    internal string[] Lines = [];
    internal string ErrorCode = string.Empty;
    internal string ErrorMessage = string.Empty;
}

internal class ArtifactManifestDecodeResult
{
    internal bool Success;
    internal ArtifactManifest Manifest = new ArtifactManifest();
    internal string ErrorCode = string.Empty;
    internal string ErrorMessage = string.Empty;
}
