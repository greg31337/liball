using fake;

namespace fake.Tests;

[TestFixture]
public class HealthProviderTests
{
    static Dictionary<string, string> BodySystems =
        new Dictionary<string, string>
        {
            ["bone"] = "skeletal",
            ["brain"] = "nervous",
            ["heart"] = "circulatory",
            ["kidney"] = "urinary",
            ["lung"] = "respiratory",
            ["muscle"] = "muscular",
            ["skin"] = "integumentary",
            ["stomach"] = "digestive"
        };

    static Dictionary<string, string> Facilities =
        new Dictionary<string, string>
        {
            ["Avery Example"] = "Example Test Clinic",
            ["Casey Fixture"] = "Fixture Sample Center",
            ["Jordan Sample"] = "Sample Demo Practice",
            ["Morgan Test"] = "Test Example Facility",
            ["Riley Demo"] = "Demo Fixture Center",
            ["Taylor Mock"] = "Mock Sample Clinic"
        };

    static string[] BloodTypes = new string[]
    {
        "fixture A+", "fixture A-", "fixture B+", "fixture B-",
        "fixture AB+", "fixture AB-", "fixture O+", "fixture O-"
    };

    static string[] Symptoms = new string[]
    {
        "sample ache", "sample cough", "sample fatigue", "sample nausea",
        "sample soreness", "sample stiffness"
    };

    static string[] MedicineForms = new string[]
    {
        "capsule-shaped", "cream-shaped", "inhaler-shaped", "liquid-shaped",
        "patch-shaped", "tablet-shaped"
    };

    [Test]
    public void BodyPartsAndSystemsArePairedAndDeterministic()
    {
        int seed = 12900;
        while (seed < 13000)
        {
            string part = Fake.Create(seed).Health().BodyPart();
            string system = Fake.Create(seed).Health().BodySystem();

            Assert.Multiple(() =>
            {
                Assert.That(BodySystems.ContainsKey(part), Is.True);
                Assert.That(system, Is.EqualTo(BodySystems[part]));
                Assert.That(Fake.Create(seed).Health().BodyPart(),
                    Is.EqualTo(part));
                Assert.That(Fake.Create(seed).Health().BodySystem(),
                    Is.EqualTo(system));
            });
            seed++;
        }
    }

