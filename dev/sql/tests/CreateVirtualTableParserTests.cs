using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class CreateVirtualTableParserTests
{
    [Test]
    public void StatementParser_AttachesCreateVirtualTableSchemaAndPreservesModuleArguments()
    {
        SqlParseResult result = SqlStatementParser.ParseScript("create virtual table if not exists docs using fts5(content,tokenize='porter',prefix='2 3')", SqlParseOptions.CreateSqlite());

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.Script.GetStatementCount(), Is.EqualTo(1));
        SqlStatement statement = result.Script.GetStatementAt(0);
        Assert.That(statement.Kind, Is.EqualTo(SqlStatementKind.CreateVirtualTable));
        Assert.That(statement.CreateVirtualTable, Is.Not.Null);
        SqlVirtualTableDefinition table = statement.CreateVirtualTable!;
        Assert.That(table.IfNotExists, Is.True);
        Assert.That(table.Name.GetText(), Is.EqualTo("docs"));
        Assert.That(table.ModuleName.GetText(), Is.EqualTo("fts5"));
        Assert.That(table.GetModuleArgumentCount(), Is.EqualTo(9));
        Assert.That(table.GetModuleArgumentAt(0).Text, Is.EqualTo("content"));
        Assert.That(table.GetModuleArgumentAt(2).Text, Is.EqualTo("tokenize"));
        Assert.That(table.GetModuleArgumentAt(4).Text, Is.EqualTo("'porter'"));
        Assert.That(table.GetModuleArgumentAt(8).Text, Is.EqualTo("'2 3'"));
    }

    [Test]
    public void CreateVirtualTableParser_PreservesNestedModuleArgumentTokens()
    {
        SqlVirtualTableDefinition table = ParseVirtualTable("create virtual table docs using custom(arg(matchinfo(docs)),option)");

        Assert.That(table.IfNotExists, Is.False);
        Assert.That(table.ModuleName.GetText(), Is.EqualTo("custom"));
        Assert.That(table.GetModuleArgumentCount(), Is.EqualTo(9));
        Assert.That(table.GetModuleArgumentAt(0).Text, Is.EqualTo("arg"));
        Assert.That(table.GetModuleArgumentAt(1).Kind, Is.EqualTo(SqlTokenKind.OpenParen));
        Assert.That(table.GetModuleArgumentAt(5).Kind, Is.EqualTo(SqlTokenKind.CloseParen));
        Assert.That(table.GetModuleArgumentAt(8).Text, Is.EqualTo("option"));
    }

    [Test]
    public void CreateVirtualTableParser_ReportsMalformedInputs()
    {
        SqlParseResult missingUsing = SqlStatementParser.ParseScript("create virtual table docs fts5(content)", SqlParseOptions.CreateSqlite());
        SqlParseResult missingArguments = SqlStatementParser.ParseScript("create virtual table docs using fts5", SqlParseOptions.CreateSqlite());
        SqlParseResult malformedIf = SqlStatementParser.ParseScript("create virtual table if docs using fts5(content)", SqlParseOptions.CreateSqlite());

        Assert.That(missingUsing.IsSuccess(), Is.False);
        Assert.That(missingUsing.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLVIRTUAL008"));
        Assert.That(missingArguments.IsSuccess(), Is.False);
        Assert.That(missingArguments.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLVIRTUAL010"));
        Assert.That(malformedIf.IsSuccess(), Is.False);
        Assert.That(malformedIf.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLVIRTUAL004"));
    }

    [Test]
    public void CreateVirtualTableParser_RejectsInvalidInputs()
    {
        Assert.Throws<ArgumentNullException>(() => new SqlCreateVirtualTableParser(null!));
    }

    static SqlVirtualTableDefinition ParseVirtualTable(string text)
    {
        SqlParseResult result = SqlStatementParser.ParseScript(text, SqlParseOptions.CreateSqlite());
        Assert.That(result.IsSuccess(), Is.True);
        return result.Script.GetStatementAt(0).CreateVirtualTable!;
    }
}
