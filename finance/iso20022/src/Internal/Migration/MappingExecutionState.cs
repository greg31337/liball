using System.Collections.Generic;

namespace iso20022;

internal class MappingExecutionState
{
    internal MappingRequest Request = new MappingRequest();
    internal MappingResult Result = new MappingResult();
    internal DynamicXmlNode SourceTree = new DynamicXmlNode();
    internal DynamicXmlNode TargetTree = new DynamicXmlNode();
    internal List<DynamicXmlNode> ConsumedNodes = new List<DynamicXmlNode>();
    internal bool InputWasModel;
    internal bool TargetIsModel;
}
