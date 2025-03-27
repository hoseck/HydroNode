using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HydroNode.Monitoring
{
    internal class ServerMonitor
    {
        private readonly ILogger _logger;
        private readonly Timer _timer;

        public ServerMonitor(ILogger logger)
        {
            _logger = logger;
            _timer = new Timer(LogStatus, null, 0, 10000); // 10초마다
        }

        private void LogStatus(object state)
        {
            _logger.LogInformation($"[모니터링] 현재 상태 점검: {DateTime.Now}");
            // 여기에 CPU, 메모리 등 추가 가능
        }
    }
}
