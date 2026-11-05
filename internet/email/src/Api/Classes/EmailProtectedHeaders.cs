using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Provides operations for working with protected headers.
/// </summary>
public class EmailProtectedHeaders : IEmailProtectedHeadersContract
{
    const string HpOuterName = "HP-Outer";
    internal static EmailProtectedHeaderConstructionResult ConstructImplementation(EmailMessage message, EmailProtectedHeaderIntent intent)
    {
        if (message == null)
            throw new ArgumentNullException("message");
        ValidateIntent(intent);
        if (!message.HasBody())
            throw new InvalidOperationException("A message body is required to construct a protected-header payload.");
        EmailIssueList issues = new EmailIssueList();
        MimeEntity payload = (message.GetBody() ?? throw new InvalidOperationException()).Clone();
        RemoveNonStructuralHeaders(payload.GetHeaders());
        payload.SetMediaType(WithIntent(payload.GetMediaType(), intent));
        EmailHeaders outer = new EmailHeadersOperations().CreateWith();
        int index = 0;
        while (index < (message.GetHeaders() ?? throw new InvalidOperationException()).GetCount())
        {
            EmailHeader header = (message.GetHeaders() ?? throw new InvalidOperationException()).GetAt(index);
            if (!IsStructural(header.GetName()))
            {
                if (NameEquals(header.GetName(), HpOuterName))
                {
                    issues.Add(Issue(EmailIssueSeverity.Error, EmailIssueCode.ProtectedHeaderMalformed, "An input message cannot contain an HP-Outer field.", header.GetName(), "Remove the reserved field before constructing protected headers."));
                }
                else if (HasUnsafeUnfoldedValue(header))
                {
                    issues.Add(Issue(EmailIssueSeverity.Error, EmailIssueCode.ProtectedHeaderMalformed, "A protected field contains a NUL character.", header.GetName(), "Replace the unsafe field value before constructing protected headers."));
                }
                else
                {
                    (payload.GetHeaders() ?? throw new InvalidOperationException()).Add(header.Clone());
                    EmailHeader? exposed = CreateExposedHeader(header, intent);
                    if (exposed != null)
                    {
                        outer.Add(exposed);
                        if (intent == EmailProtectedHeaderIntent.Cipher)
                        {
                            (payload.GetHeaders() ?? throw new InvalidOperationException()).Add(CreateHpOuter(exposed));
                        }
                    }
                }
            }

            index++;
        }

        return new EmailProtectedHeaderConstructionResult(payload, outer, issues);
    }

    /// <summary>
    /// Constructs the supplied value.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "intent">The intent used by the operation.</param>
    /// <returns>The protected header construction result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "InvalidOperationException">Thrown when the instance is not in a state that permits this operation.</exception>
    public EmailProtectedHeaderConstructionResult Construct(EmailMessage message, EmailProtectedHeaderIntent intent)
    {
        return EmailProtectedHeaders.ConstructImplementation(message, intent);
    }

