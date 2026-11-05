using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Provides the public factory and type-level operations associated with <see cref = "EmailQuery"/>.
/// </summary>
public class EmailQueryOperations : IEmailQueryOperationsContract
{
    /// <summary>
    /// Determines whether the field is present or available.
    /// </summary>
    /// <param name = "fieldName">The field name used by the operation.</param>
    /// <returns>The query produced by the operation.</returns>
    public EmailQuery HasField(string fieldName)
    {
        return EmailQuery.HasFieldImplementation(fieldName);
    }

    /// <summary>
    /// Creates a query that evaluates the raw field contains condition.
    /// </summary>
    /// <param name = "fieldName">The field name used by the operation.</param>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The query produced by the operation.</returns>
    public EmailQuery RawFieldContains(string fieldName, string value)
    {
        return EmailQuery.RawFieldContainsImplementation(fieldName, value);
    }

    /// <summary>
    /// Creates a query that evaluates the raw field equals condition.
    /// </summary>
    /// <param name = "fieldName">The field name used by the operation.</param>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The query produced by the operation.</returns>
    public EmailQuery RawFieldEquals(string fieldName, string value)
    {
        return EmailQuery.RawFieldEqualsImplementation(fieldName, value);
    }

    /// <summary>
    /// Decodes the d field contains.
    /// </summary>
    /// <param name = "fieldName">The field name used by the operation.</param>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailQuery DecodedFieldContains(string fieldName, string value)
    {
        return EmailQuery.DecodedFieldContainsImplementation(fieldName, value);
    }

    /// <summary>
    /// Decodes the d field equals.
    /// </summary>
    /// <param name = "fieldName">The field name used by the operation.</param>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailQuery DecodedFieldEquals(string fieldName, string value)
    {
        return EmailQuery.DecodedFieldEqualsImplementation(fieldName, value);
    }

    /// <summary>
    /// Adds a ress equals to the current collection or model.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The query produced by the operation.</returns>
    public EmailQuery AddressEqualsWithValue(EmailAddress value)
    {
        return EmailQuery.AddressEqualsImplementationWithValue(value);
    }

    /// <summary>
    /// Adds a ress equals to the current collection or model.
    /// </summary>
    /// <param name = "fieldName">The field name used by the operation.</param>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The query produced by the operation.</returns>
    public EmailQuery AddressEqualsWithFieldNameAndValue(string fieldName, EmailAddress value)
    {
        return EmailQuery.AddressEqualsImplementationWithFieldNameAndValue(fieldName, value);
    }

    /// <summary>
    /// Adds a ress domain equals to the current collection or model.
    /// </summary>
    /// <param name = "domain">The domain used by the operation.</param>
    /// <returns>The query produced by the operation.</returns>
    public EmailQuery AddressDomainEqualsWithDomain(string domain)
    {
        return EmailQuery.AddressDomainEqualsImplementationWithDomain(domain);
    }

    /// <summary>
    /// Adds a ress domain equals to the current collection or model.
    /// </summary>
    /// <param name = "fieldName">The field name used by the operation.</param>
    /// <param name = "domain">The domain used by the operation.</param>
    /// <returns>The query produced by the operation.</returns>
    public EmailQuery AddressDomainEqualsWithFieldNameAndDomain(string fieldName, string domain)
    {
        return EmailQuery.AddressDomainEqualsImplementationWithFieldNameAndDomain(fieldName, domain);
    }

    /// <summary>
    /// Creates a query that evaluates the date before condition.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The query produced by the operation.</returns>
    public EmailQuery DateBeforeWithValue(EmailDateTime value)
    {
        return EmailQuery.DateBeforeImplementationWithValue(value);
    }

    /// <summary>
    /// Creates a query that evaluates the date before condition.
    /// </summary>
    /// <param name = "fieldName">The field name used by the operation.</param>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The query produced by the operation.</returns>
    public EmailQuery DateBeforeWithFieldNameAndValue(string fieldName, EmailDateTime value)
    {
        return EmailQuery.DateBeforeImplementationWithFieldNameAndValue(fieldName, value);
    }

    /// <summary>
    /// Creates a query that evaluates the date on or after condition.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The query produced by the operation.</returns>
    public EmailQuery DateOnOrAfterWithValue(EmailDateTime value)
    {
        return EmailQuery.DateOnOrAfterImplementationWithValue(value);
    }

    /// <summary>
    /// Creates a query that evaluates the date on or after condition.
    /// </summary>
    /// <param name = "fieldName">The field name used by the operation.</param>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The query produced by the operation.</returns>
    public EmailQuery DateOnOrAfterWithFieldNameAndValue(string fieldName, EmailDateTime value)
    {
        return EmailQuery.DateOnOrAfterImplementationWithFieldNameAndValue(fieldName, value);
    }

