using System;
using System.Configuration;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace SimpleSerialToApi.Services
{
    /// <summary>
    /// 예약어 처리 서비스
    /// </summary>
    public class ReservedWordService
    {
        private readonly ILogger<ReservedWordService> _logger;
        private readonly Regex _reservedWordRegex;

        public ReservedWordService(ILogger<ReservedWordService> logger)
        {
            _logger = logger;
            // @ 기호로 시작하는 예약어 패턴
            _reservedWordRegex = new Regex(@"@\w+", RegexOptions.Compiled);
        }

        /// <summary>
        /// 템플릿에서 예약어를 실제 값으로 치환
        /// </summary>
        /// <param name="template">예약어가 포함된 템플릿 문자열</param>
        /// <returns>예약어가 치환된 문자열</returns>
        public string ProcessReservedWords(string template)
        {
            if (string.IsNullOrEmpty(template))
                return template;

            try
            {
                var result = _reservedWordRegex.Replace(template, match =>
                {
                    var reservedWord = match.Value;
                    return GetReservedWordValue(reservedWord);
                });

                _logger.LogDebug("Processed template: '{Template}' -> '{Result}'", template, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing reserved words in template: {Template}", template);
                return template;
            }
        }

        /// <summary>
        /// 예약어에 대응하는 실제 값 반환
        /// </summary>
        /// <param name="reservedWord">예약어 (@로 시작하는 문자열)</param>
        /// <returns>치환할 값</returns>
        private string GetReservedWordValue(string reservedWord)
        {
            var now = DateTime.Now;

            return reservedWord.ToLower() switch
            {
                "@yyyymmddhhmmssfffff" => now.ToString("yyyyMMddHHmmssfff"),
                "@yyyymmddhhmmss" => now.ToString("yyyyMMddHHmmss"),
                "@yyyymmdd" => now.ToString("yyyyMMdd"),
                "@deviceid" => GetDeviceId(),
                "@timestamp" => now.ToString("yyyy-MM-dd HH:mm:ss"),
                "@unixtime" => ((DateTimeOffset)now).ToUnixTimeSeconds().ToString(),
                "@guid" => Guid.NewGuid().ToString(),
                _ => HandleUnknownReservedWord(reservedWord)
            };
        }

        /// <summary>
        /// App.config에서 DeviceId 가져오기
        /// </summary>
        /// <returns>설정된 DeviceId 또는 기본값</returns>
        private string GetDeviceId()
        {
            try
            {
                var deviceId = ConfigurationManager.AppSettings["DeviceId"];
                return !string.IsNullOrEmpty(deviceId) ? deviceId : "DEVICE_UNKNOWN";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get DeviceId from config, using default");
                return "DEVICE_UNKNOWN";
            }
        }

        /// <summary>
        /// 알려지지 않은 예약어 처리
        /// </summary>
        /// <param name="reservedWord">알려지지 않은 예약어</param>
        /// <returns>처리 결과</returns>
        private string HandleUnknownReservedWord(string reservedWord)
        {
            _logger.LogWarning("Unknown reserved word: {ReservedWord}", reservedWord);
            return reservedWord; // 원본 그대로 반환
        }

        /// <summary>
        /// 지원되는 예약어 목록 반환
        /// </summary>
        /// <returns>예약어 목록 배열</returns>
        public string[] GetSupportedReservedWords()
        {
            return new[]
            {
                "@yyyyMMddHHmmssfff",
                "@yyyyMMddHHmmss", 
                "@yyyyMMdd",
                "@deviceId",
                "@timestamp",
                "@unixTime",
                "@guid"
            };
        }
    }
}
