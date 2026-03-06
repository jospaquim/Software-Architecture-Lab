using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text;

namespace DDD.Sales.Infrastructure.ExternalServices;

/// <summary>
/// Payment Gateway Client with Polly resilience
/// Example: Integration with external payment providers (Stripe, PayPal, etc.)
/// </summary>
public interface IPaymentGatewayClient
{
    Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request, CancellationToken cancellationToken = default);
    Task<RefundResult> RefundPaymentAsync(string transactionId, decimal amount, CancellationToken cancellationToken = default);
}

public class PaymentGatewayClient : IPaymentGatewayClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PaymentGatewayClient> _logger;

    public PaymentGatewayClient(HttpClient httpClient, ILogger<PaymentGatewayClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("[DDD-Sales] Processing payment for order {OrderId}, amount: {Amount}",
                request.OrderId, request.Amount);

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Polly policies are automatically applied
            var response = await _httpClient.PostAsync("/api/payments", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var resultJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<PaymentResult>(resultJson)
                ?? throw new InvalidOperationException("Failed to deserialize payment result");

            _logger.LogInformation("[DDD-Sales] Payment processed. TransactionId: {TransactionId}, Status: {Status}",
                result.TransactionId, result.Status);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DDD-Sales] Payment processing failed for order {OrderId}", request.OrderId);
            throw;
        }
    }

    public async Task<RefundResult> RefundPaymentAsync(string transactionId, decimal amount, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("[DDD-Sales] Refunding payment {TransactionId}, amount: {Amount}",
                transactionId, amount);

            var request = new { transactionId, amount };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/refunds", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var resultJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<RefundResult>(resultJson)
                ?? throw new InvalidOperationException("Failed to deserialize refund result");

            _logger.LogInformation("[DDD-Sales] Refund processed. RefundId: {RefundId}", result.RefundId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DDD-Sales] Refund failed for transaction {TransactionId}", transactionId);
            throw;
        }
    }
}

// DTOs
public record PaymentRequest(
    string OrderId,
    decimal Amount,
    string Currency,
    string CardNumber,
    string CardHolderName);

public record PaymentResult(
    string TransactionId,
    string Status,
    string Message);

public record RefundResult(
    string RefundId,
    string TransactionId,
    decimal Amount,
    string Status);
