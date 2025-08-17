using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SimpleSerialToApi.Models;

namespace SimpleSerialToApi.Services
{
    /// <summary>
    /// 시리얼 데이터 시뮬레이터 - 실제 장비 없이 테스트할 수 있도록 가상 데이터 생성
    /// </summary>
    public class SerialDataSimulator
    {
        private readonly ILogger<SerialDataSimulator> _logger;
        private readonly Random _random;
        private System.Threading.Timer? _timer;
        private bool _isRunning = false;

        /// <summary>
        /// 시뮬레이션 데이터 수신 이벤트
        /// </summary>
        public event EventHandler<SimulatedSerialDataEventArgs>? DataGenerated;

        public SerialDataSimulator(ILogger<SerialDataSimulator> logger)
        {
            _logger = logger;
            _random = new Random();
        }

        /// <summary>
        /// 시뮬레이션 중인지 여부
        /// </summary>
        public bool IsRunning => _isRunning;

        /// <summary>
        /// 시뮬레이션 시작
        /// </summary>
        /// <param name="intervalSeconds">데이터 생성 간격 (초)</param>
        public void Start(int intervalSeconds = 5)
        {
            if (_isRunning)
            {
                _logger.LogWarning("Simulator is already running");
                return;
            }

            _isRunning = true;
            var interval = TimeSpan.FromSeconds(intervalSeconds);
            
            _timer = new System.Threading.Timer(GenerateData, null, TimeSpan.Zero, interval);
            _logger.LogInformation("Serial data simulator started with {Interval}s interval", intervalSeconds);
        }

        /// <summary>
        /// 시뮬레이션 중지
        /// </summary>
        public void Stop()
        {
            if (!_isRunning)
            {
                _logger.LogWarning("Simulator is not running");
                return;
            }

            _timer?.Dispose();
            _timer = null;
            _isRunning = false;
            _logger.LogInformation("Serial data simulator stopped");
        }

        /// <summary>
        /// 단일 데이터 생성 (수동 트리거)
        /// </summary>
        public void GenerateSingleData()
        {
            GenerateData(null);
        }

        /// <summary>
        /// 시뮬레이션 데이터 생성
        /// </summary>
        private void GenerateData(object? state)
        {
            try
            {
                var scenarios = GetSimulationScenarios();
                var selectedScenario = scenarios[_random.Next(scenarios.Length)];
                
                var data = GenerateScenarioData(selectedScenario);
                var encodedData = Encoding.UTF8.GetBytes(data);
                
                var eventArgs = new SimulatedSerialDataEventArgs
                {
                    Data = encodedData,
                    DataString = data,
                    Scenario = selectedScenario,
                    Timestamp = DateTime.Now
                };

                DataGenerated?.Invoke(this, eventArgs);
                _logger.LogDebug("Generated simulation data: '{Data}' (Scenario: {Scenario})", 
                    data, selectedScenario);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating simulation data");
            }
        }

        /// <summary>
        /// 시나리오별 데이터 생성
        /// </summary>
        private string GenerateScenarioData(string scenario)
        {
            return scenario switch
            {
                "Temperature" => GenerateTemperatureData(),
                "Humidity" => GenerateHumidityData(),
                "Pressure" => GeneratePressureData(),
                "Status" => GenerateStatusData(),
                "Error" => GenerateErrorData(),
                "Heartbeat" => GenerateHeartbeatData(),
                "Custom" => GenerateCustomData(),
                _ => GenerateGenericData()
            };
        }

        /// <summary>
        /// 온도 데이터 생성
        /// </summary>
        private string GenerateTemperatureData()
        {
            var temperature = _random.NextDouble() * 50 + 10; // 10-60도 범위
            return $"STX TEMP:{temperature:F1} ETX";
        }

        /// <summary>
        /// 습도 데이터 생성
        /// </summary>
        private string GenerateHumidityData()
        {
            var humidity = _random.NextDouble() * 80 + 20; // 20-100% 범위
            return $"STX HUMID:{humidity:F1}% ETX";
        }

        /// <summary>
        /// 압력 데이터 생성
        /// </summary>
        private string GeneratePressureData()
        {
            var pressure = _random.NextDouble() * 200 + 800; // 800-1000 hPa 범위
            return $"STX PRESSURE:{pressure:F2}hPa ETX";
        }

        /// <summary>
        /// 상태 데이터 생성
        /// </summary>
        private string GenerateStatusData()
        {
            var statuses = new[] { "NORMAL", "WARNING", "CRITICAL", "MAINTENANCE" };
            var status = statuses[_random.Next(statuses.Length)];
            return $"STX STATUS:{status} ETX";
        }

        /// <summary>
        /// 에러 데이터 생성
        /// </summary>
        private string GenerateErrorData()
        {
            var errorCodes = new[] { "E001", "E002", "E101", "E201" };
            var errorCode = errorCodes[_random.Next(errorCodes.Length)];
            return $"STX ERROR:{errorCode} ETX";
        }

        /// <summary>
        /// 하트비트 데이터 생성
        /// </summary>
        private string GenerateHeartbeatData()
        {
            return $"STX HEARTBEAT:{DateTime.Now:HHmmss} ETX";
        }

        /// <summary>
        /// 사용자 정의 데이터 생성
        /// </summary>
        private string GenerateCustomData()
        {
            var value = _random.Next(0, 1000);
            return $"STX CUSTOM_DATA:{value} ETX";
        }

        /// <summary>
        /// 일반 데이터 생성
        /// </summary>
        private string GenerateGenericData()
        {
            var value = _random.Next(0, 100);
            return $"STX DATA:{value} ETX";
        }

        /// <summary>
        /// 시뮬레이션 시나리오 목록
        /// </summary>
        private static string[] GetSimulationScenarios()
        {
            return new[]
            {
                "Temperature",
                "Humidity", 
                "Pressure",
                "Status",
                "Error",
                "Heartbeat",
                "Custom"
            };
        }

        public void Dispose()
        {
            Stop();
        }
    }

    /// <summary>
    /// 시뮬레이션 데이터 이벤트 인자
    /// </summary>
    public class SimulatedSerialDataEventArgs : EventArgs
    {
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public string DataString { get; set; } = string.Empty;
        public string Scenario { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
