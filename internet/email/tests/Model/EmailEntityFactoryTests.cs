using System.Text;
using email;

namespace email.Tests;

public class EmailEntityFactoryTests
{
    [Test]
    public void DefaultMediaTypeIsTextPlainUsAsciiOutsideMultipartDigest()
    {
        EmailMediaType direct = new EmailMimeDefaults().CreateDefaultMediaTypeWith();
        EmailMediaType mixedChild = new EmailMimeDefaults().CreateDefaultMediaTypeWithParentMediaType(Media("multipart", "mixed"));

        AssertDefaultText(direct);
        AssertDefaultText(mixedChild);
        Assert.That(new EmailMimeDefaults().GetDefaultEntityKind(null!), Is.EqualTo(MimeEntityKind.Leaf));
        Assert.That(new EmailMimeDefaults().GetDefaultEntityKind(Media("multipart", "mixed")), Is.EqualTo(MimeEntityKind.Leaf));
    }

    [Test]
    public void MultipartDigestChildrenDefaultToMessageRfc822()
    {
        EmailMediaType digest = Media("multipart", "digest");
        EmailMediaType child = new EmailMimeDefaults().CreateDefaultMediaTypeWithParentMediaType(digest);

        Assert.That(new EmailMimeDefaults().UsesDigestChildDefault(digest), Is.True);
        Assert.That(child.IsType("message", "rfc822"), Is.True);
        Assert.That(child.WasDefaulted(), Is.True);
        Assert.That(child.GetParameters().GetCount(), Is.Zero);
        Assert.That(new EmailMimeDefaults().GetDefaultEntityKind(digest), Is.EqualTo(MimeEntityKind.Message));
    }

    [Test]
    public void MissingTransferEncodingHasAnEffectiveSevenBitDefault()
    {
        MimeEntity entity = new EmailEntityFactory().CreateDefaultLeaf(Content("body"));

        Assert.That(entity.HasTransferEncoding(), Is.False);
        Assert.That(entity.WasTransferEncodingDefaulted(), Is.True);
        Assert.That(entity.GetEffectiveTransferEncodingName(), Is.EqualTo("7bit"));
        Assert.That(entity.GetEffectiveTransferEncodingKind(), Is.EqualTo(EmailTransferEncodingKind.SevenBit));
        Assert.That(new EmailMimeDefaults().GetDefaultTransferEncodingName(), Is.EqualTo("7bit"));
        Assert.That(new EmailMimeDefaults().GetDefaultTransferEncodingKind(), Is.EqualTo(EmailTransferEncodingKind.SevenBit));
    }

    [Test]
    public void TextCharsetDefaultsToUsAsciiOnlyWhenNoCharsetIsExplicit()
    {
        EmailMediaType missing = Media("text", "html");
        MimeEntity explicitEntity = new EmailEntityFactory().CreateTextWithValueAndSubtypeAndCharset("hello", "html", "utf-8");

        Assert.That(new EmailMimeDefaults().GetEffectiveTextCharset(missing), Is.EqualTo("us-ascii"));
        Assert.That(new EmailMimeDefaults().GetEffectiveTextCharset(explicitEntity.GetMediaType()), Is.EqualTo("utf-8"));
        Assert.Throws<InvalidOperationException>(() => new EmailMimeDefaults().GetEffectiveTextCharset(Media("application", "json")));
        Assert.Throws<ArgumentNullException>(() => new EmailMimeDefaults().GetEffectiveTextCharset(null));
    }

    [Test]
    public void DefaultLeafUsesSemanticDefaultsWithoutAddingHeaders()
    {
        EmailContent content = Content("hello");
        MimeEntity entity = new EmailEntityFactory().CreateDefaultLeaf(content);

        Assert.That(entity.GetKind(), Is.EqualTo(MimeEntityKind.Leaf));
        AssertDefaultText(entity.GetMediaType()!);
        Assert.That(entity.GetHeaders()!.GetCount(), Is.Zero);
        Assert.That(entity.HasRawContent(), Is.False);
        Assert.That(entity.GetDecodedContent(), Is.SameAs(content));
    }

