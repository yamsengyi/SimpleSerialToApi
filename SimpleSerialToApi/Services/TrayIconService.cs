using System;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Reflection;
using DrawingFont = System.Drawing.Font;
using DrawingFontStyle = System.Drawing.FontStyle;
using WpfWindow = System.Windows.Window;
using WpfWindowState = System.Windows.WindowState;

namespace SimpleSerialToApi.Services
{
    /// <summary>
    /// 시스템 트레이 아이콘 관리 서비스
    /// </summary>
    public class TrayIconService : IDisposable
    {
        private readonly ILogger<TrayIconService> _logger;
        private NotifyIcon? _notifyIcon;
        private bool _disposed = false;
        private WpfWindow? _mainWindow;

        public event EventHandler? ShowMainWindow;
        public event EventHandler? ExitApplication;

        public TrayIconService(ILogger<TrayIconService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 메인 윈도우 설정
        /// </summary>
        /// <param name="mainWindow">메인 윈도우 인스턴스</param>
        public void SetMainWindow(WpfWindow mainWindow)
        {
            _mainWindow = mainWindow;
            
            // 메인 윈도우 최소화 이벤트 처리
            if (_mainWindow != null)
            {
                _mainWindow.StateChanged += MainWindow_StateChanged;
            }
            
            _logger.LogInformation("Main window set for tray icon service");
        }

        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            if (_mainWindow?.WindowState == WpfWindowState.Minimized)
            {
                _mainWindow.ShowInTaskbar = false;
                Show();
                UpdateStatus(false, "프로그램이 트레이로 최소화되었습니다.");
            }
        }

        private void OnShowMainWindow(object? sender, EventArgs e)
        {
            if (_mainWindow != null)
            {
                _mainWindow.Show();
                _mainWindow.WindowState = WpfWindowState.Normal;
                _mainWindow.ShowInTaskbar = true;
                _mainWindow.Activate();
                Hide();
                _logger.LogInformation("Main window restored from tray");
            }
            else
            {
                ShowMainWindow?.Invoke(this, EventArgs.Empty);
            }
        }

        private void OnExitApplication(object? sender, EventArgs e)
        {
            ExitApplication?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 트레이 아이콘 초기화
        /// </summary>
        public void Initialize()
        {
            try
            {
                _notifyIcon = new NotifyIcon
                {
                    Icon = CreateDefaultIcon(),
                    Text = "Simple Serial To API",
                    Visible = false
                };

                // 컨텍스트 메뉴 생성
                var contextMenu = new ContextMenuStrip();
                
                var showMenuItem = new ToolStripMenuItem("보기", null, OnShowMainWindow);
                var exitMenuItem = new ToolStripMenuItem("종료", null, OnExitApplication);
                
                contextMenu.Items.Add(showMenuItem);
                contextMenu.Items.Add(new ToolStripSeparator());
                contextMenu.Items.Add(exitMenuItem);
                
                _notifyIcon.ContextMenuStrip = contextMenu;

                // 더블클릭으로 창 보기
                _notifyIcon.DoubleClick += (s, e) => OnShowMainWindow(s, e);

                _logger.LogInformation("Tray icon initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing tray icon");
            }
        }

        /// <summary>
        /// 트레이 아이콘 표시
        /// </summary>
        public void Show()
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = true;
                _logger.LogInformation("Tray icon shown");
            }
        }

        /// <summary>
        /// 트레이 아이콘 숨김
        /// </summary>
        public void Hide()
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _logger.LogInformation("Tray icon hidden");
            }
        }

        /// <summary>
        /// 트레이 아이콘 상태 업데이트
        /// </summary>
        /// <param name="isConnected">연결 상태</param>
        /// <param name="message">상태 메시지</param>
        public void UpdateStatus(bool isConnected, string message)
        {
            if (_notifyIcon != null)
            {
                var statusText = isConnected ? "연결됨" : "연결 안됨";
                _notifyIcon.Text = $"Simple Serial To API - {statusText}";
                
                if (!string.IsNullOrEmpty(message))
                {
                    _notifyIcon.BalloonTipTitle = "Simple Serial To API";
                    _notifyIcon.BalloonTipText = message;
                    _notifyIcon.BalloonTipIcon = isConnected ? ToolTipIcon.Info : ToolTipIcon.Warning;
                    _notifyIcon.ShowBalloonTip(3000);
                }
                
                _logger.LogDebug("Tray icon status updated: {Status}, Message: {Message}", statusText, message);
            }
        }

        /// <summary>
        /// 기본 아이콘 생성
        /// </summary>
        /// <returns>기본 아이콘</returns>
        private Icon CreateDefaultIcon()
        {
            try
            {
                // 16x16 간단한 아이콘 생성
                using var bitmap = new Bitmap(16, 16);
                using var graphics = Graphics.FromImage(bitmap);
                
                // 배경
                graphics.Clear(Color.Transparent);
                
                // 간단한 원형 아이콘
                using var brush = new SolidBrush(Color.Blue);
                graphics.FillEllipse(brush, 2, 2, 12, 12);
                
                // 텍스트 'S'
                using var font = new DrawingFont(FontFamily.GenericSansSerif, 8, DrawingFontStyle.Bold);
                using var textBrush = new SolidBrush(Color.White);
                graphics.DrawString("S", font, textBrush, 4, 1);

                return Icon.FromHandle(bitmap.GetHicon());
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not create custom icon, using system default");
                return SystemIcons.Application;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _notifyIcon?.Dispose();
                _disposed = true;
                _logger.LogInformation("TrayIconService disposed");
            }
        }
    }
}
