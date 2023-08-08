using SerbleAi.Data.Schemas;

namespace SerbleAi.Data; 

public static class UserAiLimitManager {
    
    // For tokens, 1k costs 2c for me, so 1c = 500 tokens
    // Token limits are per user per month

    private const int DefaultLimit = 1000;  // Cost: 2c
    private const int LoggedInLimit = 1000;  // Cost: 2c
    private const int VerifiedEmailLimit = 5000;  // Cost: 10c
    private const int PremiumLimit = 50000;  // Cost: $1
    
    public static async Task<int> GetLimit(User? user, string token) {
        if (user == null) {
            return DefaultLimit;
        }
        if (await PremiumStatusCache.IsPremium(user, token)) {
            return PremiumLimit;
        }
        if (user.VerifiedEmail) {
            return VerifiedEmailLimit;
        }
        return LoggedInLimit;
    }
    
    public static async Task AddTokens(StoredUser user, int tokens) {
        await user.CheckTokens();
        user.UsedTokens += tokens;
        await Program.StorageManager.UpdateUser(user);
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="user"></param>
    /// <returns>True if the tokens reset otherwise false</returns>
    private static async Task<StoredUser> CheckForTokenRefresh(StoredUser user) {
        if (DateTime.FromBinary(user.LastTokenRenewal).AddDays(28) > DateTime.UtcNow) {
            return user;
        }
        user.UsedTokens = 0;
        user.LastTokenRenewal = DateTime.UtcNow.ToBinary();
        await Program.StorageManager.UpdateUser(user);
        return user;
    }

    public static async Task CheckTokens(this StoredUser user) {
        user = await CheckForTokenRefresh(user);
    }

    public static async Task<bool> CanUseAi(User? user, string token, int tokens) {
        if (user == null) {
            return false;
        }

        // Get the user's current AI usage
        StoredUser? storedUser = await Program.StorageManager.GetUser(user.Id!);
        if (storedUser == null) {
            return false;
        }

        await storedUser.Value.CheckTokens();

        // Get the user's current AI limit
        int limit = await GetLimit(user, token);
        
        // Check if the user can use AI
        return storedUser.Value.UsedTokens + tokens + AiManager.MaxTokensInResponse <= limit;
    }

    public static Task<bool> CanUseAi(User? user, string token, string prompt) => CanUseAi(user, token, prompt.Length / 4);

}