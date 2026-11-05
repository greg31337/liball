using System.Collections.Generic;
using System.Text;

namespace fix;

internal static class EnvelopeValidator
{
    internal static List<ValidationIssue> ValidateEnvelopeAndSession(FixMessage message, FixDictionary dictionary, ValidationOptions options, ValidationContext context)
    {
        List<ValidationIssue> issues = [];
        ValidateConfiguredLimits(message, options, issues);
        if (ValidationCoordinator.IsLayerEnabled(options, ValidationLayer.Envelope))
        {
            ValidateMandatoryEnvelope(message, options, issues);
            ValidateEnvelopeCalculations(message, issues);
        }
        if (ValidationCoordinator.IsLayerEnabled(options, ValidationLayer.SessionHeader))
        {
            ValidateVersionExpectations(message, dictionary, context, issues);
            ValidateRequiredSessionMembers(message, dictionary, options, issues);
            ValidateSessionDuplicates(message.HeaderFields, options, issues, true, message.MsgType);
            ValidateSessionDuplicates(message.TrailerFields, options, issues, false, message.MsgType);
            ValidateSessionEmptyValues(message.HeaderFields, options, issues, message.MsgType);
            ValidateSessionEmptyValues(message.TrailerFields, options, issues, message.MsgType);
        }
        return issues;
    }

    internal static void ValidateConfiguredLimits(FixMessage message, ValidationOptions options, List<ValidationIssue> issues)
    {
        ValidationLayer layer = ValidationLayer.Envelope;
        if (!ValidationCoordinator.IsLayerEnabled(options, layer))
        {
            return;
        }
        if (options.MaxMessageSize < 0 || options.MaxFieldSize < 0 || options.MaxFieldsPerMessage < 0)
        {
            AddValidationIssue(issues, layer, ValidationSeverity.Fatal, "invalid-validation-limit", "A configured message validation limit is negative.", false, 0, null, message.MsgType);
            return;
        }
        int fieldCount = message.HeaderFields.Count + message.BodyFields.Count + message.TrailerFields.Count;
        if (fieldCount > options.MaxFieldsPerMessage)
        {
            AddValidationIssue(issues, layer, ValidationSeverity.Fatal, "field-count-limit-exceeded", "The message contains more fields than the configured validation limit.", false, 0, null, message.MsgType);
        }
        int encodedSize = CalculateEncodedSize(message);
        if (encodedSize > options.MaxMessageSize)
        {
            AddValidationIssue(issues, layer, ValidationSeverity.Fatal, "message-size-limit-exceeded", "The message exceeds the configured validation size limit.", false, 0, null, message.MsgType);
        }
        ValidateFieldValueLimits(message.HeaderFields, options, issues, message.MsgType);
        ValidateFieldValueLimits(message.BodyFields, options, issues, message.MsgType);
        ValidateFieldValueLimits(message.TrailerFields, options, issues, message.MsgType);
    }

    internal static void ValidateMandatoryEnvelope(FixMessage message, ValidationOptions options, List<ValidationIssue> issues)
    {
        List<FixField> ordered = CreateWireOrder(message);
        ValidateEnvelopeField(message, ordered, FixTags.BeginString, 0, false, options, issues);
        ValidateEnvelopeField(message, ordered, FixTags.BodyLength, 1, false, options, issues);
        ValidateEnvelopeField(message, ordered, FixTags.MsgType, 2, false, options, issues);
        ValidateEnvelopeField(message, ordered, FixTags.CheckSum, ordered.Count - 1, true, options, issues);

        FixField? begin = FindFirstMessageField(message, FixTags.BeginString);
        if (begin != null && message.BeginString.Length != 0 && Encoding.Latin1.GetString(begin.Value) != message.BeginString)
        {
            AddValidationIssue(issues, ValidationLayer.Envelope, ValidationSeverity.Error, "begin-string-value-mismatch", "The BeginString field does not match the decoded message value.", true, FixTags.BeginString, begin, message.MsgType);
        }
        FixField? msgType = FindFirstMessageField(message, FixTags.MsgType);
        if (msgType != null && message.MsgType.Length != 0 && Encoding.Latin1.GetString(msgType.Value) != message.MsgType)
        {
            AddValidationIssue(issues, ValidationLayer.Envelope, ValidationSeverity.Error, "msg-type-value-mismatch", "The MsgType field does not match the decoded message value.", true, FixTags.MsgType, msgType, message.MsgType);
        }
    }

