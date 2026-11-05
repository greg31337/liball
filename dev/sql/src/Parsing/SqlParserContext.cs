using System;

namespace sql;

internal class SqlParserContext
{
    internal SqlTokenStream Stream;
    internal SqlDiagnosticBag Diagnostics;
    internal SqlParseOptions Options;
    internal SqlSourceText? SourceText;
    internal int RecoveryStepCount;

    internal static SqlParserContext Create(SqlToken[] tokens)
    {
        return Create(tokens, new SqlParseOptions(), null);
    }

    internal SqlParserContext(SqlTokenStream stream, SqlParseOptions options, SqlSourceText? sourceText)
    {
        if (stream == null)
        {
            throw new ArgumentNullException("stream");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        options.Validate();
        Options = options;
        Stream = stream;
        Diagnostics = stream.Diagnostics;
        SourceText = sourceText;
        RecoveryStepCount = 0;
    }

    internal static SqlParserContext Create(SqlToken[] tokens, SqlParseOptions options, SqlSourceText? sourceText)
    {
        return new SqlParserContext(new SqlTokenStream(tokens), options, sourceText);
    }

    internal static SqlParserContext Create(SqlTokenStream stream, SqlParseOptions options, SqlSourceText? sourceText)
    {
        return new SqlParserContext(stream, options, sourceText);
    }

    internal int GetPosition()
    {
        return Stream.GetPosition();
    }

    internal bool IsAtEnd()
    {
        return Stream.IsAtEnd();
    }

    internal SqlToken Peek()
    {
        return Stream.Peek();
    }

    internal SqlToken PeekAtOffset(int offset)
    {
        return Stream.PeekAtOffset(offset);
    }

    internal SqlToken Consume()
    {
        return Stream.Consume();
    }

    internal bool Match(SqlTokenKind kind)
    {
        return Stream.Match(kind);
    }

    internal SqlToken Expect(SqlTokenKind kind)
    {
        return Stream.Expect(kind);
    }

    internal bool HasDiagnostics()
    {
        return !Diagnostics.IsEmpty();
    }

    internal bool HasErrors()
    {
        return Diagnostics.HasErrors();
    }

    internal int GetDiagnosticCount()
    {
        return Diagnostics.GetCount();
    }

    internal SqlDiagnostic? AddDiagnostic(SqlDiagnosticSeverity severity, string code, string message, SqlTextSpan span)
    {
        if (IsDiagnosticLimitReached())
        {
            return null;
        }

        return Diagnostics.AddDiagnosticWithDetails(severity, code, message, span, SourceText);
    }

    internal SqlDiagnostic? AddError(string code, string message, SqlToken token)
    {
        if (token == null)
        {
            throw new ArgumentNullException("token");
        }

        return AddDiagnostic(SqlDiagnosticSeverity.Error, code, message, token.Span);
    }

    internal SqlDiagnostic? AddUnsupportedFeature(string featureName, SqlTextSpan span)
    {
        if (featureName == null)
        {
            throw new ArgumentNullException("featureName");
        }

        return AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLDIALECT001", "SQL feature is not supported by the selected dialect: " + featureName + ".", span);
    }

    internal bool IsDiagnosticLimitReached()
    {
        return Options.ShouldStopAfterDiagnosticCount(Diagnostics.GetCount());
    }

    internal bool CanRecover()
    {
        return Options.ShouldRecover() && RecoveryStepCount < Options.MaxRecoverySteps;
    }

    internal bool RecordRecoveryStep()
    {
        if (!CanRecover())
        {
            return false;
        }

        RecoveryStepCount = RecoveryStepCount + 1;
        return true;
    }

    internal SqlToken SkipForRecovery()
    {
        SqlToken token = Peek();
        if (!RecordRecoveryStep())
        {
            return token;
        }

        return Consume();
    }

    internal void SynchronizeTo(SqlTokenKind kind)
    {
        while (!IsAtEnd() && Peek().Kind != kind)
        {
            if (!RecordRecoveryStep())
            {
                return;
            }

            Consume();
        }
    }
}
