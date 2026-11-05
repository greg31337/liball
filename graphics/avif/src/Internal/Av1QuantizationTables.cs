namespace avif;

// AV1 specification section 9.3.7 quantizer lookup tables.
internal static class Av1QuantizationTables
{
    static readonly string[] chunks = new string[]
    {
        "BAAIAAgACQAKAAsADAAMAA0ADgAPABAAEQASABMAEwAUABUAFgAXABgAGQAaABoAGwAcAB0AHgAfACAAIAAhACIAIwAkACUAJgAm",
        "ACcAKAApACoAKwArACwALQAuAC8AMAAwADEAMgAzADQANQA1ADYANwA4ADkAOQA6ADsAPAA9AD4APgA/AEAAQQBCAEIAQwBEAEUA",
        "RgBGAEcASABJAEoASgBLAEwATQBOAE4ATwBQAFEAUQBSAFMAVABVAFUAVwBYAFoAXABdAF8AYABiAGMAZQBmAGgAaQBrAGwAbgBv",
        "AHEAcgB0AHUAdgB4AHkAewB9AH8AgQCDAIYAiACKAIwAjgCQAJIAlACWAJgAmgCcAJ4AoQCkAKYAqQCsAK4AsQC0ALYAuQC7AL4A",
        "wADDAMcAygDNANAA0wDWANkA3ADfAOIA5gDpAO0A8ADzAPcA+gD9AAEBBQEJAQ0BEAEUARgBHAEgASQBKAEsATABNQE5AT0BQgFG",
        "AUoBTwFUAVgBXQFiAWcBbAFxAXYBewGAAYUBiwGQAZYBmwGhAacBrQGzAbkBvwHGAc0B0wHbAeIB6QHxAfkBAQIKAhICGwIlAi8C",
        "OQJDAk4CWgJmAnICgAKOApwCrAK8As0C4ALzAgcDHAMzA0sDZQOAA50DuwPcA/4DIgRKBHMEoATQBAIFOAUEAAkACgANAA8AEQAU",
        "ABYAGQAcAB8AIgAlACgAKwAvADIANQA5ADwAQABEAEcASwBOAFIAVgBaAF0AYQBlAGkAbQBxAHQAeAB8AIAAhACIAIwAjwCTAJcA",
        "mwCfAKMApgCqAK4AsgC2ALkAvQDBAMUAyADMANAA1ADXANsA3wDiAOYA6QDtAPEA9AD4APsA/wADAQYBCgENAREBFAEYARsBHwEi",
        "ASUBKQEsATABMwE2AToBPQFBAUQBRwFLAU4BUQFXAV4BZAFqAXEBdwF9AYMBigGQAZYBnAGiAagBrgG0AboBwAHGAcwB0gHYAd4B",
        "5AHqAfMB+wEEAg0CFQIeAiYCLwI3AkACSAJQAlkCYQJpAnECegKEAo8CmgKkAq8CugLEAs4C2QLjAu0C9wICAw4DGwMnAzMDPwNM",
        "A1gDZANwA3sDigOYA6UDswPBA88D3APpA/cDBgQVBCUENARCBFEEYARxBIEEkgSiBLIEwgTUBOUE9wQIBRoFKwU+BVEFYwV2BYgF",
        "nAWwBcQF2AXsBQEGFwYsBkEGWAZvBoYGnAa1Bs0G5gb/BhkHNAdPB2wHiQemB8YH5QcGCCgISwhvCJUIvAjkCA8JOwlqCZoJzAkB",
        "CjgKcwqxCvIKNwuAC8wLHgx0DNAMLw2WDQIOdg7vDnEP+Q+MECoRzxGBEkETChTjFAQADAASABkAIQApADIAPABGAFAAWwBnAHMA",
        "fwCMAJkApgC0AMIA0ADeAO0A+wAKARkBKAE4AUcBVwFmAXYBhgGVAaUBtQHFAdUB5AH0AQQCFAIkAjQCRAJUAmMCcwKDApMCogKy",
        "AsIC0QLhAvACAAMPAx4DLgM9A0wDWwNqA3kDiAOXA6YDtQPEA9ID4QPwA/4DDQQbBCkEOARGBFQEYgRwBH8EjQSbBKgEtgTEBNIE",
        "4ATtBPsECAUWBSMFMQU+BVgFcQWLBaQFvQXWBe8FCAYhBjoGUgZrBoQGnAa1Bs0G5Qb9BhYHLgdGB10HdQeNB6UHyAfrBw0IMAhS",
        "CHUIlwi5CNsI/AgeCT8JYAmCCaMJwwnkCQ8KOgplCpAKugrkCg4LOAthC4oLswvcCwQMNwxpDJoMywz8DC0NXQ2NDb0N7Q0lDl0O",
        "lQ7MDgMPOQ9vD6UP2g8XEFUQkRDNEAkRRBF/EcIRBBJFEoYSxxIHE04TlRPbEyEUZhSrFPcUQhWNFdcVIRZxFsEWERdgF68XBRha",
        "GK8YBBlfGbsZFhpxGtMaNhuYG/sbZRzQHDsdrx0jHpgeFh+VHxYgoCAsIbshVCLxIpAjOyTqJKclaCYvJwUo4SjOKcIqyivaLAAu",
        "MS95MM4xPjO9NFk2BzjXObo7xD3kPy9Cp0Q9RwVKBk0pUItTBAAIAAkACgALAAwADQAOAA8AEAARABIAEwAUABUAFgAXABgAGQAa",
        "ABsAHAAdAB4AHwAgACEAIgAjACQAJQAmACcAKAApACoAKwAsAC0ALgAvADAAMQAyADMANAA1ADYANwA4ADkAOgA7ADwAPQA+AD8A",
        "QABBAEIAQwBEAEUARgBHAEgASQBKAEsATABNAE4ATwBQAFEAUgBTAFQAVQBWAFcAWABZAFoAWwBcAF0AXgBfAGAAYQBiAGMAZABl",
        "AGYAaABqAGwAbgBwAHIAdAB2AHgAegB8AH4AgACCAIQAhgCIAIoAjACOAJAAkgCUAJYAmACbAJ4AoQCkAKcAqgCtALAAswC2ALkA",
        "vAC/AMIAxQDIAMsAzwDTANcA2wDfAOMA5wDrAO8A8wD3APsA/wAEAQkBDgETARgBHQEiAScBLAExATcBPQFDAUkBTwFVAVsBYQFn",
        "AW4BdQF8AYMBigGRAZgBoAGoAbABuAHAAcgB0QHaAeMB7AH1Af4BCAISAhwCJgIwAjsCRgJRAlwCZwJzAn8CiwKXAqQCsQK+AssC",
        "2QLnAvUCAwMSAyEDMANAA1ADYANxA4IDkwOlA7cDyQPcA+8DAgQWBCoEPwRUBGkEfwSVBKwEwwTbBPMEDAUlBT8FWQV0BY8FqwXH",
        "BeQFAQYfBj0GXAZ8BpwGvQbfBgEHJAcEAAkACwANABAAEgAVABgAGwAeACEAJQAoACwAMAAzADcAOwA/AEMARwBLAE8AUwBYAFwA",
        "YABkAGkAbQByAHYAegB/AIMAiACMAJEAlQCaAJ4AowCoAKwAsQC1ALoAvgDDAMcAzADQANUA2QDeAOIA5wDrAPAA9AD5AP0AAgEG",
        "AQsBDwETARgBHAEhASUBKQEuATIBNwE7AT8BRAFIAUwBUQFVAVkBXQFiAWYBagFvAXMBdwF7AYABhAGIAYwBkQGZAaEBqQGxAbkB",
        "wQHKAdIB2gHiAeoB8gH6AQICCwITAhsCIwIrAjMCOwJDAkwCVAJcAmgCdAKAAowCmAKkArACvALJAtUC4QLtAvkCBQMRAx0DKQM5",
        "A0kDWQNpA3kDiQOaA6oDugPKA9oD6gP6Aw4EIgQ2BEoEXgRyBIYEmgSuBMIE2gTyBAoFIgU6BVIFagWDBZsFtwXTBe8FCwYnBkMG",
        "XwZ/Bp8GvwbfBv8GHwdDB2cHiwevB9MH9wcfCEcIbwiXCL8I6wgXCUMJbwmbCcsJ+wkrClsKjwrDCvcKKwtjC5sL0wsLDEcMgwy/",
        "DP8MPw1/DcMNBw5LDpMO2w4kD3APvA8IEFgQqBD8EFARpBH8EVQSsBIME2wTzBMwFJQU/BRkFdAVPBasFhwXkBcEGHwY9BhwGfAZ",
        "cBr0GnwbBByQHAQADQATABsAIwAsADYAQABLAFcAYwBwAH4AiwCaAKgAtwDHANYA5gD3AAcBGAEpAToBSwFdAW4BgAGSAaQBtgHI",
        "AdsB7QH/ARICJAI3AkoCXAJvAoIClAKnAroCzALfAvECBAMXAykDPANOA2EDdAOGA5gDqwO9A9AD4gP0AwYEGQQrBD0ETwRhBHME",
        "hQSXBKkEuwTNBN4E8AQCBRMFJQU3BUgFWgVrBX0FjgWgBbEFwgXTBeUF9gUHBhgGKQY7BlsGfAadBr0G3gb/BiAHQAdhB4IHogfD",
        "B+QHBAglCEYIZgiHCKgIyAjpCAkJKglKCWsJmwnMCfwJLQpdCo0KvgruCh8LTwt/C7AL4AsQDEEMcQyiDOIMIg1iDaMN4w0jDmQO",
        "pA7kDiQPZQ+lD+UPNRCGENYQJhF2EcYRFxJnErcSBxNnE8gTKBSIFOgUSBWoFQkWaRbZFkkXuRcpGJkYChl6GfoZehr6Gnob+ht6",
        "HAsdmx0rHrseSx/bH3sgGyG7IVsi/CKsI1wkDCW8JWwmLCfsJ6wobCk8Kgwr3CutLI0tbS5NLy0wHTENMv0y/TP9NP01DTcdOC05",
        "TTptO448vj3uPh5AXkGeQu5DPkWORu5HTkm+Si5Mrk0uT75QTlLuU45VPlfuWK5ablw+Xg5g7mHPY79lv2e/ac9r720PcD9y"
    };

