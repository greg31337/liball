using System;
using System.Collections.Generic;

namespace cbor;

internal class CborValidationCore
{
    internal byte[]? Source;
    internal CborDecodeOptions? Options;
    internal CborTagLookup? Tags;
    internal List<CborValidationIssue> Issues = new List<CborValidationIssue>();
    internal CborValue? Root;
    internal bool Invalid;
    internal bool Stop;
    internal int IssueLimit;

    internal CborValidationCore(byte[]? source, CborDecodeOptions? options)
    {
        Source = source;
        Options = options;
        if (options != null)
        {
            Tags = new CborTagLookup(options.TagRegistrations);
        }
    }

    internal CborValidationResult ValidateEncoding()
    {
        if (Options == null)
        {
            IssueLimit = 1;
            AddIssue(
                "CBOR_VALIDATION_OPTIONS_MISSING",
                "Validation options are required.",
                -1,
                "$"
            );
            return CreateResult();
        }
        IssueLimit = Options.MaxValidationIssues;
        if (IssueLimit < 0)
        {
            IssueLimit = 1;
            AddIssue(
                "CBOR_MAX_VALIDATION_ISSUES_INVALID",
                "The validation issue limit cannot be negative.",
                -1,
                "$"
            );
            return CreateResult();
        }
        if (!ValidateTagRegistrations())
        {
            return CreateResult();
        }
        if (Source == null)
        {
            AddIssue(
                "CBOR_SOURCE_MISSING",
                "Encoded CBOR bytes are required.",
                -1,
                "$"
            );
            return CreateResult();
        }
        int level = (int)Options.ValidationLevel;
        if (level < (int)CborValidationLevel.WellFormedness
            || level > (int)CborValidationLevel.Application)
        {
            AddIssue(
                "CBOR_VALIDATION_LEVEL_INVALID",
                "The requested validation level is not defined.",
                -1,
                "$"
            );
            return CreateResult();
        }
        if (!ParseValue())
        {
            return CreateResult();
        }
        if (Root == null)
        {
            AddIssue(
                "CBOR_VALIDATION_VALUE_MISSING",
                "Validation completed without a decoded value.",
                -1,
                "$"
            );
            return CreateResult();
        }

        if (level >= (int)CborValidationLevel.Basic)
        {
            ValidateUtf8();
            if (!Stop)
            {
                ValidateDuplicateKeys();
            }
        }
        if (!Stop && level >= (int)CborValidationLevel.TagValidity)
        {
            ValidateTree(true, level >= (int)CborValidationLevel.Application);
        }
        if (!Stop && level >= (int)CborValidationLevel.Deterministic)
        {
            ValidateDeterministicEncoding();
        }
        return CreateResult();
    }

    internal CborValidationResult ValidateValue(CborValue? value)
    {
        if (!InitializeValidation())
        {
            return CreateResult();
        }
        if (value == null)
        {
            AddIssue(
                "CBOR_VALUE_MISSING",
                "A CBOR value is required.",
                -1,
                "$"
            );
            return CreateResult();
        }
        Root = value;
        int level = Options == null
            ? -1
            : (int)Options.ValidationLevel;
        ValidateModelGraph();
        if (!Stop)
        {
            ValidateModelEncodability(level);
        }
        if (!Stop && level >= (int)CborValidationLevel.Basic)
        {
            ValidateDuplicateKeys();
        }
        if (!Stop && level >= (int)CborValidationLevel.TagValidity)
        {
            ValidateTree(true, level >= (int)CborValidationLevel.Application);
        }
        return CreateResult();
    }

    internal bool InitializeValidation()
    {
        if (Options == null)
        {
            IssueLimit = 1;
            AddIssue(
                "CBOR_VALIDATION_OPTIONS_MISSING",
                "Validation options are required.",
                -1,
                "$"
            );
            return false;
        }
        IssueLimit = Options.MaxValidationIssues;
        if (IssueLimit < 0)
        {
            IssueLimit = 1;
            AddIssue(
                "CBOR_MAX_VALIDATION_ISSUES_INVALID",
                "The validation issue limit cannot be negative.",
                -1,
                "$"
            );
            return false;
        }
        int level = (int)Options.ValidationLevel;
        if (level < (int)CborValidationLevel.WellFormedness
            || level > (int)CborValidationLevel.Application)
        {
            AddIssue(
                "CBOR_VALIDATION_LEVEL_INVALID",
                "The requested validation level is not defined.",
                -1,
                "$"
            );
            return false;
        }
        if (!ValidateTagRegistrations())
        {
            return false;
        }
        return true;
    }

    internal bool ValidateTagRegistrations()
    {
        if (Tags == null)
        {
            return true;
        }
        if (Tags.ErrorCode != null)
        {
            AddIssue(
                Tags.ErrorCode,
                Tags.ErrorMessage ?? "The tag registrations are invalid.",
                -1,
                "$"
            );
            return false;
        }
        return true;
    }

