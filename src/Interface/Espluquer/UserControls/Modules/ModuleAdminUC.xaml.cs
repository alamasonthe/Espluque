using Espluque.Contracts.Enums;
using Espluque.Contracts.Interfaces;
using Espluque.Contracts.ModuleInterfaces;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Espluquer.Entities;
using Espluquer.Adapters;

namespace Espluquer.UserControls.Modules
{
    public partial class ModuleAdminUC : UserControl
    {
        // public List<string> _moduleFilePaths = [];

        public ObservableCollection<IModuleInfo> _modules = [];
        public List<ModuleHealthDto> _moduleHealths { get; set; } = [];
        public List<ContributionHealthDto> _contribHealths { get; set; } = [];

        private readonly IModuleService _moduleService;
        private readonly IModuleDiagService _moduleDiagService;
        private readonly IEntityFactory _entityFactory;

        private readonly ModuleTestDetailUC _moduleTestDetailUC;

        public ModuleAdminUC(IModuleService moduleService, IModuleDiagService moduleDiagService, IEntityFactory entityFactory)
        {
            _moduleService = moduleService;
            _moduleDiagService = moduleDiagService;
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

        private List<ContributionHealthDto> CreateContribHealths(List<IModuleInfo> modules)
        {
            List<ContributionHealthDto> contribHealths = [];

            foreach (IModuleInfo module in modules)
            {
                foreach (IModuleContributionInfo contrib in module.Contributions)
                {
                    ContributionHealthDto contribHealth = new()
                    {
                        ModuleName = module.Name,
                        ContribInterfaceType = contrib.InterfaceType,
                        ContribClassName = contrib.ClassName,
                        HealthCheck = ModuleHealthCheckEnum.NotTested,
                        Diag = string.Empty
                    };

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

        private async Task DiagnoseModulesOld()
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

        private async Task DiagnoseModules()
        {
            foreach (IModuleInfo module in _modules)
            {
                (IModuleHealth moduleHealth, List<IContributionHealth> contributionHealths)
                    = await _moduleDiagService.DiagAsync(module.FilePath);

                ModuleHealthDto diagnosedModuleHealth =
                    ModuleHealthAdapter.FromDomain(moduleHealth);

                ModuleHealthDto? currentModuleHealth =
                    _moduleHealths.FirstOrDefault(
                        health => health.ModuleName == diagnosedModuleHealth.ModuleName);

                if (currentModuleHealth is null)
                {
                    _moduleHealths.Add(diagnosedModuleHealth);
                }
                else
                {
                    currentModuleHealth.HealthCheck = diagnosedModuleHealth.HealthCheck;
                    currentModuleHealth.Diag = diagnosedModuleHealth.Diag;
                }

                foreach (IContributionHealth contributionHealth in contributionHealths)
                {
                    ContributionHealthDto diagnosedContribHealth =
                        ContributionHealthAdapter.FromDomain(contributionHealth);

                    ContributionHealthDto? currentContribHealth =
                        _contribHealths.FirstOrDefault(
                            health =>
                                health.ModuleName == diagnosedContribHealth.ModuleName
                                && health.ContribInterfaceType == diagnosedContribHealth.ContribInterfaceType
                                && health.ContribClassName == diagnosedContribHealth.ContribClassName);

                    if (currentContribHealth is null)
                    {
                        _contribHealths.Add(diagnosedContribHealth);
                    }
                    else
                    {
                        currentContribHealth.HealthCheck = diagnosedContribHealth.HealthCheck;
                        currentContribHealth.Diag = diagnosedContribHealth.Diag;
                    }
                }
            }
        }

        private async void ModuleDiagnosticUC_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= ModuleDiagnosticUC_Loaded;

            _modules = new(await CreateModuleList());
            _moduleHealths = CreateModuleHealths(_modules.ToList());
            _contribHealths = CreateContribHealths(_modules.ToList());

            ModuleListBox.ItemsSource = _modules;

            if (_modules.Count > 0)
            {
                ModuleListBox.SelectedIndex = 0;
            }

            await DiagnoseModules();
            if (ModuleListBox.SelectedIndex < 0 && _modules.Count > 0) ModuleListBox.SelectedIndex = 0;

        }

        private void ModuleListBox_SelectionChanged( object sender, SelectionChangedEventArgs e)
        {
            _moduleTestDetailUC.ModuleInfo = ModuleListBox.SelectedItem as IModuleInfo;
            _moduleTestDetailUC.ContributionHealths = _contribHealths;
        }

        private void ModuleTestLineUC_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ModuleTestLineUC moduleLine || moduleLine.DataContext is not IModuleInfo moduleInfo)
            {
                return;
            }
            moduleLine.ModuleHealth = _moduleHealths.First( health => health.ModuleName == moduleInfo.Name);
        }

        private List<ModuleHealthDto> CreateModuleHealths(List<IModuleInfo> modules)
        {
            List<ModuleHealthDto> moduleHealths = [];

            foreach (IModuleInfo module in modules)
            {
                moduleHealths.Add(new ModuleHealthDto
                {
                    ModuleName = module.Name,
                    HealthCheck = ModuleHealthCheckEnum.NotTested,
                    Diag = string.Empty
                });
            }

            return moduleHealths;
        }
    }
}
