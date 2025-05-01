namespace Aggregator.Services;

public interface IPaymentService
{
    Task<string> GetPaymentByOrderIdAsync(string orderId);
} 