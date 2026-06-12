using HomeschoolManager.Application.Assessments;
using HomeschoolManager.Application.Courses;
using HomeschoolManager.Application.Portfolio;
using HomeschoolManager.Application.Records;
using HomeschoolManager.Application.Requirements;
using HomeschoolManager.Application.Setup;
using HomeschoolManager.Application.Submissions;
using HomeschoolManager.Domain.Access;
using HomeschoolManager.Domain.Assessments;
using HomeschoolManager.Domain.Common;
using HomeschoolManager.Domain.Curriculum;
using HomeschoolManager.Domain.Household;
using HomeschoolManager.Domain.LegalRequirements;
using HomeschoolManager.Domain.Students;
using HomeschoolManager.Domain.Submissions;
using HomeschoolManager.Infrastructure.Persistence;
using Microsoft.Extensions.Options;
using HomeschoolManager.Infrastructure.Configuration;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

var tests = new List<(string Name, Func<Task> Test)>
{
    ("Household requires parent name", () =>
    {
        AssertThrows<DomainException>(() => new Household(Guid.NewGuid(), "Family", ""));
        return Task.CompletedTask;
    }),
    ("Student role cannot create household", async () =>
    {
        var repository = await CreateRepositoryAsync();
        var service = new SetupService(repository);
        var result = await service.CreateHouseholdAsync(UserContext.Student("Student"), new CreateHouseholdCommand("Family", "Parent"));
        AssertFalse(result.Succeeded, "Student command should fail.");
    }),
    ("Admin gradebook page declares a routable page directive", () =>
    {
        var root = FindRepositoryRoot();
        var path = Path.Combine(root, "src", "HomeschoolManager.Web", "Components", "Pages", "Gradebook.razor");
        var content = File.ReadAllText(path);
        AssertTrue(content.StartsWith("@page \"/gradebook\"", StringComparison.Ordinal), "Gradebook page must declare the /gradebook route.");
        AssertFalse(content.StartsWith("@gradebook", StringComparison.Ordinal), "Gradebook page route directive was accidentally renamed.");
        return Task.CompletedTask;
    }),
    ("Setup commands persist and reload", async () =>
    {
        var repository = await CreateRepositoryAsync();
        var service = new SetupService(repository);
        var parent = UserContext.ParentAdmin("Parent");

        AssertTrue((await service.CreateHouseholdAsync(parent, new CreateHouseholdCommand("Family", "Parent"))).Succeeded, "Household failed.");
        AssertTrue((await service.ConfigureSchoolProfileAsync(parent, new ConfigureSchoolProfileCommand(
            "Family Homeschool",
            "Parent",
            "Michigan",
            new DateOnly(2026, 8, 24),
            "Michigan Exemption 3(f)",
            "Parent",
            "Lansing",
            "Michigan"))).Succeeded, "School profile failed.");
        AssertTrue((await service.CreateStudentAsync(parent, new CreateStudentCommand("Student", "Learner", 12))).Succeeded, "Student failed.");
        AssertTrue((await service.ConfigureSchoolYearAsync(parent, new ConfigureSchoolYearCommand(
            "2026-2027",
            2026,
            2027,
            new DateOnly(2026, 8, 24),
            new DateOnly(2026, 12, 18),
            new DateOnly(2027, 1, 11),
            new DateOnly(2027, 5, 28)))).Succeeded, "School year failed.");

        var summary = await service.GetSummaryAsync();
        AssertTrue(summary.HasHousehold, "Missing household.");
        AssertTrue(summary.HasSchoolProfile, "Missing school profile.");
        AssertTrue(summary.HasStudent, "Missing student.");
        AssertTrue(summary.HasSchoolYear, "Missing school year.");
    }),
    ("Assessment record requires explicit student course and valid result state", () =>
    {
        var studentId = Guid.NewGuid();
        var courseId = Guid.NewGuid();

        AssertThrows<DomainException>(() => new AssessmentRecord(
            Guid.NewGuid(),
            Guid.Empty,
            courseId,
            null,
            null,
            null,
            null,
            AssessmentSourceType.CourseContext,
            AssessmentState.Assessed,
            AssessmentResultType.Narrative,
            "",
            null,
            null,
            null,
            "Shows clear understanding.",
            "",
            "",
            "",
            false,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow));

        AssertThrows<DomainException>(() => new AssessmentRecord(
            Guid.NewGuid(),
            studentId,
            courseId,
            null,
            null,
            null,
            null,
            AssessmentSourceType.CourseContext,
            AssessmentState.Assessed,
            AssessmentResultType.Points,
            "",
            11,
            10,
            null,
            "",
            "",
            "",
            "",
            false,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow));

        return Task.CompletedTask;
    }),
    ("Student cannot save assessment records", async () =>
    {
        var repository = await CreateRepositoryAsync();
        var setup = await CreateCourseWithAssignmentAsync(repository);
        var service = new GradebookService(repository);

        var result = await service.SaveAssessmentAsync(UserContext.Student("Student"), new SaveAssessmentCommand(
            null,
            setup.Student.Id,
            setup.Course.Id,
            setup.Module.Id,
            setup.Assignment.Id,
            null,
            null,
            AssessmentSourceType.Assignment,
            AssessmentState.Assessed,
            AssessmentResultType.Narrative,
            "",
            null,
            null,
            null,
            "Student work shows mastery.",
            "",
            "",
            "Good work.",
            true));

        AssertFalse(result.Succeeded, "Student assessment mutation should fail.");
        AssertEqual(0, (await repository.GetAssessmentRecordsAsync()).Count, "Student mutation should not persist assessment records.");
    }),
    ("Accepted submission remains needs review until parent records assessment", (Func<Task>)(async () =>
    {
        var repository = await CreateRepositoryAsync();
        var setup = await CreateCourseWithAssignmentAsync(repository);
        var service = new GradebookService(repository);
        var submittedAt = DateTimeOffset.UtcNow.AddMinutes(-5);
        var submission = new AssignmentSubmission(
            Guid.NewGuid(),
            setup.Student.Id,
            setup.Course.Id,
            setup.Module.Id,
            setup.Assignment.Id,
            1,
            AssignmentSubmissionStatus.Accepted,
            "Completed work.",
            "",
            [],
            submittedAt,
            submittedAt,
            submittedAt,
            null,
            submittedAt,
            "Accepted for assessment.",
            false);
        await repository.SaveAssignmentSubmissionAsync(submission);

        var before = await service.GetGradebookAsync(UserContext.ParentAdmin("Parent"), setup.Student.Id, setup.Course.Id);
        AssertTrue(before.Succeeded, "Gradebook should load before assessment.");
        var pendingRow = before.Value?.Rows.Single() ?? throw new InvalidOperationException("Expected one gradebook row.");
        AssertEqual(AssessmentState.NeedsReview, pendingRow.EffectiveState, "Accepted work should need assessment.");
        AssertEqual(10m, pendingRow.PlannedPoints ?? 0, "Planned points should remain planning data.");

        var save = await service.SaveAssessmentAsync(UserContext.ParentAdmin("Parent"), new SaveAssessmentCommand(
            null,
            setup.Student.Id,
            setup.Course.Id,
            setup.Module.Id,
            setup.Assignment.Id,
            submission.Id,
            null,
            AssessmentSourceType.Submission,
            AssessmentState.Assessed,
            AssessmentResultType.Points,
            "",
            9,
            10,
            null,
            "",
            "",
            "Parent internal note.",
            "Strong work on the submitted analysis.",
            true));
        AssertTrue(save.Succeeded, "Parent assessment save should succeed.");

        var after = await service.GetGradebookAsync(UserContext.ParentAdmin("Parent"), setup.Student.Id, setup.Course.Id);
        var assessedRow = after.Value?.Rows.Single() ?? throw new InvalidOperationException("Expected one assessed gradebook row.");
        AssertEqual(AssessmentState.Assessed, assessedRow.EffectiveState, "Recorded assessment should set explicit assessed state.");
        AssertEqual(AssessmentResultType.Points, assessedRow.Assessment?.ResultType ?? AssessmentResultType.NotGraded, "Assessment result type should be points.");
        AssertEqual(1, after.Value?.Summary?.AssessedCount ?? 0, "Summary should count explicit assessments.");
    })),
    ("Submitted work counts as needing assessment in gradebook summaries", (Func<Task>)(async () =>
    {
        var repository = await CreateRepositoryAsync();
        var setup = await CreateCourseWithAssignmentAsync(repository);
        var service = new GradebookService(repository);
        var submittedAt = DateTimeOffset.UtcNow.AddMinutes(-2);
        await repository.SaveAssignmentSubmissionAsync(new AssignmentSubmission(
            Guid.NewGuid(),
            setup.Student.Id,
            setup.Course.Id,
            setup.Module.Id,
            setup.Assignment.Id,
            1,
            AssignmentSubmissionStatus.Submitted,
            "Ready for review.",
            "",
            [],
            submittedAt,
            submittedAt,
            submittedAt,
            null,
            null,
            "",
            false));

        var gradebook = await service.GetGradebookAsync(UserContext.ParentAdmin("Parent"), setup.Student.Id, setup.Course.Id);
        AssertTrue(gradebook.Succeeded, "Gradebook should load submitted work.");
        AssertEqual(AssessmentState.NeedsReview, gradebook.Value!.Rows.Single().EffectiveState, "Submitted work should need assessment.");
        AssertEqual(1, gradebook.Value.Summary?.NeedsReviewCount ?? 0, "Course gradebook summary should count submitted work as needing assessment.");

        var dashboard = await service.GetDashboardSummaryAsync(UserContext.ParentAdmin("Parent"));
        AssertTrue(dashboard.Succeeded, "Gradebook dashboard should load submitted work.");
        AssertEqual(1, dashboard.Value!.NeedsReviewCount, "Gradebook dashboard should count submitted work as needing assessment.");
    })),
    ("Student gradebook groups upcoming submitted and graded assignments", (Func<Task>)(async () =>
    {
        var repository = await CreateRepositoryAsync();
        var setup = await CreateCourseWithAssignmentAsync(repository);
        var course = setup.Course;
        var module = setup.Module;
        var submittedAt = DateTimeOffset.UtcNow.AddMinutes(-4);
        await repository.SaveAssignmentSubmissionAsync(new AssignmentSubmission(
            Guid.NewGuid(),
            setup.Student.Id,
            course.Id,
            module.Id,
            setup.Assignment.Id,
            1,
            AssignmentSubmissionStatus.Submitted,
            "Submitted work.",
            "",
            [],
            submittedAt,
            submittedAt,
            submittedAt,
            null,
            null,
            "",
            false));

        var gradedAssignment = new ModuleAssignment(
            Guid.NewGuid(),
            module.Id,
            "graded-source",
            2,
            "Reviewed analysis",
            AssignmentType.Project,
            InstructionalMethodProfile.Hybrid,
            "Submit a reviewed analysis.",
            "One lesson",
            "After review",
            null,
            ["Explain evidence clearly."],
            [],
            "Written analysis",
            "",
            false,
            10,
            20,
            AssignmentStatus.Assigned);
        await repository.SaveCourseAsync(course with
        {
            Modules =
            [
                module with
                {
                    Assignments = module.Assignments.Concat([gradedAssignment]).ToArray()
                }
            ]
        });

        await repository.SaveAssessmentRecordAsync(new AssessmentRecord(
            Guid.NewGuid(),
            setup.Student.Id,
            course.Id,
            module.Id,
            gradedAssignment.Id,
            null,
            null,
            AssessmentSourceType.Assignment,
            AssessmentState.Assessed,
            AssessmentResultType.Points,
            "",
            9,
            10,
            null,
            "",
            "",
            "Parent note.",
            "Strong explanation.",
            true,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow));

        var studentGradebook = await new StudentCourseService(repository).GetGradebookAsync(UserContext.Student("Student"), setup.Student.Id);
        AssertTrue(studentGradebook.Succeeded, "Student gradebook should load for student portal.");
        AssertEqual(0, studentGradebook.Value!.UpcomingCount, "All assignments in this fixture should have submission or visible feedback.");
        AssertEqual(1, studentGradebook.Value.SubmittedCount, "Submitted assignment should appear in the submitted group.");
        AssertEqual(1, studentGradebook.Value.GradedCount, "Visible parent feedback should appear in the graded group.");
        AssertTrue(studentGradebook.Value.Assignments.Any(row => row.GradebookStatus == StudentGradebookStatus.Submitted && row.AssignmentId == setup.Assignment.Id), "Submitted assignment should be listed.");
        AssertTrue(studentGradebook.Value.Assignments.Any(row => row.GradebookStatus == StudentGradebookStatus.Graded && row.AssignmentId == gradedAssignment.Id), "Graded assignment should be listed.");

        var parentGradebook = await new StudentCourseService(repository).GetGradebookAsync(UserContext.ParentAdmin("Parent"), setup.Student.Id);
        AssertFalse(parentGradebook.Succeeded, "Admin preview should not use the true student gradebook.");
    })),
    ("Gradebook exposes submitted files and parent preview renders markdown attachments", (Func<Task>)(async () =>
    {
        var (repository, paths) = await CreateRepositoryWithPathsAsync();
        var setup = await CreateCourseWithAssignmentAsync(repository);
        var fileStore = new LocalSubmissionFileStore(paths);
        var submissionService = new AssignmentSubmissionService(repository, fileStore);
        var previewService = new SubmissionFilePreviewService(repository, fileStore);
        var gradebookService = new GradebookService(repository);
        var parent = UserContext.ParentAdmin("Parent");
        var studentUser = UserContext.Student("Student");
        var attachmentBytes = System.Text.Encoding.UTF8.GetBytes("# Acceptable Values\n\nEvidence paragraph with details for review.");

        var submitted = await submissionService.SubmitAssignmentAsync(
            studentUser,
            new SubmitAssignmentCommand(
                setup.Course.Id,
                setup.Module.Id,
                setup.Assignment.Id,
                "Please review the attached work.",
                "",
                [new AssignmentAttachmentUpload("acceptable-values.md", "text/markdown", attachmentBytes)],
                setup.Student.Id));
        AssertTrue(submitted.Succeeded, "Student submission with attachment should succeed.");

        var gradebook = await gradebookService.GetGradebookAsync(parent, setup.Student.Id, setup.Course.Id);
        AssertTrue(gradebook.Succeeded, "Gradebook should load submitted file metadata.");
        var row = gradebook.Value!.Rows.Single();
        AssertEqual(submitted.Value, row.LatestSubmissionId ?? Guid.Empty, "Gradebook row should point to the latest submission.");
        AssertEqual(1, row.LatestSubmissionAttachments.Count, "Gradebook row should include submitted attachment metadata.");
        AssertEqual("acceptable-values.md", row.LatestSubmissionAttachments[0].OriginalFileName, "Attachment filename should be visible in gradebook.");

        var preview = await previewService.GetPreviewAsync(parent, submitted.Value, row.LatestSubmissionAttachments[0].FileId);
        AssertTrue(preview.Succeeded, "Parent should be able to preview the submitted attachment.");
        AssertEqual("text/html", preview.Value!.ContentType, "Markdown attachment preview should render as HTML.");
        var previewHtml = System.Text.Encoding.UTF8.GetString(preview.Value.Content);
        AssertTrue(previewHtml.Contains("<h1>Acceptable Values</h1>", StringComparison.Ordinal), "Markdown heading should render as a level-one heading.");
        AssertFalse(previewHtml.Contains("# Acceptable Values", StringComparison.Ordinal), "Markdown preview should not show heading markup.");

        var download = await previewService.GetDownloadAsync(parent, submitted.Value, row.LatestSubmissionAttachments[0].FileId);
        AssertTrue(download.Succeeded, "Parent should be able to download the original attachment.");
        AssertEqual("text/markdown", download.Value!.ContentType, "Download should keep the original content type.");
        AssertTrue(download.Value.Content.SequenceEqual(attachmentBytes), "Download should return the original attachment bytes.");

        var studentPreview = await previewService.GetPreviewAsync(studentUser, submitted.Value, row.LatestSubmissionAttachments[0].FileId);
        AssertFalse(studentPreview.Succeeded, "Student should not use the admin gradebook file preview route.");
    })),
    ("Parent preview keeps browser media attachments playable", (Func<Task>)(async () =>
    {
        var (repository, paths) = await CreateRepositoryWithPathsAsync();
        var setup = await CreateCourseWithAssignmentAsync(repository);
        var fileStore = new LocalSubmissionFileStore(paths);
        var submissionService = new AssignmentSubmissionService(repository, fileStore);
        var previewService = new SubmissionFilePreviewService(repository, fileStore);
        var parent = UserContext.ParentAdmin("Parent");
        var studentUser = UserContext.Student("Student");
        var mediaBytes = new byte[] { 0, 1, 2, 3, 4 };

        var submitted = await submissionService.SubmitAssignmentAsync(
            studentUser,
            new SubmitAssignmentCommand(
                setup.Course.Id,
                setup.Module.Id,
                setup.Assignment.Id,
                "Please review the attached media.",
                "",
                [
                    new AssignmentAttachmentUpload("field-photo.png", "image/png", mediaBytes),
                    new AssignmentAttachmentUpload("field-video.mp4", "video/mp4", mediaBytes),
                    new AssignmentAttachmentUpload("field-audio.mp3", "audio/mpeg", mediaBytes)
                ],
                setup.Student.Id));
        AssertTrue(submitted.Succeeded, "Student submission with media attachments should succeed.");

        var submission = await repository.GetAssignmentSubmissionAsync(submitted.Value);
        foreach (var attachment in submission!.Attachments)
        {
            var preview = await previewService.GetPreviewAsync(parent, submitted.Value, attachment.Id);
            AssertTrue(preview.Succeeded, $"Parent should preview {attachment.OriginalFileName}.");
            AssertEqual(attachment.ContentType, preview.Value!.ContentType, "Media preview should keep the browser-playable content type.");
            AssertTrue(preview.Value.Content.SequenceEqual(mediaBytes), "Media preview should return the original bytes.");
        }
    })),
    ("Assessment records persist and reload", (Func<Task>)(async () =>
    {
        var created = await CreateRepositoryWithPathsAsync();
        var setup = await CreateCourseWithAssignmentAsync(created.Repository);
        var service = new GradebookService(created.Repository);
        var save = await service.SaveAssessmentAsync(UserContext.ParentAdmin("Parent"), new SaveAssessmentCommand(
            null,
            setup.Student.Id,
            setup.Course.Id,
            setup.Module.Id,
            setup.Assignment.Id,
            null,
            null,
            AssessmentSourceType.Assignment,
            AssessmentState.Incomplete,
            AssessmentResultType.NotGraded,
            "",
            null,
            null,
            null,
            "",
            "",
            "Waiting on the final graph.",
            "",
            false));
        AssertTrue(save.Succeeded, "Assessment should save before reload.");

        var reloaded = new JsonHomeschoolRepository(created.Paths);
        await reloaded.EnsureStoreCreatedAsync();
        var records = await reloaded.GetAssessmentRecordsAsync();
        AssertEqual(1, records.Count, "Assessment should persist through repository reload.");
        AssertEqual(AssessmentState.Incomplete, records[0].State, "Reloaded assessment state should remain explicit.");
    })),
    ("Student feedback only exposes parent approved assessment feedback", async () =>
    {
        var repository = await CreateRepositoryAsync();
        var setup = await CreateCourseWithAssignmentAsync(repository);
        var service = new GradebookService(repository);
        var parent = UserContext.ParentAdmin("Parent");

        AssertTrue((await service.SaveAssessmentAsync(parent, new SaveAssessmentCommand(
            null,
            setup.Student.Id,
            setup.Course.Id,
            setup.Module.Id,
            setup.Assignment.Id,
            null,
            null,
            AssessmentSourceType.Assignment,
            AssessmentState.Assessed,
            AssessmentResultType.LetterGrade,
            "A",
            null,
            null,
            null,
            "",
            "",
            "Internal grading note.",
            "This feedback is hidden.",
            false))).Succeeded, "Hidden feedback assessment should save.");

        AssertTrue((await service.SaveAssessmentAsync(parent, new SaveAssessmentCommand(
            null,
            setup.Student.Id,
            setup.Course.Id,
            setup.Module.Id,
            setup.Assignment.Id,
            null,
            null,
            AssessmentSourceType.Assignment,
            AssessmentState.ReturnedForRevision,
            AssessmentResultType.NotGraded,
            "",
            null,
            null,
            null,
            "",
            "",
            "Internal revision note.",
            "Please revise the evidence paragraph.",
            true))).Succeeded, "Visible feedback assessment should save.");

        var feedback = await service.ListStudentFeedbackAsync(UserContext.Student("Student"), setup.Student.Id, setup.Course.Id, setup.Module.Id);
        AssertTrue(feedback.Succeeded, "Student should be able to read approved feedback.");
        AssertEqual(1, feedback.Value?.Count ?? 0, "Student should see only visible feedback.");
        AssertEqual("Please revise the evidence paragraph.", feedback.Value?[0].StudentFeedback ?? "", "Student feedback should match approved text.");
    }),
    ("Michigan seed is idempotent and keeps views distinct", async () =>
    {
        var repository = await CreateRepositoryAsync();
        var service = new RequirementService(repository);
        var parent = UserContext.ParentAdmin("Parent");

        AssertTrue((await service.SeedMichiganAsync(parent)).Succeeded, "First seed failed.");
        AssertTrue((await service.SeedMichiganAsync(parent)).Succeeded, "Second seed failed.");

        var checklist = await service.GetChecklistAsync();
        AssertEqual(16, checklist.Count, "Unexpected seeded area count.");
        AssertEqual("Statutory", checklist[0].View, "Statutory requirements should be listed first.");
        AssertTrue(checklist.Any(item => item.View == "Statutory" && item.Name == "English Grammar"), "Missing statutory view area.");
        AssertTrue(checklist.Any(item => item.View == "MDE Summary" && item.Name == "U.S. Constitution"), "Missing MDE U.S. Constitution area.");
        AssertTrue(checklist.Any(item => item.View == "MMC Reference" && item.Name == "Personal Finance"), "Missing MMC personal finance reference area.");
        AssertFalse(checklist.Any(item => item.View != "Statutory" && item.Name == "English Language Arts"), "English Language Arts should be represented by statutory English subject rows.");
    }),
    ("Default Michigan course pack mappings match seeded requirement areas", (Func<Task>)(() =>
    {
        var pack = DefaultCoursePacks.All.First(item => item.Id == DefaultCoursePacks.MichiganCollegeReadyPackId);
        AssertEqual("mi-general-high-school-core-v4", pack.Id, "Default pack should use the revised embedded coursepack.");
        AssertEqual("Michigan General High School Core Starter", pack.Name, "Default pack should use the revised embedded coursepack name.");
        AssertEqual("Michigan", pack.RequirementJurisdiction, "Default pack should target Michigan requirements.");
        foreach (var template in pack.Courses)
        {
            foreach (var option in template.Options)
            {
                AssertTrue(option.RequirementMappings.Count > 0, $"Default pack option should include requirement mapping guidance for {template.TemplateId}/{option.OptionId}.");

                AssertFalse(string.IsNullOrWhiteSpace(option.Description.MajorTopics), $"Missing major topics for {template.TemplateId}/{option.OptionId}.");
                AssertFalse(string.IsNullOrWhiteSpace(option.Description.TextsAndResources), $"Missing texts/resources for {template.TemplateId}/{option.OptionId}.");
                AssertTrue(option.Description.TextsAndResources.Split(Environment.NewLine).Any(line => line.Contains('|', StringComparison.Ordinal)), $"Texts/resources should include named resource links for {template.TemplateId}/{option.OptionId}.");
                AssertFalse(string.IsNullOrWhiteSpace(option.Description.InstructionalMethods), $"Missing instructional methods for {template.TemplateId}/{option.OptionId}.");
                AssertFalse(string.IsNullOrWhiteSpace(option.Description.AssessmentMethods), $"Missing assessment methods for {template.TemplateId}/{option.OptionId}.");
                AssertFalse(string.IsNullOrWhiteSpace(option.Description.GradingBasis), $"Missing grading basis for {template.TemplateId}/{option.OptionId}.");
                AssertFalse(string.IsNullOrWhiteSpace(option.CurriculumPlan.Goals), $"Missing curriculum goals for {template.TemplateId}/{option.OptionId}.");
                AssertFalse(string.IsNullOrWhiteSpace(option.CurriculumPlan.LearningObjectives), $"Missing learning objectives for {template.TemplateId}/{option.OptionId}.");
                AssertTrue(option.CurriculumPlan.LearningObjectives.Split(Environment.NewLine).Length >= 3, $"Learning objectives should be separated for {template.TemplateId}/{option.OptionId}.");
                AssertFalse(option.CurriculumPlan.LearningObjectives.Contains("Upon completion", StringComparison.OrdinalIgnoreCase), $"Learning objectives should finish the standard sentence without repeating it for {template.TemplateId}/{option.OptionId}.");
                AssertFalse(option.CurriculumPlan.LearningObjectives.Contains("course-level", StringComparison.OrdinalIgnoreCase), $"Learning objectives should be course-specific for {template.TemplateId}/{option.OptionId}.");
                AssertFalse(option.CurriculumPlan.LearningObjectives.Contains("produce evidence suitable for course records", StringComparison.OrdinalIgnoreCase), $"Learning objectives should avoid generic recordkeeping language for {template.TemplateId}/{option.OptionId}.");
                AssertTrue(string.IsNullOrWhiteSpace(option.CurriculumPlan.MajorResources), $"Major resources should be retired for {template.TemplateId}/{option.OptionId}.");
                AssertFalse(string.IsNullOrWhiteSpace(option.CurriculumPlan.PlannedSequence), $"Missing planned sequence for {template.TemplateId}/{option.OptionId}.");
                AssertTrue(option.Modules.Count >= 3, $"Every default-pack option should include learning modules for {template.TemplateId}/{option.OptionId}.");
                AssertTrue(option.Modules.All(module => module.SequenceOrder >= 1), $"Pack module order should be valid for {template.TemplateId}/{option.OptionId}.");
                AssertTrue(option.Modules.All(module => !string.IsNullOrWhiteSpace(module.Instructions)), $"Pack modules should include instructions for {template.TemplateId}/{option.OptionId}.");
                AssertTrue(option.Modules.All(module => module.LearningObjectives.Count > 0), $"Pack modules should include learning objectives for {template.TemplateId}/{option.OptionId}.");
                AssertTrue(option.Modules.All(module => module.Resources.Count > 0), $"Pack modules should include concrete resources for {template.TemplateId}/{option.OptionId}.");
                AssertTrue(option.Modules.All(module => module.Lessons.Count >= module.LearningObjectives.Count), $"Pack modules should include at least one lesson per module objective for {template.TemplateId}/{option.OptionId}.");
                AssertTrue(option.Modules.SelectMany(module => module.Lessons).All(lesson => lesson.Resources.Count > 0), $"Pack lessons should include resources for {template.TemplateId}/{option.OptionId}.");
                AssertTrue(option.Modules.All(module => module.Assignments.Count > 0), $"Pack modules should include assignments for {template.TemplateId}/{option.OptionId}.");
                AssertTrue(option.Modules.SelectMany(module => module.Assignments).All(assignment => assignment.Variants.Any(variant => variant.MethodProfile == InstructionalMethodProfile.Hybrid)), $"Pack assignments should include a hybrid variant for {template.TemplateId}/{option.OptionId}.");
                AssertTrue(option.Modules.SelectMany(module => module.Assignments).All(assignment => assignment.Variants.Count >= 3), $"Pack assignments should include multiple variants for {template.TemplateId}/{option.OptionId}.");
                foreach (var module in option.Modules)
                {
                    foreach (var assignment in module.Assignments)
                    {
                        foreach (var variant in assignment.Variants)
                        {
                            AssertTrue(variant.LinkedModuleObjectives.Count > 0, $"Assignment variants should link module objectives for {template.TemplateId}/{option.OptionId}.");
                            AssertTrue(variant.LinkedLessonIds.All(sourceLessonId => module.Lessons.Any(lesson => lesson.LessonId == sourceLessonId)), $"Assignment variants should only link existing source lessons for {template.TemplateId}/{option.OptionId}.");
                        }
                    }
                }
                AssertTrue(option.Modules.SelectMany(module => module.LearningObjectives).Any(objective => !string.IsNullOrWhiteSpace(objective.LinkedCourseObjective)), $"Pack modules should include course objective links for {template.TemplateId}/{option.OptionId}.");
                AssertTrue(option.Modules.All(module => !string.IsNullOrWhiteSpace(module.AssignmentEvidencePlaceholder)), $"Pack modules should include assignment/evidence placeholders for {template.TemplateId}/{option.OptionId}.");
            }
        }

        AssertEqual(8m, pack.Courses.Sum(course => course.DefaultOption.PlannedCreditValue), "Default pack should total eight planned credits.");

        return Task.CompletedTask;
    })),
    ("Default Michigan course pack downloads as JSON coursepack", async () =>
    {
        var repository = CreateRepositoryAsync().GetAwaiter().GetResult();
        var service = new CourseService(repository);
        var result = await service.DownloadCoursePackAsync(DefaultCoursePacks.MichiganCollegeReadyPackId);
        AssertTrue(result.Succeeded, "Course pack download should succeed.");
        if (result.Value is null)
        {
            throw new InvalidOperationException("Course pack download did not return a file.");
        }

        AssertTrue(result.Value.FileName.EndsWith(".coursepack", StringComparison.Ordinal), "Course pack download should use the .coursepack extension.");
        AssertEqual("application/json", result.Value.ContentType, "Course pack download should be JSON.");
        AssertFalse(result.Value.IsArchive, "Current course pack download should not be a zip archive.");

        using var document = JsonDocument.Parse(result.Value.Content);
        var root = document.RootElement;
        AssertEqual("homeschool-manager.coursepack", root.GetProperty("format").GetString() ?? "", "Unexpected course pack download format.");
        AssertEqual(1, root.GetProperty("formatVersion").GetInt32(), "Unexpected course pack download version.");
        AssertEqual("json", root.GetProperty("packageMode").GetString() ?? "", "Current download should be JSON mode.");
        AssertTrue(root.TryGetProperty("downloadedAtUtc", out _), "Download envelope should include downloadedAtUtc.");
        var pack = root.GetProperty("pack");
        AssertEqual(DefaultCoursePacks.MichiganCollegeReadyPackId, pack.GetProperty("id").GetString() ?? "", "Download should include the selected pack id.");
        AssertTrue(pack.GetProperty("courses").GetArrayLength() > 0, "Download should include courses.");
        var firstCourse = pack.GetProperty("courses")[0];
        AssertTrue(firstCourse.GetProperty("options").GetArrayLength() > 0, "Download should include course options.");
        var firstModule = firstCourse.GetProperty("modules")[0];
        AssertTrue(firstModule.GetProperty("lessons").GetArrayLength() > 0, "Download should include lessons.");
        AssertTrue(firstModule.GetProperty("assignments").GetArrayLength() > 0, "Download should include assignments.");
        var firstVariant = firstModule.GetProperty("assignments")[0].GetProperty("variants")[0];
        AssertTrue(firstVariant.TryGetProperty("assignmentSummary", out _), "Course pack assignment variants should include assignment summary.");
        AssertTrue(firstVariant.TryGetProperty("requiredDeliverables", out _), "Course pack assignment variants should include deliverables.");
        AssertTrue(firstVariant.TryGetProperty("assignmentSteps", out _), "Course pack assignment variants should include assignment steps.");
        AssertTrue(firstVariant.TryGetProperty("scoring", out _), "Course pack assignment variants should include scoring.");
    }),
    ("Single coursepack template downloads as a one-course shell", async () =>
    {
        var repository = await CreateRepositoryAsync();
        var service = new CourseService(repository);
        var result = service.DownloadCoursePackTemplate();
        AssertTrue(result.Succeeded, "Coursepack template download should succeed.");
        if (result.Value is null)
        {
            throw new InvalidOperationException("Coursepack template did not return a file.");
        }

        AssertTrue(result.Value.FileName.EndsWith(".coursepack", StringComparison.Ordinal), "Template should use .coursepack.");
        AssertEqual("application/json", result.Value.ContentType, "Template should be JSON.");

        using var document = JsonDocument.Parse(result.Value.Content);
        var root = document.RootElement;
        AssertEqual("homeschool-manager.coursepack", root.GetProperty("format").GetString() ?? "", "Unexpected coursepack template format.");
        AssertEqual(2, root.GetProperty("formatVersion").GetInt32(), "Single-course coursepack should use version 2.");
        AssertTrue(root.GetProperty("isTemplate").GetBoolean(), "Template should identify itself as a template.");
        AssertTrue(root.TryGetProperty("sourceIdentity", out var templateIdentity), "Template should include source identity metadata.");
        AssertEqual("template.coursepack-template", templateIdentity.GetProperty("sourceNamespace").GetString() ?? "", "Template source namespace should be explicit.");
        var course = root.GetProperty("course");
        AssertTrue(course.TryGetProperty("description", out _), "Coursepack should include course detail description.");
        AssertTrue(course.TryGetProperty("curriculumPlan", out _), "Coursepack should include curriculum plan.");
        AssertTrue(course.TryGetProperty("moduleReferences", out var moduleReferences), "Coursepack should include module references.");
        AssertTrue(moduleReferences.GetArrayLength() > 0, "Coursepack template should include a sample module reference.");
        AssertFalse(course.TryGetProperty("modules", out _), "Single-course coursepack should not embed module bodies.");
    }),
    ("Default course plan downloads as structured zip bundle", async () =>
    {
        var repository = await CreateRepositoryAsync();
        var service = new CourseService(repository);
        var result = await service.DownloadCoursePlanBundleAsync(DefaultCoursePacks.MichiganCollegeReadyPackId);
        AssertTrue(result.Succeeded, "Course plan bundle download should succeed.");
        if (result.Value is null)
        {
            throw new InvalidOperationException("Course plan bundle did not return a file.");
        }

        AssertTrue(result.Value.FileName.EndsWith(".zip", StringComparison.Ordinal), "Bundle should use .zip so standard zip programs recognize it.");
        AssertTrue(result.Value.IsArchive, "Course plan bundle should be a zip archive.");
        using var stream = new MemoryStream(result.Value.Content);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        AssertBundleEntryNamesAreShort(archive);
        AssertTrue(archive.Entries.Any(entry => entry.FullName == "courseplan.courseplanpack"), "Bundle should include a courseplanpack manifest.");
        AssertTrue(archive.Entries.Any(entry => entry.FullName.EndsWith("/course.coursepack", StringComparison.OrdinalIgnoreCase)), "Bundle should include course folders with coursepacks.");
        AssertTrue(archive.Entries.Any(entry => entry.FullName.EndsWith("/module.modulepack", StringComparison.OrdinalIgnoreCase)), "Bundle should include modulepacks.");
        AssertTrue(archive.Entries.Any(entry => entry.FullName.EndsWith("/lessons.lessonpack", StringComparison.OrdinalIgnoreCase)), "Bundle should include lessonpacks.");
        AssertTrue(archive.Entries.Any(entry => entry.FullName.EndsWith("/assignments.assignmentpack", StringComparison.OrdinalIgnoreCase)), "Bundle should include assignmentpacks.");

        var manifest = archive.Entries.First(entry => entry.FullName == "courseplan.courseplanpack");
        using var manifestDocument = JsonDocument.Parse(manifest.Open());
        AssertTrue(manifestDocument.RootElement.TryGetProperty("sourceIdentity", out var planIdentity), "Course plan manifest should include source identity.");
        AssertEqual("builtin.mi-general-high-school-core-v4", planIdentity.GetProperty("sourceNamespace").GetString() ?? "", "Default plan source namespace should be stable.");

        var firstCourse = archive.Entries.First(entry => entry.FullName.EndsWith("/course.coursepack", StringComparison.OrdinalIgnoreCase));
        using var courseDocument = JsonDocument.Parse(firstCourse.Open());
        AssertEqual(2, courseDocument.RootElement.GetProperty("formatVersion").GetInt32(), "Bundled coursepacks should use single-course format.");
        AssertTrue(courseDocument.RootElement.TryGetProperty("sourceIdentity", out var courseIdentity), "Bundled coursepacks should include source identity.");
        AssertEqual("builtin.mi-general-high-school-core-v4", courseIdentity.GetProperty("sourceNamespace").GetString() ?? "", "Bundled course source namespace should match the plan.");
        AssertTrue(courseDocument.RootElement.GetProperty("course").TryGetProperty("moduleReferences", out _), "Bundled coursepack should reference modules.");
        AssertFalse(courseDocument.RootElement.GetProperty("course").TryGetProperty("modules", out _), "Bundled coursepack should not embed module bodies.");
    }),
    ("Course plan bundle template downloads all inner templates", async () =>
    {
        var repository = await CreateRepositoryAsync();
        var service = new CourseService(repository);
        var result = service.DownloadCoursePlanBundleTemplate();
        AssertTrue(result.Succeeded, "Course plan bundle template download should succeed.");
        if (result.Value is null)
        {
            throw new InvalidOperationException("Course plan bundle template did not return a file.");
        }

        AssertEqual("course-plan-bundle-template.zip", result.Value.FileName, "Template should have a clear zip file name.");
        AssertEqual("application/zip", result.Value.ContentType, "Template should be a zip archive.");
        AssertTrue(result.Value.IsArchive, "Template should be marked as an archive.");

        using var stream = new MemoryStream(result.Value.Content);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        AssertBundleEntryNamesAreShort(archive);
        var expectedEntries = new[]
        {
            "courseplan.courseplanpack",
            "acceptable-values.md",
            "courses/sample-course/course.coursepack",
            "courses/sample-course/modules/01-sample-module/module.modulepack",
            "courses/sample-course/modules/01-sample-module/lessons.lessonpack",
            "courses/sample-course/modules/01-sample-module/assignments.assignmentpack"
        };

        foreach (var expectedEntry in expectedEntries)
        {
            AssertTrue(archive.Entries.Any(entry => entry.FullName == expectedEntry), $"Template bundle should include {expectedEntry}.");
        }

        foreach (var entry in archive.Entries.Where(item => item.FullName.EndsWith("pack", StringComparison.OrdinalIgnoreCase)))
        {
            using var document = JsonDocument.Parse(entry.Open());
            AssertTrue(document.RootElement.GetProperty("isTemplate").GetBoolean(), $"{entry.FullName} should identify itself as a template.");
            AssertTrue(document.RootElement.TryGetProperty("sourceIdentity", out var sourceIdentity), $"{entry.FullName} should include source identity.");
            AssertEqual("template.courseplan-bundle-template", sourceIdentity.GetProperty("sourceNamespace").GetString() ?? "", $"{entry.FullName} should share the bundle template source namespace.");
        }

        var guideEntry = archive.Entries.First(entry => entry.FullName == "acceptable-values.md");
        using var guideReader = new StreamReader(guideEntry.Open());
        var guide = guideReader.ReadToEnd();
        AssertTrue(guide.Contains("Course Plan Bundle Acceptable Values", StringComparison.Ordinal), "Template guide should explain acceptable values.");
        AssertTrue(guide.Contains("duration: One of OneSemester, TwoSemesters", StringComparison.Ordinal), "Template guide should include course duration values.");
        AssertTrue(guide.Contains("lessonType: One of SelfGuided, ParentLed, Discussion, LabOrFieldwork, ProjectStudio, AssessmentPrep", StringComparison.Ordinal), "Template guide should include lesson type values.");
        AssertTrue(guide.Contains("type: One of Reading, TextbookChapter, Article, Video, Website, File, PhysicalResource, DataSource", StringComparison.Ordinal), "Template guide should include resource type values.");
        AssertTrue(guide.Contains("submissionFormats accepted values", StringComparison.Ordinal), "Template guide should include assignment submission values.");
        AssertTrue(guide.Contains("gradingMode: One of Completion, Points, Rubric, ParentReview, NotGraded", StringComparison.Ordinal), "Template guide should include scoring values.");
    }),
    ("Course plan bundle imports courses modules lessons and assignments", (Func<Task>)(async () =>
    {
        var sourceRepository = await CreateRepositoryAsync();
        var sourceService = new CourseService(sourceRepository);
        var bundle = await sourceService.DownloadCoursePlanBundleAsync(DefaultCoursePacks.MichiganCollegeReadyPackId);
        AssertTrue(bundle.Succeeded, "Course plan bundle download should succeed.");
        if (bundle.Value is null)
        {
            throw new InvalidOperationException("Course plan bundle did not return content.");
        }

        var repository = await CreateRepositoryAsync();
        var setup = new SetupService(repository);
        var parent = UserContext.ParentAdmin("Parent");
        await CreateSetupAsync(repository);
        var studentId = (await setup.ListStudentsAsync()).Single().Id;

        var service = new CourseService(repository);
        var import = await service.ImportCoursePlanBundleAsync(parent, studentId, bundle.Value.Content);
        AssertTrue(import.Succeeded, $"Course plan bundle import should succeed. {string.Join(" | ", import.Errors)}");
        AssertTrue(import.Value?.CourseCount > 0, "Bundle import should add courses.");
        AssertTrue(import.Value?.ModuleCount > 0, "Bundle import should add modules.");
        AssertTrue(import.Value?.LessonCount > 0, "Bundle import should add lessons.");
        AssertTrue(import.Value?.AssignmentCount > 0, "Bundle import should add assignments.");
        var courses = await service.ListCoursesAsync(studentId);
        AssertTrue(courses.Count > 0, "Imported courses should be visible.");
        var detail = await service.GetCourseDetailAsync(courses[0].Id);
        AssertTrue(detail?.Modules.Count > 0, "Imported course should have modules.");
        AssertTrue(detail?.Modules.SelectMany(module => module.Lessons).Any() ?? false, "Imported modules should have lessons.");
        AssertTrue(detail?.Modules.SelectMany(module => module.Assignments).Any() ?? false, "Imported modules should have assignments.");
        var storedCourses = await repository.GetCoursesAsync();
        AssertTrue(storedCourses.All(course => course.SourcePackId == "builtin.mi-general-high-school-core-v4"), "Imported courses should store the pack namespace as the source pack key.");

        var courseCount = courses.Count;
        var moduleCount = detail?.Modules.Count ?? 0;
        var lessonCount = detail?.Modules.SelectMany(module => module.Lessons).Count() ?? 0;
        var assignmentCount = detail?.Modules.SelectMany(module => module.Assignments).Count() ?? 0;
        var secondImport = await service.ImportCoursePlanBundleAsync(parent, studentId, bundle.Value.Content);
        AssertTrue(secondImport.Succeeded, $"Second course plan bundle import should update existing pack content. {string.Join(" | ", secondImport.Errors)}");
        var coursesAfterSecondImport = await service.ListCoursesAsync(studentId);
        var detailAfterSecondImport = await service.GetCourseDetailAsync(courses[0].Id);
        AssertEqual(courseCount, coursesAfterSecondImport.Count, "Re-importing a newer version of the same plan should not duplicate courses.");
        AssertEqual(moduleCount, detailAfterSecondImport?.Modules.Count ?? 0, "Re-importing a newer version should not duplicate modules.");
        AssertEqual(lessonCount, detailAfterSecondImport?.Modules.SelectMany(module => module.Lessons).Count() ?? 0, "Re-importing a newer version should not duplicate lessons.");
        AssertEqual(assignmentCount, detailAfterSecondImport?.Modules.SelectMany(module => module.Assignments).Count() ?? 0, "Re-importing a newer version should not duplicate assignments.");
    })),
    ("Downloaded coursepack installs into system before course import", async () =>
    {
        var sourceRepository = await CreateRepositoryAsync();
        var sourceService = new CourseService(sourceRepository);
        var download = await sourceService.DownloadCoursePackAsync(DefaultCoursePacks.MichiganCollegeReadyPackId);
        AssertTrue(download.Succeeded, "Course pack download should succeed.");
        if (download.Value is null)
        {
            throw new InvalidOperationException("Course pack download did not return content.");
        }

        var repository = await CreateRepositoryAsync();
        await CreateSetupAsync(repository);
        var service = new CourseService(repository);
        var parent = UserContext.ParentAdmin("Parent");
        var builtInInstall = await service.InstallCoursePackFileAsync(parent, download.Value.Content);
        AssertFalse(builtInInstall.Succeeded, "Installing an exact built-in download should not duplicate the built-in pack.");
        AssertTrue(builtInInstall.Errors.Any(error => error.Contains("already built into the system", StringComparison.OrdinalIgnoreCase)), "Exact built-in download should be parsed and rejected as already installed, not as an unreadable format.");

        var installedContent = System.Text.Encoding.UTF8
            .GetString(download.Value.Content)
            .Replace(DefaultCoursePacks.MichiganCollegeReadyPackId, "installed-test-course-pack", StringComparison.Ordinal)
            .Replace("Michigan college-recognizable high school core", "Installed test high school core", StringComparison.Ordinal);

        var install = await service.InstallCoursePackFileAsync(parent, System.Text.Encoding.UTF8.GetBytes(installedContent));
        AssertTrue(install.Succeeded, "Course pack file install should succeed.");
        AssertEqual("installed-test-course-pack", install.Value!.Id, "Installed pack id should be returned.");
        AssertEqual(0, (await service.ListCoursesAsync()).Count, "Installing a course pack should not import courses.");

        var availablePacks = await service.ListCoursePacksAsync();
        AssertTrue(availablePacks.Any(pack => pack.Id == "installed-test-course-pack"), "Installed course pack should be listed.");

        var import = await service.ImportCoursePackAsync(parent, new ImportCoursePackCommand("installed-test-course-pack", []));
        AssertTrue(import.Succeeded, "Installed course pack import should succeed.");
        AssertTrue(import.Value > 0, "Installed course pack import should create courses.");

        var courses = await service.ListCoursesAsync();
        AssertEqual(import.Value, courses.Count, "Imported course count should match created courses.");
        AssertTrue(courses.Any(course => course.Title.Contains("English", StringComparison.OrdinalIgnoreCase)), "Imported course pack should create recognizable course records.");

        var duplicate = await service.ImportCoursePackAsync(parent, new ImportCoursePackCommand("installed-test-course-pack", []));
        AssertTrue(duplicate.Succeeded, "Duplicate installed course pack import should succeed.");
        AssertEqual(0, duplicate.Value, "Duplicate installed course pack import should skip existing templates.");
    }),
    ("Older PascalCase coursepack downloads install successfully", async () =>
    {
        var repository = await CreateRepositoryAsync();
        await CreateSetupAsync(repository);
        var service = new CourseService(repository);
        var parent = UserContext.ParentAdmin("Parent");
        var pack = DefaultCoursePacks.All
            .First(item => item.Id == DefaultCoursePacks.MichiganCollegeReadyPackId)
            with
            {
                Id = "pascal-case-installed-pack",
                Name = "Pascal Case Installed Pack"
            };
        var envelope = new
        {
            Format = "homeschool-manager.coursepack",
            FormatVersion = 1,
            ExportedAtUtc = DateTimeOffset.UtcNow,
            PackageMode = "json",
            ArchiveNote = "Older downloaded course pack format.",
            Pack = pack
        };
        var content = JsonSerializer.SerializeToUtf8Bytes(envelope, new JsonSerializerOptions { WriteIndented = true });

        var install = await service.InstallCoursePackFileAsync(parent, content);
        AssertTrue(install.Succeeded, "Older PascalCase course pack download should install.");
        AssertEqual("pascal-case-installed-pack", install.Value!.Id, "Installed pack id should be returned.");
        AssertTrue((await service.ListCoursePacksAsync()).Any(item => item.Id == "pascal-case-installed-pack"), "Installed PascalCase pack should be listed.");
    }),
    ("Student role cannot seed Michigan requirements", async () =>
    {
        var repository = await CreateRepositoryAsync();
        var service = new RequirementService(repository);
        var result = await service.SeedMichiganAsync(UserContext.Student("Student"));
        AssertFalse(result.Succeeded, "Student seed command should fail.");
    }),
    ("Parent requirements extend requirement lists and mappings", async () =>
    {
        var repository = await CreateRepositoryAsync();
        await CreateSetupAsync(repository);
        var requirementService = new RequirementService(repository);
        var courseService = new CourseService(repository);
        var parent = UserContext.ParentAdmin("Parent");

        AssertFalse((await requirementService.AddParentRequirementAsync(
            UserContext.Student("Student"),
            new AddParentRequirementCommand("Statutory", "Family Civics Project", "Grade 12"))).Succeeded, "Student should not add parent requirements.");
        AssertTrue((await requirementService.AddParentRequirementAsync(
            parent,
            new AddParentRequirementCommand("Statutory", "Family Civics Project", "Grade 12"))).Succeeded, "Parent requirement add failed.");

        var checklist = await requirementService.GetChecklistAsync();
        var parentRequirement = checklist.FirstOrDefault(item =>
            item.View == "Statutory" &&
            item.Name == "Family Civics Project" &&
            item.RequiredOrRecommended == "Parent");
        if (parentRequirement is null)
        {
            throw new InvalidOperationException("Parent requirement was not listed.");
        }
        var refreshedChecklist = await requirementService.GetChecklistAsync();
        AssertEqual(1, refreshedChecklist.Count(item => item.Name == "Family Civics Project"), "Parent requirement should not duplicate on seed refresh.");

        var createResult = await courseService.CreateCourseAsync(parent, new CreateCourseCommand("Civics Practicum", "Applied civic learning.", [], CourseDuration.OneSemester, 0.5m));
        AssertTrue(createResult.Succeeded, "Course create without visible subject input should pass.");
        var courseId = createResult.Value;
        AssertTrue((await courseService.SetRequirementMappingsAsync(parent, new SetCourseRequirementMappingsCommand(
            courseId,
            [new RequirementMappingCommand(parentRequirement.RequirementAreaId, CoverageLevel.Primary, "Parent-added coverage.")]))).Succeeded, "Parent requirement mapping failed.");

        var coverage = await courseService.GetCoverageSummaryAsync();
        AssertTrue(coverage.Any(item => item.Name == "Family Civics Project" && item.IsMapped), "Parent requirement should appear in coverage summary.");
    }),
    ("Course requires title and valid planned credit", () =>
    {
        var studentId = Guid.NewGuid();
        var schoolYearId = Guid.NewGuid();
        AssertThrows<DomainException>(() => new Course(Guid.NewGuid(), studentId, schoolYearId, "", ["Science"], CourseDuration.TwoSemesters, 1, null, null, null, null, []));
        AssertThrows<DomainException>(() => new Course(Guid.NewGuid(), studentId, schoolYearId, "Biology", [], CourseDuration.TwoSemesters, 1, null, null, null, null, []));
        AssertThrows<DomainException>(() => new Course(Guid.NewGuid(), studentId, schoolYearId, "Biology", ["Science"], CourseDuration.TwoSemesters, 0, null, null, null, null, []));
        AssertThrows<DomainException>(() => new Course(Guid.NewGuid(), studentId, schoolYearId, "Biology", ["Science"], CourseDuration.TwoSemesters, 4, null, null, null, null, []));
        _ = new Course(Guid.NewGuid(), studentId, schoolYearId, "Biology", ["Science"], CourseDuration.TwoSemesters, 1, null, null, null, null, []);
        return Task.CompletedTask;
    }),
    ("Learning module requires course, title, instructions, objectives, and valid order", () =>
    {
        var courseId = Guid.NewGuid();
        AssertThrows<DomainException>(() => new LearningModule(Guid.NewGuid(), Guid.Empty, "", 1, "Foundations", "", "", "Read and discuss.", "Topic", "Explain core ideas.", "", "", ModuleStatus.Planned));
        AssertThrows<DomainException>(() => new LearningModule(Guid.NewGuid(), courseId, "", 0, "Foundations", "", "", "Read and discuss.", "Topic", "Explain core ideas.", "", "", ModuleStatus.Planned));
        AssertThrows<DomainException>(() => new LearningModule(Guid.NewGuid(), courseId, "", 1, "", "", "", "Read and discuss.", "Topic", "Explain core ideas.", "", "", ModuleStatus.Planned));
        AssertThrows<DomainException>(() => new LearningModule(Guid.NewGuid(), courseId, "", 1, "Foundations", "", "", "", "Topic", "Explain core ideas.", "", "", ModuleStatus.Planned));
        AssertThrows<DomainException>(() => new LearningModule(Guid.NewGuid(), courseId, "", 1, "Foundations", "", "", "Read and discuss.", "Topic", "", "", "", ModuleStatus.Planned));
        _ = new LearningModule(Guid.NewGuid(), courseId, "", 1, "Foundations", "", "", "Read and discuss.", "Topic", "Explain core ideas.", "", "Draft assignment evidence.", ModuleStatus.Planned);
        return Task.CompletedTask;
    }),
    ("Course modules are course-owned and ordered without goals", () =>
    {
        var courseId = Guid.NewGuid();
        var course = new Course(courseId, Guid.NewGuid(), Guid.NewGuid(), "Biology", ["Science"], CourseDuration.TwoSemesters, 1, null, null, null, null, []);
        var second = new LearningModule(Guid.NewGuid(), course.Id, "second", 2, "Second", "", "", "Instruction", "Topic", "Objective", "", "", ModuleStatus.Planned);
        var first = new LearningModule(Guid.NewGuid(), course.Id, "first", 1, "First", "", "", "Instruction", "Topic", "Objective", "", "", ModuleStatus.Planned);
        var otherCourseModule = new LearningModule(Guid.NewGuid(), Guid.NewGuid(), "other", 1, "Other", "", "", "Instruction", "Topic", "Objective", "", "", ModuleStatus.Planned);

        var updated = course.WithModules([second, first]);
        AssertEqual("First", updated.Modules[0].Title, "Modules should be ordered by sequence.");
        AssertEqual(1, updated.Modules[0].SequenceOrder, "Module sequence should be normalized.");
        AssertFalse(typeof(LearningModule).GetProperties().Any(property => property.Name == "Goals"), "Learning modules should not include a goals field.");
        AssertThrows<DomainException>(() => course.WithModules([otherCourseModule]));
        AssertThrows<DomainException>(() => course.WithModules([first, first with { Id = Guid.NewGuid() }]));
        return Task.CompletedTask;
    }),
    ("Lessons require module ownership intro text and resources", () =>
    {
        var moduleId = Guid.NewGuid();
        var resource = new LessonResource(Guid.NewGuid(), "OpenStax section", LessonResourceType.TextbookChapter, "https://openstax.org/", "", false, "Source note");
        AssertThrows<DomainException>(() => new Lesson(Guid.NewGuid(), Guid.Empty, "", 1, "Lesson", "Intro", "", [resource]));
        AssertThrows<DomainException>(() => new Lesson(Guid.NewGuid(), moduleId, "", 0, "Lesson", "Intro", "", [resource]));
        AssertThrows<DomainException>(() => new Lesson(Guid.NewGuid(), moduleId, "", 1, "", "Intro", "", [resource]));
        AssertThrows<DomainException>(() => new Lesson(Guid.NewGuid(), moduleId, "", 1, "Lesson", "", "", [resource]));
        AssertThrows<DomainException>(() => new Lesson(Guid.NewGuid(), moduleId, "", 1, "Lesson", "Intro", "", []));
        _ = new Lesson(Guid.NewGuid(), moduleId, "source-lesson", 1, "Lesson", "Intro", "Objective", [resource]);
        return Task.CompletedTask;
    }),
    ("Assignments require module ownership instructions output and valid links", () =>
    {
        var moduleId = Guid.NewGuid();
        var lessonId = Guid.NewGuid();
        var lesson = new Lesson(
            lessonId,
            moduleId,
            "source-lesson",
            1,
            "Lesson",
            "Introductory lesson text.",
            "Explain the concept.",
            LessonResources("OpenStax section").Select(command => new LessonResource(
                Guid.NewGuid(),
                command.Name,
                command.Type,
                command.Url,
                command.FilePath,
                command.IsPhysicalResource,
                command.SourceNote)).ToArray());

        AssertThrows<DomainException>(() => new ModuleAssignment(Guid.NewGuid(), Guid.Empty, "", 1, "Assignment", AssignmentType.Reflection, InstructionalMethodProfile.Hybrid, "Instructions", "", "", null, ["Explain the concept."], [lessonId], "Written reflection.", "", false, null, null, AssignmentStatus.Planned));
        AssertThrows<DomainException>(() => new ModuleAssignment(Guid.NewGuid(), moduleId, "", 0, "Assignment", AssignmentType.Reflection, InstructionalMethodProfile.Hybrid, "Instructions", "", "", null, ["Explain the concept."], [lessonId], "Written reflection.", "", false, null, null, AssignmentStatus.Planned));
        AssertThrows<DomainException>(() => new ModuleAssignment(Guid.NewGuid(), moduleId, "", 1, "", AssignmentType.Reflection, InstructionalMethodProfile.Hybrid, "Instructions", "", "", null, ["Explain the concept."], [lessonId], "Written reflection.", "", false, null, null, AssignmentStatus.Planned));
        AssertThrows<DomainException>(() => new ModuleAssignment(Guid.NewGuid(), moduleId, "", 1, "Assignment", AssignmentType.Reflection, InstructionalMethodProfile.Hybrid, "", "", "", null, ["Explain the concept."], [lessonId], "Written reflection.", "", false, null, null, AssignmentStatus.Planned));
        AssertThrows<DomainException>(() => new ModuleAssignment(Guid.NewGuid(), moduleId, "", 1, "Assignment", AssignmentType.Reflection, InstructionalMethodProfile.Hybrid, "Instructions", "", "", null, ["Explain the concept."], [lessonId], "", "", false, null, null, AssignmentStatus.Planned));
        AssertThrows<DomainException>(() => new ModuleAssignment(Guid.NewGuid(), moduleId, "", 1, "Assignment", (AssignmentType)99, InstructionalMethodProfile.Hybrid, "Instructions", "", "", null, ["Explain the concept."], [lessonId], "Written reflection.", "", false, null, null, AssignmentStatus.Planned));
        AssertThrows<DomainException>(() => new ModuleAssignment(Guid.NewGuid(), moduleId, "", 1, "Assignment", AssignmentType.Reflection, InstructionalMethodProfile.Hybrid, "Instructions", "", "", null, ["Explain the concept."], [lessonId], "Written reflection.", "", false, -1, null, AssignmentStatus.Planned));

        var module = new LearningModule(moduleId, Guid.NewGuid(), "", 1, "Module", "Overview", "", "Instructions", "Topics", "Explain the concept.", "", "", ModuleStatus.Active, lessons: [lesson]);
        var assignment = new ModuleAssignment(Guid.NewGuid(), module.Id, "source-assignment", 1, "Reflection", AssignmentType.Reflection, InstructionalMethodProfile.Hybrid, "Explain the idea in your own words.", "30 minutes", "After lesson 1", null, ["Explain the concept."], [lesson.Id], "One paragraph response.", "Parent review.", true, 10, null, AssignmentStatus.Planned);
        AssertThrows<DomainException>(() => module.WithAssignments([assignment with { ModuleId = Guid.NewGuid() }]));
        AssertThrows<DomainException>(() => module.WithAssignments([assignment with { LinkedLessonIds = [Guid.NewGuid()] }]));
        _ = module.WithAssignments([assignment]);
        return Task.CompletedTask;
    }),
    ("Requirement mapping requires course and requirement area", () =>
    {
        AssertThrows<DomainException>(() => new RequirementMapping(Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), CoverageLevel.Primary, ""));
        AssertThrows<DomainException>(() => new RequirementMapping(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, CoverageLevel.Primary, ""));
        AssertThrows<DomainException>(() => new RequirementMapping(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), (CoverageLevel)99, ""));
        _ = new RequirementMapping(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), CoverageLevel.Supporting, "Practical evidence");
        return Task.CompletedTask;
    }),
    ("Course mappings reject duplicates and mismatched courses", () =>
    {
        var course = new Course(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Biology", ["Science"], CourseDuration.TwoSemesters, 1, null, null, null, null, []);
        var requirementAreaId = Guid.NewGuid();
        var mapping = new RequirementMapping(Guid.NewGuid(), course.Id, requirementAreaId, CoverageLevel.Primary, "");
        var duplicate = new RequirementMapping(Guid.NewGuid(), course.Id, requirementAreaId, CoverageLevel.Secondary, "");
        var mismatched = new RequirementMapping(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), CoverageLevel.Primary, "");

        AssertThrows<DomainException>(() => course.WithMappings([mismatched]));
        AssertThrows<DomainException>(() => course.WithMappings([mapping, duplicate]));
        _ = course.WithMappings([mapping]);
        return Task.CompletedTask;
    }),
    ("Parent can create course and student cannot mutate courses", async () =>
    {
        var repository = await CreateRepositoryAsync();
        await CreateSetupAsync(repository);
        var service = new CourseService(repository);

        var studentResult = await service.CreateCourseAsync(
            UserContext.Student("Student"),
            new CreateCourseCommand("Biology", "Life science overview.", ["Science"], CourseDuration.TwoSemesters, 1));
        AssertFalse(studentResult.Succeeded, "Student course creation should fail.");

        var parentResult = await service.CreateCourseAsync(
            UserContext.ParentAdmin("Parent"),
            new CreateCourseCommand("Biology", "Life science overview.", ["Science"], CourseDuration.TwoSemesters, 1));
        AssertTrue(parentResult.Succeeded, "Parent course creation should pass.");

        var courses = await service.ListCoursesAsync();
        AssertEqual(1, courses.Count, "Unexpected course count.");
        AssertEqual("Biology", courses[0].Title, "Unexpected course title.");
        AssertEqual("Life science overview.", courses[0].Description, "Unexpected course description.");
        AssertEqual(1, courses[0].SubjectAreas.Count, "Unexpected subject area count.");
    }),
    ("Parent can create update and reorder modules while student cannot mutate them", (Func<Task>)(async () =>
    {
        var repository = await CreateRepositoryAsync();
        await CreateSetupAsync(repository);
        var service = new CourseService(repository);
        var parent = UserContext.ParentAdmin("Parent");
        var student = UserContext.Student("Student");

        var courseResult = await service.CreateCourseAsync(parent, new CreateCourseCommand("Civics", "Government course.", [], CourseDuration.OneSemester, 0.5m));
        AssertTrue(courseResult.Succeeded, "Parent should create course.");
        var courseId = courseResult.Value;

        var denied = await service.CreateLearningModuleAsync(student, new CreateLearningModuleCommand(
            courseId,
            "Constitutional Foundations",
            "Overview",
            null,
            "2 weeks",
            "Read, discuss, and summarize source material.",
            Objectives("Explain constitutional principles."),
            Resources("Primary source excerpts"),
            "Short written explanation.",
            ModuleStatus.Planned));
        AssertFalse(denied.Succeeded, "Student should not create modules.");

        var first = await service.CreateLearningModuleAsync(parent, new CreateLearningModuleCommand(
            courseId,
            "Constitutional Foundations",
            "Overview",
            null,
            "2 weeks",
            "Read, discuss, and summarize source material.",
            Objectives("Explain constitutional principles."),
            Resources("Primary source excerpts"),
            "Short written explanation.",
            ModuleStatus.Planned));
        var second = await service.CreateLearningModuleAsync(parent, new CreateLearningModuleCommand(
            courseId,
            "Civic Participation",
            "Participation overview",
            null,
            "2 weeks",
            "Compare civic actions and document a response.",
            Objectives("Evaluate civic participation options."),
            Resources("iCivics resources"),
            "Reflection placeholder.",
            ModuleStatus.Planned));
        AssertTrue(first.Succeeded, "First module should create.");
        AssertTrue(second.Succeeded, "Second module should create.");

        var updateDenied = await service.UpdateLearningModuleAsync(student, new UpdateLearningModuleCommand(
            courseId,
            first.Value,
            "Denied",
            "",
            null,
            "",
            "Instruction",
            Objectives("Objective"),
            Resources("Resource"),
            "",
            ModuleStatus.Active));
        AssertFalse(updateDenied.Succeeded, "Student should not update modules.");

        var update = await service.UpdateLearningModuleAsync(parent, new UpdateLearningModuleCommand(
            courseId,
            first.Value,
            "Constitutional Foundations",
            "Overview",
            null,
            "3 weeks",
            "Read, discuss, summarize, and compare constitutional sources.",
            Objectives("Explain constitutional principles.", "Compare U.S. and Michigan constitutional structures."),
            Resources("Primary source excerpts"),
            "Short written explanation.",
            ModuleStatus.Active));
        AssertTrue(update.Succeeded, "Parent should update module.");

        var reorder = await service.ReorderLearningModulesAsync(parent, new ReorderLearningModulesCommand(courseId, [second.Value, first.Value]));
        AssertTrue(reorder.Succeeded, "Parent should reorder modules.");

        var modules = await service.ListModulesAsync(courseId);
        AssertEqual("Civic Participation", modules[0].Title, "Modules should be reordered.");
        AssertEqual(1, modules[0].SequenceOrder, "Reordered module sequence should normalize.");
        AssertEqual((int)ModuleStatus.Active, (int)modules[1].Status, "Module update should persist.");

        var detail = await service.GetModuleDetailAsync(courseId, first.Value);
        if (detail is null)
        {
            throw new InvalidOperationException("Module detail was not found.");
        }

        AssertTrue(detail.LearningObjectives.Contains("Michigan constitutional structures", StringComparison.OrdinalIgnoreCase), "Module detail should include updated objectives.");
        var deleteDenied = await service.DeleteLearningModuleAsync(parent, new DeleteLearningModuleCommand(courseId, first.Value, "delete"));
        AssertFalse(deleteDenied.Succeeded, "Module deletion should require exact confirmation text.");
        var deleteResult = await service.DeleteLearningModuleAsync(parent, new DeleteLearningModuleCommand(courseId, first.Value, "Delete"));
        AssertTrue(deleteResult.Succeeded, "Parent should delete module after confirmation.");
        AssertFalse((await service.ListModulesAsync(courseId)).Any(module => module.Id == first.Value), "Deleted module should be removed.");
    })),
    ("Module packs download templates and import module shells without lesson or assignment bodies", async () =>
    {
        var repository = await CreateRepositoryAsync();
        await CreateSetupAsync(repository);
        var service = new CourseService(repository);
        var parent = UserContext.ParentAdmin("Parent");
        var student = UserContext.Student("Student");

        var course = await service.CreateCourseAsync(parent, new CreateCourseCommand("Biology", "Life science overview.", ["Science"], CourseDuration.TwoSemesters, 1));
        AssertTrue(course.Succeeded, "Course create failed.");
        var module = await service.CreateLearningModuleAsync(
            parent,
            new CreateLearningModuleCommand(
                course.Value,
                "Cells",
                "Cell biology module.",
                null,
                "2 weeks",
                "Study cells.",
                Objectives("Explain cell structure."),
                Resources("OpenStax Cells"),
                "Cell diagram evidence.",
                ModuleStatus.Active));
        AssertTrue(module.Succeeded, "Module create failed.");
        var lesson = await service.CreateLessonAsync(parent, new CreateLessonCommand(course.Value, module.Value, "Cell structure", "Study the major parts of cells.", "Explain cell structure.", LessonResources("OpenStax Cells")));
        AssertTrue(lesson.Succeeded, "Lesson create failed.");
        var assignment = await service.CreateAssignmentAsync(
            parent,
            new CreateAssignmentCommand(
                course.Value,
                module.Value,
                "Cell structure reflection",
                AssignmentType.Reflection,
                InstructionalMethodProfile.Hybrid,
                "Explain the function of cell structures.",
                "45 minutes",
                "After lesson 1",
                null,
                ["Explain cell structure."],
                [lesson.Value],
                "Written reflection.",
                "",
                true,
                20,
                null,
                AssignmentStatus.Planned));
        AssertTrue(assignment.Succeeded, "Assignment create failed.");

        var template = service.DownloadModulePackTemplate();
        AssertTrue(template.Succeeded, "Module pack template should download.");
        AssertTrue(template.Value?.FileName.EndsWith(".modulepack", StringComparison.Ordinal) ?? false, "Template should use .modulepack.");
        using (var templateDocument = JsonDocument.Parse(template.Value!.Content))
        {
            var root = templateDocument.RootElement;
            AssertEqual("homeschool-manager.modulepack", root.GetProperty("format").GetString() ?? "", "Template should use modulepack format.");
            var templateModule = root.GetProperty("module");
            AssertTrue(templateModule.TryGetProperty("lessonSequence", out _), "Template should include lesson sequence references.");
            AssertTrue(templateModule.TryGetProperty("assignmentSequence", out _), "Template should include assignment sequence references.");
        }

        var download = await service.DownloadModulePackAsync(course.Value, module.Value);
        AssertTrue(download.Succeeded, "Module pack download should succeed.");
        AssertTrue(download.Value?.FileName.EndsWith(".modulepack", StringComparison.Ordinal) ?? false, "Download should use .modulepack.");
        using (var downloadDocument = JsonDocument.Parse(download.Value!.Content))
        {
            var downloadedModule = downloadDocument.RootElement.GetProperty("module");
            AssertEqual("Cells", downloadedModule.GetProperty("title").GetString() ?? "", "Downloaded module should include module title.");
            AssertEqual(1, downloadedModule.GetProperty("lessonSequence").GetArrayLength(), "Downloaded module should include lightweight lesson sequencing.");
            AssertEqual(1, downloadedModule.GetProperty("assignmentSequence").GetArrayLength(), "Downloaded module should include lightweight assignment sequencing.");
            AssertFalse(downloadedModule.TryGetProperty("lessons", out _), "Module pack should not embed lesson details.");
            AssertFalse(downloadedModule.TryGetProperty("assignments", out _), "Module pack should not embed assignment details.");
        }

        var importJson = """
            {
              "format": "homeschool-manager.modulepack",
              "formatVersion": 1,
              "downloadedAtUtc": "2026-06-07T00:00:00+00:00",
              "packageMode": "json",
              "archiveNote": "No attached files.",
              "name": "Cells Module Shell",
              "description": "A module shell without lesson or assignment bodies.",
              "module": {
                "sourceModuleId": "cells-module-shell",
                "sequenceOrder": 1,
                "title": "Imported Cells",
                "description": "Imported module description.",
                "termName": "",
                "estimatedLength": "3 weeks",
                "instructions": "Work through installed lessons and assignments after importing them separately.",
                "learningObjectives": [
                  {
                    "text": "Explain how cell parts work together.",
                    "linkedCourseObjective": ""
                  }
                ],
                "resources": [
                  {
                    "name": "Module overview resource",
                    "link": "https://example.com/module",
                    "filePath": "",
                    "isPhysicalResource": false
                  }
                ],
                "assignmentEvidencePlaceholder": "Import matching lessonpack and assignmentpack files next.",
                "status": "Planned",
                "lessonSequence": [
                  {
                    "sourceId": "cells-lesson-1",
                    "title": "Cell structure",
                    "sequenceOrder": 1
                  }
                ],
                "assignmentSequence": [
                  {
                    "sourceId": "cells-assignment-1",
                    "title": "Cell reflection",
                    "sequenceOrder": 1
                  }
                ]
              }
            }
            """;

        var denied = await service.ImportModulePackAsync(student, course.Value, System.Text.Encoding.UTF8.GetBytes(importJson));
        AssertFalse(denied.Succeeded, "Student should not import module packs.");

        var import = await service.ImportModulePackAsync(parent, course.Value, System.Text.Encoding.UTF8.GetBytes(importJson));
        AssertTrue(import.Succeeded, "Module pack import should succeed.");
        var imported = await service.GetModuleDetailAsync(course.Value, import.Value?.ModuleId ?? Guid.Empty);
        if (imported is null)
        {
            throw new InvalidOperationException("Imported module was not found.");
        }

        AssertEqual("Imported Cells", imported.Title, "Imported module title should persist.");
        AssertEqual(0, imported.Lessons.Count, "Module pack import should not create lesson bodies.");
        AssertEqual(0, imported.Assignments.Count, "Module pack import should not create assignment bodies.");
    }),
    ("Parent can create update and reorder lessons while student cannot mutate them", async () =>
    {
        var repository = await CreateRepositoryAsync();
        await CreateSetupAsync(repository);
        var service = new CourseService(repository);
        var parent = UserContext.ParentAdmin("Parent");
        var course = await service.CreateCourseAsync(parent, new CreateCourseCommand("Biology", "Life science overview.", ["Science"], CourseDuration.TwoSemesters, 1));
        AssertTrue(course.Succeeded, "Course create failed.");
        var module = await service.CreateLearningModuleAsync(
            parent,
            new CreateLearningModuleCommand(
                course.Value,
                "Cells",
                "Cell biology module.",
                null,
                "2 weeks",
                "Study cells.",
                Objectives("Explain cell structure."),
                [],
                "Cell diagram evidence.",
                ModuleStatus.Active));
        AssertTrue(module.Succeeded, "Module create failed.");

        var studentCreate = await service.CreateLessonAsync(
            UserContext.Student("Student"),
            new CreateLessonCommand(course.Value, module.Value, "Cell structure", "Study the major parts of cells.", "Explain cell structure.", LessonResources("OpenStax Cells")));
        AssertFalse(studentCreate.Succeeded, "Student should not create lessons.");

        var firstLesson = await service.CreateLessonAsync(
            parent,
            new CreateLessonCommand(course.Value, module.Value, "Cell structure", "Study the major parts of cells.", "Explain cell structure.", LessonResources("OpenStax Cells")));
        AssertTrue(firstLesson.Succeeded, "Lesson create failed.");
        var secondLesson = await service.CreateLessonAsync(
            parent,
            new CreateLessonCommand(course.Value, module.Value, "Cell evidence", "Use diagrams to explain cell structures.", "Explain cell structure.", LessonResources("Cell diagram")));
        AssertTrue(secondLesson.Succeeded, "Second lesson create failed.");

        var update = await service.UpdateLessonAsync(
            parent,
            new UpdateLessonCommand(course.Value, module.Value, firstLesson.Value, "Cell structure and function", "Read, watch, and summarize cell structures.", "Explain cell structure.", LessonResources("OpenStax Biology 2e")));
        AssertTrue(update.Succeeded, "Lesson update failed.");
        var reorder = await service.ReorderLessonsAsync(parent, new ReorderLessonsCommand(course.Value, module.Value, [secondLesson.Value, firstLesson.Value]));
        AssertTrue(reorder.Succeeded, "Lesson reorder failed.");

        var lessons = await service.ListLessonsAsync(course.Value, module.Value);
        AssertEqual("Cell evidence", lessons[0].Title, "Lessons should reorder.");
        AssertEqual("Cell structure and function", lessons[1].Title, "Lesson update should persist.");

        var deleteDenied = await service.DeleteLessonAsync(parent, new DeleteLessonCommand(course.Value, module.Value, secondLesson.Value, "delete"));
        AssertFalse(deleteDenied.Succeeded, "Lesson delete should require exact confirmation.");
        var delete = await service.DeleteLessonAsync(parent, new DeleteLessonCommand(course.Value, module.Value, secondLesson.Value, "Delete"));
        AssertTrue(delete.Succeeded, "Lesson delete failed.");
        AssertFalse((await service.ListLessonsAsync(course.Value, module.Value)).Any(lesson => lesson.Id == secondLesson.Value), "Deleted lesson should be removed.");
    }),
    ("Lesson packs download templates and import one or more lessons into a module", (Func<Task>)(async () =>
    {
        var repository = await CreateRepositoryAsync();
        await CreateSetupAsync(repository);
        var service = new CourseService(repository);
        var parent = UserContext.ParentAdmin("Parent");
        var student = UserContext.Student("Student");
        var course = await service.CreateCourseAsync(parent, new CreateCourseCommand("Biology", "Life science overview.", ["Science"], CourseDuration.TwoSemesters, 1));
        AssertTrue(course.Succeeded, "Course create failed.");
        var module = await service.CreateLearningModuleAsync(
            parent,
            new CreateLearningModuleCommand(
                course.Value,
                "Cells",
                "Cell biology module.",
                null,
                "2 weeks",
                "Study cells.",
                Objectives("Explain cell structure."),
                [],
                "Cell diagram evidence.",
                ModuleStatus.Active));
        AssertTrue(module.Succeeded, "Module create failed.");
        var linkedAssignment = await service.CreateAssignmentAsync(
            parent,
            new CreateAssignmentCommand(
                course.Value,
                module.Value,
                "Cell Evidence Assignment",
                AssignmentType.PortfolioArtifact,
                InstructionalMethodProfile.Hybrid,
                "Create evidence showing how cell structures support cell function.",
                "45 minutes",
                "After the organelles lesson",
                null,
                ["Explain cell structure."],
                [],
                "Annotated diagram or written explanation.",
                "",
                true,
                20,
                null,
                AssignmentStatus.Planned));
        AssertTrue(linkedAssignment.Succeeded, "Linked assignment create failed.");

        var template = service.DownloadLessonPackTemplate();
        AssertTrue(template.Succeeded, "Lesson pack template should download.");
        AssertTrue(template.Value?.FileName.EndsWith(".lessonpack", StringComparison.Ordinal) ?? false, "Template should use .lessonpack.");
        using (var templateDocument = JsonDocument.Parse(template.Value!.Content))
        {
            var root = templateDocument.RootElement;
            AssertEqual("homeschool-manager.lessonpack", root.GetProperty("format").GetString() ?? "", "Template should use lessonpack format.");
            AssertTrue(root.GetProperty("lessons").GetArrayLength() >= 1, "Template should include at least one sample lesson.");
            var lesson = root.GetProperty("lessons")[0];
            AssertTrue(lesson.TryGetProperty("lessonType", out _), "Template should include lesson type.");
            AssertTrue(lesson.TryGetProperty("learningObjectives", out _), "Template should include lesson objectives.");
            AssertTrue(lesson.TryGetProperty("successCriteria", out _), "Template should include success criteria.");
            AssertTrue(lesson.TryGetProperty("lessonSteps", out _), "Template should include lesson steps.");
            AssertTrue(lesson.TryGetProperty("problemSets", out _), "Template should include problem sets.");
            AssertTrue(lesson.TryGetProperty("portfolioConnections", out _), "Template should include portfolio connections.");
            AssertTrue(lesson.TryGetProperty("rubric", out _), "Template should include rubric.");
            AssertTrue(lesson.TryGetProperty("instructorNotes", out _), "Template should include instructor notes.");
            var resource = lesson.GetProperty("resources")[0];
            AssertTrue(resource.TryGetProperty("filePath", out _), "Template should include the full resource filePath field.");
            AssertTrue(resource.TryGetProperty("isPhysicalResource", out _), "Template should include the full resource physical-resource field.");
            AssertTrue(resource.TryGetProperty("studentInstructions", out _), "Template should include resource student instructions.");
        }

        var lessonPackJson = """
            {
              "format": "homeschool-manager.lessonpack",
              "formatVersion": 1,
              "downloadedAtUtc": "2026-06-07T00:00:00+00:00",
              "packageMode": "json",
              "archiveNote": "No attached files.",
              "name": "Cells Mini Lesson Pack",
              "description": "Two cell lessons.",
              "lessons": [
                {
                  "sourceLessonId": "cells-pack-lesson-1",
                  "sequenceOrder": 1,
                  "title": "Cell Organelles",
                  "introductoryText": "Learn how major organelles help a cell perform life functions.",
                  "linkedModuleObjective": "Explain cell structure.",
                  "lessonType": "SelfGuided",
                  "estimatedMinutes": 90,
                  "suggestedDays": 2,
                  "difficultyLevel": "StandardHighSchool",
                  "subjectAreas": [ "Science" ],
                  "tags": [ "cells", "organelles" ],
                  "prerequisites": [ "Basic microscope vocabulary" ],
                  "learningObjectives": [
                    {
                      "objectiveId": "cell-organelle-1",
                      "text": "Explain how major organelles support cell function.",
                      "bloomLevel": "Understand"
                    }
                  ],
                  "standardsAlignments": [
                    {
                      "framework": "Parent-defined",
                      "code": "BIO-CELL-01",
                      "description": "Explains cell organelles and their functions."
                    }
                  ],
                  "successCriteria": [
                    "I can identify major organelles.",
                    "I can explain each organelle's job."
                  ],
                  "lessonSteps": [
                    {
                      "stepOrder": 1,
                      "title": "Read and annotate",
                      "stepType": "Planning",
                      "instructions": "Read the section and annotate organelle functions.",
                      "estimatedMinutes": 30,
                      "required": true
                    },
                    {
                      "stepOrder": 2,
                      "title": "Research vocabulary",
                      "stepType": "Research",
                      "instructions": "Find one reliable explanation of a cell organelle.",
                      "estimatedMinutes": 15,
                      "required": true
                    }
                  ],
                  "resources": [
                    {
                      "name": "OpenStax Biology 2e - Eukaryotic Cells",
                      "type": "DataSource",
                      "url": "https://openstax.org/books/biology-2e/pages/4-4-eukaryotic-cells",
                      "filePath": "",
                      "isPhysicalResource": false,
                      "sourceNote": "Open textbook section.",
                      "required": true,
                      "estimatedMinutes": 25,
                      "studentInstructions": "Identify three organelles and explain their jobs.",
                      "notesPrompt": "Which organelle seems most important for energy?",
                      "citation": {
                        "title": "Eukaryotic Cells",
                        "publisher": "OpenStax",
                        "accessedAtUtc": "2026-06-07T00:00:00+00:00"
                      },
                      "offlineAvailable": false,
                      "license": "Check OpenStax license terms."
                    }
                  ],
                  "problemSets": [
                    {
                      "problemSetId": "cells-ps-1",
                      "title": "Cell Function Practice",
                      "instructions": "Answer in complete sentences.",
                      "estimatedMinutes": 20,
                      "problems": [
                        {
                          "problemId": "cells-ps-1-1",
                          "prompt": "Which organelle releases usable energy for the cell?",
                          "responseType": "GraphAndWrittenAnalysis",
                          "expectedAnswer": "Mitochondrion",
                          "solution": "Mitochondria release usable energy through cellular respiration.",
                          "skills": [ "cell vocabulary" ],
                          "difficulty": "Medium"
                        }
                      ]
                    }
                  ],
                  "portfolioConnections": [
                    {
                      "portfolioSection": "Biology Portfolio",
                      "artifactTitle": "Cell Function Diagram",
                      "artifactPurpose": "Shows understanding of organelles and function.",
                      "crossCourseLinks": [ "Scientific Communication" ],
                      "reuseInstructions": "Revise when studying body systems."
                    }
                  ],
                  "rubric": {
                    "rubricId": "cell-function-rubric",
                    "scale": "4-point",
                    "criteria": [
                      {
                        "criterion": "Accuracy",
                        "level4": "All organelles are accurate and explained.",
                        "level3": "Most organelles are accurate.",
                        "level2": "Some organelles are confused.",
                        "level1": "Major organelles are missing."
                      }
                    ]
                  },
                  "reflectionPrompts": [ "Which cell structure was most surprising?" ],
                  "instructorNotes": {
                    "overview": "Look for accurate function explanations.",
                    "lookFors": [ "Student explains structure and function." ],
                    "commonIssues": [ "Confusing nucleus and nucleolus." ],
                    "suggestedFeedback": [ "Ask for one analogy per organelle." ]
                  },
                  "linkedAssignmentSourceIds": [],
                  "linkedAssignmentTitles": [ "Cell Evidence Assignment" ]
                },
                {
                  "sourceLessonId": "cells-pack-lesson-2",
                  "sequenceOrder": 2,
                  "title": "Cell Membranes",
                  "introductoryText": "Study how cell membranes regulate movement into and out of cells.",
                  "linkedModuleObjective": "Explain cell structure.",
                  "resources": [
                    {
                      "name": "Physical biology textbook",
                      "type": "PhysicalResource",
                      "url": "",
                      "filePath": "",
                      "isPhysicalResource": true,
                      "sourceNote": "Parent-selected chapter."
                    }
                  ]
                }
              ]
            }
            """;

        var denied = await service.ImportLessonPackAsync(student, course.Value, module.Value, System.Text.Encoding.UTF8.GetBytes(lessonPackJson));
        AssertFalse(denied.Succeeded, "Student should not import lesson packs.");

        var import = await service.ImportLessonPackAsync(parent, course.Value, module.Value, System.Text.Encoding.UTF8.GetBytes(lessonPackJson));
        AssertTrue(import.Succeeded, "Lesson pack import should succeed.");
        AssertEqual(2, import.Value?.LessonCount ?? 0, "Lesson pack should import both lessons.");
        var lessons = await service.ListLessonsAsync(course.Value, module.Value);
        AssertEqual(2, lessons.Count, "Module should contain imported lessons.");
        AssertEqual("Cell Organelles", lessons[0].Title, "First imported lesson should keep pack order.");
        AssertTrue(lessons[0].LessonType == LessonType.SelfGuided, "Lesson type should import.");
        AssertEqual(90, lessons[0].EstimatedMinutes, "Estimated minutes should import.");
        AssertTrue(lessons[0].LearningObjectives.Any(objective => objective.Text.Contains("organelles", StringComparison.OrdinalIgnoreCase)), "Lesson objectives should import.");
        AssertTrue(lessons[0].SuccessCriteria.Count > 0, "Success criteria should import.");
        AssertTrue(lessons[0].LessonSteps.Count > 0, "Lesson steps should import.");
        AssertTrue(lessons[0].LessonSteps[0].StepType == LessonStepType.Planning, "Planning lesson steps should import.");
        AssertTrue(lessons[0].LessonSteps[1].StepType == LessonStepType.Research, "Research lesson steps should import.");
        AssertTrue(lessons[0].ProblemSets.Count > 0, "Problem sets should import.");
        AssertTrue(lessons[0].ProblemSets[0].Problems[0].ResponseType == ProblemResponseType.GraphAndWrittenAnalysis, "Graph and written analysis problem responses should import.");
        AssertTrue(lessons[0].PortfolioConnections.Count > 0, "Portfolio connections should import.");
        AssertTrue(lessons[0].Rubric?.Criteria.Count > 0, "Rubric should import.");
        AssertTrue(lessons[0].InstructorNotes?.LookFors.Count > 0, "Instructor notes should import.");
        AssertTrue(lessons[0].LinkedAssignmentIds.Contains(linkedAssignment.Value), "Lesson should link to matching module assignment.");
        AssertEqual("Identify three organelles and explain their jobs.", lessons[0].Resources[0].StudentInstructions, "Resource student instructions should import.");
        AssertTrue(lessons[0].Resources[0].Type == LessonResourceType.DataSource, "Data source resources should import.");
        AssertTrue(lessons[1].Resources[0].IsPhysicalResource, "Physical-resource marker should import.");

        var download = await service.DownloadModuleLessonPackAsync(course.Value, module.Value);
        AssertTrue(download.Succeeded, "Module lesson pack download should succeed.");
        AssertTrue(download.Value?.FileName.EndsWith(".lessonpack", StringComparison.Ordinal) ?? false, "Download should use .lessonpack.");
        using var downloadDocument = JsonDocument.Parse(download.Value!.Content);
        AssertEqual(2, downloadDocument.RootElement.GetProperty("lessons").GetArrayLength(), "Downloaded module lesson pack should include current module lessons.");

        var duplicateImport = await service.ImportLessonPackAsync(parent, course.Value, module.Value, System.Text.Encoding.UTF8.GetBytes(lessonPackJson));
        AssertTrue(duplicateImport.Succeeded, "Importing the same lesson pack again should append safely.");
        AssertEqual(4, (await service.ListLessonsAsync(course.Value, module.Value)).Count, "Repeated lesson pack import should append lessons.");
    })),
    ("Parent can create update and reorder assignments while student cannot mutate them", async () =>
    {
        var repository = await CreateRepositoryAsync();
        await CreateSetupAsync(repository);
        var service = new CourseService(repository);
        var parent = UserContext.ParentAdmin("Parent");
        var student = UserContext.Student("Student");
        var course = await service.CreateCourseAsync(parent, new CreateCourseCommand("Biology", "Life science overview.", ["Science"], CourseDuration.TwoSemesters, 1));
        AssertTrue(course.Succeeded, "Course create failed.");
        var module = await service.CreateLearningModuleAsync(
            parent,
            new CreateLearningModuleCommand(
                course.Value,
                "Cells",
                "Cell biology module.",
                null,
                "2 weeks",
                "Study cells.",
                Objectives("Explain cell structure."),
                [],
                "Cell diagram evidence.",
                ModuleStatus.Active));
        AssertTrue(module.Succeeded, "Module create failed.");
        var lesson = await service.CreateLessonAsync(
            parent,
            new CreateLessonCommand(course.Value, module.Value, "Cell structure", "Study the major parts of cells.", "Explain cell structure.", LessonResources("OpenStax Cells")));
        AssertTrue(lesson.Succeeded, "Lesson create failed.");

        var denied = await service.CreateAssignmentAsync(
            student,
            new CreateAssignmentCommand(
                course.Value,
                module.Value,
                "Cell reflection",
                AssignmentType.Reflection,
                InstructionalMethodProfile.Hybrid,
                "Explain the cell structures from the lesson.",
                "30 minutes",
                "After lesson 1",
                null,
                ["Explain cell structure."],
                [lesson.Value],
                "Written reflection.",
                "",
                true,
                10,
                null,
                AssignmentStatus.Planned));
        AssertFalse(denied.Succeeded, "Student should not create assignments.");

        var first = await service.CreateAssignmentAsync(
            parent,
            new CreateAssignmentCommand(
                course.Value,
                module.Value,
                "Cell reflection",
                AssignmentType.Reflection,
                InstructionalMethodProfile.Hybrid,
                "Explain the cell structures from the lesson.",
                "30 minutes",
                "After lesson 1",
                null,
                ["Explain cell structure."],
                [lesson.Value],
                "Written reflection.",
                "Parent should check vocabulary.",
                true,
                10,
                null,
                AssignmentStatus.Planned));
        AssertTrue(first.Succeeded, "Assignment create failed.");
        var second = await service.CreateAssignmentAsync(
            parent,
            new CreateAssignmentCommand(
                course.Value,
                module.Value,
                "Cell diagram",
                AssignmentType.PortfolioArtifact,
                InstructionalMethodProfile.ProjectBasedApplied,
                "Create and label a cell diagram.",
                "45 minutes",
                "After lesson 1",
                null,
                ["Explain cell structure."],
                [lesson.Value],
                "Labeled diagram.",
                "",
                true,
                15,
                null,
                AssignmentStatus.Assigned));
        AssertTrue(second.Succeeded, "Second assignment create failed.");

        var update = await service.UpdateAssignmentAsync(
            parent,
            new UpdateAssignmentCommand(
                course.Value,
                module.Value,
                first.Value,
                "Cell structure reflection",
                AssignmentType.Reflection,
                InstructionalMethodProfile.Hybrid,
                "Explain the function of each cell structure in your own words.",
                "40 minutes",
                "After lesson 1",
                null,
                ["Explain cell structure."],
                [lesson.Value],
                "One-page written reflection.",
                "Look for structure/function connections.",
                true,
                12,
                null,
                AssignmentStatus.Assigned));
        AssertTrue(update.Succeeded, "Assignment update failed.");
        AssertFalse((await service.UpdateAssignmentAsync(
            student,
            new UpdateAssignmentCommand(course.Value, module.Value, first.Value, "Denied", AssignmentType.Reflection, InstructionalMethodProfile.Hybrid, "Instructions", "", "", null, ["Explain cell structure."], [lesson.Value], "Output.", "", false, null, null, AssignmentStatus.Planned))).Succeeded, "Student should not update assignments.");

        var reorder = await service.ReorderAssignmentsAsync(parent, new ReorderAssignmentsCommand(course.Value, module.Value, [second.Value, first.Value]));
        AssertTrue(reorder.Succeeded, "Assignment reorder failed.");
        var assignments = await service.ListAssignmentsAsync(course.Value, module.Value);
        AssertEqual("Cell diagram", assignments[0].Title, "Assignments should reorder.");
        AssertEqual("Cell structure reflection", assignments[1].Title, "Assignment update should persist.");
        AssertTrue(assignments[1].LinkedLessonIds.Contains(lesson.Value), "Assignment should keep lesson links.");
        AssertTrue(assignments[1].IsPortfolioCandidate, "Portfolio marker should persist.");

        var deleteDenied = await service.DeleteAssignmentAsync(parent, new DeleteAssignmentCommand(course.Value, module.Value, second.Value, "delete"));
        AssertFalse(deleteDenied.Succeeded, "Assignment delete should require exact confirmation.");
        var delete = await service.DeleteAssignmentAsync(parent, new DeleteAssignmentCommand(course.Value, module.Value, second.Value, "Delete"));
        AssertTrue(delete.Succeeded, "Assignment delete failed.");
        AssertFalse((await service.ListAssignmentsAsync(course.Value, module.Value)).Any(assignment => assignment.Id == second.Value), "Deleted assignment should be removed.");
    }),
    ("Assignment packs download templates and import one or more assignments into a module", (Func<Task>)(async () =>
    {
        var repository = await CreateRepositoryAsync();
        await CreateSetupAsync(repository);
        var service = new CourseService(repository);
        var parent = UserContext.ParentAdmin("Parent");
        var student = UserContext.Student("Student");
        var course = await service.CreateCourseAsync(parent, new CreateCourseCommand("Biology", "Life science overview.", ["Science"], CourseDuration.TwoSemesters, 1));
        AssertTrue(course.Succeeded, "Course create failed.");
        var module = await service.CreateLearningModuleAsync(
            parent,
            new CreateLearningModuleCommand(
                course.Value,
                "Cells",
                "Cell biology module.",
                null,
                "2 weeks",
                "Study cells.",
                Objectives("Explain cell structure."),
                [],
                "Cell diagram evidence.",
                ModuleStatus.Active));
        AssertTrue(module.Succeeded, "Module create failed.");
        var lesson = await service.CreateLessonAsync(
            parent,
            new CreateLessonCommand(course.Value, module.Value, "Cell structure", "Study the major parts of cells.", "Explain cell structure.", LessonResources("OpenStax Cells")));
        AssertTrue(lesson.Succeeded, "Lesson create failed.");

        var template = service.DownloadAssignmentPackTemplate();
        AssertTrue(template.Succeeded, "Assignment pack template should download.");
        AssertTrue(template.Value?.FileName.EndsWith(".assignmentpack", StringComparison.Ordinal) ?? false, "Template should use .assignmentpack.");
        using (var templateDocument = JsonDocument.Parse(template.Value!.Content))
        {
            var root = templateDocument.RootElement;
            AssertEqual("homeschool-manager.assignmentpack", root.GetProperty("format").GetString() ?? "", "Template should use assignmentpack format.");
            AssertTrue(root.GetProperty("assignments").GetArrayLength() >= 1, "Template should include at least one sample assignment.");
            var assignment = root.GetProperty("assignments")[0];
            AssertTrue(assignment.TryGetProperty("linkedLessonSourceIds", out _), "Template should include lesson source-id links.");
            AssertTrue(assignment.TryGetProperty("linkedLessonTitles", out _), "Template should include lesson title links.");
            AssertTrue(assignment.TryGetProperty("requiredOutput", out _), "Template should include required output.");
            AssertTrue(assignment.TryGetProperty("plannedPoints", out _), "Template should include planned points.");
            AssertTrue(assignment.TryGetProperty("assignmentSummary", out _), "Template should include assignment summary.");
            AssertTrue(assignment.TryGetProperty("requiredDeliverables", out _), "Template should include required deliverables.");
            AssertTrue(assignment.TryGetProperty("submissionFormats", out _), "Template should include submission formats.");
            AssertTrue(assignment.TryGetProperty("portfolioConnection", out _), "Template should include portfolio connection.");
            AssertTrue(assignment.TryGetProperty("rubric", out _), "Template should include rubric.");
            AssertTrue(assignment.TryGetProperty("assignmentSteps", out _), "Template should include assignment steps.");
            AssertTrue(assignment.TryGetProperty("completionCriteria", out _), "Template should include completion criteria.");
            AssertTrue(assignment.TryGetProperty("evidenceRequirements", out _), "Template should include evidence requirements.");
            AssertTrue(assignment.TryGetProperty("scoring", out _), "Template should include scoring.");
        }

        var assignmentPackJson = """
            {
              "format": "homeschool-manager.assignmentpack",
              "formatVersion": 1,
              "downloadedAtUtc": "2026-06-07T00:00:00+00:00",
              "packageMode": "json",
              "archiveNote": "No attached files.",
              "name": "Cells Assignment Pack",
              "description": "Two cell assignments.",
              "assignments": [
                {
                  "sourceAssignmentId": "cells-pack-assignment-1",
                  "sequenceOrder": 1,
                  "title": "Cell Structure Reflection",
                  "type": "Reflection",
                  "methodProfile": "Hybrid",
                  "instructions": "Explain the function of each cell structure using the lesson resource.",
                  "estimatedEffort": "45 minutes",
                  "dueTimingLabel": "After lesson 1",
                  "dueDate": null,
                  "linkedModuleObjectives": [ "Explain cell structure." ],
                  "linkedLessonSourceIds": [],
                  "linkedLessonTitles": [ "Cell structure" ],
                  "requiredOutput": "One-page written reflection.",
                  "parentNotes": "Check for structure/function connections.",
                  "isPortfolioCandidate": true,
                  "plannedPoints": 20,
                  "plannedWeight": null,
                  "status": "Planned",
                  "assignmentSummary": "Write a short reflection connecting cell structures to cell functions.",
                  "studentFacingGoal": "Explain how cell parts work together in a way a reader can understand.",
                  "estimatedMinutesMin": 35,
                  "estimatedMinutesMax": 50,
                  "requiredDeliverables": [ "One-page reflection", "At least three cell structures", "Clear structure/function explanation" ],
                  "submissionFormats": [ "WrittenAnalysis", "SpreadsheetOptional", "GraphOptional", "PortfolioEntry" ],
                  "portfolioConnection": {
                    "isPortfolioCandidate": true,
                    "portfolioSection": "Biology Portfolio",
                    "artifactTitle": "Cell Structure Reflection",
                    "artifactPurpose": "Demonstrates understanding of cell structures and functions.",
                    "reuseInstructions": "Revise after studying body systems.",
                    "crossCourseLinks": [ "Scientific Communication" ]
                  },
                  "rubric": {
                    "rubricId": "cell-reflection-rubric",
                    "scale": "4-point",
                    "criteria": [
                      {
                        "criterion": "Explanation",
                        "level4": "Clearly explains how structures support cell functions.",
                        "level3": "Explains most structure/function links.",
                        "level2": "Includes partial or vague explanations.",
                        "level1": "Does not explain structure/function links."
                      }
                    ]
                  },
                  "linkedRubricId": "cell-reflection-rubric",
                  "assessmentSkills": [ "cell vocabulary", "scientific explanation" ],
                  "studentChecklist": [ "I named at least three structures.", "I explained each structure's function." ],
                  "resources": [
                    {
                      "name": "OpenStax Cells Review",
                      "type": "Article",
                      "url": "https://openstax.org/books/biology-2e/pages/4-introduction",
                      "filePath": "",
                      "isPhysicalResource": false,
                      "required": true,
                      "studentInstructions": "Use this as a reference while writing.",
                      "sourceNote": "Open textbook chapter introduction."
                    }
                  ],
                  "assignmentSteps": [
                    {
                      "stepOrder": 1,
                      "title": "Review lesson notes",
                      "instructions": "Review the related lesson and choose three structures.",
                      "estimatedMinutes": 10
                    },
                    {
                      "stepOrder": 2,
                      "title": "Write reflection",
                      "instructions": "Explain each structure/function link.",
                      "estimatedMinutes": 30
                    }
                  ],
                  "revisionPolicy": {
                    "allowRevision": true,
                    "revisionExpectation": "Revise after parent feedback if the explanation is unclear.",
                    "minimumRevisionCount": 1
                  },
                  "completionCriteria": {
                    "minimumRequirements": [ "All three structures are included.", "Reflection can be understood without verbal explanation." ],
                    "requiresParentReview": true,
                    "masteryThreshold": 3
                  },
                  "reflectionPrompts": [ "Which structure was easiest to explain?", "What did you revise?" ],
                  "evidenceRequirements": {
                    "retainForRecords": true,
                    "evidenceType": "PortfolioArtifact",
                    "recommendedFileTypes": [ "pdf", "docx" ],
                    "requiresStudentExplanation": true,
                    "requiresParentEvaluation": true
                  },
                  "scoring": {
                    "plannedPoints": 20,
                    "plannedWeight": null,
                    "gradingMode": "Rubric",
                    "countsTowardGrade": true,
                    "allowPartialCredit": true
                  }
                },
                {
                  "sourceAssignmentId": "cells-pack-assignment-2",
                  "sequenceOrder": 2,
                  "title": "Cell Diagram",
                  "type": "PortfolioArtifact",
                  "methodProfile": "Digital",
                  "instructions": "Create and label a cell diagram, then explain three structures.",
                  "estimatedEffort": "60 minutes",
                  "dueTimingLabel": "After lesson 1",
                  "dueDate": null,
                  "linkedModuleObjectives": [ "Explain cell structure." ],
                  "linkedLessonSourceIds": [ "missing-source-id" ],
                  "linkedLessonTitles": [],
                  "requiredOutput": "Labeled diagram with short explanations.",
                  "parentNotes": "",
                  "isPortfolioCandidate": true,
                  "plannedPoints": 30,
                  "plannedWeight": null,
                  "status": "Assigned"
                }
              ]
            }
            """;

        var denied = await service.ImportAssignmentPackAsync(student, course.Value, module.Value, System.Text.Encoding.UTF8.GetBytes(assignmentPackJson));
        AssertFalse(denied.Succeeded, "Student should not import assignment packs.");

        var import = await service.ImportAssignmentPackAsync(parent, course.Value, module.Value, System.Text.Encoding.UTF8.GetBytes(assignmentPackJson));
        AssertTrue(import.Succeeded, "Assignment pack import should succeed.");
        AssertEqual(2, import.Value?.AssignmentCount ?? 0, "Assignment pack should import both assignments.");
        var assignments = await service.ListAssignmentsAsync(course.Value, module.Value);
        AssertEqual(2, assignments.Count, "Module should contain imported assignments.");
        AssertEqual("Cell Structure Reflection", assignments[0].Title, "First imported assignment should keep pack order.");
        AssertTrue(assignments[0].LinkedLessonIds.Contains(lesson.Value), "Lesson title links should reconnect to local lesson ids.");
        AssertEqual("Write a short reflection connecting cell structures to cell functions.", assignments[0].AssignmentSummary, "Assignment summary should import.");
        AssertEqual(35, assignments[0].EstimatedMinutesMin ?? 0, "Minimum estimated minutes should import.");
        AssertEqual(50, assignments[0].EstimatedMinutesMax ?? 0, "Maximum estimated minutes should import.");
        AssertTrue(assignments[0].RequiredDeliverables.Count >= 3, "Required deliverables should import.");
        AssertTrue(assignments[0].SubmissionFormats.Contains(AssignmentSubmissionFormat.WrittenAnalysis), "Written analysis submission format should import.");
        AssertTrue(assignments[0].SubmissionFormats.Contains(AssignmentSubmissionFormat.SpreadsheetOptional), "Optional spreadsheet submission format should import.");
        AssertTrue(assignments[0].SubmissionFormats.Contains(AssignmentSubmissionFormat.GraphOptional), "Optional graph submission format should import.");
        AssertTrue(assignments[0].PortfolioConnection?.CrossCourseLinks.Count > 0, "Portfolio connection should import.");
        AssertTrue(assignments[0].Rubric?.Criteria.Count > 0, "Assignment rubric should import.");
        AssertEqual("cell-reflection-rubric", assignments[0].LinkedRubricId, "Linked rubric id should import.");
        AssertTrue(assignments[0].AssessmentSkills.Contains("cell vocabulary"), "Assessment skills should import.");
        AssertTrue(assignments[0].StudentChecklist.Count > 0, "Student checklist should import.");
        AssertTrue(assignments[0].Resources.Count > 0, "Assignment resources should import.");
        AssertTrue(assignments[0].AssignmentSteps.Count > 0, "Assignment steps should import.");
        AssertTrue(assignments[0].RevisionPolicy?.AllowRevision ?? false, "Revision policy should import.");
        AssertTrue(assignments[0].CompletionCriteria?.RequiresParentReview ?? false, "Completion criteria should import.");
        AssertTrue(assignments[0].ReflectionPrompts.Count > 0, "Reflection prompts should import.");
        AssertTrue(assignments[0].EvidenceRequirements?.RetainForRecords ?? false, "Evidence requirements should import.");
        AssertTrue(assignments[0].Scoring?.GradingMode == AssignmentGradingMode.Rubric, "Scoring should import.");
        AssertEqual(0, assignments[1].LinkedLessonIds.Count, "Missing lesson links should not block assignment import.");
        AssertTrue(assignments[1].MethodProfile == InstructionalMethodProfile.Digital, "Digital method profile should import.");

        var download = await service.DownloadModuleAssignmentPackAsync(course.Value, module.Value);
        AssertTrue(download.Succeeded, "Module assignment pack download should succeed.");
        AssertTrue(download.Value?.FileName.EndsWith(".assignmentpack", StringComparison.Ordinal) ?? false, "Download should use .assignmentpack.");
        using var downloadDocument = JsonDocument.Parse(download.Value!.Content);
        AssertEqual(2, downloadDocument.RootElement.GetProperty("assignments").GetArrayLength(), "Downloaded module assignment pack should include current module assignments.");
        AssertTrue(downloadDocument.RootElement.GetProperty("assignments")[0].TryGetProperty("assignmentSteps", out _), "Downloaded assignment pack should include assignment steps.");
        AssertTrue(downloadDocument.RootElement.GetProperty("assignments")[0].TryGetProperty("linkedLessonTitles", out _), "Downloaded assignment pack should retain portable lesson links.");

        var duplicateImport = await service.ImportAssignmentPackAsync(parent, course.Value, module.Value, System.Text.Encoding.UTF8.GetBytes(assignmentPackJson));
        AssertTrue(duplicateImport.Succeeded, "Importing the same assignment pack again should append safely.");
        AssertEqual(4, (await service.ListAssignmentsAsync(course.Value, module.Value)).Count, "Repeated assignment pack import should append assignments.");
    })),
    ("Module autosave preserves existing lessons", async () =>
    {
        var repository = await CreateRepositoryAsync();
        await CreateSetupAsync(repository);
        var service = new CourseService(repository);
        var parent = UserContext.ParentAdmin("Parent");
        var course = await service.CreateCourseAsync(parent, new CreateCourseCommand("Civics", "Government overview.", ["Civics"], CourseDuration.OneSemester, 0.5m));
        AssertTrue(course.Succeeded, "Course create failed.");
        var module = await service.CreateLearningModuleAsync(
            parent,
            new CreateLearningModuleCommand(
                course.Value,
                "Constitutional foundations",
                "Study constitutional principles.",
                null,
                "2 weeks",
                "Read, discuss, and write about constitutional foundations.",
                Objectives("Explain constitutional principles."),
                [],
                "Written explanation.",
                ModuleStatus.Active));
        AssertTrue(module.Succeeded, "Module create failed.");
        var lesson = await service.CreateLessonAsync(
            parent,
            new CreateLessonCommand(
                course.Value,
                module.Value,
                "The constitutional convention",
                "Read and explain why the federal Constitution replaced the Articles of Confederation.",
                "Explain constitutional principles.",
                LessonResources("OpenStax U.S. Constitution")));
        AssertTrue(lesson.Succeeded, "Lesson create failed.");
        var assignment = await service.CreateAssignmentAsync(
            parent,
            new CreateAssignmentCommand(
                course.Value,
                module.Value,
                "Constitution reflection",
                AssignmentType.Reflection,
                InstructionalMethodProfile.Hybrid,
                "Explain constitutional principles using the lesson source.",
                "30 minutes",
                "After lesson 1",
                null,
                ["Explain constitutional principles."],
                [lesson.Value],
                "Written reflection.",
                "",
                true,
                10,
                null,
                AssignmentStatus.Planned));
        AssertTrue(assignment.Succeeded, "Assignment create failed.");

        var update = await service.UpdateLearningModuleAsync(
            parent,
            new UpdateLearningModuleCommand(
                course.Value,
                module.Value,
                "Constitutional foundations and federalism",
                "Study constitutional principles and federalism.",
                null,
                "3 weeks",
                "Read, discuss, and write about constitutional foundations and federalism.",
                Objectives("Explain constitutional principles."),
                [],
                "Written explanation and discussion notes.",
                ModuleStatus.Active));
        AssertTrue(update.Succeeded, "Module update failed.");

        var lessons = await service.ListLessonsAsync(course.Value, module.Value);
        AssertEqual(1, lessons.Count, "Module update should preserve existing lessons.");
        AssertEqual("The constitutional convention", lessons[0].Title, "Existing lesson content should be preserved.");
        var assignments = await service.ListAssignmentsAsync(course.Value, module.Value);
        AssertEqual(1, assignments.Count, "Module update should preserve existing assignments.");
        AssertEqual("Constitution reflection", assignments[0].Title, "Existing assignment content should be preserved.");
    }),
    ("Student course client reads courses syllabi and modules without inferred grades", async () =>
    {
        var repository = await CreateRepositoryAsync();
        await CreateSetupAsync(repository);
        var parent = UserContext.ParentAdmin("Parent");
        var student = UserContext.Student("Student");
        var setupService = new SetupService(repository);
        var courseService = new CourseService(repository);
        var studentService = new StudentCourseService(repository);

        var import = await courseService.ImportCoursePackAsync(parent, new ImportCoursePackCommand(DefaultCoursePacks.MichiganCollegeReadyPackId, []));
        AssertTrue(import.Succeeded, "Default pack import should succeed.");

        AssertTrue((await setupService.CreateStudentAsync(parent, new CreateStudentCommand("Younger", "Learner", 9))).Succeeded, "Second child should be added.");
        var configuredStudents = await setupService.ListStudentsAsync();
        var primaryStudent = configuredStudents.First(item => item.FirstName == "Student");
        var secondStudent = configuredStudents.First(item => item.FirstName == "Younger");

        var dashboard = await studentService.ListCoursesAsync(student, primaryStudent.Id);
        AssertTrue(dashboard.Succeeded, "Student course dashboard should load.");
        AssertEqual("Student", dashboard.Value!.StudentFirstName, "Dashboard should welcome the selected student.");
        AssertTrue(dashboard.Value.TermNames.Count == 2, "Dashboard should include configured semester headings.");
        AssertTrue(dashboard.Value.Courses.Count > 0, "Student should see imported courses.");
        var firstCourse = dashboard.Value.Courses.First();
        AssertEqual("No grade yet", firstCourse.CurrentGrade, "Student client should not infer grades before gradebook exists.");
        AssertTrue(firstCourse.TermNames.Count > 0, "Course cards should expose semester placement.");

        var secondDashboard = await studentService.ListCoursesAsync(student, secondStudent.Id);
        AssertTrue(secondDashboard.Succeeded, "Second child dashboard should load.");
        AssertEqual(0, secondDashboard.Value!.Courses.Count, "A child should not see another child's courses.");
        AssertFalse((await studentService.GetCourseAsync(student, firstCourse.CourseId, secondStudent.Id)).Succeeded, "Wrong child path should not expose the course.");

        var course = await studentService.GetCourseAsync(student, firstCourse.CourseId, primaryStudent.Id);
        AssertTrue(course.Succeeded, "Student course page should load.");
        AssertTrue(course.Value!.LearningObjectives.Count > 0, "Student course page should include course objectives.");
        AssertTrue(course.Value.Modules.Count > 0, "Student course page should include module links.");
        AssertTrue(course.Value.TermNames.Count == 2, "Student course page should include configured semester headings.");

        var syllabus = await studentService.GetSyllabusAsync(student, firstCourse.CourseId, primaryStudent.Id);
        AssertTrue(syllabus.Succeeded, "Student syllabus should load.");
        AssertFalse(string.IsNullOrWhiteSpace(syllabus.Value!.InstructionalMethods), "Syllabus should include instructional methods.");
        AssertTrue(syllabus.Value.TextsAndResources.Count > 0, "Syllabus should include course resources.");
        AssertTrue(syllabus.Value.TermNames.Count == 2, "Syllabus should include configured semester headings.");

        var module = await studentService.GetModuleAsync(student, firstCourse.CourseId, course.Value.Modules[0].ModuleId, primaryStudent.Id);
        AssertTrue(module.Succeeded, "Student module page should load.");
        AssertFalse(string.IsNullOrWhiteSpace(module.Value!.Instructions), "Module page should include instructions.");
        AssertTrue(module.Value.LearningObjectives.Count > 0, "Module page should include objectives.");
        AssertTrue(module.Value.Lessons.Count > 0, "Module page should include lessons.");
        AssertTrue(module.Value.Lessons.All(lesson => lesson.LessonId != Guid.Empty), "Student lessons should expose stable lesson ids for navigation.");
        AssertTrue(module.Value.Lessons.All(lesson => lesson.Resources.Count > 0), "Student lessons should include lesson resources.");
        AssertTrue(module.Value.Assignments.Count > 0, "Student module page should include assignments.");
        AssertTrue(module.Value.Assignments.All(assignment => !string.IsNullOrWhiteSpace(assignment.RequiredOutput)), "Student assignments should include expected work.");
        AssertTrue(module.Value.Assignments.All(assignment => assignment.RelatedLessonIds.Count > 0), "Student assignments should expose lesson links for sequential lesson flow.");
        AssertTrue(module.Value.Assignments.All(assignment => assignment.RelatedLessonTitles.Count > 0), "Student assignments should connect to lesson materials.");

        var parentPreview = await studentService.GetCourseAsync(parent, firstCourse.CourseId, primaryStudent.Id);
        AssertTrue(parentPreview.Succeeded, "Parent should be able to preview student course read model.");
    }),
    ("Student submissions create local file metadata and parent acceptance creates evidence", (Func<Task>)(async () =>
    {
        var (repository, paths) = await CreateRepositoryWithPathsAsync();
        await CreateSetupAsync(repository);
        var courseService = new CourseService(repository);
        var submissionService = new AssignmentSubmissionService(repository, new LocalSubmissionFileStore(paths));
        var studentCourseService = new StudentCourseService(repository);
        var parent = UserContext.ParentAdmin("Parent");
        var studentUser = UserContext.Student("Student");
        var student = await repository.GetStudentAsync();
        if (student is null)
        {
            throw new InvalidOperationException("Student setup failed.");
        }

        var course = await courseService.CreateCourseAsync(parent, new CreateCourseCommand("Biology", "Life science overview.", ["Science"], CourseDuration.TwoSemesters, 1, student.Id));
        AssertTrue(course.Succeeded, "Course create failed.");
        var module = await courseService.CreateLearningModuleAsync(
            parent,
            new CreateLearningModuleCommand(
                course.Value,
                "Cells",
                "Cell biology module.",
                null,
                "2 weeks",
                "Study cells.",
                Objectives("Explain cell structure."),
                Resources("OpenStax Cells"),
                "Cell diagram evidence.",
                ModuleStatus.Active));
        AssertTrue(module.Succeeded, "Module create failed.");
        var lesson = await courseService.CreateLessonAsync(parent, new CreateLessonCommand(course.Value, module.Value, "Cell structure", "Study the major parts of cells.", "Explain cell structure.", LessonResources("OpenStax Cells")));
        AssertTrue(lesson.Succeeded, "Lesson create failed.");
        var assignment = await courseService.CreateAssignmentAsync(
            parent,
            new CreateAssignmentCommand(
                course.Value,
                module.Value,
                "Cell structure reflection",
                AssignmentType.Reflection,
                InstructionalMethodProfile.Hybrid,
                "Explain the function of cell structures.",
                "45 minutes",
                "After lesson 1",
                null,
                ["Explain cell structure."],
                [lesson.Value],
                "Written reflection.",
                "",
                true,
                20,
                null,
                AssignmentStatus.Assigned));
        AssertTrue(assignment.Succeeded, "Assignment create failed.");

        var deniedSubmit = await submissionService.SubmitAssignmentAsync(
            parent,
            new SubmitAssignmentCommand(course.Value, module.Value, assignment.Value, "Parent cannot submit.", "", Array.Empty<AssignmentAttachmentUpload>(), student.Id));
        AssertFalse(deniedSubmit.Succeeded, "Parent/admin preview should not submit student work.");

        var firstSubmit = await submissionService.SubmitAssignmentAsync(
            studentUser,
            new SubmitAssignmentCommand(
                course.Value,
                module.Value,
                assignment.Value,
                "Cells have structures with distinct jobs.",
                "Please review my diagram.",
                [new AssignmentAttachmentUpload("cell-notes.txt", "text/plain", System.Text.Encoding.UTF8.GetBytes("cell evidence"))],
                student.Id));
        AssertTrue(firstSubmit.Succeeded, "Student submit failed.");

        var storedSubmission = await repository.GetAssignmentSubmissionAsync(firstSubmit.Value);
        AssertTrue(storedSubmission is not null, "Submission should persist.");
        AssertEqual(1, storedSubmission!.Attachments.Count, "Submission should include attachment metadata.");
        var storedFile = storedSubmission.Attachments[0];
        AssertTrue(storedFile.Category == StoredFileCategory.AssignmentSubmission, "Attachment category should be assignment submission.");
        AssertFalse(string.IsNullOrWhiteSpace(storedFile.ChecksumSha256), "Stored file should include checksum.");
        AssertTrue(File.Exists(Path.Combine(paths.DataRoot, storedFile.StoredPath)), "Stored attachment should exist on disk.");

        var studentModule = await studentCourseService.GetModuleAsync(studentUser, course.Value, module.Value, student.Id);
        AssertTrue(studentModule.Succeeded, "Student module read failed.");
        AssertEqual(1, studentModule.Value!.Assignments.Single(item => item.AssignmentId == assignment.Value).Submissions.Count, "True student portal read model should show submission history.");

        var previewModule = await studentCourseService.GetModuleAsync(parent, course.Value, module.Value, student.Id);
        AssertTrue(previewModule.Succeeded, "Parent preview module read failed.");
        AssertEqual(0, previewModule.Value!.Assignments.Single(item => item.AssignmentId == assignment.Value).Submissions.Count, "Parent/admin preview should not show student submissions.");

        var pending = await submissionService.ListPendingReviewsAsync(parent);
        AssertTrue(pending.Succeeded, "Pending reviews should load.");
        AssertEqual(1, pending.Value!.Count, "Submitted work should appear for parent review.");

        var returned = await submissionService.ReturnSubmissionAsync(parent, new ReturnSubmissionCommand(firstSubmit.Value, "Add one concrete example."));
        AssertTrue(returned.Succeeded, "Parent should return work with notes.");

        var secondSubmit = await submissionService.SubmitAssignmentAsync(
            studentUser,
            new SubmitAssignmentCommand(
                course.Value,
                module.Value,
                assignment.Value,
                "Cells have structures with distinct jobs, such as nuclei storing genetic information.",
                "",
                Array.Empty<AssignmentAttachmentUpload>(),
                student.Id));
        AssertTrue(secondSubmit.Succeeded, "Student resubmit failed.");

        var accept = await submissionService.AcceptSubmissionAsync(parent, new AcceptSubmissionCommand(secondSubmit.Value, "Reviewed and accepted.", true));
        AssertTrue(accept.Succeeded, "Parent should accept submitted work as evidence.");
        var evidence = await submissionService.ListEvidenceAsync(parent);
        AssertTrue(evidence.Succeeded, "Evidence list should load.");
        AssertEqual(1, evidence.Value!.Count, "Accepted submission should create one evidence record.");
        AssertTrue(evidence.Value[0].PortfolioCandidate, "Portfolio candidate should remain a marker on evidence.");

        var acceptedSubmission = await repository.GetAssignmentSubmissionAsync(secondSubmit.Value);
        AssertTrue(acceptedSubmission!.Status == AssignmentSubmissionStatus.Accepted, "Accepted submission should have accepted workflow status.");

        var thirdSubmit = await submissionService.SubmitAssignmentAsync(
            studentUser,
            new SubmitAssignmentCommand(
                course.Value,
                module.Value,
                assignment.Value,
                "Trying another accepted attempt.",
                "",
                Array.Empty<AssignmentAttachmentUpload>(),
                student.Id));
        AssertFalse(thirdSubmit.Succeeded, "Single-attempt assignments should block another attempt after accepted evidence.");
    })),
    ("Multi-draft assignment accepts separate lesson draft submissions", (Func<Task>)(async () =>
    {
        var (repository, paths) = await CreateRepositoryWithPathsAsync();
        await CreateSetupAsync(repository);
        var courseService = new CourseService(repository);
        var submissionService = new AssignmentSubmissionService(repository, new LocalSubmissionFileStore(paths));
        var gradebookService = new GradebookService(repository);
        var parent = UserContext.ParentAdmin("Parent");
        var studentUser = UserContext.Student("Student");
        var student = await repository.GetStudentAsync() ?? throw new InvalidOperationException("Student setup failed.");

        var course = await courseService.CreateCourseAsync(parent, new CreateCourseCommand("Agro Business", "Business planning.", ["Business"], CourseDuration.OneSemester, 0.5m, student.Id));
        AssertTrue(course.Succeeded, "Course create failed.");
        var module = await courseService.CreateLearningModuleAsync(parent, new CreateLearningModuleCommand(
            course.Value,
            "Business Plan",
            "Build a business plan in stages.",
            null,
            "3 lessons",
            "Complete one draft per lesson.",
            Objectives("Develop a complete business plan."),
            Resources("Business plan guide"),
            "Retain final plan and drafts.",
            ModuleStatus.Active));
        AssertTrue(module.Succeeded, "Module create failed.");
        var lesson1 = await courseService.CreateLessonAsync(parent, new CreateLessonCommand(course.Value, module.Value, "Market analysis", "Draft the market analysis.", "Develop a complete business plan.", LessonResources("Market guide")));
        var lesson2 = await courseService.CreateLessonAsync(parent, new CreateLessonCommand(course.Value, module.Value, "Budget plan", "Draft the budget plan.", "Develop a complete business plan.", LessonResources("Budget guide")));
        var lesson3 = await courseService.CreateLessonAsync(parent, new CreateLessonCommand(course.Value, module.Value, "Final plan", "Submit the final plan.", "Develop a complete business plan.", LessonResources("Final plan checklist")));
        AssertTrue(lesson1.Succeeded && lesson2.Succeeded && lesson3.Succeeded, "Lessons should be created.");

        var assignment = await courseService.CreateAssignmentAsync(parent, new CreateAssignmentCommand(
            course.Value,
            module.Value,
            "Agro-business plan",
            AssignmentType.Project,
            InstructionalMethodProfile.ProjectBasedApplied,
            "Submit the business plan draft for the current lesson.",
            "One lesson",
            "After each lesson",
            null,
            ["Develop a complete business plan."],
            [lesson1.Value, lesson2.Value, lesson3.Value],
            "Business plan draft or final plan.",
            "",
            true,
            100,
            null,
            AssignmentStatus.Assigned,
            AssignmentSummary: "Build one larger assignment across three lesson drafts.",
            StudentFacingGoal: "Improve the plan across three drafts.",
            AttemptPolicy: AssignmentAttemptPolicy.SingleAttempt,
            SubmissionStructure: AssignmentSubmissionStructure.MultiDraft,
            DraftCount: 3));
        AssertTrue(assignment.Succeeded, "Multi-draft assignment create failed.");

        var draft1 = await submissionService.SubmitAssignmentAsync(studentUser, new SubmitAssignmentCommand(
            course.Value,
            module.Value,
            assignment.Value,
            "Draft 1 market analysis.",
            "",
            [],
            student.Id,
            1));
        AssertTrue(draft1.Succeeded, "Draft 1 should submit.");

        var duplicatePendingDraft1 = await submissionService.SubmitAssignmentAsync(studentUser, new SubmitAssignmentCommand(
            course.Value,
            module.Value,
            assignment.Value,
            "Duplicate draft 1.",
            "",
            [],
            student.Id,
            1));
        AssertFalse(duplicatePendingDraft1.Succeeded, "Same draft should not allow duplicate pending work.");

        var acceptedDraft1 = await submissionService.AcceptSubmissionAsync(parent, new AcceptSubmissionCommand(draft1.Value, "Draft 1 accepted.", true));
        AssertTrue(acceptedDraft1.Succeeded, "Parent should accept draft 1.");

        var secondDraft1 = await submissionService.SubmitAssignmentAsync(studentUser, new SubmitAssignmentCommand(
            course.Value,
            module.Value,
            assignment.Value,
            "Second draft 1.",
            "",
            [],
            student.Id,
            1));
        AssertFalse(secondDraft1.Succeeded, "Single-attempt draft 1 should not reopen after accepted evidence.");

        var draft2 = await submissionService.SubmitAssignmentAsync(studentUser, new SubmitAssignmentCommand(
            course.Value,
            module.Value,
            assignment.Value,
            "Draft 2 budget plan.",
            "",
            [],
            student.Id,
            2));
        AssertTrue(draft2.Succeeded, "Accepted draft 1 should not block draft 2.");

        var finalDraft = await submissionService.SubmitAssignmentAsync(studentUser, new SubmitAssignmentCommand(
            course.Value,
            module.Value,
            assignment.Value,
            "Final business plan.",
            "",
            [],
            student.Id,
            3));
        AssertTrue(finalDraft.Succeeded, "Earlier drafts should not block final draft submission.");

        var storedDrafts = await repository.GetAssignmentSubmissionsAsync();
        AssertEqual(3, storedDrafts.Count(submission => submission.AssignmentId == assignment.Value), "Three draft submissions should be retained.");
        AssertTrue(storedDrafts.Any(submission => submission.DraftNumber == 3), "Final draft should be stored with draft number 3.");

        var gradebook = await gradebookService.GetGradebookAsync(parent, student.Id, course.Value);
        AssertTrue(gradebook.Succeeded, "Gradebook should load multi-draft submissions.");
        var row = gradebook.Value!.Rows.Single();
        AssertEqual(3, row.ActiveSubmissions.Count, "Gradebook should show every active submitted draft for a multi-draft assignment.");
        AssertTrue(row.ActiveSubmissions.Any(submission => submission.DraftNumber == 1 && submission.Status == AssignmentSubmissionStatus.Accepted), "Accepted draft 1 should remain visible in the submission area.");
        AssertTrue(row.ActiveSubmissions.Any(submission => submission.DraftNumber == 2 && submission.Status == AssignmentSubmissionStatus.Submitted), "Submitted draft 2 should remain visible after later drafts.");
        AssertTrue(row.ActiveSubmissions.Any(submission => submission.DraftNumber == 3 && submission.IsFinalDraft), "Final draft should be labeled as the final draft.");

        var clearDraft2 = await submissionService.ClearSubmissionAsync(parent, new ClearSubmissionCommand(draft2.Value, "Clearing draft 2 for a corrected upload.", "Clear"));
        AssertTrue(clearDraft2.Succeeded, "Parent should clear a specific multi-draft submission.");

        var afterClear = await gradebookService.GetGradebookAsync(parent, student.Id, course.Value);
        AssertTrue(afterClear.Succeeded, "Gradebook should reload after clearing one draft.");
        var rowAfterClear = afterClear.Value!.Rows.Single();
        AssertEqual(2, rowAfterClear.ActiveSubmissions.Count, "Clearing draft 2 should remove only that draft from active gradebook submissions.");
        AssertFalse(rowAfterClear.ActiveSubmissions.Any(submission => submission.SubmissionId == draft2.Value), "Cleared draft 2 should not remain active in the gradebook submission area.");
        AssertTrue(rowAfterClear.ActiveSubmissions.Any(submission => submission.DraftNumber == 1), "Draft 1 should remain active after clearing draft 2.");
        AssertTrue(rowAfterClear.ActiveSubmissions.Any(submission => submission.DraftNumber == 3), "Final draft should remain active after clearing draft 2.");

        var correctedDraft2 = await submissionService.SubmitAssignmentAsync(studentUser, new SubmitAssignmentCommand(
            course.Value,
            module.Value,
            assignment.Value,
            "Corrected draft 2 budget plan.",
            "",
            [],
            student.Id,
            2));
        AssertTrue(correctedDraft2.Succeeded, "Clearing draft 2 should reopen draft 2 for student submission.");
    })),
    ("Student portfolio suggestion requires parent acceptance before portfolio item approval", (Func<Task>)(async () =>
    {
        var (repository, paths) = await CreateRepositoryWithPathsAsync();
        var setup = await CreateCourseWithAssignmentAsync(repository);
        var submissionService = new AssignmentSubmissionService(repository, new LocalSubmissionFileStore(paths));
        var portfolioService = new PortfolioService(repository);
        var parent = UserContext.ParentAdmin("Parent");
        var studentUser = UserContext.Student("Student");

        var suggested = await submissionService.SubmitAssignmentAsync(
            studentUser,
            new SubmitAssignmentCommand(
                setup.Course.Id,
                setup.Module.Id,
                setup.Assignment.Id,
                "This may fit the portfolio.",
                "I think this is a good portfolio piece.",
                [],
                setup.Student.Id,
                MarkPortfolioCandidate: true));
        AssertTrue(suggested.Succeeded, "Student should submit work with a portfolio suggestion.");

        var storedSuggested = await repository.GetAssignmentSubmissionAsync(suggested.Value);
        AssertTrue(storedSuggested!.StudentPortfolioCandidate, "Submission should retain the student's portfolio suggestion.");
        AssertFalse(storedSuggested.PortfolioCandidate, "Student suggestion alone should not become a parent portfolio candidate marker.");

        var detail = await submissionService.GetReviewDetailAsync(parent, suggested.Value);
        AssertTrue(detail.Succeeded, "Parent review detail should load the suggested submission.");
        AssertTrue(detail.Value!.StudentPortfolioCandidate, "Parent review should show the student suggestion.");
        AssertFalse(detail.Value.PortfolioCandidate, "Parent review should keep parent candidate state separate before acceptance.");

        var acceptedWithoutPortfolio = await submissionService.AcceptSubmissionAsync(
            parent,
            new AcceptSubmissionCommand(suggested.Value, "Accepted as evidence, not portfolio.", false));
        AssertTrue(acceptedWithoutPortfolio.Succeeded, "Parent should be able to accept evidence without accepting the portfolio suggestion.");
        var evidenceAfterDecline = await repository.GetEvidenceRecordsAsync();
        AssertFalse(evidenceAfterDecline.Single().PortfolioCandidate, "Declined portfolio suggestion should not create an assignment candidate.");
        var workspaceAfterDecline = await portfolioService.GetReviewWorkspaceAsync(parent);
        AssertTrue(workspaceAfterDecline.Succeeded, "Portfolio review workspace should load after declined suggestion.");
        AssertEqual(0, workspaceAfterDecline.Value!.AssignmentCandidates.Count, "Declined suggestion should not appear as a portfolio assignment candidate.");

        var secondAssignment = setup.Assignment with
        {
            Id = Guid.NewGuid(),
            SourceAssignmentId = "second-portfolio-candidate",
            SequenceOrder = 2,
            Title = "Second evidence analysis"
        };
        await repository.SaveCourseAsync(setup.Course with
        {
            Modules =
            [
                setup.Module with
                {
                    Assignments = [setup.Assignment, secondAssignment]
                }
            ]
        });

        var secondSuggested = await submissionService.SubmitAssignmentAsync(
            studentUser,
            new SubmitAssignmentCommand(
                setup.Course.Id,
                setup.Module.Id,
                secondAssignment.Id,
                "This one should go in the portfolio.",
                "Please consider this for the portfolio.",
                [],
                setup.Student.Id,
                MarkPortfolioCandidate: true));
        AssertTrue(secondSuggested.Succeeded, "Student should submit a second portfolio suggestion.");
        var acceptedCandidate = await submissionService.AcceptSubmissionAsync(
            parent,
            new AcceptSubmissionCommand(secondSuggested.Value, "Accepted as portfolio evidence.", true));
        AssertTrue(acceptedCandidate.Succeeded, "Parent should accept the suggested work as portfolio evidence.");

        var workspaceBeforeApproval = await portfolioService.GetReviewWorkspaceAsync(parent);
        AssertTrue(workspaceBeforeApproval.Succeeded, "Portfolio review workspace should load candidate evidence.");
        var candidate = workspaceBeforeApproval.Value!.AssignmentCandidates.Single();
        AssertTrue(candidate.StudentSuggested, "Assignment candidate should show that the student suggested it.");
        AssertEqual(secondSuggested.Value, candidate.SubmissionId, "Assignment candidate should point back to the accepted submission.");
        AssertFalse(candidate.PortfolioDraftStatus.HasValue, "Candidate should not be a portfolio item before parent portfolio approval.");

        var evidenceBeforeApproval = await repository.GetEvidenceRecordsAsync();
        var assessmentsBeforeApproval = await repository.GetAssessmentRecordsAsync();
        var approvedCandidate = await portfolioService.AcceptAssignmentCandidateAsync(
            parent,
            new AcceptPortfolioAssignmentCandidateCommand(candidate.EvidenceRecordId, "Ready for the parent portfolio packet."));
        AssertTrue(approvedCandidate.Succeeded, "Parent should approve an accepted assignment candidate as a portfolio item.");

        var workspaceAfterApproval = await portfolioService.GetReviewWorkspaceAsync(parent);
        AssertEqual(1, workspaceAfterApproval.Value!.Items.Count, "Parent-approved assignment candidate should create one portfolio item.");
        AssertTrue(workspaceAfterApproval.Value.Items[0].Status == PortfolioDraftStatus.ParentApproved, "Created portfolio item should be parent approved.");
        AssertTrue(workspaceAfterApproval.Value.AssignmentCandidates.Single().PortfolioDraftStatus == PortfolioDraftStatus.ParentApproved, "Candidate should show its approved portfolio status.");
        AssertEqual(evidenceBeforeApproval.Count, (await repository.GetEvidenceRecordsAsync()).Count, "Portfolio approval should not duplicate evidence records.");
        AssertEqual(assessmentsBeforeApproval.Count, (await repository.GetAssessmentRecordsAsync()).Count, "Portfolio approval should not create assessment records.");
        AssertEqual(setup.Course.PlannedCreditValue, (await repository.GetCourseAsync(setup.Course.Id))!.PlannedCreditValue, "Portfolio approval should not alter course credits.");
    })),
    ("Student configures portfolio draft from accepted evidence and parent reviews it", (Func<Task>)(async () =>
    {
        var (repository, paths) = await CreateRepositoryWithPathsAsync();
        var setup = await CreateCourseWithAssignmentAsync(repository);
        var submissionService = new AssignmentSubmissionService(repository, new LocalSubmissionFileStore(paths));
        var portfolioService = new PortfolioService(repository);
        var parent = UserContext.ParentAdmin("Parent");
        var studentUser = UserContext.Student("Student");

        var submitted = await submissionService.SubmitAssignmentAsync(
            studentUser,
            new SubmitAssignmentCommand(
                setup.Course.Id,
                setup.Module.Id,
                setup.Assignment.Id,
                "This analysis explains why the evidence matters.",
                "",
                [],
                setup.Student.Id));
        AssertTrue(submitted.Succeeded, "Student submission should succeed before evidence can be curated.");

        var accepted = await submissionService.AcceptSubmissionAsync(parent, new AcceptSubmissionCommand(submitted.Value, "Accepted as portfolio evidence.", true));
        AssertTrue(accepted.Succeeded, "Parent acceptance should create evidence.");
        var evidenceBefore = await repository.GetEvidenceRecordsAsync();
        var assessmentsBefore = await repository.GetAssessmentRecordsAsync();

        var designWorkspace = await portfolioService.GetStudentWorkspaceAsync(studentUser, setup.Student.Id);
        AssertTrue(designWorkspace.Succeeded, "Student portfolio design should load with starter sections.");
        var sectionId = designWorkspace.Value!.Design.Sections.First().SectionId;

        var added = await portfolioService.AddDraftItemAsync(studentUser, new AddPortfolioDraftItemCommand(accepted.Value, setup.Student.Id, sectionId));
        AssertTrue(added.Succeeded, "Student should add accepted evidence to a portfolio draft.");

        var duplicate = await portfolioService.AddDraftItemAsync(studentUser, new AddPortfolioDraftItemCommand(accepted.Value, setup.Student.Id, sectionId));
        AssertTrue(duplicate.Succeeded, "Re-adding accepted evidence should be idempotent.");
        AssertEqual(added.Value, duplicate.Value, "Duplicate add should return the existing draft item id.");

        var updated = await portfolioService.UpdateDraftItemAsync(studentUser, new UpdatePortfolioDraftItemCommand(
            added.Value,
            "Evidence analysis portfolio artifact",
            sectionId,
            "This shows I can explain evidence carefully.",
            "It is one of my stronger written analyses.",
            ["Evidence", "Analysis"],
            2,
            true));
        AssertTrue(updated.Succeeded, "Student should configure draft portfolio metadata.");

        var submitForReview = await portfolioService.SubmitDraftItemAsync(studentUser, new SubmitPortfolioDraftItemCommand(added.Value));
        AssertTrue(submitForReview.Succeeded, "Student should submit configured portfolio item for parent review.");

        var studentReview = await portfolioService.ReviewDraftItemAsync(studentUser, new ReviewPortfolioDraftItemCommand(
            added.Value,
            PortfolioDraftStatus.ParentApproved,
            "Trying to approve."));
        AssertFalse(studentReview.Succeeded, "Student should not review or approve portfolio draft entries.");

        var revision = await portfolioService.ReviewDraftItemAsync(parent, new ReviewPortfolioDraftItemCommand(
            added.Value,
            PortfolioDraftStatus.NeedsRevision,
            "Add one more concrete skill."));
        AssertTrue(revision.Succeeded, "Parent should be able to request portfolio revisions.");

        var revised = await portfolioService.UpdateDraftItemAsync(studentUser, new UpdatePortfolioDraftItemCommand(
            added.Value,
            "Evidence analysis portfolio artifact",
            sectionId,
            "This shows I can explain evidence carefully.",
            "It shows evidence interpretation and revision.",
            ["Evidence", "Analysis", "Revision"],
            2,
            true));
        AssertTrue(revised.Succeeded, "Student should revise an item returned by the parent.");

        var resubmitted = await portfolioService.SubmitDraftItemAsync(studentUser, new SubmitPortfolioDraftItemCommand(added.Value));
        AssertTrue(resubmitted.Succeeded, "Student should resubmit revised portfolio item.");

        var approved = await portfolioService.ReviewDraftItemAsync(parent, new ReviewPortfolioDraftItemCommand(
            added.Value,
            PortfolioDraftStatus.ParentApproved,
            "Ready for parent portfolio review packet."));
        AssertTrue(approved.Succeeded, "Parent should approve a student-curated portfolio item.");

        var workspace = await portfolioService.GetStudentWorkspaceAsync(studentUser, setup.Student.Id);
        AssertTrue(workspace.Succeeded, "Student portfolio workspace should load.");
        AssertEqual(1, workspace.Value!.DraftItems.Count, "Student workspace should include the curated draft item.");
        AssertTrue(workspace.Value.DraftItems[0].Status == PortfolioDraftStatus.ParentApproved, "Approved status should be visible to the student.");
        AssertTrue(workspace.Value.AvailableEvidence[0].AlreadyAdded, "Accepted evidence should show that it has already been added.");

        var reviewWorkspace = await portfolioService.GetReviewWorkspaceAsync(parent);
        AssertTrue(reviewWorkspace.Succeeded, "Parent portfolio review workspace should load.");
        AssertEqual(1, reviewWorkspace.Value!.Items.Count, "Parent review workspace should include the draft item.");

        var evidenceAfter = await repository.GetEvidenceRecordsAsync();
        var assessmentsAfter = await repository.GetAssessmentRecordsAsync();
        var courseAfter = await repository.GetCourseAsync(setup.Course.Id);
        AssertEqual(evidenceBefore.Count, evidenceAfter.Count, "Portfolio drafting should not duplicate evidence records.");
        AssertEqual(assessmentsBefore.Count, assessmentsAfter.Count, "Portfolio drafting should not create assessment records.");
        AssertEqual(setup.Course.PlannedCreditValue, courseAfter!.PlannedCreditValue, "Portfolio drafting should not alter course credits.");
    })),
    ("Portfolio design supports sections suggestions approval snapshots and assignment suggestions", (Func<Task>)(async () =>
    {
        var (repository, paths) = await CreateRepositoryWithPathsAsync();
        var setup = await CreateCourseWithAssignmentAsync(repository);
        var submissionService = new AssignmentSubmissionService(repository, new LocalSubmissionFileStore(paths));
        var portfolioService = new PortfolioService(repository);
        var parent = UserContext.ParentAdmin("Parent");
        var studentUser = UserContext.Student("Student");

        var plannedAssignment = setup.Assignment with
        {
            Id = Guid.NewGuid(),
            SourceAssignmentId = "planned-portfolio-presentation",
            SequenceOrder = 2,
            Title = "Planned portfolio presentation"
        };
        await repository.SaveCourseAsync(setup.Course with
        {
            Modules =
            [
                setup.Module with
                {
                    Assignments = [setup.Assignment, plannedAssignment]
                }
            ]
        });

        var submitted = await submissionService.SubmitAssignmentAsync(
            studentUser,
            new SubmitAssignmentCommand(
                setup.Course.Id,
                setup.Module.Id,
                setup.Assignment.Id,
                "Accepted work for the portfolio.",
                "",
                [],
                setup.Student.Id));
        AssertTrue(submitted.Succeeded, "Student submission should succeed.");

        var accepted = await submissionService.AcceptSubmissionAsync(parent, new AcceptSubmissionCommand(submitted.Value, "Accepted evidence.", true));
        AssertTrue(accepted.Succeeded, "Parent should accept evidence.");
        var evidenceBefore = await repository.GetEvidenceRecordsAsync();
        var assessmentsBefore = await repository.GetAssessmentRecordsAsync();

        var workspace = await portfolioService.GetStudentWorkspaceAsync(studentUser, setup.Student.Id);
        AssertTrue(workspace.Succeeded, "Student portfolio workspace should load.");
        AssertTrue(workspace.Value!.CanStudentAuthor, "High-school student should author the working portfolio design.");
        var narrative = workspace.Value.Design.Narratives.Single();
        var defaultSection = workspace.Value.Design.Sections.First();

        var updateDesign = await portfolioService.UpdateDesignAsync(studentUser, new UpdatePortfolioDesignCommand(
            setup.Student.Id,
            "Senior Learning Portfolio",
            "College and family archive review.",
            [new PortfolioNarrativeInput(narrative.NarrativeId, narrative.Prompt, "growth in statistical writing and applied analysis.", narrative.SortOrder)]));
        AssertTrue(updateDesign.Succeeded, "Student should edit portfolio title and narrative.");

        var addSection = await portfolioService.AddSectionAsync(studentUser, new AddPortfolioSectionCommand(
            setup.Student.Id,
            "Research Writing",
            "Technical writing and evidence-based analysis."));
        AssertTrue(addSection.Succeeded, "Student should add a custom portfolio section.");

        var addEvidence = await portfolioService.AddDraftItemAsync(studentUser, new AddPortfolioDraftItemCommand(accepted.Value, setup.Student.Id, addSection.Value));
        AssertTrue(addEvidence.Succeeded, "Student should place accepted evidence in the custom section.");

        var updateItem = await portfolioService.UpdateDraftItemAsync(studentUser, new UpdatePortfolioDraftItemCommand(
            addEvidence.Value,
            "Statistical inference analysis brief",
            addSection.Value,
            "This shows I can explain data limits and make a careful conclusion.",
            "It represents my strongest applied statistics writing.",
            ["Statistics", "Technical writing", "Revision"],
            1,
            true));
        AssertTrue(updateItem.Succeeded, "Student should edit item reflection and placement.");

        var suggestion = await portfolioService.SuggestAssignmentAsync(studentUser, new SuggestPortfolioAssignmentCommand(
            setup.Student.Id,
            setup.Course.Id,
            setup.Module.Id,
            plannedAssignment.Id,
            "This presentation could become a good capstone artifact."));
        AssertTrue(suggestion.Succeeded, "Student should suggest a planned assignment.");
        AssertEqual(evidenceBefore.Count, (await repository.GetEvidenceRecordsAsync()).Count, "Planned assignment suggestions should not create accepted evidence.");

        var submitDesign = await portfolioService.SubmitDesignAsync(studentUser, new SubmitPortfolioDesignCommand(setup.Student.Id));
        AssertTrue(submitDesign.Succeeded, "Student should submit the portfolio design for review.");

        var reviewWorkspace = await portfolioService.GetReviewWorkspaceAsync(parent, setup.Student.Id);
        AssertTrue(reviewWorkspace.Succeeded, "Parent review workspace should load.");
        AssertEqual(1, reviewWorkspace.Value!.AssignmentSuggestions.Count, "Parent should see planned assignment suggestions separately from evidence.");
        AssertTrue(reviewWorkspace.Value.Preview.Sections.Any(section => section.Heading == "Research Writing"), "Preview should include the custom section.");

        var reviewedNarrative = reviewWorkspace.Value.Design.Narratives.Single();
        var reviewedSection = reviewWorkspace.Value.Design.Sections.First(section => section.Heading == "Research Writing");
        var reviewedItem = reviewWorkspace.Value.Items.Single();
        AssertTrue((await portfolioService.AddSuggestionAsync(parent, new AddPortfolioSuggestionCommand(setup.Student.Id, PortfolioSuggestionTargetType.Narrative, reviewedNarrative.NarrativeId, "Add one specific accomplishment."))).Succeeded, "Parent should add a narrative suggestion.");
        AssertTrue((await portfolioService.AddSuggestionAsync(parent, new AddPortfolioSuggestionCommand(setup.Student.Id, PortfolioSuggestionTargetType.Section, reviewedSection.SectionId, "Clarify why this section matters."))).Succeeded, "Parent should add a section suggestion.");
        AssertTrue((await portfolioService.AddSuggestionAsync(parent, new AddPortfolioSuggestionCommand(setup.Student.Id, PortfolioSuggestionTargetType.Item, reviewedItem.PortfolioDraftItemId, "Name the audience for this brief."))).Succeeded, "Parent should add an item suggestion.");
        AssertTrue((await portfolioService.RequestRevisionAsync(parent, new RequestPortfolioRevisionCommand(setup.Student.Id, "Please revise the introduction and item explanation."))).Succeeded, "Parent should request revision.");

        var studentRevision = await portfolioService.GetStudentWorkspaceAsync(studentUser, setup.Student.Id);
        AssertEqual(PortfolioDesignStatus.NeedsRevision, studentRevision.Value!.Design.Status, "Revision request should be visible to the student.");
        AssertTrue(studentRevision.Value.Design.Suggestions.Count(item => item.Status == PortfolioSuggestionStatus.Open) >= 4, "Student should see targeted and overall suggestions.");

        foreach (var openSuggestion in studentRevision.Value.Design.Suggestions.Where(item => item.Status == PortfolioSuggestionStatus.Open))
        {
            AssertTrue((await portfolioService.ResolveSuggestionAsync(parent, new ResolvePortfolioSuggestionCommand(setup.Student.Id, openSuggestion.SuggestionId))).Succeeded, "Parent should resolve suggestions before approval.");
        }

        var resubmit = await portfolioService.SubmitDesignAsync(studentUser, new SubmitPortfolioDesignCommand(setup.Student.Id));
        AssertTrue(resubmit.Succeeded, "Student should resubmit after revision.");

        var approved = await portfolioService.ApproveDesignAsync(parent, new ApprovePortfolioDesignCommand(setup.Student.Id));
        AssertTrue(approved.Succeeded, "Parent should approve the complete portfolio design.");

        var approvedWorkspace = await portfolioService.GetReviewWorkspaceAsync(parent, setup.Student.Id);
        AssertEqual(PortfolioDesignStatus.Approved, approvedWorkspace.Value!.Design.Status, "Approved design status should be visible.");
        AssertEqual(1, approvedWorkspace.Value.Design.ApprovalSnapshots.Count, "Approval should create one snapshot.");
        AssertEqual(1, approvedWorkspace.Value.Design.ApprovalSnapshots.Single().ItemCount, "Approval snapshot should include the selected item.");
        AssertEqual(evidenceBefore.Count, (await repository.GetEvidenceRecordsAsync()).Count, "Portfolio approval should not duplicate evidence.");
        AssertEqual(assessmentsBefore.Count, (await repository.GetAssessmentRecordsAsync()).Count, "Portfolio approval should not create assessments.");
        AssertEqual(setup.Course.PlannedCreditValue, (await repository.GetCourseAsync(setup.Course.Id))!.PlannedCreditValue, "Portfolio approval should not alter credits.");

        var snapshotTitle = approvedWorkspace.Value.Design.ApprovalSnapshots.Single().Title;
        var editAfterApproval = await portfolioService.UpdateDesignAsync(studentUser, new UpdatePortfolioDesignCommand(
            setup.Student.Id,
            "Senior Learning Portfolio Revised",
            approvedWorkspace.Value.Design.Purpose,
            approvedWorkspace.Value.Design.Narratives
                .Select(item => new PortfolioNarrativeInput(item.NarrativeId, item.Prompt, item.Response, item.SortOrder))
                .ToArray()));
        AssertTrue(editAfterApproval.Succeeded, "Later student edits should create a new working version.");

        var afterEdit = await portfolioService.GetReviewWorkspaceAsync(parent, setup.Student.Id);
        AssertEqual(PortfolioDesignStatus.Working, afterEdit.Value!.Design.Status, "Later edits should return the current design to working status.");
        AssertEqual(2, afterEdit.Value.Design.Version, "Later edits after approval should increment the working version.");
        AssertEqual(snapshotTitle, afterEdit.Value.Design.ApprovalSnapshots.Single().Title, "Approval snapshot should remain immutable after later edits.");
    })),
    ("Approved portfolio exports archive packet from immutable snapshot", (Func<Task>)(async () =>
    {
        var (repository, paths) = await CreateRepositoryWithPathsAsync();
        var setup = await CreateCourseWithAssignmentAsync(repository);
        var fileStore = new LocalSubmissionFileStore(paths);
        var submissionService = new AssignmentSubmissionService(repository, fileStore);
        var portfolioService = new PortfolioService(repository);
        var exportService = new PortfolioExportService(repository, fileStore);
        var parent = UserContext.ParentAdmin("Parent");
        var studentUser = UserContext.Student("Student");

        var submitted = await submissionService.SubmitAssignmentAsync(
            studentUser,
            new SubmitAssignmentCommand(
                setup.Course.Id,
                setup.Module.Id,
                setup.Assignment.Id,
                "Portfolio-ready work with an attachment.",
                "",
                [new AssignmentAttachmentUpload("analysis-notes.txt", "text/plain", System.Text.Encoding.UTF8.GetBytes("accepted portfolio evidence"))],
                setup.Student.Id));
        AssertTrue(submitted.Succeeded, "Student submission should succeed.");

        var accepted = await submissionService.AcceptSubmissionAsync(parent, new AcceptSubmissionCommand(submitted.Value, "Accepted as portfolio evidence.", true));
        AssertTrue(accepted.Succeeded, "Parent should accept evidence.");
        var evidenceBefore = await repository.GetEvidenceRecordsAsync();
        var assessmentsBefore = await repository.GetAssessmentRecordsAsync();

        var workspace = await portfolioService.GetStudentWorkspaceAsync(studentUser, setup.Student.Id);
        AssertTrue(workspace.Succeeded, "Student portfolio workspace should load.");
        var narrative = workspace.Value!.Design.Narratives.Single();
        var sectionId = workspace.Value.Design.Sections.First().SectionId;
        AssertTrue((await portfolioService.UpdateDesignAsync(studentUser, new UpdatePortfolioDesignCommand(
            setup.Student.Id,
            "Approved Export Portfolio",
            "Family archive packet.",
            [new PortfolioNarrativeInput(narrative.NarrativeId, narrative.Prompt, "growth in applied analysis.", narrative.SortOrder)]))).Succeeded, "Student should update portfolio design.");

        var draft = await portfolioService.AddDraftItemAsync(studentUser, new AddPortfolioDraftItemCommand(accepted.Value, setup.Student.Id, sectionId));
        AssertTrue(draft.Succeeded, "Student should add accepted evidence.");
        AssertTrue((await portfolioService.UpdateDraftItemAsync(studentUser, new UpdatePortfolioDraftItemCommand(
            draft.Value,
            "Applied analysis notes",
            sectionId,
            "This artifact shows careful written analysis.",
            "It belongs in the archive because it has accepted source evidence.",
            ["Analysis", "Writing"],
            1,
            true))).Succeeded, "Student should configure the portfolio item.");
        AssertTrue((await portfolioService.SubmitDesignAsync(studentUser, new SubmitPortfolioDesignCommand(setup.Student.Id))).Succeeded, "Student should submit the design.");
        AssertTrue((await portfolioService.ApproveDesignAsync(parent, new ApprovePortfolioDesignCommand(setup.Student.Id))).Succeeded, "Parent should approve the design.");

        var approvedWorkspace = await portfolioService.GetReviewWorkspaceAsync(parent, setup.Student.Id);
        AssertTrue(approvedWorkspace.Value!.LatestApprovedPreview is not null, "Approved preview should be available to parent.");
        AssertEqual("Approved Export Portfolio", approvedWorkspace.Value.LatestApprovedPreview!.Title, "Approved preview should use snapshot title.");

        var studentApprovedWorkspace = await portfolioService.GetStudentWorkspaceAsync(studentUser, setup.Student.Id);
        AssertTrue(studentApprovedWorkspace.Value!.LatestApprovedPreview is not null, "Student should be able to view the approved portfolio preview.");
        AssertEqual(PortfolioDesignStatus.Approved, studentApprovedWorkspace.Value.LatestApprovedPreview!.Status, "Student approved preview should be read-only approved state.");

        var studentExport = await exportService.CreateArchivePacketAsync(studentUser, new CreatePortfolioExportCommand(setup.Student.Id));
        AssertFalse(studentExport.Succeeded, "Student should not create the official archive export.");

        var preview = await exportService.GetExportPreviewAsync(parent, setup.Student.Id);
        AssertTrue(preview.Succeeded, "Parent should preview the approved export.");
        AssertEqual(1, preview.Value!.ItemCount, "Preview should include the approved portfolio item.");
        AssertEqual(1, preview.Value.AttachedFileCount, "Preview should count the accepted attachment.");
        AssertEqual(0, preview.Value.MissingFileCount, "Preview should not report missing files before deletion.");
        AssertEqual("Approved Export Portfolio", preview.Value.Preview.Title, "Export preview should use the approved snapshot.");

        AssertTrue((await portfolioService.UpdateDesignAsync(studentUser, new UpdatePortfolioDesignCommand(
            setup.Student.Id,
            "Later Working Draft Title",
            approvedWorkspace.Value.Design.Purpose,
            approvedWorkspace.Value.Design.Narratives
                .Select(item => new PortfolioNarrativeInput(item.NarrativeId, item.Prompt, item.Response, item.SortOrder))
                .ToArray()))).Succeeded, "Student should be able to start a later working draft.");

        var download = await exportService.CreateArchivePacketAsync(parent, new CreatePortfolioExportCommand(setup.Student.Id));
        AssertTrue(download.Succeeded, "Parent should create the approved portfolio archive packet.");
        AssertTrue(download.Value!.FileName.EndsWith(".zip", StringComparison.Ordinal), "Portfolio archive should be a zip file.");
        AssertEqual("Approved Export Portfolio", download.Value.Manifest.PortfolioTitle, "Export should use the approved snapshot, not later draft edits.");
        AssertEqual(1, download.Value.Manifest.AttachedFileCount, "Manifest should count included evidence files.");
        AssertEqual(0, download.Value.Manifest.MissingFileCount, "Manifest should not report missing files.");
        AssertEqual(evidenceBefore.Count, (await repository.GetEvidenceRecordsAsync()).Count, "Export should not create evidence records.");
        AssertEqual(assessmentsBefore.Count, (await repository.GetAssessmentRecordsAsync()).Count, "Export should not create assessment records.");
        AssertEqual(setup.Course.PlannedCreditValue, (await repository.GetCourseAsync(setup.Course.Id))!.PlannedCreditValue, "Export should not alter course credits.");

        using (var stream = new MemoryStream(download.Value.Content))
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
        {
            AssertTrue(archive.GetEntry("portfolio.html") is not null, "Archive should include printable HTML.");
            AssertTrue(archive.GetEntry("manifest.json") is not null, "Archive should include JSON manifest.");
            AssertTrue(archive.GetEntry("manifest.md") is not null, "Archive should include readable manifest.");
            AssertTrue(archive.Entries.Any(entry => entry.FullName.StartsWith("evidence-files/", StringComparison.OrdinalIgnoreCase) && entry.FullName.EndsWith("analysis-notes.txt", StringComparison.OrdinalIgnoreCase)), "Archive should include accepted evidence file content.");

            var manifestEntry = archive.GetEntry("manifest.json") ?? throw new InvalidOperationException("Manifest entry missing.");
            using var manifestStream = manifestEntry.Open();
            using var manifestDocument = JsonDocument.Parse(manifestStream);
            AssertEqual("homeschool-manager.portfolio-export", manifestDocument.RootElement.GetProperty("schema").GetString() ?? "", "Manifest should identify the portfolio export schema.");
            AssertEqual("Approved Export Portfolio", manifestDocument.RootElement.GetProperty("portfolioTitle").GetString() ?? "", "Manifest should preserve approved title.");
        }

        var storedSubmission = await repository.GetAssignmentSubmissionAsync(submitted.Value) ?? throw new InvalidOperationException("Submission missing.");
        var storedFile = storedSubmission.Attachments.Single();
        File.Delete(Path.Combine(paths.DataRoot, storedFile.StoredPath));
        var missingDownload = await exportService.CreateArchivePacketAsync(parent, new CreatePortfolioExportCommand(setup.Student.Id));
        AssertTrue(missingDownload.Succeeded, "Export should still complete with a missing file warning.");
        AssertEqual(1, missingDownload.Value!.Manifest.MissingFileCount, "Manifest should count the missing file.");
        AssertTrue(missingDownload.Value.Manifest.Warnings.Any(warning => warning.Contains("was not found", StringComparison.OrdinalIgnoreCase)), "Manifest should include a missing-file warning.");
        using var missingStream = new MemoryStream(missingDownload.Value.Content);
        using var missingArchive = new ZipArchive(missingStream, ZipArchiveMode.Read);
        AssertFalse(missingArchive.Entries.Any(entry => entry.FullName.StartsWith("evidence-files/", StringComparison.OrdinalIgnoreCase)), "Missing files should not be silently included.");
    })),
    ("K-5 portfolio editing is parent controlled", (Func<Task>)(async () =>
    {
        var repository = await CreateRepositoryAsync();
        var setupService = new SetupService(repository);
        var portfolioService = new PortfolioService(repository);
        var parent = UserContext.ParentAdmin("Parent");
        var studentUser = UserContext.Student("Student");

        AssertTrue((await setupService.CreateHouseholdAsync(parent, new CreateHouseholdCommand("Family", "Parent"))).Succeeded, "Household setup failed.");
        AssertTrue((await setupService.CreateStudentAsync(parent, new CreateStudentCommand("Elementary", "Learner", 4))).Succeeded, "K-5 student setup failed.");
        var student = await repository.GetStudentAsync() ?? throw new InvalidOperationException("Student setup failed.");

        var studentWorkspace = await portfolioService.GetStudentWorkspaceAsync(studentUser, student.Id);
        AssertTrue(studentWorkspace.Succeeded, "K-5 student should be able to view portfolio workspace state.");
        AssertFalse(studentWorkspace.Value!.CanStudentAuthor, "K-5 student should not author the portfolio design.");

        var studentEdit = await portfolioService.UpdateDesignAsync(studentUser, new UpdatePortfolioDesignCommand(
            student.Id,
            "Elementary Portfolio",
            "Trying to edit.",
            []));
        AssertFalse(studentEdit.Succeeded, "K-5 student should not edit the portfolio design.");

        var parentEdit = await portfolioService.UpdateDesignAsync(parent, new UpdatePortfolioDesignCommand(
            student.Id,
            "Elementary Learning Portfolio",
            "Parent-managed portfolio.",
            studentWorkspace.Value.Design.Narratives
                .Select(item => new PortfolioNarrativeInput(item.NarrativeId, item.Prompt, "early learning progress.", item.SortOrder))
                .ToArray()));
        AssertTrue(parentEdit.Succeeded, "Parent should edit a K-5 portfolio design.");

        var parentSection = await portfolioService.AddSectionAsync(parent, new AddPortfolioSectionCommand(
            student.Id,
            "Reading Samples",
            "Selected reading and writing work."));
        AssertTrue(parentSection.Succeeded, "Parent should add K-5 portfolio sections.");
    })),
    ("Parent can clear active submission and reopen a single-attempt assignment", (Func<Task>)(async () =>
    {
        var (repository, paths) = await CreateRepositoryWithPathsAsync();
        await CreateSetupAsync(repository);
        var courseService = new CourseService(repository);
        var submissionService = new AssignmentSubmissionService(repository, new LocalSubmissionFileStore(paths));
        var parent = UserContext.ParentAdmin("Parent");
        var studentUser = UserContext.Student("Student");
        var student = await repository.GetStudentAsync() ?? throw new InvalidOperationException("Student setup failed.");

        var course = await courseService.CreateCourseAsync(parent, new CreateCourseCommand("Writing", "Composition.", ["English"], CourseDuration.OneSemester, 0.5m, student.Id));
        AssertTrue(course.Succeeded, "Course create failed.");
        var module = await courseService.CreateLearningModuleAsync(parent, new CreateLearningModuleCommand(
            course.Value,
            "Essay",
            "Essay module.",
            null,
            "1 week",
            "Draft and submit.",
            Objectives("Write a clear argument."),
            Resources("Writing handbook"),
            "Essay evidence.",
            ModuleStatus.Active));
        AssertTrue(module.Succeeded, "Module create failed.");
        var assignment = await courseService.CreateAssignmentAsync(parent, new CreateAssignmentCommand(
            course.Value,
            module.Value,
            "Argument paragraph",
            AssignmentType.Reflection,
            InstructionalMethodProfile.Hybrid,
            "Write one paragraph.",
            "30 minutes",
            "After drafting",
            null,
            ["Write a clear argument."],
            [],
            "One paragraph.",
            "",
            false,
            10,
            null,
            AssignmentStatus.Assigned));
        AssertTrue(assignment.Succeeded, "Assignment create failed.");

        var firstSubmit = await submissionService.SubmitAssignmentAsync(
            studentUser,
            new SubmitAssignmentCommand(course.Value, module.Value, assignment.Value, "Initial paragraph.", "", [], student.Id));
        AssertTrue(firstSubmit.Succeeded, "Student submit failed.");

        var blockedWhileSubmitted = await submissionService.SubmitAssignmentAsync(
            studentUser,
            new SubmitAssignmentCommand(course.Value, module.Value, assignment.Value, "Second paragraph.", "", [], student.Id));
        AssertFalse(blockedWhileSubmitted.Succeeded, "Pending submitted work should block a duplicate submission.");

        var deniedClear = await submissionService.ClearSubmissionAsync(
            studentUser,
            new ClearSubmissionCommand(firstSubmit.Value, "Student cannot clear.", "Clear"));
        AssertFalse(deniedClear.Succeeded, "Student should not clear submissions.");

        var clear = await submissionService.ClearSubmissionAsync(
            parent,
            new ClearSubmissionCommand(firstSubmit.Value, "Cleared so the student can submit a replacement.", "Clear"));
        AssertTrue(clear.Succeeded, "Parent should clear active submitted work.");
        var cleared = await repository.GetAssignmentSubmissionAsync(firstSubmit.Value);
        AssertTrue(cleared!.Status == AssignmentSubmissionStatus.Cleared, "Cleared submission should retain history with cleared status.");

        var pending = await submissionService.ListPendingReviewsAsync(parent);
        AssertEqual(0, pending.Value!.Count, "Cleared submissions should leave pending review.");

        var gradebook = await new GradebookService(repository).GetGradebookAsync(parent, student.Id, course.Value);
        AssertTrue(gradebook.Succeeded, "Gradebook should load after clearing.");
        var rowAfterClear = gradebook.Value!.Rows.Single();
        AssertTrue(rowAfterClear.LatestSubmissionId is null, "Cleared submission should not remain active in the gradebook editor.");
        AssertEqual(0, rowAfterClear.LatestSubmissionAttachments.Count, "Cleared submission files should not remain in the active gradebook file panel.");

        var secondSubmit = await submissionService.SubmitAssignmentAsync(
            studentUser,
            new SubmitAssignmentCommand(course.Value, module.Value, assignment.Value, "Replacement paragraph.", "", [], student.Id));
        AssertTrue(secondSubmit.Succeeded, "Cleared single-attempt assignment should reopen submission.");
        var replacement = await repository.GetAssignmentSubmissionAsync(secondSubmit.Value);
        AssertEqual(2, replacement!.AttemptNumber, "Replacement submission should keep the next attempt number.");
    })),
    ("Completion statuses update without changing planned credit", (Func<Task>)(async () =>
    {
        var repository = await CreateRepositoryAsync();
        await CreateSetupAsync(repository);
        var student = await repository.GetStudentAsync() ?? throw new InvalidOperationException("Student setup failed.");
        var parent = UserContext.ParentAdmin("Parent");
        var courseService = new CourseService(repository);
        var studentCourseService = new StudentCourseService(repository);

        var course = await courseService.CreateCourseAsync(parent, new CreateCourseCommand("Ecology", "Local ecology.", ["Science"], CourseDuration.OneSemester, 0.5m, student.Id));
        AssertTrue(course.Succeeded, "Course create failed.");
        var module = await courseService.CreateLearningModuleAsync(parent, new CreateLearningModuleCommand(
            course.Value,
            "Field notes",
            "Observe and summarize.",
            null,
            "1 week",
            "Complete the field notes module.",
            Objectives("Summarize field observations."),
            Resources("Field guide"),
            "Field note evidence.",
            ModuleStatus.Active));
        AssertTrue(module.Succeeded, "Module create failed.");
        var lesson = await courseService.CreateLessonAsync(parent, new CreateLessonCommand(
            course.Value,
            module.Value,
            "Observation walk",
            "Observe the site.",
            "Summarize field observations.",
            LessonResources("Field guide")));
        AssertTrue(lesson.Succeeded, "Lesson create failed.");

        AssertTrue((await courseService.UpdateLessonCompletionStatusAsync(parent, new UpdateLessonCompletionStatusCommand(course.Value, module.Value, lesson.Value, CompletionStatus.Completed))).Succeeded, "Lesson completion update failed.");
        AssertTrue((await courseService.UpdateModuleCompletionStatusAsync(parent, new UpdateModuleCompletionStatusCommand(course.Value, module.Value, CompletionStatus.Completed))).Succeeded, "Module completion update failed.");
        AssertTrue((await courseService.UpdateCourseCompletionStatusAsync(parent, new UpdateCourseCompletionStatusCommand(course.Value, CompletionStatus.Completed))).Succeeded, "Course completion update failed.");

        var detail = await courseService.GetCourseDetailAsync(course.Value);
        AssertEqual(CompletionStatus.Completed, detail!.CompletionStatus, "Course completion status should persist.");
        AssertEqual(0.5m, detail.PlannedCreditValue, "Completion status should not alter planned credit value.");
        AssertEqual(CompletionStatus.Completed, detail.Modules.Single().CompletionStatus, "Module completion status should persist.");
        AssertEqual(CompletionStatus.Completed, detail.Modules.Single().Lessons.Single().CompletionStatus, "Lesson completion status should persist.");

        var dashboard = await studentCourseService.ListCoursesAsync(UserContext.Student("Student"), student.Id);
        AssertEqual(1, dashboard.Value!.Courses.Single().CompletedModuleCount, "Student dashboard should count completed modules from completion status.");
    })),
    ("Admin courses are scoped to the selected student", async () =>
    {
        var repository = await CreateRepositoryAsync();
        await CreateSetupAsync(repository);
        var parent = UserContext.ParentAdmin("Parent");
        var setupService = new SetupService(repository);
        var courseService = new CourseService(repository);

        AssertTrue((await setupService.CreateStudentAsync(parent, new CreateStudentCommand("Younger", "Learner", 9))).Succeeded, "Second child should be added.");
        var configuredStudents = await setupService.ListStudentsAsync();
        var primaryStudent = configuredStudents.First(item => item.FirstName == "Student");
        var secondStudent = configuredStudents.First(item => item.FirstName == "Younger");

        var primaryCourse = await courseService.CreateCourseAsync(
            parent,
            new CreateCourseCommand("Senior English", "Primary student's English course.", [], CourseDuration.TwoSemesters, 1, primaryStudent.Id));
        AssertTrue(primaryCourse.Succeeded, "Primary student course should be created.");

        var secondCourse = await courseService.CreateCourseAsync(
            parent,
            new CreateCourseCommand("Foundations Science", "Second student's science course.", [], CourseDuration.TwoSemesters, 1, secondStudent.Id));
        AssertTrue(secondCourse.Succeeded, "Second student course should be created.");

        var pack = DefaultCoursePacks.All.First(item => item.Id == DefaultCoursePacks.MichiganCollegeReadyPackId);
        var templateId = pack.Courses.First().TemplateId;
        var primaryImport = await courseService.ImportCoursePackAsync(
            parent,
            new ImportCoursePackCommand(pack.Id, [templateId], [], primaryStudent.Id));
        var secondImport = await courseService.ImportCoursePackAsync(
            parent,
            new ImportCoursePackCommand(pack.Id, [templateId], [], secondStudent.Id));
        AssertTrue(primaryImport.Succeeded && primaryImport.Value == 1, "Primary student should import the selected pack course.");
        AssertTrue(secondImport.Succeeded && secondImport.Value == 1, "Second student should import the same pack course independently.");

        var primaryCourses = await courseService.ListCoursesAsync(primaryStudent.Id);
        var secondCourses = await courseService.ListCoursesAsync(secondStudent.Id);
        var allCourses = await courseService.ListCoursesAsync();
        AssertEqual(2, primaryCourses.Count, "Primary list should include only primary student's courses.");
        AssertEqual(2, secondCourses.Count, "Second list should include only second student's courses.");
        AssertEqual(4, allCourses.Count, "Unscoped list should still include all courses for internal callers.");
        AssertTrue(primaryCourses.All(course => course.StudentId == primaryStudent.Id), "Primary list should not include another student's courses.");
        AssertTrue(secondCourses.All(course => course.StudentId == secondStudent.Id), "Second list should not include another student's courses.");

        var primaryCoverage = await courseService.GetCoverageSummaryAsync(primaryStudent.Id);
        AssertTrue(primaryCoverage.Any(item => item.CourseTitles.Any()), "Primary coverage should be based on primary student's courses.");
        AssertFalse(primaryCoverage.Any(item => item.CourseTitles.Contains("Foundations Science")), "Primary coverage should not use second student's courses.");
    }),
    ("Parent can archive and delete active student courses", async () =>
    {
        var repository = await CreateRepositoryAsync();
        await CreateSetupAsync(repository);
        var parent = UserContext.ParentAdmin("Parent");
        var courseService = new CourseService(repository);
        var studentService = new StudentCourseService(repository);
        var student = await repository.GetStudentAsync();
        if (student is null)
        {
            throw new InvalidOperationException("Setup did not create a student.");
        }

        var first = await courseService.CreateCourseAsync(
            parent,
            new CreateCourseCommand("Course to Archive", "Archive test.", [], CourseDuration.OneSemester, 0.5m, student.Id));
        var second = await courseService.CreateCourseAsync(
            parent,
            new CreateCourseCommand("Course to Delete", "Delete test.", [], CourseDuration.OneSemester, 0.5m, student.Id));
        AssertTrue(first.Succeeded && second.Succeeded, "Courses should be created.");

        var archive = await courseService.ArchiveCoursesAsync(
            parent,
            new CourseListActionCommand(student.Id, [first.Value], false));
        AssertTrue(archive.Succeeded, "Archive should succeed.");
        AssertEqual(1, archive.Value!.SuccessCount, "One course should be archived.");
        AssertEqual(0, archive.Value.Failures.Count, "Archive should not fail.");

        var activeCourses = await courseService.ListCoursesAsync(student.Id);
        AssertFalse(activeCourses.Any(course => course.Id == first.Value), "Archived course should leave the active admin course list.");
        var studentDashboard = await studentService.ListCoursesAsync(UserContext.Student("Student"), student.Id);
        AssertTrue(studentDashboard.Succeeded, "Student dashboard should load.");
        AssertFalse(studentDashboard.Value!.Courses.Any(course => course.CourseId == first.Value), "Archived course should leave the student course list.");
        AssertTrue((await repository.GetCourseAsync(first.Value))?.IsArchived == true, "Archived course should remain in storage.");

        var delete = await courseService.DeleteCoursesAsync(
            parent,
            new CourseListActionCommand(student.Id, [second.Value], false));
        AssertTrue(delete.Succeeded, "Delete should succeed.");
        AssertEqual(1, delete.Value!.SuccessCount, "One course should be deleted.");
        AssertEqual(0, delete.Value.Failures.Count, "Delete should not fail.");
        AssertTrue(await repository.GetCourseAsync(second.Value) is null, "Deleted course should be removed from storage.");
    }),
    ("Course details and mapping persist and coverage shows unmapped areas", async () =>
    {
        var repository = await CreateRepositoryAsync();
        await CreateSetupAsync(repository);
        var requirementService = new RequirementService(repository);
        var courseService = new CourseService(repository);
        var parent = UserContext.ParentAdmin("Parent");

        AssertTrue((await requirementService.SeedMichiganAsync(parent)).Succeeded, "Michigan seed failed.");
        var createResult = await courseService.CreateCourseAsync(parent, new CreateCourseCommand("Homestead Biology", "Biology through soil and ecology.", ["Science", "Writing"], CourseDuration.TwoSemesters, 1));
        AssertTrue(createResult.Succeeded, "Course create failed.");
        var courseId = createResult.Value;
        if (courseId == Guid.Empty)
        {
            throw new InvalidOperationException("Course id was not returned.");
        }

        AssertTrue((await courseService.SaveCourseDescriptionAsync(parent, new SaveCourseDescriptionCommand(
            courseId,
            "Biology through soil, plants, and ecology.",
            "Reading, field observation, and project work.",
            "Soil science; plant biology.",
            "Textbook and field notes.",
            "Projects and parent evaluation.",
            "Letter grade"))).Succeeded, "Description save failed.");

        AssertTrue((await courseService.SaveCurriculumPlanAsync(parent, new SaveCurriculumPlanCommand(
            courseId,
            "Understand living systems in practical contexts.",
            "Explain soil biology and plant growth.",
            "Biology text and garden records.",
            "Fall soil work, spring plant study.",
            "Practical homesteading emphasis."))).Succeeded, "Plan save failed.");

        var checklist = await requirementService.GetChecklistAsync();
        var science = checklist.First(item => item.Name == "Science" && item.View == "Statutory");
        AssertTrue((await courseService.SetRequirementMappingsAsync(parent, new SetCourseRequirementMappingsCommand(
            courseId,
            [new RequirementMappingCommand(science.RequirementAreaId, CoverageLevel.Primary, "Main science course")]))).Succeeded, "Mapping failed.");

        var detail = await courseService.GetCourseDetailAsync(courseId);
        if (detail is null)
        {
            throw new InvalidOperationException("Course detail was not found.");
        }

        AssertEqual("Biology through soil, plants, and ecology.", detail.Description, "Description did not persist.");
        AssertTrue(detail.SubjectAreas.Contains("Writing"), "Multiple subject areas should persist.");
        AssertEqual(1, detail.Mappings.Count, "Mapping did not persist.");

        var coverage = await courseService.GetCoverageSummaryAsync();
        AssertTrue(coverage.Any(item => item.Name == "Science" && item.IsMapped), "Science should be mapped.");
        AssertTrue(coverage.Any(item => item.Name == "English Grammar" && !item.IsMapped), "Unmapped areas should remain visible.");
    }),
    ("Default course pack imports once and creates multi-subject transcript courses", (Func<Task>)(async () =>
    {
        var repository = await CreateRepositoryAsync();
        await CreateSetupAsync(repository);
        var requirementService = new RequirementService(repository);
        var courseService = new CourseService(repository);
        var parent = UserContext.ParentAdmin("Parent");

        AssertTrue((await requirementService.SeedMichiganAsync(parent)).Succeeded, "Michigan seed failed.");
        var firstImport = await courseService.ImportCoursePackAsync(parent, new ImportCoursePackCommand(DefaultCoursePacks.MichiganCollegeReadyPackId, []));
        AssertTrue(firstImport.Succeeded, "Course pack import failed.");
        AssertTrue(firstImport.Value > 0, "Course pack should import courses.");

        var secondImport = await courseService.ImportCoursePackAsync(parent, new ImportCoursePackCommand(DefaultCoursePacks.MichiganCollegeReadyPackId, []));
        AssertTrue(secondImport.Succeeded, "Second course pack import failed.");
        AssertEqual(0, secondImport.Value, "Second import should skip existing template courses.");

        var courses = await courseService.ListCoursesAsync();
        var english = courses.First(course => course.Title == "English Language Arts 12");
        AssertTrue(english.Description.Contains("senior English", StringComparison.OrdinalIgnoreCase), "Course list should include the course description.");
        AssertTrue(english.SubjectAreas.Contains("Reading"), "ELA course should include reading.");
        AssertTrue(english.SubjectAreas.Contains("English Grammar"), "ELA course should include grammar.");
        AssertEqual((int)CourseDuration.TwoSemesters, (int)english.Duration, "ELA should be a two-semester course.");

        var government = courses.First(course => course.Title == "Government and Economics");
        AssertEqual((int)CourseDuration.TwoSemesters, (int)government.Duration, "Government and Economics should be the two-semester default social studies course.");

        var history = courses.First(course => course.Title == "U.S. History and Geography");
        AssertEqual((int)CourseDuration.TwoSemesters, (int)history.Duration, "Default history should be a two-semester course.");

        var math = courses.First(course => course.Title == "Math 12: Quantitative Reasoning");
        AssertTrue(math.Description.Contains("quantitative reasoning", StringComparison.OrdinalIgnoreCase), "Default math should use the v4 Math 12 description.");

        var science = courses.First(course => course.Title == "Physics");
        AssertTrue(science.Description.Contains("Physics", StringComparison.OrdinalIgnoreCase), "Default science should be Physics with its description.");

        var capstone = courses.First(course => course.Title == "Experiential Capstone");
        AssertTrue(capstone.SubjectAreas.Contains("Elective"), "Capstone should behave like an elective.");

        var worldLanguage = courses.First(course => course.Title == "Spanish");
        AssertTrue(worldLanguage.SubjectAreas.Contains("World Language"), "Default world language should preserve its subject label.");

        var coverage = await courseService.GetCoverageSummaryAsync();
        AssertEqual("Civics", coverage.First().Name, "Statutory coverage groups should be listed first.");
        AssertTrue(coverage.Any(item => item.Name == "Reading" && item.Source == "Statutory" && item.IsMapped), "Reading should be mapped through the statutory English subject rows.");
        AssertTrue(coverage.Any(item => item.Name == "Writing" && item.Source == "Statutory" && item.IsMapped), "Writing should be mapped through the statutory English subject rows.");
        AssertTrue(coverage.Any(item => item.Name == "U.S. Constitution" && item.Source == "MDE Summary" && item.IsMapped), "U.S. Constitution should be mapped by the default government and U.S. history courses.");
        AssertTrue(coverage.Any(item => item.Name == "Michigan Constitution" && item.Source == "MDE Summary" && item.IsMapped), "Michigan Constitution should be mapped by the default government and U.S. history courses.");
        var mathematics = coverage.First(item => item.Name == "Mathematics");
        AssertTrue(mathematics.Source.Contains("Statutory", StringComparison.Ordinal), "Mathematics should include statutory source.");
        AssertEqual(mathematics.CourseTitles.Count, mathematics.CourseTitles.Distinct().Count(), "Coverage summary should not duplicate course titles.");

        var governmentDetail = await courseService.GetCourseDetailAsync(government.Id);
        if (governmentDetail is null)
        {
            throw new InvalidOperationException("Government course detail was not found.");
        }

        AssertTrue(governmentDetail.Modules.Count >= 3, "Imported default course should include learning modules.");
        AssertTrue(governmentDetail.Modules.All(module => !string.IsNullOrWhiteSpace(module.Instructions)), "Imported modules should include instructions.");
    })),
    ("Selected course pack import imports only selected templates", (Func<Task>)(async () =>
    {
        var repository = await CreateRepositoryAsync();
        await CreateSetupAsync(repository);
        var requirementService = new RequirementService(repository);
        var courseService = new CourseService(repository);
        var parent = UserContext.ParentAdmin("Parent");

        AssertTrue((await requirementService.SeedMichiganAsync(parent)).Succeeded, "Michigan seed failed.");
        var import = await courseService.ImportCoursePackAsync(
            parent,
            new ImportCoursePackCommand(
                DefaultCoursePacks.MichiganCollegeReadyPackId,
                [],
                [
                    new CoursePackSelectionCommand("ela-12", "ela-12"),
                    new CoursePackSelectionCommand("math-12", "calculus-i"),
                    new CoursePackSelectionCommand("physics", "environmental-science")
                ]));
        AssertTrue(import.Succeeded, "Selected course pack import failed.");
        AssertEqual(3, import.Value, "Selected import should import three courses.");

        var courses = await courseService.ListCoursesAsync();
        AssertEqual(3, courses.Count, "Unexpected selected course count.");
        AssertTrue(courses.Any(course => course.Title == "English Language Arts 12"), "ELA should import.");
        AssertTrue(courses.Any(course => course.Title == "Calculus I"), "Selected math option should import.");
        AssertTrue(courses.Any(course => course.Title == "Environmental Science"), "Selected science option should import.");
        AssertFalse(courses.Any(course => course.Title == "Physics"), "Unselected default science option should not import.");
    })),
    ("Installed course pack import skips unmatched requirement mappings", (Func<Task>)(async () =>
    {
        var repository = await CreateRepositoryAsync();
        await CreateSetupAsync(repository);
        var parent = UserContext.ParentAdmin("Parent");
        var sourcePack = DefaultCoursePacks.All.First(item => item.Id == DefaultCoursePacks.MichiganCollegeReadyPackId);
        var sourceTemplate = sourcePack.Courses.First(item => item.TemplateId == "government-economics");
        var sourceOption = sourceTemplate.DefaultOption;
        var legacyOption = sourceOption with
        {
            RequirementMappings =
            [
                new CourseTemplateRequirementMapping("Statutory", "Social Studies", CoverageLevel.Primary, "Legacy generic social studies mapping.")
            ]
        };
        var legacyTemplate = sourceTemplate with
        {
            TemplateId = "legacy-social-studies",
            Title = legacyOption.Title,
            DefaultOptionId = legacyOption.OptionId,
            Options = [legacyOption]
        };
        var legacyPack = new CoursePackDefinition(
            "legacy-social-studies-pack",
            "Legacy Social Studies Pack",
            "Pack with an older generic requirement mapping.",
            "Michigan",
            [legacyTemplate]);
        await repository.SaveInstalledCoursePackAsync(legacyPack);

        var courseService = new CourseService(repository);
        var import = await courseService.ImportCoursePackAsync(parent, new ImportCoursePackCommand(legacyPack.Id, []));
        AssertTrue(import.Succeeded, "Import should succeed even when a pack mapping points to a retired requirement area.");
        AssertEqual(1, import.Value, "One course should be imported from the legacy pack.");

        var courses = await courseService.ListCoursesAsync();
        AssertEqual(1, courses.Count, "Imported legacy pack should create the course.");
        var detail = await courseService.GetCourseDetailAsync(courses[0].Id);
        AssertTrue(detail is not null, "Imported course detail should load.");
        AssertFalse(detail!.Mappings.Any(mapping => mapping.RequirementAreaName == "Social Studies"), "Unmatched legacy mapping should be omitted for parent review.");
    })),
    ("Michigan course pack import repairs stale requirement seed", (Func<Task>)(async () =>
    {
        var repository = await CreateRepositoryAsync();
        await CreateSetupAsync(repository);
        var staleAreas = MichiganRequirementSeed.CreateAreas()
            .Where(area => area.View != "MMC Reference")
            .ToArray();
        await repository.SaveRequirementSeedAsync(MichiganRequirementSeed.CreateSet(), staleAreas);

        var courseService = new CourseService(repository);
        var parent = UserContext.ParentAdmin("Parent");
        var import = await courseService.ImportCoursePackAsync(parent, new ImportCoursePackCommand(DefaultCoursePacks.MichiganCollegeReadyPackId, []));
        AssertTrue(import.Succeeded, "Course pack import should repair missing Michigan requirement areas.");

        var coverage = await courseService.GetCoverageSummaryAsync();
        var personalFinance = coverage.First(item => item.Name == "Personal Finance");
        AssertTrue(personalFinance.Source.Contains("MMC Reference", StringComparison.Ordinal), "MMC personal finance reference should be restored.");
        AssertTrue(personalFinance.IsMapped, "Personal Finance should be mapped after seed repair.");
    })),
    ("Imported course mappings migrate away from stale duplicate requirement rows", (Func<Task>)(async () =>
    {
        var repository = await CreateRepositoryAsync();
        await CreateSetupAsync(repository);
        var student = await repository.GetStudentAsync();
        var schoolYear = await repository.GetSchoolYearAsync();
        if (student is null || schoolYear is null)
        {
            throw new InvalidOperationException("Setup did not create student and school year.");
        }

        var requirementSet = MichiganRequirementSeed.CreateSet();
        var staleEnglishMde = new RequirementArea(
            DeterministicGuid("MDE Summary:English Language Arts"),
            requirementSet.Id,
            "English Language Arts",
            "",
            "All grades",
            "Guidance",
            "MDE Summary");
        var staleEnglishMmc = new RequirementArea(
            DeterministicGuid("MMC Reference:English Language Arts"),
            requirementSet.Id,
            "English Language Arts",
            "",
            "High school",
            "Reference",
            "MMC Reference");
        var staleAreas = MichiganRequirementSeed.CreateAreas()
            .Concat([staleEnglishMde, staleEnglishMmc])
            .ToArray();
        await repository.SaveRequirementSeedAsync(requirementSet, staleAreas);

        var courseId = Guid.NewGuid();
        await repository.SaveCourseAsync(new Course(
            courseId,
            student.Id,
            schoolYear.Id,
            "English Language Arts 12",
            ["English Language Arts"],
            CourseDuration.TwoSemesters,
            1,
            DefaultCoursePacks.MichiganCollegeReadyPackId,
            "ela-12",
            null,
            null,
            [
                new RequirementMapping(Guid.NewGuid(), courseId, staleEnglishMde.Id, CoverageLevel.Primary, "Old MDE mapping."),
                new RequirementMapping(Guid.NewGuid(), courseId, staleEnglishMmc.Id, CoverageLevel.Primary, "Old MMC mapping.")
            ]));

        var service = new CourseService(repository);
        var coverage = await service.GetCoverageSummaryAsync();
        AssertFalse(coverage.Any(item => item.Name == "English Language Arts"), "Duplicate ELA requirement rows should be removed from coverage lists.");
        AssertTrue(coverage.Any(item => item.Name == "Reading" && item.Source == "Statutory" && item.IsMapped), "Imported ELA course should migrate to statutory Reading coverage.");
        AssertTrue(coverage.Any(item => item.Name == "Writing" && item.Source == "Statutory" && item.IsMapped), "Imported ELA course should migrate to statutory Writing coverage.");

        var detail = await service.GetCourseDetailAsync(courseId);
        if (detail is null)
        {
            throw new InvalidOperationException("Course detail was not found.");
        }

        AssertFalse(detail.Mappings.Any(mapping => mapping.RequirementAreaName == "English Language Arts"), "Stale ELA mappings should be removed from imported course details.");
    })),
    ("Imported courses backfill default-pack modules without replacing parent modules", (Func<Task>)(async () =>
    {
        var repository = await CreateRepositoryAsync();
        await CreateSetupAsync(repository);
        var student = await repository.GetStudentAsync();
        var schoolYear = await repository.GetSchoolYearAsync();
        if (student is null || schoolYear is null)
        {
            throw new InvalidOperationException("Setup did not create student and school year.");
        }

        var courseId = Guid.NewGuid();
        var parentModule = new LearningModule(
            Guid.NewGuid(),
            courseId,
            "",
            1,
            "Parent custom module",
            "Parent-created module.",
            "1 week",
            "Parent-created instructions.",
            "Parent topic",
            "Explain the parent-selected topic.",
            "Parent resource",
            "Parent evidence placeholder.",
            ModuleStatus.Active);
        await repository.SaveCourseAsync(new Course(
            courseId,
            student.Id,
            schoolYear.Id,
            "Precalculus",
            ["Mathematics"],
            CourseDuration.TwoSemesters,
            1,
            DefaultCoursePacks.MichiganCollegeReadyPackId,
            "math-12",
            null,
            null,
            [],
            [parentModule]));

        var service = new CourseService(repository);
        var detail = await service.GetCourseDetailAsync(courseId);
        if (detail is null)
        {
            throw new InvalidOperationException("Course detail was not found.");
        }

        AssertTrue(detail.Modules.Any(module => module.Title == "Parent custom module" && module.Instructions == "Parent-created instructions."), "Backfill should preserve parent-created module text.");
        AssertTrue(detail.Modules.Any(module => !string.IsNullOrWhiteSpace(module.SourceModuleId)), "Backfill should add built-in pack modules.");
        AssertTrue(detail.Modules.Where(module => !string.IsNullOrWhiteSpace(module.SourceModuleId)).All(module => module.Lessons.Count > 0), "Backfilled source modules should include lessons.");
        AssertTrue(detail.Modules.SelectMany(module => module.Lessons).All(lesson => lesson.Resources.Count > 0), "Backfilled lessons should include resources.");
        AssertTrue(detail.Modules.Count > 1, "Backfill should keep parent module and add starter modules.");
    })),
    ("Imported source modules with no lessons are backfilled", (Func<Task>)(async () =>
    {
        var repository = await CreateRepositoryAsync();
        await CreateSetupAsync(repository);
        var student = await repository.GetStudentAsync();
        var schoolYear = await repository.GetSchoolYearAsync();
        if (student is null || schoolYear is null)
        {
            throw new InvalidOperationException("Setup did not create student and school year.");
        }

        var courseId = Guid.NewGuid();
        var existingPackModule = new LearningModule(
            Guid.NewGuid(),
            courseId,
            "personal-finance-module-1",
            1,
            "Budgeting and related topics",
            "Existing imported module without lesson rows.",
            "3-5 weeks",
            "Existing module instructions.",
            "",
            "Explain budgeting and credit decisions.",
            "",
            "Existing evidence placeholder.",
            ModuleStatus.Planned,
            null,
            [new ModuleLearningObjective("Explain budgeting and credit decisions.", "")],
            [],
            []);
        await repository.SaveCourseAsync(new Course(
            courseId,
            student.Id,
            schoolYear.Id,
            "Personal Finance",
            ["Mathematics"],
            CourseDuration.OneSemester,
            0.5m,
            DefaultCoursePacks.MichiganCollegeReadyPackId,
            "personal-finance",
            null,
            null,
            [],
            [existingPackModule]));

        var service = new CourseService(repository);
        var detail = await service.GetCourseDetailAsync(courseId);
        if (detail is null)
        {
            throw new InvalidOperationException("Course detail was not found.");
        }

        var backfilledModule = detail.Modules.First(module => module.SourceModuleId == "personal-finance-module-1");
        AssertTrue(backfilledModule.Lessons.Count > 0, "Existing imported source modules with empty lessons should be backfilled.");
        AssertTrue(backfilledModule.Lessons.All(lesson => lesson.Resources.Count > 0), "Backfilled source lessons should include resources.");
    })),
    ("Current imported modules with stripped lessons are detected as changed", (Func<Task>)(async () =>
    {
        var repository = await CreateRepositoryAsync();
        await CreateSetupAsync(repository);
        var service = new CourseService(repository);
        var parent = UserContext.ParentAdmin("Parent");
        var import = await service.ImportCoursePackAsync(parent, new ImportCoursePackCommand(DefaultCoursePacks.MichiganCollegeReadyPackId, []));
        AssertTrue(import.Succeeded, "Default pack import should succeed.");

        var importedCourse = (await repository.GetCoursesAsync()).First(course =>
            string.Equals(course.SourceTemplateId, "personal-finance", StringComparison.OrdinalIgnoreCase));
        AssertTrue(importedCourse.Modules.All(module => module.Lessons.Count > 0), "Imported modules should start with lessons.");

        var strippedModules = importedCourse.Modules
            .Select(module => module.WithLessons([]))
            .ToArray();
        await repository.SaveCourseAsync(importedCourse.WithModules(strippedModules));
        var strippedCourse = await repository.GetCourseAsync(importedCourse.Id);
        AssertTrue(strippedCourse?.Modules.All(module => module.Lessons.Count == 0) ?? false, "Test setup should strip lessons from otherwise current imported modules.");

        var detail = await service.GetCourseDetailAsync(importedCourse.Id);
        if (detail is null)
        {
            throw new InvalidOperationException("Course detail was not found.");
        }

        AssertTrue(detail.Modules.All(module => module.Lessons.Count > 0), "Backfill comparison should detect missing nested lessons and save the repair.");
        AssertTrue(detail.Modules.SelectMany(module => module.Lessons).All(lesson => lesson.Resources.Count > 0), "Repaired lessons should include resources.");
    })),
    ("Imported government and U.S. history courses backfill constitution mappings", (Func<Task>)(async () =>
    {
        var repository = await CreateRepositoryAsync();
        await CreateSetupAsync(repository);
        var requirementService = new RequirementService(repository);
        var parent = UserContext.ParentAdmin("Parent");
        AssertTrue((await requirementService.SeedMichiganAsync(parent)).Succeeded, "Michigan seed failed.");

        var student = await repository.GetStudentAsync();
        var schoolYear = await repository.GetSchoolYearAsync();
        if (student is null || schoolYear is null)
        {
            throw new InvalidOperationException("Setup did not create student and school year.");
        }

        var areas = await repository.GetRequirementAreasAsync();
        var civics = areas.First(area => area.View == "Statutory" && area.Name == "Civics");
        var history = areas.First(area => area.View == "Statutory" && area.Name == "History");

        var governmentCourseId = Guid.NewGuid();
        await repository.SaveCourseAsync(new Course(
            governmentCourseId,
            student.Id,
            schoolYear.Id,
            "Government and Economics",
            ["Social Studies", "Civics", "Economics"],
            CourseDuration.TwoSemesters,
            1,
            DefaultCoursePacks.MichiganCollegeReadyPackId,
            "government-economics",
            null,
            null,
            [new RequirementMapping(Guid.NewGuid(), governmentCourseId, civics.Id, CoverageLevel.Primary, "Old civics mapping.")]));

        var historyCourseId = Guid.NewGuid();
        await repository.SaveCourseAsync(new Course(
            historyCourseId,
            student.Id,
            schoolYear.Id,
            "U.S. History and Geography",
            ["History", "Social Studies"],
            CourseDuration.TwoSemesters,
            1,
            DefaultCoursePacks.MichiganCollegeReadyPackId,
            "us-history-geography",
            null,
            null,
            [new RequirementMapping(Guid.NewGuid(), historyCourseId, history.Id, CoverageLevel.Primary, "Old history mapping.")]));

        var courseService = new CourseService(repository);
        var governmentDetail = await courseService.GetCourseDetailAsync(governmentCourseId);
        var historyDetail = await courseService.GetCourseDetailAsync(historyCourseId);
        if (governmentDetail is null || historyDetail is null)
        {
            throw new InvalidOperationException("Course detail was not found.");
        }

        AssertTrue(governmentDetail.Mappings.Any(mapping => mapping.RequirementView == "MDE Summary" && mapping.RequirementAreaName == "U.S. Constitution"), "Government course should backfill U.S. Constitution coverage.");
        AssertTrue(governmentDetail.Mappings.Any(mapping => mapping.RequirementView == "MDE Summary" && mapping.RequirementAreaName == "Michigan Constitution"), "Government course should backfill Michigan Constitution coverage.");
        AssertTrue(historyDetail.Mappings.Any(mapping => mapping.RequirementView == "MDE Summary" && mapping.RequirementAreaName == "U.S. Constitution"), "U.S. history course should backfill U.S. Constitution coverage.");
        AssertTrue(historyDetail.Mappings.Any(mapping => mapping.RequirementView == "MDE Summary" && mapping.RequirementAreaName == "Michigan Constitution"), "U.S. history course should backfill Michigan Constitution coverage.");
    })),
    ("Imported course detail backfill fills blank pack fields without overwriting parent text", (Func<Task>)(async () =>
    {
        var repository = await CreateRepositoryAsync();
        await CreateSetupAsync(repository);
        var student = await repository.GetStudentAsync();
        var schoolYear = await repository.GetSchoolYearAsync();
        if (student is null || schoolYear is null)
        {
            throw new InvalidOperationException("Setup did not create student and school year.");
        }

        var courseId = Guid.NewGuid();
        await repository.SaveCourseAsync(new Course(
            courseId,
            student.Id,
            schoolYear.Id,
            "Precalculus",
            ["Mathematics"],
            CourseDuration.TwoSemesters,
            1,
            DefaultCoursePacks.MichiganCollegeReadyPackId,
            "math-12",
            new CourseDescription("Parent custom description.", "", "", "", "", ""),
            CurriculumPlan.Empty,
            []));

        var service = new CourseService(repository);
        var detail = await service.GetCourseDetailAsync(courseId);
        if (detail is null)
        {
            throw new InvalidOperationException("Course detail was not found.");
        }

        AssertEqual("Parent custom description.", detail.Description, "Backfill should not overwrite parent description.");
        AssertTrue(detail.MajorTopics.Contains("Functions", StringComparison.OrdinalIgnoreCase), "Backfill should fill major topics.");
        AssertTrue(detail.TextsAndResources.Contains("OpenStax", StringComparison.OrdinalIgnoreCase), "Backfill should fill resources.");
        AssertTrue(detail.InstructionalMethods.Contains("Instruction combines", StringComparison.OrdinalIgnoreCase), "Backfill should fill instructional methods.");
        AssertTrue(detail.AssessmentMethods.Contains("Assessment uses", StringComparison.OrdinalIgnoreCase), "Backfill should fill assessment methods.");
        AssertTrue(detail.GradingBasis.Contains("Mastery", StringComparison.OrdinalIgnoreCase), "Backfill should fill grading basis.");
        AssertFalse(string.IsNullOrWhiteSpace(detail.Goals), "Backfill should fill curriculum goals.");
        AssertFalse(string.IsNullOrWhiteSpace(detail.LearningObjectives), "Backfill should fill learning objectives.");
        AssertTrue(string.IsNullOrWhiteSpace(detail.MajorResources), "Backfill should not restore retired curriculum resources.");
        AssertFalse(string.IsNullOrWhiteSpace(detail.PlannedSequence), "Backfill should fill planned sequence.");
    })),
    ("Imported course module backfill adds assignments linked to local lessons", (Func<Task>)(async () =>
    {
        var repository = await CreateRepositoryAsync();
        await CreateSetupAsync(repository);
        var student = await repository.GetStudentAsync();
        var schoolYear = await repository.GetSchoolYearAsync();
        if (student is null || schoolYear is null)
        {
            throw new InvalidOperationException("Setup did not create student and school year.");
        }

        var pack = DefaultCoursePacks.All.First(item => item.Id == DefaultCoursePacks.MichiganCollegeReadyPackId);
        var template = pack.Courses.First(item => item.TemplateId == "math-12");
        var option = template.DefaultOption;
        var sourceModule = option.Modules.First();
        var courseId = Guid.NewGuid();
        var moduleId = Guid.NewGuid();
        var existingLesson = new Lesson(
            Guid.NewGuid(),
            moduleId,
            sourceModule.Lessons[0].LessonId,
            1,
            sourceModule.Lessons[0].Title,
            sourceModule.Lessons[0].IntroductoryText,
            sourceModule.Lessons[0].LinkedModuleObjective,
            LessonResources("Legacy source").Select(command => new LessonResource(
                Guid.NewGuid(),
                command.Name,
                command.Type,
                command.Url,
                command.FilePath,
                command.IsPhysicalResource,
                command.SourceNote)).ToArray());
        var existingModule = new LearningModule(
            moduleId,
            courseId,
            sourceModule.ModuleId,
            sourceModule.SequenceOrder,
            sourceModule.Title,
            sourceModule.Description,
            sourceModule.EstimatedLength,
            sourceModule.Instructions,
            "",
            string.Join(Environment.NewLine, sourceModule.LearningObjectives.Select(objective => objective.Text)),
            "",
            sourceModule.AssignmentEvidencePlaceholder,
            ModuleStatus.Active,
            learningObjectiveItems: sourceModule.LearningObjectives.Select(objective => new ModuleLearningObjective(objective.Text, objective.LinkedCourseObjective)).ToArray(),
            lessons: [existingLesson]);

        await repository.SaveCourseAsync(new Course(
            courseId,
            student.Id,
            schoolYear.Id,
            option.Title,
            option.SubjectAreas,
            option.Duration,
            option.PlannedCreditValue,
            pack.Id,
            template.TemplateId,
            option.Description,
            option.CurriculumPlan,
            [],
            [existingModule]));

        var requirementService = new RequirementService(repository);
        AssertTrue((await requirementService.SeedMichiganAsync(UserContext.ParentAdmin("Parent"))).Succeeded, "Michigan seed failed.");
        var service = new CourseService(repository);
        var modules = await service.ListModulesAsync(courseId);
        var module = modules.First(item => item.SourceModuleId == sourceModule.ModuleId);
        AssertTrue(module.Lessons.Count >= sourceModule.Lessons.Count, "Backfill should add missing pack lessons.");
        AssertTrue(module.Assignments.Count > 0, "Backfill should add pack assignments.");
        AssertTrue(module.Assignments.All(assignment => assignment.LinkedLessonIds.Count > 0), "Backfilled assignments should link to local lessons.");
        AssertTrue(module.Assignments.All(assignment => assignment.LinkedLessonIds.All(lessonId => module.Lessons.Any(lesson => lesson.Id == lessonId))), "Backfilled assignment lesson links should point to local lessons.");
    })),
    ("Imported course detail backfill upgrades legacy pack defaults", (Func<Task>)(async () =>
    {
        var repository = await CreateRepositoryAsync();
        await CreateSetupAsync(repository);
        var student = await repository.GetStudentAsync();
        var schoolYear = await repository.GetSchoolYearAsync();
        if (student is null || schoolYear is null)
        {
            throw new InvalidOperationException("Setup did not create student and school year.");
        }

        var courseId = Guid.NewGuid();
        await repository.SaveCourseAsync(new Course(
            courseId,
            student.Id,
            schoolYear.Id,
            "Precalculus",
            ["Mathematics"],
            CourseDuration.TwoSemesters,
            1,
            DefaultCoursePacks.MichiganCollegeReadyPackId,
            "math-12",
            new CourseDescription(
                "Parent custom description.",
                "Explicit instruction with guided practice, discussion, independent reading or problem work, applied projects, and parent feedback.",
                "Advanced functions; trigonometry.",
                "OpenStax Precalculus; Khan Academy Precalculus; CK-12 Precalculus; Desmos graphing activities.",
                "Ongoing formative checks, reviewed assignments, discussion or conference notes, quizzes or problem sets where appropriate.",
                "Mastery-aligned letter grade using parent-reviewed evidence."),
            new CurriculumPlan(
                "Parent custom goals.",
                "Explain major concepts in Precalculus; apply course skills in written, oral, practical, or problem-based work; use appropriate vocabulary and resources; and produce evidence suitable for course records.",
                "OpenStax Precalculus; Khan Academy Precalculus; CK-12 Precalculus; Desmos graphing activities.",
                "Parent custom sequence.",
                "Parent custom notes."),
            []));

        var service = new CourseService(repository);
        var detail = await service.GetCourseDetailAsync(courseId);
        if (detail is null)
        {
            throw new InvalidOperationException("Course detail was not found.");
        }

        AssertEqual("Parent custom description.", detail.Description, "Backfill should not overwrite parent description.");
        AssertTrue(detail.TextsAndResources.Contains('|'), "Legacy resources should upgrade to linked item rows.");
        AssertTrue(detail.InstructionalMethods.Contains("Instruction combines", StringComparison.OrdinalIgnoreCase), "Legacy instructional methods should upgrade to the v4 default.");
        AssertTrue(detail.AssessmentMethods.Contains("Assessment uses", StringComparison.OrdinalIgnoreCase), "Legacy assessment methods should upgrade to the v4 default.");
        AssertTrue(detail.GradingBasis.Contains("Suggested weighting", StringComparison.OrdinalIgnoreCase), "Legacy grading basis should upgrade to the v4 default.");
        AssertTrue(detail.LearningObjectives.Split(Environment.NewLine).Length >= 3, "Legacy learning objectives should upgrade to separated objective rows.");
        AssertFalse(detail.LearningObjectives.Contains("produce evidence suitable for course records", StringComparison.OrdinalIgnoreCase), "Legacy learning objectives should upgrade away from generic recordkeeping language.");
        AssertEqual("Parent custom goals.", detail.Goals, "Backfill should not overwrite parent goals.");
        AssertEqual("Parent custom sequence.", detail.PlannedSequence, "Backfill should not overwrite parent sequence.");
    })),
    ("Transcript uses only parent-recorded final grade and earned credit", (Func<Task>)(async () =>
    {
        var repository = await CreateRepositoryAsync();
        var setup = await CreateCourseWithAssignmentAsync(repository);
        var service = new TranscriptService(repository);
        await repository.SaveCourseAsync(setup.Course with
        {
            CompletionStatus = CompletionStatus.Completed,
            Description = new CourseDescription(
                "A focused science course with documented lab and evidence work.",
                "Parent-led discussion and independent work.",
                "Observation, evidence analysis, and written explanation.",
                "Lab notes and source readings.",
                "Parent-reviewed assignments and course evaluation.",
                "Letter grade from parent-reviewed evidence.")
        });

        var before = await service.GetTranscriptAsync(UserContext.ParentAdmin("Parent"), setup.Student.Id);
        var beforeCourse = before.Value?.Years.SelectMany(year => year.Courses).Single() ?? throw new InvalidOperationException("Expected a transcript course row.");
        AssertEqual("Not recorded", beforeCourse.FinalGrade, "Missing final grade must stay explicit.");
        AssertFalse(beforeCourse.EarnedCreditValue.HasValue, "Credit must not be inferred from planned credit.");
        AssertEqual("Grade 12", before.Value?.TypicalGradeRange ?? "", "Transcript grade span should reflect only course records available in the system.");
        AssertFalse(before.Value?.SpanLabel.Contains("Grades 9-12", StringComparison.OrdinalIgnoreCase) == true, "Transcript title should not overstate unavailable high school years.");
        AssertTrue(before.Value?.CoverageNote.Contains("Other transcripts may be available for grades 9-11", StringComparison.OrdinalIgnoreCase) == true, "Partial high school transcript should identify that other transcripts may exist.");

        var saved = await service.SaveCourseRecordAsync(UserContext.ParentAdmin("Parent"), new SaveTranscriptCourseRecordCommand(
            setup.Student.Id,
            setup.Course.Id,
            "A",
            0.5m,
            new DateOnly(2027, 5, 28),
            "Completed course with parent-reviewed assignments and evaluation.",
            "Strong evidence across the course.",
            true));
        AssertTrue(saved.Succeeded, "Parent transcript course record should save.");

        var after = await service.GetTranscriptAsync(UserContext.ParentAdmin("Parent"), setup.Student.Id);
        var row = after.Value?.Years.SelectMany(year => year.Courses).Single() ?? throw new InvalidOperationException("Expected an updated transcript course row.");
        AssertEqual("A", row.FinalGrade, "Final grade should come from the parent transcript record.");
        AssertEqual(0.5m, row.EarnedCreditValue ?? 0, "Earned credit should come from the parent transcript record.");
        AssertEqual(0.5m, after.Value?.Summary.EarnedCredits ?? 0, "Transcript summary should total recorded earned credit.");
        AssertTrue(after.Value?.CourseDescriptions.Single().Description.Contains("focused science", StringComparison.OrdinalIgnoreCase) == true, "Course description appendix should be populated.");
    })),
    ("Student can view transcript but cannot edit or export it", (Func<Task>)(async () =>
    {
        var repository = await CreateRepositoryAsync();
        var setup = await CreateCourseWithAssignmentAsync(repository);
        var service = new TranscriptService(repository);

        var view = await service.GetTranscriptAsync(UserContext.Student("Student"), setup.Student.Id);
        AssertTrue(view.Succeeded, "Student should be able to view transcript preview.");

        var save = await service.SaveCourseRecordAsync(UserContext.Student("Student"), new SaveTranscriptCourseRecordCommand(
            setup.Student.Id,
            setup.Course.Id,
            "A",
            0.5m,
            null,
            "Student attempted record.",
            "",
            true));
        AssertFalse(save.Succeeded, "Student must not save transcript course records.");

        var export = await service.CreateTranscriptPacketAsync(UserContext.Student("Student"), new CreateTranscriptExportCommand(setup.Student.Id, TranscriptSpan.HighSchool));
        AssertFalse(export.Succeeded, "Student must not export transcript packets.");
        var pdfExport = await service.CreateTranscriptPdfPacketAsync(UserContext.Student("Student"), new CreateTranscriptExportCommand(setup.Student.Id, TranscriptSpan.HighSchool));
        AssertFalse(pdfExport.Succeeded, "Student must not export transcript PDF packets.");
        AssertEqual(0, (await repository.GetTranscriptCourseRecordsAsync()).Count, "Student transcript mutation should not persist.");
    })),
    ("Transcript span separates middle school and high school courses", (Func<Task>)(async () =>
    {
        var repository = await CreateRepositoryAsync();
        var setup = await CreateCourseWithAssignmentAsync(repository);
        var middleYear = new SchoolYear(
            Guid.NewGuid(),
            setup.Student.Id,
            "2022-2023",
            2022,
            2023,
            [
                new Term(Guid.NewGuid(), "Semester 1", new DateOnly(2022, 8, 22), new DateOnly(2022, 12, 16)),
                new Term(Guid.NewGuid(), "Semester 2", new DateOnly(2023, 1, 9), new DateOnly(2023, 5, 26))
            ]);
        await repository.SaveSchoolYearAsync(middleYear);
        var middleCourse = new Course(
            Guid.NewGuid(),
            setup.Student.Id,
            middleYear.Id,
            "Middle School Earth Science",
            ["Science"],
            CourseDuration.TwoSemesters,
            1.0m,
            null,
            null,
            new CourseDescription("Middle school science description.", "", "", "", "", ""),
            CurriculumPlan.Empty,
            [],
            completionStatus: CompletionStatus.Completed);
        await repository.SaveCourseAsync(middleCourse);

        var service = new TranscriptService(repository);
        var middle = await service.GetTranscriptAsync(UserContext.ParentAdmin("Parent"), setup.Student.Id, TranscriptSpan.MiddleSchool);
        var high = await service.GetTranscriptAsync(UserContext.ParentAdmin("Parent"), setup.Student.Id, TranscriptSpan.HighSchool);

        AssertTrue(middle.Value?.Years.SelectMany(year => year.Courses).Any(course => course.CourseTitle == "Middle School Earth Science") == true, "Middle school span should include estimated grade 8 course.");
        AssertFalse(middle.Value?.Years.SelectMany(year => year.Courses).Any(course => course.CourseTitle == setup.Course.Title) == true, "Middle school span should not include high school course.");
        AssertTrue(high.Value?.Years.SelectMany(year => year.Courses).Any(course => course.CourseTitle == setup.Course.Title) == true, "High school span should include current high school course.");
        AssertFalse(high.Value?.Years.SelectMany(year => year.Courses).Any(course => course.CourseTitle == "Middle School Earth Science") == true, "High school span should not include middle school course.");
    })),
    ("Transcript export includes html manifest and avoids prohibited credential wording", (Func<Task>)(async () =>
    {
        var repository = await CreateRepositoryAsync();
        var setup = await CreateCourseWithAssignmentAsync(repository);
        var service = new TranscriptService(repository);
        var save = await service.SaveCourseRecordAsync(UserContext.ParentAdmin("Parent"), new SaveTranscriptCourseRecordCommand(
            setup.Student.Id,
            setup.Course.Id,
            "Pass",
            0.5m,
            new DateOnly(2027, 5, 28),
            "Parent-recorded completed course.",
            "",
            true));
        AssertTrue(save.Succeeded, "Transcript record setup should save.");

        var export = await service.CreateTranscriptPacketAsync(UserContext.ParentAdmin("Parent"), new CreateTranscriptExportCommand(setup.Student.Id, TranscriptSpan.HighSchool));
        AssertTrue(export.Succeeded, "Parent transcript export should succeed.");
        using var stream = new MemoryStream(export.Value!.Content);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        AssertTrue(archive.GetEntry("transcript.html") is not null, "Transcript archive should include transcript.html.");
        AssertTrue(archive.GetEntry("manifest.json") is not null, "Transcript archive should include manifest.json.");
        AssertTrue(archive.GetEntry("manifest.md") is not null, "Transcript archive should include manifest.md.");

        using var htmlReader = new StreamReader(archive.GetEntry("transcript.html")!.Open());
        var html = await htmlReader.ReadToEndAsync();
        AssertTrue(html.Contains("Family-issued homeschool academic record", StringComparison.OrdinalIgnoreCase), "Transcript should use family-issued wording.");
        AssertTrue(html.Contains("Grade 12", StringComparison.OrdinalIgnoreCase), "Transcript should show the actual grade coverage.");
        AssertTrue(html.Contains("Other transcripts may be available for grades 9-11", StringComparison.OrdinalIgnoreCase), "Transcript should disclose unavailable high school years.");
        AssertFalse(html.Contains("Grades 9-12", StringComparison.OrdinalIgnoreCase), "Transcript should not claim a full high school span when only grade 12 is recorded.");
        AssertFalse(html.Contains("MDE-approved", StringComparison.OrdinalIgnoreCase), "Transcript must not imply MDE approval.");
        AssertFalse(html.Contains("state-approved", StringComparison.OrdinalIgnoreCase), "Transcript must not imply state approval.");
        AssertFalse(html.Contains("accredited", StringComparison.OrdinalIgnoreCase), "Transcript must not imply accreditation.");
    })),
    ("Transcript PDF export creates one family issued packet file", (Func<Task>)(async () =>
    {
        var repository = await CreateRepositoryAsync();
        var setup = await CreateCourseWithAssignmentAsync(repository);
        var service = new TranscriptService(repository);
        var save = await service.SaveCourseRecordAsync(UserContext.ParentAdmin("Parent"), new SaveTranscriptCourseRecordCommand(
            setup.Student.Id,
            setup.Course.Id,
            "A",
            0.5m,
            new DateOnly(2027, 5, 28),
            "Parent-recorded completed course.",
            "PDF packet test note.",
            true));
        AssertTrue(save.Succeeded, "Transcript record setup should save.");

        var export = await service.CreateTranscriptPdfPacketAsync(UserContext.ParentAdmin("Parent"), new CreateTranscriptExportCommand(setup.Student.Id, TranscriptSpan.HighSchool));
        AssertTrue(export.Succeeded, "Parent transcript PDF export should succeed.");
        AssertTrue(export.Value!.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase), "Transcript PDF export should download as a PDF file.");
        AssertEqual("application/pdf", export.Value.ContentType, "Transcript PDF export should use the PDF content type.");

        var pdfText = Encoding.ASCII.GetString(export.Value.Content);
        AssertTrue(pdfText.StartsWith("%PDF-1.4", StringComparison.Ordinal), "Transcript PDF export should be a PDF document.");
        AssertTrue(pdfText.Contains("Family-issued homeschool academic record", StringComparison.OrdinalIgnoreCase), "PDF should use family-issued wording.");
        AssertTrue(pdfText.Contains("Course Descriptions", StringComparison.OrdinalIgnoreCase), "PDF should include course descriptions in the same file.");
        AssertTrue(pdfText.Contains("Source Summary", StringComparison.OrdinalIgnoreCase), "PDF should include source summary context.");
        AssertTrue(pdfText.Contains("Transcript coverage", StringComparison.OrdinalIgnoreCase), "PDF should include transcript coverage wording.");
        AssertTrue(pdfText.Contains("Other transcripts may be available", StringComparison.OrdinalIgnoreCase), "PDF should not imply unavailable grades are included.");
        AssertTrue(pdfText.Contains("grades 9-11", StringComparison.OrdinalIgnoreCase), "PDF should identify the unavailable high school years.");
        AssertTrue(pdfText.Contains(" re", StringComparison.Ordinal), "PDF should draw structured boxes and table cells.");
        AssertFalse(pdfText.Contains("Course | Subject | Term", StringComparison.OrdinalIgnoreCase), "PDF should not use the old pipe-delimited text layout.");
        AssertFalse(pdfText.Contains("MDE-approved", StringComparison.OrdinalIgnoreCase), "PDF must not imply MDE approval.");
        AssertFalse(pdfText.Contains("state-approved", StringComparison.OrdinalIgnoreCase), "PDF must not imply state approval.");
        AssertFalse(pdfText.Contains("accredited", StringComparison.OrdinalIgnoreCase), "PDF must not imply accreditation.");
    }))
};

