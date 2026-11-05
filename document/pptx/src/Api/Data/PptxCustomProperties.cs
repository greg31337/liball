using System.Collections.Generic;
using System.Globalization;
using System.Xml;

namespace pptx;

/// <summary>
/// Represents custom property data that can be read from or written to a PowerPoint presentation.
/// </summary>
public class PptxCustomProperty
{
    /// <summary>
    /// Stores the name associated with this object.
    /// </summary>
    public string Name;
    /// <summary>
    /// Stores the value type associated with this object.
    /// </summary>
    public string ValueType;
    /// <summary>
    /// Stores the value associated with this object.
    /// </summary>
    public string Value;
    /// <summary>
    /// Stores the property id used to identify or connect related presentation data.
    /// </summary>
    public int PropertyId;

    /// <summary>
    /// Initializes a new custom property with safe defaults or the supplied values.
    /// </summary>
    public PptxCustomProperty()
    {
        Name = "";
        ValueType = "lpwstr";
        Value = "";
        PropertyId = 0;
    }
}

/// <summary>
/// Represents custom properties data that can be read from or written to a PowerPoint presentation.
/// </summary>
public class PptxCustomProperties
{
    internal PptxPart Part;
    /// <summary>
    /// Stores the part name associated with this object.
    /// </summary>
    public string PartName;
    /// <summary>
    /// Stores the relationship id used to identify or connect related presentation data.
    /// </summary>
    public string RelationshipId;
    /// <summary>
    /// Contains the items in package or presentation order.
    /// </summary>
    public List<PptxCustomProperty> Items;

    internal PptxCustomProperties(PptxPart part, string relationshipId)
    {
        if (part == null)
        {
            throw new System.ArgumentNullException("part");
        }

        Part = part;
        PartName = part.PartName;
        RelationshipId = PptxDiagnostic.CleanText(relationshipId);
        Items = new List<PptxCustomProperty>();
        Load();
    }
    internal int Count()
    {
        return Items.Count;
    }

