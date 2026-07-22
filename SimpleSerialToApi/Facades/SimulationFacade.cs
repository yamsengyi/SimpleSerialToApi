using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using SimpleSerialToApi.Services;

namespace SimpleSerialToApi.Facades
{
    /// <summary>
    /// 시뮬레이션 관리를 위한 파사드 구현
    /// </summary>
    public class SimulationFacade : ISimulationFacade, INotifyPropertyChanged
    {
        private readonly ILogger<SimulationFacade> _logger;
        private readonly SerialDataSimulator _serialDataSimulator;

        private bool _isSimulating;
        private string _simulationInterval = "3";

        public SimulationFacade(
            ILogger<SimulationFacade> logger,
            SerialDataSimulator serialDataSimulator)
        {
            _logger = logger;
            _serialDataSimulator = serialDataSimulator;
        }

        public bool IsSimulating
        {
            get => _isSimulating;
            private set
            {
                _isSimulating = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SimulationButtonText));
            }
        }

        public string SimulationInterval
        {
            get => _simulationInterval;
            set { _simulationInterval = value; OnPropertyChanged(); }
        }

        public string SimulationButtonText => IsSimulating ? "Stop Simulation" : "Start Simulation";

        public void Toggle()
        {
            if (IsSimulating)
                Stop();
            else
                Start();
        }

        public void Start()
        {
            try
            {
                if (int.TryParse(SimulationInterval, out var interval) && interval > 0)
                {
                    _serialDataSimulator.Start(interval);
                    IsSimulating = true;
                    _logger.LogInformation("Simulation started (interval: {Interval}s)", interval);
                }
                else
                {
                System.Windows.MessageBox.Show("Please enter a valid simulation interval (positive number).",
                        "Invalid Input", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting simulation");
                System.Windows.MessageBox.Show($"Error starting simulation: {ex.Message}",
                    "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        public void Stop()
        {
            try
            {
                _serialDataSimulator.Stop();
                IsSimulating = false;
                _logger.LogInformation("Simulation stopped");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping simulation");
                System.Windows.MessageBox.Show($"Error stopping simulation: {ex.Message}",
                    "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        public void GenerateSingleData()
        {
            try
            {
                _serialDataSimulator.GenerateSingleData();
                _logger.LogInformation("Single simulation data generated");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating single simulation data");
                System.Windows.MessageBox.Show($"Error generating data: {ex.Message}",
                    "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