    internal void ValidateModelGraph()
    {
        if (Root == null || Options == null)
        {
            return;
        }
        List<CborValidationNode> pending = new List<CborValidationNode>();
        List<CborValue> visited = new List<CborValue>();
        pending.Add(new CborValidationNode(Root, "$"));
        while (pending.Count > 0 && !Stop)
        {
            int last = pending.Count - 1;
            CborValidationNode node = pending[last];
            pending.RemoveAt(last);
            if (ContainsIdentity(visited, node.Value))
            {
                continue;
            }
            visited.Add(node.Value);
            ValidateModelFields(node.Value, node.Path);
            if (!Stop)
            {
                ValidateModelCollectionLimits(node.Value, node.Path);
            }
            AddChildren(node.Value, node.Path, pending);
        }
    }

    internal void ValidateModelFields(CborValue value, string path)
    {
        bool integerKind = value.Kind == CborValueKind.UnsignedInteger
            || value.Kind == CborValueKind.NegativeInteger;
        bool lengthKind = value.Kind == CborValueKind.ByteString
            || value.Kind == CborValueKind.TextString
            || value.Kind == CborValueKind.Array
            || value.Kind == CborValueKind.Map;
        bool mismatch = (!integerKind && value.IntegerValue != 0)
            || (value.Kind != CborValueKind.ByteString
                && value.ByteStringValue != null)
            || (value.Kind != CborValueKind.TextString
                && value.TextStringValue != null)
            || (value.Kind != CborValueKind.Array && value.ArrayItems != null)
            || (value.Kind != CborValueKind.Map && value.MapEntries != null)
            || (value.Kind != CborValueKind.Tag
                && (value.TagNumber != 0 || value.TaggedValue != null))
            || (value.Kind != CborValueKind.Float
                && (value.FloatValue != null
                    || value.FloatWidth != CborFloatWidth.None))
            || (value.Kind != CborValueKind.SimpleValue
                && value.SimpleValue != 0)
            || (!lengthKind && value.LengthKind != CborLengthKind.None);
        if (mismatch)
        {
            AddIssue(
                "CBOR_VALUE_FIELD_MISMATCH",
                "The value contains fields that do not belong to its selected kind.",
                GetOffset(value),
                path
            );
        }
        int encodedLengthKind = (int)value.LengthKind;
        if (!Stop && (encodedLengthKind < (int)CborLengthKind.None
                || encodedLengthKind > (int)CborLengthKind.Indefinite))
        {
            AddIssue(
                "CBOR_LENGTH_KIND_INVALID",
                "The value length kind is not defined.",
                GetOffset(value),
                path
            );
        }
        if (!Stop && value.Kind == CborValueKind.Float
            && value.FloatValue != null)
        {
            int width = (int)value.FloatWidth;
            int payloadWidth = (int)value.FloatValue.Width;
            bool widthValid = width == (int)CborFloatWidth.None
                || width == (int)CborFloatWidth.Half
                || width == (int)CborFloatWidth.Single
                || width == (int)CborFloatWidth.Double;
            bool payloadWidthValid = payloadWidth == (int)CborFloatWidth.None
                || payloadWidth == (int)CborFloatWidth.Half
                || payloadWidth == (int)CborFloatWidth.Single
                || payloadWidth == (int)CborFloatWidth.Double;
            if (!widthValid || !payloadWidthValid
                || (value.FloatWidth != CborFloatWidth.None
                    && value.FloatValue.Width != CborFloatWidth.None
                    && value.FloatWidth != value.FloatValue.Width))
            {
                AddIssue(
                    "CBOR_FLOAT_WIDTH_MISMATCH",
                    "The floating-point width fields are invalid or inconsistent.",
                    GetOffset(value),
                    path
                );
            }
        }
    }

    internal void ValidateModelCollectionLimits(CborValue value, string path)
    {
        if (Options == null)
        {
            return;
        }
        if (value.Kind == CborValueKind.ByteString
            && value.ByteStringValue != null
            && (Options.MaxStringBytes < 0
                || value.ByteStringValue.Length > Options.MaxStringBytes))
        {
            AddIssue(
                "CBOR_MAX_STRING_BYTES",
                "The byte string exceeds the configured string-byte limit.",
                GetOffset(value),
                path
            );
        }
        if (!Stop && value.Kind == CborValueKind.TextString
            && value.TextStringValue != null
            && CborTokenWritePlan.IsWellFormedUtf16(value.TextStringValue)
            && (Options.MaxStringBytes < 0
                || System.Text.Encoding.UTF8.GetByteCount(value.TextStringValue)
                    > Options.MaxStringBytes))
        {
            AddIssue(
                "CBOR_MAX_STRING_BYTES",
                "The text string exceeds the configured string-byte limit.",
                GetOffset(value),
                path
            );
        }
        if (!Stop && value.Kind == CborValueKind.Array
            && value.ArrayItems != null
            && (Options.MaxArrayItems < 0
                || value.ArrayItems.Count > Options.MaxArrayItems))
        {
            AddIssue(
                "CBOR_MAX_ARRAY_ITEMS",
                "The array exceeds the configured item limit.",
                GetOffset(value),
                path
            );
        }
        if (!Stop && value.Kind == CborValueKind.Map
            && value.MapEntries != null
            && (Options.MaxMapPairs < 0
                || value.MapEntries.Count > Options.MaxMapPairs))
        {
            AddIssue(
                "CBOR_MAX_MAP_PAIRS",
                "The map exceeds the configured pair limit.",
                GetOffset(value),
                path
            );
        }
    }