    [Test]
    public void TextFactoryChoosesAsciiOrUtf8WithoutSyntheticFields()
    {
        MimeEntity ascii = new EmailEntityFactory().CreateTextWithValue("hello");
        MimeEntity unicode = new EmailEntityFactory().CreateTextWithValue("café ✓");

        Assert.That(Charset(ascii), Is.EqualTo("us-ascii"));
        Assert.That(ascii.GetTransferEncodingKind(), Is.EqualTo(EmailTransferEncodingKind.SevenBit));
        Assert.That(Encoding.ASCII.GetString(ascii.GetContent()!.Materialize(100)), Is.EqualTo("hello"));
        Assert.That(Charset(unicode), Is.EqualTo("utf-8"));
        Assert.That(unicode.GetTransferEncodingKind(), Is.EqualTo(EmailTransferEncodingKind.EightBit));
        Assert.That(Encoding.UTF8.GetString(unicode.GetContent()!.Materialize(100)), Is.EqualTo("café ✓"));
        Assert.That(ascii.GetHeaders()!.GetCount(), Is.Zero);
        Assert.That(unicode.GetHeaders()!.GetCount(), Is.Zero);
    }

    [Test]
    public void TextFactorySupportsExplicitSubtypeAndRegisteredCharset()
    {
        MimeEntity entity = new EmailEntityFactory().CreateTextWithValueAndSubtypeAndCharset("café", "html", "latin1");

        Assert.That(entity.GetMediaType()!.IsType("text", "html"), Is.True);
        Assert.That(Charset(entity), Is.EqualTo("iso-8859-1"));
        Assert.That(entity.GetContent()!.Materialize(10), Is.EqualTo(new byte[] { 99, 97, 102, 233 }));
        Assert.That(entity.GetTransferEncodingKind(), Is.EqualTo(EmailTransferEncodingKind.EightBit));
        Assert.That(entity.GetHeaders()!.GetCount(), Is.Zero);
    }

    [Test]
    public void BinaryFactoryCopiesInputAndUsesSafeSemanticMetadataOnly()
    {
        byte[] input = new byte[] { 0, 1, 2, 255 };
        MimeEntity entity = new EmailEntityFactory().CreateBinaryWithValueAndTypeAndSubtype(input, "image", "png");
        input[0] = 99;

        Assert.That(entity.GetMediaType()!.IsType("image", "png"), Is.True);
        Assert.That(entity.GetTransferEncodingKind(), Is.EqualTo(EmailTransferEncodingKind.Base64));
        Assert.That(entity.GetContent()!.Materialize(10), Is.EqualTo(new byte[] { 0, 1, 2, 255 }));
        Assert.That(entity.GetHeaders()!.GetCount(), Is.Zero);
    }

    [Test]
    public void BinaryContentFactoryRetainsCallerContentAndRejectsClosedContent()
    {
        EmailContent content = Content("binary");
        MimeEntity entity = new EmailEntityFactory().CreateBinaryWithContentAndMediaType(content, Media("application", "pdf"));
        EmailContent closed = Content("closed");
        closed.Close();

        Assert.That(entity.GetContent(), Is.SameAs(content));
        Assert.That(entity.GetMediaType()!.IsType("application", "pdf"), Is.True);
        Assert.Throws<ObjectDisposedException>(() => new EmailEntityFactory().CreateBinaryWithContentAndMediaType(closed, Media("application", "pdf")));
        Assert.Throws<ObjectDisposedException>(() => new EmailEntityFactory().CreateDefaultLeaf(closed));
    }

    [Test]
    public void MultipartFactoryPreservesChildrenButDoesNotInventBoundaryOrHeaders()
    {
        MimeEntity first = new EmailEntityFactory().CreateTextWithValue("plain");
        MimeEntity second = new EmailEntityFactory().CreateTextWithValueAndSubtypeAndCharset("<p>html</p>", "html", "us-ascii");
        MimeEntity multipart = new EmailEntityFactory().CreateMultipartWithSubtypeAndChildren("alternative", new List<MimeEntity> { first, second });

        Assert.That(multipart.GetKind(), Is.EqualTo(MimeEntityKind.Multipart));
        Assert.That(multipart.GetMediaType()!.IsType("multipart", "alternative"), Is.True);
        Assert.That(multipart.GetMediaType()!.GetParameters().GetCount(), Is.Zero);
        Assert.That(multipart.GetHeaders()!.GetCount(), Is.Zero);
        Assert.That(multipart.GetChildren(), Is.EqualTo(new[] { first, second }));
        Assert.That(first.GetParent(), Is.SameAs(multipart));
        Assert.That(second.GetParent(), Is.SameAs(multipart));
    }

