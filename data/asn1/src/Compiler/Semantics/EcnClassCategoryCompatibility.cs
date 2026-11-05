namespace asn1;

internal static class EcnClassCategoryCompatibility
{
    internal static bool IsCategoryCompatible(
        EcnClassCategory actual,
        EcnClassCategory required)
    {
        return actual == required;
    }

    internal static bool IsGroupCompatible(
        EcnClassCategory actual,
        EcnClassCategoryGroup required)
    {
        return EcnBuiltInClassCatalog.IsInGroup(actual, required);
    }

    internal static bool CanDeriveAlias(EcnClassCategory category)
    {
        return category != EcnClassCategory.Unknown
            && category != EcnClassCategory.EncodingProcedure;
    }
}
