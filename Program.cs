using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Diagnostics;
using System.Threading;

class Tracert
{
    private const int timeout = 1000;
    private const int maxHops = 30;
    private const int attempts = 3;
    private const int packetSize = 32;

    static void Main(string[] args)
    {
        Console.Write("Введите IP-адрес или доменное имя: ");
        string host = Console.ReadLine();

        try
        {
            IPAddress targetAddress = Dns.GetHostAddresses(host)[0];
            Console.WriteLine($"Трассировка маршрута к {host} [{targetAddress}]");
            Console.WriteLine($"с максимальным числом прыжков {maxHops}:");

            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.Icmp))
            {
                socket.ReceiveTimeout = timeout;

                byte[] receiveBuffer = new byte[256];
                EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

                for (int ttl = 1; ttl <= maxHops; ttl++)
                {
                    socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.IpTimeToLive, ttl);
                    Console.Write($"{ttl}\t");

                    IPAddress lastAddress = null;
                    string hostName = "неизвестно";
                    bool destinationReached = false;

                    for (int attempt = 0; attempt < attempts; attempt++)
                    {
                        try
                        {
                           
                            byte[] packet = CreateIcmpEchoRequest(ttl);
                            Stopwatch sw = Stopwatch.StartNew();
                            socket.SendTo(packet, new IPEndPoint(targetAddress, 0));

                            int bytesReceived = socket.ReceiveFrom(receiveBuffer, ref remoteEP);
                            sw.Stop();

                            
                            if (bytesReceived >= 20)
                            {
                                IPAddress responseAddress = ((IPEndPoint)remoteEP).Address;
                                lastAddress = responseAddress;

                                
                                int icmpType = receiveBuffer[20];
                                if (icmpType == 11 || icmpType == 0)
                                {
                                    Console.Write($"{sw.ElapsedMilliseconds} ms\t");
                                    destinationReached = (icmpType == 0);
                                }
                                else
                                {
                                    Console.Write("*\t");
                                }
                            }
                            else
                            {
                                Console.Write("*\t");
                            }
                        }
                        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut)
                        {
                            Console.Write("*\t");
                        }
                        catch
                        {
                            Console.Write("*\t");
                        }

                        Thread.Sleep(100); 
                    }

                    
                    if (lastAddress != null)
                    {
                        try
                        {
                            hostName = Dns.GetHostEntry(lastAddress).HostName;
                        }
                        catch { }
                        Console.WriteLine($"{lastAddress}\t{hostName}");

                        if (destinationReached)
                        {
                            Console.WriteLine("Трассировка завершена.");
                            break;
                        }
                    }
                    else
                    {
                        Console.WriteLine("*\tнеизвестно");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
        }
    }

    private static byte[] CreateIcmpEchoRequest(int ttl)
    {
        
        byte[] packet = new byte[packetSize];
        packet[0] = 8;  
        packet[1] = 0;  
        packet[2] = 0; 
        packet[3] = 0;  
        packet[4] = 0;  
        packet[5] = 1;  
        packet[6] = 0;  
        packet[7] = 1;  

        
        for (int i = 8; i < packetSize; i++)
        {
            packet[i] = (byte)i;
        }

        
        ushort checksum = CalculateChecksum(packet);
        packet[2] = (byte)(checksum >> 8);
        packet[3] = (byte)(checksum & 0xFF);

        return packet;
    }

    private static ushort CalculateChecksum(byte[] buffer)
    {
        uint sum = 0;
        int length = buffer.Length;
        int i = 0;

        while (i < length - 1)
        {
            sum += (uint)((buffer[i] << 8) + buffer[i + 1]);
            i += 2;
        }

        if (i < length)
        {
            sum += (uint)(buffer[i] << 8);
        }

        sum = (sum >> 16) + (sum & 0xFFFF);
        sum += (sum >> 16);
        return (ushort)(~sum);
    }
}