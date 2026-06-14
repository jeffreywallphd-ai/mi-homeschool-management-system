namespace HomeschoolManager.Application.Records;

public sealed record DiplomaDesignerView(
    Guid StudentId,
    string StudentName,
    IReadOnlyList<DiplomaStudentOption> Students,
    GraduationPlanView GraduationPlan,
    DiplomaDesignView Design,
    DiplomaReadinessView Readiness,
    IReadOnlyList<string> BuiltInFontFamilies);

public sealed record DiplomaStudentOption(Guid StudentId, string Name, int GradeLevel);

public sealed record GraduationPlanView(
    Guid? GraduationPlanId,
    string Title,
    string StandardsSummary,
    bool StandardsAccepted,
    bool RequirementsSatisfiedOrWaived,
    string ParentDecisionNotes,
    string AcceptedBy,
    DateTimeOffset? AcceptedAtUtc);

public sealed record DiplomaDesignView(
    Guid? DiplomaDesignId,
    string TemplateId,
    string PageSize,
    string HomeschoolName,
    string CertifiesText,
    string StudentName,
    string CompletionText,
    string DiplomaTitle,
    string PrivilegesText,
    string AwardedText,
    DateOnly? AwardedDate,
    string SignatureLabel,
    string DateLabel,
    string SealText,
    IReadOnlyList<DiplomaTextStyleView> TextStyles);

public sealed record DiplomaTextStyleView(
    string ElementKey,
    string Label,
    string FontFamily,
    decimal FontSize,
    bool Uppercase,
    decimal LetterSpacing);

public sealed record DiplomaReadinessView(bool CanGenerate, IReadOnlyList<string> BlockingReasons);

public sealed record DiplomaDownloadFile(string FileName, string ContentType, byte[] Content);
