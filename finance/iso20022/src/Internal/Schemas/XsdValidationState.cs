using System.Collections.Generic;
using System.Xml;

namespace iso20022;

internal class XsdValidationState
{
    internal SchemaCompilationResult Result = new SchemaCompilationResult();
    internal SchemaRegistryOptions Options = new SchemaRegistryOptions();
    internal SchemaArtifact Schema = new SchemaArtifact();
    internal XmlPathTracker Paths = new XmlPathTracker();
    internal List<int> ChildElementCounts = new List<int>();
    internal XsdDocumentLocationMap DocumentLocations = new XsdDocumentLocationMap();
    internal XmlReader? Reader;
    internal string? SourceName;
    internal int ElementCount;
    internal bool Continue = true;
    internal bool HasValidationError;
}
