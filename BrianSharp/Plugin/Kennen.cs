using System;
using System.Drawing;
using System.Linq;
using BrianSharp.Common;
using LeagueSharp;
using LeagueSharp.Common;
using Orbwalk = BrianSharp.Common.Orbwalker;

namespace BrianSharp.Plugin
{
    internal class Kennen : Helper
    {
        public Kennen()
        {
            Q = new Spell(SpellSlot.Q, 1050);
            W = new Spell(SpellSlot.W, 910);
            E = new Spell(SpellSlot.E);
            R = new Spell(SpellSlot.R, 560);
            Q.SetSkillshot(0.125f, 50, 1700, true, SkillshotType.SkillshotLine);
            W.SetSkillshot(0.5f, 910, float.MaxValue, false, SkillshotType.SkillshotCircle);
            R.SetSkillshot(0.5f, 560, 779.9f, false, SkillshotType.SkillshotCircle);

            var champMenu = new Menu("Plugin", PlayerName + "_Plugin");
            {
                var comboMenu = new Menu("Combo", "Combo");
                {
                    AddItem(comboMenu, "Q", "Use Q");
                    AddItem(comboMenu, "W", "Use W");
                    AddItem(comboMenu, "R", "Use R");
                    AddItem(comboMenu, "RHpU", "-> If Enemy Hp Under", 60);
                    AddItem(comboMenu, "RCountA", "-> If Enemy Above", 2, 1, 5);
                    AddItem(comboMenu, "RItem", "-> Use Zhonya When R Active");
                    AddItem(comboMenu, "RItemHpU", "--> If Hp Under", 60);
                    champMenu.AddSubMenu(comboMenu);
                }
                var harassMenu = new Menu("Harass", "Harass");
                {
                    AddItem(harassMenu, "AutoQ", "Auto Q", "H", KeyBindType.Toggle);
                    AddItem(harassMenu, "AutoQMpA", "-> If Mp Above", 50);
                    AddItem(harassMenu, "Q", "Use Q");
                    AddItem(harassMenu, "W", "Use W");
                    AddItem(harassMenu, "WMpA", "-> If Mp Above", 50);
                    champMenu.AddSubMenu(harassMenu);
                }
                var clearMenu = new Menu("Clear", "Clear");
                {
                    AddItem(clearMenu, "Q", "Use Q");
                    AddItem(clearMenu, "W", "Use W");
                    AddItem(clearMenu, "WHitA", "-> If Hit Above", 2, 1, 5);
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
                    AddItem(fleeMenu, "W", "Use W To Stun Enemy");
                    champMenu.AddSubMenu(fleeMenu);
                }
                var miscMenu = new Menu("Misc", "Misc");
                {
                    var killStealMenu = new Menu("Kill Steal", "KillSteal");
                    {
                        AddItem(killStealMenu, "Q", "Use Q");
                        AddItem(killStealMenu, "W", "Use W");
                        AddItem(killStealMenu, "R", "Use R");
                        AddItem(killStealMenu, "Ignite", "Use Ignite");
                        miscMenu.AddSubMenu(killStealMenu);
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
                    champMenu.AddSubMenu(miscMenu);
                }
                var drawMenu = new Menu("Draw", "Draw");
                {
                    AddItem(drawMenu, "Q", "Q Range", false);
                    AddItem(drawMenu, "W", "W Range", false);
                    AddItem(drawMenu, "R", "R Range", false);
                    champMenu.AddSubMenu(drawMenu);
                }
                MainMenu.AddSubMenu(champMenu);
            }
            Game.OnUpdate += OnGameUpdate;
            Drawing.OnDraw += OnDraw;
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
                    Flee();
                    break;
            }
            AutoQ();
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
            if (GetValue<bool>("Draw", "R") && R.Level > 0)
            {
                Render.Circle.DrawCircle(Player.Position, R.Range, R.IsReady() ? Color.Green : Color.Red);
            }
        }

        private void OnPossibleToInterrupt(Obj_AI_Hero unit, InterruptableSpell spell)
        {
            if (Player.IsDead || !GetValue<bool>("Interrupt", "W") ||
                !GetValue<bool>("Interrupt", unit.ChampionName + "_" + spell.Slot) || !W.CanCast(unit) ||
                !unit.HasBuff("KennenMarkOfStorm"))
            {
                return;
            }
            if (HaveWStun(unit))
            {
                W.Cast(PacketCast);
            }
            else if (!HaveWStun(unit))
            {
                Q.CastIfHitchanceEquals(unit, HitChance.High, PacketCast);
            }
        }

