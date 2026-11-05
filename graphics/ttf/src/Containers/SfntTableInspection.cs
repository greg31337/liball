namespace ttf;

internal static class SfntTableInspection
{
    internal static bool HasVersion(SfntTableData table)
    {
        if (table == null)
        {
            throw new System.ArgumentNullException("table");
        }

        SfntTableParser? parser = FindParser(table.GetTag());
        return parser != null &&
            table.GetLength() >= GetMinimumVersionLength(table.GetTag());
    }

    internal static SfntTableVersion GetVersion(SfntTableData table)
    {
        if (table == null)
        {
            throw new System.ArgumentNullException("table");
        }

        SfntTableParser? parser = FindParser(table.GetTag());

        if (parser == null ||
            table.GetLength() < GetMinimumVersionLength(table.GetTag()))
        {
            throw new System.InvalidOperationException(
                "A parser-defined table version is not available.");
        }

        return parser.ReadVersion(table);
    }

    internal static FontTableSupportState GetSupportState(
        SfntTableData table,
        FontLoadMode mode)
    {
        if (table == null)
        {
            throw new System.ArgumentNullException("table");
        }

        SfntTableClassification classification = table.GetClassification();

        if (classification == SfntTableClassification.Unsupported)
        {
            return FontTableSupportState.Unsupported;
        }

        if (classification == SfntTableClassification.Unknown)
        {
            return FontTableSupportState.Unknown;
        }

        SfntTableParser? parser = FindParser(table.GetTag());

        if (parser == null)
        {
            return FontTableSupportState.Supported;
        }

        if (table.GetLength() < GetMinimumVersionLength(table.GetTag()))
        {
            return FontTableSupportState.RawOnly;
        }

        SfntTableVersionSupport support = parser.EvaluateVersion(
            parser.ReadVersion(table),
            mode);

        if (support == SfntTableVersionSupport.Supported)
        {
            return FontTableSupportState.Supported;
        }

        if (support == SfntTableVersionSupport.CompatibleExtension)
        {
            return FontTableSupportState.CompatibleExtension;
        }

        return FontTableSupportState.RawOnly;
    }

    static SfntTableParser? FindParser(int tag)
    {
        switch (tag)
        {
            case FontTags.Avar:
                return SfntAvarTableParser.GetParser();
            case FontTags.Base:
                return SfntBaseTableParser.GetParser();
            case FontTags.Cbdt:
                return SfntCbdtTableParser.GetParser();
            case FontTags.Cblc:
                return SfntCblcTableParser.GetParser();
            case FontTags.Cff:
                return SfntCffTableParser.GetParser();
            case FontTags.Cff2:
                return SfntCff2TableParser.GetParser();
            case FontTags.Cmap:
                return SfntCmapTableParser.GetParser();
            case FontTags.Colr:
                return SfntColrTableParser.GetParser();
            case FontTags.Cpal:
                return SfntCpalTableParser.GetParser();
            case FontTags.Cvar:
                return SfntCvarTableParser.GetParser();
            case FontTags.Dsig:
                return SfntDsigTableParser.GetParser();
            case FontTags.Ebdt:
                return SfntEbdtTableParser.GetParser();
            case FontTags.Eblc:
                return SfntEblcTableParser.GetParser();
            case FontTags.Ebsc:
                return SfntEbscTableParser.GetParser();
            case FontTags.Fvar:
                return SfntFvarTableParser.GetParser();
            case FontTags.Gasp:
                return SfntGaspTableParser.GetParser();
            case FontTags.Gdef:
                return SfntGdefTableParser.GetParser();
            case FontTags.Gpos:
                return SfntGposTableParser.GetParser();
            case FontTags.Gsub:
                return SfntGsubTableParser.GetParser();
            case FontTags.Gvar:
                return SfntGvarTableParser.GetParser();
            case FontTags.Hdmx:
                return SfntHdmxTableParser.GetParser();
            case FontTags.Head:
                return SfntHeadTableParser.GetParser();
            case FontTags.Hhea:
                return SfntHheaTableParser.GetParser();
            case FontTags.Hmtx:
                return SfntHmtxTableParser.GetParser();
            case FontTags.Hvar:
                return SfntMetricVariationsTableParser.GetHvarParser();
            case FontTags.Jstf:
                return SfntJstfTableParser.GetParser();
            case FontTags.Kern:
                return SfntKernTableParser.GetParser();
            case FontTags.Loca:
                return SfntLocaTableParser.GetParser();
            case FontTags.Ltsh:
                return SfntLtshTableParser.GetParser();
            case FontTags.Math:
                return SfntMathTableParser.GetParser();
            case FontTags.Maxp:
                return SfntMaxpTableParser.GetParser();
            case FontTags.Merg:
                return SfntMergTableParser.GetParser();
            case FontTags.Meta:
                return SfntMetaTableParser.GetParser();
            case FontTags.Mvar:
                return SfntMvarTableParser.GetParser();
            case FontTags.Name:
                return SfntNameTableParser.GetParser();
            case FontTags.Os2:
                return SfntOs2TableParser.GetParser();
            case FontTags.Pclt:
                return SfntPcltTableParser.GetParser();
            case FontTags.Post:
                return SfntPostTableParser.GetParser();
            case FontTags.Sbix:
                return SfntSbixTableParser.GetParser();
            case FontTags.Stat:
                return SfntStatTableParser.GetParser();
            case FontTags.Svg:
                return SfntSvgTableParser.GetParser();
            case FontTags.Vdmx:
                return SfntVdmxTableParser.GetParser();
            case FontTags.Vhea:
                return SfntVheaTableParser.GetParser();
            case FontTags.Vmtx:
                return SfntVmtxTableParser.GetParser();
            case FontTags.Vorg:
                return SfntVorgTableParser.GetParser();
            case FontTags.Vvar:
                return SfntMetricVariationsTableParser.GetVvarParser();
            default:
                return null;
        }
    }

    static int GetMinimumVersionLength(int tag)
    {
        switch (tag)
        {
            case FontTags.Hmtx:
            case FontTags.Loca:
            case FontTags.Vmtx:
                return 0;
            case FontTags.Cff:
            case FontTags.Cff2:
            case FontTags.Cmap:
            case FontTags.Colr:
            case FontTags.Cpal:
            case FontTags.Gasp:
            case FontTags.Hdmx:
            case FontTags.Ltsh:
            case FontTags.Merg:
            case FontTags.Name:
            case FontTags.Os2:
            case FontTags.Sbix:
            case FontTags.Svg:
            case FontTags.Vdmx:
                return 2;
            default:
                return 4;
        }
    }
}
