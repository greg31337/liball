using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace cbor;

internal class CborSharingDecoder
{
    internal CborDecodeOptions Options;
    internal CborTagLookup Tags;
    internal List<CborValue> SharedValues = new List<CborValue>();
    internal List<List<CborValue>> StringNamespaces = new List<List<CborValue>>();
    internal CborValue? Result;
    internal bool Failed;
    internal string? ErrorCode;
    internal string? ErrorMessage;
    internal long ErrorOffset = -1;
    internal string? ErrorPath;

    internal CborSharingDecoder(CborDecodeOptions options, CborTagLookup tags)
    {
        Options = options;
        Tags = tags;
    }

    internal bool Process(CborValue value)
    {
        SharedValues.Clear();
        StringNamespaces.Clear();
        Result = null;
        Failed = false;
        ErrorCode = null;
        ErrorMessage = null;
        ErrorOffset = -1;
        ErrorPath = null;
        if (Tags.ErrorCode != null)
        {
            return SetFailureAtRoot(
                Tags.ErrorCode,
                Tags.ErrorMessage ?? "The tag registrations are invalid."
            );
        }
        int tagPolicy = (int)Options.TagPolicy;
        if (tagPolicy < (int)CborTagPolicy.Preserve
            || tagPolicy > (int)CborTagPolicy.Forbid)
        {
            return SetFailureAtRoot(
                "CBOR_TAG_POLICY_INVALID",
                "The semantic-tag policy is not defined."
            );
        }
        if (!ProcessValue(value))
        {
            return false;
        }
        Result = value;
        return true;
    }

    internal bool ProcessValue(CborValue value)
    {
        switch (value.Kind)
        {
            case CborValueKind.ByteString:
            case CborValueKind.TextString:
                return ProcessString(value);
            case CborValueKind.Array:
                return ProcessArray(value);
            case CborValueKind.Map:
                return ProcessMap(value);
            case CborValueKind.Tag:
                return ProcessTag(value);
            default:
                return true;
        }
    }

    internal bool ProcessArray(CborValue value)
    {
        if (value.ArrayItems == null)
        {
            return SetFailure(
                "CBOR_REFERENCE_ARRAY_INVALID",
                "The decoded array has no item collection.",
                value
            );
        }
        for (int index = 0; index < value.ArrayItems.Count; index++)
        {
            if (!ProcessValue(value.ArrayItems[index]))
            {
                return false;
            }
        }
        return true;
    }

    internal bool ProcessMap(CborValue value)
    {
        if (value.MapEntries == null)
        {
            return SetFailure(
                "CBOR_REFERENCE_MAP_INVALID",
                "The decoded map has no entry collection.",
                value
            );
        }
        for (int index = 0; index < value.MapEntries.Count; index++)
        {
            CborMapEntry entry = value.MapEntries[index];
            if (entry.Key == null || entry.Value == null)
            {
                return SetFailure(
                    "CBOR_REFERENCE_MAP_ENTRY_INVALID",
                    "The decoded map contains an incomplete entry.",
                    value
                );
            }
            if (!ProcessValue(entry.Key) || !ProcessValue(entry.Value))
            {
                return false;
            }
        }
        return true;
    }

