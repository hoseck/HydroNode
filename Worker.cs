using System.Net.Sockets;
using HydroNode.Core;
using HydroNode.Monitoring;
using HydroNode.Socket;

namespace HydroNode
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly DatabaseService _db;
        private readonly IConfiguration _config;

        private int port = 5000;

        public Worker(ILogger<Worker> logger, DatabaseService db, IConfiguration config)
        {
            _logger = logger;
            _db = db;
            _config = config;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // var udpServer = new UdpSocketServer(_logger);

            var tcpServer = new TcpSocketServer(_logger, _db, _config);

            var monitor = new ServerMonitor(_logger);

            await tcpServer.StartAsync(stoppingToken);
        }
    }
}
