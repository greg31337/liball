namespace currencycodes.Tests;

public class PublicExceptionTests
{
    [Test]
    public void CurrencyNotFoundExceptionPreservesCallerMessage()
    {
        const string Message = "Currency 'ZZZ' was not found.";

        Assert.That(
            () => throw new CurrencyNotFoundException(Message),
            Throws.TypeOf<CurrencyNotFoundException>().With.Message.EqualTo(Message));
    }

    [Test]
    public void InvalidCurrencyCodeExceptionPreservesCallerMessage()
    {
        const string Message = "Currency code 'US$' is malformed.";

        Assert.That(
            () => throw new InvalidCurrencyCodeException(Message),
            Throws.TypeOf<InvalidCurrencyCodeException>().With.Message.EqualTo(Message));
    }

    [Test]
    public void InvalidCurrencyAmountExceptionPreservesCallerMessage()
    {
        const string Message = "Amount '1.03' is not a valid CHF cash increment.";

        Assert.That(
            () => throw new InvalidCurrencyAmountException(Message),
            Throws.TypeOf<InvalidCurrencyAmountException>().With.Message.EqualTo(Message));
    }
}
