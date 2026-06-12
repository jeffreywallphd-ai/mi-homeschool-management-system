using System.Globalization;
using System.Text;

namespace HomeschoolManager.Application.Records;

internal static class TranscriptPdfRenderer
{
    private const decimal PageWidth = 612m;
    private const decimal PageHeight = 792m;
    private const decimal Margin = 42m;
    private const decimal ContentWidth = PageWidth - Margin * 2;

    public static byte[] Create(TranscriptPreview preview, TranscriptArchiveManifest manifest)
    {
        var writer = new PdfWriter();
        writer.Title(preview.SpanLabel);
        writer.CenterText("Family-issued homeschool academic record", 10, Font.Regular);
        writer.Space(10);
        writer.Note(preview.CoverageNote);
        writer.MetaGrid([
            ("Student", preview.StudentName),
            ("School", preview.SchoolName),
            ("Grade span", preview.TypicalGradeRange),
            ("Jurisdiction", Blank(preview.Jurisdiction)),
            ("Administrator", Blank(preview.AdministratorParentName)),
            ("Generated", preview.GeneratedAtUtc.ToLocalTime().ToString("d", CultureInfo.CurrentCulture))
        ]);

        writer.SummaryGrid([
            ("Courses", preview.Summary.CourseCount.ToString(CultureInfo.InvariantCulture)),
            ("Completed", preview.Summary.CompletedCourseCount.ToString(CultureInfo.InvariantCulture)),
            ("Earned credits", Credit(preview.Summary.EarnedCredits)),
            ("GPA", preview.Summary.GpaDisplay)
        ]);

        writer.Section("Academic Record");
        foreach (var year in preview.Years)
        {
            writer.Subsection($"{year.SchoolYearName} - {year.GradeLabel}");
            writer.Table(
                ["Course", "Subject", "Term", "Credit", "Earned", "Final", "Status"],
                year.Courses.Select(course => new[]
                {
                    course.CourseTitle,
                    course.SubjectArea,
                    course.TermLabel,
                    Credit(course.PlannedCreditValue),
                    course.EarnedCreditValue.HasValue ? Credit(course.EarnedCreditValue.Value) : "Not recorded",
                    course.FinalGrade,
                    course.Status
                }),
                [146m, 68m, 70m, 46m, 54m, 50m, 94m]);
        }

        if (preview.Warnings.Count > 0)
        {
            writer.Section("Record Notes");
            foreach (var warning in preview.Warnings)
            {
                writer.Bullet(warning);
            }
        }

        writer.SignatureRow();
        writer.Section("Course Descriptions", forceNewPage: true);
        foreach (var course in preview.CourseDescriptions)
        {
            writer.Subsection(course.CourseTitle);
            writer.Text($"Subject: {course.SubjectArea}", 10, Font.Bold);
            writer.Paragraph("Description", course.Description);
            writer.Paragraph("Major topics", course.MajorTopics);
            writer.Paragraph("Texts and resources", course.TextsAndResources);
            writer.Paragraph("Assessment methods", course.AssessmentMethods);
            if (course.Modules.Count > 0)
            {
                writer.Text("Modules and assignments:", 10, Font.Bold);
                foreach (var module in course.Modules)
                {
                    writer.Bullet($"{module.SequenceOrder}. {module.Title}");
                    foreach (var assignment in module.Assignments)
                    {
                        writer.Bullet(assignment.Title, indent: 18m);
                    }
                }
            }

            writer.Space(6);
        }

        writer.Section("Source Summary", forceNewPage: true);
        writer.Text($"Manifest format: {manifest.Format} v{manifest.FormatVersion}", 10, Font.Regular);
        writer.Text($"Coverage note: {manifest.CoverageNote}", 10, Font.Regular);
        writer.Space(4);
        foreach (var source in manifest.CourseSources)
        {
            var earned = source.EarnedCreditValue.HasValue ? Credit(source.EarnedCreditValue.Value) : "not recorded";
            writer.Bullet($"{source.CourseTitle}: {source.SchoolYearName}; final grade {source.FinalGrade}; earned credit {earned}; course source {source.CourseId}");
        }

        return writer.Build();
    }