    internal static EmailProtectedHeaderExtractionResult ExtractImplementation(EmailHeaders actualOuterHeaders, MimeEntity authenticatedPayload, EmailProtectedHeaderSecurityContext securityContext)
    {
        if (actualOuterHeaders == null)
            throw new ArgumentNullException("actualOuterHeaders");
        if (authenticatedPayload == null)
            throw new ArgumentNullException("authenticatedPayload");
        if (securityContext == null)
            throw new ArgumentNullException("securityContext");
        EmailIssueList issues = new EmailIssueList();
        EmailParameterList parameters = (authenticatedPayload.GetMediaType() ?? throw new InvalidOperationException()).GetParameters();
        List<EmailParameter> hpValues = parameters.GetAllNamed("hp");
        int parsedIntent = hpValues.Count == 1 ? ParseIntent(hpValues[0].GetValueBytes()) : -1;
        if (parsedIntent < 0)
        {
            issues.Add(Issue(EmailIssueSeverity.Error, EmailIssueCode.ProtectedHeaderMalformed, hpValues.Count == 0 ? "The cryptographic payload has no protected-header intent parameter." : "The cryptographic payload has a duplicate or unsupported protected-header intent parameter.", "Content-Type", "Require exactly one hp=clear or hp=cipher parameter on the cryptographic payload root."));
            return EmptyExtraction(securityContext.GetProtocol(), issues);
        }

        EmailProtectedHeaderIntent intent = (EmailProtectedHeaderIntent)parsedIntent;
        EmailHeaders protectedHeaders = new EmailHeadersOperations().CreateWith();
        EmailHeaders recordedOuter = new EmailHeadersOperations().CreateWith();
        bool malformedRecord = false;
        int index = 0;
        while (index < (authenticatedPayload.GetHeaders() ?? throw new InvalidOperationException()).GetCount())
        {
            EmailHeader header = (authenticatedPayload.GetHeaders() ?? throw new InvalidOperationException()).GetAt(index);
            if (NameEquals(header.GetName(), HpOuterName))
            {
                EmailHeader? parsed = ParseHpOuter(header);
                if (parsed == null)
                {
                    malformedRecord = true;
                    issues.Add(Issue(EmailIssueSeverity.Error, EmailIssueCode.ProtectedHeaderMalformed, "An HP-Outer field does not contain a safe field-name and field-value pair.", HpOuterName, "Ignore the malformed outer-header record."));
                }
                else
                {
                    recordedOuter.Add(parsed);
                }
            }
            else if (!IsStructural(header.GetName()))
            {
                protectedHeaders.Add(header.Clone());
            }

            index++;
        }

        if (intent == EmailProtectedHeaderIntent.Clear && recordedOuter.GetCount() > 0)
        {
            issues.Add(Issue(EmailIssueSeverity.Error, EmailIssueCode.ProtectedHeaderMalformed, "An hp=clear payload must not contain HP-Outer fields.", HpOuterName, "Remove the HP-Outer fields or use hp=cipher inside an encrypted payload."));
        }

        if (intent == EmailProtectedHeaderIntent.Cipher && !securityContext.IsEncrypted())
        {
            issues.Add(Issue(EmailIssueSeverity.Error, EmailIssueCode.ProtectedHeaderIntentMismatch, "An hp=cipher payload was not obtained through an encrypted cryptographic layer.", "Content-Type", "Do not treat the protected fields as confidential."));
        }

        if (malformedRecord)
        {
            recordedOuter = new EmailHeadersOperations().CreateWith();
        }

        List<EmailProtectedHeaderMismatch> mismatches = CompareOuter(actualOuterHeaders, protectedHeaders, recordedOuter, intent, issues);
        bool fromMismatch = HasFromMismatch(actualOuterHeaders, protectedHeaders);
        if (fromMismatch)
        {
            mismatches.Add(new EmailProtectedHeaderMismatch(EmailProtectedHeaderMismatchKind.FromAddressMismatch, "From", JoinValues(protectedHeaders.GetAllNamed("From")), JoinValues(actualOuterHeaders.GetAllNamed("From"))));
            EmailIssueSeverity severity = securityContext.IsProtectedFromSignerBound() ? EmailIssueSeverity.Information : EmailIssueSeverity.Warning;
            issues.Add(Issue(severity, EmailIssueCode.ProtectedHeaderFromMismatch, "The outer From address does not match the protected From address.", "From", securityContext.IsProtectedFromSignerBound() ? "The mismatch was retained for reporting; the protected signer-bound identity remains authoritative." : "Warn the user and display the outer From identity while retaining the protected identity for replies."));
        }

        List<EmailProtectedHeaderEntry> entries = Classify(protectedHeaders, recordedOuter, intent, securityContext);
        EmailHeaders display = fromMismatch && !securityContext.IsProtectedFromSignerBound() ? ReplaceFrom(protectedHeaders, actualOuterHeaders) : protectedHeaders.Clone();
        return new EmailProtectedHeaderExtractionResult(true, intent, securityContext.GetProtocol(), protectedHeaders, recordedOuter, display, entries, mismatches, issues);
    }