    [Test]
    public void NestedMessageFactoriesDistinguishExplicitAndDigestDefaultMediaTypes()
    {
        EmailMessage message = new EmailMessageOperations().CreateWith();
        MimeEntity explicitEntity = new EmailEntityFactory().CreateNestedMessage(message);
        MimeEntity digestEntity = new EmailEntityFactory().CreateDigestChild(new EmailMessageOperations().CreateWith());

        Assert.That(explicitEntity.GetKind(), Is.EqualTo(MimeEntityKind.Message));
        Assert.That(explicitEntity.GetMediaType()!.IsType("message", "rfc822"), Is.True);
        Assert.That(explicitEntity.GetMediaType()!.WasDefaulted(), Is.False);
        Assert.That(explicitEntity.GetNestedMessage(), Is.SameAs(message));
        Assert.That(explicitEntity.GetHeaders()!.GetCount(), Is.Zero);
        Assert.That(digestEntity.GetMediaType()!.IsType("message", "rfc822"), Is.True);
        Assert.That(digestEntity.GetMediaType()!.WasDefaulted(), Is.True);
        Assert.That(digestEntity.GetHeaders()!.GetCount(), Is.Zero);
    }

    [Test]
    public void FactoryArgumentsRejectInvalidOrUnsupportedRequests()
    {
        Assert.Throws<ArgumentNullException>(() => new EmailEntityFactory().CreateTextWithValue(null!));
        Assert.Throws<ArgumentException>(() => new EmailEntityFactory().CreateTextWithValueAndSubtypeAndCharset("value", "bad/subtype", "utf-8"));
        Assert.Throws<ArgumentException>(() => new EmailEntityFactory().CreateTextWithValueAndSubtypeAndCharset("value", "plain", "not-a-real-charset"));
        Assert.Throws<ArgumentNullException>(() => new EmailEntityFactory().CreateBinaryWithValue((byte[])null!));
        Assert.Throws<ArgumentException>(() => new EmailEntityFactory().CreateBinaryWithValueAndTypeAndSubtype(new byte[0], "bad type", "data"));
        Assert.Throws<ArgumentNullException>(() => new EmailEntityFactory().CreateMultipartWithSubtype(null!));
        Assert.Throws<ArgumentException>(() => new EmailEntityFactory().CreateMultipartWithSubtype("signed"));
        Assert.Throws<ArgumentNullException>(() => new EmailEntityFactory().CreateNestedMessage(null!));
        Assert.Throws<ArgumentNullException>(() => new EmailEntityFactory().CreateDigestChild(null!));
    }

    static void AssertDefaultText(EmailMediaType value)
    {
        Assert.That(value.IsType("text", "plain"), Is.True);
        Assert.That(value.WasDefaulted(), Is.True);
        Assert.That(Encoding.ASCII.GetString(value.GetParameters().GetFirst("charset")!.GetValueBytes()), Is.EqualTo("us-ascii"));
    }

    static string Charset(MimeEntity entity)
    {
        return Encoding.ASCII.GetString(entity.GetMediaType()!.GetParameters().GetFirst("charset")!.GetValueBytes());
    }

    static EmailMediaType Media(string type, string subtype)
    {
        return new EmailMediaType(type, subtype, new EmailParameterList(new List<EmailParameter>(), new List<EmailRfc2231DecodedParameter>()), new byte[0], new EmailSourcePosition(0, 1, 1), false);
    }

    static EmailContent Content(string value)
    {
        return new EmailContentOperations().CreateMemory(Encoding.ASCII.GetBytes(value), EmailContentOwnership.Borrowed, false);
    }
}
