namespace qrcode;

internal class MatrixValidationState
{
    internal QrFailureReason[] FailureReasons;
    internal string[] Failures;
    internal int FailureCount;
    internal int[] DamagedModuleIndices;
    internal QrModuleRole[] DamagedRoles;
    internal bool[] DamageRecorded;
    internal int DamageCount;
    internal int FixedPatternDamageCount;
    internal int CriticalMetadataDamageCount;
    internal int PayloadDamageCount;
    internal int ErrorCorrectionDamageCount;
    internal int UnknownModuleCount;
    internal bool FormatModuleDamage;
    internal bool VersionModuleDamage;

    internal MatrixValidationState(int moduleCount)
    {
        FailureReasons = new QrFailureReason[16];
        Failures = new string[16];
        DamagedModuleIndices = new int[moduleCount];
        DamagedRoles = new QrModuleRole[moduleCount];
        DamageRecorded = new bool[moduleCount];
    }

    internal void AddFailure(QrFailureReason reason, string message)
    {
        for (int index = 0; index < FailureCount; index++)
        {
            if (Failures[index] == message)
            {
                return;
            }
        }

        if (FailureCount >= FailureReasons.Length)
        {
            return;
        }

        FailureReasons[FailureCount] = reason;
        Failures[FailureCount] = message;
        FailureCount++;
    }

    internal void AddDamage(int index, QrModuleRole role)
    {
        if (DamageRecorded[index])
        {
            return;
        }

        DamageRecorded[index] = true;
        DamagedModuleIndices[DamageCount] = index;
        DamagedRoles[DamageCount] = role;
        DamageCount++;
    }

    internal QrValidationResult CreateResult(
        bool formatInformationConsistent,
        bool versionInformationConsistent,
        bool quietZoneValid,
        bool isRecoverable)
    {
        QrFailureReason[] reasons = new QrFailureReason[FailureCount];
        string[] messages = new string[FailureCount];
        for (int index = 0; index < FailureCount; index++)
        {
            reasons[index] = FailureReasons[index];
            messages[index] = Failures[index];
        }

        int[] damagedIndices = new int[DamageCount];
        QrModuleRole[] damagedRoles = new QrModuleRole[DamageCount];
        for (int index = 0; index < DamageCount; index++)
        {
            damagedIndices[index] = DamagedModuleIndices[index];
            damagedRoles[index] = DamagedRoles[index];
        }

        return new QrValidationResult
        {
            IsConformant = FailureCount == 0,
            FailureReasons = reasons,
            Failures = messages,
            DamagedModuleIndices = damagedIndices,
            DamagedRoles = damagedRoles,
            FixedPatternDamageCount = FixedPatternDamageCount,
            CriticalMetadataDamageCount = CriticalMetadataDamageCount,
            PayloadDamageCount = PayloadDamageCount,
            ErrorCorrectionDamageCount = ErrorCorrectionDamageCount,
            UnknownModuleCount = UnknownModuleCount,
            FormatInformationConsistent = formatInformationConsistent,
            VersionInformationConsistent = versionInformationConsistent,
            QuietZoneValid = quietZoneValid,
            IsRecoverable = isRecoverable
        };
    }
}
