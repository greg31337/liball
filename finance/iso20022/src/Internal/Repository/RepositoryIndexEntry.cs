using System.Collections.Generic;

namespace iso20022;

internal class RepositoryIndexEntry
{
    internal string Identifier = string.Empty;
    internal string Name = string.Empty;
    internal RepositoryEntityKind Kind;
    internal bool HasRegistrationStatus;
    internal RegistrationStatus RegistrationStatus;
    internal string RegistrationStatusValue = string.Empty;
    internal string BusinessAreaCode = string.Empty;
    internal string MessageIdentifier = string.Empty;
    internal string MessageFunctionality = string.Empty;
    internal string Variant = string.Empty;
    internal string NamespaceUri = string.Empty;
    internal List<string> NamespaceUris = new List<string>();
    internal string Version = string.Empty;
    internal List<string> MessageSetIdentifiers = new List<string>();
    internal int Order;
    internal SourceLocation Location = new SourceLocation();
    internal object? Value;
}
