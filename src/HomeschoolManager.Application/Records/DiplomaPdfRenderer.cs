using System.Globalization;
using System.Text;

namespace HomeschoolManager.Application.Records;

internal static class DiplomaPdfRenderer
{
    private const decimal PageWidth = 792m;
    private const decimal PageHeight = 612m;
    private const decimal GoldR = .68m;
    private const decimal GoldG = .48m;
    private const decimal GoldB = .16m;

    public static byte[] Create(DiplomaDesignView design)
    {
        var writer = new PdfWriter();
        writer.Border();
        writer.CornerFlourish(42, PageHeight - 44, flipX: false, flipY: false);
        writer.CornerFlourish(PageWidth - 42, PageHeight - 44, flipX: true, flipY: false);
        writer.CornerFlourish(42, 44, flipX: false, flipY: true);
        writer.CornerFlourish(PageWidth - 42, 44, flipX: true, flipY: true);

        writer.Center(design.HomeschoolName, Style(design, "homeschoolName"), 518, maxWidth: 620);
        writer.Rule(282, 492, 510);
        writer.Center("*", new DiplomaTextStyleView("ornament", "Ornament", "Times New Roman", 20, false, 0), 485, maxWidth: 80, gold: true);
        writer.Center(design.CertifiesText, Style(design, "certifiesText"), 456, maxWidth: 420);
        writer.Center(design.StudentName, Style(design, "studentName"), 398, maxWidth: 650);
        writer.Rule(282, 370, 510);
        writer.Center("<>", new DiplomaTextStyleView("ornament", "Ornament", "Times New Roman", 15, false, 0), 363, maxWidth: 80, gold: true);
        writer.CenterWrapped(design.CompletionText, Style(design, "completionText"), 338, 600, 25);
        writer.Center(design.DiplomaTitle, Style(design, "diplomaTitle"), 270, maxWidth: 630);
        writer.Rule(282, 242, 510);
        writer.Center("<>", new DiplomaTextStyleView("ornament", "Ornament", "Times New Roman", 15, false, 0), 235, maxWidth: 80, gold: true);
        writer.CenterWrapped(design.PrivilegesText, Style(design, "privilegesText"), 215, 610, 22);
        writer.Rule(322, 190, 470);
        writer.Center("*", new DiplomaTextStyleView("ornament", "Ornament", "Times New Roman", 16, false, 0), 183, maxWidth: 80, gold: true);
        writer.Center($"{design.AwardedText} {AwardedDate(design.AwardedDate)}", Style(design, "awardedText"), 163, maxWidth: 420);

        writer.Seal(396, 112, design.SealText, Style(design, "sealText"));
        writer.SignatureLine(82, 108, 290, design.SignatureLabel, Style(design, "signatureLabel"));
        writer.SignatureLine(502, 108, 710, design.DateLabel, Style(design, "dateLabel"));

        return writer.Build();
    }

    private static DiplomaTextStyleView Style(DiplomaDesignView design, string key)
    {
        return design.TextStyles.FirstOrDefault(style => string.Equals(style.ElementKey, key, StringComparison.OrdinalIgnoreCase))
            ?? new DiplomaTextStyleView(key, key, "Times New Roman", 18, false, 0);
    }

    private static string AwardedDate(DateOnly? date)
    {
        return date?.ToString("MMMM yyyy", CultureInfo.CurrentCulture) ?? "________________";
    }

    private sealed class PdfWriter
    {
        private readonly StringBuilder content = new();

        public void Border()
        {
            Stroke(31, 31, 35);
            LineWidth(2);
            Rect(15, 15, PageWidth - 30, PageHeight - 30);
            LineWidth(1);
            Rect(22, 22, PageWidth - 44, PageHeight - 44);
            GoldStroke();
            LineWidth(.8m);
            Rect(30, 30, PageWidth - 60, PageHeight - 60);
        }

