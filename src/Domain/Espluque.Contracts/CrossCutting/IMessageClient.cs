namespace Espluque.Contracts.CrossCutting
{
    public interface IMessageClient
    {
        Task SendAsync(IMessage message);

        Task HandleAsync(IMessage message);
    }
}