    internal void ValidateModelEncodability(int level)
    {
        if (Root == null || Options == null)
        {
            return;
        }
        CborEncodeOptions encoding = new CborEncodeOptions();
        encoding.MaxNestingDepth = Options.MaxNestingDepth;
        encoding.MaxTotalItems = Options.MaxTotalItems;
        encoding.MaxOutputBytes = Options.MaxTotalBytes;
        encoding.MaxSharedValues = Options.MaxTotalItems;
        if (level >= (int)CborValidationLevel.Application)
        {
            encoding.AllowIndefiniteLength = Options.AllowIndefiniteLength;
            encoding.SharedValuePolicy = Options.SharedValuePolicy;
        }
        else
        {
            encoding.SharedValuePolicy = CborSharedValuePolicy.Resolve;
        }
        if (level >= (int)CborValidationLevel.Deterministic)
        {
            if (Options.DeterministicEncodingProfile
                    != CborEncodingProfile.CoreDeterministic
                && Options.DeterministicEncodingProfile
                    != CborEncodingProfile.LengthFirstCoreDeterministic)
            {
                AddIssue(
                    "CBOR_DETERMINISTIC_PROFILE_INVALID",
                    "Deterministic validation requires a core deterministic encoding profile.",
                    -1,
                    "$"
                );
                return;
            }
            encoding.EncodingProfile = Options.DeterministicEncodingProfile;
            encoding.SharedValuePolicy = CborSharedValuePolicy.Disabled;
        }
        CborSizeCalculator calculator = new CborSizeCalculator(encoding);
        if (!calculator.Calculate(Root))
        {
            string code = calculator.ErrorCode ?? "CBOR_VALUE_NOT_ENCODABLE";
            string message = calculator.ErrorMessage
                ?? "The value cannot be encoded.";
            if (code == "CBOR_MAX_OUTPUT_BYTES")
            {
                code = "CBOR_MAX_TOTAL_BYTES";
                message = "The value exceeds the configured total-byte limit.";
            }
            AddIssue(
                code,
                message,
                -1,
                calculator.ErrorPath ?? "$"
            );
        }
    }

    internal bool ParseValue()
    {
        if (Source == null || Options == null)
        {
            return false;
        }
        CborDecodeOptions parsingOptions = CreateParsingOptions(false);
        CborTokenizerCore tokenizer = new CborTokenizerCore(parsingOptions, null);
        CborDomBuilder builder = new CborDomBuilder();
        int sourceOffset = 0;
        while (!builder.Completed)
        {
            byte[] available = Source.AsSpan(sourceOffset).ToArray();
            CborTokenReadResult tokenResult = tokenizer.ReadToken(available);
            if (tokenResult.Status == CborReadStatus.Error)
            {
                AddIssue(
                    tokenResult.ErrorCode ?? "CBOR_WELL_FORMEDNESS_ERROR",
                    tokenResult.ErrorMessage ?? "The encoded CBOR item is not well formed.",
                    tokenResult.ErrorOffset,
                    tokenResult.ErrorPath ?? "$"
                );
                return false;
            }
            if (tokenResult.Status == CborReadStatus.NeedMoreData)
            {
                AddIssue(
                    "CBOR_UNEXPECTED_END_OF_DATA",
                    "The encoded input ended before one complete CBOR value was available.",
                    Source.Length,
                    "$"
                );
                return false;
            }
            if (tokenResult.Token == null)
            {
                AddIssue(
                    "CBOR_DECODE_TOKEN_MISSING",
                    "The tokenizer completed without producing a token.",
                    tokenizer.AbsoluteOffset,
                    "$"
                );
                return false;
            }
            if (!builder.AddToken(tokenResult.Token))
            {
                AddIssue(
                    builder.ErrorCode ?? "CBOR_DOM_BUILD_FAILED",
                    builder.ErrorMessage ?? "The decoded tokens could not form a CBOR value.",
                    tokenizer.AbsoluteOffset,
                    "$"
                );
                return false;
            }
            sourceOffset = sourceOffset + tokenResult.Consumed;
        }
        if (sourceOffset < Source.Length)
        {
            AddIssue(
                "CBOR_TRAILING_DATA",
                "Bytes remain after the complete CBOR value.",
                sourceOffset,
                "$"
            );
            return false;
        }
        Root = builder.Value;
        return true;
    }

