using System;

namespace sql;

/// <summary>Provides copy-on-write operations for replacing direct AST children.</summary>
public class SqlAstRewriteService : ISqlAstRewriter
{
    /// <summary>Initializes a new AST rewrite service.</summary>
    public SqlAstRewriteService()
    {
    }

    /// <summary>Replaces one statement in a script.</summary>
    /// <param name="script">The source script.</param>
    /// <param name="index">The zero-based statement index.</param>
    /// <param name="replacement">The replacement statement.</param>
    /// <param name="options">The metadata-preservation options.</param>
    /// <returns>A rewritten script.</returns>
    public SqlScript ReplaceScriptStatementAt(SqlScript script, int index, SqlStatement replacement, SqlRewriteOptions options)
    {
        if (script == null)
        {
            throw new ArgumentNullException("script");
        }

        if (replacement == null)
        {
            throw new ArgumentNullException("replacement");
        }

        ValidateRewriteOptions(options);
        if (index < 0 || index >= script.Statements.Length)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        SqlStatement[] statements = new SqlStatement[script.Statements.Length];
        Array.Copy(script.Statements, statements, statements.Length);
        statements[index] = replacement;
        return CopyScriptRoot(script, statements, options);
    }

    /// <summary>Replaces one statement in a parse result while applying diagnostic and source preservation options.</summary>
    /// <param name="result">The parse result to rewrite.</param>
    /// <param name="index">The zero-based statement index.</param>
    /// <param name="replacement">The replacement statement.</param>
    /// <param name="options">The metadata-preservation options.</param>
    /// <returns>A rewritten parse result.</returns>
    public SqlParseResult ReplaceResultStatementAt(SqlParseResult result, int index, SqlStatement replacement, SqlRewriteOptions options)
    {
        if (result == null)
        {
            throw new ArgumentNullException("result");
        }

        ValidateRewriteOptions(options);
        SqlScript script = ReplaceScriptStatementAt(result.Script, index, replacement, options);
        SqlToken[] tokens = options.PreserveTokens ? result.Tokens : new SqlToken[0];
        SqlDiagnosticBag diagnostics = options.PreserveDiagnostics ? result.Diagnostics : new SqlDiagnosticBag();
        SqlSourceText? sourceText = options.PreserveSourceText ? result.SourceText : null;
        return SqlParseResult.Create(script, tokens, diagnostics, sourceText, result.Options);
    }

    /// <summary>Replaces a statement's direct typed payloads using another statement as the payload source.</summary>
    /// <param name="statement">The statement root whose metadata is preserved.</param>
    /// <param name="payloadSource">The statement supplying the discriminator and direct payload fields.</param>
    /// <param name="options">The metadata-preservation options.</param>
    /// <returns>A rewritten statement.</returns>
    public SqlStatement ReplaceStatementPayload(SqlStatement statement, SqlStatement payloadSource, SqlRewriteOptions options)
    {
        if (statement == null)
        {
            throw new ArgumentNullException("statement");
        }

        if (payloadSource == null)
        {
            throw new ArgumentNullException("payloadSource");
        }

        ValidateRewriteOptions(options);
        SqlStatement copy = CopyStatementRoot(statement, options);
        copy.Kind = payloadSource.Kind;
        copy.Query = payloadSource.Query;
        copy.Insert = payloadSource.Insert;
        copy.Update = payloadSource.Update;
        copy.Delete = payloadSource.Delete;
        copy.Merge = payloadSource.Merge;
        copy.CreateTable = payloadSource.CreateTable;
        copy.AlterTable = payloadSource.AlterTable;
        copy.DropTable = payloadSource.DropTable;
        copy.CreateIndex = payloadSource.CreateIndex;
        copy.DropIndex = payloadSource.DropIndex;
        copy.CreateView = payloadSource.CreateView;
        copy.DropView = payloadSource.DropView;
        copy.CreateTrigger = payloadSource.CreateTrigger;
        copy.DropTrigger = payloadSource.DropTrigger;
        copy.CreateVirtualTable = payloadSource.CreateVirtualTable;
        copy.Pragma = payloadSource.Pragma;
        copy.Analyze = payloadSource.Analyze;
        copy.Vacuum = payloadSource.Vacuum;
        copy.Attach = payloadSource.Attach;
        copy.Detach = payloadSource.Detach;
        copy.Reindex = payloadSource.Reindex;
        copy.Transaction = payloadSource.Transaction;
        copy.Savepoint = payloadSource.Savepoint;
        copy.Release = payloadSource.Release;
        copy.Explain = payloadSource.Explain;
        return copy;
    }

