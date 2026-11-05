using System.Xml.Schema;

namespace iso20022;

internal class SchemaCompilationUnit
{
    internal SchemaArtifact Artifact = new SchemaArtifact();
    internal XmlSchema? Schema;
}
