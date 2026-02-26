using IBox.ChatBot.DB.Models.ChatBot;
using IBox.Database.Root;
using IBox.Database.Tenant;
using Microsoft.Extensions.Caching.Memory;
using Renci.SshNet;
using Serilog;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace IBox.ChatBot.DB.DBChatDay
{
    public static class SessionChatBot
    {
        private static readonly MemoryCache _cache = new(new MemoryCacheOptions
        {
            SizeLimit = 100000
        });

        public static void AddOrUpdateSession(string senderId, SessionObj sessionObj, int EndChatTime)
        {
            if (string.IsNullOrWhiteSpace(senderId) || sessionObj == null)
                return;

            var cacheEntryOptions = new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromHours(EndChatTime),
                Size = 1
            };

            _cache.Set(senderId, sessionObj, cacheEntryOptions);
        }
       
        public static SessionObj? GetSession(string senderId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(senderId))
                    return null;

                _cache.TryGetValue(senderId, out SessionObj? session);
                return session;
            }
            catch (Exception ex)
            {
                Log.Error($"GetSession: {ex.Message}");
                return null;
            }
        }

        public static void RemoveSession(string senderId)
        {
            if (!string.IsNullOrWhiteSpace(senderId))
                _cache.Remove(senderId);
        }

        public static bool HasAnySession()
        {
            return _cache.Count > 0;
        }
    }
}
