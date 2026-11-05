namespace asn1;

internal class ParameterInstantiationSchemaValidator
{
    internal DiagnosticBag Diagnostics;

    internal ParameterInstantiationSchemaValidator(DiagnosticBag diagnostics)
    {
        this.Diagnostics = diagnostics == null
            ? throw new System.ArgumentNullException("diagnostics")
            : diagnostics;
    }

    internal bool ValidateTables(SymbolTable[] tables)
    {
        bool success = true;
        int tableIndex = 0;
        while (tableIndex < tables.Length)
        {
            int symbolIndex = 0;
            while (symbolIndex < tables[tableIndex].Symbols.Length)
            {
                SymbolTableEntry symbol = tables[tableIndex].Symbols[symbolIndex];
                if (!symbol.Parameterized
                    && symbol.SemanticDeclarationTokens.Length > 0
                    && symbol.ParameterizedReferences.Length > 0
                    && !this.IsResolvedSchema(symbol))
                {
                    this.AddInvalidSchemaDiagnostic(symbol);
                    success = false;
                }

                symbolIndex = symbolIndex + 1;
            }

            tableIndex = tableIndex + 1;
        }

        return success;
    }

    internal bool IsResolvedSchema(SymbolTableEntry symbol)
    {
        switch (symbol.Kind)
        {
            case AsnAssignmentKind.Type:
                return symbol.ResolvedType != null && symbol.ResolvedType.Success;
            case AsnAssignmentKind.Value:
                return symbol.ResolvedValue != null && symbol.ResolvedValue.Success;
            case AsnAssignmentKind.ValueSet:
                return symbol.ValueSetResolutionState == 2
                    && symbol.ResolvedValueSetType != null;
            case AsnAssignmentKind.ObjectClass:
                return symbol.ObjectClassResolutionState == 2
                    && symbol.ResolvedObjectClassSyntax != null;
            case AsnAssignmentKind.Object:
                return symbol.ObjectResolutionState == 2
                    && symbol.DeclaredObjectSyntax != null;
            case AsnAssignmentKind.ObjectSet:
                return symbol.ObjectSetResolutionState == 2
                    && symbol.DeclaredObjectSetSyntax != null;
            default:
                return false;
        }
    }

    internal void AddInvalidSchemaDiagnostic(SymbolTableEntry symbol)
    {
        ParsedParameterizedReferenceSyntax reference =
            symbol.ParameterizedReferences[0];
        this.Diagnostics.TryAddDiagnostic(
            AsnDiagnosticSeverity.Error,
            AsnDiagnosticCategory.Binding,
            ActualParameterDiagnosticCodes.PostSubstitutionInvalidSchema,
            "Instantiation of the parameterized reference '"
                + this.FormatReference(reference)
                + "' produced an invalid " + this.FormatKind(symbol.Kind)
                + " schema for '" + symbol.QualifiedName + "'.",
            reference.Span == null ? symbol.Span : reference.Span,
            null);
    }

    internal string FormatReference(ParsedParameterizedReferenceSyntax reference)
    {
        return reference.ReferenceModule.Length == 0
            ? reference.ReferenceName
            : reference.ReferenceModule + "." + reference.ReferenceName;
    }

    internal string FormatKind(AsnAssignmentKind kind)
    {
        switch (kind)
        {
            case AsnAssignmentKind.Type:
                return "type";
            case AsnAssignmentKind.Value:
                return "value";
            case AsnAssignmentKind.ValueSet:
                return "value-set";
            case AsnAssignmentKind.ObjectClass:
                return "object-class";
            case AsnAssignmentKind.Object:
                return "object";
            case AsnAssignmentKind.ObjectSet:
                return "object-set";
            default:
                return "assignment";
        }
    }
}
