using System.Collections.Generic;

namespace iso20022;

internal class MigrationExecutionState
{
    internal MigrationRequest Request = new MigrationRequest();
    internal MigrationResult Result = new MigrationResult();
    internal DynamicXmlNode Tree = new DynamicXmlNode();
    internal List<DynamicXmlNode> UnmappedNodes = new List<DynamicXmlNode>();
    internal bool InputWasModel;
    internal bool RequiredStepFailed;
}
