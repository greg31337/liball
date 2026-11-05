namespace uspto;

/// <summary>
/// Describes one leaf or logical node in a structured search predicate tree.
/// </summary>
public class SearchFilter
{
    /// <summary>The field addressed by a leaf predicate, or <c>null</c> for a logical node.</summary>
    public string? Field = null;

    /// <summary>The comparison or logical operator.</summary>
    public SearchOperator Operator = SearchOperator.None;

    /// <summary>The single comparison value, when used by the selected operator.</summary>
    public string? Value = null;

    /// <summary>The comparison values used by set-oriented operators.</summary>
    public string[] Values = new string[0];

    /// <summary>The child predicates used by logical operators.</summary>
    public SearchFilter[] Children = new SearchFilter[0];
}

/// <summary>
/// Describes a lower and upper bound for one searchable field.
/// </summary>
public class SearchRange
{
    /// <summary>The field to which the range applies.</summary>
    public string? Field = null;

    /// <summary>The lower bound text, or <c>null</c> for no lower bound.</summary>
    public string? LowerValue = null;

    /// <summary>The upper bound text, or <c>null</c> for no upper bound.</summary>
    public string? UpperValue = null;

    /// <summary>Whether a supplied lower bound is inclusive.</summary>
    public bool IncludeLower = true;

    /// <summary>Whether a supplied upper bound is inclusive.</summary>
    public bool IncludeUpper = true;
}

/// <summary>
/// Describes deterministic ordering by one result field.
/// </summary>
public class SearchSort
{
    /// <summary>The field used for ordering.</summary>
    public string? Field = null;

    /// <summary>The direction in which values are ordered.</summary>
    public SortDirection Direction = SortDirection.Ascending;
}

/// <summary>
/// Selects the fields that a search service should return.
/// </summary>
public class SearchFieldSelection
{
    /// <summary>The requested service field names in caller-defined order.</summary>
    public string[] Fields = new string[0];
}

/// <summary>
/// Requests aggregation values for one searchable field.
/// </summary>
public class SearchFacetRequest
{
    /// <summary>The field to aggregate.</summary>
    public string? Field = null;

    /// <summary>An optional maximum number of facet values to return.</summary>
    public int Limit = 0;

    /// <summary>Whether <see cref="Limit"/> contains a caller-supplied value.</summary>
    public bool HasLimit = false;

    /// <summary>An optional value prefix used to constrain returned facet values.</summary>
    public string? Prefix = null;

    /// <summary>Whether records with a missing field value are included.</summary>
    public bool IncludeMissing = false;
}

/// <summary>
/// Holds one value and count returned for a facet field.
/// </summary>
public class SearchFacetValue
{
    /// <summary>The facet value text.</summary>
    public string? Value = null;

    /// <summary>The number of matching records.</summary>
    public long Count = 0L;
}

/// <summary>
/// Holds all returned values for one facet field.
/// </summary>
public class SearchFacetResult
{
    /// <summary>The aggregated field name.</summary>
    public string? Field = null;

    /// <summary>The returned values and their counts.</summary>
    public SearchFacetValue[] Values = new SearchFacetValue[0];
}

/// <summary>
/// Selects a page using a zero-based result offset and a page limit.
/// </summary>
public class OffsetPage
{
    /// <summary>The zero-based offset of the first requested record.</summary>
    public long Offset = 0L;

    /// <summary>The maximum number of records requested.</summary>
    public int Limit = UsptoDefaults.PageSize;
}

/// <summary>
/// Selects a page using stable sort values from the preceding page.
/// </summary>
public class CursorPage
{
    /// <summary>The stable sort values after which the next page begins.</summary>
    public string[] AfterValues = new string[0];

    /// <summary>The maximum number of records requested.</summary>
    public int Size = UsptoDefaults.PageSize;
}

/// <summary>
/// Describes a service-independent structured search request.
/// </summary>
public class SearchRequest
{
    /// <summary>An optional service-specific free-text or simplified query.</summary>
    public string? Query = null;

    /// <summary>The structured predicate roots applied to the search.</summary>
    public SearchFilter[] Filters = new SearchFilter[0];

    /// <summary>The field ranges applied to the search.</summary>
    public SearchRange[] Ranges = new SearchRange[0];

    /// <summary>The deterministic result ordering.</summary>
    public SearchSort[] Sorts = new SearchSort[0];

    /// <summary>The fields selected for the response.</summary>
    public SearchFieldSelection FieldSelection = new SearchFieldSelection();

    /// <summary>The requested field aggregations.</summary>
    public SearchFacetRequest[] Facets = new SearchFacetRequest[0];

    /// <summary>The offset page used when <see cref="UseCursorPage"/> is <c>false</c>.</summary>
    public OffsetPage OffsetPage = new OffsetPage();

    /// <summary>The cursor page used when <see cref="UseCursorPage"/> is <c>true</c>.</summary>
    public CursorPage CursorPage = new CursorPage();

    /// <summary>Whether cursor paging is selected instead of offset paging.</summary>
    public bool UseCursorPage = false;

    /// <summary>The caller limit on pages processed by aggregate operations.</summary>
    public int MaximumPages = UsptoDefaults.MaximumPages;

    /// <summary>The caller limit on records returned by aggregate operations.</summary>
    public int MaximumResults = UsptoDefaults.MaximumResults;

    /// <summary>The shared context for cancellation, raw retention, and deadlines.</summary>
    public RequestContext Context = new RequestContext();
}

/// <summary>
/// Holds service-independent metadata describing one search response page.
/// </summary>
public class SearchResponseMetadata
{
    /// <summary>The request associated with the response.</summary>
    public SearchRequest Request = new SearchRequest();

    /// <summary>The total number of matching records, when supplied by the service.</summary>
    public long Total = 0L;

    /// <summary>Whether <see cref="Total"/> contains a service-provided value.</summary>
    public bool HasTotal = false;

    /// <summary>The number of records returned in this response page.</summary>
    public int ReturnedCount = 0;

    /// <summary>The offset-page coordinates associated with this response.</summary>
    public OffsetPage OffsetPage = new OffsetPage();

    /// <summary>Whether <see cref="OffsetPage"/> describes the returned page.</summary>
    public bool HasOffsetPage = false;

    /// <summary>The cursor coordinates for a following page, when supplied.</summary>
    public CursorPage NextCursorPage = new CursorPage();

    /// <summary>Whether <see cref="NextCursorPage"/> contains a following-page cursor.</summary>
    public bool HasNextCursorPage = false;

    /// <summary>The facet results returned with the page.</summary>
    public SearchFacetResult[] Facets = new SearchFacetResult[0];

    /// <summary>The service request identifier, when supplied.</summary>
    public string? RequestIdentifier = null;

    /// <summary>The origin and authority of the returned page.</summary>
    public DataProvenance Provenance = DataProvenance.Unknown;

    /// <summary>Whether the returned page came from a response cache.</summary>
    public bool FromCache = false;

    /// <summary>The origin and delivery labels for fields returned on the page.</summary>
    public FieldDataProvenance[] FieldSources = new FieldDataProvenance[0];
}