    static readonly byte[] data = Load();

    internal static int GetDcQuantizer(int qIndex, int delta, int bitDepth)
    {
        return GetQuantizer(qIndex, delta, bitDepth, false);
    }

    internal static int GetAcQuantizer(int qIndex, int delta, int bitDepth)
    {
        return GetQuantizer(qIndex, delta, bitDepth, true);
    }

    static int GetQuantizer(int qIndex, int delta, int bitDepth, bool ac)
    {
        if (qIndex < 0 || qIndex > 255
            || delta < -255 || delta > 255
            || bitDepth != 8 && bitDepth != 10 && bitDepth != 12)
        {
            return 0;
        }

        int adjusted = qIndex + delta;
        if (adjusted < 0)
        {
            adjusted = 0;
        }
        else if (adjusted > 255)
        {
            adjusted = 255;
        }

        int depthIndex = bitDepth == 8 ? 0 : (bitDepth == 10 ? 1 : 2);
        int tableIndex = (ac ? 3 : 0) + depthIndex;
        int offset = (tableIndex * 256 + adjusted) * 2;
        return data[offset] | data[offset + 1] << 8;
    }

    static byte[] Load()
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < chunks.Length)
        {
            builder.Append(chunks[index]);
            index++;
        }
        return System.Convert.FromBase64String(builder.ToString());
    }
}
