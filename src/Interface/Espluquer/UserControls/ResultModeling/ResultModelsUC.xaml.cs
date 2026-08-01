using Espluque.Contracts.DetectionResult;
using Espluque.Contracts.Interfaces;
using Espluquer.Entities;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Espluquer.UserControls.ResultModeling
{
    public partial class ResultModelsUC : RefreshableUserControl
    {
        private readonly IResultService _resultService;
        private readonly IEntityFactory _entityFactory;

        private List<ResultModelDefinitionDto> _resultModelDefinitions = [];
        private bool _isCreatingResultModel;

        public ResultModelsUC(IResultService resultService, IEntityFactory entityFactory)
        {
            _resultService = resultService;
            _entityFactory = entityFactory;

            InitializeComponent();
        }

        protected override async Task RefreshAsync()
        {
            List<IResultModelDefinition> definitions = await _resultService.GetResultModelDefinitions();

            _resultModelDefinitions = definitions.Select(definition => new ResultModelDefinitionDto
            {
                Id = definition.Id,
                Name = definition.Name,
                ThesaurusTag = definition.ThesaurusTag,
                Properties = definition.Properties,
                PropertyLinks = definition.PropertyLinks
            }).ToList();

            ResultModelsListBox.ItemsSource = _resultModelDefinitions;
            ResultModelsListBox.SelectedIndex = _resultModelDefinitions.Count > 0 ? 0 : -1;
        }

        private async void DeleteResultModelButton_Click(object sender, RoutedEventArgs e)
        {
            if ((ResultModelsListBox.SelectedItem is not ResultModelDefinitionDto resultModelDefinition)
                || (resultModelDefinition.Id is not int resultModelId))
            {
                return;
            }

            bool isDeleted = await _resultService.DeleteResultModelDefinition(resultModelId);

            if (isDeleted)
            {
                await RefreshAsync();
            }
        }

        private void PropertyNameTextBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (sender is not TextBox textBox ||
                textBox.Tag is not int propertyIndex ||
                ResultModelsListBox.SelectedItem is not ResultModelDefinitionDto resultModelDefinition ||
                propertyIndex < 0 ||
                propertyIndex >= resultModelDefinition.Properties.Count)
            {
                return;
            }

            string propertyName = textBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(propertyName))
            {
                textBox.Text = resultModelDefinition.Properties[propertyIndex];
                return;
            }

            resultModelDefinition.Properties[propertyIndex] = propertyName;
        }

        private void MovePropertyUpButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button ||
                button.Tag is not int propertyIndex ||
                ResultModelsListBox.SelectedItem is not ResultModelDefinitionDto resultModelDefinition ||
                propertyIndex <= 0 ||
                propertyIndex >= resultModelDefinition.Properties.Count)
            {
                return;
            }

            (resultModelDefinition.Properties[propertyIndex - 1], resultModelDefinition.Properties[propertyIndex]) =
                (resultModelDefinition.Properties[propertyIndex], resultModelDefinition.Properties[propertyIndex - 1]);

            ResultModelsListBox.SelectedItem = null;
            ResultModelsListBox.SelectedItem = resultModelDefinition;
        }

        private void MovePropertyDownButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button ||
                button.Tag is not int propertyIndex ||
                ResultModelsListBox.SelectedItem is not ResultModelDefinitionDto resultModelDefinition ||
                propertyIndex < 0 ||
                propertyIndex >= resultModelDefinition.Properties.Count - 1)
            {
                return;
            }

            (resultModelDefinition.Properties[propertyIndex], resultModelDefinition.Properties[propertyIndex + 1]) =
                (resultModelDefinition.Properties[propertyIndex + 1], resultModelDefinition.Properties[propertyIndex]);

            ResultModelsListBox.SelectedItem = null;
            ResultModelsListBox.SelectedItem = resultModelDefinition;
        }

        private void DeletePropertyButton_Click(object sender, RoutedEventArgs e)
        {
            if ((sender is not Button button || button.Tag is not int propertyIndex)
                || (ResultModelsListBox.SelectedItem is not ResultModelDefinitionDto resultModelDefinition)
                || (propertyIndex < 0)
                || (propertyIndex >= resultModelDefinition.Properties.Count))
            {
                return;
            }

            resultModelDefinition.Properties.RemoveAt(propertyIndex);

            ResultModelsListBox.SelectedItem = null;
            ResultModelsListBox.SelectedItem = resultModelDefinition;
        }

        private void AddPropertyButton_Click(object sender, RoutedEventArgs e)
        {
            if (ResultModelsListBox.SelectedItem is not ResultModelDefinitionDto resultModelDefinition)
            {
                return;
            }

            const string baseName = "New property";
            string propertyName = baseName;
            int index = 2;

            while (resultModelDefinition.Properties.Contains(propertyName, StringComparer.Ordinal))
            {
                propertyName = $"{baseName} {index}";
                index++;
            }

            resultModelDefinition.Properties.Add(propertyName);

            ResultModelsListBox.SelectedItem = null;
            ResultModelsListBox.SelectedItem = resultModelDefinition;
        }

        private void AddResultModelButton_Click(object sender, RoutedEventArgs e)
        {
            _isCreatingResultModel = true;

            AddResultModelButton.Visibility = Visibility.Collapsed;
            NewResultModelTextBox.Visibility = Visibility.Visible;
            NewResultModelTextBox.Text = "New Result Model";

            NewResultModelTextBox.Focus();
            NewResultModelTextBox.SelectAll();
        }

        private async void NewResultModelTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
            {
                return;
            }

            e.Handled = true;
            await SaveNewResultModelAsync();
        }

        private async void NewResultModelTextBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            await SaveNewResultModelAsync();
        }

        private async Task SaveNewResultModelAsync()
        {
            if (!_isCreatingResultModel)
            {
                return;
            }

            _isCreatingResultModel = false;

            string resultModelName = NewResultModelTextBox.Text.Trim();

            NewResultModelTextBox.Visibility = Visibility.Collapsed;
            AddResultModelButton.Visibility = Visibility.Visible;

            if (string.IsNullOrWhiteSpace(resultModelName))
            {
                return;
            }

            IResultModelDefinition resultModelDefinition = _entityFactory.CreateResultModelDefinition( null, resultModelName, string.Empty, [], []);

            IResultModelDefinition? savedResultModelDefinition = await _resultService.SaveResultModelDefinition(resultModelDefinition);

            if (savedResultModelDefinition?.Id is not int resultModelId)
            {
                return;
            }

            await RefreshAsync();

            ResultModelsListBox.SelectedItem = _resultModelDefinitions.FirstOrDefault( resultModelDefinitionDto => resultModelDefinitionDto.Id == resultModelId);
        }
    }
}
