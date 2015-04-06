using System;
using System.Drawing;
using System.Linq;
using BrianSharp.Common;
using LeagueSharp;
using LeagueSharp.Common;
using Orbwalk = BrianSharp.Common.Orbwalker;

namespace BrianSharp.Plugin
{
    internal class Amumu : Helper
    {
        public Amumu()
        {
            Q = new Spell(SpellSlot.Q, 1100);
            W = new Spell(SpellSlot.W, 300);
            E = new Spell(SpellSlot.E, 350);
            R = new Spell(SpellSlot.R, 550);
            Q.SetSkillshot(0.25f, 90, 2000, true, SkillshotType.SkillshotLine);
            E.SetSkillshot(0.5f, 350, float.MaxValue, false, SkillshotType.SkillshotCircle);
            R.SetSkillshot(0.25f, 550, float.MaxValue, false, SkillshotType.SkillshotCircle);

            var champMenu = new Menu("Plugin", PlayerName + "_Plugin");
            {
                var comboMenu = new Menu("Combo", "Combo");
                {
                    AddItem(comboMenu, "Q", "Use Q");
                    AddItem(comboMenu, "QCol", "-> Smite Collision");
                    AddItem(comboMenu, "W", "Use W");
                    AddItem(comboMenu, "WMpA", "-> If Mp Above", 20);
                    AddItem(comboMenu, "E", "Use E");
                    AddItem(comboMenu, "R", "Use R");
                    AddItem(comboMenu, "RHpU", "-> If Enemy Hp Under", 60);
                    AddItem(comboMenu, "RCountA", "-> If Enemy Above", 2, 1, 5);
                    champMenu.AddSubMenu(comboMenu);
                }
                var harassMenu = new Menu("Harass", "Harass");
                {
                    AddItem(harassMenu, "W", "Use W");
                    AddItem(harassMenu, "WMpA", "-> If Mp Above", 20);
                    AddItem(harassMenu, "E", "Use E");
                    champMenu.AddSubMenu(harassMenu);
                }
                var clearMenu = new Menu("Clear", "Clear");
                {
                    AddSmiteMobMenu(clearMenu);
                    AddItem(clearMenu, "Q", "Use Q");
                    AddItem(clearMenu, "W", "Use W");
                    AddItem(clearMenu, "WMpA", "-> If Mp Above", 20);
                    AddItem(clearMenu, "E", "Use E");
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
                    var antiGapMenu = new Menu("Anti Gap Closer", "AntiGap");
                    {
                        AddItem(antiGapMenu, "Q", "Use Q");
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
                        AddItem(interruptMenu, "Q", "Use Q");
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
                    AddItem(miscMenu, "WExtraRange", "W Extra Range Before Cancel", 60, 0, 200);
                    champMenu.AddSubMenu(miscMenu);
                }
                var drawMenu = new Menu("Draw", "Draw");
                {
                    AddItem(drawMenu, "Q", "Q Range", false);
                    AddItem(drawMenu, "W", "W Range", false);
                    AddItem(drawMenu, "E", "E Range", false);
                    AddItem(drawMenu, "R", "R Range", false);
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
            }
            KillSteal();
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
            if (GetValue<bool>("Draw", "R") && R.Level > 0)
            {
                Render.Circle.DrawCircle(Player.Position, R.Range, R.IsReady() ? Color.Green : Color.Red);
            }
        }

        private void OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (Player.IsDead || !GetValue<bool>("AntiGap", "Q") ||
                !GetValue<bool>("AntiGap", gapcloser.Sender.ChampionName + "_" + gapcloser.Slot) || !Q.IsReady())
            {
                return;
            }
            Q.CastIfHitchanceEquals(gapcloser.Sender, HitChance.High, PacketCast);
        }

        private void OnPossibleToInterrupt(Obj_AI_Hero unit, InterruptableSpell spell)
        {
            if (Player.IsDead || !GetValue<bool>("Interrupt", "Q") ||
                !GetValue<bool>("Interrupt", unit.ChampionName + "_" + spell.Slot) || !Q.CanCast(unit))
            {
                return;
            }
            Q.CastIfHitchanceEquals(unit, HitChance.High, PacketCast);
        }

        private void Fight(string mode)
        {
            if (mode == "Combo")
            {
                if (GetValue<bool>(mode, "R") && R.IsReady())
                {
                    var target = HeroManager.Enemies.Where(i => i.IsValidTarget(R.Range)).ToList();
                    if (((target.Count > 1 && target.Count(i => CanKill(i, R)) > 0) ||
                         (target.Count > 1 &&
                          target.Count(i => i.HealthPercent < GetValue<Slider>(mode, "RHpU").Value) > 0) ||
                         target.Count >= GetValue<Slider>(mode, "RCountA").Value) && R.Cast(PacketCast))
                    {
                        return;
                    }
                }
                if (GetValue<bool>(mode, "Q") && Q.IsReady())
                {
                    if (GetValue<bool>(mode, "R") && R.IsReady())
                    {
                        if (
                            (from obj in
                                ObjectManager.Get<Obj_AI_Base>()
                                    .Where(
                                        i =>
                                            !(i is Obj_AI_Turret) && i.IsValidTarget(Q.Range) &&
                                            Q.GetPrediction(i).Hitchance >= HitChance.High)
                                    .OrderByDescending(i => i.CountEnemiesInRange(R.Range))
                                let sub =
                                    HeroManager.Enemies.Where(
                                        i => i.IsValidTarget(R.Range - 20, true, obj.ServerPosition)).ToList()
                                where
                                    sub.Count > 0 &&
                                    ((sub.Count > 1 && sub.Count(i => CanKill(i, R)) > 0) ||
                                     (sub.Count > 1 &&
                                      sub.Count(i => i.HealthPercent < GetValue<Slider>(mode, "RHpU").Value) > 0) ||
                                     sub.Count >= GetValue<Slider>(mode, "RCountA").Value) &&
                                    Q.CastIfHitchanceEquals(obj, HitChance.High, PacketCast)
                                select obj).Any())
                        {
                            return;
                        }
                    }
                    var target = Q.GetTarget();
                    if (target != null && !Orbwalk.InAutoAttackRange(target))
                    {
                        var state = Q.Cast(target, PacketCast);
                        if (state.IsCasted())
                        {
                            return;
                        }
                        if (state == Spell.CastStates.Collision && GetValue<bool>(mode, "QCol"))
                        {
                            var pred = Q.GetPrediction(target);
                            if (pred.CollisionObjects.Count(i => i.IsMinion) == 1 &&
                                CastSmite(pred.CollisionObjects.First()) && Q.Cast(pred.CastPosition, PacketCast))
                            {
                                return;
                            }
                        }
                    }
                }
            }
            if (GetValue<bool>(mode, "E") && E.IsReady() && E.GetTarget() != null && E.Cast(PacketCast))
            {
                return;
            }
            if (GetValue<bool>(mode, "W") && W.IsReady())
            {
                if (Player.ManaPercentage() >= GetValue<Slider>(mode, "WMpA").Value)
                {
                    if (W.GetTarget(GetValue<Slider>("Misc", "WExtraRange").Value) != null)
                    {
                        if (!Player.HasBuff("AuraofDespair"))
                        {
                            W.Cast(PacketCast);
                        }
                    }
                    else if (Player.HasBuff("AuraofDespair"))
                    {
                        W.Cast(PacketCast);
                    }
                }
                else if (Player.HasBuff("AuraofDespair"))
                {
                    W.Cast(PacketCast);
                }
            }
        }

        private void Clear()
        {
            SmiteMob();
            var minionObj = MinionManager.GetMinions(
                Q.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth);
            if (minionObj.Count == 0)
            {
                if (GetValue<bool>("Clear", "W") && W.IsReady() && Player.HasBuff("AuraofDespair"))
                {
                    W.Cast(PacketCast);
                }
                return;
            }
            if (GetValue<bool>("Clear", "E") && E.IsReady() && minionObj.Count(i => E.IsInRange(i)) > 0 &&
                E.Cast(PacketCast))
            {
                return;
            }
            if (GetValue<bool>("Clear", "W") && W.IsReady())
            {
                if (Player.ManaPercentage() >= GetValue<Slider>("Clear", "WMpA").Value)
                {
                    if (minionObj.Count(i => W.IsInRange(i, W.Range + GetValue<Slider>("Misc", "WExtraRange").Value)) >
                        1 ||
                        minionObj.Count(
                            i =>
                                i.MaxHealth >= 1200 &&
                                W.IsInRange(i, W.Range + GetValue<Slider>("Misc", "WExtraRange").Value)) > 0)
                    {
                        if (!Player.HasBuff("AuraofDespair") && W.Cast(PacketCast))
                        {
                            return;
                        }
                    }
                    else if (Player.HasBuff("AuraofDespair") && W.Cast(PacketCast))
                    {
                        return;
                    }
                }
                else if (Player.HasBuff("AuraofDespair") && W.Cast(PacketCast))
                {
                    return;
                }
            }
            if (GetValue<bool>("Clear", "Q") && Q.IsReady())
            {
                var obj = minionObj.Cast<Obj_AI_Minion>().Find(i => CanKill(i, Q)) ??
                          minionObj.Find(i => !Orbwalk.InAutoAttackRange(i));
                if (obj != null)
                {
                    Q.CastIfHitchanceEquals(obj, HitChance.Medium, PacketCast);
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
                if (target != null && CanKill(target, E) && E.Cast(PacketCast))
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