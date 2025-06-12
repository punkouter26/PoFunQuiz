using System;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace PoFunQuiz.Web.Services
{
    /// <summary>
    /// Service for tracking connection state and notifying components
    /// </summary>
    public class ConnectionState
    {
        private readonly ILogger<ConnectionState> _logger;
        private bool _isConnected = true;
        private readonly object _lock = new object();
        
        public ConnectionState(ILogger<ConnectionState> logger)
        {
            _logger = logger;
            _logger.LogInformation("🔌 ConnectionState service initialized at {Time}", DateTime.Now);
            Console.WriteLine($"🔌 ConnectionState service initialized at {DateTime.Now}");
        }
        
        public bool IsConnected 
        { 
            get 
            {
                lock (_lock)
                {
                    return _isConnected;
                }
            }
            private set
            {
                lock (_lock)
                {
                    _isConnected = value;
                }
            }
        }
        
        public event Action<bool>? ConnectionChanged;

        public void SetConnectionState(bool isConnected)
        {
            bool changed = false;
            bool currentValue;
            
            lock (_lock)
            {
                currentValue = _isConnected;
                if (_isConnected != isConnected)
                {
                    _isConnected = isConnected;
                    changed = true;
                }
            }
            
            if (changed)
            {
                _logger.LogInformation("🔌 Connection state changing from {OldState} to {NewState} at {Time}", 
                    currentValue ? "Connected" : "Disconnected",
                    isConnected ? "Connected" : "Disconnected", 
                    DateTime.Now);
                Console.WriteLine($"🔌 Connection state changing from {(currentValue ? "Connected" : "Disconnected")} to {(isConnected ? "Connected" : "Disconnected")} at {DateTime.Now}");
                
                try
                {
                    ConnectionChanged?.Invoke(isConnected);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error invoking ConnectionChanged event: {Message}", ex.Message);
                    Console.WriteLine($"❌ Error invoking ConnectionChanged event: {ex.Message}");
                }
            }
        }
    }
} 