namespace odf;

internal static class NoteNavigator
{
    internal static OdfNote? FindNote(OdfDocument document, string id)
    {
        RequireDocument(document);
        OdfNote? result = null;
        int count = 0;
        int index = 0;
        while (index < document.Notes.Length)
        {
            OdfNote note = document.Notes[index];
            if (note.Id == id)
            {
                result = note;
                count = count + 1;
            }
            index = index + 1;
        }
        return count == 1 ? result : null;
    }

    internal static OdfNoteConfiguration? FindConfiguration(
        OdfDocument document,
        OdfNoteKind kind)
    {
        RequireDocument(document);
        OdfNoteConfiguration? result = null;
        int count = 0;
        int index = 0;
        while (index < document.NoteConfigurations.Length)
        {
            OdfNoteConfiguration configuration =
                document.NoteConfigurations[index];
            if (configuration.Kind == kind)
            {
                result = configuration;
                count = count + 1;
            }
            index = index + 1;
        }
        return count == 1 ? result : null;
    }

    internal static void RenameNoteId(
        OdfDocument document,
        OdfNote note,
        string newId)
    {
        RequireDocument(document);
        if (note == null || newId == null || newId.Length == 0)
        {
            throw new OdfEditException(
                "A note and non-empty note identifier are required.");
        }
        if (!ContainsNote(document.Notes, note))
        {
            throw new OdfEditException(
                "The note does not belong to the document.");
        }
        if (HasIdConflict(document.Notes, note, newId))
        {
            throw new OdfEditException(
                "A note with the requested identifier already exists.");
        }
        string oldId = note.Id;
        note.Id = newId;
        int index = 0;
        while (index < document.TextFields.Length)
        {
            OdfTextField field = document.TextFields[index];
            if (field.ElementName == "note-ref"
                && field.ReferenceName == oldId)
            {
                field.ReferenceName = newId;
            }
            index = index + 1;
        }
        NoteDocumentModel.ResolveNoteRelationships(document);
    }

    static bool ContainsNote(OdfNote[] notes, OdfNote target)
    {
        int index = 0;
        while (index < notes.Length)
        {
            if (notes[index] == target)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static bool HasIdConflict(
        OdfNote[] notes,
        OdfNote target,
        string id)
    {
        int index = 0;
        while (index < notes.Length)
        {
            OdfNote note = notes[index];
            if (note != target && note.Id == id)
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
                "A document is required for note navigation.");
        }
    }
}
