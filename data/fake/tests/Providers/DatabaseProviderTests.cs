using fake;

namespace fake.Tests;

[TestFixture]
public class DatabaseProviderTests
{
    [Test]
    public void ScalarIdentifiersAreDeterministicPortableAndReviewed()
    {
        const long seed = 11700;
        DatabaseProvider first = Fake.Create(seed).Database();
        DatabaseProvider second = Fake.Create(seed).Database();
        string[] databases = new string[]
        {
            "analytics", "application", "catalog", "example", "reporting",
            "warehouse"
        };
        string[] schemas = new string[]
        {
            "app", "audit", "core", "public", "reporting", "staging"
        };
        string[] tables = new string[]
        {
            "accounts", "audit_events", "customers", "inventory_items",
            "order_lines", "orders", "products", "users"
        };
        string[] columns = new string[]
        {
            "created_at", "customer_id", "display_name", "id", "is_active",
            "order_id", "quantity", "status", "updated_at", "user_id"
        };

        int index = 0;
        while (index < 128)
        {
            string database = first.DatabaseName();
            string schema = first.SchemaName();
            string table = first.TableName();
            string column = first.ColumnName();
            string identifier = first.SqlIdentifier();
            Assert.Multiple(() =>
            {
                Assert.That(database, Is.EqualTo(second.DatabaseName()));
                Assert.That(schema, Is.EqualTo(second.SchemaName()));
                Assert.That(table, Is.EqualTo(second.TableName()));
                Assert.That(column, Is.EqualTo(second.ColumnName()));
                Assert.That(identifier, Is.EqualTo(second.SqlIdentifier()));
                Assert.That(database, Is.AnyOf(databases));
                Assert.That(schema, Is.AnyOf(schemas));
                Assert.That(table, Is.AnyOf(tables));
                Assert.That(column, Is.AnyOf(columns));
                Assert.That(first.IsSqlIdentifier(database), Is.True);
                Assert.That(first.IsSqlIdentifier(schema), Is.True);
                Assert.That(first.IsSqlIdentifier(table), Is.True);
                Assert.That(first.IsSqlIdentifier(column), Is.True);
                Assert.That(first.IsSqlIdentifier(identifier), Is.True);
            });
            index++;
        }
    }

    [Test]
    public void IndexNamesCorrelateReviewedTableAndColumnParts()
    {
        DatabaseProvider database = Fake.Create(11701).Database();
        string[] tables = new string[]
        {
            "accounts", "audit_events", "customers", "inventory_items",
            "order_lines", "orders", "products", "users"
        };
        string[] columns = new string[]
        {
            "created_at", "customer_id", "display_name", "id", "is_active",
            "order_id", "quantity", "status", "updated_at", "user_id"
        };

        int index = 0;
        while (index < 128)
        {
            string value = database.IndexName();
            Assert.That(value, Does.StartWith("ix_"));
            Assert.That(database.IsSqlIdentifier(value), Is.True);
            bool matched = false;
            int tableIndex = 0;
            while (tableIndex < tables.Length)
            {
                int columnIndex = 0;
                while (columnIndex < columns.Length)
                {
                    if (value == "ix_" + tables[tableIndex] + "_" +
                        columns[columnIndex])
                    {
                        matched = true;
                    }
                    columnIndex++;
                }
                tableIndex++;
            }
            Assert.That(matched, Is.True, value);
            index++;
        }
    }

    [Test]
    public void ConstraintKindsUseCategorySpecificPortablePrefixes()
    {
        DatabaseProvider database = Fake.Create(11702).Database();
        string primary = database.ConstraintNameFromKind(
            DatabaseConstraintKind.PrimaryKey);
        string foreign = database.ConstraintNameFromKind(
            DatabaseConstraintKind.ForeignKey);
        string unique = database.ConstraintNameFromKind(
            DatabaseConstraintKind.Unique);
        string check = database.ConstraintNameFromKind(
            DatabaseConstraintKind.Check);

        Assert.Multiple(() =>
        {
            Assert.That(primary, Does.Match("^pk_[a-z][a-z0-9_]*$"));
            Assert.That(foreign, Does.Match("^fk_[a-z][a-z0-9_]*$"));
            Assert.That(unique, Does.Match("^uq_[a-z][a-z0-9_]*$"));
            Assert.That(check, Does.Match("^ck_[a-z][a-z0-9_]*$"));
            Assert.That(database.IsSqlIdentifier(primary), Is.True);
            Assert.That(database.IsSqlIdentifier(foreign), Is.True);
            Assert.That(database.IsSqlIdentifier(unique), Is.True);
            Assert.That(database.IsSqlIdentifier(check), Is.True);
        });

        int index = 0;
        while (index < 128)
        {
            string value = database.ConstraintName();
            Assert.Multiple(() =>
            {
                Assert.That(value, Does.Match("^(?:pk|fk|uq|ck)_"));
                Assert.That(database.IsSqlIdentifier(value), Is.True);
            });
            index++;
        }
    }

