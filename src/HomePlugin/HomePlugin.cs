using System;
using System.IO;
using PluginManager.Api;
using PluginManager.Api.Capabilities.Implementations.Commands;
using PluginManager.Api.Capabilities.Implementations.Translations;
using PluginManager.Api.Capabilities.Implementations.Utils;
using PluginManager.Api.Contracts;

namespace HomePlugin;

public class HomePlugin : BasePlugin
{
    public override string ModuleName => "HomePlugin";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "TouchMe-Inc";
    public override string ModuleDescription => "Home plugin";

    private IPlayerLocalization _localization;
    private ITeleportRepository _repository;

    protected override void OnLoad()
    {
        var db = Path.Combine(ModulePath, "teleports.db");
        var lang = Path.Combine(ModulePath, "lang");
        _repository = new SqliteTeleportRepository($"Data Source={db};Version=3;");
        _localization = Capabilities.Get<IPlayerLocalizationFactory>().Create(lang);

        RegisterCommand("home", "Print demo help", OnTriggeredTeleport);
    }

    protected override void OnUnload()
    {
        if (_repository is IDisposable d) d.Dispose();
    }

    private void OnTriggeredTeleport(ICommandContext ctx)
    {
        var tag = $"[ffaaaa][{ModuleName}][-] ";
        var entityId = ctx.ClientInfo.EntityId;
        var userId = ctx.ClientInfo.CrossplatformId;
        
        if (ctx.Args.Count < 1)
        {
            ChatMessenger.SendTo(entityId,
                $"{tag}{_localization.Translate(userId, "Bad args")}");
            return;
        }

        var action = ctx.Args[0].ToLower();

        switch (action)
        {
            case "set":
            {
                if (ctx.Args.Count < 2)
                {
                    ChatMessenger.SendTo(entityId,
                        $"{tag}{_localization.Translate(userId, "Bad args set home")}");
                    return;
                }

                var name = ctx.Args[1];
                var position = Capabilities.Get<IPlayerUtil>().GetPlayerPosition(entityId);

                if (position == null)
                {
                    return;
                }

                var point = new TeleportPoint
                    { UserId = userId, Name = name, X = position.X, Y = position.Y, Z = position.Z };
                _repository.AddPoint(point);

                ChatMessenger.SendTo(entityId,
                    $"{tag}{_localization.Translate(userId, "Home saved", name)}");
                break;
            }
            case "go":
            {
                if (ctx.Args.Count < 2)
                {
                    ChatMessenger.SendTo(entityId,
                        $"{tag}{_localization.Translate(userId, "Bad args go home")}");
                    return;
                }

                var name = ctx.Args[1];
                var point = _repository.GetPoint(userId, name);

                if (point == null)
                {
                    ChatMessenger.SendTo(entityId,
                        $"{tag}{_localization.Translate(userId, "Home not found", name)}");
                    return;
                }

                Capabilities.Get<IPlayerUtil>()
                    .Teleport(entityId, new Vector3(point.X, point.Y, point.Z));
                break;
            }
            default:
                ChatMessenger.SendTo(entityId,
                    $"{tag}{_localization.Translate(userId, "Unknown action", action)}");
                break;
        }
    }
}