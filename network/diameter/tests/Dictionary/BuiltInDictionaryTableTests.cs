namespace diameter.Tests;

public class BuiltInDictionaryTableTests
{
    [Test]
    public void EveryBuiltInCommandMatchesTheCommandDefinitionTable()
    {
        string[] table = new string[]
        {
            "rfc6733|0|257|true|Capabilities-Exchange|128|112|true|12",
            "rfc6733|0|257|false|Capabilities-Exchange|0|240|true|15",
            "rfc6733|0|282|true|Disconnect-Peer|128|112|true|3",
            "rfc6733|0|282|false|Disconnect-Peer|0|240|true|5",
            "rfc6733|0|280|true|Device-Watchdog|128|112|true|3",
            "rfc6733|0|280|false|Device-Watchdog|0|240|true|6",
            "rfc6733|0|258|true|Re-Auth|192|32|true|11",
            "rfc6733|0|258|false|Re-Auth|64|176|true|13",
            "rfc6733|0|275|true|Session-Termination|192|32|true|12",
            "rfc6733|0|275|false|Session-Termination|64|176|true|14",
            "rfc6733|0|274|true|Abort-Session|192|32|true|10",
            "rfc6733|0|274|false|Abort-Session|64|176|true|13",
            "rfc6733|3|271|true|Accounting|192|32|true|19",
            "rfc6733|3|271|false|Accounting|64|176|true|20",
            "rfc6737|10|328|true|Capabilities-Update|128|112|true|11",
            "rfc6737|10|328|false|Capabilities-Update|0|240|true|4",
            "rfc8506|4|272|true|Credit-Control|192|32|true|29",
            "rfc8506|4|272|false|Credit-Control|64|176|true|28",
            "rfc8506|4|258|true|Re-Auth|192|32|true|15",
            "rfc8506|4|258|false|Re-Auth|64|176|true|17"
        };
        DiameterDictionarySnapshot snapshot = CreateSnapshot();
        int actualCount = 0;
        int moduleIndex = 0;
        while (moduleIndex < snapshot.Modules.Count)
        {
            actualCount = actualCount + snapshot.Modules[moduleIndex].Commands.Count;
            moduleIndex = moduleIndex + 1;
        }

        Assert.That(actualCount, Is.EqualTo(table.Length));
        int index = 0;
        while (index < table.Length)
        {
            string[] values = table[index].Split('|');
            DiameterDictionaryModule module = FindModule(snapshot, values[0]);
            uint applicationId = uint.Parse(values[1]);
            uint code = uint.Parse(values[2]);
            bool request = bool.Parse(values[3]);
            DiameterCommandDefinition command = FindCommand(
                module,
                applicationId,
                code,
                request);
            Assert.Multiple(() =>
            {
                Assert.That(command.Name, Is.EqualTo(values[4]), table[index]);
                Assert.That((int)command.RequiredFlags,
                    Is.EqualTo(int.Parse(values[5])), table[index]);
                Assert.That((int)command.ProhibitedFlags,
                    Is.EqualTo(int.Parse(values[6])), table[index]);
                Assert.That(command.AllowUnlistedAvps,
                    Is.EqualTo(bool.Parse(values[7])), table[index]);
                Assert.That(command.Rules,
                    Has.Count.EqualTo(int.Parse(values[8])), table[index]);
            });
            index = index + 1;
        }
    }

