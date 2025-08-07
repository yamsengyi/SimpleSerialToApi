namespace SimpleSerialToApi.ViewModels
{
    public interface IMessenger
    {
        void Subscribe<T>(Action<T> handler) where T : class;
        void Unsubscribe<T>(Action<T> handler) where T : class;
        void Send<T>(T message) where T : class;
    }

    public class Messenger : IMessenger
    {
        private readonly Dictionary<Type, List<Delegate>> _handlers = new();
        private readonly object _lock = new();

        public void Subscribe<T>(Action<T> handler) where T : class
        {
            lock (_lock)
            {
                var messageType = typeof(T);
                if (!_handlers.ContainsKey(messageType))
                {
                    _handlers[messageType] = new List<Delegate>();
                }
                _handlers[messageType].Add(handler);
            }
        }

        public void Unsubscribe<T>(Action<T> handler) where T : class
        {
            lock (_lock)
            {
                var messageType = typeof(T);
                if (_handlers.ContainsKey(messageType))
                {
                    _handlers[messageType].Remove(handler);
                    if (_handlers[messageType].Count == 0)
                    {
                        _handlers.Remove(messageType);
                    }
                }
            }
        }

        public void Send<T>(T message) where T : class
        {
            List<Delegate>? handlers;
            lock (_lock)
            {
                var messageType = typeof(T);
                if (!_handlers.TryGetValue(messageType, out handlers))
                {
                    return;
                }
                handlers = new List<Delegate>(handlers); // Create a copy to avoid modification during enumeration
            }

            foreach (var handler in handlers)
            {
                try
                {
                    ((Action<T>)handler)(message);
                }
                catch (Exception ex)
                {
                    // Log the exception if you have a logger available
                    System.Diagnostics.Debug.WriteLine($"Error in message handler: {ex.Message}");
                }
            }
        }
    }

    // Message types for communication between ViewModels
    public class StatusUpdatedMessage
    {
        public string ComponentName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    public class LogMessage
    {
        public string Level { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string? Source { get; set; }
    }

    public class ConfigurationChangedMessage
    {
        public string SectionName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}