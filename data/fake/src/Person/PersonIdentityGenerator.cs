namespace fake;

internal static class PersonIdentityGenerator
{
    internal static string Gender(
        GenerationContext context,
        FakeRandom random)
    {
        PersonIdentityData data = PersonIdentityDataParser.Resolve(context);
        return data.Choose(random, data.Genders);
    }

    internal static PronounSet Pronouns(
        GenerationContext context,
        FakeRandom random)
    {
        PersonIdentityData data = PersonIdentityDataParser.Resolve(context);
        PersonPronounData value = data.ChoosePronouns(random);
        return new PronounSet(
            value.Subject,
            value.Object,
            value.PossessiveAdjective,
            value.PossessivePronoun,
            value.Reflexive,
            value.Display);
    }

    internal static string Biography(
        GenerationContext context,
        FakeRandom random)
    {
        PersonIdentityData data = PersonIdentityDataParser.Resolve(context);
        return data.Choose(random, data.Biographies);
    }

    internal static string ZodiacSign(
        GenerationContext context,
        FakeRandom random)
    {
        PersonIdentityData data = PersonIdentityDataParser.Resolve(context);
        return data.Choose(random, data.ZodiacSigns);
    }

    internal static string MaritalStatus(
        GenerationContext context,
        FakeRandom random)
    {
        PersonIdentityData data = PersonIdentityDataParser.Resolve(context);
        return data.Choose(random, data.MaritalStatuses);
    }

    internal static string Relationship(
        GenerationContext context,
        FakeRandom random)
    {
        PersonIdentityData data = PersonIdentityDataParser.Resolve(context);
        return data.Choose(random, data.Relationships);
    }

    internal static string HouseholdRole(
        GenerationContext context,
        FakeRandom random)
    {
        PersonIdentityData data = PersonIdentityDataParser.Resolve(context);
        return data.Choose(random, data.HouseholdRoles);
    }

    internal static string HouseholdType(
        GenerationContext context,
        FakeRandom random)
    {
        PersonIdentityData data = PersonIdentityDataParser.Resolve(context);
        return data.Choose(random, data.HouseholdTypes);
    }

    internal static string Demographic(
        GenerationContext context,
        FakeRandom random,
        string category)
    {
        PersonIdentityData data = PersonIdentityDataParser.Resolve(context);
        return data.ChooseDemographic(random, category);
    }

    internal static string[] DemographicCategories(GenerationContext context)
    {
        return PersonIdentityDataParser.Resolve(context).DemographicCategories();
    }
}
