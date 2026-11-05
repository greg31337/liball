using System;
using System.Collections.Generic;

namespace sql;

internal class SqlCreateTableParser
{
    internal SqlParserContext Context;

    internal SqlCreateTableParser(SqlParserContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        Context = context;
    }

    internal SqlTableDefinition ParseCreateTable()
    {
        SkipTrivia();
        SqlToken first = Context.Peek();
        if (IsKeyword(Context.Peek(), "CREATE"))
        {
            Context.Consume();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLDDL001", "Expected CREATE.", Context.Peek().Span);
            Context.Consume();
        }

        bool isTemporary = false;
        SkipTrivia();
        if (IsKeyword(Context.Peek(), "TEMP") || IsKeyword(Context.Peek(), "TEMPORARY"))
        {
            Context.Consume();
            isTemporary = true;
        }

        SkipTrivia();
        if (IsKeyword(Context.Peek(), "TABLE"))
        {
            Context.Consume();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLDDL002", "Expected TABLE in CREATE TABLE statement.", Context.Peek().Span);
        }

        bool ifNotExists = ParseOptionalIfNotExists();
        SkipTrivia();
        SqlName tableName = ParseName("SQLDDL003", "Expected table name in CREATE TABLE statement.");
        SqlColumnDefinition[] columns = new SqlColumnDefinition[0];
        SqlTableConstraint[] constraints = new SqlTableConstraint[0];
        SqlQuery? query = null;
        bool withoutRowId = false;
        bool isStrict = false;
        int end = tableName.Span.GetEnd();
        SkipTrivia();
        if (Context.Peek().Kind == SqlTokenKind.OpenParen)
        {
            SqlCreateTableElementList elements = ParseTableElementList();
            columns = elements.Columns;
            constraints = elements.Constraints;
            end = Context.PeekAtOffset(-1).Span.GetEnd();
            SqlCreateTableOptions options = ParseTableOptions(end);
            withoutRowId = options.WithoutRowId;
            isStrict = options.IsStrict;
            end = options.End;
        }
        else
        {
            if (IsKeyword(Context.Peek(), "AS"))
            {
                query = ParseCreateTableQuery();
                end = query.Span.GetEnd();
            }
            else
            {
                Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLDDL004", "Expected column list in CREATE TABLE statement.", Context.Peek().Span);
            }
        }

        SqlTextSpan span = SqlTextSpan.FromBounds(first.Span.Start, end);
        SqlTableDefinition definition = new SqlTableDefinition(isTemporary, ifNotExists, tableName, columns, constraints, query, withoutRowId, isStrict, span);
        if (query == null)
        {
            definition.Options = CompletedTableOptions;
        }

        return definition;
    }

    internal SqlTableOptionKind[] CompletedTableOptions = new SqlTableOptionKind[0];

    internal bool ParseOptionalIfNotExists()
    {
        SkipTrivia();
        if (!IsKeyword(Context.Peek(), "IF"))
        {
            return false;
        }

        Context.Consume();
        SkipTrivia();
        if (IsKeyword(Context.Peek(), "NOT"))
        {
            Context.Consume();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLDDL005", "Expected NOT after IF.", Context.Peek().Span);
        }

        SkipTrivia();
        if (IsKeyword(Context.Peek(), "EXISTS"))
        {
            Context.Consume();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLDDL006", "Expected EXISTS after IF NOT.", Context.Peek().Span);
        }

        return true;
    }

    internal SqlQuery ParseCreateTableQuery()
    {
        Context.Consume();
        SkipTrivia();
        SqlQueryParser parser = new SqlQueryParser(Context);
        return parser.ParseQuery();
    }

    internal SqlCreateTableElementList ParseTableElementList()
    {
        Context.Consume();
        List<SqlColumnDefinition> columns = new List<SqlColumnDefinition>();
        List<SqlTableConstraint> constraints = new List<SqlTableConstraint>();
        SkipTrivia();
        while (Context.Peek().Kind != SqlTokenKind.CloseParen && Context.Peek().Kind != SqlTokenKind.EndOfFile)
        {
            if (IsTableConstraintStart(Context.Peek()))
            {
                SqlTableConstraint constraint = ParseTableConstraint();
                constraints.Add(constraint);
            }
            else
            {
                SqlColumnDefinition column = ParseColumnDefinition();
                columns.Add(column);
            }

            SkipTrivia();
            if (Context.Peek().Kind == SqlTokenKind.Comma)
            {
                Context.Consume();
                SkipTrivia();
                continue;
            }

            break;
        }

        if (Context.Peek().Kind == SqlTokenKind.CloseParen)
        {
            Context.Consume();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLDDL007", "Expected closing parenthesis in CREATE TABLE statement.", Context.Peek().Span);
        }

        if (columns.Count == 0)
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLDDL008", "Expected column definition in CREATE TABLE statement.", Context.Peek().Span);
        }

