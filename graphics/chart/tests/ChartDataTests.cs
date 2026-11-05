namespace chart.Tests;

[TestFixture]
public class ChartDataTests
{
    [Test]
    public void DataColumns_CopyValuesAndExposeTypedAccess()
    {
        double[] numbers = new double[] { 1.5, 2.5 };
        string[] text = new string[] { "alpha", "beta" };
        System.DateTime[] dates = new System.DateTime[]
        {
            new System.DateTime(2024, 1, 2),
            new System.DateTime(2024, 1, 3)
        };
        string[] categories = new string[] { "B", "A", "B", "C", "A" };

        ChartDataColumn numberColumn = ChartDataColumn.CreateNumber("x", numbers);
        ChartDataColumn textColumn = ChartDataColumn.CreateText("label", text);
        ChartDataColumn dateColumn = ChartDataColumn.CreateDateTime("time", dates);
        ChartDataColumn categoryColumn = ChartDataColumn.CreateCategory("group", categories);

        numbers[0] = 99.0;
        text[0] = "changed";
        dates[0] = new System.DateTime(2000, 1, 1);
        categories[0] = "changed";

        Assert.That(numberColumn.Kind, Is.EqualTo(ChartDataKind.Number));
        Assert.That(numberColumn.GetNumber(0), Is.EqualTo(1.5));
        Assert.That(textColumn.GetText(0), Is.EqualTo("alpha"));
        Assert.That(dateColumn.GetDateTime(0), Is.EqualTo(new System.DateTime(2024, 1, 2)));
        Assert.That(categoryColumn.GetCategory(0), Is.EqualTo("B"));
        Assert.That(categoryColumn.CopyCategoryOrder(), Is.EqualTo(new string[] { "B", "A", "C" }));

        string?[] copiedOrder = categoryColumn.CopyCategoryOrder();
        copiedOrder[0] = "changed";

        Assert.That(categoryColumn.CopyCategoryOrder(), Is.EqualTo(new string[] { "B", "A", "C" }));
    }

    [Test]
    public void ChartDataMatrix_StoresRowMajorValuesAndCopies()
    {
        double[] values = new double[] { 1.0, 2.0, 3.0, 4.0, 5.0, 6.0 };
        ChartDataMatrix matrix = ChartDataMatrix.FromValues(2, 3, values);

        values[0] = 99.0;
        matrix.SetValue(1, 2, 60.0);
        ChartDataMatrix copy = matrix.Copy();
        double[] copiedValues = matrix.CopyValues();
        copiedValues[0] = 77.0;
        matrix.SetValue(0, 0, 10.0);

        Assert.That(matrix.Rows, Is.EqualTo(2));
        Assert.That(matrix.Columns, Is.EqualTo(3));
        Assert.That(matrix.GetValue(0, 0), Is.EqualTo(10.0));
        Assert.That(matrix.GetValue(1, 2), Is.EqualTo(60.0));
        Assert.That(copy.GetValue(0, 0), Is.EqualTo(1.0));
        Assert.That(copy.GetValue(1, 2), Is.EqualTo(60.0));
    }

    [Test]
    public void ChartDataTable_AddsColumnsAndFindsTypedValues()
    {
        ChartDataTable table = new ChartDataTable();
        System.DateTime[] dates = new System.DateTime[]
        {
            new System.DateTime(2024, 2, 1),
            new System.DateTime(2024, 2, 2)
        };

        table.AddNumberColumn("x", new double[] { 1.0, 2.0 });
        table.AddTextColumn("label", new string[] { "A", "B" });
        table.AddDateTimeColumn("time", dates);
        table.AddCategoryColumn("group", new string[] { "east", "west" });
        table.AddNumberColumn("y", new double[] { 10.0, 20.0 });

        Assert.That(table.RowCount, Is.EqualTo(2));
        Assert.That(table.ColumnCount, Is.EqualTo(5));
        Assert.That(table.ContainsColumn("label"), Is.True);
        Assert.That(table.ColumnIndex("time"), Is.EqualTo(2));
        Assert.That(table.GetNumber("x", 1), Is.EqualTo(2.0));
        Assert.That(table.GetText("label", 0), Is.EqualTo("A"));
        Assert.That(table.GetDateTime("time", 1), Is.EqualTo(new System.DateTime(2024, 2, 2)));
        Assert.That(table.GetCategory("group", 1), Is.EqualTo("west"));
        Assert.That(table.GetColumnAt(4).Name, Is.EqualTo("y"));
    }

