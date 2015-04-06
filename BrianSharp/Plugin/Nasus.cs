﻿using System;
using System.Drawing;
using System.Linq;
using BrianSharp.Common;
using LeagueSharp;
using LeagueSharp.Common;
using Orbwalk = BrianSharp.Common.Orbwalker;

namespace BrianSharp.Plugin
{
    internal class Nasus : Helper
    {
        public Nasus()
        {
            Q = new Spell(SpellSlot.Q, Orbwalk.GetAutoAttackRange());
            W = new Spell(SpellSlot.W, 600);
            E = new Spell(SpellSlot.E, 650);
            R = new Spell(SpellSlot.R);
            Q.SetTargetted(0.2333f, float.MaxValue);
            E.SetSkillshot(0.5f, 380, float.MaxValue, false, SkillshotType.SkillshotCircle);

            var champMenu = new Menu("Plugin", PlayerName + "_Plugin");
            {
                var comboMenu = new Menu("Combo", "Combo");
                {
                    AddItem(comboMenu, "Q", "Use Q");
                    AddItem(comboMenu, "W", "Use W");
                    AddItem(comboMenu, "E", "Use E");
                    AddItem(comboMenu, "R", "Use R");
                    AddItem(comboMenu, "RHpU", "-> If Player Hp Under", 60);
                    AddItem(comboMenu, "RCountA", "-> If Enemy Above", 2, 1, 5);
                    champMenu.AddSubMenu(comboMenu);
                }
                var harassMenu = new Menu("Harass", "Harass");
                {
                    AddItem(harassMenu, "Q", "Use Q");
                    AddItem(harassMenu, "W", "Use W");
                    AddItem(harassMenu, "E", "Use E");
                    champMenu.AddSubMenu(harassMenu);
                }
                var clearMenu = new Menu("Clear", "Clear");
                {
                    AddSmiteMobMenu(clearMenu);
                    AddItem(clearMenu, "Q", "Use Q");
                    AddItem(clearMenu, "E", "Use E");
                    champMenu.AddSubMenu(clearMenu);
                }
                var lastHitMenu = new Menu("Last Hit", "LastHit");
                {
                    AddItem(lastHitMenu, "Q", "Use Q");
                    champMenu.AddSubMenu(lastHitMenu);
                }
                var miscMenu = new Menu("Misc", "Misc");
                {
                    var killStealMenu = new Menu("Kill Steal", "KillSteal");
                    {
                        AddItem(killStealMenu, "Q", "Use Q");
                        AddItem(killStealMenu, "E", "Use E");
                        AddItem(killStealMenu, "Ignite", "Use Ignite");
                        AddItem(killStealMenu, "Smite", "Use Smite");
                        miscMenu.AddSubMenu(killStealMenu);
                    }
                    var antiGapMenu = new Menu("Anti Gap Closer", "AntiGap");
                    {
                        AddItem(antiGapMenu, "W", "Use W");
                        foreach (var spell in
                            AntiGapcloser.Spells.Where(
                                i => HeroManager.Enemies.Any(a => i.ChampionName == a.ChampionName)))
                        {
                            AddItem(
                                antiGapMenu, spell.ChampionName + "_" + spell.Slot,
                                "-> Skill " + spell.Slot + " Of " + spell.ChampionName);
                        }
                        miscMenu.AddSubMenu(antiGapMenu);
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
            AntiGapcloser.OnEnemyGapcloser += OnEnemyGapcloser;
            Orbwalk.AfterAttack += AfterAttack;
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
                case Orbwalker.Mode.LastHit:
                    LastHit();
                    break;
            }
            KillSteal();
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

        private void OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (Player.IsDead || !GetValue<bool>("AntiGap", "W") ||
                !GetValue<bool>("AntiGap", gapcloser.Sender.ChampionName + "_" + gapcloser.Slot) ||
                !W.CanCast(gapcloser.Sender))
            {
                return;
            }
            W.CastOnUnit(gapcloser.Sender, PacketCast);
        }

        private void AfterAttack(AttackableUnit target)
        {
            if (!Q.IsReady())
            {
                return;
            }
            if ((Orbwalk.CurrentMode == Orbwalker.Mode.Combo || Orbwalk.CurrentMode == Orbwalker.Mode.Harass) &&
                GetValue<bool>(Orbwalk.CurrentMode.ToString(), "Q") && target is Obj_AI_Hero && Q.Cast(PacketCast))
            {
                Player.IssueOrder(GameObjectOrder.AttackUnit, target);
            }
        }

        private void Fight(string mode)
        {
            if (mode == "Combo" && GetValue<bool>(mode, "R") && R.IsReady())
            {
                var rCount = GetValue<Slider>(mode, "RCountA").Value;
                if (((rCount > 1 && (Player.CountEnemiesInRange(E.Range) >= rCount || E.GetTarget() != null)) ||
                     (rCount == 1 && E.GetTarget() != null)) &&
                    Player.HealthPercent < GetValue<Slider>(mode, "RHpU").Value)
                {
                    R.Cast(PacketCast);
                }
            }
            if (GetValue<bool>(mode, "E") && E.IsReady())
            {
                var target = E.GetTarget(E.Width);
                if (target != null && (mode == "Combo" || Orbwalk.InAutoAttackRange(target, 50)) &&
                    E.CastIfWillHit(target, -1, PacketCast))
                {
                    return;
                }
            }
            if (GetValue<bool>(mode, "W") && W.IsReady())
            {
                var target = W.GetTarget();
                if (target != null &&
                    ((mode == "Combo" && (!Orbwalk.InAutoAttackRange(target, 50) || target.HealthPercent > 30)) ||
                     (mode == "Harass" && Orbwalk.InAutoAttackRange(target, 50))))
                {
                    W.CastOnUnit(target, PacketCast);
                }
            }
        }

        private void Clear()
        {
            SmiteMob();
            var minionObj = MinionManager.GetMinions(
                E.Range + E.Width, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth);
            if (minionObj.Count == 0)
            {
                return;
            }
            if (GetValue<bool>("Clear", "Q") && (Q.IsReady() || Player.HasBuff("SiphoningStrike")))
            {
                var obj =
                    ObjectManager.Get<Obj_AI_Turret>()
                        .Find(i => Orbwalk.InAutoAttackRange(i) && CanKill(i, Q, GetBonusDmg(i))) ??
                    (Obj_AI_Base)
                        minionObj.Cast<Obj_AI_Minion>()
                            .Where(i => Orbwalk.InAutoAttackRange(i))
                            .Find(
                                i =>
                                    CanKill(i, Q, GetBonusDmg(i)) ||
                                    !CanKill(
                                        i, Q,
                                        GetBonusDmg(i) +
                                        Player.GetAutoAttackDamage(i, true) *
                                        Math.Floor(Q.Instance.Cooldown / 1 / Player.AttackDelay)));
                if (obj != null)
                {
                    if (!Player.HasBuff("SiphoningStrike"))
                    {
                        Q.Cast(PacketCast);
                    }
                    Orbwalk.Move = false;
                    Utility.DelayAction.Add(80, () => Orbwalk.Move = true);
                    Player.IssueOrder(GameObjectOrder.AttackUnit, obj);
                }
            }
            if (GetValue<bool>("Clear", "E") && E.IsReady())
            {
                var pos = E.GetCircularFarmLocation(minionObj);
                if (pos.MinionsHit > 1)
                {
                    E.Cast(pos.Position, PacketCast);
                }
                else
                {
                    var obj = minionObj.Find(i => i.MaxHealth >= 1200);
                    if (obj != null)
                    {
                        E.CastIfWillHit(obj, -1, PacketCast);
                    }
                }
            }
        }

        private void LastHit()
        {
            if (!GetValue<bool>("LastHit", "Q") || (!Q.IsReady() && !Player.HasBuff("SiphoningStrike")))
            {
                return;
            }
            var obj =
                MinionManager.GetMinions(Q.Range + 100, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth)
                    .Cast<Obj_AI_Minion>()
                    .Where(i => Orbwalk.InAutoAttackRange(i))
                    .Find(i => CanKill(i, Q, GetBonusDmg(i)));
            if (obj == null)
            {
                return;
            }
            if (!Player.HasBuff("SiphoningStrike"))
            {
                Q.Cast(PacketCast);
            }
            Orbwalk.Move = false;
            Utility.DelayAction.Add(80, () => Orbwalk.Move = true);
            Player.IssueOrder(GameObjectOrder.AttackUnit, obj);
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
            if (GetValue<bool>("KillSteal", "Q") && (Q.IsReady() || Player.HasBuff("SiphoningStrike")))
            {
                var target = Orbwalk.GetBestHeroTarget();
                if (target != null && CanKill(target, Q, GetBonusDmg(target)))
                {
                    if (!Player.HasBuff("SiphoningStrike"))
                    {
                        Q.Cast(PacketCast);
                    }
                    Orbwalk.Move = false;
                    Utility.DelayAction.Add(80, () => Orbwalk.Move = true);
                    Player.IssueOrder(GameObjectOrder.AttackUnit, target);
                }
            }
            if (GetValue<bool>("KillSteal", "E") && E.IsReady())
            {
                var target = E.GetTarget(E.Width);
                if (target != null && CanKill(target, E))
                {
                    E.CastIfWillHit(target, -1, PacketCast);
                }
            }
        }

        private double GetBonusDmg(Obj_AI_Base target)
        {
            double dmgItem = 0;
            if (Sheen.IsOwned() && (Sheen.IsReady() || Player.HasBuff("Sheen")) && Player.BaseAttackDamage > dmgItem)
            {
                dmgItem = Player.BaseAttackDamage;
            }
            if (Iceborn.IsOwned() && (Iceborn.IsReady() || Player.HasBuff("ItemFrozenFist")) &&
                Player.BaseAttackDamage * 1.25 > dmgItem)
            {
                dmgItem = Player.BaseAttackDamage * 1.25;
            }
            if (Trinity.IsOwned() && (Trinity.IsReady() || Player.HasBuff("Sheen")) &&
                Player.BaseAttackDamage * 2 > dmgItem)
            {
                dmgItem = Player.BaseAttackDamage * 2;
            }
            return (Q.IsReady() ? Q.GetDamage(target) : 0) + Player.GetAutoAttackDamage(target, true) +
                   (dmgItem > 0 ? Player.CalcDamage(target, Damage.DamageType.Physical, dmgItem) : 0) + 5;
        }
    }
}