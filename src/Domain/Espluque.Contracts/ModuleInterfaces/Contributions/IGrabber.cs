namespace Espluque.Contracts.ModuleInterfaces.Contributions
{
    public interface IGrabber
    {
        Task<List<KeyValuePair<string, string>>> Grab(string filePath);
    }
}
