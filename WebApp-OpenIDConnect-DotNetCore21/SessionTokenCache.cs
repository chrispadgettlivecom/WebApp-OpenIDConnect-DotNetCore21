using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.Identity.Client;

namespace WebApp_OpenIDConnect_DotNetCore21
{
    public class SessionTokenCache
    {
        private static ReaderWriterLockSlim TokenCacheLock;

        private readonly HttpContext _context;
        private readonly string _sessionKey;
        private readonly TokenCache _tokenCache;
        private readonly string _userId;

        static SessionTokenCache()
        {
            TokenCacheLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        }

        public SessionTokenCache(HttpContext context, string userId)
        {
            _context = context;
            _userId = userId;
            _sessionKey = userId + "_TokenCache";
            _tokenCache = new TokenCache();
        }

        public TokenCache GetInstance()
        {
            _tokenCache.SetBeforeAccess(OnBeforeAccess);
            _tokenCache.SetAfterAccess(OnAfterAccess);
            Load();
            return _tokenCache;
        }

        public void Load()
        {
            TokenCacheLock.EnterReadLock();
            _tokenCache.Deserialize(_context.Session.Get(_sessionKey));
            TokenCacheLock.ExitReadLock();
        }

        public void Save()
        {
            TokenCacheLock.EnterWriteLock();
            _tokenCache.HasStateChanged = false;
            _context.Session.Set(_sessionKey, _tokenCache.Serialize());
            TokenCacheLock.ExitWriteLock();
        }

        private void OnAfterAccess(TokenCacheNotificationArgs args)
        {
            if (_tokenCache.HasStateChanged)
            {
                Save();
            }
        }

        private void OnBeforeAccess(TokenCacheNotificationArgs args)
        {
            Load();
        }
    }
}
