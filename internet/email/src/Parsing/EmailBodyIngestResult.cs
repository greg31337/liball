namespace email;

internal class EmailBodyIngestResult
{
    EmailContent? rawContent;
    EmailContent? decodedContent;
    EmailBodyIngestMetadata metadata;
    EmailIssueList issues;

    internal EmailBodyIngestResult(EmailContent? rawContent, EmailContent? decodedContent, EmailBodyIngestMetadata metadata, EmailIssueList issues)
    {
        this.rawContent = rawContent;
        this.decodedContent = decodedContent;
        this.metadata = metadata;
        this.issues = issues;
    }

    internal EmailContent? GetRawContent()
    {
        return rawContent;
    }

    internal EmailContent? GetDecodedContent()
    {
        return decodedContent;
    }

    internal EmailBodyIngestMetadata GetMetadata()
    {
        return metadata;
    }

    internal EmailIssueList GetIssues()
    {
        return issues;
    }
}
