using CarLine.SubscriptionService.Data.Entities;
using CarLine.SubscriptionService.Models;

namespace CarLine.SubscriptionService.Email;

public interface IEmailSender
{
    Task SendNewCarsDigestAsync(
        string toEmail,
        SubscriptionEntity subscription,
        IReadOnlyList<MatchedCarDto> cars,
        CancellationToken cancellationToken);
}