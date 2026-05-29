namespace Vesia.Dispatch.Interfaces;

public interface INotificationHandler<in TNotification>
{
    Task Handle(TNotification notification, CancellationToken cancellationToken = default);
}