using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class AttachDetachParserTests
{
    [Test]
    public void StatementParser_AttachesAttachTargetPayload()
    {
        SqlParseResult result = SqlStatementParser.ParseScript("attach database 'extra.db' as aux", SqlParseOptions.CreateSqlite());

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.Script.GetStatementCount(), Is.EqualTo(1));
        SqlStatement statement = result.Script.GetStatementAt(0);
        Assert.That(statement.Kind, Is.EqualTo(SqlStatementKind.Attach));
        Assert.That(statement.Attach, Is.Not.Null);
        SqlAttachStatement attach = statement.Attach!;
        Assert.That(attach.HasDatabaseExpression(), Is.True);
        Assert.That(attach.DatabaseExpression!.Kind, Is.EqualTo(SqlExpressionKind.Literal));
        Assert.That(attach.DatabaseExpression.Literal!.Kind, Is.EqualTo(SqlLiteralKind.String));
        Assert.That(attach.DatabaseExpression.Literal.Value, Is.EqualTo("extra.db"));
        Assert.That(attach.HasSchemaName(), Is.True);
        Assert.That(attach.SchemaName!.GetText(), Is.EqualTo("aux"));
    }

    [Test]
    public void AttachParser_ParsesWithoutDatabaseKeywordAndQuotedSchema()
    {
        SqlAttachStatement attach = ParseAttach("attach 'extra.db' as \"aux db\"");

        Assert.That(attach.DatabaseExpression!.Literal!.Value, Is.EqualTo("extra.db"));
        Assert.That(attach.SchemaName!.GetText(), Is.EqualTo("aux db"));
        Assert.That(attach.SchemaName.GetPartAt(0).QuoteStyle, Is.EqualTo(SqlNameQuoteStyle.DoubleQuote));
    }

    [Test]
    public void AttachParser_ReportsMissingAsAndSchema()
    {
        SqlParseResult missingAs = SqlStatementParser.ParseScript("attach 'extra.db' aux", SqlParseOptions.CreateSqlite());
        SqlParseResult missingSchema = SqlStatementParser.ParseScript("attach 'extra.db' as", SqlParseOptions.CreateSqlite());

        Assert.That(missingAs.IsSuccess(), Is.False);
        Assert.That(missingAs.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLATTACH003"));
        Assert.That(missingAs.Script.GetStatementAt(0).Attach, Is.TypeOf<SqlAttachStatement>());
        Assert.That(missingSchema.IsSuccess(), Is.False);
        Assert.That(missingSchema.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLATTACH004"));
        Assert.That(missingSchema.Script.GetStatementAt(0).Attach, Is.TypeOf<SqlAttachStatement>());
    }

    [Test]
    public void StatementParser_AttachesDetachTargetPayload()
    {
        SqlParseResult result = SqlStatementParser.ParseScript("detach database aux", SqlParseOptions.CreateSqlite());

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.Script.GetStatementCount(), Is.EqualTo(1));
        SqlStatement statement = result.Script.GetStatementAt(0);
        Assert.That(statement.Kind, Is.EqualTo(SqlStatementKind.Detach));
        Assert.That(statement.Detach, Is.Not.Null);
        SqlDetachStatement detach = statement.Detach!;
        Assert.That(detach.HasSchemaName(), Is.True);
        Assert.That(detach.SchemaName!.GetText(), Is.EqualTo("aux"));
    }

    [Test]
    public void DetachParser_ParsesWithoutDatabaseKeywordAndQuotedSchema()
    {
        SqlDetachStatement detach = ParseDetach("detach \"aux db\"");

        Assert.That(detach.SchemaName!.GetText(), Is.EqualTo("aux db"));
        Assert.That(detach.SchemaName.GetPartAt(0).QuoteStyle, Is.EqualTo(SqlNameQuoteStyle.DoubleQuote));
    }

    [Test]
    public void DetachParser_ReportsMissingSchema()
    {
        SqlParseResult result = SqlStatementParser.ParseScript("detach database", SqlParseOptions.CreateSqlite());

        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLDETACH002"));
        Assert.That(result.Script.GetStatementAt(0).Detach, Is.TypeOf<SqlDetachStatement>());
    }

    [Test]
    public void AttachDetachParsers_RejectInvalidInputs()
    {
        Assert.Throws<ArgumentNullException>(() => new SqlAttachParser(null!));
        Assert.Throws<ArgumentNullException>(() => new SqlDetachParser(null!));
    }

    static SqlAttachStatement ParseAttach(string text)
    {
        SqlParseResult result = SqlStatementParser.ParseScript(text, SqlParseOptions.CreateSqlite());
        Assert.That(result.IsSuccess(), Is.True);
        return result.Script.GetStatementAt(0).Attach!;
    }

    static SqlDetachStatement ParseDetach(string text)
    {
        SqlParseResult result = SqlStatementParser.ParseScript(text, SqlParseOptions.CreateSqlite());
        Assert.That(result.IsSuccess(), Is.True);
        return result.Script.GetStatementAt(0).Detach!;
    }
}