var failures = new List<string>();
foreach (var test in tests)
{
    try
    {
        await test.Test();
        Console.WriteLine($"PASS {test.Name}");
    }
    catch (Exception ex)
    {
        failures.Add($"{test.Name}: {ex.Message}");
        Console.WriteLine($"FAIL {test.Name}: {ex.Message}");
    }
}

if (failures.Count > 0)
{
    Console.WriteLine();
    Console.WriteLine("Failures:");
    foreach (var failure in failures)
    {
        Console.WriteLine(failure);
    }

    return 1;
}

return 0;

static async Task<JsonHomeschoolRepository> CreateRepositoryAsync()
{
    var root = Path.Combine(Path.GetTempPath(), "HomeschoolManagerTests", Guid.NewGuid().ToString("N"));
    var options = Options.Create(new HomeschoolManagerOptions
    {
        DataRoot = root,
        UseDevelopmentDataRoot = true
    });
    var repository = new JsonHomeschoolRepository(new AppDataPaths(options));
    await repository.EnsureStoreCreatedAsync();
    return repository;
}

static async Task<(JsonHomeschoolRepository Repository, AppDataPaths Paths)> CreateRepositoryWithPathsAsync()
{
    var root = Path.Combine(Path.GetTempPath(), "HomeschoolManagerTests", Guid.NewGuid().ToString("N"));
    var options = Options.Create(new HomeschoolManagerOptions
    {
        DataRoot = root,
        UseDevelopmentDataRoot = true
    });
    var paths = new AppDataPaths(options);
    var repository = new JsonHomeschoolRepository(paths);
    await repository.EnsureStoreCreatedAsync();
    return (repository, paths);
}

