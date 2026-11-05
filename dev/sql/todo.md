# Universal SQL Parser Library Development Plan

## Goal

Build a no-dependency C# SQL parser library that produces a useful AST, formats SQL, and supports multiple SQL dialects. The first complete dialect target is SQLite. Other dialects should be supported incrementally through explicit dialect options, not by depending on external parser generators or SQL libraries.

Implementation must follow `syntax.md`: simplified C# only, no external libraries, no records, no properties, no LINQ, no inheritance-heavy model, no parser generators, and only built-in .NET APIs. Use small interfaces only at stable substitution boundaries. Only the supported API layer is public; implementation symbols are internal. Every public symbol has XML documentation using only the elements allowed by `syntax.md`.

## Research Notes

Sources reviewed:

- JSqlParser: https://jsqlparser.github.io/JSqlParser/
- SQLGlot: https://sqlglot.com/
- SQLGlot expression tree docs: https://sqlglot.com/sqlglot/expressions/core.html
- Apache Calcite overview: https://calcite.apache.org/docs/index.html
- Apache Calcite SQL reference: https://calcite.apache.org/docs/reference.html
- Apache Calcite validator API: https://calcite.apache.org/javadocAggregate/org/apache/calcite/sql/validate/SqlValidator.html
- Microsoft ScriptDom parser API: https://learn.microsoft.com/dotnet/api/microsoft.sqlserver.transactsql.scriptdom.tsqlparser
- Microsoft ScriptDom generator API: https://learn.microsoft.com/dotnet/api/microsoft.sqlserver.transactsql.scriptdom.sqlscriptgenerator
- sqlparser-rs docs: https://docs.rs/crate/sqlparser/latest
- sqlparser-rs dialect docs: https://docs.rs/sqlparser/latest/sqlparser/dialect/trait.Dialect.html
- SQLite language docs: https://www.sqlite.org/lang.html
- SQLite syntax diagrams: https://www.sqlite.org/syntax.html
- SQLite architecture and parser notes: https://www.sqlite.org/arch.html
- SQLite PRAGMA docs: https://www.sqlite.org/pragma.html
- SQLite source notes for `parse.y`: https://github.com/sqlite/sqlite/blob/master/README.md

Critical lessons from mature libraries:

- JSqlParser proves the value of a traversable AST, broad statement coverage, visitors, deparsing, parameter support, and dialect quirks such as Oracle joins, PostgreSQL casts, T-SQL bracket identifiers, and array syntax.
- SQLGlot proves the value of a no-dependency core, explicit read/write dialects, syntax errors with locations, unsupported-dialect diagnostics, AST traversal helpers, SQL generation, formatting, and optional later transforms.
- Calcite proves the value of separating parsing from validation and planning. Even if this library starts as a parser/formatter, AST design should leave room for catalog-aware validation, scopes, namespaces, and relational plan conversion.
- ScriptDom proves the value of versioned dialect parsers, token stream access, multiple entry points for parsing fragments, parse errors as data, AST fragments with source spans, and a script generator.
- sqlparser-rs proves the value of hand-written recursive descent plus Pratt expression parsing, a generic dialect plus specific dialect flags, and AST output useful for engines and analysis tools.
- SQLite documentation and `parse.y` prove that complete SQLite support must be driven by the official syntax diagrams and grammar, including PRAGMA, virtual tables, triggers, UPSERT, RETURNING, window functions, generated columns, STRICT tables, and SQLite-specific conflict clauses.

## Design Principles

- Use a hand-written lexer, Pratt expression parser, and recursive-descent statement parser.
- Preserve source spans for every token and AST node.
- Produce diagnostics instead of throwing for normal parse failures.
- Keep the AST stable, inspectable, and formatter-friendly.
- Keep dialect differences declarative where possible: keywords, identifier quoting, operators, statement availability, literal rules, and feature flags.
- Keep parser behavior deterministic and allocation-conscious.
- Prefer full fidelity over over-normalization: preserve comments, semicolons, trivia, quoting, and source text when needed.
- Add semantic validation later without making parsing depend on schemas.
- Make SQLite the acceptance baseline before broadening dialect depth.

## Feature Ladder

### Level 0: Repository And Constraints

- Project builds on current .NET target.
- Source files conform to `syntax.md`.
- No external runtime libraries.
- No parser generators.
- Public API uses ordinary classes, enums, fields, constructors, and block-bodied methods.
- Avoid inheritance-based AST. Prefer tagged node classes and explicit child fields.
- Tests include syntax-policy conformance.