    /// <summary>
    /// Extracts the supplied value.
    /// </summary>
    /// <param name = "actualOuterHeaders">The actual outer headers used by the operation.</param>
    /// <param name = "authenticatedPayload">The authenticated payload used by the operation.</param>
    /// <param name = "securityContext">The security context used by the operation.</param>
    /// <returns>The protected header extraction result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailProtectedHeaderExtractionResult Extract(EmailHeaders actualOuterHeaders, MimeEntity authenticatedPayload, EmailProtectedHeaderSecurityContext securityContext)
    {
        return EmailProtectedHeaders.ExtractImplementation(actualOuterHeaders, authenticatedPayload, securityContext);
    }

    static EmailProtectedHeaderExtractionResult EmptyExtraction(EmailProtectedHeaderProtocol protocol, EmailIssueList issues)
    {
        EmailHeaders empty = new EmailHeadersOperations().CreateWith();
        return new EmailProtectedHeaderExtractionResult(false, EmailProtectedHeaderIntent.Clear, protocol, empty, empty, empty, new List<EmailProtectedHeaderEntry>(), new List<EmailProtectedHeaderMismatch>(), issues);
    }

    static EmailMediaType WithIntent(EmailMediaType? source, EmailProtectedHeaderIntent intent)
    {
        List<EmailParameter> physical = new List<EmailParameter>();
        int index = 0;
        while (index < (source ?? throw new InvalidOperationException()).GetParameters().GetCount())
        {
            EmailParameter parameter = source.GetParameters().GetAt(index);
            if (!NameEquals(parameter.GetName(), "hp"))
                physical.Add(parameter);
            index++;
        }

        string text = intent == EmailProtectedHeaderIntent.Clear ? "clear" : "cipher";
        byte[] value = Encoding.ASCII.GetBytes(text);
        EmailSourcePosition position = new EmailSourcePosition(0, 1, 1);
        physical.Add(new EmailParameter("hp", value, value, false, Encoding.ASCII.GetBytes("hp=" + text), position));
        List<EmailRfc2231DecodedParameter> decoded = new List<EmailRfc2231DecodedParameter>();
        index = 0;
        while (index < source.GetParameters().GetDecodedCount())
        {
            EmailRfc2231DecodedParameter parameter = source.GetParameters().GetDecodedAt(index);
            if (!NameEquals(parameter.GetName(), "hp"))
                decoded.Add(parameter);
            index++;
        }

        return new EmailMediaType(source.GetTopLevelType(), source.GetSubtype(), new EmailParameterList(physical, decoded), new byte[0], position, false);
    }

    static EmailHeader? CreateExposedHeader(EmailHeader source, EmailProtectedHeaderIntent intent)
    {
        if (intent == EmailProtectedHeaderIntent.Clear)
            return source.Clone();
        if (NameEquals(source.GetName(), "Comments") || NameEquals(source.GetName(), "Keywords"))
            return null;
        if (NameEquals(source.GetName(), "Subject"))
            return new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind(source.GetName(), Encoding.ASCII.GetBytes(" [...]"), EmailHeaderValueKind.Unstructured);
        return source.Clone();
    }

    static EmailHeader CreateHpOuter(EmailHeader exposed)
    {
        string record = " " + exposed.GetName() + ":" + Encoding.UTF8.GetString(exposed.GetUnfoldedValue());
        return new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind(HpOuterName, Encoding.UTF8.GetBytes(record), EmailHeaderValueKind.Unknown);
    }

