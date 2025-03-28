using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using HydroNode.Core;
using System.Diagnostics.CodeAnalysis;

using HydroNode.Helpers;
using static HydroNode.Core.WrmsPacketParser;

namespace HydroNode.Socket
{
    public class TcpSocketServer
    {
        private readonly ILogger _logger;
        private readonly DatabaseService _db;
        private readonly IConfiguration _config;
        private readonly TcpListener _listener;

        public TcpSocketServer(ILogger logger, DatabaseService db, IConfiguration config)
        {
            _logger = logger;
            _db = db;
            _config = config;

            int tcpPort = _config.GetValue<int>("Settings:Tcp:Port", 1993);
            _listener = new TcpListener(IPAddress.Any, tcpPort);
        }

        public async Task StartAsync(CancellationToken token)
        {
            _listener.Start();
            _logger.LogInformation("TCP 서버 시작됨 (1993)");

            while (!token.IsCancellationRequested)
            {
                try
                {
                    var client = await _listener.AcceptTcpClientAsync(token);
                    _logger.LogInformation($"TCP 클라이언트 접속됨: {client.Client.RemoteEndPoint}");

                    _ = HandleClientAsync(client, token);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "클라이언트 수락 중 예외 발생");
                }
            }

            _listener.Stop();
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken token)
        {
            using var stream = client.GetStream();
            var buffer = new byte[4096];
            var received = new List<byte>();

            try
            {
                while (!token.IsCancellationRequested && client.Connected)
                {
                    int bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), token);
                    if (bytesRead == 0)
                    {
                        _logger.LogInformation($"클라이언트 연결 종료: {client.Client.RemoteEndPoint}");
                        break;
                    }

                    received.AddRange(buffer.Take(bytesRead));

                    // 수신된 데이터에서 가능한 모든 패킷 파싱
                    while (WrmsPacketParser.TryParse(received.ToArray(), out var parsedPacket, out int usedBytes))
                    {
                        received.RemoveRange(0, usedBytes);

                        var sensor = FindSensorBySeparate(parsedPacket.DataList, 0x10); // 수위
                        float waterLevel = sensor?.Value ?? 0f;
                        sensor = FindSensorBySeparate(parsedPacket.DataList, 0x16);    // 강우
                        float rainFall = sensor?.Value ?? 0f;

                        await _db.InsertDataAsync(parsedPacket.DateTime, parsedPacket.DevAddr, waterLevel, rainFall);
                        await AckPacketSender.SendAckAsync(stream);
                    }

                    // TryParse 실패 시, 비정상 패킷에 대해 NEK 전송 (조건: 최소 길이 만족)
                    if (received.Count > 20 && !WrmsPacketParser.IsValidPacket(received.ToArray()))
                    {
                        _logger.LogWarning("비정상 패킷 수신 → NEK 전송");
                        await AckPacketSender.SendNekAsync(stream);

                        // 비정상 패킷 버림 (또는 일부 제거)
                        received.Clear();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"클라이언트 처리 중 예외 발생: {client.Client?.RemoteEndPoint}");
            }
            finally
            {
                client.Close();
                _logger.LogInformation($"클라이언트 연결 해제됨: {client.Client?.RemoteEndPoint}");
            }
        }

        private SensorData? FindSensorBySeparate(List<SensorData> sensors, byte separate)
        {
            return sensors.FirstOrDefault(s => s.Separate == separate);
        }
    }
}