### Level 1: Text, Tokens, And Diagnostics

- Source text abstraction with file name, text, line starts, and position lookup.
- `SqlTextSpan` with start, length, end, line, and column helpers.
- Token kinds for identifiers, keywords, literals, punctuation, operators, comments, whitespace, end-of-file, and bad tokens.
- Trivia model for leading and trailing comments and whitespace.
- Lexer diagnostics for invalid characters, unterminated strings, bad numeric literals, malformed blobs, and unterminated comments.
- Case-insensitive keyword recognition with original spelling preserved.
- Dialect-aware keyword tables.
- Dialect-aware identifier rules.
- Token stream API for tooling and tests.

### Level 2: Literals And Identifiers

- Bare identifiers.
- Double-quoted identifiers.
- SQLite bracket identifiers.
- SQLite backtick identifiers.
- T-SQL bracket identifiers.
- MySQL backtick identifiers.
- PostgreSQL dollar-quoted strings.
- Unicode and escaped string options by dialect.
- SQLite string literals with doubled quotes.
- Numeric literals: integers, decimals, exponent notation, signed-number parsing where grammar requires it.
- SQLite hexadecimal integer literals.
- SQLite blob literals.
- Boolean/null/current date/time/timestamp literal values.
- Parameter placeholders: `?`, `?NNN`, `:name`, `@name`, `$name`, and dialect-specific forms.
- Named object support with catalog/schema/table/column parts.

### Level 3: Expression Parser

- Pratt parser with dialect-configurable precedence.
- Parenthesized expressions.
- Unary operators.
- Binary operators.
- Collation operator.
- SQLite postfix `COLLATE`.
- SQLite postfix `ESCAPE` for pattern matching.
- `IS`, `IS NOT`, `IS DISTINCT FROM`, `IS NOT DISTINCT FROM`.
- `LIKE`, `GLOB`, `REGEXP`, `MATCH`.
- `BETWEEN`, `NOT BETWEEN`.
- `IN`, `NOT IN` with list, table, function, and subquery forms.
- `EXISTS`.
- `CASE` searched and simple forms.
- `CAST`.
- PostgreSQL `::` casts.
- Function calls.
- Aggregate invocation with `DISTINCT`, `FILTER`, and function-level `ORDER BY`.
- Window function `OVER`.
- Extract-like special forms.
- Row-value expressions.
- Subquery expressions.
- Qualified wildcard expressions.
- Array/subscript syntax where dialects need it.
- JSON and custom operators as dialect extensions.
- Expression source spans and precedence-aware formatting.

### Level 4: Query Parser

- `SELECT` statement and subquery parsing.
- Result columns with aliases.
- `FROM` table references.
- Table aliases and column aliases.
- Joins: comma, cross, inner, left, right, full, natural, outer, and dialect-specific forms.
- Join constraints: `ON`, `USING`.
- SQLite join operator combinations.
- `WHERE`.
- `GROUP BY`.
- `HAVING`.
- `WINDOW` clause.
- Named windows, partitioning, ordering, frame specs, and exclusions.
- Compound selects: `UNION`, `UNION ALL`, `INTERSECT`, `EXCEPT`.
- SQLite compound select rules.
- `ORDER BY`.
- `LIMIT` and `OFFSET`.
- `FETCH` and `TOP` for other dialects.
- Common table expressions.
- Recursive CTEs.
- Materialized and not-materialized CTE hints where supported.
- Values clauses as query bodies.

### Level 5: DML Statements

- `INSERT`.
- SQLite `INSERT OR conflict-action`.
- Column lists.
- `VALUES`.
- `INSERT ... SELECT`.
- `DEFAULT VALUES`.
- SQLite UPSERT clauses.
- PostgreSQL-style `ON CONFLICT`.
- `RETURNING`.
- `UPDATE`.
- SQLite `UPDATE OR conflict-action`.
- SQLite `UPDATE FROM`.
- SQLite optional `ORDER BY` and `LIMIT` in update when enabled.
- `DELETE`.
- SQLite qualified table names with index hints.
- SQLite optional `ORDER BY` and `LIMIT` in delete when enabled.
- `MERGE` for later non-SQLite dialects.
- `REPLACE` aliases where dialects support them.

### Level 6: SQLite DDL Statements

