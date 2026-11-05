namespace image;

internal class IptcDatasetNameTable
{
    internal static string NameFor(int record, int dataset)
    {
        if (record == 1)
        {
            return EnvelopeName(dataset);
        }

        if (record == 2)
        {
            return ApplicationName(dataset);
        }

        return "Dataset" + dataset.ToString();
    }

    internal static string EnvelopeName(int dataset)
    {
        switch (dataset)
        {
            case 0: return "ModelVersion";
            case 5: return "Destination";
            case 20: return "FileFormat";
            case 22: return "FileFormatVersion";
            case 30: return "ServiceIdentifier";
            case 40: return "EnvelopeNumber";
            case 50: return "ProductId";
            case 60: return "EnvelopePriority";
            case 70: return "DateSent";
            case 80: return "TimeSent";
            case 90: return "CodedCharacterSet";
            case 100: return "UniqueObjectName";
            case 120: return "ArmIdentifier";
            case 122: return "ArmVersion";
            default: return "Dataset" + dataset.ToString();
        }
    }

    internal static string ApplicationName(int dataset)
    {
        switch (dataset)
        {
            case 0: return "RecordVersion";
            case 3: return "ObjectTypeReference";
            case 4: return "ObjectAttributeReference";
            case 5: return "ObjectName";
            case 7: return "EditStatus";
            case 8: return "EditorialUpdate";
            case 10: return "Urgency";
            case 12: return "SubjectReference";
            case 15: return "Category";
            case 20: return "SupplementalCategories";
            case 22: return "FixtureIdentifier";
            case 25: return "Keywords";
            case 26: return "ContentLocationCode";
            case 27: return "ContentLocationName";
            case 30: return "ReleaseDate";
            case 35: return "ReleaseTime";
            case 37: return "ExpirationDate";
            case 38: return "ExpirationTime";
            case 40: return "SpecialInstructions";
            case 42: return "ActionAdvised";
            case 45: return "ReferenceService";
            case 47: return "ReferenceDate";
            case 50: return "ReferenceNumber";
            case 55: return "DateCreated";
            case 60: return "TimeCreated";
            case 62: return "DigitalCreationDate";
            case 63: return "DigitalCreationTime";
            case 65: return "OriginatingProgram";
            case 70: return "ProgramVersion";
            case 75: return "ObjectCycle";
            case 80: return "Byline";
            case 85: return "BylineTitle";
            case 90: return "City";
            case 92: return "SubLocation";
            case 95: return "ProvinceState";
            case 100: return "CountryCode";
            case 101: return "CountryName";
            case 103: return "OriginalTransmissionReference";
            case 105: return "Headline";
            case 110: return "Credit";
            case 115: return "Source";
            case 116: return "CopyrightNotice";
            case 118: return "Contact";
            case 120: return "Caption";
            case 121: return "LocalCaption";
            case 122: return "WriterEditor";
            case 125: return "RasterizedCaption";
            case 130: return "ImageType";
            case 131: return "ImageOrientation";
            case 135: return "LanguageIdentifier";
            case 150: return "AudioType";
            case 151: return "AudioSamplingRate";
            case 152: return "AudioSamplingResolution";
            case 153: return "AudioDuration";
            case 154: return "AudioOutcue";
            case 184: return "JobId";
            case 185: return "MasterDocumentId";
            case 186: return "ShortDocumentId";
            case 187: return "UniqueDocumentId";
            case 188: return "OwnerId";
            case 200: return "ObjectPreviewFileFormat";
            case 201: return "ObjectPreviewFileFormatVersion";
            case 202: return "ObjectPreviewData";
            default: return "Dataset" + dataset.ToString();
        }
    }

    internal static string RecordGroup(int record)
    {
        if (record == 1) return "Envelope";
        if (record >= 2 && record <= 6) return "Application" + record.ToString();
        if (record == 7) return "PreObjectData";
        if (record == 8) return "ObjectData";
        if (record == 9) return "PostObjectData";
        return "Record" + record.ToString();
    }

    internal static string RecordName(int record)
    {
        if (record == 1) return "IPTC Envelope Record";
        if (record >= 2 && record <= 6) return "IPTC Application Record " + record.ToString();
        if (record == 7) return "IPTC Pre-Object Descriptor Record";
        if (record == 8) return "IPTC Object Record";
        if (record == 9) return "IPTC Post-Object Descriptor Record";
        return "IPTC Record " + record.ToString();
    }

    internal static bool IsDate(int record, int dataset)
    {
        if (record == 1)
        {
            return dataset == 70;
        }

        if (record == 2)
        {
            return dataset == 30 || dataset == 37 || dataset == 47 || dataset == 55 || dataset == 62;
        }

        return false;
    }

    internal static bool IsInteger(int record, int dataset)
    {
        if (record == 1)
        {
            return dataset == 0 || dataset == 20 || dataset == 22 || dataset == 120 || dataset == 122;
        }

        if (record == 2)
        {
            return dataset == 0 || dataset == 200 || dataset == 201;
        }

        return false;
    }

    internal static bool IsBinary(int record, int dataset)
    {
        if (record == 2 && (dataset == 125 || dataset == 202))
        {
            return true;
        }

        return record == 8;
    }

    internal static bool IsKnown(int record, int dataset)
    {
        return NameFor(record, dataset) != "Dataset" + dataset.ToString();
    }
}
