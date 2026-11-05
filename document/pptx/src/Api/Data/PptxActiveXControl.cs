using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace pptx;

/// <summary>
/// Represents an ActiveX control reference and its persisted control properties.
/// </summary>
public class PptxActiveXControl
{
    internal PptxPresentation Presentation;
    internal PptxPart OwnerPart;
    internal XmlElement Element;
    internal PptxRelationship? ControlRelationship;
    internal PptxPart? ControlPart;
    internal PptxRelationship? PropertyRelationship;
    internal PptxPart? PropertyPart;
    /// <summary>
    /// Stores the slide relationship identifier for the control persistence part.
    /// </summary>
    public string RelationshipId;
    /// <summary>
    /// Stores the package part that contains the <c>p:control</c> reference.
    /// </summary>
    public string SourcePartName;
    /// <summary>
    /// Stores the control name used by scripting environments.
    /// </summary>
    public string Name;
    /// <summary>
    /// Stores the control shape identifier.
    /// </summary>
    public string ShapeId;
    /// <summary>
    /// Indicates whether the control is displayed as an icon.
    /// </summary>
    public bool ShowAsIcon;
    /// <summary>
    /// Stores the control image width in English Metric Units (EMUs).
    /// </summary>
    public long ImageWidth;
    /// <summary>
    /// Stores the control image height in English Metric Units (EMUs).
    /// </summary>
    public long ImageHeight;
    /// <summary>
    /// Stores the ActiveX control part name.
    /// </summary>
    public string PartName;
    /// <summary>
    /// Stores the ActiveX control part content type.
    /// </summary>
    public string ContentType;
    /// <summary>
    /// Stores the complete ActiveX control persistence XML, including unsupported content.
    /// </summary>
    public string ControlXml;
    /// <summary>
    /// Stores the ActiveX class identifier.
    /// </summary>
    public string ClassId;
    /// <summary>
    /// Stores the ActiveX license string.
    /// </summary>
    public string License;
    /// <summary>
    /// Stores the ActiveX persistence mode.
    /// </summary>
    public string Persistence;
    /// <summary>
    /// Contains inline property-bag entries in control-part order.
    /// </summary>
    public List<PptxActiveXProperty> Properties;
    /// <summary>
    /// Stores the relationship identifier for binary control properties.
    /// </summary>
    public string PropertyRelationshipId;
    /// <summary>
    /// Stores the binary control-property part name.
    /// </summary>
    public string PropertyPartName;
    /// <summary>
    /// Stores the binary control-property content type.
    /// </summary>
    public string PropertyContentType;
    /// <summary>
    /// Contains the binary control-property payload.
    /// </summary>
    public byte[] PropertyBytes;
    /// <summary>
    /// Indicates whether a binary control-property part is available.
    /// </summary>
    public bool HasPropertyPart;

    internal PptxActiveXControl(PptxPresentation presentation, PptxPart ownerPart, XmlElement element)
    {
        if (presentation == null)
        {
            throw new System.ArgumentNullException("presentation");
        }

        if (ownerPart == null)
        {
            throw new System.ArgumentNullException("ownerPart");
        }

        if (element == null)
        {
            throw new System.ArgumentNullException("element");
        }

        Presentation = presentation;
        OwnerPart = ownerPart;
        Element = element;
        ControlRelationship = null;
        ControlPart = null;
        PropertyRelationship = null;
        PropertyPart = null;
        RelationshipId = "";
        SourcePartName = "";
        Name = "";
        ShapeId = "";
        ShowAsIcon = false;
        ImageWidth = 0L;
        ImageHeight = 0L;
        PartName = "";
        ContentType = "";
        ControlXml = "";
        ClassId = "";
        License = "";
        Persistence = "";
        Properties = new List<PptxActiveXProperty>();
        PropertyRelationshipId = "";
        PropertyPartName = "";
        PropertyContentType = "";
        PropertyBytes = new byte[0];
        HasPropertyPart = false;
        Load();
    }

