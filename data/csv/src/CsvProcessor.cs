namespace csv;

/// <summary>
/// Runs streaming CSV pipelines and loads bounded in-memory tables.
/// </summary>
internal static class CsvProcessor
{
    /// <summary>
    /// Reads rows forward-only, applies pipeline stages, and writes accepted rows.
    /// </summary>
    /// <param name="reader">The source reader.</param>
    /// <param name="writer">The destination CSV writer.</param>
    /// <returns>The number of rows written.</returns>
    internal static long Process(ICsvRowReader reader, ICsvRowWriter writer)
    {
        CsvPipeline pipeline = CsvPipeline.CreateDefault();
        long count = Process(reader, writer, pipeline);
        return count;
    }

    /// <summary>
    /// Reads rows forward-only, applies pipeline stages, and writes accepted rows.
    /// </summary>
    /// <param name="reader">The source reader.</param>
    /// <param name="writer">The destination CSV writer.</param>
    /// <param name="pipeline">Pipeline configuration and counters; null uses defaults.</param>
    /// <returns>The number of rows written.</returns>
    internal static long Process(ICsvRowReader reader, ICsvRowWriter writer, CsvPipeline? pipeline)
    {
        CsvPipeline activePipeline = ResolvePipeline(pipeline);
        CsvHeader readerHeader = reader.GetHeaders();
        if (writer.ShouldWriteHeader() && readerHeader.Names.Length > 0)
        {
            writer.WriteHeader(readerHeader);
        }

        while (!ShouldStopForLimit(activePipeline) && reader.Read())
        {
            activePipeline.RowsRead = activePipeline.RowsRead + 1;
            CsvRow row = reader.GetCurrentRow();
            if (ShouldSkipRowByPosition(activePipeline))
            {
                activePipeline.RowsSkipped = activePipeline.RowsSkipped + 1;
            }
            else if (ShouldWriteRow(row, activePipeline))
            {
                CsvRow outputRow = TransformRow(row, activePipeline);
                if (ShouldAcceptRow(outputRow, activePipeline))
                {
                    CsvDiagnosticList fieldDiagnostics = ValidateFields(outputRow, activePipeline);
                    if (fieldDiagnostics.Count() == 0)
                    {
                        writer.WriteRow(outputRow);
                        activePipeline.RowsWritten = activePipeline.RowsWritten + 1;
                    }
                    else
                    {
                        RejectFieldValidatedRow(outputRow, activePipeline, fieldDiagnostics);
                    }
                }
                else
                {
                    RejectRow(outputRow, activePipeline);
                }
            }
            else
            {
                activePipeline.RowsFiltered = activePipeline.RowsFiltered + 1;
            }

            ReportProgress(activePipeline, row);
        }

        return activePipeline.RowsWritten;
    }

    /// <summary>
    /// Reads rows into a bounded table using requested copying and diagnostic settings.
    /// </summary>
    /// <param name="reader">The source reader.</param>
    /// <param name="options">Operation options; null uses defaults where supported.</param>
    /// <returns>The loaded table, which stops growing at the configured row limit.</returns>
    internal static CsvTable LoadTable(ICsvRowReader reader, CsvTableOptions? options)
    {
        CsvTableOptions tableOptions = ResolveTableOptions(options);
        CsvTable table = new CsvTable();
        LoadHeader(reader, table, tableOptions);

        while (ShouldLoadMoreRows(table, tableOptions) && reader.Read())
        {
            CsvHeader readerHeader = reader.GetHeaders();
            if (tableOptions.IncludeHeader && table.Header.Names.Length == 0 && readerHeader.Names.Length > 0)
            {
                table.Header = readerHeader.Clone();
                table.Schema = CsvSchema.CreateFromHeader(table.Header);
            }

            CsvRow row = CreateLoadedRow(reader.GetCurrentRow(), tableOptions);
            table.AddRow(row);
        }

        if (tableOptions.PreserveDiagnostics)
        {
            table.Diagnostics = reader.GetDiagnostics().Clone();
        }

        return table;
    }

    internal static CsvPipeline ResolvePipeline(CsvPipeline? pipeline)
    {
        if (pipeline == null)
        {
            CsvPipeline defaultPipeline = CsvPipeline.CreateDefault();
            return defaultPipeline;
        }

        return pipeline;
    }

    internal static CsvTableOptions ResolveTableOptions(CsvTableOptions? options)
    {
        if (options == null)
        {
            CsvTableOptions defaultOptions = CsvTableOptions.CreateDefault();
            return defaultOptions;
        }

        return options;
    }

    internal static void LoadHeader(ICsvRowReader reader, CsvTable table, CsvTableOptions options)
    {
        if (!options.IncludeHeader)
        {
            return;
        }

        CsvHeader readerHeader = reader.GetHeaders();
        if (readerHeader.Names.Length > 0)
        {
            table.Header = readerHeader.Clone();
            table.Schema = CsvSchema.CreateFromHeader(table.Header);
            return;
        }

        if (reader.ShouldReadHeader())
        {
            bool read = reader.ReadHeader();
            if (read)
            {
                table.Header = reader.GetHeaders().Clone();
                table.Schema = CsvSchema.CreateFromHeader(table.Header);
            }
        }
    }

