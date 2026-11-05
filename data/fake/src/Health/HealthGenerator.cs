using System.Globalization;

namespace fake;

internal static class HealthGenerator
{
    const int CodeMaximum = 9999;
    const int CodeLength = 12;
    const int DosageMaximum = 999;
    const int DosageFixedLength = 31;

    internal static string BloodType(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateValues(context, HealthData.BloodTypes, "bloodTypeData");
        return Value(context, random, HealthData.BloodTypes, "bloodType");
    }

    internal static string BodyPart(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateBodyData(context);
        return Value(context, random, HealthData.BodyParts, "bodyPart");
    }

    internal static string BodySystem(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateBodyData(context);
        return Value(context, random, HealthData.BodySystems, "bodySystem");
    }

    internal static string Symptom(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateValues(context, HealthData.Symptoms, "symptomData");
        return Value(context, random, HealthData.Symptoms, "symptom");
    }

    internal static string ProcedureCode(
        GenerationContext context,
        FakeRandom random)
    {
        Guard.OutputSize(
            CodeLength,
            context.CapturedOptions.MaximumOutputSize,
            "procedureCode");
        return CodeUnchecked(random, "TEST-PX-");
    }

    internal static string DiagnosisCode(
        GenerationContext context,
        FakeRandom random)
    {
        Guard.OutputSize(
            CodeLength,
            context.CapturedOptions.MaximumOutputSize,
            "diagnosisCode");
        return CodeUnchecked(random, "TEST-DX-");
    }

    internal static string MedicineForm(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateValues(
            context,
            HealthData.MedicineForms,
            "medicineFormData");
        return Value(
            context,
            random,
            HealthData.MedicineForms,
            "medicineForm");
    }

    internal static string Dosage(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateValues(
            context,
            HealthData.MedicineForms,
            "medicineFormData");
        Guard.OutputSize(
            MaximumDosageLength(),
            context.CapturedOptions.MaximumOutputSize,
            "dosage");
        int formIndex = random.IntFromMaximumExclusive(HealthData.MedicineForms.Length);
        return DosageUnchecked(random, formIndex);
    }

    internal static string Provider(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateProviderData(context);
        return Value(context, random, HealthData.ProviderNames, "provider");
    }

    internal static string Facility(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateProviderData(context);
        return Value(context, random, HealthData.Facilities, "facility");
    }

    internal static HealthRecord Record(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateValues(context, HealthData.BloodTypes, "bloodTypeData");
        ValidateBodyData(context);
        ValidateValues(context, HealthData.Symptoms, "symptomData");
        ValidateValues(
            context,
            HealthData.MedicineForms,
            "medicineFormData");
        ValidateProviderData(context);
        Guard.OutputSize(
            MaximumRecordSize(),
            context.CapturedOptions.MaximumOutputSize,
            "healthRecord");
        int bodyIndex = random.IntFromMaximumExclusive(HealthData.BodyParts.Length);
        int formIndex = random.IntFromMaximumExclusive(HealthData.MedicineForms.Length);
        int providerIndex = random.IntFromMaximumExclusive(HealthData.ProviderNames.Length);
        HealthRecord result = new HealthRecord();
        result.BloodType = HealthData.BloodTypes[
            random.IntFromMaximumExclusive(HealthData.BloodTypes.Length)];
        result.BodyPart = HealthData.BodyParts[bodyIndex];
        result.BodySystem = HealthData.BodySystems[bodyIndex];
        result.Symptom = HealthData.Symptoms[
            random.IntFromMaximumExclusive(HealthData.Symptoms.Length)];
        result.ProcedureCode = CodeUnchecked(random, "TEST-PX-");
        result.DiagnosisCode = CodeUnchecked(random, "TEST-DX-");
        result.MedicineForm = HealthData.MedicineForms[formIndex];
        result.Dosage = DosageUnchecked(random, formIndex);
        result.Provider = HealthData.ProviderNames[providerIndex];
        result.Facility = HealthData.Facilities[providerIndex];
        result.IsSynthetic = true;
        result.ForClinicalUse = false;
        result.CodesAreAssigned = false;
        result.RepresentsPatient = false;
        result.RepresentsRealProviderOrFacility = false;
        return result;
    }

    static string CodeUnchecked(FakeRandom random, string prefix)
    {
        return prefix + random.IntFromMinimumAndMaximumAndMaximumInclusive(
            0,
            CodeMaximum,
            true).ToString("D4", CultureInfo.InvariantCulture);
    }

    static string DosageUnchecked(FakeRandom random, int formIndex)
    {
        string amount = random.IntFromMinimumAndMaximumAndMaximumInclusive(
            1,
            DosageMaximum,
            true).ToString("D3", CultureInfo.InvariantCulture);
        return "TEST-DOSE-" + amount + " " +
            HealthData.MedicineForms[formIndex] + " units per fixture";
    }

    static string Value(
        GenerationContext context,
        FakeRandom random,
        string[] values,
        string outputName)
    {
        Guard.OutputSize(
            MaximumLength(values),
            context.CapturedOptions.MaximumOutputSize,
            outputName);
        return values[random.IntFromMaximumExclusive(values.Length)];
    }

    static void ValidateBodyData(GenerationContext context)
    {
        ValidatePairs(
            context,
            HealthData.BodyParts,
            HealthData.BodySystems,
            "bodyData");
    }

    static void ValidateProviderData(GenerationContext context)
    {
        ValidatePairs(
            context,
            HealthData.ProviderNames,
            HealthData.Facilities,
            "providerData");
    }

    static void ValidatePairs(
        GenerationContext context,
        string[] first,
        string[] second,
        string dataName)
    {
        if (first.Length != second.Length)
        {
            throw FakeException.Create(
                "A project-authored non-clinical table has inconsistent column lengths.");
        }
        ValidateValues(context, first, dataName);
        ValidateValues(context, second, dataName);
    }

    static void ValidateValues(
        GenerationContext context,
        string[] values,
        string dataName)
    {
        Guard.Count(
            values.Length,
            context.CapturedOptions.MaximumCount,
            dataName);
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == null || values[index].Length == 0)
            {
                throw FakeException.Create(
                    "A project-authored non-clinical data table is malformed.");
            }
            index = index + 1;
        }
    }

    static int MaximumDosageLength()
    {
        return DosageFixedLength +
            MaximumLength(HealthData.MedicineForms);
    }

    static int MaximumRecordSize()
    {
        return MaximumLength(HealthData.BloodTypes) +
            MaximumLength(HealthData.BodyParts) +
            MaximumLength(HealthData.BodySystems) +
            MaximumLength(HealthData.Symptoms) + CodeLength * 2 +
            MaximumLength(HealthData.MedicineForms) +
            MaximumDosageLength() +
            MaximumLength(HealthData.ProviderNames) +
            MaximumLength(HealthData.Facilities);
    }

    static int MaximumLength(string[] values)
    {
        int maximum = 0;
        int index = 0;
        while (index < values.Length)
        {
            if (values[index].Length > maximum)
            {
                maximum = values[index].Length;
            }
            index = index + 1;
        }
        return maximum;
    }
}
