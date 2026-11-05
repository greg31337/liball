namespace chart.Tests;

[TestFixture]
public class ChartDataModelPublicBehaviorTests
{
    [Test]
    public void DataColumn_PublicTypedCopiesSpansAccessorsAndMutationsAreConsistent()
    {
        ChartDataColumnFactory factory = new ChartDataColumnFactory();
        ChartDataColumn number = factory.CreateNumber("number", new double[] { 1.0, 2.0 });
        ChartDataColumn text = factory.CreateText("text", new string? [] { "a", null });
        ChartDataColumn date = factory.CreateDateTime("date", new System.DateTime[] { new System.DateTime(2024, 1, 1) });
        ChartDataColumn category = factory.CreateCategory("category", new string? [] { "b", "a", "b" });
        ChartDataColumn matrix = factory.CreateMatrix("matrix", new ChartDataMatrixFactory().FromValues(1, 2, new double[] { 3.0, 4.0 }));

        double[] numberCopy = number.CopyNumberValues();
        string? [] textCopy = text.CopyTextValues();
        System.DateTime[] dateCopy = date.CopyDateTimeValues();
        string? [] categoryCopy = category.CopyCategoryValues();
        string? [] orderCopy = category.CopyCategoryOrder();
        numberCopy[0] = 99.0;
        textCopy[0] = "changed";
        dateCopy[0] = new System.DateTime(2000, 1, 1);
        categoryCopy[0] = "changed";
        orderCopy[0] = "changed";

        Assert.That(number.GetNumberSpan().ToArray(), Is.EqualTo(new double[] { 1.0, 2.0 }));
        Assert.That(text.GetText(0), Is.EqualTo("a"));
        Assert.That(date.GetDateTime(0), Is.EqualTo(new System.DateTime(2024, 1, 1)));
        Assert.That(category.GetCategory(0), Is.EqualTo("b"));
        Assert.That(category.CopyCategoryOrder(), Is.EqualTo(new string? [] { "b", "a" }));
        Assert.That(matrix.GetMatrixValue(0, 1), Is.EqualTo(4.0));
        Assert.That(matrix.GetMatrix().GetValue(0, 0), Is.EqualTo(3.0));

        ChartDataColumn copiedNumber = number.Copy();
        number.GetNumberValues()[0] = 50.0;
        Assert.That(copiedNumber.GetNumber(0), Is.EqualTo(1.0));

        ChartDataColumn mutable = new ChartDataColumn("mutable", ChartDataKind.Number);
        mutable.SetName("changed");
        mutable.SetKind(ChartDataKind.Text);
        mutable.SetLength(1);
        mutable.SetNumberValues(new double[] { 1.0 });
        mutable.SetTextValues(new string? [] { "text" });
        mutable.SetDateTimeValues(new System.DateTime[] { new System.DateTime(2024, 2, 1) });
        mutable.SetCategoryValues(new string? [] { "category" });
        mutable.SetCategoryOrder(new string? [] { "category" });
        mutable.SetMatrixValue(new ChartDataMatrix(1, 1));
        Assert.That(mutable.GetName(), Is.EqualTo("changed"));
        Assert.That(mutable.GetKind(), Is.EqualTo(ChartDataKind.Text));
        Assert.That(mutable.GetLength(), Is.EqualTo(1));
        Assert.That(mutable.GetNumberValues(), Is.EqualTo(new double[] { 1.0 }));
        Assert.That(mutable.GetTextValues(), Is.EqualTo(new string? [] { "text" }));
        Assert.That(mutable.GetDateTimeValues().Length, Is.EqualTo(1));
        Assert.That(mutable.GetCategoryValues(), Is.EqualTo(new string? [] { "category" }));
        Assert.That(mutable.GetCategoryOrder(), Is.EqualTo(new string? [] { "category" }));
    }

