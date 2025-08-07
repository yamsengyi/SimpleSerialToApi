using Microsoft.Extensions.Logging;
using SimpleSerialToApi.Services.Logging;
using System.Windows;

namespace SimpleSerialToApi.Services.Notifications
{
    /// <summary>
    /// Interface for user notification services
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Shows an informational message to the user
        /// </summary>
        void ShowInfo(string message);

        /// <summary>
        /// Shows a warning message to the user
        /// </summary>
        void ShowWarning(string message);

        /// <summary>
        /// Shows an error message to the user
        /// </summary>
        void ShowError(string message, Exception? exception = null);

        /// <summary>
        /// Shows a confirmation dialog to the user
        /// </summary>
        Task<bool> ShowConfirmationAsync(string message, string title = "확인");

        /// <summary>
        /// Shows a progress notification (typically non-blocking)
        /// </summary>
        void ShowProgress(string message, CancellationToken cancellationToken = default);

        /// <summary>
        /// Hides the progress notification
        /// </summary>
        void HideProgress();

        /// <summary>
        /// Shows a toast notification (brief, non-intrusive)
        /// </summary>
        void ShowToast(string message, NotificationLevel level = NotificationLevel.Info, TimeSpan? duration = null);
    }

    /// <summary>
    /// Notification level for categorizing messages
    /// </summary>
    public enum NotificationLevel
    {
        Info,
        Success,
        Warning,
        Error
    }

    /// <summary>
    /// WPF implementation of the notification service
    /// </summary>
    public class WpfNotificationService : INotificationService
    {
        private readonly ILogger<WpfNotificationService> _logger;
        private readonly object _progressLock = new object();
        private Window? _progressWindow;
        private CancellationTokenSource? _progressCancellation;

        public WpfNotificationService(ILogger<WpfNotificationService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void ShowInfo(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;

            _logger.LogUserAction("ShowInfo", "NotificationService", message);

            Application.Current?.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(message, "정보", MessageBoxButton.OK, MessageBoxImage.Information);
            });
        }

        public void ShowWarning(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;

            _logger.LogUserAction("ShowWarning", "NotificationService", message);

            Application.Current?.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(message, "경고", MessageBoxButton.OK, MessageBoxImage.Warning);
            });
        }

        public void ShowError(string message, Exception? exception = null)
        {
            if (string.IsNullOrEmpty(message))
                return;

            _logger.LogUserAction("ShowError", "NotificationService", $"{message} | Exception: {exception?.GetType().Name}");

            Application.Current?.Dispatcher.Invoke(() =>
            {
                var detailedMessage = exception != null
                    ? $"{message}\n\n기술적 세부사항:\n{exception.Message}"
                    : message;

                MessageBox.Show(detailedMessage, "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }

        public async Task<bool> ShowConfirmationAsync(string message, string title = "확인")
        {
            if (string.IsNullOrEmpty(message))
                return false;

            _logger.LogUserAction("ShowConfirmation", "NotificationService", $"{title}: {message}");

            var result = false;
            var tcs = new TaskCompletionSource<bool>();

            Application.Current?.Dispatcher.Invoke(() =>
            {
                try
                {
                    var messageBoxResult = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
                    result = messageBoxResult == MessageBoxResult.Yes;
                    tcs.SetResult(result);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error showing confirmation dialog");
                    tcs.SetException(ex);
                }
            });

            return await tcs.Task;
        }

        public void ShowProgress(string message, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(message))
                return;

            _logger.LogUserAction("ShowProgress", "NotificationService", message);

            lock (_progressLock)
            {
                // Cancel any existing progress
                _progressCancellation?.Cancel();
                _progressCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                Application.Current?.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        HideProgressInternal(); // Hide any existing progress window

                        _progressWindow = new ProgressWindow(message, _progressCancellation.Token);
                        _progressWindow.Show();

                        // Auto-hide after cancellation
                        _progressCancellation.Token.Register(() =>
                        {
                            Application.Current?.Dispatcher.BeginInvoke(() => HideProgressInternal());
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error showing progress window");
                    }
                });
            }
        }

        public void HideProgress()
        {
            _logger.LogUserAction("HideProgress", "NotificationService");

            lock (_progressLock)
            {
                _progressCancellation?.Cancel();
                Application.Current?.Dispatcher.BeginInvoke(HideProgressInternal);
            }
        }

        public void ShowToast(string message, NotificationLevel level = NotificationLevel.Info, TimeSpan? duration = null)
        {
            if (string.IsNullOrEmpty(message))
                return;

            _logger.LogUserAction("ShowToast", "NotificationService", $"{level}: {message}");

            // For now, implement as a temporary message box that auto-closes
            // In a full implementation, this could be a custom toast notification system
            Task.Run(async () =>
            {
                try
                {
                    var actualDuration = duration ?? TimeSpan.FromSeconds(level == NotificationLevel.Error ? 5 : 3);
                    
                    Application.Current?.Dispatcher.Invoke(() =>
                    {
                        var toastWindow = new ToastWindow(message, level);
                        toastWindow.Show();
                    });

                    await Task.Delay(actualDuration);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error showing toast notification");
                }
            });
        }

        private void HideProgressInternal()
        {
            try
            {
                _progressWindow?.Close();
                _progressWindow = null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error hiding progress window");
            }
        }
    }

    /// <summary>
    /// Simple progress window for showing progress notifications
    /// </summary>
    internal class ProgressWindow : Window
    {
        public ProgressWindow(string message, CancellationToken cancellationToken)
        {
            Title = "작업 진행 중";
            Width = 400;
            Height = 150;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.NoResize;
            WindowStyle = WindowStyle.ToolWindow;

            var grid = new System.Windows.Controls.Grid();
            Content = grid;

            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition());
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new GridLength(50) });

            var textBlock = new System.Windows.Controls.TextBlock
            {
                Text = message,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(20),
                TextWrapping = TextWrapping.Wrap
            };

            var progressBar = new System.Windows.Controls.ProgressBar
            {
                IsIndeterminate = true,
                Height = 20,
                Margin = new Thickness(20, 0, 20, 10)
            };

            System.Windows.Controls.Grid.SetRow(textBlock, 0);
            System.Windows.Controls.Grid.SetRow(progressBar, 1);

            grid.Children.Add(textBlock);
            grid.Children.Add(progressBar);

            // Auto-close when cancelled
            cancellationToken.Register(() =>
            {
                Dispatcher.BeginInvoke(() =>
                {
                    try
                    {
                        Close();
                    }
                    catch { }
                });
            });
        }
    }

    /// <summary>
    /// Simple toast window for brief notifications
    /// </summary>
    internal class ToastWindow : Window
    {
        public ToastWindow(string message, NotificationLevel level)
        {
            Title = "";
            Width = 300;
            Height = 100;
            WindowStartupLocation = WindowStartupLocation.Manual;
            ResizeMode = ResizeMode.NoResize;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Topmost = true;
            ShowInTaskbar = false;

            // Position at bottom right of screen
            Left = SystemParameters.WorkArea.Right - Width - 20;
            Top = SystemParameters.WorkArea.Bottom - Height - 20;

            var border = new System.Windows.Controls.Border
            {
                CornerRadius = new CornerRadius(5),
                Background = GetBackgroundBrush(level),
                BorderBrush = GetBorderBrush(level),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10)
            };

            var textBlock = new System.Windows.Controls.TextBlock
            {
                Text = message,
                Foreground = System.Windows.Media.Brushes.White,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            border.Child = textBlock;
            Content = border;

            // Auto-close after delay
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(level == NotificationLevel.Error ? 5 : 3)
            };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                try { Close(); } catch { }
            };
            timer.Start();

            // Fade in animation
            Opacity = 0;
            var fadeIn = new System.Windows.Media.Animation.DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
            BeginAnimation(OpacityProperty, fadeIn);
        }

        private System.Windows.Media.Brush GetBackgroundBrush(NotificationLevel level)
        {
            return level switch
            {
                NotificationLevel.Success => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(40, 167, 69)),
                NotificationLevel.Warning => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 193, 7)),
                NotificationLevel.Error => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 53, 69)),
                _ => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(23, 162, 184))
            };
        }

        private System.Windows.Media.Brush GetBorderBrush(NotificationLevel level)
        {
            return level switch
            {
                NotificationLevel.Success => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(34, 142, 58)),
                NotificationLevel.Warning => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(217, 164, 6)),
                NotificationLevel.Error => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(187, 45, 59)),
                _ => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(20, 138, 157))
            };
        }
    }

    /// <summary>
    /// Console implementation of the notification service (for testing or console apps)
    /// </summary>
    public class ConsoleNotificationService : INotificationService
    {
        private readonly ILogger<ConsoleNotificationService> _logger;

        public ConsoleNotificationService(ILogger<ConsoleNotificationService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void ShowInfo(string message)
        {
            _logger.LogInformation("INFO: {Message}", message);
            Console.WriteLine($"[INFO] {message}");
        }

        public void ShowWarning(string message)
        {
            _logger.LogWarning("WARNING: {Message}", message);
            Console.WriteLine($"[WARNING] {message}");
        }

        public void ShowError(string message, Exception? exception = null)
        {
            _logger.LogError(exception, "ERROR: {Message}", message);
            Console.WriteLine($"[ERROR] {message}");
            if (exception != null)
            {
                Console.WriteLine($"Exception: {exception.Message}");
            }
        }

        public async Task<bool> ShowConfirmationAsync(string message, string title = "확인")
        {
            _logger.LogInformation("CONFIRMATION: {Title} - {Message}", title, message);
            Console.WriteLine($"[{title}] {message} (y/n): ");
            var response = await Task.Run(() => Console.ReadLine());
            return string.Equals(response?.Trim(), "y", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(response?.Trim(), "yes", StringComparison.OrdinalIgnoreCase);
        }

        public void ShowProgress(string message, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("PROGRESS: {Message}", message);
            Console.WriteLine($"[PROGRESS] {message}...");
        }

        public void HideProgress()
        {
            Console.WriteLine("[PROGRESS] 완료");
        }

        public void ShowToast(string message, NotificationLevel level = NotificationLevel.Info, TimeSpan? duration = null)
        {
            _logger.LogInformation("TOAST [{Level}]: {Message}", level, message);
            Console.WriteLine($"[TOAST-{level.ToString().ToUpper()}] {message}");
        }
    }
}