    [Test]
    public void ChartDataTable_CopiesColumnsAndRejectsInvalidShapes()
    {
        ChartDataTable table = new ChartDataTable();
        double[] source = new double[] { 1.0, 2.0 };

        table.AddNumberColumn("x", source);
        source[0] = 99.0;
        ChartDataTable copy = table.Copy();
        ChartDataColumn[] columns = table.CopyColumns();

        columns[0].NumberValues[0] = 77.0;

        Assert.That(table.GetNumber("x", 0), Is.EqualTo(1.0));
        Assert.That(copy.GetNumber("x", 0), Is.EqualTo(1.0));
        Assert.Throws<System.ArgumentException>(() => table.AddNumberColumn("x", new double[] { 3.0, 4.0 }));
        Assert.Throws<System.ArgumentException>(() => table.AddNumberColumn("bad", new double[] { 1.0 }));
        Assert.Throws<System.ArgumentException>(() => table.GetColumn("missing"));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => table.GetColumnAt(1));
    }

    [Test]
    public void MatrixColumns_CanBeStoredAndQueried()
    {
        ChartDataMatrix matrix = ChartDataMatrix.FromValues(2, 2, new double[] { 1.0, 2.0, 3.0, 4.0 });
        ChartDataTable table = new ChartDataTable();

        table.AddMatrixColumn("z", matrix);
        matrix.SetValue(1, 1, 99.0);

        Assert.That(table.RowCount, Is.EqualTo(4));
        Assert.That(table.GetColumn("z").Kind, Is.EqualTo(ChartDataKind.Matrix));
        Assert.That(table.GetMatrixValue("z", 1, 1), Is.EqualTo(4.0));
    }

    [Test]
    public void ChartDataGroup_GrowsAndCopiesIndices()
    {
        ChartDataGroup group = new ChartDataGroup("north");

        int index = 0;
        while (index < 10)
        {
            group.AddIndex(index * 2);
            index = index + 1;
        }

        int[] copied = group.CopyIndices();
        copied[0] = 999;

        Assert.That(group.Key, Is.EqualTo("north"));
        Assert.That(group.Count, Is.EqualTo(10));
        Assert.That(group.Indices.Length, Is.GreaterThanOrEqualTo(10));
        Assert.That(group.CopyIndices(), Is.EqualTo(new int[] { 0, 2, 4, 6, 8, 10, 12, 14, 16, 18 }));
        Assert.Throws<System.ArgumentNullException>(() => new ChartDataGroup(null!));
    }

    [Test]
    public void ChartData_ValidatesInputsAndKinds()
    {
        ChartDataColumn numberColumn = ChartDataColumn.CreateNumber("x", new double[] { 1.0 });
        ChartDataMatrix matrix = new ChartDataMatrix(1, 1);
        ChartDataTable table = new ChartDataTable();

        Assert.Throws<System.ArgumentNullException>(() => ChartDataColumn.CreateNumber("x", null!));
        Assert.Throws<System.ArgumentNullException>(() => ChartDataColumn.CreateText("x", null!));
        Assert.Throws<System.ArgumentNullException>(() => ChartDataColumn.CreateDateTime("x", null!));
        Assert.Throws<System.ArgumentNullException>(() => ChartDataColumn.CreateCategory("x", null!));
        Assert.Throws<System.ArgumentNullException>(() => ChartDataColumn.CreateMatrix("x", null!));
        Assert.Throws<System.ArgumentNullException>(() => ChartDataColumn.ValidateName(null!));
        Assert.Throws<System.ArgumentException>(() => ChartDataColumn.ValidateName(""));
        Assert.Throws<System.InvalidOperationException>(() => numberColumn.GetText(0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => numberColumn.GetNumber(1));

        Assert.Throws<System.ArgumentOutOfRangeException>(() => new ChartDataMatrix(0, 1));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new ChartDataMatrix(int.MaxValue, 2));
        Assert.Throws<System.ArgumentNullException>(() => ChartDataMatrix.FromValues(1, 1, null!));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartDataMatrix.FromValues(int.MaxValue, 2, new double[0]));
        Assert.Throws<System.ArgumentException>(() => ChartDataMatrix.FromValues(2, 2, new double[] { 1.0 }));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => matrix.GetValue(1, 0));

        Assert.Throws<System.ArgumentNullException>(() => table.AddColumn(null!));
    }
}