    internal static void ValidateEnvelopeField(FixMessage message, List<FixField> ordered, int tag, int expectedPosition, bool finalPosition, ValidationOptions options, List<ValidationIssue> issues)
    {
        int count = CountMessageFields(message, tag);
        FixField? field = FindFirstMessageField(message, tag);
        if (count == 0)
        {
            AddValidationIssue(issues, ValidationLayer.Envelope, ValidationSeverity.Error, MissingEnvelopeCode(tag), "A mandatory FIX envelope field is missing.", true, tag, null, message.MsgType);
            return;
        }
        if (count > 1 && options.RejectDuplicateFields)
        {
            AddValidationIssue(issues, ValidationLayer.Envelope, ValidationSeverity.Error, "duplicate-envelope-field", "A mandatory FIX envelope field occurs more than once.", true, tag, field, message.MsgType);
        }
        int actualPosition = FindOrderedPosition(ordered, field);
        if (actualPosition != expectedPosition || finalPosition && actualPosition != ordered.Count - 1)
        {
            AddValidationIssue(issues, ValidationLayer.Envelope, ValidationSeverity.Error, "invalid-envelope-position", "A mandatory FIX envelope field is outside its required position.", true, tag, field, message.MsgType);
        }
        if (field != null && field.Value.Length == 0)
        {
            if (options.RejectEmptyValues)
            {
                AddValidationIssue(issues, ValidationLayer.Envelope, ValidationSeverity.Error, "empty-envelope-field", "A mandatory FIX envelope field has an empty value.", true, tag, field, message.MsgType);
            }
            return;
        }
        if (field != null && tag == FixTags.BodyLength)
        {
            AsciiIntegerResult parsed = AsciiDigits.ParseUnsignedInt(field.Value, FixDefaults.MaxMessageSize);
            if (!parsed.Success)
            {
                AddValidationIssue(issues, ValidationLayer.Envelope, ValidationSeverity.Error, "invalid-body-length", "BodyLength is not a valid non-negative decimal size.", true, tag, field, message.MsgType);
            }
        }
        if (field != null && tag == FixTags.CheckSum)
        {
            AsciiIntegerResult parsed = ChecksumCalculator.ParseChecksum(field.Value);
            if (!parsed.Success)
            {
                AddValidationIssue(issues, ValidationLayer.Envelope, ValidationSeverity.Error, "invalid-checksum", "CheckSum is not exactly three decimal digits from 000 through 255.", true, tag, field, message.MsgType);
            }
        }
    }

    internal static void ValidateEnvelopeCalculations(FixMessage message, List<ValidationIssue> issues)
    {
        FixField? lengthField = FindFirstMessageField(message, FixTags.BodyLength);
        FixField? checksumField = FindFirstMessageField(message, FixTags.CheckSum);
        if (lengthField == null || checksumField == null)
        {
            return;
        }
        if (message.OriginalBytes.Length != 0)
        {
            BodyLengthResult calculated = BodyLengthCalculator.CalculateBodyLength(message.OriginalBytes);
            if (calculated.Success)
            {
                if (!calculated.Matches)
                {
                    AddValidationIssue(issues, ValidationLayer.Envelope, ValidationSeverity.Error, "body-length-mismatch", "BodyLength does not match the encoded body size.", true, FixTags.BodyLength, lengthField, message.MsgType);
                }
                int checksum = ChecksumCalculator.CalculateChecksumRange(message.OriginalBytes, 0, calculated.ChecksumStart);
                if (checksum != ReadChecksumValue(message.OriginalBytes, calculated.ChecksumStart))
                {
                    AddValidationIssue(issues, ValidationLayer.Envelope, ValidationSeverity.Error, "checksum-mismatch", "CheckSum does not match the modulo-256 sum of the encoded message.", true, FixTags.CheckSum, checksumField, message.MsgType);
                }
            }
            return;
        }
        AsciiIntegerResult declared = AsciiDigits.ParseUnsignedInt(lengthField.Value, FixDefaults.MaxMessageSize);
        if (declared.Success && lengthField.SourceOffset >= 0 && lengthField.SourceLength > 0 && checksumField.SourceOffset >= 0)
        {
            int calculatedLength = checksumField.SourceOffset - lengthField.SourceOffset - lengthField.SourceLength;
            if (calculatedLength >= 0 && declared.Value != calculatedLength)
            {
                AddValidationIssue(issues, ValidationLayer.Envelope, ValidationSeverity.Error, "body-length-mismatch", "BodyLength does not match the encoded body size.", true, FixTags.BodyLength, lengthField, message.MsgType);
            }
        }
    }

