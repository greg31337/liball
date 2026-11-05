namespace iso20022;

internal class SupplementaryDiscoveryState
{
    internal SupplementaryValidationRequest Request;
    internal SupplementaryValidationResult Result;
    internal DynamicXmlNode Tree;
    internal int ElementCount;
    internal bool Continue = true;

    internal SupplementaryDiscoveryState(
        SupplementaryValidationRequest request,
        SupplementaryValidationResult result,
        DynamicXmlNode tree)
    {
        Request = request;
        Result = result;
        Tree = tree;
    }
}