static async Task CreateSetupAsync(JsonHomeschoolRepository repository)
{
    var setupService = new SetupService(repository);
    var parent = UserContext.ParentAdmin("Parent");
    AssertTrue((await setupService.CreateHouseholdAsync(parent, new CreateHouseholdCommand("Family", "Parent"))).Succeeded, "Household setup failed.");
    AssertTrue((await setupService.CreateStudentAsync(parent, new CreateStudentCommand("Student", "Learner", 12))).Succeeded, "Student setup failed.");
    AssertTrue((await setupService.ConfigureSchoolYearAsync(parent, new ConfigureSchoolYearCommand(
        "2026-2027",
        2026,
        2027,
        new DateOnly(2026, 8, 24),
        new DateOnly(2026, 12, 18),
        new DateOnly(2027, 1, 11),
            new DateOnly(2027, 5, 28)))).Succeeded, "School year setup failed.");
}

static async Task<(Student Student, SchoolYear SchoolYear, Course Course, LearningModule Module, ModuleAssignment Assignment)> CreateCourseWithAssignmentAsync(JsonHomeschoolRepository repository)
{
    await CreateSetupAsync(repository);
    var student = await repository.GetStudentAsync() ?? throw new InvalidOperationException("Student setup failed.");
    var schoolYear = await repository.GetSchoolYearAsync() ?? throw new InvalidOperationException("School year setup failed.");
    var courseId = Guid.NewGuid();
    var moduleId = Guid.NewGuid();
    var assignment = new ModuleAssignment(
        Guid.NewGuid(),
        moduleId,
        "assessment-source",
        1,
        "Evidence analysis",
        AssignmentType.Project,
        InstructionalMethodProfile.Hybrid,
        "Complete the analysis and submit the result.",
        "One lesson",
        "After lesson work",
        null,
        ["Explain evidence clearly."],
        [],
        "Written analysis",
        "",
        false,
        10,
        20,
        AssignmentStatus.Assigned);
    var module = new LearningModule(
        moduleId,
        courseId,
        "assessment-module",
        1,
        "Assessment Module",
        "Module for assessment tests.",
        "One week",
        "Work through the module.",
        "Evidence",
        "Explain evidence clearly.",
        "",
        "Retain submitted analysis.",
        ModuleStatus.Active,
        assignments: [assignment]);
    var course = new Course(
        courseId,
        student.Id,
        schoolYear.Id,
        "Assessment Course",
        ["Science"],
        CourseDuration.OneSemester,
        0.5m,
        null,
        null,
        CourseDescription.Empty,
        CurriculumPlan.Empty,
        [],
        [module]);

    await repository.SaveCourseAsync(course);
    return (student, schoolYear, course, module, assignment);
}

