using Espluque.Contracts.Enums;

namespace Espluque.Contracts.Interfaces
{
    public interface IFact
    {
        string? Evidence { get; set; }
        string Key { get; set; }
        string Source { get; set; }
        FactStatusEnum? Status { get; set; }
        string? Value { get; set; }
    }
}