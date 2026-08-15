using Espluque.Contracts.MessageInterfaces;

namespace Espluque.Application.CrossCutting.MessageBus
{
    public class MessageCenter : IMessageCenter
    {
        private readonly List<IMessageClient> _clients = [];

        public void Register(IMessageClient client)
        {
            if (!_clients.Contains(client))
            {
                _clients.Add(client);
            }
        }

        public void Unregister(IMessageClient client)
        {
            _clients.Remove(client);
        }

        public async Task SendAsync(IMessage message)
        {
            List<IMessageClient> clients = _clients.ToList();

            foreach (IMessageClient client in clients)
            {
                try
                {
                    await client.HandleAsync(message);
                }
                catch
                {
                }
            }
        }
    }
}