- `CREATE TABLE`.
- Temporary tables.
- `IF NOT EXISTS`.
- Column definitions.
- SQLite flexible type names and typeless columns.
- Column constraints: primary key, not null, unique, check, default, collate, references, generated columns.
- Table constraints: primary key, unique, check, foreign key.
- Conflict clauses.
- Autoincrement.
- Generated columns: virtual and stored.
- `WITHOUT ROWID`.
- `STRICT`.
- `CREATE TABLE AS SELECT`.
- `ALTER TABLE RENAME TO`.
- `ALTER TABLE RENAME COLUMN`.
- `ALTER TABLE ADD COLUMN`.
- `ALTER TABLE DROP COLUMN`.
- `DROP TABLE`.
- `CREATE INDEX`.
- Unique indexes.
- Partial indexes with `WHERE`.
- Expression indexes.
- Indexed-column ordering and collation.
- `DROP INDEX`.
- `CREATE VIEW`.
- `DROP VIEW`.
- `CREATE TRIGGER`.
- Trigger timing, event, target, `WHEN`, and trigger body statements.
- `DROP TRIGGER`.
- `CREATE VIRTUAL TABLE`.
- Module arguments preserved as structured or raw tokens.

### Level 7: SQLite Utility And Transaction Statements

- `PRAGMA` with optional schema.
- PRAGMA value by equals or parenthesized form.
- PRAGMA table-valued function awareness for normal expression parsing.
- `ANALYZE`.
- `VACUUM` and `VACUUM INTO`.
- `ATTACH DATABASE`.
- `DETACH DATABASE`.
- `REINDEX`.
- `BEGIN`, including deferred, immediate, and exclusive.
- `COMMIT`.
- `END`.
- `ROLLBACK`.
- `ROLLBACK TO`.
- `SAVEPOINT`.
- `RELEASE`.
- `EXPLAIN`.
- `EXPLAIN QUERY PLAN`.

### Level 8: AST Usability

- Root script node containing a statement list.
- Empty statement preservation.
- Statement nodes with kind enum and statement-specific payload fields.
- Expression nodes with kind enum and expression-specific payload fields.
- Query nodes with explicit select cores, compound operators, order, limit, and CTEs.
- Schema nodes for table, column, constraint, index, trigger, and view definitions.
- Name nodes preserving parts and quote style.
- Literal nodes preserving value kind and source text.
- Token and node spans.
- Leading and trailing comments preserved in trivia.
- Internal AST traversal helpers without interfaces or generics, exposed through the public `SqlAnalyzer` facade.
- AST search helpers for tables, columns, parameters, and function calls.
- AST rewrite helpers for replacing direct child nodes.
- Stable enum values.
- Diagnostics attached to parse result.

### Level 9: Formatting And SQL Generation

- Formatter from AST to SQL.
- Generator from AST to compact SQL.
- Dialect-aware quoting and keyword spelling.
- Configurable indentation.
- Configurable keyword casing.
- Configurable identifier casing policy.
- Configurable comma placement.
- Configurable line width target.
- Stable parenthesis insertion based on precedence.
- Comment preservation mode.
- Minimal mode that drops nonessential trivia.
- Round-trip mode that keeps statement order, semicolons, and comments.
- Formatter tests with golden files.
- Parse-format-parse structural equivalence tests.

### Level 10: Multi-Dialect Architecture

- `SqlDialectKind` enum.
- `SqlDialectOptions` class with feature flags.
- `ISqlDialect` descriptor contract for built-in and caller-defined dialects.
- Built-in dialect descriptors for Generic, SQLite, PostgreSQL, MySQL, MariaDB, SQL Server, Oracle, DuckDB, BigQuery, Snowflake, Redshift, Databricks, and ANSI.
- Dialect keyword tables.
- Dialect operator tables.
- Dialect identifier quote policies.
- Dialect string literal policies.
- Dialect statement support flags.
- Dialect-specific parser hooks implemented as ordinary classes and methods.
- Unsupported feature diagnostics.
- Read dialect and write dialect separated in APIs.
- Generic permissive dialect where grammar is unambiguous.

### Level 11: Validation And Analysis

- Optional semantic validation pass.
- Catalog model for schemas, tables, columns, indexes, functions, and collations.
- Name resolution scopes for queries and CTEs.
- Column lineage extraction.
- Table reference extraction.
- Parameter extraction.
- Function inventory extraction.
- Statement kind classification.
- Read/write table impact summary.
- Basic type affinity analysis for SQLite.
- Dialect compatibility warnings.

