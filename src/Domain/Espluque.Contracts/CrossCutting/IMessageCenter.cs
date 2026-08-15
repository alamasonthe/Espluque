namespace Espluque.Contracts.CrossCutting

{
    public interface IMessageCenter
    {
        void Register(IMessageClient client);
        Task SendAsync(IMessage message);
        void Unregister(IMessageClient client);
    }
}