    static EmailHeader? ParseHpOuter(EmailHeader source)
    {
        byte[] value = TrimAsciiWhitespace(source.GetUnfoldedValue());
        int colon = Array.IndexOf(value, (byte)':');
        if (colon <= 0)
            return null;
        string name = Encoding.ASCII.GetString(value, 0, colon);
        if (!IsSafeFieldName(name) || IsStructural(name) || NameEquals(name, HpOuterName))
            return null;
        byte[] raw = new byte[value.Length - colon - 1];
        Array.Copy(value, colon + 1, raw, 0, raw.Length);
        if (ContainsNul(raw))
            return null;
        return new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind(name, raw, new EmailHeaderRegistry().GetValueKind(name));
    }

    static List<EmailProtectedHeaderMismatch> CompareOuter(EmailHeaders actual, EmailHeaders protectedHeaders, EmailHeaders recordedOuter, EmailProtectedHeaderIntent intent, EmailIssueList issues)
    {
        EmailHeaders expected = intent == EmailProtectedHeaderIntent.Clear ? protectedHeaders : recordedOuter;
        List<string> names = UniqueNames(protectedHeaders, expected);
        List<EmailProtectedHeaderMismatch> result = new List<EmailProtectedHeaderMismatch>();
        int nameIndex = 0;
        while (nameIndex < names.Count)
        {
            string name = names[nameIndex];
            List<EmailHeader> expectedValues = expected.GetAllNamed(name);
            List<EmailHeader> actualValues = actual.GetAllNamed(name);
            int count = Math.Max(expectedValues.Count, actualValues.Count);
            int index = 0;
            while (index < count)
            {
                EmailProtectedHeaderMismatch? mismatch = null;
                if (index >= actualValues.Count)
                {
                    mismatch = new EmailProtectedHeaderMismatch(EmailProtectedHeaderMismatchKind.MissingOuterHeader, name, Value(expectedValues[index]), null);
                }
                else if (index >= expectedValues.Count)
                {
                    mismatch = new EmailProtectedHeaderMismatch(EmailProtectedHeaderMismatchKind.UnexpectedOuterHeader, name, null, Value(actualValues[index]));
                }
                else if (!ValuesEqual(expectedValues[index], actualValues[index]))
                {
                    mismatch = new EmailProtectedHeaderMismatch(EmailProtectedHeaderMismatchKind.OuterValueMismatch, name, Value(expectedValues[index]), Value(actualValues[index]));
                }

                if (mismatch != null)
                {
                    result.Add(mismatch);
                    issues.Add(Issue(EmailIssueSeverity.Warning, EmailIssueCode.ProtectedHeaderOuterMismatch, "An outer field does not match the cryptographically recorded outer field.", name, "Treat the outer field as untrusted and render the protected field according to the header-protection policy."));
                }

                index++;
            }

            nameIndex++;
        }

        return result;
    }

    static List<EmailProtectedHeaderEntry> Classify(EmailHeaders protectedHeaders, EmailHeaders recordedOuter, EmailProtectedHeaderIntent intent, EmailProtectedHeaderSecurityContext context)
    {
        List<EmailHeader> remainingReferences = recordedOuter.GetAll();
        List<EmailProtectedHeaderEntry> entries = new List<EmailProtectedHeaderEntry>();
        int index = 0;
        while (index < protectedHeaders.GetCount())
        {
            EmailHeader header = protectedHeaders.GetAt(index);
            bool referenced = RemoveFirstMatch(remainingReferences, header);
            EmailProtectedHeaderState state;
            if (intent == EmailProtectedHeaderIntent.Cipher && context.IsEncrypted() && !referenced)
            {
                state = context.HasValidSignature() ? EmailProtectedHeaderState.SignedAndEncrypted : EmailProtectedHeaderState.EncryptedOnly;
            }
            else
            {
                state = context.HasValidSignature() ? EmailProtectedHeaderState.SignedOnly : EmailProtectedHeaderState.Unprotected;
            }

            entries.Add(new EmailProtectedHeaderEntry(header, state));
            index++;
        }

        return entries;
    }