    internal static void ValidateVersionExpectations(FixMessage message, FixDictionary dictionary, ValidationContext context, List<ValidationIssue> issues)
    {
        string expectedBegin = context.ExpectedBeginString;
        if (expectedBegin.Length == 0 && context.DictionaryIdentity != null)
        {
            expectedBegin = context.DictionaryIdentity.BeginString;
        }
        if (expectedBegin.Length == 0)
        {
            expectedBegin = dictionary.Identity.BeginString;
        }
        FixField? beginField = FindFirstMessageField(message, FixTags.BeginString);
        string actualBegin = message.BeginString;
        if (actualBegin.Length == 0 && beginField != null)
        {
            actualBegin = Encoding.Latin1.GetString(beginField.Value);
        }
        if (expectedBegin.Length != 0 && actualBegin != expectedBegin)
        {
            AddValidationIssue(issues, ValidationLayer.SessionHeader, ValidationSeverity.Error, "begin-string-mismatch", "BeginString does not match the active session dictionary.", true, FixTags.BeginString, beginField, message.MsgType);
        }

        string expectedApplication = "";
        if (context.DictionaryIdentity != null)
        {
            expectedApplication = context.DictionaryIdentity.ApplicationVersion;
        }
        if (expectedApplication.Length == 0)
        {
            expectedApplication = dictionary.Identity.ApplicationVersion;
        }
        FixField? applicationField = FindFirstMessageField(message, FixTags.ApplVerID);
        if (applicationField == null && message.MsgType == FixAdminMessageTypes.Logon)
        {
            applicationField = FindFirstMessageField(message, FixTags.DefaultApplVerID);
        }
        if (expectedApplication.Length != 0 && applicationField != null && Encoding.Latin1.GetString(applicationField.Value) != expectedApplication)
        {
            AddValidationIssue(issues, ValidationLayer.SessionHeader, ValidationSeverity.Error, "application-version-mismatch", "The message application version does not match the selected dictionary.", true, applicationField.Tag, applicationField, message.MsgType);
        }
        FixField? defaultApplicationField = FindFirstMessageField(message, FixTags.DefaultApplVerID);
        if (context.DefaultApplicationVersion.Length != 0 && defaultApplicationField != null && Encoding.Latin1.GetString(defaultApplicationField.Value) != context.DefaultApplicationVersion)
        {
            AddValidationIssue(issues, ValidationLayer.SessionHeader, ValidationSeverity.Error, "default-application-version-mismatch", "DefaultApplVerID does not match the configured session default.", true, FixTags.DefaultApplVerID, defaultApplicationField, message.MsgType);
        }
    }

    internal static void ValidateRequiredSessionMembers(FixMessage message, FixDictionary dictionary, ValidationOptions options, List<ValidationIssue> issues)
    {
        if (!options.CheckRequiredFields)
        {
            return;
        }
        ValidateRequiredSection(message.HeaderFields, dictionary.Header, ValidationLayer.SessionHeader, "required-header-field-missing", message.MsgType, issues);
        ValidateRequiredSection(message.TrailerFields, dictionary.Trailer, ValidationLayer.SessionHeader, "required-trailer-field-missing", message.MsgType, issues);
    }

