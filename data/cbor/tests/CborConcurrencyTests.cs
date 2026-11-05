using System.Collections.Concurrent;
using System.Numerics;
using cbor;

namespace cbor.Tests;

[TestFixture]
public class CborConcurrencyTests
{
    [Test]
    public void CompiledSchemaSupportsConcurrentValidation()
    {
        CddlSchema schema = Compile(
            "root = node\n"
            + "node = uint / [* node]");
        CddlValidator validator = new CddlValidator();
        ConcurrentQueue<string> failures = new ConcurrentQueue<string>();

        Parallel.For(0, 256, index =>
        {
            bool expected = index % 7 != 0;
            CborValue value = expected
                ? Array(Integer(index), Array(Integer(index + 1)))
                : Text("invalid-" + index);
            CddlValidationResult result = validator.ValidateCbor(
                schema,
                value,
                new CddlValidationOptions());
            if (result.IsValid != expected)
            {
                failures.Enqueue("validation " + index
                    + " expected " + expected
                    + " but was " + result.IsValid);
            }
        });

        Assert.That(failures, Is.Empty,
            string.Join(System.Environment.NewLine, failures));
    }

    [Test]
    public void ValidationFreezesOptionsAndCompiledControlRegistrations()
    {
        BlockingControlHandler handler = new BlockingControlHandler();
        CddlControlRegistration registration = new CddlControlRegistration
        {
            Name = ".concurrent",
            Handler = handler
        };
        CddlCompileOptions compileOptions = new CddlCompileOptions();
        compileOptions.ControlRegistrations.Add(registration);
        CddlSchema schema = Compile(
            "root = uint .concurrent 1",
            compileOptions);
        registration.Name = ".mutated";
        registration.IsEnabled = false;
        compileOptions.ControlRegistrations.Clear();

        CddlValidationOptions options = new CddlValidationOptions
        {
            EnabledFeatures = ["original"],
            MaxSteps = 1000
        };
        Task<CddlValidationResult> operation = Task.Run(() =>
            new CddlValidator().ValidateCbor(schema, Integer(1), options));
        bool entered = handler.Entered.Wait(TimeSpan.FromSeconds(5));
        if (!entered)
        {
            handler.Release.Set();
            Assert.Fail("The custom control callback was not reached.");
        }

        options.RootRuleName = "missing";
        options.EnabledFeatures[0] = "mutated";
        options.MaxSteps = 0;
        handler.Release.Set();
        CddlValidationResult result = operation.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(handler.Calls, Is.EqualTo(1));
            Assert.That(handler.ObservedRootRuleName, Is.Null);
            Assert.That(handler.ObservedFeature, Is.EqualTo("original"));
            Assert.That(handler.ObservedMaxSteps, Is.EqualTo(1000));
            Assert.That(result.Options.RootRuleName, Is.Null);
            Assert.That(result.Options.EnabledFeatures,
                Is.EqualTo(new[] { "original" }));
            Assert.That(result.Options.MaxSteps, Is.EqualTo(1000));
            Assert.That(schema.ControlRegistrations, Has.Length.EqualTo(1));
            Assert.That(schema.ControlRegistrations[0].Name,
                Is.EqualTo(".concurrent"));
            Assert.That(schema.ControlRegistrations[0].IsEnabled, Is.True);
        });
    }

    [Test]
    public void DecodeFreezesTagRegistrationsBeforeConcurrentCallbacks()
    {
        BlockingTagHandler handler = new BlockingTagHandler();
        RejectingTagHandler replacement = new RejectingTagHandler();
        CborTagRegistration first = new CborTagRegistration
        {
            TagNumber = 100,
            Handler = handler
        };
        CborTagRegistration second = new CborTagRegistration
        {
            TagNumber = 101,
            Handler = handler
        };
        CborDecodeOptions options = new CborDecodeOptions
        {
            TagPolicy = CborTagPolicy.RejectUnknown,
            TagRegistrations = [first, second]
        };
        Task<CborReadResult> operation = Task.Run(() =>
            new CborDecoder().Decode(
                Convert.FromHexString("82D86400D86501"),
                options));
        bool entered = handler.Entered.Wait(TimeSpan.FromSeconds(5));
        if (!entered)
        {
            handler.Release.Set();
            Assert.Fail("The semantic tag callback was not reached.");
        }

        first.Handler = replacement;
        second.Handler = replacement;
        first.ValidateContent = false;
        second.ValidateContent = false;
        options.TagRegistrations.Clear();
        handler.Release.Set();
        CborReadResult result = operation.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(CborReadStatus.Done),
                result.ErrorMessage);
            Assert.That(handler.Calls, Is.EqualTo(2));
            Assert.That(replacement.Calls, Is.Zero);
        });
    }

    [Test]
    public void SharedHighLevelEncoderAndDecoderHandleConcurrentCalls()
    {
        CborEncoder encoder = new CborEncoder();
        CborDecoder decoder = new CborDecoder();
        ConcurrentQueue<string> failures = new ConcurrentQueue<string>();
        ParallelOptions parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = 8
        };

        Parallel.For(0, 512, parallelOptions, index =>
        {
            try
            {
                CborValue value = Array(
                    Integer(index),
                    Text("item-" + index));
                byte[] encoded = encoder.Encode(
                    value,
                    new CborEncodeOptions());
                CborReadResult decoded = decoder.Decode(
                    encoded,
                    new CborDecodeOptions());
                if (decoded.Status != CborReadStatus.Done
                    || decoded.Value == null
                    || decoded.Value.ArrayItems == null
                    || decoded.Value.ArrayItems.Count != 2
                    || decoded.Value.ArrayItems[0].IntegerValue
                        != new BigInteger(index)
                    || decoded.Value.ArrayItems[1].TextStringValue
                        != "item-" + index)
                {
                    failures.Enqueue("codec operation " + index
                        + " returned " + decoded.Status
                        + " / " + decoded.ErrorCode);
                }
            }
            catch (System.Exception exception)
            {
                failures.Enqueue("codec operation " + index
                    + " threw " + exception.GetType().Name
                    + ": " + exception.Message);
            }
        });

        Assert.That(failures, Is.Empty,
            string.Join(System.Environment.NewLine, failures));
    }

    [Test]
    public void IndependentTokenReadersKeepFragmentStateIsolated()
    {
        ConcurrentQueue<string> failures = new ConcurrentQueue<string>();

        Parallel.For(0, 256, index =>
        {
            uint expected = (uint)(65536 + index);
            byte[] encoded =
            [
                0x1a,
                (byte)(expected >> 24),
                (byte)(expected >> 16),
                (byte)(expected >> 8),
                (byte)expected
            ];
            CborTokenReader reader = new CborTokenReader(
                new CborDecodeOptions());
            CborTokenReadResult prefix = reader.ReadToken(
                encoded.AsSpan(0, 2).ToArray());
            Thread.Yield();
            CborTokenReadResult completed = reader.ReadToken(
                encoded.AsSpan(2).ToArray());
            if (prefix.Status != CborReadStatus.NeedMoreData
                || completed.Status != CborReadStatus.Done
                || completed.Token == null
                || completed.Token.IntegerValue != new BigInteger(expected))
            {
                failures.Enqueue("token reader " + index
                    + " returned " + prefix.Status
                    + " then " + completed.Status);
            }
        });

        Assert.That(failures, Is.Empty,
            string.Join(System.Environment.NewLine, failures));
    }

    [Test]
    public void RegistriesSupportConcurrentIndependentEntries()
    {
        CborTagRegistry tags = new CborTagRegistry();
        CddlControlRegistry controls = new CddlControlRegistry();
        PassiveControlHandler controlHandler = new PassiveControlHandler();
        ConcurrentQueue<string> failures = new ConcurrentQueue<string>();

        Parallel.For(0, 128, index =>
        {
            ulong tagNumber = (ulong)(1000 + index);
            string controlName = ".control" + index;
            tags.AddTag(new CborTagRegistration
            {
                TagNumber = tagNumber,
                Name = "tag-" + index
            });
            controls.AddControl(new CddlControlRegistration
            {
                Name = controlName,
                Handler = controlHandler
            });
            CborTagRegistration? tag = tags.FindTag(tagNumber);
            CddlControlRegistration? control = controls.FindControl(controlName);
            if (tag == null || tag.Name != "tag-" + index)
            {
                failures.Enqueue("tag registry entry " + index
                    + " was not isolated");
            }
            if (control == null || control.Name != controlName
                || control.Handler != controlHandler)
            {
                failures.Enqueue("control registry entry " + index
                    + " was not isolated");
            }
        });

        Assert.That(failures, Is.Empty,
            string.Join(System.Environment.NewLine, failures));
    }

    private static CddlSchema Compile(string source)
    {
        return Compile(source, new CddlCompileOptions());
    }

    private static CddlSchema Compile(
        string source,
        CddlCompileOptions options)
    {
        CddlDocument document = new CddlParser().ParseDocument(
            source,
            new CddlParseOptions { SourceName = "concurrency.cddl" });
        return new CddlCompiler().CompileSchema(document, options);
    }

    private static CborValue Integer(int value)
    {
        return new CborValue
        {
            Kind = CborValueKind.UnsignedInteger,
            IntegerValue = new BigInteger(value)
        };
    }

    private static CborValue Text(string value)
    {
        return new CborValue
        {
            Kind = CborValueKind.TextString,
            TextStringValue = value,
            LengthKind = CborLengthKind.Definite
        };
    }

    private static CborValue Array(params CborValue[] items)
    {
        return new CborValue
        {
            Kind = CborValueKind.Array,
            ArrayItems = new List<CborValue>(items),
            LengthKind = CborLengthKind.Definite
        };
    }

    private class BlockingControlHandler : ICddlControlHandler
    {
        public ManualResetEventSlim Entered = new ManualResetEventSlim(false);
        public ManualResetEventSlim Release = new ManualResetEventSlim(false);
        public int Calls;
        public string? ObservedRootRuleName;
        public string? ObservedFeature;
        public int ObservedMaxSteps;

        public CddlControlResult EvaluateControl(
            CddlNode target,
            CddlNode controller,
            CborValue value,
            CddlValidationOptions options)
        {
            Interlocked.Increment(ref Calls);
            Entered.Set();
            Release.Wait();
            ObservedRootRuleName = options.RootRuleName;
            ObservedFeature = options.EnabledFeatures[0];
            ObservedMaxSteps = options.MaxSteps;
            return new CddlControlResult
            {
                Status = CddlControlStatus.Applied
            };
        }

        public CddlControlResult GenerateControl(
            CddlNode target,
            CddlNode controller,
            CddlGenerationContext context)
        {
            return new CddlControlResult
            {
                Status = CddlControlStatus.NotApplicable
            };
        }
    }

    private class BlockingTagHandler : ICborTagHandler
    {
        public ManualResetEventSlim Entered = new ManualResetEventSlim(false);
        public ManualResetEventSlim Release = new ManualResetEventSlim(false);
        public int Calls;

        public CborTagResult ValidateTag(ulong tagNumber, CborValue content)
        {
            int call = Interlocked.Increment(ref Calls);
            if (call == 1)
            {
                Entered.Set();
                Release.Wait();
            }
            return ValidTagResult();
        }

        public CborTagResult DecodeTag(ulong tagNumber, CborValue content)
        {
            return ValidTagResult();
        }

        public CborTagResult EncodeTag(ulong tagNumber, object nativeValue)
        {
            return ValidTagResult();
        }
    }

    private class RejectingTagHandler : ICborTagHandler
    {
        public int Calls;

        public CborTagResult ValidateTag(ulong tagNumber, CborValue content)
        {
            Interlocked.Increment(ref Calls);
            return new CborTagResult
            {
                Handled = true,
                IsValid = false,
                ErrorCode = "CONCURRENT_REPLACEMENT_USED",
                ErrorMessage = "A mutated registration was observed."
            };
        }

        public CborTagResult DecodeTag(ulong tagNumber, CborValue content)
        {
            return ValidateTag(tagNumber, content);
        }

        public CborTagResult EncodeTag(ulong tagNumber, object nativeValue)
        {
            Interlocked.Increment(ref Calls);
            return new CborTagResult
            {
                Handled = true,
                IsValid = false
            };
        }
    }

    private class PassiveControlHandler : ICddlControlHandler
    {
        public CddlControlResult EvaluateControl(
            CddlNode target,
            CddlNode controller,
            CborValue value,
            CddlValidationOptions options)
        {
            return new CddlControlResult
            {
                Status = CddlControlStatus.NotApplicable
            };
        }

        public CddlControlResult GenerateControl(
            CddlNode target,
            CddlNode controller,
            CddlGenerationContext context)
        {
            return new CddlControlResult
            {
                Status = CddlControlStatus.NotApplicable
            };
        }
    }

    private static CborTagResult ValidTagResult()
    {
        return new CborTagResult
        {
            Handled = true,
            IsValid = true
        };
    }
}