    [Test]
    public void DataMatrix_PublicViewsCopiesAccessorsAndBoundsBehaveAsDocumented()
    {
        ChartDataMatrix matrix = new ChartDataMatrixFactory().FromValues(2, 2, new double[] { 1.0, 2.0, 3.0, 4.0 });
        ChartDataMatrix copy = matrix.Copy();
        double[] valuesCopy = matrix.CopyValues();
        matrix.GetValueSpan()[0] = 10.0;
        matrix.GetRowSpan(1)[1] = 40.0;
        valuesCopy[1] = 20.0;

        Assert.That(matrix.GetRows(), Is.EqualTo(2));
        Assert.That(matrix.GetColumns(), Is.EqualTo(2));
        Assert.That(matrix.GetValues(), Is.EqualTo(new double[] { 10.0, 2.0, 3.0, 40.0 }));
        Assert.That(copy.CopyValues(), Is.EqualTo(new double[] { 1.0, 2.0, 3.0, 4.0 }));
        Assert.That(valuesCopy, Is.EqualTo(new double[] { 1.0, 20.0, 3.0, 4.0 }));

        ChartDataMatrix mutable = new ChartDataMatrix(1, 1);
        mutable.SetRows(2);
        mutable.SetColumns(2);
        mutable.SetValues(new double[] { 5.0, 6.0, 7.0, 8.0 });
        Assert.That(mutable.GetRows(), Is.EqualTo(2));
        Assert.That(mutable.GetColumns(), Is.EqualTo(2));
        Assert.That(mutable.GetValue(1, 1), Is.EqualTo(8.0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => matrix.GetValue(-1, 0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => matrix.SetValue(0, 2, 1.0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => matrix.GetRowSpan(2));
    }

    [Test]
    public void DataTable_PublicLookupCopySerializationAndRawAccessorsRemainConsistent()
    {
        ChartDataTable table = new ChartDataTable();
        table.AddNumberColumn("number", new double[] { 1.0 });
        table.AddTextColumn("text", new string? [] { "a,b" });
        table.AddDateTimeColumn("date", new System.DateTime[] { new System.DateTime(2024, 1, 2) });
        table.AddCategoryColumn("category", new string? [] { "x" });
        table.AddMatrixColumn("matrix", new ChartDataMatrixFactory().FromValues(1, 1, new double[] { 5.0 }));

        Assert.That(table.ContainsColumn("number"), Is.True);
        Assert.That(table.ContainsColumn("missing"), Is.False);
        Assert.That(table.ColumnIndex("date"), Is.EqualTo(2));
        Assert.That(table.ColumnIndex("missing"), Is.EqualTo(-1));
        Assert.That(table.FindColumn("missing"), Is.Null);
        Assert.That(table.GetColumnAt(1).GetName(), Is.EqualTo("text"));
        Assert.That(table.GetNumber("number", 0), Is.EqualTo(1.0));
        Assert.That(table.GetText("text", 0), Is.EqualTo("a,b"));
        Assert.That(table.GetDateTime("date", 0), Is.EqualTo(new System.DateTime(2024, 1, 2)));
        Assert.That(table.GetCategory("category", 0), Is.EqualTo("x"));
        Assert.That(table.GetMatrixValue("matrix", 0, 0), Is.EqualTo(5.0));
        Assert.That(table.ToCsv(), Does.Contain("\"a,b\""));
        Assert.That(table.ToTsv(), Does.Contain("a,b"));
        Assert.That(table.ToJsonTable(), Does.Contain("\"rows\":[[1,"));

        ChartDataColumn[] columnCopies = table.CopyColumns();
        ChartDataTable tableCopy = table.Copy();
        table.GetColumn("number").GetNumberValues()[0] = 99.0;
        Assert.That(columnCopies[0].GetNumber(0), Is.EqualTo(1.0));
        Assert.That(tableCopy.GetNumber("number", 0), Is.EqualTo(1.0));

        ChartDataTable mutable = new ChartDataTable();
        mutable.SetColumns(new ChartDataColumn[] { new ChartDataColumnFactory().CreateNumber("value", new double[] { 2.0 }) });
        mutable.SetColumnCount(1);
        mutable.SetRowCount(1);
        Assert.That(mutable.GetColumns().Length, Is.EqualTo(1));
        Assert.That(mutable.GetColumnCount(), Is.EqualTo(1));
        Assert.That(mutable.GetRowCount(), Is.EqualTo(1));
        Assert.That(mutable.GetNumber("value", 0), Is.EqualTo(2.0));
        Assert.Throws<System.ArgumentException>(() => table.GetColumn("missing"));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => table.GetColumnAt(5));
    }

    [Test]
    public void GroupHistogramAndStackResult_PublicMutationsExposeConfiguredState()
    {
        ChartDataGroup group = new ChartDataGroup("group");
        group.AddIndex(2);
        group.AddIndex(4);
        int[] indicesCopy = group.CopyIndices();
        indicesCopy[0] = 99;
        group.SetKey("changed");
        group.SetIndices(new int[] { 6, 8, 0, 0 });
        group.SetCount(2);
        Assert.That(group.GetKey(), Is.EqualTo("changed"));
        Assert.That(group.GetIndices(), Is.EqualTo(new int[] { 6, 8, 0, 0 }));
        Assert.That(group.GetCount(), Is.EqualTo(2));
        Assert.That(group.CopyIndices(), Is.EqualTo(new int[] { 6, 8 }));

        ChartHistogramBin first = new ChartHistogramBin(0.0, 1.0);
        first.SetCount(2);
        first.SetMinimum(-1.0);
        first.SetMaximum(2.0);
        Assert.That(first.GetMinimum(), Is.EqualTo(-1.0));
        Assert.That(first.GetMaximum(), Is.EqualTo(2.0));
        Assert.That(first.Width(), Is.EqualTo(3.0));
        Assert.That(first.GetCount(), Is.EqualTo(2));

        ChartHistogram histogram = new ChartHistogram(new ChartHistogramBin[] { first }, -1.0, 2.0, 3.0, 2, ChartHistogramBinStrategy.FixedWidth);
        ChartHistogramBin[] binCopies = histogram.CopyBins();
        histogram.SetBins(new ChartHistogramBin[] { new ChartHistogramBin(0.0, 2.0), new ChartHistogramBin(2.0, 4.0) });
        histogram.SetBinCount(2);
        histogram.SetMinimum(0.0);
        histogram.SetMaximum(4.0);
        histogram.SetBinWidth(2.0);
        histogram.SetValueCount(0);
        histogram.SetStrategy(ChartHistogramBinStrategy.FixedCount);
        Assert.That(binCopies[0].GetCount(), Is.EqualTo(2));
        Assert.That(histogram.GetBins().Length, Is.EqualTo(2));
        Assert.That(histogram.GetBinCount(), Is.EqualTo(2));
        Assert.That(histogram.GetMinimum(), Is.Zero);
        Assert.That(histogram.GetMaximum(), Is.EqualTo(4.0));
        Assert.That(histogram.GetBinWidth(), Is.EqualTo(2.0));
        Assert.That(histogram.GetValueCount(), Is.Zero);
        Assert.That(histogram.GetStrategy(), Is.EqualTo(ChartHistogramBinStrategy.FixedCount));

        ChartStackResult stack = new ChartStackResult(new ChartDataMatrix(1, 1), new ChartDataMatrix(1, 1), ChartStackMode.None);
        ChartDataMatrix starts = new ChartDataMatrixFactory().FromValues(1, 1, new double[] { 1.0 });
        ChartDataMatrix ends = new ChartDataMatrixFactory().FromValues(1, 1, new double[] { 2.0 });
        stack.SetStarts(starts);
        stack.SetEnds(ends);
        stack.SetMode(ChartStackMode.Zero);
        stack.SetRows(1);
        stack.SetColumns(1);
        Assert.That(stack.GetStarts(), Is.SameAs(starts));
        Assert.That(stack.GetEnds(), Is.SameAs(ends));
        Assert.That(stack.GetStart(0, 0), Is.EqualTo(1.0));
        Assert.That(stack.GetEnd(0, 0), Is.EqualTo(2.0));
        Assert.That(stack.GetMode(), Is.EqualTo(ChartStackMode.Zero));
        Assert.That(stack.GetRows(), Is.EqualTo(1));
        Assert.That(stack.GetColumns(), Is.EqualTo(1));
    }

    [Test]
    public void PivotTrendlineAndPareto_PublicCopiesAndMutationsBehavePredictably()
    {
        ChartPivotTable pivot = new ChartPivotTable(new string? [] { "r" }, new string? [] { "c" }, new ChartDataMatrixFactory().FromValues(1, 1, new double[] { 1.0 }));
        string? [] rowsCopy = pivot.CopyRowKeys();
        string? [] columnsCopy = pivot.CopyColumnKeys();
        pivot.SetRowKeys(new string? [] { "changed-r" });
        pivot.SetColumnKeys(new string? [] { "changed-c" });
        ChartDataMatrix pivotValues = new ChartDataMatrixFactory().FromValues(1, 1, new double[] { 2.0 });
        pivot.SetValues(pivotValues);
        Assert.That(rowsCopy, Is.EqualTo(new string? [] { "r" }));
        Assert.That(columnsCopy, Is.EqualTo(new string? [] { "c" }));
        Assert.That(pivot.GetRowKeys(), Is.EqualTo(new string? [] { "changed-r" }));
        Assert.That(pivot.GetColumnKeys(), Is.EqualTo(new string? [] { "changed-c" }));
        Assert.That(pivot.GetValues(), Is.SameAs(pivotValues));
        Assert.That(pivot.GetValue(0, 0), Is.EqualTo(2.0));

        ChartLinearTrendline linear = new ChartLinearTrendline(1.0, 2.0, 0.5);
        ChartLinearTrendline linearCopy = linear.Copy();
        linear.SetSlope(3.0);
        linear.SetIntercept(4.0);
        linear.SetRSquared(0.75);
        Assert.That(linear.GetSlope(), Is.EqualTo(3.0));
        Assert.That(linear.GetIntercept(), Is.EqualTo(4.0));
        Assert.That(linear.GetRSquared(), Is.EqualTo(0.75));
        Assert.That(linear.Predict(2.0), Is.EqualTo(10.0));
        Assert.That(linearCopy.Predict(2.0), Is.EqualTo(4.0));

        ChartPolynomialTrendline polynomial = new ChartPolynomialTrendline(new double[] { 1.0, 2.0 }, 0.9);
        ChartPolynomialTrendline polynomialCopy = polynomial.Copy();
        polynomial.GetCoefficients()[0] = 5.0;
        Assert.That(polynomial.GetCoefficientCount(), Is.EqualTo(2));
        Assert.That(polynomial.CoefficientAt(1), Is.EqualTo(2.0));
        Assert.That(polynomial.GetRSquared(), Is.EqualTo(0.9));
        Assert.That(polynomial.Predict(2.0), Is.EqualTo(5.0));
        Assert.That(polynomialCopy.Predict(2.0), Is.EqualTo(5.0));

        ChartParetoResult pareto = new ChartParetoResult(new string[] { "a" }, new double[] { 1.0 }, new double[] { 1.0 });
        ChartParetoResult paretoCopy = pareto.Copy();
        pareto.SetLabels(new string[] { "changed" });
        pareto.SetValues(new double[] { 2.0 });
        pareto.SetCumulativeFractions(new double[] { 0.5 });
        Assert.That(pareto.GetLabels(), Is.EqualTo(new string[] { "changed" }));
        Assert.That(pareto.GetValues(), Is.EqualTo(new double[] { 2.0 }));
        Assert.That(pareto.GetCumulativeFractions(), Is.EqualTo(new double[] { 0.5 }));
        Assert.That(paretoCopy.GetLabels(), Is.EqualTo(new string[] { "a" }));
        Assert.That(paretoCopy.GetValues(), Is.EqualTo(new double[] { 1.0 }));
    }
}