    /// <summary>
    /// Creates a query that evaluates the date equals condition.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The query produced by the operation.</returns>
    public EmailQuery DateEqualsWithValue(EmailDateTime value)
    {
        return EmailQuery.DateEqualsImplementationWithValue(value);
    }

    /// <summary>
    /// Creates a query that evaluates the date equals condition.
    /// </summary>
    /// <param name = "fieldName">The field name used by the operation.</param>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The query produced by the operation.</returns>
    public EmailQuery DateEqualsWithFieldNameAndValue(string fieldName, EmailDateTime value)
    {
        return EmailQuery.DateEqualsImplementationWithFieldNameAndValue(fieldName, value);
    }

    /// <summary>
    /// Creates a query that evaluates the date between condition.
    /// </summary>
    /// <param name = "earliestInclusive">The earliest inclusive used by the operation.</param>
    /// <param name = "latestExclusive">The latest exclusive used by the operation.</param>
    /// <returns>The query produced by the operation.</returns>
    public EmailQuery DateBetweenWithEarliestInclusiveAndLatestExclusive(EmailDateTime earliestInclusive, EmailDateTime latestExclusive)
    {
        return EmailQuery.DateBetweenImplementationWithEarliestInclusiveAndLatestExclusive(earliestInclusive, latestExclusive);
    }

    /// <summary>
    /// Creates a query that evaluates the date between condition.
    /// </summary>
    /// <param name = "fieldName">The field name used by the operation.</param>
    /// <param name = "earliestInclusive">The earliest inclusive used by the operation.</param>
    /// <param name = "latestExclusive">The latest exclusive used by the operation.</param>
    /// <returns>The query produced by the operation.</returns>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailQuery DateBetweenWithFieldNameAndEarliestInclusiveAndLatestExclusive(string fieldName, EmailDateTime earliestInclusive, EmailDateTime latestExclusive)
    {
        return EmailQuery.DateBetweenImplementationWithFieldNameAndEarliestInclusiveAndLatestExclusive(fieldName, earliestInclusive, latestExclusive);
    }

    /// <summary>
    /// Creates a query that evaluates the MIME type condition.
    /// </summary>
    /// <param name = "topLevelType">The top level type used by the operation.</param>
    /// <param name = "subtype">The subtype used by the operation.</param>
    /// <returns>The query produced by the operation.</returns>
    public EmailQuery MimeType(string topLevelType, string subtype)
    {
        return EmailQuery.MimeTypeImplementation(topLevelType, subtype);
    }

    /// <summary>
    /// Creates a query that evaluates the MIME top level type condition.
    /// </summary>
    /// <param name = "topLevelType">The top level type used by the operation.</param>
    /// <returns>The query produced by the operation.</returns>
    public EmailQuery MimeTopLevelType(string topLevelType)
    {
        return EmailQuery.MimeTopLevelTypeImplementation(topLevelType);
    }

    /// <summary>
    /// Determines whether the attachment is present or available.
    /// </summary>
    /// <returns>The query produced by the operation.</returns>
    public EmailQuery HasAttachment()
    {
        return EmailQuery.HasAttachmentImplementation();
    }

    /// <summary>
    /// Attaches the ment filename contains.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The query produced by the operation.</returns>
    public EmailQuery AttachmentFilenameContains(string value)
    {
        return EmailQuery.AttachmentFilenameContainsImplementation(value);
    }

    /// <summary>
    /// Attaches the ment MIME type.
    /// </summary>
    /// <param name = "topLevelType">The top level type used by the operation.</param>
    /// <param name = "subtype">The subtype used by the operation.</param>
    /// <returns>The query produced by the operation.</returns>
    public EmailQuery AttachmentMimeType(string topLevelType, string subtype)
    {
        return EmailQuery.AttachmentMimeTypeImplementation(topLevelType, subtype);
    }

    /// <summary>
    /// Creates a query that evaluates the body contains condition.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The query produced by the operation.</returns>
    public EmailQuery BodyContains(string value)
    {
        return EmailQuery.BodyContainsImplementation(value);
    }

    /// <summary>
    /// Determines whether the issue is present or available.
    /// </summary>
    /// <param name = "code">The code used by the operation.</param>
    /// <returns>The query produced by the operation.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailQuery HasIssue(EmailIssueCode code)
    {
        return EmailQuery.HasIssueImplementation(code);
    }

