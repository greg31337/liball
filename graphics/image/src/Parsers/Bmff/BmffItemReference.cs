using System.Collections.Generic;

namespace image;

internal class BmffItemReference
{
    internal string Type;
    internal uint FromItemId;
    internal List<uint> ToItemIds;

    internal BmffItemReference()
    {
        Type = "";
        FromItemId = 0;
        ToItemIds = new List<uint>();
    }
}

internal class BmffMetadataState
{
    internal uint PrimaryItemId;
    internal long IdatDataOffset;
    internal ulong IdatDataLength;
    internal List<BmffItem> Items;
    internal List<BmffItemProperty> Properties;
    internal List<BmffItemReference> References;

    internal BmffMetadataState()
    {
        PrimaryItemId = 0;
        IdatDataOffset = -1;
        IdatDataLength = 0;
        Items = new List<BmffItem>();
        Properties = new List<BmffItemProperty>();
        References = new List<BmffItemReference>();
    }
}
