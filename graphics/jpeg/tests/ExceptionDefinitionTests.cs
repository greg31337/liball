namespace jpeg.Tests;

internal class ExceptionDefinitionTests
{
    [Test]
    public void JpegExceptions_DeriveFromSystemException()
    {
        Assert.That(typeof(System.Exception).IsAssignableFrom(typeof(global::jpeg.JpegInvalidDataException)), Is.True);
        Assert.That(typeof(System.Exception).IsAssignableFrom(typeof(global::jpeg.JpegUnsupportedException)), Is.True);
        Assert.That(typeof(System.Exception).IsAssignableFrom(typeof(global::jpeg.JpegResourceLimitException)), Is.True);
        Assert.That(typeof(System.Exception).IsAssignableFrom(typeof(global::jpeg.JpegWriteValidationException)), Is.True);
        Assert.That(typeof(System.Exception).IsAssignableFrom(typeof(global::jpeg.JpegIOException)), Is.True);
    }

    [Test]
    public void JpegExceptions_PreserveMessages()
    {
        Assert.That(global::jpeg.JpegInvalidDataException.Create("bad marker").Message, Is.EqualTo("bad marker"));
        Assert.That(global::jpeg.JpegUnsupportedException.Create("arithmetic coding").Message, Is.EqualTo("arithmetic coding"));
        Assert.That(global::jpeg.JpegResourceLimitException.Create("too large").Message, Is.EqualTo("too large"));
        Assert.That(global::jpeg.JpegWriteValidationException.Create("bad quality").Message, Is.EqualTo("bad quality"));
        Assert.That(new global::jpeg.JpegIOException("read failed", null).Message, Is.EqualTo("read failed"));
    }

    [Test]
    public void JpegExceptions_PreserveInnerExceptions()
    {
        System.Exception inner = new System.Exception("inner");

        Assert.That(new global::jpeg.JpegInvalidDataException("outer", inner).InnerException, Is.SameAs(inner));
        Assert.That(new global::jpeg.JpegUnsupportedException("outer", inner).InnerException, Is.SameAs(inner));
        Assert.That(new global::jpeg.JpegResourceLimitException("outer", inner).InnerException, Is.SameAs(inner));
        Assert.That(new global::jpeg.JpegWriteValidationException("outer", inner).InnerException, Is.SameAs(inner));
        Assert.That(new global::jpeg.JpegIOException("outer", inner).InnerException, Is.SameAs(inner));
    }

    [Test]
    public void JpegExceptions_CanBeThrownAsTheirConcreteTypes()
    {
        Assert.That(
            Assert.Throws<global::jpeg.JpegInvalidDataException>(
                delegate
                {
                    throw global::jpeg.JpegInvalidDataException.Create("invalid");
                })!.Message,
            Is.EqualTo("invalid"));

        Assert.That(
            Assert.Throws<global::jpeg.JpegUnsupportedException>(
                delegate
                {
                    throw global::jpeg.JpegUnsupportedException.Create("unsupported");
                })!.Message,
            Is.EqualTo("unsupported"));

        Assert.That(
            Assert.Throws<global::jpeg.JpegResourceLimitException>(
                delegate
                {
                    throw global::jpeg.JpegResourceLimitException.Create("limit");
                })!.Message,
            Is.EqualTo("limit"));

        Assert.That(
            Assert.Throws<global::jpeg.JpegWriteValidationException>(
                delegate
                {
                    throw global::jpeg.JpegWriteValidationException.Create("write");
                })!.Message,
            Is.EqualTo("write"));

        Assert.That(
            Assert.Throws<global::jpeg.JpegIOException>(
                delegate
                {
                    throw new global::jpeg.JpegIOException("io", null);
                })!.Message,
            Is.EqualTo("io"));
    }
}
