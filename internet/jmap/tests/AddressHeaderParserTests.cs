using System.Text.Json;

namespace jmap.Tests;

public class AddressHeaderParserTests
{
    [Test]
    public void ParsesRfcExampleAsFlatAndGroupedAddresses()
    {
        AddressHeaderParser parser = new AddressHeaderParser();
        string value = " \"  James Smythe\" <james@example.com>, Friends:\r\n" +
            " jane@example.com, =?UTF-8?Q?John_Sm=C3=AEth?= <john@example.com>;";

        List<EmailAddress> flat = parser.ParseAddresses(value);
        List<EmailAddressGroup> grouped = parser.ParseGroupedAddresses(value);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(flat.Select(address => address.Name),
                Is.EqualTo(new string?[] { "James Smythe", null, "John Smîth" }));
            Assert.That(flat.Select(address => address.Email),
                Is.EqualTo(new[] { "james@example.com", "jane@example.com", "john@example.com" }));
            Assert.That(grouped, Has.Count.EqualTo(2));
            Assert.That(grouped[0].Name, Is.Null);
            Assert.That(grouped[0].Addresses, Has.Count.EqualTo(1));
            Assert.That(grouped[1].Name, Is.EqualTo("Friends"));
            Assert.That(grouped[1].Addresses.Select(address => address.Email),
                Is.EqualTo(new[] { "jane@example.com", "john@example.com" }));
        }
    }

    [Test]
    public void ParsesQuotedPairsCommentsAndInternationalizedAddresses()
    {
        AddressHeaderParser parser = new AddressHeaderParser();
        string value = "\"Doe\\, Jane\" <δοκιμή@παράδειγμα.δοκιμή>, " +
            "user@example.com (Support Desk), user(note)@[IPv6:2001:db8::1], " +
            "\"comma,local\"@example.com";

        List<EmailAddress> addresses = parser.ParseAddresses(value);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(addresses, Has.Count.EqualTo(4));
            Assert.That(addresses[0].Name, Is.EqualTo("Doe, Jane"));
            Assert.That(addresses[0].Email, Is.EqualTo("δοκιμή@παράδειγμα.δοκιμή"));
            Assert.That(addresses[1].Name, Is.EqualTo("Support Desk"));
            Assert.That(addresses[1].Email, Is.EqualTo("user@example.com"));
            Assert.That(addresses[2].Name, Is.Null);
            Assert.That(addresses[2].Email, Is.EqualTo("user@[IPv6:2001:db8::1]"));
            Assert.That(addresses[3].Email, Is.EqualTo("\"comma,local\"@example.com"));
        }
    }

    [Test]
    public void RetainsBestEffortInvalidAddressesAndIncompleteGroups()
    {
        AddressHeaderParser parser = new AddressHeaderParser();
        string value = "broken-address, Empty:;, Draft: <unfinished@example.test";

        List<EmailAddressGroup> groups = parser.ParseGroupedAddresses(value);
        List<EmailAddress> addresses = parser.ParseAddresses(value);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(groups, Has.Count.EqualTo(3));
            Assert.That(groups[0].Name, Is.Null);
            Assert.That(groups[0].Addresses.Single().Email, Is.EqualTo("broken-address"));
            Assert.That(groups[1].Name, Is.EqualTo("Empty"));
            Assert.That(groups[1].Addresses, Is.Empty);
            Assert.That(groups[2].Name, Is.EqualTo("Draft"));
            Assert.That(groups[2].Addresses.Single().Email, Is.EqualTo("unfinished@example.test"));
            Assert.That(addresses.Select(address => address.Email),
                Is.EqualTo(new[] { "broken-address", "unfinished@example.test" }));
        }
    }

    [Test]
    public void SelectorEncodesLastAndAllRepeatedAddressHeaders()
    {
        HeaderPropertySelector selector = new HeaderPropertySelector();
        List<EmailHeader> headers = new List<EmailHeader>
        {
            new EmailHeader { Name = "From", Value = "First <first@example.com>" },
            new EmailHeader { Name = "from", Value = "Team: second@example.com;" }
        };

        JsonElement last = selector.SelectProperty(headers, "header:FROM:asAddresses");
        JsonElement all = selector.SelectProperty(headers, "header:From:asGroupedAddresses:all");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(last.GetArrayLength(), Is.EqualTo(1));
            Assert.That(last[0].GetProperty("name").ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(last[0].GetProperty("email").GetString(), Is.EqualTo("second@example.com"));
            Assert.That(all.GetArrayLength(), Is.EqualTo(2));
            Assert.That(all[0][0].GetProperty("name").ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(all[0][0].GetProperty("addresses")[0].GetProperty("name").GetString(), Is.EqualTo("First"));
            Assert.That(all[1][0].GetProperty("name").GetString(), Is.EqualTo("Team"));
        }
    }

    [Test]
    public void SelectorAllowsEverySpecifiedAddressHeaderAndCustomHeaders()
    {
        HeaderPropertySelector selector = new HeaderPropertySelector();
        string[] names =
        {
            "From", "Sender", "Reply-To", "To", "Cc", "Bcc", "Resent-From", "Resent-Sender",
            "Resent-Reply-To", "Resent-To", "Resent-Cc", "Resent-Bcc", "X-Participants"
        };

        foreach (string name in names)
        {
            List<EmailHeader> headers = new List<EmailHeader>
            {
                new EmailHeader { Name = name, Value = "person@example.com" }
            };
            JsonElement addresses = selector.SelectProperty(headers, "header:" + name + ":asAddresses");
            JsonElement groups = selector.SelectProperty(headers, "header:" + name + ":asGroupedAddresses");
            Assert.That(addresses[0].GetProperty("email").GetString(), Is.EqualTo("person@example.com"), name);
            Assert.That(groups[0].GetProperty("addresses")[0].GetProperty("email").GetString(),
                Is.EqualTo("person@example.com"), name);
        }

        Assert.That(() => selector.SelectProperty(new List<EmailHeader>(), "header:Subject:asAddresses"),
            Throws.TypeOf<JmapValidationException>());
    }
}
