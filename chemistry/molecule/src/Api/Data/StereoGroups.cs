using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Provides stereo groups data and operations for molecular graphs.
/// </summary>
public class StereoGroups
{
    /// <summary>
    /// The groups.
    /// </summary>
    public List<EnhancedStereoGroup> Groups;

    /// <summary>
    /// Initializes a new instance of the <see cref="StereoGroups"/> class.
    /// </summary>
    public StereoGroups()
    {
        Groups = new List<EnhancedStereoGroup>();
    }

    /// <summary>
    /// Adds the group.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <param name="source">The source.</param>
    /// <param name="label">The label.</param>
    /// <returns>The resulting enhanced stereo group.</returns>
    internal EnhancedStereoGroup AddGroup(string? type, string? source, string? label)
    {
        EnhancedStereoGroup group = EnhancedStereoGroup.Create(Groups.Count, NormalizeGroupType(type));
        if (source == null)
        {
            group.Source = "";
        }
        else
        {
            group.Source = source;
        }

        if (label == null)
        {
            group.Label = "";
        }
        else
        {
            group.Label = label;
        }

        Groups.Add(group);
        return group;
    }

    /// <summary>
    /// Adds the record to group.
    /// </summary>
    /// <param name="groupId">The group identifier.</param>
    /// <param name="stereoRecordId">The stereo record identifier.</param>
    internal void AddRecordToGroup(int groupId, int stereoRecordId)
    {
        EnhancedStereoGroup group = GetGroup(groupId);
        if (group.Id < 0)
        {
            return;
        }

        AddUniqueInt(group.StereoRecordIds, stereoRecordId);
    }

    /// <summary>
    /// Assigns the record to group.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="stereoRecordId">The stereo record identifier.</param>
    /// <param name="groupId">The group identifier.</param>
    internal void AssignRecordToGroup(Molecule? molecule, int stereoRecordId, int groupId)
    {
        if (molecule != null && stereoRecordId >= 0 && stereoRecordId < molecule.StereoRecords.Count)
        {
            molecule.StereoRecords[stereoRecordId].GroupId = groupId;
        }

        AddRecordToGroup(groupId, stereoRecordId);
    }

    /// <summary>
    /// Returns the group.
    /// </summary>
    /// <param name="groupId">The group identifier.</param>
    /// <returns>The resulting enhanced stereo group.</returns>
    internal EnhancedStereoGroup GetGroup(int groupId)
    {
        int index = 0;
        while (index < Groups.Count)
        {
            if (Groups[index].Id == groupId)
            {
                return Groups[index];
            }

            index = index + 1;
        }

        return new EnhancedStereoGroup();
    }

    /// <summary>
    /// Returns the groups for record.
    /// </summary>
    /// <param name="stereoRecordId">The stereo record identifier.</param>
    /// <returns>The resulting values in deterministic order.</returns>
    internal List<EnhancedStereoGroup> GetGroupsForRecord(int stereoRecordId)
    {
        List<EnhancedStereoGroup> result = new List<EnhancedStereoGroup>();
        int index = 0;
        while (index < Groups.Count)
        {
            if (ContainsInt(Groups[index].StereoRecordIds, stereoRecordId))
            {
                result.Add(Groups[index]);
            }

            index = index + 1;
        }

        return result;
    }

    /// <summary>
    /// Checks the supplied value and returns diagnostics without silently repairing it.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    internal MoleculeReport Validate(Molecule? molecule)
    {
        MoleculeReport report = new MoleculeReport();

        if (molecule == null)
        {
            AddError(report, "ENHANCED_STEREO_NULL_MOLECULE", "Cannot validate enhanced stereo groups for a null molecule.", -1);
            return report;
        }

        List<int> seenRecordIds = new List<int>();
        int groupIndex = 0;
        while (groupIndex < Groups.Count)
        {
            EnhancedStereoGroup group = Groups[groupIndex];
            if (!IsValidGroupType(group.Type))
            {
                AddError(report, "ENHANCED_STEREO_GROUP_TYPE", "Enhanced stereo group has an unsupported type.", -1);
            }

            ValidateGroupRecords(molecule, group, seenRecordIds, report);
            groupIndex = groupIndex + 1;
        }

        return report;
    }

    internal void ValidateGroupRecords(Molecule molecule, EnhancedStereoGroup group, List<int> seenRecordIds, MoleculeReport report)
    {
        int recordIndex = 0;
        while (recordIndex < group.StereoRecordIds.Count)
        {
            int stereoRecordId = group.StereoRecordIds[recordIndex];
            if (stereoRecordId < 0 || stereoRecordId >= molecule.StereoRecords.Count)
            {
                AddError(report, "ENHANCED_STEREO_GROUP_RECORD", "Enhanced stereo group references a stereo record that does not exist.", -1);
            }
            else
            {
                if (ContainsInt(seenRecordIds, stereoRecordId))
                {
                    AddError(report, "ENHANCED_STEREO_GROUP_DUPLICATE_RECORD", "Stereo record appears in more than one enhanced stereo group.", stereoRecordId);
                }
                else
                {
                    seenRecordIds.Add(stereoRecordId);
                }

                if (molecule.StereoRecords[stereoRecordId].GroupId >= 0 && molecule.StereoRecords[stereoRecordId].GroupId != group.Id)
                {
                    AddWarning(report, "ENHANCED_STEREO_GROUP_MISMATCH", "Stereo record group id does not match containing enhanced stereo group.", stereoRecordId);
                }
            }

            recordIndex = recordIndex + 1;
        }
    }

    /// <summary>
    /// Creates an independent copy of the value and its mutable collections.
    /// </summary>
    /// <returns>The resulting stereo groups.</returns>
    internal StereoGroups Clone()
    {
        StereoGroups clone = new StereoGroups();
        int groupIndex = 0;
        while (groupIndex < Groups.Count)
        {
            EnhancedStereoGroup source = Groups[groupIndex];
            EnhancedStereoGroup group = clone.AddGroup(source.Type, source.Source, source.Label);
            group.Id = source.Id;
            int recordIndex = 0;
            while (recordIndex < source.StereoRecordIds.Count)
            {
                group.StereoRecordIds.Add(source.StereoRecordIds[recordIndex]);
                recordIndex = recordIndex + 1;
            }

            groupIndex = groupIndex + 1;
        }

        return clone;
    }

    internal string NormalizeGroupType(string? type)
    {
        if (type == null)
        {
            return "";
        }

        if (type == "ABS" || type == "Abs")
        {
            return "abs";
        }

        if (type == "OR" || type == "Or")
        {
            return "or";
        }

        if (type == "AND" || type == "And")
        {
            return "and";
        }

        return type;
    }

    internal bool IsValidGroupType(string type)
    {
        return type == "abs" || type == "or" || type == "and";
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

    internal void AddUniqueInt(List<int> values, int value)
    {
        if (!ContainsInt(values, value))
        {
            values.Add(value);
        }
    }

    internal void AddError(MoleculeReport report, string code, string message, int stereoRecordId)
    {
        MoleculeError error = new MoleculeError(code, message, DiagnosticLocation.CreateEmpty(), -1, stereoRecordId, ErrorSeverity.Error);
        report.Add(error);
    }

    internal void AddWarning(MoleculeReport report, string code, string message, int stereoRecordId)
    {
        MoleculeError error = new MoleculeError(code, message, DiagnosticLocation.CreateEmpty(), -1, stereoRecordId, ErrorSeverity.Warning);
        report.Add(error);
    }
}

