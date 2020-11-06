namespace Felt.Redactor.Xml
{
    public sealed class XmlRedactorOptions : RedactorOptions
    {
        public XmlRedactorFormatting Formatting { get; set; } = XmlRedactorFormatting.Compressed;
    }
}