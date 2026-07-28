using Espluque.Application.ModuleManager.Entities;
using Espluque.Contracts.Entities;
using Espluque.Contracts.ModuleInterfaces;
using Espluque.Contracts.ModuleInterfaces.Contributions;
using Espluque.Contracts.Ports;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Runtime.Loader;
using System.Windows;
using System.Windows.Controls;

namespace Espluquer.UserControls.Parameters
{
    public partial class ModuleToolsUC : UserControl
    {
        private readonly ILogger _logger;
        private readonly IModuleAdministrationService _moduleAdministrationService;

        private readonly List<ICatalogEntry> _catalogEntries;
        private readonly string _contributionType;

        public ModuleToolsUC()
        {
            InitializeComponent();
        }

        public ModuleToolsUC(ILogger logger, IModuleAdministrationService moduleAdministrationService, List<ICatalogEntry> catalog, string contributionType)
        {
            InitializeComponent();

            _logger = logger;
            _moduleAdministrationService = moduleAdministrationService;
            _contributionType = contributionType;
            _catalogEntries = catalog.Where(x => x.InterfaceType == contributionType).ToList();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            await ShowToolsTab();
        }

        private async Task ShowToolsTabOld()
        {
            foreach (var catalogEntry in _catalogEntries)
            {
                try
                {
                    Type toolType;

                    switch (_contributionType)
                    {
                        case "IWpfSettings":
                            toolType = typeof(IWpfSettings);
                            break;
                        case "IWpfMaintenance":
                            toolType = typeof(IWpfMaintenance);
                            break;
                        default:
                            _logger.Log(Microsoft.Extensions.Logging.LogLevel.Warning, $"Unknown contribution type: {_contributionType}");
                            continue;
                    }

                    AssemblyLoadContext? loadContext = AssemblyLoadContext.GetLoadContext(toolType.Assembly);

                    (string label, object instance) ? moduleInstance =await _moduleAdministrationService.CreateAdminInstance(catalogEntry); 

                    if (moduleInstance?.instance is not IWpfMaintenance && moduleInstance?.instance is not IWpfSettings) return;

                    object? content;
                    switch (_contributionType)
                    {
                        case "IWpfSettings":
                            if (loadContext is null)
                            {
                                content = await ((IWpfSettings)moduleInstance.Value.instance).GetSettingsUC();
                            }
                            else
                            {
                                using (loadContext.EnterContextualReflection())
                                {
                                    content = await ((IWpfSettings)moduleInstance.Value.instance).GetSettingsUC();
                                }
                            }
                            break;
                        case "IWpfMaintenance":
                            if (loadContext is null)
                            {
                                content = await ((IWpfMaintenance)moduleInstance.Value.instance).GetWpfMaintenance();
                            }
                            else
                            {
                                using (loadContext.EnterContextualReflection())
                                {
                                    content = await ((IWpfMaintenance)moduleInstance.Value.instance).GetWpfMaintenance();
                                }
                            }
                            break;
                        default:
                            _logger.Log(Microsoft.Extensions.Logging.LogLevel.Warning, $"Cannot intanciate: {_contributionType}");
                            continue;
                    }

                    if (content is UserControl userControl)
                    {
                        TabItem tabItem = new TabItem
                        {
                            Header = moduleInstance.Value.label,
                            Content = userControl
                        };

                        ToolsTabControl.Items.Add(tabItem);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Log(Microsoft.Extensions.Logging.LogLevel.Error, $"Error loading tool {catalogEntry.Label}: {ex.ToString().ReplaceLineEndings(" | ")}");
                }
            }
        }

        private sealed record ToolDefinition( Type InterfaceType, Func<object, Task<object?>> GetContent);

        private ToolDefinition? GetToolDefinition()
        {
            return _contributionType switch
            {
                nameof(IWpfSettings) => new ToolDefinition(
                    typeof(IWpfSettings),
                    instance => ((IWpfSettings)instance).GetSettingsUC()),

                nameof(IWpfMaintenance) => new ToolDefinition(
                    typeof(IWpfMaintenance),
                    instance => ((IWpfMaintenance)instance).GetWpfMaintenance()),

                _ => null
            };
        }

        private static async Task<object?> GetContent(
            ToolDefinition definition,
            object instance,
            AssemblyLoadContext? loadContext)
        {
            if (loadContext is null)
                return await definition.GetContent(instance);

            using (loadContext.EnterContextualReflection())
                return await definition.GetContent(instance);
        }

        private async Task ShowToolsTab()
        {
            NoToolsTextBlock.Text = _contributionType == nameof(IWpfSettings)
                ? "No module settings found"
                : "No module maintenance found";

            NoToolsTextBlock.Visibility = Visibility.Visible;

            ToolDefinition? definition = GetToolDefinition();

            if (definition is null)
            {
                _logger.Log(
                    Microsoft.Extensions.Logging.LogLevel.Warning,
                    $"Unknown contribution type: {_contributionType}");

                return;
            }

            AssemblyLoadContext? loadContext =
                AssemblyLoadContext.GetLoadContext(definition.InterfaceType.Assembly);

            foreach (var catalogEntry in _catalogEntries)
            {
                try
                {
                    (string label, object instance)? moduleInstance =
                        await _moduleAdministrationService.CreateAdminInstance(catalogEntry);

                    if (moduleInstance is null)
                        continue;

                    object instance = moduleInstance.Value.instance;

                    if (!definition.InterfaceType.IsInstanceOfType(instance))
                        continue;

                    object? content = await GetContent(definition, instance, loadContext);

                    if (content is UserControl userControl)
                    {
                        ToolsTabControl.Items.Add(new TabItem
                        {
                            Header = moduleInstance.Value.label,
                            Content = userControl
                        });

                        NoToolsTextBlock.Visibility = Visibility.Collapsed;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Log(
                        Microsoft.Extensions.Logging.LogLevel.Error,
                        $"Error loading tool {catalogEntry.Label}: {ex.ToString().ReplaceLineEndings(" | ")}");
                }
            }
        }
    }

    
}
