using HomeschoolManager.Application.Persistence;
using HomeschoolManager.Application.Assessments;
using HomeschoolManager.Application.Courses;
using HomeschoolManager.Application.Portfolio;
using HomeschoolManager.Application.Requirements;
using HomeschoolManager.Application.Setup;
using HomeschoolManager.Application.Submissions;
using HomeschoolManager.Infrastructure.Access;
using HomeschoolManager.Infrastructure.Configuration;
using HomeschoolManager.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HomeschoolManager.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddHomeschoolManagerInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<HomeschoolManagerOptions>(configuration.GetSection("HomeschoolManager"));
        services.AddSingleton<AppDataPaths>();
        services.AddSingleton<IHomeschoolRepository, JsonHomeschoolRepository>();
        services.AddSingleton<ISubmissionFileStore, LocalSubmissionFileStore>();
        services.AddScoped<CourseService>();
        services.AddScoped<StudentCourseService>();
        services.AddScoped<GradebookService>();
        services.AddScoped<AssignmentSubmissionService>();
        services.AddScoped<SubmissionFilePreviewService>();
        services.AddScoped<PortfolioService>();
        services.AddScoped<SetupService>();
        services.AddScoped<RequirementService>();
        services.AddScoped<LocalAccessService>();
        return services;
    }
}
