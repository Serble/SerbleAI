using System.Collections.Concurrent;
using SerbleAi.Data.Schemas;

namespace SerbleAi.Data; 

public static class PremiumStatusCache {
    private static readonly ConcurrentDictionary<string, (DateTime, bool)> Cache = new();

    public static async Task<bool> IsPremium(User user, string? token) {
        if (token == null) {
            return false;
        }
        if (Cache.ContainsKey(user.Id!)) {
            if (Cache[user.Id!].Item1.AddMinutes(5) >= DateTime.UtcNow) return Cache[user.Id!].Item2;
            Cache.TryRemove(user.Id!, out _);
            return await IsPremium(user, token);
        }

        SerbleApiResponse<string[]> ownedProducts = await SerbleApiHandler.GetUsersOwnedProducts(token);
        bool premium = ownedProducts.Success && ownedProducts.ResponseObject!.Contains("premium");
        Cache[user.Id!] = (DateTime.UtcNow, premium);
        return premium;
    }
    
}