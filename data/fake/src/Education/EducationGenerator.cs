namespace fake;

internal static class EducationGenerator
{
    internal static string Institution(GenerationContext context, FakeRandom random)
    {
        EducationLocaleData data = EducationLocaleDataParser.Resolve(context);
        return data.Choose(random, data.Institutions).Value;
    }

    internal static string Course(GenerationContext context, FakeRandom random)
    {
        EducationLocaleData data = EducationLocaleDataParser.Resolve(context);
        EducationSubjectData subject = data.ChooseSubject(random);
        return data.Choose(random, subject.Courses).Value;
    }

    internal static string Subject(GenerationContext context, FakeRandom random)
    {
        EducationLocaleData data = EducationLocaleDataParser.Resolve(context);
        return data.ChooseSubject(random).Value;
    }

    internal static string Degree(GenerationContext context, FakeRandom random)
    {
        EducationLocaleData data = EducationLocaleDataParser.Resolve(context);
        EducationSubjectData subject = data.ChooseSubject(random);
        return data.Choose(random, subject.Degrees).Value;
    }

    internal static string Term(GenerationContext context, FakeRandom random)
    {
        EducationLocaleData data = EducationLocaleDataParser.Resolve(context);
        return data.Choose(random, data.Terms).Value;
    }

    internal static string Grade(GenerationContext context, FakeRandom random)
    {
        EducationLocaleData data = EducationLocaleDataParser.Resolve(context);
        return data.Choose(random, data.Grades).Value;
    }

    internal static string StudentId(GenerationContext context, FakeRandom random)
    {
        EducationLocaleData data = EducationLocaleDataParser.Resolve(context);
        return StudentId(data, context, random);
    }

    internal static StudentProfile Record(GenerationContext context, FakeRandom random)
    {
        EducationLocaleData data = EducationLocaleDataParser.Resolve(context);
        EducationSubjectData subject = data.ChooseSubject(random);
        string institution = data.Choose(random, data.Institutions).Value;
        string course = data.Choose(random, subject.Courses).Value;
        string degree = data.Choose(random, subject.Degrees).Value;
        string term = data.Choose(random, data.Terms).Value;
        string grade = data.Choose(random, data.Grades).Value;
        string studentId = StudentId(data, context, random);
        return new StudentProfile(institution, course, subject.Value, degree, term, grade, studentId);
    }

    static string StudentId(EducationLocaleData data, GenerationContext context, FakeRandom random)
    {
        string mask = data.Choose(random, data.StudentIdMasks).Value;
        return MaskFormatter.Format(random, mask, context.CapturedOptions.MaximumOutputSize);
    }
}
