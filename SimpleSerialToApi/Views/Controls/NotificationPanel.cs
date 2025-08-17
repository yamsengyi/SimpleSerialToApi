using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SimpleSerialToApi.Views.Controls
{
    public class NotificationPanel : System.Windows.Controls.Control
    {
        static NotificationPanel()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(NotificationPanel),
                new FrameworkPropertyMetadata(typeof(NotificationPanel)));
        }

        public static readonly DependencyProperty NotificationsProperty =
            DependencyProperty.Register(nameof(Notifications), typeof(ObservableCollection<NotificationItem>), 
                typeof(NotificationPanel), new PropertyMetadata(null));

        public static readonly DependencyProperty MaxNotificationsProperty =
            DependencyProperty.Register(nameof(MaxNotifications), typeof(int), typeof(NotificationPanel),
                new PropertyMetadata(5));

        public ObservableCollection<NotificationItem>? Notifications
        {
            get => (ObservableCollection<NotificationItem>?)GetValue(NotificationsProperty);
            set => SetValue(NotificationsProperty, value);
        }

        public int MaxNotifications
        {
            get => (int)GetValue(MaxNotificationsProperty);
            set => SetValue(MaxNotificationsProperty, value);
        }

        public ICommand? DismissNotificationCommand { get; set; }
    }

    public class NotificationItem
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public NotificationLevel Level { get; set; } = NotificationLevel.Info;
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public bool CanDismiss { get; set; } = true;
    }

    public enum NotificationLevel
    {
        Info,
        Warning,
        Error,
        Success
    }
}
