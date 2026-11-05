using System.Collections.Generic;

namespace odf;

internal static class TextFieldNavigator
{
    internal static OdfTextField[] FindFields(
        OdfDocument document,
        OdfTextFieldKind kind)
    {
        RequireDocument(document);
        List<OdfTextField> result = new List<OdfTextField>();
        int index = 0;
        while (index < document.TextFields.Length)
        {
            if (document.TextFields[index].Kind == kind)
            {
                result.Add(document.TextFields[index]);
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    internal static OdfTextFieldDeclaration? FindDeclaration(
        OdfDocument document,
        OdfTextFieldDeclarationKind kind,
        string name)
    {
        RequireDocument(document);
        OdfTextFieldDeclaration? result = null;
        int count = 0;
        int index = 0;
        while (index < document.TextFieldDeclarations.Length)
        {
            OdfTextFieldDeclaration declaration =
                document.TextFieldDeclarations[index];
            if (declaration.Kind == kind && declaration.Name == name)
            {
                result = declaration;
                count = count + 1;
            }
            index = index + 1;
        }
        return count == 1 ? result : null;
    }

    internal static void RenameDeclaration(
        OdfDocument document,
        OdfTextFieldDeclaration declaration,
        string newName)
    {
        RequireDocument(document);
        if (declaration == null || newName == null || newName.Length == 0)
        {
            throw new OdfEditException(
                "A declaration and non-empty new name are required.");
        }
        if (!ContainsDeclaration(
            document.TextFieldDeclarations,
            declaration))
        {
            throw new OdfEditException(
                "The declaration does not belong to the document.");
        }
        if (HasDeclarationConflict(
            document.TextFieldDeclarations,
            declaration,
            newName))
        {
            throw new OdfEditException(
                "A declaration with the requested name already exists.");
        }
        string oldName = declaration.Name;
        declaration.Name = newName;
        int index = 0;
        while (index < document.TextFields.Length)
        {
            OdfTextField field = document.TextFields[index];
            if (field.Declaration == declaration && field.Name == oldName)
            {
                field.Name = newName;
            }
            index = index + 1;
        }
        TextFieldDocumentModel.ResolveDeclarations(document);
    }

    static bool ContainsDeclaration(
        OdfTextFieldDeclaration[] declarations,
        OdfTextFieldDeclaration target)
    {
        int index = 0;
        while (index < declarations.Length)
        {
            if (declarations[index] == target)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static bool HasDeclarationConflict(
        OdfTextFieldDeclaration[] declarations,
        OdfTextFieldDeclaration target,
        string name)
    {
        int index = 0;
        while (index < declarations.Length)
        {
            OdfTextFieldDeclaration declaration = declarations[index];
            if (declaration != target
                && declaration.Kind == target.Kind
                && declaration.Name == name)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static void RequireDocument(OdfDocument document)
    {
        if (document == null)
        {
            throw new OdfDocumentException(
                "A document is required for text-field navigation.");
        }
    }
}
