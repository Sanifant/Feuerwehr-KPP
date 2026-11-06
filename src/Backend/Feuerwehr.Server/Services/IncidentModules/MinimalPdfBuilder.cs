using System.Text;

namespace De.Openelp.Feuerwehr.Server.Services.IncidentModules
{
    /// <summary>
    /// Builds a simple text PDF for incident diary and situation-map exports.
    /// </summary>
    public static class MinimalPdfBuilder
    {
        /// <summary>
        /// Builds the simple Pdf.
        /// </summary>
        /// <param name="title">The title to include in the response or exported document.</param>
        /// <param name="lines">The ordered text lines to render in the PDF document.</param>
        /// <returns>The generated PDF document bytes.</returns>
        public static byte[] BuildSimplePdf(string title, IEnumerable<string> lines)
        {
            List<string[]> pages = Paginate(lines.SelectMany(WrapLine));
            int fontId = 3 + (pages.Count * 2);
            using MemoryStream ms = new();
            using StreamWriter writer = new(ms, Encoding.ASCII, leaveOpen: true);
            writer.Write("%PDF-1.4\n");
            List<long> offsets = new()
            { 0 };

            void WriteObject(int id, string obj)
            {
                writer.Flush();
                offsets.Add(ms.Position);
                writer.Write($"{id} 0 obj\n{obj}\nendobj\n");
            }

            WriteObject(1, "<< /Type /Catalog /Pages 2 0 R >>");
            string pageReferences = string.Join(" ", Enumerable.Range(0, pages.Count).Select(index => $"{3 + (index * 2)} 0 R"));
            WriteObject(2, $"<< /Type /Pages /Kids [{pageReferences}] /Count {pages.Count} >>");
            for (int index = 0; index < pages.Count; index++)
            {
                int pageId = 3 + (index * 2);
                int contentId = pageId + 1;
                StringBuilder content = new();
                _ = content.Append($"BT /F1 16 Tf 50 790 Td ({Escape(title)}) Tj ET\n");
                int y = 760;
                foreach (string line in pages[index])
                {
                    _ = content.Append($"BT /F1 9.5 Tf 50 {y} Td ({Escape(line)}) Tj ET\n");
                    y -= 12;
                }

                _ = content.Append($"BT /F1 9 Tf 50 32 Td (Seite {index + 1} / {pages.Count}) Tj ET\n");
                string contentText = content.ToString();
                WriteObject(pageId, $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Contents {contentId} 0 R /Resources << /Font << /F1 {fontId} 0 R >> >> >>");
                WriteObject(contentId, $"<< /Length {Encoding.ASCII.GetByteCount(contentText)} >>\nstream\n{contentText}endstream");
            }

            WriteObject(fontId, "<< /Type /Font /Subtype /Type1 /BaseFont /Courier /Encoding /WinAnsiEncoding >>");

            writer.Flush();
            long xrefPos = ms.Position;
            writer.Write($"xref\n0 {offsets.Count}\n");
            writer.Write("0000000000 65535 f \n");
            for (int i = 1; i < offsets.Count; i++)
            {
                writer.Write($"{offsets[i]:D10} 00000 n \n");
            }

            writer.Write($"trailer << /Size {offsets.Count} /Root 1 0 R >>\nstartxref\n{xrefPos}\n%%EOF");
            writer.Flush();

            return ms.ToArray();
        }

        private static List<string[]> Paginate(IEnumerable<string> lines)
        {
            List<string[]> pages = new();
            List<string> page = new();
            List<string> block = new();
            void FlushBlock(bool separator)
            {
                if (block.Count <= 56 && page.Count + block.Count > 56)
                {
                    pages.Add(page.ToArray());
                    page.Clear();
                }

                foreach (string line in block)
                {
                    if (page.Count == 56)
                    {
                        pages.Add(page.ToArray());
                        page.Clear();
                    }

                    page.Add(line);
                }

                block.Clear();
                if (separator && page.Count is > 0 and < 56 && page[^1] != string.Empty)
                {
                    page.Add(string.Empty);
                }
            }

            foreach (string line in lines)
            {
                if (line.Length == 0)
                {
                    FlushBlock(true);
                }
                else
                {
                    block.Add(line);
                }
            }

            FlushBlock(false);
            if (page.Count > 0)
            {
                pages.Add(page.ToArray());
            }

            if (pages.Count == 0)
            {
                pages.Add([]);
            }

            return pages;
        }

        private static IEnumerable<string> WrapLine(string input)
        {
            foreach (string paragraph in (input ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n').Replace("\t", "    ").Split('\n'))
            {
                string remaining = paragraph;
                while (remaining.Length > 86)
                {
                    int split = remaining.LastIndexOf(' ', 86, 87);
                    if (split <= 0)
                    {
                        split = 86;
                    }

                    yield return remaining[..split];
                    remaining = remaining[split..].TrimStart();
                }

                yield return remaining;
            }
        }

        private static string Escape(string input)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            byte[] bytes = Encoding.GetEncoding(1252).GetBytes(input);
            StringBuilder result = new();
            foreach (byte value in bytes)
            {
                if (value is (byte)'\\' or (byte)'(' or (byte)')')
                {
                    _ = result.Append('\\').Append((char)value);
                }
                else if (value is < 32 or > 126)
                {
                    _ = result.Append('\\').Append(Convert.ToString(value, 8).PadLeft(3, '0'));
                }
                else
                {
                    _ = result.Append((char)value);
                }
            }

            return result.ToString();
        }
    }
}
