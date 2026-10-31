using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace De.Openelp.Feuerwehr.Server.Services.IncidentModules;

internal static class SvgSymbolValidator
{
    private static readonly HashSet<string> Elements = new("svg g path rect circle ellipse line polyline polygon text tspan defs clipPath mask linearGradient radialGradient stop use symbol title desc pattern marker style image".Split(' '), StringComparer.Ordinal);
    private static readonly HashSet<string> Attributes = new("id class viewBox width height x y x1 y1 x2 y2 dx dy cx cy r rx ry d points transform fill fill-opacity fill-rule stroke stroke-width stroke-opacity stroke-linecap stroke-linejoin stroke-miterlimit stroke-dasharray stroke-dashoffset opacity color clip-path clip-rule mask filter font-family font-size font-weight font-style text-anchor dominant-baseline alignment-baseline letter-spacing word-spacing gradientUnits gradientTransform spreadMethod offset stop-color stop-opacity patternUnits patternContentUnits patternTransform markerWidth markerHeight markerUnits refX refY orient preserveAspectRatio vector-effect href style".Split(' '), StringComparer.Ordinal);
    private static readonly HashSet<string> CssProperties = new("fill fill-opacity fill-rule stroke stroke-width stroke-opacity stroke-linecap stroke-linejoin stroke-miterlimit stroke-dasharray stroke-dashoffset opacity color clip-path clip-rule mask filter font-family font-size font-weight font-style text-anchor dominant-baseline alignment-baseline letter-spacing word-spacing stop-color stop-opacity vector-effect".Split(' '), StringComparer.OrdinalIgnoreCase);
    private static readonly XNamespace SvgNamespace = "http://www.w3.org/2000/svg";

    /// <summary>
    /// Validates the operation.
    /// </summary>
    /// <param name="content">The content.</param>
    public static string Validate(string content)
    {
        if (string.IsNullOrWhiteSpace(content) || Encoding.UTF8.GetByteCount(content) > 262144)
            throw new ArgumentException("Bitte eine SVG-Datei mit höchstens 256 KB hochladen.");
        using var reader = XmlReader.Create(new StringReader(content), new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 262144,
        });
        var document = XDocument.Load(reader);
        var root = document.Root;
        if (root is null || root.Name.LocalName != "svg" || (root.Name.Namespace != SvgNamespace && root.Name.Namespace != XNamespace.None))
            throw new ArgumentException("Die Datei muss ein SVG-Bild enthalten.");
        if (document.DescendantNodes().OfType<XProcessingInstruction>().Any())
            throw new ArgumentException("SVG-Verarbeitungsanweisungen sind nicht erlaubt.");
        root.Descendants().Where(node => node.Name.LocalName == "metadata" || node.Name.NamespaceName.Contains("inkscape") || node.Name.NamespaceName.Contains("sodipodi")).Remove();
        var nodes = root.DescendantsAndSelf().ToList();
        if (nodes.Count > 10000 || nodes.Any(node => node.Ancestors().Count() > 40))
            throw new ArgumentException("Das SVG ist zu komplex.");
        foreach (var node in nodes)
        {
            if (node.Name.LocalName == "metadata" || node.Name.NamespaceName.Contains("inkscape") || node.Name.NamespaceName.Contains("sodipodi"))
            {
                node.Remove();
                continue;
            }
            if (!Elements.Contains(node.Name.LocalName) || (node.Name.Namespace != SvgNamespace && node.Name.Namespace != XNamespace.None))
                throw new ArgumentException($"Das SVG-Element '{node.Name.LocalName}' wird nicht unterstützt.");
            node.Name = SvgNamespace + node.Name.LocalName;
            foreach (var attribute in node.Attributes().ToList())
            {
                var name = attribute.Name.LocalName;
                if (attribute.IsNamespaceDeclaration) continue;
                if (name.StartsWith("on", StringComparison.OrdinalIgnoreCase) || attribute.Name == XNamespace.Xml + "base")
                    throw new ArgumentException("Skripte und Ereignisbehandlung sind in SVG-Bildern nicht erlaubt.");
                if (!Attributes.Contains(name)) { attribute.Remove(); continue; }
                if (name == "href")
                {
                    if (node.Name.LocalName == "image")
                    {
                        attribute.Value = ValidateImageReference(attribute.Value);
                        continue;
                    }
                    if (!Regex.IsMatch(attribute.Value, @"^#[A-Za-z_][\w:.-]*$"))
                        throw new ArgumentException("SVG-Bilder dürfen nur interne Verweise enthalten.");
                }
                ValidateValue(attribute.Value);
                if (name == "style") ValidateDeclarations(attribute.Value);
            }
            if (node.Name.LocalName == "style") ValidateStylesheet(node.Value);
        }