    private static string Credit(decimal value)
    {
        return value.ToString("0.0##", CultureInfo.InvariantCulture);
    }

    private static string Blank(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "Not recorded" : value;
    }

    private enum Font
    {
        Regular,
        Bold,
        Serif
    }

    private sealed class PdfWriter
    {
        private readonly List<string> pages = [];
        private StringBuilder current = new();
        private decimal y = PageHeight - Margin;

        public PdfWriter()
        {
            StartPage();
        }

        public void Title(string value)
        {
            Ensure(34);
            CenterText(value.ToUpperInvariant(), 20, Font.Serif);
            Space(3);
            Line(Margin + 120, y, PageWidth - Margin - 120, y, .8m);
            Space(10);
        }

        public void Section(string value, bool forceNewPage = false)
        {
            if (forceNewPage && y < PageHeight - Margin - 20)
            {
                StartPage();
            }

            Ensure(34);
            Space(10);
            Text(value, 14, Font.Bold);
            Line(Margin, y + 2, PageWidth - Margin, y + 2, 1m);
            Space(7);
        }

        public void Subsection(string value)
        {
            Ensure(24);
            Space(6);
            Text(value, 11, Font.Bold);
            Space(2);
        }

        public void Note(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            var lines = SplitSentences(value)
                .SelectMany(sentence => Wrap(sentence, ContentWidth - 18, 9))
                .ToArray();
            var height = 18 + lines.Length * 11;
            Ensure(height + 6);
            Fill(245, 245, 250);
            Rect(Margin, y, ContentWidth, height, fill: true, stroke: true);
            DrawText("Transcript coverage:", Margin + 8, y - 13, 9, Font.Bold);
            var lineY = y - 26;
            foreach (var line in lines)
            {
                DrawText(line, Margin + 8, lineY, 9, Font.Regular);
                lineY -= 11;
            }

            y -= height + 10;
        }

        public void MetaGrid(IReadOnlyList<(string Label, string Value)> items)
        {
            var cellWidth = (ContentWidth - 8) / 2;
            for (var index = 0; index < items.Count; index += 2)
            {
                Ensure(42);
                DrawMetaCell(Margin, y, cellWidth, items[index].Label, items[index].Value);
                if (index + 1 < items.Count)
                {
                    DrawMetaCell(Margin + cellWidth + 8, y, cellWidth, items[index + 1].Label, items[index + 1].Value);
                }

                y -= 39;
            }

            Space(8);
        }

        public void SummaryGrid(IReadOnlyList<(string Label, string Value)> items)
        {
            var cellWidth = (ContentWidth - 24) / 4;
            Ensure(44);
            for (var index = 0; index < items.Count; index++)
            {
                DrawMetaCell(Margin + index * (cellWidth + 8), y, cellWidth, items[index].Label, items[index].Value, valueSize: 13);
            }

            y -= 46;
        }

        public void Table(IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<string>> rows, IReadOnlyList<decimal> widths)
        {
            Ensure(28);
            DrawTableRow(headers, widths, isHeader: true);
            foreach (var row in rows)
            {
                DrawTableRow(row, widths, isHeader: false);
            }

            Space(8);
        }

        public void Paragraph(string label, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            var text = $"{label}: {value}";
            foreach (var line in Wrap(text, ContentWidth, 10))
            {
                Text(line, 10, line == text || line.StartsWith($"{label}:", StringComparison.Ordinal) ? Font.Regular : Font.Regular);
            }

            Space(3);
        }

        public void Bullet(string value, decimal indent = 0)
        {
            var lines = Wrap(value, ContentWidth - indent - 18, 9);
            Ensure(lines.Count * 11 + 4);
            DrawText("-", Margin + indent, y, 9, Font.Regular);
            for (var index = 0; index < lines.Count; index++)
            {
                DrawText(lines[index], Margin + indent + 12, y - index * 11, 9, Font.Regular);
            }

            y -= Math.Max(12, lines.Count * 11);
        }

