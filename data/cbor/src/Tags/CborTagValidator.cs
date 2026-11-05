using System.Collections.Generic;

namespace cbor;

internal class CborTagValidator
{
    internal CborDecodeOptions Options;
    internal CborTagLookup Tags;
    internal bool RequirePreferredBignums;

    internal CborTagValidator(
        CborDecodeOptions options,
        CborTagLookup tags,
        bool requirePreferredBignums)
    {
        Options = options;
        Tags = tags;
        RequirePreferredBignums = requirePreferredBignums;
    }

    internal CborTagResult ValidateTag(
        ulong tagNumber,
        CborValue content,
        int embeddedDepth)
    {
        CborTagRegistration? registration = Tags.FindTag(tagNumber);
        if (registration == null)
        {
            registration = CborBuiltInTags.FindRegistration(tagNumber);
        }
        if (registration == null)
        {
            return CborBuiltInTags.Unhandled();
        }
        if (!registration.ValidateContent)
        {
            return CborBuiltInTags.Valid(null);
        }
        if (registration.Handler == null)
        {
            return CborBuiltInTags.Invalid(
                "CBOR_TAG_HANDLER_MISSING",
                $"Semantic tag {tagNumber} has no validation handler."
            );
        }
        CborTagResult result = registration.Handler.ValidateTag(
            tagNumber,
            content
        );
        if (!result.Handled || !result.IsValid)
        {
            return result;
        }
        if (RequirePreferredBignums
            && (tagNumber == CborTagNumbers.PositiveBignum
                || tagNumber == CborTagNumbers.NegativeBignum))
        {
            result = ValidatePreferredBignum(content);
        }
        if (result.IsValid && tagNumber == CborTagNumbers.EncodedCbor)
        {
            result = ValidateEmbedded(content, embeddedDepth);
        }
        return result;
    }

    internal CborTagResult ValidatePreferredBignum(CborValue content)
    {
        if (content.ByteStringValue == null)
        {
            return CborBuiltInTags.Invalid(
                "CBOR_BIGNUM_TYPE",
                "A bignum tag requires a byte string."
            );
        }
        byte[] bytes = content.ByteStringValue;
        if (bytes.Length <= 8 || bytes[0] == 0)
        {
            return CborBuiltInTags.Invalid(
                "CBOR_BIGNUM_NOT_PREFERRED",
                "Preferred serialization uses a basic integer when possible and omits leading zero bytes."
            );
        }
        return CborBuiltInTags.Valid(null);
    }

    internal CborTagResult ValidateEmbedded(CborValue content, int embeddedDepth)
    {
        if (Options.MaxEmbeddedDepth < 0
            || embeddedDepth >= Options.MaxEmbeddedDepth)
        {
            return CborBuiltInTags.Invalid(
                "CBOR_MAX_EMBEDDED_DEPTH",
                "The encoded-CBOR tag exceeds the configured embedded-depth limit."
            );
        }
        byte[] bytes = content.ByteStringValue ?? [];
        if (bytes.Length == 0)
        {
            return CborBuiltInTags.Invalid(
                "CBOR_EMBEDDED_CBOR_ITEM_COUNT",
                "The encoded-CBOR byte string must contain exactly one data item."
            );
        }
        CborReadResult decoded = new CborDecoder().Decode(
            bytes,
            CreateEmbeddedOptions()
        );
        if (decoded.Status != CborReadStatus.Done || decoded.Value == null)
        {
            if (decoded.ErrorCode == "CBOR_TRAILING_DATA"
                || decoded.ErrorCode == "CBOR_UNEXPECTED_END_OF_DATA")
            {
                return CborBuiltInTags.Invalid(
                    "CBOR_EMBEDDED_CBOR_ITEM_COUNT",
                    "The encoded-CBOR byte string must contain exactly one data item."
                );
            }
            return CborBuiltInTags.Invalid(
                decoded.ErrorCode ?? "CBOR_EMBEDDED_CBOR_INVALID",
                decoded.ErrorMessage ?? "The embedded CBOR data item is not well formed."
            );
        }
        return ValidateGraph(decoded.Value, embeddedDepth + 1);
    }

    internal CborTagResult ValidateGraph(CborValue root, int embeddedDepth)
    {
        List<CborValue> pending = new List<CborValue>();
        pending.Add(root);
        while (pending.Count > 0)
        {
            int last = pending.Count - 1;
            CborValue value = pending[last];
            pending.RemoveAt(last);
            if (value.Kind == CborValueKind.Tag && value.TaggedValue != null)
            {
                CborTagResult tagResult = ValidateTag(
                    value.TagNumber,
                    value.TaggedValue,
                    embeddedDepth
                );
                if (tagResult.Handled && !tagResult.IsValid)
                {
                    return tagResult;
                }
                pending.Add(value.TaggedValue);
            }
            if (value.Kind == CborValueKind.Array && value.ArrayItems != null)
            {
                int index = 0;
                while (index < value.ArrayItems.Count)
                {
                    pending.Add(value.ArrayItems[index]);
                    index = index + 1;
                }
            }
            if (value.Kind == CborValueKind.Map && value.MapEntries != null)
            {
                int index = 0;
                while (index < value.MapEntries.Count)
                {
                    CborMapEntry entry = value.MapEntries[index];
                    if (entry.Key != null)
                    {
                        pending.Add(entry.Key);
                    }
                    if (entry.Value != null)
                    {
                        pending.Add(entry.Value);
                    }
                    index = index + 1;
                }
            }
        }
        return CborBuiltInTags.Valid(null);
    }

    internal CborDecodeOptions CreateEmbeddedOptions()
    {
        CborDecodeOptions embedded = new CborDecodeOptions();
        embedded.TagPolicy = CborTagPolicy.Preserve;
        embedded.SharedValuePolicy = CborSharedValuePolicy.Preserve;
        embedded.AllowIndefiniteLength = Options.AllowIndefiniteLength;
        embedded.AllowUnknownSimpleValues = Options.AllowUnknownSimpleValues;
        embedded.RejectInvalidUtf8 = Options.RejectInvalidUtf8;
        embedded.MaxNestingDepth = Options.MaxNestingDepth;
        embedded.MaxTotalItems = Options.MaxTotalItems;
        embedded.MaxArrayItems = Options.MaxArrayItems;
        embedded.MaxMapPairs = Options.MaxMapPairs;
        embedded.MaxStringBytes = Options.MaxStringBytes;
        embedded.MaxTotalBytes = Options.MaxTotalBytes;
        embedded.MaxRetainedSourceBytes = Options.MaxRetainedSourceBytes;
        embedded.MaxEmbeddedDepth = Options.MaxEmbeddedDepth;
        embedded.TagRegistrations = new List<CborTagRegistration>();
        int index = 0;
        while (index < Tags.Registrations.Count)
        {
            embedded.TagRegistrations.Add(Tags.Registrations[index]);
            index = index + 1;
        }
        return embedded;
    }
}
