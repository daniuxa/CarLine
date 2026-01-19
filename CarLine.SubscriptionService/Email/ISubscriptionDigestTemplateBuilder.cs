using CarLine.SubscriptionService.Data.Entities;
using CarLine.SubscriptionService.Models;

namespace CarLine.SubscriptionService.Email;

public interface ISubscriptionDigestTemplateBuilder
{
    IReadOnlyList<string> BuildBodyLines(SubscriptionEntity subscription, IReadOnlyList<MatchedCarDto> cars);
}