using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class VacuumParserTests
{
    [Test]
    public void StatementParser_AttachesVacuumTargetPayload()
    {
        SqlParseResult result = SqlStatementParser.ParseScript("vacuum temp into 'backup.db'", SqlParseOptions.CreateSqlite());

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.Script.GetStatementCount(), Is.EqualTo(1));
        SqlStatement statement = result.Script.GetStatementAt(0);
        Assert.That(statement.Kind, Is.EqualTo(SqlStatementKind.Vacuum));
        Assert.That(statement.Vacuum, Is.Not.Null);
        SqlVacuumStatement vacuum = statement.Vacuum!;
        Assert.That(vacuum.HasDatabaseName(), Is.True);
        Assert.That(vacuum.DatabaseName!.GetText(), Is.EqualTo("temp"));
        Assert.That(vacuum.HasIntoExpression(), Is.True);
        Assert.That(vacuum.IntoExpression!.Kind, Is.EqualTo(SqlExpressionKind.Literal));
        Assert.That(vacuum.IntoExpression.Literal!.Kind, Is.EqualTo(SqlLiteralKind.String));
        Assert.That(vacuum.IntoExpression.Literal.Value, Is.EqualTo("backup.db"));
    }

    [Test]
    public void VacuumParser_ParsesBareAndIntoOnlyVacuum()
    {
        SqlVacuumStatement bare = ParseVacuum("vacuum");
        SqlVacuumStatement intoOnly = ParseVacuum("vacuum into 'backup.db'");

        Assert.That(bare.HasDatabaseName(), Is.False);
        Assert.That(bare.HasIntoExpression(), Is.False);
        Assert.That(intoOnly.HasDatabaseName(), Is.False);
        Assert.That(intoOnly.HasIntoExpression(), Is.True);
        Assert.That(intoOnly.IntoExpression!.Literal!.Value, Is.EqualTo("backup.db"));
    }

    [Test]
    public void VacuumParser_ParsesQuotedDatabaseName()
    {
        SqlVacuumStatement vacuum = ParseVacuum("vacuum \"aux db\"");

        Assert.That(vacuum.HasDatabaseName(), Is.True);
        Assert.That(vacuum.DatabaseName!.GetText(), Is.EqualTo("aux db"));
        Assert.That(vacuum.DatabaseName.GetPartAt(0).QuoteStyle, Is.EqualTo(SqlNameQuoteStyle.DoubleQuote));
    }

    [Test]
    public void VacuumParser_ReportsMalformedDatabaseName()
    {
        SqlParseResult result = SqlStatementParser.ParseScript("vacuum .", SqlParseOptions.CreateSqlite());

        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLVACUUM002"));
        Assert.That(result.Script.GetStatementAt(0).Vacuum, Is.TypeOf<SqlVacuumStatement>());
    }

    [Test]
    public void VacuumParser_ReportsMissingIntoExpression()
    {
        SqlParseResult result = SqlStatementParser.ParseScript("vacuum main into", SqlParseOptions.CreateSqlite());

        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLVACUUM003"));
        Assert.That(result.Script.GetStatementAt(0).Vacuum, Is.TypeOf<SqlVacuumStatement>());
    }

    [Test]
    public void VacuumParser_RejectsInvalidInputs()
    {
        Assert.Throws<ArgumentNullException>(() => new SqlVacuumParser(null!));
    }

    static SqlVacuumStatement ParseVacuum(string text)
    {
        SqlParseResult result = SqlStatementParser.ParseScript(text, SqlParseOptions.CreateSqlite());
        Assert.That(result.IsSuccess(), Is.True);
        return result.Script.GetStatementAt(0).Vacuum!;
    }
}