    internal void Load()
    {
        RelationshipId = PptxXml.GetAttributeValue(Element, PptxNamespaces.Relationships, "id");
        SourcePartName = OwnerPart.PartName;
        Name = PptxXml.GetAttributeValue(Element, "", "name");
        ShapeId = PptxXml.GetAttributeValue(Element, "", "spid");
        ShowAsIcon = PptxXml.GetBoolAttribute(Element, "", "showAsIcon", false);
        ImageWidth = PptxTransform.GetLongAttribute(Element, "imgW", 0L);
        ImageHeight = PptxTransform.GetLongAttribute(Element, "imgH", 0L);
        ControlRelationship = FindRelationship(OwnerPart.Relationships, RelationshipId);
        ControlPart = null;
        PropertyRelationship = null;
        PropertyPart = null;
        PartName = "";
        ContentType = "";
        ControlXml = "";
        ClassId = "";
        License = "";
        Persistence = "";
        Properties.Clear();
        PropertyRelationshipId = "";
        PropertyPartName = "";
        PropertyContentType = "";
        PropertyBytes = new byte[0];
        HasPropertyPart = false;
        if (ControlRelationship == null || !ControlRelationship.IsInternal())
        {
            return;
        }

        PptxPackage package = Presentation.Package;
        if (!package.HasPart(ControlRelationship.TargetPartName))
        {
            return;
        }

        ControlPart = package.GetPart(ControlRelationship.TargetPartName);
        PartName = ControlPart.PartName;
        ContentType = ControlPart.ContentType;
        XmlDocument document = GetControlDocument();
        if (ControlPart.HasXmlDocument)
        {
            ControlXml = PptxXml.WriteString(document);
        }
        else
        {
            ControlXml = Encoding.UTF8.GetString(ControlPart.GetBytes());
        }
        XmlElement? root = document.DocumentElement;
        if (root == null)
        {
            return;
        }

        ClassId = GetActiveXAttribute(root, "classid");
        License = GetActiveXAttribute(root, "license");
        Persistence = GetActiveXAttribute(root, "persistence");
        PropertyRelationshipId = PptxXml.GetAttributeValue(root, PptxNamespaces.Relationships, "id");
        LoadInlineProperties(root);
        PropertyRelationship = FindRelationship(ControlPart.Relationships, PropertyRelationshipId);
        if (PropertyRelationship == null || !PropertyRelationship.IsInternal())
        {
            return;
        }

        if (!package.HasPart(PropertyRelationship.TargetPartName))
        {
            return;
        }

        PropertyPart = package.GetPart(PropertyRelationship.TargetPartName);
        PropertyPartName = PropertyPart.PartName;
        PropertyContentType = PropertyPart.ContentType;
        PropertyBytes = PropertyPart.GetBytes();
        HasPropertyPart = true;
    }

    internal string GetControlXml()
    {
        return ControlXml;
    }

    internal byte[] GetPropertyBytes()
    {
        if (PropertyPart == null)
        {
            return new byte[0];
        }

        return PropertyPart.GetBytes();
    }

    internal void ReplaceControlXml(string xml)
    {
        if (ControlPart == null)
        {
            throw new System.ArgumentException("ActiveX control part was not found.");
        }

        XmlDocument document = PptxXml.LoadString(xml);
        ValidateControlRoot(document.DocumentElement);
        ControlPart.SetXmlDocument(document);
        Load();
    }

