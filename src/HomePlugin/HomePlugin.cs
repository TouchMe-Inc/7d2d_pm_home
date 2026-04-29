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
    public override string ModuleVersion => "1.2.0";
    public override string ModuleAuthor => "TouchMe-Inc";
    public override string ModuleDescription => "Home plugin";

    private ITeleportRepository _repository;
    private IPlayerLocalization _localization;
    private PluginConfig _pluginConfig;
    private IPlayerUtil _playerUtil;

    private readonly Dictionary<string, long> _nextTeleportTime = new();

    protected override void OnLoad()
    {
        _repository = GetRepository();
        _localization = GetPlayerLocalization();
        _pluginConfig = ReadPluginConfig();
        _playerUtil = Capabilities.Get<IPlayerUtil>();

        RegisterCommand("home", "The command allows you to teleport to saved points", OnTriggeredTeleport);
        RegisterCommand("sethome", "The command allows you to teleport to saved points", OnTriggeredSet);
        RegisterCommand("delhome", "The command allows you to teleport to saved points", OnTriggeredRemove);
    }

    protected override void OnUnload()
    {
        if (_repository is IDisposable d) d.Dispose();
    }

    private void OnTriggeredSet(ICommandContext ctx)
    {
        if (ctx.Args.Count < 1)
        {
            Reply(ctx, "Bad args set home");
            return;
        }

        if (_repository.GetPointsCount(ctx.ClientInfo.CrossplatformId) >= _pluginConfig.HomeLimit)
        {
            Reply(ctx, "Home limited", _pluginConfig.HomeLimit);
            return;
        }

        var name = ctx.Args[0];
        var position = _playerUtil.GetPlayerPosition(ctx.ClientInfo.EntityId);

        if (position == null)
            return;

        _repository.AddPoint(new TeleportPoint
            { UserId = ctx.ClientInfo.CrossplatformId, Name = name, X = position.X, Y = position.Y, Z = position.Z });

        Reply(ctx, "Home saved", name);
    }

    private void OnTriggeredRemove(ICommandContext ctx)
    {
        if (ctx.Args.Count < 1)
        {
            Reply(ctx, "Bad args remove home");
            return;
        }

        var name = ctx.Args[0];

        if (_repository.RemovePoint(ctx.ClientInfo.CrossplatformId, name) > 0)
        {
            Reply(ctx, "Home removed", name);
        }
    }

    private void OnTriggeredTeleport(ICommandContext ctx)
    {
        if (ctx.Args.Count < 1)
        {
            Reply(ctx, "Bad args tp home");
            return;
        }

        var name = ctx.Args[0];
        var platformId = ctx.ClientInfo.CrossplatformId;

        if (!_repository.TryGetPoint(platformId, name, out var point))
        {
            Reply(ctx, "Home not found", name);
            return;
        }

        var unixTime = DateTimeOffset.Now.ToUnixTimeSeconds();

        if (_nextTeleportTime.TryGetValue(platformId, out var nextTeleportTime) && nextTeleportTime > unixTime)
        {
            var cooldown = TimeSpan.FromSeconds(unixTime - nextTeleportTime);
            Reply(ctx, "Teleport cooldown", cooldown);
            return;
        }

        _nextTeleportTime[platformId] = unixTime + _pluginConfig.Delay;

        _playerUtil.Teleport(ctx.ClientInfo.EntityId, new Vector3(point.X, point.Y, point.Z));
    }

    private void Reply(ICommandContext ctx, string key, params object[] args)
    {
        var tag = _localization.Translate(ctx.ClientInfo.CrossplatformId, "Tag");
        var text = _localization.Translate(ctx.ClientInfo.CrossplatformId, key, args);
        _playerUtil.PrintToChat(ctx.ClientInfo.EntityId, $"{tag}{text}");
    }

    private ITeleportRepository GetRepository()
    {
        SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_sqlite3());
        SQLitePCL.raw.FreezeProvider();

        return new SqliteTeleportRepository($"Data Source={Path.Combine(ModulePath, "teleports.db")};");
    }

    private IPlayerLocalization GetPlayerLocalization()
    {
        var playerLanguageStore = Capabilities.Get<IPlayerLanguageStore>();
        return _localization = new JsonPlayerLocalizationFactory(playerLanguageStore)
            .Create(Path.Combine(ModulePath, "lang"));
    }

    private PluginConfig ReadPluginConfig()
    {
        return new JsonConfigReader().Read<PluginConfig>(Path.Combine(ModulePath, "config.json"));
    }
}