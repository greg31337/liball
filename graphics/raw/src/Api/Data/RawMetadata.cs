namespace raw;

/// <summary>Represents an Exif local timestamp with independently retained UTC offset and subsecond text.</summary>
public class RawExifTimestamp
{
    /// <summary>The year component.</summary>
    public int Year;
    /// <summary>The month component.</summary>
    public int Month;
    /// <summary>The day component.</summary>
    public int Day;
    /// <summary>The hour component.</summary>
    public int Hour;
    /// <summary>The minute component.</summary>
    public int Minute;
    /// <summary>The second component.</summary>
    public int Second;
    /// <summary>The exact decimal subsecond digits, without punctuation.</summary>
    public string Subseconds;
    /// <summary>Whether an explicit UTC offset is present.</summary>
    public bool HasOffset;
    /// <summary>The signed UTC offset in minutes.</summary>
    public int OffsetMinutes;
    /// <summary>Whether all supplied components form a representable Exif timestamp.</summary>
    public bool Valid;
    /// <summary>The exact decoded date-time text.</summary>
    public string DateTimeText;
    /// <summary>The exact decoded offset text.</summary>
    public string OffsetText;

    /// <summary>Creates a timestamp from decoded Exif date-time, offset, and subsecond strings.</summary>
    /// <param name="dateTimeText">Text in <c>YYYY:MM:DD HH:MM:SS</c> form.</param>
    /// <param name="offsetText">Text in <c>+HH:MM</c> or <c>-HH:MM</c> form, or an empty string.</param>
    /// <param name="subseconds">Decimal subsecond digits, or an empty string.</param>
    public RawExifTimestamp(string dateTimeText, string offsetText, string subseconds)
    {
        DateTimeText = dateTimeText == null ? string.Empty : dateTimeText;
        OffsetText = offsetText == null ? string.Empty : offsetText;
        Subseconds = subseconds == null ? string.Empty : subseconds;
        Year = 0;
        Month = 0;
        Day = 0;
        Hour = 0;
        Minute = 0;
        Second = 0;
        HasOffset = false;
        OffsetMinutes = 0;
        Valid = ParseDateTime() && ParseOffset() && ValidateSubseconds();
    }

