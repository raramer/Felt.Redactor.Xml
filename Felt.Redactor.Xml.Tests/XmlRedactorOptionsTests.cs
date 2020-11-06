using System;
using Xunit;

namespace Felt.Redactor.Xml.Tests
{
    public class XmlRedactorOptionsTests
    {
        [Fact]
        public void Defaults()
        {
            Assert.Equal("[REDACTED]", RedactorOptions.DefaultMask);

            var options = new XmlRedactorOptions();

            Assert.Equal(ComplexTypeHandling.RedactValue, options.ComplexTypeHandling);
            Assert.Equal(XmlRedactorFormatting.Compressed, options.Formatting);
            Assert.Null(options.IfIsRedacts);
            Assert.Equal(RedactorOptions.DefaultMask, options.Mask);
            Assert.Equal(OnErrorRedact.All, options.OnErrorRedact);
            Assert.Null(options.Redacts);
            Assert.Equal(StringComparison.OrdinalIgnoreCase, options.StringComparison);
        }
    }
}