    internal void ReplacePropertyBytes(byte[] bytes, string contentType, string persistence)
    {
        if (bytes == null)
        {
            throw new System.ArgumentNullException("bytes");
        }

        if (ControlPart == null)
        {
            throw new System.ArgumentException("ActiveX control part was not found.");
        }

        string cleanContentType = PptxPart.ValidateAndNormalizeContentType(contentType);
        string cleanPersistence = PptxDiagnostic.CleanText(persistence);
        if (cleanPersistence.Length == 0)
        {
            throw new System.ArgumentException("ActiveX persistence mode is required.", "persistence");
        }

        PptxPackage package = Presentation.Package;
        XmlDocument document = GetControlDocument();
        XmlElement? root = document.DocumentElement;
        ValidateControlRoot(root);
        if (root == null)
        {
            throw new System.ArgumentException("ActiveX control XML root is required.");
        }

        PptxPart propertyPart;
        PptxRelationship propertyRelationship;
        if (PropertyPart != null && PropertyRelationship != null)
        {
            propertyPart = PropertyPart;
            propertyRelationship = PropertyRelationship;
            propertyPart.SetBytes(bytes);
            propertyPart.SetContentType(cleanContentType);
            package.EnsureContentTypeOverride(propertyPart.PartName, propertyPart.ContentType);
        }
        else
        {
            propertyPart = package.AddPart(AllocateBinaryPartName(package), cleanContentType, bytes);
            string target = PptxSlideCloner.BuildRelativeTargetPath(ControlPart.PartName, propertyPart.PartName);
            propertyRelationship = package.AddRelationship(ControlPart.PartName, PptxRelationshipTypes.ActiveXControlBinary, target, PptxRelationshipTargetMode.Internal);
        }

        RemoveInlineProperties(root);
        PptxXml.SetAttributeValue(root, "r", PptxNamespaces.Relationships, "id", propertyRelationship.Id);
        SetActiveXAttribute(root, "persistence", cleanPersistence);
        ControlPart.SetXmlDocument(document);
        Load();
    }

    internal void ReplaceProperties(List<PptxActiveXProperty> properties)
    {
        if (properties == null)
        {
            throw new System.ArgumentNullException("properties");
        }

        if (ControlPart == null)
        {
            throw new System.ArgumentException("ActiveX control part was not found.");
        }

        XmlDocument document = GetControlDocument();
        XmlElement? root = document.DocumentElement;
        ValidateControlRoot(root);
        if (root == null)
        {
            throw new System.ArgumentException("ActiveX control XML root is required.");
        }

        RemoveInlineProperties(root);
        for (int index = 0; index < properties.Count; index++)
        {
            PptxActiveXProperty property = properties[index];
            if (property == null)
            {
                throw new System.ArgumentException("ActiveX properties cannot contain null values.", "properties");
            }

            XmlElement element = document.CreateElement("ax", "ocxPr", PptxNamespaces.ActiveX);
            SetActiveXAttribute(element, "name", property.Name);
            SetActiveXAttribute(element, "value", property.Value);
            root.AppendChild(element);
        }

        root.RemoveAttribute("id", PptxNamespaces.Relationships);
        SetActiveXAttribute(root, "persistence", "persistPropertyBag");
        string oldPartName = PropertyPartName;
        if (PropertyRelationship != null)
        {
            Presentation.Package.RemoveRelationship(ControlPart.PartName, PropertyRelationship.Id);
        }

        ControlPart.SetXmlDocument(document);
        Presentation.RemovePartWhenUnreferenced(oldPartName);
        Load();
    }

    internal PptxResult ScanSecurity()
    {
        PptxResult result = new PptxResult();
        if (ControlRelationship != null)
        {
            PptxSecurityScannerCore.ScanRelationship(ControlRelationship, result);
        }

        if (ControlPart != null)
        {
            PptxSecurityScannerCore.ScanPart(ControlPart, result);
            PptxSecurityScannerCore.ScanRelationshipCollection(ControlPart.Relationships, result);
        }

        if (PropertyPart != null)
        {
            PptxSecurityScannerCore.ScanPart(PropertyPart, result);
        }

        return result;
    }

    internal XmlDocument GetControlDocument()
    {
        if (ControlPart == null)
        {
            throw new System.ArgumentException("ActiveX control part was not found.");
        }

        if (ControlPart.HasXmlDocument)
        {
            return ControlPart.XmlDocument;
        }

        return PptxXml.LoadBytes(ControlPart.GetBytes());
    }

