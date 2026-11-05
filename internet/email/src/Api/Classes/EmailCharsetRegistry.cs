using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace email;
/// <summary>
/// Stores and resolves registered charset definitions.
/// </summary>
public class EmailCharsetRegistry : IEmailCharsetRegistryContract
{
    const string InvalidSentinel = "\uFDD0\uFDD1";
    object syncRoot;
    Dictionary<string, EmailCharsetRegistration> registrations;
    List<string> canonicalNames;
    /// <summary>
    /// Creates a new charset registry.
    /// </summary>
    public EmailCharsetRegistry()
    {
        syncRoot = new object ();
        registrations = new Dictionary<string, EmailCharsetRegistration>(StringComparer.OrdinalIgnoreCase);
        canonicalNames = new List<string>();
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        AddBuiltIn("us-ascii", new ASCIIEncoding());
        AddBuiltIn("utf-8", new UTF8Encoding(false, false));
        AddBuiltIn("utf-16", new UnicodeEncoding(false, false, false));
        AddBuiltIn("utf-16be", new UnicodeEncoding(true, false, false));
        AddBuiltIn("utf-32", new UTF32Encoding(false, false, false));
        AddBuiltIn("utf-32be", new UTF32Encoding(true, false, false));
        AddBuiltIn("iso-8859-1", Encoding.Latin1);
        AddAliasIfMissing("ascii", "us-ascii");
        AddAliasIfMissing("ansi_x3.4-1968", "us-ascii");
        AddAliasIfMissing("iso-ir-6", "us-ascii");
        AddAliasIfMissing("us", "us-ascii");
        AddAliasIfMissing("utf8", "utf-8");
        AddAliasIfMissing("unicode-1-1-utf-8", "utf-8");
        AddAliasIfMissing("utf-16le", "utf-16");
        AddAliasIfMissing("unicode", "utf-16");
        AddAliasIfMissing("ucs-2", "utf-16");
        AddAliasIfMissing("unicodefffe", "utf-16be");
        AddAliasIfMissing("utf-32le", "utf-32");
        AddAliasIfMissing("latin1", "iso-8859-1");
        AddAliasIfMissing("latin-1", "iso-8859-1");
        AddAliasIfMissing("iso_8859-1:1987", "iso-8859-1");
        AddAliasIfMissing("cp819", "iso-8859-1");
        AddAliasIfMissing("ibm819", "iso-8859-1");
        AddRuntimeEncodings();
    }

    /// <summary>
    /// Looks up the supplied value.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <returns>The charset lookup result produced by the operation.</returns>
    public EmailCharsetLookupResult Lookup(string? name)
    {
        ValidateName(name, "name");
        lock (syncRoot)
        {
            if (!registrations.ContainsKey((name ?? throw new InvalidOperationException())))
            {
                return new EmailCharsetLookupResult(false, name, null, null);
            }

            EmailCharsetRegistration registration = registrations[name];
            return new EmailCharsetLookupResult(true, name, registration.GetCanonicalName(), registration.GetEncoding());
        }
    }

    /// <summary>
    /// Determines whether the collection contains the supplied value.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool Contains(string name)
    {
        return Lookup(name).IsFound();
    }

    /// <summary>
    /// Registers the supplied value.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <param name = "encoding">The encoding used by the operation.</param>
    /// <param name = "replaceExisting">Whether to enable the replace existing behavior.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "InvalidOperationException">Thrown when the instance is not in a state that permits this operation.</exception>
    public void Register(string name, Encoding encoding, bool replaceExisting)
    {
        ValidateName(name, "name");
        if (encoding == null)
        {
            throw new ArgumentNullException("encoding");
        }

        lock (syncRoot)
        {
            if (registrations.ContainsKey(name) && !replaceExisting)
            {
                throw new InvalidOperationException("The charset name is already registered.");
            }

            EmailCharsetRegistration registration = new EmailCharsetRegistration(name, encoding);
            registrations[name] = registration;
            AddCanonicalName(name);
        }
    }

