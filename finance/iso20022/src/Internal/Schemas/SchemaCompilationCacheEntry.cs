using System.Collections.Generic;
using System.Xml.Schema;

namespace iso20022;

internal class SchemaCompilationCacheEntry
{
    internal XmlSchemaSet CompiledSchemas = new XmlSchemaSet();
    internal List<string> SchemaKeys = new List<string>();
}
