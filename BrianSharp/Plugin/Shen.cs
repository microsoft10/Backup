﻿using System;
using System.Drawing;
using System.Linq;
using BrianSharp.Common;
using LeagueSharp;
using LeagueSharp.Common;
using Orbwalk = BrianSharp.Common.Orbwalker;

namespace BrianSharp.Plugin
{
    internal class Shen : Helper
    {
        private Obj_AI_Hero _alertAlly;
        private bool _eCasted, _alertCasted;

        public Shen()
        {
            Q = new Spell(SpellSlot.Q, 485);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 650);
            R = new Spell(SpellSlot.R);
            Q.SetTargetted(0.5f, 1500);
            E.SetSkillshot(0, 50, 1600, false, SkillshotType.SkillshotLine);

            var champMenu = new Menu("Plugin", PlayerName + "_Plugin");
            {
                var comboMenu = new Menu("Combo", "Combo");
                {
                    AddItem(comboMenu, "Q", "Use Q");
                    AddItem(comboMenu, "W", "Use W");
                    AddItem(comboMenu, "WHpU", "-> If Hp Under", 20);
                    AddItem(comboMenu, "E", "Use E");
                    champMenu.AddSubMenu(comboMenu);
                }
                var harassMenu = new Menu("Harass", "Harass");
                {
                    AddItem(harassMenu, "AutoQ", "Auto Q", "H", KeyBindType.Toggle);
                    AddItem(harassMenu, "AutoQMpA", "-> If Mp Above", 50);
                    AddItem(harassMenu, "Q", "Use Q");
                    AddItem(harassMenu, "E", "Use E");
                    AddItem(harassMenu, "EHpA", "-> If Hp Above", 20);
                    champMenu.AddSubMenu(harassMenu);
                }
                var clearMenu = new Menu("Clear", "Clear");
                {
                    AddItem(clearMenu, "Q", "Use Q");
                    AddItem(clearMenu, "W", "Use W");
                    champMenu.AddSubMenu(clearMenu);
                }
                var lastHitMenu = new Menu("Last Hit", "LastHit");
                {
                    AddItem(lastHitMenu, "Q", "Use Q");
                    champMenu.AddSubMenu(lastHitMenu);
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
                        AddItem(killStealMenu, "Q", "Use Q");
                        AddItem(killStealMenu, "Ignite", "Use Ignite");
                        miscMenu.AddSubMenu(killStealMenu);
                    }
                    var antiGapMenu = new Menu("Anti Gap Closer", "AntiGap");
                    {
                        AddItem(antiGapMenu, "E", "Use E");
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
                    var interruptMenu = new Menu("Interrupt", "Interrupt");
                    {
                        AddItem(interruptMenu, "E", "Use E");
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
                    var ultiMenu = new Menu("Ultimate", "Ultimate");
                    {
                        var saveMenu = new Menu("Ally", "Ally");
                        {
                            foreach (var obj in HeroManager.Allies.Where(i => !i.IsMe))
                            {
                                AddItem(saveMenu, obj.ChampionName, obj.ChampionName);
                            }
                            ultiMenu.AddSubMenu(saveMenu);
                        }
                        AddItem(ultiMenu, "Alert", "Alert Ally");
                        AddItem(ultiMenu, "AlertHpU", "-> If Hp Under", 30);
                        AddItem(ultiMenu, "Save", "-> Save Ally");
                        AddItem(ultiMenu, "SaveKey", "--> Key", "T");
                        miscMenu.AddSubMenu(ultiMenu);
                    }
                    //AddItem(miscMenu, "EFlash", "E Flash", "Z");
                    AddItem(miscMenu, "ETower", "Auto E If Enemy Under Tower");
                    champMenu.AddSubMenu(miscMenu);
                }
                var drawMenu = new Menu("Draw", "Draw");
                {
                    AddItem(drawMenu, "Q", "Q Range", false);
                    AddItem(drawMenu, "E", "E Range", false);
                    champMenu.AddSubMenu(drawMenu);
                }
                MainMenu.AddSubMenu(champMenu);
            }
            Game.OnUpdate += OnGameUpdate;
            Drawing.OnDraw += OnDraw;
            AntiGapcloser.OnEnemyGapcloser += OnEnemyGapcloser;
            Interrupter.OnPossibleToInterrupt += OnPossibleToInterrupt;
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
                case Orbwalker.Mode.Flee:
                    if (GetValue<bool>("Flee", "E") && E.IsReady() &&
                        E.Cast(Player.ServerPosition.Extend(Game.CursorPos, E.Range), PacketCast))
                    {
                        return;
                    }
                    break;
            }
            //if (GetValue<KeyBind>("Misc", "EFlash").Active)
            //{
            //    FlashTaunt();
            //}
            AutoQ();
            KillSteal();
            UltimateAlert();
            AutoEUnderTower();
        }

