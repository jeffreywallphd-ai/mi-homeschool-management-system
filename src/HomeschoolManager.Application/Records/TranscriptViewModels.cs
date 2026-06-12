using HomeschoolManager.Domain.Curriculum;

namespace HomeschoolManager.Application.Records;

public enum TranscriptSpan
{
    MiddleSchool = 1,
    HighSchool = 2,
    AllCourses = 3
}

public sealed record TranscriptStudentOption(Guid StudentId, string Name, int GradeLevel);

public sealed record TranscriptPreview(
    Guid StudentId,
    string StudentName,
    int CurrentGradeLevel,
    IReadOnlyList<TranscriptStudentOption> Students,
    TranscriptSpan Span,
    string SpanLabel,
    string TypicalGradeRange,
    string CoverageNote,
    string SchoolName,
    string AdministratorParentName,
    string Jurisdiction,
    DateTimeOffset GeneratedAtUtc,
    TranscriptSummary Summary,
    IReadOnlyList<TranscriptYearSection> Years,
    IReadOnlyList<TranscriptCourseDescriptionView> CourseDescriptions,
    IReadOnlyList<string> Warnings);

public sealed record TranscriptSummary(
    int CourseCount,
    int CompletedCourseCount,
    int InProgressCourseCount,
    decimal PlannedCredits,
    decimal EarnedCredits,
    string GpaDisplay,
    string CreditNote);

public sealed record TranscriptYearSection(
    Guid? SchoolYearId,
    string SchoolYearName,
    int? EstimatedGradeLevel,
    string GradeLabel,
    int StartYear,
    int EndYear,
    IReadOnlyList<TranscriptCourseRow> Courses,
    decimal PlannedCredits,
    decimal EarnedCredits);

public sealed record TranscriptCourseRow(
    Guid CourseId,
    Guid? TranscriptCourseRecordId,
    string CourseTitle,
    string SubjectArea,
    string TermLabel,
    CourseDuration Duration,
    decimal PlannedCreditValue,
    decimal? EarnedCreditValue,
    string FinalGrade,
    string Status,
    CompletionStatus CompletionStatus,
    DateOnly? CompletionDate,
    bool IncludeInTranscript,
    string CreditBasis,
    string ParentNotes,
    IReadOnlyList<string> SourceReferences);

public sealed record TranscriptCourseDescriptionView(
    Guid CourseId,
    string CourseTitle,
    string SubjectArea,
    string Description,
    string InstructionalMethods,
    string MajorTopics,
    string TextsAndResources,
    string AssessmentMethods,
    string GradingBasis,
    IReadOnlyList<TranscriptModuleDescriptionView> Modules);

public sealed record TranscriptModuleDescriptionView(
    Guid ModuleId,
    int SequenceOrder,
    string Title,
    string Description,
    string TermName,
    IReadOnlyList<string> LearningObjectives,
    IReadOnlyList<string> Resources,
    IReadOnlyList<TranscriptAssignmentDescriptionView> Assignments);

public sealed record TranscriptAssignmentDescriptionView(
    Guid AssignmentId,
    int SequenceOrder,
    string Title,
    string Summary,
    string Goal,
    bool PortfolioCandidate);

public sealed record TranscriptExportDownloadFile(
    string FileName,
    string ContentType,
    byte[] Content,
    TranscriptArchiveManifest Manifest);

public sealed record TranscriptArchiveManifest(
    string Format,
    int FormatVersion,
    DateTimeOffset GeneratedAtUtc,
    Guid StudentId,
    string StudentName,
    TranscriptSpan Span,
    string SpanLabel,
    string SchoolName,
    string AdministratorParentName,
    string CoverageNote,
    int CourseCount,
    decimal PlannedCredits,
    decimal EarnedCredits,
    IReadOnlyList<TranscriptArchiveCourseSource> CourseSources,
    IReadOnlyList<string> Warnings);

public sealed record TranscriptArchiveCourseSource(
    Guid CourseId,
    Guid? TranscriptCourseRecordId,
    string CourseTitle,
    string SchoolYearName,
    string FinalGrade,
    decimal? EarnedCreditValue,
    bool Included);
