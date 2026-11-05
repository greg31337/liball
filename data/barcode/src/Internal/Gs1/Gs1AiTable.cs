namespace barcode;

internal class Gs1AiComponentDefinition
{
    internal string CharacterSet = "";
    internal int MinimumLength;
    internal int MaximumLength;
    internal bool Optional;
    internal string[] Linters = new string[0];
}

internal class Gs1AiDefinition
{
    internal string AiStart = "";
    internal string AiEnd = "";
    internal string WildcardPattern = "";
    internal bool PredefinedLength;
    internal bool DigitalLinkDataAttribute;
    internal Gs1AiComponentDefinition[] Components =
        new Gs1AiComponentDefinition[0];
    internal string[][] RequiredAssociations = new string[0][];
    internal string[] Exclusions = new string[0];
    internal bool DigitalLinkPrimaryKey;
    internal string[][] DigitalLinkQualifierSequences =
        new string[0][];
    internal string Title = "";
    internal int SourceLine;
}

internal static class Gs1AiTable
{
    internal const string SnapshotDate = "2026-08-26";
    internal const string SnapshotSha256 = "1ea02f4e3b81d3092e095987600e01c7fd55310f4da5181f5a070262ff6ffc94";
    internal const int SourceEntryCount = 224;
    internal const int ExpandedEntryCount = 541;
    internal const int TrieNodeCount = 648;
    internal const int TrieRadix = 10;
    internal static readonly Gs1AiDefinition[] Definitions =
        new Gs1AiDefinition[]
        {
            new Gs1AiDefinition
            {
                AiStart = "00",
                AiEnd = "00",
                WildcardPattern = "00",
                PredefinedLength = true,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 18,
                            MaximumLength = 18,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "csum",
                                    "gcppos2",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[0][],
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = true,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "SSCC",
                SourceLine = 121,
            },
            new Gs1AiDefinition
            {
                AiStart = "01",
                AiEnd = "01",
                WildcardPattern = "01",
                PredefinedLength = true,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 14,
                            MaximumLength = 14,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "csum",
                                    "gcppos2",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[0][],
                Exclusions =
                    new string[]
                    {
                        "255",
                        "37",
                    },
                DigitalLinkPrimaryKey = true,
                DigitalLinkQualifierSequences =
                    new string[][]
                    {
                        new string[] { "22", "10", "21" },
                        new string[] { "235" },
                    },
                Title = "GTIN",
                SourceLine = 122,
            },
            new Gs1AiDefinition
            {
                AiStart = "02",
                AiEnd = "02",
                WildcardPattern = "02",
                PredefinedLength = true,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 14,
                            MaximumLength = 14,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "csum",
                                    "gcppos2",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "37" },
                    },
                Exclusions =
                    new string[]
                    {
                        "01",
                        "03",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "CONTENT",
                SourceLine = 123,
            },
            new Gs1AiDefinition
            {
                AiStart = "03",
                AiEnd = "03",
                WildcardPattern = "03",
                PredefinedLength = true,
                DigitalLinkDataAttribute = false,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 14,
                            MaximumLength = 14,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "csum",
                                    "gcppos2",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[0][],
                Exclusions =
                    new string[]
                    {
                        "01",
                        "02",
                        "37",
                        "235",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "MTO GTIN",
                SourceLine = 124,
            },
            new Gs1AiDefinition
            {
                AiStart = "10",
                AiEnd = "10",
                WildcardPattern = "10",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 20,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "02" },
                        new string[] { "03" },
                        new string[] { "8006" },
                        new string[] { "8026" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "BATCH/LOT",
                SourceLine = 125,
            },
            new Gs1AiDefinition
            {
                AiStart = "11",
                AiEnd = "11",
                WildcardPattern = "11",
                PredefinedLength = true,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "yymmd0",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "02" },
                        new string[] { "03" },
                        new string[] { "8006" },
                        new string[] { "8026" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "PROD DATE",
                SourceLine = 126,
            },
            new Gs1AiDefinition
            {
                AiStart = "12",
                AiEnd = "12",
                WildcardPattern = "12",
                PredefinedLength = true,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "yymmd0",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "8020" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "DUE DATE",
                SourceLine = 127,
            },
            new Gs1AiDefinition
            {
                AiStart = "13",
                AiEnd = "13",
                WildcardPattern = "13",
                PredefinedLength = true,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "yymmd0",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "02" },
                        new string[] { "03" },
                        new string[] { "8006" },
                        new string[] { "8026" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "PACK DATE",
                SourceLine = 128,
            },
            new Gs1AiDefinition
            {
                AiStart = "15",
                AiEnd = "15",
                WildcardPattern = "15",
                PredefinedLength = true,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "yymmd0",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "02" },
                        new string[] { "03" },
                        new string[] { "8006" },
                        new string[] { "8026" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "BEST BEFORE or BEST BY",
                SourceLine = 129,
            },
            new Gs1AiDefinition
            {
                AiStart = "16",
                AiEnd = "16",
                WildcardPattern = "16",
                PredefinedLength = true,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "yymmd0",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "02" },
                        new string[] { "03" },
                        new string[] { "8006" },
                        new string[] { "8026" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "SELL BY",
                SourceLine = 130,
            },
            new Gs1AiDefinition
            {
                AiStart = "17",
                AiEnd = "17",
                WildcardPattern = "17",
                PredefinedLength = true,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "yymmd0",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "02" },
                        new string[] { "03" },
                        new string[] { "255" },
                        new string[] { "8006" },
                        new string[] { "8026" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "USE BY or EXPIRY",
                SourceLine = 131,
            },
            new Gs1AiDefinition
            {
                AiStart = "20",
                AiEnd = "20",
                WildcardPattern = "20",
                PredefinedLength = true,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 2,
                            MaximumLength = 2,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "02" },
                        new string[] { "03" },
                        new string[] { "8006" },
                        new string[] { "8026" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "VARIANT",
                SourceLine = 132,
            },
            new Gs1AiDefinition
            {
                AiStart = "21",
                AiEnd = "21",
                WildcardPattern = "21",
                PredefinedLength = false,
                DigitalLinkDataAttribute = false,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 20,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "03" },
                        new string[] { "8006" },
                    },
                Exclusions =
                    new string[]
                    {
                        "235",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "SERIAL",
                SourceLine = 133,
            },
            new Gs1AiDefinition
            {
                AiStart = "22",
                AiEnd = "22",
                WildcardPattern = "22",
                PredefinedLength = false,
                DigitalLinkDataAttribute = false,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 20,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "CPV",
                SourceLine = 134,
            },
            new Gs1AiDefinition
            {
                AiStart = "235",
                AiEnd = "235",
                WildcardPattern = "235",
                PredefinedLength = false,
                DigitalLinkDataAttribute = false,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 28,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "TPX",
                SourceLine = 135,
            },
            new Gs1AiDefinition
            {
                AiStart = "240",
                AiEnd = "240",
                WildcardPattern = "240",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 30,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "02" },
                        new string[] { "03" },
                        new string[] { "8006" },
                        new string[] { "8026" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "ADDITIONAL ID",
                SourceLine = 136,
            },
            new Gs1AiDefinition
            {
                AiStart = "241",
                AiEnd = "241",
                WildcardPattern = "241",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 30,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "02" },
                        new string[] { "03" },
                        new string[] { "8006" },
                        new string[] { "8026" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "CUST. PART No.",
                SourceLine = 137,
            },
            new Gs1AiDefinition
            {
                AiStart = "242",
                AiEnd = "242",
                WildcardPattern = "242",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 1,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "02" },
                        new string[] { "8006" },
                        new string[] { "8026" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "MTO VARIANT",
                SourceLine = 138,
            },
            new Gs1AiDefinition
            {
                AiStart = "243",
                AiEnd = "243",
                WildcardPattern = "243",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 20,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "03" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "PCN",
                SourceLine = 139,
            },
            new Gs1AiDefinition
            {
                AiStart = "250",
                AiEnd = "250",
                WildcardPattern = "250",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 30,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01", "21" },
                        new string[] { "03", "21" },
                        new string[] { "8006", "21" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "SECONDARY SERIAL",
                SourceLine = 140,
            },
            new Gs1AiDefinition
            {
                AiStart = "251",
                AiEnd = "251",
                WildcardPattern = "251",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 30,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "03" },
                        new string[] { "8006" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "REF. TO SOURCE",
                SourceLine = 141,
            },
            new Gs1AiDefinition
            {
                AiStart = "253",
                AiEnd = "253",
                WildcardPattern = "253",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 13,
                            MaximumLength = 13,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "csum",
                                    "gcppos1",
                                },
                        },
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 17,
                            Optional = true,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[0][],
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = true,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "GDTI",
                SourceLine = 142,
            },
            new Gs1AiDefinition
            {
                AiStart = "254",
                AiEnd = "254",
                WildcardPattern = "254",
                PredefinedLength = false,
                DigitalLinkDataAttribute = false,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 20,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "414" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "GLN EXTENSION COMPONENT",
                SourceLine = 143,
            },
            new Gs1AiDefinition
            {
                AiStart = "255",
                AiEnd = "255",
                WildcardPattern = "255",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 13,
                            MaximumLength = 13,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "csum",
                                    "gcppos1",
                                },
                        },
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 1,
                            MaximumLength = 12,
                            Optional = true,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[0][],
                Exclusions =
                    new string[]
                    {
                        "01",
                        "02",
                        "415",
                        "8006",
                        "8020",
                        "8026",
                    },
                DigitalLinkPrimaryKey = true,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "GCN",
                SourceLine = 144,
            },
            new Gs1AiDefinition
            {
                AiStart = "30",
                AiEnd = "30",
                WildcardPattern = "30",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 1,
                            MaximumLength = 8,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "02" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "VAR. COUNT",
                SourceLine = 145,
            },
            new Gs1AiDefinition
            {
                AiStart = "3100",
                AiEnd = "3105",
                WildcardPattern = "310n",
                PredefinedLength = true,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "02" },
                    },
                Exclusions =
                    new string[]
                    {
                        "310n",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "NET WEIGHT (kg)",
                SourceLine = 146,
            },
            new Gs1AiDefinition
            {
                AiStart = "3110",
                AiEnd = "3115",
                WildcardPattern = "311n",
                PredefinedLength = true,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "02" },
                    },
                Exclusions =
                    new string[]
                    {
                        "311n",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "LENGTH (m)",
                SourceLine = 147,
            },
            new Gs1AiDefinition
            {
                AiStart = "3120",
                AiEnd = "3125",
                WildcardPattern = "312n",
                PredefinedLength = true,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "02" },
                    },
                Exclusions =
                    new string[]
                    {
                        "312n",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "WIDTH (m)",
                SourceLine = 148,
            },
            new Gs1AiDefinition
            {
                AiStart = "3130",
                AiEnd = "3135",
                WildcardPattern = "313n",
                PredefinedLength = true,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "02" },
                    },
                Exclusions =
                    new string[]
                    {
                        "313n",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "HEIGHT (m)",
                SourceLine = 149,
            },
            new Gs1AiDefinition
            {
                AiStart = "3140",
                AiEnd = "3145",
                WildcardPattern = "314n",
                PredefinedLength = true,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "02" },
                    },
                Exclusions =
                    new string[]
                    {
                        "314n",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "AREA (m²)",
                SourceLine = 150,
            },
            new Gs1AiDefinition
            {
                AiStart = "3150",
                AiEnd = "3155",
                WildcardPattern = "315n",
                PredefinedLength = true,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "02" },
                    },
                Exclusions =
                    new string[]
                    {
                        "315n",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "NET VOLUME (l)",
                SourceLine = 151,
            },
            new Gs1AiDefinition
            {
                AiStart = "3160",
                AiEnd = "3165",
                WildcardPattern = "316n",
                PredefinedLength = true,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "02" },
                    },
                Exclusions =
                    new string[]
                    {
                        "316n",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "NET VOLUME (m³)",
                SourceLine = 152,
            },
            new Gs1AiDefinition
            {
                AiStart = "3200",
                AiEnd = "3205",
                WildcardPattern = "320n",
                PredefinedLength = true,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "02" },
                    },
                Exclusions =
                    new string[]
                    {
                        "320n",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "NET WEIGHT (lb)",
                SourceLine = 153,
            },
            new Gs1AiDefinition
            {
                AiStart = "3210",
                AiEnd = "3215",
                WildcardPattern = "321n",
                PredefinedLength = true,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "02" },
                    },
                Exclusions =
                    new string[]
                    {
                        "321n",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "LENGTH (in)",
                SourceLine = 154,
            },
            new Gs1AiDefinition
            {
                AiStart = "3220",
                AiEnd = "3225",
                WildcardPattern = "322n",
                PredefinedLength = true,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "02" },
                    },
                Exclusions =
                    new string[]
                    {
                        "322n",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "LENGTH (ft)",
                SourceLine = 155,
            },
            new Gs1AiDefinition
            {
                AiStart = "3230",
                AiEnd = "3235",
                WildcardPattern = "323n",
                PredefinedLength = true,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "02" },
                    },
                Exclusions =
                    new string[]
                    {
                        "323n",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "LENGTH (yd)",
                SourceLine = 156,
            },
            new Gs1AiDefinition
            {
                AiStart = "3240",
                AiEnd = "3245",
                WildcardPattern = "324n",
                PredefinedLength = true,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "02" },
                    },
                Exclusions =
                    new string[]
                    {
                        "324n",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "WIDTH (in)",
                SourceLine = 157,
            },
            new Gs1AiDefinition
            {
                AiStart = "3250",
                AiEnd = "3255",
                WildcardPattern = "325n",
                PredefinedLength = true,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "02" },
                    },
                Exclusions =
                    new string[]
                    {
                        "325n",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "WIDTH (ft)",
                SourceLine = 158,
            },
            new Gs1AiDefinition
            {
                AiStart = "3260",
                AiEnd = "3265",
                WildcardPattern = "326n",
                PredefinedLength = true,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "02" },
                    },
                Exclusions =
                    new string[]
                    {
                        "326n",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "WIDTH (yd)",
                SourceLine = 159,
            },
            new Gs1AiDefinition
            {
                AiStart = "3270",
                AiEnd = "3275",
                WildcardPattern = "327n",
                PredefinedLength = true,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "02" },
                    },
                Exclusions =
                    new string[]
                    {
                        "327n",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "HEIGHT (in)",
                SourceLine = 160,
            },
            new Gs1AiDefinition
            {
                AiStart = "3280",
                AiEnd = "3285",
                WildcardPattern = "328n",
                PredefinedLength = true,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "02" },
                    },
                Exclusions =
                    new string[]
                    {
                        "328n",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "HEIGHT (ft)",
                SourceLine = 161,
            },
            new Gs1AiDefinition
            {
                AiStart = "3290",
                AiEnd = "3295",
                WildcardPattern = "329n",
                PredefinedLength = true,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "02" },
                    },
                Exclusions =
                    new string[]
                    {
                        "329n",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "HEIGHT (yd)",
                SourceLine = 162,
            },
            new Gs1AiDefinition
            {
                AiStart = "3300",
                AiEnd = "3305",
                WildcardPattern = "330n",
                PredefinedLength = true,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "00" },
                        new string[] { "01" },
                    },
                Exclusions =
                    new string[]
                    {
                        "330n",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "GROSS WEIGHT (kg)",
                SourceLine = 163,
            },
            new Gs1AiDefinition
            {
                AiStart = "3310",
                AiEnd = "3315",
                WildcardPattern = "331n",
                PredefinedLength = true,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "00" },
                        new string[] { "01" },
                    },
                Exclusions =
                    new string[]
                    {
                        "331n",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "LENGTH (m), log",
                SourceLine = 164,
            },
            new Gs1AiDefinition
            {
                AiStart = "3320",
                AiEnd = "3325",
                WildcardPattern = "332n",
                PredefinedLength = true,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "00" },
                        new string[] { "01" },
                    },
                Exclusions =
                    new string[]
                    {
                        "332n",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "WIDTH (m), log",
                SourceLine = 165,
            },
            new Gs1AiDefinition
            {
                AiStart = "3330",
                AiEnd = "3335",
                WildcardPattern = "333n",
                PredefinedLength = true,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "00" },
                        new string[] { "01" },
                    },
                Exclusions =
                    new string[]
                    {
                        "333n",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "HEIGHT (m), log",
                SourceLine = 166,
            },
            new Gs1AiDefinition
            {
                AiStart = "3340",
                AiEnd = "3345",
                WildcardPattern = "334n",
                PredefinedLength = true,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "00" },
                        new string[] { "01" },
                    },
                Exclusions =
                    new string[]
                    {
                        "334n",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "AREA (m²), log",
                SourceLine = 167,
            },
            new Gs1AiDefinition
            {
                AiStart = "3350",
                AiEnd = "3355",
                WildcardPattern = "335n",
                PredefinedLength = true,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "00" },
                        new string[] { "01" },
                    },
                Exclusions =
                    new string[]
                    {
                        "335n",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "VOLUME (l), log",
                SourceLine = 168,
            },
            new Gs1AiDefinition
            {
                AiStart = "3360",
                AiEnd = "3365",
                WildcardPattern = "336n",
                PredefinedLength = true,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "00" },
                        new string[] { "01" },
                    },
                Exclusions =
                    new string[]
                    {
                        "336n",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "VOLUME (m³), log",
                SourceLine = 169,
            },
            new Gs1AiDefinition
            {
                AiStart = "3370",
                AiEnd = "3375",
                WildcardPattern = "337n",
                PredefinedLength = true,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                    },
                Exclusions =
                    new string[]
                    {
                        "337n",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "KG PER m²",
                SourceLine = 170,
            },
            new Gs1AiDefinition
            {
                AiStart = "3400",
                AiEnd = "3405",
                WildcardPattern = "340n",
                PredefinedLength = true,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "00" },
                        new string[] { "01" },
                    },
                Exclusions =
                    new string[]
                    {
                        "340n",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "GROSS WEIGHT (lb)",
                SourceLine = 171,
            },
            new Gs1AiDefinition
            {
                AiStart = "3410",
                AiEnd = "3415",
                WildcardPattern = "341n",
                PredefinedLength = true,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "00" },
                        new string[] { "01" },
                    },
                Exclusions =
                    new string[]
                    {
                        "341n",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "LENGTH (in), log",
                SourceLine = 172,
            },
            new Gs1AiDefinition
            {
                AiStart = "3420",
                AiEnd = "3425",
                WildcardPattern = "342n",
                PredefinedLength = true,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "00" },
                        new string[] { "01" },
                    },
                Exclusions =
                    new string[]
                    {
                        "342n",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "LENGTH (ft), log",
                SourceLine = 173,
            },
            new Gs1AiDefinition
            {
                AiStart = "3430",
                AiEnd = "3435",
                WildcardPattern = "343n",
                PredefinedLength = true,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "00" },
                        new string[] { "01" },
                    },
                Exclusions =
                    new string[]
                    {
                        "343n",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "LENGTH (yd), log",
                SourceLine = 174,
            },
            new Gs1AiDefinition
            {
                AiStart = "3440",
                AiEnd = "3445",
                WildcardPattern = "344n",
                PredefinedLength = true,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "00" },
                        new string[] { "01" },
                    },
                Exclusions =
                    new string[]
                    {
                        "344n",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "WIDTH (in), log",
                SourceLine = 175,
            },
            new Gs1AiDefinition
            {
                AiStart = "3450",
                AiEnd = "3455",
                WildcardPattern = "345n",
                PredefinedLength = true,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "00" },
                        new string[] { "01" },
                    },
                Exclusions =
                    new string[]
                    {
                        "345n",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "WIDTH (ft), log",
                SourceLine = 176,
            },
            new Gs1AiDefinition
            {
                AiStart = "3460",
                AiEnd = "3465",
                WildcardPattern = "346n",
                PredefinedLength = true,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "00" },
                        new string[] { "01" },
                    },
                Exclusions =
                    new string[]
                    {
                        "346n",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "WIDTH (yd), log",
                SourceLine = 177,
            },
            new Gs1AiDefinition
            {
                AiStart = "3470",
                AiEnd = "3475",
                WildcardPattern = "347n",
                PredefinedLength = true,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "00" },
                        new string[] { "01" },
                    },
                Exclusions =
                    new string[]
                    {
                        "347n",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "HEIGHT (in), log",
                SourceLine = 178,
            },
            new Gs1AiDefinition
            {
                AiStart = "3480",
                AiEnd = "3485",
                WildcardPattern = "348n",
                PredefinedLength = true,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "00" },
                        new string[] { "01" },
                    },
                Exclusions =
                    new string[]
                    {
                        "348n",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "HEIGHT (ft), log",
                SourceLine = 179,
            },
            new Gs1AiDefinition
            {
                AiStart = "3490",
                AiEnd = "3495",
                WildcardPattern = "349n",
                PredefinedLength = true,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "00" },
                        new string[] { "01" },
                    },
                Exclusions =
                    new string[]
                    {
                        "349n",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "HEIGHT (yd), log",
                SourceLine = 180,
            },
            new Gs1AiDefinition
            {
                AiStart = "3500",
                AiEnd = "3505",
                WildcardPattern = "350n",
                PredefinedLength = true,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "02" },
                    },
                Exclusions =
                    new string[]
                    {
                        "350n",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "AREA (in²)",
                SourceLine = 181,
            },
            new Gs1AiDefinition
            {
                AiStart = "3510",
                AiEnd = "3515",
                WildcardPattern = "351n",
                PredefinedLength = true,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "02" },
                    },
                Exclusions =
                    new string[]
                    {
                        "351n",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "AREA (ft²)",
                SourceLine = 182,
            },
            new Gs1AiDefinition
            {
                AiStart = "3520",
                AiEnd = "3525",
                WildcardPattern = "352n",
                PredefinedLength = true,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "02" },
                    },
                Exclusions =
                    new string[]
                    {
                        "352n",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "AREA (yd²)",
                SourceLine = 183,
            },
            new Gs1AiDefinition
            {
                AiStart = "3530",
                AiEnd = "3535",
                WildcardPattern = "353n",
                PredefinedLength = true,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "00" },
                        new string[] { "01" },
                    },
                Exclusions =
                    new string[]
                    {
                        "353n",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "AREA (in²), log",
                SourceLine = 184,
            },
            new Gs1AiDefinition
            {
                AiStart = "3540",
                AiEnd = "3545",
                WildcardPattern = "354n",
                PredefinedLength = true,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "00" },
                        new string[] { "01" },
                    },
                Exclusions =
                    new string[]
                    {
                        "354n",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "AREA (ft²), log",
                SourceLine = 185,
            },
            new Gs1AiDefinition
            {
                AiStart = "3550",
                AiEnd = "3555",
                WildcardPattern = "355n",
                PredefinedLength = true,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "00" },
                        new string[] { "01" },
                    },
                Exclusions =
                    new string[]
                    {
                        "355n",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "AREA (yd²), log",
                SourceLine = 186,
            },
            new Gs1AiDefinition
            {
                AiStart = "3560",
                AiEnd = "3565",
                WildcardPattern = "356n",
                PredefinedLength = true,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "02" },
                    },
                Exclusions =
                    new string[]
                    {
                        "356n",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "NET WEIGHT (tr oz)",
                SourceLine = 187,
            },
            new Gs1AiDefinition
            {
                AiStart = "3570",
                AiEnd = "3575",
                WildcardPattern = "357n",
                PredefinedLength = true,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "02" },
                    },
                Exclusions =
                    new string[]
                    {
                        "357n",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "NET VOLUME (oz)",
                SourceLine = 188,
            },
            new Gs1AiDefinition
            {
                AiStart = "3600",
                AiEnd = "3605",
                WildcardPattern = "360n",
                PredefinedLength = true,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "02" },
                    },
                Exclusions =
                    new string[]
                    {
                        "360n",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "NET VOLUME (qt (US))",
                SourceLine = 189,
            },
            new Gs1AiDefinition
            {
                AiStart = "3610",
                AiEnd = "3615",
                WildcardPattern = "361n",
                PredefinedLength = true,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "02" },
                    },
                Exclusions =
                    new string[]
                    {
                        "361n",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "NET VOLUME (gal.)",
                SourceLine = 190,
            },
            new Gs1AiDefinition
            {
                AiStart = "3620",
                AiEnd = "3625",
                WildcardPattern = "362n",
                PredefinedLength = true,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "00" },
                        new string[] { "01" },
                    },
                Exclusions =
                    new string[]
                    {
                        "362n",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "VOLUME (qt (US)), log",
                SourceLine = 191,
            },
            new Gs1AiDefinition
            {
                AiStart = "3630",
                AiEnd = "3635",
                WildcardPattern = "363n",
                PredefinedLength = true,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "00" },
                        new string[] { "01" },
                    },
                Exclusions =
                    new string[]
                    {
                        "363n",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "VOLUME (gal (US)), log",
                SourceLine = 192,
            },
            new Gs1AiDefinition
            {
                AiStart = "3640",
                AiEnd = "3645",
                WildcardPattern = "364n",
                PredefinedLength = true,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "02" },
                    },
                Exclusions =
                    new string[]
                    {
                        "364n",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "NET VOLUME (in³)",
                SourceLine = 193,
            },
            new Gs1AiDefinition
            {
                AiStart = "3650",
                AiEnd = "3655",
                WildcardPattern = "365n",
                PredefinedLength = true,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "02" },
                    },
                Exclusions =
                    new string[]
                    {
                        "365n",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "NET VOLUME (ft³)",
                SourceLine = 194,
            },
            new Gs1AiDefinition
            {
                AiStart = "3660",
                AiEnd = "3665",
                WildcardPattern = "366n",
                PredefinedLength = true,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "02" },
                    },
                Exclusions =
                    new string[]
                    {
                        "366n",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "NET VOLUME (yd³)",
                SourceLine = 195,
            },
            new Gs1AiDefinition
            {
                AiStart = "3670",
                AiEnd = "3675",
                WildcardPattern = "367n",
                PredefinedLength = true,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "00" },
                        new string[] { "01" },
                    },
                Exclusions =
                    new string[]
                    {
                        "367n",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "VOLUME (in³), log",
                SourceLine = 196,
            },
            new Gs1AiDefinition
            {
                AiStart = "3680",
                AiEnd = "3685",
                WildcardPattern = "368n",
                PredefinedLength = true,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "00" },
                        new string[] { "01" },
                    },
                Exclusions =
                    new string[]
                    {
                        "368n",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "VOLUME (ft³), log",
                SourceLine = 197,
            },
            new Gs1AiDefinition
            {
                AiStart = "3690",
                AiEnd = "3695",
                WildcardPattern = "369n",
                PredefinedLength = true,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "00" },
                        new string[] { "01" },
                    },
                Exclusions =
                    new string[]
                    {
                        "369n",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "VOLUME (yd³), log",
                SourceLine = 198,
            },
            new Gs1AiDefinition
            {
                AiStart = "37",
                AiEnd = "37",
                WildcardPattern = "37",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 1,
                            MaximumLength = 8,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "00", "02" },
                        new string[] { "00", "8026" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "COUNT",
                SourceLine = 199,
            },
            new Gs1AiDefinition
            {
                AiStart = "3900",
                AiEnd = "3909",
                WildcardPattern = "390n",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 1,
                            MaximumLength = 15,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "255" },
                        new string[] { "8020" },
                    },
                Exclusions =
                    new string[]
                    {
                        "390n",
                        "391n",
                        "394n",
                        "8111",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "AMOUNT",
                SourceLine = 200,
            },
            new Gs1AiDefinition
            {
                AiStart = "3910",
                AiEnd = "3919",
                WildcardPattern = "391n",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 3,
                            MaximumLength = 3,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "iso4217",
                                },
                        },
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 1,
                            MaximumLength = 15,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "8020" },
                    },
                Exclusions =
                    new string[]
                    {
                        "391n",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "AMOUNT",
                SourceLine = 201,
            },
            new Gs1AiDefinition
            {
                AiStart = "3920",
                AiEnd = "3929",
                WildcardPattern = "392n",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 1,
                            MaximumLength = 15,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01", "30" },
                        new string[] { "01", "31nn" },
                        new string[] { "01", "32nn" },
                        new string[] { "01", "35nn" },
                        new string[] { "01", "36nn" },
                    },
                Exclusions =
                    new string[]
                    {
                        "392n",
                        "393n",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "PRICE",
                SourceLine = 202,
            },
            new Gs1AiDefinition
            {
                AiStart = "3930",
                AiEnd = "3939",
                WildcardPattern = "393n",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 3,
                            MaximumLength = 3,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "iso4217",
                                },
                        },
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 1,
                            MaximumLength = 15,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "30" },
                        new string[] { "31nn" },
                        new string[] { "32nn" },
                        new string[] { "35nn" },
                        new string[] { "36nn" },
                    },
                Exclusions =
                    new string[]
                    {
                        "393n",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "PRICE",
                SourceLine = 203,
            },
            new Gs1AiDefinition
            {
                AiStart = "3940",
                AiEnd = "3943",
                WildcardPattern = "394n",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 4,
                            MaximumLength = 4,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "255" },
                    },
                Exclusions =
                    new string[]
                    {
                        "394n",
                        "8111",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "PRCNT OFF",
                SourceLine = 204,
            },
            new Gs1AiDefinition
            {
                AiStart = "3950",
                AiEnd = "3955",
                WildcardPattern = "395n",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "30" },
                        new string[] { "31nn" },
                        new string[] { "32nn" },
                        new string[] { "35nn" },
                        new string[] { "36nn" },
                    },
                Exclusions =
                    new string[]
                    {
                        "392n",
                        "393n",
                        "395n",
                        "8005",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "PRICE/UoM",
                SourceLine = 205,
            },
            new Gs1AiDefinition
            {
                AiStart = "400",
                AiEnd = "400",
                WildcardPattern = "400",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 30,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[0][],
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "ORDER NUMBER",
                SourceLine = 206,
            },
            new Gs1AiDefinition
            {
                AiStart = "401",
                AiEnd = "401",
                WildcardPattern = "401",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 30,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "gcppos1",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[0][],
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = true,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "GINC",
                SourceLine = 207,
            },
            new Gs1AiDefinition
            {
                AiStart = "402",
                AiEnd = "402",
                WildcardPattern = "402",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 17,
                            MaximumLength = 17,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "csum",
                                    "gcppos1",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[0][],
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = true,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "GSIN",
                SourceLine = 208,
            },
            new Gs1AiDefinition
            {
                AiStart = "403",
                AiEnd = "403",
                WildcardPattern = "403",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 30,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "00" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "ROUTE",
                SourceLine = 209,
            },
            new Gs1AiDefinition
            {
                AiStart = "410",
                AiEnd = "410",
                WildcardPattern = "410",
                PredefinedLength = true,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 13,
                            MaximumLength = 13,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "csum",
                                    "gcppos1",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[0][],
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "SHIP TO LOC",
                SourceLine = 210,
            },
            new Gs1AiDefinition
            {
                AiStart = "411",
                AiEnd = "411",
                WildcardPattern = "411",
                PredefinedLength = true,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 13,
                            MaximumLength = 13,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "csum",
                                    "gcppos1",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[0][],
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "BILL TO",
                SourceLine = 211,
            },
            new Gs1AiDefinition
            {
                AiStart = "412",
                AiEnd = "412",
                WildcardPattern = "412",
                PredefinedLength = true,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 13,
                            MaximumLength = 13,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "csum",
                                    "gcppos1",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[0][],
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "PURCHASE FROM",
                SourceLine = 212,
            },
            new Gs1AiDefinition
            {
                AiStart = "413",
                AiEnd = "413",
                WildcardPattern = "413",
                PredefinedLength = true,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 13,
                            MaximumLength = 13,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "csum",
                                    "gcppos1",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[0][],
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "SHIP FOR LOC",
                SourceLine = 213,
            },
            new Gs1AiDefinition
            {
                AiStart = "414",
                AiEnd = "414",
                WildcardPattern = "414",
                PredefinedLength = true,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 13,
                            MaximumLength = 13,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "csum",
                                    "gcppos1",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[0][],
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = true,
                DigitalLinkQualifierSequences =
                    new string[][]
                    {
                        new string[] { "254" },
                        new string[] { "7040" },
                    },
                Title = "LOC No.",
                SourceLine = 214,
            },
            new Gs1AiDefinition
            {
                AiStart = "415",
                AiEnd = "415",
                WildcardPattern = "415",
                PredefinedLength = true,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 13,
                            MaximumLength = 13,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "csum",
                                    "gcppos1",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "8020" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = true,
                DigitalLinkQualifierSequences =
                    new string[][]
                    {
                        new string[] { "8020" },
                    },
                Title = "PAY TO",
                SourceLine = 215,
            },
            new Gs1AiDefinition
            {
                AiStart = "416",
                AiEnd = "416",
                WildcardPattern = "416",
                PredefinedLength = true,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 13,
                            MaximumLength = 13,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "csum",
                                    "gcppos1",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[0][],
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "PROD/SERV LOC",
                SourceLine = 216,
            },
            new Gs1AiDefinition
            {
                AiStart = "417",
                AiEnd = "417",
                WildcardPattern = "417",
                PredefinedLength = true,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 13,
                            MaximumLength = 13,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "csum",
                                    "gcppos1",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[0][],
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = true,
                DigitalLinkQualifierSequences =
                    new string[][]
                    {
                        new string[] { "7040" },
                    },
                Title = "PARTY",
                SourceLine = 217,
            },
            new Gs1AiDefinition
            {
                AiStart = "420",
                AiEnd = "420",
                WildcardPattern = "420",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 20,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[0][],
                Exclusions =
                    new string[]
                    {
                        "421",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "SHIP TO POST",
                SourceLine = 218,
            },
            new Gs1AiDefinition
            {
                AiStart = "421",
                AiEnd = "421",
                WildcardPattern = "421",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 3,
                            MaximumLength = 3,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "iso3166",
                                },
                        },
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 9,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[0][],
                Exclusions =
                    new string[]
                    {
                        "4307",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "SHIP TO POST",
                SourceLine = 219,
            },
            new Gs1AiDefinition
            {
                AiStart = "422",
                AiEnd = "422",
                WildcardPattern = "422",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 3,
                            MaximumLength = 3,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "iso3166",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "02" },
                        new string[] { "03" },
                        new string[] { "8006" },
                        new string[] { "8026" },
                    },
                Exclusions =
                    new string[]
                    {
                        "426",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "ORIGIN",
                SourceLine = 220,
            },
            new Gs1AiDefinition
            {
                AiStart = "423",
                AiEnd = "423",
                WildcardPattern = "423",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 3,
                            MaximumLength = 3,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "iso3166",
                                },
                        },
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 3,
                            MaximumLength = 3,
                            Optional = true,
                            Linters =
                                new string[]
                                {
                                    "iso3166",
                                },
                        },
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 3,
                            MaximumLength = 3,
                            Optional = true,
                            Linters =
                                new string[]
                                {
                                    "iso3166",
                                },
                        },
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 3,
                            MaximumLength = 3,
                            Optional = true,
                            Linters =
                                new string[]
                                {
                                    "iso3166",
                                },
                        },
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 3,
                            MaximumLength = 3,
                            Optional = true,
                            Linters =
                                new string[]
                                {
                                    "iso3166",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "02" },
                        new string[] { "03" },
                    },
                Exclusions =
                    new string[]
                    {
                        "426",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "COUNTRY - INITIAL PROCESS",
                SourceLine = 221,
            },
            new Gs1AiDefinition
            {
                AiStart = "424",
                AiEnd = "424",
                WildcardPattern = "424",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 3,
                            MaximumLength = 3,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "iso3166",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "02" },
                        new string[] { "03" },
                    },
                Exclusions =
                    new string[]
                    {
                        "426",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "COUNTRY - PROCESS",
                SourceLine = 222,
            },
            new Gs1AiDefinition
            {
                AiStart = "425",
                AiEnd = "425",
                WildcardPattern = "425",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 3,
                            MaximumLength = 3,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "iso3166",
                                },
                        },
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 3,
                            MaximumLength = 3,
                            Optional = true,
                            Linters =
                                new string[]
                                {
                                    "iso3166",
                                },
                        },
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 3,
                            MaximumLength = 3,
                            Optional = true,
                            Linters =
                                new string[]
                                {
                                    "iso3166",
                                },
                        },
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 3,
                            MaximumLength = 3,
                            Optional = true,
                            Linters =
                                new string[]
                                {
                                    "iso3166",
                                },
                        },
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 3,
                            MaximumLength = 3,
                            Optional = true,
                            Linters =
                                new string[]
                                {
                                    "iso3166",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "02" },
                        new string[] { "03" },
                    },
                Exclusions =
                    new string[]
                    {
                        "426",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "COUNTRY - DISASSEMBLY",
                SourceLine = 223,
            },
            new Gs1AiDefinition
            {
                AiStart = "426",
                AiEnd = "426",
                WildcardPattern = "426",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 3,
                            MaximumLength = 3,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "iso3166",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "02" },
                        new string[] { "03" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "COUNTRY - FULL PROCESS",
                SourceLine = 224,
            },
            new Gs1AiDefinition
            {
                AiStart = "427",
                AiEnd = "427",
                WildcardPattern = "427",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 3,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01", "422" },
                        new string[] { "02", "422" },
                        new string[] { "03", "422" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "ORIGIN SUBDIVISION",
                SourceLine = 225,
            },
            new Gs1AiDefinition
            {
                AiStart = "4300",
                AiEnd = "4300",
                WildcardPattern = "4300",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 35,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "pcenc",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "00" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "SHIP TO COMP",
                SourceLine = 226,
            },
            new Gs1AiDefinition
            {
                AiStart = "4301",
                AiEnd = "4301",
                WildcardPattern = "4301",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 35,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "pcenc",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "00" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "SHIP TO NAME",
                SourceLine = 227,
            },
            new Gs1AiDefinition
            {
                AiStart = "4302",
                AiEnd = "4302",
                WildcardPattern = "4302",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 70,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "pcenc",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "00" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "SHIP TO ADD1",
                SourceLine = 228,
            },
            new Gs1AiDefinition
            {
                AiStart = "4303",
                AiEnd = "4303",
                WildcardPattern = "4303",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 70,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "pcenc",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "4302" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "SHIP TO ADD2",
                SourceLine = 229,
            },
            new Gs1AiDefinition
            {
                AiStart = "4304",
                AiEnd = "4304",
                WildcardPattern = "4304",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 70,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "pcenc",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "00" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "SHIP TO SUB",
                SourceLine = 230,
            },
            new Gs1AiDefinition
            {
                AiStart = "4305",
                AiEnd = "4305",
                WildcardPattern = "4305",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 70,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "pcenc",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "00" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "SHIP TO LOC",
                SourceLine = 231,
            },
            new Gs1AiDefinition
            {
                AiStart = "4306",
                AiEnd = "4306",
                WildcardPattern = "4306",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 70,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "pcenc",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "00" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "SHIP TO REG",
                SourceLine = 232,
            },
            new Gs1AiDefinition
            {
                AiStart = "4307",
                AiEnd = "4307",
                WildcardPattern = "4307",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 2,
                            MaximumLength = 2,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "iso3166alpha2",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "00" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "SHIP TO COUNTRY",
                SourceLine = 233,
            },
            new Gs1AiDefinition
            {
                AiStart = "4308",
                AiEnd = "4308",
                WildcardPattern = "4308",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 30,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "00" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "SHIP TO PHONE",
                SourceLine = 234,
            },
            new Gs1AiDefinition
            {
                AiStart = "4309",
                AiEnd = "4309",
                WildcardPattern = "4309",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 10,
                            MaximumLength = 10,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "latitude",
                                },
                        },
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 10,
                            MaximumLength = 10,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "longitude",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "00" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "SHIP TO GEO",
                SourceLine = 235,
            },
            new Gs1AiDefinition
            {
                AiStart = "4310",
                AiEnd = "4310",
                WildcardPattern = "4310",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 35,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "pcenc",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "00" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "RTN TO COMP",
                SourceLine = 236,
            },
            new Gs1AiDefinition
            {
                AiStart = "4311",
                AiEnd = "4311",
                WildcardPattern = "4311",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 35,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "pcenc",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "00" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "RTN TO NAME",
                SourceLine = 237,
            },
            new Gs1AiDefinition
            {
                AiStart = "4312",
                AiEnd = "4312",
                WildcardPattern = "4312",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 70,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "pcenc",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "00" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "RTN TO ADD1",
                SourceLine = 238,
            },
            new Gs1AiDefinition
            {
                AiStart = "4313",
                AiEnd = "4313",
                WildcardPattern = "4313",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 70,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "pcenc",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "4312" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "RTN TO ADD2",
                SourceLine = 239,
            },
            new Gs1AiDefinition
            {
                AiStart = "4314",
                AiEnd = "4314",
                WildcardPattern = "4314",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 70,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "pcenc",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "00" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "RTN TO SUB",
                SourceLine = 240,
            },
            new Gs1AiDefinition
            {
                AiStart = "4315",
                AiEnd = "4315",
                WildcardPattern = "4315",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 70,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "pcenc",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "00" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "RTN TO LOC",
                SourceLine = 241,
            },
            new Gs1AiDefinition
            {
                AiStart = "4316",
                AiEnd = "4316",
                WildcardPattern = "4316",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 70,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "pcenc",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "00" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "RTN TO REG",
                SourceLine = 242,
            },
            new Gs1AiDefinition
            {
                AiStart = "4317",
                AiEnd = "4317",
                WildcardPattern = "4317",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 2,
                            MaximumLength = 2,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "iso3166alpha2",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "00" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "RTN TO COUNTRY",
                SourceLine = 243,
            },
            new Gs1AiDefinition
            {
                AiStart = "4318",
                AiEnd = "4318",
                WildcardPattern = "4318",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 20,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "00" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "RTN TO POST",
                SourceLine = 244,
            },
            new Gs1AiDefinition
            {
                AiStart = "4319",
                AiEnd = "4319",
                WildcardPattern = "4319",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 30,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "00" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "RTN TO PHONE",
                SourceLine = 245,
            },
            new Gs1AiDefinition
            {
                AiStart = "4320",
                AiEnd = "4320",
                WildcardPattern = "4320",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 35,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "pcenc",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "00" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "SRV DESCRIPTION",
                SourceLine = 246,
            },
            new Gs1AiDefinition
            {
                AiStart = "4321",
                AiEnd = "4321",
                WildcardPattern = "4321",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 1,
                            MaximumLength = 1,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "yesno",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "00" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "DANGEROUS GOODS",
                SourceLine = 247,
            },
            new Gs1AiDefinition
            {
                AiStart = "4322",
                AiEnd = "4322",
                WildcardPattern = "4322",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 1,
                            MaximumLength = 1,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "yesno",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "00" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "AUTH TO LEAVE",
                SourceLine = 248,
            },
            new Gs1AiDefinition
            {
                AiStart = "4323",
                AiEnd = "4323",
                WildcardPattern = "4323",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 1,
                            MaximumLength = 1,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "yesno",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "00" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "SIG REQUIRED",
                SourceLine = 249,
            },
            new Gs1AiDefinition
            {
                AiStart = "4324",
                AiEnd = "4324",
                WildcardPattern = "4324",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "yymmd0",
                                },
                        },
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 4,
                            MaximumLength = 4,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "hhmi",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "00" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "NOT BEF DEL DT",
                SourceLine = 250,
            },
            new Gs1AiDefinition
            {
                AiStart = "4325",
                AiEnd = "4325",
                WildcardPattern = "4325",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "yymmd0",
                                },
                        },
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 4,
                            MaximumLength = 4,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "hhmi",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "00" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "NOT AFT DEL DT",
                SourceLine = 251,
            },
            new Gs1AiDefinition
            {
                AiStart = "4326",
                AiEnd = "4326",
                WildcardPattern = "4326",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "yymmdd",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "00" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "REL DATE",
                SourceLine = 252,
            },
            new Gs1AiDefinition
            {
                AiStart = "4330",
                AiEnd = "4330",
                WildcardPattern = "4330",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 1,
                            Optional = true,
                            Linters =
                                new string[]
                                {
                                    "hyphen",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "00" },
                    },
                Exclusions =
                    new string[]
                    {
                        "4331",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "MAX TEMP F.",
                SourceLine = 253,
            },
            new Gs1AiDefinition
            {
                AiStart = "4331",
                AiEnd = "4331",
                WildcardPattern = "4331",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 1,
                            Optional = true,
                            Linters =
                                new string[]
                                {
                                    "hyphen",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "00" },
                    },
                Exclusions =
                    new string[]
                    {
                        "4330",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "MAX TEMP C.",
                SourceLine = 254,
            },
            new Gs1AiDefinition
            {
                AiStart = "4332",
                AiEnd = "4332",
                WildcardPattern = "4332",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 1,
                            Optional = true,
                            Linters =
                                new string[]
                                {
                                    "hyphen",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "00" },
                    },
                Exclusions =
                    new string[]
                    {
                        "4333",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "MIN TEMP F.",
                SourceLine = 255,
            },
            new Gs1AiDefinition
            {
                AiStart = "4333",
                AiEnd = "4333",
                WildcardPattern = "4333",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 1,
                            Optional = true,
                            Linters =
                                new string[]
                                {
                                    "hyphen",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "00" },
                    },
                Exclusions =
                    new string[]
                    {
                        "4332",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "MIN TEMP C.",
                SourceLine = 256,
            },
            new Gs1AiDefinition
            {
                AiStart = "7001",
                AiEnd = "7001",
                WildcardPattern = "7001",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 13,
                            MaximumLength = 13,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "02" },
                        new string[] { "8006" },
                        new string[] { "8026" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "NSN",
                SourceLine = 257,
            },
            new Gs1AiDefinition
            {
                AiStart = "7002",
                AiEnd = "7002",
                WildcardPattern = "7002",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 30,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "02" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "MEAT CUT",
                SourceLine = 258,
            },
            new Gs1AiDefinition
            {
                AiStart = "7003",
                AiEnd = "7003",
                WildcardPattern = "7003",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "yymmdd",
                                },
                        },
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 4,
                            MaximumLength = 4,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "hhmi",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "02" },
                        new string[] { "03" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "EXPIRY TIME",
                SourceLine = 259,
            },
            new Gs1AiDefinition
            {
                AiStart = "7004",
                AiEnd = "7004",
                WildcardPattern = "7004",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 1,
                            MaximumLength = 4,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01", "10" },
                        new string[] { "03", "10" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "ACTIVE POTENCY",
                SourceLine = 260,
            },
            new Gs1AiDefinition
            {
                AiStart = "7005",
                AiEnd = "7005",
                WildcardPattern = "7005",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 12,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "02" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "CATCH AREA",
                SourceLine = 261,
            },
            new Gs1AiDefinition
            {
                AiStart = "7006",
                AiEnd = "7006",
                WildcardPattern = "7006",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "yymmdd",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "02" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "FIRST FREEZE DATE",
                SourceLine = 262,
            },
            new Gs1AiDefinition
            {
                AiStart = "7007",
                AiEnd = "7007",
                WildcardPattern = "7007",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "yymmdd",
                                },
                        },
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = true,
                            Linters =
                                new string[]
                                {
                                    "yymmdd",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "02" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "HARVEST DATE",
                SourceLine = 263,
            },
            new Gs1AiDefinition
            {
                AiStart = "7008",
                AiEnd = "7008",
                WildcardPattern = "7008",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 3,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "02" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "AQUATIC SPECIES",
                SourceLine = 264,
            },
            new Gs1AiDefinition
            {
                AiStart = "7009",
                AiEnd = "7009",
                WildcardPattern = "7009",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 10,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "02" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "FISHING GEAR TYPE",
                SourceLine = 265,
            },
            new Gs1AiDefinition
            {
                AiStart = "7010",
                AiEnd = "7010",
                WildcardPattern = "7010",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 2,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "02" },
                        new string[] { "03" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "PROD METHOD",
                SourceLine = 266,
            },
            new Gs1AiDefinition
            {
                AiStart = "7011",
                AiEnd = "7011",
                WildcardPattern = "7011",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "yymmdd",
                                },
                        },
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 4,
                            MaximumLength = 4,
                            Optional = true,
                            Linters =
                                new string[]
                                {
                                    "hhmi",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "02" },
                        new string[] { "03" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "TEST BY DATE",
                SourceLine = 267,
            },
            new Gs1AiDefinition
            {
                AiStart = "7020",
                AiEnd = "7020",
                WildcardPattern = "7020",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 20,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01", "416" },
                        new string[] { "03", "416" },
                        new string[] { "8006", "416" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "REFURB LOT",
                SourceLine = 268,
            },
            new Gs1AiDefinition
            {
                AiStart = "7021",
                AiEnd = "7021",
                WildcardPattern = "7021",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 20,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "03" },
                        new string[] { "8006" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "FUNC STAT",
                SourceLine = 269,
            },
            new Gs1AiDefinition
            {
                AiStart = "7022",
                AiEnd = "7022",
                WildcardPattern = "7022",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 20,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01", "7021" },
                        new string[] { "03", "7021" },
                        new string[] { "8006", "7021" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "REV STAT",
                SourceLine = 270,
            },
            new Gs1AiDefinition
            {
                AiStart = "7023",
                AiEnd = "7023",
                WildcardPattern = "7023",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 30,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "gcppos1",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[0][],
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "GIAI - ASSEMBLY",
                SourceLine = 271,
            },
            new Gs1AiDefinition
            {
                AiStart = "7030",
                AiEnd = "7030",
                WildcardPattern = "7030",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 3,
                            MaximumLength = 3,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "iso3166999",
                                },
                        },
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 27,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "02" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "PROCESSOR # 0",
                SourceLine = 272,
            },
            new Gs1AiDefinition
            {
                AiStart = "7031",
                AiEnd = "7031",
                WildcardPattern = "7031",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 3,
                            MaximumLength = 3,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "iso3166999",
                                },
                        },
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 27,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "02" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "PROCESSOR # 1",
                SourceLine = 273,
            },
            new Gs1AiDefinition
            {
                AiStart = "7032",
                AiEnd = "7032",
                WildcardPattern = "7032",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 3,
                            MaximumLength = 3,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "iso3166999",
                                },
                        },
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 27,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "02" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "PROCESSOR # 2",
                SourceLine = 274,
            },
            new Gs1AiDefinition
            {
                AiStart = "7033",
                AiEnd = "7033",
                WildcardPattern = "7033",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 3,
                            MaximumLength = 3,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "iso3166999",
                                },
                        },
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 27,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "02" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "PROCESSOR # 3",
                SourceLine = 275,
            },
            new Gs1AiDefinition
            {
                AiStart = "7034",
                AiEnd = "7034",
                WildcardPattern = "7034",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 3,
                            MaximumLength = 3,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "iso3166999",
                                },
                        },
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 27,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "02" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "PROCESSOR # 4",
                SourceLine = 276,
            },
            new Gs1AiDefinition
            {
                AiStart = "7035",
                AiEnd = "7035",
                WildcardPattern = "7035",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 3,
                            MaximumLength = 3,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "iso3166999",
                                },
                        },
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 27,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "02" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "PROCESSOR # 5",
                SourceLine = 277,
            },
            new Gs1AiDefinition
            {
                AiStart = "7036",
                AiEnd = "7036",
                WildcardPattern = "7036",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 3,
                            MaximumLength = 3,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "iso3166999",
                                },
                        },
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 27,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "02" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "PROCESSOR # 6",
                SourceLine = 278,
            },
            new Gs1AiDefinition
            {
                AiStart = "7037",
                AiEnd = "7037",
                WildcardPattern = "7037",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 3,
                            MaximumLength = 3,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "iso3166999",
                                },
                        },
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 27,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "02" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "PROCESSOR # 7",
                SourceLine = 279,
            },
            new Gs1AiDefinition
            {
                AiStart = "7038",
                AiEnd = "7038",
                WildcardPattern = "7038",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 3,
                            MaximumLength = 3,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "iso3166999",
                                },
                        },
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 27,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "02" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "PROCESSOR # 8",
                SourceLine = 280,
            },
            new Gs1AiDefinition
            {
                AiStart = "7039",
                AiEnd = "7039",
                WildcardPattern = "7039",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 3,
                            MaximumLength = 3,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "iso3166999",
                                },
                        },
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 27,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "02" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "PROCESSOR # 9",
                SourceLine = 281,
            },
            new Gs1AiDefinition
            {
                AiStart = "7040",
                AiEnd = "7040",
                WildcardPattern = "7040",
                PredefinedLength = false,
                DigitalLinkDataAttribute = false,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 1,
                            MaximumLength = 1,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 1,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 1,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 1,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "importeridx",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[0][],
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "UIC+EXT",
                SourceLine = 282,
            },
            new Gs1AiDefinition
            {
                AiStart = "7041",
                AiEnd = "7041",
                WildcardPattern = "7041",
                PredefinedLength = false,
                DigitalLinkDataAttribute = false,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 4,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "packagetype",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "00" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "UFRGT UNIT TYPE",
                SourceLine = 283,
            },
            new Gs1AiDefinition
            {
                AiStart = "710",
                AiEnd = "710",
                WildcardPattern = "710",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 20,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "NHRN PZN",
                SourceLine = 284,
            },
            new Gs1AiDefinition
            {
                AiStart = "711",
                AiEnd = "711",
                WildcardPattern = "711",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 20,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "NHRN CIP",
                SourceLine = 285,
            },
            new Gs1AiDefinition
            {
                AiStart = "712",
                AiEnd = "712",
                WildcardPattern = "712",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 20,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "NHRN CN",
                SourceLine = 286,
            },
            new Gs1AiDefinition
            {
                AiStart = "713",
                AiEnd = "713",
                WildcardPattern = "713",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 20,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "NHRN DRN",
                SourceLine = 287,
            },
            new Gs1AiDefinition
            {
                AiStart = "714",
                AiEnd = "714",
                WildcardPattern = "714",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 20,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "NHRN AIM",
                SourceLine = 288,
            },
            new Gs1AiDefinition
            {
                AiStart = "715",
                AiEnd = "715",
                WildcardPattern = "715",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 20,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "NHRN NDC",
                SourceLine = 289,
            },
            new Gs1AiDefinition
            {
                AiStart = "716",
                AiEnd = "716",
                WildcardPattern = "716",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 20,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "NHRN AIC",
                SourceLine = 290,
            },
            new Gs1AiDefinition
            {
                AiStart = "717",
                AiEnd = "717",
                WildcardPattern = "717",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 20,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "NHRN SRN",
                SourceLine = 291,
            },
            new Gs1AiDefinition
            {
                AiStart = "7230",
                AiEnd = "7230",
                WildcardPattern = "7230",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 2,
                            MaximumLength = 2,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 28,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "8004" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "CERT # 1",
                SourceLine = 292,
            },
            new Gs1AiDefinition
            {
                AiStart = "7231",
                AiEnd = "7231",
                WildcardPattern = "7231",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 2,
                            MaximumLength = 2,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 28,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "8004" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "CERT # 2",
                SourceLine = 293,
            },
            new Gs1AiDefinition
            {
                AiStart = "7232",
                AiEnd = "7232",
                WildcardPattern = "7232",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 2,
                            MaximumLength = 2,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 28,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "8004" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "CERT # 3",
                SourceLine = 294,
            },
            new Gs1AiDefinition
            {
                AiStart = "7233",
                AiEnd = "7233",
                WildcardPattern = "7233",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 2,
                            MaximumLength = 2,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 28,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "8004" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "CERT # 4",
                SourceLine = 295,
            },
            new Gs1AiDefinition
            {
                AiStart = "7234",
                AiEnd = "7234",
                WildcardPattern = "7234",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 2,
                            MaximumLength = 2,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 28,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "8004" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "CERT # 5",
                SourceLine = 296,
            },
            new Gs1AiDefinition
            {
                AiStart = "7235",
                AiEnd = "7235",
                WildcardPattern = "7235",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 2,
                            MaximumLength = 2,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 28,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "8004" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "CERT # 6",
                SourceLine = 297,
            },
            new Gs1AiDefinition
            {
                AiStart = "7236",
                AiEnd = "7236",
                WildcardPattern = "7236",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 2,
                            MaximumLength = 2,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 28,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "8004" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "CERT # 7",
                SourceLine = 298,
            },
            new Gs1AiDefinition
            {
                AiStart = "7237",
                AiEnd = "7237",
                WildcardPattern = "7237",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 2,
                            MaximumLength = 2,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 28,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "8004" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "CERT # 8",
                SourceLine = 299,
            },
            new Gs1AiDefinition
            {
                AiStart = "7238",
                AiEnd = "7238",
                WildcardPattern = "7238",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 2,
                            MaximumLength = 2,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 28,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "8004" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "CERT # 9",
                SourceLine = 300,
            },
            new Gs1AiDefinition
            {
                AiStart = "7239",
                AiEnd = "7239",
                WildcardPattern = "7239",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 2,
                            MaximumLength = 2,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 28,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "8004" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "CERT # 10",
                SourceLine = 301,
            },
            new Gs1AiDefinition
            {
                AiStart = "7240",
                AiEnd = "7240",
                WildcardPattern = "7240",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 20,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "8006" },
                    },
                Exclusions =
                    new string[]
                    {
                        "03",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "PROTOCOL",
                SourceLine = 302,
            },
            new Gs1AiDefinition
            {
                AiStart = "7241",
                AiEnd = "7241",
                WildcardPattern = "7241",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 2,
                            MaximumLength = 2,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "mediatype",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "8017" },
                        new string[] { "8018" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "AIDC MEDIA TYPE",
                SourceLine = 303,
            },
            new Gs1AiDefinition
            {
                AiStart = "7242",
                AiEnd = "7242",
                WildcardPattern = "7242",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 25,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "8017" },
                        new string[] { "8018" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "VCN",
                SourceLine = 304,
            },
            new Gs1AiDefinition
            {
                AiStart = "7250",
                AiEnd = "7250",
                WildcardPattern = "7250",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 8,
                            MaximumLength = 8,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "yyyymmdd",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "8018" },
                    },
                Exclusions =
                    new string[]
                    {
                        "7251",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "DOB",
                SourceLine = 305,
            },
            new Gs1AiDefinition
            {
                AiStart = "7251",
                AiEnd = "7251",
                WildcardPattern = "7251",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 8,
                            MaximumLength = 8,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "yyyymmdd",
                                },
                        },
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 4,
                            MaximumLength = 4,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "hhmi",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "8018" },
                    },
                Exclusions =
                    new string[]
                    {
                        "7250",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "DOB TIME",
                SourceLine = 306,
            },
            new Gs1AiDefinition
            {
                AiStart = "7252",
                AiEnd = "7252",
                WildcardPattern = "7252",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 1,
                            MaximumLength = 1,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "iso5218",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "8018" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "BIO SEX",
                SourceLine = 307,
            },
            new Gs1AiDefinition
            {
                AiStart = "7253",
                AiEnd = "7253",
                WildcardPattern = "7253",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 40,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "pcenc",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "8017" },
                        new string[] { "8018" },
                    },
                Exclusions =
                    new string[]
                    {
                        "7256",
                        "7259",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "FAMILY NAME",
                SourceLine = 308,
            },
            new Gs1AiDefinition
            {
                AiStart = "7254",
                AiEnd = "7254",
                WildcardPattern = "7254",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 40,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "pcenc",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "8017" },
                        new string[] { "8018" },
                    },
                Exclusions =
                    new string[]
                    {
                        "7256",
                        "7259",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "GIVEN NAME",
                SourceLine = 309,
            },
            new Gs1AiDefinition
            {
                AiStart = "7255",
                AiEnd = "7255",
                WildcardPattern = "7255",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 10,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "8017" },
                        new string[] { "8018" },
                    },
                Exclusions =
                    new string[]
                    {
                        "7256",
                        "7259",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "SUFFIX",
                SourceLine = 310,
            },
            new Gs1AiDefinition
            {
                AiStart = "7256",
                AiEnd = "7256",
                WildcardPattern = "7256",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 90,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "pcenc",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "8017" },
                        new string[] { "8018" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "FULL NAME",
                SourceLine = 311,
            },
            new Gs1AiDefinition
            {
                AiStart = "7257",
                AiEnd = "7257",
                WildcardPattern = "7257",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 70,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "pcenc",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "8018" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "PERSON ADDR",
                SourceLine = 312,
            },
            new Gs1AiDefinition
            {
                AiStart = "7258",
                AiEnd = "7258",
                WildcardPattern = "7258",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 3,
                            MaximumLength = 3,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "posinseqslash",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "8018", "7259" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "BIRTH SEQUENCE",
                SourceLine = 313,
            },
            new Gs1AiDefinition
            {
                AiStart = "7259",
                AiEnd = "7259",
                WildcardPattern = "7259",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 40,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "pcenc",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "8018" },
                    },
                Exclusions =
                    new string[]
                    {
                        "7256",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "BABY",
                SourceLine = 314,
            },
            new Gs1AiDefinition
            {
                AiStart = "8001",
                AiEnd = "8001",
                WildcardPattern = "8001",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 4,
                            MaximumLength = 4,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "nonzero",
                                },
                        },
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 5,
                            MaximumLength = 5,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "nonzero",
                                },
                        },
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 3,
                            MaximumLength = 3,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "nonzero",
                                },
                        },
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 1,
                            MaximumLength = 1,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "winding",
                                },
                        },
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 1,
                            MaximumLength = 1,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "DIMENSIONS",
                SourceLine = 315,
            },
            new Gs1AiDefinition
            {
                AiStart = "8002",
                AiEnd = "8002",
                WildcardPattern = "8002",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 20,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[0][],
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "CMT No.",
                SourceLine = 316,
            },
            new Gs1AiDefinition
            {
                AiStart = "8003",
                AiEnd = "8003",
                WildcardPattern = "8003",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 1,
                            MaximumLength = 1,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "zero",
                                },
                        },
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 13,
                            MaximumLength = 13,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "csum",
                                    "gcppos1",
                                },
                        },
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 16,
                            Optional = true,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[0][],
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = true,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "GRAI",
                SourceLine = 317,
            },
            new Gs1AiDefinition
            {
                AiStart = "8004",
                AiEnd = "8004",
                WildcardPattern = "8004",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 30,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "gcppos1",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[0][],
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = true,
                DigitalLinkQualifierSequences =
                    new string[][]
                    {
                        new string[] { "7040" },
                    },
                Title = "GIAI",
                SourceLine = 318,
            },
            new Gs1AiDefinition
            {
                AiStart = "8005",
                AiEnd = "8005",
                WildcardPattern = "8005",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "02" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "PRICE PER UNIT",
                SourceLine = 319,
            },
            new Gs1AiDefinition
            {
                AiStart = "8006",
                AiEnd = "8006",
                WildcardPattern = "8006",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 14,
                            MaximumLength = 14,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "csum",
                                    "gcppos2",
                                },
                        },
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 4,
                            MaximumLength = 4,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "pieceoftotal",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[0][],
                Exclusions =
                    new string[]
                    {
                        "01",
                        "03",
                        "37",
                    },
                DigitalLinkPrimaryKey = true,
                DigitalLinkQualifierSequences =
                    new string[][]
                    {
                        new string[] { "22", "10", "21" },
                    },
                Title = "ITIP",
                SourceLine = 320,
            },
            new Gs1AiDefinition
            {
                AiStart = "8007",
                AiEnd = "8007",
                WildcardPattern = "8007",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 34,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "iban",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "415" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "IBAN",
                SourceLine = 321,
            },
            new Gs1AiDefinition
            {
                AiStart = "8008",
                AiEnd = "8008",
                WildcardPattern = "8008",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 6,
                            MaximumLength = 6,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "yymmdd",
                                },
                        },
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 2,
                            MaximumLength = 2,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "hh",
                                },
                        },
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 2,
                            MaximumLength = 2,
                            Optional = true,
                            Linters =
                                new string[]
                                {
                                    "mi",
                                },
                        },
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 2,
                            MaximumLength = 2,
                            Optional = true,
                            Linters =
                                new string[]
                                {
                                    "ss",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "02" },
                        new string[] { "03" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "PROD TIME",
                SourceLine = 322,
            },
            new Gs1AiDefinition
            {
                AiStart = "8009",
                AiEnd = "8009",
                WildcardPattern = "8009",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 50,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "00" },
                        new string[] { "01" },
                        new string[] { "03" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "OPTSEN",
                SourceLine = 323,
            },
            new Gs1AiDefinition
            {
                AiStart = "8010",
                AiEnd = "8010",
                WildcardPattern = "8010",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "Y",
                            MinimumLength = 1,
                            MaximumLength = 30,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "gcppos1",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[0][],
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = true,
                DigitalLinkQualifierSequences =
                    new string[][]
                    {
                        new string[] { "8011" },
                    },
                Title = "CPID",
                SourceLine = 324,
            },
            new Gs1AiDefinition
            {
                AiStart = "8011",
                AiEnd = "8011",
                WildcardPattern = "8011",
                PredefinedLength = false,
                DigitalLinkDataAttribute = false,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 1,
                            MaximumLength = 12,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "nozeroprefix",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "8010" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "CPID SERIAL",
                SourceLine = 325,
            },
            new Gs1AiDefinition
            {
                AiStart = "8012",
                AiEnd = "8012",
                WildcardPattern = "8012",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 20,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                        new string[] { "03" },
                        new string[] { "8006" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "VERSION",
                SourceLine = 326,
            },
            new Gs1AiDefinition
            {
                AiStart = "8013",
                AiEnd = "8013",
                WildcardPattern = "8013",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 25,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "csumalpha",
                                    "gcppos1",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[0][],
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = true,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "GMN",
                SourceLine = 327,
            },
            new Gs1AiDefinition
            {
                AiStart = "8014",
                AiEnd = "8014",
                WildcardPattern = "8014",
                PredefinedLength = false,
                DigitalLinkDataAttribute = false,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 25,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "csumalpha",
                                    "gcppos1",
                                    "hasnondigit",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "MUDI",
                SourceLine = 328,
            },
            new Gs1AiDefinition
            {
                AiStart = "8017",
                AiEnd = "8017",
                WildcardPattern = "8017",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 18,
                            MaximumLength = 18,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "csum",
                                    "gcppos1",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[0][],
                Exclusions =
                    new string[]
                    {
                        "8018",
                    },
                DigitalLinkPrimaryKey = true,
                DigitalLinkQualifierSequences =
                    new string[][]
                    {
                        new string[] { "8019" },
                    },
                Title = "GSRN - PROVIDER",
                SourceLine = 329,
            },
            new Gs1AiDefinition
            {
                AiStart = "8018",
                AiEnd = "8018",
                WildcardPattern = "8018",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 18,
                            MaximumLength = 18,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "csum",
                                    "gcppos1",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[0][],
                Exclusions =
                    new string[]
                    {
                        "8017",
                    },
                DigitalLinkPrimaryKey = true,
                DigitalLinkQualifierSequences =
                    new string[][]
                    {
                        new string[] { "8019" },
                    },
                Title = "GSRN - RECIPIENT",
                SourceLine = 330,
            },
            new Gs1AiDefinition
            {
                AiStart = "8019",
                AiEnd = "8019",
                WildcardPattern = "8019",
                PredefinedLength = false,
                DigitalLinkDataAttribute = false,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 1,
                            MaximumLength = 10,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "8017" },
                        new string[] { "8018" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "SRIN",
                SourceLine = 331,
            },
            new Gs1AiDefinition
            {
                AiStart = "8020",
                AiEnd = "8020",
                WildcardPattern = "8020",
                PredefinedLength = false,
                DigitalLinkDataAttribute = false,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 25,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "415" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "REF No.",
                SourceLine = 332,
            },
            new Gs1AiDefinition
            {
                AiStart = "8026",
                AiEnd = "8026",
                WildcardPattern = "8026",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 14,
                            MaximumLength = 14,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "csum",
                                    "gcppos2",
                                },
                        },
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 4,
                            MaximumLength = 4,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "pieceoftotal",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "37" },
                    },
                Exclusions =
                    new string[]
                    {
                        "02",
                        "03",
                        "8006",
                    },
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "ITIP CONTENT",
                SourceLine = 333,
            },
            new Gs1AiDefinition
            {
                AiStart = "8030",
                AiEnd = "8030",
                WildcardPattern = "8030",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "Z",
                            MinimumLength = 1,
                            MaximumLength = 90,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "00" },
                        new string[] { "01", "21" },
                        new string[] { "03", "21" },
                        new string[] { "253" },
                        new string[] { "255" },
                        new string[] { "8003" },
                        new string[] { "8004" },
                        new string[] { "8006", "21" },
                        new string[] { "8010", "8011" },
                        new string[] { "8017" },
                        new string[] { "8018" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "DIGSIG",
                SourceLine = 334,
            },
            new Gs1AiDefinition
            {
                AiStart = "8040",
                AiEnd = "8040",
                WildcardPattern = "8040",
                PredefinedLength = false,
                DigitalLinkDataAttribute = false,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 15,
                            MaximumLength = 15,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01", "21" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "IMEI",
                SourceLine = 335,
            },
            new Gs1AiDefinition
            {
                AiStart = "8041",
                AiEnd = "8041",
                WildcardPattern = "8041",
                PredefinedLength = false,
                DigitalLinkDataAttribute = false,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 15,
                            MaximumLength = 15,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01", "21", "8040" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "IMEI2",
                SourceLine = 336,
            },
            new Gs1AiDefinition
            {
                AiStart = "8042",
                AiEnd = "8042",
                WildcardPattern = "8042",
                PredefinedLength = false,
                DigitalLinkDataAttribute = false,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 32,
                            MaximumLength = 32,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01", "21", "8040" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "ESIM",
                SourceLine = 337,
            },
            new Gs1AiDefinition
            {
                AiStart = "8043",
                AiEnd = "8043",
                WildcardPattern = "8043",
                PredefinedLength = false,
                DigitalLinkDataAttribute = false,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 18,
                            MaximumLength = 18,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 1,
                            MaximumLength = 2,
                            Optional = true,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01", "21", "8040" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "PSIM",
                SourceLine = 338,
            },
            new Gs1AiDefinition
            {
                AiStart = "8110",
                AiEnd = "8110",
                WildcardPattern = "8110",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 70,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "couponcode",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[0][],
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "",
                SourceLine = 339,
            },
            new Gs1AiDefinition
            {
                AiStart = "8111",
                AiEnd = "8111",
                WildcardPattern = "8111",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "N",
                            MinimumLength = 4,
                            MaximumLength = 4,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "255" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "POINTS",
                SourceLine = 340,
            },
            new Gs1AiDefinition
            {
                AiStart = "8112",
                AiEnd = "8112",
                WildcardPattern = "8112",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 70,
                            Optional = false,
                            Linters =
                                new string[]
                                {
                                    "couponposoffer",
                                },
                        },
                    },
                RequiredAssociations =
                    new string[0][],
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "",
                SourceLine = 341,
            },
            new Gs1AiDefinition
            {
                AiStart = "8200",
                AiEnd = "8200",
                WildcardPattern = "8200",
                PredefinedLength = false,
                DigitalLinkDataAttribute = false,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 70,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[][]
                    {
                        new string[] { "01" },
                    },
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "PRODUCT URL",
                SourceLine = 342,
            },
            new Gs1AiDefinition
            {
                AiStart = "90",
                AiEnd = "90",
                WildcardPattern = "90",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 30,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[0][],
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "INTERNAL",
                SourceLine = 343,
            },
            new Gs1AiDefinition
            {
                AiStart = "91",
                AiEnd = "99",
                WildcardPattern = "9n",
                PredefinedLength = false,
                DigitalLinkDataAttribute = true,
                Components =
                    new Gs1AiComponentDefinition[]
                    {
                        new Gs1AiComponentDefinition
                        {
                            CharacterSet = "X",
                            MinimumLength = 1,
                            MaximumLength = 90,
                            Optional = false,
                            Linters =
                                new string[0],
                        },
                    },
                RequiredAssociations =
                    new string[0][],
                Exclusions =
                    new string[0],
                DigitalLinkPrimaryKey = false,
                DigitalLinkQualifierSequences =
                    new string[0][],
                Title = "INTERNAL",
                SourceLine = 344,
            },
        };
    internal static readonly int[] TrieDefinitionIndices =
        new int[]
        {
            -1, -1, 0, 1, 2, 3, -1, 4, 5, 6,
            7, 8, 9, 10, -1, 11, 12, 13, -1, 14,
            -1, 15, 16, 17, 18, -1, 19, 20, 21, 22,
            23, -1, 24, -1, -1, 25, 25, 25, 25, 25,
            25, -1, 26, 26, 26, 26, 26, 26, -1, 27,
            27, 27, 27, 27, 27, -1, 28, 28, 28, 28,
            28, 28, -1, 29, 29, 29, 29, 29, 29, -1,
            30, 30, 30, 30, 30, 30, -1, 31, 31, 31,
            31, 31, 31, -1, -1, 32, 32, 32, 32, 32,
            32, -1, 33, 33, 33, 33, 33, 33, -1, 34,
            34, 34, 34, 34, 34, -1, 35, 35, 35, 35,
            35, 35, -1, 36, 36, 36, 36, 36, 36, -1,
            37, 37, 37, 37, 37, 37, -1, 38, 38, 38,
            38, 38, 38, -1, 39, 39, 39, 39, 39, 39,
            -1, 40, 40, 40, 40, 40, 40, -1, 41, 41,
            41, 41, 41, 41, -1, -1, 42, 42, 42, 42,
            42, 42, -1, 43, 43, 43, 43, 43, 43, -1,
            44, 44, 44, 44, 44, 44, -1, 45, 45, 45,
            45, 45, 45, -1, 46, 46, 46, 46, 46, 46,
            -1, 47, 47, 47, 47, 47, 47, -1, 48, 48,
            48, 48, 48, 48, -1, 49, 49, 49, 49, 49,
            49, -1, -1, 50, 50, 50, 50, 50, 50, -1,
            51, 51, 51, 51, 51, 51, -1, 52, 52, 52,
            52, 52, 52, -1, 53, 53, 53, 53, 53, 53,
            -1, 54, 54, 54, 54, 54, 54, -1, 55, 55,
            55, 55, 55, 55, -1, 56, 56, 56, 56, 56,
            56, -1, 57, 57, 57, 57, 57, 57, -1, 58,
            58, 58, 58, 58, 58, -1, 59, 59, 59, 59,
            59, 59, -1, -1, 60, 60, 60, 60, 60, 60,
            -1, 61, 61, 61, 61, 61, 61, -1, 62, 62,
            62, 62, 62, 62, -1, 63, 63, 63, 63, 63,
            63, -1, 64, 64, 64, 64, 64, 64, -1, 65,
            65, 65, 65, 65, 65, -1, 66, 66, 66, 66,
            66, 66, -1, 67, 67, 67, 67, 67, 67, -1,
            -1, 68, 68, 68, 68, 68, 68, -1, 69, 69,
            69, 69, 69, 69, -1, 70, 70, 70, 70, 70,
            70, -1, 71, 71, 71, 71, 71, 71, -1, 72,
            72, 72, 72, 72, 72, -1, 73, 73, 73, 73,
            73, 73, -1, 74, 74, 74, 74, 74, 74, -1,
            75, 75, 75, 75, 75, 75, -1, 76, 76, 76,
            76, 76, 76, -1, 77, 77, 77, 77, 77, 77,
            78, -1, -1, 79, 79, 79, 79, 79, 79, 79,
            79, 79, 79, -1, 80, 80, 80, 80, 80, 80,
            80, 80, 80, 80, -1, 81, 81, 81, 81, 81,
            81, 81, 81, 81, 81, -1, 82, 82, 82, 82,
            82, 82, 82, 82, 82, 82, -1, 83, 83, 83,
            83, -1, 84, 84, 84, 84, 84, 84, -1, -1,
            85, 86, 87, 88, -1, 89, 90, 91, 92, 93,
            94, 95, 96, -1, 97, 98, 99, 100, 101, 102,
            103, 104, -1, -1, 105, 106, 107, 108, 109, 110,
            111, 112, 113, 114, -1, 115, 116, 117, 118, 119,
            120, 121, 122, 123, 124, -1, 125, 126, 127, 128,
            129, 130, 131, -1, 132, 133, 134, 135, -1, -1,
            -1, 136, 137, 138, 139, 140, 141, 142, 143, 144,
            -1, 145, 146, -1, 147, 148, 149, 150, -1, 151,
            152, 153, 154, 155, 156, 157, 158, 159, 160, -1,
            161, 162, -1, 163, 164, 165, 166, 167, 168, 169,
            170, -1, -1, 171, 172, 173, 174, 175, 176, 177,
            178, 179, 180, -1, 181, 182, 183, -1, 184, 185,
            186, 187, 188, 189, 190, 191, 192, 193, -1, -1,
            -1, 194, 195, 196, 197, 198, 199, 200, 201, 202,
            -1, 203, 204, 205, 206, 207, 208, 209, 210, -1,
            211, 212, -1, 213, -1, 214, 215, 216, 217, -1,
            -1, 218, 219, 220, -1, -1, 221, -1, 222, 223,
            223, 223, 223, 223, 223, 223, 223, 223,
        };
    internal static readonly int[] TrieChildIndices =
        new int[]
        {
            1, 6, 14, 31, 468, -1, -1, 528, 598, 637,
            2, 3, 4, 5, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            7, 8, 9, 10, -1, 11, 12, 13, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            15, 16, 17, 18, 20, 25, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, 19, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            21, 22, 23, 24, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            26, 27, -1, 28, 29, 30, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            32, 33, 83, 154, 211, 282, 339, 410, -1, 411,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            34, 41, 48, 55, 62, 69, 76, -1, -1, -1,
            35, 36, 37, 38, 39, 40, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            42, 43, 44, 45, 46, 47, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            49, 50, 51, 52, 53, 54, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            56, 57, 58, 59, 60, 61, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            63, 64, 65, 66, 67, 68, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            70, 71, 72, 73, 74, 75, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            77, 78, 79, 80, 81, 82, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            84, 91, 98, 105, 112, 119, 126, 133, 140, 147,
            85, 86, 87, 88, 89, 90, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            92, 93, 94, 95, 96, 97, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            99, 100, 101, 102, 103, 104, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            106, 107, 108, 109, 110, 111, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            113, 114, 115, 116, 117, 118, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            120, 121, 122, 123, 124, 125, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            127, 128, 129, 130, 131, 132, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            134, 135, 136, 137, 138, 139, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            141, 142, 143, 144, 145, 146, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            148, 149, 150, 151, 152, 153, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            155, 162, 169, 176, 183, 190, 197, 204, -1, -1,
            156, 157, 158, 159, 160, 161, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            163, 164, 165, 166, 167, 168, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            170, 171, 172, 173, 174, 175, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            177, 178, 179, 180, 181, 182, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            184, 185, 186, 187, 188, 189, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            191, 192, 193, 194, 195, 196, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            198, 199, 200, 201, 202, 203, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            205, 206, 207, 208, 209, 210, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            212, 219, 226, 233, 240, 247, 254, 261, 268, 275,
            213, 214, 215, 216, 217, 218, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            220, 221, 222, 223, 224, 225, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            227, 228, 229, 230, 231, 232, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            234, 235, 236, 237, 238, 239, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            241, 242, 243, 244, 245, 246, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            248, 249, 250, 251, 252, 253, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            255, 256, 257, 258, 259, 260, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            262, 263, 264, 265, 266, 267, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            269, 270, 271, 272, 273, 274, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            276, 277, 278, 279, 280, 281, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            283, 290, 297, 304, 311, 318, 325, 332, -1, -1,
            284, 285, 286, 287, 288, 289, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            291, 292, 293, 294, 295, 296, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            298, 299, 300, 301, 302, 303, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            305, 306, 307, 308, 309, 310, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            312, 313, 314, 315, 316, 317, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            319, 320, 321, 322, 323, 324, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            326, 327, 328, 329, 330, 331, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            333, 334, 335, 336, 337, 338, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            340, 347, 354, 361, 368, 375, 382, 389, 396, 403,
            341, 342, 343, 344, 345, 346, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            348, 349, 350, 351, 352, 353, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            355, 356, 357, 358, 359, 360, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            362, 363, 364, 365, 366, 367, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            369, 370, 371, 372, 373, 374, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            376, 377, 378, 379, 380, 381, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            383, 384, 385, 386, 387, 388, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            390, 391, 392, 393, 394, 395, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            397, 398, 399, 400, 401, 402, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            404, 405, 406, 407, 408, 409, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            412, 423, 434, 445, 456, 461, -1, -1, -1, -1,
            413, 414, 415, 416, 417, 418, 419, 420, 421, 422,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            424, 425, 426, 427, 428, 429, 430, 431, 432, 433,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            435, 436, 437, 438, 439, 440, 441, 442, 443, 444,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            446, 447, 448, 449, 450, 451, 452, 453, 454, 455,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            457, 458, 459, 460, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            462, 463, 464, 465, 466, 467, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            469, 474, 483, 492, -1, -1, -1, -1, -1, -1,
            470, 471, 472, 473, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            475, 476, 477, 478, 479, 480, 481, 482, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            484, 485, 486, 487, 488, 489, 490, 491, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            493, 504, 515, 523, -1, -1, -1, -1, -1, -1,
            494, 495, 496, 497, 498, 499, 500, 501, 502, 503,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            505, 506, 507, 508, 509, 510, 511, 512, 513, 514,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            516, 517, 518, 519, 520, 521, 522, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            524, 525, 526, 527, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            529, 562, 571, -1, -1, -1, -1, -1, -1, -1,
            530, 540, 543, 548, 559, -1, -1, -1, -1, -1,
            -1, 531, 532, 533, 534, 535, 536, 537, 538, 539,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            541, 542, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            544, 545, 546, 547, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            549, 550, 551, 552, 553, 554, 555, 556, 557, 558,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            560, 561, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            563, 564, 565, 566, 567, 568, 569, 570, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, 572, 583, 587, -1, -1, -1, -1,
            573, 574, 575, 576, 577, 578, 579, 580, 581, 582,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            584, 585, 586, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            588, 589, 590, 591, 592, 593, 594, 595, 596, 597,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            599, 629, 634, -1, -1, -1, -1, -1, -1, -1,
            600, 610, 619, 622, 624, -1, -1, -1, -1, -1,
            -1, 601, 602, 603, 604, 605, 606, 607, 608, 609,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            611, 612, 613, 614, 615, -1, -1, 616, 617, 618,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            620, -1, -1, -1, -1, -1, 621, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            623, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            625, 626, 627, 628, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, 630, -1, -1, -1, -1, -1, -1, -1, -1,
            631, 632, 633, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            635, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            636, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            638, 639, 640, 641, 642, 643, 644, 645, 646, 647,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        };
}