    internal CborDecodeOptions CreateParsingOptions(bool rejectInvalidUtf8)
    {
        CborDecodeOptions sourceOptions = Options ?? new CborDecodeOptions();
        CborDecodeOptions parsing = new CborDecodeOptions();
        parsing.DuplicateKeyPolicy = CborDuplicateKeyPolicy.Preserve;
        parsing.TagPolicy = CborTagPolicy.Preserve;
        parsing.SharedValuePolicy = CborSharedValuePolicy.Preserve;
        parsing.ValidationLevel = CborValidationLevel.WellFormedness;
        parsing.AllowIndefiniteLength = true;
        parsing.AllowUnknownSimpleValues = true;
        parsing.RejectInvalidUtf8 = rejectInvalidUtf8;
        parsing.RetainRepresentation = true;
        parsing.RetainSourceRanges = true;
        parsing.MaxNestingDepth = sourceOptions.MaxNestingDepth;
        parsing.MaxTotalItems = sourceOptions.MaxTotalItems;
        parsing.MaxArrayItems = sourceOptions.MaxArrayItems;
        parsing.MaxMapPairs = sourceOptions.MaxMapPairs;
        parsing.MaxStringBytes = sourceOptions.MaxStringBytes;
        parsing.MaxTotalBytes = sourceOptions.MaxTotalBytes;
        parsing.MaxRetainedSourceBytes = int.MaxValue;
        parsing.MaxEmbeddedDepth = sourceOptions.MaxEmbeddedDepth;
        return parsing;
    }

    internal void ValidateUtf8()
    {
        if (Source == null)
        {
            return;
        }
        CborTokenizerCore tokenizer = new CborTokenizerCore(
            CreateParsingOptions(true),
            null
        );
        CborDomBuilder builder = new CborDomBuilder();
        int sourceOffset = 0;
        while (!builder.Completed)
        {
            byte[] available = Source.AsSpan(sourceOffset).ToArray();
            CborTokenReadResult tokenResult = tokenizer.ReadToken(available);
            if (tokenResult.Status == CborReadStatus.Error)
            {
                AddIssue(
                    tokenResult.ErrorCode ?? "CBOR_UTF8_INVALID",
                    tokenResult.ErrorMessage ?? "A text string is not valid UTF-8.",
                    tokenResult.ErrorOffset,
                    tokenResult.ErrorPath ?? "$"
                );
                return;
            }
            if (tokenResult.Status != CborReadStatus.Done
                || tokenResult.Token == null)
            {
                return;
            }
            if (!builder.AddToken(tokenResult.Token))
            {
                return;
            }
            sourceOffset = sourceOffset + tokenResult.Consumed;
        }
    }

    internal void ValidateDuplicateKeys()
    {
        if (Root == null)
        {
            return;
        }
        List<CborValidationNode> pending = new List<CborValidationNode>();
        List<CborValue> visited = new List<CborValue>();
        pending.Add(new CborValidationNode(Root, "$"));
        while (pending.Count > 0 && !Stop)
        {
            int last = pending.Count - 1;
            CborValidationNode node = pending[last];
            pending.RemoveAt(last);
            CborValue value = node.Value;
            if (ContainsIdentity(visited, value))
            {
                continue;
            }
            visited.Add(value);
            if (value.Kind == CborValueKind.Array && value.ArrayItems != null)
            {
                int index = value.ArrayItems.Count - 1;
                while (index >= 0)
                {
                    CborValue? item = value.ArrayItems[index];
                    if (item != null)
                    {
                        pending.Add(new CborValidationNode(
                            item,
                            $"{node.Path}[{index}]"
                        ));
                    }
                    index = index - 1;
                }
            }
            if (value.Kind == CborValueKind.Map && value.MapEntries != null)
            {
                ValidateMapDuplicates(value, node.Path);
                int index = value.MapEntries.Count - 1;
                while (index >= 0)
                {
                    CborMapEntry entry = value.MapEntries[index];
                    if (entry != null && entry.Value != null)
                    {
                        pending.Add(new CborValidationNode(
                            entry.Value,
                            $"{node.Path}[{index}].value"
                        ));
                    }
                    if (entry != null && entry.Key != null)
                    {
                        pending.Add(new CborValidationNode(
                            entry.Key,
                            $"{node.Path}[{index}].key"
                        ));
                    }
                    index = index - 1;
                }
            }
            if (value.Kind == CborValueKind.Tag && value.TaggedValue != null)
            {
                pending.Add(new CborValidationNode(
                    value.TaggedValue,
                    $"{node.Path}<tag:{value.TagNumber}>"
                ));
            }
        }
    }

