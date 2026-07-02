using System.Globalization;
using System.Text;

namespace HomeschoolManager.Application.Records;

internal static class DiplomaPdfRenderer
{
    private const decimal PageWidth = 792m;
    private const decimal PageHeight = 612m;
    private const decimal PdfPointsPerCssPixel = .75m;
    private const decimal GoldR = .68m;
    private const decimal GoldG = .48m;
    private const decimal GoldB = .16m;

    public static byte[] Create(DiplomaDesignView design)
    {
        var writer = new PdfWriter();
        var layout = new DiplomaLayout(writer);

        layout.Border();
        layout.CornerFlourishes();

        var top = layout.ContentTop;
        top = layout.CenterBlock(design.HomeschoolName, Style(design, "homeschoolName"), top + layout.Rem(.4m), layout.AvailableTextWidth, strong: true);
        top = layout.Rule(top, width: layout.Rem(28), marginY: layout.Rem(1.05m), ornament: "*", ornamentSizeCss: 20);
        top = layout.CenterBlock(design.CertifiesText, Style(design, "certifiesText"), top, layout.Rem(26.25m));
        top = layout.CenterBlock(design.StudentName, Style(design, "studentName"), top + layout.Rem(.8m), layout.Rem(46), bottomMargin: layout.Rem(.8m));
        top = layout.Rule(top, width: layout.Rem(26), marginY: layout.Rem(.8m), ornament: "<>", ornamentSizeCss: 15);
        top = layout.CenterBlock(design.CompletionText, Style(design, "completionText"), top, layout.Rem(44));
        top = layout.CenterBlock(design.DiplomaTitle, Style(design, "diplomaTitle"), top + layout.Rem(.8m), layout.Rem(46), bottomMargin: layout.Rem(.8m));
        top = layout.Rule(top, width: layout.Rem(26), marginY: layout.Rem(.8m), ornament: "<>", ornamentSizeCss: 15);
        _ = layout.CenterBlock(design.PrivilegesText, Style(design, "privilegesText"), top, layout.Rem(44));

        layout.AbsoluteRuleFromBottom(layout.Rem(14.8m), width: layout.Rem(18), ornament: "*", ornamentSizeCss: 16);
        layout.CenterLineFromBottom(
            $"{design.AwardedText} {AwardedDate(design.AwardedDate)}",
            Style(design, "awardedText"),
            bottom: layout.Rem(12.2m),
            maxWidth: layout.Rem(26.25m));

        layout.Footer(
            design.SealText,
            Style(design, "sealText"),
            design.SignatureLabel,
            Style(design, "signatureLabel"),
            design.DateLabel,
            Style(design, "dateLabel"));

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

    private sealed class DiplomaLayout
    {
        private const decimal SheetWidthCss = 1056m;
        private const decimal SheetHeightCss = 816m;
        private readonly PdfWriter writer;

        public DiplomaLayout(PdfWriter writer)
        {
            this.writer = writer;
        }

        public decimal ContentTop => Rem(3.75m);

        public decimal AvailableTextWidth => Css(SheetWidthCss) - Rem(8.5m);

        public decimal Rem(decimal value) => Css(value * 16m);

        public decimal Css(decimal value) => value * PdfPointsPerCssPixel;

        public void Border()
        {
            writer.Stroke(31, 31, 35);
            writer.LineWidth(2);
            writer.Rect(Rem(1), Rem(1), PageWidth - Rem(2), PageHeight - Rem(2));
            writer.LineWidth(1);
            writer.Rect(Rem(1.55m), Rem(1.55m), PageWidth - Rem(3.1m), PageHeight - Rem(3.1m));
            writer.GoldStroke();
            writer.LineWidth(.8m);
            writer.Rect(Rem(2.15m), Rem(2.15m), PageWidth - Rem(4.3m), PageHeight - Rem(4.3m));
        }

        public void CornerFlourishes()
        {
            writer.CornerFlourish(Rem(2.3m), PageHeight - Rem(2.35m), flipX: false, flipY: false);
            writer.CornerFlourish(PageWidth - Rem(2.3m), PageHeight - Rem(2.35m), flipX: true, flipY: false);
            writer.CornerFlourish(Rem(2.3m), Rem(2.35m), flipX: false, flipY: true);
            writer.CornerFlourish(PageWidth - Rem(2.3m), Rem(2.35m), flipX: true, flipY: true);
        }

        public decimal Rule(decimal top, decimal width, decimal marginY, string ornament, decimal ornamentSizeCss)
        {
            var ruleTop = top + marginY;
            var centerY = PageHeight - (ruleTop + Css(10));
            DrawRule(centerY, width);
            writer.CenterLine(
                ornament,
                new DiplomaTextStyleView("ornament", "Ornament", "Times New Roman", ornamentSizeCss, false, 0),
                centerY - Css(5),
                maxWidth: Rem(4),
                gold: true,
                strong: false);
            return ruleTop + Css(20) + marginY;
        }

        public void AbsoluteRuleFromBottom(decimal bottom, decimal width, string ornament, decimal ornamentSizeCss)
        {
            var centerY = bottom;
            DrawRule(centerY, width);
            writer.CenterLine(
                ornament,
                new DiplomaTextStyleView("ornament", "Ornament", "Times New Roman", ornamentSizeCss, false, 0),
                centerY - Css(5),
                maxWidth: Rem(4),
                gold: true,
                strong: false);
        }

        public decimal CenterBlock(
            string value,
            DiplomaTextStyleView style,
            decimal top,
            decimal maxWidth,
            bool strong = false,
            decimal bottomMargin = 0)
        {
            var lines = writer.Wrap(Prepare(value, style), style, maxWidth);
            var textStyle = writer.ScaleStyle(style);
            var lineHeight = Math.Max(Css(12), textStyle.FontSize * 1.15m);
            var y = PageHeight - top - textStyle.FontSize * .86m;
            foreach (var line in lines)
            {
                writer.CenterPreparedLine(line, textStyle, y, maxWidth, gold: false, strong);
                y -= lineHeight;
            }

            return top + lineHeight * lines.Count + bottomMargin;
        }

        public void CenterLineFromBottom(string value, DiplomaTextStyleView style, decimal bottom, decimal maxWidth)
        {
            var textStyle = writer.ScaleStyle(style);
            writer.CenterLine(value, style, bottom + textStyle.FontSize * .8m, maxWidth, gold: false, strong: false);
        }

        public void Footer(
            string sealText,
            DiplomaTextStyleView sealStyle,
            string signatureLabel,
            DiplomaTextStyleView signatureStyle,
            string dateLabel,
            DiplomaTextStyleView dateStyle)
        {
            var sideInset = Rem(6.5m);
            var gap = Rem(3.25m);
            var sealSize = Rem(8.2m);
            var signatureColumnWidth = (PageWidth - sideInset * 2 - sealSize - gap * 2) / 2;
            var bottom = Rem(3.25m);
            var sealCenterY = bottom + sealSize / 2;
            var sealCenterX = PageWidth / 2;
            var lineY = bottom + Css(27.5m);

            writer.Seal(sealCenterX, sealCenterY, sealText, sealStyle, sealSize / 2);
            writer.SignatureLine(sideInset, lineY, sideInset + signatureColumnWidth, signatureLabel, signatureStyle);
            writer.SignatureLine(PageWidth - sideInset - signatureColumnWidth, lineY, PageWidth - sideInset, dateLabel, dateStyle);
        }

        private void DrawRule(decimal centerY, decimal width)
        {
            var x1 = (PageWidth - width) / 2;
            var x2 = x1 + width;
            writer.GoldStroke();
            writer.LineWidth(.7m);
            writer.Move(x1, centerY);
            writer.LineTo(x2, centerY);
            writer.StrokePath();
        }

        private static string Prepare(string value, DiplomaTextStyleView style)
        {
            var text = string.IsNullOrWhiteSpace(value) ? "" : value.Trim();
            return style.Uppercase ? text.ToUpperInvariant() : text;
        }
    }

    private sealed class PdfWriter
    {
        private readonly StringBuilder content = new();

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

        public void CenterLine(string value, DiplomaTextStyleView style, decimal y, decimal maxWidth, bool gold = false, bool strong = false)
        {
            var text = Prepare(value, style);
            var textStyle = ScaleStyle(style);
            var size = FitSize(text, textStyle.FontSize, textStyle.LetterSpacing, maxWidth);
            var fitted = textStyle with { FontSize = size };
            CenterPreparedLine(text, fitted, y, maxWidth, gold, strong);
        }

        public void CenterPreparedLine(string text, DiplomaTextStyleView style, decimal y, decimal maxWidth, bool gold, bool strong)
        {
            var size = FitSize(text, style.FontSize, style.LetterSpacing, maxWidth);
            var fitted = style with { FontSize = size };
            var width = TextWidth(text, fitted.FontSize, fitted.LetterSpacing);
            DrawText(text, (PageWidth - width) / 2, y, fitted, gold, strong);
        }

        public IReadOnlyList<string> Wrap(string value, DiplomaTextStyleView style, decimal maxWidth)
        {
            var textStyle = ScaleStyle(style);
            var lines = new List<string>();
            var words = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var current = new StringBuilder();
            foreach (var word in words)
            {
                var candidate = current.Length == 0 ? word : $"{current} {word}";
                if (TextWidth(candidate, textStyle.FontSize, textStyle.LetterSpacing) > maxWidth && current.Length > 0)
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

        public void Seal(decimal centerX, decimal centerY, string text, DiplomaTextStyleView style, decimal radius)
        {
            ScallopedSeal(centerX, centerY, radius, radius * .85m);
            Fill(.96m, .91m, .78m);
            GoldStroke();
            LineWidth(.8m);
            FilledCircle(centerX, centerY, radius * .76m);
            LineWidth(1);
            Circle(centerX, centerY, radius * .65m);
            Circle(centerX, centerY, radius * .48m);
            LineWidth(.35m);
            for (var index = 0; index < 48; index++)
            {
                var angle = Math.PI * 2 * index / 48;
                var x1 = centerX + (decimal)Math.Cos(angle) * radius * .56m;
                var y1 = centerY + (decimal)Math.Sin(angle) * radius * .56m;
                var x2 = centerX + (decimal)Math.Cos(angle) * radius * .72m;
                var y2 = centerY + (decimal)Math.Sin(angle) * radius * .72m;
                Move(x1, y1);
                LineTo(x2, y2);
                StrokePath();
            }

            var scaled = ScaleStyle(style with { FontSize = Math.Min(style.FontSize, 18), Uppercase = true });
            var lines = Prepare(text, style with { Uppercase = true })
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var sealLines = lines.Length == 0 ? ["FAMILY", "ISSUED"] : lines;
            var lineHeight = scaled.FontSize * 1.18m;
            var firstY = centerY + lineHeight * (sealLines.Length - 1) / 2 - scaled.FontSize * .35m;
            for (var index = 0; index < sealLines.Length; index++)
            {
                CenterPreparedLine(sealLines[index], scaled, firstY - index * lineHeight, radius * 1.4m, gold: false, strong: true);
            }
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
            var textStyle = ScaleStyle(style);
            var text = Prepare(label, style);
            var width = TextWidth(text, textStyle.FontSize, textStyle.LetterSpacing);
            DrawText(text, (x1 + x2 - width) / 2, y - textStyle.FontSize - 8, textStyle, gold: false, strong: true);
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

        public DiplomaTextStyleView ScaleStyle(DiplomaTextStyleView style)
        {
            return style with
            {
                FontSize = style.FontSize * PdfPointsPerCssPixel,
                LetterSpacing = style.LetterSpacing * PdfPointsPerCssPixel
            };
        }

        private void DrawText(string value, decimal x, decimal y, DiplomaTextStyleView style, bool gold, bool strong)
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
            content.Append('/').Append(FontName(style, strong)).Append(' ').Append(Format(style.FontSize)).AppendLine(" Tf");
            content.Append(Format(style.LetterSpacing)).AppendLine(" Tc");
            content.Append(Format(x)).Append(' ').Append(Format(y)).AppendLine(" Td");
            content.Append('(').Append(Escape(value)).AppendLine(") Tj");
            content.AppendLine("0 Tc");
            content.AppendLine("ET");
        }

        private static string Prepare(string value, DiplomaTextStyleView style)
        {
            var text = string.IsNullOrWhiteSpace(value) ? "" : value.Trim();
            return style.Uppercase ? text.ToUpperInvariant() : text;
        }

        private static decimal FitSize(string text, decimal requested, decimal letterSpacing, decimal maxWidth)
        {
            var size = requested;
            while (size > 7 && TextWidth(text, size, letterSpacing) > maxWidth)
            {
                size -= .5m;
            }

            return size;
        }

        private static decimal TextWidth(string value, decimal size, decimal letterSpacing)
        {
            var text = ToPdfText(value);
            if (text.Length == 0)
            {
                return 0;
            }

            var ems = text.Sum(WidthEm);
            return ems * size + Math.Max(0, text.Length - 1) * letterSpacing;
        }

        private static decimal WidthEm(char character)
        {
            if (character == ' ')
            {
                return .28m;
            }

            if ("ilI1|!.,'`:;".Contains(character))
            {
                return .25m;
            }

            if ("mwMW@#%&".Contains(character))
            {
                return .82m;
            }

            if (char.IsUpper(character))
            {
                return .62m;
            }

            if (char.IsDigit(character))
            {
                return .5m;
            }

            if ("-_/()[]".Contains(character))
            {
                return .34m;
            }

            return .48m;
        }

        private static string FontName(DiplomaTextStyleView style, bool strong)
        {
            var family = style.FontFamily ?? "";
            if (family.Contains("courier", StringComparison.OrdinalIgnoreCase) ||
                family.Contains("consolas", StringComparison.OrdinalIgnoreCase))
            {
                return "F5";
            }

            var sans = family.Contains("arial", StringComparison.OrdinalIgnoreCase) ||
                family.Contains("segoe", StringComparison.OrdinalIgnoreCase) ||
                family.Contains("calibri", StringComparison.OrdinalIgnoreCase) ||
                family.Contains("verdana", StringComparison.OrdinalIgnoreCase);

            if (sans)
            {
                return strong ? "F2" : "F1";
            }

            return strong ? "F4" : "F3";
        }

        public void Rect(decimal x, decimal y, decimal width, decimal height)
        {
            content.Append(Format(x)).Append(' ').Append(Format(y)).Append(' ').Append(Format(width)).Append(' ').Append(Format(height)).AppendLine(" re S");
        }

        private void ScallopedSeal(decimal x, decimal y, decimal outerRadius, decimal innerRadius)
        {
            Fill(.9m, .75m, .42m);
            GoldStroke();
            LineWidth(.8m);
            var points = 96;
            for (var index = 0; index < points; index++)
            {
                var angle = Math.PI * 2 * index / points;
                var radius = index % 2 == 0 ? outerRadius : innerRadius;
                var px = x + (decimal)Math.Cos(angle) * radius;
                var py = y + (decimal)Math.Sin(angle) * radius;
                if (index == 0)
                {
                    Move(px, py);
                }
                else
                {
                    LineTo(px, py);
                }
            }

            CloseFillStroke();
        }

        private void FilledCircle(decimal x, decimal y, decimal radius)
        {
            CirclePath(x, y, radius);
            content.AppendLine("B");
        }

        private void Circle(decimal x, decimal y, decimal radius)
        {
            CirclePath(x, y, radius);
            StrokePath();
        }

        private void CirclePath(decimal x, decimal y, decimal radius)
        {
            const decimal k = .5522847498m;
            var c = radius * k;
            Move(x + radius, y);
            CurveTo(x + radius, y + c, x + c, y + radius, x, y + radius);
            CurveTo(x - c, y + radius, x - radius, y + c, x - radius, y);
            CurveTo(x - radius, y - c, x - c, y - radius, x, y - radius);
            CurveTo(x + c, y - radius, x + radius, y - c, x + radius, y);
        }

        private void Curve(decimal x1, decimal y1, decimal x2, decimal y2, decimal x3, decimal y3, decimal x4, decimal y4)
        {
            Move(x1, y1);
            CurveTo(x2, y2, x3, y3, x4, y4);
            StrokePath();
        }

        public void Move(decimal x, decimal y) => content.Append(Format(x)).Append(' ').Append(Format(y)).AppendLine(" m");

        public void LineTo(decimal x, decimal y) => content.Append(Format(x)).Append(' ').Append(Format(y)).AppendLine(" l");

        private void CurveTo(decimal x1, decimal y1, decimal x2, decimal y2, decimal x3, decimal y3)
        {
            content.Append(Format(x1)).Append(' ').Append(Format(y1)).Append(' ')
                .Append(Format(x2)).Append(' ').Append(Format(y2)).Append(' ')
                .Append(Format(x3)).Append(' ').Append(Format(y3)).AppendLine(" c");
        }

        public void StrokePath() => content.AppendLine("S");

        private void CloseFillStroke() => content.AppendLine("h B");

        public void LineWidth(decimal width) => content.Append(Format(width)).AppendLine(" w");

        public void Stroke(int r, int g, int b)
        {
            content.Append(Format(r / 255m)).Append(' ').Append(Format(g / 255m)).Append(' ').Append(Format(b / 255m)).AppendLine(" RG");
        }

        public void GoldStroke()
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
