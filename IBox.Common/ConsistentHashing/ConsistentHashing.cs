using IBox.Common.Objects;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace IBox.Common.ConsistentHashing
{
    public static class ConsistentHashing
    {
        private static readonly SortedDictionary<int, string> serverRing = new SortedDictionary<int, string>();
        private static readonly SortedDictionary<int, string> chatbotdbs = new SortedDictionary<int, string>();

        public static void AddListServerVirtual(List<string> servers, int numberOfVirtualNodes = 100)
        {
            try
            {
                foreach (var server in servers)
                {
                    for (int i = 0; i < numberOfVirtualNodes; i++)
                    {
                        string virtualNode = server + "#" + i;
                        int hash = GetHash(virtualNode);
                        if (serverRing.Count < 1000)
                        {
                            serverRing[hash] = server;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                new IboxLog($"AddListServerVirtual: {ex.Message}", "AppLogs", "Error");
            }
        }
        public static void AddListChatBotDBVirtual(List<string> chatbotdbs1, int numberOfVirtualNodes = 100)
        {
            try
            {
                foreach (var chatbot in chatbotdbs1)
                {
                    for (int i = 0; i < numberOfVirtualNodes; i++)
                    {
                        string virtualNode = chatbot + "#" + i;
                        int hash = GetHash(virtualNode);
                        if (chatbotdbs.Count < 1000)
                        {
                            chatbotdbs[hash] = chatbot;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                new IboxLog($"AddListServerVirtual: {ex.Message}", "AppLogs", "Error");
            }
        }

        private static int GetHash(string key)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
                int hash = BitConverter.ToInt32(hashBytes, 0);
                return Math.Abs(hash);
            }
        }

        public static List<string> GetServers(string customerId)
        {
            int hash = GetHash(customerId);
            string primary = GetServerForHash(hash);

            string secondary = GetSecondaryServer(hash);

            return new List<string>
            {
                primary,
                secondary
            };
        }

        private static string GetSecondaryServer(int hash)
        {
            var node = serverRing.FirstOrDefault(x => x.Key > hash);
            if (node.Key == 0)
            {
                return serverRing.First().Value;
            }
            return node.Value;
        }
        private static string GetServerForHash(int hash)
        {
            var node = serverRing.FirstOrDefault(x => x.Key >= hash);
            if (node.Key == 0)
            {
                return serverRing.First().Value;
            }
            return node.Value;
        }

        public static string GetChatBotDB(string customerId)
        {
            int hash = GetHash(customerId);
            string primary = GetChatBotForHash(hash);
            return primary;
        }

        private static string GetChatBotForHash(int hash)
        {
            var node = chatbotdbs.FirstOrDefault(x => x.Key >= hash);
            if (node.Key == 0)
            {
                return chatbotdbs.First().Value;
            }
            return node.Value;
        }
    }
}
