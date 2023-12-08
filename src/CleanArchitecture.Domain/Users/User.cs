using CleanArchitecture.Domain.Common;
using CleanArchitecture.Domain.Reminders;
using CleanArchitecture.Domain.Subscriptions;
using CleanArchitecture.Domain.Users.Events;

using ErrorOr;

namespace CleanArchitecture.Domain.Users;

public class User : Entity
{
    private readonly Calendar _calendar = null!;
    private readonly List<Guid> _reminderIds = [];

    public Subscription Subscription { get; private set; } = null!;

    public User(
        Guid id,
        Subscription subscription,
        Calendar? calendar = null)
            : base(id)
    {
        Subscription = subscription;
        _calendar = calendar ?? Calendar.Empty();
    }

    public ErrorOr<Success> SetReminder(Reminder reminder)
    {
        if (Subscription == Subscription.Canceled)
        {
            return Error.NotFound("Subscription not found");
        }

        if (HasReachedDailyReminderLimit(reminder.DateTime))
        {
            return UserErrors.CannotCreateMoreRemindersThanSubscriptionAllows;
        }

        _calendar.IncrementEventCount(reminder.Date);

        _reminderIds.Add(reminder.Id);

        _domainEvents.Add(new ReminderSetEvent(reminder));

        return Result.Success;
    }

    public ErrorOr<Success> DismissReminder(Guid reminderId)
    {
        if (Subscription == Subscription.Canceled)
        {
            return Error.NotFound("Subscription not found");
        }

        if (!_reminderIds.Remove(reminderId))
        {
            return Error.NotFound(description: "Reminder not found");
        }

        _domainEvents.Add(new ReminderDismissedEvent(reminderId));

        return Result.Success;
    }

    public ErrorOr<Success> CancelSubscription(Guid subscriptionId)
    {
        if (subscriptionId != Subscription.Id)
        {
            return Error.NotFound("Subscription not found");
        }

        Subscription = Subscription.Canceled;

        _domainEvents.Add(new SubscriptionDeletedEvent(this, subscriptionId));

        return Result.Success;
    }

    public ErrorOr<Success> DeleteReminder(Reminder reminder)
    {
        if (!_reminderIds.Remove(reminder.Id))
        {
            return Error.NotFound("Reminder not found");
        }

        _calendar.DecrementEventCount(reminder.Date);

        _domainEvents.Add(new ReminderDeletedEvent(reminder.Id));

        return Result.Success;
    }

    public void DeleteAllReminders()
    {
        _reminderIds.ForEach(reminderId => _domainEvents.Add(new ReminderDeletedEvent(reminderId)));

        _reminderIds.Clear();
    }

    private bool HasReachedDailyReminderLimit(DateTimeOffset dateTime)
    {
        var dailyReminderCount = _calendar.GetNumEventsOnDay(dateTime.Date);

        return dailyReminderCount >= Subscription.SubscriptionType.GetMaxDailyReminders()
            || dailyReminderCount == int.MaxValue;
    }

    private User()
    {
    }
}