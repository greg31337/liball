# sql

A small C# SQL parser, AST, formatter, analyzer, and validation library with SQLite support.

## Public API

Public API declarations live under `src/Api` and are organized by role. Operations are defined by interfaces and provided by instance-based service classes. Results, options, diagnostics, catalogs, tokens, source text, and AST nodes are data classes whose state is exposed through fields. Low-level parsers, tree walkers, dialect builders, and output writers remain internal implementation details.

## Parsing

Create a parser service, select dialect options, and inspect the returned data fields:

```csharp
using sql;

ISqlDialectCatalog dialects = new SqlDialectCatalogService();
SqlParseOptions options = new SqlParseOptions();
options.Dialect = dialects.CreateOptions(SqlDialectKind.SQLite);

ISqlParser parser = new SqlParserService();
SqlParseResult result = parser.ParseScript(
    "select id, name from users where id = ?;",
    options);

if (result.Diagnostics.Diagnostics.Count != 0)
{
    SqlDiagnostic diagnostic = result.Diagnostics.Diagnostics[0];
    Console.WriteLine(diagnostic.Code + ": " + diagnostic.Message);
}

SqlScript script = result.Script;
SqlStatement statement = script.Statements[0];
Console.WriteLine(statement.Kind); // Select
Console.WriteLine(result.Tokens.Length); // Includes the end-of-file token
```

`ISqlParser` also defines `ParseStatement`, `ParseExpression`, and `Tokenize` for narrower workflows. Applications can inject or decorate that contract and pass it to `SqlFormatterService`.

## Inspecting

Use `ISqlAnalyzer` for read-only searches and high-level statement effects:

```csharp
ISqlAnalyzer analyzer = new SqlAnalyzerService();

SqlName[] tables = analyzer.FindTablesInScript(script);
SqlName[] columns = analyzer.FindColumnsInScript(script);
SqlExpression[] parameters = analyzer.FindParametersInScript(script);
SqlExpression[] functions = analyzer.FindFunctionsInScript(script);

SqlStatementSummary summary = analyzer.GetStatementSummary(script.Statements[0]);

Console.WriteLine(tables[0].Parts[0].Text);
Console.WriteLine(columns[0].Parts[0].Text);
Console.WriteLine(parameters[0].Value);
Console.WriteLine(functions[0].Name.Parts[0].Text);
Console.WriteLine(summary.ReadTables.Length);
```

The analyzer uses distinct method names for scripts, statements, queries, and expressions so its interface contains no overloaded members.

## Formatting and generation

Format raw SQL through the formatter service:

```csharp
ISqlFormatter formatter = new SqlFormatterService(parser);

SqlFormatOptions formatOptions = new SqlFormatOptions();
formatOptions.KeywordCase = SqlKeywordCase.Lower;
formatOptions.CommaStyle = SqlCommaStyle.Leading;

string formatted = formatter.Format(
    "select id, name from users",
    formatOptions);
```

Use `ISqlGenerator` and `SqlGeneratorService` when the input is already an expression, query, statement, or script. Methods with explicit options have unique names such as `GenerateStatementWithOptions` and `GenerateScriptWithOptions`.

## Validation and diagnostics

Catalogs are data models, so callers populate their public collections directly before validating:

```csharp
SqlCatalog catalog = new SqlCatalog();
SqlCatalogSchema schema = catalog.Schemas[0];

SqlCatalogTable users = new SqlCatalogTable("users");
users.Columns.Add(new SqlCatalogColumn("id", "integer"));
users.Columns.Add(new SqlCatalogColumn("name", "text"));
schema.Tables.Add(users);

SqlParseResult parsed = parser.ParseScript(
    "select missing from users;",
    options);

ISqlValidator validator = new SqlValidatorService();
SqlValidationResult validation = validator.Validate(parsed.Script, catalog);

if (validation.Diagnostics.Diagnostics.Count != 0)
{
    SqlDiagnostic diagnostic = validation.Diagnostics.Diagnostics[0];
    Console.WriteLine(diagnostic.Code + ": " + diagnostic.Message);
}
```
