using System;
using System.IO.Ports;
using System.Windows;
using SimpleSerialToApi.Models;

namespace SimpleSerialToApi.Views
{
    /// <summary>
    /// 시리얼 통신 설정 창
    /// </summary>
    public partial class SerialConfigWindow : Window
    {
        private SerialConnectionSettings _originalSettings;
        
        /// <summary>
        /// 설정 결과
        /// </summary>
        public SerialConnectionSettings Settings { get; private set; }
        
        /// <summary>
        /// 설정이 변경되었는지 여부
        /// </summary>
        public bool IsChanged { get; private set; } = false;
        
        public SerialConfigWindow(SerialConnectionSettings currentSettings)
        {
            InitializeComponent();
            
            _originalSettings = currentSettings;
            Settings = new SerialConnectionSettings
            {
                PortName = currentSettings.PortName, // 읽기 전용으로 유지 (변경은 메인화면에서만)
                BaudRate = currentSettings.BaudRate,
                Parity = currentSettings.Parity,
                DataBits = currentSettings.DataBits,
                StopBits = currentSettings.StopBits,
                Handshake = currentSettings.Handshake,
                ReadTimeout = currentSettings.ReadTimeout,
                WriteTimeout = currentSettings.WriteTimeout
            };
            
            DataContext = new SerialConfigViewModel(Settings);
        }
        
        private void BtnOK_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 현재 DataContext에서 설정 값들을 가져와서 Settings에 적용
                if (DataContext is SerialConfigViewModel viewModel)
                {
                    viewModel.ApplyToSettings(Settings);
                    // PortName은 항상 원본 설정 유지 (메인화면에서만 변경 가능)
                    Settings.PortName = _originalSettings.PortName;
                }
                
                // 변경 사항 확인
                IsChanged = !Settings.Equals(_originalSettings);
                
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"설정 저장 중 오류가 발생했습니다: {ex.Message}", 
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
        
        private void BtnDefault_Click(object sender, RoutedEventArgs e)
        {
            var defaultSettings = new SerialConnectionSettings();
            // PortName은 원본 설정 유지 (메인화면에서만 변경 가능)
            defaultSettings.PortName = _originalSettings.PortName;
            DataContext = new SerialConfigViewModel(defaultSettings);
            
            System.Windows.MessageBox.Show("기본 설정으로 복원되었습니다.\n(COM PORT는 변경되지 않습니다)", 
                "알림", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        
        private void BtnTest_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 현재 설정으로 임시 시리얼 포트 테스트
                if (DataContext is SerialConfigViewModel viewModel)
                {
                    var testSettings = new SerialConnectionSettings();
                    viewModel.ApplyToSettings(testSettings);
                    
                    // PortName은 원본 설정에서 가져오기 (메인화면에서만 변경 가능)
                    testSettings.PortName = _originalSettings.PortName;
                    
                    using (var testPort = new SerialPort())
                    {
                        testPort.PortName = testSettings.PortName;
                        testPort.BaudRate = testSettings.BaudRate;
                        testPort.Parity = testSettings.Parity;
                        testPort.DataBits = testSettings.DataBits;
                        testPort.StopBits = testSettings.StopBits;
                        testPort.Handshake = testSettings.Handshake;
                        testPort.ReadTimeout = testSettings.ReadTimeout;
                        testPort.WriteTimeout = testSettings.WriteTimeout;
                        
                        testPort.Open();
                        testPort.Close();
                        
                        System.Windows.MessageBox.Show($"통신 설정 테스트가 성공했습니다!\nPort: {testSettings.PortName}", 
                            "테스트 성공", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"통신 설정 테스트 실패:\n{ex.Message}", 
                    "테스트 실패", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
    
    /// <summary>
    /// 시리얼 설정 뷰모델
    /// </summary>
    public class SerialConfigViewModel : System.ComponentModel.INotifyPropertyChanged
    {
        private SerialConnectionSettings _settings;
        
        public SerialConfigViewModel(SerialConnectionSettings settings)
        {
            _settings = settings;
        }
        
        public int BaudRate
        {
            get => _settings.BaudRate;
            set
            {
                _settings.BaudRate = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SettingsDescription));
            }
        }
        
        public Parity Parity
        {
            get => _settings.Parity;
            set
            {
                _settings.Parity = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SettingsDescription));
            }
        }
        
        public int DataBits
        {
            get => _settings.DataBits;
            set
            {
                _settings.DataBits = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SettingsDescription));
            }
        }
        
        public StopBits StopBits
        {
            get => _settings.StopBits;
            set
            {
                _settings.StopBits = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SettingsDescription));
            }
        }
        
        public Handshake Handshake
        {
            get => _settings.Handshake;
            set
            {
                _settings.Handshake = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SettingsDescription));
            }
        }
        
        public int ReadTimeout
        {
            get => _settings.ReadTimeout;
            set
            {
                _settings.ReadTimeout = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SettingsDescription));
            }
        }
        
        public int WriteTimeout
        {
            get => _settings.WriteTimeout;
            set
            {
                _settings.WriteTimeout = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SettingsDescription));
            }
        }
        
        public string SettingsDescription =>
            $"BaudRate: {BaudRate}, Parity: {Parity}, DataBits: {DataBits}\n" +
            $"StopBits: {StopBits}, Handshake: {Handshake}\n" +
            $"Timeouts: Read={ReadTimeout}ms, Write={WriteTimeout}ms";
        
        public void ApplyToSettings(SerialConnectionSettings target)
        {
            target.BaudRate = BaudRate;
            target.Parity = Parity;
            target.DataBits = DataBits;
            target.StopBits = StopBits;
            target.Handshake = Handshake;
            target.ReadTimeout = ReadTimeout;
            target.WriteTimeout = WriteTimeout;
        }
        
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
    }
}