    [Test]
    public void EveryBuiltInGroupedAvpAndRuleMatchesTheGrammarTable()
    {
        string[] table = new string[]
        {
            "rfc6733|260|Vendor-Specific-Application-Id|false|266:Vendor-Id:1:1;258:Auth-Application-Id:0:1;259:Acct-Application-Id:0:1",
            "rfc6733|279|Failed-AVP|true|0:AVP:1:-1",
            "rfc6733|284|Proxy-Info|true|280:Proxy-Host:1:1;33:Proxy-State:1:1",
            "rfc6733|297|Experimental-Result|false|266:Vendor-Id:1:1;298:Experimental-Result-Code:1:1",
            "rfc7683|621|OC-Supported-Features|true|622:OC-Feature-Vector:0:1",
            "rfc7683|623|OC-OLR|true|624:OC-Sequence-Number:1:1;626:OC-Report-Type:1:1;627:OC-Reduction-Percentage:0:1;625:OC-Validity-Duration:0:1",
            "rfc8506|423|Cost-Information|false|445:Unit-Value:1:1;425:Currency-Code:1:1;424:Cost-Unit:0:1",
            "rfc8506|445|Unit-Value|false|447:Value-Digits:1:1;429:Exponent:0:1",
            "rfc8506|456|Multiple-Services-Credit-Control|true|431:Granted-Service-Unit:0:1;437:Requested-Service-Unit:0:1;446:Used-Service-Unit:0:-1;452:Tariff-Change-Usage:0:1;439:Service-Identifier:0:-1;432:Rating-Group:0:1;457:G-S-U-Pool-Reference:0:-1;448:Validity-Time:0:1;268:Result-Code:0:1;430:Final-Unit-Indication:0:1;669:QoS-Final-Unit-Indication:0:1",
            "rfc8506|431|Granted-Service-Unit|true|451:Tariff-Time-Change:0:1;420:CC-Time:0:1;413:CC-Money:0:1;421:CC-Total-Octets:0:1;412:CC-Input-Octets:0:1;414:CC-Output-Octets:0:1;417:CC-Service-Specific-Units:0:1",
            "rfc8506|437|Requested-Service-Unit|true|420:CC-Time:0:1;413:CC-Money:0:1;421:CC-Total-Octets:0:1;412:CC-Input-Octets:0:1;414:CC-Output-Octets:0:1;417:CC-Service-Specific-Units:0:1",
            "rfc8506|446|Used-Service-Unit|true|452:Tariff-Change-Usage:0:1;420:CC-Time:0:1;413:CC-Money:0:1;421:CC-Total-Octets:0:1;412:CC-Input-Octets:0:1;414:CC-Output-Octets:0:1;417:CC-Service-Specific-Units:0:1",
            "rfc8506|413|CC-Money|false|445:Unit-Value:1:1;425:Currency-Code:0:1",
            "rfc8506|457|G-S-U-Pool-Reference|false|453:G-S-U-Pool-Identifier:1:1;454:CC-Unit-Type:1:1;445:Unit-Value:1:1",
            "rfc8506|430|Final-Unit-Indication|false|449:Final-Unit-Action:1:1;438:Restriction-Filter-Rule:0:-1;11:Filter-Id:0:-1;434:Redirect-Server:0:1",
            "rfc8506|434|Redirect-Server|false|433:Redirect-Address-Type:1:1;435:Redirect-Server-Address:1:1",
            "rfc8506|440|Service-Parameter-Info|false|441:Service-Parameter-Type:1:1;442:Service-Parameter-Value:1:1",
            "rfc8506|443|Subscription-Id|false|450:Subscription-Id-Type:1:1;444:Subscription-Id-Data:1:1",
            "rfc8506|458|User-Equipment-Info|false|459:User-Equipment-Info-Type:1:1;460:User-Equipment-Info-Value:1:1",
            "rfc8506|653|User-Equipment-Info-Extension|true|654:User-Equipment-Info-IMEISV:0:1;655:User-Equipment-Info-MAC:0:1;656:User-Equipment-Info-EUI64:0:1;657:User-Equipment-Info-ModifiedEUI64:0:1;658:User-Equipment-Info-IMEI:0:1",
            "rfc8506|659|Subscription-Id-Extension|true|660:Subscription-Id-E164:0:1;661:Subscription-Id-IMSI:0:1;662:Subscription-Id-SIP-URI:0:1;663:Subscription-Id-NAI:0:1;664:Subscription-Id-Private:0:1",
            "rfc8506|665|Redirect-Server-Extension|true|666:Redirect-Address-IPAddress:0:1;667:Redirect-Address-URL:0:1;668:Redirect-Address-SIP-URI:0:1",
            "rfc8506|669|QoS-Final-Unit-Indication|true|449:Final-Unit-Action:1:1;509:Filter-Rule:0:-1;11:Filter-Id:0:-1;665:Redirect-Server-Extension:0:1",
            "rfc8583|650|Load|true|651:Load-Type:0:1;652:Load-Value:0:1;649:SourceID:0:1"
        };
        DiameterDictionarySnapshot snapshot = CreateSnapshot();
        int actualCount = CountGroupedAvps(snapshot);
        Assert.That(actualCount, Is.EqualTo(table.Length));

        int index = 0;
        while (index < table.Length)
        {
            string[] values = table[index].Split('|');
            DiameterAvpDefinition avp = FindAvp(
                FindModule(snapshot, values[0]),
                uint.Parse(values[1]));
            string[] rules = values[4].Split(';');
            Assert.Multiple(() =>
            {
                Assert.That(avp.Name, Is.EqualTo(values[2]), table[index]);
                Assert.That(avp.BaseType, Is.EqualTo(DiameterAvpBaseType.Grouped), table[index]);
                Assert.That(avp.AllowUnlistedAvps,
                    Is.EqualTo(bool.Parse(values[3])), table[index]);
                Assert.That(avp.GroupedRules, Has.Count.EqualTo(rules.Length), table[index]);
            });
            int ruleIndex = 0;
            while (ruleIndex < rules.Length)
            {
                string[] ruleValues = rules[ruleIndex].Split(':');
                DiameterAvpRuleDefinition rule = avp.GroupedRules[ruleIndex];
                Assert.Multiple(() =>
                {
                    Assert.That(rule.AvpCode,
                        Is.EqualTo(uint.Parse(ruleValues[0])), table[index]);
                    Assert.That(rule.AvpVendorId, Is.Zero, table[index]);
                    Assert.That(rule.AvpName, Is.EqualTo(ruleValues[1]), table[index]);
                    Assert.That(rule.MinimumCount,
                        Is.EqualTo(int.Parse(ruleValues[2])), table[index]);
                    Assert.That(rule.MaximumCount,
                        Is.EqualTo(int.Parse(ruleValues[3])), table[index]);
                    Assert.That(rule.FixedHeadPosition, Is.EqualTo(-1), table[index]);
                    Assert.That(rule.OrderingGroup, Is.EqualTo(-1), table[index]);
                });
                ruleIndex = ruleIndex + 1;
            }

            index = index + 1;
        }
    }

