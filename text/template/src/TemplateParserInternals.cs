using System;
using System.Collections.Generic;
using System.Text;

namespace template;

internal class TemplateParserBlockResult
{
    internal bool FoundEnd;
    internal bool FoundBranch;
    internal TemplateStatementSyntax? EndStatement;
    internal TemplateStatementSyntax? BranchStatement;

    internal TemplateParserBlockResult()
    {
        FoundEnd = false;
        FoundBranch = false;
        EndStatement = null;
        BranchStatement = null;
    }
}

internal class TemplateExpressionOperatorMatch
{
    internal bool Found;
    internal int StartIndex;
    internal int EndIndex;
    internal TemplateExpressionOperatorKind OperatorKind;
    internal int ColonIndex;

    internal TemplateExpressionOperatorMatch()
    {
        Found = false;
        StartIndex = -1;
        EndIndex = -1;
        OperatorKind = TemplateExpressionOperatorKind.None;
        ColonIndex = -1;
    }
}