    /// <summary>Replaces one select core in a query.</summary>
    /// <param name="query">The query root.</param>
    /// <param name="index">The zero-based select-core index.</param>
    /// <param name="replacement">The replacement select core.</param>
    /// <param name="options">The metadata-preservation options.</param>
    /// <returns>A rewritten query.</returns>
    public SqlQuery ReplaceQuerySelectCoreAt(SqlQuery query, int index, SqlSelectCore replacement, SqlRewriteOptions options)
    {
        if (query == null)
        {
            throw new ArgumentNullException("query");
        }

        if (replacement == null)
        {
            throw new ArgumentNullException("replacement");
        }

        ValidateRewriteOptions(options);
        if (index < 0 || index >= query.SelectCores.Length)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        SqlSelectCore[] selectCores = new SqlSelectCore[query.SelectCores.Length];
        Array.Copy(query.SelectCores, selectCores, selectCores.Length);
        selectCores[index] = replacement;

        SqlCompoundQueryPart[] compoundParts = query.CompoundParts;
        if (index > 0 && index <= query.CompoundParts.Length)
        {
            compoundParts = new SqlCompoundQueryPart[query.CompoundParts.Length];
            Array.Copy(query.CompoundParts, compoundParts, compoundParts.Length);
            SqlCompoundQueryPart existingPart = query.CompoundParts[index - 1];
            SqlTextSpan partSpan = options.PreserveSpans ? existingPart.Span : new SqlTextSpan(0, 0);
            compoundParts[index - 1] = new SqlCompoundQueryPart(existingPart.OperatorKind, replacement, partSpan);
        }

        return CopyQueryRoot(query, selectCores, compoundParts, options);
    }

    /// <summary>Replaces a direct query child of a table reference.</summary>
    /// <param name="table">The table-reference root.</param>
    /// <param name="existingQuery">The direct query child to replace.</param>
    /// <param name="replacement">The replacement query.</param>
    /// <param name="options">The metadata-preservation options.</param>
    /// <returns>A rewritten table reference.</returns>
    public SqlTableReference ReplaceTableQuery(SqlTableReference table, SqlQuery existingQuery, SqlQuery replacement, SqlRewriteOptions options)
    {
        if (table == null)
        {
            throw new ArgumentNullException("table");
        }

        if (existingQuery == null)
        {
            throw new ArgumentNullException("existingQuery");
        }

        if (replacement == null)
        {
            throw new ArgumentNullException("replacement");
        }

        ValidateRewriteOptions(options);
        bool found = table.Query == existingQuery;
        if (table.SourceData != null && table.SourceData.Query == existingQuery)
        {
            found = true;
        }

        if (!found)
        {
            throw new ArgumentException("The query is not a direct child of the table reference.", "existingQuery");
        }

        SqlTableReference copy = CopyTableRoot(table, table.Joins, options);
        if (table.Query == existingQuery)
        {
            copy.Query = replacement;
        }

        if (table.SourceData != null && table.SourceData.Query == existingQuery)
        {
            copy.SourceData = new SqlTableSourceData(table.SourceData.Name, table.SourceData.FunctionArguments, table.SourceData.Values, replacement, table.SourceData.IsLateral);
        }

        return copy;
    }

