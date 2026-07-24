using Microsoft.Extensions.Logging;

namespace Espluque.Contracts.Ports
{
    public interface ILogger
    {
        event Action<string>? LineLogged;

        void Log(LogLevel logLevel, string message);
    }
}