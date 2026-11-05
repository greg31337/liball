using System.Collections.Generic;

namespace csv;

/// <summary>
/// Configures streaming row and field processing and records counters and rejected rows.
/// </summary>
public class CsvPipeline
{
    /// <summary>
    /// The configured row filter, or null when explicitly disabled.
    /// </summary>
    public ICsvRowFilter? Filter;
    /// <summary>
    /// The configured row transformer, or null when explicitly disabled.
    /// </summary>
    public ICsvRowTransformer? Transformer;
    /// <summary>
    /// The configured row validator, or null when explicitly disabled.
    /// </summary>
    public ICsvRowValidator? Validator;
    /// <summary>
    /// The configured field validator, or null when explicitly disabled.
    /// </summary>
    public ICsvFieldValidator? FieldValidator;
    /// <summary>
    /// The configured reject handler, or null when explicitly disabled.
    /// </summary>
    public ICsvRejectHandler? RejectHandler;
    /// <summary>
    /// The configured progress handler, or null when explicitly disabled.
    /// </summary>
    public ICsvProgressHandler? ProgressHandler;
    /// <summary>
    /// Whether filtering is active.
    /// </summary>
    public bool HasFilter;
    /// <summary>
    /// Whether transformation is active.
    /// </summary>
    public bool HasTransformer;
    /// <summary>
    /// Whether row validation is active.
    /// </summary>
    public bool HasValidator;
    /// <summary>
    /// Whether field validation is active.
    /// </summary>
    public bool HasFieldValidator;
    /// <summary>
    /// Whether reject callbacks are active.
    /// </summary>
    public bool HasRejectHandler;
    /// <summary>
    /// Whether progress callbacks are active.
    /// </summary>
    public bool HasProgressHandler;
    /// <summary>
    /// Input rows skipped before processing.
    /// </summary>
    public int SkipRows;
    /// <summary>
    /// Maximum rows written or loaded; zero means no limit.
    /// </summary>
    public int MaxRows;
    /// <summary>
    /// Input rows examined.
    /// </summary>
    public long RowsRead;
    /// <summary>
    /// Rows written.
    /// </summary>
    public long RowsWritten;
    /// <summary>
    /// Rows skipped by position.
    /// </summary>
    public long RowsSkipped;
    /// <summary>
    /// Rows excluded by filtering.
    /// </summary>
    public long RowsFiltered;
    /// <summary>
    /// Rows passed through transformation.
    /// </summary>
    public long RowsTransformed;
    /// <summary>
    /// Rows rejected by validation.
    /// </summary>
    public long RowsRejected;
    /// <summary>
    /// Fields examined by field validation.
    /// </summary>
    public long FieldsValidated;
    /// <summary>
    /// Fields rejected by field validation.
    /// </summary>
    public long FieldsRejected;
    /// <summary>
    /// Rejected rows retained for inspection.
    /// </summary>
    public List<CsvRow> Rejects;
    /// <summary>
    /// Diagnostics corresponding to pipeline rejects.
    /// </summary>
    public CsvDiagnosticList RejectDiagnostics;

    /// <summary>
    /// Initializes a new CsvPipeline with library defaults.
    /// </summary>
    public CsvPipeline()
    {
        Filter = CsvRowFilter.CreateDefault();
        Transformer = CsvRowTransformer.CreateDefault();
        Validator = CsvRowValidator.CreateDefault();
        FieldValidator = CsvFieldValidator.CreateDefault();
        RejectHandler = CsvRejectHandler.CreateDefault();
        ProgressHandler = CsvProgressHandler.CreateDefault();
        HasFilter = false;
        HasTransformer = false;
        HasValidator = false;
        HasFieldValidator = false;
        HasRejectHandler = false;
        HasProgressHandler = false;
        SkipRows = 0;
        MaxRows = 0;
        RowsRead = 0;
        RowsWritten = 0;
        RowsSkipped = 0;
        RowsFiltered = 0;
        RowsTransformed = 0;
        RowsRejected = 0;
        FieldsValidated = 0;
        FieldsRejected = 0;
        Rejects = new List<CsvRow>();
        RejectDiagnostics = CsvDiagnosticList.Create();
    }

    /// <summary>
    /// Creates an instance populated with library defaults.
    /// </summary>
    /// <returns>The initialized result.</returns>
    internal static CsvPipeline CreateDefault()
    {
        CsvPipeline pipeline = new CsvPipeline();
        return pipeline;
    }

    /// <summary>
    /// Sets and activates the row filter, or disables filtering when null.
    /// </summary>
    /// <param name="filter">The filter to use, or null to disable filtering.</param>
    internal void UseFilter(ICsvRowFilter? filter)
    {
        Filter = filter;
        HasFilter = filter != null;
    }

    /// <summary>
    /// Sets and activates the row transformer, or disables transformation when null.
    /// </summary>
    /// <param name="transformer">The transformer to use, or null to disable transformation.</param>
    internal void UseTransformer(ICsvRowTransformer? transformer)
    {
        Transformer = transformer;
        HasTransformer = transformer != null;
    }

    /// <summary>
    /// Sets and activates the row validator, or disables validation when null.
    /// </summary>
    /// <param name="validator">The validator to use, or null to disable validation.</param>
    internal void UseValidator(ICsvRowValidator? validator)
    {
        Validator = validator;
        HasValidator = validator != null;
    }

    /// <summary>
    /// Sets and activates the field validator, or disables field validation when null.
    /// </summary>
    /// <param name="validator">The validator to use, or null to disable field validation.</param>
    internal void UseFieldValidator(ICsvFieldValidator? validator)
    {
        FieldValidator = validator;
        HasFieldValidator = validator != null;
    }

    /// <summary>
    /// Sets and activates the reject handler, or disables reject handling when null.
    /// </summary>
    /// <param name="handler">The handler to use, or null to disable reject handling.</param>
    internal void UseRejectHandler(ICsvRejectHandler? handler)
    {
        RejectHandler = handler;
        HasRejectHandler = handler != null;
    }

    /// <summary>
    /// Sets and activates the progress handler, or disables progress reporting when null.
    /// </summary>
    /// <param name="handler">The handler to use, or null to disable progress reporting.</param>
    internal void UseProgressHandler(ICsvProgressHandler? handler)
    {
        ProgressHandler = handler;
        HasProgressHandler = handler != null;
    }
}
