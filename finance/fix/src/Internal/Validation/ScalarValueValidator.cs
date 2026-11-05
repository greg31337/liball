using System;
using System.Collections.Generic;
using System.Text;

namespace fix;

internal static class ScalarValueValidator
{
    internal static List<ValidationIssue> ValidateScalarValues(FixMessage message, FixDictionary dictionary, ValidationOptions options)
    {
        List<ValidationIssue> issues = [];
        bool validateDatatypes = ValidationCoordinator.IsLayerEnabled(options, ValidationLayer.Datatype);
        bool validateCodeSets = ValidationCoordinator.IsLayerEnabled(options, ValidationLayer.CodeSet);
        if (!validateDatatypes && !validateCodeSets)
        {
            return issues;
        }

        List<FixField> fields = EnvelopeValidator.CreateWireOrder(message);
        FixFieldValueCodec codec = new FixFieldValueCodec();
        DecodeOptions decodeOptions = new DecodeOptions();
        decodeOptions.AllowEmptyValues = !options.RejectEmptyValues;
        decodeOptions.MaxFieldSize = int.MaxValue;
        for (int index = 0; index < fields.Count; index++)
        {
            FixField field = fields[index];
            if (EnvelopeValidator.IsEnvelopeTag(field.Tag))
            {
                continue;
            }
            FixFieldDefinition? definition = MessageBuilder.FindFieldDefinition(dictionary, field.Tag);
            if (definition == null)
            {
                continue;
            }

            bool validValue = ValidateDatatype(field, definition, codec, decodeOptions, options, validateDatatypes, issues, message.MsgType);
            if (validateCodeSets && validValue && field.Value.Length != 0 && definition.CodeSetName.Length != 0)
            {
                ValidateCodeSet(field, definition, dictionary, issues, message.MsgType);
            }
            if (validateDatatypes)
            {
                ValidateLengthDataAssociation(fields, index, field, definition, issues, message.MsgType);
            }
        }
        return issues;
    }

    internal static bool ValidateDatatype(FixField field, FixFieldDefinition definition, FixFieldValueCodec codec, DecodeOptions decodeOptions, ValidationOptions options, bool addIssue, List<ValidationIssue> issues, string msgType)
    {
        if (field.Value.Length == 0 && options.RejectEmptyValues)
        {
            if (addIssue)
            {
                AddValueIssue(issues, ValidationLayer.Datatype, "empty-field-value", "The field value is empty.", field, msgType, 0);
            }
            return false;
        }

        FieldParseResult parsed = codec.DecodeFieldValue(field.Value, field.Tag, definition.FieldType, decodeOptions);
        if (parsed.Status == DecodeStatus.Success)
        {
            return true;
        }
        if (!addIssue)
        {
            return false;
        }

        string code = ResolveDatatypeIssueCode(definition.FieldType, parsed.ErrorCode);
        string description = $"The field value is invalid for {definition.FieldType}: {parsed.ErrorCode}.";
        AddValueIssue(issues, ValidationLayer.Datatype, code, description, field, msgType, parsed.ErrorOffset);
        return false;
    }

    internal static string ResolveDatatypeIssueCode(FixFieldType fieldType, string parseErrorCode)
    {
        if (parseErrorCode == "unsupported-field-type")
        {
            return "unsupported-field-datatype";
        }
        if (FixFieldValueCodec.IsTemporalType(fieldType))
        {
            return "invalid-timestamp-value";
        }
        if (parseErrorCode == "overflow" || parseErrorCode == "precision-exceeded" || parseErrorCode == "negative-value" || parseErrorCode == "zero-not-allowed")
        {
            return "field-value-out-of-range";
        }
        return "invalid-datatype-value";
    }

