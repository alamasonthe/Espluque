namespace Espluque.Contracts.ModuleInterfaces.Contributions
{
    public interface IWpfViewer
    {
        Task<object?> GetViewer(string filePath);
    }
}
