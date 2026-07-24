using Espluque.Contracts.Enums;

namespace Espluque.Contracts.MessageInterfaces
{
    public interface IMessage
    {
        string MessageLabel { get; set; }
        MessageTypeEnum MessageType { get; set; }
        List<KeyValuePair<string, string>> Payload { get; set; }
    }
}