static string FindRepositoryRoot()
{
    var directory = new DirectoryInfo(AppContext.BaseDirectory);
    while (directory is not null)
    {
        if (File.Exists(Path.Combine(directory.FullName, "docs", "README.md")) &&
            Directory.Exists(Path.Combine(directory.FullName, "src", "HomeschoolManager.Web")))
        {
            return directory.FullName;
        }

        directory = directory.Parent;
    }

    throw new InvalidOperationException("Repository root was not found.");
}

static void AssertTrue(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static void AssertFalse(bool condition, string message)
{
    AssertTrue(!condition, message);
}

static void AssertEqual<T>(T expected, T actual, string message)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"{message} Expected {expected}, got {actual}.");
    }
}

static void AssertBundleEntryNamesAreShort(ZipArchive archive)
{
    foreach (var entry in archive.Entries)
    {
        AssertTrue(entry.FullName.Length <= 140, $"Bundle entry path should stay short for Windows extraction: {entry.FullName}");
        foreach (var segment in entry.FullName.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            AssertTrue(segment.Length <= 36, $"Bundle path segment should stay short for Windows extraction: {entry.FullName}");
        }
    }
}

static void AssertThrows<TException>(Action action)
    where TException : Exception
{
    try
    {
        action();
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException($"Expected {typeof(TException).Name}.");
}

static Guid DeterministicGuid(string value)
{
    var bytes = System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(value));
    return new Guid(bytes);
}

static IReadOnlyList<ModuleLearningObjectiveCommand> Objectives(params string[] objectives)
{
    return objectives.Select(objective => new ModuleLearningObjectiveCommand(objective, "")).ToArray();
}

static IReadOnlyList<ModuleResourceCommand> Resources(params string[] resources)
{
    return resources.Select(resource => new ModuleResourceCommand(resource, "", "", true)).ToArray();
}

static IReadOnlyList<LessonResourceCommand> LessonResources(params string[] resources)
{
    return resources
        .Select(resource => new LessonResourceCommand(
            resource,
            LessonResourceType.Reading,
            "https://example.com/resource",
            "",
            false,
            "Test resource."))
        .ToArray();
}