        public void CornerFlourish(decimal x, decimal y, bool flipX, bool flipY)
        {
            GoldStroke();
            LineWidth(1.2m);
            var sx = flipX ? -1 : 1;
            var sy = flipY ? -1 : 1;
            Curve(x, y, x + sx * 16, y - sy * 4, x + sx * 20, y - sy * 20, x + sx * 4, y - sy * 28);
            Curve(x + sx * 8, y - sy * 8, x + sx * 28, y - sy * 8, x + sx * 32, y - sy * 28, x + sx * 18, y - sy * 40);
            Curve(x + sx * 2, y - sy * 30, x + sx * 8, y - sy * 50, x + sx * 32, y - sy * 45, x + sx * 40, y - sy * 30);
        }

        public void Center(string value, DiplomaTextStyleView style, decimal y, decimal maxWidth, bool gold = false)
        {
            var text = Prepare(value, style);
            var size = FitSize(text, style.FontSize, style.LetterSpacing, maxWidth);
            var width = TextWidth(text, size, style.LetterSpacing);
            DrawText(text, (PageWidth - width) / 2, y, size, style, gold);
        }

        public void CenterWrapped(string value, DiplomaTextStyleView style, decimal startY, decimal maxWidth, decimal lineHeight)
        {
            var lines = Wrap(Prepare(value, style), maxWidth, style.FontSize);
            var y = startY;
            foreach (var line in lines)
            {
                var width = TextWidth(line, style.FontSize, style.LetterSpacing);
                DrawText(line, (PageWidth - width) / 2, y, style.FontSize, style, gold: false);
                y -= lineHeight;
            }
        }

        public void Rule(decimal x1, decimal y, decimal x2)
        {
            GoldStroke();
            LineWidth(.7m);
            Move(x1, y);
            LineTo(x2, y);
            StrokePath();
        }

        public void Seal(decimal centerX, decimal centerY, string text, DiplomaTextStyleView style)
        {
            GoldStroke();
            LineWidth(1);
            Circle(centerX, centerY, 54);
            Circle(centerX, centerY, 43);
            for (var index = 0; index < 36; index++)
            {
                var angle = Math.PI * 2 * index / 36;
                var x1 = centerX + (decimal)Math.Cos(angle) * 55;
                var y1 = centerY + (decimal)Math.Sin(angle) * 55;
                var x2 = centerX + (decimal)Math.Cos(angle) * 60;
                var y2 = centerY + (decimal)Math.Sin(angle) * 60;
                Move(x1, y1);
                LineTo(x2, y2);
                StrokePath();
            }

            Center(text.Replace(' ', '\n'), style with { FontSize = Math.Min(style.FontSize, 17), Uppercase = true }, centerY + 8, 90, gold: false);
        }

        public void SignatureLine(decimal x1, decimal y, decimal x2, string label, DiplomaTextStyleView style)
        {
            Stroke(31, 31, 35);
            LineWidth(.8m);
            Move(x1, y);
            LineTo(x2, y);
            StrokePath();
            GoldStroke();
            Move((x1 + x2) / 2 - 9, y - 2);
            LineTo((x1 + x2) / 2 + 9, y - 2);
            StrokePath();
            var text = Prepare(label, style);
            var width = TextWidth(text, style.FontSize, style.LetterSpacing);
            DrawText(text, (x1 + x2 - width) / 2, y - 23, style.FontSize, style, gold: false);
        }

        public byte[] Build()
        {
            var stream = content.ToString();
            var objects = new List<string>
            {
                "<< /Type /Catalog /Pages 2 0 R >>",
                "<< /Type /Pages /Kids [4 0 R] /Count 1 >>",
                $"<< /Length {Encoding.ASCII.GetByteCount(stream)} >>\nstream\n{stream}endstream",
                "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 792 612] /Resources << /Font << /F1 5 0 R /F2 6 0 R /F3 7 0 R /F4 8 0 R /F5 9 0 R >> >> /Contents 3 0 R >>",
                "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
                "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>",
                "<< /Type /Font /Subtype /Type1 /BaseFont /Times-Roman >>",
                "<< /Type /Font /Subtype /Type1 /BaseFont /Times-Bold >>",
                "<< /Type /Font /Subtype /Type1 /BaseFont /Courier >>"
            };
            return BuildPdf(objects);
        }