    internal void Load()
    {
        Items.Clear();
        XmlDocument document = PptxXml.LoadBytes(GetCurrentBytes());
        XmlElement? root = document.DocumentElement;
        if (root == null)
        {
            return;
        }

        for (int index = 0; index < root.ChildNodes.Count; index++)
        {
            XmlNode? child = root.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement element = (XmlElement)child;
            if (element.NamespaceURI == PptxNamespaces.CustomProperties && element.LocalName == "property")
            {
                Items.Add(ParseProperty(element));
            }
        }
    }
    internal void Save()
    {
        XmlDocument document = PptxXml.LoadBytes(GetCurrentBytes());
        XmlElement? rootValue = document.DocumentElement;
        if (rootValue == null)
        {
            throw new System.ArgumentException("Custom properties XML root is required.");
        }

        XmlElement root = rootValue;
        RemovePropertyElements(root);

        for (int index = 0; index < Items.Count; index++)
        {
            PptxCustomProperty item = Items[index];
            ValidateProperty(item.Name, item.ValueType);
            item.PropertyId = index + 2;
            XmlElement property = PptxXml.CreateElement(document, "", "property", PptxNamespaces.CustomProperties);
            PptxXml.SetAttributeValue(property, "", "", "fmtid", "{D5CDD505-2E9C-101B-9397-08002B2CF9AE}");
            PptxXml.SetAttributeValue(property, "", "", "pid", item.PropertyId.ToString());
            PptxXml.SetAttributeValue(property, "", "", "name", item.Name);
            XmlElement value = PptxXml.CreateElement(document, "vt", GetValueType(item), PptxNamespaces.VariantTypes);
            value.InnerText = PptxDiagnostic.CleanText(item.Value);
            property.AppendChild(value);
            root.AppendChild(property);
        }

        Part.SetXmlDocument(document);
    }
    internal PptxCustomProperty? Find(string name)
    {
        string clean = PptxDiagnostic.CleanText(name);
        for (int index = 0; index < Items.Count; index++)
        {
            if (Items[index].Name == clean)
            {
                return Items[index];
            }
        }

        return null;
    }
    internal string GetValue(string name)
    {
        PptxCustomProperty? item = Find(name);
        if (item == null)
        {
            return "";
        }

        return item.Value;
    }
    internal PptxCustomProperty SetString(string name, string value)
    {
        return SetValue(name, "lpwstr", value);
    }
    internal PptxCustomProperty SetInt(string name, int value)
    {
        return SetValue(name, "i4", value.ToString());
    }
    internal PptxCustomProperty SetBool(string name, bool value)
    {
        if (value)
        {
            return SetValue(name, "bool", "true");
        }

        return SetValue(name, "bool", "false");
    }
    internal PptxCustomProperty SetLong(string name, long value)
    {
        return SetValue(name, "i8", value.ToString(CultureInfo.InvariantCulture));
    }
    internal PptxCustomProperty SetDouble(string name, double value)
    {
        return SetValue(name, "r8", value.ToString("R", CultureInfo.InvariantCulture));
    }
    internal PptxCustomProperty SetDateTime(string name, string value)
    {
        return SetValue(name, "filetime", value);
    }
    internal PptxCustomProperty SetValue(string name, string valueType, string value)
    {
        string cleanName = PptxDiagnostic.CleanText(name);
        string cleanType = PptxDiagnostic.CleanText(valueType);
        ValidateProperty(cleanName, cleanType);
        PptxCustomProperty? existing = Find(cleanName);
        if (existing != null)
        {
            existing.ValueType = cleanType;
            existing.Value = PptxDiagnostic.CleanText(value);
            return existing;
        }

        PptxCustomProperty item = new PptxCustomProperty();
        item.Name = cleanName;
        item.ValueType = cleanType;
        item.Value = PptxDiagnostic.CleanText(value);
        Items.Add(item);
        return item;
    }
    internal bool Remove(string name)
    {
        string clean = PptxDiagnostic.CleanText(name);
        for (int index = 0; index < Items.Count; index++)
        {
            if (Items[index].Name == clean)
            {
                Items.RemoveAt(index);
                return true;
            }
        }

        return false;
    }
    internal bool Rename(string oldName, string newName)
    {
        PptxCustomProperty? item = Find(oldName);
        if (item == null)
        {
            return false;
        }

        string cleanNewName = PptxDiagnostic.CleanText(newName);
        ValidateProperty(cleanNewName, item.ValueType);
        PptxCustomProperty? conflict = Find(cleanNewName);
        if (conflict != null && conflict != item)
        {
            throw new System.ArgumentException("A custom property with the new name already exists.");
        }

        item.Name = cleanNewName;
        return true;
    }
    internal int Clear()
    {
        int count = Items.Count;
        Items.Clear();
        return count;
    }

    internal byte[] GetCurrentBytes()
    {
        if (Part.HasXmlDocument)
        {
            return PptxXml.WriteBytes(Part.XmlDocument);
        }

        return Part.GetBytes();
    }

    internal static PptxCustomProperty ParseProperty(XmlElement element)
    {
        PptxCustomProperty item = new PptxCustomProperty();
        item.Name = PptxXml.GetAttributeValue(element, "", "name");
        item.PropertyId = PptxXml.GetIntAttribute(element, "", "pid", 0);

        for (int index = 0; index < element.ChildNodes.Count; index++)
        {
            XmlNode? child = element.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement valueElement = (XmlElement)child;
            item.ValueType = valueElement.LocalName;
            item.Value = valueElement.InnerText;
            return item;
        }

        return item;
    }
    internal static string GetValueType(PptxCustomProperty item)
    {
        if (item.ValueType.Length == 0)
        {
            return "lpwstr";
        }

        return item.ValueType;
    }

    internal static string CreateEmptyXml()
    {
        return "<Properties xmlns=\"" + PptxNamespaces.CustomProperties + "\" xmlns:vt=\"" + PptxNamespaces.VariantTypes + "\"/>";
    }

    internal static void RemovePropertyElements(XmlElement root)
    {
        for (int index = root.ChildNodes.Count - 1; index >= 0; index--)
        {
            XmlNode? child = root.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement element = (XmlElement)child;
            if (element.NamespaceURI == PptxNamespaces.CustomProperties && element.LocalName == "property")
            {
                root.RemoveChild(element);
            }
        }
    }

    internal static void ValidateProperty(string name, string valueType)
    {
        if (name.Length == 0)
        {
            throw new System.ArgumentException("Custom property name is required.");
        }

        if (valueType.Length == 0)
        {
            throw new System.ArgumentException("Custom property value type is required.");
        }

        XmlConvert.VerifyNCName(valueType);
    }
}
