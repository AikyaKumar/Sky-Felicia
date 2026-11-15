#if !UNITY_WEBGL || UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace OrbitalFrontier.Core
{
    // Local TCP transport. The host alone ticks GameSession; clients send compact command lines.
    public sealed class LanLink : IDisposable
    {
        const int MaxLine = 128 * 1024;
        const int MaxCommandsPerPoll = 24;
        const int MaxClients = 3;
        readonly byte[] buffer = new byte[8192];
        readonly List<Peer> clients = new List<Peer>();
        TcpListener listen;
        Peer host;
        TcpClient pending;
        IAsyncResult connection;
        double connectionAge;
        string lastError;

        public bool Hosting => listen != null;
        public bool Joining => pending != null;
        public bool Joined => host != null;
        public int CrewCount { get; private set; } = 1;
        public string Error => lastError;
        public int Seat { get; private set; }
        public int CrewMask { get; private set; } = 1;

        public void StartHost(int port)
        {
            Leave();
            try
            {
                listen = new TcpListener(IPAddress.Any, port);
                listen.Start(MaxClients + 1);
            }
            catch (SocketException e) { Leave(); lastError = e.Message; }
        }

        public void StartJoin(string address, int port)
        {
            Leave();
            try
            {
                pending = new TcpClient();
                connection = pending.BeginConnect(address, port, null, null);
            }
            catch (Exception e) when (e is SocketException || e is ArgumentException)
            {
                Leave();
                lastError = e.Message;
            }
        }

        public void Poll(double elapsed, Action<string> input, Action<string> snapshot) =>
            PollCrew(elapsed, (_, line) => input(line), snapshot);

        public void PollCrew(double elapsed, Action<int,string> input, Action<string> snapshot)
        {
            if (Joining)
            {
                connectionAge += elapsed;
                if (connection.IsCompleted)
                {
                    try
                    {
                        pending.EndConnect(connection);
                        host = new Peer(pending);
                        pending = null;
                    }
                    catch (SocketException e) { Leave(); lastError = e.Message; }
                }
                else if (connectionAge >= 7)
                {
                    Leave();
                    lastError = "Connection timed out.";
                }
            }

            if (Hosting)
            {
                for (int request = 0; request < MaxClients + 1 && listen.Pending(); request++)
                {
                    var peer = new Peer(listen.AcceptTcpClient());
                    if (clients.Count == MaxClients)
                    {
                        peer.Queue("ERR room is full");
                        try { peer.Flush(); }
                        catch (SocketException) { }
                        finally { peer.Dispose(); }
                        continue;
                    }
                    for (int seat = 1; seat <= MaxClients; seat++)
                        if (!clients.Exists(other => other.Seat == seat))
                        {
                            peer.Seat = seat;
                            break;
                        }
                    clients.Add(peer);
                    peer.Queue("SEAT " + peer.Seat, false);
                    try { peer.Flush(); } catch (SocketException) { }
                }
                for (int i = clients.Count - 1; i >= 0; i--)
                {
                    Peer peer = clients[i];
                    if (peer.Poll(buffer, line => { if (line.StartsWith("CMD ", StringComparison.Ordinal)) input(peer.Seat, line.Substring(4)); }))
                        continue;
                    peer.Dispose();
                    clients.RemoveAt(i);
                }
                CrewCount = 1 + clients.Count;
                CrewMask = 1;
                foreach (Peer peer in clients) CrewMask |= 1 << peer.Seat;
            }
            if (Joined)
            {
                string failure = null;
                if (!host.Poll(buffer, line => {
                    if (line.StartsWith("STATE ", StringComparison.Ordinal))
                    {
                        int split = line.IndexOf(' ', 6);
                        if (split > 6 && int.TryParse(line.Substring(6, split - 6), out int count))
                        {
                            CrewCount = count;
                            snapshot(line.Substring(split + 1));
                        }
                    }
                    else if (line.StartsWith("SEAT ", StringComparison.Ordinal) && int.TryParse(line.Substring(5), out int seat))
                        Seat = seat;
                    else if (line.StartsWith("ERR ", StringComparison.Ordinal)) failure = line.Substring(4);
                }) || failure != null)
                {
                    Leave();
                    lastError = failure ?? "The host disconnected.";
                }
            }
        }

        public void SendCommand(string json)
        {
            if (host != null && !host.Queue("CMD " + json, false))
                lastError = "Connection congested. Try the command again.";
        }

        public void Broadcast(string json)
        {
            foreach (Peer peer in clients)
                peer.Queue("STATE " + CrewCount + " " + json);
        }

        public void Leave()
        {
            listen?.Stop();
            listen = null;
            foreach (Peer peer in clients) peer.Dispose();
            clients.Clear();
            host?.Dispose();
            host = null;
            pending?.Close();
            pending = null;
            connectionAge = 0;
            CrewCount = 1;
            CrewMask = 1;
            Seat = 0;
            lastError = null;
        }

        public void Dispose() => Leave();

        sealed class Peer : IDisposable
        {
            readonly TcpClient client;
            readonly Socket socket;
            readonly MemoryStream input = new MemoryStream();
            readonly List<byte[]> pending = new List<byte[]>();
            int sent;
            public int Seat { get; set; }

            public Peer(TcpClient client)
            {
                this.client = client;
                this.client.NoDelay = true;
                socket = client.Client;
                socket.Blocking = false;
            }

            public bool Queue(string line, bool latestOnly = true)
            {
                var bytes = Encoding.UTF8.GetBytes(line + "\n");
                if (bytes.Length > MaxLine) return false;
                if (latestOnly && pending.Count > 0)
                {
                    if (sent == 0) pending.Clear();
                    else if (pending.Count > 1) pending.RemoveAt(1);
                }
                if (pending.Count >= 48) return false;
                pending.Add(bytes);
                return true;
            }

            public void Flush()
            {
                for (int i = 0; i < 4 && pending.Count > 0; i++)
                {
                    var bytes = pending[0];
                    int result = socket.Send(bytes, sent, bytes.Length - sent, SocketFlags.None, out var error);
                    if (error == SocketError.WouldBlock || error == SocketError.IOPending) return;
                    if (error != SocketError.Success) throw new SocketException((int)error);
                    sent += result;
                    if (sent == bytes.Length)
                    {
                        pending.RemoveAt(0);
                        sent = 0;
                    }
                }
            }

            public bool Poll(byte[] buffer, Action<string> onLine)
            {
                try
                {
                    // Deliver any buffered rejection notice before reacting to EOF.
                    int messages = 0;
                    for (int read = 0; read < 12 && socket.Poll(0, SelectMode.SelectRead); read++)
                    {
                        int count = socket.Receive(buffer, 0, buffer.Length, SocketFlags.None, out var error);
                        if (error == SocketError.WouldBlock) break;
                        if (error != SocketError.Success || count == 0) return false;
                        for (int i = 0; i < count; i++)
                        {
                            byte b = buffer[i];
                            if (b == 10)
                            {
                                if (++messages > MaxCommandsPerPoll) return false;
                                onLine(Encoding.UTF8.GetString(input.GetBuffer(), 0, (int)input.Length));
                                input.SetLength(0);
                            }
                            else
                            {
                                input.WriteByte(b);
                                if (input.Length > MaxLine) return false;
                            }
                        }
                    }
                    Flush();
                    return true;
                }
                catch (SocketException) { return false; }
                catch (ObjectDisposedException) { return false; }
            }

            public void Dispose() { client.Close(); input.Dispose(); }
        }
    }
}
#endif
