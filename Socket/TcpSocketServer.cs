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
            _logger.LogInformation("TCP 서버 시작됨 (6000)");

            while (!token.IsCancellationRequested)
            {
                var client = await _listener.AcceptTcpClientAsync(token);
                _logger.LogInformation("TCP 클라이언트 접속됨");

                _ = Task.Run(async () =>
                {
                    using var stream = client.GetStream();
                    var buffer = new byte[2048];
                    
                    //var buffer = new byte[] {
                    //    0x02, 0x01, 0x47, 0x00, 0x00, 0x00, 0x01, 0x01,   //TCP Header
                    //    0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x00, 0xfb, 0xe9, 0x07, 0x03, 0x18, 0x13, 0x28, 0x00, 0x07,
                    //    0x10,
                    //    0x01,
                    //    0x16, 0xea, 0xbc, 0x41,
                    //    0x16,
                    //    0x00,
                    //    0x00, 0x00, 0x00, 0x00,
                    //    0x06,
                    //    0x01,
                    //    0xe5, 0xd0, 0x22, 0x3e,
                    //    0x06,
                    //    0x02,
                    //    0x00, 0x00, 0x00, 0x00,
                    //    0x01,
                    //    0x01,
                    //    0xcd, 0xcc, 0x4c, 0x41,
                    //    0x01,
                    //    0x02,
                    //    0x00, 0x00, 0xc0, 0x3e,
                    //    0x45,
                    //    0x01,
                    //    0x00, 0x00, 0x00, 0x00,
                    //    0x03   //TCP Tail
                    //};
                    int bytesRead = await stream.ReadAsync(buffer, token);
                    //var received = buffer.Take(bytesRead).ToList();

                    //int start = received.IndexOf(0x02);
                    //int end = received.IndexOf(0x03, start + 1);

                    // var msg = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    // _logger.LogInformation($"[TCP] 수신: {buffer}");
                    //int length = end - start + 1;
                    //var packet = received.Skip(start).Take(length).ToArray();

                    var parsed = WrmsPacketParser.Parse(buffer);
                    // 수위
                    var sensor = FindSensorBySeparate(parsed.DataList, 0x10);
                    float waterLevel = sensor?.Value ?? 0f;
                    // 강우
                    sensor = FindSensorBySeparate(parsed.DataList, 0x16);
                    float rainFall = sensor?.Value ?? 0f;
                    await _db.InsertDataAsync(parsed.DateTime, parsed.DevAddr, waterLevel, rainFall);

                    await AckPacketSender.SendAckAsync(stream);

                    client.Close();
                }, token);
            }

            _listener.Stop();
        }

        private SensorData? FindSensorBySeparate(List<SensorData> sensors, byte separate)
        {
            return sensors.FirstOrDefault(s => s.Separate == separate);
        }
    }
}
