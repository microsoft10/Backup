﻿using System;
using System.Drawing;
using System.Linq;
using BrianSharp.Common;
using LeagueSharp;
using LeagueSharp.Common;
using Orbwalk = BrianSharp.Common.Orbwalker;

namespace BrianSharp.Plugin
{
    internal class XinZhao : Helper
    {
        public XinZhao()
        {
            Q = new Spell(SpellSlot.Q);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 659);
            R = new Spell(SpellSlot.R, 510);
            Q.SetTargetted(0.5f, float.MaxValue);
            E.SetTargetted(0.5f, float.MaxValue);
            R.SetSkillshot(0.35f, 510, 347.8f, false, SkillshotType.SkillshotCircle);

            var champMenu = new Menu("Plugin", PlayerName + "_Plugin");
            {
                var comboMenu = new Menu("Combo", "Combo");
                {
                    var killableMenu = new Menu("Killable (R)", "Killable");
                    {
                        foreach (var obj in HeroManager.Enemies)
                        {
                            AddItem(killableMenu, obj.ChampionName, obj.ChampionName);
                        }
                        comboMenu.AddSubMenu(killableMenu);
                    }
                    AddItem(comboMenu, "Q", "Use Q");
                    AddItem(comboMenu, "W", "Use W");
                    AddItem(comboMenu, "E", "Use E");
                    AddItem(comboMenu, "R", "Use R If Killable");
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
                    AddItem(clearMenu, "W", "Use W");
                    AddItem(clearMenu, "E", "Use E");
                    AddItem(clearMenu, "Item", "Use Tiamat/Hydra Item");
                    champMenu.AddSubMenu(clearMenu);
                }
                var miscMenu = new Menu("Misc", "Misc");
                {
                    var killStealMenu = new Menu("Kill Steal", "KillSteal");
                    {
                        AddItem(killStealMenu, "E", "Use E");
                        AddItem(killStealMenu, "R", "Use R");
                        AddItem(killStealMenu, "Ignite", "Use Ignite");
                        AddItem(killStealMenu, "Smite", "Use Smite");
                        miscMenu.AddSubMenu(killStealMenu);
                    }
                    var interruptMenu = new Menu("Interrupt", "Interrupt");
                    {
                        AddItem(interruptMenu, "R", "Use R");
                        foreach (var spell in
                            Interrupter.Spells.Where(
                                i => HeroManager.Enemies.Any(a => i.ChampionName == a.ChampionName)))
                        {
                            AddItem(
                                interruptMenu, spell.ChampionName + "_" + spell.Slot,
                                "-> Skill " + spell.Slot + " Of " + spell.ChampionName);
                        }
                        miscMenu.AddSubMenu(interruptMenu);
                    }
                    champMenu.AddSubMenu(miscMenu);
                }
                var drawMenu = new Menu("Draw", "Draw");
                {
                    AddItem(drawMenu, "E", "E Range", false);
                    AddItem(drawMenu, "R", "R Range", false);
                    champMenu.AddSubMenu(drawMenu);
                }
                MainMenu.AddSubMenu(champMenu);
            }
            Game.OnUpdate += OnGameUpdate;
            Drawing.OnDraw += OnDraw;
            Interrupter.OnPossibleToInterrupt += OnPossibleToInterrupt;
            Orbwalk.OnAttack += OnAttack;
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
            }
            KillSteal();
        }

        private void OnDraw(EventArgs args)
        {
            if (Player.IsDead)
            {
                return;
            }
            if (GetValue<bool>("Draw", "E") && E.Level > 0)
            {
                Render.Circle.DrawCircle(Player.Position, E.Range, E.IsReady() ? Color.Green : Color.Red);
            }
            if (GetValue<bool>("Draw", "R") && R.Level > 0)
            {
                Render.Circle.DrawCircle(Player.Position, R.Range, R.IsReady() ? Color.Green : Color.Red);
            }
        }

        private void OnPossibleToInterrupt(Obj_AI_Hero unit, InterruptableSpell spell)
        {
            if (Player.IsDead || !GetValue<bool>("Interrupt", "R") ||
                !GetValue<bool>("Interrupt", unit.ChampionName + "_" + spell.Slot) || !R.IsReady() ||
                unit.HasBuff("XinZhaoIntimidate"))
            {
                return;
            }
            if (R.IsInRange(unit) && R.Cast(PacketCast))
            {
                return;
            }
            if (!R.IsInRange(unit) && E.IsReady() && Player.Mana >= E.Instance.ManaCost + R.Instance.ManaCost)
            {
                var obj =
                    ObjectManager.Get<Obj_AI_Base>()
                        .Where(
                            i =>
                                !(i is Obj_AI_Turret) && i.IsValidTarget(E.Range) && i != unit &&
                                i.Distance(unit) < R.Range - 15)
                        .MinOrDefault(i => i.Distance(unit));
                if (obj != null)
                {
                    E.CastOnUnit(obj, PacketCast);
                }
            }
        }

        private void OnAttack(AttackableUnit target)
        {
            if (!target.IsValidTarget() || !W.IsReady())
            {
                return;
            }
            if ((((Orbwalk.CurrentMode == Orbwalker.Mode.Combo || Orbwalk.CurrentMode == Orbwalker.Mode.Harass) &&
                  target is Obj_AI_Hero) || (Orbwalk.CurrentMode == Orbwalker.Mode.Clear && target is Obj_AI_Minion)) &&
                GetValue<bool>(Orbwalk.CurrentMode.ToString(), "W"))
            {
                W.Cast(PacketCast);
            }
        }

        private void AfterAttack(AttackableUnit target)
        {
            if (!Q.IsReady())
            {
                return;
            }
            if ((((Orbwalk.CurrentMode == Orbwalker.Mode.Combo || Orbwalk.CurrentMode == Orbwalker.Mode.Harass) &&
                  target is Obj_AI_Hero) || (Orbwalk.CurrentMode == Orbwalker.Mode.Clear && target is Obj_AI_Minion)) &&
                GetValue<bool>(Orbwalk.CurrentMode.ToString(), "Q") && Q.Cast(PacketCast))
            {
                Player.IssueOrder(GameObjectOrder.AttackUnit, target);
            }
        }

        private void Fight(string mode)
        {
            if (mode == "Combo" && GetValue<bool>(mode, "R") && R.IsReady())
            {
                var target = R.GetTarget(0, HeroManager.Enemies.Where(i => !GetValue<bool>("Killable", i.ChampionName)));
                if (target != null)
                {
                    if (CanKill(target, R) && R.Cast(PacketCast))
                    {
                        return;
                    }
                    if (
                        CanKill(
                            target, R, R.GetDamage(target),
                            E.GetDamage(target) + Player.GetAutoAttackDamage(target, true)) && GetValue<bool>(mode, "E") &&
                        E.IsReady() && Player.Mana >= E.Instance.ManaCost + R.Instance.ManaCost && R.Cast(PacketCast) &&
                        E.CastOnUnit(target, PacketCast))
                    {
                        return;
                    }
                }
            }
            if (GetValue<bool>(mode, "E") && E.IsReady())
            {
                var target = E.GetTarget();
                if (target != null)
                {
                    if (mode == "Harass")
                    {
                        if (Orbwalk.InAutoAttackRange(target, 100))
                        {
                            E.CastOnUnit(target, PacketCast);
                        }
                    }
                    else if ((!Orbwalk.InAutoAttackRange(target, 20) || Player.Health < target.Health))
                    {
                        E.CastOnUnit(target, PacketCast);
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
                var obj = (Obj_AI_Base) minionObj.Cast<Obj_AI_Minion>().Find(i => CanKill(i, E));
                if (obj == null && minionObj.Count(i => Orbwalk.InAutoAttackRange(i, 30)) == 0)
                {
                    obj = minionObj.MinOrDefault(i => i.Health);
                }
                if (obj != null && E.CastOnUnit(obj, PacketCast))
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
                if (target != null && CanKill(target, E) && E.CastOnUnit(target, PacketCast))
                {
                    return;
                }
            }
            if (GetValue<bool>("KillSteal", "R") && R.IsReady())
            {
                var target = R.GetTarget();
                if (target != null && CanKill(target, R))
                {
                    R.Cast(PacketCast);
                }
            }
        }
    }
}