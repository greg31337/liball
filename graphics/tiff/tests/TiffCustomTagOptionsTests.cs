using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffCustomTagOptionsTests
{
    [Test]
    public void ReadOptions_DefaultRegistryIncludesKnownTags()
    {
        TiffReadOptions options = new TiffReadOptions();

        TiffTagDefinition definition = RequireDefinition(options.FindTagDefinition(TiffTagId.ImageWidth));

        Assert.That(definition.Name, Is.EqualTo("ImageWidth"));
        Assert.That(definition.AllowsDataType(TiffDataType.Long), Is.True);
    }

    [Test]
    public void WriteOptions_DefaultRegistryIncludesKnownTags()
    {
        TiffWriteOptions options = new TiffWriteOptions();

        TiffTagDefinition definition = RequireDefinition(options.FindTagDefinition(TiffTagId.Compression));

        Assert.That(definition.Name, Is.EqualTo("Compression"));
        Assert.That(definition.AllowsDataType(TiffDataType.Short), Is.True);
    }

    [Test]
    public void ReadOptions_RegisterPrivateTagDefinition()
    {
        TiffReadOptions options = new TiffReadOptions();
        TiffTagDefinition custom = TiffTagDefinition.Create(65000, "PrivateShorts", TiffDataType.Short, new TiffDataType[] { TiffDataType.Short }, TiffTagDefinition.AnyCount);

        options.RegisterTagDefinition(custom);
        custom.Name = "Changed";

        TiffTagDefinition definition = RequireDefinition(options.FindTagDefinition(65000));
        Assert.That(definition.Name, Is.EqualTo("PrivateShorts"));
        Assert.That(definition.PreferredDataType, Is.EqualTo(TiffDataType.Short));
        Assert.That(definition.AllowsDataType(TiffDataType.Short), Is.True);
        Assert.That(definition.AllowsCount(8), Is.True);
    }

    [Test]
    public void WriteOptions_RegisterPrivateTagDefinition()
    {
        TiffWriteOptions options = new TiffWriteOptions();
        TiffTagDefinition custom = TiffTagDefinition.Create(65001, "PrivateAscii", TiffDataType.Ascii, new TiffDataType[] { TiffDataType.Ascii }, TiffTagDefinition.AnyCount);

        options.RegisterTagDefinition(custom);

        TiffTagDefinition definition = RequireDefinition(options.FindTagDefinition(65001));
        Assert.That(definition.Name, Is.EqualTo("PrivateAscii"));
        Assert.That(definition.AllowsDataType(TiffDataType.Ascii), Is.True);
    }

    [Test]
    public void ReadOptions_CopyDeepCopiesCustomRegistry()
    {
        TiffReadOptions options = new TiffReadOptions();
        options.RegisterTagDefinition(TiffTagDefinition.Create(65002, "Original", TiffDataType.Long, new TiffDataType[] { TiffDataType.Long }, 1));

        TiffReadOptions copy = options.Copy();
        options.RegisterTagDefinition(TiffTagDefinition.Create(65002, "Changed", TiffDataType.Ascii, new TiffDataType[] { TiffDataType.Ascii }, TiffTagDefinition.AnyCount));

        TiffTagDefinition copiedDefinition = RequireDefinition(copy.FindTagDefinition(65002));
        Assert.That(copiedDefinition.Name, Is.EqualTo("Original"));
        Assert.That(copiedDefinition.AllowsDataType(TiffDataType.Long), Is.True);
        Assert.That(copiedDefinition.AllowsDataType(TiffDataType.Ascii), Is.False);
    }

    [Test]
    public void WriteOptions_CopyDeepCopiesCustomRegistry()
    {
        TiffWriteOptions options = new TiffWriteOptions();
        options.RegisterTagDefinition(TiffTagDefinition.Create(65003, "Original", TiffDataType.Double, new TiffDataType[] { TiffDataType.Double }, 3));

        TiffWriteOptions copy = options.Copy();
        options.RegisterTagDefinition(TiffTagDefinition.Create(65003, "Changed", TiffDataType.Byte, new TiffDataType[] { TiffDataType.Byte }, TiffTagDefinition.AnyCount));

        TiffTagDefinition copiedDefinition = RequireDefinition(copy.FindTagDefinition(65003));
        Assert.That(copiedDefinition.Name, Is.EqualTo("Original"));
        Assert.That(copiedDefinition.AllowsDataType(TiffDataType.Double), Is.True);
        Assert.That(copiedDefinition.AllowsCount(3), Is.True);
        Assert.That(copiedDefinition.AllowsCount(4), Is.False);
    }

    [Test]
    public void OptionRegistries_AreIndependentBetweenReadAndWriteOptions()
    {
        TiffReadOptions readOptions = new TiffReadOptions();
        TiffWriteOptions writeOptions = new TiffWriteOptions();

        readOptions.RegisterTagDefinition(TiffTagDefinition.Create(65004, "ReadOnlyPrivate", TiffDataType.Byte, new TiffDataType[] { TiffDataType.Byte }, 1));

        Assert.That(readOptions.FindTagDefinition(65004), Is.Not.Null);
        Assert.That(writeOptions.FindTagDefinition(65004), Is.Null);
    }

    [Test]
    public void Options_RecreateDefaultRegistryIfFieldWasCleared()
    {
        TiffReadOptions readOptions = new TiffReadOptions();
        TiffWriteOptions writeOptions = new TiffWriteOptions();
        readOptions.TagRegistry = null!;
        writeOptions.TagRegistry = null!;

        TiffTagDefinition readDefinition = RequireDefinition(readOptions.FindTagDefinition(TiffTagId.ImageLength));
        TiffTagDefinition writeDefinition = RequireDefinition(writeOptions.FindTagDefinition(TiffTagId.ImageLength));

        Assert.That(readDefinition.Name, Is.EqualTo("ImageLength"));
        Assert.That(writeDefinition.Name, Is.EqualTo("ImageLength"));
        Assert.That(readOptions.TagRegistry, Is.Not.Null);
        Assert.That(writeOptions.TagRegistry, Is.Not.Null);
    }

    static TiffTagDefinition RequireDefinition(TiffTagDefinition? definition)
    {
        if (definition == null)
        {
            throw new InvalidOperationException("The expected tag definition was not registered.");
        }

        return definition;
    }
}
