namespace tiff;

/// <summary>Defines the name, accepted data types, and expected element count for a TIFF tag.</summary>
public class TiffTagDefinition
{
    /// <summary>Defines the any count value used by this API.</summary>
    public const long AnyCount = -1;

    /// <summary>Identifies the TIFF tag.</summary>
    public int TagId;
    /// <summary>Contains the name.</summary>
    public string Name;
    /// <summary>Contains the preferred data type.</summary>
    public TiffDataType PreferredDataType;
    /// <summary>Contains the allowed data types.</summary>
    public TiffDataType[] AllowedDataTypes;
    /// <summary>Contains the expected count.</summary>
    public long ExpectedCount;

    /// <summary>Creates a new instance of the TIFF Tag Definition class.</summary>
    public TiffTagDefinition()
    {
        TagId = 0;
        Name = "";
        PreferredDataType = TiffDataType.Unknown;
        AllowedDataTypes = new TiffDataType[0];
        ExpectedCount = AnyCount;
    }

    /// <summary>Creates a new instance of the TIFF Tag Definition class.</summary>
    /// <param name="tagId">The numeric TIFF tag identifier.</param>
    /// <param name="name">The name.</param>
    /// <param name="preferredDataType">The preferred data type.</param>
    /// <param name="allowedDataTypes">The allowed data types.</param>
    /// <param name="expectedCount">The expected count.</param>
    internal static TiffTagDefinition Create(int tagId, string name, TiffDataType preferredDataType, TiffDataType[] allowedDataTypes, long expectedCount)
    {
        TiffTagDefinition value = new TiffTagDefinition();
        value.Initialize(tagId, name, preferredDataType, allowedDataTypes, expectedCount);
        return value;
    }

    internal void Initialize(int tagId, string name, TiffDataType preferredDataType, TiffDataType[] allowedDataTypes, long expectedCount)
    {
        TagId = tagId;
        if (name == null)
        {
            Name = "";
        }
        else
        {
            Name = name;
        }

        PreferredDataType = preferredDataType;
        AllowedDataTypes = CopyDataTypes(allowedDataTypes);
        ExpectedCount = expectedCount;
    }

    /// <summary>Creates a detached copy whose mutable arrays and child models do not alias this instance.</summary>
    /// <returns>The resulting TIFF tag definition.</returns>
    internal TiffTagDefinition Copy()
    {
        return TiffTagDefinition.Create(TagId, Name, PreferredDataType, AllowedDataTypes, ExpectedCount);
    }

    /// <summary>Determines whether allows data type under the current TIFF data and policy.</summary>
    /// <param name="dataType">The data type.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool AllowsDataType(TiffDataType dataType)
    {
        int index = 0;
        while (index < AllowedDataTypes.Length)
        {
            if (AllowedDataTypes[index] == dataType)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    /// <summary>Returns the number of available allows entries.</summary>
    /// <param name="count">The count.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool AllowsCount(long count)
    {
        if (count < 0)
        {
            return false;
        }

        if (ExpectedCount == AnyCount)
        {
            return true;
        }

        return count == ExpectedCount;
    }

    /// <summary>Returns the number of available has fixed entries.</summary>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool HasFixedCount()
    {
        return ExpectedCount != AnyCount;
    }

    internal static TiffDataType[] CopyDataTypes(TiffDataType[] dataTypes)
    {
        if (dataTypes == null)
        {
            return new TiffDataType[0];
        }

        TiffDataType[] copy = new TiffDataType[dataTypes.Length];
        int index = 0;
        while (index < dataTypes.Length)
        {
            copy[index] = dataTypes[index];
            index = index + 1;
        }

        return copy;
    }
}