    [Test]
    public void HealthVocabularyIsConspicuouslyNonClinical()
    {
        const long seed = 12901;
        HealthProvider first = Fake.Create(seed).Health();
        HealthProvider second = Fake.Create(seed).Health();
        HashSet<string> bloodTypes = new HashSet<string>();
        HashSet<string> symptoms = new HashSet<string>();

        int index = 0;
        while (index < 256)
        {
            string bloodType = first.BloodType();
            string symptom = first.Symptom();
            Assert.Multiple(() =>
            {
                Assert.That(bloodType, Is.EqualTo(second.BloodType()));
                Assert.That(symptom, Is.EqualTo(second.Symptom()));
                Assert.That(bloodType, Is.AnyOf(BloodTypes));
                Assert.That(bloodType, Does.StartWith("fixture "));
                Assert.That(symptom, Is.AnyOf(Symptoms));
                Assert.That(symptom, Does.StartWith("sample "));
            });
            bloodTypes.Add(bloodType);
            symptoms.Add(symptom);
            index++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(bloodTypes, Is.EquivalentTo(BloodTypes));
            Assert.That(symptoms, Is.EquivalentTo(Symptoms));
        });
    }

    [Test]
    public void CodeShapesAreSyntheticAndDeterministic()
    {
        const long seed = 12902;
        HealthProvider first = Fake.Create(seed).Health();
        HealthProvider second = Fake.Create(seed).Health();

        int index = 0;
        while (index < 256)
        {
            string procedure = first.ProcedureCode();
            string diagnosis = first.DiagnosisCode();
            Assert.Multiple(() =>
            {
                Assert.That(procedure,
                    Is.EqualTo(second.ProcedureCode()));
                Assert.That(diagnosis,
                    Is.EqualTo(second.DiagnosisCode()));
                Assert.That(procedure, Does.Match("^TEST-PX-[0-9]{4}$"));
                Assert.That(diagnosis, Does.Match("^TEST-DX-[0-9]{4}$"));
            });
            index++;
        }
    }

    [Test]
    public void MedicineFormsAndDosageShapesArePaired()
    {
        int seed = 12903;
        while (seed < 13003)
        {
            string form = Fake.Create(seed).Health().MedicineForm();
            string dosage = Fake.Create(seed).Health().Dosage();

            Assert.Multiple(() =>
            {
                Assert.That(form, Is.AnyOf(MedicineForms));
                Assert.That(dosage,
                    Does.Match("^TEST-DOSE-[0-9]{3} .+-shaped units per fixture$"));
                Assert.That(dosage,
                    Does.EndWith(form + " units per fixture"));
                Assert.That(Fake.Create(seed).Health().Dosage(),
                    Is.EqualTo(dosage));
            });
            seed++;
        }
    }

    [Test]
    public void ProvidersAndFacilitiesArePairedFictionalFixtures()
    {
        int seed = 12904;
        while (seed < 13004)
        {
            string provider = Fake.Create(seed).Health().Provider();
            string facility = Fake.Create(seed).Health().Facility();

            Assert.Multiple(() =>
            {
                Assert.That(Facilities.ContainsKey(provider), Is.True);
                Assert.That(facility, Is.EqualTo(Facilities[provider]));
                Assert.That(Fake.Create(seed).Health().Provider(),
                    Is.EqualTo(provider));
                Assert.That(Fake.Create(seed).Health().Facility(),
                    Is.EqualTo(facility));
            });
            seed++;
        }
    }

    [Test]
    public void HealthRecordsPreserveCorrelationsAndSafetySemantics()
    {
        const long seed = 12905;
        HealthProvider first = Fake.Create(seed).Health();
        HealthProvider second = Fake.Create(seed).Health();

        int index = 0;
        while (index < 512)
        {
            HealthRecord value = first.Record();
            HealthRecord same = second.Record();

            Assert.Multiple(() =>
            {
                AssertRecordEqual(value, same);
                Assert.That(value.BodySystem,
                    Is.EqualTo(BodySystems[value.BodyPart]));
                Assert.That(value.Dosage,
                    Does.EndWith(value.MedicineForm +
                        " units per fixture"));
                Assert.That(value.Facility,
                    Is.EqualTo(Facilities[value.Provider]));
                Assert.That(value.BloodType, Does.StartWith("fixture "));
                Assert.That(value.Symptom, Does.StartWith("sample "));
                Assert.That(value.ProcedureCode,
                    Does.Match("^TEST-PX-[0-9]{4}$"));
                Assert.That(value.DiagnosisCode,
                    Does.Match("^TEST-DX-[0-9]{4}$"));
                Assert.That(value.IsSynthetic, Is.True);
                Assert.That(value.ForClinicalUse, Is.False);
                Assert.That(value.CodesAreAssigned, Is.False);
                Assert.That(value.RepresentsPatient, Is.False);
                Assert.That(value.RepresentsRealProviderOrFacility,
                    Is.False);
            });
            index++;
        }
    }

    [Test]
    public void LimitsFailBeforeHealthEntropy()
    {
        const long seed = 12906;
        FakeOptions tinyOptions = FakeOptions.Create(seed);
        tinyOptions.MaximumOutputSize = 1;
        HealthProvider tinyInspected = Fake.Create(tinyOptions).Health();
        HealthProvider tinyUntouched = Fake.Create(tinyOptions).Health();
        FakeOptions countOptions = FakeOptions.Create(seed);
        countOptions.MaximumCount = 3;
        HealthProvider countInspected = Fake.Create(countOptions).Health();
        HealthProvider countUntouched = Fake.Create(countOptions).Health();

        Assert.Multiple(() =>
        {
            Assert.That(() => tinyInspected.BloodType(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.BodyPart(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.BodySystem(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.Symptom(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.ProcedureCode(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.DiagnosisCode(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.MedicineForm(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.Dosage(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.Provider(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.Facility(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.Record(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.BloodType(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.BodyPart(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.BodySystem(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.Symptom(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.MedicineForm(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.Dosage(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.Provider(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.Facility(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.Record(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(tinyInspected.RandomValue.Long(),
                Is.EqualTo(tinyUntouched.RandomValue.Long()));
            Assert.That(countInspected.RandomValue.Long(),
                Is.EqualTo(countUntouched.RandomValue.Long()));
        });
    }

    [Test]
    public void HealthGenerationDoesNotPerturbOtherProviderStreams()
    {
        Fake inspected = Fake.Create(12907);
        Fake untouched = Fake.Create(12907);

        int index = 0;
        while (index < 32)
        {
            inspected.Health().BloodType();
            inspected.Health().BodyPart();
            inspected.Health().BodySystem();
            inspected.Health().Symptom();
            inspected.Health().ProcedureCode();
            inspected.Health().DiagnosisCode();
            inspected.Health().MedicineForm();
            inspected.Health().Dosage();
            inspected.Health().Provider();
            inspected.Health().Facility();
            inspected.Health().Record();
            index++;
        }

        Assert.That(inspected.Person().FullName(),
            Is.EqualTo(untouched.Person().FullName()));
    }

    static void AssertRecordEqual(HealthRecord first, HealthRecord second)
    {
        Assert.Multiple(() =>
        {
            Assert.That(first.BloodType, Is.EqualTo(second.BloodType));
            Assert.That(first.BodyPart, Is.EqualTo(second.BodyPart));
            Assert.That(first.BodySystem, Is.EqualTo(second.BodySystem));
            Assert.That(first.Symptom, Is.EqualTo(second.Symptom));
            Assert.That(first.ProcedureCode,
                Is.EqualTo(second.ProcedureCode));
            Assert.That(first.DiagnosisCode,
                Is.EqualTo(second.DiagnosisCode));
            Assert.That(first.MedicineForm,
                Is.EqualTo(second.MedicineForm));
            Assert.That(first.Dosage, Is.EqualTo(second.Dosage));
            Assert.That(first.Provider, Is.EqualTo(second.Provider));
            Assert.That(first.Facility, Is.EqualTo(second.Facility));
            Assert.That(first.IsSynthetic, Is.EqualTo(second.IsSynthetic));
            Assert.That(first.ForClinicalUse,
                Is.EqualTo(second.ForClinicalUse));
            Assert.That(first.CodesAreAssigned,
                Is.EqualTo(second.CodesAreAssigned));
            Assert.That(first.RepresentsPatient,
                Is.EqualTo(second.RepresentsPatient));
            Assert.That(first.RepresentsRealProviderOrFacility,
                Is.EqualTo(second.RepresentsRealProviderOrFacility));
        });
    }
}