        private void Fight(string mode)
        {
            if (GetValue<bool>(mode, "Q") && Q.CastOnBestTarget(0, PacketCast).IsCasted())
            {
                return;
            }
            if (GetValue<bool>(mode, "W") && W.IsReady() &&
                HeroManager.Enemies.Any(i => i.IsValidTarget(W.Range) && i.HasBuff("KennenMarkOfStorm")) &&
                (mode == "Combo" || Player.ManaPercentage() >= GetValue<Slider>(mode, "WMpA").Value))
            {
                if (Player.HasBuff("KennenShurikenStorm"))
                {
                    var target =
                        HeroManager.Enemies.Where(i => i.IsValidTarget(W.Range) && i.HasBuff("KennenMarkOfStorm"))
                            .ToList();
                    if ((target.Count(i => CanKill(i, W, 1)) > 0 || target.Count(HaveWStun) > 1 || target.Count > 2 ||
                         (target.Count(HaveWStun) == 1 && target.Count(i => !HaveWStun(i)) > 0)) && W.Cast(PacketCast))
                    {
                        return;
                    }
                }
                else if (W.Cast(PacketCast))
                {
                    return;
                }
            }
            if (mode == "Combo" && GetValue<bool>(mode, "R"))
            {
                if (R.IsReady())
                {
                    var target = HeroManager.Enemies.Where(i => i.IsValidTarget(R.Range)).ToList();
                    if ((target.Count > 1 && target.Count(i => CanKill(i, R, GetRDmg(i))) > 0) ||
                        (target.Count > 1 &&
                         target.Count(i => i.HealthPercent < GetValue<Slider>(mode, "RHpU").Value) > 0) ||
                        target.Count >= GetValue<Slider>(mode, "RCountA").Value)
                    {
                        R.Cast(PacketCast);
                    }
                }
                else if (Player.HasBuff("KennenShurikenStorm") && GetValue<bool>(mode, "RItem") &&
                         Player.HealthPercent < GetValue<Slider>(mode, "RItemHpU").Value && R.GetTarget() != null &&
                         Zhonya.IsReady())
                {
                    Zhonya.Cast();
                }
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
                          minionObj.Find(i => i.MaxHealth >= 1200);
                if (obj != null && Q.CastIfHitchanceEquals(obj, HitChance.Medium, PacketCast))
                {
                    return;
                }
            }
            if (GetValue<bool>("Clear", "W") && W.IsReady() &&
                minionObj.Count(i => W.IsInRange(i) && i.HasBuff("KennenMarkOfStorm")) >=
                GetValue<Slider>("Clear", "WHitA").Value)
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
            Q.CastIfHitchanceEquals(obj, HitChance.High, PacketCast);
        }

        private void Flee()
        {
            if (GetValue<bool>("Flee", "E") && E.IsReady() && E.Instance.Name == "KennenLightningRush" &&
                E.Cast(PacketCast))
            {
                return;
            }
            if (GetValue<bool>("Flee", "W") && W.IsReady() &&
                HeroManager.Enemies.Any(i => i.IsValidTarget(W.Range) && i.HasBuff("KennenMarkOfStorm") && HaveWStun(i)))
            {
                W.Cast(PacketCast);
            }
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
                if (target != null && CanKill(target, Q) && Q.CastIfHitchanceEquals(target, HitChance.High, PacketCast))
                {
                    return;
                }
            }
            if (GetValue<bool>("KillSteal", "W") && W.IsReady())
            {
                var target = W.GetTarget();
                if (target != null && target.HasBuff("KennenMarkOfStorm") && CanKill(target, W, 1) && W.Cast(PacketCast))
                {
                    return;
                }
            }
            if (GetValue<bool>("KillSteal", "R") && R.IsReady())
            {
                var target = R.GetTarget();
                if (target != null && CanKill(target, R, GetRDmg(target)))
                {
                    R.Cast(PacketCast);
                }
            }
        }

        private double GetRDmg(Obj_AI_Hero target)
        {
            return Player.CalcDamage(
                target, Damage.DamageType.Magical,
                (new double[] { 80, 145, 210 }[R.Level - 1] + 0.4 * Player.FlatMagicDamageMod) * 3);
        }

        private bool HaveWStun(Obj_AI_Base target)
        {
            return target.Buffs.First(a => a.DisplayName == "KennenMarkOfStorm").Count == 2;
        }
    }
}