### Level 12: Transforms And Future Planning

- AST normalization helpers.
- Dialect transpilation where syntax maps cleanly.
- Safe unsupported markers where transpilation would be lossy.
- Query canonicalization for diffing.
- AST diff support.
- Optional relational logical plan conversion later.
- Formatting-only API independent from semantic validation.

## Complete SQLite Dialect Acceptance Scope

SQLite support is complete when the parser accepts and formats the syntax diagrams listed at https://www.sqlite.org/syntax.html, including:

- Statement list and semicolon handling.
- Comments.
- Expressions.
- Literal values and numeric literals.
- Bind parameters.
- All select forms: factored select, select core, simple select, compound select, values, CTE, recursive CTE, joins, windows, frame specs, filter clauses, ordering terms, and result columns.
- DML: insert, upsert, update, delete, returning, conflict actions, qualified table names, and limited update/delete forms.
- DDL: create/drop/alter table, create/drop index, create/drop view, create/drop trigger, create virtual table.
- Constraints: column constraints, table constraints, conflict clauses, foreign keys, generated columns, collations, defaults, checks, strict tables, without rowid.
- Utility statements: analyze, attach, detach, begin, commit, rollback, savepoint, release, pragma, reindex, vacuum.
- SQLite-specific quirks: typeless columns, flexible type names, bracket/backtick identifiers, `INDEXED BY`, `NOT INDEXED`, `RAISE`, `INSERT OR`, `UPDATE OR`, `ON CONFLICT`, `excluded` references as identifiers, virtual table module arguments, and `EXPLAIN QUERY PLAN`.

## Public API Completion Tasks

All implementation under `src/` must use only built-in .NET APIs. Use `ReadOnlySpan<char>` or `Span<char>` at public boundaries only where ownership and lifetime remain clear; retained source and AST data stays in ordinary strings and arrays.

