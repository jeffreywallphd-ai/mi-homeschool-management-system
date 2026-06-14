using HomeschoolManager.Application.Access;
using HomeschoolManager.Application.Common;
using HomeschoolManager.Application.Persistence;
using HomeschoolManager.Domain.Access;
using HomeschoolManager.Domain.Common;
using HomeschoolManager.Domain.Household;
using HomeschoolManager.Domain.Records;
using HomeschoolManager.Domain.Students;

namespace HomeschoolManager.Application.Records;

public sealed class DiplomaService
{
    private const string ContentTypePdf = "application/pdf";

    private static readonly IReadOnlyDictionary<string, string> StyleLabels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["homeschoolName"] = "Homeschool name",
        ["certifiesText"] = "Certifies line",
        ["studentName"] = "Student name",
        ["completionText"] = "Completion statement",
        ["diplomaTitle"] = "Diploma title",
        ["privilegesText"] = "Rights and privileges line",
        ["awardedText"] = "Awarded line",
        ["signatureLabel"] = "Signature label",
        ["dateLabel"] = "Date label",
        ["sealText"] = "Seal text"
    };

    private static readonly string[] BuiltInFonts =
    [
        "Georgia",
        "Times New Roman",
        "Cambria",
        "Garamond",
        "Baskerville",
        "Palatino Linotype",
        "Segoe UI",
        "Arial",
        "Calibri",
        "Verdana",
        "Courier New"
    ];

    private readonly IHomeschoolRepository repository;

    public DiplomaService(IHomeschoolRepository repository)
    {
        this.repository = repository;
    }

    public async Task<OperationResult<DiplomaDesignerView>> GetDesignerAsync(
        UserContext user,
        Guid? studentId = null,
        CancellationToken cancellationToken = default)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return OperationResult<DiplomaDesignerView>.Failure(authorized.Errors.ToArray());
        }

        var students = await repository.GetStudentsAsync(cancellationToken);
        var student = studentId.HasValue
            ? students.FirstOrDefault(candidate => candidate.Id == studentId.Value)
            : students.FirstOrDefault();
        if (student is null)
        {
            return OperationResult<DiplomaDesignerView>.Failure("Student was not found.");
        }

        var profile = await repository.GetSchoolProfileAsync(cancellationToken);
        var plan = await repository.GetGraduationPlanAsync(student.Id, cancellationToken);
        var design = await repository.GetDiplomaDesignAsync(student.Id, cancellationToken) ?? DefaultDesign(student, profile, plan);
        var readiness = Readiness(plan, design);

        return OperationResult<DiplomaDesignerView>.Success(new DiplomaDesignerView(
            student.Id,
            StudentName(student),
            students.Select(candidate => new DiplomaStudentOption(candidate.Id, StudentName(candidate), candidate.GradeLevel)).ToArray(),
            ToPlanView(plan),
            ToDesignView(design),
            readiness,
            BuiltInFonts));
    }

    public async Task<OperationResult> SaveGraduationPlanAsync(
        UserContext user,
        SaveGraduationPlanCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return authorized;
        }

        var student = await repository.GetStudentAsync(command.StudentId, cancellationToken);
        if (student is null)
        {
            return OperationResult.Failure("Student was not found.");
        }

        try
        {
            var existing = await repository.GetGraduationPlanAsync(student.Id, cancellationToken);
            var now = DateTimeOffset.UtcNow;
            var plan = new GraduationPlan(
                existing?.Id ?? Guid.NewGuid(),
                student.Id,
                command.Title,
                command.StandardsSummary,
                command.StandardsAccepted,
                command.RequirementsSatisfiedOrWaived,
                command.ParentDecisionNotes,
                command.StandardsAccepted ? user.DisplayName : "",
                command.StandardsAccepted ? existing?.AcceptedAtUtc ?? now : null,
                now);
            await repository.SaveGraduationPlanAsync(plan, cancellationToken);
            return OperationResult.Success();
        }
        catch (DomainException ex)
        {
            return OperationResult.Failure(ex.Message);
        }
    }

    public async Task<OperationResult> SaveDiplomaDesignAsync(
        UserContext user,
        SaveDiplomaDesignCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return authorized;
        }

        var student = await repository.GetStudentAsync(command.StudentId, cancellationToken);
        if (student is null)
        {
            return OperationResult.Failure("Student was not found.");
        }

        var wording = ValidateWording(command);
        if (wording.Count > 0)
        {
            return OperationResult.Failure(wording.ToArray());
        }

        try
        {
            var existing = await repository.GetDiplomaDesignAsync(student.Id, cancellationToken);
            var plan = await repository.GetGraduationPlanAsync(student.Id, cancellationToken);
            var design = new DiplomaDesign(
                existing?.Id ?? Guid.NewGuid(),
                student.Id,
                plan?.Id,
                command.TemplateId,
                command.PageSize,
                command.HomeschoolName,
                command.CertifiesText,
                command.StudentName,
                command.CompletionText,
                command.DiplomaTitle,
                command.PrivilegesText,
                command.AwardedText,
                command.AwardedDate,
                command.SignatureLabel,
                command.DateLabel,
                command.SealText,
                command.TextStyles.Select(style => new DiplomaTextStyle(style.ElementKey, style.FontFamily, style.FontSize, style.Uppercase, style.LetterSpacing)).ToArray(),
                DateTimeOffset.UtcNow);
            await repository.SaveDiplomaDesignAsync(design, cancellationToken);
            return OperationResult.Success();
        }
        catch (DomainException ex)
        {
            return OperationResult.Failure(ex.Message);
        }
    }

    public async Task<OperationResult<DiplomaDownloadFile>> CreateDiplomaPdfAsync(
        UserContext user,
        CreateDiplomaPdfCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return OperationResult<DiplomaDownloadFile>.Failure(authorized.Errors.ToArray());
        }

        var student = await repository.GetStudentAsync(command.StudentId, cancellationToken);
        if (student is null)
        {
            return OperationResult<DiplomaDownloadFile>.Failure("Student was not found.");
        }

        var profile = await repository.GetSchoolProfileAsync(cancellationToken);
        var plan = await repository.GetGraduationPlanAsync(student.Id, cancellationToken);
        var design = await repository.GetDiplomaDesignAsync(student.Id, cancellationToken) ?? DefaultDesign(student, profile, plan);
        var readiness = Readiness(plan, design);
        if (!readiness.CanGenerate)
        {
            return OperationResult<DiplomaDownloadFile>.Failure(readiness.BlockingReasons.ToArray());
        }

        var wording = ValidateWording(ToCommand(design));
        if (wording.Count > 0)
        {
            return OperationResult<DiplomaDownloadFile>.Failure(wording.ToArray());
        }

        var pdf = DiplomaPdfRenderer.Create(ToDesignView(design));
        return OperationResult<DiplomaDownloadFile>.Success(new DiplomaDownloadFile(
            $"{SafeFileName(design.StudentName)}-diploma.pdf",
            ContentTypePdf,
            pdf));
    }

    private static DiplomaDesign DefaultDesign(Student student, SchoolProfile? profile, GraduationPlan? plan)
    {
        var studentName = StudentName(student);
        var schoolName = string.IsNullOrWhiteSpace(profile?.SchoolName) ? "Family Homeschool" : profile!.SchoolName;
        DateOnly? awardedDate = plan?.AcceptedAtUtc is null ? null : DateOnly.FromDateTime(plan.AcceptedAtUtc.Value.LocalDateTime);
        return new DiplomaDesign(
            Guid.NewGuid(),
            student.Id,
            plan?.Id,
            "classic-formal",
            "letter-landscape",
            schoolName,
            "This certifies that",
            studentName,
            "has satisfactorily completed the parent-defined course of study, including selected Michigan subject-area records, and is therefore awarded this",
            "High School Diploma",
            "with all of the rights, honors, and privileges pertaining thereto",
            "Awarded on",
            awardedDate,
            "Parent / Administrator",
            "Date",
            "Family Issued",
            DiplomaTextStyle.DefaultStyles,
            DateTimeOffset.UtcNow);
    }

    private static DiplomaReadinessView Readiness(GraduationPlan? plan, DiplomaDesign design)
    {
        var reasons = new List<string>();
        if (plan is null)
        {
            reasons.Add("Create and accept a parent-defined graduation plan before generating a diploma.");
        }
        else
        {
            if (!plan.StandardsAccepted)
            {
                reasons.Add("Accept the parent-defined graduation standards before generating a diploma.");
            }

            if (!plan.RequirementsSatisfiedOrWaived)
            {
                reasons.Add("Mark graduation requirements as satisfied or explicitly waived by the parent before generating a diploma.");
            }
        }

        if (!design.AwardedDate.HasValue)
        {
            reasons.Add("Add an awarded date before generating a diploma.");
        }

        if (string.IsNullOrWhiteSpace(design.SignatureLabel))
        {
            reasons.Add("Add a parent/admin signature label before generating a diploma.");
        }

        return new DiplomaReadinessView(reasons.Count == 0, reasons);
    }

    private static GraduationPlanView ToPlanView(GraduationPlan? plan)
    {
        return plan is null
            ? new GraduationPlanView(null, "Parent-Defined Graduation Plan", "", false, false, "", "", null)
            : new GraduationPlanView(plan.Id, plan.Title, plan.StandardsSummary, plan.StandardsAccepted, plan.RequirementsSatisfiedOrWaived, plan.ParentDecisionNotes, plan.AcceptedBy, plan.AcceptedAtUtc);
    }

    private static DiplomaDesignView ToDesignView(DiplomaDesign design)
    {
        return new DiplomaDesignView(
            design.Id,
            design.TemplateId,
            design.PageSize,
            design.HomeschoolName,
            design.CertifiesText,
            design.StudentName,
            design.CompletionText,
            design.DiplomaTitle,
            design.PrivilegesText,
            design.AwardedText,
            design.AwardedDate,
            design.SignatureLabel,
            design.DateLabel,
            design.SealText,
            StylesWithLabels(design.TextStyles));
    }

    private static IReadOnlyList<DiplomaTextStyleView> StylesWithLabels(IReadOnlyList<DiplomaTextStyle> styles)
    {
        return styles
            .Select(style => new DiplomaTextStyleView(
                style.ElementKey,
                StyleLabels.TryGetValue(style.ElementKey, out var label) ? label : style.ElementKey,
                style.FontFamily,
                style.FontSize,
                style.Uppercase,
                style.LetterSpacing))
            .ToArray();
    }

    private static SaveDiplomaDesignCommand ToCommand(DiplomaDesign design)
    {
        return new SaveDiplomaDesignCommand(
            design.StudentId,
            design.TemplateId,
            design.PageSize,
            design.HomeschoolName,
            design.CertifiesText,
            design.StudentName,
            design.CompletionText,
            design.DiplomaTitle,
            design.PrivilegesText,
            design.AwardedText,
            design.AwardedDate,
            design.SignatureLabel,
            design.DateLabel,
            design.SealText,
            StylesWithLabels(design.TextStyles));
    }

    private static IReadOnlyList<string> ValidateWording(SaveDiplomaDesignCommand command)
    {
        var text = string.Join(" ", [
            command.HomeschoolName,
            command.CertifiesText,
            command.StudentName,
            command.CompletionText,
            command.DiplomaTitle,
            command.PrivilegesText,
            command.AwardedText,
            command.SignatureLabel,
            command.DateLabel,
            command.SealText
        ]);
        var errors = new List<string>();
        var prohibited = new[]
        {
            "compliant with michigan law",
            "legally certified",
            "state-approved",
            "state approved",
            "mde-approved",
            "mde approved",
            "accredited",
            "official state record",
            "mde submission",
            "prescribed by michigan law"
        };
        foreach (var phrase in prohibited)
        {
            if (text.Contains(phrase, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"Diploma wording cannot include \"{phrase}\" because generated credentials must not imply state approval, accreditation, legal certification, or compliance.");
            }
        }

        return errors;
    }

    private static string StudentName(Student student)
    {
        return $"{student.FirstName} {student.LastName}".Trim();
    }

    private static string SafeFileName(string value)
    {
        var safe = new string((value ?? "")
            .Trim()
            .Select(character => char.IsAsciiLetterOrDigit(character) ? char.ToLowerInvariant(character) : '-')
            .ToArray());
        while (safe.Contains("--", StringComparison.Ordinal))
        {
            safe = safe.Replace("--", "-", StringComparison.Ordinal);
        }

        return string.IsNullOrWhiteSpace(safe.Trim('-')) ? "diploma" : safe.Trim('-');
    }
}