    internal void LoadInlineProperties(XmlElement root)
    {
        for (int index = 0; index < root.ChildNodes.Count; index++)
        {
            XmlNode? child = root.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement element = (XmlElement)child;
            if (element.NamespaceURI != PptxNamespaces.ActiveX || element.LocalName != "ocxPr")
            {
                continue;
            }

            PptxActiveXProperty property = new PptxActiveXProperty();
            property.Name = GetActiveXAttribute(element, "name");
            property.Value = GetActiveXAttribute(element, "value");
            Properties.Add(property);
        }
    }

    internal static void RemoveInlineProperties(XmlElement root)
    {
        for (int index = root.ChildNodes.Count - 1; index >= 0; index--)
        {
            XmlNode? child = root.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement element = (XmlElement)child;
            if (element.NamespaceURI == PptxNamespaces.ActiveX && element.LocalName == "ocxPr")
            {
                root.RemoveChild(element);
            }
        }
    }

    internal static PptxRelationship? FindRelationship(PptxRelationshipCollection relationships, string relationshipId)
    {
        string clean = PptxDiagnostic.CleanText(relationshipId);
        for (int index = 0; index < relationships.Relationships.Count; index++)
        {
            if (relationships.Relationships[index].Id == clean)
            {
                return relationships.Relationships[index];
            }
        }

        return null;
    }

    internal static List<PptxActiveXControl> FromPart(PptxPresentation presentation, PptxPart ownerPart)
    {
        List<PptxActiveXControl> controls = new List<PptxActiveXControl>();
        if (!ownerPart.IsXmlContentType())
        {
            return controls;
        }

        XmlDocument document;
        if (ownerPart.HasXmlDocument)
        {
            document = ownerPart.XmlDocument;
        }
        else
        {
            document = PptxXml.LoadBytes(ownerPart.GetBytes());
        }

        XmlElement? root = document.DocumentElement;
        if (root != null)
        {
            CollectControlElements(presentation, ownerPart, root, controls);
        }

        return controls;
    }

    internal static void CollectControlElements(PptxPresentation presentation, PptxPart ownerPart, XmlNode node, List<PptxActiveXControl> controls)
    {
        for (int index = 0; index < node.ChildNodes.Count; index++)
        {
            XmlNode? child = node.ChildNodes[index];
            if (child == null || child.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            XmlElement element = (XmlElement)child;
            if (element.NamespaceURI == PptxNamespaces.Presentation && element.LocalName == "control" && element.ParentNode != null && element.ParentNode.LocalName == "controls")
            {
                controls.Add(new PptxActiveXControl(presentation, ownerPart, element));
            }

            CollectControlElements(presentation, ownerPart, element, controls);
        }
    }

    internal static string GetActiveXAttribute(XmlElement element, string localName)
    {
        string value = PptxXml.GetAttributeValue(element, PptxNamespaces.ActiveX, localName);
        if (value.Length == 0)
        {
            value = PptxXml.GetAttributeValue(element, "", localName);
        }

        return value;
    }

    internal static void SetActiveXAttribute(XmlElement element, string localName, string value)
    {
        PptxXml.SetAttributeValue(element, "ax", PptxNamespaces.ActiveX, localName, PptxDiagnostic.CleanText(value));
        element.RemoveAttribute(localName);
    }

    internal static void ValidateControlRoot(XmlElement? root)
    {
        if (root == null || root.NamespaceURI != PptxNamespaces.ActiveX || root.LocalName != "ocx")
        {
            throw new System.ArgumentException("ActiveX control XML must have an ax:ocx root element.");
        }
    }

    internal static string AllocateBinaryPartName(PptxPackage package)
    {
        int index = 1;
        string partName = "/ppt/activeX/activeX" + index.ToString() + ".bin";
        while (package.HasPart(partName))
        {
            index = index + 1;
            partName = "/ppt/activeX/activeX" + index.ToString() + ".bin";
        }

        return partName;
    }
}
