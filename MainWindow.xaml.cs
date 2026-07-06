using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using z2d.Models;
using z2d.Services;

namespace z2d
{
    public partial class MainWindow : Window
    {
        private const string PresetsDirectoryName = "presets";
        private const string ExeDirectoryName = "exe";
        private const string ExecutableName = "winws2.exe";

        private readonly string _baseDir = AppContext.BaseDirectory;
        private readonly WinwsStatusService _winwsStatusService = new();
        private readonly UserSettingsService _userSettingsService = new();
        private Dictionary<string, FileInfo> _presets = new(StringComparer.OrdinalIgnoreCase);
        private Process? _currentProcess;
        private bool _isWinwsRunning;
        private bool _isInitializingUserState;
        private UserSettings _currentUserSettings = new();

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_OnLoaded;
            Closing += MainWindow_OnClosing;
            UpdateUiState();
        }

        private async void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            _winwsStatusService.StatusChanged += OnWinwsStatusChanged;
            _winwsStatusService.StartWatching();

            _isInitializingUserState = true;

            try
            {
                _currentUserSettings = await _userSettingsService.LoadAsync();
                HiddenModeCheckBox.IsChecked = _currentUserSettings.StartWithoutWindow;
                await LoadPresetsAsync(_currentUserSettings);
            }
            finally
            {
                _isInitializingUserState = false;
                UpdateUiState();
            }
            
        }

        private async Task LoadPresetsAsync(UserSettings settings)
        {
            try
            {
                SetStatus("Загружаем пресеты...");

                _presets = await PresetDiscoveryService.GetPresetsAsync(PresetsDirectoryName, _baseDir);
                var presetItems = _presets.OrderBy(x => x.Key).ToList();
                PresetComboBox.ItemsSource = presetItems;

                if (presetItems.Count > 0)
                {
                    PresetComboBox.SelectedIndex = GetPresetIndexToSelect(presetItems, settings.LastSelectedPresetName);
                    SetStatus($"Найдено пресетов: {_presets.Count}");
                }
                else
                {
                    SetStatus($"Папка '{PresetsDirectoryName}' пуста или не найдена рядом с приложением.");
                }
            }
            catch (Exception ex)
            {
                SetStatus($"Ошибка загрузки пресетов: {ex.Message}");
            }
        }

        private static int GetPresetIndexToSelect(
            List<KeyValuePair<string, FileInfo>> presetItems,
            string? lastSelectedPresetName)
        {
            if (string.IsNullOrWhiteSpace(lastSelectedPresetName))
            {
                return 0;
            }

            var savedPresetIndex = presetItems.FindIndex(x =>
                string.Equals(x.Key, lastSelectedPresetName, StringComparison.OrdinalIgnoreCase));

            return savedPresetIndex >= 0
                ? savedPresetIndex
                : 0;
        }

        private async void PresetComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializingUserState)
            {
                return;
            }

            if (PresetComboBox.SelectedItem is not KeyValuePair<string, FileInfo> selectedPreset)
            {
                return;
            }

            _currentUserSettings.LastSelectedPresetName = selectedPreset.Key;
            await SaveCurrentUserSettingsAsync();
        }

        private async void HiddenModeCheckBox_OnChanged(object sender, RoutedEventArgs e)
        {
            if (_isInitializingUserState)
            {
                return;
            }

            _currentUserSettings.StartWithoutWindow = HiddenModeCheckBox.IsChecked == true;
            await SaveCurrentUserSettingsAsync();
        }

        private async Task SaveCurrentUserSettingsAsync()
        {
            try
            {
                await _userSettingsService.SaveAsync(_currentUserSettings);
            }
            catch (Exception ex)
            {
                SetStatus($"Не удалось сохранить настройки: {ex.Message}");
            }
        }

        private async void StartButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (_isWinwsRunning)
            {
                SetStatus("Процесс уже запущен.");
                return;
            }

            if (PresetComboBox.SelectedItem is not KeyValuePair<string, FileInfo> selectedPreset)
            {
                SetStatus("Выберите пресет.");
                return;
            }

            try
            {
                _isWinwsRunning = true;
                UpdateUiState();
                SetStatus($"Запуск пресета '{selectedPreset.Key}'...");

                var arguments = await WinwsArgsBuilder.BuildArgumentsAsync(selectedPreset.Value.FullName);
                var exePath = Path.Combine(_baseDir, ExeDirectoryName, ExecutableName);

                var isHiddenMode = HiddenModeCheckBox.IsChecked == true;

                _currentProcess = await WinwsProcessService.StartAsync(exePath, arguments, _baseDir, isHiddenMode);

                if (_currentProcess is null)
                {
                    throw new InvalidOperationException("Не удалось запустить процесс.");
                }

                _currentProcess.EnableRaisingEvents = true;
                _currentProcess.Exited += CurrentProcess_OnExited;

                SetStatus($"Процесс запущен. PID: {_currentProcess.Id}");
            }
            catch (Exception ex)
            {
                _currentProcess = null;
                _isWinwsRunning = _winwsStatusService.IsRunning();
                UpdateUiState();
                SetStatus($"Ошибка запуска: {ex.Message}");
            }
        }

        private void StopButton_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                WinwsProcessService.Stop();
                _currentProcess = null;
                _isWinwsRunning = false;
                UpdateUiState();
                SetStatus("Все процессы winws остановлены.");
            }
            catch (Exception ex)
            {
                SetStatus($"Ошибка остановки: {ex.Message}");
            }
        }

        private void CurrentProcess_OnExited(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                _currentProcess = null;
                _isWinwsRunning = _winwsStatusService.IsRunning();
                UpdateUiState();
                SetStatus("Процесс завершен.");
            });
        }

        private async void MainWindow_OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            _winwsStatusService.StatusChanged -= OnWinwsStatusChanged;
            _winwsStatusService.Dispose();

            if (!_isInitializingUserState)
            {
                await SaveCurrentUserSettingsAsync();
            }

            if (_currentProcess is { HasExited: false })
            {
                WinwsProcessService.Stop();
            }
        }

        private void UpdateUiState()
        {
            StartButton.IsEnabled = !_isWinwsRunning && PresetComboBox.Items.Count > 0;
            StopButton.IsEnabled = _isWinwsRunning;
            PresetComboBox.IsEnabled = !_isWinwsRunning;
            HiddenModeCheckBox.IsEnabled = !_isWinwsRunning;
        }

        private void SetStatus(string message)
        {
            StatusTextBlock.Text = message;
        }

        private void OnWinwsStatusChanged(bool isRunning)
        {
            Dispatcher.Invoke(() =>
            {
                _isWinwsRunning = isRunning;
                UpdateUiState();

                WinwsStateTextBlock.Text = isRunning
                    ? "winws: запущен"
                    : "winws: не запущен";

                WinwsStateTextBlock.Foreground = isRunning
                    ? Brushes.ForestGreen
                    : Brushes.DarkOrange;
            });
        }

        private void PresetTestMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            var window = new PresetTestWindow(_baseDir, _presets)
            {
                Owner = this
            };

            window.ShowDialog();
        }
    }
}
