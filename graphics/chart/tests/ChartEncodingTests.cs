namespace chart.Tests;

[TestFixture]
public class ChartEncodingTests
{
    [Test]
    public void VisualEncoding_BindsAllRequestedFieldChannels()
    {
        ChartVisualEncoding encoding = CreateFullEncoding();

        Assert.That(encoding.BindingCount, Is.EqualTo(14));
        Assert.That(encoding.HasChannel(ChartEncodingChannel.X), Is.True);
        Assert.That(encoding.HasChannel(ChartEncodingChannel.Column), Is.True);
        AssertBinding(encoding.BindingFor(ChartEncodingChannel.X), "x", ChartDataKind.Number);
        AssertBinding(encoding.BindingFor(ChartEncodingChannel.Y), "y", ChartDataKind.Number);
        AssertBinding(encoding.BindingFor(ChartEncodingChannel.X2), "x2", ChartDataKind.Number);
        AssertBinding(encoding.BindingFor(ChartEncodingChannel.Y2), "y2", ChartDataKind.Number);
        AssertBinding(encoding.BindingFor(ChartEncodingChannel.Color), "color", ChartDataKind.Category);
        AssertBinding(encoding.BindingFor(ChartEncodingChannel.Size), "size", ChartDataKind.Number);
        AssertBinding(encoding.BindingFor(ChartEncodingChannel.Shape), "shape", ChartDataKind.Category);
        AssertBinding(encoding.BindingFor(ChartEncodingChannel.Opacity), "opacity", ChartDataKind.Number);
        AssertBinding(encoding.BindingFor(ChartEncodingChannel.Text), "text", ChartDataKind.Text);
        AssertBinding(encoding.BindingFor(ChartEncodingChannel.Tooltip), "tooltip", ChartDataKind.Text);
        AssertBinding(encoding.BindingFor(ChartEncodingChannel.Order), "order", ChartDataKind.Number);
        AssertBinding(encoding.BindingFor(ChartEncodingChannel.Detail), "detail", ChartDataKind.Category);
        AssertBinding(encoding.BindingFor(ChartEncodingChannel.Row), "row", ChartDataKind.Category);
        AssertBinding(encoding.BindingFor(ChartEncodingChannel.Column), "column", ChartDataKind.Category);
    }

    [Test]
    public void EncodingBinding_ChannelNamesAreStable()
    {
        Assert.That(ChartEncodingBinding.ChannelName(ChartEncodingChannel.X), Is.EqualTo("x"));
        Assert.That(ChartEncodingBinding.ChannelName(ChartEncodingChannel.Y), Is.EqualTo("y"));
        Assert.That(ChartEncodingBinding.ChannelName(ChartEncodingChannel.X2), Is.EqualTo("x2"));
        Assert.That(ChartEncodingBinding.ChannelName(ChartEncodingChannel.Y2), Is.EqualTo("y2"));
        Assert.That(ChartEncodingBinding.ChannelName(ChartEncodingChannel.Color), Is.EqualTo("color"));
        Assert.That(ChartEncodingBinding.ChannelName(ChartEncodingChannel.Size), Is.EqualTo("size"));
        Assert.That(ChartEncodingBinding.ChannelName(ChartEncodingChannel.Shape), Is.EqualTo("shape"));
        Assert.That(ChartEncodingBinding.ChannelName(ChartEncodingChannel.Opacity), Is.EqualTo("opacity"));
        Assert.That(ChartEncodingBinding.ChannelName(ChartEncodingChannel.Text), Is.EqualTo("text"));
        Assert.That(ChartEncodingBinding.ChannelName(ChartEncodingChannel.Tooltip), Is.EqualTo("tooltip"));
        Assert.That(ChartEncodingBinding.ChannelName(ChartEncodingChannel.Order), Is.EqualTo("order"));
        Assert.That(ChartEncodingBinding.ChannelName(ChartEncodingChannel.Detail), Is.EqualTo("detail"));
        Assert.That(ChartEncodingBinding.ChannelName(ChartEncodingChannel.Row), Is.EqualTo("row"));
        Assert.That(ChartEncodingBinding.ChannelName(ChartEncodingChannel.Column), Is.EqualTo("column"));
    }

    [Test]
    public void VisualEncoding_StoresConstantsAndCopiesIndependently()
    {
        ChartVisualEncoding encoding = new ChartVisualEncoding();
        ChartColor color = ChartColor.FromRgb(10, 20, 30);

        encoding.SetConstantNumber(ChartEncodingChannel.Size, 8.0);
        encoding.SetConstantNumber(ChartEncodingChannel.Opacity, 0.5);
        encoding.SetConstantText(ChartEncodingChannel.Text, "fixed");
        encoding.SetConstantColor(ChartEncodingChannel.Color, color);
        encoding.SetConstantShape(ChartEncodingChannel.Shape, ChartMarkerShape.Diamond);
        color.R = 99;

        ChartVisualEncoding copy = encoding.Copy();
        encoding.BindingFor(ChartEncodingChannel.Color).ColorValue.G = 99;
        ChartEncodingBinding[] bindings = copy.CopyBindings();
        bindings[0].NumberValue = 99.0;

        Assert.That(copy.BindingFor(ChartEncodingChannel.Size).NumberValue, Is.EqualTo(8.0));
        Assert.That(copy.BindingFor(ChartEncodingChannel.Opacity).NumberValue, Is.EqualTo(0.5));
        Assert.That(copy.BindingFor(ChartEncodingChannel.Text).TextValue, Is.EqualTo("fixed"));
        AssertColor(copy.BindingFor(ChartEncodingChannel.Color).ColorValue, 10, 20, 30, 255);
        Assert.That(copy.BindingFor(ChartEncodingChannel.Shape).ShapeValue, Is.EqualTo(ChartMarkerShape.Diamond));
    }

