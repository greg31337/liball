namespace fake;

internal static class PersonWorkGenerator
{
    internal static PersonWorkProfile Record(
        GenerationContext context,
        FakeRandom random)
    {
        PersonWorkData data = PersonWorkDataParser.Resolve(context);
        PersonWorkProfile result = new PersonWorkProfile();
        result.Title = data.Choose(random, data.Titles);
        result.Descriptor = data.Choose(random, data.Descriptors);
        result.Area = data.Choose(random, data.Areas);
        result.Type = data.Choose(random, data.Types);
        result.Seniority = data.Choose(random, data.Seniorities);
        result.Department = data.Choose(random, data.Departments);
        result.Status = data.Choose(random, data.Statuses);
        result.SalaryBand = data.Choose(random, data.SalaryBands);
        return result;
    }

    internal static string JobTitle(
        GenerationContext context,
        FakeRandom random)
    {
        PersonWorkData data = PersonWorkDataParser.Resolve(context);
        return data.Choose(random, data.Titles);
    }

    internal static string JobDescriptor(
        GenerationContext context,
        FakeRandom random)
    {
        PersonWorkData data = PersonWorkDataParser.Resolve(context);
        return data.Choose(random, data.Descriptors);
    }

    internal static string JobArea(
        GenerationContext context,
        FakeRandom random)
    {
        PersonWorkData data = PersonWorkDataParser.Resolve(context);
        return data.Choose(random, data.Areas);
    }

    internal static string JobType(
        GenerationContext context,
        FakeRandom random)
    {
        PersonWorkData data = PersonWorkDataParser.Resolve(context);
        return data.Choose(random, data.Types);
    }

    internal static string JobSeniority(
        GenerationContext context,
        FakeRandom random)
    {
        PersonWorkData data = PersonWorkDataParser.Resolve(context);
        return data.Choose(random, data.Seniorities);
    }

    internal static string JobDepartment(
        GenerationContext context,
        FakeRandom random)
    {
        PersonWorkData data = PersonWorkDataParser.Resolve(context);
        return data.Choose(random, data.Departments);
    }

    internal static string JobStatus(
        GenerationContext context,
        FakeRandom random)
    {
        PersonWorkData data = PersonWorkDataParser.Resolve(context);
        return data.Choose(random, data.Statuses);
    }

    internal static string SalaryBand(
        GenerationContext context,
        FakeRandom random)
    {
        PersonWorkData data = PersonWorkDataParser.Resolve(context);
        return data.Choose(random, data.SalaryBands);
    }
}