        private void DrawText(string value, decimal x, decimal y, decimal size, DiplomaTextStyleView style, bool gold)
        {
            if (gold)
            {
                Fill(GoldR, GoldG, GoldB);
            }
            else
            {
                Fill(.09m, .09m, .11m);
            }

            content.AppendLine("BT");
            content.Append('/').Append(FontName(style)).Append(' ').Append(Format(size)).AppendLine(" Tf");
            content.Append(Format(style.LetterSpacing)).AppendLine(" Tc");
            content.Append(Format(x)).Append(' ').Append(Format(y)).AppendLine(" Td");
            foreach (var line in value.Split('\n'))
            {
                content.Append('(').Append(Escape(line)).AppendLine(") Tj");
                content.AppendLine("0 -20 Td");
            }

            content.AppendLine("0 Tc");
            content.AppendLine("ET");
        }

        private static string Prepare(string value, DiplomaTextStyleView style)
        {
            var text = string.IsNullOrWhiteSpace(value) ? "" : value.Trim();
            return style.Uppercase ? text.ToUpperInvariant() : text;
        }

        private static IReadOnlyList<string> Wrap(string value, decimal maxWidth, decimal size)
        {
            var lines = new List<string>();
            var words = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var current = new StringBuilder();
            foreach (var word in words)
            {
                var candidate = current.Length == 0 ? word : $"{current} {word}";
                if (TextWidth(candidate, size, 0) > maxWidth && current.Length > 0)
                {
                    lines.Add(current.ToString());
                    current.Clear();
                    current.Append(word);
                }
                else
                {
                    current.Clear();
                    current.Append(candidate);
                }
            }

            if (current.Length > 0)
            {
                lines.Add(current.ToString());
            }

            return lines.Count == 0 ? [""] : lines;
        }

        private static decimal FitSize(string text, decimal requested, decimal letterSpacing, decimal maxWidth)
        {
            var size = requested;
            while (size > 8 && TextWidth(text, size, letterSpacing) > maxWidth)
            {
                size -= 1;
            }

            return size;
        }

        private static decimal TextWidth(string value, decimal size, decimal letterSpacing)
        {
            return ToPdfText(value.Replace("\n", "", StringComparison.Ordinal)).Length * size * .52m + Math.Max(0, value.Length - 1) * letterSpacing;
        }

        private static string FontName(DiplomaTextStyleView style)
        {
            var family = style.FontFamily ?? "";
            if (family.Contains("courier", StringComparison.OrdinalIgnoreCase) ||
                family.Contains("consolas", StringComparison.OrdinalIgnoreCase))
            {
                return "F5";
            }

            if (family.Contains("arial", StringComparison.OrdinalIgnoreCase) ||
                family.Contains("segoe", StringComparison.OrdinalIgnoreCase) ||
                family.Contains("calibri", StringComparison.OrdinalIgnoreCase) ||
                family.Contains("verdana", StringComparison.OrdinalIgnoreCase))
            {
                return "F2";
            }

            return "F4";
        }

        private void Rect(decimal x, decimal y, decimal width, decimal height)
        {
            content.Append(Format(x)).Append(' ').Append(Format(y)).Append(' ').Append(Format(width)).Append(' ').Append(Format(height)).AppendLine(" re S");
        }

