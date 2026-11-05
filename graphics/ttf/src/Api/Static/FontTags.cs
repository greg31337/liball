namespace ttf;

/// <summary>
/// Provides the big-endian integer values of registered OpenType table tags.
/// </summary>
/// <remarks>
/// Each value is the four tag bytes packed from most significant to least
/// significant byte. The values are stable and may be persisted by callers.
/// </remarks>
public static class FontTags
{
    /// <summary>The <c>avar</c> axis variations table.</summary>
    public const int Avar = 0x61766172;

    /// <summary>The <c>BASE</c> baseline data table.</summary>
    public const int Base = 0x42415345;

    /// <summary>The <c>CBDT</c> color bitmap data table.</summary>
    public const int Cbdt = 0x43424454;

    /// <summary>The <c>CBLC</c> color bitmap location data table.</summary>
    public const int Cblc = 0x43424C43;

    /// <summary>The <c>CFF </c> Compact Font Format 1 table.</summary>
    public const int Cff = 0x43464620;

    /// <summary>The <c>CFF2</c> Compact Font Format 2 table.</summary>
    public const int Cff2 = 0x43464632;

    /// <summary>The <c>cmap</c> character-to-glyph mapping table.</summary>
    public const int Cmap = 0x636D6170;

    /// <summary>The <c>COLR</c> color glyph table.</summary>
    public const int Colr = 0x434F4C52;

    /// <summary>The <c>CPAL</c> color palette table.</summary>
    public const int Cpal = 0x4350414C;

    /// <summary>The <c>cvar</c> control value variations table.</summary>
    public const int Cvar = 0x63766172;

    /// <summary>The <c>cvt </c> control value table.</summary>
    public const int Cvt = 0x63767420;

    /// <summary>The <c>DSIG</c> digital signature table.</summary>
    public const int Dsig = 0x44534947;

    /// <summary>The <c>EBDT</c> embedded bitmap data table.</summary>
    public const int Ebdt = 0x45424454;

    /// <summary>The <c>EBLC</c> embedded bitmap location data table.</summary>
    public const int Eblc = 0x45424C43;

    /// <summary>The <c>EBSC</c> embedded bitmap scaling table.</summary>
    public const int Ebsc = 0x45425343;

    /// <summary>The <c>fpgm</c> TrueType font program table.</summary>
    public const int Fpgm = 0x6670676D;

    /// <summary>The <c>fvar</c> font variations table.</summary>
    public const int Fvar = 0x66766172;

    /// <summary>The <c>gasp</c> grid-fitting and scan-conversion table.</summary>
    public const int Gasp = 0x67617370;

    /// <summary>The <c>GDEF</c> glyph definition table.</summary>
    public const int Gdef = 0x47444546;

    /// <summary>The <c>glyf</c> TrueType glyph data table.</summary>
    public const int Glyf = 0x676C7966;

    /// <summary>The <c>GPOS</c> glyph positioning table.</summary>
    public const int Gpos = 0x47504F53;

    /// <summary>The <c>GSUB</c> glyph substitution table.</summary>
    public const int Gsub = 0x47535542;

    /// <summary>The <c>gvar</c> glyph variations table.</summary>
    public const int Gvar = 0x67766172;

    /// <summary>The <c>hdmx</c> horizontal device metrics table.</summary>
    public const int Hdmx = 0x68646D78;

    /// <summary>The <c>head</c> font header table.</summary>
    public const int Head = 0x68656164;

    /// <summary>The <c>hhea</c> horizontal header table.</summary>
    public const int Hhea = 0x68686561;

    /// <summary>The <c>hmtx</c> horizontal metrics table.</summary>
    public const int Hmtx = 0x686D7478;

    /// <summary>The <c>HVAR</c> horizontal metrics variations table.</summary>
    public const int Hvar = 0x48564152;

    /// <summary>The <c>JSTF</c> justification table.</summary>
    public const int Jstf = 0x4A535446;

    /// <summary>The <c>kern</c> legacy kerning table.</summary>
    public const int Kern = 0x6B65726E;

    /// <summary>The <c>loca</c> glyph location index table.</summary>
    public const int Loca = 0x6C6F6361;

    /// <summary>The <c>LTSH</c> linear threshold table.</summary>
    public const int Ltsh = 0x4C545348;

    /// <summary>The <c>MATH</c> mathematical typesetting table.</summary>
    public const int Math = 0x4D415448;

    /// <summary>The <c>maxp</c> maximum profile table.</summary>
    public const int Maxp = 0x6D617870;

    /// <summary>The <c>MERG</c> merge table.</summary>
    public const int Merg = 0x4D455247;

    /// <summary>The <c>meta</c> metadata table.</summary>
    public const int Meta = 0x6D657461;

    /// <summary>The <c>MVAR</c> metrics variations table.</summary>
    public const int Mvar = 0x4D564152;

    /// <summary>The <c>name</c> naming table.</summary>
    public const int Name = 0x6E616D65;

    /// <summary>The <c>OS/2</c> OS/2 and Windows metrics table.</summary>
    public const int Os2 = 0x4F532F32;

    /// <summary>The <c>PCLT</c> PCL 5 data table.</summary>
    public const int Pclt = 0x50434C54;

    /// <summary>The <c>post</c> PostScript information table.</summary>
    public const int Post = 0x706F7374;

    /// <summary>The <c>prep</c> TrueType control value program table.</summary>
    public const int Prep = 0x70726570;

    /// <summary>The <c>sbix</c> standard bitmap graphics table.</summary>
    public const int Sbix = 0x73626978;

    /// <summary>The <c>STAT</c> style attributes table.</summary>
    public const int Stat = 0x53544154;

    /// <summary>The <c>SVG </c> scalable vector graphics table.</summary>
    public const int Svg = 0x53564720;

    /// <summary>The <c>VDMX</c> vertical device metrics table.</summary>
    public const int Vdmx = 0x56444D58;

    /// <summary>The <c>vhea</c> vertical metrics header table.</summary>
    public const int Vhea = 0x76686561;

    /// <summary>The <c>vmtx</c> vertical metrics table.</summary>
    public const int Vmtx = 0x766D7478;

    /// <summary>The <c>VORG</c> vertical origin table.</summary>
    public const int Vorg = 0x564F5247;

    /// <summary>The <c>VVAR</c> vertical metrics variations table.</summary>
    public const int Vvar = 0x56564152;
}
