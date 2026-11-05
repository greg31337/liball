namespace barcode.Tests;

public class ResourceContextTests
{
    [Test]
    public void ResourceBudgetTracksIndependentInclusiveLimits()
    {
        ResourceBudget budget = new ResourceBudget(5, 7, 9);

        Assert.Multiple(() =>
        {
            Assert.That(budget.ConsumeWork(2), Is.EqualTo(2));
            Assert.That(budget.ConsumeWork(3), Is.EqualTo(5));
            Assert.That(budget.ReserveBytes(7), Is.EqualTo(7));
            Assert.That(budget.ReserveModules(4), Is.EqualTo(4));
            Assert.That(budget.ReserveModules(5), Is.EqualTo(9));
            Assert.That(budget.WorkUsed, Is.EqualTo(5));
            Assert.That(budget.BytesReserved, Is.EqualTo(7));
            Assert.That(budget.ModulesReserved, Is.EqualTo(9));
        });
    }

    [Test]
    public void ResourceBudgetRejectsNegativeAndCumulativeExcess()
    {
        ResourceBudget budget = new ResourceBudget(1, 1, 1);

        Assert.Multiple(() =>
        {
            Assert.That(() => budget.ConsumeWork(-1), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => budget.ReserveBytes(-1), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => budget.ReserveModules(-1), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => budget.ConsumeWork(2), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => budget.ReserveBytes(2), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => budget.ReserveModules(2), Throws.TypeOf<BarcodeValidationException>());
        });
    }

    [Test]
    public void ResourceBudgetRejectsInvalidConfiguredLimits()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => new ResourceBudget(-1, 1, 1), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => new ResourceBudget(1, -1, 1), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => new ResourceBudget(1, 1, -1), Throws.TypeOf<BarcodeValidationException>());
        });
    }

    [Test]
    public void EncodingContextResolvesFormatAndAccountsForPayloadStorage()
    {
        BarcodePayload payload = new BarcodePayload();
        payload.Text = "abc";
        payload.RawBytes = new byte[] { 1, 2, 3, 4 };
        BarcodeEncodeRequest request = new BarcodeEncodeRequest();
        request.Format = BarcodeFormat.Automatic;
        request.Payload = payload;

        EncodingContext context = new EncodingContext(request);

        Assert.Multiple(() =>
        {
            Assert.That(context.Request, Is.SameAs(request));
            Assert.That(context.Format, Is.EqualTo(BarcodeFormat.DataMatrix));
            Assert.That(context.Family, Is.EqualTo(BarcodeCodecFamily.DataMatrix));
            Assert.That(context.Resources.BytesReserved, Is.EqualTo(10));
            Assert.That(context.ConsumeWorkUnits(3), Is.EqualTo(3));
            Assert.That(context.ReservePayloadBytes(2), Is.EqualTo(12));
            Assert.That(context.ReserveModuleCount(5), Is.EqualTo(5));
        });
    }

    [Test]
    public void DecodingContextExpandsFormatsAndAccountsForInputStorage()
    {
        BarcodeImage image = new BarcodeImage();
        image.Width = 4;
        image.Height = 3;
        image.Pixels = new byte[12];
        BarcodeSymbol symbol = new BarcodeSymbol();
        symbol.Modules = new byte[5];
        BarcodeDecodeRequest request = new BarcodeDecodeRequest();
        request.Image = image;
        request.Symbol = symbol;

        DecodingContext context = new DecodingContext(request);

        Assert.Multiple(() =>
        {
            Assert.That(context.Request, Is.SameAs(request));
            Assert.That(context.Formats.Length, Is.EqualTo(17));
            Assert.That(context.Resources.BytesReserved, Is.EqualTo(12));
            Assert.That(context.Resources.ModulesReserved, Is.EqualTo(17));
            Assert.That(context.IsFormatAllowed(BarcodeFormat.Code128), Is.True);
            Assert.That(context.IsFormatAllowed(BarcodeFormat.Automatic), Is.False);
            Assert.That(context.ConsumeWorkUnits(2), Is.EqualTo(2));
            Assert.That(context.ReserveInputBytes(3), Is.EqualTo(15));
            Assert.That(context.ReserveModuleCount(4), Is.EqualTo(21));
        });
    }

    [Test]
    public void DecodingContextRejectsMissingRequestAndExcessiveImageArea()
    {
        BarcodeImage image = new BarcodeImage();
        image.Width = DecodingContext.DefaultMaximumModules;
        image.Height = 2;
        BarcodeDecodeRequest request = new BarcodeDecodeRequest();
        request.Image = image;

        Assert.Multiple(() =>
        {
            Assert.That(() => new DecodingContext(null!), Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(() => new DecodingContext(request), Throws.TypeOf<BarcodeValidationException>());
        });
    }

    [Test]
    public void ExceptionlessDispatcherRejectsInvalidRequestsWithoutThrowing()
    {
        BarcodeDispatcher dispatcher = new BarcodeDispatcher();
        BarcodeEncodeRequest encode = new BarcodeEncodeRequest();
        encode.Format = (BarcodeFormat)999;
        BarcodeDecodeOptions options = new BarcodeDecodeOptions();
        options.AllowedFormats = new BarcodeFormat[] { (BarcodeFormat)999 };
        BarcodeDecodeRequest decode = new BarcodeDecodeRequest();
        decode.Options = options;

        BarcodeDecodeAttempt attempt = dispatcher.TryDecodeBarcode(decode);

        Assert.Multiple(() =>
        {
            Assert.That(dispatcher.TryEncodeBarcode(encode), Is.Null);
            Assert.That(attempt.Success, Is.False);
            Assert.That(attempt.FailureCode, Is.EqualTo("INVALID_REQUEST"));
            Assert.That(attempt.Results, Is.Empty);
            Assert.That(attempt.Diagnostics, Is.Empty);
        });
    }
}
