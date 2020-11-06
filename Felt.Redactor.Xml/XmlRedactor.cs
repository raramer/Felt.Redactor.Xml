using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Felt.Redactor.Xml
{
    public sealed class XmlRedactor : RedactorBase, IRedact
    {
        private static readonly string xsiUri = "http://www.w3.org/2001/XMLSchema-instance";
        private readonly SaveOptions _saveOptions = SaveOptions.DisableFormatting | SaveOptions.OmitDuplicateNamespaces;

        public XmlRedactor() : base(new RedactorOptions())
        {
        }

        public XmlRedactor(params string[] redacts) : base(new RedactorOptions { Redacts = redacts })
        {
        }

        public XmlRedactor(params IfIsRedact[] ifIsRedacts) : base(new RedactorOptions { IfIsRedacts = ifIsRedacts })
        {
        }

        public XmlRedactor(string[] redacts, IfIsRedact[] ifIsRedacts) : base(new RedactorOptions { Redacts = redacts, IfIsRedacts = ifIsRedacts })
        {
        }

        public XmlRedactor(RedactorOptions options) : base(options)
        {
        }

        public XmlRedactor(XmlRedactorOptions options) : base(options)
        {
            switch (options.Formatting)
            {
                case XmlRedactorFormatting.Compressed:
                    _saveOptions = SaveOptions.DisableFormatting | SaveOptions.OmitDuplicateNamespaces;
                    break;

                case XmlRedactorFormatting.Indented:
                    _saveOptions = SaveOptions.None | SaveOptions.OmitDuplicateNamespaces;
                    break;

                default:
                    throw new InvalidOperationException("Invalid " + nameof(XmlRedactorFormatting));
            }
        }

        public override string Redact(string xml)
        {
            return TryRedact(xml, out var redactedXml, out _) ? redactedXml
                : _options.OnErrorRedact == OnErrorRedact.None ? xml
                : _options.Mask;
        }

        public override bool TryRedact(string xml, out string redactedXml)
        {
            return TryRedact(xml, out redactedXml, out _);
        }

        public override bool TryRedact(string xml, out string redactedXml, out string errorMessage)
        {
            try
            {
                var xDocument = XDocument.Parse(xml);
                RedactXElement(xDocument.Root, false);
                redactedXml = xDocument.ToString(_saveOptions);
                errorMessage = null;
                return true;
            }
            catch (Exception ex)
            {
                redactedXml = null;
                errorMessage = ex.Message;
                return false;
            }
        }

        protected override string SanitizeMask(string mask)
        {
            return mask;
        }

        private XNamespace GetOrAddXsi(XElement xElement)
        {
            // get existing xsi if defined
            var xsiAttribute = xElement.Attributes().FirstOrDefault(xAttribute => xAttribute.IsNamespaceDeclaration
                                                                     && xAttribute.Value.Equals(xsiUri, StringComparison.OrdinalIgnoreCase));

            // if defined return localname
            if (xsiAttribute != null)
                return xsiUri;

            // if not root, get or add xsi to parent
            if (xElement != xElement.Document.Root)
                return GetOrAddXsi(xElement.Parent);

            // add xsi to parent
            xsiAttribute = new XAttribute(XNamespace.Xmlns + "xsi", xsiUri);
            xElement.Add(xsiAttribute);

            // return null
            return xsiUri;
        }

        private void RedactXElement(XElement xElement, bool redacting)
        {
            redacting = redacting || _options.Redacts.Any(r => r.Equals(xElement.Name.LocalName, _options.StringComparison));

            if (xElement.Attributes().Any())
            {
                var xAttributes = xElement.Attributes().ToList();
                foreach (var xAttribute in xAttributes)
                {
                    var attributeName = xElement.Name.LocalName + "@" + xAttribute.Name.LocalName;

                    if (_options.Redacts.Any(r => r.Equals(attributeName, _options.StringComparison))
                            || _options.IfIsRedacts.Any(iir => iir.Redact.Equals(attributeName, _options.StringComparison)
                                                            && xAttributes.Any(a => iir.If.Equals(xElement.Name.LocalName + "@" + a.Name.LocalName, _options.StringComparison)
                                                                                 && iir.Is.Equals(a.Value, _options.StringComparison))))
                    {
                        xAttribute.SetValue(_options.Mask ?? string.Empty);
                    }

                    redacting = redacting
                        || _options.IfIsRedacts.Any(iir => iir.Redact.Equals(xElement.Name.LocalName, _options.StringComparison)
                                                        && iir.If.Equals(attributeName, _options.StringComparison)
                                                        && iir.Is.Equals(xAttribute.Value, _options.StringComparison));
                }
            }

            if (redacting)
            {
                if (xElement.Elements().Any() && _options.ComplexTypeHandling == ComplexTypeHandling.RedactDescendants)
                {
                    RedactXElements(xElement.Elements().ToList(), redacting);
                }
                else
                {
                    if (_options.Mask == null)
                    {
                        var xsi = GetOrAddXsi(xElement);
                        xElement.ReplaceWith(new XElement(xElement.Name, new XAttribute(xsi + "nil", true)));
                        xElement.Value = string.Empty;
                    }
                    else
                    {
                        xElement.Value = _options.Mask;
                    }
                }
            }
            else if (xElement.Elements().Any())
            {
                RedactXElements(xElement.Elements().ToList(), redacting);
            }
        }

        private void RedactXElements(List<XElement> xElements, bool redacting)
        {
            foreach (var xElement in xElements)
            {
                var redactXElement = redacting // redacting parent
                    || _options.Redacts.Any(r => r.Equals(xElement.Name.LocalName, _options.StringComparison)) // redact by name
                    || _options.IfIsRedacts.Any(iir => iir.Redact.Equals(xElement.Name.LocalName, _options.StringComparison) // redact if other property has a specific value
                                               && xElements.Any(e => iir.If.Equals(e.Name.LocalName, _options.StringComparison)
                                                                       && iir.Is.Equals(e.Value, _options.StringComparison)));

                RedactXElement(xElement, redactXElement);
            }
        }
    }
}