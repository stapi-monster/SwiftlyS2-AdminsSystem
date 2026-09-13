using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;

namespace Admins.Core.Flags;

public class FlagDefinition
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("access")]
    public Dictionary<string, string> Access { get; set; } = new();
}

public class FlagsConfig
{
    public Dictionary<string, FlagDefinition> Flags { get; set; } = new();
}

public class FlagsManager
{
    private readonly ISwiftlyCore _core;
    private FlagsConfig _flagsConfig = new();

    public FlagsManager(ISwiftlyCore core)
    {
        _core = core;
        LoadFlags();
    }

    public void LoadFlags()
    {
        try
        {
            _core.Configuration
                .InitializeWithTemplate("flags.jsonc", "flags.template.jsonc");

            var builder = new ConfigurationBuilder();
            builder.AddJsonFile("flags.jsonc", optional: true, reloadOnChange: true);
            var root = builder.Build();

            var config = new FlagsConfig();
            root.Bind(config);
            _flagsConfig = config;
        }
        catch (Exception ex)
        {
            _core.Logger.LogError(ex, "[Admins.Core] Ошибка загрузки файла конфигурации флагов (flags.jsonc)");
        }
    }

    public List<string> GetPermissionsForFlags(string flagsStr)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(flagsStr)) return result.ToList();

        string lowerFlags = flagsStr.ToLower();

        // 1. Проверка буквенных флагов из конфига
        foreach (char ch in lowerFlags)
        {
            string flagKey = ch.ToString();
            if (_flagsConfig.Flags.TryGetValue(flagKey, out var flagDef))
            {
                foreach (var kvp in flagDef.Access)
                {
                    if (kvp.Value == "1" || kvp.Value.Equals("true", StringComparison.OrdinalIgnoreCase))
                    {
                        result.Add(kvp.Key);
                    }
                }
            }

            // 2. Встроенный стандартный фолбэк-маппинг для базовых буквенных флагов ('a'-'z')
            switch (ch)
            {
                case 'a':
                    result.Add("@admin/reserve");
                    break;
                case 'b':
                    result.Add("admins.commands.ban");
                    result.Add("admins.commands.unban");
                    result.Add("admins.commands.kick");
                    result.Add("admins.menu.bans");
                    break;
                case 'c':
                    result.Add("admins.commands.kick");
                    break;
                case 'd':
                    result.Add("admins.commands.ban");
                    result.Add("admins.commands.unban");
                    result.Add("admins.menu.bans");
                    break;
                case 'e':
                    result.Add("admins.commands.mute");
                    result.Add("admins.commands.gag");
                    result.Add("admins.commands.silence");
                    result.Add("admins.commands.unmute");
                    result.Add("admins.commands.ungag");
                    result.Add("admins.commands.unsilence");
                    result.Add("admins.menu.comms");
                    break;
                case 'f':
                    result.Add("admins.commands.noclip");
                    result.Add("admins.commands.slap");
                    result.Add("admins.commands.slay");
                    result.Add("admins.commands.hide");
                    result.Add("@admin/hide");
                    result.Add("admins.commands.esp");
                    result.Add("@admin/esp");
                    break;
                case 'g':
                    result.Add("admins.commands.hp");
                    result.Add("admins.commands.god");
                    result.Add("admins.commands.goto");
                    result.Add("admins.commands.bring");
                    result.Add("admins.commands.respawn");
                    break;
                case 'h':
                    result.Add("admins.commands.vote");
                    result.Add("admins.commands.votekick");
                    result.Add("admins.commands.votemap");
                    break;
                case 'l':
                case 'r':
                case 'm':
                    result.Add("admins.commands.rcon");
                    result.Add("@admin/rcon");
                    break;
                case 'k':
                    result.Add("admins.commands.check");
                    result.Add("@admin/check");
                    break;
                case 'o':
                    result.Add("admins.commands.reports");
                    result.Add("@admin/reports");
                    break;
                case 'z':
                    result.Add("admins.commands.*");
                    result.Add("admins.menu.bans");
                    result.Add("admins.menu.comms");
                    result.Add("@admin/own_reason");
                    result.Add("@admin/rcon");
                    result.Add("@admin/hide");
                    result.Add("@admin/check");
                    result.Add("@admin/reports");
                    result.Add("@admin/esp");
                    break;
            }
        }

        // 3. Если в строке напрямую переданы ноды прав через точку/запятую
        if (flagsStr.Contains('.'))
        {
            var parts = flagsStr.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var part in parts)
            {
                if (part.Contains('.'))
                {
                    result.Add(part);
                }
            }
        }

        // 4. Любой администратор с любыми флагами получает доступ к вызову меню !admin
        if (result.Count > 0)
        {
            result.Add("admins.commands.admin");
        }

        // 5. Если выдан вайлдкард admins.commands.* или флаг 'z', автоматически разворачиваем его во все конкретные права
        if (result.Contains("admins.commands.*") || lowerFlags.Contains('z'))
        {
            result.Add("admins.commands.admin");
            result.Add("admins.commands.ban");
            result.Add("admins.commands.unban");
            result.Add("admins.commands.kick");
            result.Add("admins.commands.mute");
            result.Add("admins.commands.gag");
            result.Add("admins.commands.silence");
            result.Add("admins.commands.unmute");
            result.Add("admins.commands.ungag");
            result.Add("admins.commands.unsilence");
            result.Add("admins.commands.noclip");
            result.Add("admins.commands.slap");
            result.Add("admins.commands.slay");
            result.Add("admins.commands.hp");
            result.Add("admins.commands.god");
            result.Add("admins.commands.goto");
            result.Add("admins.commands.bring");
            result.Add("admins.commands.respawn");
            result.Add("admins.commands.vote");
            result.Add("admins.commands.votekick");
            result.Add("admins.commands.votemap");
            result.Add("admins.commands.givemoney");
            result.Add("admins.commands.givecash");
            result.Add("admins.commands.setmoney");
            result.Add("admins.commands.adminlog");
            result.Add("admins.commands.rcon");
            result.Add("admins.commands.hide");
            result.Add("@admin/hide");
            result.Add("admins.commands.check");
            result.Add("@admin/check");
            result.Add("admins.menu.bans");
            result.Add("admins.menu.comms");
        }

        if (result.Contains("admins.commands.ban") || result.Contains("admins.commands.kick"))
        {
            result.Add("admins.menu.bans");
        }

        if (result.Contains("admins.commands.mute") || result.Contains("admins.commands.gag") || result.Contains("admins.commands.silence"))
        {
            result.Add("admins.menu.comms");
        }

        return result.ToList();
    }
}
