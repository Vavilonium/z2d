using System;
using System.Diagnostics;
using System.Management;

namespace z2d.Services
{
    public sealed class WinwsStatusService : IDisposable
    {
        private ManagementEventWatcher? _startWatcher;
        private ManagementEventWatcher? _stopWatcher;

        public event Action<bool>? StatusChanged;

        public bool IsRunning()
        {
            var processes = Process.GetProcesses()
                .Where(p =>
                    p.ProcessName.Equals("winws", StringComparison.OrdinalIgnoreCase) ||
                    p.ProcessName.Equals("winws2", StringComparison.OrdinalIgnoreCase))
                .ToList();

            return processes.Count > 0;
        }

        public void StartWatching()
        {
            _startWatcher = new ManagementEventWatcher(
                new WqlEventQuery("SELECT * FROM Win32_ProcessStartTrace WHERE ProcessName = 'winws.exe' OR ProcessName = 'winws2.exe'"));
            _startWatcher.EventArrived += OnProcessChanged;
            _startWatcher.Start();

            _stopWatcher = new ManagementEventWatcher(
                new WqlEventQuery("SELECT * FROM Win32_ProcessStopTrace WHERE ProcessName = 'winws.exe' OR ProcessName = 'winws2.exe'"));
            _stopWatcher.EventArrived += OnProcessChanged;
            _stopWatcher.Start();

            RaiseStatusChanged();
        }

        private void OnProcessChanged(object sender, EventArrivedEventArgs e)
        {
            RaiseStatusChanged();
        }

        private void RaiseStatusChanged()
        {
            StatusChanged?.Invoke(IsRunning());
        }

        public void Dispose()
        {
            if (_startWatcher is not null)
            {
                _startWatcher.EventArrived -= OnProcessChanged;
                _startWatcher.Stop();
                _startWatcher.Dispose();
            }

            if (_stopWatcher is not null)
            {
                _stopWatcher.EventArrived -= OnProcessChanged;
                _stopWatcher.Stop();
                _stopWatcher.Dispose();
            }
        }
    }
}
