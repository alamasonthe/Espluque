using Microsoft.Extensions.Logging;

namespace Espluque.Contracts.CrossCutting
{
    public interface ILogger
    {
        event Action<string>? LineLogged;

        void Log(LogLevel logLevel, string message);
    }
}