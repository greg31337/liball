using System;
using System.Collections.Generic;
using System.IO;

namespace fake;
/// <summary>Defines the public operations exposed by <see cref="HealthProvider"/>.</summary>
public interface IHealthProvider
{
    /// <summary>Selects a fixture-prefixed blood-type label.</summary>
    /// <remarks>The value is test vocabulary and is not suitable for transfusion, compatibility, or clinical decisions.</remarks>
    /// <returns>A conspicuously synthetic blood-type label.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string BloodType();
    /// <summary>Selects a generic body part.</summary>
    /// <returns>A body part correlated with the body-system fixture table.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string BodyPart();
    /// <summary>Selects a broad body system.</summary>
    /// <returns>A body system correlated with the body-part fixture table.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string BodySystem();
    /// <summary>Selects a sample-prefixed symptom label.</summary>
    /// <remarks>The value is test vocabulary and is not a symptom assessment or medical advice.</remarks>
    /// <returns>A conspicuously synthetic symptom label.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Symptom();
    /// <summary>Generates a deliberately unassigned procedure-code shape.</summary>
    /// <remarks>The value begins with <c>TEST-PX-</c>, belongs to no claimed coding system, and is not clinically usable.</remarks>
    /// <returns>A synthetic procedure-code shape.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is insufficient.</exception>
    string ProcedureCode();
    /// <summary>Generates a deliberately unassigned diagnosis-code shape.</summary>
    /// <remarks>The value begins with <c>TEST-DX-</c>, belongs to no claimed coding system, and is not clinically usable.</remarks>
    /// <returns>A synthetic diagnosis-code shape.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is insufficient.</exception>
    string DiagnosisCode();
    /// <summary>Selects a medicine-form shape for software fixtures.</summary>
    /// <remarks>The value names no medicine and makes no treatment or administration claim.</remarks>
    /// <returns>A form label ending in <c>-shaped</c>.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string MedicineForm();
    /// <summary>Generates conspicuously synthetic dosage-shaped text.</summary>
    /// <remarks>The value uses fixture units rather than medical units or a treatment schedule and is not medical advice.</remarks>
    /// <returns>Text beginning with <c>TEST-DOSE-</c>.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Dosage();
    /// <summary>Selects a project-fictional provider name.</summary>
    /// <remarks>The value does not claim identity, licensure, employment, or medical service.</remarks>
    /// <returns>A fictional provider name correlated with the facility fixture table.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Provider();
    /// <summary>Selects a project-fictional facility name.</summary>
    /// <remarks>The value does not claim a real location or medical service.</remarks>
    /// <returns>A fictional facility name correlated with the provider fixture table.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Facility();
    /// <summary>Generates a correlated, conspicuously non-clinical health-shaped fixture.</summary>
    /// <remarks>The record is not suitable for clinical use and does not represent a patient, coded event, prescription, provider, facility, or medical advice.</remarks>
    /// <returns>Correlated non-clinical body, code, medicine-form, dosage, provider, and facility data.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    HealthRecord Record();
}
