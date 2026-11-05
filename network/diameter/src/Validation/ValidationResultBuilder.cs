using System;

namespace diameter;

internal class ValidationResultBuilder
{
    internal DictionaryIndexes Indexes;
    internal int TypeCount;

    internal ValidationResultBuilder(DiameterDictionarySnapshot? snapshot)
    {
        Indexes = new DictionaryIndexes(snapshot);
        if (snapshot != null)
        {
            TypeCount = snapshot.Types.Count;
        }
    }

    internal DiameterValidationResult BuildValidationResult(
        DiameterMessage? message,
        CommandValidationResult? commandResult,
        AvpValidationResult? avpResult)
    {
        DiameterValidationResult result = new DiameterValidationResult();
        AppendCommandIssues(result, message, commandResult);
        AppendAvpIssues(result, avpResult);
        result.Status = DiameterValidationStatus.Valid;
        if (result.Issues.Count > 0)
        {
            result.Status = DiameterValidationStatus.Invalid;
            DiameterValidationIssue first = result.Issues[0];
            DiameterErrorAnswerRequest error = new DiameterErrorAnswerRequest();
            error.Request = message;
            error.Issue = first;
            error.ResultCode = first.ResultCode;
            error.ErrorMessage = first.Message;
            error.FailedAvp = first.FailedAvp;
            result.ErrorAnswer = error;
        }

        return result;
    }

    internal void AppendCommandIssues(
        DiameterValidationResult result,
        DiameterMessage? message,
        CommandValidationResult? commandResult)
    {
        if (commandResult == null)
        {
            return;
        }

        int index = 0;
        while (index < commandResult.Failures.Count)
        {
            CommandValidationFailure failure = commandResult.Failures[index];
            DiameterValidationIssue issue = CreateCommandIssue(failure);
            AttachCommandFailedAvp(issue, failure, message);
            result.Issues.Add(issue);
            index = index + 1;
        }
    }

    internal void AppendAvpIssues(
        DiameterValidationResult result,
        AvpValidationResult? avpResult)
    {
        if (avpResult == null)
        {
            return;
        }

        int index = 0;
        while (index < avpResult.Failures.Count)
        {
            AvpValidationFailure failure = avpResult.Failures[index];
            DiameterValidationIssue issue = CreateAvpIssue(failure);
            AttachAvpFailedAvp(issue, failure);
            result.Issues.Add(issue);
            index = index + 1;
        }
    }

    internal DiameterValidationIssue CreateCommandIssue(CommandValidationFailure failure)
    {
        DiameterValidationIssue issue = new DiameterValidationIssue();
        issue.Severity = DiameterIssueSeverity.Error;
        issue.Code = failure.Code;
        issue.Message = DescribeCode(failure.Code);
        issue.ResultCode = SelectCommandResultCode(failure.Code);
        issue.AvpCode = failure.AvpCode;
        issue.AvpVendorId = failure.AvpVendorId;
        issue.AvpIndex = failure.AvpIndex;
        issue.ExpectedMinimum = failure.ExpectedMinimum;
        issue.ExpectedMaximum = failure.ExpectedMaximum;
        issue.ActualCount = failure.ActualCount;
        issue.ExpectedPosition = failure.ExpectedPosition;
        issue.ExpectedMessageFlags = failure.ExpectedFlags;
        issue.ActualMessageFlags = failure.ActualFlags;
        return issue;
    }

    internal DiameterValidationIssue CreateAvpIssue(AvpValidationFailure failure)
    {
        DiameterValidationIssue issue = new DiameterValidationIssue();
        issue.Severity = DiameterIssueSeverity.Error;
        issue.Code = failure.Code;
        issue.Message = DescribeCode(failure.Code);
        issue.ResultCode = SelectAvpResultCode(failure.Code);
        issue.AvpCode = failure.AvpCode;
        issue.AvpVendorId = failure.AvpVendorId;
        issue.ParentAvpCode = failure.ParentAvpCode;
        issue.ParentAvpVendorId = failure.ParentAvpVendorId;
        issue.AvpIndex = failure.AvpIndex;
        issue.Depth = failure.Depth;
        issue.ExpectedMinimum = failure.ExpectedMinimum;
        issue.ExpectedMaximum = failure.ExpectedMaximum;
        issue.ActualCount = failure.ActualCount;
        issue.ExpectedLength = failure.ExpectedLength;
        issue.ActualLength = failure.ActualLength;
        issue.ExpectedPosition = failure.ExpectedPosition;
        issue.ExpectedAvpFlags = failure.ExpectedFlags;
        issue.ActualAvpFlags = failure.ActualFlags;
        issue.OffendingAvp = failure.Avp;
        return issue;
    }

