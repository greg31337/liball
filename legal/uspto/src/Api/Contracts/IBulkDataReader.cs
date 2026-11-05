namespace uspto;

/// <summary>
/// Provides generic and product-family-specific streaming bulk record reads.
/// </summary>
public interface IBulkDataReader
{
    /// <summary>Reads bounded generic JSON records.</summary>
    /// <param name="input">The caller-owned input stream.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The raw records.</returns>
    string[] ReadBulkJsonRecords(global::System.IO.Stream input, RequestContext context);

    /// <summary>Reads bounded generic XML records.</summary>
    /// <param name="input">The caller-owned input stream.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The raw records.</returns>
    string[] ReadBulkXmlRecords(global::System.IO.Stream input, RequestContext context);

    /// <summary>Reads bounded generic delimited records.</summary>
    /// <param name="input">The caller-owned input stream.</param>
    /// <param name="delimiter">The field delimiter.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The raw records.</returns>
    string[] ReadBulkDelimitedRecords(global::System.IO.Stream input, char delimiter, RequestContext context);

    /// <summary>Reads patent grant full-text records.</summary>
    /// <param name="input">The caller-owned input stream.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The normalized records pending their typed domain adapter.</returns>
    string[] ReadPatentGrantRecords(global::System.IO.Stream input, RequestContext context);

    /// <summary>Reads published patent application full-text records.</summary>
    /// <param name="input">The caller-owned input stream.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The normalized records pending their typed domain adapter.</returns>
    string[] ReadPatentApplicationRecords(global::System.IO.Stream input, RequestContext context);

    /// <summary>Reads Patent File Wrapper bulk records.</summary>
    /// <param name="input">The caller-owned input stream.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The normalized records pending their typed domain adapter.</returns>
    string[] ReadPatentFileWrapperRecords(global::System.IO.Stream input, RequestContext context);

    /// <summary>Reads patent assignment bulk records.</summary>
    /// <param name="input">The caller-owned input stream.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The normalized records pending their typed domain adapter.</returns>
    string[] ReadPatentAssignmentRecords(global::System.IO.Stream input, RequestContext context);

    /// <summary>Reads Office Action archive records.</summary>
    /// <param name="input">The caller-owned input stream.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The normalized records pending their typed domain adapter.</returns>
    string[] ReadOfficeActionRecords(global::System.IO.Stream input, RequestContext context);

    /// <summary>Reads trademark application and status bulk records.</summary>
    /// <param name="input">The caller-owned input stream.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The normalized records pending their typed domain adapter.</returns>
    string[] ReadTrademarkApplicationRecords(global::System.IO.Stream input, RequestContext context);

    /// <summary>Reads trademark assignment bulk records.</summary>
    /// <param name="input">The caller-owned input stream.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The normalized records pending their typed domain adapter.</returns>
    string[] ReadTrademarkAssignmentRecords(global::System.IO.Stream input, RequestContext context);

    /// <summary>Reads TTAB bulk records.</summary>
    /// <param name="input">The caller-owned input stream.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The normalized records pending their typed domain adapter.</returns>
    string[] ReadTtabRecords(global::System.IO.Stream input, RequestContext context);

    /// <summary>Reads PatentsView patent-grant table records.</summary>
    /// <param name="input">The caller-owned input stream.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The normalized records pending their typed domain adapter.</returns>
    string[] ReadPatentsViewGrantRecords(global::System.IO.Stream input, RequestContext context);

    /// <summary>Reads PatentsView pre-grant publication table records.</summary>
    /// <param name="input">The caller-owned input stream.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The normalized records pending their typed domain adapter.</returns>
    string[] ReadPatentsViewPreGrantRecords(global::System.IO.Stream input, RequestContext context);
}