    [Test]
    public void TypesCollationsAndEnginesComeFromReviewedTables()
    {
        const long seed = 11703;
        DatabaseProvider first = Fake.Create(seed).Database();
        DatabaseProvider second = Fake.Create(seed).Database();
        string[] types = new string[]
        {
            "BIGINT", "BOOLEAN", "DATE", "DECIMAL(18,2)", "INTEGER", "JSON",
            "TEXT", "TIMESTAMP", "UUID", "VARCHAR(255)"
        };
        string[] collations = new string[]
        {
            "BINARY", "Latin1_General_100_CI_AS", "NOCASE", "en_US.utf8",
            "utf8mb4_unicode_ci"
        };
        string[] engines = new string[]
        {
            "MariaDB", "Microsoft SQL Server", "MySQL", "Oracle Database",
            "PostgreSQL", "SQLite"
        };

        int index = 0;
        while (index < 128)
        {
            string type = first.Type();
            string collation = first.Collation();
            string engine = first.Engine();
            Assert.Multiple(() =>
            {
                Assert.That(type, Is.EqualTo(second.Type()));
                Assert.That(collation, Is.EqualTo(second.Collation()));
                Assert.That(engine, Is.EqualTo(second.Engine()));
                Assert.That(type, Is.AnyOf(types));
                Assert.That(collation, Is.AnyOf(collations));
                Assert.That(engine, Is.AnyOf(engines));
            });
            index++;
        }
    }

    [Test]
    public void SqlIdentifierValidationRejectsMalformedAndReservedText()
    {
        DatabaseProvider database = Fake.Create(11704).Database();

        Assert.Multiple(() =>
        {
            Assert.That(database.IsSqlIdentifier("a"), Is.True);
            Assert.That(database.IsSqlIdentifier("_private"), Is.True);
            Assert.That(database.IsSqlIdentifier("Item_20"), Is.True);
            Assert.That(database.IsSqlIdentifier(""), Is.False);
            Assert.That(database.IsSqlIdentifier("20_items"), Is.False);
            Assert.That(database.IsSqlIdentifier("line-item"), Is.False);
            Assert.That(database.IsSqlIdentifier("two words"), Is.False);
            Assert.That(database.IsSqlIdentifier("naïve"), Is.False);
            Assert.That(database.IsSqlIdentifier("select"), Is.False);
            Assert.That(database.IsSqlIdentifier("SELECT"), Is.False);
            Assert.That(database.IsSqlIdentifier("table"), Is.False);
            Assert.That(database.IsSqlIdentifier("user"), Is.False);
            Assert.That(() => database.IsSqlIdentifier(null),
                Throws.TypeOf<ArgumentNullException>());
        });
    }

    [Test]
    public void LimitsAndInvalidCategoriesFailBeforeEntropy()
    {
        const long seed = 11705;
        FakeOptions tinyOptions = FakeOptions.Create(seed);
        tinyOptions.MaximumOutputSize = 1;
        DatabaseProvider inspected = Fake.Create(tinyOptions).Database();
        DatabaseProvider untouched = Fake.Create(tinyOptions).Database();
        FakeOptions countOptions = FakeOptions.Create(seed);
        countOptions.MaximumCount = 5;
        DatabaseProvider countInspected = Fake.Create(countOptions).Database();
        DatabaseProvider countUntouched = Fake.Create(countOptions).Database();
        FakeOptions identifierOptions = FakeOptions.Create(seed);
        identifierOptions.MaximumCount = 9;
        DatabaseProvider identifierInspected = Fake.Create(identifierOptions)
            .Database();
        DatabaseProvider identifierUntouched = Fake.Create(identifierOptions)
            .Database();

        Assert.Multiple(() =>
        {
            Assert.That(() => inspected.DatabaseName(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => inspected.SchemaName(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => inspected.TableName(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => inspected.ColumnName(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => inspected.IndexName(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => inspected.ConstraintName(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => inspected.Type(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => inspected.Collation(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => inspected.Engine(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => inspected.SqlIdentifier(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => inspected.ConstraintNameFromKind(
                (DatabaseConstraintKind)99),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.DatabaseName(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.Engine(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => identifierInspected.SqlIdentifier(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => identifierInspected.IsSqlIdentifier(
                "identifier"), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(inspected.RandomValue.Long(),
                Is.EqualTo(untouched.RandomValue.Long()));
            Assert.That(countInspected.RandomValue.Long(),
                Is.EqualTo(countUntouched.RandomValue.Long()));
            Assert.That(identifierInspected.RandomValue.Long(),
                Is.EqualTo(identifierUntouched.RandomValue.Long()));
        });
    }

    [Test]
    public void DatabaseGenerationDoesNotPerturbOtherProviderStreams()
    {
        Fake inspected = Fake.Create(11706);
        Fake untouched = Fake.Create(11706);

        int index = 0;
        while (index < 32)
        {
            inspected.Database().DatabaseName();
            inspected.Database().SchemaName();
            inspected.Database().TableName();
            inspected.Database().ColumnName();
            inspected.Database().IndexName();
            inspected.Database().ConstraintName();
            inspected.Database().Type();
            inspected.Database().Collation();
            inspected.Database().Engine();
            inspected.Database().SqlIdentifier();
            index++;
        }

        Assert.That(inspected.Person().FullName(),
            Is.EqualTo(untouched.Person().FullName()));
    }
}