    static bool HasFromMismatch(EmailHeaders actual, EmailHeaders protectedHeaders)
    {
        List<EmailHeader> inner = protectedHeaders.GetAllNamed("From");
        if (inner.Count == 0)
            return false;
        List<EmailHeader> outer = actual.GetAllNamed("From");
        List<string> innerAddresses = AddressKeys(inner);
        List<string> outerAddresses = AddressKeys(outer);
        if (innerAddresses.Count > 0 || outerAddresses.Count > 0)
        {
            if (innerAddresses.Count != outerAddresses.Count)
                return true;
            int index = 0;
            while (index < innerAddresses.Count)
            {
                if (!string.Equals(innerAddresses[index], outerAddresses[index], StringComparison.OrdinalIgnoreCase))
                    return true;
                index++;
            }

            return false;
        }

        if (inner.Count != outer.Count)
            return true;
        int fallback = 0;
        while (fallback < inner.Count)
        {
            if (!ValuesEqual(inner[fallback], outer[fallback]))
                return true;
            fallback++;
        }

        return false;
    }

    static List<string> AddressKeys(List<EmailHeader> headers)
    {
        List<string> result = new List<string>();
        int headerIndex = 0;
        while (headerIndex < headers.Count)
        {
            EmailAddressParseResult parsed = new EmailAddressParser().ParseListWithInputAndMode(headers[headerIndex].GetUnfoldedValue(), EmailAddressParseMode.Tolerant);
            if (parsed.IsSuccess() && parsed.IsComplete())
            {
                EmailAddressList list = parsed.GetAddressList();
                int entryIndex = 0;
                while (entryIndex < list.GetCount())
                {
                    EmailAddressListEntry entry = list.GetAt(entryIndex);
                    if (entry.IsMailbox())
                        AddAddressKey(result, entry.GetMailbox());
                    else
                    {
                        int groupIndex = 0;
                        while (groupIndex < (entry.GetGroup() ?? throw new InvalidOperationException()).GetAddressCount())
                        {
                            AddAddressKey(result, (entry.GetGroup() ?? throw new InvalidOperationException()).GetAddress(groupIndex));
                            groupIndex++;
                        }
                    }

                    entryIndex++;
                }
            }

            headerIndex++;
        }

        return result;
    }

    static void AddAddressKey(List<string> result, EmailAddress? address)
    {
        string rawDomain = (address ?? throw new InvalidOperationException()).GetDomain();
        string domain = address.IsDomainLiteral() || Uri.CheckHostName(rawDomain) != UriHostNameType.Dns ? rawDomain : new EmailAddressIdn().ToAscii(rawDomain);
        result.Add(address.GetLocalPart().ToLowerInvariant() + "@" + domain.ToLowerInvariant());
    }

    static EmailHeaders ReplaceFrom(EmailHeaders protectedHeaders, EmailHeaders actual)
    {
        EmailHeaders result = new EmailHeadersOperations().CreateWith();
        bool inserted = false;
        int index = 0;
        while (index < protectedHeaders.GetCount())
        {
            EmailHeader header = protectedHeaders.GetAt(index);
            if (NameEquals(header.GetName(), "From"))
            {
                if (!inserted)
                {
                    List<EmailHeader> outer = actual.GetAllNamed("From");
                    int outerIndex = 0;
                    while (outerIndex < outer.Count)
                    {
                        result.Add(outer[outerIndex].Clone());
                        outerIndex++;
                    }

                    inserted = true;
                }
            }
            else
                result.Add(header.Clone());
            index++;
        }

        return result;
    }

    static List<string> UniqueNames(EmailHeaders first, EmailHeaders second)
    {
        List<string> result = new List<string>();
        AddUniqueNames(result, first);
        AddUniqueNames(result, second);
        return result;
    }

    static void AddUniqueNames(List<string> result, EmailHeaders headers)
    {
        int index = 0;
        while (index < headers.GetCount())
        {
            string name = headers.GetAt(index).GetName();
            bool found = false;
            int existing = 0;
            while (existing < result.Count)
            {
                if (NameEquals(result[existing], name))
                {
                    found = true;
                    break;
                }

                existing++;
            }

            if (!found)
                result.Add(name);
            index++;
        }
    }