    /// <summary>Creates an Exif timestamp from an offset-aware .NET value.</summary>
    /// <param name="value">The timestamp.</param>
    /// <param name="subsecondDigits">The number of decimal digits to retain from zero through seven.</param>
    /// <returns>The formatted timestamp.</returns>
    /// <exception cref="RawFormatException">The digit count or UTC offset is outside the Exif representation.</exception>
    internal static RawExifTimestamp Create(System.DateTimeOffset value, int subsecondDigits)
    {
        if (subsecondDigits < 0 || subsecondDigits > 7)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "Exif timestamps can retain zero through seven .NET subsecond digits.", -1);
        }
        int offsetMinutes = (int)value.Offset.TotalMinutes;
        if (!ValidOffset(offsetMinutes))
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "The UTC offset is outside the Exif timestamp range.", -1);
        }
        string dateText = value.ToString("yyyy:MM:dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
        string sign = offsetMinutes < 0 ? "-" : "+";
        int absolute = offsetMinutes < 0 ? -offsetMinutes : offsetMinutes;
        string offsetText = sign + (absolute / 60).ToString("D2", System.Globalization.CultureInfo.InvariantCulture) + ":" + (absolute % 60).ToString("D2", System.Globalization.CultureInfo.InvariantCulture);
        string fraction = string.Empty;
        if (subsecondDigits > 0)
        {
            long ticks = value.Ticks % System.TimeSpan.TicksPerSecond;
            fraction = ticks.ToString("D7", System.Globalization.CultureInfo.InvariantCulture).Substring(0, subsecondDigits);
        }
        return new RawExifTimestamp(dateText, offsetText, fraction);
    }

    /// <summary>Converts the timestamp to an offset-aware .NET value, retaining up to seven subsecond digits.</summary>
    /// <returns>The converted timestamp.</returns>
    /// <exception cref="RawFormatException">The timestamp is invalid or has no explicit UTC offset.</exception>
    internal System.DateTimeOffset ToDateTimeOffset()
    {
        if (!Valid || !HasOffset)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "A valid Exif timestamp with an explicit UTC offset is required.", -1);
        }
        long ticks = FractionTicks(Subseconds);
        System.DateTimeOffset value = new System.DateTimeOffset(Year, Month, Day, Hour, Minute, Second, System.TimeSpan.FromMinutes(OffsetMinutes));
        return value.AddTicks(ticks);
    }

    /// <summary>Returns a null-terminated ASCII value for the date-time component.</summary>
    /// <returns>The encoded value.</returns>
    /// <exception cref="RawFormatException">The timestamp is invalid.</exception>
    internal RawTagValue EncodeDateTime()
    {
        RequireValid();
        return RawTagValue.CreateAscii(DateTimeText);
    }

    /// <summary>Returns a null-terminated ASCII value for the UTC offset component.</summary>
    /// <returns>The encoded value.</returns>
    /// <exception cref="RawFormatException">The timestamp is invalid or has no offset.</exception>
    internal RawTagValue EncodeOffset()
    {
        RequireValid();
        if (!HasOffset)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "The Exif timestamp does not contain an offset.", -1);
        }
        return RawTagValue.CreateAscii(OffsetText);
    }

    /// <summary>Returns a null-terminated ASCII value for the subsecond component.</summary>
    /// <returns>The encoded value.</returns>
    /// <exception cref="RawFormatException">The timestamp is invalid or has no subsecond digits.</exception>
    internal RawTagValue EncodeSubseconds()
    {
        RequireValid();
        if (Subseconds.Length == 0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "The Exif timestamp does not contain subsecond digits.", -1);
        }
        return RawTagValue.CreateAscii(Subseconds);
    }

    internal bool ParseDateTime()
    {
        if (DateTimeText.Length != 19 || DateTimeText[4] != ':' || DateTimeText[7] != ':' || DateTimeText[10] != ' ' || DateTimeText[13] != ':' || DateTimeText[16] != ':')
        {
            return false;
        }
        Year = ParseDigits(DateTimeText, 0, 4);
        Month = ParseDigits(DateTimeText, 5, 2);
        Day = ParseDigits(DateTimeText, 8, 2);
        Hour = ParseDigits(DateTimeText, 11, 2);
        Minute = ParseDigits(DateTimeText, 14, 2);
        Second = ParseDigits(DateTimeText, 17, 2);
        if (Year < 1 || Year > 9999 || Month < 1 || Month > 12 || Hour < 0 || Hour > 23 || Minute < 0 || Minute > 59 || Second < 0 || Second > 59)
        {
            return false;
        }
        return Day >= 1 && Day <= System.DateTime.DaysInMonth(Year, Month);
    }

    internal bool ParseOffset()
    {
        if (OffsetText.Length == 0)
        {
            HasOffset = false;
            OffsetMinutes = 0;
            return true;
        }
        if (OffsetText.Length != 6 || (OffsetText[0] != '+' && OffsetText[0] != '-') || OffsetText[3] != ':')
        {
            return false;
        }
        int hours = ParseDigits(OffsetText, 1, 2);
        int minutes = ParseDigits(OffsetText, 4, 2);
        if (hours < 0 || minutes < 0 || minutes > 59)
        {
            return false;
        }
        int total = hours * 60 + minutes;
        if (OffsetText[0] == '-')
        {
            total = -total;
        }
        if (!ValidOffset(total))
        {
            return false;
        }
        HasOffset = true;
        OffsetMinutes = total;
        return true;
    }

    internal bool ValidateSubseconds()
    {
        int index = 0;
        while (index < Subseconds.Length)
        {
            if (Subseconds[index] < '0' || Subseconds[index] > '9')
            {
                return false;
            }
            index++;
        }
        return true;
    }

    internal static int ParseDigits(string text, int offset, int count)
    {
        int value = 0;
        int index = 0;
        while (index < count)
        {
            char character = text[offset + index];
            if (character < '0' || character > '9')
            {
                return -1;
            }
            value = value * 10 + character - '0';
            index++;
        }
        return value;
    }

    internal static bool ValidOffset(int minutes)
    {
        return minutes >= -14 * 60 && minutes <= 14 * 60;
    }

    internal static long FractionTicks(string digits)
    {
        int count = digits.Length > 7 ? 7 : digits.Length;
        long value = 0;
        int index = 0;
        while (index < count)
        {
            value = value * 10 + digits[index] - '0';
            index++;
        }
        while (index < 7)
        {
            value *= 10;
            index++;
        }
        return value;
    }

    internal void RequireValid()
    {
        if (!Valid)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "A valid Exif timestamp is required.", -1);
        }
    }
}

/// <summary>Contains typed GPS coordinates and altitude decoded from a GPS IFD.</summary>
public class RawGpsLocation
{
    /// <summary>Whether both latitude and longitude are available.</summary>
    public bool HasCoordinates;
    /// <summary>Signed latitude in decimal degrees.</summary>
    public double Latitude;
    /// <summary>Signed longitude in decimal degrees.</summary>
    public double Longitude;
    /// <summary>Whether altitude is available.</summary>
    public bool HasAltitude;
    /// <summary>Signed altitude in meters using the reference declared by GPSAltitudeRef.</summary>
    public double Altitude;
    /// <summary>The GPS altitude reference value from Exif 3.1.</summary>
    public byte AltitudeReference;

