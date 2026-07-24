namespace Espluque.Contracts.ModuleInterfaces
{
    public interface IModuleDiagnosticService
    {
        Task<IModuleDiagnostic> DiagnoseAsync(string moduleInfoPath);
    }
}
