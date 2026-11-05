using fake;

namespace fake.Tests;

[TestFixture]
public class DatabaseSqlTextTests
{
    [Test]
    public void ConnectionStringKindsReturnInertReviewedFixtures()
    {
        DatabaseProvider database = Fake.Create(11800).Database();

        Assert.Multiple(() =>
        {
            Assert.That(database.ConnectionStringFromKind(
                DatabaseConnectionKind.PostgreSql), Is.EqualTo(
                "Host=example.invalid;Port=5432;Database=example;Username=example_user;Password=not-a-real-password;"));
            Assert.That(database.ConnectionStringFromKind(
                DatabaseConnectionKind.MySql), Is.EqualTo(
                "Server=example.invalid;Port=3306;Database=example;User ID=example_user;Password=not-a-real-password;"));
            Assert.That(database.ConnectionStringFromKind(
                DatabaseConnectionKind.SqlServer), Is.EqualTo(
                "Server=tcp:example.invalid,1433;Initial Catalog=example;User ID=example_user;Password=not-a-real-password;Encrypt=True;"));
            Assert.That(database.ConnectionStringFromKind(
                DatabaseConnectionKind.Sqlite), Is.EqualTo(
                "Data Source=:memory:;Mode=Memory;Cache=Shared;"));
        });
    }

    [Test]
    public void RandomConnectionStringsAreDeterministicAndRemainSynthetic()
    {
        const long seed = 11801;
        DatabaseProvider first = Fake.Create(seed).Database();
        DatabaseProvider second = Fake.Create(seed).Database();
        string[] reviewed = new string[]
        {
            first.ConnectionStringFromKind(DatabaseConnectionKind.PostgreSql),
            first.ConnectionStringFromKind(DatabaseConnectionKind.MySql),
            first.ConnectionStringFromKind(DatabaseConnectionKind.SqlServer),
            first.ConnectionStringFromKind(DatabaseConnectionKind.Sqlite)
        };

        int index = 0;
        while (index < 128)
        {
            string value = first.ConnectionString();
            Assert.Multiple(() =>
            {
                Assert.That(value, Is.EqualTo(second.ConnectionString()));
                Assert.That(value, Is.AnyOf(reviewed));
                Assert.That(value, Does.EndWith(";"));
                Assert.That(value.Contains("example.invalid") ||
                    value.Contains(":memory:"), Is.True);
                if (value.Contains("Password="))
                {
                    Assert.That(value, Does.Contain(
                        "Password=not-a-real-password;"));
                }
            });
            index++;
        }
    }

    [Test]
    public void IdentifierAndLiteralQuotingEscapeEmbeddedDelimiters()
    {
        DatabaseProvider database = Fake.Create(11802).Database();

        Assert.Multiple(() =>
        {
            Assert.That(database.SqlIdentifierFromValue("customer"),
                Is.EqualTo("\"customer\""));
            Assert.That(database.SqlIdentifierFromValue("order"),
                Is.EqualTo("\"order\""));
            Assert.That(database.SqlIdentifierFromValue("naïve table"),
                Is.EqualTo("\"naïve table\""));
            Assert.That(database.SqlIdentifierFromValue("customer\"archive"),
                Is.EqualTo("\"customer\"\"archive\""));
            Assert.That(database.SqlStringLiteral(""), Is.EqualTo("''"));
            Assert.That(database.SqlStringLiteral("sample"),
                Is.EqualTo("'sample'"));
            Assert.That(database.SqlStringLiteral("O'Brien"),
                Is.EqualTo("'O''Brien'"));
            Assert.That(database.SqlStringLiteral("'quoted'"),
                Is.EqualTo("'''quoted'''"));
            Assert.That(database.SqlStringLiteral("東京"),
                Is.EqualTo("'東京'"));
        });
    }

    [Test]
    public void ExplicitInsertEscapesEveryIdentifierAndValue()
    {
        DatabaseProvider database = Fake.Create(11803).Database();
        string single = database.InsertFromTableAndColumnAndValue(
            "order",
            "display\"name",
            "O'Brien");
        string multi = database.InsertFromTableAndColumnsAndValues(
            "customer archive",
            new string[] { "select", "display\"name" },
            new string[] { "O'Brien", "ready" });
        string injectionShaped = database.InsertFromTableAndColumnAndValue(
            "users\"; DROP TABLE users;--",
            "display_name",
            "x'); DROP TABLE users;--");

        Assert.Multiple(() =>
        {
            Assert.That(single, Is.EqualTo(
                "INSERT INTO \"order\" (\"display\"\"name\") VALUES ('O''Brien');"));
            Assert.That(multi, Is.EqualTo(
                "INSERT INTO \"customer archive\" (\"select\", \"display\"\"name\") VALUES ('O''Brien', 'ready');"));
            Assert.That(injectionShaped, Is.EqualTo(
                "INSERT INTO \"users\"\"; DROP TABLE users;--\" (\"display_name\") VALUES ('x''); DROP TABLE users;--');"));
        });
    }

