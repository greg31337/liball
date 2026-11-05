namespace barcode;

internal static class Pdf417CodewordTable
{
    internal const int CodewordCount = 929;
    internal const int ClusterCount = 3;
    internal const int ModulesPerCodeword = 17;
    internal const int RunsPerCodeword = 8;
    internal const int StartPatternBits = 0x1fea8;
    internal const int StartPatternModuleCount = 17;
    internal const int StopPatternBits = 0x3fa29;
    internal const int StopPatternModuleCount = 18;

    const string EncodedPatterns =
        "6uD1ePq+6nD1PPqf1GDqONQwqCDUGKgQ1uDrePW+1nDrPPWfrGDWOKwwruDXeOu+rnDXPOufrjjXHq94176vPNefr776/elw9Lz6X9Jg6Tj0ntIw" +
        "6RykINIY6Q6kENIMpAjTcOm89N+mYNM46Z6mMNMc6Y+mGNMOp3DTvOnfpzjTnqcc04+nvNPfp56nj9Fg6Lj0XtEw6Jz0T6Ig0RjojqIQ0QyiCKIE" +
        "o2DRuOjeozDRnOjPoxjRjqMMowajuNHeo5zRz6OOo97QsOhc9C+hINCY6E6hENCM6EehCNCGoQTQg6Gw0Nzob6GY0M6hjNDHoYahg9DvocegoNBY" +
        "6C6gkNBM6CegiNBGoITQQ6CCoNigzKDGoFDoF9Am0COgQeVw8rz5X8pg5Tjynsow5Rzyj5QgyhiUEMtw5bzy35ZgyzjlnpYwyxyWGJYMl3DLvOXf" +
        "lzjLnpcclw6XvMvfl56X3+1g9rj7Xu0w9pz7T9og7Rj2jtoQ7Qz2h9oI7QbJYOS48l7bYMkw5JzyT9sw7Zz2z7YgkhDJDOSHthDbDLYIk2DJuOTe" +
        "t2CTMMmc5M+3MNuc7c+3GJMMtwyTuMnet7iTnMnPt5zbz7eOk9633pPPt8/ssPZc+y/ZIOyY9k7ZEOyM9kfZCOyG2QTZAsiw5FzyL9mwyJjkTrMg" +
        "kRDszuRHsxCRCMiGswjZhsiDkQKRsMjc5G+zsJGYyM6zmNnOyMezjJGGkYOR3Mjvs9yRzrPOkcezx7Pv2KDsWPYu2JDsTPYn2IjsRtiE7EPYgtiB" +
        "kKDIWOQusaCQkMhM5CexkNjM7GexiJCEyEOxhNjDsYKQ2MhusdiQzMhnsczY57HGkMOxw7HusefYUOws9hfYSOwm2ETsI9hC2EGQUMgs5Bew0JBI" +
        "yCawyNhmyCOwxJBCsMKQQZBssOyw5rDj7BbsE9ghyBaQJLBksGKwYcVg4rjxXsUw4pyKIMUY4o6KEMUMigiKBItgxbji3oswxZziz4sYxY6LDIsG" +
        "i7jF3oucxc+Ljovei8/msPNc+a/NIOaY807NEOaM80fNCOaGzQTmg8Sw4lzxL82wxJjiTpsgiRDmzuJHmxDNjMSGmwiJBJsEibDE3OJvm7CJmObv" +
        "m5jNzsTHm4yJhpuGidzE75vcic6bzonHie+b7+6g91j7ru6Q90z7p+6I90buhPdD7oLMoOZY8y7doMyQ927zJ92Q7sz3Z92IzITmQ92E7sPMgYig" +
        "xFjiLpmgiJDETOInu6CZkMzM5me7kN3M7ufEQ7uImYTMw7uEiIGI2MRumdiIzMRnu9iZzMznu8zd54jDmcOI7pnuiOe77pnn7lD3LPuX7kj3Ju5E" +
        "9yPuQu5BzFDmLPMX3NDMSPc33MjuZuYj3MTMQtzCzEHcwYhQxCziF5jQiEjEJrnQmMjMZsQjucjc5ohCucSYwohBmMGIbMQ3mOyIZrnsmOaIY7nm" +
        "mOOId7n37ij3Fu4k9xPuIu4hzCjmFtxozCTmE9xk7jPcYswh3GGIKMQWmGiIJMQTuOiYZMwzuOTcc4ghuOKYYbjhmHa49rjz9wvuEeYLzBLMEYgU" +
        "mDS4dIgRmDHCsIUgwpiFEMKM4UeFCMKGhQTCg4Wwwtzhb4WYws6FjMLHhYaFg4Xcwu+FzoXHhe/GoONY8a7GkONMxojjRsaE40PGgoSgwljhLo2g" +
        "hJDjbuEnjZDGzONnjYiEhMJDjYTGw4SBhNjCbo3YhMzCZ43MxueNxoTDhO6N7oTnjefnUPOs+dfnSPOm50Tzo+dC50HGUOMsztDGSOMmzsjnZuMj" +
        "zsTGQs7CxkHOwYRQwiyM0IRI4zed0IzIxmbCI53IzuaEQp3EjMKEQYzBhGzCN4zshGad7IzmhGOd5ozjhHeM953396j71vek+9P3oveh5yjzlu9o" +
        "97bzk+9k97PvYuch72HGKOMWzmjGJOMT3ujOZOcz3uTvc8Yh3uLOYd7hhCjCFoxohCTCE5zojGTGM73onOTOc4QhveTe84xhveKENox2hDOc9oxz" +
        "vfac873z95T7y/eS95HnFPOL7zT3m+8y5xHvMcYU4wvONMYS3nTOMsYR3nLOMd5xhBTCC4w0xhucdIwyhBG89JxyjDG88pxxvPGMO7z794nvGu8Z" +
        "zhreOt45jBqcOrx6vHmCoIKQwUyCiIKEgoKC2ILMgsaCw4LugufDUMNI4abDROGjw0LDQYJQwSyG0MNswSaGyMNmhsTDY4bCgkGGwYJswTeG7MN3" +
        "huaCY4bjgneG9+Oo46TjouOhwyjHaOO24ZPHZOOzx2LDIcdhgiiGaIIkwROO6IZkgiKO5IZigiGO4oZhgjaGdoIzjvaGc47z89Tz0vPR45TntPPb" +
        "57LjkeexwxThi8c045vPdMcywxHPcscxz3GCFMELhjTDG450hjKCEZ70jnKGMZ7yjnGCG4Y7jnue+/vq++nzyvfa88n32eOK55rjie+655nvucMK" +
        "xxrDCc86xxnfevqw/Vz1IPqY/U71EPqM/Uf1CPqG9QT6g/UC9bD63P1v6yD1mPrO6xD1jPrH6wj1husE9YPrAuuw9dz679cg65j1ztcQ64z1x9cI" +
        "64bXBOuD1wLXsOvc9e+vINeY686vENeM68evCNeGrwTXg6+w19zr76+Y186vjNfHr4av3Nfvr86vx/Sg+lj9LvSQ+kz9J/SI+kb0hPpD9IL0gemg" +
        "9Nj6bumQ9Mz6Z+mI9MbphPTD6YLpgdOg6dj07tOQ6cz059OI6cbThOnD04LTgaeg09jp7qeQ08zp56eI08anhNPDp4Kn2NPup8zT56fGp8On7qfn" +
        "9FD6LP0X9Ej6JvRE+iP0QvRB6ND0bPo36Mj0ZujE9GPowujB0dDo7PR30cjo5tHE6OPRwtHBo9DR7Oj3o8jR5qPE0eOjwqPBo+zR96Pmo+Oj9/Qo" +
        "+hb0JPoT9CL0Ieho9DboZPQz6GLoYdDo6HbQ5Ohz0OLQ4aHo0Pah5NDzoeKh4aH2ofP0FPoL9BL0Eeg09BvoMugx0HToO9By0HGg9NB7oPKg8fQK" +
        "9AnoGugZ0DrQOfKg+Vj8rvKQ+Uz8p/KI+UbyhPlD8oLygeWg8tj5buWQ8sz5Z+WI8sblhPLD5YLlgcug5djy7suQ5czy58uI5cbLhOXDy4LLgZeg" +
        "y9jl7peQy8zl55eIy8aXhMvDl4KX2Mvul8zL55fGl8OX7pfn+1D9rLX4+0j9prT8+0T9o7R++0L7QfJQ+Sz8l/bQ8kj9t/bI+2b5I/bE8kL2wvJB" +
        "9sHk0PJs+Tft0OTI8mbtyPbm8mPtxOTC7cLkwe3BydDk7PJ329DJyOTm28jt5uTj28TJwtvCycHbwZPQyezk97fQk8jJ5rfI2+bJ47fEk8K3wpPB" +
        "k+zJ97fsk+a35pPjt+OT9/so/Zay/Psk/ZOyfvsisj/7IfIo+Rb2aPIk+RP2ZPsz9mLyIfZh5GjyNuzo5GTyM+zk9nPs4uRh7OHI6OR22ejI5ORz" +
        "2eTs89niyOHZ4ZHoyPaz6JHkyPOz5Nnzs+KR4bPhkfaz9pHzs/P7FP2LsX77ErE/+xHyFPkL9jT7G/Yy8hH2MeQ08hvsdOQy7HLkMexxyHTkO9j0" +
        "7HvY8shx2PGQ9Mh7sfSQ8rHykPGx8ZD7sfv7CrC/+wnyCvYa8gn2GeQa7DrkGew5yDrYesg52HmQerD6kHmw+fsF8gX2DeQN7B3IHdg98VD4rPxX" +
        "8Uj4pvFE+KPxQvFB4tDxbPi34sjxZuLE8WPiwuLBxdDi7PF3xcji5sXE4uPFwsXBi9DF7OL3i8jF5ovExeOLwovBi+zF94vmi+OL9/mo/Naa/Pmk" +
        "/NOafvmimj/5ofEo+JbzaPEk+JPzZPmz82LxIfNh4mjxNubo4mTxM+bk83Pm4uJh5uHE6OJ2zejE5OJzzeTm883ixOHN4YnoxPab6InkxPOb5M3z" +
        "m+KJ4Zvhifab9onzm/P91Lr43X790rp83T/90bo+uh/5lPzLmX77tP3bu36ZP/uy+ZG7P/ux8RT4i/M08RL3dPu78RH3cvMx93HiNPEb5nTiMu70" +
        "5nLiMe7y5nHu8cR04jvM9MRy3fTM8sRx3fLM8d3xiPTEe5n0iPK79JnyiPG78pnxu/GI+5n7/cq5fNy//cm5Prkf+YqYv/ua+Ym5v/uZ8QrzGvEJ" +
        "9zrzGfc54hrmOuIZ7nrmOe55xDrMesQ53PrMedz5iHqY+oh5ufqY+bn5/cW4vrif+YX7jfEF8w33HeIN5h3uPcQdzD3cfYg9mH24/bhf8Kj4VvCk" +
        "+FPwovCh4WjwtuFk8LPhYuFhwujhdsLk4XPC4sLhhejC9oXkwvOF4oXhhfaF8/jU/GuNfvjSjT/40fCU+EvxtPCS8bLwkfGx4TTwm+N04TLjcuEx" +
        "43HCdOE7xvTCcsbywnHG8YT0wnuN9ITyjfKE8Y3xhPuN+/zqnXzOv/zpnT6dH/jKjL/52vjJnb/52fCK8ZrwifO68ZnzueEa4zrhGed64znnecI6" +
        "xnrCOc76xnnO+YR6jPqEeZ36jPmd+b143r69PN6fvR69D/zlnL797b2+nJ+9n/jF+c373fCF8Y3znfe94Q3jHec9733CHcY9zn3e/YQ9jH2c/by8" +
        "3l+8nryPnF+837xevE+8L/BU8FLwUeC08FvgsuCxwXTgu8FywXGC9MF7gvKC8YL7+GqGv/hp8Erw2vBJ8NngmuG64JnhucE6w3rBOcN5gnqG+oJ5" +
        "hvn8dY6+jp/4Zfjt8EXwzfHd4I3hneO9wR3DPcd9gj2GfY79nrzPX56eno+OX57fvrjfXr6c30++jr6Hnl6+3p5Pvs++XN8vvk6+R54vvm++Lr4n" +
        "vhfgWuBZwLrAuYF6gXnwbeBN4N3AncG9gT2DfYdfj16PT59cz6+fTp9Hjy+fb79Y366/TN+nv0a/Q58uv26fJ79nvyzfl78mvyOfF783vxa/E4ev" +
        "j66Pp5+sz9efpp+jj5eft5+Wn5PV8Or8qeDU+Op+qPDUfOo/qHjUPqg8/Wit8Nb8/WSs+NZ+/WKsfNY//WGsPvro/Xau/Prk/XOufvrirj/64fXo" +
        "+vb15Prz9eL14evo9fbr5PXz6+Lr4dfo6/bX5Ovz1+Kl4NL46X6k8NJ86T+keNI+pDzSH6Qe/TSm+NN+/TKmfNM//TGmPqYf+nT9O6d++nKnP/px" +
        "9PT6e/Ty9PHp9PT76fLp8dP06fvT8tPxovDRfOi/onjRPqI80R+iHqIP/RqjfNG//RmjPqMf+jqjv/o59Hr0eej66PnR+tH5oXjQvqE80J+hHqEP" +
        "/Q2hvqGf+h30Peh9oLzQX6CeoI+g36BeoE+V4Mr45X6U8Mp85T+UeMo+lDzKH5Qe/LSW+Mt+/LKWfMs//LGWPpYf+XT8u5d++XKXP/lx8vT5e/Ly" +
        "8vHl9PL75fLl8cv05fvL8svx2vDtfPa/tODaeO0+tHDaPO0ftDjaHrQc2g+0DpLwyXzkv7bwknjJPrZ42z7JH7Y8kh62HpIPtg/8mpN8yb/9uvyZ" +
        "t3yTPv25tz6TH7cf+TqTv/t6+Tm3v/t58nr2+vJ59vnk+u365Pnt+cn6yfmy4Nl47L6ycNk87J+yONkeshzZD7IOsgeReMi+s3iRPMifszzZn7Me" +
        "kQ+zD/yNkb79nbO+kZ+zn/kd+z3yPfZ95H3s/cj9sXDYvOxfsTjYnrEc2I+xDrEHkLzIX7G8kJ6xnpCPsY+Q37HfsLjYXrCc2E+wjrCHkF6w3pBP" +
        "sM+wXNgvsE6wR5AvsG+wLrAnivDFfOK/injFPoo8xR+KHooP/FqLfMW//FmLPosf+LqLv/i58XrxeeL64vnF+sX5muDNeOa+mnDNPOafmjjNHpoc" +
        "zQ+aDpoHiXjEvpt4iTzEn5s8zZ+bHokPmw/8TYm+/N2bvomfm5/4nfm98T3zfeJ95v3E/d1w7rz3X7pg3Tjunrow3Rzuj7oY3Q66DN0HugaZcMy8" +
        "5l+7cJk4zJ67ON2ezI+7HJkOuw6ZB7sHiLzEX5m8iJ67vJmeiI+7npmPu4+I35nfu9+5YNy47l65MNyc7k+5GNyOuQzch7kGuQOYuMxeubiYnMxP" +
        "uZzcz7mOmIe5h4hemN6IT7nemM+5z7iw3FzuL7iY3E64jNxHuIa4g5hczC+43JhOuM6YR7jHiC+Yb7jvuFjcLrhM3Ce4RrhDmC64bpgnuGe4LNwX" +
        "uCa4I5gXuDe4FrgThXjCvoU8wp+FHoUPhb6Fn/hd8L3hfcL9jXDGvONfjTjGno0cxo+NDo0HhLzCX428hJ6NnoSPjY+E343fnWDOuOdenTDOnOdP" +
        "nRjOjp0MzoedBp0DjLjGXp24jJzGT52cjI6djoyHnYeEXozehE+d3ozPnc/esO9c96+9IN6Y7069EN6M70e9CN6GvQTeg70CnLDOXOcvvbCcmM5O" +
        "vZjezs5HvYychr2GnIO9g4xcxi+c3IxOvdyczoxHvc6cx73HhC+Mb5zvve+8oN5Y7y68kN5M7ye8iN5GvITeQ7yCvIGcWM4uvNicTM4nvMzeZ7zG" +
        "nEO8w4wunG6MJ7zunGe857xQ3izvF7xI3ia8RN4jvEK8QZwszhe8bJwmvGacI7xjjBecN7x3vCjeFrwk3hO8IrwhnBa8NpwTvDO8FN4LvBK8EZwL" +
        "vBuCvMFfgp6Cj4LfhrjDXoacw0+GjoaHgl6G3oJPhs+OsMdc46+OmMdOjozHR46GjoOGXMMvjtyGTo7OhkeOx4Ivhm+O756gz1jnrp6Qz0znp56I" +
        "z0aehM9DnoKegY5Yxy6e2I5MxyeezM9nnsaOQ57Dhi6OboYnnu6OZ57n31DvrPfX30jvpt9E76PfQt9BnlDPLOeXvtCeSM8mvsjfZs8jvsSeQr7C" +
        "nkG+wY4sxxeebI4mvuyeZo4jvuaeY77jhheON553vvffKO+W3yTvk98i3yGeKM8WvmieJM8TvmTfM75iniG+YY4WnjaOE752njO+c98U74vfEt8R" +
        "nhTPC740nhK+Mp4RvjGOC54bvjvfCt8Jngq+Gp4JvhmBXoFPg1zBr4NOg0eBL4Nvh1jDrodMw6eHRodDgy6HboMnh2ePUMes49ePSMemj0THo49C" +
        "j0GHLMOXj2zHt49mhyOPY4MXhzePd8+o59bPpOfTz6LPoY8ox5afaM+2x5OfZI8in2KPIZ9hhxaPNocTn3aPM59z79T36+/S79HPlOfL37TPkt+y" +
        "z5HfsY8Ux4ufNI8Sv3SfMo8Rv3KfMb9xhwuPG587v3vvyu/Jz4rfms+J35mPCp8ajwm/Op8Zvznvxc+F342PBZ8Nvx2BroGng6zB14Omg6OBl4O3" +
        "h6jD1oekw9OHooehg5aHtoOTh7PH1OPrx9LH0YeUw8uPtMfbj7KHkY+xg4uHm4+75+rn6cfKz9rHyc/Zh4qPmoeJn7qPmZ+55+XHxc/Nh4WPjZ+d" +
        "gdaB04PUweuD0oPRgcuD28Pqw+mDyofag8mH2eP1";

