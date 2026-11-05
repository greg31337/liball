using System.Collections.Generic;

namespace gltf;

internal class Draft4SchemaFailure
{
    internal string Keyword = "";
    internal string InstancePointer = "";
    internal string SchemaPointer = "";
    internal string Message = "";
}

internal class Draft4SchemaValidation
{
    internal bool IsValid = true;
    internal List<Draft4SchemaFailure> Failures = new List<Draft4SchemaFailure>();
}
