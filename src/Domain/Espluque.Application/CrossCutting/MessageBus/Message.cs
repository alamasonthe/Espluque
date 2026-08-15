using Espluque.Contracts.Enums;
using Espluque.Contracts.MessageInterfaces;

namespace Espluque.Application.CrossCutting.MessageBus
{
    public class Message : IMessage
    {
        public MessageTypeEnum MessageType { get; set; }
        public string MessageLabel { get; set; }
        public List<KeyValuePair<string, string>> Payload { get; set; } = [];
    }
}