    internal void ValidateMapDuplicates(CborValue map, string path)
    {
        if (map.MapEntries == null)
        {
            return;
        }
        int laterIndex = 1;
        while (laterIndex < map.MapEntries.Count && !Stop)
        {
            CborMapEntry laterEntry = map.MapEntries[laterIndex];
            if (laterEntry == null || laterEntry.Key == null)
            {
                laterIndex = laterIndex + 1;
                continue;
            }
            CborValue laterKey = laterEntry.Key;
            int earlierIndex = 0;
            while (earlierIndex < laterIndex)
            {
                CborMapEntry earlierEntry = map.MapEntries[earlierIndex];
                if (earlierEntry != null
                    && earlierEntry.Key != null
                    && CborValueComparer.AreEqual(earlierEntry.Key, laterKey))
                {
                    AddIssue(
                        "CBOR_DUPLICATE_MAP_KEY",
                        "The map contains equivalent keys.",
                        GetOffset(laterKey),
                        $"{path}[{laterIndex}].key"
                    );
                    break;
                }
                earlierIndex = earlierIndex + 1;
            }
            laterIndex = laterIndex + 1;
        }
    }

    internal void ValidateTree(bool validateTags, bool applicationPolicies)
    {
        if (Root == null || Options == null)
        {
            return;
        }
        if (applicationPolicies && !ValidateApplicationConfiguration())
        {
            return;
        }
        List<CborValidationNode> pending = new List<CborValidationNode>();
        List<CborValue> visited = new List<CborValue>();
        pending.Add(new CborValidationNode(Root, "$"));
        while (pending.Count > 0 && !Stop)
        {
            int last = pending.Count - 1;
            CborValidationNode node = pending[last];
            pending.RemoveAt(last);
            CborValue value = node.Value;
            if (ContainsIdentity(visited, value))
            {
                continue;
            }
            visited.Add(value);
            if (applicationPolicies)
            {
                ValidateApplicationValue(value, node.Path);
            }
            if (validateTags && value.Kind == CborValueKind.Tag)
            {
                ValidateTagValue(value, node.Path, applicationPolicies);
            }
            AddChildren(value, node.Path, pending);
        }
    }

    internal bool ValidateApplicationConfiguration()
    {
        if (Options == null)
        {
            return false;
        }
        int duplicatePolicy = (int)Options.DuplicateKeyPolicy;
        int tagPolicy = (int)Options.TagPolicy;
        int sharedPolicy = (int)Options.SharedValuePolicy;
        if (duplicatePolicy < (int)CborDuplicateKeyPolicy.Preserve
            || duplicatePolicy > (int)CborDuplicateKeyPolicy.KeepLast)
        {
            AddIssue(
                "CBOR_DUPLICATE_KEY_POLICY_INVALID",
                "The duplicate-key policy is not defined.",
                -1,
                "$"
            );
        }
        if (!Stop && (tagPolicy < (int)CborTagPolicy.Preserve
                || tagPolicy > (int)CborTagPolicy.Forbid))
        {
            AddIssue(
                "CBOR_TAG_POLICY_INVALID",
                "The semantic-tag policy is not defined.",
                -1,
                "$"
            );
        }
        if (!Stop && (sharedPolicy < (int)CborSharedValuePolicy.Disabled
                || sharedPolicy > (int)CborSharedValuePolicy.Resolve))
        {
            AddIssue(
                "CBOR_SHARED_VALUE_POLICY_INVALID",
                "The shared-value policy is not defined.",
                -1,
                "$"
            );
        }
        return !Stop;
    }

    internal void ValidateApplicationValue(CborValue value, string path)
    {
        if (Options == null)
        {
            return;
        }
        if (!Options.AllowIndefiniteLength
            && value.LengthKind == CborLengthKind.Indefinite)
        {
            AddIssue(
                "CBOR_INDEFINITE_LENGTH_DISABLED",
                "Indefinite-length values are disabled by the configured policy.",
                GetOffset(value),
                path
            );
        }
        if (!Stop
            && !Options.AllowUnknownSimpleValues
            && value.Kind == CborValueKind.SimpleValue
            && !CborTokenizerCore.IsKnownSimpleValue(value.SimpleValue))
        {
            AddIssue(
                "CBOR_UNKNOWN_SIMPLE_VALUE",
                $"Simple value {value.SimpleValue} is not enabled by the configured policy.",
                GetOffset(value),
                path
            );
        }
    }

