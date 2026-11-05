using ttf;

namespace ttf.Tests;

public class FontTagsTests
{
    [TestCase("avar", FontTags.Avar)]
    [TestCase("BASE", FontTags.Base)]
    [TestCase("CBDT", FontTags.Cbdt)]
    [TestCase("CBLC", FontTags.Cblc)]
    [TestCase("CFF ", FontTags.Cff)]
    [TestCase("CFF2", FontTags.Cff2)]
    [TestCase("cmap", FontTags.Cmap)]
    [TestCase("COLR", FontTags.Colr)]
    [TestCase("CPAL", FontTags.Cpal)]
    [TestCase("cvar", FontTags.Cvar)]
    [TestCase("cvt ", FontTags.Cvt)]
    [TestCase("DSIG", FontTags.Dsig)]
    [TestCase("EBDT", FontTags.Ebdt)]
    [TestCase("EBLC", FontTags.Eblc)]
    [TestCase("EBSC", FontTags.Ebsc)]
    [TestCase("fpgm", FontTags.Fpgm)]
    [TestCase("fvar", FontTags.Fvar)]
    [TestCase("gasp", FontTags.Gasp)]
    [TestCase("GDEF", FontTags.Gdef)]
    [TestCase("glyf", FontTags.Glyf)]
    [TestCase("GPOS", FontTags.Gpos)]
    [TestCase("GSUB", FontTags.Gsub)]
    [TestCase("gvar", FontTags.Gvar)]
    [TestCase("hdmx", FontTags.Hdmx)]
    [TestCase("head", FontTags.Head)]
    [TestCase("hhea", FontTags.Hhea)]
    [TestCase("hmtx", FontTags.Hmtx)]
    [TestCase("HVAR", FontTags.Hvar)]
    [TestCase("JSTF", FontTags.Jstf)]
    [TestCase("kern", FontTags.Kern)]
    [TestCase("loca", FontTags.Loca)]
    [TestCase("LTSH", FontTags.Ltsh)]
    [TestCase("MATH", FontTags.Math)]
    [TestCase("maxp", FontTags.Maxp)]
    [TestCase("MERG", FontTags.Merg)]
    [TestCase("meta", FontTags.Meta)]
    [TestCase("MVAR", FontTags.Mvar)]
    [TestCase("name", FontTags.Name)]
    [TestCase("OS/2", FontTags.Os2)]
    [TestCase("PCLT", FontTags.Pclt)]
    [TestCase("post", FontTags.Post)]
    [TestCase("prep", FontTags.Prep)]
    [TestCase("sbix", FontTags.Sbix)]
    [TestCase("STAT", FontTags.Stat)]
    [TestCase("SVG ", FontTags.Svg)]
    [TestCase("VDMX", FontTags.Vdmx)]
    [TestCase("vhea", FontTags.Vhea)]
    [TestCase("vmtx", FontTags.Vmtx)]
    [TestCase("VORG", FontTags.Vorg)]
    [TestCase("VVAR", FontTags.Vvar)]
    public void RegisteredTagHasExpectedBigEndianValue(string text, int value)
    {
        int expected = PackTag(text);

        Assert.That(value, Is.EqualTo(expected));
    }

    [Test]
    public void RegisteredTagsAreUnique()
    {
        int[] tags =
        [
            FontTags.Avar, FontTags.Base, FontTags.Cbdt, FontTags.Cblc,
            FontTags.Cff, FontTags.Cff2, FontTags.Cmap, FontTags.Colr,
            FontTags.Cpal, FontTags.Cvar, FontTags.Cvt, FontTags.Dsig,
            FontTags.Ebdt, FontTags.Eblc, FontTags.Ebsc, FontTags.Fpgm,
            FontTags.Fvar, FontTags.Gasp, FontTags.Gdef, FontTags.Glyf,
            FontTags.Gpos, FontTags.Gsub, FontTags.Gvar, FontTags.Hdmx,
            FontTags.Head, FontTags.Hhea, FontTags.Hmtx, FontTags.Hvar,
            FontTags.Jstf, FontTags.Kern, FontTags.Loca, FontTags.Ltsh,
            FontTags.Math, FontTags.Maxp, FontTags.Merg, FontTags.Meta,
            FontTags.Mvar, FontTags.Name, FontTags.Os2, FontTags.Pclt,
            FontTags.Post, FontTags.Prep, FontTags.Sbix, FontTags.Stat,
            FontTags.Svg, FontTags.Vdmx, FontTags.Vhea, FontTags.Vmtx,
            FontTags.Vorg, FontTags.Vvar
        ];

        Assert.That(tags.Distinct().Count(), Is.EqualTo(tags.Length));
    }

    private static int PackTag(string value)
    {
        return (value[0] << 24) |
            (value[1] << 16) |
            (value[2] << 8) |
            value[3];
    }
}
