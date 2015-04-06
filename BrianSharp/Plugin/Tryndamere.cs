﻿using System;
using System.Drawing;
using System.Linq;
using BrianSharp.Common;
using LeagueSharp;
using LeagueSharp.Common;
using Orbwalk = BrianSharp.Common.Orbwalker;

namespace BrianSharp.Plugin
{
    internal class Tryndamere : Helper
    {
        public Tryndamere()
        {
            Q = new Spell(SpellSlot.Q);
            W = new Spell(SpellSlot.W, 830);
            E = new Spell(SpellSlot.E, 830);
            R = new Spell(SpellSlot.R);
            E.SetSkillshot(0, 225, 1300, false, SkillshotType.SkillshotLine);

            var champMenu = new Menu("Plugin", PlayerName + "_Plugin");
            {
                var comboMenu = new Menu("Combo", "Combo");
                {
                    AddItem(comboMenu, "W", "Use W");
                    AddItem(comboMenu, "WSolo", "-> Both Facing", false);
                    AddItem(comboMenu, "E", "Use E");
                    champMenu.AddSubMenu(comboMenu);
                }
                var harassMenu = new Menu("Harass", "Harass");
                {
                    AddItem(harassMenu, "E", "Use E");
                    AddItem(harassMenu, "EHpA", "-> If Hp Above", 20);
                    champMenu.AddSubMenu(harassMenu);
                }
                var clearMenu = new Menu("Clear", "Clear");
                {
                    AddSmiteMobMenu(clearMenu);
                    AddItem(clearMenu, "E", "Use E");
                    AddItem(clearMenu, "Item", "Use Tiamat/Hydra");
                    champMenu.AddSubMenu(clearMenu);
                }
                var fleeMenu = new Menu("Flee", "Flee");
                {
                    AddItem(fleeMenu, "E", "Use E");
                    champMenu.AddSubMenu(fleeMenu);
                }
                var miscMenu = new Menu("Misc", "Misc");
                {
                    var killStealMenu = new Menu("Kill Steal", "KillSteal");
                    {
                        AddItem(killStealMenu, "E", "Use E");
                        AddItem(killStealMenu, "Ignite", "Use Ignite");
                        AddItem(killStealMenu, "Smite", "Use Smite");
                        miscMenu.AddSubMenu(killStealMenu);
                    }
                    var surviveMenu = new Menu("Survive", "Survive");
                    {
                        AddItem(surviveMenu, "Q", "Use Q");
                        AddItem(surviveMenu, "QHpU", "-> If Hp Under", 40);
                        AddItem(surviveMenu, "R", "Use R");
                        miscMenu.AddSubMenu(surviveMenu);
                    }
                    champMenu.AddSubMenu(miscMenu);
                }
                var drawMenu = new Menu("Draw", "Draw");
                {
                    AddItem(drawMenu, "W", "W Range", false);
                    AddItem(drawMenu, "E", "E Range", false);
                    champMenu.AddSubMenu(drawMenu);
                }
                MainMenu.AddSubMenu(champMenu);
            }
            Game.OnUpdate += OnGameUpdate;
            Drawing.OnDraw += OnDraw;
        }

        private void OnGameUpdate(EventArgs args)
        {
            if (Player.IsDead || MenuGUI.IsChatOpen || Player.IsRecalling())
            {
                return;
            }
            switch (Orbwalk.CurrentMode)
            {
                case Orbwalker.Mode.Combo:
                    Fight("Combo");
                    break;
                case Orbwalker.Mode.Harass:
                    Fight("Harass");
                    break;
                case Orbwalker.Mode.Clear:
                    Clear();
                    break;
                case Orbwalker.Mode.Flee:
                    if (GetValue<bool>("Flee", "E") && E.IsReady() &&
                        E.Cast(Player.ServerPosition.Extend(Game.CursorPos, E.Range), PacketCast))
                    {
                        return;
                    }
                    break;
            }
            KillSteal();
            Survive();
        }

        private void OnDraw(EventArgs args)
        {
            if (Player.IsDead)
            {
                return;
            }
            if (GetValue<bool>("Draw", "W") && W.Level > 0)
            {
                Render.Circle.DrawCircle(Player.Position, W.Range, W.IsReady() ? Color.Green : Color.Red);
            }
            if (GetValue<bool>("Draw", "E") && E.Level > 0)
            {
                Render.Circle.DrawCircle(Player.Position, E.Range, E.IsReady() ? Color.Green : Color.Red);
            }
        }

