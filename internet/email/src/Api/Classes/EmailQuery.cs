using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Represents a query.
/// </summary>
public class EmailQuery : IEmailQueryContract
{
    EmailQueryKind kind;
    string? fieldName;
    string? text;
    EmailAddress? address;
    EmailDateTime? firstDate;
    EmailDateTime? secondDate;
    string? topLevelType;
    string? subtype;
    EmailIssueCode issueCode;
    EmailIssueSeverity issueSeverity;
    EmailQuerySecurityState securityState;
    List<EmailQuery> children;
    EmailQuery(EmailQueryKind kind)
    {
        this.kind = kind;
        fieldName = null;
        text = null;
        address = null;
        firstDate = null;
        secondDate = null;
        topLevelType = null;
        subtype = null;
        issueCode = EmailIssueCode.Unknown;
        issueSeverity = EmailIssueSeverity.Information;
        securityState = EmailQuerySecurityState.None;
        children = new List<EmailQuery>();
    }

    internal static EmailQuery HasFieldImplementation(string fieldName)
    {
        EmailQuery query = new EmailQuery(EmailQueryKind.HasField);
        query.fieldName = ValidateFieldName(fieldName);
        return query;
    }

    internal static EmailQuery RawFieldContainsImplementation(string fieldName, string value)
    {
        return CreateFieldText(EmailQueryKind.RawFieldContains, fieldName, value);
    }

    internal static EmailQuery RawFieldEqualsImplementation(string fieldName, string value)
    {
        return CreateFieldText(EmailQueryKind.RawFieldEquals, fieldName, value);
    }

    internal static EmailQuery DecodedFieldContainsImplementation(string fieldName, string value)
    {
        return CreateFieldText(EmailQueryKind.DecodedFieldContains, fieldName, value);
    }

    internal static EmailQuery DecodedFieldEqualsImplementation(string fieldName, string value)
    {
        return CreateFieldText(EmailQueryKind.DecodedFieldEquals, fieldName, value);
    }

    internal static EmailQuery AddressEqualsImplementationWithValue(EmailAddress value)
    {
        return CreateAddress(EmailQueryKind.AddressEquals, null, value);
    }

    internal static EmailQuery AddressEqualsImplementationWithFieldNameAndValue(string fieldName, EmailAddress value)
    {
        return CreateAddress(EmailQueryKind.AddressEquals, ValidateFieldName(fieldName), value);
    }

    internal static EmailQuery AddressDomainEqualsImplementationWithDomain(string domain)
    {
        EmailQuery query = new EmailQuery(EmailQueryKind.AddressDomainEquals);
        query.text = NormalizeDomain(domain);
        return query;
    }

    internal static EmailQuery AddressDomainEqualsImplementationWithFieldNameAndDomain(string fieldName, string domain)
    {
        EmailQuery query = AddressDomainEqualsImplementationWithDomain(domain);
        query.fieldName = ValidateFieldName(fieldName);
        return query;
    }

    internal static EmailQuery DateBeforeImplementationWithValue(EmailDateTime value)
    {
        return DateBeforeImplementationWithFieldNameAndValue("Date", value);
    }

    internal static EmailQuery DateBeforeImplementationWithFieldNameAndValue(string fieldName, EmailDateTime value)
    {
        return CreateDate(EmailQueryKind.DateBefore, fieldName, value, null);
    }

    internal static EmailQuery DateOnOrAfterImplementationWithValue(EmailDateTime value)
    {
        return DateOnOrAfterImplementationWithFieldNameAndValue("Date", value);
    }

    internal static EmailQuery DateOnOrAfterImplementationWithFieldNameAndValue(string fieldName, EmailDateTime value)
    {
        return CreateDate(EmailQueryKind.DateOnOrAfter, fieldName, value, null);
    }

    internal static EmailQuery DateEqualsImplementationWithValue(EmailDateTime value)
    {
        return DateEqualsImplementationWithFieldNameAndValue("Date", value);
    }

    internal static EmailQuery DateEqualsImplementationWithFieldNameAndValue(string fieldName, EmailDateTime value)
    {
        return CreateDate(EmailQueryKind.DateEquals, fieldName, value, null);
    }

    internal static EmailQuery DateBetweenImplementationWithEarliestInclusiveAndLatestExclusive(EmailDateTime earliestInclusive, EmailDateTime latestExclusive)
    {
        return DateBetweenImplementationWithFieldNameAndEarliestInclusiveAndLatestExclusive("Date", earliestInclusive, latestExclusive);
    }