    [Test]
    public void EveryBuiltInTypeMatchesTheTypeHierarchyTable()
    {
        string[] table = new string[]
        {
            "rfc6733|OctetString|0||-1", "rfc6733|Integer32|1||4",
            "rfc6733|Integer64|2||8", "rfc6733|Unsigned32|3||4",
            "rfc6733|Unsigned64|4||8", "rfc6733|Float32|5||4",
            "rfc6733|Float64|6||8", "rfc6733|Grouped|7||-1",
            "rfc6733|Address|0|OctetString|-1", "rfc6733|Time|0|OctetString|4",
            "rfc6733|UTF8String|0|OctetString|-1",
            "rfc6733|DiameterIdentity|0|OctetString|-1",
            "rfc6733|DiameterURI|0|OctetString|-1",
            "rfc6733|Enumerated|1|Integer32|4",
            "rfc6733|IPFilterRule|0|OctetString|-1",
            "rfc6733|Disconnect-Cause|1|Enumerated|4",
            "rfc6733|Redirect-Host-Usage|1|Enumerated|4",
            "rfc6733|Auth-Request-Type|1|Enumerated|4",
            "rfc6733|Auth-Session-State|1|Enumerated|4",
            "rfc6733|Re-Auth-Request-Type|1|Enumerated|4",
            "rfc6733|Termination-Cause|1|Enumerated|4",
            "rfc6733|Session-Server-Failover|1|Enumerated|4",
            "rfc6733|Accounting-Record-Type|1|Enumerated|4",
            "rfc6733|Accounting-Realtime-Required|1|Enumerated|4",
            "rfc7683|OC-Report-Type|1|Enumerated|4",
            "rfc7944|DRMP|1|Enumerated|4",
            "rfc8506|CC-Request-Type|1|Enumerated|4",
            "rfc8506|CC-Session-Failover|1|Enumerated|4",
            "rfc8506|Check-Balance-Result|1|Enumerated|4",
            "rfc8506|Credit-Control|1|Enumerated|4",
            "rfc8506|Credit-Control-Failure-Handling|1|Enumerated|4",
            "rfc8506|Direct-Debiting-Failure-Handling|1|Enumerated|4",
            "rfc8506|Tariff-Change-Usage|1|Enumerated|4",
            "rfc8506|CC-Unit-Type|1|Enumerated|4",
            "rfc8506|Final-Unit-Action|1|Enumerated|4",
            "rfc8506|Redirect-Address-Type|1|Enumerated|4",
            "rfc8506|Multiple-Services-Indicator|1|Enumerated|4",
            "rfc8506|Requested-Action|1|Enumerated|4",
            "rfc8506|Subscription-Id-Type|1|Enumerated|4",
            "rfc8506|User-Equipment-Info-Type|1|Enumerated|4",
            "rfc8506|QoSFilterRule|0|OctetString|-1",
            "rfc8583|Load-Type|1|Enumerated|4"
        };
        DiameterDictionarySnapshot snapshot = CreateSnapshot();
        int actualCount = 0;
        int moduleIndex = 0;
        while (moduleIndex < snapshot.Modules.Count)
        {
            actualCount = actualCount + snapshot.Modules[moduleIndex].Types.Count;
            moduleIndex = moduleIndex + 1;
        }

        Assert.That(actualCount, Is.EqualTo(table.Length));
        int index = 0;
        while (index < table.Length)
        {
            string[] values = table[index].Split('|');
            DiameterTypeDefinition type = FindType(
                FindModule(snapshot, values[0]),
                values[1]);
            Assert.Multiple(() =>
            {
                Assert.That(type.VendorId, Is.Zero, table[index]);
                Assert.That((int)type.BaseType,
                    Is.EqualTo(int.Parse(values[2])), table[index]);
                Assert.That(type.ParentTypeName, Is.EqualTo(values[3]), table[index]);
                Assert.That(type.ParentTypeVendorId, Is.Zero, table[index]);
                Assert.That(type.FixedDataLength,
                    Is.EqualTo(int.Parse(values[4])), table[index]);
            });
            index = index + 1;
        }
    }

