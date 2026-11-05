namespace iso20022;

internal class XmlSignatureValidationState
{
    internal SignatureValidationRequest Request = new SignatureValidationRequest();
    internal SignatureValidationResult Result = new SignatureValidationResult();
    internal DynamicXmlNode Tree = new DynamicXmlNode();
    internal bool Continue = true;
}
