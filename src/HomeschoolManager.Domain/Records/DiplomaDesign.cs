using HomeschoolManager.Domain.Common;

namespace HomeschoolManager.Domain.Records;

public sealed record DiplomaDesign
{
    public Guid Id { get; init; }
    public Guid StudentId { get; init; }
    public Guid? GraduationPlanId { get; init; }
    public string TemplateId { get; init; }
    public string PageSize { get; init; }
    public string HomeschoolName { get; init; }
    public string CertifiesText { get; init; }
    public string StudentName { get; init; }
    public string CompletionText { get; init; }
    public string DiplomaTitle { get; init; }
    public string PrivilegesText { get; init; }
    public string AwardedText { get; init; }
    public DateOnly? AwardedDate { get; init; }
    public string SignatureLabel { get; init; }
    public string DateLabel { get; init; }
    public string SealText { get; init; }
    public IReadOnlyList<DiplomaTextStyle> TextStyles { get; init; }
    public DateTimeOffset UpdatedAtUtc { get; init; }

    public DiplomaDesign(
        Guid id,
        Guid studentId,
        Guid? graduationPlanId,
        string templateId,
        string pageSize,
        string homeschoolName,
        string certifiesText,
        string studentName,
        string completionText,
        string diplomaTitle,
        string privilegesText,
        string awardedText,
        DateOnly? awardedDate,
        string signatureLabel,
        string dateLabel,
        string sealText,
        IReadOnlyList<DiplomaTextStyle> textStyles,
        DateTimeOffset updatedAtUtc)
    {
        if (studentId == Guid.Empty)
        {
            throw new DomainException("Student is required for a diploma design.");
        }

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        StudentId = studentId;
        GraduationPlanId = graduationPlanId;
        TemplateId = Clean(templateId, "classic-formal");
        PageSize = Clean(pageSize, "letter-landscape");
        HomeschoolName = Clean(homeschoolName, "Family Homeschool");
        CertifiesText = Clean(certifiesText, "This certifies that");
        StudentName = Clean(studentName, "Student Learner");
        CompletionText = Clean(completionText, "has satisfactorily completed the parent-defined course of study, including selected Michigan subject-area records, and is therefore awarded this");
        DiplomaTitle = Clean(diplomaTitle, "High School Diploma");
        PrivilegesText = Clean(privilegesText, "with all of the rights, honors, and privileges pertaining thereto");
        AwardedText = Clean(awardedText, "Awarded on");
        AwardedDate = awardedDate;
        SignatureLabel = Clean(signatureLabel, "Parent / Administrator");
        DateLabel = Clean(dateLabel, "Date");
        SealText = Clean(sealText, "Family Issued");
        TextStyles = textStyles.Count == 0 ? DiplomaTextStyle.DefaultStyles : textStyles;
        UpdatedAtUtc = updatedAtUtc == default ? DateTimeOffset.UtcNow : updatedAtUtc;
    }

    private static string Clean(string value, string fallback = "")
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }
}

public sealed record DiplomaTextStyle
{
    public string ElementKey { get; init; }
    public string FontFamily { get; init; }
    public decimal FontSize { get; init; }
    public bool Uppercase { get; init; }
    public decimal LetterSpacing { get; init; }

    public DiplomaTextStyle(
        string elementKey,
        string fontFamily,
        decimal fontSize,
        bool uppercase,
        decimal letterSpacing)
    {
        if (string.IsNullOrWhiteSpace(elementKey))
        {
            throw new DomainException("Diploma text style element is required.");
        }

        if (fontSize is < 8 or > 96)
        {
            throw new DomainException("Diploma font sizes must be between 8 and 96 points.");
        }

        if (letterSpacing is < 0 or > 16)
        {
            throw new DomainException("Diploma letter spacing must be between 0 and 16 points.");
        }

        ElementKey = elementKey.Trim();
        FontFamily = string.IsNullOrWhiteSpace(fontFamily) ? "Georgia" : fontFamily.Trim();
        FontSize = fontSize;
        Uppercase = uppercase;
        LetterSpacing = letterSpacing;
    }

    public static IReadOnlyList<DiplomaTextStyle> DefaultStyles { get; } =
    [
        new("homeschoolName", "Georgia", 38, true, 4),
        new("certifiesText", "Georgia", 20, false, 0),
        new("studentName", "Georgia", 52, true, 8),
        new("completionText", "Georgia", 18, false, 0),
        new("diplomaTitle", "Georgia", 44, true, 6),
        new("privilegesText", "Georgia", 18, false, 0),
        new("awardedText", "Georgia", 20, false, 0),
        new("signatureLabel", "Georgia", 14, true, 1),
        new("dateLabel", "Georgia", 14, true, 1),
        new("sealText", "Georgia", 18, true, 1)
    ];
}