    internal void ValidateTagValue(
        CborValue value,
        string path,
        bool applicationPolicies)
    {
        if (Options == null)
        {
            return;
        }
        CborTagRegistration? registration = FindRegistration(value.TagNumber);
        bool known = registration != null || IsBuiltInTag(value.TagNumber);
        if (applicationPolicies && Options.TagPolicy == CborTagPolicy.Forbid)
        {
            AddIssue(
                "CBOR_TAG_FORBIDDEN",
                "Semantic tags are forbidden by the configured policy.",
                GetOffset(value),
                path
            );
        }
        if (!Stop
            && applicationPolicies
            && Options.TagPolicy == CborTagPolicy.RejectUnknown
            && !known)
        {
            AddIssue(
                "CBOR_UNKNOWN_TAG",
                $"Semantic tag {value.TagNumber} is not registered.",
                GetOffset(value),
                path
            );
        }
        if (!Stop
            && applicationPolicies
            && Options.SharedValuePolicy == CborSharedValuePolicy.Disabled
            && IsReferenceTag(value.TagNumber))
        {
            AddIssue(
                "CBOR_REFERENCE_TAG_DISABLED",
                "Reference-related CBOR tags are disabled by the configured policy.",
                GetOffset(value),
                path
            );
        }
        if (Stop || !known || Tags == null)
        {
            return;
        }
        if (value.TaggedValue == null)
        {
            AddIssue(
                "CBOR_TAG_CONTENT_MISSING",
                $"Semantic tag {value.TagNumber} has no content value.",
                GetOffset(value),
                path
            );
            return;
        }
        CborTagValidator validator = new CborTagValidator(
            Options,
            Tags,
            (int)Options.ValidationLevel
                >= (int)CborValidationLevel.Deterministic
        );
        CborTagResult tagResult = validator.ValidateTag(
            value.TagNumber,
            value.TaggedValue,
            0
        );
        if (!tagResult.Handled)
        {
            if (registration == null)
            {
                return;
            }
            AddIssue(
                "CBOR_TAG_NOT_HANDLED",
                $"The registered handler did not handle semantic tag {value.TagNumber}.",
                GetOffset(value),
                path
            );
            return;
        }
        if (!tagResult.IsValid)
        {
            AddIssue(
                tagResult.ErrorCode ?? "CBOR_TAG_CONTENT_INVALID",
                tagResult.ErrorMessage ?? $"Semantic tag {value.TagNumber} has invalid content.",
                GetOffset(value),
                path
            );
        }
    }

    internal CborTagRegistration? FindRegistration(ulong tagNumber)
    {
        if (Tags == null)
        {
            return null;
        }
        return Tags.FindTag(tagNumber);
    }

    internal void AddChildren(
        CborValue value,
        string path,
        List<CborValidationNode> pending)
    {
        if (value.Kind == CborValueKind.Array && value.ArrayItems != null)
        {
            int index = value.ArrayItems.Count - 1;
            while (index >= 0)
            {
                CborValue? item = value.ArrayItems[index];
                if (item != null)
                {
                    pending.Add(new CborValidationNode(
                        item,
                        $"{path}[{index}]"
                    ));
                }
                index = index - 1;
            }
        }
        if (value.Kind == CborValueKind.Map && value.MapEntries != null)
        {
            int index = value.MapEntries.Count - 1;
            while (index >= 0)
            {
                CborMapEntry entry = value.MapEntries[index];
                if (entry != null && entry.Value != null)
                {
                    pending.Add(new CborValidationNode(
                        entry.Value,
                        $"{path}[{index}].value"
                    ));
                }
                if (entry != null && entry.Key != null)
                {
                    pending.Add(new CborValidationNode(
                        entry.Key,
                        $"{path}[{index}].key"
                    ));
                }
                index = index - 1;
            }
        }
        if (value.Kind == CborValueKind.Tag && value.TaggedValue != null)
        {
            pending.Add(new CborValidationNode(
                value.TaggedValue,
                $"{path}<tag:{value.TagNumber}>"
            ));
        }
    }

    internal static bool ContainsIdentity(List<CborValue> values, CborValue value)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (object.ReferenceEquals(values[index], value))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal void ValidateDeterministicEncoding()
    {
        if (Root == null || Options == null)
        {
            return;
        }
        CborMapKeyOrder mapOrder;
        if (Options.DeterministicEncodingProfile
            == CborEncodingProfile.CoreDeterministic)
        {
            mapOrder = CborMapKeyOrder.Lexicographic;
        }
        else if (Options.DeterministicEncodingProfile
            == CborEncodingProfile.LengthFirstCoreDeterministic)
        {
            mapOrder = CborMapKeyOrder.LengthFirst;
        }
        else
        {
            AddIssue(
                "CBOR_DETERMINISTIC_PROFILE_INVALID",
                "Deterministic validation requires a core deterministic encoding profile.",
                -1,
                "$"
            );
            return;
        }

        List<CborValidationNode> pending = new List<CborValidationNode>();
        List<CborValue> visited = new List<CborValue>();
        pending.Add(new CborValidationNode(Root, "$"));
        while (pending.Count > 0 && !Stop)
        {
            int last = pending.Count - 1;
            CborValidationNode node = pending[last];
            pending.RemoveAt(last);
            if (ContainsIdentity(visited, node.Value))
            {
                continue;
            }
            visited.Add(node.Value);
            ValidateDeterministicValue(node.Value, node.Path);
            if (!Stop && node.Value.Kind == CborValueKind.Map)
            {
                ValidateDeterministicMap(node.Value, node.Path, mapOrder);
            }
            AddChildren(node.Value, node.Path, pending);
        }
    }

