using Newtonsoft.Json;
using System;
using System.Xml.Linq;
using Xunit;

namespace Felt.Redactor.Xml.Tests
{
    public class XmlRedactorRedactTests
    {
        public const string BasicXmlExample =
        @"<root>
            <a>1</a>
            <b c=""3"">2</b>
        </root>";

        /// <summary>
        /// a: string
        /// b: number
        /// c: boolean
        /// d: element with sub elements (including mixed content)
        /// e: element with attributes
        /// f: single-quoted attribute
        /// g: double-quoted attribute
        /// h: namespaced element
        /// i: namespaced attribute
        /// j: repeated element
        /// k: self-closing element
        /// l: null element
        /// m: element defined in comment
        /// n: element with CDATA
        /// o: element defined in CDATA
        /// </summary>
        public const string ComplexXmlExample =
        @"<?xml version=""1.0"" encoding=""UTF-8""?>
        <root xmlns:ns=""https://xml.org/ns"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
            <a>A</a>
            <b>1&amp;one</b>
            <c>true</c>
            <d>
                MIXED CONTENT
                <e f='&apos;7&apos;' g=""&quot;8&quot;"">5</e>
                <ns:h ns:i=""IX"">
                    <ns:j>&lt;ten&gt;</ns:j>
                    <ns:j>X</ns:j>
                </ns:h>
            </d>
            <k />
            <l xsi:nil=""true"" />
            <!-- this is a comment <m>thirteen</m> -->
            <n><![CDATA[<o>XV</o>]]></n>
        </root>";

