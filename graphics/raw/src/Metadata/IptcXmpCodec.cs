namespace raw;

internal static class IptcXmpCodec
{
    internal const string CoreNamespace = "http://iptc.org/std/Iptc4xmpCore/1.0/xmlns/";
    internal const string ExtensionNamespace = "http://iptc.org/std/Iptc4xmpExt/2008-02-29/";
    internal const string DublinCoreNamespace = "http://purl.org/dc/elements/1.1/";
    internal const string PhotoshopNamespace = "http://ns.adobe.com/photoshop/1.0/";
    internal const string PlusNamespace = "http://ns.useplus.org/ldf/xmp/1.0/";
    internal const string XmpNamespace = "http://ns.adobe.com/xap/1.0/";
    internal const string XmpRightsNamespace = "http://ns.adobe.com/xap/1.0/rights/";
    internal const string ExifNamespace = "http://ns.adobe.com/exif/1.0/";

    internal static RawIptcXmpField[] Extract(RawXmpPacket[] packets, RawIptcSchema schema)
    {
        System.Collections.Generic.List<RawIptcXmpField> fields = new System.Collections.Generic.List<RawIptcXmpField>();
        if (packets == null)
        {
            return fields.ToArray();
        }
        int packetIndex = 0;
        while (packetIndex < packets.Length)
        {
            RawXmpPacket packet = packets[packetIndex];
            if (packet != null && packet.Fields != null)
            {
                int fieldIndex = 0;
                while (fieldIndex < packet.Fields.Length)
                {
                    RawXmpField field = packet.Fields[fieldIndex];
                    if (field != null && IsSchemaField(field, schema))
                    {
                        fields.Add(new RawIptcXmpField(schema, field, packetIndex));
                    }
                    fieldIndex++;
                }
            }
            packetIndex++;
        }
        return fields.ToArray();
    }

    internal static bool IsSchemaField(RawXmpField field, RawIptcSchema schema)
    {
        if (schema == RawIptcSchema.Core)
        {
            if (field.NamespaceUri == CoreNamespace)
            {
                return true;
            }
            if (field.NamespaceUri == DublinCoreNamespace)
            {
                return field.Name == "title" || field.Name == "creator" || field.Name == "description" || field.Name == "subject" || field.Name == "rights";
            }
            if (field.NamespaceUri == PhotoshopNamespace)
            {
                return IsPhotoshopCoreName(field.Name);
            }
            return field.NamespaceUri == XmpRightsNamespace && field.Name == "UsageTerms";
        }
        if (field.NamespaceUri == ExtensionNamespace || field.NamespaceUri == PlusNamespace)
        {
            return true;
        }
        if (field.NamespaceUri == XmpNamespace)
        {
            return field.Name == "Rating";
        }
        if (field.NamespaceUri == XmpRightsNamespace)
        {
            return field.Name == "WebStatement";
        }
        if (field.NamespaceUri == ExifNamespace)
        {
            return field.Name == "GPSAltitude" || field.Name == "GPSAltitudeRef" || field.Name == "GPSLatitude" || field.Name == "GPSLongitude";
        }
        return false;
    }

    internal static bool IsPhotoshopCoreName(string name)
    {
        return name == "AuthorsPosition" || name == "CaptionWriter" || name == "Category" || name == "City" || name == "Country" || name == "Credit" || name == "DateCreated" || name == "Headline" || name == "Instructions" || name == "Source" || name == "State" || name == "SupplementalCategories" || name == "TransmissionReference" || name == "Urgency";
    }
}
