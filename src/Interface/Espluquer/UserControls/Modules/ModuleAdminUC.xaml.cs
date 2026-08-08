using Espluque.Contracts.Enums;
using Espluque.Contracts.Interfaces;
using Espluque.Contracts.ModuleInterfaces;
using System.Collections.ObjectModel;
using System.DirectoryServices;
using System.Windows;
using System.Windows.Controls;

namespace Espluquer.UserControls.Modules
{
    public partial class ModuleAdminUC : UserControl
    {
        // public List<string> _moduleFilePaths = [];

        public ObservableCollection<IModuleInfo> _modules = [];
        public List<IContributionHealth> _contribHealths { get; set; } = [];

        private readonly IModuleService _moduleService;
        // private readonly IModuleDiagnosticService _moduleDiagnosticService;
        private readonly IEntityFactory _entityFactory;

        private readonly ModuleTestDetailUC _moduleTestDetailUC;

        public ModuleAdminUC(IModuleService moduleService, IModuleDiagnosticService moduleDiagnosticService, IEntityFactory entityFactory)
        {
            _moduleService = moduleService;
            // _moduleDiagnosticService = moduleDiagnosticService;
            _entityFactory = entityFactory;

            InitializeComponent();

            _moduleTestDetailUC = new ModuleTestDetailUC();

            ModuleDetailsHost.Content = _moduleTestDetailUC;

            Loaded += ModuleDiagnosticUC_Loaded;
            ModuleListBox.SelectionChanged += ModuleListBox_SelectionChanged;
        }

        private async Task<List<IModuleInfo>> CreateModuleList()
        {
            string modulesRootPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Modules");
            List<string> moduleFilePaths = _moduleService.GetModuleInfoPaths(modulesRootPath);

            List<IModuleInfo> modules = [];
            foreach (var path in moduleFilePaths)
            {
                string moduleFolderName = System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(path));
                var moduleInfo = await _moduleService.LoadModuleInfo(path);
                if (moduleInfo is not null)
                {
                    modules.Add(moduleInfo);
                }
            }

            return modules;
        }

        private List<IContributionHealth> CreateContribHealths(List<IModuleInfo> modules)
        {
            List<IContributionHealth> contribHealths  = [];

            foreach (var module in modules)
            {
                foreach (var contrib in module.Contributions)
                {

                    var contribHealth = _entityFactory.CreateContributionHealth(
                    module.Name,
                    contrib.InterfaceType,
                    contrib.ClassName,
                    ModuleHealthCheckEnum.NotTested,
                    string.Empty
                    );
                    contribHealths.Add(contribHealth);
                }
            }

            return contribHealths;
        }

        private async Task CreateModuleListOld()
        {
            /*
            string modulesRootPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Modules");
            _moduleFilePaths = _moduleService.GetModuleInfoPaths(modulesRootPath);

            _moduleDiagList = [];
            foreach (var path in _moduleFilePaths)
            {
                string moduleFolderName = System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(path));
                var moduleDiag = _entityFactory.CreateModuleDiagnostic(path, moduleFolderName);
                _moduleDiagList.Add(moduleDiag);
            }

            ModuleListBox.ItemsSource = _moduleDiagList;
            if (_moduleDiagList.Count > 0)
            {
                ModuleListBox.SelectedIndex = 0;
            }
            */
        }

        private async Task DiagnoseModules()
        {
            /*
            for (int index = 0; index < _moduleDiagList.Count; index++)
            {
                IModuleDiagnostic diagnosedModule = await Task.Run(
                    () => _moduleDiagnosticService.DiagnoseAsync(_moduleFilePaths[index]));

                _moduleDiagList[index] = diagnosedModule;

                if (ModuleListBox.SelectedIndex == index)
                {
                    _moduleTestDetailUC.ModuleDiagnostic = diagnosedModule;
                }

                await Dispatcher.Yield(DispatcherPriority.Background);
            }
            */
        }

        private async void ModuleDiagnosticUC_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= ModuleDiagnosticUC_Loaded;

            _modules = new(await CreateModuleList());
            _contribHealths = CreateContribHealths(_modules.ToList());

            ModuleListBox.ItemsSource = _modules;

            if (_modules.Count > 0)
            {
                ModuleListBox.SelectedIndex = 0;
            }

            // await DiagnoseModules();
            // if (ModuleListBox.SelectedIndex < 0 && _moduleDiagList.Count > 0) ModuleListBox.SelectedIndex = 0;

        }

        private void ModuleListBox_SelectionChanged( object sender, SelectionChangedEventArgs e)
        {
            _moduleTestDetailUC.ModuleInfo = ModuleListBox.SelectedItem as IModuleInfo;
            _moduleTestDetailUC.ContributionHealths = _contribHealths;
        }

    }
}
