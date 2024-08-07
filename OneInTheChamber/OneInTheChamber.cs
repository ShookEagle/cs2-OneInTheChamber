using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.VisualBasic.CompilerServices;

namespace OneInTheChamber;

public class OneInTheChamber : BasePlugin
{
    public override string ModuleName => "OneInTheChamber";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "ShookEagle";

    private readonly Dictionary<CCSPlayerController, CBasePlayerWeapon> _deagles = new();

    public override void Load(bool hotReload)
    {
        Server.ExecuteCommand("mp_death_drop_gun 0");
    }

    [GameEventHandler]
    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo _)
    {
        var players = Utilities.GetPlayers();
        foreach (var player in players)
        {
            GiveWeapons(player);
        }
        return HookResult.Continue;
    }

    [GameEventHandler(HookMode.Pre)]
    public HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
    {
        var player = @event.Userid!;
        
        if (!player.IsValid)
        {
            return HookResult.Continue;
        }

        player.PlayerPawn.Value!.Health = 0;
        Utilities.SetStateChanged(player.PlayerPawn.Value, "CBaseEntity", "m_iHealth");
        
        return HookResult.Changed;
    }

    [GameEventHandler]
    public HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo _)
    {
        var player = @event.Userid!;
        var attacker = @event.Attacker!;
        
        AddBullet(attacker);
        _deagles.Remove(player);
        
        return HookResult.Continue;
    }

    private void AddBullet(CCSPlayerController player)
    {
        var weapon = player.PlayerPawn.Value?.WeaponServices?.MyWeapons.FirstOrDefault(w => w.Value?.DesignerName == "weapon_deagle")
            ?.Value;
        if (weapon == null) return;
        if (weapon.Clip1 + 1 > weapon.VData!.MaxClip1) {
            var overflowBullets = weapon.Clip1 + 1 - weapon.VData!.MaxClip1;
            weapon.Clip1          =  weapon.VData!.MaxClip1;
            weapon.ReserveAmmo[0] += overflowBullets;
        } else { weapon.Clip1 += 1; }
        Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_iClip1");
        Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_pReserveAmmo");
    }

    [GameEventHandler]
    public HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo _)
    {
        _deagles.Clear();
        return HookResult.Continue;
    }
    public void GiveWeapons(CCSPlayerController player)
    {
        if (player == null) return;
        
        player.RemoveWeapons();
        player.GiveNamedItem(player.Team == CsTeam.Terrorist ? CsItem.KnifeT : CsItem.KnifeCT);
        player.GiveNamedItem(CsItem.Deagle);
        
        var deagle = player.Pawn.Value?.WeaponServices?
            .MyWeapons.FirstOrDefault(w => w.Value?.DesignerName == "weapon_deagle")?.Value;
        if (deagle == null) return;
        
        deagle.Clip1 = 1;
        Utilities.SetStateChanged(deagle, "CBasePlayerWeapon", "m_iClip1");
        deagle.ReserveAmmo[0] = 0;
        Utilities.SetStateChanged(deagle, "CBasePlayerWeapon", "m_iClip1");
        
        _deagles.TryAdd(player, deagle);
    }
}