    internal static void ValidateCodeSet(FixField field, FixFieldDefinition definition, FixDictionary dictionary, List<ValidationIssue> issues, string msgType)
    {
        FixCodeSetDefinition? codeSet = FindCodeSet(dictionary, definition.CodeSetName);
        if (codeSet == null)
        {
            AddValueIssue(issues, ValidationLayer.CodeSet, "unresolved-code-set", "The field references a code set that is not present in the active dictionary.", field, msgType, 0);
            return;
        }

        if (definition.FieldType == FixFieldType.MultipleCharValue || definition.FieldType == FixFieldType.MultipleStringValue)
        {
            ValidateCodeSetTokens(field, codeSet, issues, msgType);
            return;
        }
        string value = Encoding.Latin1.GetString(field.Value);
        if (!ContainsCode(codeSet, value))
        {
            AddValueIssue(issues, ValidationLayer.CodeSet, "invalid-code-set-value", "The field value is not a member of its dictionary code set.", field, msgType, 0);
        }
    }

    internal static void ValidateCodeSetTokens(FixField field, FixCodeSetDefinition codeSet, List<ValidationIssue> issues, string msgType)
    {
        int tokenStart = 0;
        for (int index = 0; index <= field.Value.Length; index++)
        {
            if (index != field.Value.Length && field.Value[index] != 32)
            {
                continue;
            }
            string token = Encoding.Latin1.GetString(field.Value, tokenStart, index - tokenStart);
            if (!ContainsCode(codeSet, token))
            {
                AddValueIssue(issues, ValidationLayer.CodeSet, "invalid-code-set-value", "A field value token is not a member of its dictionary code set.", field, msgType, tokenStart);
                return;
            }
            tokenStart = index + 1;
        }
    }

    internal static FixCodeSetDefinition? FindCodeSet(FixDictionary dictionary, string name)
    {
        StringComparison comparison = dictionary.Options.NormalizeNames ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        for (int index = 0; index < dictionary.CodeSets.Count; index++)
        {
            FixCodeSetDefinition codeSet = dictionary.CodeSets[index];
            if (!codeSet.IsRemoved && string.Equals(codeSet.Name, name, comparison))
            {
                return codeSet;
            }
        }
        return null;
    }

    internal static bool ContainsCode(FixCodeSetDefinition codeSet, string value)
    {
        for (int index = 0; index < codeSet.Codes.Count; index++)
        {
            FixCodeDefinition code = codeSet.Codes[index];
            if (!code.IsRemoved && code.Value == value)
            {
                return true;
            }
        }
        return false;
    }

    internal static void ValidateLengthDataAssociation(List<FixField> fields, int index, FixField field, FixFieldDefinition definition, List<ValidationIssue> issues, string msgType)
    {
        if (definition.HasDataTag)
        {
            if (index + 1 >= fields.Count || fields[index + 1].Tag != definition.DataTag)
            {
                AddValueIssue(issues, ValidationLayer.Datatype, "length-data-not-adjacent", "A length field is not immediately followed by its associated data field.", field, msgType, 0);
            }
            else
            {
                AsciiIntegerResult declared = AsciiDigits.ParseUnsignedInt(field.Value, int.MaxValue);
                if (declared.Success && declared.Value != fields[index + 1].Value.Length)
                {
                    AddValueIssue(issues, ValidationLayer.Datatype, "data-length-mismatch", "The associated data field byte count does not match its declared length.", fields[index + 1], msgType, 0);
                }
            }
        }
        if (definition.HasLengthTag && (index == 0 || fields[index - 1].Tag != definition.LengthTag))
        {
            AddValueIssue(issues, ValidationLayer.Datatype, "length-data-not-adjacent", "A data field is not immediately preceded by its associated length field.", field, msgType, 0);
        }
    }

    internal static void AddValueIssue(List<ValidationIssue> issues, ValidationLayer layer, string code, string description, FixField field, string msgType, int valueOffset)
    {
        ValidationIssue issue = new ValidationIssue();
        issue.Layer = layer;
        issue.Severity = ValidationSeverity.Error;
        issue.Code = code;
        issue.Message = description;
        issue.HasTag = true;
        issue.Tag = field.Tag;
        issue.MsgType = msgType;
        if (field.SourceOffset >= 0)
        {
            issue.HasSourceOffset = true;
            int relativeOffset = valueOffset < 0 ? 0 : valueOffset;
            issue.SourceOffset = field.SourceOffset + DataFieldScanner.CountDecimalDigits(field.Tag) + 1 + relativeOffset;
        }
        issues.Add(issue);
    }
}