    internal bool ProcessTag(CborValue value)
    {
        CborTagRegistration? registration = Tags.FindTag(value.TagNumber);
        bool known = registration != null
            || CborValidationCore.IsBuiltInTag(value.TagNumber);
        if (Options.TagPolicy == CborTagPolicy.Forbid)
        {
            return SetFailure(
                "CBOR_TAG_FORBIDDEN",
                "Semantic tags are forbidden by the decoding options.",
                value
            );
        }
        if (Options.TagPolicy == CborTagPolicy.RejectUnknown && !known)
        {
            return SetFailure(
                "CBOR_UNKNOWN_TAG",
                $"Semantic tag {value.TagNumber} is not registered.",
                value
            );
        }
        if (value.TaggedValue == null)
        {
            return SetFailure(
                "CBOR_TAG_CONTENT_MISSING",
                "The decoded tag has no content value.",
                value
            );
        }
        if (Options.TagPolicy != CborTagPolicy.Preserve
            && known
            && !ValidateKnownTag(value))
        {
            return false;
        }
        bool isReferenceTag = value.TagNumber == CborTagNumbers.StringReference
            || value.TagNumber == CborTagNumbers.StringReferenceNamespace
            || value.TagNumber == CborTagNumbers.ShareableValue
            || value.TagNumber == CborTagNumbers.SharedValueReference;
        if (isReferenceTag && Options.SharedValuePolicy == CborSharedValuePolicy.Disabled)
        {
            return SetFailure(
                "CBOR_REFERENCE_TAG_DISABLED",
                "Reference-related CBOR tags are disabled by the decoding options.",
                value
            );
        }
        if (value.TagNumber == CborTagNumbers.StringReferenceNamespace)
        {
            return ProcessStringNamespace(value);
        }
        if (value.TagNumber == CborTagNumbers.StringReference)
        {
            return ProcessStringReference(value);
        }
        if (value.TagNumber == CborTagNumbers.ShareableValue)
        {
            return ProcessShareableValue(value);
        }
        if (value.TagNumber == CborTagNumbers.SharedValueReference)
        {
            return ProcessSharedValueReference(value);
        }
        return ProcessValue(value.TaggedValue);
    }

    internal bool ValidateKnownTag(CborValue value)
    {
        if (value.TaggedValue == null)
        {
            return SetFailure(
                "CBOR_TAG_CONTENT_MISSING",
                $"Semantic tag {value.TagNumber} has no content value.",
                value
            );
        }
        CborTagValidator validator = new CborTagValidator(
            Options,
            Tags,
            false
        );
        CborTagResult result = validator.ValidateTag(
            value.TagNumber,
            value.TaggedValue,
            0
        );
        if (!result.Handled)
        {
            if (Tags.FindTag(value.TagNumber) == null)
            {
                return true;
            }
            return SetFailure(
                "CBOR_TAG_NOT_HANDLED",
                $"The registered handler did not handle semantic tag {value.TagNumber}.",
                value
            );
        }
        if (!result.IsValid)
        {
            return SetFailure(
                result.ErrorCode ?? "CBOR_TAG_CONTENT_INVALID",
                result.ErrorMessage
                    ?? $"Semantic tag {value.TagNumber} has invalid content.",
                value
            );
        }
        return true;
    }

    internal bool ProcessShareableValue(CborValue value)
    {
        if (value.TaggedValue == null)
        {
            return SetFailure(
                "CBOR_SHAREABLE_VALUE_MISSING",
                "A shareable-value tag requires a content value.",
                value
            );
        }
        if (SharedValues.Count == int.MaxValue)
        {
            return SetFailure(
                "CBOR_SHARED_VALUE_INDEX_OUT_OF_RANGE",
                "The shared-value table exceeds the supported index range.",
                value
            );
        }
        int sharedIndex = SharedValues.Count;
        SharedValues.Add(value.TaggedValue);
        if (Options.SharedValuePolicy == CborSharedValuePolicy.Resolve)
        {
            value.TaggedValue.IsSharedValue = true;
            value.TaggedValue.SharedValueIndex = sharedIndex;
        }
        return ProcessValue(value.TaggedValue);
    }

    internal bool ProcessSharedValueReference(CborValue value)
    {
        int sharedIndex = ReadReferenceIndex(
            value,
            "CBOR_SHARED_REFERENCE_TYPE",
            "CBOR_SHARED_REFERENCE_INDEX_OUT_OF_RANGE"
        );
        if (sharedIndex < 0)
        {
            return false;
        }
        if (sharedIndex >= SharedValues.Count)
        {
            return SetFailure(
                "CBOR_SHARED_REFERENCE_NOT_FOUND",
                "The shared-value reference does not identify a previously marked value.",
                value
            );
        }
        if (Options.SharedValuePolicy == CborSharedValuePolicy.Resolve)
        {
            value.SharedValueIndex = sharedIndex;
            value.ReferencedValue = SharedValues[sharedIndex];
        }
        return true;
    }

