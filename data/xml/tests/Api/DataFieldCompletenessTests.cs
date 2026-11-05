using System.Reflection;
using System.Text;
using xml;

namespace xml.Tests;

[TestFixture]
public class DataFieldCompletenessTests
{
    [Test]
    public void EveryPublicDataFieldRoundTripsThroughRuntimeStorage()
    {
        Type[] dataTypes = typeof(XmlInput).Assembly.GetTypes()
            .Where(type => type.Namespace == "xml")
            .Where(type => type.IsPublic && type.IsClass && !type.IsAbstract)
            .Where(type => !typeof(Exception).IsAssignableFrom(type))
            .Where(type => type.GetFields(BindingFlags.Public | BindingFlags.Instance).Length > 0)
            .OrderBy(type => type.Name)
            .ToArray();

        Assert.That(dataTypes, Is.Not.Empty);
        foreach (Type dataType in dataTypes)
        {
            object instance = Activator.CreateInstance(dataType)!;
            FieldInfo[] fields = dataType.GetFields(BindingFlags.Public | BindingFlags.Instance);
            Assert.That(fields, Is.Not.Empty, dataType.Name);
            foreach (FieldInfo field in fields)
            {
                object value = CreateFieldValue(field.FieldType);
                field.SetValue(instance, value);
                object? actual = field.GetValue(instance);
                if (field.FieldType.IsValueType)
                {
                    Assert.That(actual, Is.EqualTo(value), dataType.Name + "." + field.Name);
                }
                else
                {
                    Assert.That(actual, Is.SameAs(value), dataType.Name + "." + field.Name);
                }
            }
        }
    }

    private static object CreateFieldValue(Type type)
    {
        if (type == typeof(string))
        {
            return "round-trip";
        }

        if (type == typeof(bool))
        {
            return true;
        }

        if (type == typeof(byte))
        {
            return (byte)37;
        }

        if (type == typeof(char))
        {
            return 'x';
        }

        if (type == typeof(int))
        {
            return 37;
        }

        if (type == typeof(uint))
        {
            return 37U;
        }

        if (type == typeof(long))
        {
            return 37L;
        }

        if (type == typeof(decimal))
        {
            return 37.5M;
        }

        if (type == typeof(double))
        {
            return 37.5D;
        }

        if (type == typeof(DateTimeOffset))
        {
            return DateTimeOffset.UnixEpoch;
        }

        if (type == typeof(ReadOnlyMemory<byte>))
        {
            return new ReadOnlyMemory<byte>(new byte[] { 1, 2, 3 });
        }

        if (type == typeof(ReadOnlyMemory<char>))
        {
            return new ReadOnlyMemory<char>(new char[] { 'x', 'm', 'l' });
        }

        if (type == typeof(Stream))
        {
            return new MemoryStream(new byte[] { 1 });
        }

        if (type == typeof(TextReader))
        {
            return new StringReader("xml");
        }

        if (type == typeof(TextWriter))
        {
            return new StringWriter();
        }

        if (type == typeof(Encoding))
        {
            return Encoding.UTF8;
        }

        if (type == typeof(IXmlByteSink))
        {
            return new XmlByteCollector();
        }

        if (type == typeof(IXmlResourceResolver))
        {
            return new XmlDefaultResolver();
        }

        if (type == typeof(IXsltResultDocumentResolver))
        {
            return new XsltResultDocumentCollector();
        }

        if (type == typeof(IXsltMessageReceiver))
        {
            return new XsltMessageCollector();
        }

        if (type == typeof(IXsltTraceReceiver))
        {
            return new XsltTraceCollector();
        }

        if (type == typeof(IXPathFunction))
        {
            return new XPathFunctionAdapter();
        }

        if (type == typeof(IXPathVariableResolver))
        {
            return new XPathVariableResolver();
        }

        if (type == typeof(IXPathFunctionResolver))
        {
            return new XPathFunctionResolver();
        }

        if (type == typeof(IXPathCollectionResolver))
        {
            return new XPathCollectionResolver();
        }

        if (type == typeof(IXPathCollation))
        {
            return new XPathCodepointCollation();
        }

        if (type == typeof(IXPathTraceReceiver))
        {
            return new XPathTraceCollector();
        }

        if (type == typeof(IXmlCancellation))
        {
            return new XmlCancellationFlag();
        }

        if (type == typeof(IXmlSerializationMethod))
        {
            return new SerializationMethodStub();
        }

        if (type == typeof(IXsltExtensionInstruction))
        {
            return new XsltExtensionInstructionAdapter();
        }

        if (type.IsEnum)
        {
            Array values = Enum.GetValues(type);
            return values.GetValue(values.Length - 1)!;
        }

        if (type.IsArray)
        {
            return Array.CreateInstance(type.GetElementType()!, 1);
        }

        return Activator.CreateInstance(type)!;
    }

    private sealed class SerializationMethodStub : IXmlSerializationMethod
    {
        public void SerializeXmlSequence(XdmSequence sequence, XmlOutput output, XmlSerializationOptions options)
        {
        }
    }
}