    /// <summary>Replaces the direct inner table-reference child.</summary>
    /// <param name="table">The table-reference root.</param>
    /// <param name="replacement">The replacement inner table reference.</param>
    /// <param name="options">The metadata-preservation options.</param>
    /// <returns>A rewritten table reference.</returns>
    public SqlTableReference ReplaceTableInner(SqlTableReference table, SqlTableReference replacement, SqlRewriteOptions options)
    {
        if (table == null)
        {
            throw new ArgumentNullException("table");
        }

        if (replacement == null)
        {
            throw new ArgumentNullException("replacement");
        }

        ValidateRewriteOptions(options);
        if (table.Inner == null)
        {
            throw new ArgumentException("The table reference has no direct inner child.", "table");
        }

        SqlTableReference copy = CopyTableRoot(table, table.Joins, options);
        copy.Inner = replacement;
        return copy;
    }

    /// <summary>Replaces the direct function-expression child of a table reference.</summary>
    /// <param name="table">The table-reference root.</param>
    /// <param name="replacement">The replacement function expression.</param>
    /// <param name="options">The metadata-preservation options.</param>
    /// <returns>A rewritten table reference.</returns>
    public SqlTableReference ReplaceTableFunction(SqlTableReference table, SqlExpression replacement, SqlRewriteOptions options)
    {
        if (table == null)
        {
            throw new ArgumentNullException("table");
        }

        if (replacement == null)
        {
            throw new ArgumentNullException("replacement");
        }

        ValidateRewriteOptions(options);
        if (table.FunctionExpression == null)
        {
            throw new ArgumentException("The table reference has no direct function-expression child.", "table");
        }

        SqlTableReference copy = CopyTableRoot(table, table.Joins, options);
        copy.FunctionExpression = replacement;
        if (table.SourceData != null && replacement.FunctionData != null)
        {
            copy.SourceData = new SqlTableSourceData(replacement.FunctionData.Name, replacement.FunctionData.Arguments, table.SourceData.Values, table.SourceData.Query, table.SourceData.IsLateral);
        }

        return copy;
    }

    /// <summary>Replaces one direct join child of a table reference.</summary>
    /// <param name="table">The table-reference root.</param>
    /// <param name="index">The zero-based join index.</param>
    /// <param name="replacement">The replacement join.</param>
    /// <param name="options">The metadata-preservation options.</param>
    /// <returns>A rewritten table reference.</returns>
    public SqlTableReference ReplaceTableJoinAt(SqlTableReference table, int index, SqlJoin replacement, SqlRewriteOptions options)
    {
        if (table == null)
        {
            throw new ArgumentNullException("table");
        }

        if (replacement == null)
        {
            throw new ArgumentNullException("replacement");
        }

        ValidateRewriteOptions(options);
        if (index < 0 || index >= table.Joins.Length)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        SqlJoin[] joins = new SqlJoin[table.Joins.Length];
        Array.Copy(table.Joins, joins, joins.Length);
        joins[index] = replacement;
        return CopyTableRoot(table, joins, options);
    }