    /// <summary>
    /// Determines whether the issue severity at least is present or available.
    /// </summary>
    /// <param name = "severity">The severity used by the operation.</param>
    /// <returns>The query produced by the operation.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailQuery HasIssueSeverityAtLeast(EmailIssueSeverity severity)
    {
        return EmailQuery.HasIssueSeverityAtLeastImplementation(severity);
    }

    /// <summary>
    /// Determines whether the security state is present or available.
    /// </summary>
    /// <param name = "state">The state used by the operation.</param>
    /// <returns>The query produced by the operation.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailQuery HasSecurityState(EmailQuerySecurityState state)
    {
        return EmailQuery.HasSecurityStateImplementation(state);
    }

    /// <summary>
    /// Creates a query that evaluates the and condition.
    /// </summary>
    /// <param name = "left">The left used by the operation.</param>
    /// <param name = "right">The right used by the operation.</param>
    /// <returns>The query produced by the operation.</returns>
    public EmailQuery AndWithLeftAndRight(EmailQuery left, EmailQuery right)
    {
        return EmailQuery.AndImplementationWithLeftAndRight(left, right);
    }

    /// <summary>
    /// Creates a query that evaluates the and condition.
    /// </summary>
    /// <param name = "queries">The queries used by the operation.</param>
    /// <returns>The query produced by the operation.</returns>
    public EmailQuery AndWithQueries(List<EmailQuery> queries)
    {
        return EmailQuery.AndImplementationWithQueries(queries);
    }

    /// <summary>
    /// Creates a query that evaluates the and condition.
    /// </summary>
    /// <param name = "queries">The queries used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The query produced by the operation.</returns>
    public EmailQuery AndWithQueriesAndOptions(List<EmailQuery> queries, EmailQueryOptions options)
    {
        return EmailQuery.AndImplementationWithQueriesAndOptions(queries, options);
    }

    /// <summary>
    /// Creates a query that evaluates the all condition.
    /// </summary>
    /// <param name = "queries">The queries used by the operation.</param>
    /// <returns>The query produced by the operation.</returns>
    public EmailQuery AllWithQueries(List<EmailQuery> queries)
    {
        return EmailQuery.AllImplementationWithQueries(queries);
    }

    /// <summary>
    /// Creates a query that evaluates the all condition.
    /// </summary>
    /// <param name = "queries">The queries used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The query produced by the operation.</returns>
    public EmailQuery AllWithQueriesAndOptions(List<EmailQuery> queries, EmailQueryOptions options)
    {
        return EmailQuery.AllImplementationWithQueriesAndOptions(queries, options);
    }

    /// <summary>
    /// Creates a query that evaluates the or condition.
    /// </summary>
    /// <param name = "left">The left used by the operation.</param>
    /// <param name = "right">The right used by the operation.</param>
    /// <returns>The query produced by the operation.</returns>
    public EmailQuery OrWithLeftAndRight(EmailQuery left, EmailQuery right)
    {
        return EmailQuery.OrImplementationWithLeftAndRight(left, right);
    }

    /// <summary>
    /// Creates a query that evaluates the or condition.
    /// </summary>
    /// <param name = "queries">The queries used by the operation.</param>
    /// <returns>The query produced by the operation.</returns>
    public EmailQuery OrWithQueries(List<EmailQuery> queries)
    {
        return EmailQuery.OrImplementationWithQueries(queries);
    }

    /// <summary>
    /// Creates a query that evaluates the or condition.
    /// </summary>
    /// <param name = "queries">The queries used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The query produced by the operation.</returns>
    public EmailQuery OrWithQueriesAndOptions(List<EmailQuery> queries, EmailQueryOptions options)
    {
        return EmailQuery.OrImplementationWithQueriesAndOptions(queries, options);
    }

    /// <summary>
    /// Creates a query that evaluates the any condition.
    /// </summary>
    /// <param name = "queries">The queries used by the operation.</param>
    /// <returns>The query produced by the operation.</returns>
    public EmailQuery AnyWithQueries(List<EmailQuery> queries)
    {
        return EmailQuery.AnyImplementationWithQueries(queries);
    }

    /// <summary>
    /// Creates a query that evaluates the any condition.
    /// </summary>
    /// <param name = "queries">The queries used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The query produced by the operation.</returns>
    public EmailQuery AnyWithQueriesAndOptions(List<EmailQuery> queries, EmailQueryOptions options)
    {
        return EmailQuery.AnyImplementationWithQueriesAndOptions(queries, options);
    }

    /// <summary>
    /// Creates a query that evaluates the not condition.
    /// </summary>
    /// <param name = "query">The query used by the operation.</param>
    /// <returns>The query produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailQuery Not(EmailQuery query)
    {
        return EmailQuery.NotImplementation(query);
    }
}