    /// <summary>Creates a typed GPS location.</summary>
    /// <param name="hasCoordinates">Whether both coordinates are present.</param>
    /// <param name="latitude">Signed latitude.</param>
    /// <param name="longitude">Signed longitude.</param>
    /// <param name="hasAltitude">Whether altitude is present.</param>
    /// <param name="altitude">Signed altitude.</param>
    /// <param name="altitudeReference">The encoded reference value.</param>
    public RawGpsLocation(bool hasCoordinates, double latitude, double longitude, bool hasAltitude, double altitude, byte altitudeReference)
    {
        HasCoordinates = hasCoordinates;
        Latitude = latitude;
        Longitude = longitude;
        HasAltitude = hasAltitude;
        Altitude = altitude;
        AltitudeReference = altitudeReference;
    }
}

/// <summary>Provides a typed, exact-byte view over TIFF, Exif, GPS, and interoperability directories.</summary>
public class RawMetadata
{
    /// <summary>All inspected directories in deterministic graph order.</summary>
    public RawIfd[] Ifds;
    /// <summary>The camera manufacturer, or an empty string.</summary>
    public string Make;
    /// <summary>The camera model, or an empty string.</summary>
    public string Model;
    /// <summary>The lens manufacturer, or an empty string.</summary>
    public string LensMake;
    /// <summary>The lens model, or an empty string.</summary>
    public string LensModel;
    /// <summary>The Exif version bytes interpreted as ASCII, or an empty string.</summary>
    public string ExifVersion;
    /// <summary>The InteroperabilityIndex string, or an empty string.</summary>
    public string InteroperabilityIndex;
    /// <summary>The decoded UserComment result, or <c>null</c>.</summary>
    public RawTextValue? UserComment;
    /// <summary>The file modification timestamp, or <c>null</c>.</summary>
    public RawExifTimestamp? ModifiedTime;
    /// <summary>The original capture timestamp, or <c>null</c>.</summary>
    public RawExifTimestamp? OriginalTime;
    /// <summary>The digitization timestamp, or <c>null</c>.</summary>
    public RawExifTimestamp? DigitizedTime;
    /// <summary>The decoded GPS location, or <c>null</c>.</summary>
    public RawGpsLocation? GpsLocation;
    /// <summary>All discovered XMP packets in deterministic directory order.</summary>
    public RawXmpPacket[] XmpPackets;
    /// <summary>All discovered IPTC IIM payloads in deterministic container order.</summary>
    public RawIptcRecord[] IptcRecords;
    /// <summary>IPTC Core fields represented through discovered XMP packets.</summary>
    public RawIptcXmpField[] IptcCoreFields;
    /// <summary>IPTC Extension fields represented through discovered XMP packets.</summary>
    public RawIptcXmpField[] IptcExtensionFields;
    /// <summary>All discovered and validated ICC profiles in deterministic directory order.</summary>
    public RawIccProfile[] IccProfiles;
    /// <summary>All preserved maker notes with their recognized offset-base evidence.</summary>
    public RawMakerNote[] MakerNotes;
    /// <summary>Normalized fields that are unambiguous or were selected by explicit reconciliation.</summary>
    public RawMetadataField[] UnifiedFields;
    /// <summary>Every normalized source representation retained for editing and conflict inspection.</summary>
    public RawMetadataField[] SourceFields;
    /// <summary>Differences found across Exif, XMP, and IPTC representations.</summary>
    public RawMetadataConflict[] Conflicts;
    /// <summary>Whether explicit precedence was applied to conflicting fields.</summary>
    public bool Reconciled;
    /// <summary>The precedence applied to this unified view, or <see cref="RawMetadataPrecedence.None"/>.</summary>
    public RawMetadataPrecedence Precedence;
    /// <summary>Source-targeted edits staged for a later metadata writer.</summary>
    public RawMetadataEdit[] PendingEdits;

    /// <summary>Creates a metadata view.</summary>
    /// <param name="ifds">The public directories, or <c>null</c>.</param>
    public RawMetadata(RawIfd[]? ifds)
    {
        Ifds = ifds == null ? System.Array.Empty<RawIfd>() : ifds;
        Make = string.Empty;
        Model = string.Empty;
        LensMake = string.Empty;
        LensModel = string.Empty;
        ExifVersion = string.Empty;
        InteroperabilityIndex = string.Empty;
        UserComment = null;
        ModifiedTime = null;
        OriginalTime = null;
        DigitizedTime = null;
        GpsLocation = null;
        XmpPackets = System.Array.Empty<RawXmpPacket>();
        IptcRecords = System.Array.Empty<RawIptcRecord>();
        IptcCoreFields = System.Array.Empty<RawIptcXmpField>();
        IptcExtensionFields = System.Array.Empty<RawIptcXmpField>();
        IccProfiles = System.Array.Empty<RawIccProfile>();
        MakerNotes = System.Array.Empty<RawMakerNote>();
        UnifiedFields = System.Array.Empty<RawMetadataField>();
        SourceFields = System.Array.Empty<RawMetadataField>();
        Conflicts = System.Array.Empty<RawMetadataConflict>();
        Reconciled = false;
        Precedence = RawMetadataPrecedence.None;
        PendingEdits = System.Array.Empty<RawMetadataEdit>();
    }