    /// <summary>Replaces every direct expression-child reference equal to the supplied existing child.</summary>
    /// <param name="expression">The expression root.</param>
    /// <param name="existingChild">The direct expression child to replace.</param>
    /// <param name="replacement">The replacement expression.</param>
    /// <param name="options">The metadata-preservation options.</param>
    /// <returns>A rewritten expression.</returns>
    public SqlExpression ReplaceExpressionChild(SqlExpression expression, SqlExpression existingChild, SqlExpression replacement, SqlRewriteOptions options)
    {
        if (expression == null)
        {
            throw new ArgumentNullException("expression");
        }

        if (existingChild == null)
        {
            throw new ArgumentNullException("existingChild");
        }

        if (replacement == null)
        {
            throw new ArgumentNullException("replacement");
        }

        ValidateRewriteOptions(options);
        bool found = false;
        SqlExpression[] arguments = expression.Arguments;
        int argumentIndex = 0;
        while (argumentIndex < expression.Arguments.Length)
        {
            if (expression.Arguments[argumentIndex] == existingChild)
            {
                if (arguments == expression.Arguments)
                {
                    arguments = new SqlExpression[expression.Arguments.Length];
                    Array.Copy(expression.Arguments, arguments, arguments.Length);
                }

                arguments[argumentIndex] = replacement;
                found = true;
            }

            argumentIndex = argumentIndex + 1;
        }

        SqlExpression copy = CopyExpressionRoot(expression, arguments, options);
        if (expression.Left == existingChild)
        {
            copy.Left = replacement;
            found = true;
        }

        if (expression.Right == existingChild)
        {
            copy.Right = replacement;
            found = true;
        }

        if (expression.Operand == existingChild)
        {
            copy.Operand = replacement;
            found = true;
        }

        if (expression.Alternate == existingChild)
        {
            copy.Alternate = replacement;
            found = true;
        }

        if (expression.Filter == existingChild)
        {
            copy.Filter = replacement;
            found = true;
        }

        if (expression.CollateData != null && expression.CollateData.Operand == existingChild)
        {
            copy.CollateData = new SqlCollateExpressionData(replacement, expression.CollateData.Collation);
            found = true;
        }

        if (expression.PatternData != null)
        {
            SqlExpression patternValue = expression.PatternData.Value;
            SqlExpression pattern = expression.PatternData.Pattern;
            SqlExpression? escape = expression.PatternData.Escape;
            bool patternChanged = false;
            if (patternValue == existingChild)
            {
                patternValue = replacement;
                patternChanged = true;
            }

            if (pattern == existingChild)
            {
                pattern = replacement;
                patternChanged = true;
            }

            if (escape == existingChild)
            {
                escape = replacement;
                patternChanged = true;
            }

            if (patternChanged)
            {
                copy.PatternData = new SqlPatternExpressionData(patternValue, pattern, escape, expression.PatternData.OperatorText, expression.PatternData.IsNegated);
                found = true;
            }
        }

        if (expression.DistinctnessData != null)
        {
            SqlExpression distinctLeft = expression.DistinctnessData.Left;
            SqlExpression distinctRight = expression.DistinctnessData.Right;
            bool distinctChanged = false;
            if (distinctLeft == existingChild)
            {
                distinctLeft = replacement;
                distinctChanged = true;
            }

            if (distinctRight == existingChild)
            {
                distinctRight = replacement;
                distinctChanged = true;
            }

            if (distinctChanged)
            {
                copy.DistinctnessData = new SqlDistinctnessExpressionData(distinctLeft, distinctRight, expression.DistinctnessData.Kind);
                found = true;
            }
        }

        if (expression.InData != null)
        {
            SqlExpression inOperand = expression.InData.Operand;
            SqlExpression[] values = expression.InData.Values;
            SqlExpression[] functionArguments = expression.InData.FunctionArguments;
            bool inChanged = false;
            if (inOperand == existingChild)
            {
                inOperand = replacement;
                inChanged = true;
            }

            int valueIndex = 0;
            while (valueIndex < expression.InData.Values.Length)
            {
                if (expression.InData.Values[valueIndex] == existingChild)
                {
                    if (values == expression.InData.Values)
                    {
                        values = new SqlExpression[expression.InData.Values.Length];
                        Array.Copy(expression.InData.Values, values, values.Length);
                    }

                    values[valueIndex] = replacement;
                    inChanged = true;
                }

                valueIndex = valueIndex + 1;
            }

            int functionArgumentIndex = 0;
            while (functionArgumentIndex < expression.InData.FunctionArguments.Length)
            {
                if (expression.InData.FunctionArguments[functionArgumentIndex] == existingChild)
                {
                    if (functionArguments == expression.InData.FunctionArguments)
                    {
                        functionArguments = new SqlExpression[expression.InData.FunctionArguments.Length];
                        Array.Copy(expression.InData.FunctionArguments, functionArguments, functionArguments.Length);
                    }

                    functionArguments[functionArgumentIndex] = replacement;
                    inChanged = true;
                }

                functionArgumentIndex = functionArgumentIndex + 1;
            }

            if (inChanged)
            {
                copy.InData = new SqlInExpressionData(inOperand, expression.InData.IsNegated, expression.InData.TargetKind, values, expression.InData.Query, expression.InData.TargetName, functionArguments);
                found = true;
            }
        }

        if (expression.ExtractData != null && expression.ExtractData.Source == existingChild)
        {
            copy.ExtractData = new SqlExtractExpressionData(expression.ExtractData.Field, replacement);
            found = true;
        }

        if (expression.CastData != null && expression.CastData.Operand == existingChild)
        {
            copy.CastData = new SqlCastExpressionData(replacement, expression.CastData.TypeName, expression.CastData.SyntaxKind);
            found = true;
        }

        if (expression.ArrayData != null)
        {
            SqlExpression[] elements = expression.ArrayData.Elements;
            bool arrayChanged = false;
            int elementIndex = 0;
            while (elementIndex < expression.ArrayData.Elements.Length)
            {
                if (expression.ArrayData.Elements[elementIndex] == existingChild)
                {
                    if (elements == expression.ArrayData.Elements)
                    {
                        elements = new SqlExpression[expression.ArrayData.Elements.Length];
                        Array.Copy(expression.ArrayData.Elements, elements, elements.Length);
                    }

                    elements[elementIndex] = replacement;
                    arrayChanged = true;
                }

                elementIndex = elementIndex + 1;
            }

            if (arrayChanged)
            {
                copy.ArrayData = new SqlArrayExpressionData(elements, expression.ArrayData.Query);
                found = true;
            }
        }

        if (expression.SubscriptData != null)
        {
            SqlExpression target = expression.SubscriptData.Target;
            SqlExpression? lowerBound = expression.SubscriptData.LowerBound;
            SqlExpression? upperBound = expression.SubscriptData.UpperBound;
            bool subscriptChanged = false;
            if (target == existingChild)
            {
                target = replacement;
                subscriptChanged = true;
            }

            if (lowerBound == existingChild)
            {
                lowerBound = replacement;
                subscriptChanged = true;
            }

            if (upperBound == existingChild)
            {
                upperBound = replacement;
                subscriptChanged = true;
            }

            if (subscriptChanged)
            {
                copy.SubscriptData = new SqlSubscriptExpressionData(target, lowerBound, upperBound, expression.SubscriptData.IsSlice);
                found = true;
            }
        }

        if (expression.OperatorData != null)
        {
            SqlExpression operatorLeft = expression.OperatorData.Left;
            SqlExpression operatorRight = expression.OperatorData.Right;
            bool operatorChanged = false;
            if (operatorLeft == existingChild)
            {
                operatorLeft = replacement;
                operatorChanged = true;
            }

            if (operatorRight == existingChild)
            {
                operatorRight = replacement;
                operatorChanged = true;
            }

            if (operatorChanged)
            {
                copy.OperatorData = new SqlOperatorExpressionData(operatorLeft, operatorRight, expression.OperatorData.OperatorText, expression.OperatorData.Precedence, expression.OperatorData.Kind);
                found = true;
            }
        }

        if (expression.FunctionData != null)
        {
            SqlExpression[] functionDataArguments = expression.FunctionData.Arguments;
            SqlExpression? functionFilter = expression.FunctionData.Filter;
            bool functionChanged = false;
            int dataArgumentIndex = 0;
            while (dataArgumentIndex < expression.FunctionData.Arguments.Length)
            {
                if (expression.FunctionData.Arguments[dataArgumentIndex] == existingChild)
                {
                    if (functionDataArguments == expression.FunctionData.Arguments)
                    {
                        functionDataArguments = new SqlExpression[expression.FunctionData.Arguments.Length];
                        Array.Copy(expression.FunctionData.Arguments, functionDataArguments, functionDataArguments.Length);
                    }

                    functionDataArguments[dataArgumentIndex] = replacement;
                    functionChanged = true;
                }

                dataArgumentIndex = dataArgumentIndex + 1;
            }

            if (functionFilter == existingChild)
            {
                functionFilter = replacement;
                functionChanged = true;
            }

            if (functionChanged)
            {
                copy.FunctionData = new SqlFunctionInvocationData(expression.FunctionData.Name, functionDataArguments, expression.FunctionData.IsDistinct, expression.FunctionData.OrderBy, functionFilter, expression.FunctionData.WindowName, expression.FunctionData.WindowDefinition);
                found = true;
            }
        }

        if (!found)
        {
            throw new ArgumentException("The expression is not a direct child of the expression root.", "existingChild");
        }

        return copy;
    }

