using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;
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
        private Dictionary<string, FileInfo> _presets = new(StringComparer.OrdinalIgnoreCase);
        private Process? _currentProcess;
        private bool _isWinwsRunning;

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

            await LoadPresetsAsync();
        }

        private async Task LoadPresetsAsync()
        {
            try
            {
                SetStatus("Загружаем пресеты...");

                _presets = await PresetDiscoveryService.GetPresetsAsync(PresetsDirectoryName, _baseDir);
                PresetComboBox.ItemsSource = _presets.OrderBy(x => x.Key).ToList();

                if (PresetComboBox.Items.Count > 0)
                {
                    PresetComboBox.SelectedIndex = 0;
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
            finally
            {
                UpdateUiState();
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

                _currentProcess = await WinwsProcessService.StartAsync(exePath, arguments, _baseDir);

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

        private void MainWindow_OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            _winwsStatusService.StatusChanged -= OnWinwsStatusChanged;
            _winwsStatusService.Dispose();

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
    }
}
