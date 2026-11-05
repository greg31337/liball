using System.Collections.Generic;

namespace molecule;

internal class CommonFragments
{
    internal const string SaltCategory = "salt";
    internal const string SolventCategory = "solvent";
    internal const string CounterionCategory = "counterion";

    internal List<CommonFragmentRecord> Fragments;

    internal CommonFragments()
    {
        Fragments = new List<CommonFragmentRecord>();
        RegisterDefaults();
    }

    internal void RegisterDefaults()
    {
        Fragments.Clear();
        AddSaltCounterion("chloride", "[Cl-]", "Cl");
        AddSaltCounterion("bromide", "[Br-]", "Br");
        AddSaltCounterion("iodide", "[I-]", "I");
        AddSaltCounterion("fluoride", "[F-]", "F");
        AddSaltCounterion("sodium", "[Na+]", "Na");
        AddSaltCounterion("potassium", "[K+]", "K");
        AddSaltCounterion("lithium", "[Li+]", "Li");
        AddSaltCounterion("calcium", "[Ca+2]", "Ca");
        AddSaltCounterion("magnesium", "[Mg+2]", "Mg");
        AddSaltCounterion("ammonium", "[NH4+]", "H4N");
        AddSaltCounterion("acetate", "CC(=O)[O-]", "C2H3O2");
        AddSaltCounterion("trifluoroacetate", "FC(F)(F)C(=O)[O-]", "C2F3O2");
        AddSaltCounterion("formate", "C(=O)[O-]", "CHO2");
        AddSaltCounterion("mesylate", "CS(=O)(=O)[O-]", "CH3O3S");
        AddSaltCounterion("tosylate", "Cc1ccc(S(=O)(=O)[O-])cc1", "C7H7O3S");
        AddSaltCounterion("sulfate", "[O-]S(=O)(=O)[O-]", "O4S");
        AddSaltCounterion("phosphate", "[O-]P(=O)([O-])[O-]", "O4P");
        AddSaltCounterion("nitrate", "[O-][N+](=O)[O-]", "NO3");
        AddSaltCounterion("carbonate", "[O-]C(=O)[O-]", "CO3");
        AddSolvent("water", "O", "H2O");
        AddSolvent("methanol", "CO", "CH4O");
        AddSolvent("ethanol", "CCO", "C2H6O");
        AddSolvent("isopropanol", "CC(C)O", "C3H8O");
        AddSolvent("acetonitrile", "CC#N", "C2H3N");
        AddSolvent("acetone", "CC(=O)C", "C3H6O");
        AddSolvent("dimethyl sulfoxide", "CS(=O)C", "C2H6OS");
        AddSolvent("dimethylformamide", "CN(C)C=O", "C3H7NO");
        AddSolvent("tetrahydrofuran", "C1CCOC1", "C4H8O");
        AddSolvent("dichloromethane", "ClCCl", "CH2Cl2");
        AddSolvent("chloroform", "ClC(Cl)Cl", "CHCl3");
        AddSolvent("ethyl acetate", "CCOC(=O)C", "C4H8O2");
        AddSolvent("diethyl ether", "CCOCC", "C4H10O");
        AddSolvent("hexane", "CCCCCC", "C6H14");
        AddSolvent("benzene", "c1ccccc1", "C6H6");
        AddSolvent("toluene", "Cc1ccccc1", "C7H8");
    }

    internal void AddSaltCounterion(string name, string smiles, string formula)
    {
        Add(name, smiles, formula, true, false, true);
    }

    internal void AddSolvent(string name, string smiles, string formula)
    {
        Add(name, smiles, formula, false, true, false);
    }

    internal void Add(string name, string smiles, string formula, bool isSalt, bool isSolvent, bool isCounterion)
    {
        CommonFragmentRecord record = new CommonFragmentRecord();
        record.Name = NormalizeString(name);
        record.Smiles = NormalizeString(smiles);
        record.Formula = NormalizeString(formula);
        record.IsSalt = isSalt;
        record.IsSolvent = isSolvent;
        record.IsCounterion = isCounterion;
        record.Category = SelectCategory(isSalt, isSolvent, isCounterion);
        Fragments.Add(record);
    }

