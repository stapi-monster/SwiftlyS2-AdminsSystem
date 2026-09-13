using Dapper;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;

namespace Admins.CheckCheats.Database;

public class CheckStatsRecord
{
    public int id { get; set; }
    public int server_id { get; set; }
    public string player_steamid { get; set; } = string.Empty;
    public string player_name { get; set; } = string.Empty;
    public string admin_steamid { get; set; } = string.Empty;
    public string admin_name { get; set; } = string.Empty;
    public long datestart { get; set; }
    public long date_end { get; set; }
    public string verdict { get; set; } = string.Empty;
    public string suspect_discord { get; set; } = string.Empty;
}

public class CheckCheatsDb
{
    private readonly ISwiftlyCore Core;

    public CheckCheatsDb(ISwiftlyCore core)
    {
        Core = core;
    }

    public async Task InitializeDatabaseAsync()
    {
        try
        {
            var db = Core.Database.GetConnection("admin_system");
            string sql = @"
                CREATE TABLE IF NOT EXISTS `checkcheats_stats` (
                    `id` INT AUTO_INCREMENT PRIMARY KEY,
                    `server_id` INT NOT NULL DEFAULT 1,
                    `player_steamid` VARCHAR(64) NOT NULL,
                    `player_name` VARCHAR(128) NOT NULL,
                    `admin_steamid` VARCHAR(64) NOT NULL,
                    `admin_name` VARCHAR(128) NOT NULL,
                    `datestart` BIGINT NOT NULL,
                    `date_end` BIGINT DEFAULT 0,
                    `verdict` VARCHAR(255) DEFAULT '',
                    `suspect_discord` VARCHAR(255) DEFAULT ''
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";

            await db.ExecuteAsync(sql);
            Core.Logger.LogInformation("[Admins.CheckCheats] Database table 'checkcheats_stats' verified/created.");
        }
        catch (Exception ex)
        {
            Core.Logger.LogError(ex, "[Admins.CheckCheats] Error initializing database table 'checkcheats_stats'.");
        }
    }

    public async Task<int> InsertCheckStartAsync(int serverId, string playerSteamId, string playerName, string adminSteamId, string adminName, long startTimestamp)
    {
        try
        {
            var db = Core.Database.GetConnection("admin_system");
            string sql = @"
                INSERT INTO `checkcheats_stats` (`server_id`, `player_steamid`, `player_name`, `admin_steamid`, `admin_name`, `datestart`, `date_end`, `verdict`, `suspect_discord`)
                VALUES (@ServerId, @PlayerSteamId, @PlayerName, @AdminSteamId, @AdminName, @DateStart, 0, '', '');
                SELECT LAST_INSERT_ID();";

            return await db.QuerySingleAsync<int>(sql, new
            {
                ServerId = serverId,
                PlayerSteamId = playerSteamId,
                PlayerName = playerName,
                AdminSteamId = adminSteamId,
                AdminName = adminName,
                DateStart = startTimestamp
            });
        }
        catch (Exception ex)
        {
            Core.Logger.LogError(ex, "[Admins.CheckCheats] Error inserting check start record into DB.");
            return 0;
        }
    }

    public async Task UpdateContactAsync(int dbRecordId, string contactInfo)
    {
        if (dbRecordId <= 0) return;
        try
        {
            var db = Core.Database.GetConnection("admin_system");
            string sql = "UPDATE `checkcheats_stats` SET `suspect_discord` = @Contact WHERE `id` = @Id;";
            await db.ExecuteAsync(sql, new { Contact = contactInfo, Id = dbRecordId });
        }
        catch (Exception ex)
        {
            Core.Logger.LogError(ex, "[Admins.CheckCheats] Error updating contact info in DB.");
        }
    }

    public async Task UpdateVerdictAsync(int dbRecordId, string verdict, long endTimestamp)
    {
        if (dbRecordId <= 0) return;
        try
        {
            var db = Core.Database.GetConnection("admin_system");
            string sql = "UPDATE `checkcheats_stats` SET `verdict` = @Verdict, `date_end` = @DateEnd WHERE `id` = @Id;";
            await db.ExecuteAsync(sql, new { Verdict = verdict, DateEnd = endTimestamp, Id = dbRecordId });
        }
        catch (Exception ex)
        {
            Core.Logger.LogError(ex, "[Admins.CheckCheats] Error updating verdict in DB.");
        }
    }
}