    [Test]
    public void GeneratedInsertsAreDeterministicEscapedText()
    {
        const long seed = 11804;
        DatabaseProvider first = Fake.Create(seed).Database();
        DatabaseProvider second = Fake.Create(seed).Database();
        bool sawEscapedApostrophe = false;

        int index = 0;
        while (index < 128)
        {
            string value = first.Insert();
            Assert.Multiple(() =>
            {
                Assert.That(value, Is.EqualTo(second.Insert()));
                Assert.That(value, Does.Match(
                    "^INSERT INTO \"[a-z_]+\" \\(\"[a-z_]+\"\\) VALUES \\('.*'\\);$"));
            });
            if (value.Contains("''"))
            {
                sawEscapedApostrophe = true;
            }
            index++;
        }

        Assert.That(sawEscapedApostrophe, Is.True);
    }

    [Test]
    public void MalformedInsertInputsAreRejectedWithoutChangingArrays()
    {
        DatabaseProvider database = Fake.Create(11805).Database();
        string[] columns = new string[] { "first", "second" };
        string[] values = new string[] { "one", "two" };

        Assert.That(database.InsertFromTableAndColumnsAndValues("sample", columns, values), Is.EqualTo(
            "INSERT INTO \"sample\" (\"first\", \"second\") VALUES ('one', 'two');"));
        Assert.Multiple(() =>
        {
            Assert.That(columns, Is.EqualTo(new string[] { "first", "second" }));
            Assert.That(values, Is.EqualTo(new string[] { "one", "two" }));
            Assert.That(() => database.InsertFromTableAndColumnAndValue(null, "column", "value"),
                Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => database.InsertFromTableAndColumnAndValue("", "column", "value"),
                Throws.TypeOf<ArgumentException>());
            Assert.That(() => database.InsertFromTableAndColumnAndValue("bad\nname", "column", "value"),
                Throws.TypeOf<ArgumentException>());
            Assert.That(() => database.InsertFromTableAndColumnsAndValues("table", null, values),
                Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => database.InsertFromTableAndColumnsAndValues("table", columns, null),
                Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => database.InsertFromTableAndColumnsAndValues(
                "table",
                new string[0],
                new string[0]), Throws.TypeOf<ArgumentException>());
            Assert.That(() => database.InsertFromTableAndColumnsAndValues(
                "table",
                columns,
                new string[] { "one" }), Throws.TypeOf<ArgumentException>());
            Assert.That(() => database.InsertFromTableAndColumnsAndValues(
                "table",
                new string[] { null },
                new string[] { "one" }), Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => database.InsertFromTableAndColumnsAndValues(
                "table",
                new string[] { "column" },
                new string[] { null }), Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => database.SqlIdentifierFromValue("\ud800"),
                Throws.TypeOf<ArgumentException>());
            Assert.That(() => database.SqlStringLiteral("a\0b"),
                Throws.TypeOf<ArgumentException>());
        });
    }

    [Test]
    public void LimitsAndInvalidConnectionKindsFailBeforeEntropy()
    {
        const long seed = 11806;
        FakeOptions tinyOptions = FakeOptions.Create(seed);
        tinyOptions.MaximumOutputSize = 1;
        DatabaseProvider inspected = Fake.Create(tinyOptions).Database();
        DatabaseProvider untouched = Fake.Create(tinyOptions).Database();
        FakeOptions countOptions = FakeOptions.Create(seed);
        countOptions.MaximumCount = 3;
        DatabaseProvider countInspected = Fake.Create(countOptions).Database();
        DatabaseProvider countUntouched = Fake.Create(countOptions).Database();

        Assert.Multiple(() =>
        {
            Assert.That(() => inspected.ConnectionString(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => inspected.SqlIdentifierFromValue("a"),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => inspected.SqlStringLiteral("a"),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => inspected.Insert(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.ConnectionString(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.SqlIdentifierFromValue("four"),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.InsertFromTableAndColumnsAndValues(
                "t",
                new string[] { "a", "b", "c", "d" },
                new string[] { "1", "2", "3", "4" }),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => inspected.ConnectionStringFromKind(
                (DatabaseConnectionKind)99),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(inspected.RandomValue.Long(),
                Is.EqualTo(untouched.RandomValue.Long()));
            Assert.That(countInspected.RandomValue.Long(),
                Is.EqualTo(countUntouched.RandomValue.Long()));
        });
    }

    [Test]
    public void SqlTextGenerationDoesNotPerturbOtherProviderStreams()
    {
        Fake inspected = Fake.Create(11807);
        Fake untouched = Fake.Create(11807);

        int index = 0;
        while (index < 32)
        {
            inspected.Database().ConnectionString();
            inspected.Database().SqlIdentifierFromValue("example");
            inspected.Database().SqlStringLiteral("O'Brien");
            inspected.Database().Insert();
            inspected.Database().InsertFromTableAndColumnAndValue("users", "display_name", "sample");
            index++;
        }

        Assert.That(inspected.Person().FullName(),
            Is.EqualTo(untouched.Person().FullName()));
    }
}
