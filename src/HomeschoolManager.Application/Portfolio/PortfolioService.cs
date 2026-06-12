using HomeschoolManager.Application.Access;
using HomeschoolManager.Application.Common;
using HomeschoolManager.Application.Persistence;
using HomeschoolManager.Domain.Access;
using HomeschoolManager.Domain.Curriculum;
using HomeschoolManager.Domain.Students;
using HomeschoolManager.Domain.Submissions;

namespace HomeschoolManager.Application.Portfolio;

public sealed class PortfolioService
{
    private readonly IHomeschoolRepository repository;

    public PortfolioService(IHomeschoolRepository repository)
    {
        this.repository = repository;
    }

    public async Task<OperationResult<StudentPortfolioWorkspace>> GetStudentWorkspaceAsync(
        UserContext user,
        Guid? studentId = null,
        CancellationToken cancellationToken = default)
    {
        var authorized = RequireStudentReadAccess(user);
        if (!authorized.Succeeded)
        {
            return OperationResult<StudentPortfolioWorkspace>.Failure(authorized.Errors.ToArray());
        }

        var student = await ResolveStudentAsync(studentId, cancellationToken);
        if (student is null)
        {
            return OperationResult<StudentPortfolioWorkspace>.Failure("Student was not found.");
        }

        var (design, context) = await LoadPreparedDesignAsync(student, cancellationToken);
        var draftItems = DraftItemsFor(student, design, context);
        var availableEvidence = EvidenceOptionsFor(student, design, context, draftItems);
        var assignmentSuggestions = AssignmentSuggestionsFor(student, design, context);
        var assignmentPlanOptions = AssignmentPlanOptionsFor(student, design, context);
        var preview = BuildPreview(student, design, draftItems);
        var latestApprovedPreview = BuildLatestApprovedPreview(student, design);

        return OperationResult<StudentPortfolioWorkspace>.Success(new StudentPortfolioWorkspace(
            student.Id,
            StudentName(student),
            student.GradeLevel,
            StudentAuthoringAllowed(student),
            ToDesignView(design, draftItems),
            draftItems,
            availableEvidence,
            assignmentSuggestions,
            assignmentPlanOptions,
            preview,
            latestApprovedPreview));
    }

