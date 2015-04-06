using System;
using System.Drawing;
using System.Linq;
using BrianSharp.Common;
using LeagueSharp;
using LeagueSharp.Common;
using Orbwalk = BrianSharp.Common.Orbwalker;

namespace BrianSharp.Plugin
{
    internal class Ryze : Helper
    {
        public Ryze()
        {
            Q = new Spell(SpellSlot.Q, 630);
            W = new Spell(SpellSlot.W, 605);
            E = new Spell(SpellSlot.E, 605);
            R = new Spell(SpellSlot.R);
            Q.SetTargetted(0.5f, 1400);
            W.SetTargetted(0.5f, float.MaxValue);
            E.SetTargetted(0.5f, 1000);

            var champMenu = new Menu("Plugin", PlayerName + "_Plugin");
            {
                var comboMenu = new Menu("Combo", "Combo");
                {
                    AddItem(comboMenu, "QDelay", "Stop W/E If Q Will Ready In (ms)", 500, 100, 1000);
                    AddItem(comboMenu, "R", "Use R");
                    AddItem(comboMenu, "RHpU", "-> If Enemy Hp Under", 70);
                    AddItem(comboMenu, "Seraph", "Use Seraph's Embrace");
                    AddItem(comboMenu, "SeraphHpU", "-> If Hp Under", 50);
                    champMenu.AddSubMenu(comboMenu);
                }
                var harassMenu = new Menu("Harass", "Harass");
                {
                    AddItem(harassMenu, "AutoQ", "Auto Q", "H", KeyBindType.Toggle);
                    AddItem(harassMenu, "AutoQMpA", "-> If Mp Above", 50);
                    AddItem(harassMenu, "Q", "Use Q");
                    AddItem(harassMenu, "W", "Use W");
                    AddItem(harassMenu, "E", "Use E");
                    champMenu.AddSubMenu(harassMenu);
                }
                var clearMenu = new Menu("Clear", "Clear");
                {
                    AddItem(clearMenu, "Q", "Use Q");
                    AddItem(clearMenu, "W", "Use W");
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
                        AddItem(killStealMenu, "W", "Use W");
                        AddItem(killStealMenu, "E", "Use E");
                        AddItem(killStealMenu, "Ignite", "Use Ignite");
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
                    var interruptMenu = new Menu("Interrupt", "Interrupt");
                    {
                        AddItem(interruptMenu, "W", "Use W");
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
                    AddItem(miscMenu, "Chase", "Chase", "Z");
                    AddItem(miscMenu, "WTower", "Auto W If Enemy Under Tower");
                    champMenu.AddSubMenu(miscMenu);
                }
                var drawMenu = new Menu("Draw", "Draw");
                {
                    AddItem(drawMenu, "Q", "Q Range", false);
                    AddItem(drawMenu, "W", "W Range", false);
                    AddItem(drawMenu, "E", "E Range", false);
                    champMenu.AddSubMenu(drawMenu);
                }
                MainMenu.AddSubMenu(champMenu);
            }
            Game.OnUpdate += OnGameUpdate;
            Drawing.OnDraw += OnDraw;
            AntiGapcloser.OnEnemyGapcloser += OnEnemyGapcloser;
            Interrupter.OnPossibleToInterrupt += OnPossibleToInterrupt;
            //Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            //Orbwalk.BeforeAttack += BeforeAttack;
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
            if (GetValue<KeyBind>("Misc", "Chase").Active)
            {
                Fight("Chase");
            }
            AutoQ();
            KillSteal();
            AutoWUnderTower();
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

        private void OnPossibleToInterrupt(Obj_AI_Hero unit, InterruptableSpell spell)
        {
            if (Player.IsDead || !GetValue<bool>("Interrupt", "W") ||
                !GetValue<bool>("Interrupt", unit.ChampionName + "_" + spell.Slot) || !W.CanCast(unit))
            {
                return;
            }
            W.CastOnUnit(unit, PacketCast);
        }

        private void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            //if (!sender.IsMe)
            //    return;
            //if (ItemBool("Misc", "Exploit") && W.IsReady() && args.SData.Name == "Overload" &&
            //    Orbwalk.CurrentMode == Orbwalk.Mode.LastHit && ItemBool("Misc", "QLastHit") &&
            //    args.Target is Obj_AI_Minion)
            //    Utility.DelayAction.Add(
            //        (int) ((Player.Distance3D((Obj_AI_Minion) args.Target) - 50) / Q.Speed * 1000 + Q.Delay),
            //        () => W.CastOnUnit((Obj_AI_Minion) args.Target, PacketCast()));
        }

        private void BeforeAttack(Orbwalker.BeforeAttackEventArgs Args)
        {
            //var Target = (Obj_AI_Base) Args.Target;
            //if (Orbwalk.CurrentMode == Orbwalk.Mode.Combo || Orbwalk.CurrentMode == Orbwalk.Mode.Harass)
            //{
            //    if ((ItemBool(Orbwalk.CurrentMode.ToString(), "Q") && Q.CanCast(Target)) ||
            //        (ItemBool(Orbwalk.CurrentMode.ToString(), "W") && W.CanCast(Target)) ||
            //        (ItemBool(Orbwalk.CurrentMode.ToString(), "E") && E.CanCast(Target)))
            //        Args.Process = false;
            //}
            //else if (Orbwalk.CurrentMode == Orbwalk.Mode.LaneClear)
            //{
            //    if ((ItemBool("Clear", "Q") && Q.CanCast(Target)) || (ItemBool("Clear", "W") && W.CanCast(Target)) ||
            //        (ItemBool("Clear", "E") && E.CanCast(Target)))
            //        Args.Process = false;
            //}
            //else if (Orbwalk.CurrentMode == Orbwalk.Mode.LastHit && ItemBool("Misc", "QLastHit") && Q.CanCast(Target) &&
            //         CanKill(Target, Q))
            //    Args.Process = false;
        }

        private void Fight(string mode)
        {
            switch (mode)
            {
                case "Harass":
                    if (GetValue<bool>(mode, "Q") && Q.CastOnBestTarget(0, PacketCast).IsCasted())
                    {
                        return;
                    }
                    if (GetValue<bool>(mode, "W") && W.CastOnBestTarget(0, PacketCast).IsCasted())
                    {
                        return;
                    }
                    if (GetValue<bool>(mode, "E"))
                    {
                        E.CastOnBestTarget(0, PacketCast);
                    }
                    break;
                case "Combo":
                    if (GetValue<bool>(mode, "Seraph") && Seraph.IsReady() &&
                        Player.HealthPercent < GetValue<Slider>(mode, "SeraphHpU").Value &&
                        Q.GetTarget(200) != null && !Player.InFountain() && Seraph.Cast())
                    {
                        return;
                    }
                    var fastCd = Math.Abs(Player.PercentCooldownMod) >= 0.2;
                    if (GetValue<bool>(mode, "R") && Q.GetTarget(100) != null &&
                        Q.GetTarget().HealthPercent < GetValue<Slider>(mode, "RHpU").Value &&
                        (!fastCd || Player.LastCastedSpellName() == "Overload") && R.Cast(PacketCast))
                    {
                        return;
                    }
                    if (Q.CastOnBestTarget(0, PacketCast).IsCasted() || Q.IsReady() ||
                        (Q.IsReady(GetValue<Slider>(mode, "QDelay").Value) && fastCd))
                    {
                        return;
                    }
                    if ((!fastCd ||
                         (Player.LastCastedSpellName() == "Overload" ||
                          (GetValue<bool>(mode, "R") && Player.LastCastedSpellName() == "DesperatePower" &&
                           Player.HasBuff("DesperatePower")))) && W.CastOnBestTarget(0, PacketCast).IsCasted())
                    {
                        return;
                    }
                    if (!W.IsReady() && (!fastCd || Player.LastCastedSpellName() == "Overload"))
                    {
                        E.CastOnBestTarget(0, PacketCast);
                    }
                    break;
                case "Chase":
                    var target = W.GetTarget();
                    CustomOrbwalk(target);
                    if (target == null)
                    {
                        return;
                    }
                    if (W.CastOnUnit(target, PacketCast) || W.IsReady() || !target.HasBuff("Rune Prison"))
                    {
                        return;
                    }
                    if (E.CastOnUnit(target, PacketCast))
                    {
                        return;
                    }
                    Q.CastOnUnit(target, PacketCast);
                    break;
            }
        }

        private void Clear()
        {
            var minionObj =
                MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth)
                    .Cast<Obj_AI_Minion>()
                    .ToList();
            if (minionObj.Count == 0)
            {
                return;
            }
            if (GetValue<bool>("Clear", "Q") && Q.IsReady())
            {
                var obj = minionObj.Find(i => CanKill(i, Q)) ?? minionObj.MinOrDefault(i => i.Health);
                if (obj != null && Q.CastOnUnit(obj, PacketCast))
                {
                    return;
                }
            }
            if (GetValue<bool>("Clear", "W") && W.IsReady())
            {
                var obj = minionObj.Where(i => W.IsInRange(i)).Find(i => CanKill(i, W)) ??
                          minionObj.Where(i => W.IsInRange(i)).MinOrDefault(i => i.Health);
                if (obj != null && W.CastOnUnit(obj, PacketCast))
                {
                    return;
                }
            }
            if (GetValue<bool>("Clear", "E") && E.IsReady())
            {
                var obj = minionObj.Where(i => E.IsInRange(i)).Find(i => CanKill(i, E)) ??
                          minionObj.Where(i => E.IsInRange(i)).MinOrDefault(i => i.Health);
                if (obj != null)
                {
                    E.CastOnUnit(obj, PacketCast);
                }
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
                if (target != null && CanKill(target, Q) && Q.CastOnUnit(target, PacketCast))
                {
                    return;
                }
            }
            if (GetValue<bool>("KillSteal", "W") && W.IsReady())
            {
                var target = W.GetTarget();
                if (target != null && CanKill(target, W) && W.CastOnUnit(target, PacketCast))
                {
                    return;
                }
            }
            if (GetValue<bool>("KillSteal", "E") && E.IsReady())
            {
                var target = E.GetTarget();
                if (target != null && CanKill(target, E))
                {
                    E.CastOnUnit(target, PacketCast);
                }
            }
        }

        private void AutoWUnderTower()
        {
            if (!GetValue<bool>("Misc", "WTower") || !W.IsReady())
            {
                return;
            }
            var target = HeroManager.Enemies.Where(i => i.IsValidTarget(W.Range)).MinOrDefault(i => i.Distance(Player));
            var tower = ObjectManager.Get<Obj_AI_Turret>().Find(i => i.IsAlly && !i.IsDead && i.Distance(Player) <= 850);
            if (target != null && tower != null && target.Distance(tower) <= 850)
            {
                W.CastOnUnit(target, PacketCast);
            }
        }
    }
}