    internal uint SelectCommandResultCode(string code)
    {
        if (code == "unknown-command")
        {
            return DiameterResultCodes.CommandUnsupported;
        }

        if (code == "application-id-mismatch")
        {
            return DiameterResultCodes.ApplicationUnsupported;
        }

        if (code == "required-header-flags-missing" ||
            code == "prohibited-header-flags-present")
        {
            return DiameterResultCodes.InvalidBitInHeader;
        }

        if (code == "missing-required-avp")
        {
            return DiameterResultCodes.MissingAvp;
        }

        if (code == "too-many-avp-occurrences")
        {
            return DiameterResultCodes.AvpOccursTooManyTimes;
        }

        if (code == "fixed-head-avp-misordered" ||
            code == "avp-ordering-violation" || code == "avp-not-allowed")
        {
            return DiameterResultCodes.AvpNotAllowed;
        }

        return DiameterResultCodes.UnableToComply;
    }

    internal uint SelectAvpResultCode(string code)
    {
        if (code == "unsupported-mandatory-avp")
        {
            return DiameterResultCodes.AvpUnsupported;
        }

        if (code == "invalid-derived-avp-value" || code == "unknown-enumerated-value")
        {
            return DiameterResultCodes.InvalidAvpValue;
        }

        if (code == "missing-required-grouped-avp")
        {
            return DiameterResultCodes.MissingAvp;
        }

        if (code == "too-many-grouped-avp-occurrences")
        {
            return DiameterResultCodes.AvpOccursTooManyTimes;
        }

        if (code == "grouped-avp-not-allowed" ||
            code == "fixed-head-grouped-avp-misordered" ||
            code == "grouped-avp-ordering-violation")
        {
            return DiameterResultCodes.AvpNotAllowed;
        }

        if (code == "invalid-avp-size" || code == "avp-data-required" ||
            code == "invalid-grouped-value")
        {
            return DiameterResultCodes.InvalidAvpLength;
        }

        if (code == "vendor-id-required" || code == "vendor-flag-required" ||
            code == "avp-vendor-mismatch" || code == "required-avp-flags-missing" ||
            code == "prohibited-avp-flags-present")
        {
            return DiameterResultCodes.InvalidAvpBitCombination;
        }

        return DiameterResultCodes.UnableToComply;
    }

    internal string DescribeCode(string code)
    {
        if (code == "unknown-command")
        {
            return "The Diameter command is not supported.";
        }

        if (code == "application-id-mismatch")
        {
            return "The command is not defined for the message application identifier.";
        }

        if (code == "missing-required-avp" || code == "missing-required-grouped-avp")
        {
            return "A required Diameter AVP is missing.";
        }

        if (code == "too-many-avp-occurrences" ||
            code == "too-many-grouped-avp-occurrences")
        {
            return "A Diameter AVP occurs more often than permitted.";
        }

        if (code == "unsupported-mandatory-avp")
        {
            return "A mandatory Diameter AVP is not supported.";
        }

        if (code == "invalid-derived-avp-value" || code == "unknown-enumerated-value")
        {
            return "A Diameter AVP contains an invalid value.";
        }

        if (code == "invalid-avp-size" || code == "avp-data-required" ||
            code == "invalid-grouped-value")
        {
            return "A Diameter AVP has an invalid payload length.";
        }

        if (code == "required-header-flags-missing" ||
            code == "prohibited-header-flags-present")
        {
            return "The Diameter header contains an invalid flag combination.";
        }

        if (code == "vendor-id-required" || code == "vendor-flag-required" ||
            code == "avp-vendor-mismatch" || code == "required-avp-flags-missing" ||
            code == "prohibited-avp-flags-present")
        {
            return "The Diameter AVP header contains an invalid flag or Vendor-ID combination.";
        }

        if (code == "avp-not-allowed" || code == "grouped-avp-not-allowed" ||
            code == "fixed-head-avp-misordered" || code == "avp-ordering-violation" ||
            code == "fixed-head-grouped-avp-misordered" ||
            code == "grouped-avp-ordering-violation")
        {
            return "The Diameter AVP is not allowed at this location.";
        }

        return "The Diameter message could not be validated.";
    }

    internal void AttachCommandFailedAvp(
        DiameterValidationIssue issue,
        CommandValidationFailure failure,
        DiameterMessage? message)
    {
        DiameterAvp? contained = null;
        if (failure.Code == "missing-required-avp")
        {
            contained = CreatePlaceholder(failure.AvpCode, failure.AvpVendorId);
        }
        else if (message != null && failure.AvpIndex >= 0 &&
            failure.AvpIndex < message.Avps.Count)
        {
            contained = message.Avps[failure.AvpIndex];
            issue.OffendingAvp = contained;
        }

        if (contained != null)
        {
            issue.FailedAvp = CreateFailedWrapper(contained, 0U, 0U);
        }
    }

    internal void AttachAvpFailedAvp(
        DiameterValidationIssue issue,
        AvpValidationFailure failure)
    {
        DiameterAvp? contained = failure.Avp;
        if (failure.Code == "missing-required-grouped-avp")
        {
            contained = CreatePlaceholder(failure.AvpCode, failure.AvpVendorId);
        }

        if (contained != null)
        {
            issue.FailedAvp = CreateFailedWrapper(
                contained,
                failure.ParentAvpCode,
                failure.ParentAvpVendorId);
        }
    }

