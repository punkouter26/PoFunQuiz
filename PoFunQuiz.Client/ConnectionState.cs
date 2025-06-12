using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PoFunQuiz.Client // Updated namespace
{
    /// <summary>
    /// Manages the connection state of the Blazor client to the server.
    /// </summary>
    public class ConnectionState
    {
        public event Action<bool>? ConnectionStateChanged;

        private bool _isConnected;

        public bool IsConnected
        {
            get => _isConnected;
            private set
            {
                if (_isConnected != value)
                {
                    _isConnected = value;
                    ConnectionStateChanged?.Invoke(_isConnected);
                }
            }
        }

        public void SetConnectionState(bool isConnected)
        {
            IsConnected = isConnected;
        }
    }
}
