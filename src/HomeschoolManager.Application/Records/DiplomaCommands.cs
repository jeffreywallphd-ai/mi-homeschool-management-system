namespace HomeschoolManager.Application.Records;

public sealed record SaveGraduationPlanCommand(
    Guid StudentId,
    string Title,
    string StandardsSummary,
    bool StandardsAccepted,
    bool RequirementsSatisfiedOrWaived,
    string ParentDecisionNotes);

public sealed record SaveDiplomaDesignCommand(
    Guid StudentId,
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

public sealed record CreateDiplomaPdfCommand(Guid StudentId);