    internal static void ValidateRequiredSection(List<FixField> fields, List<FixMemberDefinition> members, ValidationLayer layer, string code, string msgType, List<ValidationIssue> issues)
    {
        for (int memberIndex = 0; memberIndex < members.Count; memberIndex++)
        {
            FixMemberDefinition member = members[memberIndex];
            if (member.Kind != FixMemberKind.Field || member.Presence != FixPresence.Required && member.Presence != FixPresence.Constant || IsEnvelopeTag(member.ReferenceId))
            {
                continue;
            }
            if (FindFirstSectionField(fields, member.ReferenceId) == null)
            {
                AddValidationIssue(issues, layer, ValidationSeverity.Error, code, "A required session section field is missing.", true, member.ReferenceId, null, msgType);
            }
        }
    }

    internal static void ValidateSessionDuplicates(List<FixField> fields, ValidationOptions options, List<ValidationIssue> issues, bool header, string msgType)
    {
        if (!options.RejectDuplicateFields)
        {
            return;
        }
        for (int index = 0; index < fields.Count; index++)
        {
            FixField field = fields[index];
            if (IsEnvelopeTag(field.Tag) || HasEarlierTag(fields, index, field.Tag))
            {
                continue;
            }
            if (CountSectionFields(fields, field.Tag) > 1)
            {
                string code = header ? "duplicate-header-field" : "duplicate-trailer-field";
                AddValidationIssue(issues, ValidationLayer.SessionHeader, ValidationSeverity.Error, code, "A session section field occurs more than once.", true, field.Tag, field, msgType);
            }
        }
    }

    internal static void ValidateSessionEmptyValues(List<FixField> fields, ValidationOptions options, List<ValidationIssue> issues, string msgType)
    {
        if (!options.RejectEmptyValues)
        {
            return;
        }
        for (int index = 0; index < fields.Count; index++)
        {
            FixField field = fields[index];
            if (!IsEnvelopeTag(field.Tag) && field.Value.Length == 0)
            {
                AddValidationIssue(issues, ValidationLayer.SessionHeader, ValidationSeverity.Error, "empty-session-field", "A session header or trailer field has an empty value.", true, field.Tag, field, msgType);
            }
        }
    }

    internal static void ValidateFieldValueLimits(List<FixField> fields, ValidationOptions options, List<ValidationIssue> issues, string msgType)
    {
        for (int index = 0; index < fields.Count; index++)
        {
            FixField field = fields[index];
            if (field.Value.Length > options.MaxFieldSize)
            {
                AddValidationIssue(issues, ValidationLayer.Envelope, ValidationSeverity.Fatal, "field-size-limit-exceeded", "A field value exceeds the configured validation size limit.", true, field.Tag, field, msgType);
            }
        }
    }

    internal static List<FixField> CreateWireOrder(FixMessage message)
    {
        List<FixField> fields = [];
        AppendSectionFields(fields, message.HeaderFields);
        AppendSectionFields(fields, message.BodyFields);
        AppendSectionFields(fields, message.TrailerFields);
        bool hasCompleteWireOrder = true;
        for (int index = 0; index < fields.Count; index++)
        {
            if (fields[index].WireIndex < 0)
            {
                hasCompleteWireOrder = false;
            }
        }
        if (hasCompleteWireOrder)
        {
            for (int index = 1; index < fields.Count; index++)
            {
                FixField current = fields[index];
                int position = index;
                while (position > 0 && fields[position - 1].WireIndex > current.WireIndex)
                {
                    fields[position] = fields[position - 1];
                    position--;
                }
                fields[position] = current;
            }
        }
        return fields;
    }

    internal static void AppendSectionFields(List<FixField> destination, List<FixField> source)
    {
        for (int index = 0; index < source.Count; index++)
        {
            destination.Add(source[index]);
        }
    }

    internal static int CalculateEncodedSize(FixMessage message)
    {
        if (message.OriginalBytes.Length != 0)
        {
            return message.OriginalBytes.Length;
        }
        int size = AddEncodedSizes(CalculateSectionSize(message.HeaderFields), CalculateSectionSize(message.BodyFields));
        size = AddEncodedSizes(size, CalculateSectionSize(message.TrailerFields));
        return size;
    }

