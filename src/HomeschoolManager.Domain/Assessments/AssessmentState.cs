namespace HomeschoolManager.Domain.Assessments;

public enum AssessmentState
{
    NotAssessed = 0,
    NeedsReview = 1,
    Assessed = 2,
    ReturnedForRevision = 3,
    Excused = 4,
    Incomplete = 5,
    NotApplicable = 6
}
