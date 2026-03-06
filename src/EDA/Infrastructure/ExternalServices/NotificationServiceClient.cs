using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text;

namespace EDA.Infrastructure.ExternalServices;

/// <summary>
/// Notification Service Client with Polly resilience
/// Example: Integration with notification providers (SendGrid, Twilio, Firebase, etc.)
/// </summary>
public interface INotificationServiceClient
{
    Task SendEmailAsync(EmailNotification notification, CancellationToken cancellationToken = default);
    Task SendSmsAsync(SmsNotification notification, CancellationToken cancellationToken = default);
}

public class NotificationServiceClient : INotificationServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NotificationServiceClient> _logger;

    public NotificationServiceClient(HttpClient httpClient, ILogger<NotificationServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task SendEmailAsync(EmailNotification notification, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("[EDA] Sending email to {Recipient}: {Subject}",
                notification.To, notification.Subject);

            var json = JsonSerializer.Serialize(notification);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Polly policies are automatically applied
            var response = await _httpClient.PostAsync("/api/emails", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            _logger.LogInformation("[EDA] Email sent successfully to {Recipient}", notification.To);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EDA] Failed to send email to {Recipient}", notification.To);
            throw;
        }
    }

    public async Task SendSmsAsync(SmsNotification notification, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("[EDA] Sending SMS to {PhoneNumber}", notification.PhoneNumber);

            var json = JsonSerializer.Serialize(notification);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/sms", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            _logger.LogInformation("[EDA] SMS sent successfully to {PhoneNumber}", notification.PhoneNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EDA] Failed to send SMS to {PhoneNumber}", notification.PhoneNumber);
            throw;
        }
    }
}

// DTOs
public record EmailNotification(
    string To,
    string Subject,
    string Body,
    string? From = null);

public record SmsNotification(
    string PhoneNumber,
    string Message);
