namespace uri;

internal static class TemplateOperatorTable
{
    internal static readonly TemplateOperatorMetadata Simple =
        new TemplateOperatorMetadata("", ",", false, "", false);

    internal static readonly TemplateOperatorMetadata Reserved =
        new TemplateOperatorMetadata("", ",", false, "", true);

    internal static readonly TemplateOperatorMetadata Fragment =
        new TemplateOperatorMetadata("#", ",", false, "", true);

    internal static readonly TemplateOperatorMetadata Label =
        new TemplateOperatorMetadata(".", ".", false, "", false);

    internal static readonly TemplateOperatorMetadata PathSegment =
        new TemplateOperatorMetadata("/", "/", false, "", false);

    internal static readonly TemplateOperatorMetadata PathParameter =
        new TemplateOperatorMetadata(";", ";", true, "", false);

    internal static readonly TemplateOperatorMetadata Query =
        new TemplateOperatorMetadata("?", "&", true, "=", false);

    internal static readonly TemplateOperatorMetadata QueryContinuation =
        new TemplateOperatorMetadata("&", "&", true, "=", false);

    internal static TemplateOperatorMetadata Get(UriTemplateOperator value)
    {
        switch (value)
        {
            case UriTemplateOperator.Simple:
                return Simple;
            case UriTemplateOperator.Reserved:
                return Reserved;
            case UriTemplateOperator.Fragment:
                return Fragment;
            case UriTemplateOperator.Label:
                return Label;
            case UriTemplateOperator.PathSegment:
                return PathSegment;
            case UriTemplateOperator.PathParameter:
                return PathParameter;
            case UriTemplateOperator.Query:
                return Query;
            case UriTemplateOperator.QueryContinuation:
                return QueryContinuation;
            default:
                throw new System.ArgumentOutOfRangeException("value");
        }
    }
}
