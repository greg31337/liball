using barcode;

var calculator = new BarcodeChecksumCalculator();

// The input digits exclude the check digit, which the calculator appends.
var samples = new[]
{
    (Format: BarcodeFormat.Ean13, Digits: "400638133393"),
    (Format: BarcodeFormat.Ean8, Digits: "5512345"),
    (Format: BarcodeFormat.UpcA, Digits: "03600029145")
};

Console.WriteLine("Retail barcode check digits");
foreach (var sample in samples)
{
    var result = calculator.CalculateBarcodeChecksum(new BarcodeChecksumRequest
    {
        Format = sample.Format,
        Text = sample.Digits
    });

    var valid = calculator.VerifyBarcodeChecksum(new BarcodeChecksumRequest
    {
        Format = sample.Format,
        Text = sample.Digits,
        HasObservedValue = true,
        ObservedValue = result.CalculatedValue
    });

    // Change only the observed check digit to demonstrate validation failure.
    var damaged = calculator.VerifyBarcodeChecksum(new BarcodeChecksumRequest
    {
        Format = sample.Format,
        Text = sample.Digits,
        HasObservedValue = true,
        ObservedValue = (result.CalculatedValue + 1) % 10
    });

    Console.WriteLine(
        $"{sample.Format,-6} {sample.Digits}{result.CalculatedValue} | " +
        $"valid={valid.IsValid}, changed digit valid={damaged.IsValid}");
    if (!valid.IsValid || damaged.IsValid)
    {
        throw new InvalidOperationException("Check digit validation failed.");
    }
}