- [DONE] T001: Replace the `object` and `object[]` fields in `SqlStatement`, `SqlExpression`, `SqlSelectCore`, and `SqlReturningClause` with explicit typed payload classes or typed arrays so every supported AST variant is safely inspectable and constructible.
- [DONE] T002: Complete the public source-location layer with exclusive-end, containment, overlap, source-slice, line, column, and file-location operations exposed through an interface and implementation class, including a `ReadOnlySpan<char>` source-slice operation where its lifetime is unambiguous.
- [DONE] T003: Complete the public token-stream tooling layer with non-overloaded peek, lookahead, consume, match, expect, end-state, and position operations exposed through an interface and implementation class without exposing parser internals.
- [DONE] T004: Expand the dialect model to expose immutable keyword data, operator and precedence policies, identifier and string quote policies, parameter forms, literal policies, and statement or clause capability flags, including caller-defined keywords and custom operators.
- [DONE] T005: Complete lexical public models for PostgreSQL dollar-quoted strings, Unicode or escaped strings, dialect-specific parameter forms, custom operators, and all quote styles while preserving original spelling, decoded value, trivia, and source spans.
- [DONE] T006: Complete the expression AST for postfix `COLLATE` and `ESCAPE`, distinctness predicates, list/table/function/subquery `IN`, extract-like forms, PostgreSQL casts, array constructors, subscripts, JSON operators, custom operators, qualified wildcards, aggregate ordering and filtering, and window invocation without lossy generic fields.
- [DONE] T007: Complete the query AST for table-valued and `VALUES` table sources, all join modifiers and constraints, named windows and frames, compound queries, recursive and materialized CTEs, `TOP`, `FETCH`, and dialect-specific query clauses with explicit unsupported-state representation.
- [DONE] T008: Add a query-fragment parse result and uniquely named query parsing operation to the parser contract and service, with the same source, token, option, recovery, and diagnostic data as the existing script, statement, and expression entry points.
- [DONE] T009: Complete DML public models for all planned insert, replace, upsert, returning, update, delete, limited update/delete, and `MERGE` forms, including typed multi-column assignments and dialect-specific conflict targets.
- [DONE] T010: Complete SQLite DDL public models so flexible type names, indexed columns, column and table constraints, foreign-key actions and deferral, generated columns, table options, partial and expression indexes, triggers, views, and virtual-table module arguments round-trip without lost syntax.
- [DONE] T011: Complete SQLite utility and transaction public models for every `PRAGMA`, `ANALYZE`, `VACUUM`, attach/detach, reindex, transaction, savepoint, release, and explain form, including distinct `END`, rollback-to-savepoint, and `EXPLAIN QUERY PLAN` representation.
- [DONE] T012: Add full-fidelity AST data for leading and trailing trivia, empty statements, terminators, preserved token ranges, quoting, and unsupported syntax markers so round-trip generation can retain source order, comments, and semicolons.
- [DONE] T013: Complete generator coverage for every public expression, query, DML, DDL, utility, transaction, and unsupported-marker node, and make the write dialect explicit and independent from the dialect used to parse the tree.
- [DONE] T014: Expand formatting options and formatter operations to support identifier casing, trailing comma policy where legal, target line width, compact/minimal/round-trip modes, comment preservation, dialect-aware quoting, and an explicit read dialect for raw SQL input instead of an implicit SQLite parse.
- [DONE] T015: Add public AST rewrite operations for replacing direct children in scripts, statements, queries, table references, and expressions while preserving untouched nodes, spans, tokens, and diagnostics according to explicit rewrite options.
- [DONE] T016: Expand the catalog data layer to represent qualified schemas, tables, views, columns, indexes, functions, collations, aliases, and SQLite type affinity data needed by validation and analysis.
- [DONE] T017: Complete semantic validation operations for nested query scopes, aliases, CTEs, correlated references, ambiguous columns, DML targets, DDL references, functions, collations, and indexes, with exception-free diagnostics for ordinary validation failures.
- [DONE] T018: Add public dialect-compatibility analysis that reports unsupported statements, clauses, expressions, literals, quoting, and operators against a selected target dialect without changing the input AST.
- [DONE] T019: Add public column-lineage result models and analyzer operations that map output columns and DML assignments to their source tables, columns, expressions, and unknown or ambiguous origins.
- [DONE] T020: Add public SQLite type-affinity result models and analyzer operations for literals, columns, expressions, result columns, and assignments, preserving unknown results when inference is not safe.
- [DONE] T021: Add public normalization and canonicalization options, result models, and transform operations for stable AST comparison while preserving explicit markers for changes that cannot be made losslessly.
- [DONE] T022: Add public dialect-transpilation options, result models, and transform operations for syntax that maps safely between read and write dialects, with diagnostics and unsupported markers for lossy mappings.
- [DONE] T023: Add public AST diff result models and operations that report added, removed, replaced, and unchanged nodes with paths and source spans.
- [DONE] T024: Add the optional public relational logical-plan model and conversion operation for supported query and DML trees, returning diagnostics and unknown nodes when semantic information is insufficient.
- [DONE] T025: Add complete tests for all public source text, span, line-map, diagnostic, token, trivia, token-stream, option, result, dialect descriptor, enum, constructor, boundary, and invalid-argument behavior.
- [DONE] T026: Add exhaustive public AST model tests that construct and inspect every statement, expression, query, DML, DDL, utility, transaction, full-fidelity, unsupported-marker, and enum variant, including all constructor invariants and typed payload relationships.
- [DONE] T027: Add specification-derived SQLite lexer and parser acceptance tests covering every official syntax diagram and grammar form, plus malformed-input diagnostics, recovery limits, fragment entry points, source spans, token preservation, and trivia preservation.
- [DONE] T028: Add dialect contract tests for Generic, ANSI, PostgreSQL, MySQL, MariaDB, SQL Server, Oracle, DuckDB, BigQuery, Snowflake, Redshift, and Databricks covering keywords, literals, identifiers, parameters, operators, precedence, feature flags, custom dialects, and unsupported-feature diagnostics.
- [DONE] T029: Add complete formatter and generator tests for every public AST node and option combination, including compact, minimal, full-fidelity, read/write dialect separation, precedence-safe parentheses, golden output, and parse-generate-parse structural equivalence.
- [DONE] T030: Add complete analyzer, catalog, validation, statement-summary, compatibility, lineage, and SQLite type-affinity tests covering nested scopes, ambiguity, unknown metadata, DDL and DML effects, and all public option branches and failure results.
- [DONE] T031: Add complete transform tests for direct-child rewrites, normalization, canonicalization, safe and lossy transpilation, unsupported markers, AST diffs, and logical-plan conversion, including immutability of untouched input trees and diagnostics for unsupported cases.
