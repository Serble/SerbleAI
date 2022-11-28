namespace SerbleAi.Data.Schemas; 

public struct StoredUser {
    public string Id;
    public string SessionId;
    public string RefreshToken;
    public int UsedTokens;
    public long LastTokenRenewal;
}