    internal void ValidateDeterministicValue(CborValue value, string path)
    {
        if (value.LengthKind == CborLengthKind.Indefinite)
        {
            AddIssue(
                "CBOR_DETERMINISTIC_INDEFINITE_LENGTH",
                "Core deterministic encoding does not permit indefinite-length values.",
                GetOffset(value),
                path
            );
        }
        if (Stop)
        {
            return;
        }
        if (value.Kind == CborValueKind.Float)
        {
            ValidateDeterministicFloat(value, path);
            return;
        }
        ValidateDeterministicHead(value, path);
    }

    internal void ValidateDeterministicHead(CborValue value, string path)
    {
        CborRepresentation? representation = value.Representation;
        if (representation == null)
        {
            return;
        }
        bool hasArgument = true;
        ulong argument = 0;
        switch (value.Kind)
        {
            case CborValueKind.UnsignedInteger:
                argument = (ulong)value.IntegerValue;
                break;
            case CborValueKind.NegativeInteger:
                argument = (ulong)(-System.Numerics.BigInteger.One - value.IntegerValue);
                break;
            case CborValueKind.ByteString:
                if (value.LengthKind == CborLengthKind.Indefinite
                    || value.ByteStringValue == null)
                {
                    hasArgument = false;
                }
                else
                {
                    argument = (ulong)value.ByteStringValue.Length;
                }
                break;
            case CborValueKind.TextString:
                if (value.LengthKind == CborLengthKind.Indefinite
                    || representation.SourceLength < representation.HeadArgumentWidth + 1)
                {
                    hasArgument = false;
                }
                else
                {
                    argument = (ulong)(representation.SourceLength
                        - representation.HeadArgumentWidth
                        - 1);
                }
                break;
            case CborValueKind.Array:
                if (value.LengthKind == CborLengthKind.Indefinite
                    || value.ArrayItems == null)
                {
                    hasArgument = false;
                }
                else
                {
                    argument = (ulong)value.ArrayItems.Count;
                }
                break;
            case CborValueKind.Map:
                if (value.LengthKind == CborLengthKind.Indefinite
                    || value.MapEntries == null)
                {
                    hasArgument = false;
                }
                else
                {
                    argument = (ulong)value.MapEntries.Count;
                }
                break;
            case CborValueKind.Tag:
                argument = value.TagNumber;
                break;
            case CborValueKind.SimpleValue:
                argument = value.SimpleValue;
                break;
            default:
                hasArgument = false;
                break;
        }
        if (!hasArgument)
        {
            return;
        }
        int expectedWidth = GetMinimalArgumentWidth(argument);
        if (representation.HeadArgumentWidth != expectedWidth)
        {
            AddIssue(
                "CBOR_DETERMINISTIC_NON_MINIMAL_HEAD",
                "The integer, length, tag, or simple-value head is not minimally encoded.",
                GetOffset(value),
                path
            );
        }
    }

    internal void ValidateDeterministicFloat(CborValue value, string path)
    {
        if (value.FloatValue == null || value.Representation == null)
        {
            return;
        }
        double numericValue = value.FloatValue.Value;
        if (double.IsNaN(numericValue))
        {
            if (value.FloatWidth != CborFloatWidth.Half
                || value.Representation.FloatBits != 0x7e00UL)
            {
                AddIssue(
                    "CBOR_DETERMINISTIC_NAN",
                    "Core deterministic encoding requires the canonical half-precision NaN.",
                    GetOffset(value),
                    path
                );
            }
            return;
        }

        CborFloatWidth preferredWidth;
        if (CborHalfPrecision.IsExactlyRepresentableAsHalf(numericValue))
        {
            preferredWidth = CborFloatWidth.Half;
        }
        else if (CborHalfPrecision.IsExactlyRepresentableAsSingle(numericValue))
        {
            preferredWidth = CborFloatWidth.Single;
        }
        else
        {
            preferredWidth = CborFloatWidth.Double;
        }
        if (value.FloatWidth != preferredWidth)
        {
            AddIssue(
                "CBOR_DETERMINISTIC_FLOAT_WIDTH",
                "The floating-point value is not encoded in its shortest lossless width.",
                GetOffset(value),
                path
            );
        }
    }

