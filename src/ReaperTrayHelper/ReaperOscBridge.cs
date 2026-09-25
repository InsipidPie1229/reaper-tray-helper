using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace ReaperTrayHelper
{
    internal sealed class ReaperOscResult
    {
        internal ReaperOscResult(string code, string value)
        {
            Code = code;
            Value = value;
        }

        internal string Code { get; private set; }
        internal string Value { get; private set; }
    }

    internal static class ReaperOscBridge
    {
        private const int TimeoutMilliseconds = 1600;
        private static readonly string BridgeDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ReaperTrayHelper", "bridge");

        internal static bool IsValidCommandId(string commandId)
        {
            return !String.IsNullOrWhiteSpace(commandId) && Regex.IsMatch(commandId.Trim(), "^_[A-Za-z0-9_-]{3,128}$");
        }

        internal static void Test(int port, string commandId)
        {
            ReaperOscResult result = Request(port, commandId, "PING", "");
            if (result.Code != "PING_OK")
                throw new InvalidOperationException("스크립트 응답: " + result.Code);
        }

        internal static ReaperOscResult ToggleTrack(int port, string commandId, string trackName)
        {
            return Request(port, commandId, "TOGGLE", trackName);
        }

        internal static byte[] BuildOscActionPacket(string commandId)
        {
            using (var stream = new MemoryStream())
            {
                WriteOscString(stream, "/action/str");
                WriteOscString(stream, ",s");
                WriteOscString(stream, commandId);
                return stream.ToArray();
            }
        }

        private static ReaperOscResult Request(int port, string commandId, string operation, string trackName)
        {
            if (port < 1024 || port > 65535) throw new ArgumentOutOfRangeException("port");
            if (!IsValidCommandId(commandId)) throw new ArgumentException("잘못된 ReaScript 명령 ID입니다.", "commandId");

            Directory.CreateDirectory(BridgeDirectory);
            string id = Guid.NewGuid().ToString("N");
            string requestPath = Path.Combine(BridgeDirectory, id + ".req");
            string temporaryPath = requestPath + ".tmp";
            string responsePath = Path.Combine(BridgeDirectory, id + ".res");
            long expires = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 3;
            string request = operation + "\n" + expires + "\n" + (trackName ?? "");
            File.WriteAllText(temporaryPath, request, new UTF8Encoding(false));
            File.Move(temporaryPath, requestPath);

            try
            {
                using (var udp = new UdpClient(AddressFamily.InterNetwork))
                {
                    byte[] packet = BuildOscActionPacket(commandId.Trim());
                    udp.Send(packet, packet.Length, new IPEndPoint(IPAddress.Loopback, port));
                }

                DateTime until = DateTime.UtcNow.AddMilliseconds(TimeoutMilliseconds);
                while (DateTime.UtcNow < until)
                {
                    if (File.Exists(responsePath))
                    {
                        string[] lines = File.ReadAllText(responsePath, Encoding.UTF8).Split(new[] { '\n' }, 2);
                        return new ReaperOscResult(lines[0].Trim(), lines.Length > 1 ? lines[1].Trim() : "");
                    }
                    Thread.Sleep(25);
                }
                return new ReaperOscResult("TIMEOUT", "");
            }
            finally
            {
                TryDelete(requestPath);
                TryDelete(temporaryPath);
                TryDelete(responsePath);
            }
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch { }
        }

        private static void WriteOscString(Stream stream, string value)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            stream.Write(bytes, 0, bytes.Length);
            stream.WriteByte(0);
            int padding = (4 - ((bytes.Length + 1) % 4)) % 4;
            for (int i = 0; i < padding; i++) stream.WriteByte(0);
        }
    }
}
