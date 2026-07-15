using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using Microsoft.Win32;
using z2d.Models;
using z2d.Services;

namespace z2d
{
    public partial class PresetTestWindow : Window
    {
        private const string TargetsFilePath = "utils\\targets.txt";

        private readonly string _baseDir;
        private readonly IReadOnlyDictionary<string, FileInfo> _presets;
        private readonly TargetReaderService _targetReaderService = new();
        private readonly PresetTestRunner _presetTestRunner;

        private readonly ObservableCollection<SelectableItem<TargetItem>> _targets = new();
        private readonly ObservableCollection<SelectableItem<FileInfo>> _presetItems = new();

        public PresetTestWindow(string baseDir, IReadOnlyDictionary<string, FileInfo> presets)
        {
            InitializeComponent();

            _baseDir = baseDir;
            _presets = presets;
            _presetTestRunner = new PresetTestRunner(baseDir);

            TargetsListBox.ItemsSource = _targets;
            PresetsListBox.ItemsSource = _presetItems;

            Loaded += PresetTestWindow_OnLoaded;
        }

        private async void PresetTestWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            LoadPresets();
            await LoadTargetsAsync();
        }

        private void LoadPresets()
        {
            _presetItems.Clear();

            foreach (var preset in _presets.OrderBy(x => x.Key))
            {
                _presetItems.Add(new SelectableItem<FileInfo>(
                    value: preset.Value,
                    displayName: preset.Key,
                    isSelected: true));
            }
        }

        private async Task LoadTargetsAsync()
        {
            var targetsPath = Path.Combine(_baseDir, TargetsFilePath);

            try
            {
                var targets = await _targetReaderService.ReadAsync(targetsPath);

                _targets.Clear();

                foreach (var target in targets)
                {
                    _targets.Add(new SelectableItem<TargetItem>(
                        value: target,
                        displayName: $"{target.Name} - {target.Target}",
                        isSelected: true));
                }
            }
            catch (Exception ex)
            {
                
            }
        }

        private void SelectAllTargetsButton_OnClick(object sender, RoutedEventArgs e)
        {
            SetSelection(_targets, isSelected: true);
        }

        private void ClearAllTargetsButton_OnClick(object sender, RoutedEventArgs e)
        {
            SetSelection(_targets, isSelected: false);
        }

        private void SelectAllPresetsButton_OnClick(object sender, RoutedEventArgs e)
        {
            SetSelection(_presetItems, isSelected: true);
        }

        private void ClearAllPresetsButton_OnClick(object sender, RoutedEventArgs e)
        {
            SetSelection(_presetItems, isSelected: false);
        }

        private static void SetSelection<T>(
            IEnumerable<SelectableItem<T>> items,
            bool isSelected)
        {
            foreach (var item in items)
            {
                item.IsSelected = isSelected;
            }
        }

        private async void StartTestButton_OnClick(object sender, RoutedEventArgs e)
        {
            var selectedTargets = _targets
                .Where(x => x.IsSelected)
                .Select(x => x.Value)
                .ToList();

            var selectedPresets = _presetItems
                .Where(x => x.IsSelected)
                .Select(x => x.Value)
                .ToList();

            var request = new PresetTestRunRequest
            {
                Targets = selectedTargets,
                Presets = selectedPresets,
                TimeoutSeconds = 5,
                MaxParallelTargets = 8
            };

            var progress = new Progress<PresetTestProgress>(p =>
            {
                TestProgressBar.Value = p.Percent;
                Title = $"Тест пресетов - {p.CurrentPresetName} ({p.PresetIndex}/{p.PresetCount})";
            });

            SetTestingState(true);

            try
            {
                TestProgressBar.Value = 0;

                var result = await _presetTestRunner.RunAsync(request, progress);

                ShowTestCompletedDialog(result);
            }
            catch (OperationCanceledException)
            {

            }
            catch (Exception ex)
            {

            }
            finally
            {
                Title = "Тест престов";
                SetTestingState(false);
            }
        }

        private void SetTestingState(bool isTesting)
        {
            StartTestButton.IsEnabled = !isTesting;
            TargetsListBox.IsEnabled = !isTesting;
            PresetsListBox.IsEnabled = !isTesting;

            SelectAllTargetsButton.IsEnabled = !isTesting;
            ClearAllTargetsButton.IsEnabled = !isTesting;
            SelectAllPresetsButton.IsEnabled = !isTesting;
            ClearAllPresetsButton.IsEnabled = !isTesting;
        }

        private void ShowTestCompletedDialog(PresetTestRunResult result)
        {
            var bestPresetName = result.ToString();

            var dialogResult = MessageBox.Show(
                this,
                $"Лучший пресет: {bestPresetName}\n\nСохранить результаты тестирования?",
                "Тестирование завершено",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (dialogResult != MessageBoxResult.Yes)
            {
                return;
            }

            var saveFileDialog = new SaveFileDialog
            {
                Title = "Сохранить результаты тестирования",
                FileName = $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt",
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                DefaultExt = ".txt",
                AddExtension = true,
                OverwritePrompt = true
            };

            if (saveFileDialog.ShowDialog(this) != true)
            {
                return;
            }

            File.WriteAllText(
                saveFileDialog.FileName,
                result.ToReportString(),
                Encoding.UTF8);
        }
    }
}
