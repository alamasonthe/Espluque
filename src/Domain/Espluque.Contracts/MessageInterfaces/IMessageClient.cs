namespace Espluque.Contracts.MessageInterfaces
{
    public interface IMessageClient
    {
        Task SendAsync(IMessage message);

        Task HandleAsync(IMessage message);
    }
}