    internal static EmailQuery DateBetweenImplementationWithFieldNameAndEarliestInclusiveAndLatestExclusive(string fieldName, EmailDateTime earliestInclusive, EmailDateTime latestExclusive)
    {
        if (earliestInclusive == null)
        {
            throw new ArgumentNullException("earliestInclusive");
        }

        if (latestExclusive == null)
        {
            throw new ArgumentNullException("latestExclusive");
        }

        if (CompareDates(earliestInclusive, latestExclusive) >= 0)
        {
            throw new ArgumentException("The exclusive upper date bound must follow the inclusive lower bound.", "latestExclusive");
        }

        return CreateDate(EmailQueryKind.DateBetween, fieldName, earliestInclusive, latestExclusive);
    }

    internal static EmailQuery MimeTypeImplementation(string topLevelType, string subtype)
    {
        EmailQuery query = new EmailQuery(EmailQueryKind.MimeType);
        query.topLevelType = ValidateMimeToken(topLevelType, "topLevelType");
        query.subtype = ValidateMimeToken(subtype, "subtype");
        return query;
    }

    internal static EmailQuery MimeTopLevelTypeImplementation(string topLevelType)
    {
        EmailQuery query = new EmailQuery(EmailQueryKind.MimeTopLevelType);
        query.topLevelType = ValidateMimeToken(topLevelType, "topLevelType");
        return query;
    }

    internal static EmailQuery HasAttachmentImplementation()
    {
        return new EmailQuery(EmailQueryKind.HasAttachment);
    }

    internal static EmailQuery AttachmentFilenameContainsImplementation(string value)
    {
        EmailQuery query = new EmailQuery(EmailQueryKind.AttachmentFilenameContains);
        query.text = ValidateText(value, "value");
        return query;
    }

    internal static EmailQuery AttachmentMimeTypeImplementation(string topLevelType, string subtype)
    {
        EmailQuery query = MimeTypeImplementation(topLevelType, subtype);
        query.kind = EmailQueryKind.AttachmentMimeType;
        return query;
    }

    internal static EmailQuery BodyContainsImplementation(string value)
    {
        EmailQuery query = new EmailQuery(EmailQueryKind.BodyContains);
        query.text = ValidateText(value, "value");
        return query;
    }

    internal static EmailQuery HasIssueImplementation(EmailIssueCode code)
    {
        if (!new EmailIssueCodeRangesOperations().IsLibraryCode((int)code))
        {
            throw new ArgumentOutOfRangeException("code");
        }

        EmailQuery query = new EmailQuery(EmailQueryKind.HasIssueCode);
        query.issueCode = code;
        return query;
    }

    internal static EmailQuery HasIssueSeverityAtLeastImplementation(EmailIssueSeverity severity)
    {
        if (severity < EmailIssueSeverity.Information || severity > EmailIssueSeverity.Fatal)
        {
            throw new ArgumentOutOfRangeException("severity");
        }

        EmailQuery query = new EmailQuery(EmailQueryKind.HasIssueSeverityAtLeast);
        query.issueSeverity = severity;
        return query;
    }

    internal static EmailQuery HasSecurityStateImplementation(EmailQuerySecurityState state)
    {
        if (state < EmailQuerySecurityState.None || state > EmailQuerySecurityState.Report)
        {
            throw new ArgumentOutOfRangeException("state");
        }

        EmailQuery query = new EmailQuery(EmailQueryKind.SecurityState);
        query.securityState = state;
        return query;
    }

    internal static EmailQuery AndImplementationWithLeftAndRight(EmailQuery left, EmailQuery right)
    {
        return CreateBinary(EmailQueryKind.And, left, right);
    }

    internal static EmailQuery AndImplementationWithQueries(List<EmailQuery> queries)
    {
        return CreateComposite(EmailQueryKind.And, queries, EmailQueryOptionsConstants.DefaultMaximumChildrenPerNode);
    }

    internal static EmailQuery AndImplementationWithQueriesAndOptions(List<EmailQuery> queries, EmailQueryOptions options)
    {
        return CreateComposite(EmailQueryKind.And, queries, GetConstructionChildLimit(options));
    }

    internal static EmailQuery AllImplementationWithQueries(List<EmailQuery> queries)
    {
        return AndImplementationWithQueries(queries);
    }

