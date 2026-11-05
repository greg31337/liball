using System;
using System.Xml;

namespace iso20022;

internal class ResourceLimitGuard
{
    ResourceLimits limits;
    int elementCount;
    int schemaCount;
    int diagnosticCount;

    internal ResourceLimitGuard(ResourceLimits limits)
    {
        if (limits == null)
        {
            throw new ArgumentNullException("limits");
        }

        ValidatePositive(limits.MaxDepth, "MaxDepth");
        ValidatePositive(limits.MaxAttributesPerElement, "MaxAttributesPerElement");
        ValidatePositive(limits.MaxTextLength, "MaxTextLength");
        ValidatePositive(limits.MaxElementCount, "MaxElementCount");
        ValidatePositive(limits.MaxCollectionCount, "MaxCollectionCount");
        ValidatePositive(limits.MaxTotalBytes, "MaxTotalBytes");
        ValidatePositive(limits.MaxSchemaCount, "MaxSchemaCount");
        ValidatePositive(limits.MaxDiagnosticCount, "MaxDiagnosticCount");
        this.limits = new ResourceLimits();
        this.limits.MaxDepth = limits.MaxDepth;
        this.limits.MaxAttributesPerElement = limits.MaxAttributesPerElement;
        this.limits.MaxTextLength = limits.MaxTextLength;
        this.limits.MaxElementCount = limits.MaxElementCount;
        this.limits.MaxCollectionCount = limits.MaxCollectionCount;
        this.limits.MaxTotalBytes = limits.MaxTotalBytes;
        this.limits.MaxSchemaCount = limits.MaxSchemaCount;
        this.limits.MaxDiagnosticCount = limits.MaxDiagnosticCount;
    }

    internal void EnsureInputByteCount(long byteCount)
    {
        if (byteCount < 0 || byteCount > limits.MaxTotalBytes)
        {
            throw new XmlException("The XML input exceeds MaxTotalBytes.");
        }
    }

    internal void RecordElement(int depth, int attributeCount)
    {
        if (depth > limits.MaxDepth)
        {
            throw new XmlException("The XML element depth exceeds MaxDepth.");
        }

        if (attributeCount > limits.MaxAttributesPerElement)
        {
            throw new XmlException("An XML element exceeds MaxAttributesPerElement.");
        }

        elementCount++;
        if (elementCount > limits.MaxElementCount)
        {
            throw new XmlException("The XML document exceeds MaxElementCount.");
        }
    }

    internal void EnsureTextLength(int textLength)
    {
        if (textLength > limits.MaxTextLength)
        {
            throw new XmlException("An XML value exceeds MaxTextLength.");
        }
    }

    internal void EnsureCollectionCount(int itemCount)
    {
        if (itemCount > limits.MaxCollectionCount)
        {
            throw new XmlException("An XML child collection exceeds MaxCollectionCount.");
        }
    }

    internal void RecordSchema()
    {
        schemaCount++;
        if (schemaCount > limits.MaxSchemaCount)
        {
            throw new XmlException("The schema collection exceeds MaxSchemaCount.");
        }
    }

    internal void RecordDiagnostic()
    {
        diagnosticCount++;
        if (diagnosticCount > limits.MaxDiagnosticCount)
        {
            throw new XmlException("The diagnostic collection exceeds MaxDiagnosticCount.");
        }
    }

    internal static void ValidatePositive(int value, string name)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException("limits", name + " must be positive.");
        }
    }
}
