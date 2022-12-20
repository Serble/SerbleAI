using GeneralPurposeLib;
using SerbleAi.Data.Storage;
using LogLevel = GeneralPurposeLib.LogLevel;

namespace SerbleAi; 

internal static class Program {
    
    private static readonly Dictionary<string, Property> DefaultConfig = new() {
        { "SerbleApiUrl", "https://api.serble.net/api/v1/" },
        { "OAuthUrl", "https://localhost:7244/oauth/authorize" },
        { "ClientID", "fe140ca4-9c47-4f66-8bcf-651e244d9040" },
        { "AppSecret", "7cbfe44f-9c47-8bcf-4f66-ffe709fdc023" },
        { "Scope", "user_info" },
        { "RecaptchaSiteKey", "ReCaptchaSiteKey" },
        { "StorageMethod", "MySQL" },
        { "MySQLHost", "mysql.serble.net" },
        { "MySQLUser", "admin" },
        { "MySQLPassword", "my complex password" },
        { "MySQLDatabase", "serbleai" },
        { "OpenAIToken", "fancytoken" },
        { "EnableWhitelist", false }
    };
    public static IStorageManager StorageManager { get; private set; } = null!;
    public static string[] Whitelist { get; private set; } = Array.Empty<string>();

    public static void Main(string[] args) {
        Logger.Init(LogLevel.Debug);
        
        Logger.Info("Loading config...");
        Config config = new(DefaultConfig);
        GlobalConfig.Init(config);
        Logger.Info("Config loaded!");
        
        // Whitelist
        if (GlobalConfig.Config["EnableWhitelist"]) {
            if (!File.Exists("whitelist.txt")) {
                Logger.Info("Whitelist file not found, only admins will be able to access site.");
            }
            else {
                Whitelist = File.ReadAllLines("whitelist.txt");
                Logger.Info("Whitelist loaded!");
            }
        }

        Logger.Info("Starting...");

        StorageManager = GlobalConfig.Config["StorageMethod"].Text.ToLower() switch {
            "mysql" => new MySqlStorage(),
            _ => throw new ArgumentException("Invalid storage method!")
        };
        StorageManager.Init();
        Logger.Info("Storage initialized");

        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddRazorPages();
        builder.Services.AddServerSideBlazor();

        WebApplication app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment()) {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseStaticFiles();

        app.UseRouting();

        app.MapBlazorHub();
        app.MapFallbackToPage("/_Host");

        app.Run();

        Logger.Info("Stopping");
        StorageManager.Deinit();
        Logger.Info("Stopped");
    }
    
}