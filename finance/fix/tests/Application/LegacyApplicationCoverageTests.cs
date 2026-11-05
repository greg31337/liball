using System;
using System.Collections.Generic;
using fix;

namespace fix.Tests;

[TestFixture]
public class LegacyApplicationCoverageTests
{
    [Test]
    public void NewOrderSingleCompletesTheApplicationFlowAcrossEveryLegacyVersion()
    {
        string[] dictionaryNames = ["FIX40", "FIX41", "FIX42", "FIX43", "FIX44", "FIX50", "FIX50SP1", "FIX50SP2"];
        List<string> failures = [];

        for (int index = 0; index < dictionaryNames.Length; index++)
        {
            string dictionaryName = dictionaryNames[index];
            string failure = RunFlow(dictionaryName, "D", "NewOrderSingle");
            if (failure.Length != 0) failures.Add(dictionaryName + ":" + failure);
        }

        Assert.That(failures, Is.Empty, string.Join(Environment.NewLine, failures));
    }

    [TestCase("order", "FIX40", "D", "NewOrderSingle")]
    [TestCase("execution", "FIX41", "8", "ExecutionReport")]
    [TestCase("market data", "FIX42", "V", "MarketDataRequest")]
    [TestCase("quote", "FIX42", "S", "Quote")]
    [TestCase("security definition", "FIX43", "d", "SecurityDefinition")]
    [TestCase("allocation", "FIX43", "J", "Allocation")]
    [TestCase("trade capture", "FIX44", "AE", "TradeCaptureReport")]
    [TestCase("position", "FIX44", "AP", "PositionReport")]
    [TestCase("collateral", "FIX44", "BA", "CollateralReport")]
    [TestCase("settlement", "FIX41", "T", "SettlementInstructions")]
    [TestCase("user", "FIX44", "BE", "UserRequest")]
    [TestCase("session", "FIX40", "A", "Logon")]
    public void RepresentativeLegacyBusinessFlowRoundTrips(string area, string dictionaryName, string msgType, string messageName)
    {
        string failure = RunFlow(dictionaryName, msgType, messageName);

        Assert.That(failure, Is.Empty, area + ":" + dictionaryName + ":" + failure);
    }

    private static string RunFlow(string dictionaryName, string msgType, string messageName)
    {
        DictionaryLoadResult loaded = new EmbeddedDictionaryProvider(null).ResolveDictionary(new FixDictionaryIdentity { Name = dictionaryName });
        if (!loaded.Success || loaded.Dictionary == null) return "load:" + string.Join(",", loaded.Diagnostics.ConvertAll(item => item.Code));
        FixDictionary dictionary = loaded.Dictionary;
        FixMessageDefinition? definition = dictionary.Messages.Find(item => item.MsgType == msgType && item.Name == messageName && !item.IsRemoved);
        if (definition == null) return "missing-definition";
        string beginString = dictionary.Identity.BeginString.Length == 0 ? FixBeginStrings.Fixt11 : dictionary.Identity.BeginString;
        return LatestApplicationCoverageTests.RunApplicationMessageFlow(dictionary, definition, beginString);
    }
}