    [Test]
    public void EveryBuiltInEnumeratedValueMatchesTheDomainTable()
    {
        string[] table = new string[]
        {
            "rfc6733|Disconnect-Cause|273|REBOOTING=0;BUSY=1;DO_NOT_WANT_TO_TALK_TO_YOU=2",
            "rfc6733|Redirect-Host-Usage|261|DONT_CACHE=0;ALL_SESSION=1;ALL_REALM=2;REALM_AND_APPLICATION=3;ALL_APPLICATION=4;ALL_HOST=5;ALL_USER=6",
            "rfc6733|Auth-Request-Type|274|AUTHENTICATE_ONLY=1;AUTHORIZE_ONLY=2;AUTHORIZE_AUTHENTICATE=3",
            "rfc6733|Auth-Session-State|277|STATE_MAINTAINED=0;NO_STATE_MAINTAINED=1",
            "rfc6733|Re-Auth-Request-Type|285|AUTHORIZE_ONLY=0;AUTHORIZE_AUTHENTICATE=1",
            "rfc6733|Termination-Cause|295|DIAMETER_LOGOUT=1;DIAMETER_SERVICE_NOT_PROVIDED=2;DIAMETER_BAD_ANSWER=3;DIAMETER_ADMINISTRATIVE=4;DIAMETER_LINK_BROKEN=5;DIAMETER_AUTH_EXPIRED=6;DIAMETER_USER_MOVED=7;DIAMETER_SESSION_TIMEOUT=8",
            "rfc6733|Session-Server-Failover|271|REFUSE_SERVICE=0;TRY_AGAIN=1;ALLOW_SERVICE=2;TRY_AGAIN_ALLOW_SERVICE=3",
            "rfc6733|Accounting-Record-Type|480|EVENT_RECORD=1;START_RECORD=2;INTERIM_RECORD=3;STOP_RECORD=4",
            "rfc6733|Accounting-Realtime-Required|483|DELIVER_AND_GRANT=1;GRANT_AND_STORE=2;GRANT_AND_LOSE=3",
            "rfc6733||299|NO_INBAND_SECURITY=0;TLS=1",
            "rfc7075||268|DIAMETER_REALM_REDIRECT_INDICATION=3011",
            "rfc7683||622|OLR_DEFAULT_ALGO=1",
            "rfc7683|OC-Report-Type|626|HOST_REPORT=0;REALM_REPORT=1",
            "rfc7944|DRMP|301|PRIORITY_0=0;PRIORITY_1=1;PRIORITY_2=2;PRIORITY_3=3;PRIORITY_4=4;PRIORITY_5=5;PRIORITY_6=6;PRIORITY_7=7;PRIORITY_8=8;PRIORITY_9=9;PRIORITY_10=10;PRIORITY_11=11;PRIORITY_12=12;PRIORITY_13=13;PRIORITY_14=14;PRIORITY_15=15",
            "rfc8506|CC-Request-Type|416|INITIAL_REQUEST=1;UPDATE_REQUEST=2;TERMINATION_REQUEST=3;EVENT_REQUEST=4",
            "rfc8506|CC-Session-Failover|418|FAILOVER_NOT_SUPPORTED=0;FAILOVER_SUPPORTED=1",
            "rfc8506|Check-Balance-Result|422|ENOUGH_CREDIT=0;NO_CREDIT=1",
            "rfc8506|Credit-Control|426|CREDIT_AUTHORIZATION=0;RE_AUTHORIZATION=1",
            "rfc8506|Credit-Control-Failure-Handling|427|TERMINATE=0;CONTINUE=1;RETRY_AND_TERMINATE=2",
            "rfc8506|Direct-Debiting-Failure-Handling|428|TERMINATE_OR_BUFFER=0;CONTINUE=1",
            "rfc8506|Tariff-Change-Usage|452|UNIT_BEFORE_TARIFF_CHANGE=0;UNIT_AFTER_TARIFF_CHANGE=1;UNIT_INDETERMINATE=2",
            "rfc8506|CC-Unit-Type|454|TIME=0;MONEY=1;TOTAL-OCTETS=2;INPUT-OCTETS=3;OUTPUT-OCTETS=4;SERVICE-SPECIFIC-UNITS=5",
            "rfc8506|Final-Unit-Action|449|TERMINATE=0;REDIRECT=1;RESTRICT_ACCESS=2",
            "rfc8506|Redirect-Address-Type|433|IPV4_ADDRESS=0;IPV6_ADDRESS=1;URL=2;SIP_URI=3",
            "rfc8506|Multiple-Services-Indicator|455|MULTIPLE_SERVICES_NOT_SUPPORTED=0;MULTIPLE_SERVICES_SUPPORTED=1",
            "rfc8506|Requested-Action|436|DIRECT_DEBITING=0;REFUND_ACCOUNT=1;CHECK_BALANCE=2;PRICE_ENQUIRY=3",
            "rfc8506|Subscription-Id-Type|450|END_USER_E164=0;END_USER_IMSI=1;END_USER_SIP_URI=2;END_USER_NAI=3;END_USER_PRIVATE=4",
            "rfc8506|User-Equipment-Info-Type|459|IMEISV=0;MAC=1;EUI64=2;MODIFIED_EUI64=3",
            "rfc8506||268|DIAMETER_END_USER_SERVICE_DENIED=4010;DIAMETER_CREDIT_CONTROL_NOT_APPLICABLE=4011;DIAMETER_CREDIT_LIMIT_REACHED=4012;DIAMETER_USER_UNKNOWN=5030;DIAMETER_RATING_FAILED=5031",
            "rfc8581||622|OC_PEER_REPORT=16",
            "rfc8581|OC-Report-Type|626|PEER_REPORT=2",
            "rfc8582||622|OLR_RATE_ALGORITHM=4",
            "rfc8583|Load-Type|651|HOST=0;PEER=1"
        };
        DiameterDictionarySnapshot snapshot = CreateSnapshot();
        int expectedCount = 0;
        int index = 0;
        while (index < table.Length)
        {
            string[] values = table[index].Split('|');
            string[] expectedValues = values[3].Split(';');
            expectedCount = expectedCount + expectedValues.Length;
            DiameterDictionaryModule module = FindModule(snapshot, values[0]);
            List<DiameterEnumValueDefinition> domain = FindEnumDomain(
                module,
                values[1],
                uint.Parse(values[2]));
            Assert.That(domain, Has.Count.EqualTo(expectedValues.Length), table[index]);
            int valueIndex = 0;
            while (valueIndex < expectedValues.Length)
            {
                string[] expected = expectedValues[valueIndex].Split('=');
                Assert.Multiple(() =>
                {
                    Assert.That(domain[valueIndex].Name,
                        Is.EqualTo(expected[0]), table[index]);
                    Assert.That(domain[valueIndex].Value,
                        Is.EqualTo(int.Parse(expected[1])), table[index]);
                    Assert.That(domain[valueIndex].TypeVendorId, Is.Zero, table[index]);
                    Assert.That(domain[valueIndex].AvpVendorId, Is.Zero, table[index]);
                });
                valueIndex = valueIndex + 1;
            }

            index = index + 1;
        }

        Assert.That(CountEnumValues(snapshot), Is.EqualTo(expectedCount));
    }