    internal void ValidateDeterministicMap(
        CborValue map,
        string path,
        CborMapKeyOrder mapOrder)
    {
        if (Source == null || map.MapEntries == null)
        {
            return;
        }
        int index = 1;
        while (index < map.MapEntries.Count && !Stop)
        {
            CborValue? previousKey = map.MapEntries[index - 1].Key;
            CborValue? currentKey = map.MapEntries[index].Key;
            bool equivalent = CborValueComparer.AreEqual(previousKey, currentKey);
            if (equivalent)
            {
                AddIssue(
                    "CBOR_DETERMINISTIC_DUPLICATE_KEY",
                    "Core deterministic maps cannot contain equivalent keys.",
                    GetOffset(currentKey),
                    $"{path}[{index}].key"
                );
            }
            if (!Stop
                && previousKey != null
                && previousKey.Representation != null
                && currentKey != null
                && currentKey.Representation != null
                && HasSourceRange(previousKey)
                && HasSourceRange(currentKey))
            {
                int previousOffset = (int)previousKey.Representation.SourceOffset;
                int previousLength = (int)previousKey.Representation.SourceLength;
                int currentOffset = (int)currentKey.Representation.SourceOffset;
                int currentLength = (int)currentKey.Representation.SourceLength;
                int comparison = CborKeyComparer.CompareDeterministicEncoding(
                    Source.AsSpan(previousOffset, previousLength),
                    Source.AsSpan(currentOffset, currentLength),
                    mapOrder
                );
                if (comparison == 0 && !equivalent)
                {
                    AddIssue(
                        "CBOR_DETERMINISTIC_DUPLICATE_KEY",
                        "Core deterministic maps cannot contain identical encoded keys.",
                        GetOffset(currentKey),
                        $"{path}[{index}].key"
                    );
                }
                else if (comparison > 0)
                {
                    AddIssue(
                        "CBOR_DETERMINISTIC_MAP_KEY_ORDER",
                        "The map keys are not in deterministic encoded-byte order.",
                        GetOffset(currentKey),
                        $"{path}[{index}].key"
                    );
                }
            }
            index = index + 1;
        }
    }

    internal static int GetMinimalArgumentWidth(ulong argument)
    {
        if (argument < 24)
        {
            return 0;
        }
        if (argument <= byte.MaxValue)
        {
            return 1;
        }
        if (argument <= ushort.MaxValue)
        {
            return 2;
        }
        if (argument <= uint.MaxValue)
        {
            return 4;
        }
        return 8;
    }

    internal bool HasSourceRange(CborValue? value)
    {
        if (Source == null
            || value == null
            || value.Representation == null
            || !value.Representation.HasSourceRange
            || value.Representation.SourceOffset < 0
            || value.Representation.SourceLength < 0
            || value.Representation.SourceOffset > Source.Length
            || value.Representation.SourceLength
                > Source.Length - value.Representation.SourceOffset)
        {
            return false;
        }
        return true;
    }

    internal void AddIssue(
        string code,
        string message,
        long byteOffset,
        string path)
    {
        Invalid = true;
        if (Issues.Count < IssueLimit)
        {
            CborValidationIssue issue = new CborValidationIssue();
            issue.Severity = CborIssueSeverity.Error;
            issue.Code = code;
            issue.Message = message;
            issue.ByteOffset = byteOffset;
            issue.Path = path;
            Issues.Add(issue);
        }
        if (Options == null
            || !Options.CollectValidationIssues
            || Issues.Count >= IssueLimit)
        {
            Stop = true;
        }
    }

    internal CborValidationResult CreateResult()
    {
        CborValidationResult result = new CborValidationResult();
        result.IsValid = !Invalid;
        result.Issues = Issues.ToArray();
        return result;
    }

    internal static long GetOffset(CborValue? value)
    {
        if (value == null
            || value.Representation == null
            || !value.Representation.HasSourceRange)
        {
            return -1;
        }
        return value.Representation.SourceOffset;
    }

    internal static bool IsReferenceTag(ulong tagNumber)
    {
        return tagNumber == CborTagNumbers.StringReference
            || tagNumber == CborTagNumbers.StringReferenceNamespace
            || tagNumber == CborTagNumbers.ShareableValue
            || tagNumber == CborTagNumbers.SharedValueReference;
    }

    internal static bool IsBuiltInTag(ulong tagNumber)
    {
        switch (tagNumber)
        {
            case CborTagNumbers.DateTimeString:
            case CborTagNumbers.EpochDateTime:
            case CborTagNumbers.PositiveBignum:
            case CborTagNumbers.NegativeBignum:
            case CborTagNumbers.DecimalFraction:
            case CborTagNumbers.Bigfloat:
            case CborTagNumbers.ExpectedBase64Url:
            case CborTagNumbers.ExpectedBase64:
            case CborTagNumbers.ExpectedBase16:
            case CborTagNumbers.EncodedCbor:
            case CborTagNumbers.StringReference:
            case CborTagNumbers.ShareableValue:
            case CborTagNumbers.SharedValueReference:
            case CborTagNumbers.Uri:
            case CborTagNumbers.Base64Url:
            case CborTagNumbers.Base64:
            case CborTagNumbers.RegularExpression:
            case CborTagNumbers.MimeMessage:
            case CborTagNumbers.StringReferenceNamespace:
            case CborTagNumbers.SelfDescribedCbor:
                return true;
            default:
                return false;
        }
    }
}