    public async Task<OperationResult<PortfolioReviewWorkspace>> GetReviewWorkspaceAsync(
        UserContext user,
        Guid? studentId = null,
        CancellationToken cancellationToken = default)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return OperationResult<PortfolioReviewWorkspace>.Failure(authorized.Errors.ToArray());
        }

        var students = await repository.GetStudentsAsync(cancellationToken);
        var selectedStudent = ResolveStudent(students, studentId);
        if (selectedStudent is null)
        {
            return OperationResult<PortfolioReviewWorkspace>.Failure("Student was not found.");
        }

        var (design, context) = await LoadPreparedDesignAsync(selectedStudent, cancellationToken);
        var draftItems = DraftItemsFor(selectedStudent, design, context);
        var availableEvidence = EvidenceOptionsFor(selectedStudent, design, context, draftItems);
        var assignmentCandidates = context.EvidenceRecords
            .Where(evidence => evidence.StudentId == selectedStudent.Id)
            .Where(evidence => evidence.PortfolioCandidate)
            .Select(evidence => ToAssignmentCandidateView(evidence, selectedStudent, context))
            .Where(item => item is not null)
            .Select(item => item!)
            .ToArray();
        var assignmentSuggestions = AssignmentSuggestionsFor(selectedStudent, design, context);
        var assignmentPlanOptions = AssignmentPlanOptionsFor(selectedStudent, design, context);
        var preview = BuildPreview(selectedStudent, design, draftItems);
        var latestApprovedPreview = BuildLatestApprovedPreview(selectedStudent, design);

        return OperationResult<PortfolioReviewWorkspace>.Success(new PortfolioReviewWorkspace(
            selectedStudent.Id,
            StudentName(selectedStudent),
            selectedStudent.GradeLevel,
            StudentAuthoringAllowed(selectedStudent),
            students.Select(student => new PortfolioStudentOption(student.Id, StudentName(student), student.GradeLevel)).ToArray(),
            ToDesignView(design, draftItems),
            draftItems,
            assignmentCandidates,
            availableEvidence,
            assignmentSuggestions,
            assignmentPlanOptions,
            preview,
            latestApprovedPreview));
    }

    public async Task<OperationResult> UpdateDesignAsync(
        UserContext user,
        UpdatePortfolioDesignCommand command,
        CancellationToken cancellationToken = default)
    {
        var student = await ResolveStudentAsync(command.StudentId, cancellationToken);
        if (student is null)
        {
            return OperationResult.Failure("Student was not found.");
        }

        var authorized = RequirePortfolioAuthorAccess(user, student);
        if (!authorized.Succeeded)
        {
            return authorized;
        }

        if (string.IsNullOrWhiteSpace(command.Title))
        {
            return OperationResult.Failure("Add a portfolio title before saving.");
        }

        var (design, _) = await LoadPreparedDesignAsync(student, cancellationToken);
        var narratives = command.Narratives.Count == 0
            ? design.Narratives
            : command.Narratives
                .Select(item => new PortfolioNarrativeBlock(item.NarrativeId, item.Prompt, item.Response, item.SortOrder))
                .ToArray();

        await repository.SavePortfolioDesignAsync(TouchForEdit(design with
        {
            Title = Clean(command.Title),
            Purpose = Clean(command.Purpose),
            Narratives = narratives
        }), cancellationToken);

        return OperationResult.Success();
    }

    public async Task<OperationResult<Guid>> AddSectionAsync(
        UserContext user,
        AddPortfolioSectionCommand command,
        CancellationToken cancellationToken = default)
    {
        var student = await ResolveStudentAsync(command.StudentId, cancellationToken);
        if (student is null)
        {
            return OperationResult<Guid>.Failure("Student was not found.");
        }

        var authorized = RequirePortfolioAuthorAccess(user, student);
        if (!authorized.Succeeded)
        {
            return OperationResult<Guid>.Failure(authorized.Errors.ToArray());
        }

        if (string.IsNullOrWhiteSpace(command.Heading))
        {
            return OperationResult<Guid>.Failure("Add a section heading before saving.");
        }

        var (design, _) = await LoadPreparedDesignAsync(student, cancellationToken);
        var nextSortOrder = design.Sections.Select(section => section.SortOrder).DefaultIfEmpty(0).Max() + 1;
        var section = new PortfolioSection(
            Guid.NewGuid(),
            command.Heading,
            command.Introduction,
            nextSortOrder,
            true,
            PortfolioSectionStatus.NoSuggestions);

        await repository.SavePortfolioDesignAsync(TouchForEdit(design with
        {
            Sections = design.Sections.Append(section).ToArray()
        }), cancellationToken);

        return OperationResult<Guid>.Success(section.Id);
    }

    public async Task<OperationResult> UpdateSectionAsync(
        UserContext user,
        UpdatePortfolioSectionCommand command,
        CancellationToken cancellationToken = default)
    {
        var student = await ResolveStudentAsync(command.StudentId, cancellationToken);
        if (student is null)
        {
            return OperationResult.Failure("Student was not found.");
        }

        var authorized = RequirePortfolioAuthorAccess(user, student);
        if (!authorized.Succeeded)
        {
            return authorized;
        }

        if (string.IsNullOrWhiteSpace(command.Heading))
        {
            return OperationResult.Failure("Add a section heading before saving.");
        }

        var (design, _) = await LoadPreparedDesignAsync(student, cancellationToken);
        var section = design.Sections.FirstOrDefault(item => item.Id == command.SectionId);
        if (section is null)
        {
            return OperationResult.Failure("Portfolio section was not found.");
        }

        var updated = new PortfolioSection(
            section.Id,
            command.Heading,
            command.Introduction,
            Math.Max(0, command.SortOrder),
            command.IncludeInPortfolio,
            command.IncludeInPortfolio ? section.Status : PortfolioSectionStatus.Excluded);

        await repository.SavePortfolioDesignAsync(TouchForEdit(design with
        {
            Sections = design.Sections.Select(item => item.Id == section.Id ? updated : item).ToArray()
        }), cancellationToken);

        return OperationResult.Success();
    }

    public async Task<OperationResult<Guid>> AddDraftItemAsync(
        UserContext user,
        AddPortfolioDraftItemCommand command,
        CancellationToken cancellationToken = default)
    {
        var evidence = await FindEvidenceAsync(command.EvidenceRecordId, cancellationToken);
        if (evidence is null)
        {
            return OperationResult<Guid>.Failure("Evidence was not found.");
        }

        var student = await ResolveStudentAsync(command.StudentId ?? evidence.StudentId, cancellationToken);
        if (student is null || evidence.StudentId != student.Id)
        {
            return OperationResult<Guid>.Failure("Evidence was not found for this student.");
        }

        var authorized = RequirePortfolioAuthorAccess(user, student);
        if (!authorized.Succeeded)
        {
            return OperationResult<Guid>.Failure(authorized.Errors.ToArray());
        }

        var (design, context) = await LoadPreparedDesignAsync(student, cancellationToken);
        var existing = context.DraftItems.FirstOrDefault(item =>
            item.StudentId == student.Id &&
            item.PortfolioDesignId == design.Id &&
            item.EvidenceRecordId == evidence.Id);
        if (existing is not null)
        {
            return OperationResult<Guid>.Success(existing.Id);
        }

        var section = ResolveSection(design, command.PortfolioSectionId, null);
        if (section is null)
        {
            return OperationResult<Guid>.Failure("Add a portfolio section before adding evidence.");
        }

        var now = DateTimeOffset.UtcNow;
        var nextSortOrder = context.DraftItems
            .Where(item => item.StudentId == student.Id && item.PortfolioDesignId == design.Id && item.PortfolioSectionId == section.Id)
            .Select(item => item.SortOrder)
            .DefaultIfEmpty(0)
            .Max() + 1;
        var item = new PortfolioDraftItem(
            Guid.NewGuid(),
            student.Id,
            evidence.Id,
            evidence.Title,
            section.Heading,
            "",
            "",
            [],
            nextSortOrder,
            true,
            PortfolioDraftStatus.Draft,
            "",
            now,
            now,
            portfolioDesignId: design.Id,
            portfolioSectionId: section.Id);

        await repository.SavePortfolioDraftItemAsync(item, cancellationToken);
        await repository.SavePortfolioDesignAsync(TouchForEdit(design), cancellationToken);
        return OperationResult<Guid>.Success(item.Id);
    }

    public async Task<OperationResult> UpdateDraftItemAsync(
        UserContext user,
        UpdatePortfolioDraftItemCommand command,
        CancellationToken cancellationToken = default)
    {
        var item = await repository.GetPortfolioDraftItemAsync(command.PortfolioDraftItemId, cancellationToken);
        if (item is null)
        {
            return OperationResult.Failure("Portfolio item was not found.");
        }

        var student = await ResolveStudentAsync(item.StudentId, cancellationToken);
        if (student is null)
        {
            return OperationResult.Failure("Portfolio item was not found for this student.");
        }

        var authorized = RequirePortfolioAuthorAccess(user, student);
        if (!authorized.Succeeded)
        {
            return authorized;
        }

        if (user.Role == UserRole.Student &&
            item.Status is not (PortfolioDraftStatus.Draft or PortfolioDraftStatus.NeedsRevision))
        {
            return OperationResult.Failure("Only draft or revision-requested portfolio items can be edited by the student.");
        }

        var (design, _) = await LoadPreparedDesignAsync(student, cancellationToken);
        var section = ResolveSection(design, command.PortfolioSectionId, item.PortfolioSection);
        if (section is null)
        {
            return OperationResult.Failure("Choose a portfolio section before saving the item.");
        }

        await repository.SavePortfolioDraftItemAsync(item with
        {
            PortfolioDesignId = design.Id,
            PortfolioSectionId = section.Id,
            DisplayTitle = Clean(command.DisplayTitle),
            PortfolioSection = section.Heading,
            StudentReflection = Clean(command.StudentReflection),
            ChosenReason = Clean(command.ChosenReason),
            SkillsShown = CleanList(command.SkillsShown),
            SortOrder = Math.Max(0, command.SortOrder),
            IncludeInDraft = command.IncludeInDraft,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        }, cancellationToken);

        await repository.SavePortfolioDesignAsync(TouchForEdit(design), cancellationToken);
        return OperationResult.Success();
    }

    public async Task<OperationResult> SubmitDraftItemAsync(
        UserContext user,
        SubmitPortfolioDraftItemCommand command,
        CancellationToken cancellationToken = default)
    {
        if (user.Role != UserRole.Student)
        {
            return OperationResult.Failure("Only the true student portal can submit portfolio items for review.");
        }

        var item = await repository.GetPortfolioDraftItemAsync(command.PortfolioDraftItemId, cancellationToken);
        if (item is null)
        {
            return OperationResult.Failure("Portfolio item was not found.");
        }

        var student = await ResolveStudentAsync(item.StudentId, cancellationToken);
        if (student is null)
        {
            return OperationResult.Failure("Portfolio item was not found for this student.");
        }

        var authorized = RequirePortfolioAuthorAccess(user, student);
        if (!authorized.Succeeded)
        {
            return authorized;
        }

        if (item.Status is not (PortfolioDraftStatus.Draft or PortfolioDraftStatus.NeedsRevision))
        {
            return OperationResult.Failure("This portfolio item is not ready for student submission.");
        }

        if (string.IsNullOrWhiteSpace(item.StudentReflection) && string.IsNullOrWhiteSpace(item.ChosenReason))
        {
            return OperationResult.Failure("Add a reflection or reason before submitting the portfolio item for review.");
        }

        var now = DateTimeOffset.UtcNow;
        await repository.SavePortfolioDraftItemAsync(item with
        {
            Status = PortfolioDraftStatus.SubmittedForReview,
            SubmittedAtUtc = now,
            UpdatedAtUtc = now
        }, cancellationToken);

        return OperationResult.Success();
    }

    public async Task<OperationResult> SubmitDesignAsync(
        UserContext user,
        SubmitPortfolioDesignCommand command,
        CancellationToken cancellationToken = default)
    {
        var student = await ResolveStudentAsync(command.StudentId, cancellationToken);
        if (student is null)
        {
            return OperationResult.Failure("Student was not found.");
        }

        var authorized = RequirePortfolioAuthorAccess(user, student);
        if (!authorized.Succeeded)
        {
            return authorized;
        }

        var (design, context) = await LoadPreparedDesignAsync(student, cancellationToken);
        var draftItems = DraftItemsFor(student, design, context);
        var validation = ValidateDesignForSubmission(design, draftItems);
        if (!validation.Succeeded)
        {
            return validation;
        }

        var now = DateTimeOffset.UtcNow;
        foreach (var item in context.DraftItems.Where(item =>
            item.StudentId == student.Id &&
            item.PortfolioDesignId == design.Id &&
            item.IncludeInDraft &&
            item.Status is PortfolioDraftStatus.Draft or PortfolioDraftStatus.NeedsRevision))
        {
            await repository.SavePortfolioDraftItemAsync(item with
            {
                Status = PortfolioDraftStatus.SubmittedForReview,
                SubmittedAtUtc = now,
                UpdatedAtUtc = now
            }, cancellationToken);
        }

        await repository.SavePortfolioDesignAsync(design with
        {
            Status = PortfolioDesignStatus.SubmittedForReview,
            SubmittedAtUtc = now,
            UpdatedAtUtc = now
        }, cancellationToken);

        return OperationResult.Success();
    }

    public async Task<OperationResult> ReviewDraftItemAsync(
        UserContext user,
        ReviewPortfolioDraftItemCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return authorized;
        }

        if (command.Status is not (PortfolioDraftStatus.ParentApproved or PortfolioDraftStatus.NeedsRevision or PortfolioDraftStatus.Excluded))
        {
            return OperationResult.Failure("Choose approved, needs revision, or excluded for the portfolio review.");
        }

        if (command.Status is PortfolioDraftStatus.NeedsRevision or PortfolioDraftStatus.Excluded &&
            string.IsNullOrWhiteSpace(command.ParentReviewNotes))
        {
            return OperationResult.Failure("Add parent review notes before sending the item back or excluding it.");
        }

        var item = await repository.GetPortfolioDraftItemAsync(command.PortfolioDraftItemId, cancellationToken);
        if (item is null)
        {
            return OperationResult.Failure("Portfolio item was not found.");
        }

        if (item.Status == PortfolioDraftStatus.Draft)
        {
            return OperationResult.Failure("Only portfolio items submitted by the student can be reviewed.");
        }

        var now = DateTimeOffset.UtcNow;
        await repository.SavePortfolioDraftItemAsync(item with
        {
            Status = command.Status,
            ParentReviewNotes = Clean(command.ParentReviewNotes),
            ReviewedAtUtc = now,
            UpdatedAtUtc = now
        }, cancellationToken);

        return OperationResult.Success();
    }

    public async Task<OperationResult<Guid>> AcceptAssignmentCandidateAsync(
        UserContext user,
        AcceptPortfolioAssignmentCandidateCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return OperationResult<Guid>.Failure(authorized.Errors.ToArray());
        }

        var context = await LoadContextAsync(cancellationToken);
        var evidence = context.EvidenceRecords.FirstOrDefault(record => record.Id == command.EvidenceRecordId);
        if (evidence is null)
        {
            return OperationResult<Guid>.Failure("Accepted evidence was not found.");
        }

        if (!evidence.PortfolioCandidate)
        {
            return OperationResult<Guid>.Failure("Only accepted evidence marked as a portfolio candidate can be accepted as a portfolio item.");
        }

        var student = context.Students.FirstOrDefault(candidate => candidate.Id == evidence.StudentId);
        if (student is null)
        {
            return OperationResult<Guid>.Failure("Student was not found.");
        }

        if (ToAssignmentCandidateView(evidence, student, context) is null)
        {
            return OperationResult<Guid>.Failure("Assignment context was not found.");
        }

        var (design, preparedContext) = await LoadPreparedDesignAsync(student, cancellationToken);
        var existing = preparedContext.DraftItems.FirstOrDefault(item =>
            item.PortfolioDesignId == design.Id &&
            item.EvidenceRecordId == evidence.Id);
        if (existing is not null)
        {
            if (existing.Status == PortfolioDraftStatus.ParentApproved)
            {
                return OperationResult<Guid>.Success(existing.Id);
            }

            return OperationResult<Guid>.Failure("This evidence is already in the student's portfolio design. Review that item instead.");
        }

        var section = ResolveSection(design, command.PortfolioSectionId, null);
        if (section is null)
        {
            return OperationResult<Guid>.Failure("Add a portfolio section before accepting this evidence.");
        }

        var now = DateTimeOffset.UtcNow;
        var nextSortOrder = preparedContext.DraftItems
            .Where(item => item.StudentId == evidence.StudentId && item.PortfolioDesignId == design.Id && item.PortfolioSectionId == section.Id)
            .Select(item => item.SortOrder)
            .DefaultIfEmpty(0)
            .Max() + 1;
        var item = new PortfolioDraftItem(
            Guid.NewGuid(),
            evidence.StudentId,
            evidence.Id,
            evidence.Title,
            section.Heading,
            "",
            "",
            [],
            nextSortOrder,
            true,
            PortfolioDraftStatus.ParentApproved,
            Clean(command.ParentReviewNotes),
            now,
            now,
            reviewedAtUtc: now,
            portfolioDesignId: design.Id,
            portfolioSectionId: section.Id);

        await repository.SavePortfolioDraftItemAsync(item, cancellationToken);
        await repository.SavePortfolioDesignAsync(TouchForEdit(design), cancellationToken);
        return OperationResult<Guid>.Success(item.Id);
    }

    public async Task<OperationResult<Guid>> SuggestAssignmentAsync(
        UserContext user,
        SuggestPortfolioAssignmentCommand command,
        CancellationToken cancellationToken = default)
    {
        var student = await ResolveStudentAsync(command.StudentId, cancellationToken);
        if (student is null)
        {
            return OperationResult<Guid>.Failure("Student was not found.");
        }

        var authorized = RequirePortfolioAuthorAccess(user, student);
        if (!authorized.Succeeded)
        {
            return OperationResult<Guid>.Failure(authorized.Errors.ToArray());
        }

        var (design, context) = await LoadPreparedDesignAsync(student, cancellationToken);
        var assignment = ResolveAssignment(command.CourseId, command.ModuleId, command.AssignmentId, student.Id, context);
        if (assignment is null)
        {
            return OperationResult<Guid>.Failure("Assignment was not found for this student.");
        }

        var existing = design.AssignmentSuggestions.FirstOrDefault(item =>
            item.CourseId == command.CourseId &&
            item.ModuleId == command.ModuleId &&
            item.AssignmentId == command.AssignmentId);
        if (existing is not null)
        {
            return OperationResult<Guid>.Success(existing.Id);
        }

        var now = DateTimeOffset.UtcNow;
        var suggestion = new PortfolioAssignmentSuggestion(
            Guid.NewGuid(),
            command.CourseId,
            command.ModuleId,
            command.AssignmentId,
            command.StudentReason,
            "",
            PortfolioAssignmentSuggestionStatus.Suggested,
            now,
            now);

        await repository.SavePortfolioDesignAsync(TouchForEdit(design with
        {
            AssignmentSuggestions = design.AssignmentSuggestions.Append(suggestion).ToArray()
        }), cancellationToken);

        return OperationResult<Guid>.Success(suggestion.Id);
    }

    public async Task<OperationResult<Guid>> AddSuggestionAsync(
        UserContext user,
        AddPortfolioSuggestionCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return OperationResult<Guid>.Failure(authorized.Errors.ToArray());
        }

        if (string.IsNullOrWhiteSpace(command.SuggestionText))
        {
            return OperationResult<Guid>.Failure("Add a suggestion before saving.");
        }

        var student = await ResolveStudentAsync(command.StudentId, cancellationToken);
        if (student is null)
        {
            return OperationResult<Guid>.Failure("Student was not found.");
        }

        var (design, _) = await LoadPreparedDesignAsync(student, cancellationToken);
        var suggestion = new PortfolioSuggestion(
            Guid.NewGuid(),
            command.TargetType,
            command.TargetId,
            command.SuggestionText,
            user.DisplayName,
            PortfolioSuggestionStatus.Open,
            DateTimeOffset.UtcNow);

        var sections = design.Sections;
        if (command.TargetType == PortfolioSuggestionTargetType.Section && command.TargetId.HasValue)
        {
            sections = design.Sections
                .Select(section => section.Id == command.TargetId.Value
                    ? section with { Status = PortfolioSectionStatus.SuggestionsOpen }
                    : section)
                .ToArray();
        }

        await repository.SavePortfolioDesignAsync(TouchForEdit(design with
        {
            Sections = sections,
            Suggestions = design.Suggestions.Append(suggestion).ToArray()
        }), cancellationToken);

        return OperationResult<Guid>.Success(suggestion.Id);
    }

    public async Task<OperationResult> ResolveSuggestionAsync(
        UserContext user,
        ResolvePortfolioSuggestionCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return authorized;
        }

        var student = await ResolveStudentAsync(command.StudentId, cancellationToken);
        if (student is null)
        {
            return OperationResult.Failure("Student was not found.");
        }

        var (design, _) = await LoadPreparedDesignAsync(student, cancellationToken);
        var suggestion = design.Suggestions.FirstOrDefault(item => item.Id == command.SuggestionId);
        if (suggestion is null)
        {
            return OperationResult.Failure("Suggestion was not found.");
        }

        var resolved = suggestion with
        {
            Status = PortfolioSuggestionStatus.Resolved,
            ResolvedAtUtc = DateTimeOffset.UtcNow
        };
        var suggestions = design.Suggestions
            .Select(item => item.Id == resolved.Id ? resolved : item)
            .ToArray();
        var sections = design.Sections
            .Select(section => SectionWithSuggestionStatus(section, suggestions))
            .ToArray();

        await repository.SavePortfolioDesignAsync(design with
        {
            Sections = sections,
            Suggestions = suggestions,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        }, cancellationToken);

        return OperationResult.Success();
    }

    public async Task<OperationResult> RequestRevisionAsync(
        UserContext user,
        RequestPortfolioRevisionCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return authorized;
        }

        if (string.IsNullOrWhiteSpace(command.ParentReviewNotes))
        {
            return OperationResult.Failure("Add revision guidance before requesting changes.");
        }

        var student = await ResolveStudentAsync(command.StudentId, cancellationToken);
        if (student is null)
        {
            return OperationResult.Failure("Student was not found.");
        }

        var (design, _) = await LoadPreparedDesignAsync(student, cancellationToken);
        var suggestion = new PortfolioSuggestion(
            Guid.NewGuid(),
            PortfolioSuggestionTargetType.Portfolio,
            null,
            command.ParentReviewNotes,
            user.DisplayName,
            PortfolioSuggestionStatus.Open,
            DateTimeOffset.UtcNow);

        await repository.SavePortfolioDesignAsync(design with
        {
            Status = PortfolioDesignStatus.NeedsRevision,
            Suggestions = design.Suggestions.Append(suggestion).ToArray(),
            ReviewedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        }, cancellationToken);

        return OperationResult.Success();
    }

    public async Task<OperationResult<Guid>> ApproveDesignAsync(
        UserContext user,
        ApprovePortfolioDesignCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorized = AuthorizationGuard.RequireParentAdmin(user);
        if (!authorized.Succeeded)
        {
            return OperationResult<Guid>.Failure(authorized.Errors.ToArray());
        }

        var student = await ResolveStudentAsync(command.StudentId, cancellationToken);
        if (student is null)
        {
            return OperationResult<Guid>.Failure("Student was not found.");
        }

        var (design, context) = await LoadPreparedDesignAsync(student, cancellationToken);
        var draftItems = DraftItemsFor(student, design, context);
        var validation = ValidateDesignForApproval(design, draftItems);
        if (!validation.Succeeded)
        {
            return OperationResult<Guid>.Failure(validation.Errors.ToArray());
        }

        var now = DateTimeOffset.UtcNow;
        foreach (var item in context.DraftItems.Where(item =>
            item.StudentId == student.Id &&
            item.PortfolioDesignId == design.Id &&
            item.IncludeInDraft &&
            item.Status != PortfolioDraftStatus.Excluded))
        {
            await repository.SavePortfolioDraftItemAsync(item with
            {
                Status = PortfolioDraftStatus.ParentApproved,
                ReviewedAtUtc = now,
                UpdatedAtUtc = now
            }, cancellationToken);
        }

        context = await LoadContextAsync(cancellationToken);
        draftItems = DraftItemsFor(student, design, context);
        var snapshot = BuildApprovalSnapshot(user, design, draftItems, now);

        await repository.SavePortfolioDesignAsync(design with
        {
            Status = PortfolioDesignStatus.Approved,
            ApprovalSnapshots = design.ApprovalSnapshots.Append(snapshot).ToArray(),
            ReviewedAtUtc = now,
            UpdatedAtUtc = now
        }, cancellationToken);

        return OperationResult<Guid>.Success(snapshot.Id);
    }

    private async Task<(PortfolioDesign Design, PortfolioContext Context)> LoadPreparedDesignAsync(
        Student student,
        CancellationToken cancellationToken)
    {
        var context = await LoadContextAsync(cancellationToken);
        var design = await EnsurePortfolioDesignAsync(student, context, cancellationToken);
        context = await LoadContextAsync(cancellationToken);
        design = context.Designs.FirstOrDefault(item => item.Id == design.Id) ?? design;
        await EnsureDraftItemsHaveDesignAsync(student, design, context, cancellationToken);
        context = await LoadContextAsync(cancellationToken);
        design = context.Designs.FirstOrDefault(item => item.Id == design.Id) ?? design;
        return (design, context);
    }

    private async Task<PortfolioDesign> EnsurePortfolioDesignAsync(
        Student student,
        PortfolioContext context,
        CancellationToken cancellationToken)
    {
        var design = context.Designs
            .Where(item => item.StudentId == student.Id)
            .OrderByDescending(item => item.UpdatedAtUtc)
            .FirstOrDefault();
        if (design is null)
        {
            design = StarterDesign(student);
            await repository.SavePortfolioDesignAsync(design, cancellationToken);
            return design;
        }

        var changed = false;
        var narratives = design.Narratives;
        if (narratives.Count == 0)
        {
            narratives = StarterNarratives();
            changed = true;
        }

        var sections = design.Sections;
        if (sections.Count == 0)
        {
            sections = StarterSections(student);
            changed = true;
        }

        if (!changed)
        {
            return design;
        }

        var updated = design with
        {
            Narratives = narratives,
            Sections = sections,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };
        await repository.SavePortfolioDesignAsync(updated, cancellationToken);
        return updated;
    }

    private async Task EnsureDraftItemsHaveDesignAsync(
        Student student,
        PortfolioDesign design,
        PortfolioContext context,
        CancellationToken cancellationToken)
    {
        foreach (var item in context.DraftItems.Where(item => item.StudentId == student.Id))
        {
            if (item.PortfolioDesignId != Guid.Empty && item.PortfolioSectionId != Guid.Empty)
            {
                continue;
            }

            var section = ResolveSection(design, null, item.PortfolioSection);
            if (section is null)
            {
                continue;
            }

            await repository.SavePortfolioDraftItemAsync(item with
            {
                PortfolioDesignId = design.Id,
                PortfolioSectionId = section.Id,
                PortfolioSection = string.IsNullOrWhiteSpace(item.PortfolioSection) ? section.Heading : item.PortfolioSection
            }, cancellationToken);
        }
    }

    private async Task<PortfolioContext> LoadContextAsync(CancellationToken cancellationToken)
    {
        return new PortfolioContext(
            await repository.GetStudentsAsync(cancellationToken),
            await repository.GetCoursesAsync(cancellationToken),
            await repository.GetAssignmentSubmissionsAsync(cancellationToken),
            await repository.GetEvidenceRecordsAsync(cancellationToken),
            await repository.GetPortfolioDraftItemsAsync(cancellationToken),
            await repository.GetPortfolioDesignsAsync(cancellationToken));
    }

    private async Task<Student?> ResolveStudentAsync(Guid? studentId, CancellationToken cancellationToken)
    {
        return studentId.HasValue
            ? await repository.GetStudentAsync(studentId.Value, cancellationToken)
            : await repository.GetStudentAsync(cancellationToken);
    }

    private static Student? ResolveStudent(IReadOnlyList<Student> students, Guid? studentId)
    {
        return studentId.HasValue
            ? students.FirstOrDefault(student => student.Id == studentId.Value)
            : students.FirstOrDefault();
    }

    private async Task<EvidenceRecord?> FindEvidenceAsync(Guid evidenceRecordId, CancellationToken cancellationToken)
    {
        var evidenceRecords = await repository.GetEvidenceRecordsAsync(cancellationToken);
        return evidenceRecords.FirstOrDefault(evidence => evidence.Id == evidenceRecordId);
    }

    private static OperationResult RequireStudentReadAccess(UserContext user)
    {
        return user.Role is UserRole.Student or UserRole.ParentAdmin
            ? OperationResult.Success()
            : OperationResult.Failure("Sign in to view portfolio information.");
    }

    private static OperationResult RequirePortfolioAuthorAccess(UserContext user, Student student)
    {
        if (user.Role == UserRole.ParentAdmin)
        {
            return OperationResult.Success();
        }

        if (user.Role == UserRole.Student && StudentAuthoringAllowed(student))
        {
            return OperationResult.Success();
        }

        if (user.Role == UserRole.Student)
        {
            return OperationResult.Failure("For K-5 students, the parent/admin manages portfolio editing.");
        }

        return OperationResult.Failure("Sign in to edit the portfolio.");
    }

    private static bool StudentAuthoringAllowed(Student student)
    {
        return student.GradeLevel >= 6;
    }

    private static PortfolioDesign StarterDesign(Student student)
    {
        var now = DateTimeOffset.UtcNow;
        return new PortfolioDesign(
            Guid.NewGuid(),
            student.Id,
            StarterTitle(student),
            "A reviewed collection of selected learning evidence.",
            PortfolioDesignStatus.Working,
            1,
            StarterNarratives(),
            StarterSections(student),
            [],
            [],
            [],
            now,
            now);
    }

    private static IReadOnlyList<PortfolioNarrativeBlock> StarterNarratives()
    {
        return
        [
            new PortfolioNarrativeBlock(Guid.NewGuid(), "This portfolio shows the student's...", "", 1)
        ];
    }

    private static IReadOnlyList<PortfolioSection> StarterSections(Student student)
    {
        var sections = student.GradeLevel >= 9
            ? new[] { "Academic Work", "Projects", "Writing", "Practical Skills", "Reflections" }
            : student.GradeLevel >= 6
                ? ["Academic Work", "Projects", "Writing", "Reflections"]
                : ["Learning Samples", "Projects", "Reading and Writing", "Reflections"];

        return sections
            .Select((heading, index) => new PortfolioSection(
                Guid.NewGuid(),
                heading,
                "",
                index + 1,
                true,
                PortfolioSectionStatus.NoSuggestions))
            .ToArray();
    }

    private static string StarterTitle(Student student)
    {
        return student.GradeLevel >= 9
            ? "High School Portfolio"
            : student.GradeLevel >= 6
                ? "Middle School Portfolio"
                : "Elementary Portfolio";
    }

    private static PortfolioDesign TouchForEdit(PortfolioDesign design)
    {
        var wasApproved = design.Status == PortfolioDesignStatus.Approved;
        return design with
        {
            Status = PortfolioDesignStatus.Working,
            Version = wasApproved ? design.Version + 1 : design.Version,
            SubmittedAtUtc = wasApproved ? null : design.SubmittedAtUtc,
            ReviewedAtUtc = wasApproved ? null : design.ReviewedAtUtc,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };
    }

    private static PortfolioSection? ResolveSection(
        PortfolioDesign design,
        Guid? sectionId,
        string? headingFallback)
    {
        if (sectionId.HasValue && sectionId.Value != Guid.Empty)
        {
            var selected = design.Sections.FirstOrDefault(section =>
                section.Id == sectionId.Value &&
                section.IncludeInPortfolio);
            if (selected is not null)
            {
                return selected;
            }
        }

        if (!string.IsNullOrWhiteSpace(headingFallback))
        {
            var byHeading = design.Sections.FirstOrDefault(section =>
                section.IncludeInPortfolio &&
                string.Equals(section.Heading, headingFallback.Trim(), StringComparison.OrdinalIgnoreCase));
            if (byHeading is not null)
            {
                return byHeading;
            }
        }

        return design.Sections
            .Where(section => section.IncludeInPortfolio)
            .OrderBy(section => section.SortOrder)
            .FirstOrDefault();
    }

    private static AssignmentContext? ResolveAssignment(
        Guid courseId,
        Guid moduleId,
        Guid assignmentId,
        Guid studentId,
        PortfolioContext context)
    {
        var course = context.Courses.FirstOrDefault(candidate => candidate.Id == courseId && candidate.StudentId == studentId && !candidate.IsArchived);
        var module = course?.Modules.FirstOrDefault(candidate => candidate.Id == moduleId);
        var assignment = module?.Assignments.FirstOrDefault(candidate => candidate.Id == assignmentId);
        return course is null || module is null || assignment is null
            ? null
            : new AssignmentContext(course, module, assignment);
    }

    private static IReadOnlyList<PortfolioDraftItemView> DraftItemsFor(
        Student student,
        PortfolioDesign design,
        PortfolioContext context)
    {
        return context.DraftItems
            .Where(item => item.StudentId == student.Id)
            .Where(item => item.PortfolioDesignId == design.Id || item.PortfolioDesignId == Guid.Empty)
            .Select(item => ToDraftView(item, student, design, context))
            .Where(item => item is not null)
            .Select(item => item!)
            .OrderBy(item => item.PortfolioSection)
            .ThenBy(item => item.SortOrder)
            .ThenBy(item => item.DisplayTitle)
            .ToArray();
    }

    private static IReadOnlyList<PortfolioEvidenceOptionView> EvidenceOptionsFor(
        Student student,
        PortfolioDesign design,
        PortfolioContext context,
        IReadOnlyList<PortfolioDraftItemView> draftItems)
    {
        return context.EvidenceRecords
            .Where(evidence => evidence.StudentId == student.Id)
            .Select(evidence => ToEvidenceOption(evidence, student, context, draftItems))
            .Where(item => item is not null)
            .Select(item => item!)
            .OrderByDescending(item => item.PortfolioCandidate)
            .ThenByDescending(item => item.ConfirmedAtUtc)
            .ToArray();
    }

    private static IReadOnlyList<PortfolioAssignmentSuggestionView> AssignmentSuggestionsFor(
        Student student,
        PortfolioDesign design,
        PortfolioContext context)
    {
        return design.AssignmentSuggestions
            .Select(suggestion =>
            {
                var assignment = ResolveAssignment(suggestion.CourseId, suggestion.ModuleId, suggestion.AssignmentId, student.Id, context);
                if (assignment is null)
                {
                    return null;
                }

                var acceptedEvidenceExists = context.EvidenceRecords.Any(evidence =>
                    evidence.StudentId == student.Id &&
                    evidence.CourseId == suggestion.CourseId &&
                    evidence.ModuleId == suggestion.ModuleId &&
                    evidence.AssignmentId == suggestion.AssignmentId);
                return new PortfolioAssignmentSuggestionView(
                    suggestion.Id,
                    student.Id,
                    StudentName(student),
                    assignment.Course.Id,
                    assignment.Course.Title,
                    assignment.Module.Id,
                    assignment.Module.Title,
                    assignment.Assignment.Id,
                    assignment.Assignment.Title,
                    suggestion.StudentReason,
                    suggestion.ParentNotes,
                    acceptedEvidenceExists ? PortfolioAssignmentSuggestionStatus.EvidenceAccepted : suggestion.Status,
                    acceptedEvidenceExists,
                    suggestion.CreatedAtUtc,
                    suggestion.UpdatedAtUtc);
            })
            .Where(item => item is not null)
            .Select(item => item!)
            .ToArray();
    }

    private static IReadOnlyList<PortfolioAssignmentPlanOptionView> AssignmentPlanOptionsFor(
        Student student,
        PortfolioDesign design,
        PortfolioContext context)
    {
        var suggestions = design.AssignmentSuggestions
            .Select(item => (item.CourseId, item.ModuleId, item.AssignmentId))
            .ToHashSet();

        return context.Courses
            .Where(course => course.StudentId == student.Id && !course.IsArchived)
            .SelectMany(course => course.Modules.SelectMany(module => module.Assignments.Select(assignment =>
            {
                var acceptedEvidenceExists = context.EvidenceRecords.Any(evidence =>
                    evidence.StudentId == student.Id &&
                    evidence.CourseId == course.Id &&
                    evidence.ModuleId == module.Id &&
                    evidence.AssignmentId == assignment.Id);
                return new PortfolioAssignmentPlanOptionView(
                    course.Id,
                    course.Title,
                    module.Id,
                    module.Title,
                    assignment.Id,
                    assignment.Title,
                    assignment.RequiredOutput,
                    suggestions.Contains((course.Id, module.Id, assignment.Id)),
                    acceptedEvidenceExists);
            })))
            .OrderBy(item => item.AcceptedEvidenceExists)
            .ThenBy(item => item.CourseTitle)
            .ThenBy(item => item.ModuleTitle)
            .ThenBy(item => item.AssignmentTitle)
            .ToArray();
    }

    private static PortfolioDraftItemView? ToDraftView(
        PortfolioDraftItem item,
        Student student,
        PortfolioDesign design,
        PortfolioContext context)
    {
        var evidence = context.EvidenceRecords.FirstOrDefault(record => record.Id == item.EvidenceRecordId);
        if (evidence is null)
        {
            return null;
        }

        var assignment = ResolveAssignment(evidence.CourseId, evidence.ModuleId, evidence.AssignmentId, student.Id, context);
        if (assignment is null)
        {
            return null;
        }

        var section = ResolveSection(design, item.PortfolioSectionId, item.PortfolioSection);
        var sectionHeading = section?.Heading ?? item.PortfolioSection;
        var sectionId = section?.Id ?? Guid.Empty;
        return new PortfolioDraftItemView(
            item.Id,
            item.StudentId,
            StudentName(student),
            design.Id,
            sectionId,
            item.EvidenceRecordId,
            evidence.SubmissionId,
            assignment.Course.Id,
            assignment.Course.Title,
            assignment.Module.Id,
            assignment.Module.Title,
            assignment.Assignment.Id,
            assignment.Assignment.Title,
            item.DisplayTitle,
            sectionHeading,
            item.StudentReflection,
            item.ChosenReason,
            item.SkillsShown,
            item.SortOrder,
            item.IncludeInDraft,
            item.Status,
            item.ParentReviewNotes,
            item.UpdatedAtUtc,
            item.SubmittedAtUtc,
            item.ReviewedAtUtc,
            evidence.PortfolioCandidate,
            evidence.StoredFileIds.Count,
            OpenSuggestionCount(design, PortfolioSuggestionTargetType.Item, item.Id));
    }

    private static PortfolioEvidenceOptionView? ToEvidenceOption(
        EvidenceRecord evidence,
        Student student,
        PortfolioContext context,
        IReadOnlyList<PortfolioDraftItemView> draftItems)
    {
        var assignment = ResolveAssignment(evidence.CourseId, evidence.ModuleId, evidence.AssignmentId, student.Id, context);
        if (assignment is null)
        {
            return null;
        }

        return new PortfolioEvidenceOptionView(
            evidence.Id,
            evidence.StudentId,
            StudentName(student),
            assignment.Course.Id,
            assignment.Course.Title,
            assignment.Module.Id,
            assignment.Module.Title,
            assignment.Assignment.Id,
            assignment.Assignment.Title,
            evidence.Title,
            evidence.Description,
            evidence.ConfirmedAtUtc,
            evidence.PortfolioCandidate,
            evidence.StoredFileIds.Count,
            draftItems.Any(item => item.EvidenceRecordId == evidence.Id));
    }

    private static PortfolioAssignmentCandidateView? ToAssignmentCandidateView(
        EvidenceRecord evidence,
        Student student,
        PortfolioContext context)
    {
        var assignment = ResolveAssignment(evidence.CourseId, evidence.ModuleId, evidence.AssignmentId, student.Id, context);
        var submission = context.Submissions.FirstOrDefault(candidate => candidate.Id == evidence.SubmissionId);
        if (assignment is null || submission is null)
        {
            return null;
        }

        var draftItem = context.DraftItems.FirstOrDefault(item => item.EvidenceRecordId == evidence.Id);
        return new PortfolioAssignmentCandidateView(
            evidence.Id,
            evidence.StudentId,
            StudentName(student),
            assignment.Course.Id,
            assignment.Course.Title,
            assignment.Module.Id,
            assignment.Module.Title,
            assignment.Assignment.Id,
            assignment.Assignment.Title,
            evidence.SubmissionId,
            evidence.Title,
            evidence.Description,
            submission.StudentNotes,
            evidence.ParentNotes,
            evidence.ConfirmedAtUtc,
            submission.StudentPortfolioCandidate,
            evidence.StoredFileIds.Count,
            draftItem?.Id,
            draftItem?.Status);
    }

    private static PortfolioDesignView ToDesignView(
        PortfolioDesign design,
        IReadOnlyList<PortfolioDraftItemView> draftItems)
    {
        return new PortfolioDesignView(
            design.Id,
            design.StudentId,
            design.Title,
            design.Purpose,
            design.Status,
            design.Version,
            design.Narratives
                .OrderBy(item => item.SortOrder)
                .Select(item => new PortfolioNarrativeBlockView(
                    item.Id,
                    item.Prompt,
                    item.Response,
                    item.SortOrder,
                    OpenSuggestionCount(design, PortfolioSuggestionTargetType.Narrative, item.Id)))
                .ToArray(),
            design.Sections
                .OrderBy(item => item.SortOrder)
                .Select(item => new PortfolioSectionView(
                    item.Id,
                    item.Heading,
                    item.Introduction,
                    item.SortOrder,
                    item.IncludeInPortfolio,
                    item.Status,
                    draftItems.Count(draft => draft.PortfolioSectionId == item.Id),
                    OpenSuggestionCount(design, PortfolioSuggestionTargetType.Section, item.Id)))
                .ToArray(),
            design.Suggestions.Select(ToSuggestionView).ToArray(),
            design.ApprovalSnapshots.Select(ToApprovalSnapshotView).ToArray(),
            design.UpdatedAtUtc,
            design.SubmittedAtUtc,
            design.ReviewedAtUtc);
    }

    private static PortfolioSuggestionView ToSuggestionView(PortfolioSuggestion suggestion)
    {
        return new PortfolioSuggestionView(
            suggestion.Id,
            suggestion.TargetType,
            suggestion.TargetId,
            suggestion.SuggestionText,
            suggestion.AuthorDisplayName,
            suggestion.Status,
            suggestion.CreatedAtUtc,
            suggestion.ResolvedAtUtc);
    }

    private static PortfolioApprovalSnapshotView ToApprovalSnapshotView(PortfolioApprovalSnapshot snapshot)
    {
        return new PortfolioApprovalSnapshotView(
            snapshot.Id,
            snapshot.Version,
            snapshot.Title,
            snapshot.ApprovedAtUtc,
            snapshot.ApprovedBy,
            snapshot.ExportManifestName,
            snapshot.Sections.Count,
            snapshot.Sections.Sum(section => section.Items.Count));
    }

    private static PortfolioPreviewView BuildPreview(
        Student student,
        PortfolioDesign design,
        IReadOnlyList<PortfolioDraftItemView> draftItems)
    {
        var includedItems = draftItems
            .Where(item => item.IncludeInDraft)
            .Where(item => item.Status != PortfolioDraftStatus.Excluded)
            .ToArray();
        var sections = design.Sections
            .Where(section => section.IncludeInPortfolio)
            .OrderBy(section => section.SortOrder)
            .Select(section => new PortfolioPreviewSectionView(
                section.Id,
                section.Heading,
                section.Introduction,
                section.SortOrder,
                includedItems
                    .Where(item => item.PortfolioSectionId == section.Id)
                    .OrderBy(item => item.SortOrder)
                    .Select(item => new PortfolioPreviewItemView(
                        item.PortfolioDraftItemId,
                        item.EvidenceRecordId,
                        item.DisplayTitle,
                        item.CourseTitle,
                        item.ModuleTitle,
                        item.AssignmentTitle,
                        item.StudentReflection,
                        item.ChosenReason,
                        item.SkillsShown,
                        item.ParentReviewNotes,
                        item.FileCount,
                        item.SortOrder))
                    .ToArray()))
            .ToArray();

        return new PortfolioPreviewView(
            student.Id,
            StudentName(student),
            design.Title,
            design.Purpose,
            design.Status,
            design.Version,
            design.Status == PortfolioDesignStatus.Approved ? design.ApprovalSnapshots.FirstOrDefault()?.ApprovedAtUtc : null,
            design.Narratives
                .OrderBy(item => item.SortOrder)
                .Select(item => new PortfolioPreviewNarrativeView(item.Prompt, item.Response, item.SortOrder))
                .ToArray(),
            sections);
    }

    private static PortfolioPreviewView? BuildLatestApprovedPreview(Student student, PortfolioDesign design)
    {
        var snapshot = design.ApprovalSnapshots
            .OrderByDescending(item => item.ApprovedAtUtc)
            .FirstOrDefault();
        if (snapshot is null)
        {
            return null;
        }

        return new PortfolioPreviewView(
            student.Id,
            StudentName(student),
            snapshot.Title,
            snapshot.Purpose,
            PortfolioDesignStatus.Approved,
            snapshot.Version,
            snapshot.ApprovedAtUtc,
            snapshot.Narratives
                .OrderBy(item => item.SortOrder)
                .Select(item => new PortfolioPreviewNarrativeView(item.Prompt, item.Response, item.SortOrder))
                .ToArray(),
            snapshot.Sections
                .OrderBy(item => item.SortOrder)
                .Select(section => new PortfolioPreviewSectionView(
                    section.SectionId,
                    section.Heading,
                    section.Introduction,
                    section.SortOrder,
                    section.Items
                        .OrderBy(item => item.SortOrder)
                        .Select(item => new PortfolioPreviewItemView(
                            item.PortfolioDraftItemId,
                            item.EvidenceRecordId,
                            item.DisplayTitle,
                            item.CourseTitle,
                            item.ModuleTitle,
                            item.AssignmentTitle,
                            item.StudentReflection,
                            item.ChosenReason,
                            item.SkillsShown,
                            item.ParentReviewNotes,
                            item.FileCount,
                            item.SortOrder))
                        .ToArray()))
                .ToArray());
    }

    private static PortfolioApprovalSnapshot BuildApprovalSnapshot(
        UserContext user,
        PortfolioDesign design,
        IReadOnlyList<PortfolioDraftItemView> draftItems,
        DateTimeOffset approvedAtUtc)
    {
        var includedItems = draftItems
            .Where(item => item.IncludeInDraft)
            .Where(item => item.Status != PortfolioDraftStatus.Excluded)
            .ToArray();
        var sections = design.Sections
            .Where(section => section.IncludeInPortfolio)
            .OrderBy(section => section.SortOrder)
            .Select(section => new PortfolioApprovedSectionSnapshot(
                section.Id,
                section.Heading,
                section.Introduction,
                section.SortOrder,
                includedItems
                    .Where(item => item.PortfolioSectionId == section.Id)
                    .OrderBy(item => item.SortOrder)
                    .Select(item => new PortfolioApprovedItemSnapshot(
                        item.PortfolioDraftItemId,
                        item.EvidenceRecordId,
                        item.DisplayTitle,
                        item.CourseTitle,
                        item.ModuleTitle,
                        item.AssignmentTitle,
                        item.StudentReflection,
                        item.ChosenReason,
                        item.SkillsShown,
                        item.ParentReviewNotes,
                        item.FileCount,
                        item.SortOrder))
                    .ToArray()))
            .ToArray();

        return new PortfolioApprovalSnapshot(
            Guid.NewGuid(),
            design.Version,
            design.Title,
            design.Purpose,
            design.Narratives
                .OrderBy(item => item.SortOrder)
                .Select(item => new PortfolioApprovedNarrativeSnapshot(item.Prompt, item.Response, item.SortOrder))
                .ToArray(),
            sections,
            approvedAtUtc,
            user.DisplayName,
            $"portfolio-{design.StudentId:N}-v{design.Version}-{approvedAtUtc:yyyyMMddHHmmss}.json");
    }

    private static OperationResult ValidateDesignForSubmission(
        PortfolioDesign design,
        IReadOnlyList<PortfolioDraftItemView> draftItems)
    {
        if (string.IsNullOrWhiteSpace(design.Title))
        {
            return OperationResult.Failure("Add a portfolio title before sending it for review.");
        }

        if (!design.Sections.Any(section => section.IncludeInPortfolio && !string.IsNullOrWhiteSpace(section.Heading)))
        {
            return OperationResult.Failure("Add at least one portfolio section before sending it for review.");
        }

        if (!draftItems.Any(item => item.IncludeInDraft && item.Status != PortfolioDraftStatus.Excluded))
        {
            return OperationResult.Failure("Add at least one accepted evidence item before sending the portfolio for review.");
        }

        return OperationResult.Success();
    }

    private static OperationResult ValidateDesignForApproval(
        PortfolioDesign design,
        IReadOnlyList<PortfolioDraftItemView> draftItems)
    {
        var submissionValidation = ValidateDesignForSubmission(design, draftItems);
        if (!submissionValidation.Succeeded)
        {
            return submissionValidation;
        }

        if (design.Suggestions.Any(suggestion => suggestion.Status == PortfolioSuggestionStatus.Open))
        {
            return OperationResult.Failure("Resolve open suggestions before approving the portfolio design.");
        }

        return OperationResult.Success();
    }

    private static PortfolioSection SectionWithSuggestionStatus(
        PortfolioSection section,
        IReadOnlyList<PortfolioSuggestion> suggestions)
    {
        if (!section.IncludeInPortfolio)
        {
            return section with { Status = PortfolioSectionStatus.Excluded };
        }

        var hasOpenSuggestion = suggestions.Any(suggestion =>
            suggestion.TargetType == PortfolioSuggestionTargetType.Section &&
            suggestion.TargetId == section.Id &&
            suggestion.Status == PortfolioSuggestionStatus.Open);
        if (hasOpenSuggestion)
        {
            return section with { Status = PortfolioSectionStatus.SuggestionsOpen };
        }

        var hasResolvedSuggestion = suggestions.Any(suggestion =>
            suggestion.TargetType == PortfolioSuggestionTargetType.Section &&
            suggestion.TargetId == section.Id &&
            suggestion.Status == PortfolioSuggestionStatus.Resolved);
        return section with
        {
            Status = hasResolvedSuggestion ? PortfolioSectionStatus.Resolved : PortfolioSectionStatus.NoSuggestions
        };
    }

    private static int OpenSuggestionCount(
        PortfolioDesign design,
        PortfolioSuggestionTargetType targetType,
        Guid? targetId)
    {
        return design.Suggestions.Count(suggestion =>
            suggestion.TargetType == targetType &&
            suggestion.TargetId == targetId &&
            suggestion.Status == PortfolioSuggestionStatus.Open);
    }

    private static string StudentName(Student student)
    {
        return $"{student.FirstName} {student.LastName}".Trim();
    }

    private static string Clean(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "" : value.Trim();
    }

    private static IReadOnlyList<string> CleanList(IReadOnlyList<string>? values)
    {
        return (values ?? [])
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private sealed record AssignmentContext(
        Course Course,
        LearningModule Module,
        ModuleAssignment Assignment);

    private sealed record PortfolioContext(
        IReadOnlyList<Student> Students,
        IReadOnlyList<Course> Courses,
        IReadOnlyList<AssignmentSubmission> Submissions,
        IReadOnlyList<EvidenceRecord> EvidenceRecords,
        IReadOnlyList<PortfolioDraftItem> DraftItems,
        IReadOnlyList<PortfolioDesign> Designs);
}
