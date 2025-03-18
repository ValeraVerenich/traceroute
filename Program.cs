using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;

class Tracert
{
    private const int timeout = 1000; 
    private const int maxHops = 30;    
    private const int attempts = 3;    

    static void Main(string[] args)
    {
        Console.Write("Введите IP-адрес или доменное имя: ");
        string host = Console.ReadLine(); 

        IPAddress ipAddress;

        try
        {
            
            ipAddress = Dns.GetHostAddresses(host)[0];
        }
        catch (Exception)
        {
            Console.WriteLine($"Не удалось разрешить имя хоста или IP-адрес: {host}");
            return;
        }

        Console.WriteLine($"Трассировка маршрута к {host} [{ipAddress}]");
        Console.WriteLine($"с максимальным числом прыжков {maxHops}:");

        using (Ping ping = new Ping())
        {
            PingOptions pingOptions = new PingOptions(1, true); 

            for (int ttl = 1; ttl <= maxHops; ttl++)
            {
                pingOptions.Ttl = ttl;

                Console.Write($"{ttl}\t");

                long[] roundTripTimes = new long[attempts]; 
                IPAddress replyAddress = null; 
                string hostName = "неизвестно"; 

                for (int attempt = 0; attempt < attempts; attempt++)
                {
                    try
                    {
                        
                        PingReply reply = ping.Send(ipAddress, timeout, new byte[32], pingOptions);

                        if (reply.Status == IPStatus.Success || reply.Status == IPStatus.TtlExpired)
                        {
                            
                            roundTripTimes[attempt] = reply.RoundtripTime < 1 ? 1 : reply.RoundtripTime;
                            replyAddress = reply.Address;
                            hostName = GetHostName(reply.Address); 
                        }
                        else
                        {
                            roundTripTimes[attempt] = -1; 
                        }
                    }
                    catch (PingException)
                    {
                        roundTripTimes[attempt] = -1; 
                    }
                    catch (Exception)
                    {
                        roundTripTimes[attempt] = -1; 
                    }
                }

                
                foreach (var time in roundTripTimes)
                {
                    Console.Write(time == -1 ? "*\t" : $"{time} ms\t");
                }

                
                if (replyAddress != null)
                {
                    Console.Write($"{replyAddress}\t{hostName}");
                }
                else
                {
                    Console.Write("*\tнеизвестно");
                }

                Console.WriteLine(); 

               
                if (replyAddress != null && replyAddress.Equals(ipAddress))
                {
                    Console.WriteLine("Трассировка завершена: достигнут целевой узел.");
                    break;
                }
            }
        }
    }

    
    private static string GetHostName(IPAddress ipAddress)
    {
        try
        {
            IPHostEntry hostEntry = Dns.GetHostEntry(ipAddress);
            return hostEntry.HostName;
        }
        catch (Exception)
        {
            return "неизвестно";
        }
    }
}