namespace ttf;

internal static class PdfFontDescriptorFlagsBuilder
{
    internal static PdfFontDescriptorFlagsReport Build(
        FontInstance instance)
    {
        if (instance == null)
        {
            throw new System.ArgumentNullException("instance");
        }

        FontFace face = instance.GetFace();
        SfntOs2Table? os2 = face.GetOs2Table();
        SfntPostTable? post = face.GetPostTable();
        SfntCmapMapping? cmap = face.GetCmapMapping();
        bool hasOs2 = os2 != null;
        bool hasPost = post != null;
        bool hasCmap = cmap != null;
        bool panoseMonospace = os2 != null && IsPanoseMonospace(os2);
        bool fixedPitch = post != null && post.IsFixedPitch() ||
            panoseMonospace;
        bool familySerif = os2 != null && IsSerifFamily(os2.GetFamilyClass());
        bool panoseSerif = os2 != null && IsPanoseSerif(os2);
        bool serif = familySerif || panoseSerif;
        bool familyScript = os2 != null && os2.GetFamilyClass() == 10;
        bool panoseScript = os2 != null && os2.GetPanoseValue(0) == 3;
        bool script = familyScript || panoseScript;
        bool os2Symbolic = os2 != null && os2.GetFamilyClass() == 12;
        bool cmapSymbolic = cmap != null && cmap.IsSymbolMapping();
        bool symbolic = os2Symbolic || cmapSymbolic;
        bool os2Italic = os2 != null &&
            (os2.HasSelectionFlag(0) ||
                (os2.GetVersion() >= 4 && os2.HasSelectionFlag(9)));
        bool postItalic = post != null && post.GetItalicAngleRaw() != 0;
        bool italic = os2Italic || postItalic;
        bool allCap = cmap != null && !symbolic &&
            HasUppercaseWithoutLowercase(cmap);
        bool forceBold = os2 != null &&
            (os2.HasSelectionFlag(5) || os2.GetWeightClass() >= 700);
        System.Collections.Generic.List<PdfFontDescriptorFlagInference>
            inferences =
                new System.Collections.Generic.List<
                    PdfFontDescriptorFlagInference>();

        AddInference(
            inferences,
            PdfFontDescriptorFlag.FixedPitch,
            fixedPitch,
            SelectFixedPitchEvidence(hasPost, hasOs2),
            fixedPitch ?
                "Fixed pitch is declared by post or OS/2 PANOSE." :
                "Available post and OS/2 data do not declare fixed pitch.");
        AddInference(
            inferences,
            PdfFontDescriptorFlag.Serif,
            serif,
            SelectFamilyEvidence(hasOs2, familySerif, panoseSerif),
            serif ?
                "OS/2 family or PANOSE data identifies serif forms." :
                "OS/2 family and PANOSE data do not identify serif forms.");
        AddInference(
            inferences,
            PdfFontDescriptorFlag.Symbolic,
            symbolic,
            SelectSymbolEvidence(hasOs2, hasCmap),
            symbolic ?
                "OS/2 family or cmap data identifies symbolic encoding." :
                "OS/2 family and cmap data do not identify symbolic encoding.");
        AddInference(
            inferences,
            PdfFontDescriptorFlag.Script,
            script,
            SelectFamilyEvidence(hasOs2, familyScript, panoseScript),
            script ?
                "OS/2 family or PANOSE data identifies script forms." :
                "OS/2 family and PANOSE data do not identify script forms.");
        AddInference(
            inferences,
            PdfFontDescriptorFlag.Nonsymbolic,
            !symbolic,
            SelectSymbolEvidence(hasOs2, hasCmap),
            symbolic ?
                "Nonsymbolic is clear because Symbolic is set." :
                "Nonsymbolic is set because no symbolic evidence was found.");
        AddInference(
            inferences,
            PdfFontDescriptorFlag.Italic,
            italic,
            SelectItalicEvidence(hasOs2, hasPost),
            italic ?
                "OS/2 selection or post angle data identifies slanted glyphs." :
                "OS/2 selection and post angle data do not identify slant.");
        AddInference(
            inferences,
            PdfFontDescriptorFlag.AllCap,
            allCap,
            hasCmap ?
                PdfFontDescriptorFlagEvidence.CmapLatinCoverage :
                PdfFontDescriptorFlagEvidence.Unavailable,
            allCap ?
                "The cmap covers uppercase Latin but no lowercase Latin." :
                "The cmap does not establish an uppercase-only Latin font.");
        AddInference(
            inferences,
            PdfFontDescriptorFlag.SmallCap,
            false,
            PdfFontDescriptorFlagEvidence.ConservativeDefault,
            "OS/2, post, and cmap do not reliably identify small-cap forms.");
        AddInference(
            inferences,
            PdfFontDescriptorFlag.ForceBold,
            forceBold,
            SelectBoldEvidence(os2),
            forceBold ?
                "OS/2 selection or weight data identifies a bold font." :
                "OS/2 selection and weight data do not identify bold weight.");

        int flags = 0;
        int index = 0;

        while (index < inferences.Count)
        {
            PdfFontDescriptorFlagInference inference = inferences[index];

            if (inference.IsSet())
            {
                flags |= (int)inference.GetFlag();
            }

            index += 1;
        }

        return new PdfFontDescriptorFlagsReport(
            flags,
            inferences.ToArray());
    }

