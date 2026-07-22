using System;
using Microsoft.Extensions.Logging;

namespace SimpleSerialToApi.Facades
{
    /// <summary>
    /// 팝업 창 관리를 위한 파사드 구현
    /// </summary>
    public class WindowManagementFacade : IWindowManagementFacade
    {
        private readonly ILogger<WindowManagementFacade> _logger;
        private readonly IMonitorFacade _monitorFacade;

        private Views.DataMappingWindow? _dataMappingWindow;
        private Views.ReservedWordsWindow? _reservedWordsWindow;
        private Views.SerialMonitorWindow? _serialMonitorWindow;
        private Views.ApiMonitorWindow? _apiMonitorWindow;

        public WindowManagementFacade(
            ILogger<WindowManagementFacade> logger,
            IMonitorFacade monitorFacade)
        {
            _logger = logger;
            _monitorFacade = monitorFacade;
        }

        public void OpenDataMapping(object dataContext)
        {
            try
            {
                if (_dataMappingWindow != null)
                {
                    try
                    {
                        if (_dataMappingWindow.IsVisible)
                        {
                            _dataMappingWindow.Activate();
                            _dataMappingWindow.Focus();
                            return;
                        }
                        _dataMappingWindow = null;
                    }
                    catch { _dataMappingWindow = null; }
                }

                _dataMappingWindow = new Views.DataMappingWindow(dataContext);
                _dataMappingWindow.Closed += (_, _) => _dataMappingWindow = null;
                _dataMappingWindow.Show();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error opening data mapping window");
            }
        }

        public void ShowReservedWords()
        {
            try
            {
                if (_reservedWordsWindow != null)
                {
                    try
                    {
                        if (_reservedWordsWindow.IsVisible)
                        {
                            _reservedWordsWindow.Activate();
                            _reservedWordsWindow.Focus();
                            return;
                        }
                        _reservedWordsWindow = null;
                    }
                    catch { _reservedWordsWindow = null; }
                }

                _reservedWordsWindow = new Views.ReservedWordsWindow();
                _reservedWordsWindow.Closed += (_, _) => _reservedWordsWindow = null;
                _reservedWordsWindow.Show();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing reserved words window");
            }
        }

        public void OpenSerialMonitor(object dataContext)
        {
            try
            {
                if (_serialMonitorWindow != null)
                {
                    try
                    {
                        if (_serialMonitorWindow.IsVisible)
                        {
                            _serialMonitorWindow.Activate();
                            _serialMonitorWindow.Focus();
                            return;
                        }
                        _serialMonitorWindow = null;
                    }
                    catch { _serialMonitorWindow = null; }
                }

                _monitorFacade.LoadExistingSerialMessages();

                _serialMonitorWindow = new Views.SerialMonitorWindow(dataContext);
                _serialMonitorWindow.Closed += (_, _) => _serialMonitorWindow = null;
                _serialMonitorWindow.Show();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error opening serial monitor window");
            }
        }

        public void OpenApiMonitor(object dataContext)
        {
            try
            {
                if (_apiMonitorWindow != null)
                {
                    try
                    {
                        if (_apiMonitorWindow.IsVisible)
                        {
                            _apiMonitorWindow.Activate();
                            _apiMonitorWindow.Focus();
                            return;
                        }
                        _apiMonitorWindow = null;
                    }
                    catch { _apiMonitorWindow = null; }
                }

                _monitorFacade.LoadExistingApiMessages();

                _apiMonitorWindow = new Views.ApiMonitorWindow(dataContext);
                _apiMonitorWindow.Closed += (_, _) => _apiMonitorWindow = null;
                _apiMonitorWindow.Show();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error opening API monitor window");
            }
        }

        public void CloseAllChildWindows()
        {
            try
            {
                CloseWindowSafely(ref _dataMappingWindow);
                CloseWindowSafely(ref _reservedWordsWindow);
                CloseWindowSafely(ref _serialMonitorWindow);
                CloseWindowSafely(ref _apiMonitorWindow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during closing child windows");
            }
        }

        private void CloseWindowSafely<T>(ref T? window) where T : System.Windows.Window
        {
            if (window == null) return;
            try
            {
                window.Close();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error closing {WindowType}", typeof(T).Name);
            }
            window = null;
        }
    }
}
