using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Infrastructure.BackgroundJobs;

public class SampleJobs
{
    private readonly ILogger<SampleJobs> _logger;

    public SampleJobs(ILogger<SampleJobs> logger)
    {
        _logger = logger;
    }

    public void SendWelcomeEmail(string email)
    {
        _logger.LogInformation("Sending welcome email to {Email}", email);
        // Email sending logic
    }

    public void GenerateMonthlyReport()
    {
        _logger.LogInformation("Generating monthly report");
        // Report generation logic
    }

    public void CleanupOldData()
    {
        _logger.LogInformation("Cleaning up old data");
        // Cleanup logic
    }
}