    static readonly byte[] PatternBytes = global::System.Convert.FromBase64String(
        EncodedPatterns);

    internal static int GetPattern(int clusterIndex, int codeword)
    {
        ValidateClusterIndex(clusterIndex);
        ValidateCodeword(codeword);
        int patternIndex = (clusterIndex * CodewordCount) + codeword;
        int byteIndex = patternIndex * 2;
        int trailingModules = (PatternBytes[byteIndex] << 8)
            | PatternBytes[byteIndex + 1];
        return trailingModules << 1;
    }

    internal static int[] CopyClusterPatterns(int clusterIndex)
    {
        ValidateClusterIndex(clusterIndex);
        int[] patterns = new int[CodewordCount];
        int codeword = 0;
        while (codeword < CodewordCount)
        {
            patterns[codeword] = GetPattern(clusterIndex, codeword);
            codeword = codeword + 1;
        }

        return patterns;
    }

    internal static int FindCodeword(int clusterIndex, int pattern)
    {
        ValidateClusterIndex(clusterIndex);
        int codeword = 0;
        while (codeword < CodewordCount)
        {
            if (GetPattern(clusterIndex, codeword) == pattern)
            {
                return codeword;
            }

            codeword = codeword + 1;
        }

        return -1;
    }

    internal static int GetClusterNumber(int pattern)
    {
        int[] widths = GetRunWidths(pattern);
        return (widths[0] - widths[2] + widths[4] - widths[6] + 9) % 9;
    }