        [Theory]
        [InlineData(ComplexTypeHandling.RedactValue, "<root xmlns:ns=\"https://xml.org/ns\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"><a>A</a><b>1&amp;one</b><c>true</c><d>[REDACTED]</d><k /><l xsi:nil=\"true\" /><!-- this is a comment <m>thirteen</m> --><n><![CDATA[<o>XV</o>]]></n></root>")]
        [InlineData(ComplexTypeHandling.RedactDescendants, "<root xmlns:ns=\"https://xml.org/ns\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"><a>A</a><b>1&amp;one</b><c>true</c><d>\r\n                MIXED CONTENT\r\n                <e f=\"'7'\" g=\"&quot;8&quot;\">[REDACTED]</e><ns:h ns:i=\"IX\"><ns:j>[REDACTED]</ns:j><ns:j>[REDACTED]</ns:j></ns:h></d><k /><l xsi:nil=\"true\" /><!-- this is a comment <m>thirteen</m> --><n><![CDATA[<o>XV</o>]]></n></root>")]
        public void ComplexTypeHandlingIs(ComplexTypeHandling complexTypeHandling, string expectedResult)
        {
            var redactor = new XmlRedactor(new XmlRedactorOptions
            {
                ComplexTypeHandling = complexTypeHandling,
                Redacts = new[] { "d" }
            });

            var result = redactor.Redact(ComplexXmlExample);

            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [InlineData(XmlRedactorFormatting.Compressed, "<root><a>[REDACTED]</a><b c=\"3\">2</b></root>")]
        [InlineData(XmlRedactorFormatting.Indented, "<root>\r\n  <a>[REDACTED]</a>\r\n  <b c=\"3\">2</b>\r\n</root>")]
        public void FormattingIs(XmlRedactorFormatting formatting, string expectedResult)
        {
            var redactor = new XmlRedactor(new XmlRedactorOptions
            {
                Redacts = new[] { "a" },
                Formatting = formatting
            });

            var result = redactor.Redact(BasicXmlExample);

            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [InlineData(BasicXmlExample)]
        [InlineData(ComplexXmlExample)]
        public void IsExampleValidXml(string xml)
        {
            var redactor = new XmlRedactor();
            Assert.True(redactor.TryRedact(xml, out _));
        }

        [Theory]
        [InlineData("Null", null, "<root xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"><a xsi:nil=\"true\" /><b c=\"\">2</b></root>")]
        [InlineData("Empty", "", "<root><a></a><b c=\"\">2</b></root>")]
        [InlineData("WhiteSpace", " ", "<root><a> </a><b c=\" \">2</b></root>")]
        [InlineData("String", "StRiNg", "<root><a>StRiNg</a><b c=\"StRiNg\">2</b></root>")]
        [InlineData("Asterisks", "********", "<root><a>********</a><b c=\"********\">2</b></root>")]
        [InlineData("Contains <", "X<Y", "<root><a>X&lt;Y</a><b c=\"X&lt;Y\">2</b></root>")]
        [InlineData("Contains >", "X>Y", "<root><a>X&gt;Y</a><b c=\"X&gt;Y\">2</b></root>")]
        [InlineData("Contains \"", "X\"Y", "<root><a>X\"Y</a><b c=\"X&quot;Y\">2</b></root>")]
        [InlineData("Contains '", "X'Y", "<root><a>X'Y</a><b c=\"X'Y\">2</b></root>")]
        [InlineData("Contains &", "X&Y", "<root><a>X&amp;Y</a><b c=\"X&amp;Y\">2</b></root>")]
        public void MaskIs(string description, string mask, string expectedResult)
        {
            var redactor = new XmlRedactor(new XmlRedactorOptions
            {
                Redacts = new[] { "a", "b@c" },
                Mask = mask
            });

            var result = redactor.Redact(BasicXmlExample);

            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [InlineData("Null", null)]
        [InlineData("Empty", "")]
        [InlineData("WhiteSpace", " ")]
        [InlineData("String", "abc")]
        [InlineData("Number", "123")]
        [InlineData("Boolean", "True")]
        [InlineData("Json", @"{ ""abc"": 1 }")]
        [InlineData("Non-xml compliant html", "<html><br></html>")]
        [InlineData("Invalid xml element name", "<xml><1></1></xml>")]
        public void OnErrorRedactIsAll(string description, string xml)
        {
            var redactor = new XmlRedactor(new RedactorOptions
            {
                OnErrorRedact = OnErrorRedact.All
            });

            var result = redactor.Redact(xml);

            Assert.Equal(RedactorOptions.DefaultMask, result);
        }

        [Theory]
        [InlineData("Null", null)]
        [InlineData("Empty", "")]
        [InlineData("WhiteSpace", " ")]
        [InlineData("String", "abc")]
        [InlineData("Number", "123")]
        [InlineData("Boolean", "True")]
        [InlineData("Json", @"{ ""abc"": 1 }")]
        [InlineData("Non-xml compliant html", "<html><br></html>")]
        [InlineData("Invalid xml element name", "<xml><1></1></xml>")]
        public void OnErrorRedactIsNone(string description, string xml)
        {
            var redactor = new XmlRedactor(new RedactorOptions
            {
                OnErrorRedact = OnErrorRedact.None
            });

            var result = redactor.Redact(xml);

            Assert.Equal(xml, result);
        }

        [Fact]
        public void RedactSampleData()
        {
            var json = $"{{root:{JsonConvert.SerializeObject(SampleData.UserBillingHistory)}}}";
            var xml = JsonConvert.DeserializeXNode(json).ToString(SaveOptions.DisableFormatting);

            var redacts = new[]
            {
                "password",
                "passwordHistory",
                "socialSecurityNumber",
            };
            var ifIsRedacts = new[]
            {
                new IfIsRedact { If = "type", Is = "check", Redact = "checkNumber" },
                new IfIsRedact { If = "type", Is = "creditCard", Redact = "creditCardData" },
            };
            var expectedValueRedactions = new[]
            {
                // using "" for string
                @"P@ssw0rd5", // password
                @"P@ssw0rd1", @"P@ssw0rd2", @"P@ssw0rd3", // passwordHistory
                @"1234567890", // socialSecurityNumber
                @"2468", // checkNumber
                @"Visa", @"4111111111111111", @"04/25", @"258", @"false", // creditCardData
            };

            var redactor = new XmlRedactor(new RedactorOptions
            {
                ComplexTypeHandling = ComplexTypeHandling.RedactDescendants,
                Redacts = redacts,
                IfIsRedacts = ifIsRedacts
            });

            var result = redactor.Redact(xml);

            var xmlMask = RedactorOptions.DefaultMask;
            var expectedResult = xml;
            foreach (var expectedValueRedaction in expectedValueRedactions)
                expectedResult = expectedResult.Replace(expectedValueRedaction, xmlMask);

            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [InlineData("Self-closing", "<a />", "<a>[REDACTED]</a>")]
        [InlineData("Single Element", "<a></a>", "<a>[REDACTED]</a>")]
        [InlineData("Nested Elements", "<root><a></a></root>", "<root><a>[REDACTED]</a></root>")]
        [InlineData("Repeated Elements", "<root><a></a><a></a></root>", "<root><a>[REDACTED]</a><a>[REDACTED]</a></root>")]
        [InlineData("Contains Attributes", "<root><a b=\"1\" c=\"2\"></a></root>", "<root><a b=\"[REDACTED]\" c=\"2\">[REDACTED]</a></root>")]
        [InlineData("Contains Namespaces", "<root xmlns:ns=\"http://xml.com/xyz\"><ns:a></ns:a></root>", "<root xmlns:ns=\"http://xml.com/xyz\"><ns:a>[REDACTED]</ns:a></root>")]
        public void StringComparisonIsOrdinal(string description, string xml, string _)
        {
            var redactor = new XmlRedactor(new RedactorOptions
            {
                Redacts = new[] { "A", "A@B" },
                StringComparison = StringComparison.Ordinal
            });

            var result = redactor.Redact(xml);

            Assert.Equal(xml, result);
        }

        [Theory]
        [InlineData("Self-closing", "<a />", "<a>[REDACTED]</a>")]
        [InlineData("Single Element", "<a></a>", "<a>[REDACTED]</a>")]
        [InlineData("Nested Elements", "<root><a></a></root>", "<root><a>[REDACTED]</a></root>")]
        [InlineData("Repeated Elements", "<root><a></a><a></a></root>", "<root><a>[REDACTED]</a><a>[REDACTED]</a></root>")]
        [InlineData("Contains Attributes", "<root><a b=\"1\" c=\"2\"></a></root>", "<root><a b=\"[REDACTED]\" c=\"2\">[REDACTED]</a></root>")]
        [InlineData("Contains Namespaces", "<root xmlns:ns=\"http://xml.com/xyz\"><ns:a></ns:a></root>", "<root xmlns:ns=\"http://xml.com/xyz\"><ns:a>[REDACTED]</ns:a></root>")]
        public void StringComparisonIsOrdinalIgnoreCase(string description, string xml, string expectedResult)
        {
            var redactor = new XmlRedactor(new RedactorOptions
            {
                Redacts = new[] { "A", "A@B" },
                StringComparison = StringComparison.OrdinalIgnoreCase
            });

            var result = redactor.Redact(xml);

            Assert.Equal(expectedResult, result);
        }
    }
}