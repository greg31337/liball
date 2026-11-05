using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class DataSizeDataRateTests
{
    [Test]
    public void DataSizeFactories_ConstructCommonInformationUnits()
    {
        DataSize bits = DataSize.FromBits(8.0d);
        DataSize bytes = DataSize.FromBytes(1024.0d);
        DataSize kilobytes = DataSize.FromKilobytes(1.0d);
        DataSize kibibytes = DataSize.FromKibibytes(1.0d);
        DataSize megabytes = DataSize.FromMegabytes(1.0d);
        DataSize mebibytes = DataSize.FromMebibytes(1.0d);

        Assert.That(bits.GetUnit().GetId(), Is.EqualTo("bit"));
        Assert.That(bits.Bytes(), Is.EqualTo(1.0d));
        Assert.That(bytes.Bits(), Is.EqualTo(8192.0d));
        Assert.That(kilobytes.Bytes(), Is.EqualTo(1000.0d));
        Assert.That(kibibytes.Bytes(), Is.EqualTo(1024.0d));
        Assert.That(megabytes.Kilobytes(), Is.EqualTo(1000.0d));
        Assert.That(mebibytes.Kibibytes(), Is.EqualTo(1024.0d));
    }

    [Test]
    public void DataSizeToUnitArithmeticAndBridge_ConvertAcrossInformationUnits()
    {
        DataSize bytes = DataSize.FromBytes(500.0d);
        DataSize kilobyte = DataSize.FromKilobytes(1.0d);
        DataSize negativeMebibytes = DataSize.FromMebibytes(-2.0d);

        DataSize converted = DataSize.FromKibibytes(1.0d).ToUnit(new StandardUnits().Byte());
        DataSize sum = bytes.Add(kilobyte);
        DataSize difference = DataSize.FromKibibytes(2.0d).Subtract(DataSize.FromBytes(1024.0d));
        DataSize scaled = negativeMebibytes.Scale(2.0d);
        DataSize negated = negativeMebibytes.Negate();
        DataSize absolute = negativeMebibytes.AbsoluteValue();
        QuantityValue quantity = negativeMebibytes.ToQuantityValue();

        Assert.That(converted.GetUnit().GetId(), Is.EqualTo("byte"));
        Assert.That(converted.GetValue(), Is.EqualTo(1024.0d));
        Assert.That(sum.GetUnit().GetId(), Is.EqualTo("byte"));
        Assert.That(sum.GetValue(), Is.EqualTo(1500.0d));
        Assert.That(difference.GetUnit().GetId(), Is.EqualTo("kibibyte"));
        Assert.That(difference.GetValue(), Is.EqualTo(1.0d));
        Assert.That(scaled.GetValue(), Is.EqualTo(-4.0d));
        Assert.That(negated.GetValue(), Is.EqualTo(2.0d));
        Assert.That(absolute.GetValue(), Is.EqualTo(2.0d));
        Assert.That(negativeMebibytes.Abs().GetValue(), Is.EqualTo(2.0d));
        Assert.That(quantity.GetValue(), Is.EqualTo(-2.0d));
        Assert.That(quantity.GetUnit().GetId(), Is.EqualTo("mebibyte"));
    }

    [Test]
    public void DataSize_RejectsMissingOrNonDataSizeInputs()
    {
        DataSize dataSize = DataSize.FromBytes(1.0d);

        Assert.Throws<System.ArgumentNullException>(() => new DataSize(1.0d, null!));
        Assert.Throws<System.ArgumentException>(() => new DataSize(1.0d, new StandardUnits().Second()));
        Assert.Throws<System.ArgumentException>(() => new DataSize(1.0d, new StandardUnits().Bit().Divide(new StandardUnits().Second())));
        Assert.Throws<System.ArgumentNullException>(() => dataSize.Add(null!));
        Assert.Throws<System.ArgumentNullException>(() => dataSize.Subtract(null!));
    }

    [Test]
    public void DataRateFactories_ConstructCommonPerSecondUnits()
    {
        DataRate bitsPerSecond = DataRate.FromBitsPerSecond(8000.0d);
        DataRate bytesPerSecond = DataRate.FromBytesPerSecond(1000.0d);
        DataRate kilobytesPerSecond = DataRate.FromKilobytesPerSecond(1.0d);
        DataRate kibibytesPerSecond = DataRate.FromKibibytesPerSecond(1.0d);
        DataRate megabytesPerSecond = DataRate.FromMegabytesPerSecond(1.0d);
        DataRate mebibytesPerSecond = DataRate.FromMebibytesPerSecond(1.0d);

        Assert.That(bitsPerSecond.GetUnit().GetId(), Is.EqualTo("(bit/second)"));
        Assert.That(bitsPerSecond.BytesPerSecond(), Is.EqualTo(1000.0d));
        Assert.That(bytesPerSecond.BitsPerSecond(), Is.EqualTo(8000.0d));
        Assert.That(kilobytesPerSecond.BytesPerSecond(), Is.EqualTo(1000.0d));
        Assert.That(kibibytesPerSecond.BytesPerSecond(), Is.EqualTo(1024.0d));
        Assert.That(megabytesPerSecond.KilobytesPerSecond(), Is.EqualTo(1000.0d));
        Assert.That(mebibytesPerSecond.KibibytesPerSecond(), Is.EqualTo(1024.0d));
    }

    [Test]
    public void DataRateToUnitArithmeticAndBridge_ConvertAcrossPerSecondUnits()
    {
        DataRate bytesPerSecond = DataRate.FromBytesPerSecond(500.0d);
        DataRate kilobytesPerSecond = DataRate.FromKilobytesPerSecond(1.0d);
        DataRate negativeMebibytesPerSecond = DataRate.FromMebibytesPerSecond(-2.0d);

        DataRate converted = DataRate.FromKibibytesPerSecond(1.0d).ToUnit(new StandardUnits().Byte().Divide(new StandardUnits().Second()));
        DataRate sum = bytesPerSecond.Add(kilobytesPerSecond);
        DataRate difference = DataRate.FromMegabytesPerSecond(2.0d).Subtract(DataRate.FromKilobytesPerSecond(500.0d));
        DataRate scaled = negativeMebibytesPerSecond.Scale(2.0d);
        DataRate negated = negativeMebibytesPerSecond.Negate();
        DataRate absolute = negativeMebibytesPerSecond.AbsoluteValue();
        QuantityValue quantity = negativeMebibytesPerSecond.ToQuantityValue();

        Assert.That(converted.GetUnit().GetId(), Is.EqualTo("(byte/second)"));
        Assert.That(converted.GetValue(), Is.EqualTo(1024.0d));
        Assert.That(sum.GetUnit().GetId(), Is.EqualTo("(byte/second)"));
        Assert.That(sum.GetValue(), Is.EqualTo(1500.0d));
        Assert.That(difference.GetUnit().GetId(), Is.EqualTo("(megabyte/second)"));
        Assert.That(difference.GetValue(), Is.EqualTo(1.5d));
        Assert.That(scaled.GetValue(), Is.EqualTo(-4.0d));
        Assert.That(negated.GetValue(), Is.EqualTo(2.0d));
        Assert.That(absolute.GetValue(), Is.EqualTo(2.0d));
        Assert.That(negativeMebibytesPerSecond.Abs().GetValue(), Is.EqualTo(2.0d));
        Assert.That(quantity.GetValue(), Is.EqualTo(-2.0d));
        Assert.That(quantity.GetUnit().GetId(), Is.EqualTo("(mebibyte/second)"));
    }

    [Test]
    public void DataRate_RejectsMissingOrNonDataRateInputs()
    {
        DataRate dataRate = DataRate.FromBitsPerSecond(1.0d);

        Assert.Throws<System.ArgumentNullException>(() => new DataRate(1.0d, null!));
        Assert.Throws<System.ArgumentException>(() => new DataRate(1.0d, new StandardUnits().Byte()));
        Assert.Throws<System.ArgumentException>(() => new DataRate(1.0d, new StandardUnits().Meter().Divide(new StandardUnits().Second())));
        Assert.Throws<System.ArgumentNullException>(() => dataRate.Add(null!));
        Assert.Throws<System.ArgumentNullException>(() => dataRate.Subtract(null!));
    }
}
