using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using HydroNode.Core;

namespace HydroNode.Socket
{
    public class UdpSocketServer
    {
        private readonly ILogger _logger;
        private readonly DatabaseService _db;
        private readonly UdpClient _udpClient;

        public UdpSocketServer(ILogger logger, DatabaseService db)
        {
            _logger = logger;
            _db = db;
            _udpClient = new UdpClient(5000); // 예시 포트
        }

        public async Task StartAsync(CancellationToken token)
        {
            _logger.LogInformation("UDP 서버가 시작되었습니다.");
            while (!token.IsCancellationRequested)
            {
                var result = await _udpClient.ReceiveAsync();
                string msg = Encoding.UTF8.GetString(result.Buffer);
                _logger.LogInformation($"수신: {msg}");

                // 메시지 파싱 및 처리
                // var parsed = MessageParser.Parse(msg);
                // 처리 로직 (DB 저장 등)
                // await _db.InsertDataAsync(parsed);

                // ✅ 응답 메시지 전송 (ACK)
                var ack = Encoding.UTF8.GetBytes("ACK: 수신 완료");
                await _udpClient.SendAsync(ack, ack.Length, result.RemoteEndPoint);
            }
        }

        public void Stop()
        {
            _udpClient.Close();
        }
    }
}
