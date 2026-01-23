// Jakar.Extensions :: Jakar.Database
// 06/02/2024  14:06

using Microsoft.AspNetCore.OutputCaching;



namespace Jakar.Database;


public static class MinApis
{
    extension( OutputCachePolicyBuilder self )
    {
        public void ExpireOneMinute() { self.Expire(TimeSpan.FromMinutes(1)); }
        public void ExpireOneMinute( params string[] queryKeys )
        {
            self.Expire(TimeSpan.FromMinutes(1))
                  .SetVaryByQuery(queryKeys);
        }
        public void ExpireFiveMinutes() { self.Expire(TimeSpan.FromMinutes(5)); }
        public void ExpireFiveMinutes( params string[] queryKeys )
        {
            self.Expire(TimeSpan.FromMinutes(5))
                  .SetVaryByQuery(queryKeys);
        }
    }
}
