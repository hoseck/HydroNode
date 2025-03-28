﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace HydroNode.Helpers
{
    /*
    NECK 패킷 구성 규격 (총 9바이트)
    ──────────────────────────────────────────────
    | 항목         | 크기       | 설명                        |
    |--------------|------------|-----------------------------|
    | HEADER       | 1 byte     | 패킷 시작 표시 (0x02)       |
    | VERSION      | 1 byte     | 프로토콜 버전 (0x01)        |
    | LENGTH       | 4 byte     | HEADER부터 TAIL까지 총 길이 |
    | PACKET-TYPE  | 1 byte     | 패킷 종류 (0x01 ~ 0xFF)     |
    | DATA-COUNT   | 1 byte     | 데이터 개수 (0x00 ~ 0xFF)   |
    | PAYLOAD      | 사용 안 함 |                             |
    | TAIL         | 1 byte     | 패킷 종료 표시 (0x03)       |
    ──────────────────────────────────────────────
    ※ 총 바이트 수 = 1(HEADER) + 1(VERSION) + 4(LENGTH) + 1(PACKET-TYPE) + 1(DATA-COUNT) + 1(TAIL) = 9 bytes
    */

    public static class AckPacketSender
    {
        public static byte[] CreateAckPacket()
        {
            byte header = 0x02;
            byte version = 0x28;
            byte tail = 0x03;

            int totalLength = 9;

            using MemoryStream ms = new();
            using BinaryWriter writer = new(ms);

            writer.Write(header); // HEADER
            writer.Write(version); // VERSION
            writer.Write(BitConverter.GetBytes(totalLength)); // LENGTH (Little-endian)
            writer.Write(0x81); // PACKET-TYPE 0x81은 이미지 전송시
            writer.Write(0x00); // DATA-COUNT
            writer.Write(tail); // TAIL

            return ms.ToArray();
        }

        public static async Task SendAckAsync(NetworkStream stream)
        {
            var ack = new byte[] { 0x02, 0x01, 0x00, 0x00, 0x00, 0x09, 0x81, 0x00, 0x03 }; // ACK 
            await stream.WriteAsync(ack, 0, ack.Length);
        }

        public static async Task SendNekAsync(NetworkStream stream)
        {
            var nek = new byte[] { 0x02, 0x01, 0x00, 0x00, 0x00, 0x09, 0xF1, 0x00, 0x03 }; // NEK 
            await stream.WriteAsync(nek, 0, nek.Length);
        }
    }
}