        private void OnDraw(EventArgs args)
        {
            if (Player.IsDead)
            {
                return;
            }
            if (GetValue<bool>("Draw", "Q") && Q.Level > 0)
            {
                Render.Circle.DrawCircle(Player.Position, Q.Range, Q.IsReady() ? Color.Green : Color.Red);
            }
            if (GetValue<bool>("Draw", "E") && E.Level > 0)
            {
                Render.Circle.DrawCircle(Player.Position, E.Range, E.IsReady() ? Color.Green : Color.Red);
            }
        }

        private void OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (Player.IsDead || !GetValue<bool>("AntiGap", "E") ||
                !GetValue<bool>("AntiGap", gapcloser.Sender.ChampionName + "_" + gapcloser.Slot) ||
                !E.CanCast(gapcloser.Sender))
            {
                return;
            }
            var predE = E.GetPrediction(gapcloser.Sender);
            if (predE.Hitchance >= HitChance.High)
            {
                E.Cast(predE.CastPosition.Extend(Player.ServerPosition, -100), PacketCast);
            }
        }

        private void OnPossibleToInterrupt(Obj_AI_Hero unit, InterruptableSpell spell)
        {
            if (Player.IsDead || !GetValue<bool>("Interrupt", "Q") ||
                !GetValue<bool>("Interrupt", unit.ChampionName + "_" + spell.Slot) || !E.CanCast(unit))
            {
                return;
            }
            var predE = E.GetPrediction(unit);
            if (predE.Hitchance >= HitChance.High)
            {
                E.Cast(predE.CastPosition.Extend(Player.ServerPosition, -100), PacketCast);
            }
        }

        private void Fight(string mode)
        {
            if (GetValue<bool>(mode, "E") &&
                (mode == "Combo" || Player.HealthPercent >= GetValue<Slider>(mode, "EHpA").Value))
            {
                var target = E.GetTarget();
                if (target != null)
                {
                    var predE = E.GetPrediction(target);
                    if (predE.Hitchance >= HitChance.High &&
                        E.Cast(predE.CastPosition.Extend(Player.ServerPosition, -100), PacketCast))
                    {
                        if (mode == "Combo" && GetValue<bool>(mode, "W") && W.IsReady())
                        {
                            W.Cast(PacketCast);
                        }
                        return;
                    }
                }
            }
            if (GetValue<bool>(mode, "Q") && Q.CastOnBestTarget(0, PacketCast).IsCasted())
            {
                return;
            }
            if (mode == "Combo" && GetValue<bool>(mode, "W") && W.IsReady() && Q.GetTarget() != null &&
                Player.HealthPercent < GetValue<Slider>(mode, "WHpU").Value)
            {
                W.Cast(PacketCast);
            }
        }

        private void Clear()
        {
            var minionObj = MinionManager.GetMinions(
                Q.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth);
            if (minionObj.Count == 0)
            {
                return;
            }
            if (GetValue<bool>("Clear", "Q") && Q.IsReady())
            {
                var obj = minionObj.Cast<Obj_AI_Minion>().Find(i => CanKill(i, Q)) ??
                          minionObj.MinOrDefault(i => i.Health);
                if (obj != null && Q.CastOnUnit(obj, PacketCast))
                {
                    return;
                }
            }
            if (GetValue<bool>("Clear", "W") && W.IsReady() &&
                (minionObj.Count > 1 || minionObj.Any(i => i.MaxHealth >= 1200)))
            {
                W.Cast(PacketCast);
            }
        }

        private void LastHit()
        {
            if (!GetValue<bool>("LastHit", "Q") || !Q.IsReady())
            {
                return;
            }
            var obj =
                MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth)
                    .Cast<Obj_AI_Minion>()
                    .Find(i => CanKill(i, Q));
            if (obj == null)
            {
                return;
            }
            Q.CastOnUnit(obj, PacketCast);
        }

        private void AutoQ()
        {
            if (!GetValue<KeyBind>("Harass", "AutoQ").Active ||
                Player.ManaPercentage() < GetValue<Slider>("Harass", "AutoQMpA").Value || !Q.IsReady())
            {
                return;
            }
            Q.CastOnBestTarget(0, PacketCast);
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
            if (GetValue<bool>("KillSteal", "Q") && Q.IsReady())
            {
                var target = Q.GetTarget();
                if (target != null && CanKill(target, Q))
                {
                    Q.CastOnUnit(target, PacketCast);
                }
            }
        }

        private void FlashTaunt()
        {
            var target = E.GetTarget(Flash.IsReady() ? 380 : 0);
            CustomOrbwalk(target);
            if (target == null)
            {
                return;
            }
            if (!E.IsReady())
            {
                if (Flash.IsReady() && _eCasted)
                {
                    CastFlash(target.ServerPosition.Extend(Player.ServerPosition, -100));
                    _eCasted = false;
                }
                return;
            }
            if (!E.IsInRange(target) && E.Cast(target.ServerPosition, PacketCast))
            {
                Utility.DelayAction.Add(300, () => _eCasted = true);
                return;
            }
            var predE = E.GetPrediction(target);
            if (predE.Hitchance >= HitChance.High)
            {
                E.Cast(predE.CastPosition.Extend(Player.ServerPosition, -100), PacketCast);
            }
        }

        private void UltimateAlert()
        {
            if (!GetValue<bool>("Ultimate", "Alert") || !R.IsReady())
            {
                _alertAlly = null;
                return;
            }
            if (!_alertCasted)
            {
                var obj =
                    HeroManager.Allies.Where(
                        i =>
                            !i.IsMe && i.IsValidTarget(R.Range, false) && GetValue<bool>("Ally", i.ChampionName) &&
                            i.HealthPercent < GetValue<Slider>("Ultimate", "AlertHpU").Value &&
                            i.CountEnemiesInRange(E.Range) > 0 && !i.HasBuff("Undying Rage"))
                        .MinOrDefault(i => i.Health);
                if (obj != null)
                {
                    AddNotif(obj.ChampionName + ": In Dangerous", 4900);
                    _alertAlly = obj;
                    _alertCasted = true;
                    Utility.DelayAction.Add(
                        5000, () =>
                        {
                            _alertAlly = null;
                            _alertCasted = false;
                        });
                    return;
                }
            }
            if (GetValue<bool>("Ultimate", "Save") && GetValue<KeyBind>("Ultimate", "SaveKey").Active &&
                _alertAlly.IsValidTarget(R.Range, false))
            {
                R.CastOnUnit(_alertAlly, PacketCast);
            }
        }

        private void AutoEUnderTower()
        {
            if (!GetValue<bool>("Misc", "ETower") || !E.IsReady())
            {
                return;
            }
            var target = HeroManager.Enemies.Where(i => i.IsValidTarget(E.Range)).MinOrDefault(i => i.Distance(Player));
            var tower = ObjectManager.Get<Obj_AI_Turret>().Find(i => i.IsAlly && !i.IsDead && i.Distance(Player) <= 850);
            if (target != null && tower != null && target.Distance(tower) <= 850)
            {
                var predE = E.GetPrediction(target);
                if (predE.Hitchance >= HitChance.High)
                {
                    E.Cast(predE.CastPosition.Extend(Player.ServerPosition, -100), PacketCast);
                }
            }
        }
    }
}