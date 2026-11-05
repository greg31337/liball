namespace fake;

/// <summary>Represents a correlated, conspicuously non-clinical health-shaped fixture.</summary>
/// <remarks>The record does not represent a patient, diagnosis, procedure, prescription, clinician, facility, or medical advice. Codes are deliberately synthetic and unassigned.</remarks>
public class HealthRecord
{
    /// <summary>The fixture-prefixed blood-type label.</summary>
    public string BloodType = string.Empty;

    /// <summary>The body part correlated with <c>BodySystem</c>.</summary>
    public string BodyPart = string.Empty;

    /// <summary>The broad body system correlated with <c>BodyPart</c>.</summary>
    public string BodySystem = string.Empty;

    /// <summary>The sample-prefixed symptom label.</summary>
    public string Symptom = string.Empty;

    /// <summary>The deliberately unassigned procedure-code shape.</summary>
    public string ProcedureCode = string.Empty;

    /// <summary>The deliberately unassigned diagnosis-code shape.</summary>
    public string DiagnosisCode = string.Empty;

    /// <summary>The medicine-form shape correlated with <c>Dosage</c>.</summary>
    public string MedicineForm = string.Empty;

    /// <summary>The test-only dosage-shaped text using fixture units.</summary>
    public string Dosage = string.Empty;

    /// <summary>The fictional provider name correlated with <c>Facility</c>.</summary>
    public string Provider = string.Empty;

    /// <summary>The fictional facility correlated with <c>Provider</c>.</summary>
    public string Facility = string.Empty;

    /// <summary>Whether the record is synthetic, always <c>true</c>.</summary>
    public bool IsSynthetic;

    /// <summary>Whether the record is suitable for clinical use, always <c>false</c>.</summary>
    public bool ForClinicalUse;

    /// <summary>Whether the codes claim assignment by a coding authority, always <c>false</c>.</summary>
    public bool CodesAreAssigned;

    /// <summary>Whether the record represents a patient, always <c>false</c>.</summary>
    public bool RepresentsPatient;

    /// <summary>Whether the provider or facility claims a real entity, always <c>false</c>.</summary>
    public bool RepresentsRealProviderOrFacility;

    internal HealthRecord()
    {
    }
}