    internal static EmailQuery AllImplementationWithQueriesAndOptions(List<EmailQuery> queries, EmailQueryOptions options)
    {
        return AndImplementationWithQueriesAndOptions(queries, options);
    }

    internal static EmailQuery OrImplementationWithLeftAndRight(EmailQuery left, EmailQuery right)
    {
        return CreateBinary(EmailQueryKind.Or, left, right);
    }

    internal static EmailQuery OrImplementationWithQueries(List<EmailQuery> queries)
    {
        return CreateComposite(EmailQueryKind.Or, queries, EmailQueryOptionsConstants.DefaultMaximumChildrenPerNode);
    }

    internal static EmailQuery OrImplementationWithQueriesAndOptions(List<EmailQuery> queries, EmailQueryOptions options)
    {
        return CreateComposite(EmailQueryKind.Or, queries, GetConstructionChildLimit(options));
    }

    internal static EmailQuery AnyImplementationWithQueries(List<EmailQuery> queries)
    {
        return OrImplementationWithQueries(queries);
    }

    internal static EmailQuery AnyImplementationWithQueriesAndOptions(List<EmailQuery> queries, EmailQueryOptions options)
    {
        return OrImplementationWithQueriesAndOptions(queries, options);
    }

    internal static EmailQuery NotImplementation(EmailQuery query)
    {
        if (query == null)
        {
            throw new ArgumentNullException("query");
        }

        EmailQuery result = new EmailQuery(EmailQueryKind.Not);
        result.children.Add(query);
        return result;
    }

    /// <summary>
    /// Gets the kind.
    /// </summary>
    /// <returns>The current kind.</returns>
    public EmailQueryKind GetKind()
    {
        return kind;
    }

    /// <summary>
    /// Determines whether the field name is present or available.
    /// </summary>
    /// <returns><c>true</c> when the field name is available; otherwise, <c>false</c>.</returns>
    public bool HasFieldName()
    {
        return fieldName != null;
    }

    /// <summary>
    /// Gets the field name.
    /// </summary>
    /// <returns>The current field name.</returns>
    public string? GetFieldName()
    {
        return fieldName;
    }

    /// <summary>
    /// Determines whether the text is present or available.
    /// </summary>
    /// <returns><c>true</c> when the text is available; otherwise, <c>false</c>.</returns>
    public bool HasText()
    {
        return text != null;
    }

    /// <summary>
    /// Gets the text.
    /// </summary>
    /// <returns>The current text.</returns>
    public string? GetText()
    {
        return text;
    }

    /// <summary>
    /// Gets the child count.
    /// </summary>
    /// <returns>The current child count.</returns>
    public int GetChildCount()
    {
        return children.Count;
    }

    /// <summary>
    /// Gets the child.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The current child.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailQuery GetChild(int index)
    {
        if (index < 0 || index >= children.Count)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        return children[index];
    }

    /// <summary>
    /// Gets the children.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current children.</returns>
    public List<EmailQuery> GetChildren()
    {
        return new List<EmailQuery>(children);
    }

    /// <summary>
    /// Matches the es.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool Matches(EmailMessage message)
    {
        return MatchesWithOptions(message, new EmailQueryOptions());
    }