    internal static int CalculateSectionSize(List<FixField> fields)
    {
        int size = 0;
        for (int index = 0; index < fields.Count; index++)
        {
            FixField field = fields[index];
            int fieldSize = DataFieldScanner.CountDecimalDigits(field.Tag) + 2;
            fieldSize = AddEncodedSizes(fieldSize, field.Value.Length);
            size = AddEncodedSizes(size, fieldSize);
        }
        return size;
    }

    internal static int AddEncodedSizes(int first, int second)
    {
        if (first > int.MaxValue - second)
        {
            return int.MaxValue;
        }
        return first + second;
    }

    internal static int CountMessageFields(FixMessage message, int tag)
    {
        return CountSectionFields(message.HeaderFields, tag) + CountSectionFields(message.BodyFields, tag) + CountSectionFields(message.TrailerFields, tag);
    }

    internal static int CountSectionFields(List<FixField> fields, int tag)
    {
        int count = 0;
        for (int index = 0; index < fields.Count; index++)
        {
            if (fields[index].Tag == tag)
            {
                count++;
            }
        }
        return count;
    }

    internal static FixField? FindFirstMessageField(FixMessage message, int tag)
    {
        FixField? field = FindFirstSectionField(message.HeaderFields, tag);
        if (field == null)
        {
            field = FindFirstSectionField(message.BodyFields, tag);
        }
        if (field == null)
        {
            field = FindFirstSectionField(message.TrailerFields, tag);
        }
        return field;
    }

    internal static FixField? FindFirstSectionField(List<FixField> fields, int tag)
    {
        for (int index = 0; index < fields.Count; index++)
        {
            if (fields[index].Tag == tag)
            {
                return fields[index];
            }
        }
        return null;
    }

    internal static int FindOrderedPosition(List<FixField> fields, FixField? field)
    {
        if (field == null)
        {
            return -1;
        }
        for (int index = 0; index < fields.Count; index++)
        {
            if (fields[index] == field)
            {
                return index;
            }
        }
        return -1;
    }

    internal static bool HasEarlierTag(List<FixField> fields, int exclusiveEnd, int tag)
    {
        for (int index = 0; index < exclusiveEnd; index++)
        {
            if (fields[index].Tag == tag)
            {
                return true;
            }
        }
        return false;
    }

    internal static bool IsEnvelopeTag(int tag)
    {
        return tag == FixTags.BeginString || tag == FixTags.BodyLength || tag == FixTags.MsgType || tag == FixTags.CheckSum;
    }

    internal static int ReadChecksumValue(byte[] bytes, int checksumStart)
    {
        if (checksumStart < 0 || checksumStart + 6 >= bytes.Length)
        {
            return -1;
        }
        AsciiIntegerResult parsed = ChecksumCalculator.ParseChecksum(new System.ReadOnlySpan<byte>(bytes, checksumStart + 3, 3));
        return parsed.Success ? parsed.Value : -1;
    }

    internal static string MissingEnvelopeCode(int tag)
    {
        if (tag == FixTags.BeginString) return "missing-begin-string";
        if (tag == FixTags.BodyLength) return "missing-body-length";
        if (tag == FixTags.MsgType) return "missing-msg-type";
        return "missing-checksum";
    }

    internal static void AddValidationIssue(List<ValidationIssue> issues, ValidationLayer layer, ValidationSeverity severity, string code, string description, bool hasTag, int tag, FixField? field, string msgType)
    {
        ValidationIssue issue = new ValidationIssue();
        issue.Layer = layer;
        issue.Severity = severity;
        issue.Code = code;
        issue.Message = description;
        issue.HasTag = hasTag;
        issue.Tag = tag;
        issue.MsgType = msgType;
        if (field != null && field.SourceOffset >= 0)
        {
            issue.HasSourceOffset = true;
            issue.SourceOffset = field.SourceOffset;
        }
        issues.Add(issue);
    }
}