    static bool RemoveFirstMatch(List<EmailHeader> headers, EmailHeader target)
    {
        int index = 0;
        while (index < headers.Count)
        {
            if (NameEquals(headers[index].GetName(), target.GetName()) && ValuesEqual(headers[index], target))
            {
                headers.RemoveAt(index);
                return true;
            }

            index++;
        }

        return false;
    }

    static int ParseIntent(byte[] value)
    {
        if (value == null)
            return -1;
        string text = Encoding.ASCII.GetString(TrimAsciiWhitespace(value));
        if (string.Equals(text, "clear", StringComparison.OrdinalIgnoreCase))
            return (int)EmailProtectedHeaderIntent.Clear;
        if (string.Equals(text, "cipher", StringComparison.OrdinalIgnoreCase))
            return (int)EmailProtectedHeaderIntent.Cipher;
        return -1;
    }

    static void ValidateIntent(EmailProtectedHeaderIntent intent)
    {
        if (intent < EmailProtectedHeaderIntent.Clear || intent > EmailProtectedHeaderIntent.Cipher)
            throw new ArgumentOutOfRangeException("intent");
    }

    static void RemoveNonStructuralHeaders(EmailHeaders? headers)
    {
        int index = (headers ?? throw new InvalidOperationException()).GetCount() - 1;
        while (index >= 0)
        {
            if (!IsStructural(headers.GetAt(index).GetName()))
                headers.RemoveAt(index);
            index--;
        }
    }

    static bool IsStructural(string name)
    {
        return NameEquals(name, "MIME-Version") || name.StartsWith("Content-", StringComparison.OrdinalIgnoreCase);
    }

    static bool IsSafeFieldName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;
        int index = 0;
        while (index < name.Length)
        {
            char character = name[index];
            if (character < 33 || character > 126 || character == ':')
                return false;
            index++;
        }

        return true;
    }

    static bool HasUnsafeUnfoldedValue(EmailHeader header)
    {
        return ContainsNul(header.GetUnfoldedValue());
    }

    static bool ContainsNul(byte[] value)
    {
        return Array.IndexOf(value, (byte)0) >= 0;
    }

    static bool NameEquals(string first, string second)
    {
        return string.Equals(first, second, StringComparison.OrdinalIgnoreCase);
    }

    static bool ValuesEqual(EmailHeader first, EmailHeader second)
    {
        return string.Equals(Value(first), Value(second), StringComparison.Ordinal);
    }

    static string Value(EmailHeader header)
    {
        byte[] value = TrimAsciiWhitespace(header.GetUnfoldedValue());
        List<byte> normalized = new List<byte>();
        bool whitespace = false;
        int index = 0;
        while (index < value.Length)
        {
            byte current = value[index];
            if (current == 32 || current == 9)
                whitespace = true;
            else
            {
                if (whitespace && normalized.Count > 0)
                    normalized.Add(32);
                normalized.Add(current);
                whitespace = false;
            }

            index++;
        }

        return Encoding.UTF8.GetString(normalized.ToArray());
    }

    static string JoinValues(List<EmailHeader> headers)
    {
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < headers.Count)
        {
            if (index > 0)
                result.Append(" | ");
            result.Append(Value(headers[index]));
            index++;
        }

        return result.ToString();
    }

    static byte[] TrimAsciiWhitespace(byte[] value)
    {
        int start = 0;
        int end = value.Length;
        while (start < end && (value[start] == 32 || value[start] == 9))
            start++;
        while (end > start && (value[end - 1] == 32 || value[end - 1] == 9))
            end--;
        byte[] result = new byte[end - start];
        Array.Copy(value, start, result, 0, result.Length);
        return result;
    }

    static EmailIssue Issue(EmailIssueSeverity severity, EmailIssueCode code, string description, string fieldName, string recovery)
    {
        return new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(severity, code, description, null, null, fieldName, recovery);
    }
}