    /// <summary>
    /// Matches the es.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public bool MatchesWithOptions(EmailMessage message, EmailQueryOptions options)
    {
        if (message == null)
        {
            throw new ArgumentNullException("message");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        EmailQueryOptions snapshot = options.Clone();
        EmailQueryEvaluationBudget budget = new EmailQueryEvaluationBudget(snapshot.GetMaximumQueryNodeCount());
        ValidateTree(snapshot, 1, budget);
        return MatchesCore(message, snapshot);
    }

    bool MatchesCore(EmailMessage message, EmailQueryOptions options)
    {
        if (kind == EmailQueryKind.And)
        {
            int index = 0;
            while (index < children.Count)
            {
                if (!children[index].MatchesCore(message, options))
                {
                    return false;
                }

                index++;
            }

            return true;
        }

        if (kind == EmailQueryKind.Or)
        {
            int index = 0;
            while (index < children.Count)
            {
                if (children[index].MatchesCore(message, options))
                {
                    return true;
                }

                index++;
            }

            return false;
        }

        if (kind == EmailQueryKind.Not)
        {
            return !children[0].MatchesCore(message, options);
        }

        if (kind == EmailQueryKind.HasField)
        {
            return MatchHasField(message, options);
        }

        if (kind >= EmailQueryKind.RawFieldContains && kind <= EmailQueryKind.DecodedFieldEquals)
        {
            return MatchFieldText(message, options);
        }

        if (kind == EmailQueryKind.AddressEquals || kind == EmailQueryKind.AddressDomainEquals)
        {
            return MatchAddresses(message, options);
        }

        if (kind >= EmailQueryKind.DateBefore && kind <= EmailQueryKind.DateBetween)
        {
            return MatchDates(message, options);
        }

        if (kind == EmailQueryKind.MimeType || kind == EmailQueryKind.MimeTopLevelType)
        {
            return MatchMime(message, options);
        }

        if (kind == EmailQueryKind.HasAttachment || kind == EmailQueryKind.AttachmentFilenameContains || kind == EmailQueryKind.AttachmentMimeType)
        {
            return MatchAttachments(message, options);
        }

        if (kind == EmailQueryKind.BodyContains)
        {
            return MatchBody(message, options);
        }

        if (kind == EmailQueryKind.HasIssueCode || kind == EmailQueryKind.HasIssueSeverityAtLeast)
        {
            return MatchIssues(message, options);
        }

        return MatchSecurity(message, options);
    }

    static EmailQuery CreateBinary(EmailQueryKind kind, EmailQuery left, EmailQuery right)
    {
        if (left == null)
        {
            throw new ArgumentNullException("left");
        }

        if (right == null)
        {
            throw new ArgumentNullException("right");
        }

        List<EmailQuery> queries = new List<EmailQuery>();
        queries.Add(left);
        queries.Add(right);
        return CreateComposite(kind, queries, EmailQueryOptionsConstants.DefaultMaximumChildrenPerNode);
    }

    static EmailQuery CreateComposite(EmailQueryKind kind, List<EmailQuery> queries, int maximumChildren)
    {
        if (queries == null)
        {
            throw new ArgumentNullException("queries");
        }

        if (queries.Count < 2)
        {
            throw new ArgumentException("AND and OR query nodes require at least two children.", "queries");
        }

        if (queries.Count > maximumChildren)
        {
            throw new EmailLimitException("MaxQueryChildren", maximumChildren, queries.Count);
        }

        EmailQuery query = new EmailQuery(kind);
        int index = 0;
        while (index < queries.Count)
        {
            if (queries[index] == null)
            {
                throw new ArgumentException("A composed query cannot contain a null child.", "queries");
            }

            query.children.Add(queries[index]);
            index++;
        }

        return query;
    }

    static int GetConstructionChildLimit(EmailQueryOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        return options.GetMaximumChildrenPerNode();
    }

    static EmailQuery CreateFieldText(EmailQueryKind kind, string fieldName, string value)
    {
        EmailQuery query = new EmailQuery(kind);
        query.fieldName = ValidateFieldName(fieldName);
        query.text = ValidateText(value, "value");
        return query;
    }

    static EmailQuery CreateAddress(EmailQueryKind kind, string? fieldName, EmailAddress value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        EmailQuery query = new EmailQuery(kind);
        query.fieldName = fieldName;
        query.address = value;
        return query;
    }

    static EmailQuery CreateDate(EmailQueryKind kind, string fieldName, EmailDateTime first, EmailDateTime? second)
    {
        if (first == null)
        {
            throw new ArgumentNullException("value");
        }

        EmailQuery query = new EmailQuery(kind);
        query.fieldName = ValidateFieldName(fieldName);
        query.firstDate = first;
        query.secondDate = second;
        return query;
    }

    bool MatchHasField(EmailMessage message, EmailQueryOptions options)
    {
        int inspected = 0;
        int index = 0;
        while (index < (message.GetHeaders() ?? throw new InvalidOperationException()).GetCount())
        {
            EnsureHeaderCount(inspected, options);
            inspected++;
            if (string.Equals((message.GetHeaders() ?? throw new InvalidOperationException()).GetAt(index).GetName(), fieldName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    bool MatchFieldText(EmailMessage message, EmailQueryOptions options)
    {
        int inspected = 0;
        int characters = 0;
        int index = 0;
        while (index < (message.GetHeaders() ?? throw new InvalidOperationException()).GetCount())
        {
            EnsureHeaderCount(inspected, options);
            inspected++;
            EmailHeader header = (message.GetHeaders() ?? throw new InvalidOperationException()).GetAt(index);
            if (string.Equals(header.GetName(), fieldName, StringComparison.OrdinalIgnoreCase))
            {
                bool decoded = kind == EmailQueryKind.DecodedFieldContains || kind == EmailQueryKind.DecodedFieldEquals;
                byte[] rawValue = header.GetRawValue();
                characters = AddFieldCharacters(characters, rawValue.Length, options);
                string? candidate = decoded ? header.GetDecodedValue() : Encoding.UTF8.GetString(rawValue).Trim();
                bool equals = kind == EmailQueryKind.RawFieldEquals || kind == EmailQueryKind.DecodedFieldEquals;
                if (equals ? TextEquals(candidate, text, options) : TextContains(candidate, text, options))
                {
                    return true;
                }
            }

            index++;
        }

        return false;
    }

    bool MatchAddresses(EmailMessage message, EmailQueryOptions options)
    {
        int inspected = 0;
        int characters = 0;
        int index = 0;
        while (index < (message.GetHeaders() ?? throw new InvalidOperationException()).GetCount())
        {
            EnsureHeaderCount(inspected, options);
            inspected++;
            EmailHeader header = (message.GetHeaders() ?? throw new InvalidOperationException()).GetAt(index);
            if (ShouldInspectAddressHeader(header))
            {
                byte[] value = header.GetUnfoldedValue();
                characters = AddFieldCharacters(characters, value.Length, options);
                EmailAddressList list = new EmailAddressParser().ParseListWithInputAndMode(value, EmailAddressParseMode.Tolerant).GetAddressList();
                if (AddressListMatches(list))
                {
                    return true;
                }
            }

            index++;
        }

        return false;
    }

    bool ShouldInspectAddressHeader(EmailHeader header)
    {
        if (fieldName != null)
        {
            return string.Equals(header.GetName(), fieldName, StringComparison.OrdinalIgnoreCase);
        }

        string name = header.GetName();
        return string.Equals(name, "From", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "Sender", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "Reply-To", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "To", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "Cc", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "Bcc", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "Disposition-Notification-To", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "Resent-From", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "Resent-Sender", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "Resent-To", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "Resent-Cc", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "Resent-Bcc", StringComparison.OrdinalIgnoreCase);
    }

    bool AddressListMatches(EmailAddressList list)
    {
        int index = 0;
        while (index < list.GetCount())
        {
            EmailAddressListEntry entry = list.GetAt(index);
            if (entry.IsMailbox())
            {
                if (AddressMatches(entry.GetMailbox()))
                {
                    return true;
                }
            }
            else
            {
                EmailAddressGroup? group = entry.GetGroup();
                int groupIndex = 0;
                while (groupIndex < (group ?? throw new InvalidOperationException()).GetAddressCount())
                {
                    if (AddressMatches(group.GetAddress(groupIndex)))
                    {
                        return true;
                    }

                    groupIndex++;
                }
            }

            index++;
        }

        return false;
    }

    bool AddressMatches(EmailAddress? candidate)
    {
        if (kind == EmailQueryKind.AddressEquals)
        {
            return string.Equals((candidate ?? throw new InvalidOperationException()).GetLocalPart(), (address ?? throw new InvalidOperationException()).GetLocalPart(), StringComparison.Ordinal) && DomainsEqual(candidate, address);
        }

        string candidateDomain = (candidate ?? throw new InvalidOperationException()).IsDomainLiteral() ? candidate.GetDomain().ToLowerInvariant() : candidate.GetAsciiDomain();
        return string.Equals(candidateDomain, text, StringComparison.OrdinalIgnoreCase);
    }

    bool MatchDates(EmailMessage message, EmailQueryOptions options)
    {
        int inspected = 0;
        int characters = 0;
        int index = 0;
        while (index < (message.GetHeaders() ?? throw new InvalidOperationException()).GetCount())
        {
            EnsureHeaderCount(inspected, options);
            inspected++;
            EmailHeader header = (message.GetHeaders() ?? throw new InvalidOperationException()).GetAt(index);
            if (string.Equals(header.GetName(), fieldName, StringComparison.OrdinalIgnoreCase))
            {
                byte[] value = header.GetUnfoldedValue();
                characters = AddFieldCharacters(characters, value.Length, options);
                EmailDateParseResult parsed = new EmailDateParser().ParseWithInputAndMode(value, EmailDateParseMode.Tolerant);
                if (parsed.HasValue() && DateMatches(parsed.GetValue(), options))
                {
                    return true;
                }
            }

            index++;
        }

        return false;
    }

    bool DateMatches(EmailDateTime? candidate, EmailQueryOptions options)
    {
        if (options.GetRequireKnownDateZone() && (!(candidate ?? throw new InvalidOperationException()).HasKnownZoneOffset() || !(firstDate ?? throw new InvalidOperationException()).HasKnownZoneOffset() || secondDate != null && !secondDate.HasKnownZoneOffset()))
        {
            return false;
        }

        int firstComparison = CompareDates(candidate, firstDate);
        if (kind == EmailQueryKind.DateBefore)
        {
            return firstComparison < 0;
        }

        if (kind == EmailQueryKind.DateOnOrAfter)
        {
            return firstComparison >= 0;
        }

        if (kind == EmailQueryKind.DateEquals)
        {
            return firstComparison == 0;
        }

        return firstComparison >= 0 && CompareDates(candidate, secondDate) < 0;
    }

    bool MatchMime(EmailMessage message, EmailQueryOptions options)
    {
        if (!message.HasBody())
        {
            return false;
        }

        List<MimeEntity> entities = new EmailTraversal().DepthFirstWithMessageAndMaximumDepthAndMaximumEntityCount(message, options.GetMaximumDepth(), options.GetMaximumEntityCount());
        int index = 0;
        while (index < entities.Count)
        {
            EmailMediaType? mediaType = entities[index].GetMediaType();
            if (kind == EmailQueryKind.MimeType && (mediaType ?? throw new InvalidOperationException()).IsType(topLevelType, subtype))
            {
                return true;
            }

            if (kind == EmailQueryKind.MimeTopLevelType && string.Equals((mediaType ?? throw new InvalidOperationException()).GetTopLevelType(), topLevelType, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    bool MatchAttachments(EmailMessage message, EmailQueryOptions options)
    {
        if (!message.HasBody())
        {
            return false;
        }

        List<EmailAttachmentView> attachments = new EmailAttachmentFinder().FindWithMessageAndMaximumDepthAndMaximumEntityCount(message, options.GetMaximumDepth(), options.GetMaximumEntityCount());
        if (kind == EmailQueryKind.HasAttachment)
        {
            return attachments.Count > 0;
        }

        int index = 0;
        while (index < attachments.Count)
        {
            EmailAttachmentView attachment = attachments[index];
            if (kind == EmailQueryKind.AttachmentMimeType && (attachment.GetMediaType() ?? throw new InvalidOperationException()).IsType(topLevelType, subtype))
            {
                return true;
            }

            if (kind == EmailQueryKind.AttachmentFilenameContains && attachment.HasFilename() && TextContains(attachment.GetFilename(), text, options))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    bool MatchBody(EmailMessage message, EmailQueryOptions options)
    {
        if (!message.HasBody())
        {
            return false;
        }

        EmailTextSearchOptions searchOptions = new EmailTextSearchOptions();
        searchOptions.SetMaximumDepth(options.GetMaximumDepth());
        searchOptions.SetMaximumEntityCount(options.GetMaximumEntityCount());
        searchOptions.SetMaximumQueryCharacters(options.GetMaximumQueryCharacters());
        searchOptions.SetMaximumTotalCharacters(options.GetMaximumBodyCharacters());
        searchOptions.SetMaximumMatches(1);
        searchOptions.SetMaximumContextCharacters(1);
        searchOptions.SetCaseSensitive(options.GetCaseSensitive());
        searchOptions.SetIncludeTextAttachments(options.GetIncludeTextAttachments());
        searchOptions.SetIncludeHtml(options.GetIncludeHtml());
        EmailTextDecodeOptions textOptions = searchOptions.GetTextOptions();
        textOptions.SetMaximumDecodedBytes(options.GetMaximumBodyBytes());
        searchOptions.SetTextOptions(textOptions);
        EmailHtmlLimits htmlLimits = searchOptions.GetHtmlOptions().GetLimits();
        htmlLimits.SetMaxInputCharacters(options.GetMaximumBodyCharacters());
        htmlLimits.SetMaxOutputCharacters(options.GetMaximumBodyCharacters());
        EmailHtmlTextOptions htmlOptions = searchOptions.GetHtmlOptions();
        htmlOptions.SetLimits(htmlLimits);
        searchOptions.SetHtmlOptions(htmlOptions);
        EmailTextSearchResult result = new EmailTextProcessor().SearchWithMessageAndQueryAndOptions(message, text ?? throw new InvalidOperationException(), searchOptions);
        if (result.GetMatchCount() > 0)
        {
            return true;
        }

        if (result.WasTruncated())
        {
            throw new EmailLimitException("MaxQueryBodyCharacters", options.GetMaximumBodyCharacters(), (long)options.GetMaximumBodyCharacters() + 1);
        }

        return false;
    }

    bool MatchIssues(EmailMessage message, EmailQueryOptions options)
    {
        if (message.HasBody())
        {
            new EmailTraversal().DepthFirstWithMessageAndMaximumDepthAndMaximumEntityCount(message, options.GetMaximumDepth(), options.GetMaximumEntityCount());
        }

        EmailIssueList issues = message.GetIssues();
        if (kind == EmailQueryKind.HasIssueCode)
        {
            return issues.ContainsCode(issueCode);
        }

        return issues.HasSeverityAtLeast(issueSeverity);
    }

    bool MatchSecurity(EmailMessage message, EmailQueryOptions options)
    {
        bool signed = false;
        bool encrypted = false;
        bool report = false;
        if (message.HasBody())
        {
            List<MimeEntity> entities = new EmailTraversal().DepthFirstWithMessageAndMaximumDepthAndMaximumEntityCount(message, options.GetMaximumDepth(), options.GetMaximumEntityCount());
            int index = 0;
            while (index < entities.Count)
            {
                MimeEntity entity = entities[index];
                if (entity.GetKind() == MimeEntityKind.Signed)
                {
                    signed = true;
                }

                if (entity.GetKind() == MimeEntityKind.Encrypted)
                {
                    encrypted = true;
                }

                if (IsReport(entity))
                {
                    report = true;
                }

                index++;
            }
        }

        if (securityState == EmailQuerySecurityState.None)
        {
            return !signed && !encrypted && !report;
        }

        if (securityState == EmailQuerySecurityState.Signed)
        {
            return signed;
        }

        if (securityState == EmailQuerySecurityState.Encrypted)
        {
            return encrypted;
        }

        if (securityState == EmailQuerySecurityState.SignedAndEncrypted)
        {
            return signed && encrypted;
        }

        return report;
    }

    void ValidateQueryLength(EmailQueryOptions options)
    {
        long length = 0;
        if (fieldName != null)
        {
            length += fieldName.Length;
        }

        if (text != null)
        {
            length += text.Length;
        }

        if (topLevelType != null)
        {
            length += topLevelType.Length;
        }

        if (subtype != null)
        {
            length += subtype.Length;
        }

        if (address != null)
        {
            length += address.GetLocalPart().Length;
            length += address.GetDomain().Length;
            if (address.HasDisplayName())
            {
                length += (address.GetDisplayName() ?? throw new InvalidOperationException()).Length;
            }
        }

        if (length > options.GetMaximumQueryCharacters())
        {
            throw new EmailLimitException("MaxQueryCharacters", options.GetMaximumQueryCharacters(), length);
        }
    }

    void ValidateTree(EmailQueryOptions options, int depth, EmailQueryEvaluationBudget budget)
    {
        if (depth > options.GetMaximumQueryDepth())
        {
            throw new EmailLimitException("MaxQueryNestingDepth", options.GetMaximumQueryDepth(), depth);
        }

        budget.AddNode();
        ValidateQueryLength(options);
        if (children.Count > options.GetMaximumChildrenPerNode())
        {
            throw new EmailLimitException("MaxQueryChildren", options.GetMaximumChildrenPerNode(), children.Count);
        }

        int index = 0;
        while (index < children.Count)
        {
            children[index].ValidateTree(options, depth + 1, budget);
            index++;
        }
    }

    static int AddFieldCharacters(int current, int added, EmailQueryOptions options)
    {
        long total = (long)current + added;
        if (total > options.GetMaximumFieldCharacters())
        {
            throw new EmailLimitException("MaxQueryFieldCharacters", options.GetMaximumFieldCharacters(), total);
        }

        return (int)total;
    }

    static void EnsureHeaderCount(int inspected, EmailQueryOptions options)
    {
        if (inspected >= options.GetMaximumHeaderCount())
        {
            throw new EmailLimitException("MaxQueryHeaders", options.GetMaximumHeaderCount(), inspected + 1);
        }
    }

    static bool TextContains(string? candidate, string? expected, EmailQueryOptions options)
    {
        StringComparison comparison = options.GetCaseSensitive() ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        return (candidate ?? throw new InvalidOperationException()).IndexOf((expected ?? throw new InvalidOperationException()), comparison) >= 0;
    }

    static bool TextEquals(string? candidate, string? expected, EmailQueryOptions options)
    {
        StringComparison comparison = options.GetCaseSensitive() ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        return string.Equals(candidate, expected, comparison);
    }

    static bool DomainsEqual(EmailAddress left, EmailAddress right)
    {
        if (left.IsDomainLiteral() || right.IsDomainLiteral())
        {
            return left.IsDomainLiteral() == right.IsDomainLiteral() && string.Equals(left.GetDomain(), right.GetDomain(), StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(left.GetAsciiDomain(), right.GetAsciiDomain(), StringComparison.OrdinalIgnoreCase);
    }

    static int CompareDates(EmailDateTime? left, EmailDateTime? right)
    {
        long leftValue = GetDateScalar(left);
        long rightValue = GetDateScalar(right);
        if (leftValue < rightValue)
        {
            return -1;
        }

        return leftValue > rightValue ? 1 : 0;
    }

    static long GetDateScalar(EmailDateTime? value)
    {
        int[] daysBeforeMonth = new int[]
        {
            0,
            31,
            59,
            90,
            120,
            151,
            181,
            212,
            243,
            273,
            304,
            334
        };
        long previousYear = (value ?? throw new InvalidOperationException()).GetYear() - 1;
        long days = previousYear * 365 + previousYear / 4 - previousYear / 100 + previousYear / 400;
        days += daysBeforeMonth[value.GetMonth() - 1];
        if (value.GetMonth() > 2 && IsLeapYear(value.GetYear()))
        {
            days++;
        }

        days += value.GetDay() - 1;
        int second = value.HasSecond() ? value.GetSecond() : 0;
        long result = days * 86400 + value.GetHour() * 3600 + value.GetMinute() * 60 + second;
        if (value.HasKnownZoneOffset())
        {
            result -= value.GetZoneOffsetMinutes() * 60;
        }

        return result;
    }

    static bool IsLeapYear(int year)
    {
        return year % 4 == 0 && (year % 100 != 0 || year % 400 == 0);
    }

    static bool IsReport(MimeEntity entity)
    {
        EmailMediaType? mediaType = entity.GetMediaType();
        return (mediaType ?? throw new InvalidOperationException()).IsType("multipart", "report") || entity.GetKind() == MimeEntityKind.DeliveryStatus || entity.GetKind() == MimeEntityKind.DispositionNotification || entity.GetKind() == MimeEntityKind.FeedbackReport;
    }

    static string NormalizeDomain(string value)
    {
        string selected = ValidateText(value, "domain");
        if (selected.Length >= 2 && selected[0] == '[' && selected[selected.Length - 1] == ']')
        {
            return selected.Substring(1, selected.Length - 2).ToLowerInvariant();
        }

        return new EmailAddressIdn().ToAscii(selected);
    }

    static string ValidateFieldName(string value)
    {
        string selected = ValidateText(value, "fieldName");
        int index = 0;
        while (index < selected.Length)
        {
            char current = selected[index];
            if (current < 33 || current > 126 || current == ':')
            {
                throw new ArgumentException("A query field name contains an invalid character.", "fieldName");
            }

            index++;
        }

        return selected;
    }

    static string ValidateMimeToken(string value, string parameterName)
    {
        string selected = ValidateText(value, parameterName);
        int index = 0;
        while (index < selected.Length)
        {
            char current = selected[index];
            if (current < 33 || current > 126 || current == '(' || current == ')' || current == '<' || current == '>' || current == '@' || current == ',' || current == ';' || current == ':' || current == '\\' || current == '"' || current == '/' || current == '[' || current == ']' || current == '?' || current == '=')
            {
                throw new ArgumentException("A query MIME token contains an invalid character.", parameterName);
            }

            index++;
        }

        return selected.ToLowerInvariant();
    }

    static string ValidateText(string value, string parameterName)
    {
        if (value == null)
        {
            throw new ArgumentNullException(parameterName);
        }

        if (value.Length == 0)
        {
            throw new ArgumentException("A query value cannot be empty.", parameterName);
        }

        return value;
    }
}
