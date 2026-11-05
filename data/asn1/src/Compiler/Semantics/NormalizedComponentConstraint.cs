namespace asn1;

internal class NormalizedComponentConstraint
{
    internal string Name = "";
    internal ResolvedComponentSyntax? Component;
    internal ResolvedTypeSyntax? ComponentType;
    internal ComponentPresenceConstraintKind Presence;
    internal NormalizedConstraintGraph? ValueConstraint;
    internal ParsedNamedConstraintSyntax? SourceSyntax;
    internal bool Implicit;
}