    internal List<CommonFragmentRecord> GetAll()
    {
        List<CommonFragmentRecord> records = new List<CommonFragmentRecord>();
        int index = 0;
        while (index < Fragments.Count)
        {
            records.Add(Fragments[index]);
            index = index + 1;
        }

        return records;
    }

    internal List<CommonFragmentRecord> GetSalts()
    {
        return GetByFlag(true, false, false);
    }

    internal List<CommonFragmentRecord> GetSolvents()
    {
        return GetByFlag(false, true, false);
    }

    internal List<CommonFragmentRecord> GetCounterions()
    {
        return GetByFlag(false, false, true);
    }

    internal List<CommonFragmentRecord> GetByCategory(string category)
    {
        string normalized = NormalizeKey(category);
        if (normalized == SaltCategory)
        {
            return GetSalts();
        }

        if (normalized == SolventCategory)
        {
            return GetSolvents();
        }

        if (normalized == CounterionCategory)
        {
            return GetCounterions();
        }

        return new List<CommonFragmentRecord>();
    }

    internal List<CommonFragmentRecord> GetByFlag(bool salts, bool solvents, bool counterions)
    {
        List<CommonFragmentRecord> records = new List<CommonFragmentRecord>();
        int index = 0;
        while (index < Fragments.Count)
        {
            CommonFragmentRecord record = Fragments[index];
            if ((salts && record.IsSalt) || (solvents && record.IsSolvent) || (counterions && record.IsCounterion))
            {
                records.Add(record);
            }

            index = index + 1;
        }

        return records;
    }

    internal CommonFragmentRecord FindByName(string name)
    {
        string normalized = NormalizeKey(name);
        int index = 0;
        while (index < Fragments.Count)
        {
            if (NormalizeKey(Fragments[index].Name) == normalized)
            {
                return Fragments[index];
            }

            index = index + 1;
        }

        return new CommonFragmentRecord();
    }

    internal CommonFragmentRecord FindBySmiles(string smiles)
    {
        string normalized = NormalizeString(smiles);
        int index = 0;
        while (index < Fragments.Count)
        {
            if (Fragments[index].Smiles == normalized)
            {
                return Fragments[index];
            }

            index = index + 1;
        }

        return new CommonFragmentRecord();
    }

    internal bool ContainsName(string name)
    {
        return FindByName(name).Name.Length > 0;
    }

    internal bool ContainsSmiles(string smiles)
    {
        return FindBySmiles(smiles).Smiles.Length > 0;
    }

    internal bool IsSaltName(string name)
    {
        return FindByName(name).IsSalt;
    }

    internal bool IsSolventName(string name)
    {
        return FindByName(name).IsSolvent;
    }

    internal bool IsCounterionName(string name)
    {
        return FindByName(name).IsCounterion;
    }

    internal string SelectCategory(bool isSalt, bool isSolvent, bool isCounterion)
    {
        if (isSolvent)
        {
            return SolventCategory;
        }

        if (isCounterion)
        {
            return CounterionCategory;
        }

        if (isSalt)
        {
            return SaltCategory;
        }

        return "";
    }

    internal string NormalizeKey(string value)
    {
        return NormalizeString(value).Trim().ToLowerInvariant();
    }

    internal string NormalizeString(string? value)
    {
        if (value == null)
        {
            return "";
        }

        return value;
    }
}

internal class CommonFragmentRecord
{
    internal string Name;
    internal string Smiles;
    internal string Formula;
    internal string Category;
    internal bool IsSalt;
    internal bool IsSolvent;
    internal bool IsCounterion;

    internal CommonFragmentRecord()
    {
        Name = "";
        Smiles = "";
        Formula = "";
        Category = "";
        IsSalt = false;
        IsSolvent = false;
        IsCounterion = false;
    }
}