    [Test]
    public void EveryBuiltInAvpMatchesTheModuleFlagPolicyTable()
    {
        DiameterDictionarySnapshot snapshot = CreateSnapshot();
        AssertBaseFlagPolicy(FindModule(snapshot, "rfc6733"));
        AssertCreditControlFlagPolicy(FindModule(snapshot, "rfc8506"));
        AssertExtensionFlagPolicy(FindModule(snapshot, "rfc6737"), 0);
        AssertExtensionFlagPolicy(FindModule(snapshot, "rfc7075"), 1);
        AssertExtensionFlagPolicy(FindModule(snapshot, "rfc7683"), 7);
        AssertExtensionFlagPolicy(FindModule(snapshot, "rfc7944"), 1);
        AssertExtensionFlagPolicy(FindModule(snapshot, "rfc8581"), 2);
        AssertExtensionFlagPolicy(FindModule(snapshot, "rfc8582"), 1);
        AssertExtensionFlagPolicy(FindModule(snapshot, "rfc8583"), 3);

        int avpCount = 0;
        int moduleIndex = 0;
        while (moduleIndex < snapshot.Modules.Count)
        {
            avpCount = avpCount + snapshot.Modules[moduleIndex].Avps.Count;
            moduleIndex = moduleIndex + 1;
        }

        Assert.That(avpCount, Is.EqualTo(134));
    }