    [Test]
    public void VisualEncoding_ReplacesExistingChannelBinding()
    {
        ChartVisualEncoding encoding = new ChartVisualEncoding();

        encoding.SetX("x", ChartDataKind.Number);
        encoding.SetX("x_new", ChartDataKind.Number);

        Assert.That(encoding.BindingCount, Is.EqualTo(1));
        Assert.That(encoding.BindingFor(ChartEncodingChannel.X).FieldName, Is.EqualTo("x_new"));
    }

    [Test]
    public void VisualEncoding_ValidatesBoundFieldsAgainstTable()
    {
        ChartVisualEncoding encoding = CreateFullEncoding();
        ChartDataTable table = CreateEncodingTable();

        encoding.ValidateAgainstTable(table);

        ChartVisualEncoding missing = new ChartVisualEncoding();
        missing.SetX("missing", ChartDataKind.Number);
        ChartVisualEncoding wrongKind = new ChartVisualEncoding();
        wrongKind.SetX("x", ChartDataKind.Text);

        Assert.Throws<System.ArgumentException>(() => missing.ValidateAgainstTable(table));
        Assert.Throws<System.ArgumentException>(() => wrongKind.ValidateAgainstTable(table));
    }

    [Test]
    public void VisualEncoding_ValidatesInputs()
    {
        ChartVisualEncoding encoding = new ChartVisualEncoding();

        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartEncodingBinding.ValidateChannel((ChartEncodingChannel)99));
        Assert.Throws<System.ArgumentNullException>(() => new ChartEncodingBinding(ChartEncodingChannel.X, null!, ChartDataKind.Number));
        Assert.Throws<System.ArgumentException>(() => new ChartEncodingBinding(ChartEncodingChannel.X, "", ChartDataKind.Number));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartEncodingBinding.CreateNumberConstant(ChartEncodingChannel.Size, double.NaN));
        Assert.Throws<System.ArgumentNullException>(() => ChartEncodingBinding.CreateTextConstant(ChartEncodingChannel.Text, null!));
        Assert.Throws<System.ArgumentNullException>(() => ChartEncodingBinding.CreateColorConstant(ChartEncodingChannel.Color, null!));
        Assert.Throws<System.ArgumentNullException>(() => encoding.SetBinding(null!));
        Assert.Throws<System.ArgumentException>(() => encoding.BindingFor(ChartEncodingChannel.X));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => encoding.BindingAt(0));
        Assert.Throws<System.ArgumentNullException>(() => encoding.ValidateAgainstTable(null!));
    }

    static ChartVisualEncoding CreateFullEncoding()
    {
        ChartVisualEncoding encoding = new ChartVisualEncoding();

        encoding.SetX("x", ChartDataKind.Number);
        encoding.SetY("y", ChartDataKind.Number);
        encoding.SetX2("x2", ChartDataKind.Number);
        encoding.SetY2("y2", ChartDataKind.Number);
        encoding.SetColor("color", ChartDataKind.Category);
        encoding.SetSize("size", ChartDataKind.Number);
        encoding.SetShape("shape", ChartDataKind.Category);
        encoding.SetOpacity("opacity", ChartDataKind.Number);
        encoding.SetText("text", ChartDataKind.Text);
        encoding.SetTooltip("tooltip", ChartDataKind.Text);
        encoding.SetOrder("order", ChartDataKind.Number);
        encoding.SetDetail("detail", ChartDataKind.Category);
        encoding.SetRow("row", ChartDataKind.Category);
        encoding.SetColumn("column", ChartDataKind.Category);
        return encoding;
    }

    static ChartDataTable CreateEncodingTable()
    {
        ChartDataTable table = new ChartDataTable();

        table.AddNumberColumn("x", new double[] { 1.0, 2.0 });
        table.AddNumberColumn("y", new double[] { 3.0, 4.0 });
        table.AddNumberColumn("x2", new double[] { 5.0, 6.0 });
        table.AddNumberColumn("y2", new double[] { 7.0, 8.0 });
        table.AddCategoryColumn("color", new string[] { "A", "B" });
        table.AddNumberColumn("size", new double[] { 9.0, 10.0 });
        table.AddCategoryColumn("shape", new string[] { "circle", "square" });
        table.AddNumberColumn("opacity", new double[] { 0.5, 0.8 });
        table.AddTextColumn("text", new string[] { "one", "two" });
        table.AddTextColumn("tooltip", new string[] { "tip1", "tip2" });
        table.AddNumberColumn("order", new double[] { 1.0, 2.0 });
        table.AddCategoryColumn("detail", new string[] { "d1", "d2" });
        table.AddCategoryColumn("row", new string[] { "r1", "r2" });
        table.AddCategoryColumn("column", new string[] { "c1", "c2" });
        return table;
    }

    static void AssertBinding(ChartEncodingBinding binding, string fieldName, ChartDataKind fieldKind)
    {
        Assert.That(binding.IsConstant, Is.False);
        Assert.That(binding.FieldName, Is.EqualTo(fieldName));
        Assert.That(binding.FieldKind, Is.EqualTo(fieldKind));
    }

    static void AssertColor(ChartColor color, int r, int g, int b, int a)
    {
        Assert.That(color.R, Is.EqualTo(r));
        Assert.That(color.G, Is.EqualTo(g));
        Assert.That(color.B, Is.EqualTo(b));
        Assert.That(color.A, Is.EqualTo(a));
    }
}