    internal bool ProcessStringNamespace(CborValue value)
    {
        if (value.TaggedValue == null)
        {
            return SetFailure(
                "CBOR_STRING_NAMESPACE_VALUE_MISSING",
                "A string-reference namespace requires a content value.",
                value
            );
        }
        List<CborValue> stringTable = new List<CborValue>();
        StringNamespaces.Add(stringTable);
        bool processed = ProcessValue(value.TaggedValue);
        StringNamespaces.RemoveAt(StringNamespaces.Count - 1);
        return processed;
    }

    internal bool ProcessStringReference(CborValue value)
    {
        if (StringNamespaces.Count == 0)
        {
            return SetFailure(
                "CBOR_STRING_REFERENCE_OUTSIDE_NAMESPACE",
                "A string reference is valid only inside a string-reference namespace.",
                value
            );
        }
        int stringIndex = ReadReferenceIndex(
            value,
            "CBOR_STRING_REFERENCE_TYPE",
            "CBOR_STRING_REFERENCE_INDEX_OUT_OF_RANGE"
        );
        if (stringIndex < 0)
        {
            return false;
        }
        List<CborValue> stringTable = StringNamespaces[StringNamespaces.Count - 1];
        if (stringIndex >= stringTable.Count)
        {
            return SetFailure(
                "CBOR_STRING_REFERENCE_NOT_FOUND",
                "The string reference does not identify a previously registered string.",
                value
            );
        }
        if (Options.SharedValuePolicy == CborSharedValuePolicy.Resolve)
        {
            value.SharedValueIndex = stringIndex;
            value.ReferencedValue = stringTable[stringIndex];
        }
        return true;
    }

    internal bool ProcessString(CborValue value)
    {
        if (StringNamespaces.Count == 0 || value.LengthKind == CborLengthKind.Indefinite)
        {
            return true;
        }
        int byteLength;
        if (value.Kind == CborValueKind.ByteString)
        {
            if (value.ByteStringValue == null)
            {
                return SetFailure(
                    "CBOR_STRING_REFERENCE_VALUE_INVALID",
                    "A decoded byte string has no byte value.",
                    value
                );
            }
            byteLength = value.ByteStringValue.Length;
        }
        else
        {
            if (value.TextStringValue == null)
            {
                return SetFailure(
                    "CBOR_STRING_REFERENCE_VALUE_INVALID",
                    "A decoded text string has no text value.",
                    value
                );
            }
            byteLength = Encoding.UTF8.GetByteCount(value.TextStringValue);
        }

        List<CborValue> stringTable = StringNamespaces[StringNamespaces.Count - 1];
        int minimumLength = GetStringMinimumLength(stringTable.Count);
        if (byteLength >= minimumLength)
        {
            stringTable.Add(value);
        }
        return true;
    }

    internal int ReadReferenceIndex(
        CborValue value,
        string typeErrorCode,
        string rangeErrorCode)
    {
        if (value.TaggedValue == null
            || value.TaggedValue.Kind != CborValueKind.UnsignedInteger)
        {
            SetFailure(
                typeErrorCode,
                "A reference tag requires an unsigned integer index.",
                value
            );
            return -1;
        }
        BigInteger indexValue = value.TaggedValue.IntegerValue;
        if (indexValue < BigInteger.Zero || indexValue > new BigInteger(int.MaxValue))
        {
            SetFailure(
                rangeErrorCode,
                "The reference index exceeds the supported range.",
                value
            );
            return -1;
        }
        return (int)indexValue;
    }

    internal static int GetStringMinimumLength(int stringIndex)
    {
        if (stringIndex <= 23)
        {
            return 3;
        }
        if (stringIndex <= 255)
        {
            return 4;
        }
        if (stringIndex <= 65535)
        {
            return 5;
        }
        return 7;
    }

    internal bool SetFailure(string errorCode, string errorMessage, CborValue value)
    {
        Failed = true;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        ErrorOffset = GetSourceOffset(value);
        ErrorPath = "$";
        return false;
    }

    internal bool SetFailureAtRoot(string errorCode, string errorMessage)
    {
        Failed = true;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        ErrorOffset = -1;
        ErrorPath = "$";
        return false;
    }

    internal static long GetSourceOffset(CborValue value)
    {
        if (value.Representation == null || !value.Representation.HasSourceRange)
        {
            return -1;
        }
        return value.Representation.SourceOffset;
    }
}