    /// <summary>Replaces every direct query-child reference equal to the supplied existing query.</summary>
    /// <param name="expression">The expression root.</param>
    /// <param name="existingQuery">The direct query child to replace.</param>
    /// <param name="replacement">The replacement query.</param>
    /// <param name="options">The metadata-preservation options.</param>
    /// <returns>A rewritten expression.</returns>
    public SqlExpression ReplaceExpressionQuery(SqlExpression expression, SqlQuery existingQuery, SqlQuery replacement, SqlRewriteOptions options)
    {
        if (expression == null)
        {
            throw new ArgumentNullException("expression");
        }

        if (existingQuery == null)
        {
            throw new ArgumentNullException("existingQuery");
        }

        if (replacement == null)
        {
            throw new ArgumentNullException("replacement");
        }

        ValidateRewriteOptions(options);
        bool found = false;
        SqlExpression copy = CopyExpressionRoot(expression, expression.Arguments, options);
        if (expression.Query == existingQuery)
        {
            copy.Query = replacement;
            found = true;
        }

        if (expression.InData != null && expression.InData.Query == existingQuery)
        {
            copy.InData = new SqlInExpressionData(expression.InData.Operand, expression.InData.IsNegated, expression.InData.TargetKind, expression.InData.Values, replacement, expression.InData.TargetName, expression.InData.FunctionArguments);
            found = true;
        }

        if (expression.ArrayData != null && expression.ArrayData.Query == existingQuery)
        {
            copy.ArrayData = new SqlArrayExpressionData(expression.ArrayData.Elements, replacement);
            found = true;
        }

        if (!found)
        {
            throw new ArgumentException("The query is not a direct child of the expression root.", "existingQuery");
        }

        return copy;
    }