        public void SignatureRow()
        {
            Ensure(58);
            Space(22);
            Line(Margin, y, Margin + 310, y, .8m);
            Line(PageWidth - Margin - 145, y, PageWidth - Margin, y, .8m);
            DrawText("Parent/Admin Signature", Margin, y - 12, 9, Font.Bold);
            DrawText("Date", PageWidth - Margin - 145, y - 12, 9, Font.Bold);
            y -= 26;
        }

        public void Text(string value, decimal size, Font font)
        {
            foreach (var line in Wrap(value, ContentWidth, size))
            {
                Ensure(size + 6);
                DrawText(line, Margin, y, size, font);
                y -= size + 4;
            }
        }

        public void CenterText(string value, decimal size, Font font)
        {
            Ensure(size + 6);
            var width = TextWidth(value, size);
            DrawText(value, (PageWidth - width) / 2, y, size, font);
            y -= size + 4;
        }

        public void Space(decimal amount)
        {
            y -= amount;
        }

        public byte[] Build()
        {
            FinishPage();
            var objects = new List<string>
            {
                "<< /Type /Catalog /Pages 2 0 R >>",
                ""
            };

            var pageObjectNumbers = new List<int>();
            var fontStart = 3 + pages.Count * 2;
            for (var index = 0; index < pages.Count; index++)
            {
                var contentObjectNumber = 3 + index * 2;
                var pageObjectNumber = contentObjectNumber + 1;
                pageObjectNumbers.Add(pageObjectNumber);
                var stream = pages[index];
                objects.Add($"<< /Length {Encoding.ASCII.GetByteCount(stream)} >>\nstream\n{stream}endstream");
                objects.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 {fontStart} 0 R /F2 {fontStart + 1} 0 R /F3 {fontStart + 2} 0 R >> >> /Contents {contentObjectNumber} 0 R >>");
            }

            objects[1] = $"<< /Type /Pages /Kids [{string.Join(' ', pageObjectNumbers.Select(number => $"{number} 0 R"))}] /Count {pageObjectNumbers.Count} >>";
            objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");
            objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>");
            objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Times-Roman >>");
            return BuildPdf(objects);
        }

        private void DrawMetaCell(decimal x, decimal top, decimal width, string label, string value, decimal valueSize = 10)
        {
            Fill(255, 255, 255);
            Rect(x, top, width, 33, fill: true, stroke: true);
            DrawText(label, x + 7, top - 11, 7, Font.Bold);
            var lineY = top - 24;
            foreach (var line in Wrap(value, width - 14, valueSize).Take(2))
            {
                DrawText(line, x + 7, lineY, valueSize, Font.Regular);
                lineY -= valueSize + 2;
                valueSize = 8;
            }
        }

        private void DrawTableRow(IReadOnlyList<string> cells, IReadOnlyList<decimal> widths, bool isHeader)
        {
            var cellLines = cells.Select((cell, index) => Wrap(cell, widths[index] - 8, isHeader ? 7 : 8)).ToArray();
            var rowHeight = Math.Max(isHeader ? 21 : 23, cellLines.Max(lines => lines.Count) * (isHeader ? 9 : 10) + 10);
            Ensure(rowHeight + 4);
            var x = Margin;
            for (var index = 0; index < cells.Count; index++)
            {
                if (isHeader)
                {
                    Fill(243, 244, 246);
                }
                else
                {
                    Fill(255, 255, 255);
                }

                Rect(x, y, widths[index], rowHeight, fill: true, stroke: true);
                var lineY = y - (isHeader ? 13 : 12);
                foreach (var line in cellLines[index])
                {
                    DrawText(line, x + 4, lineY, isHeader ? 7 : 8, isHeader ? Font.Bold : Font.Regular);
                    lineY -= isHeader ? 9 : 10;
                }

                x += widths[index];
            }

            y -= rowHeight;
        }