    /// <summary>
    /// Registers the alias.
    /// </summary>
    /// <param name = "alias">The alias used by the operation.</param>
    /// <param name = "registeredName">The registered name used by the operation.</param>
    /// <param name = "replaceExisting">Whether to enable the replace existing behavior.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "InvalidOperationException">Thrown when the instance is not in a state that permits this operation.</exception>
    public void RegisterAlias(string alias, string registeredName, bool replaceExisting)
    {
        ValidateName(alias, "alias");
        ValidateName(registeredName, "registeredName");
        lock (syncRoot)
        {
            if (!registrations.ContainsKey(registeredName))
            {
                throw new ArgumentException("The target charset is not registered.", "registeredName");
            }

            if (registrations.ContainsKey(alias) && !replaceExisting)
            {
                throw new InvalidOperationException("The charset alias is already registered.");
            }

            registrations[alias] = registrations[registeredName];
        }
    }

    /// <summary>
    /// Gets the registered name count.
    /// </summary>
    /// <returns>The current registered name count.</returns>
    public int GetRegisteredNameCount()
    {
        lock (syncRoot)
        {
            return canonicalNames.Count;
        }
    }

    /// <summary>
    /// Gets the registered names.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current registered names.</returns>
    public List<string> GetRegisteredNames()
    {
        lock (syncRoot)
        {
            List<string> copy = new List<string>(canonicalNames);
            copy.Sort(StringComparer.OrdinalIgnoreCase);
            return copy;
        }
    }

