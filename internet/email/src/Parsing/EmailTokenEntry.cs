namespace email;

internal class EmailTokenEntry
{
    EmailTokenKind kind;
    EmailHeaders? headers;
    EmailContent? content;
    long contentOffset;
    int contentLength;
    EmailBodyChunkKind bodyChunkKind;
    EmailIssue? issue;
    int depth;
    EmailPath path;
    int entityIdentifier;

    internal EmailTokenEntry(EmailTokenKind kind, EmailHeaders? headers, EmailContent? content, long contentOffset, int contentLength, EmailBodyChunkKind bodyChunkKind, EmailIssue? issue, int depth, EmailPath path, int entityIdentifier)
    {
        this.kind = kind;
        this.headers = headers;
        this.content = content;
        this.contentOffset = contentOffset;
        this.contentLength = contentLength;
        this.bodyChunkKind = bodyChunkKind;
        this.issue = issue;
        this.depth = depth;
        this.path = path;
        this.entityIdentifier = entityIdentifier;
    }

    internal EmailTokenKind GetKind()
    {
        return kind;
    }

    internal EmailHeaders? GetHeaders()
    {
        return headers;
    }

    internal EmailContent? GetContent()
    {
        return content;
    }

    internal long GetContentOffset()
    {
        return contentOffset;
    }

    internal int GetContentLength()
    {
        return contentLength;
    }

    internal EmailBodyChunkKind GetBodyChunkKind()
    {
        return bodyChunkKind;
    }

    internal EmailIssue? GetIssue()
    {
        return issue;
    }

    internal int GetDepth()
    {
        return depth;
    }

    internal EmailPath GetPath()
    {
        return path;
    }

    internal int GetEntityIdentifier()
    {
        return entityIdentifier;
    }
}
