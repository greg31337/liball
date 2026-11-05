namespace ftp;

internal static class FtpTransferParameterCodec
{
    internal static string FormatRepresentation(FtpTransferOptions? options)
    {
        if (options == null)
        {
            throw new FtpConfigurationException(
                "Transfer representation options are required.", null);
        }

        ValidateRepresentationFields(options);
        if (options.Type == FtpTransferType.Ascii)
        {
            return "A " + FormatTextFormat(options.TextFormat);
        }
        if (options.Type == FtpTransferType.Ebcdic)
        {
            return "E " + FormatTextFormat(options.TextFormat);
        }
        if (options.Type == FtpTransferType.Image)
        {
            return "I";
        }
        return FtpArgumentCodec.FormatTypeLocalArgument(options.LocalByteSize);
    }

    internal static FtpTransferOptions ParseRepresentation(string? argument)
    {
        if (string.IsNullOrEmpty(argument))
        {
            throw new FtpProtocolException(
                "An FTP TYPE representation argument is required.", null);
        }

        FtpTransferOptions options = new FtpTransferOptions();
        char typeCode = char.ToUpperInvariant(argument[0]);
        if (typeCode == 'I' && argument.Length == 1)
        {
            options.Type = FtpTransferType.Image;
            return options;
        }
        if (typeCode == 'L')
        {
            options.Type = FtpTransferType.LocalByte;
            options.HasLocalByteSize = true;
            options.LocalByteSize = FtpArgumentCodec.ParseTypeLocalArgument(argument);
            return options;
        }
        if (typeCode != 'A' && typeCode != 'E')
        {
            throw new FtpProtocolException(
                "FTP TYPE supports only A, E, I, or L representations.", null);
        }

        options.Type = typeCode == 'A' ?
            FtpTransferType.Ascii : FtpTransferType.Ebcdic;
        if (argument.Length == 1)
        {
            options.TextFormat = FtpTextFormat.NonPrint;
            return options;
        }
        if (argument.Length != 3 || argument[1] != ' ')
        {
            throw new FtpProtocolException(
                "FTP TYPE A or E accepts one optional format code.", null);
        }
        options.TextFormat = ParseTextFormat(argument[2]);
        return options;
    }

    internal static string FormatStructure(FtpFileStructure structure)
    {
        if (structure == FtpFileStructure.File)
        {
            return "F";
        }
        if (structure == FtpFileStructure.Record)
        {
            return "R";
        }
        if (structure == FtpFileStructure.Page)
        {
            return "P";
        }
        throw new FtpConfigurationException(
            "The FTP file structure is invalid.", null);
    }

    internal static FtpFileStructure ParseStructure(string? argument)
    {
        char code = RequireSingleCode(argument, "STRU");
        if (code == 'F')
        {
            return FtpFileStructure.File;
        }
        if (code == 'R')
        {
            return FtpFileStructure.Record;
        }
        if (code == 'P')
        {
            return FtpFileStructure.Page;
        }
        throw new FtpProtocolException(
            "FTP STRU supports only F, R, or P.", null);
    }

    internal static string FormatMode(FtpTransferMode mode)
    {
        if (mode == FtpTransferMode.Stream)
        {
            return "S";
        }
        if (mode == FtpTransferMode.Block)
        {
            return "B";
        }
        if (mode == FtpTransferMode.Compressed)
        {
            return "C";
        }
        if (mode == FtpTransferMode.Deflate)
        {
            return "Z";
        }
        throw new FtpConfigurationException(
            "The FTP transfer mode is invalid.", null);
    }

    internal static FtpTransferMode ParseMode(string? argument)
    {
        char code = RequireSingleCode(argument, "MODE");
        if (code == 'S')
        {
            return FtpTransferMode.Stream;
        }
        if (code == 'B')
        {
            return FtpTransferMode.Block;
        }
        if (code == 'C')
        {
            return FtpTransferMode.Compressed;
        }
        if (code == 'Z')
        {
            return FtpTransferMode.Deflate;
        }
        throw new FtpProtocolException(
            "FTP MODE supports only S, B, C, or Z.", null);
    }

    internal static void ValidateRepresentationForState(
        FtpTransferOptions? representation,
        FtpSequenceState state)
    {
        if (representation == null)
        {
            throw new FtpConfigurationException(
                "Transfer representation options are required.", null);
        }
        ValidateRepresentationFields(representation);
        FtpTransferOptions combined = CreateStateOptions(state);
        combined.Type = representation.Type;
        combined.TextFormat = representation.TextFormat;
        combined.HasLocalByteSize = representation.HasLocalByteSize;
        combined.LocalByteSize = representation.LocalByteSize;
        FtpTransferValidator.ValidateTransferCombination(combined);
    }