        var viewBox = root.Attribute("viewBox")?.Value;
        if (viewBox is null)
        {
            var width = Dimension(root.Attribute("width")?.Value);
            var height = Dimension(root.Attribute("height")?.Value);
            viewBox = FormattableString.Invariant($"0 0 {width} {height}");
        }
        var parts = viewBox.Split(new[] { ' ', ',', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 4 || parts.Any(part => !double.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || !double.IsFinite(value))
            || double.Parse(parts[2], CultureInfo.InvariantCulture) <= 0 || double.Parse(parts[3], CultureInfo.InvariantCulture) <= 0)
            throw new ArgumentException("Das SVG benötigt eine gültige viewBox mit positiver Breite und Höhe.");
        root.SetAttributeValue("viewBox", string.Join(" ", parts));
        root.SetAttributeValue("width", "64");
        root.SetAttributeValue("height", "64");
        root.SetAttributeValue("preserveAspectRatio", "xMidYMid meet");
        return root.ToString(SaveOptions.DisableFormatting);
    }

    private static double Dimension(string? value)
    {
        var number = Regex.Replace(value ?? "", @"px$", "", RegexOptions.IgnoreCase);
        if (!double.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) || !double.IsFinite(result) || result <= 0)
            throw new ArgumentException("Das SVG benötigt eine viewBox oder gültige Breiten- und Höhenangaben.");
        return result;
    }

    private static string ValidateImageReference(string value)
    {
        if (Regex.IsMatch(value, @"^#[A-Za-z_][\w:.-]*$")) return value;
        var separator = value.IndexOf(',');
        var header = separator < 0 ? string.Empty : value[..separator];
        var png = header.Equals("data:image/png;base64", StringComparison.OrdinalIgnoreCase);
        var jpeg = header.Equals("data:image/jpeg;base64", StringComparison.OrdinalIgnoreCase);
        if (!png && !jpeg)
            throw new ArgumentException("Bilder im SVG müssen als PNG oder JPEG eingebettet sein (data:image/png;base64 oder data:image/jpeg;base64). Bitte externe Bilder im SVG-Editor einbetten.");

        byte[] bytes;
        try { bytes = Convert.FromBase64String(value[(separator + 1)..]); }
        catch (FormatException)
        {
            throw new ArgumentException("Ein eingebettetes Bild enthält ungültige Base64-Daten.");
        }
        // Check the declared raster format without decoding images or loading external resources.
        var signatureValid = png
            ? bytes.AsSpan().StartsWith(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })
            : bytes.AsSpan().StartsWith(new byte[] { 255, 216, 255 });
        if (!signatureValid)
            throw new ArgumentException("Der Inhalt eines eingebetteten Bildes entspricht nicht dem angegebenen PNG- oder JPEG-Format.");
        return $"data:image/{(png ? "png" : "jpeg")};base64,{Convert.ToBase64String(bytes)}";
    }

    private static void ValidateValue(string value)
    {
        if (value.Contains('\\') || value.Contains('@') || value.Contains("javascript:", StringComparison.OrdinalIgnoreCase)
            || value.Contains("expression", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Das SVG enthält nicht unterstützte aktive Inhalte.");
        var remaining = Regex.Replace(value, "url\\(\\s*(['\"]?)#[A-Za-z_][\\w:.-]*\\1\\s*\\)", "", RegexOptions.IgnoreCase);
        if (remaining.Contains("url", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Externe Ressourcen sind in SVG-Bildern nicht erlaubt.");
    }

    private static void ValidateDeclarations(string value)
    {
        ValidateValue(value);
        foreach (var declaration in value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var colon = declaration.IndexOf(':');
            if (colon <= 0 || !CssProperties.Contains(declaration[..colon].Trim()))
                throw new ArgumentException("Das SVG enthält nicht unterstützte CSS-Eigenschaften.");
        }
    }

    private static void ValidateStylesheet(string value)
    {
        ValidateValue(value);
        var remaining = Regex.Replace(value, @"/\*.*?\*/", "", RegexOptions.Singleline);
        foreach (Match rule in Regex.Matches(remaining, @"([^{}]+)\{([^{}]*)\}"))
        {
            if (!Regex.IsMatch(rule.Groups[1].Value, @"^[\w\s.#,>:+*-]+$")) throw new ArgumentException("Nicht unterstützter SVG-CSS-Selektor.");
            ValidateDeclarations(rule.Groups[2].Value);
        }
        if (!string.IsNullOrWhiteSpace(Regex.Replace(remaining, @"[^{}]+\{[^{}]*\}", "")))
            throw new ArgumentException("Ungültiges SVG-Stylesheet.");
    }
}