    internal static bool ShouldLoadMoreRows(CsvTable table, CsvTableOptions options)
    {
        if (options.MaxRows <= 0)
        {
            return true;
        }

        if (table.CountRows() < options.MaxRows)
        {
            return true;
        }

        return false;
    }

    internal static CsvRow CreateLoadedRow(CsvRow source, CsvTableOptions options)
    {
        CsvRow row = source;
        if (options.CloneRows)
        {
            row = source.Clone();
        }

        if (!options.PreserveDiagnostics && options.CloneRows)
        {
            row.Diagnostics = CsvDiagnosticList.Create();
        }

        return row;
    }

    internal static bool ShouldStopForLimit(CsvPipeline pipeline)
    {
        if (pipeline.MaxRows <= 0)
        {
            return false;
        }

        if (pipeline.RowsWritten >= pipeline.MaxRows)
        {
            return true;
        }

        return false;
    }

    internal static bool ShouldSkipRowByPosition(CsvPipeline pipeline)
    {
        if (pipeline.SkipRows <= 0)
        {
            return false;
        }

        if (pipeline.RowsSkipped < pipeline.SkipRows)
        {
            return true;
        }

        return false;
    }

    internal static bool ShouldWriteRow(CsvRow row, CsvPipeline pipeline)
    {
        if (!pipeline.HasFilter)
        {
            return true;
        }

        ICsvRowFilter? filter = pipeline.Filter;
        if (filter == null)
        {
            return true;
        }

        bool include = filter.Include(row);
        return include;
    }

    internal static CsvRow TransformRow(CsvRow row, CsvPipeline pipeline)
    {
        if (!pipeline.HasTransformer)
        {
            return row;
        }

        ICsvRowTransformer? transformer = pipeline.Transformer;
        if (transformer == null)
        {
            return row;
        }

        CsvRow? transformed = transformer.Transform(row);
        pipeline.RowsTransformed = pipeline.RowsTransformed + 1;
        if (transformed == null)
        {
            return row;
        }

        return transformed;
    }

    internal static bool ShouldAcceptRow(CsvRow row, CsvPipeline pipeline)
    {
        if (!pipeline.HasValidator)
        {
            return true;
        }

        ICsvRowValidator? validator = pipeline.Validator;
        if (validator == null)
        {
            return true;
        }

        bool valid = validator.IsValid(row);
        return valid;
    }

    internal static CsvDiagnosticList ValidateFields(CsvRow row, CsvPipeline pipeline)
    {
        CsvDiagnosticList diagnostics = CsvDiagnosticList.Create();
        if (!pipeline.HasFieldValidator)
        {
            return diagnostics;
        }

        ICsvFieldValidator? validator = pipeline.FieldValidator;
        if (validator == null)
        {
            return diagnostics;
        }

        int index = 0;
        while (index < row.Fields.Length)
        {
            string? value = row.Fields[index];
            pipeline.FieldsValidated = pipeline.FieldsValidated + 1;
            if (!validator.IsValid(row, index, value))
            {
                pipeline.FieldsRejected = pipeline.FieldsRejected + 1;
                CsvDiagnostic diagnostic = validator.CreateDiagnostic(row, index, value);
                diagnostics.Add(diagnostic);
            }

            index = index + 1;
        }

        return diagnostics;
    }

    internal static void RejectFieldValidatedRow(CsvRow row, CsvPipeline pipeline, CsvDiagnosticList diagnostics)
    {
        pipeline.RowsRejected = pipeline.RowsRejected + 1;
        CsvRow rejected = row.Clone();
        int index = 0;
        while (index < diagnostics.Count())
        {
            CsvDiagnostic diagnostic = diagnostics.Get(index);
            pipeline.RejectDiagnostics.Add(diagnostic);
            rejected.Diagnostics.Add(diagnostic.Clone());
            index = index + 1;
        }

        pipeline.Rejects.Add(rejected);
        if (!pipeline.HasRejectHandler)
        {
            return;
        }

        ICsvRejectHandler? rejectHandler = pipeline.RejectHandler;
        if (rejectHandler == null)
        {
            return;
        }

        index = 0;
        while (index < diagnostics.Count())
        {
            rejectHandler.Handle(rejected, diagnostics.Get(index));
            index = index + 1;
        }
    }

    internal static void RejectRow(CsvRow row, CsvPipeline pipeline)
    {
        pipeline.RowsRejected = pipeline.RowsRejected + 1;
        ICsvRowValidator? validator = pipeline.Validator;
        if (validator == null)
        {
            return;
        }

        CsvDiagnostic diagnostic = validator.CreateDiagnostic(row);
        pipeline.RejectDiagnostics.Add(diagnostic);
        CsvRow rejected = row.Clone();
        rejected.Diagnostics.Add(diagnostic.Clone());
        pipeline.Rejects.Add(rejected);
        if (pipeline.HasRejectHandler)
        {
            ICsvRejectHandler? rejectHandler = pipeline.RejectHandler;
            if (rejectHandler != null)
            {
                rejectHandler.Handle(rejected, diagnostic);
            }
        }
    }

    internal static void ReportProgress(CsvPipeline pipeline, CsvRow row)
    {
        if (pipeline.HasProgressHandler)
        {
            ICsvProgressHandler? progressHandler = pipeline.ProgressHandler;
            if (progressHandler != null)
            {
                progressHandler.Report(pipeline, row);
            }
        }
    }
}