    internal void ValidateRewriteOptions(SqlRewriteOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }
    }

    internal SqlScript CopyScriptRoot(SqlScript script, SqlStatement[] statements, SqlRewriteOptions options)
    {
        SqlTextSpan span = options.PreserveSpans ? script.Span : new SqlTextSpan(0, 0);
        SqlScript copy = new SqlScript(statements, span);
        copy.Tokens = options.PreserveTokens ? script.Tokens : new SqlToken[0];
        copy.TokenRange = options.PreserveTokens ? script.TokenRange : new SqlTokenRange(0, 0);
        copy.LeadingTrivia = options.PreserveTrivia ? script.LeadingTrivia : new SqlTrivia[0];
        copy.TrailingTrivia = options.PreserveTrivia ? script.TrailingTrivia : new SqlTrivia[0];
        copy.SourceText = options.PreserveSourceText ? script.SourceText : "";
        copy.UnsupportedSyntax = script.UnsupportedSyntax;
        return copy;
    }

    internal SqlStatement CopyStatementRoot(SqlStatement statement, SqlRewriteOptions options)
    {
        SqlTextSpan span = options.PreserveSpans ? statement.Span : new SqlTextSpan(0, 0);
        SqlToken[] tokens = options.PreserveTokens ? statement.Tokens : new SqlToken[0];
        SqlStatement copy = new SqlStatement(statement.Kind, span, tokens);
        copy.TokenRange = options.PreserveTokens ? statement.TokenRange : new SqlTokenRange(0, 0);
        copy.LeadingTrivia = options.PreserveTrivia ? statement.LeadingTrivia : new SqlTrivia[0];
        copy.TrailingTrivia = options.PreserveTrivia ? statement.TrailingTrivia : new SqlTrivia[0];
        copy.Terminator = options.PreserveTokens ? statement.Terminator : null;
        copy.SourceText = options.PreserveSourceText ? statement.SourceText : "";
        copy.QuotedTokens = options.PreserveTokens ? statement.QuotedTokens : new SqlToken[0];
        copy.UnsupportedSyntax = statement.UnsupportedSyntax;
        copy.Query = statement.Query;
        copy.Insert = statement.Insert;
        copy.Update = statement.Update;
        copy.Delete = statement.Delete;
        copy.Merge = statement.Merge;
        copy.CreateTable = statement.CreateTable;
        copy.AlterTable = statement.AlterTable;
        copy.DropTable = statement.DropTable;
        copy.CreateIndex = statement.CreateIndex;
        copy.DropIndex = statement.DropIndex;
        copy.CreateView = statement.CreateView;
        copy.DropView = statement.DropView;
        copy.CreateTrigger = statement.CreateTrigger;
        copy.DropTrigger = statement.DropTrigger;
        copy.CreateVirtualTable = statement.CreateVirtualTable;
        copy.Pragma = statement.Pragma;
        copy.Analyze = statement.Analyze;
        copy.Vacuum = statement.Vacuum;
        copy.Attach = statement.Attach;
        copy.Detach = statement.Detach;
        copy.Reindex = statement.Reindex;
        copy.Transaction = statement.Transaction;
        copy.Savepoint = statement.Savepoint;
        copy.Release = statement.Release;
        copy.Explain = statement.Explain;
        return copy;
    }

    internal SqlQuery CopyQueryRoot(SqlQuery query, SqlSelectCore[] selectCores, SqlCompoundQueryPart[] compoundParts, SqlRewriteOptions options)
    {
        SqlTextSpan span = options.PreserveSpans ? query.Span : new SqlTextSpan(0, 0);
        SqlQuery copy = new SqlQuery(query.CommonTableExpressions, query.IsRecursive, selectCores, query.CompoundOperators, query.OrderBy, query.Limit, span);
        copy.CompoundParts = compoundParts;
        copy.OffsetFetch = query.OffsetFetch;
        copy.UnsupportedClauses = query.UnsupportedClauses;
        return copy;
    }

    internal SqlTableReference CopyTableRoot(SqlTableReference table, SqlJoin[] joins, SqlRewriteOptions options)
    {
        SqlTextSpan span = options.PreserveSpans ? table.Span : new SqlTextSpan(0, 0);
        SqlTableReference copy = new SqlTableReference(table.Kind, span, table.Name, table.Query, table.FunctionExpression, table.Inner, joins, table.Alias, table.ColumnAliases);
        copy.SourceData = table.SourceData;
        copy.UnsupportedClauses = table.UnsupportedClauses;
        return copy;
    }

    internal SqlExpression CopyExpressionRoot(SqlExpression expression, SqlExpression[] arguments, SqlRewriteOptions options)
    {
        SqlTextSpan span = options.PreserveSpans ? expression.Span : new SqlTextSpan(0, 0);
        SqlToken[] tokens = options.PreserveTokens ? expression.Tokens : new SqlToken[0];
        SqlExpression copy = new SqlExpression(expression.Kind, span, tokens, arguments);
        copy.Left = expression.Left;
        copy.Right = expression.Right;
        copy.Operand = expression.Operand;
        copy.Alternate = expression.Alternate;
        copy.Name = expression.Name;
        copy.Literal = expression.Literal;
        copy.Query = expression.Query;
        copy.TypeName = expression.TypeName;
        copy.Value = expression.Value;
        copy.OperatorText = expression.OperatorText;
        copy.OperatorToken = options.PreserveTokens ? expression.OperatorToken : null;
        copy.UnaryOperator = expression.UnaryOperator;
        copy.BinaryOperator = expression.BinaryOperator;
        copy.IsDistinct = expression.IsDistinct;
        copy.OrderBy = expression.OrderBy;
        copy.Filter = expression.Filter;
        copy.WindowDefinition = expression.WindowDefinition;
        copy.CollateData = expression.CollateData;
        copy.PatternData = expression.PatternData;
        copy.DistinctnessData = expression.DistinctnessData;
        copy.InData = expression.InData;
        copy.ExtractData = expression.ExtractData;
        copy.CastData = expression.CastData;
        copy.ArrayData = expression.ArrayData;
        copy.SubscriptData = expression.SubscriptData;
        copy.OperatorData = expression.OperatorData;
        copy.WildcardData = expression.WildcardData;
        copy.FunctionData = expression.FunctionData;
        return copy;
    }
}