    private static void AssertBaseFlagPolicy(DiameterDictionaryModule module)
    {
        uint[] optional = new uint[] { 267U, 269U, 281U, 294U };
        Assert.That(module.Avps, Has.Count.EqualTo(49));
        int index = 0;
        while (index < module.Avps.Count)
        {
            DiameterAvpDefinition avp = module.Avps[index];
            if (ContainsCode(optional, avp.Code))
            {
                Assert.That(avp.RequiredFlags, Is.EqualTo(DiameterAvpFlags.None), avp.Name);
                Assert.That(avp.ProhibitedFlags, Is.EqualTo(
                    DiameterAvpFlags.VendorSpecific | DiameterAvpFlags.Mandatory), avp.Name);
            }
            else
            {
                Assert.That(avp.RequiredFlags,
                    Is.EqualTo(DiameterAvpFlags.Mandatory), avp.Name);
                Assert.That(avp.ProhibitedFlags,
                    Is.EqualTo(DiameterAvpFlags.VendorSpecific), avp.Name);
            }

            index = index + 1;
        }
    }

    private static void AssertCreditControlFlagPolicy(DiameterDictionaryModule module)
    {
        uint[] optional = new uint[]
        {
            411U, 440U, 441U, 442U, 458U, 459U, 460U,
            653U, 654U, 655U, 656U, 657U, 658U, 659U,
            660U, 661U, 662U, 663U, 664U, 665U, 666U, 667U, 668U, 669U
        };
        Assert.That(module.Avps, Has.Count.EqualTo(70));
        int index = 0;
        while (index < module.Avps.Count)
        {
            DiameterAvpDefinition avp = module.Avps[index];
            DiameterAvpFlags required = DiameterAvpFlags.Mandatory;
            if (ContainsCode(optional, avp.Code))
            {
                required = DiameterAvpFlags.None;
            }

            Assert.That(avp.RequiredFlags, Is.EqualTo(required), avp.Name);
            Assert.That(avp.ProhibitedFlags,
                Is.EqualTo(DiameterAvpFlags.VendorSpecific), avp.Name);
            index = index + 1;
        }
    }

