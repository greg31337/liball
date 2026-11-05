namespace ttf;

internal enum SfntEmbeddingPermission
{
    Installable = 0,
    Restricted = 1,
    PreviewAndPrint = 2,
    Editable = 3
}

internal enum SfntEmbeddingRecommendedAction
{
    EmbedSubsetOrFullFont = 0,
    EmbedFullFont = 1,
    EmbedBitmapDataOnly = 2,
    EmbedCompleteBitmapDataOnly = 3,
    DoNotEmbedWithoutExplicitPermission = 4
}

internal class SfntEmbeddingRightsReport
{
    int tableVersion;
    int rawFlags;
    SfntEmbeddingPermission permission;
    int declaredPermissionCount;
    bool legacyLeastRestrictivePrecedence;
    int ignoredFlags;
    bool noSubsetting;
    bool bitmapOnly;
    SfntEmbeddingRecommendedAction recommendedAction;

    internal SfntEmbeddingRightsReport(
        int tableVersion,
        int rawFlags,
        SfntEmbeddingPermission permission,
        int declaredPermissionCount,
        bool legacyLeastRestrictivePrecedence,
        int ignoredFlags,
        bool noSubsetting,
        bool bitmapOnly,
        SfntEmbeddingRecommendedAction recommendedAction)
    {
        this.tableVersion = tableVersion;
        this.rawFlags = rawFlags;
        this.permission = permission;
        this.declaredPermissionCount = declaredPermissionCount;
        this.legacyLeastRestrictivePrecedence = legacyLeastRestrictivePrecedence;
        this.ignoredFlags = ignoredFlags;
        this.noSubsetting = noSubsetting;
        this.bitmapOnly = bitmapOnly;
        this.recommendedAction = recommendedAction;
    }

    internal int GetTableVersion()
    {
        return tableVersion;
    }

    internal int GetRawFlags()
    {
        return rawFlags;
    }

    internal int GetPermissionFlags()
    {
        return rawFlags & 0x000E;
    }

    internal SfntEmbeddingPermission GetPermission()
    {
        return permission;
    }

    internal bool DeclaresRestrictedPermission()
    {
        return (rawFlags & 2) != 0;
    }

    internal bool DeclaresPreviewAndPrintPermission()
    {
        return (rawFlags & 4) != 0;
    }

    internal bool DeclaresEditablePermission()
    {
        return (rawFlags & 8) != 0;
    }

    internal int GetDeclaredPermissionCount()
    {
        return declaredPermissionCount;
    }

    internal bool UsesLegacyLeastRestrictivePrecedence()
    {
        return legacyLeastRestrictivePrecedence;
    }

    internal int GetIgnoredFlags()
    {
        return ignoredFlags;
    }

    internal bool AllowsEmbedding()
    {
        return permission != SfntEmbeddingPermission.Restricted;
    }

    internal bool AllowsPermanentInstallation()
    {
        return permission == SfntEmbeddingPermission.Installable;
    }

    internal bool AllowsDocumentEditing()
    {
        return permission == SfntEmbeddingPermission.Installable ||
            permission == SfntEmbeddingPermission.Editable;
    }

    internal bool RequiresReadOnlyDocuments()
    {
        return permission == SfntEmbeddingPermission.PreviewAndPrint;
    }

    internal bool RequiresTemporaryLoading()
    {
        return permission == SfntEmbeddingPermission.PreviewAndPrint ||
            permission == SfntEmbeddingPermission.Editable;
    }

    internal bool AllowsSubsetting()
    {
        return AllowsEmbedding() && !noSubsetting;
    }

    internal bool RequiresFullEmbedding()
    {
        return noSubsetting;
    }

    internal bool AllowsOutlineEmbedding()
    {
        return AllowsEmbedding() && !bitmapOnly;
    }

    internal bool AllowsBitmapEmbedding()
    {
        return AllowsEmbedding();
    }

    internal bool RequiresBitmapOnlyEmbedding()
    {
        return bitmapOnly;
    }

    internal SfntEmbeddingRecommendedAction GetRecommendedAction()
    {
        return recommendedAction;
    }