    internal static void ValidateStructureForState(
        FtpFileStructure structure,
        FtpSequenceState state)
    {
        FormatStructure(structure);
        FtpTransferOptions combined = CreateStateOptions(state);
        combined.Structure = structure;
        FtpTransferValidator.ValidateTransferCombination(combined);
    }

    internal static void ValidateModeForState(
        FtpTransferMode mode,
        FtpSequenceState state)
    {
        FormatMode(mode);
        FtpTransferOptions combined = CreateStateOptions(state);
        combined.Mode = mode;
        FtpTransferValidator.ValidateTransferCombination(combined);
    }

    internal static bool RepresentationMustPrecedeStructure(
        FtpTransferOptions options,
        FtpSequenceState state)
    {
        if (options.Structure == state.FileStructure)
        {
            return false;
        }
        if (options.Structure == FtpFileStructure.Record &&
            state.TransferType != FtpTransferType.Ascii &&
            state.TransferType != FtpTransferType.Ebcdic)
        {
            return true;
        }
        return options.Structure == FtpFileStructure.Page &&
            state.TransferType == FtpTransferType.LocalByte &&
            state.LocalByteSize < 3;
    }

    internal static void ValidateRepresentationFields(FtpTransferOptions options)
    {
        if (options.Type != FtpTransferType.Ascii &&
            options.Type != FtpTransferType.Ebcdic &&
            options.Type != FtpTransferType.Image &&
            options.Type != FtpTransferType.LocalByte)
        {
            throw new FtpConfigurationException(
                "The transfer representation type is invalid.", null);
        }
        if (options.TextFormat != FtpTextFormat.NonPrint &&
            options.TextFormat != FtpTextFormat.Telnet &&
            options.TextFormat != FtpTextFormat.CarriageControl)
        {
            throw new FtpConfigurationException(
                "The transfer text format is invalid.", null);
        }
        if (options.Type == FtpTransferType.LocalByte)
        {
            if (!options.HasLocalByteSize || options.LocalByteSize < 1 ||
                options.LocalByteSize > 255 ||
                options.TextFormat != FtpTextFormat.NonPrint)
            {
                throw new FtpConfigurationException(
                    "TYPE L requires a byte size from 1 through 255 and non-print format.",
                    null);
            }
            return;
        }
        if (options.HasLocalByteSize || options.LocalByteSize != 0)
        {
            throw new FtpConfigurationException(
                "A local byte size is valid only for TYPE L.", null);
        }
        if (options.Type == FtpTransferType.Image &&
            options.TextFormat != FtpTextFormat.NonPrint)
        {
            throw new FtpConfigurationException(
                "TYPE I cannot use a text format control.", null);
        }
    }

    internal static string FormatTextFormat(FtpTextFormat format)
    {
        if (format == FtpTextFormat.NonPrint)
        {
            return "N";
        }
        if (format == FtpTextFormat.Telnet)
        {
            return "T";
        }
        if (format == FtpTextFormat.CarriageControl)
        {
            return "C";
        }
        throw new FtpConfigurationException(
            "The FTP text format is invalid.", null);
    }

    internal static FtpTextFormat ParseTextFormat(char code)
    {
        char normalized = char.ToUpperInvariant(code);
        if (normalized == 'N')
        {
            return FtpTextFormat.NonPrint;
        }
        if (normalized == 'T')
        {
            return FtpTextFormat.Telnet;
        }
        if (normalized == 'C')
        {
            return FtpTextFormat.CarriageControl;
        }
        throw new FtpProtocolException(
            "FTP text format supports only N, T, or C.", null);
    }

    internal static char RequireSingleCode(string? argument, string commandName)
    {
        if (argument == null || argument.Length != 1)
        {
            throw new FtpProtocolException(
                $"FTP {commandName} requires one code character.", null);
        }
        return char.ToUpperInvariant(argument[0]);
    }

    internal static FtpTransferOptions CreateStateOptions(FtpSequenceState state)
    {
        FtpTransferOptions options = new FtpTransferOptions();
        options.Type = state.TransferType;
        options.TextFormat = state.TextFormat;
        options.Structure = state.FileStructure;
        options.Mode = state.TransferMode;
        options.HasLocalByteSize = state.HasLocalByteSize;
        options.LocalByteSize = state.LocalByteSize;
        return options;
    }
}
