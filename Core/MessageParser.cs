using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/*
TX 패킷 구성 (가변 길이)
──────────────────────────────────────────
| 항목         | 크기       | 설명                                      |
|--------------|------------|-------------------------------------------|
| HEADER       | 1 byte     | 패킷 시작 바이트 (0x02)                   |
| VERSION      | 1 byte     | 프로토콜 버전 (0x01)                      |
| LENGTH       | 4 byte     | HEADER부터 TAIL까지 총 패킷 길이          |
| PACKET-TYPE  | 1 byte     | 패킷 타입 (0x01 ~ 0xFF)                   |
| DATA-COUNT   | 1 byte     | 데이터 필드 개수                          |
| PAYLOAD      | 26~1204 byte | 실제 데이터 영역 (DATA-COUNT 수만큼 반복) |
| TAIL         | 1 byte     | 패킷 종료 바이트 (0x03)                   |
──────────────────────────────────────────

※ PAYLOAD 상세 구성 (DATA-COUNT 수 만큼 반복):
──────────────────────────────────────────
| DEVADDR      | 12 byte    | 장비 ID (ASCII)                           |
| DATE         | 7 byte     | 데이터 수집 시간 ("yyMMddHHmmss")         |
| USAGE        | 1 byte     | 사용 여부 또는 상태                       |
| SEPARATE1    | 1 byte     | 필드 구분자 (ASCII 문자)                  |
| SUB ID       | 1 byte     | 데이터 항목 식별자 (ASCII 문자)           |
| VALUE        | 4 byte     | float 데이터 값                           |
| …           | 반복       | → SEPARATEn + SUB ID + VALUE 반복 가능   |
──────────────────────────────────────────
*/
namespace HydroNode.Core
{
    public class WrmsPacketParser
    {
        public class SensorData
        {
            public byte Separate { get; set; }
            public byte SubId { get; set; }
            public float Value { get; set; }

            public override string ToString()
            {
                return $"SEPARATE=0x{Separate:X2}, SUB_ID={SubId}, VALUE={Value}";
            }
        }

        public class ParsedPacket
        {
            public byte Header { get; set; }
            public byte Version { get; set; }
            public int Length { get; set; }
            public byte PacketType { get; set; }
            public byte DataCount { get; set; }
            public string DevAddr { get; set; } = string.Empty;
            public string DateTime { get; set; } = string.Empty;
            public byte Usage { get; set; }
            public List<SensorData> DataList { get; set; } = new List<SensorData>();
            public byte Tail { get; set; }
        }

        public static ParsedPacket? Parse(byte[] buffer)
        {
            if (!IsValidPacket (buffer))
            {
                return null;
            }

            int offset = 0;

            var result = new ParsedPacket();

            // HEADER (1 byte)
            result.Header = buffer[offset++];

            // VERSION (1 byte)
            result.Version = buffer[offset++];

            // LENGTH (4 bytes, little endian)
            result.Length = BitConverter.ToInt32(buffer, offset);
            offset += 4;

            // PACKET TYPE (1 byte)
            result.PacketType = buffer[offset++];

            // DATA COUNT (1 byte)
            result.DataCount = buffer[offset++];

            // PAYLOAD 시작
            // DEVADDR (12 bytes)
            byte[] devAddrBytes = new byte[12];
            Array.Copy(buffer, offset, devAddrBytes, 0, 12);
            result.DevAddr = Encoding.ASCII.GetString(devAddrBytes.TakeWhile(b => b != 0x00).ToArray());
            offset += 12;

            // DATE (7 bytes)
            int year = buffer[offset++] | (buffer[offset++] << 8);
            int month = buffer[offset++];
            int day = buffer[offset++];
            int hour = buffer[offset++];
            int minute = buffer[offset++];
            int second = buffer[offset++];

            try
            {
                DateTime dateTime = new DateTime(year, month, day, hour, minute, second);
                result.DateTime = dateTime.ToString("yyyyMMddHHmmss");
            }
            catch
            {
                result.DateTime = "";
            }

            // USAGE (1 byte)
            result.Usage = buffer[offset++];

            // USAGE만큼 반복해서 데이터 파싱
            for (int i = 0; i < result.Usage; i++)
            {
                if (offset + 6 > buffer.Length - 1) break; // TAIL 남겨놓고 끝

                byte separate = buffer[offset++];
                byte subId = buffer[offset++];
                float value = BitConverter.ToSingle(buffer, offset);
                offset += 4;

                result.DataList.Add(new SensorData
                {
                    Separate = separate,
                    SubId = subId,
                    Value = value
                });
            }

            // TAIL (1 byte)
            result.Tail = buffer[offset++];

            return result;
        }

        public static bool TryParse(byte[] buffer, out WrmsPacketParser.ParsedPacket? parsed, out int usedBytes)
        {
            parsed = null;
            usedBytes = 0;

            int length = BitConverter.ToInt32(buffer, 2);

            // 전체 바이트 중 정상 패킷 하나 존재함
            var packetBytes = buffer.Take(length).ToArray();
            parsed = WrmsPacketParser.Parse(packetBytes);
            usedBytes = length;

            return parsed != null;
        }

        public static bool IsValidPacket(byte[] packet)
        {
            if (packet == null || packet.Length < 35)
                return false;

            // 1. HEADER 체크
            if (packet[0] != 0x02)
                return false;

            // 2. TAIL 체크
            if (packet[packet[2] - 1]!= 0x03)
                return false;

            // 3. LENGTH 확인 (4바이트, BigEndian 가정)
            //int lengthFromPacket = (packet[2] << 24) | (packet[3] << 16) | (packet[4] << 8) | packet[5];
            //if (lengthFromPacket != packet.Length)
            //    return false;

            return true;
        }
    }
}