        private void Ensure(decimal needed)
        {
            if (y - needed < Margin)
            {
                StartPage();
            }
        }

        private void StartPage()
        {
            if (current.Length > 0)
            {
                FinishPage();
            }

            current = new StringBuilder();
            y = PageHeight - Margin;
        }

        private void FinishPage()
        {
            if (current.Length == 0)
            {
                return;
            }

            pages.Add(current.ToString());
            current = new StringBuilder();
        }

        private void DrawText(string value, decimal x, decimal baseline, decimal size, Font font)
        {
            current.AppendLine("BT");
            current.Append('/').Append(FontName(font)).Append(' ').Append(Format(size)).AppendLine(" Tf");
            current.Append(Format(x)).Append(' ').Append(Format(baseline)).AppendLine(" Td");
            current.Append('(').Append(Escape(value)).AppendLine(") Tj");
            current.AppendLine("ET");
        }

        private void Rect(decimal x, decimal top, decimal width, decimal height, bool fill, bool stroke)
        {
            current.Append(Format(x)).Append(' ').Append(Format(top - height)).Append(' ').Append(Format(width)).Append(' ').Append(Format(height)).AppendLine(" re");
            current.AppendLine(fill && stroke ? "B" : fill ? "f" : "S");
            Stroke(156, 163, 175);
        }

        private void Line(decimal x1, decimal y1, decimal x2, decimal y2, decimal width)
        {
            current.Append(Format(width)).AppendLine(" w");
            current.Append(Format(x1)).Append(' ').Append(Format(y1)).AppendLine(" m");
            current.Append(Format(x2)).Append(' ').Append(Format(y2)).AppendLine(" l");
            current.AppendLine("S");
        }

        private void Fill(int r, int g, int b)
        {
            current.Append(Format(r / 255m)).Append(' ').Append(Format(g / 255m)).Append(' ').Append(Format(b / 255m)).AppendLine(" rg");
        }

        private void Stroke(int r, int g, int b)
        {
            current.Append(Format(r / 255m)).Append(' ').Append(Format(g / 255m)).Append(' ').Append(Format(b / 255m)).AppendLine(" RG");
        }

        private static IReadOnlyList<string> Wrap(string value, decimal width, decimal size)
        {
            var maxChars = Math.Max(8, (int)Math.Floor(width / (size * .52m)));
            var lines = new List<string>();
            foreach (var source in (value ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
            {
                var words = source.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (words.Length == 0)
                {
                    lines.Add("");
                    continue;
                }

                var current = new StringBuilder();
                foreach (var word in words)
                {
                    var safe = ToPdfText(word);
                    if (current.Length > 0 && current.Length + safe.Length + 1 > maxChars)
                    {
                        lines.Add(current.ToString());
                        current.Clear();
                    }

                    if (safe.Length > maxChars)
                    {
                        if (current.Length > 0)
                        {
                            lines.Add(current.ToString());
                            current.Clear();
                        }

                        foreach (var chunk in safe.Chunk(maxChars))
                        {
                            lines.Add(new string(chunk));
                        }

                        continue;
                    }

                    if (current.Length > 0)
                    {
                        current.Append(' ');
                    }

                    current.Append(safe);
                }

                if (current.Length > 0)
                {
                    lines.Add(current.ToString());
                }
            }

            return lines;
        }

        private static IReadOnlyList<string> SplitSentences(string value)
        {
            return (value ?? "")
                .Split(". ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(sentence => sentence.EndsWith(".", StringComparison.Ordinal) ? sentence : $"{sentence}.")
                .ToArray();
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
            builder.Append("0 ").AppendLine((objects.Count + 1).ToString());
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

        private static decimal TextWidth(string value, decimal size)
        {
            return ToPdfText(value).Length * size * .52m;
        }

        private static string FontName(Font font)
        {
            return font switch
            {
                Font.Bold => "F2",
                Font.Serif => "F3",
                _ => "F1"
            };
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
