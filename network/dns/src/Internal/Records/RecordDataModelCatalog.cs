namespace dns;

internal enum RecordDataModel
{
    Unassigned = 0,
    MetaQuery = 1,
    Address = 2,
    Name = 3,
    TwoName = 4,
    PreferenceName = 5,
    CharacterStrings = 6,
    Opaque = 7,
    FixedBytes = 8,
    Soa = 9,
    Wks = 10,
    A6 = 11,
    Px = 12,
    Loc = 13,
    Srv = 14,
    Naptr = 15,
    Cert = 16,
    Apl = 17,
    Signature = 18,
    Key = 19,
    Ds = 20,
    Sshfp = 21,
    IpsecKey = 22,
    Nsec = 23,
    Nsec3 = 24,
    Nsec3Param = 25,
    Dhcid = 26,
    Tlsa = 27,
    Hip = 28,
    Csync = 29,
    Zonemd = 30,
    Svcb = 31,
    Dsync = 32,
    IlNp = 33,
    Eui = 34,
    Uri = 35,
    Caa = 36,
    AmtRelay = 37,
    ResolverInformation = 38,
    Tkey = 39,
    Tsig = 40,
    Edns = 41
}

internal static class RecordDataModelCatalog
{
    internal static RecordDataModel ReadRecordDataModel(int recordTypeValue)
    {
        switch (recordTypeValue)
        {
            case 0: return RecordDataModel.MetaQuery;
            case 1: return RecordDataModel.Address;
            case 2: return RecordDataModel.Name;
            case 3: return RecordDataModel.Name;
            case 4: return RecordDataModel.Name;
            case 5: return RecordDataModel.Name;
            case 6: return RecordDataModel.Soa;
            case 7: return RecordDataModel.Name;
            case 8: return RecordDataModel.Name;
            case 9: return RecordDataModel.Name;
            case 10: return RecordDataModel.Opaque;
            case 11: return RecordDataModel.Wks;
            case 12: return RecordDataModel.Name;
            case 13: return RecordDataModel.CharacterStrings;
            case 14: return RecordDataModel.TwoName;
            case 15: return RecordDataModel.PreferenceName;
            case 16: return RecordDataModel.CharacterStrings;
            case 17: return RecordDataModel.TwoName;
            case 18: return RecordDataModel.PreferenceName;
            case 19: return RecordDataModel.CharacterStrings;
            case 20: return RecordDataModel.CharacterStrings;
            case 21: return RecordDataModel.PreferenceName;
            case 22: return RecordDataModel.FixedBytes;
            case 23: return RecordDataModel.Name;
            case 24: return RecordDataModel.Signature;
            case 25: return RecordDataModel.Key;
            case 26: return RecordDataModel.Px;
            case 27: return RecordDataModel.CharacterStrings;
            case 28: return RecordDataModel.Address;
            case 29: return RecordDataModel.Loc;
            case 30: return RecordDataModel.Nsec;
            case 31: return RecordDataModel.Opaque;
            case 32: return RecordDataModel.Opaque;
            case 33: return RecordDataModel.Srv;
            case 34: return RecordDataModel.Opaque;
            case 35: return RecordDataModel.Naptr;
            case 36: return RecordDataModel.PreferenceName;
            case 37: return RecordDataModel.Cert;
            case 38: return RecordDataModel.A6;
            case 39: return RecordDataModel.Name;
            case 40: return RecordDataModel.Opaque;
            case 41: return RecordDataModel.Edns;
            case 42: return RecordDataModel.Apl;
            case 43: return RecordDataModel.Ds;
            case 44: return RecordDataModel.Sshfp;
            case 45: return RecordDataModel.IpsecKey;
            case 46: return RecordDataModel.Signature;
            case 47: return RecordDataModel.Nsec;
            case 48: return RecordDataModel.Key;
            case 49: return RecordDataModel.Dhcid;
            case 50: return RecordDataModel.Nsec3;
            case 51: return RecordDataModel.Nsec3Param;
            case 52: return RecordDataModel.Tlsa;
            case 53: return RecordDataModel.Tlsa;
            case 55: return RecordDataModel.Hip;
            case 56: return RecordDataModel.Opaque;
            case 57: return RecordDataModel.Opaque;
            case 58: return RecordDataModel.Opaque;
            case 59: return RecordDataModel.Ds;
            case 60: return RecordDataModel.Key;
            case 61: return RecordDataModel.FixedBytes;
            case 62: return RecordDataModel.Csync;
            case 63: return RecordDataModel.Zonemd;
            case 64: return RecordDataModel.Svcb;
            case 65: return RecordDataModel.Svcb;
            case 66: return RecordDataModel.Dsync;
            case 67: return RecordDataModel.Opaque;
            case 68: return RecordDataModel.Opaque;
            case 69: return RecordDataModel.Opaque;
            case 70: return RecordDataModel.Opaque;
            case 99: return RecordDataModel.CharacterStrings;
            case 100: return RecordDataModel.Opaque;
            case 101: return RecordDataModel.Opaque;
            case 102: return RecordDataModel.Opaque;
            case 103: return RecordDataModel.Opaque;
            case 104: return RecordDataModel.IlNp;
            case 105: return RecordDataModel.IlNp;
            case 106: return RecordDataModel.IlNp;
            case 107: return RecordDataModel.PreferenceName;
            case 108: return RecordDataModel.Eui;
            case 109: return RecordDataModel.Eui;
            case 128: return RecordDataModel.MetaQuery;
            case 249: return RecordDataModel.Tkey;
            case 250: return RecordDataModel.Tsig;
            case 251: return RecordDataModel.MetaQuery;
            case 252: return RecordDataModel.MetaQuery;
            case 253: return RecordDataModel.MetaQuery;
            case 254: return RecordDataModel.MetaQuery;
            case 255: return RecordDataModel.MetaQuery;
            case 256: return RecordDataModel.Uri;
            case 257: return RecordDataModel.Caa;
            case 258: return RecordDataModel.Opaque;
            case 259: return RecordDataModel.Opaque;
            case 260: return RecordDataModel.AmtRelay;
            case 261: return RecordDataModel.ResolverInformation;
            case 262: return RecordDataModel.Opaque;
            case 263: return RecordDataModel.Opaque;
            case 264: return RecordDataModel.Opaque;
            case 32768: return RecordDataModel.Ds;
            case 32769: return RecordDataModel.Ds;
            default: return RecordDataModel.Unassigned;
        }
    }
}