        private void Fight(string mode)
        {
            if (mode == "Combo" && GetValue<bool>(mode, "W") && W.IsReady() && !Player.IsDashing())
            {
                var target = W.GetTarget();
                if (target != null)
                {
                    if (GetValue<bool>(mode, "WSolo") && Utility.IsBothFacing(Player, target) &&
                        Orbwalk.InAutoAttackRange(target) &&
                        Player.GetAutoAttackDamage(target, true) < target.GetAutoAttackDamage(Player, true))
                    {
                        return;
                    }
                    if (Player.IsFacing(target) && !target.IsFacing(Player) && !Orbwalk.InAutoAttackRange(target, 30) &&
                        W.Cast(PacketCast))
                    {
                        return;
                    }
                }
            }
            if (GetValue<bool>(mode, "E") && E.IsReady() &&
                (mode == "Combo" || Player.HealthPercent >= GetValue<Slider>(mode, "EHpA").Value))
            {
                var target = E.GetTarget();
                if (target != null)
                {
                    var predE = E.GetPrediction(target);
                    if (predE.Hitchance >= HitChance.High &&
                        ((mode == "Combo" && !Orbwalk.InAutoAttackRange(target, 20)) ||
                         (mode == "Harass" && Orbwalk.InAutoAttackRange(target, 50))))
                    {
                        E.Cast(predE.CastPosition.Extend(Player.ServerPosition, -E.Width), PacketCast);
                    }
                }
            }
        }

        private void Clear()
        {
            SmiteMob();
            var minionObj = MinionManager.GetMinions(
                E.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth);
            if (minionObj.Count == 0)
            {
                return;
            }
            if (GetValue<bool>("Clear", "E") && E.IsReady())
            {
                var pos = E.GetLineFarmLocation(minionObj);
                if (pos.MinionsHit > 0 &&
                    E.Cast(pos.Position.Extend(Player.ServerPosition.To2D(), -E.Width / 2), PacketCast))
                {
                    return;
                }
            }
            if (GetValue<bool>("Clear", "Item"))
            {
                var item = Hydra.IsReady() ? Hydra : Tiamat;
                if (item.IsReady() &&
                    (minionObj.Count(i => item.IsInRange(i)) > 2 ||
                     minionObj.Any(i => i.MaxHealth >= 1200 && i.Distance(Player) < item.Range - 80)))
                {
                    item.Cast();
                }
            }
        }

        private void KillSteal()
        {
            if (GetValue<bool>("KillSteal", "Ignite") && Ignite.IsReady())
            {
                var target = TargetSelector.GetTarget(600, TargetSelector.DamageType.True);
                if (target != null && CastIgnite(target))
                {
                    return;
                }
            }
            if (GetValue<bool>("KillSteal", "Smite") &&
                (CurrentSmiteType == SmiteType.Blue || CurrentSmiteType == SmiteType.Red))
            {
                var target = TargetSelector.GetTarget(760, TargetSelector.DamageType.True);
                if (target != null && CastSmite(target))
                {
                    return;
                }
            }
            if (GetValue<bool>("KillSteal", "E") && E.IsReady())
            {
                var target = E.GetTarget();
                if (target != null && CanKill(target, E))
                {
                    var predE = E.GetPrediction(target);
                    if (predE.Hitchance >= HitChance.High)
                    {
                        E.Cast(predE.CastPosition.Extend(Player.ServerPosition, -E.Width), PacketCast);
                    }
                }
            }
        }

        private void Survive()
        {
            if (Player.InFountain())
            {
                return;
            }
            if (GetValue<bool>("Survive", "R") && R.IsReady() && Player.HealthPercent < 5 &&
                Player.GetEnemiesInRange(E.Range).Any(i => !i.IsDead) && R.Cast(PacketCast))
            {
                return;
            }
            if (GetValue<bool>("Survive", "Q") && Q.IsReady() && !Player.HasBuff("Undying Rage") &&
                Player.HealthPercent < GetValue<Slider>("Survive", "QHpU").Value && E.GetTarget() != null)
            {
                Q.Cast(PacketCast);
            }
        }
    }
}