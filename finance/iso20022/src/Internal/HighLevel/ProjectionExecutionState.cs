using System.Collections.Generic;

namespace iso20022;

internal class ProjectionExecutionState
{
    internal ProjectionRequest Request = new ProjectionRequest();
    internal ProjectionResult Result = new ProjectionResult();
    internal SemanticTemplateDefinition Definition = new SemanticTemplateDefinition();
    internal MessageDocument Document = new MessageDocument();
    internal DynamicXmlNode Tree = new DynamicXmlNode();
    internal List<ProjectionNodeMatch> Matches = new List<ProjectionNodeMatch>();
    internal List<DynamicXmlNode> ConsumedNodes = new List<DynamicXmlNode>();
}