    internal static int[] GetRunWidths(int pattern)
    {
        if (pattern < 0x10000 || pattern > 0x1ffff || (pattern & 1) != 0)
        {
            throw new BarcodeValidationException("PDF417 codeword pattern must contain 17 modules beginning with a bar and ending with a space.");
        }

        int[] widths = new int[RunsPerCodeword];
        int runIndex = 0;
        int currentBit = 1;
        int bitIndex = ModulesPerCodeword - 1;
        while (bitIndex >= 0)
        {
            int bit = (pattern >> bitIndex) & 1;
            if (bit == currentBit)
            {
                widths[runIndex] = widths[runIndex] + 1;
            }
            else
            {
                runIndex = runIndex + 1;
                if (runIndex >= RunsPerCodeword)
                {
                    throw new BarcodeValidationException("PDF417 codeword pattern contains more than eight runs.");
                }

                currentBit = bit;
                widths[runIndex] = 1;
            }

            bitIndex = bitIndex - 1;
        }

        if (runIndex != RunsPerCodeword - 1)
        {
            throw new BarcodeValidationException("PDF417 codeword pattern must contain exactly eight runs.");
        }

        return widths;
    }

    static void ValidateClusterIndex(int clusterIndex)
    {
        if (clusterIndex < 0 || clusterIndex >= ClusterCount)
        {
            throw new BarcodeValidationException("PDF417 cluster index must be zero, one, or two.");
        }
    }

    static void ValidateCodeword(int codeword)
    {
        if (codeword < 0 || codeword >= CodewordCount)
        {
            throw new BarcodeValidationException("PDF417 codeword value must be between 0 and 928.");
        }
    }
}