    static void AddInference(
        System.Collections.Generic.List<PdfFontDescriptorFlagInference>
            inferences,
        PdfFontDescriptorFlag flag,
        bool set,
        PdfFontDescriptorFlagEvidence evidence,
        string explanation)
    {
        inferences.Add(new PdfFontDescriptorFlagInference(
            flag,
            set,
            evidence,
            explanation));
    }

    static bool IsSerifFamily(int familyClass)
    {
        return familyClass >= 1 && familyClass <= 5 || familyClass == 7;
    }

    static bool IsPanoseSerif(SfntOs2Table? os2)
    {
        System.ArgumentNullException.ThrowIfNull(os2);
        int serifStyle = os2.GetPanoseValue(1);
        return os2.GetPanoseValue(0) == 2 &&
            serifStyle >= 2 && serifStyle <= 10;
    }

    static bool IsPanoseMonospace(SfntOs2Table? os2)
    {
        System.ArgumentNullException.ThrowIfNull(os2);
        return os2.GetPanoseValue(0) == 2 &&
            os2.GetPanoseValue(3) == 9;
    }

    static bool HasUppercaseWithoutLowercase(SfntCmapMapping? cmap)
    {
        bool hasUppercase = false;
        bool hasLowercase = false;
        int index = 0;

        while (index < 26)
        {
            System.ArgumentNullException.ThrowIfNull(cmap);
            if (cmap.MapUnicode(0x41 + index) != 0)
            {
                hasUppercase = true;
            }

            if (cmap.MapUnicode(0x61 + index) != 0)
            {
                hasLowercase = true;
            }

            index += 1;
        }

        return hasUppercase && !hasLowercase;
    }

    static PdfFontDescriptorFlagEvidence SelectFixedPitchEvidence(
        bool hasPost,
        bool hasOs2)
    {
        if (hasPost && hasOs2)
        {
            return PdfFontDescriptorFlagEvidence.Multiple;
        }

        if (hasPost)
        {
            return PdfFontDescriptorFlagEvidence.PostFixedPitch;
        }

        if (hasOs2)
        {
            return PdfFontDescriptorFlagEvidence.Os2Panose;
        }

        return PdfFontDescriptorFlagEvidence.Unavailable;
    }

    static PdfFontDescriptorFlagEvidence SelectFamilyEvidence(
        bool hasOs2,
        bool familyEvidence,
        bool panoseEvidence)
    {
        if (!hasOs2)
        {
            return PdfFontDescriptorFlagEvidence.Unavailable;
        }

        if (familyEvidence && panoseEvidence)
        {
            return PdfFontDescriptorFlagEvidence.Multiple;
        }

        return panoseEvidence ?
            PdfFontDescriptorFlagEvidence.Os2Panose :
            PdfFontDescriptorFlagEvidence.Os2FamilyClass;
    }

    static PdfFontDescriptorFlagEvidence SelectSymbolEvidence(
        bool hasOs2,
        bool hasCmap)
    {
        if (hasOs2 && hasCmap)
        {
            return PdfFontDescriptorFlagEvidence.Multiple;
        }

        if (hasCmap)
        {
            return PdfFontDescriptorFlagEvidence.CmapSymbolEncoding;
        }

        if (hasOs2)
        {
            return PdfFontDescriptorFlagEvidence.Os2FamilyClass;
        }

        return PdfFontDescriptorFlagEvidence.Unavailable;
    }

    static PdfFontDescriptorFlagEvidence SelectItalicEvidence(
        bool hasOs2,
        bool hasPost)
    {
        if (hasOs2 && hasPost)
        {
            return PdfFontDescriptorFlagEvidence.Multiple;
        }

        if (hasPost)
        {
            return PdfFontDescriptorFlagEvidence.PostItalicAngle;
        }

        if (hasOs2)
        {
            return PdfFontDescriptorFlagEvidence.Os2Selection;
        }

        return PdfFontDescriptorFlagEvidence.Unavailable;
    }

    static PdfFontDescriptorFlagEvidence SelectBoldEvidence(
        SfntOs2Table? os2)
    {
        if (os2 == null)
        {
            return PdfFontDescriptorFlagEvidence.Unavailable;
        }

        bool selection = os2.HasSelectionFlag(5);
        bool weight = os2.GetWeightClass() >= 700;

        if (selection && weight)
        {
            return PdfFontDescriptorFlagEvidence.Multiple;
        }

        return selection ?
            PdfFontDescriptorFlagEvidence.Os2Selection :
            PdfFontDescriptorFlagEvidence.Os2WeightClass;
    }
}