    internal DiameterAvp CreatePlaceholder(uint code, uint vendorId)
    {
        DiameterAvpDefinition? definition = Indexes.FindAvpByCode(vendorId, code);
        DiameterAvp placeholder = new DiameterAvp();
        placeholder.Code = code;
        placeholder.VendorId = vendorId;
        if (definition != null)
        {
            placeholder.Flags = definition.RequiredFlags;
            placeholder.Data = new byte[FindMinimumDataLength(definition)];
        }

        if (vendorId != DiameterVendorIds.Ietf)
        {
            placeholder.Flags = placeholder.Flags | DiameterAvpFlags.VendorSpecific;
        }

        return placeholder;
    }

    internal int FindMinimumDataLength(DiameterAvpDefinition definition)
    {
        if (definition.FixedDataLength >= 0)
        {
            return definition.FixedDataLength;
        }

        string typeName = ResolveMinimumTypeName(definition);
        if (typeName == "Address")
        {
            return 6;
        }

        if (typeName == "Time" || typeName == "Enumerated" ||
            definition.BaseType == DiameterAvpBaseType.Integer32 ||
            definition.BaseType == DiameterAvpBaseType.Unsigned32 ||
            definition.BaseType == DiameterAvpBaseType.Float32)
        {
            return 4;
        }

        if (definition.BaseType == DiameterAvpBaseType.Integer64 ||
            definition.BaseType == DiameterAvpBaseType.Unsigned64 ||
            definition.BaseType == DiameterAvpBaseType.Float64)
        {
            return 8;
        }

        return 0;
    }

    internal string ResolveMinimumTypeName(DiameterAvpDefinition definition)
    {
        string name = definition.TypeName;
        uint vendorId = definition.VendorId;
        int remaining = TypeCount + 1;
        while (remaining > 0)
        {
            if (name == "Address" || name == "Time" || name == "Enumerated" ||
                name == "Integer32" || name == "Unsigned32" || name == "Float32" ||
                name == "Integer64" || name == "Unsigned64" || name == "Float64")
            {
                return name;
            }

            DiameterTypeDefinition? type = Indexes.FindTypeByName(vendorId, name);
            if (type == null && vendorId != DiameterVendorIds.Ietf)
            {
                type = Indexes.FindTypeByName(DiameterVendorIds.Ietf, name);
            }

            if (type == null || string.IsNullOrWhiteSpace(type.ParentTypeName))
            {
                return name;
            }

            name = type.ParentTypeName;
            vendorId = type.ParentTypeVendorId;
            remaining = remaining - 1;
        }

        return name;
    }

    internal DiameterAvp CreateFailedWrapper(
        DiameterAvp contained,
        uint parentCode,
        uint parentVendorId)
    {
        DiameterAvp selected = contained;
        if (parentCode != 0U &&
            (contained.Code != parentCode || contained.VendorId != parentVendorId))
        {
            DiameterAvp parent = CreatePlaceholder(parentCode, parentVendorId);
            parent.Data = EncodeContainedAvp(contained);
            selected = parent;
        }

        DiameterAvp failed = new DiameterAvp();
        failed.Code = DiameterAvpCodes.FailedAvp;
        failed.Flags = DiameterAvpFlags.Mandatory;
        failed.Data = EncodeContainedAvp(selected);
        return failed;
    }

    internal byte[] EncodeContainedAvp(DiameterAvp avp)
    {
        DiameterEncodeResult measurement = AvpEncoder.MeasureAvp(avp);
        DiameterAvp selected = avp;
        if (measurement.Status != DiameterCodecStatus.Success)
        {
            selected = SanitizeAvp(avp);
            measurement = AvpEncoder.MeasureAvp(selected);
        }

        if (measurement.Status != DiameterCodecStatus.Success)
        {
            return new byte[0];
        }

        byte[] data = new byte[measurement.RequiredCount];
        DiameterEncodeResult encoded = AvpEncoder.EncodeAvp(data, selected);
        if (encoded.Status != DiameterCodecStatus.Success)
        {
            return new byte[0];
        }

        return data;
    }

    internal DiameterAvp SanitizeAvp(DiameterAvp source)
    {
        DiameterAvp result = new DiameterAvp();
        result.Code = source.Code;
        result.VendorId = source.VendorId;
        result.Flags = (DiameterAvpFlags)((int)source.Flags &
            DiameterProtocolConstants.AvpFlagMask);
        if (result.VendorId == DiameterVendorIds.Ietf)
        {
            result.Flags = result.Flags & ~DiameterAvpFlags.VendorSpecific;
        }
        else
        {
            result.Flags = result.Flags | DiameterAvpFlags.VendorSpecific;
        }

        result.Data = source.Data;
        if (source.Data == null)
        {
            result.Data = new byte[0];
        }

        return result;
    }
}
