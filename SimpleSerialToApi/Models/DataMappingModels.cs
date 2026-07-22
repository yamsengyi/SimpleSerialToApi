using System;
using System.ComponentModel;
using System.Net.Http;

namespace SimpleSerialToApi.Models
{
    /// <summary>
    /// 데이터 소스 유형
    /// </summary>
    public enum DataSource
    {
        Serial,
        ApiResponse
    }

    /// <summary>
    /// 전송 방식 유형
    /// </summary>
    public enum TransmissionType
    {
        Serial,
        Api
    }

    /// <summary>
    /// 데이터 매핑 시나리오 모델
    /// </summary>
    public class DataMappingScenario : INotifyPropertyChanged
    {
        private bool _isEnabled = false;
        private string _name = string.Empty;
        private DataSource _source = DataSource.Serial;
        private string _identifier = string.Empty;
        private string _valueTemplate = string.Empty;
        private TransmissionType _transmissionType = TransmissionType.Api;
        private string _apiMethod = "POST";
        private string _apiUrl = string.Empty;
        private string _apiEndpoint = string.Empty;
        private string _apiHeaders = string.Empty;
        private string _contentType = "application/json";
        private string _authToken = string.Empty;
        private int _timeoutSeconds = 30;
        private int _retryCount = 3;

        public bool IsEnabled
        {
            get => _isEnabled;
            set { _isEnabled = value; OnPropertyChanged(nameof(IsEnabled)); }
        }

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(Name)); }
        }

        public DataSource Source
        {
            get => _source;
            set { _source = value; OnPropertyChanged(nameof(Source)); }
        }

        public string Identifier
        {
            get => _identifier;
            set { _identifier = value; OnPropertyChanged(nameof(Identifier)); }
        }

        public string ValueTemplate
        {
            get => _valueTemplate;
            set { _valueTemplate = value; OnPropertyChanged(nameof(ValueTemplate)); }
        }

        public TransmissionType TransmissionType
        {
            get => _transmissionType;
            set
            {
                _transmissionType = value;
                OnPropertyChanged(nameof(TransmissionType));
                OnPropertyChanged(nameof(IsApiTransmission));
                OnPropertyChanged(nameof(IsSerialTransmission));
            }
        }

        /// <summary>
        /// API 전송 방식일 때 true (Serial 전송 시 관련 컬럼 비활성화 용도)
        /// </summary>
        public bool IsApiTransmission => _transmissionType == TransmissionType.Api;

        /// <summary>
        /// Serial 전송 방식일 때 true
        /// </summary>
        public bool IsSerialTransmission => _transmissionType == TransmissionType.Serial;

        public string ApiMethod
        {
            get => _apiMethod;
            set { _apiMethod = value; OnPropertyChanged(nameof(ApiMethod)); }
        }

        public string ApiUrl
        {
            get => _apiUrl;
            set { _apiUrl = value; OnPropertyChanged(nameof(ApiUrl)); }
        }

        public string ApiEndpoint
        {
            get => _apiEndpoint;
            set { _apiEndpoint = value; OnPropertyChanged(nameof(ApiEndpoint)); }
        }

        public string ApiHeaders
        {
            get => _apiHeaders;
            set { _apiHeaders = value; OnPropertyChanged(nameof(ApiHeaders)); }
        }

        public string ContentType
        {
            get => _contentType;
            set { _contentType = value; OnPropertyChanged(nameof(ContentType)); }
        }

        public string AuthToken
        {
            get => _authToken;
            set { _authToken = value; OnPropertyChanged(nameof(AuthToken)); }
        }

        public int TimeoutSeconds
        {
            get => _timeoutSeconds;
            set { _timeoutSeconds = value; OnPropertyChanged(nameof(TimeoutSeconds)); }
        }

        public int RetryCount
        {
            get => _retryCount;
            set { _retryCount = value; OnPropertyChanged(nameof(RetryCount)); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
