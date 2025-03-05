using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PoFunQuiz.Web.Services
{
    /// <summary>
    /// CircuitHandler implementation to track connection state
    /// </summary>
    public class ConnectionStateHandler : CircuitHandler
    {
        private readonly ConnectionState _connectionState;
        private readonly ILogger<ConnectionStateHandler> _logger;
        private readonly object _lock = new object();
        private bool _isConnected = false;
        private string _circuitId = string.Empty;

        public ConnectionStateHandler(ConnectionState connectionState, ILogger<ConnectionStateHandler> logger)
        {
            _connectionState = connectionState;
            _logger = logger;
            _logger.LogInformation("🔌 ConnectionStateHandler initialized at {Time}", DateTime.Now);
            Console.WriteLine($"🔌 ConnectionStateHandler initialized at {DateTime.Now}");
        }

        public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
        {
            _circuitId = circuit.Id;
            _logger.LogInformation("🔌 Circuit opened with ID {CircuitId} at {Time}", circuit.Id, DateTime.Now);
            Console.WriteLine($"🔌 Circuit opened with ID {circuit.Id} at {DateTime.Now}");
            return Task.CompletedTask;
        }

        public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
        {
            bool shouldNotify = false;
            
            lock (_lock)
            {
                if (!_isConnected)
                {
                    _isConnected = true;
                    shouldNotify = true;
                }
            }
            
            if (shouldNotify)
            {
                _logger.LogInformation("✅ Circuit connection UP detected at {Time} for circuit {CircuitId}", 
                    DateTime.Now, circuit.Id);
                Console.WriteLine($"✅ Circuit connection UP detected at {DateTime.Now} for circuit {circuit.Id}");
                
                // Use Task.Run to avoid blocking the circuit
                Task.Run(() => _connectionState.SetConnectionState(true));
            }
            
            return Task.CompletedTask;
        }

        public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
        {
            bool shouldNotify = false;
            
            lock (_lock)
            {
                if (_isConnected)
                {
                    _isConnected = false;
                    shouldNotify = true;
                }
            }
            
            if (shouldNotify)
            {
                _logger.LogWarning("⚠️ Circuit connection DOWN detected at {Time} for circuit {CircuitId}", 
                    DateTime.Now, circuit.Id);
                Console.WriteLine($"⚠️ Circuit connection DOWN detected at {DateTime.Now} for circuit {circuit.Id}");
                
                // Use Task.Run to avoid blocking the circuit
                Task.Run(() => _connectionState.SetConnectionState(false));
            }
            
            return Task.CompletedTask;
        }
        
        public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
        {
            _logger.LogInformation("🔌 Circuit closed with ID {CircuitId} at {Time}", circuit.Id, DateTime.Now);
            Console.WriteLine($"🔌 Circuit closed with ID {circuit.Id} at {DateTime.Now}");
            return Task.CompletedTask;
        }
    }
} 