    /// <summary>
    /// Decodes the supplied value.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "declaredCharset">The declared charset used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailCharsetDecodeResult Decode(ReadOnlySpan<byte> input, string? declaredCharset, EmailCharsetDecodeOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        EmailIssueList issues = new EmailIssueList();
        string? requested = declaredCharset;
        EmailCharsetLookupResult? declared = null;
        if (declaredCharset != null && declaredCharset.Length > 0)
        {
            if (declaredCharset.Length > options.GetMaximumCharsetNameLength())
            {
                issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Fatal, EmailIssueCode.CharsetNameLimitExceeded, "The declared charset name exceeds the configured length limit.", new EmailSourcePosition(0, 1, 1), null, null, "No charset decoding was attempted."));
                return new EmailCharsetDecodeResult("", requested, null, null, 0, input.ToArray(), false, issues);
            }

            if (IsValidName(declaredCharset))
            {
                declared = Lookup(declaredCharset);
            }
            else
            {
                declared = new EmailCharsetLookupResult(false, declaredCharset, null, null);
            }
        }

        EmailCharsetBom? bom = null;
        if (options.GetBomPolicy() != EmailCharsetBomPolicy.Ignore)
        {
            bom = DetectBom(input);
        }

        EmailCharsetLookupResult? selected = declared;
        int payloadOffset = 0;
        if (bom != null)
        {
            EmailCharsetLookupResult bomLookup = Lookup(bom.GetCharset());
            payloadOffset = bom.GetByteCount();
            if (declared != null && declared.IsFound() && declared.GetCodePage() != bomLookup.GetCodePage())
            {
                EmailIssueSeverity severity = EmailIssueSeverity.Warning;
                if (options.GetBomPolicy() == EmailCharsetBomPolicy.RequireMatch)
                {
                    severity = EmailIssueSeverity.Error;
                }

                issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(severity, EmailIssueCode.CharsetBomMismatch, "The byte-order mark conflicts with the declared charset.", new EmailSourcePosition(0, 1, 1), null, null, "The byte-order mark charset was used for decoding."));
            }
            else if (declared != null && !declared.IsFound())
            {
                issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Warning, EmailIssueCode.CharsetUnsupported, "The declared charset is unsupported.", new EmailSourcePosition(0, 1, 1), null, null, "The byte-order mark charset was used for decoding."));
            }

            selected = bomLookup;
        }

        if (selected == null || !selected.IsFound())
        {
            issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Error, EmailIssueCode.CharsetUnsupported, "No supported charset was declared or detected.", new EmailSourcePosition(0, 1, 1), null, null, "The original bytes were retained in the result without producing text."));
            string? bomName = null;
            int bomCount = 0;
            if (bom != null)
            {
                bomName = bom.GetCharset();
                bomCount = bom.GetByteCount();
            }

            return new EmailCharsetDecodeResult("", requested, null, bomName, bomCount, input.ToArray(), false, issues);
        }

        Encoding? encoding = selected.GetEncoding();
        EmailCharsetDecodedText decoded = DecodeBytes(input, payloadOffset, encoding, options, issues);
        string text = ApplyCharacterLimit(decoded.GetText(), options.GetMaximumCharacters(), issues);
        string? detectedBom = null;
        int detectedBomCount = 0;
        if (bom != null)
        {
            detectedBom = bom.GetCharset();
            detectedBomCount = bom.GetByteCount();
        }

        return new EmailCharsetDecodeResult(text, requested, selected.GetCanonicalName(), detectedBom, detectedBomCount, decoded.GetUndecodableBytes(), decoded.UsedReplacement(), issues);
    }

    void AddBuiltIn(string name, Encoding encoding)
    {
        EmailCharsetRegistration registration = new EmailCharsetRegistration(name, encoding);
        registrations[name] = registration;
        AddCanonicalName(name);
    }

    void AddAliasIfMissing(string alias, string registeredName)
    {
        if (!registrations.ContainsKey(alias) && registrations.ContainsKey(registeredName))
        {
            registrations.Add(alias, registrations[registeredName]);
        }
    }

    void AddRuntimeEncodings()
    {
        EncodingInfo[] available = Encoding.GetEncodings();
        int index = 0;
        while (index < available.Length)
        {
            Encoding encoding = available[index].GetEncoding();
            string canonical = encoding.WebName;
            if (!registrations.ContainsKey(canonical))
            {
                AddBuiltIn(canonical, encoding);
            }

            AddAliasIfMissing(available[index].Name, canonical);
            AddAliasIfMissing(encoding.BodyName, canonical);
            AddAliasIfMissing(encoding.HeaderName, canonical);
            AddAliasIfMissing(encoding.CodePage.ToString(CultureInfo.InvariantCulture), canonical);
            AddAliasIfMissing("cp" + encoding.CodePage.ToString(CultureInfo.InvariantCulture), canonical);
            index++;
        }
    }

    void AddCanonicalName(string name)
    {
        int index = 0;
        while (index < canonicalNames.Count)
        {
            if (string.Equals(canonicalNames[index], name, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            index++;
        }

        canonicalNames.Add(name);
    }

    static void ValidateName(string? name, string parameterName)
    {
        if (name == null)
        {
            throw new ArgumentNullException(parameterName);
        }

        if (name.Length == 0)
        {
            throw new ArgumentException("Charset names cannot be empty.", parameterName);
        }

        if (!IsValidName(name))
        {
            throw new ArgumentException("Charset names must contain visible ASCII characters only.", parameterName);
        }
    }

    static bool IsValidName(string name)
    {
        if (name == null || name.Length == 0)
        {
            return false;
        }

        int index = 0;
        while (index < name.Length)
        {
            char character = name[index];
            if (character < 33 || character > 126)
            {
                return false;
            }

            index++;
        }

        return true;
    }

    static EmailCharsetBom? DetectBom(ReadOnlySpan<byte> input)
    {
        if (StartsWith(input, new byte[] { 255, 254, 0, 0 }))
        {
            return new EmailCharsetBom("utf-32", 4);
        }

        if (StartsWith(input, new byte[] { 0, 0, 254, 255 }))
        {
            return new EmailCharsetBom("utf-32be", 4);
        }

        if (StartsWith(input, new byte[] { 239, 187, 191 }))
        {
            return new EmailCharsetBom("utf-8", 3);
        }

        if (StartsWith(input, new byte[] { 255, 254 }))
        {
            return new EmailCharsetBom("utf-16", 2);
        }

        if (StartsWith(input, new byte[] { 254, 255 }))
        {
            return new EmailCharsetBom("utf-16be", 2);
        }

        return null;
    }

    static bool StartsWith(ReadOnlySpan<byte> input, byte[] prefix)
    {
        if (input.Length < prefix.Length)
        {
            return false;
        }

        int index = 0;
        while (index < prefix.Length)
        {
            if (input[index] != prefix[index])
            {
                return false;
            }

            index++;
        }

        return true;
    }

    static EmailCharsetDecodedText DecodeBytes(ReadOnlySpan<byte> input, int payloadOffset, Encoding? encoding, EmailCharsetDecodeOptions options, EmailIssueList issues)
    {
        int codePage = (encoding ?? throw new InvalidOperationException()).CodePage;
        if (codePage == 20127)
        {
            return DecodeAscii(input, payloadOffset, options, issues);
        }

        if (codePage == 65001)
        {
            return DecodeUtf8(input, payloadOffset, options, issues);
        }

        if (codePage == 1200)
        {
            return DecodeUtf16(input, payloadOffset, false, options, issues);
        }

        if (codePage == 1201)
        {
            return DecodeUtf16(input, payloadOffset, true, options, issues);
        }

        if (codePage == 12000)
        {
            return DecodeUtf32(input, payloadOffset, false, options, issues);
        }

        if (codePage == 12001)
        {
            return DecodeUtf32(input, payloadOffset, true, options, issues);
        }

        if (codePage == 28591)
        {
            string text = encoding.GetString(input.Slice(payloadOffset).ToArray());
            return new EmailCharsetDecodedText(text, new byte[0], false);
        }

        return DecodeLegacy(input, payloadOffset, encoding, options, issues);
    }

    static EmailCharsetDecodedText DecodeAscii(ReadOnlySpan<byte> input, int payloadOffset, EmailCharsetDecodeOptions options, EmailIssueList issues)
    {
        StringBuilder text = new StringBuilder();
        List<byte> undecodable = new List<byte>();
        int index = payloadOffset;
        while (index < input.Length)
        {
            byte value = input[index];
            if (value < 128)
            {
                text.Append((char)value);
            }
            else
            {
                ReportInvalid(input, index, 1, text, undecodable, options, issues);
            }

            index++;
        }

        return CreateDecodedText(text, undecodable, options);
    }

    static EmailCharsetDecodedText DecodeUtf8(ReadOnlySpan<byte> input, int payloadOffset, EmailCharsetDecodeOptions options, EmailIssueList issues)
    {
        StringBuilder text = new StringBuilder();
        List<byte> undecodable = new List<byte>();
        int index = payloadOffset;
        while (index < input.Length)
        {
            byte first = input[index];
            if (first < 128)
            {
                text.Append((char)first);
                index++;
                continue;
            }

            int length = GetUtf8SequenceLength(input, index);
            if (length == 0)
            {
                int invalidLength = GetInvalidUtf8SequenceLength(input, index);
                ReportInvalid(input, index, invalidLength, text, undecodable, options, issues);
                index += invalidLength;
                continue;
            }

            int scalar = GetUtf8Scalar(input, index, length);
            text.Append(char.ConvertFromUtf32(scalar));
            index += length;
        }

        return CreateDecodedText(text, undecodable, options);
    }

    static int GetUtf8SequenceLength(ReadOnlySpan<byte> input, int index)
    {
        byte first = input[index];
        if (first >= 194 && first <= 223)
        {
            if (index + 1 < input.Length && IsContinuation(input[index + 1]))
            {
                return 2;
            }

            return 0;
        }

        if (first >= 224 && first <= 239)
        {
            if (index + 2 >= input.Length || !IsContinuation(input[index + 2]))
            {
                return 0;
            }

            byte second = input[index + 1];
            if (first == 224 && second >= 160 && second <= 191)
            {
                return 3;
            }

            if (first == 237 && second >= 128 && second <= 159)
            {
                return 3;
            }

            if (first != 224 && first != 237 && IsContinuation(second))
            {
                return 3;
            }

            return 0;
        }

        if (first >= 240 && first <= 244)
        {
            if (index + 3 >= input.Length || !IsContinuation(input[index + 2]) || !IsContinuation(input[index + 3]))
            {
                return 0;
            }

            byte second = input[index + 1];
            if (first == 240 && second >= 144 && second <= 191)
            {
                return 4;
            }

            if (first == 244 && second >= 128 && second <= 143)
            {
                return 4;
            }

            if (first != 240 && first != 244 && IsContinuation(second))
            {
                return 4;
            }
        }

        return 0;
    }

    static int GetInvalidUtf8SequenceLength(ReadOnlySpan<byte> input, int index)
    {
        byte first = input[index];
        int expected = 0;
        if (first >= 194 && first <= 223)
        {
            expected = 2;
        }
        else if (first >= 224 && first <= 239)
        {
            expected = 3;
        }
        else if (first >= 240 && first <= 244)
        {
            expected = 4;
        }
        else
        {
            return 1;
        }

        if (index + 1 >= input.Length)
        {
            return 1;
        }

        byte second = input[index + 1];
        bool validSecond = IsContinuation(second);
        if (first == 224)
        {
            validSecond = second >= 160 && second <= 191;
        }
        else if (first == 237)
        {
            validSecond = second >= 128 && second <= 159;
        }
        else if (first == 240)
        {
            validSecond = second >= 144 && second <= 191;
        }
        else if (first == 244)
        {
            validSecond = second >= 128 && second <= 143;
        }

        if (!validSecond)
        {
            return 1;
        }

        int length = 2;
        while (length < expected && index + length < input.Length && IsContinuation(input[index + length]))
        {
            length++;
        }

        return length;
    }

    static bool IsContinuation(byte value)
    {
        return value >= 128 && value <= 191;
    }

    static int GetUtf8Scalar(ReadOnlySpan<byte> input, int index, int length)
    {
        if (length == 2)
        {
            return ((input[index] & 31) << 6) | (input[index + 1] & 63);
        }

        if (length == 3)
        {
            return ((input[index] & 15) << 12) | ((input[index + 1] & 63) << 6) | (input[index + 2] & 63);
        }

        return ((input[index] & 7) << 18) | ((input[index + 1] & 63) << 12) | ((input[index + 2] & 63) << 6) | (input[index + 3] & 63);
    }

    static EmailCharsetDecodedText DecodeUtf16(ReadOnlySpan<byte> input, int payloadOffset, bool bigEndian, EmailCharsetDecodeOptions options, EmailIssueList issues)
    {
        StringBuilder text = new StringBuilder();
        List<byte> undecodable = new List<byte>();
        int index = payloadOffset;
        while (index + 1 < input.Length)
        {
            int first = GetUtf16Unit(input, index, bigEndian);
            if (first >= 55296 && first <= 56319)
            {
                if (index + 3 < input.Length)
                {
                    int second = GetUtf16Unit(input, index + 2, bigEndian);
                    if (second >= 56320 && second <= 57343)
                    {
                        text.Append((char)first);
                        text.Append((char)second);
                        index += 4;
                        continue;
                    }
                }

                ReportInvalid(input, index, 2, text, undecodable, options, issues);
                index += 2;
                continue;
            }

            if (first >= 56320 && first <= 57343)
            {
                ReportInvalid(input, index, 2, text, undecodable, options, issues);
                index += 2;
                continue;
            }

            text.Append((char)first);
            index += 2;
        }

        if (index < input.Length)
        {
            ReportInvalid(input, index, 1, text, undecodable, options, issues);
        }

        return CreateDecodedText(text, undecodable, options);
    }

    static int GetUtf16Unit(ReadOnlySpan<byte> input, int index, bool bigEndian)
    {
        if (bigEndian)
        {
            return (input[index] << 8) | input[index + 1];
        }

        return input[index] | (input[index + 1] << 8);
    }

    static EmailCharsetDecodedText DecodeUtf32(ReadOnlySpan<byte> input, int payloadOffset, bool bigEndian, EmailCharsetDecodeOptions options, EmailIssueList issues)
    {
        StringBuilder text = new StringBuilder();
        List<byte> undecodable = new List<byte>();
        int index = payloadOffset;
        while (index + 3 < input.Length)
        {
            long scalar = GetUtf32Unit(input, index, bigEndian);
            if (scalar > 1114111 || scalar >= 55296 && scalar <= 57343)
            {
                ReportInvalid(input, index, 4, text, undecodable, options, issues);
            }
            else
            {
                text.Append(char.ConvertFromUtf32((int)scalar));
            }

            index += 4;
        }

        if (index < input.Length)
        {
            ReportInvalid(input, index, input.Length - index, text, undecodable, options, issues);
        }

        return CreateDecodedText(text, undecodable, options);
    }

    static long GetUtf32Unit(ReadOnlySpan<byte> input, int index, bool bigEndian)
    {
        if (bigEndian)
        {
            return ((long)input[index] << 24) | ((long)input[index + 1] << 16) | ((long)input[index + 2] << 8) | input[index + 3];
        }

        return input[index] | ((long)input[index + 1] << 8) | ((long)input[index + 2] << 16) | ((long)input[index + 3] << 24);
    }

    static EmailCharsetDecodedText DecodeLegacy(ReadOnlySpan<byte> input, int payloadOffset, Encoding encoding, EmailCharsetDecodeOptions options, EmailIssueList issues)
    {
        Encoding decoder = (Encoding)encoding.Clone();
        decoder.DecoderFallback = new DecoderReplacementFallback(InvalidSentinel);
        ReadOnlySpan<byte> payload = input.Slice(payloadOffset);
        string decoded = decoder.GetString(payload.ToArray());
        List<byte> undecodable = new List<byte>();
        if (decoded.IndexOf(InvalidSentinel, StringComparison.Ordinal) >= 0)
        {
            int index = payloadOffset;
            while (index < input.Length)
            {
                int validLength = FindValidLegacySequenceLength(input, index, decoder);
                if (validLength > 0)
                {
                    index += validLength;
                }
                else
                {
                    ReportInvalid(input, index, 1, null, undecodable, options, issues);
                    index++;
                }
            }
        }

        string text = decoded;
        if (options.GetFallbackPolicy() == EmailCharsetFallbackPolicy.Strict)
        {
            text = decoded.Replace(InvalidSentinel, "", StringComparison.Ordinal);
        }
        else
        {
            text = decoded.Replace(InvalidSentinel, options.GetReplacement(), StringComparison.Ordinal);
        }

        bool usedReplacement = undecodable.Count > 0 && options.GetFallbackPolicy() == EmailCharsetFallbackPolicy.Replacement;
        return new EmailCharsetDecodedText(text, undecodable.ToArray(), usedReplacement);
    }

    static int FindValidLegacySequenceLength(ReadOnlySpan<byte> input, int index, Encoding decoder)
    {
        int maximum = input.Length - index;
        if (maximum > 16)
        {
            maximum = 16;
        }

        int length = 1;
        while (length <= maximum)
        {
            string candidate = decoder.GetString(input.Slice(index, length).ToArray());
            if (candidate.Length > 0 && candidate.IndexOf(InvalidSentinel, StringComparison.Ordinal) < 0)
            {
                return length;
            }

            length++;
        }

        return 0;
    }

    static void ReportInvalid(ReadOnlySpan<byte> input, int index, int count, StringBuilder? text, List<byte> undecodable, EmailCharsetDecodeOptions options, EmailIssueList issues)
    {
        int byteIndex = 0;
        while (byteIndex < count)
        {
            undecodable.Add(input[index + byteIndex]);
            byteIndex++;
        }

        EmailIssueSeverity severity = EmailIssueSeverity.Error;
        string recovery = "The invalid bytes were omitted from the decoded text.";
        if (options.GetFallbackPolicy() == EmailCharsetFallbackPolicy.Replacement)
        {
            severity = EmailIssueSeverity.Warning;
            recovery = "The configured replacement text was inserted for the invalid bytes.";
            if (text != null)
            {
                text.Append(options.GetReplacement());
            }
        }

        EmailSourceTracker tracker = new EmailSourceTrackerOperations().CreateWith();
        tracker.Advance(input.Slice(0, index));
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(severity, EmailIssueCode.CharsetInvalidByteSequence, "The input contains a byte sequence that is invalid for the selected charset.", tracker.GetPosition(), null, null, recovery));
    }

    static EmailCharsetDecodedText CreateDecodedText(StringBuilder text, List<byte> undecodable, EmailCharsetDecodeOptions options)
    {
        bool usedReplacement = undecodable.Count > 0 && options.GetFallbackPolicy() == EmailCharsetFallbackPolicy.Replacement;
        return new EmailCharsetDecodedText(text.ToString(), undecodable.ToArray(), usedReplacement);
    }

    static string ApplyCharacterLimit(string text, int maximumCharacters, EmailIssueList issues)
    {
        if (text.Length <= maximumCharacters)
        {
            return text;
        }

        int length = maximumCharacters;
        if (length > 0 && length < text.Length && char.IsHighSurrogate(text[length - 1]) && char.IsLowSurrogate(text[length]))
        {
            length--;
        }

        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Fatal, EmailIssueCode.CharsetCharacterLimitExceeded, "The decoded text exceeds the configured character limit.", new EmailSourcePosition(0, 1, 1), null, null, "The decoded text was truncated at a Unicode scalar boundary."));
        return text.Substring(0, length);
    }
}
