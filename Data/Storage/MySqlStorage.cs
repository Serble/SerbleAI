using GeneralPurposeLib;
using MySql.Data.MySqlClient;
using SerbleAi.Data.Schemas;

namespace SerbleAi.Data.Storage;

public class MySqlStorage : IStorageManager {
    
    private string _connectionString = "";
    
    public void Init() {
        Logger.Info($"[MySQL] Connecting to {GlobalConfig.Config["MySQLHost"].Text} as {GlobalConfig.Config["MySQLUser"].Text}...");
        _connectionString = $"server={GlobalConfig.Config["MySQLHost"].Text};" +
                            $"userid={GlobalConfig.Config["MySQLUser"].Text};" +
                            $"password={GlobalConfig.Config["MySQLPassword"].Text};" +
                            $"database={GlobalConfig.Config["MySQLDatabase"].Text}";
        CreateTables().Wait();
        Logger.Info("Initialised MySQL");
    }
    
    private async Task CreateTables() {
        await SendMySqlStatement(@"CREATE TABLE IF NOT EXISTS users(
                           id VARCHAR(64) primary key,
                           session_id VARCHAR(64),
                           refresh_token VARCHAR(512),
                           used_tokens INT,
                           last_token_renewal BIGINT)");
    }
    
    private async Task SendMySqlStatement(string statement) {
        await MySqlHelper.ExecuteNonQueryAsync(_connectionString, statement);
    }

    public void Deinit() {
        Logger.Info("De-initialised MySQL");
    }

    public Task CreateUser(StoredUser user) {
        return SendMySqlStatement($"INSERT INTO users (id, session_id, refresh_token, used_tokens, last_token_renewal) VALUES ('{user.Id}', '{user.SessionId}', '{user.RefreshToken}', {user.UsedTokens}, {user.LastTokenRenewal})");
    }

    public Task UpdateUser(StoredUser user) {
        return SendMySqlStatement($"UPDATE users SET session_id = '{user.SessionId}', refresh_token = '{user.RefreshToken}', used_tokens = {user.UsedTokens}, last_token_renewal = {user.LastTokenRenewal} WHERE id = '{user.Id}'");
    }

    public Task DeleteUser(StoredUser user) {
        return SendMySqlStatement($"DELETE FROM users WHERE id = '{user.Id}'");
    }

    public async Task<StoredUser?> GetUser(string userId) {
        StoredUser? user;
        await using MySqlDataReader reader = await MySqlHelper.ExecuteReaderAsync(_connectionString, "SELECT * FROM users WHERE id=@id",
            new MySqlParameter("@id", userId));
        if (!reader.Read()) {
            user = null;
            return user;
        }
        user = new StoredUser {
            Id = reader.GetString("id"),
            SessionId = reader.GetString("session_id"),
            RefreshToken = reader.GetString("refresh_token"),
            UsedTokens = reader.GetInt32("used_tokens"),
            LastTokenRenewal = reader.GetInt64("last_token_renewal")
        };
        reader.Close();
        return user;
    }
    
}