        private void Circle(decimal x, decimal y, decimal radius)
        {
            const decimal k = .5522847498m;
            var c = radius * k;
            Move(x + radius, y);
            CurveTo(x + radius, y + c, x + c, y + radius, x, y + radius);
            CurveTo(x - c, y + radius, x - radius, y + c, x - radius, y);
            CurveTo(x - radius, y - c, x - c, y - radius, x, y - radius);
            CurveTo(x + c, y - radius, x + radius, y - c, x + radius, y);
            StrokePath();
        }

        private void Curve(decimal x1, decimal y1, decimal x2, decimal y2, decimal x3, decimal y3, decimal x4, decimal y4)
        {
            Move(x1, y1);
            CurveTo(x2, y2, x3, y3, x4, y4);
            StrokePath();
        }

        private void Move(decimal x, decimal y) => content.Append(Format(x)).Append(' ').Append(Format(y)).AppendLine(" m");

        private void LineTo(decimal x, decimal y) => content.Append(Format(x)).Append(' ').Append(Format(y)).AppendLine(" l");

        private void CurveTo(decimal x1, decimal y1, decimal x2, decimal y2, decimal x3, decimal y3)
        {
            content.Append(Format(x1)).Append(' ').Append(Format(y1)).Append(' ')
                .Append(Format(x2)).Append(' ').Append(Format(y2)).Append(' ')
                .Append(Format(x3)).Append(' ').Append(Format(y3)).AppendLine(" c");
        }

        private void StrokePath() => content.AppendLine("S");

        private void LineWidth(decimal width) => content.Append(Format(width)).AppendLine(" w");

        private void Stroke(int r, int g, int b)
        {
            content.Append(Format(r / 255m)).Append(' ').Append(Format(g / 255m)).Append(' ').Append(Format(b / 255m)).AppendLine(" RG");
        }

        private void GoldStroke()
        {
            content.Append(Format(GoldR)).Append(' ').Append(Format(GoldG)).Append(' ').Append(Format(GoldB)).AppendLine(" RG");
        }

        private void Fill(decimal r, decimal g, decimal b)
        {
            content.Append(Format(r)).Append(' ').Append(Format(g)).Append(' ').Append(Format(b)).AppendLine(" rg");
        }

        private static byte[] BuildPdf(IReadOnlyList<string> objects)
        {
            var builder = new StringBuilder();
            var offsets = new List<int> { 0 };
            builder.AppendLine("%PDF-1.4");
            for (var index = 0; index < objects.Count; index++)
            {
                offsets.Add(Encoding.ASCII.GetByteCount(builder.ToString()));
                builder.Append(index + 1).AppendLine(" 0 obj");
                builder.AppendLine(objects[index]);
                builder.AppendLine("endobj");
            }

            var xrefOffset = Encoding.ASCII.GetByteCount(builder.ToString());
            builder.AppendLine("xref");
            builder.Append("0 ").AppendLine((objects.Count + 1).ToString(CultureInfo.InvariantCulture));
            builder.AppendLine("0000000000 65535 f ");
            foreach (var offset in offsets.Skip(1))
            {
                builder.Append(offset.ToString("D10", CultureInfo.InvariantCulture)).AppendLine(" 00000 n ");
            }

            builder.AppendLine("trailer");
            builder.Append("<< /Size ").Append(objects.Count + 1).AppendLine(" /Root 1 0 R >>");
            builder.AppendLine("startxref");
            builder.AppendLine(xrefOffset.ToString(CultureInfo.InvariantCulture));
            builder.AppendLine("%%EOF");
            return Encoding.ASCII.GetBytes(builder.ToString());
        }

        private static string Format(decimal value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static string Escape(string value)
        {
            return ToPdfText(value).Replace("\\", "\\\\", StringComparison.Ordinal).Replace("(", "\\(", StringComparison.Ordinal).Replace(")", "\\)", StringComparison.Ordinal);
        }

        private static string ToPdfText(string value)
        {
            return new string((value ?? "").Select(character => character is >= ' ' and <= '~' ? character : '?').ToArray());
        }
    }
}
