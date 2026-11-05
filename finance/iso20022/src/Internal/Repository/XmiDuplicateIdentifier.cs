namespace iso20022;

internal class XmiDuplicateIdentifier
{
    internal string Identifier = string.Empty;
    internal XmiPassOneEntry FirstEntry = new XmiPassOneEntry();
    internal XmiPassOneEntry DuplicateEntry = new XmiPassOneEntry();
    internal string ErrorCode = "repository.xmi.id_duplicate";
    internal string ErrorMessage = "The XMI identifier is declared by more than one repository entity.";
}
