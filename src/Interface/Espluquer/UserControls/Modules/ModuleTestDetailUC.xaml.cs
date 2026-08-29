using Espluque.Contracts.Contributions;
using Espluque.Contracts.Modules;
using Espluquer.Entities;
using Espluquer.Services;
using Espluquer.UserControls.Shell;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace Espluquer.UserControls.Modules
{
    public partial class ModuleTestDetailUC : UserControl
    {
        private readonly IModuleService _moduleService;
        private readonly IContributionSettingsService _contributionSettingsService;

        public IModuleInfo? ModuleInfo
        {
            get => DataContext as IModuleInfo;
            set
            {
                DataContext = value;

                JsonTextBox.Text =
                    value is not null && File.Exists(value.FilePath)
                        ? File.ReadAllText(value.FilePath)
                        : string.Empty;
            }
        }

        public List<ContributionHealthDto> ContributionHealths { get; set; } = [];

        public static readonly DependencyProperty ModuleHealthProperty = DependencyProperty.Register(nameof(ModuleHealth), typeof(ModuleHealthDto), typeof(ModuleTestDetailUC));

        public ModuleHealthDto? ModuleHealth
        {
            get => (ModuleHealthDto?)GetValue(ModuleHealthProperty);
            set => SetValue(ModuleHealthProperty, value);
        }

        private readonly ConceptSearchUC _conceptSearchUC;
        public ConceptSearchUC ConceptSearch => _conceptSearchUC;

        private IModuleContributionInfo? _tagContribution;
        private ContentControl? _tagSearchHost;

        public ModuleTestDetailUC(
            IModuleService moduleService,
            IContributionSettingsService contributionSettingsService,
            ConceptSearchUC conceptSearchUC)
        {
            _moduleService = moduleService;
            _contributionSettingsService = contributionSettingsService;
            _conceptSearchUC = conceptSearchUC;

            InitializeComponent();

            _conceptSearchUC.ConceptSelected += ConceptSearch_ConceptSelected;
        }

        private void ConceptSearch_ConceptSelected(ConceptDto concept)
        {
            if (_tagContribution is null || _tagSearchHost is null)
            {
                return;
            }

            List<string> tags = _tagContribution.ContributionSettings.Tags;

            if (!tags.Any(tag =>
                string.Equals(tag, concept.Term, StringComparison.OrdinalIgnoreCase)))
            {
                tags.Add(concept.Term);

                CollectionViewSource
                    .GetDefaultView(tags)
                    .Refresh();
            }

            Button? addButton = GetAddTagButton(_tagSearchHost);

            if (addButton is not null)
            {
                addButton.Visibility = Visibility.Visible;
            }

            _tagSearchHost.Content = null;
            _tagSearchHost.Visibility = Visibility.Collapsed;

            _tagSearchHost.Visibility = Visibility.Collapsed;

            _tagSearchHost = null;
            _tagContribution = null;
        }

        private static Button? GetAddTagButton(ContentControl searchHost)
        {
            if (searchHost.Parent is not StackPanel tagsPanel)
            {
                return null;
            }

            return tagsPanel.Children
                .OfType<Button>()
                .FirstOrDefault(button => button.Name == "AddTagButton");
        }

        private async void ContributionDetail_SettingsChanged(object? sender, EventArgs e)
        {
            if (ModuleInfo is null
                || sender is not ContributionDetailUC contributionDetail
                || contributionDetail.DataContext is not IModuleContributionInfo contribution)
            {
                return;
            }

            await _contributionSettingsService.SaveUserSettings(
                ModuleInfo.Assembly,
                contribution.InterfaceType,
                contribution.ClassName,
                contribution.ContributionSettings);
        }

    }
}