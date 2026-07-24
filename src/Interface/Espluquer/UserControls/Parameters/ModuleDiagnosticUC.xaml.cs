using Espluque.Contracts.Interfaces;
using Espluque.Contracts.ModuleInterfaces;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Espluquer.UserControls.Components
{
    public partial class ModuleDiagnosticUC : UserControl
    {
        public List<string> _moduleFilePaths = [];

        public ObservableCollection<IModuleDiagnostic> _moduleDiagList = [];

        private readonly IModuleService _moduleService;
        private readonly IModuleDiagnosticService _moduleDiagnosticService;
        private readonly IEntityFactory _entityFactory;

        private readonly ModuleTestDetailUC _moduleTestDetailUC;

        public ModuleDiagnosticUC(IModuleService moduleService, IModuleDiagnosticService moduleDiagnosticService, IEntityFactory entityFactory)
        {
            _moduleService = moduleService;
            _moduleDiagnosticService = moduleDiagnosticService;
            _entityFactory = entityFactory;

            InitializeComponent();

            _moduleTestDetailUC = new ModuleTestDetailUC();

            ModuleDetailsHost.Content = _moduleTestDetailUC;

            Loaded += ModuleDiagnosticUC_Loaded;
            ModuleListBox.SelectionChanged += ModuleListBox_SelectionChanged;
        }

        private async Task CreateModuleList()
        {
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
        }

        private async Task DiagnoseModules()
        {
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
        }

        private async void ModuleDiagnosticUC_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= ModuleDiagnosticUC_Loaded;

            await CreateModuleList();
            await DiagnoseModules();
            if (ModuleListBox.SelectedIndex < 0 && _moduleDiagList.Count > 0) ModuleListBox.SelectedIndex = 0;
        }

        private void ModuleListBox_SelectionChanged( object sender, SelectionChangedEventArgs e)
        {
            _moduleTestDetailUC.ModuleDiagnostic =
                ModuleListBox.SelectedItem as IModuleDiagnostic;
        }

    }
}
