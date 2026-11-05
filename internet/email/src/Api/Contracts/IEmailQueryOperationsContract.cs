using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailQuery"/>.
/// </summary>
public interface IEmailQueryOperationsContract
{
    /// <summary>
    /// Determines whether the field is present or available.
    /// </summary>
    /// <param name = "fieldName">The field name used by the operation.</param>
    /// <returns>The query produced by the operation.</returns>
    EmailQuery HasField(string fieldName);
    /// <summary>
    /// Creates a query that evaluates the raw field contains condition.
    /// </summary>
    /// <param name = "fieldName">The field name used by the operation.</param>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The query produced by the operation.</returns>
    EmailQuery RawFieldContains(string fieldName, string value);
    /// <summary>
    /// Creates a query that evaluates the raw field equals condition.
    /// </summary>
    /// <param name = "fieldName">The field name used by the operation.</param>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The query produced by the operation.</returns>
    EmailQuery RawFieldEquals(string fieldName, string value);
    /// <summary>
    /// Decodes the d field contains.
    /// </summary>
    /// <param name = "fieldName">The field name used by the operation.</param>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailQuery DecodedFieldContains(string fieldName, string value);
    /// <summary>
    /// Decodes the d field equals.
    /// </summary>
    /// <param name = "fieldName">The field name used by the operation.</param>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    EmailQuery DecodedFieldEquals(string fieldName, string value);
    /// <summary>
    /// Adds a ress equals to the current collection or model.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The query produced by the operation.</returns>
    EmailQuery AddressEqualsWithValue(EmailAddress value);
    /// <summary>
    /// Adds a ress equals to the current collection or model.
    /// </summary>
    /// <param name = "fieldName">The field name used by the operation.</param>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The query produced by the operation.</returns>
    EmailQuery AddressEqualsWithFieldNameAndValue(string fieldName, EmailAddress value);
    /// <summary>
    /// Adds a ress domain equals to the current collection or model.
    /// </summary>
    /// <param name = "domain">The domain used by the operation.</param>
    /// <returns>The query produced by the operation.</returns>
    EmailQuery AddressDomainEqualsWithDomain(string domain);
    /// <summary>
    /// Adds a ress domain equals to the current collection or model.
    /// </summary>
    /// <param name = "fieldName">The field name used by the operation.</param>
    /// <param name = "domain">The domain used by the operation.</param>
    /// <returns>The query produced by the operation.</returns>
    EmailQuery AddressDomainEqualsWithFieldNameAndDomain(string fieldName, string domain);
    /// <summary>
    /// Creates a query that evaluates the date before condition.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The query produced by the operation.</returns>
    EmailQuery DateBeforeWithValue(EmailDateTime value);
    /// <summary>
    /// Creates a query that evaluates the date before condition.
    /// </summary>
    /// <param name = "fieldName">The field name used by the operation.</param>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The query produced by the operation.</returns>
    EmailQuery DateBeforeWithFieldNameAndValue(string fieldName, EmailDateTime value);
    /// <summary>
    /// Creates a query that evaluates the date on or after condition.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The query produced by the operation.</returns>
    EmailQuery DateOnOrAfterWithValue(EmailDateTime value);
    /// <summary>
    /// Creates a query that evaluates the date on or after condition.
    /// </summary>
    /// <param name = "fieldName">The field name used by the operation.</param>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The query produced by the operation.</returns>
    EmailQuery DateOnOrAfterWithFieldNameAndValue(string fieldName, EmailDateTime value);
    /// <summary>
    /// Creates a query that evaluates the date equals condition.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The query produced by the operation.</returns>
    EmailQuery DateEqualsWithValue(EmailDateTime value);
    /// <summary>
    /// Creates a query that evaluates the date equals condition.
    /// </summary>
    /// <param name = "fieldName">The field name used by the operation.</param>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The query produced by the operation.</returns>
    EmailQuery DateEqualsWithFieldNameAndValue(string fieldName, EmailDateTime value);
    /// <summary>
    /// Creates a query that evaluates the date between condition.
    /// </summary>
    /// <param name = "earliestInclusive">The earliest inclusive used by the operation.</param>
    /// <param name = "latestExclusive">The latest exclusive used by the operation.</param>
    /// <returns>The query produced by the operation.</returns>
    EmailQuery DateBetweenWithEarliestInclusiveAndLatestExclusive(EmailDateTime earliestInclusive, EmailDateTime latestExclusive);
    /// <summary>
    /// Creates a query that evaluates the date between condition.
    /// </summary>
    /// <param name = "fieldName">The field name used by the operation.</param>
    /// <param name = "earliestInclusive">The earliest inclusive used by the operation.</param>
    /// <param name = "latestExclusive">The latest exclusive used by the operation.</param>
    /// <returns>The query produced by the operation.</returns>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailQuery DateBetweenWithFieldNameAndEarliestInclusiveAndLatestExclusive(string fieldName, EmailDateTime earliestInclusive, EmailDateTime latestExclusive);
    /// <summary>
    /// Creates a query that evaluates the MIME type condition.
    /// </summary>
    /// <param name = "topLevelType">The top level type used by the operation.</param>
    /// <param name = "subtype">The subtype used by the operation.</param>
    /// <returns>The query produced by the operation.</returns>
    EmailQuery MimeType(string topLevelType, string subtype);
    /// <summary>
    /// Creates a query that evaluates the MIME top level type condition.
    /// </summary>
    /// <param name = "topLevelType">The top level type used by the operation.</param>
    /// <returns>The query produced by the operation.</returns>
    EmailQuery MimeTopLevelType(string topLevelType);
    /// <summary>
    /// Determines whether the attachment is present or available.
    /// </summary>
    /// <returns>The query produced by the operation.</returns>
    EmailQuery HasAttachment();
    /// <summary>
    /// Attaches the ment filename contains.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The query produced by the operation.</returns>
    EmailQuery AttachmentFilenameContains(string value);
    /// <summary>
    /// Attaches the ment MIME type.
    /// </summary>
    /// <param name = "topLevelType">The top level type used by the operation.</param>
    /// <param name = "subtype">The subtype used by the operation.</param>
    /// <returns>The query produced by the operation.</returns>
    EmailQuery AttachmentMimeType(string topLevelType, string subtype);
    /// <summary>
    /// Creates a query that evaluates the body contains condition.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The query produced by the operation.</returns>
    EmailQuery BodyContains(string value);
    /// <summary>
    /// Determines whether the issue is present or available.
    /// </summary>
    /// <param name = "code">The code used by the operation.</param>
    /// <returns>The query produced by the operation.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailQuery HasIssue(EmailIssueCode code);
    /// <summary>
    /// Determines whether the issue severity at least is present or available.
    /// </summary>
    /// <param name = "severity">The severity used by the operation.</param>
    /// <returns>The query produced by the operation.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailQuery HasIssueSeverityAtLeast(EmailIssueSeverity severity);
    /// <summary>
    /// Determines whether the security state is present or available.
    /// </summary>
    /// <param name = "state">The state used by the operation.</param>
    /// <returns>The query produced by the operation.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailQuery HasSecurityState(EmailQuerySecurityState state);
    /// <summary>
    /// Creates a query that evaluates the and condition.
    /// </summary>
    /// <param name = "left">The left used by the operation.</param>
    /// <param name = "right">The right used by the operation.</param>
    /// <returns>The query produced by the operation.</returns>
    EmailQuery AndWithLeftAndRight(EmailQuery left, EmailQuery right);
    /// <summary>
    /// Creates a query that evaluates the and condition.
    /// </summary>
    /// <param name = "queries">The queries used by the operation.</param>
    /// <returns>The query produced by the operation.</returns>
    EmailQuery AndWithQueries(List<EmailQuery> queries);
    /// <summary>
    /// Creates a query that evaluates the and condition.
    /// </summary>
    /// <param name = "queries">The queries used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The query produced by the operation.</returns>
    EmailQuery AndWithQueriesAndOptions(List<EmailQuery> queries, EmailQueryOptions options);
    /// <summary>
    /// Creates a query that evaluates the all condition.
    /// </summary>
    /// <param name = "queries">The queries used by the operation.</param>
    /// <returns>The query produced by the operation.</returns>
    EmailQuery AllWithQueries(List<EmailQuery> queries);
    /// <summary>
    /// Creates a query that evaluates the all condition.
    /// </summary>
    /// <param name = "queries">The queries used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The query produced by the operation.</returns>
    EmailQuery AllWithQueriesAndOptions(List<EmailQuery> queries, EmailQueryOptions options);
    /// <summary>
    /// Creates a query that evaluates the or condition.
    /// </summary>
    /// <param name = "left">The left used by the operation.</param>
    /// <param name = "right">The right used by the operation.</param>
    /// <returns>The query produced by the operation.</returns>
    EmailQuery OrWithLeftAndRight(EmailQuery left, EmailQuery right);
    /// <summary>
    /// Creates a query that evaluates the or condition.
    /// </summary>
    /// <param name = "queries">The queries used by the operation.</param>
    /// <returns>The query produced by the operation.</returns>
    EmailQuery OrWithQueries(List<EmailQuery> queries);
    /// <summary>
    /// Creates a query that evaluates the or condition.
    /// </summary>
    /// <param name = "queries">The queries used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The query produced by the operation.</returns>
    EmailQuery OrWithQueriesAndOptions(List<EmailQuery> queries, EmailQueryOptions options);
    /// <summary>
    /// Creates a query that evaluates the any condition.
    /// </summary>
    /// <param name = "queries">The queries used by the operation.</param>
    /// <returns>The query produced by the operation.</returns>
    EmailQuery AnyWithQueries(List<EmailQuery> queries);
    /// <summary>
    /// Creates a query that evaluates the any condition.
    /// </summary>
    /// <param name = "queries">The queries used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The query produced by the operation.</returns>
    EmailQuery AnyWithQueriesAndOptions(List<EmailQuery> queries, EmailQueryOptions options);
    /// <summary>
    /// Creates a query that evaluates the not condition.
    /// </summary>
    /// <param name = "query">The query used by the operation.</param>
    /// <returns>The query produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailQuery Not(EmailQuery query);
}
