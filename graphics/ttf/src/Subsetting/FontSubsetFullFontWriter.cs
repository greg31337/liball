namespace ttf;

internal static class FontSubsetFullFontWriter
{
    internal static byte[] Write(FontInstance instance)
    {
        if (instance == null)
        {
            throw new System.ArgumentNullException("instance");
        }

        FontFace face = instance.GetFace();
        FontOutlineKind kind = face.GetOutlineKind();
        bool requiresStaticMaterialization = kind == FontOutlineKind.Cff2 ||
            (kind == FontOutlineKind.TrueType &&
                face.HasTable(FontTags.Fvar));

        if (requiresStaticMaterialization)
        {
            return instance.CopyStaticSfntBytes();
        }

        return SfntFaceWriter.Write(
            face,
            face.GetTableCache().GetLimits());
    }
}
