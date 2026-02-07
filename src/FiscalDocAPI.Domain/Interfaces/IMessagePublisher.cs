namespace FiscalDocAPI.Domain.Interfaces;

public interface IMessagePublisher
{
    Task PublishAsync<T>(T message, string routingKey);
}
