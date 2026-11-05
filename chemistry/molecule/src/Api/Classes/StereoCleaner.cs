using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Provides stereo cleaner data and operations for molecular graphs.
/// </summary>
public class StereoCleaner : IStereoCleaner
{
    internal StereoValidator Validator;

    /// <summary>
    /// Initializes a new instance of the <see cref="StereoCleaner"/> class.
    /// </summary>
    public StereoCleaner()
    {
        Validator = new StereoValidator();
    }

    /// <summary>
    /// Removes invalid stereo records and rebuilds enhanced stereo groups.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    public StereoCleanupResult Clean(Molecule? molecule)
    {
        StereoCleanupResult result = new StereoCleanupResult();

        if (molecule == null)
        {
            result.Report.AddError("STEREO_CLEANUP_NULL_MOLECULE", "Cannot clean stereo for a null molecule.");
            return result;
        }

        result.Molecule = molecule;
        result.Success = true;

        List<int> oldRecordIds = new List<int>();
        List<StereoRecord> keptRecords = new List<StereoRecord>();
        int recordIndex = 0;
        while (recordIndex < molecule.StereoRecords.Count)
        {
            StereoRecord record = molecule.StereoRecords[recordIndex];
            if (RecordIsValid(molecule, record))
            {
                oldRecordIds.Add(recordIndex);
                keptRecords.Add(record);
            }
            else
            {
                result.RemovedRecordCount = result.RemovedRecordCount + 1;
                result.Report.AddWarning("STEREO_RECORD_REMOVED", "Invalid stereo record was removed during cleanup.");
            }

            recordIndex = recordIndex + 1;
        }

        molecule.StereoRecords.Clear();
        recordIndex = 0;
        while (recordIndex < keptRecords.Count)
        {
            StereoRecord record = keptRecords[recordIndex];
            record.Id = recordIndex;
            record.GroupId = -1;
            molecule.StereoRecords.Add(record);
            recordIndex = recordIndex + 1;
        }

        StereoGroups rebuiltGroups = RebuildGroups(molecule, oldRecordIds, result);
        molecule.StereoGroups = rebuiltGroups;
        return result;
    }

    internal bool RecordIsValid(Molecule molecule, StereoRecord? record)
    {
        if (record == null)
        {
            return false;
        }

        Molecule clone = MoleculeBuilder.CloneMolecule(molecule);
        clone.StereoRecords.Clear();
        clone.StereoGroups = new StereoGroups();
        clone.StereoRecords.Add(MoleculeBuilder.CloneStereoRecord(record, 0));
        MoleculeReport report = Validator.Validate(clone);
        return !report.HasErrors();
    }

    internal StereoGroups RebuildGroups(Molecule molecule, List<int> oldRecordIds, StereoCleanupResult result)
    {
        StereoGroups rebuilt = new StereoGroups();
        StereoGroups sourceGroups = result.Molecule.StereoGroups;
        if (sourceGroups == null)
        {
            return rebuilt;
        }

        List<int> assignedNewRecordIds = new List<int>();
        int groupIndex = 0;
        while (groupIndex < sourceGroups.Groups.Count)
        {
            EnhancedStereoGroup source = sourceGroups.Groups[groupIndex];
            if (sourceGroups.IsValidGroupType(source.Type))
            {
                EnhancedStereoGroup group = rebuilt.AddGroup(source.Type, source.Source, source.Label);
                AddMappedRecordsToGroup(molecule, oldRecordIds, assignedNewRecordIds, source, group);
                if (group.StereoRecordIds.Count == 0)
                {
                    rebuilt.Groups.RemoveAt(rebuilt.Groups.Count - 1);
                    result.RemovedGroupCount = result.RemovedGroupCount + 1;
                    result.Report.AddWarning("STEREO_GROUP_REMOVED", "Empty enhanced stereo group was removed during cleanup.");
                }
            }
            else
            {
                result.RemovedGroupCount = result.RemovedGroupCount + 1;
                result.Report.AddWarning("STEREO_GROUP_REMOVED", "Invalid enhanced stereo group was removed during cleanup.");
            }

            groupIndex = groupIndex + 1;
        }

        RenumberGroups(molecule, rebuilt);
        return rebuilt;
    }

    internal void AddMappedRecordsToGroup(Molecule molecule, List<int> oldRecordIds, List<int> assignedNewRecordIds, EnhancedStereoGroup source, EnhancedStereoGroup group)
    {
        int index = 0;
        while (index < source.StereoRecordIds.Count)
        {
            int newRecordId = MapOldRecordId(oldRecordIds, source.StereoRecordIds[index]);
            if (newRecordId >= 0 && !ContainsInt(assignedNewRecordIds, newRecordId))
            {
                group.StereoRecordIds.Add(newRecordId);
                molecule.StereoRecords[newRecordId].GroupId = group.Id;
                assignedNewRecordIds.Add(newRecordId);
            }

            index = index + 1;
        }
    }

    internal void RenumberGroups(Molecule molecule, StereoGroups groups)
    {
        int groupIndex = 0;
        while (groupIndex < groups.Groups.Count)
        {
            int oldGroupId = groups.Groups[groupIndex].Id;
            groups.Groups[groupIndex].Id = groupIndex;

            int recordIndex = 0;
            while (recordIndex < groups.Groups[groupIndex].StereoRecordIds.Count)
            {
                int stereoRecordId = groups.Groups[groupIndex].StereoRecordIds[recordIndex];
                if (stereoRecordId >= 0 && stereoRecordId < molecule.StereoRecords.Count && molecule.StereoRecords[stereoRecordId].GroupId == oldGroupId)
                {
                    molecule.StereoRecords[stereoRecordId].GroupId = groupIndex;
                }

                recordIndex = recordIndex + 1;
            }

            groupIndex = groupIndex + 1;
        }
    }

    internal int MapOldRecordId(List<int> oldRecordIds, int oldRecordId)
    {
        int index = 0;
        while (index < oldRecordIds.Count)
        {
            if (oldRecordIds[index] == oldRecordId)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal bool ContainsInt(List<int> values, int value)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (values[index] == value)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }
}

