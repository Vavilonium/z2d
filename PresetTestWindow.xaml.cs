using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
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

        private readonly ObservableCollection<SelectableItem<TargetItem>> _targets = new();
        private readonly ObservableCollection<SelectableItem<FileInfo>> _presetItems = new();

        public PresetTestWindow(string baseDir, IReadOnlyDictionary<string, FileInfo> presets)
        {
            InitializeComponent();

            _baseDir = baseDir;
            _presets = presets;

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

        private void StartTestButton_OnClick(object sender, RoutedEventArgs e)
        {
            
        }
    }
}