        return new SqlCreateTableElementList(columns.ToArray(), constraints.ToArray());
    }

    internal SqlCreateTableOptions ParseTableOptions(int end)
    {
        bool withoutRowId = false;
        bool isStrict = false;
        List<SqlTableOptionKind> options = new List<SqlTableOptionKind>();
        SkipTrivia();
        while (IsTableOptionStart(Context.Peek()))
        {
            if (IsKeyword(Context.Peek(), "WITHOUT"))
            {
                SqlToken last = ParseWithoutRowIdOption();
                withoutRowId = true;
                options.Add(SqlTableOptionKind.WithoutRowId);
                end = last.Span.GetEnd();
            }
            else
            {
                SqlToken strict = Context.Consume();
                isStrict = true;
                options.Add(SqlTableOptionKind.Strict);
                end = strict.Span.GetEnd();
            }

            SkipTrivia();
            if (Context.Peek().Kind != SqlTokenKind.Comma)
            {
                break;
            }

            SqlToken comma = Context.Consume();
            end = comma.Span.GetEnd();
            SkipTrivia();
            if (!IsTableOptionStart(Context.Peek()))
            {
                Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLDDL027", "Expected CREATE TABLE option after comma.", Context.Peek().Span);
                break;
            }
        }

        CompletedTableOptions = options.ToArray();
        return new SqlCreateTableOptions(withoutRowId, isStrict, end);
    }

    internal SqlToken ParseWithoutRowIdOption()
    {
        SqlToken last = Context.Consume();
        SkipTrivia();
        if (IsKeyword(Context.Peek(), "ROWID"))
        {
            last = Context.Consume();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLDDL028", "Expected ROWID after WITHOUT in CREATE TABLE option.", Context.Peek().Span);
        }

        return last;
    }

    internal SqlColumnDefinition ParseColumnDefinition()
    {
        SqlName name = ParseName("SQLDDL009", "Expected column name in CREATE TABLE statement.");
        int end = name.Span.GetEnd();
        SqlTextSpan typeSpan = ParseColumnTypeNameSpan();
        string sourceTypeName = "";
        string typeName = "";
        SqlColumnConstraint[] constraints = new SqlColumnConstraint[0];
        if (typeSpan.Length > 0)
        {
            sourceTypeName = GetSourceText(typeSpan).Trim();
            typeName = sourceTypeName.ToLowerInvariant();
            end = typeSpan.GetEnd();
        }

        constraints = ParseColumnConstraints();
        SqlExpression? generatedExpression = null;
        SqlExpression? defaultExpression = null;
        SqlGeneratedColumnKind generatedKind = SqlGeneratedColumnKind.None;
        int constraintIndex = 0;
        while (constraintIndex < constraints.Length)
        {
            if (constraints[constraintIndex].IsGenerated())
            {
                generatedExpression = constraints[constraintIndex].Expression;
                generatedKind = constraints[constraintIndex].GeneratedKind;
                break;
            }

            if (constraints[constraintIndex].Kind == SqlConstraintKind.Default)
            {
                defaultExpression = constraints[constraintIndex].Expression;
            }

            constraintIndex = constraintIndex + 1;
        }

        if (Context.PeekAtOffset(-1).Kind != SqlTokenKind.OpenParen && Context.PeekAtOffset(-1).Kind != SqlTokenKind.Comma)
        {
            end = Context.PeekAtOffset(-1).Span.GetEnd();
        }

        SqlTextSpan span = SqlTextSpan.FromBounds(name.Span.Start, end);
        SqlColumnDefinition definition = new SqlColumnDefinition(name, typeName, sourceTypeName, constraints, defaultExpression, generatedExpression, generatedKind, span);
        if (typeSpan.Length > 0)
        {
            definition.DeclaredType = new SqlColumnTypeName(typeName, sourceTypeName, typeSpan);
        }

        return definition;
    }

    internal SqlTableConstraint ParseTableConstraint()
    {
        SkipTrivia();
        SqlToken first = Context.Peek();
        SqlName? constraintName = null;
        if (IsKeyword(Context.Peek(), "CONSTRAINT"))
        {
            Context.Consume();
            SkipTrivia();
            if (IsIdentifierLike(Context.Peek()))
            {
                constraintName = ParseName("SQLDDL018", "Expected table constraint name.");
            }
            else
            {
                Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLDDL018", "Expected table constraint name.", Context.Peek().Span);
            }

            SkipTrivia();
        }

        if (IsKeyword(Context.Peek(), "PRIMARY"))
        {
            return ParseKeyTableConstraint(SqlConstraintKind.PrimaryKey, first, constraintName);
        }

        if (IsKeyword(Context.Peek(), "UNIQUE"))
        {
            return ParseKeyTableConstraint(SqlConstraintKind.Unique, first, constraintName);
        }

        if (IsKeyword(Context.Peek(), "CHECK"))
        {
            Context.Consume();
            SqlExpression expression = ParseConstraintExpression("SQLDDL019", "Expected table CHECK expression.");
            SqlToken last = Context.PeekAtOffset(-1);
            SkipDefinitionItem();
            return CreateTableConstraint(SqlConstraintKind.Check, constraintName, new SqlName[0], new SqlExpression[] { expression }, SqlConflictActionKind.None, null, first, last);
        }

        if (IsKeyword(Context.Peek(), "FOREIGN"))
        {
            return ParseForeignKeyTableConstraint(first, constraintName);
        }

        SkipDefinitionItem();
        SqlToken unknownLast = Context.PeekAtOffset(-1);
        return CreateTableConstraint(SqlConstraintKind.Unknown, constraintName, new SqlName[0], new SqlExpression[0], SqlConflictActionKind.None, null, first, unknownLast);
    }

    internal SqlTableConstraint ParseKeyTableConstraint(SqlConstraintKind kind, SqlToken first, SqlName? constraintName)
    {
        Context.Consume();
        SkipTrivia();
        if (kind == SqlConstraintKind.PrimaryKey)
        {
            if (IsKeyword(Context.Peek(), "KEY"))
            {
                Context.Consume();
            }
            else
            {
                Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLDDL020", "Expected KEY after PRIMARY.", Context.Peek().Span);
            }
        }

        SqlOrderingTerm[] indexedColumns = ParseIndexedColumnList();
        SqlName[] columns = GetIndexedColumnNames(indexedColumns);
        SqlConflictActionKind conflictAction = ParseOptionalConflictClause();
        SqlToken last = Context.PeekAtOffset(-1);
        SkipDefinitionItem();
        SqlTableConstraint constraint = CreateTableConstraint(kind, constraintName, columns, new SqlExpression[0], conflictAction, null, first, last);
        constraint.IndexedColumns = indexedColumns;
        return constraint;
    }

    internal SqlTableConstraint ParseForeignKeyTableConstraint(SqlToken first, SqlName? constraintName)
    {
        Context.Consume();
        SkipTrivia();
        if (IsKeyword(Context.Peek(), "KEY"))
        {
            Context.Consume();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLDDL021", "Expected KEY after FOREIGN.", Context.Peek().Span);
        }

        SqlName[] columns = ParseOptionalNameList();
        SkipTrivia();
        SqlForeignKeyReference? reference = null;
        SqlToken last = Context.PeekAtOffset(-1);
        if (IsKeyword(Context.Peek(), "REFERENCES"))
        {
            Context.Consume();
            SkipTrivia();
            reference = ParseForeignKeyReference();
            last = Context.PeekAtOffset(-1);
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLDDL022", "Expected REFERENCES in FOREIGN KEY constraint.", Context.Peek().Span);
        }

        SkipDefinitionItem();
        return CreateTableConstraint(SqlConstraintKind.ForeignKey, constraintName, columns, new SqlExpression[0], SqlConflictActionKind.None, reference, first, last);
    }

    internal SqlTableConstraint CreateTableConstraint(SqlConstraintKind kind, SqlName? constraintName, SqlName[] columns, SqlExpression[] expressions, SqlConflictActionKind conflictAction, SqlForeignKeyReference? reference, SqlToken first, SqlToken last)
    {
        SqlTextSpan span = SqlTextSpan.FromBounds(first.Span.Start, last.Span.GetEnd());
        return new SqlTableConstraint(kind, constraintName, columns, expressions, conflictAction, reference, span);
    }

    internal SqlColumnConstraint[] ParseColumnConstraints()
    {
        List<SqlColumnConstraint> constraints = new List<SqlColumnConstraint>();
        SkipTrivia();
        while (Context.Peek().Kind != SqlTokenKind.Comma && Context.Peek().Kind != SqlTokenKind.CloseParen && Context.Peek().Kind != SqlTokenKind.EndOfFile)
        {
            SqlColumnConstraint? constraint = ParseColumnConstraint();
            if (constraint != null)
            {
                constraints.Add(constraint);
            }

            SkipTrivia();
        }

        return constraints.ToArray();
    }

    internal SqlColumnConstraint? ParseColumnConstraint()
    {
        SkipTrivia();
        SqlToken first = Context.Peek();
        SqlName? constraintName = null;
        if (IsKeyword(Context.Peek(), "CONSTRAINT"))
        {
            Context.Consume();
            SkipTrivia();
            if (IsIdentifierLike(Context.Peek()))
            {
                constraintName = ParseName("SQLDDL010", "Expected constraint name.");
            }
            else
            {
                Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLDDL010", "Expected constraint name.", Context.Peek().Span);
            }

            SkipTrivia();
        }

        if (IsKeyword(Context.Peek(), "PRIMARY"))
        {
            return ParsePrimaryKeyColumnConstraint(first, constraintName);
        }

        if (IsKeyword(Context.Peek(), "NOT"))
        {
            return ParseNotNullColumnConstraint(first, constraintName);
        }

        if (IsKeyword(Context.Peek(), "UNIQUE"))
        {
            SqlToken unique = Context.Consume();
            SqlConflictActionKind conflictAction = ParseOptionalConflictClause();
            SqlToken last = Context.PeekAtOffset(-1);
            return CreateColumnConstraint(SqlConstraintKind.Unique, constraintName, null, null, null, false, conflictAction, first, last);
        }

        if (IsKeyword(Context.Peek(), "CHECK"))
        {
            return ParseExpressionColumnConstraint(SqlConstraintKind.Check, first, constraintName);
        }

        if (IsKeyword(Context.Peek(), "DEFAULT"))
        {
            return ParseExpressionColumnConstraint(SqlConstraintKind.Default, first, constraintName);
        }

        if (IsKeyword(Context.Peek(), "COLLATE"))
        {
            return ParseCollateColumnConstraint(first, constraintName);
        }

        if (IsKeyword(Context.Peek(), "REFERENCES"))
        {
            return ParseReferencesColumnConstraint(first, constraintName);
        }

        if (IsKeyword(Context.Peek(), "GENERATED") || IsKeyword(Context.Peek(), "AS"))
        {
            return ParseGeneratedColumnConstraint(first, constraintName);
        }

        if (IsKeyword(Context.Peek(), "NULL"))
        {
            SqlToken nullToken = Context.Consume();
            return CreateColumnConstraint(SqlConstraintKind.Unknown, constraintName, null, null, null, false, SqlConflictActionKind.None, first, nullToken);
        }

        SkipDefinitionItem();
        return null;
    }

    internal SqlColumnConstraint ParsePrimaryKeyColumnConstraint(SqlToken first, SqlName? constraintName)
    {
        Context.Consume();
        SkipTrivia();
        SqlToken last = Context.PeekAtOffset(-1);
        if (IsKeyword(Context.Peek(), "KEY"))
        {
            last = Context.Consume();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLDDL011", "Expected KEY after PRIMARY.", Context.Peek().Span);
        }

        SkipTrivia();
        SqlOrderingDirection direction = SqlOrderingDirection.None;
        if (IsKeyword(Context.Peek(), "ASC"))
        {
            last = Context.Consume();
            direction = SqlOrderingDirection.Asc;
        }
        else if (IsKeyword(Context.Peek(), "DESC"))
        {
            last = Context.Consume();
            direction = SqlOrderingDirection.Desc;
        }

        SqlConflictActionKind conflictAction = ParseOptionalConflictClause();
        bool isAutoincrement = false;
        SkipTrivia();
        if (IsKeyword(Context.Peek(), "AUTOINCREMENT"))
        {
            last = Context.Consume();
            isAutoincrement = true;
        }

        SqlColumnConstraint constraint = CreateColumnConstraint(SqlConstraintKind.PrimaryKey, constraintName, null, null, null, isAutoincrement, conflictAction, first, last);
        constraint.PrimaryKeyDirection = direction;
        return constraint;
    }

    internal SqlColumnConstraint ParseNotNullColumnConstraint(SqlToken first, SqlName? constraintName)
    {
        Context.Consume();
        SkipTrivia();
        SqlToken last = Context.PeekAtOffset(-1);
        if (IsKeyword(Context.Peek(), "NULL"))
        {
            last = Context.Consume();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLDDL012", "Expected NULL after NOT.", Context.Peek().Span);
        }

        SqlConflictActionKind conflictAction = ParseOptionalConflictClause();
        last = Context.PeekAtOffset(-1);
        return CreateColumnConstraint(SqlConstraintKind.NotNull, constraintName, null, null, null, false, conflictAction, first, last);
    }

    internal SqlColumnConstraint ParseExpressionColumnConstraint(SqlConstraintKind kind, SqlToken first, SqlName? constraintName)
    {
        Context.Consume();
        SqlExpression expression = ParseConstraintExpression("SQLDDL013", "Expected column constraint expression.");
        SqlToken last = Context.PeekAtOffset(-1);
        return CreateColumnConstraint(kind, constraintName, expression, null, null, false, SqlConflictActionKind.None, first, last);
    }

    internal SqlColumnConstraint ParseCollateColumnConstraint(SqlToken first, SqlName? constraintName)
    {
        Context.Consume();
        SkipTrivia();
        SqlName? collation = null;
        SqlToken last = Context.PeekAtOffset(-1);
        if (IsIdentifierLike(Context.Peek()))
        {
            collation = ParseName("SQLDDL014", "Expected collation name.");
            last = Context.PeekAtOffset(-1);
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLDDL014", "Expected collation name.", Context.Peek().Span);
        }

        return CreateColumnConstraint(SqlConstraintKind.Collate, constraintName, null, collation, null, false, SqlConflictActionKind.None, first, last);
    }

    internal SqlColumnConstraint ParseReferencesColumnConstraint(SqlToken first, SqlName? constraintName)
    {
        Context.Consume();
        SkipTrivia();
        SqlForeignKeyReference reference = ParseForeignKeyReference();
        SqlToken last = Context.PeekAtOffset(-1);
        return CreateColumnConstraint(SqlConstraintKind.References, constraintName, null, null, reference, false, SqlConflictActionKind.None, first, last);
    }

    internal SqlForeignKeyReference ParseForeignKeyReference()
    {
        SqlName tableName = ParseName("SQLDDL015", "Expected referenced table name.");
        SqlName[] columnNames = ParseOptionalNameList();
        string onDeleteAction = "";
        string onUpdateAction = "";
        SqlName? matchName = null;
        bool isDeferrable = false;
        bool isInitiallyDeferred = false;
        SqlForeignKeyDeferrabilityKind deferrability = SqlForeignKeyDeferrabilityKind.None;
        SqlForeignKeyInitialTimingKind initialTiming = SqlForeignKeyInitialTimingKind.None;
        List<SqlForeignKeyClause> clauses = new List<SqlForeignKeyClause>();
        SqlToken last = Context.PeekAtOffset(-1);
        SkipTrivia();
        while (IsForeignKeyClauseStart())
        {
            if (IsKeyword(Context.Peek(), "ON"))
            {
                SqlToken clauseFirst = Context.Consume();
                SkipTrivia();
                if (IsKeyword(Context.Peek(), "DELETE"))
                {
                    Context.Consume();
                    onDeleteAction = ParseForeignKeyAction();
                    last = Context.PeekAtOffset(-1);
                    clauses.Add(new SqlForeignKeyClause(SqlForeignKeyClauseKind.OnDelete, SqlForeignKeyReference.GetActionKind(onDeleteAction), null, SqlForeignKeyDeferrabilityKind.None, SqlForeignKeyInitialTimingKind.None, SqlTextSpan.FromBounds(clauseFirst.Span.Start, last.Span.GetEnd())));
                }
                else
                {
                    if (IsKeyword(Context.Peek(), "UPDATE"))
                    {
                        Context.Consume();
                        onUpdateAction = ParseForeignKeyAction();
                        last = Context.PeekAtOffset(-1);
                        clauses.Add(new SqlForeignKeyClause(SqlForeignKeyClauseKind.OnUpdate, SqlForeignKeyReference.GetActionKind(onUpdateAction), null, SqlForeignKeyDeferrabilityKind.None, SqlForeignKeyInitialTimingKind.None, SqlTextSpan.FromBounds(clauseFirst.Span.Start, last.Span.GetEnd())));
                    }
                    else
                    {
                        Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLDDL029", "Expected DELETE or UPDATE in foreign key action.", Context.Peek().Span);
                    }
                }
            }
            else
            {
                if (IsKeyword(Context.Peek(), "MATCH"))
                {
                    SqlToken clauseFirst = Context.Consume();
                    SkipTrivia();
                    matchName = ParseName("SQLDDL031", "Expected MATCH name.");
                    last = Context.PeekAtOffset(-1);
                    clauses.Add(new SqlForeignKeyClause(SqlForeignKeyClauseKind.Match, SqlForeignKeyActionKind.None, matchName, SqlForeignKeyDeferrabilityKind.None, SqlForeignKeyInitialTimingKind.None, SqlTextSpan.FromBounds(clauseFirst.Span.Start, last.Span.GetEnd())));
                }
                else
                {
                    if (IsKeyword(Context.Peek(), "DEFERRABLE"))
                    {
                        SqlToken clauseFirst = Context.Consume();
                        last = clauseFirst;
                        isDeferrable = true;
                        deferrability = SqlForeignKeyDeferrabilityKind.Deferrable;
                        SqlForeignKeyDeferralParseResult deferral = ParseOptionalInitiallyClause(last);
                        last = deferral.Last;
                        initialTiming = deferral.InitialTiming;
                        isInitiallyDeferred = initialTiming == SqlForeignKeyInitialTimingKind.Deferred;
                        clauses.Add(new SqlForeignKeyClause(SqlForeignKeyClauseKind.Deferrability, SqlForeignKeyActionKind.None, null, deferrability, initialTiming, SqlTextSpan.FromBounds(clauseFirst.Span.Start, last.Span.GetEnd())));
                    }
                    else
                    {
                        SqlToken clauseFirst = Context.Consume();
                        SkipTrivia();
                        if (IsKeyword(Context.Peek(), "DEFERRABLE"))
                        {
                            last = Context.Consume();
                            isDeferrable = false;
                            deferrability = SqlForeignKeyDeferrabilityKind.NotDeferrable;
                            SqlForeignKeyDeferralParseResult deferral = ParseOptionalInitiallyClause(last);
                            last = deferral.Last;
                            initialTiming = deferral.InitialTiming;
                            isInitiallyDeferred = initialTiming == SqlForeignKeyInitialTimingKind.Deferred;
                            clauses.Add(new SqlForeignKeyClause(SqlForeignKeyClauseKind.Deferrability, SqlForeignKeyActionKind.None, null, deferrability, initialTiming, SqlTextSpan.FromBounds(clauseFirst.Span.Start, last.Span.GetEnd())));
                        }
                    }
                }
            }

            SkipTrivia();
        }

        SqlTextSpan referenceSpan = SqlTextSpan.FromBounds(tableName.Span.Start, last.Span.GetEnd());
        SqlForeignKeyReference reference = new SqlForeignKeyReference(tableName, columnNames, onDeleteAction, onUpdateAction, matchName, isDeferrable, isInitiallyDeferred, referenceSpan);
        reference.Deferrability = deferrability;
        reference.InitialTiming = initialTiming;
        reference.Clauses = clauses.ToArray();
        return reference;
    }

    internal string ParseForeignKeyAction()
    {
        SkipTrivia();
        if (IsKeyword(Context.Peek(), "SET"))
        {
            Context.Consume();
            SkipTrivia();
            if (IsKeyword(Context.Peek(), "NULL"))
            {
                Context.Consume();
                return "SET NULL";
            }

            if (IsKeyword(Context.Peek(), "DEFAULT"))
            {
                Context.Consume();
                return "SET DEFAULT";
            }
        }
        else
        {
            if (IsKeyword(Context.Peek(), "CASCADE"))
            {
                Context.Consume();
                return "CASCADE";
            }

            if (IsKeyword(Context.Peek(), "RESTRICT"))
            {
                Context.Consume();
                return "RESTRICT";
            }

            if (IsKeyword(Context.Peek(), "NO"))
            {
                Context.Consume();
                SkipTrivia();
                if (IsKeyword(Context.Peek(), "ACTION"))
                {
                    Context.Consume();
                    return "NO ACTION";
                }
            }
        }

        Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLDDL030", "Expected foreign key action.", Context.Peek().Span);
        return "";
    }

    internal SqlForeignKeyDeferralParseResult ParseOptionalInitiallyClause(SqlToken last)
    {
        SkipTrivia();
        if (!IsKeyword(Context.Peek(), "INITIALLY"))
        {
            return new SqlForeignKeyDeferralParseResult(SqlForeignKeyInitialTimingKind.None, last);
        }

        last = Context.Consume();
        SkipTrivia();
        if (IsKeyword(Context.Peek(), "DEFERRED"))
        {
            last = Context.Consume();
            return new SqlForeignKeyDeferralParseResult(SqlForeignKeyInitialTimingKind.Deferred, last);
        }

        if (IsKeyword(Context.Peek(), "IMMEDIATE"))
        {
            last = Context.Consume();
            return new SqlForeignKeyDeferralParseResult(SqlForeignKeyInitialTimingKind.Immediate, last);
        }

        Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLDDL032", "Expected DEFERRED or IMMEDIATE after INITIALLY.", Context.Peek().Span);
        return new SqlForeignKeyDeferralParseResult(SqlForeignKeyInitialTimingKind.None, last);
    }

    internal SqlColumnConstraint ParseGeneratedColumnConstraint(SqlToken first, SqlName? constraintName)
    {
        SqlToken last = Context.PeekAtOffset(-1);
        bool isGeneratedAlways = false;
        if (IsKeyword(Context.Peek(), "GENERATED"))
        {
            last = Context.Consume();
            isGeneratedAlways = true;
            SkipTrivia();
            if (IsKeyword(Context.Peek(), "ALWAYS"))
            {
                last = Context.Consume();
                SkipTrivia();
            }
        }

        if (IsKeyword(Context.Peek(), "AS"))
        {
            last = Context.Consume();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLDDL025", "Expected AS in generated column constraint.", Context.Peek().Span);
        }

        SqlExpression expression = ParseGeneratedColumnExpression();
        last = Context.PeekAtOffset(-1);
        SqlGeneratedColumnKind generatedKind = SqlGeneratedColumnKind.Virtual;
        bool hasGeneratedKind = false;
        SkipTrivia();
        if (IsKeyword(Context.Peek(), "VIRTUAL"))
        {
            last = Context.Consume();
            generatedKind = SqlGeneratedColumnKind.Virtual;
            hasGeneratedKind = true;
        }
        else
        {
            if (IsKeyword(Context.Peek(), "STORED"))
            {
                last = Context.Consume();
                generatedKind = SqlGeneratedColumnKind.Stored;
                hasGeneratedKind = true;
            }
        }

        SqlColumnConstraint constraint = CreateGeneratedColumnConstraint(constraintName, expression, generatedKind, first, last);
        constraint.IsGeneratedAlways = isGeneratedAlways;
        constraint.HasGeneratedStorageKind = hasGeneratedKind;
        return constraint;
    }

    internal SqlExpression ParseGeneratedColumnExpression()
    {
        List<SqlToken> tokens = new List<SqlToken>();
        SkipTrivia();
        int start = Context.Peek().Span.Start;
        int end = start;
        int depth = 0;
        while (Context.Peek().Kind != SqlTokenKind.EndOfFile)
        {
            if (depth == 0 && (Context.Peek().Kind == SqlTokenKind.Comma || Context.Peek().Kind == SqlTokenKind.CloseParen || IsColumnConstraintStart(Context.Peek()) || IsKeyword(Context.Peek(), "VIRTUAL") || IsKeyword(Context.Peek(), "STORED")))
            {
                break;
            }

            SqlToken token = Context.Consume();
            tokens.Add(token);
            end = token.Span.GetEnd();
            if (token.Kind == SqlTokenKind.OpenParen)
            {
                depth = depth + 1;
            }
            else
            {
                if (token.Kind == SqlTokenKind.CloseParen)
                {
                    depth = depth - 1;
                }
            }
        }

        if (tokens.Count == 0)
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLDDL026", "Expected generated column expression.", Context.Peek().Span);
            return SqlExpression.Create(SqlExpressionKind.Unknown, Context.Peek().Span);
        }

        SqlToken[] expressionTokens = CreateParseTokens(tokens, end);
        SqlTokenStream stream = SqlTokenStream.Create(expressionTokens, Context.Diagnostics);
        SqlParserContext context = SqlParserContext.Create(stream, Context.Options, Context.SourceText);
        SqlExpressionParser parser = new SqlExpressionParser(context);
        return parser.ParseExpression();
    }

    internal SqlName[] ParseOptionalNameList()
    {
        SkipTrivia();
        if (Context.Peek().Kind != SqlTokenKind.OpenParen)
        {
            return new SqlName[0];
        }

        Context.Consume();
        List<SqlName> names = new List<SqlName>();
        SkipTrivia();
        while (Context.Peek().Kind != SqlTokenKind.CloseParen && Context.Peek().Kind != SqlTokenKind.EndOfFile)
        {
            SqlName name = ParseName("SQLDDL016", "Expected name in column list.");
            names.Add(name);
            SkipTrivia();
            if (Context.Peek().Kind == SqlTokenKind.Comma)
            {
                Context.Consume();
                SkipTrivia();
                continue;
            }

            break;
        }

        if (Context.Peek().Kind == SqlTokenKind.CloseParen)
        {
            Context.Consume();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLDDL017", "Expected closing parenthesis in column list.", Context.Peek().Span);
        }

        return names.ToArray();
    }

    internal SqlOrderingTerm[] ParseIndexedColumnList()
    {
        SkipTrivia();
        if (Context.Peek().Kind != SqlTokenKind.OpenParen)
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLDDL016", "Expected indexed column list.", Context.Peek().Span);
            return new SqlOrderingTerm[0];
        }

        Context.Consume();
        List<SqlOrderingTerm> terms = new List<SqlOrderingTerm>();
        SkipTrivia();
        while (Context.Peek().Kind != SqlTokenKind.CloseParen && Context.Peek().Kind != SqlTokenKind.EndOfFile)
        {
            SqlQueryParser parser = new SqlQueryParser(Context);
            SqlOrderingTerm term = parser.ParseOrderingTerm();
            terms.Add(term);
            SkipTrivia();
            if (Context.Peek().Kind == SqlTokenKind.Comma)
            {
                Context.Consume();
                SkipTrivia();
                continue;
            }

            break;
        }

        if (Context.Peek().Kind == SqlTokenKind.CloseParen)
        {
            Context.Consume();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLDDL017", "Expected closing parenthesis in indexed column list.", Context.Peek().Span);
        }

        return terms.ToArray();
    }

    internal SqlName[] GetIndexedColumnNames(SqlOrderingTerm[] terms)
    {
        List<SqlName> names = new List<SqlName>();
        int index = 0;
        while (index < terms.Length)
        {
            SqlName? name = terms[index].Expression.Name;
            if (name != null)
            {
                names.Add(name);
            }

            index = index + 1;
        }

        return names.ToArray();
    }

    internal SqlColumnConstraint CreateColumnConstraint(SqlConstraintKind kind, SqlName? constraintName, SqlExpression? expression, SqlName? collation, SqlForeignKeyReference? reference, bool isAutoincrement, SqlConflictActionKind conflictAction, SqlToken first, SqlToken last)
    {
        SqlTextSpan span = SqlTextSpan.FromBounds(first.Span.Start, last.Span.GetEnd());
        return new SqlColumnConstraint(kind, constraintName, expression, collation, conflictAction, reference, SqlGeneratedColumnKind.None, isAutoincrement, span);
    }

    internal SqlColumnConstraint CreateGeneratedColumnConstraint(SqlName? constraintName, SqlExpression expression, SqlGeneratedColumnKind generatedKind, SqlToken first, SqlToken last)
    {
        SqlTextSpan span = SqlTextSpan.FromBounds(first.Span.Start, last.Span.GetEnd());
        return new SqlColumnConstraint(SqlConstraintKind.Generated, constraintName, expression, null, SqlConflictActionKind.None, null, generatedKind, false, span);
    }

    internal SqlConflictActionKind ParseOptionalConflictClause()
    {
        SkipTrivia();
        if (!IsKeyword(Context.Peek(), "ON"))
        {
            return SqlConflictActionKind.None;
        }

        Context.Consume();
        SkipTrivia();
        if (IsKeyword(Context.Peek(), "CONFLICT"))
        {
            Context.Consume();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLDDL023", "Expected CONFLICT after ON.", Context.Peek().Span);
        }

        SkipTrivia();
        if (IsKeyword(Context.Peek(), "ROLLBACK"))
        {
            Context.Consume();
            return SqlConflictActionKind.Rollback;
        }

        if (IsKeyword(Context.Peek(), "ABORT"))
        {
            Context.Consume();
            return SqlConflictActionKind.Abort;
        }

        if (IsKeyword(Context.Peek(), "FAIL"))
        {
            Context.Consume();
            return SqlConflictActionKind.Fail;
        }

        if (IsKeyword(Context.Peek(), "IGNORE"))
        {
            Context.Consume();
            return SqlConflictActionKind.Ignore;
        }

        if (IsKeyword(Context.Peek(), "REPLACE"))
        {
            Context.Consume();
            return SqlConflictActionKind.Replace;
        }

        Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLDDL024", "Expected conflict action.", Context.Peek().Span);
        return SqlConflictActionKind.None;
    }

    internal SqlExpression ParseConstraintExpression(string diagnosticCode, string diagnosticMessage)
    {
        List<SqlToken> tokens = new List<SqlToken>();
        SkipTrivia();
        int start = Context.Peek().Span.Start;
        int end = start;
        int depth = 0;
        while (Context.Peek().Kind != SqlTokenKind.EndOfFile)
        {
            if (depth == 0 && (Context.Peek().Kind == SqlTokenKind.Comma || Context.Peek().Kind == SqlTokenKind.CloseParen || IsColumnConstraintStart(Context.Peek())))
            {
                break;
            }

            SqlToken token = Context.Consume();
            tokens.Add(token);
            end = token.Span.GetEnd();
            if (token.Kind == SqlTokenKind.OpenParen)
            {
                depth = depth + 1;
            }
            else
            {
                if (token.Kind == SqlTokenKind.CloseParen)
                {
                    depth = depth - 1;
                }
            }
        }

        if (tokens.Count == 0)
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, diagnosticCode, diagnosticMessage, Context.Peek().Span);
            return SqlExpression.Create(SqlExpressionKind.Unknown, Context.Peek().Span);
        }

        SqlToken[] expressionTokens = CreateParseTokens(tokens, end);
        SqlTokenStream stream = SqlTokenStream.Create(expressionTokens, Context.Diagnostics);
        SqlParserContext context = SqlParserContext.Create(stream, Context.Options, Context.SourceText);
        SqlExpressionParser parser = new SqlExpressionParser(context);
        return parser.ParseExpression();
    }

    internal SqlToken[] CreateParseTokens(List<SqlToken> tokens, int end)
    {
        SqlToken[] result = new SqlToken[tokens.Count + 1];
        int index = 0;
        while (index < tokens.Count)
        {
            result[index] = tokens[index];
            index = index + 1;
        }

        result[tokens.Count] = new SqlToken(SqlTokenKind.EndOfFile, new SqlTextSpan(end, 0), "");
        return result;
    }

    internal SqlTextSpan ParseColumnTypeNameSpan()
    {
        SkipTrivia();
        if (Context.Peek().Kind == SqlTokenKind.Comma || Context.Peek().Kind == SqlTokenKind.CloseParen || IsColumnConstraintStart(Context.Peek()))
        {
            return new SqlTextSpan(Context.Peek().Span.Start, 0);
        }

        SqlToken first = Context.Peek();
        SqlToken last = first;
        int depth = 0;
        while (Context.Peek().Kind != SqlTokenKind.EndOfFile)
        {
            if (depth == 0 && (Context.Peek().Kind == SqlTokenKind.Comma || Context.Peek().Kind == SqlTokenKind.CloseParen || IsColumnConstraintStart(Context.Peek())))
            {
                break;
            }

            SqlToken token = Context.Consume();
            last = token;
            if (token.Kind == SqlTokenKind.OpenParen)
            {
                depth = depth + 1;
            }
            else
            {
                if (token.Kind == SqlTokenKind.CloseParen)
                {
                    depth = depth - 1;
                }
            }
        }

        return SqlTextSpan.FromBounds(first.Span.Start, last.Span.GetEnd());
    }

    internal void SkipDefinitionItem()
    {
        int depth = 0;
        while (Context.Peek().Kind != SqlTokenKind.EndOfFile)
        {
            if (depth == 0 && (Context.Peek().Kind == SqlTokenKind.Comma || Context.Peek().Kind == SqlTokenKind.CloseParen))
            {
                break;
            }

            SqlToken token = Context.Consume();
            if (token.Kind == SqlTokenKind.OpenParen)
            {
                depth = depth + 1;
            }
            else
            {
                if (token.Kind == SqlTokenKind.CloseParen)
                {
                    depth = depth - 1;
                }
            }
        }
    }

    internal bool IsTableConstraintStart(SqlToken token)
    {
        return IsKeyword(token, "CONSTRAINT") || IsKeyword(token, "PRIMARY") || IsKeyword(token, "UNIQUE") || IsKeyword(token, "CHECK") || IsKeyword(token, "FOREIGN");
    }

    internal bool IsTableOptionStart(SqlToken token)
    {
        return IsKeyword(token, "WITHOUT") || IsKeyword(token, "STRICT");
    }

    internal bool IsColumnConstraintStart(SqlToken token)
    {
        return IsKeyword(token, "CONSTRAINT") || IsKeyword(token, "PRIMARY") || IsKeyword(token, "NOT") || IsKeyword(token, "NULL") || IsKeyword(token, "UNIQUE") || IsKeyword(token, "CHECK") || IsKeyword(token, "DEFAULT") || IsKeyword(token, "COLLATE") || IsKeyword(token, "REFERENCES") || IsKeyword(token, "GENERATED") || IsKeyword(token, "AS");
    }

    internal bool IsForeignKeyClauseStart()
    {
        return IsKeyword(Context.Peek(), "ON") || IsKeyword(Context.Peek(), "MATCH") || IsKeyword(Context.Peek(), "DEFERRABLE") || IsNotDeferrableClause();
    }

    internal bool IsNotDeferrableClause()
    {
        if (!IsKeyword(Context.Peek(), "NOT"))
        {
            return false;
        }

        int offset = 1;
        while (Context.PeekAtOffset(offset).Kind == SqlTokenKind.Whitespace || Context.PeekAtOffset(offset).Kind == SqlTokenKind.LineComment || Context.PeekAtOffset(offset).Kind == SqlTokenKind.BlockComment)
        {
            offset = offset + 1;
        }

        return IsKeyword(Context.PeekAtOffset(offset), "DEFERRABLE");
    }

    internal string GetSourceText(SqlTextSpan span)
    {
        if (Context.SourceText != null)
        {
            return Context.SourceText.GetText(span);
        }

        return "";
    }

    internal SqlName ParseName(string diagnosticCode, string diagnosticMessage)
    {
        List<SqlNamePart> parts = new List<SqlNamePart>();
        SkipTrivia();
        if (!IsIdentifierLike(Context.Peek()))
        {
            SqlToken token = Context.Consume();
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, diagnosticCode, diagnosticMessage, token.Span);
            parts.Add(SqlNamePart.Create(token.Text, token.Text, token.Span, SqlNameQuoteStyle.None, token));
            return CreateName(parts);
        }

        ConsumeNamePart(parts);
        SkipTrivia();
        while (Context.Peek().Kind == SqlTokenKind.Dot)
        {
            Context.Consume();
            SkipTrivia();
            if (!IsIdentifierLike(Context.Peek()))
            {
                Context.AddDiagnostic(SqlDiagnosticSeverity.Error, diagnosticCode, diagnosticMessage, Context.Peek().Span);
                break;
            }

            ConsumeNamePart(parts);
            SkipTrivia();
        }

        return CreateName(parts);
    }

    internal void ConsumeNamePart(List<SqlNamePart> parts)
    {
        SqlToken token = Context.Consume();
        parts.Add(SqlNamePart.Create(GetIdentifierText(token), token.Text, token.Span, GetQuoteStyle(token), token));
    }

    internal SqlName CreateName(List<SqlNamePart> parts)
    {
        if (parts.Count == 0)
        {
            return new SqlName(new SqlNamePart[0], new SqlTextSpan(0, 0));
        }

        SqlNamePart first = parts[0];
        SqlNamePart last = parts[parts.Count - 1];
        SqlTextSpan span = SqlTextSpan.FromBounds(first.Span.Start, last.Span.GetEnd());
        return new SqlName(parts.ToArray(), span);
    }

    internal void SkipTrivia()
    {
        while (Context.Peek().Kind == SqlTokenKind.Whitespace || Context.Peek().Kind == SqlTokenKind.LineComment || Context.Peek().Kind == SqlTokenKind.BlockComment)
        {
            Context.Consume();
        }
    }

    internal bool IsKeyword(SqlToken token, string text)
    {
        string? canonical = Context.Options.Dialect.GetCanonicalKeyword(token.Text);
        if (canonical == null)
        {
            canonical = token.Text.ToUpperInvariant();
        }

        return canonical == text;
    }

    internal bool IsIdentifierLike(SqlToken token)
    {
        return token.Kind == SqlTokenKind.Identifier || token.Kind == SqlTokenKind.Keyword;
    }

    internal string GetIdentifierText(SqlToken token)
    {
        SqlNameQuoteStyle quoteStyle = GetQuoteStyle(token);
        if (quoteStyle == SqlNameQuoteStyle.None)
        {
            return token.Text;
        }

        string text = StripOuterQuotes(token.Text, quoteStyle);
        if (quoteStyle == SqlNameQuoteStyle.DoubleQuote)
        {
            return text.Replace("\"\"", "\"");
        }

        if (quoteStyle == SqlNameQuoteStyle.Bracket)
        {
            return text.Replace("]]", "]");
        }

        if (quoteStyle == SqlNameQuoteStyle.Backtick)
        {
            return text.Replace("``", "`");
        }

        if (quoteStyle == SqlNameQuoteStyle.SingleQuote)
        {
            return text.Replace("''", "'");
        }

        return text;
    }

    internal SqlNameQuoteStyle GetQuoteStyle(SqlToken token)
    {
        if (token.Text.Length == 0)
        {
            return SqlNameQuoteStyle.None;
        }

        if (token.Text[0] == '"')
        {
            return SqlNameQuoteStyle.DoubleQuote;
        }

        if (token.Text[0] == '[')
        {
            return SqlNameQuoteStyle.Bracket;
        }

        if (token.Text[0] == '`')
        {
            return SqlNameQuoteStyle.Backtick;
        }

        if (token.Text[0] == '\'')
        {
            return SqlNameQuoteStyle.SingleQuote;
        }

        return SqlNameQuoteStyle.None;
    }

    internal string StripOuterQuotes(string text, SqlNameQuoteStyle quoteStyle)
    {
        if (quoteStyle == SqlNameQuoteStyle.None || text.Length == 0)
        {
            return text;
        }

        int start = 1;
        int end = text.Length;
        if (text.Length > 1)
        {
            if (quoteStyle == SqlNameQuoteStyle.DoubleQuote && text[text.Length - 1] == '"')
            {
                end = text.Length - 1;
            }

            if (quoteStyle == SqlNameQuoteStyle.Bracket && text[text.Length - 1] == ']')
            {
                end = text.Length - 1;
            }

            if (quoteStyle == SqlNameQuoteStyle.Backtick && text[text.Length - 1] == '`')
            {
                end = text.Length - 1;
            }

            if (quoteStyle == SqlNameQuoteStyle.SingleQuote && text[text.Length - 1] == '\'')
            {
                end = text.Length - 1;
            }
        }

        if (end < start)
        {
            return "";
        }

        return text.Substring(start, end - start);
    }
}

internal class SqlCreateTableElementList
{
    internal SqlColumnDefinition[] Columns;
    internal SqlTableConstraint[] Constraints;

    internal SqlCreateTableElementList(SqlColumnDefinition[] columns, SqlTableConstraint[] constraints)
    {
        if (columns == null)
        {
            throw new ArgumentNullException("columns");
        }

        if (constraints == null)
        {
            throw new ArgumentNullException("constraints");
        }

        Columns = columns;
        Constraints = constraints;
    }
}

internal class SqlCreateTableOptions
{
    internal bool WithoutRowId;
    internal bool IsStrict;
    internal int End;

    internal SqlCreateTableOptions(bool withoutRowId, bool isStrict, int end)
    {
        WithoutRowId = withoutRowId;
        IsStrict = isStrict;
        End = end;
    }
}

internal class SqlForeignKeyDeferralParseResult
{
    internal SqlForeignKeyInitialTimingKind InitialTiming;
    internal SqlToken Last;

    internal SqlForeignKeyDeferralParseResult(SqlForeignKeyInitialTimingKind initialTiming, SqlToken last)
    {
        if (last == null)
        {
            throw new ArgumentNullException("last");
        }

        InitialTiming = initialTiming;
        Last = last;
    }
}
