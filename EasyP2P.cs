using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;

public class P2PNode : IDisposable
{
    Socket newSocket => new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    Socket thisSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    Dictionary<int, Socket> nodes = new Dictionary<int, Socket>();
    INode thisNode;
    Socket nullSocket;
    byte[] nullBytes;
    public bool showLog;
    public int id;    //|1024-49151|=48127
    IPEndPoint ie;
    public static bool IdOutOfRange(int id)
    {
        return id >= 1024 && id <= 49151;
    }
    public void Initialize(IPAddress pi, INode node = null)
    {
        thisNode = node;
        ie = new IPEndPoint(pi, 1024);
        nullBytes = new byte[1024];
        Task.Run(Private_Initialize);
    }

    void Private_Initialize()
    {
        bool flag = true;
        while (flag && IdOutOfRange(ie.Port))
        {
            try
            {
                thisSocket.Bind(ie);
                id = ie.Port;
                ShowLog($"Your id is {ie.Port}");
                flag = false;
                thisSocket.Listen(10);
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
            {
                nodes.Add(ie.Port, newSocket);
                ie.Port += 1;
            }
        }

        foreach (var item in nodes)
        {
            ie.Port = item.Key;
            item.Value.Connect(ie);
            item.Value.Send(BitConverter.GetBytes(id));
            Listen(item.Key, item.Value);
            ShowLog($"Id:{item.Key} is connnecting!");
        }

        while (true)
        {
            Update();
        }
    }

    public void Update()
    {
        Console.WriteLine("C!");
        nullSocket = thisSocket.Accept();
        nullBytes = new byte[1024];
        int b = nullSocket.Receive(nullBytes);
        if (nullSocket == null || !nullSocket.Connected || b == 0)
        {
            nullSocket?.Close();
            nullSocket?.Dispose();
            Console.WriteLine("A!");
        }
        else
        {
            Console.WriteLine("B!");
            byte[] actualData = new byte[b];
            Array.Copy(nullBytes, 0, actualData, 0, b);
            int a = BitConverter.ToInt32(actualData, 0);
            if (nodes.ContainsKey(a) && !nodes[a].Connected)
            {
                nodes[a]?.Close();
                nodes[a]?.Dispose();
                nodes[a] = nullSocket;
                ShowLog($"Id:{a} was lost,connecting new one!");
            }
            else
            {
                nodes.Add(a, nullSocket);
                ShowLog($"Id:{a} is NEW connnecting!");
            }
            Listen(a, nullSocket);
        }
        thisSocket.Listen(1);
    }

    void ShowLog(string s)
    {
        if (showLog)
        {
            Console.WriteLine(s);
        }
    }

    async void Listen(int id, Socket node)
    {
        if (thisNode != null && thisNode.ShouldListening(id, node))
        {
            try
            {
                nullBytes = new byte[1024];
                while (node.Connected)
                {
                    int a = node.Receive(nullBytes);
                    if (a == 0)
                    {
                        continue;
                    }
                    byte[] actualData = new byte[a];
                    Array.Copy(nullBytes, 0, actualData, 0, a);
                    thisNode.OnReceive(id, node, actualData);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Listen error: {ex.Message}");
                node.Dispose();
                nodes.Remove(id);
            }
        }
    }
    public bool Send(int id, byte[] msg)
    {
        if (!nodes.ContainsKey(id))
        {
            return false;
        }
        Socket socket = nodes[id];
        if (!socket.Connected)
        {
            return false;
        }
        socket.Send(msg);
        return true;
    }

    public async Task SendGlobal(byte[] msg)
    {
        var tasks = new List<Task>();
        foreach (var item in nodes.Values.ToList())
        {
            if (item.Connected)
            {
                tasks.Add(Send(item, msg));
            }
        }

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SendGlobal error: {ex.Message}");
        }
    }

    async Task Send(Socket socket, byte[] msg)
    {
        socket.Send(msg);
    }

    public void Dispose()
    {
        foreach (Socket s in nodes.Values)
        {
            try { s.Close(); } catch { }
        }
        nodes.Clear();

        if (thisSocket != null)
        {
            try { thisSocket.Close(); } catch { }
        }
    }
}
public interface INode
{
    bool ShouldListening(int id, Socket node);
    void OnReceive(int id, Socket node, byte[] msg);
}