    internal int GetRestrictionDiagnosticCount()
    {
        int count = 0;

        if (!AllowsEmbedding())
        {
            count += 1;
        }

        if (noSubsetting)
        {
            count += 1;
        }

        if (bitmapOnly)
        {
            count += 1;
        }

        return count;
    }

    internal FontDiagnosticCode GetRestrictionDiagnosticCode(int index)
    {
        if (index < 0)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        int current = 0;

        if (!AllowsEmbedding())
        {
            if (current == index)
            {
                return FontDiagnosticCode.EmbeddingRestricted;
            }

            current += 1;
        }

        if (noSubsetting)
        {
            if (current == index)
            {
                return FontDiagnosticCode.SubsettingRestricted;
            }

            current += 1;
        }

        if (bitmapOnly)
        {
            if (current == index)
            {
                return FontDiagnosticCode.BitmapEmbeddingOnly;
            }

            current += 1;
        }

        throw new System.ArgumentOutOfRangeException("index");
    }
}

internal static class SfntEmbeddingRightsInterpreter
{
    internal static SfntEmbeddingRightsReport Interpret(
        int tableVersion,
        int rawFlags)
    {
        if (tableVersion < 0)
        {
            throw new System.ArgumentOutOfRangeException("tableVersion");
        }

        if (rawFlags < 0 || rawFlags > 65535)
        {
            throw new System.ArgumentOutOfRangeException("rawFlags");
        }

        bool restricted = (rawFlags & 2) != 0;
        bool previewAndPrint = (rawFlags & 4) != 0;
        bool editable = (rawFlags & 8) != 0;
        int declaredPermissionCount = 0;

        if (restricted)
        {
            declaredPermissionCount += 1;
        }

        if (previewAndPrint)
        {
            declaredPermissionCount += 1;
        }

        if (editable)
        {
            declaredPermissionCount += 1;
        }

        if ((rawFlags & 1) != 0)
        {
            throw FontFormatException.Create("Reserved OS/2 embedding bit 0 is set.");
        }

        if (tableVersion >= 3 && declaredPermissionCount > 1)
        {
            throw FontFormatException.Create(
                "The OS/2 embedding permission flags are not mutually exclusive.");
        }

        SfntEmbeddingPermission permission = SfntEmbeddingPermission.Installable;

        if (editable)
        {
            permission = SfntEmbeddingPermission.Editable;
        }
        else if (previewAndPrint)
        {
            permission = SfntEmbeddingPermission.PreviewAndPrint;
        }
        else if (restricted)
        {
            permission = SfntEmbeddingPermission.Restricted;
        }

        bool legacyLeastRestrictivePrecedence = tableVersion <= 2 &&
            declaredPermissionCount > 1;
        int ignoredFlags = tableVersion <= 1 ? rawFlags & 0xFFF0 : 0;
        bool noSubsetting = tableVersion >= 2 && (rawFlags & 0x0100) != 0;
        bool bitmapOnly = tableVersion >= 2 && (rawFlags & 0x0200) != 0;
        SfntEmbeddingRecommendedAction recommendedAction = GetRecommendedAction(
            permission,
            noSubsetting,
            bitmapOnly);

        return new SfntEmbeddingRightsReport(
            tableVersion,
            rawFlags,
            permission,
            declaredPermissionCount,
            legacyLeastRestrictivePrecedence,
            ignoredFlags,
            noSubsetting,
            bitmapOnly,
            recommendedAction);
    }

    static SfntEmbeddingRecommendedAction GetRecommendedAction(
        SfntEmbeddingPermission permission,
        bool noSubsetting,
        bool bitmapOnly)
    {
        if (permission == SfntEmbeddingPermission.Restricted)
        {
            return SfntEmbeddingRecommendedAction.DoNotEmbedWithoutExplicitPermission;
        }

        if (bitmapOnly && noSubsetting)
        {
            return SfntEmbeddingRecommendedAction.EmbedCompleteBitmapDataOnly;
        }

        if (bitmapOnly)
        {
            return SfntEmbeddingRecommendedAction.EmbedBitmapDataOnly;
        }

        if (noSubsetting)
        {
            return SfntEmbeddingRecommendedAction.EmbedFullFont;
        }

        return SfntEmbeddingRecommendedAction.EmbedSubsetOrFullFont;
    }
}