    private static void AssertExtensionFlagPolicy(
        DiameterDictionaryModule module,
        int expectedCount)
    {
        Assert.That(module.Avps, Has.Count.EqualTo(expectedCount), module.Name);
        int index = 0;
        while (index < module.Avps.Count)
        {
            DiameterAvpDefinition avp = module.Avps[index];
            Assert.That(avp.RequiredFlags, Is.EqualTo(DiameterAvpFlags.None), avp.Name);
            Assert.That(avp.ProhibitedFlags,
                Is.EqualTo(DiameterAvpFlags.VendorSpecific), avp.Name);
            index = index + 1;
        }
    }

    private static bool ContainsCode(uint[] values, uint code)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == code)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    private static DiameterDictionarySnapshot CreateSnapshot()
    {
        return new DiameterDictionary().ReadDictionarySnapshot();
    }

    private static DiameterDictionaryModule FindModule(
        DiameterDictionarySnapshot snapshot,
        string name)
    {
        int index = 0;
        while (index < snapshot.Modules.Count)
        {
            if (snapshot.Modules[index].Name == name)
            {
                return snapshot.Modules[index];
            }

            index = index + 1;
        }

        Assert.Fail("Expected module " + name + ".");
        return new DiameterDictionaryModule();
    }

    private static DiameterCommandDefinition FindCommand(
        DiameterDictionaryModule module,
        uint applicationId,
        uint code,
        bool request)
    {
        int index = 0;
        while (index < module.Commands.Count)
        {
            DiameterCommandDefinition command = module.Commands[index];
            if (command.ApplicationId == applicationId && command.Code == code &&
                command.IsRequest == request)
            {
                return command;
            }

            index = index + 1;
        }

        Assert.Fail("Expected command " + code + " in " + module.Name + ".");
        return new DiameterCommandDefinition();
    }

    private static DiameterAvpDefinition FindAvp(
        DiameterDictionaryModule module,
        uint code)
    {
        int index = 0;
        while (index < module.Avps.Count)
        {
            if (module.Avps[index].Code == code)
            {
                return module.Avps[index];
            }

            index = index + 1;
        }

        Assert.Fail("Expected AVP " + code + " in " + module.Name + ".");
        return new DiameterAvpDefinition();
    }

    private static DiameterTypeDefinition FindType(
        DiameterDictionaryModule module,
        string name)
    {
        int index = 0;
        while (index < module.Types.Count)
        {
            if (module.Types[index].Name == name)
            {
                return module.Types[index];
            }

            index = index + 1;
        }

        Assert.Fail("Expected type " + name + " in " + module.Name + ".");
        return new DiameterTypeDefinition();
    }

    private static List<DiameterEnumValueDefinition> FindEnumDomain(
        DiameterDictionaryModule module,
        string typeName,
        uint avpCode)
    {
        List<DiameterEnumValueDefinition> result =
            new List<DiameterEnumValueDefinition>();
        int index = 0;
        while (index < module.EnumValues.Count)
        {
            DiameterEnumValueDefinition value = module.EnumValues[index];
            if (value.TypeName == typeName && value.AvpCode == avpCode)
            {
                result.Add(value);
            }

            index = index + 1;
        }

        return result;
    }

    private static int CountGroupedAvps(DiameterDictionarySnapshot snapshot)
    {
        int count = 0;
        int moduleIndex = 0;
        while (moduleIndex < snapshot.Modules.Count)
        {
            DiameterDictionaryModule module = snapshot.Modules[moduleIndex];
            int index = 0;
            while (index < module.Avps.Count)
            {
                if (module.Avps[index].GroupedRules.Count > 0)
                {
                    count = count + 1;
                }

                index = index + 1;
            }

            moduleIndex = moduleIndex + 1;
        }

        return count;
    }

    private static int CountEnumValues(DiameterDictionarySnapshot snapshot)
    {
        int count = 0;
        int index = 0;
        while (index < snapshot.Modules.Count)
        {
            count = count + snapshot.Modules[index].EnumValues.Count;
            index = index + 1;
        }

        return count;
    }
}