    /// <summary>Finds a directory by its stable ordinal path.</summary>
    /// <param name="path">The exact path.</param>
    /// <returns>The matching directory, or <c>null</c>.</returns>
    internal RawIfd? FindIfd(string path)
    {
        if (path == null)
        {
            return null;
        }
        int index = 0;
        while (index < Ifds.Length)
        {
            if (Ifds[index] != null && Ifds[index].Path == path)
            {
                return Ifds[index];
            }
            index++;
        }
        return null;
    }

    /// <summary>Finds the first named tag across directories in deterministic graph order.</summary>
    /// <param name="name">The case-sensitive Exif field name.</param>
    /// <returns>The matching tag, or <c>null</c>.</returns>
    internal RawTag? FindTag(string name)
    {
        int index = 0;
        while (index < Ifds.Length)
        {
            RawTag? tag = Ifds[index] == null ? null : Ifds[index].FindTagByName(name);
            if (tag != null)
            {
                return tag;
            }
            index++;
        }
        return null;
    }

    /// <summary>Finds the first IPTC Core XMP field with an optional exact language.</summary>
    /// <param name="name">The exact XMP local property name.</param>
    /// <param name="language">The exact language, or <c>null</c> to accept any language.</param>
    /// <returns>The matching field, or <c>null</c>.</returns>
    internal RawIptcXmpField? GetIptcCoreField(string name, string? language)
    {
        return GetIptcXmpField(IptcCoreFields, name, language);
    }

    /// <summary>Finds the first IPTC Extension XMP field with an optional exact language.</summary>
    /// <param name="name">The exact XMP local property name.</param>
    /// <param name="language">The exact language, or <c>null</c> to accept any language.</param>
    /// <returns>The matching field, or <c>null</c>.</returns>
    internal RawIptcXmpField? GetIptcExtensionField(string name, string? language)
    {
        return GetIptcXmpField(IptcExtensionFields, name, language);
    }

    /// <summary>Builds the unified metadata view and reports every parallel-representation conflict.</summary>
    /// <param name="options">Representation selections and explicit reconciliation policy.</param>
    /// <returns>This metadata instance with refreshed unified fields and conflicts.</returns>
    /// <exception cref="RawFormatException">The reconciliation options are invalid.</exception>
    internal RawMetadata Reconcile(RawMetadataOptions options)
    {
        MetadataReconciler.Apply(this, options);
        return this;
    }

    /// <summary>Finds a unified field by stable name and optional exact language.</summary>
    /// <param name="name">The stable field name.</param>
    /// <param name="language">The exact language, or <c>null</c> to accept any language.</param>
    /// <returns>The matching field, or <c>null</c>.</returns>
    internal RawMetadataField? Get(string name, string? language)
    {
        if (name == null || UnifiedFields == null)
        {
            return null;
        }
        string? selectedLanguage = language == "x-default" ? string.Empty : language;
        int index = 0;
        while (index < UnifiedFields.Length)
        {
            RawMetadataField field = UnifiedFields[index];
            if (field != null && field.Name == name && (selectedLanguage == null || field.Language == selectedLanguage))
            {
                return field;
            }
            index++;
        }
        return null;
    }

    /// <summary>Applies an ordered edit set atomically or performs an equivalent dry run.</summary>
    /// <param name="editSet">The metadata edit set.</param>
    /// <param name="dryRun">Whether to report without mutating.</param>
    /// <returns>The ordered changes and diagnostics.</returns>
    internal RawMetadataEditReport ApplyEdits(RawMetadataEditSet editSet, bool dryRun)
    {
        return MetadataEditor.Apply(this, editSet, dryRun);
    }

    internal static RawIptcXmpField? GetIptcXmpField(RawIptcXmpField[] fields, string name, string? language)
    {
        if (fields == null || name == null)
        {
            return null;
        }
        int index = 0;
        while (index < fields.Length)
        {
            RawIptcXmpField field = fields[index];
            if (field != null && field.Name == name && (language == null || field.Language == language))
            {
                return field;
            }
            index++;
        }
        return null;
    }
}
