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
        private string _apiEndpoint = string.Empty;
        private string _apiHeaders = string.Empty;
        private string _contentType = "application/json";
        private string _authToken = string.Empty;
        private int _timeoutSeconds = 30;
        private int _retryCount = 3;
        private bool _useFullPath = false;
        private string _fullPathTemplate = string.Empty;

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
            set { _transmissionType = value; OnPropertyChanged(nameof(TransmissionType)); }
        }

        public string ApiMethod
        {
            get => _apiMethod;
            set { _apiMethod = value; OnPropertyChanged(nameof(ApiMethod)); }
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

        /// <summary>
        /// Indicates whether to use full path template with reserved words
        /// </summary>
        public bool UseFullPath
        {
            get => _useFullPath;
            set { _useFullPath = value; OnPropertyChanged(nameof(UseFullPath)); }
        }

        /// <summary>
        /// Full URL template with reserved words (e.g., http://example.com/api?dn=@deviceId&data={data})
        /// Reserved words will be replaced before URL encoding
        /// </summary>
        public string FullPathTemplate
        {
            get => _fullPathTemplate;
            set { _fullPathTemplate = value; OnPropertyChanged(nameof(FullPathTemplate)); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
