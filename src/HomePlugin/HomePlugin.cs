using System;
using System.Collections.Generic;
using System.IO;
using PluginManager.Api;
using PluginManager.Api.Capabilities.Implementations.Commands;
using PluginManager.Api.Capabilities.Implementations.Translations;
using PluginManager.Api.Capabilities.Implementations.Utils;
using PluginManager.Api.Contracts;
using PluginManager.Config;
using PluginManager.Localization;

namespace HomePlugin;

public class HomePlugin : BasePlugin
{
    public override string ModuleName => "HomePlugin";
    public override string ModuleVersion => "1.1.0";
    public override string ModuleAuthor => "TouchMe-Inc";
    public override string ModuleDescription => "Home plugin";

    private IPlayerLocalization _localization;
    private ITeleportRepository _repository;
    private IPlayerUtil _playerUtil;
    private IGameUtil _gameUtil;
    private PluginConfig _pluginConfig;

    private readonly Dictionary<string, ulong> _teleports = new();

    protected override void OnLoad()
    {
        _repository = GetRepository();
        _playerUtil = Capabilities.Get<IPlayerUtil>();
        _gameUtil = Capabilities.Get<IGameUtil>();
        
        var playerLanguageStore = Capabilities.Get<IPlayerLanguageStore>();
        _localization = new JsonPlayerLocalizationFactory(playerLanguageStore).Create(Path.Combine(ModulePath, "lang"));

        _pluginConfig = new JsonConfigReader().Read<PluginConfig>(Path.Combine(ModulePath, "config.json"));

        RegisterCommand("home", "The command allows you to teleport to saved points", OnTriggeredTeleport);
    }

    protected override void OnUnload()
    {
        if (_repository is IDisposable d) d.Dispose();
    }

    private ITeleportRepository GetRepository()
    {
        SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_sqlite3());
        SQLitePCL.raw.FreezeProvider();

        return new SqliteTeleportRepository($"Data Source={Path.Combine(ModulePath, "teleports.db")};");
    }

    private void OnTriggeredTeleport(ICommandContext ctx)
    {
        if (ctx.Args.Count < 1)
        {
            Reply(ctx, "Bad args");
            return;
        }

        var action = ctx.Args[0].ToLower();

        switch (action)
        {
            case "set": HandleSet(ctx); break;
            case "remove":
            case "rm": HandleRemove(ctx); break;
            case "tp":
            case "go": HandleTeleport(ctx); break;
            default:
                Reply(ctx, "Unknown action", action);
                break;
        }
    }

    private void HandleSet(ICommandContext ctx)
    {
        if (ctx.Args.Count < 2)
        {
            Reply(ctx, "Bad args set home");
            return;
        }

        if (_repository.GetPointsCount(ctx.ClientInfo.CrossplatformId) >= _pluginConfig.HomeLimit)
        {
            Reply(ctx, "Home limited", _pluginConfig.HomeLimit);
            return;
        }

        var name = ctx.Args[1];
        var position = _playerUtil.GetPlayerPosition(ctx.ClientInfo.EntityId);

        if (position == null)
        {
            return;
        }

        _repository.AddPoint(new TeleportPoint
            { UserId = ctx.ClientInfo.CrossplatformId, Name = name, X = position.X, Y = position.Y, Z = position.Z });

        Reply(ctx, "Home saved", name);
    }

    private void HandleRemove(ICommandContext ctx)
    {
        if (ctx.Args.Count < 2)
        {
            Reply(ctx, "Bad args remove home");
            return;
        }

        var name = ctx.Args[1];

        if (_repository.RemovePoint(ctx.ClientInfo.CrossplatformId, name) > 0)
        {
            Reply(ctx, "Home removed", name);
        }
    }

    private void HandleTeleport(ICommandContext ctx)
    {
        if (ctx.Args.Count < 2)
        {
            Reply(ctx, "Bad args tp home");
            return;
        }

        var name = ctx.Args[1];
        var platformId = ctx.ClientInfo.CrossplatformId;

        if (!_repository.TryGetPoint(platformId, name, out var point))
        {
            Reply(ctx, "Home not found", name);
            return;
        }

        var worldTime = _gameUtil.GetWorldTime();

        if (_teleports.TryGetValue(platformId, out var nextTeleportTime) && nextTeleportTime > worldTime)
        {
            Reply(ctx, "Next teleport time",
                _gameUtil.WorldTimeToDays(nextTeleportTime),
                _gameUtil.WorldTimeToHours(nextTeleportTime),
                _gameUtil.WorldTimeToMinutes(nextTeleportTime)
            );
            return;
        }

        _teleports[platformId] = worldTime + _pluginConfig.Delay;

        _playerUtil.Teleport(ctx.ClientInfo.EntityId, new Vector3(point.X, point.Y, point.Z));
    }

    private void Reply(ICommandContext ctx, string key, params object[] args)
    {
        var tag = _localization.Translate(ctx.ClientInfo.CrossplatformId, "Tag");
        var text = _localization.Translate(ctx.ClientInfo.CrossplatformId, key, args);
        _playerUtil.PrintToChat(ctx.ClientInfo.EntityId, $"{tag}{text}");
    }
}