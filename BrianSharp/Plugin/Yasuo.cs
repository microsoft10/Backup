using System;
using System.Collections.Generic;
using System.Linq;
using BrianSharp.Common;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;
using Orbwalk = BrianSharp.Common.Orbwalker;

namespace BrianSharp.Plugin
{
    internal class Yasuo : Helper
    {
        private bool _isDashing;

        public Yasuo()
        {
            Q = new Spell(SpellSlot.Q, 475);
            Q2 = new Spell(SpellSlot.Q, 1075);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 475);
            R = new Spell(SpellSlot.R, 1300);
            Q.SetSkillshot(GetQDelay, 55, float.MaxValue, false, SkillshotType.SkillshotLine);
            Q2.SetSkillshot(GetQDelay, 90, 1500, false, SkillshotType.SkillshotLine);
            E.SetSkillshot(0.1f, 375, 700 + Player.MoveSpeed, false, SkillshotType.SkillshotCircle);
            R.SetSkillshot(0.5f, 400, float.MaxValue, false, SkillshotType.SkillshotCircle);

            var champMenu = new Menu("Plugin", PlayerName + "_Plugin");
            {
                var comboMenu = new Menu("Combo", "Combo");
                {
                    AddItem(comboMenu, "Q", "Use Q");
                    AddItem(comboMenu, "QAir", "-> On Air");
                    AddItem(comboMenu, "E", "Use E");
                    AddItem(comboMenu, "EDmg", "-> Deal Damage");
                    AddItem(comboMenu, "EDmgRange", "--> If Enemy Not In", 250, 1, 475);
                    AddItem(comboMenu, "EGap", "-> Gap Closer");
                    AddItem(comboMenu, "EGapRange", "--> If Enemy Not In", 300, 1, 475);
                    AddItem(comboMenu, "EGapTower", "--> Under Tower", false);
                    AddItem(comboMenu, "R", "Use R");
                    AddItem(comboMenu, "RDelay", "-> Delay");
                    AddItem(comboMenu, "RDelayTime", "--> Time (ms)", 200, 100, 400);
                    AddItem(comboMenu, "RHpU", "-> If Enemy Hp Under", 50);
                    AddItem(comboMenu, "RCountA", "-> If Enemy Above", 2, 1, 5);
                    champMenu.AddSubMenu(comboMenu);
                }
                var harassMenu = new Menu("Harass", "Harass");
                {
                    AddItem(harassMenu, "AutoQ", "Auto Q", "H", KeyBindType.Toggle);
                    AddItem(harassMenu, "AutoQ3", "-> Use Q3");
                    AddItem(harassMenu, "AutoQTower", "-> Under Tower", false);
                    AddItem(harassMenu, "Q", "Use Q");
                    AddItem(harassMenu, "Q3", "-> Use Q3");
                    AddItem(harassMenu, "QTower", "-> Under Tower", false);
                    AddItem(harassMenu, "QLastHit", "-> Last Hit (Q1/Q2)");
                    AddItem(harassMenu, "E", "Use E");
                    AddItem(harassMenu, "ERange", "-> If Enemy Not In", 250, 1, 475);
                    AddItem(harassMenu, "ETower", "-> Under Tower", false);
                    champMenu.AddSubMenu(harassMenu);
                }
                var clearMenu = new Menu("Clear", "Clear");
                {
                    AddItem(clearMenu, "Q", "Use Q");
                    AddItem(clearMenu, "Q3", "-> Use Q3");
                    AddItem(clearMenu, "E", "Use E");
                    AddItem(clearMenu, "ETower", "-> Under Tower", false);
                    AddItem(clearMenu, "Item", "Use Tiamat/Hydra Item");
                    champMenu.AddSubMenu(clearMenu);
                }
                var lastHitMenu = new Menu("Last Hit", "LastHit");
                {
                    AddItem(lastHitMenu, "Q", "Use Q");
                    AddItem(lastHitMenu, "Q3", "-> Use Q3");
                    AddItem(lastHitMenu, "E", "Use E");
                    AddItem(lastHitMenu, "ETower", "-> Under Tower", false);
                    champMenu.AddSubMenu(lastHitMenu);
                }
                var fleeMenu = new Menu("Flee", "Flee");
                {
                    AddItem(fleeMenu, "E", "Use E");
                    AddItem(fleeMenu, "EStackQ", "-> Stack Q");
                    champMenu.AddSubMenu(fleeMenu);
                }
                var miscMenu = new Menu("Misc", "Misc");
                {
                    new WindWall(miscMenu);
                    var killStealMenu = new Menu("Kill Steal", "KillSteal");
                    {
                        AddItem(killStealMenu, "Q", "Use Q");
                        AddItem(killStealMenu, "E", "Use E");
                        AddItem(killStealMenu, "R", "Use R");
                        AddItem(killStealMenu, "Ignite", "Use Ignite");
                        miscMenu.AddSubMenu(killStealMenu);
                    }
                    var interruptMenu = new Menu("Interrupt", "Interrupt");
                    {
                        AddItem(interruptMenu, "Q", "Use Q3");
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
                    AddItem(miscMenu, "StackQ", "Auto Stack Q", "Z", KeyBindType.Toggle);
                    AddItem(miscMenu, "StackQDraw", "-> Draw Text");
                    champMenu.AddSubMenu(miscMenu);
                }
                var drawMenu = new Menu("Draw", "Draw");
                {
                    AddItem(drawMenu, "Q", "Q Range", false);
                    AddItem(drawMenu, "E", "E Range", false);
                    AddItem(drawMenu, "R", "R Range", false);
                    champMenu.AddSubMenu(drawMenu);
                }
                MainMenu.AddSubMenu(champMenu);
            }
            Game.OnUpdate += OnGameUpdate;
            Drawing.OnDraw += OnDraw;
            Interrupter.OnPossibleToInterrupt += OnPossibleToInterrupt;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            Obj_AI_Base.OnPlayAnimation += OnPlayAnimation;
        }

        private bool HaveQ3
        {
            get { return Player.HasBuff("YasuoQ3W"); }
        }

        private float GetQDelay
        {
            get { return 1 / (1 / 0.5f * Player.AttackSpeedMod); }
        }

        private void OnGameUpdate(EventArgs args)
        {
            if (Player.IsDead || MenuGUI.IsChatOpen || Player.IsRecalling())
            {
                return;
            }
            if (!Equals(Q.Delay, GetQDelay))
            {
                Q.Delay = Q2.Delay = GetQDelay;
            }
            if (!Equals(E.Speed, 700 + Player.MoveSpeed))
            {
                E.Speed = 700 + Player.MoveSpeed;
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
            StackQ();
        }

        private void OnDraw(EventArgs args)
        {
            if (Player.IsDead)
            {
                return;
            }
            if (GetValue<KeyBind>("Misc", "StackQ").Active && GetValue<bool>("Misc", "StackQDraw"))
            {
                var pos = Drawing.WorldToScreen(Player.Position);
                Drawing.DrawText(pos.X, pos.Y, Color.Orange, "Auto Stack Q");
            }
            if (GetValue<bool>("Draw", "Q") && Q.Level > 0)
            {
                Render.Circle.DrawCircle(
                    Player.Position, (HaveQ3 ? Q2 : Q).Range, Q.IsReady() ? Color.Green : Color.Red);
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
            if (Player.IsDead || !GetValue<bool>("Interrupt", "Q") ||
                !GetValue<bool>("Interrupt", unit.ChampionName + "_" + spell.Slot) || !HaveQ3)
            {
                return;
            }
            if (E.IsReady() && (Q.IsReady() || Q.IsReady(150)))
            {
                if (E.IsInRange(unit) && CanCastE(unit) && InQCir(unit, PosAfterE(unit)) &&
                    E.CastOnUnit(unit, PacketCast))
                {
                    return;
                }
                if (E.IsInRange(unit, E.Range + E.Width))
                {
                    var obj = GetNearObj(unit, true);
                    if (obj != null && E.CastOnUnit(obj, PacketCast))
                    {
                        return;
                    }
                }
            }
            if (!Q.IsReady())
            {
                return;
            }
            if (_isDashing)
            {
                if (GetQCirObj(true).Count > 0)
                {
                    Q2.Cast(unit.ServerPosition, PacketCast);
                }
            }
            else
            {
                Q2.CastIfHitchanceEquals(unit, HitChance.High, PacketCast);
            }
        }

        private void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe)
            {
                return;
            }
            if (GetValue<bool>("Combo", "Q") && GetValue<bool>("Combo", "QAir") &&
                args.SData.Name == "YasuoRKnockUpComboW" && (Q.IsReady() || Q.IsReady(1050)))
            {
                Utility.DelayAction.Add(1050, () => Q.CastOnBestTarget(0, PacketCast));
            }
        }

        private void OnPlayAnimation(GameObject sender, GameObjectPlayAnimationEventArgs args)
        {
            if (!sender.IsMe)
            {
                return;
            }
            if (args.Animation == "Spell3")
            {
                _isDashing = true;
                Utility.DelayAction.Add(
                    (int) (475000 / E.Speed), () =>
                    {
                        if (_isDashing)
                        {
                            _isDashing = false;
                        }
                    });
            }
            else
            {
                _isDashing = false;
            }
        }

        private void Fight(string mode)
        {
            if (mode == "Combo" && GetValue<bool>(mode, "R") && R.IsReady())
            {
                var obj = HeroManager.Enemies.Where(CanCastR).ToList();
                var target = obj.Find(i => i.GetEnemiesInRange(R.Width).Count(CanCastR) > 1 && CanKill(i, R)) ??
                             obj.Find(
                                 i =>
                                     i.GetEnemiesInRange(R.Width).Count(CanCastR) > 1 &&
                                     i.GetEnemiesInRange(R.Width)
                                         .Count(
                                             a =>
                                                 CanCastR(a) &&
                                                 a.HealthPercent < GetValue<Slider>(mode, "RHpU").Value) > 0) ??
                             obj.Find(
                                 i =>
                                     i.GetEnemiesInRange(R.Width).Count(CanCastR) >=
                                     GetValue<Slider>(mode, "RCountA").Value);
                if (target != null && (!GetValue<bool>(mode, "RDelay") || DelayR(target)) &&
                    R.Cast(target.ServerPosition, PacketCast))
                {
                    return;
                }
            }
            if (GetValue<bool>(mode, "E") && E.IsReady())
            {
                if (mode == "Combo" && GetValue<bool>(mode, "EGap"))
                {
                    var target = R.GetTarget();
                    if (target != null && (!UnderTower(target.ServerPosition) || GetValue<bool>(mode, "EGapTower")) &&
                        Player.Distance(target) > GetValue<Slider>(mode, "EGapRange").Value)
                    {
                        var obj = GetNearObj(target);
                        if (obj != null && E.CastOnUnit(obj, PacketCast))
                        {
                            return;
                        }
                    }
                }
                if ((mode == "Combo" && GetValue<bool>(mode, "EDmg")) ||
                    (mode == "Harass" && (!UnderTower(Player.ServerPosition) || GetValue<bool>(mode, "ETower"))))
                {
                    var target = E.GetTarget();
                    if (target != null &&
                        Player.Distance(target) >
                        GetValue<Slider>(mode, "E" + (mode == "Harass" ? "" : "Dmg") + "Range").Value)
                    {
                        var eBuff = Player.Buffs.Find(i => i.DisplayName == "YasuoDashScalar");
                        if (eBuff != null && eBuff.Count == 2 && CanCastE(target) && E.CastOnUnit(target, PacketCast))
                        {
                            return;
                        }
                        var obj = GetNearObj(target);
                        if (GetValue<bool>(mode, "Q") && (Q.IsReady() || Q.IsReady(150)) &&
                            GetNearObj(target, true) != null)
                        {
                            obj = GetNearObj(target, true);
                        }
                        if (obj != null && E.CastOnUnit(obj, PacketCast))
                        {
                            return;
                        }
                    }
                }
            }
            if (GetValue<bool>(mode, "Q") && Q.IsReady())
            {
                if (mode == "Combo" ||
                    ((!HaveQ3 || GetValue<bool>(mode, "Q3")) &&
                     (!UnderTower(Player.ServerPosition) || GetValue<bool>(mode, "QTower"))))
                {
                    if (_isDashing)
                    {
                        if (GetQCirObj(true).Count > 0 && Q.Cast(Player.ServerPosition, PacketCast))
                        {
                            return;
                        }
                    }
                    else if ((HaveQ3 ? Q2 : Q).CastOnBestTarget(0, PacketCast).IsCasted())
                    {
                        return;
                    }
                }
                if (mode == "Harass" && GetValue<bool>(mode, "QLastHit") && Q.GetTarget(100) == null && !HaveQ3 &&
                    !_isDashing)
                {
                    var obj =
                        MinionManager.GetMinions(
                            Q.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth)
                            .Cast<Obj_AI_Minion>()
                            .Find(i => CanKill(i, Q));
                    if (obj != null)
                    {
                        Q.CastIfHitchanceEquals(obj, HitChance.High, PacketCast);
                    }
                }
            }
        }

        private void Clear()
        {
            if (GetValue<bool>("Clear", "E") && E.IsReady())
            {
                var minionObj =
                    MinionManager.GetMinions(E.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth)
                        .Where(i => CanCastE(i) && (!UnderTower(PosAfterE(i)) || GetValue<bool>("Clear", "ETower")))
                        .ToList();
                if (minionObj.Count > 0)
                {
                    var obj = (Obj_AI_Base) minionObj.Cast<Obj_AI_Minion>().Find(i => CanKill(i, E, GetEDmg(i)));
                    if (obj == null && GetValue<bool>("Clear", "Q") && (Q.IsReady() || Q.IsReady(150)) &&
                        (!HaveQ3 || GetValue<bool>("Clear", "Q3")))
                    {
                        var pos =
                            E.GetCircularFarmLocation(
                                MinionManager.GetMinions(
                                    E.Range + E.Width, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth));
                        if (pos.MinionsHit > 1)
                        {
                            obj = minionObj.MinOrDefault(i => i.Distance(pos.Position));
                        }
                    }
                    if (obj != null && E.CastOnUnit(obj, PacketCast))
                    {
                        return;
                    }
                }
            }
            if (GetValue<bool>("Clear", "Q") && Q.IsReady() && (!HaveQ3 || GetValue<bool>("Clear", "Q3")))
            {
                if (_isDashing)
                {
                    if ((GetQCirObj(true).Count > 0 || GetQCirObj().Count > 1) &&
                        Q.Cast(Player.ServerPosition, PacketCast))
                    {
                        return;
                    }
                }
                else
                {
                    var minionObj = MinionManager.GetMinions(
                        (HaveQ3 ? Q2 : Q).Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth);
                    if (minionObj.Count > 0)
                    {
                        var pos = (HaveQ3 ? Q2 : Q).GetLineFarmLocation(minionObj);
                        var obj = minionObj.Cast<Obj_AI_Minion>().Find(i => CanKill(i, Q));
                        if (obj != null && !HaveQ3)
                        {
                            if (Q.CastIfHitchanceEquals(obj, HitChance.High, PacketCast))
                            {
                                return;
                            }
                        }
                        else if (pos.MinionsHit > 0 && Q.Cast(pos.Position, PacketCast))
                        {
                            return;
                        }
                    }
                }
            }
            if (GetValue<bool>("Clear", "Item") && (Hydra.IsReady() || Tiamat.IsReady()))
            {
                var minionObj = MinionManager.GetMinions(
                    (Hydra.IsReady() ? Hydra : Tiamat).Range, MinionTypes.All, MinionTeam.NotAlly);
                if (minionObj.Count > 2 ||
                    minionObj.Any(
                        i => i.MaxHealth >= 1200 && i.Distance(Player) < (Hydra.IsReady() ? Hydra : Tiamat).Range - 80))
                {
                    if (Tiamat.IsReady())
                    {
                        Tiamat.Cast();
                    }
                    if (Hydra.IsReady())
                    {
                        Hydra.Cast();
                    }
                }
            }
        }

        private void LastHit()
        {
            if (GetValue<bool>("LastHit", "Q") && Q.IsReady() && !_isDashing &&
                (!HaveQ3 || GetValue<bool>("LastHit", "Q3")))
            {
                var obj =
                    MinionManager.GetMinions(
                        (HaveQ3 ? Q2 : Q).Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth)
                        .Cast<Obj_AI_Minion>()
                        .Find(i => CanKill(i, HaveQ3 ? Q2 : Q));
                if (obj != null && (HaveQ3 ? Q2 : Q).CastIfHitchanceEquals(obj, HitChance.High, PacketCast))
                {
                    return;
                }
            }
            if (GetValue<bool>("LastHit", "E") && E.IsReady())
            {
                var obj =
                    MinionManager.GetMinions(E.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth)
                        .Cast<Obj_AI_Minion>()
                        .Where(
                            i =>
                                CanCastE(i) &&
                                (!Orbwalk.InAutoAttackRange(i) || i.Health > Player.GetAutoAttackDamage(i, true)) &&
                                (!UnderTower(PosAfterE(i)) || GetValue<bool>("LastHit", "ETower")))
                        .Find(i => CanKill(i, E, GetEDmg(i)));
                if (obj != null)
                {
                    E.CastOnUnit(obj, PacketCast);
                }
            }
        }

        private void Flee()
        {
            if (!GetValue<bool>("Flee", "E"))
            {
                return;
            }
            if (GetValue<bool>("Flee", "EStackQ") && Q.IsReady() && !HaveQ3 && _isDashing && GetQCirObj().Count > 0 &&
                Q.Cast(Player.ServerPosition, PacketCast))
            {
                return;
            }
            var obj = GetNearObj();
            if (obj == null || !E.IsReady())
            {
                return;
            }
            E.CastOnUnit(obj, PacketCast);
        }

        private void AutoQ()
        {
            if (!GetValue<KeyBind>("Harass", "AutoQ").Active || !Q.IsReady() || _isDashing ||
                (HaveQ3 && !GetValue<bool>("Harass", "AutoQ3")) ||
                (UnderTower(Player.ServerPosition) && !GetValue<bool>("Harass", "AutoQTower")))
            {
                return;
            }
            (HaveQ3 ? Q2 : Q).CastOnBestTarget(0, PacketCast);
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
                if (_isDashing)
                {
                    if (GetQCirObj(true).Cast<Obj_AI_Hero>().Count(i => CanKill(i, Q)) > 0 &&
                        Q.Cast(Player.ServerPosition, PacketCast))
                    {
                        return;
                    }
                }
                else
                {
                    var target = (HaveQ3 ? Q2 : Q).GetTarget();
                    if (target != null && CanKill(target, HaveQ3 ? Q2 : Q) &&
                        (HaveQ3 ? Q2 : Q).CastIfHitchanceEquals(target, HitChance.High, PacketCast))
                    {
                        return;
                    }
                }
            }
            if (GetValue<bool>("KillSteal", "E") && E.IsReady())
            {
                var target = E.GetTarget(0, HeroManager.Enemies.Where(i => !CanCastE(i)));
                if (target != null && CanKill(target, E, GetEDmg(target)) && E.CastOnUnit(target, PacketCast))
                {
                    return;
                }
            }
            if (GetValue<bool>("KillSteal", "R") && R.IsReady())
            {
                var target = R.GetTarget(0, HeroManager.Enemies.Where(i => !CanCastR(i)));
                if (target != null && CanKill(target, R))
                {
                    R.Cast(target.ServerPosition, PacketCast);
                }
            }
        }

        private void StackQ()
        {
            if (!GetValue<KeyBind>("Misc", "StackQ").Active || !Q.IsReady() || _isDashing || HaveQ3)
            {
                return;
            }
            var target = Q.GetTarget();
            if (target != null && !UnderTower(Player.ServerPosition))
            {
                Q.CastIfHitchanceEquals(target, HitChance.High, PacketCast);
            }
            else
            {
                var minionObj = MinionManager.GetMinions(
                    Q.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth);
                var obj = minionObj.Cast<Obj_AI_Minion>().Find(i => CanKill(i, Q)) ?? minionObj.FirstOrDefault();
                if (obj != null)
                {
                    Q.CastIfHitchanceEquals(obj, HitChance.High, PacketCast);
                }
            }
        }

        private bool CanCastE(Obj_AI_Base target)
        {
            return !target.HasBuff("YasuoDashWrapper");
        }

        private bool CanCastR(Obj_AI_Hero target)
        {
            return target.HasBuff("yasuoq3mis") || target.HasBuffOfType(BuffType.Knockup) ||
                   target.HasBuffOfType(BuffType.Knockback);
        }

        private bool DelayR(Obj_AI_Hero target)
        {
            var buff = target.Buffs.Find(i => i.Type == BuffType.Knockup) ??
                       target.Buffs.Find(i => i.Type == BuffType.Knockback);
            return buff != null &&
                   buff.EndTime - Game.Time < (float) GetValue<Slider>("Combo", "RDelayTime").Value / 1000;
        }

        private double GetEDmg(Obj_AI_Base target)
        {
            var eBuff = Player.Buffs.Find(i => i.DisplayName == "YasuoDashScalar");
            return Player.CalcDamage(
                target, Damage.DamageType.Magical,
                new[] { 70, 90, 110, 130, 150 }[E.Level - 1] * (1 + 0.25 * (eBuff != null ? eBuff.Count : 0)) +
                0.6 * Player.FlatMagicDamageMod);
        }

        private Obj_AI_Base GetNearObj(Obj_AI_Hero target = null, bool inQCir = false)
        {
            var pos = target != null ? target.ServerPosition : Game.CursorPos;
            return
                MinionManager.GetMinions(E.Range, MinionTypes.All, MinionTeam.NotAlly)
                    .Where(
                        i =>
                            CanCastE(i) &&
                            (!inQCir ? pos.Distance(PosAfterE(i)) < Player.Distance(pos) : InQCir(target, PosAfterE(i))))
                    .MinOrDefault(i => pos.Distance(PosAfterE(i))) ??
                HeroManager.Enemies.Where(
                    i =>
                        i.IsValidTarget(E.Range) && CanCastE(i) &&
                        (!inQCir ? pos.Distance(PosAfterE(i)) < Player.Distance(pos) : InQCir(target, PosAfterE(i))))
                    .MinOrDefault(i => pos.Distance(PosAfterE(i)));
        }

        private List<Obj_AI_Base> GetQCirObj(bool onlyHero = false)
        {
            var heroObj =
                HeroManager.Enemies.Where(i => i.IsValidTarget() && InQCir(i, Player.ServerPosition))
                    .Cast<Obj_AI_Base>()
                    .ToList();
            var minionObj =
                MinionManager.GetMinions(float.MaxValue, MinionTypes.All, MinionTeam.NotAlly)
                    .Where(i => InQCir(i, Player.ServerPosition))
                    .ToList();
            return onlyHero ? heroObj : (heroObj.Count > 0 ? heroObj : minionObj);
        }

        private Vector3 PosAfterE(Obj_AI_Base target)
        {
            return Player.ServerPosition.Extend(target.ServerPosition, E.Range);
        }

        private bool InQCir(Obj_AI_Base target, Vector3 pos)
        {
            return
                Prediction.GetPrediction(
                    target, (E.Delay * 1000 + Player.Distance(target) / E.Speed - 100) / 1000, 0, target.MoveSpeed)
                    .UnitPosition.Distance(pos) <= E.Width;
        }

        private bool UnderTower(Vector3 pos)
        {
            return ObjectManager.Get<Obj_AI_Turret>().Any(i => i.IsEnemy && !i.IsDead && i.Distance(pos) <= 890);
        }

        private class WindWall
        {
            private readonly string[] _castType2 =
            {
                "blindmonkqtwo", "blindmonkwtwo", "blindmonketwo",
                "infernalguardianguide", "KennenMegaProc", "sonawattackupgrade", "redcardpreattack", "fizzjumptwo",
                "fizzjumpbuffer", "gragasbarrelrolltoggle", "LeblancSlideM", "luxlightstriketoggle",
                "UrgotHeatseekingHomeMissile", "xeratharcanopulseextended", "xeratharcanopulsedamageextended",
                "XenZhaoThrust3", "ziggswtoggle", "khazixwlong", "khazixelong", "renektondice", "SejuaniNorthernWinds",
                "shyvanafireballdragon2", "shyvanaimmolatedragon", "ShyvanaDoubleAttackHitDragon",
                "talonshadowassaulttoggle", "viktorchaosstormguide", "zedw2", "ZedR2", "khazixqlong",
                "AatroxWONHAttackLife", "viktorqbuff"
            };

            private readonly string[] _castType3 =
            {
                "sonaeattackupgrade", "bluecardpreattack", "LeblancSoulShackleM",
                "UdyrPhoenixStance", "RenektonSuperExecute"
            };

            private readonly string[] _castType4 =
            {
                "FrostShot", "PowerFist", "DariusNoxianTacticsONH", "EliseR",
                "JaxEmpowerTwo", "JaxRelentlessAssault", "JayceStanceHtG", "jaycestancegth", "jaycehypercharge",
                "JudicatorRighteousFury", "kennenlrcancel", "KogMawBioArcaneBarrage", "LissandraE",
                "MordekaiserMaceOfSpades", "mordekaisercotgguide", "NasusQ", "Takedown", "NocturneParanoia", "QuinnR",
                "RengarQ", "HallucinateFull", "DeathsCaressFull", "SivirW", "ThreshQInternal", "threshqinternal",
                "PickACard", "goldcardlock", "redcardlock", "bluecardlock", "FullAutomatic", "VayneTumble",
                "MonkeyKingDoubleAttack", "YorickSpectral", "ViE", "VorpalSpikes", "FizzSeastonePassive", "GarenSlash3",
                "HecarimRamp", "leblancslidereturn", "leblancslidereturnm", "Obduracy", "UdyrTigerStance",
                "UdyrTurtleStance", "UdyrBearStance", "UrgotHeatseekingMissile", "XenZhaoComboTarget", "dravenspinning",
                "dravenrdoublecast", "FioraDance", "LeonaShieldOfDaybreak", "MaokaiDrain3", "NautilusPiercingGaze",
                "RenektonPreExecute", "RivenFengShuiEngine", "ShyvanaDoubleAttack", "shyvanadoubleattackdragon",
                "SyndraW", "TalonNoxianDiplomacy", "TalonCutthroat", "talonrakemissileone", "TrundleTrollSmash",
                "VolibearQ", "AatroxW", "aatroxw2", "AatroxWONHAttackLife", "JinxQ", "GarenQ", "yasuoq",
                "XerathArcanopulseChargeUp", "XerathLocusOfPower2", "xerathlocuspulse", "velkozqsplitactivate",
                "NetherBlade", "GragasQToggle", "GragasW", "SionW", "sionpassivespeed"
            };

            private readonly string[] _castType5 = { "VarusQ", "ZacE", "ViQ", "SionQ" };

            private readonly string[] _castType6 =
            {
                "VelkozQMissile", "KogMawQMis", "RengarEFinal", "RengarEFinalMAX",
                "BraumQMissile", "KarthusDefileSoundDummy2", "gnarqmissile", "GnarBigQMissile", "SorakaWParticleMissile"
            };

            private readonly List<WindWallData> _listWindWall = new List<WindWallData>();

            private readonly string[] _spellsE =
            {
                "KogMawVoidOozeMissile", "ToxicShotAttack", "LeonaZenithBladeMissile",
                "PowerFistAttack", "VayneCondemnMissile", "ShyvanaFireballMissile", "maokaisapling2boom",
                "VarusEMissile", "CaitlynEntrapmentMissile", "jayceaccelerationgate", "syndrae5",
                "JudicatorRighteousFuryAttack", "UdyrBearAttack", "RumbleGrenadeMissile", "Slash", "hecarimrampattack",
                "ziggse2", "UrgotPlasmaGrenadeBoom", "SkarnerFractureMissile", "YorickSummonRavenous", "BlindMonkEOne",
                "EliseHumanE", "PrimalSurge", "Swipe", "ViEAttack", "LissandraEMissile", "yasuodummyspell",
                "XerathMageSpearMissile", "RengarEFinal", "RengarEFinalMAX", "KarthusDefileSoundDummy2"
            };

            private readonly string[] _spellsQ =
            {
                "TrundleQ", "LeonaShieldOfDaybreakAttack", "XenZhaoThrust",
                "NautilusAnchorDragMissile", "RocketGrabMissile", "VayneTumbleAttack", "VayneTumbleUltAttack",
                "NidaleeTakedownAttack", "ShyvanaDoubleAttackHit", "ShyvanaDoubleAttackHitDragon", "frostarrow",
                "FrostArrow", "MonkeyKingQAttack", "MaokaiTrunkLineMissile", "FlashFrostSpell",
                "xeratharcanopulsedamage", "xeratharcanopulsedamageextended", "xeratharcanopulsedarkiron",
                "xeratharcanopulsediextended", "SpiralBladeMissile", "EzrealMysticShotMissile",
                "EzrealMysticShotPulseMissile", "jayceshockblast", "BrandBlazeMissile", "UdyrTigerAttack",
                "TalonNoxianDiplomacyAttack", "LuluQMissile", "GarenSlash2", "VolibearQAttack", "dravenspinningattack",
                "karmaheavenlywavec", "ZiggsQSpell", "UrgotHeatseekingHomeMissile", "UrgotHeatseekingLineMissile",
                "JavelinToss", "RivenTriCleave", "namiqmissile", "NasusQAttack", "BlindMonkQOne", "ThreshQInternal",
                "threshqinternal", "QuinnQMissile", "LissandraQMissile", "EliseHumanQ", "GarenQAttack", "JinxQAttack",
                "JinxQAttack2", "yasuoq", "xeratharcanopulse2", "VelkozQMissile", "KogMawQMis", "BraumQMissile",
                "KarthusLayWasteA1", "KarthusLayWasteA2", "KarthusLayWasteA3", "karthuslaywastea3", "karthuslaywastea2",
                "karthuslaywastedeada1", "MaokaiSapling2Boom", "gnarqmissile", "GnarBigQMissile", "viktorqbuff"
            };

            private readonly string[] _spellsR =
            {
                "Pantheon_GrandSkyfall_Fall", "LuxMaliceCannonMis",
                "infiniteduresschannel", "JarvanIVCataclysmAttack", "jarvanivcataclysmattack", "VayneUltAttack",
                "RumbleCarpetBombDummy", "ShyvanaTransformLeap", "jaycepassiverangedattack", "jaycepassivemeleeattack",
                "jaycestancegth", "MissileBarrageMissile", "SprayandPrayAttack", "jaxrelentlessattack",
                "syndrarcasttime", "InfernalGuardian", "UdyrPhoenixAttack", "FioraDanceStrike", "xeratharcanebarragedi",
                "NamiRMissile", "HallucinateFull", "QuinnRFinale", "lissandrarenemy", "SejuaniGlacialPrisonCast",
                "yasuordummyspell", "xerathlocuspulse", "tempyasuormissile", "PantheonRFall"
            };

            private readonly string[] _spellsW =
            {
                "KogMawBioArcaneBarrageAttack", "SivirWAttack",
                "TwitchVenomCaskMissile", "gravessmokegrenadeboom", "mordekaisercreepingdeath", "DrainChannel",
                "jaycehypercharge", "redcardpreattack", "goldcardpreattack", "bluecardpreattack", "RenektonExecute",
                "RenektonSuperExecute", "EzrealEssenceFluxMissile", "DariusNoxianTacticsONHAttack", "UdyrTurtleAttack",
                "talonrakemissileone", "LuluWTwo", "ObduracyAttack", "KennenMegaProc", "NautilusWideswingAttack",
                "NautilusBackswingAttack", "XerathLocusOfPower", "yoricksummondecayed", "Bushwhack", "karmaspiritbondc",
                "SejuaniBasicAttackW", "AatroxWONHAttackLife", "AatroxWONHAttackPower", "JinxWMissile", "GragasWAttack",
                "braumwdummyspell", "syndrawcast", "SorakaWParticleMissile"
            };

            public WindWall(Menu menu)
            {
                SetupWindWallData();
                var windMenu = new Menu("Wind Wall", "WindWall");
                {
                    AddItem(windMenu, "W", "Use W");
                    AddItem(windMenu, "BAttack", "-> Basic Attack");
                    AddItem(windMenu, "CAttack", "-> Crit Attack");
                    foreach (var wwData in
                        HeroManager.Enemies.Where(i => _listWindWall.Find(a => a.ChampName == i.ChampionName) != null)
                            .SelectMany(obj => _listWindWall.FindAll(i => i.ChampName == obj.ChampionName)))
                    {
                        AddItem(
                            windMenu, wwData.ChampName + "_" + wwData.Slot,
                            "-> Skill " + wwData.Slot + " Of " + wwData.ChampName, false);
                    }
                }
                menu.AddSubMenu(windMenu);
                Obj_AI_Base.OnProcessSpellCast += WindWallDetect;
            }

            private void WindWallDetect(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
            {
                if (Player.IsDead || !sender.IsValid<Obj_AI_Hero>() || !sender.IsEnemy ||
                    !GetValue<bool>("WindWall", "W") || !W.IsReady())
                {
                    return;
                }
                var target = (Obj_AI_Hero) sender;
                var useW = false;
                var type = 0;
                var dist = 0f;
                var radius = 0f;
                var spellData = GetSpellType(target, args.SData.Name);
                if (spellData.Item2 == 4 || spellData.Item2 == 5 || spellData.Item2 == 6)
                {
                    return;
                }
                if (spellData.Item1 == SpellSlots.BAttack && GetValue<bool>("WindWall", "BAttack"))
                {
                    useW = true;
                }
                else if (spellData.Item1 == SpellSlots.CAttack && GetValue<bool>("WindWall", "CAttack"))
                {
                    useW = true;
                }
                else if ((spellData.Item1 == SpellSlots.Q || spellData.Item1 == SpellSlots.W ||
                          spellData.Item1 == SpellSlots.E || spellData.Item1 == SpellSlots.R) &&
                         GetItem("WindWall", target.ChampionName + "_" + spellData.Item1) != null &&
                         GetValue<bool>("WindWall", target.ChampionName + "_" + spellData.Item1))
                {
                    var wwData =
                        _listWindWall.Find(i => i.ChampName == target.ChampionName && i.Slot == spellData.Item1);
                    if (wwData != null)
                    {
                        useW = true;
                        type = wwData.Type;
                        dist = wwData.Distance;
                        radius = wwData.Radius;
                    }
                }
                if (!useW)
                {
                    return;
                }
                var willHit = false;
                switch (type)
                {
                    case 0:
                        willHit = args.Target.IsMe;
                        break;
                    case 1:
                        willHit = HitLine(sender.ServerPosition, args.End, radius, dist, true);
                        break;
                    case 2:
                        willHit = HitLine(sender.ServerPosition, args.End, radius, dist);
                        break;
                    case 3:
                        willHit = HitAoE(sender.ServerPosition, args.End, radius, dist);
                        break;
                    case 4:
                        willHit = HitCone(sender.ServerPosition, args.End, radius, dist);
                        break;
                    case 5:
                        willHit = HitWall(sender.ServerPosition, args.End, radius, dist);
                        break;
                    case 6:
                        willHit = HitLine(sender.ServerPosition, args.End, radius, dist, true) ||
                                  HitLine(
                                      sender.ServerPosition, sender.ServerPosition * 2 - args.End, radius, dist, true);
                        break;
                    case 7:
                        willHit = HitCone(args.End, sender.ServerPosition, radius, dist);
                        break;
                }
                if (!willHit)
                {
                    return;
                }
                W.Cast(args.Start, PacketCast);
            }

            private bool HitAoE(Vector3 pos1, Vector3 pos2, float radius, float dist)
            {
                return Player.Distance(dist > 0 && dist < pos1.Distance(pos2) ? pos1.Extend(pos2, dist) : pos2) <
                       radius + Player.BoundingRadius;
            }

            private bool HitLine(Vector3 pos1, Vector3 pos2, float radius, float dist, bool passed = false)
            {
                var pos = passed
                    ? pos1.Extend(pos2, dist)
                    : dist > 0 && dist < pos1.Distance(pos2) ? pos1.Extend(pos2, dist) : pos2;
                var point = Player.ServerPosition.To2D().ProjectOn(pos1.To2D(), pos.To2D()).SegmentPoint.To3D();
                return Player.Distance(point) < radius + Player.BoundingRadius &&
                       pos1.Distance(point) < pos1.Distance(pos) && pos.Distance(point) < pos1.Distance(pos);
            }

            private bool HitCone(Vector3 pos1, Vector3 pos2, float angle, float dist)
            {
                var pos = pos1.Extend(pos2, dist);
                var point = Player.ServerPosition.To2D().ProjectOn(pos1.To2D(), pos.To2D()).SegmentPoint.To3D();
                return Player.Distance(point) <
                       Math.Tan(Geometry.DegreeToRadian(angle)) * Player.Distance(pos1) + Player.BoundingRadius &&
                       pos1.Distance(point) < pos1.Distance(pos) && pos.Distance(point) < pos1.Distance(pos);
            }

            private bool HitWall(Vector3 pos1, Vector3 pos2, float radius, float maxradius)
            {
                var subPos1 = pos2.To2D() - (pos2 - pos1).To2D().Perpendicular().Normalized() * maxradius;
                var subPos2 = pos2.To2D() + (pos2 - pos1).To2D().Perpendicular().Normalized() * maxradius;
                var point = Player.ServerPosition.To2D().ProjectOn(subPos1, subPos2).SegmentPoint;
                return Player.Distance(point) < radius + Player.BoundingRadius &&
                       subPos1.Distance(point) < subPos1.Distance(subPos2) &&
                       subPos2.Distance(point) < subPos1.Distance(subPos2);
            }

            private void SetupWindWallData()
            {
                _listWindWall.Add(new WindWallData("Aatrox", SpellSlots.E, 1075, 35, 7));
                _listWindWall.Add(new WindWallData("Ahri", SpellSlots.Q, 1000, 100, 1));
                _listWindWall.Add(new WindWallData("Ahri", SpellSlots.W, 0, 550, 3));
                _listWindWall.Add(new WindWallData("Ahri", SpellSlots.E, 1000, 60, 1));
                _listWindWall.Add(new WindWallData("Ahri", SpellSlots.R, 0, 600, 3));
                _listWindWall.Add(new WindWallData("Akali", SpellSlots.Q, 0, 0, 0));
                _listWindWall.Add(new WindWallData("Amumu", SpellSlots.Q, 1100, 90, 1));
                _listWindWall.Add(new WindWallData("Anivia", SpellSlots.Q, 1100, 110, 1));
                _listWindWall.Add(new WindWallData("Anivia", SpellSlots.E, 0, 0, 0));
                _listWindWall.Add(new WindWallData("Annie", SpellSlots.Q, 0, 0, 0));
                _listWindWall.Add(new WindWallData("Ashe", SpellSlots.W, 1200, 60, 4));
                _listWindWall.Add(new WindWallData("Ashe", SpellSlots.R, 20000, 130, 1)); //9
                _listWindWall.Add(new WindWallData("Blitzcrank", SpellSlots.Q, 1050, 70, 1));
                _listWindWall.Add(new WindWallData("Brand", SpellSlots.Q, 1100, 60, 1));
                _listWindWall.Add(new WindWallData("Brand", SpellSlots.R, 0, 0, 0));
                _listWindWall.Add(new WindWallData("Braum", SpellSlots.Q, 1050, 60, 1));
                _listWindWall.Add(new WindWallData("Braum", SpellSlots.R, 1200, 115, 1));
                _listWindWall.Add(new WindWallData("Caitlyn", SpellSlots.Q, 1300, 90, 1));
                _listWindWall.Add(new WindWallData("Caitlyn", SpellSlots.E, 1000, 80, 1));
                _listWindWall.Add(new WindWallData("Caitlyn", SpellSlots.R, 0, 0, 0)); //8
                _listWindWall.Add(new WindWallData("Cassiopeia", SpellSlots.W, 0, 250, 3));
                _listWindWall.Add(new WindWallData("Cassiopeia", SpellSlots.E, 0, 0, 0));
                _listWindWall.Add(new WindWallData("Corki", SpellSlots.Q, 825, 250, 3));
                _listWindWall.Add(new WindWallData("Corki", SpellSlots.R, 1300, 40, 1));
                _listWindWall.Add(new WindWallData("Diana", SpellSlots.Q, 0, 205, 3));
                _listWindWall.Add(new WindWallData("Diana", SpellSlots.W, 0, 200, 3));
                _listWindWall.Add(new WindWallData("DrMundo", SpellSlots.Q, 1050, 60, 1));
                _listWindWall.Add(new WindWallData("Draven", SpellSlots.E, 1100, 130, 1));
                _listWindWall.Add(new WindWallData("Draven", SpellSlots.R, 20000, 160, 1)); //9
                _listWindWall.Add(new WindWallData("Elise", SpellSlots.Q, 0, 0, 0));
                _listWindWall.Add(new WindWallData("Elise", SpellSlots.E, 1100, 55, 1));
                _listWindWall.Add(new WindWallData("Evelynn", SpellSlots.Q, 0, 500, 3));
                _listWindWall.Add(new WindWallData("Ezreal", SpellSlots.Q, 1200, 60, 1));
                _listWindWall.Add(new WindWallData("Ezreal", SpellSlots.W, 1050, 80, 1));
                _listWindWall.Add(new WindWallData("Ezreal", SpellSlots.R, 20000, 160, 1)); //9
                _listWindWall.Add(new WindWallData("FiddleSticks", SpellSlots.E, 0, 0, 0));
                _listWindWall.Add(new WindWallData("Fizz", SpellSlots.R, 0, 120, 2));
                _listWindWall.Add(new WindWallData("Galio", SpellSlots.Q, 0, 200, 3));
                _listWindWall.Add(new WindWallData("Galio", SpellSlots.E, 1200, 120, 1));
                _listWindWall.Add(new WindWallData("Gangplank", SpellSlots.Q, 0, 0, 0));
                _listWindWall.Add(new WindWallData("Gnar", SpellSlots.Q, 1125, 60, 1));
                _listWindWall.Add(new WindWallData("Gragas", SpellSlots.Q, 1100, 275, 3));
                _listWindWall.Add(new WindWallData("Gragas", SpellSlots.R, 0, 375, 3));
                _listWindWall.Add(new WindWallData("Graves", SpellSlots.Q, 1000, 50, 4));
                _listWindWall.Add(new WindWallData("Graves", SpellSlots.W, 0, 250, 3));
                _listWindWall.Add(new WindWallData("Graves", SpellSlots.R, 1100, 100, 1));
                _listWindWall.Add(new WindWallData("Heimerdinger", SpellSlots.W, 1500, 70, 1));
                _listWindWall.Add(new WindWallData("Heimerdinger", SpellSlots.E, 0, 100, 3));
                _listWindWall.Add(new WindWallData("Irelia", SpellSlots.R, 1200, 65, 1));
                _listWindWall.Add(new WindWallData("Janna", SpellSlots.Q, 1700, 120, 1));
                _listWindWall.Add(new WindWallData("Janna", SpellSlots.W, 0, 0, 0));
                _listWindWall.Add(new WindWallData("Jayce", SpellSlots.Q, 1300, 70, 1));
                _listWindWall.Add(new WindWallData("Jinx", SpellSlots.W, 1500, 60, 1));
                _listWindWall.Add(new WindWallData("Jinx", SpellSlots.E, 210, 120, 5));
                _listWindWall.Add(new WindWallData("Jinx", SpellSlots.R, 20000, 140, 1)); //9
                _listWindWall.Add(new WindWallData("Kalista", SpellSlots.Q, 1200, 40, 1));
                _listWindWall.Add(new WindWallData("Karma", SpellSlots.Q, 950, 60, 1));
                _listWindWall.Add(new WindWallData("Kassadin", SpellSlots.Q, 0, 0, 0));
                _listWindWall.Add(new WindWallData("Katarina", SpellSlots.Q, 0, 0, 0));
                _listWindWall.Add(new WindWallData("Katarina", SpellSlots.R, 0, 550, 3));
                _listWindWall.Add(new WindWallData("Kayle", SpellSlots.Q, 0, 0, 0));
                _listWindWall.Add(new WindWallData("Kennen", SpellSlots.Q, 1050, 50, 1));
                _listWindWall.Add(new WindWallData("Khazix", SpellSlots.W, 1025, 73, 1));
                _listWindWall.Add(new WindWallData("KogMaw", SpellSlots.Q, 1200, 70, 1));
                _listWindWall.Add(new WindWallData("KogMaw", SpellSlots.E, 1360, 120, 1));
                _listWindWall.Add(new WindWallData("Leblanc", SpellSlots.Q, 0, 0, 0));
                _listWindWall.Add(new WindWallData("Leblanc", SpellSlots.E, 950, 70, 1));
                _listWindWall.Add(new WindWallData("LeeSin", SpellSlots.Q, 1100, 65, 1));
                _listWindWall.Add(new WindWallData("Leona", SpellSlots.E, 905, 100, 1));
                _listWindWall.Add(new WindWallData("Lissandra", SpellSlots.Q, 700, 80, 1));
                _listWindWall.Add(new WindWallData("Lissandra", SpellSlots.E, 0, 125, 2));
                _listWindWall.Add(new WindWallData("Lucian", SpellSlots.W, 1000, 80, 1));
                _listWindWall.Add(new WindWallData("Lucian", SpellSlots.R, 1400, 60, 1));
                _listWindWall.Add(new WindWallData("Lulu", SpellSlots.Q, 950, 60, 1));
                _listWindWall.Add(new WindWallData("Lulu", SpellSlots.W, 0, 0, 0));
                _listWindWall.Add(new WindWallData("Lux", SpellSlots.Q, 1300, 70, 1));
                _listWindWall.Add(new WindWallData("Lux", SpellSlots.E, 0, 275, 3));
                _listWindWall.Add(new WindWallData("Malphite", SpellSlots.Q, 0, 0, 0));
                _listWindWall.Add(new WindWallData("Maokai", SpellSlots.E, 0, 225, 3));
                _listWindWall.Add(new WindWallData("MissFortune", SpellSlots.Q, 0, 0, 0));
                _listWindWall.Add(new WindWallData("MissFortune", SpellSlots.R, 1400, 38, 4));
                _listWindWall.Add(new WindWallData("Morgana", SpellSlots.Q, 1300, 80, 2));
                _listWindWall.Add(new WindWallData("Nami", SpellSlots.Q, 0, 150, 3));
                _listWindWall.Add(new WindWallData("Nami", SpellSlots.W, 0, 0, 0));
                _listWindWall.Add(new WindWallData("Nami", SpellSlots.R, 2750, 260, 1));
                _listWindWall.Add(new WindWallData("Nautilus", SpellSlots.Q, 1100, 90, 1));
                _listWindWall.Add(new WindWallData("Nautilus", SpellSlots.E, 0, 600, 3));
                _listWindWall.Add(new WindWallData("Nidalee", SpellSlots.Q, 1500, 40, 1));
                _listWindWall.Add(new WindWallData("Nocturne", SpellSlots.Q, 1200, 80, 1));
                _listWindWall.Add(new WindWallData("Nunu", SpellSlots.E, 0, 0, 0));
                _listWindWall.Add(new WindWallData("Olaf", SpellSlots.Q, 1000, 105, 2));
                _listWindWall.Add(new WindWallData("Orianna", SpellSlots.Q, 0, 175, 3));
                _listWindWall.Add(new WindWallData("Pantheon", SpellSlots.Q, 0, 0, 0));
                _listWindWall.Add(new WindWallData("Quinn", SpellSlots.Q, 1050, 80, 1));
                _listWindWall.Add(new WindWallData("Rengar", SpellSlots.E, 1000, 70, 1));
                _listWindWall.Add(new WindWallData("Riven", SpellSlots.R, 1100, 125, 4));
                _listWindWall.Add(new WindWallData("Rumble", SpellSlots.E, 950, 60, 2));
                _listWindWall.Add(new WindWallData("Ryze", SpellSlots.Q, 0, 0, 0));
                _listWindWall.Add(new WindWallData("Ryze", SpellSlots.E, 0, 0, 0));
                _listWindWall.Add(new WindWallData("Sejuani", SpellSlots.R, 1100, 110, 1));
                _listWindWall.Add(new WindWallData("Shaco", SpellSlots.E, 0, 0, 0));
                _listWindWall.Add(new WindWallData("Shen", SpellSlots.Q, 0, 0, 0));
                _listWindWall.Add(new WindWallData("Shyvana", SpellSlots.E, 950, 60, 1));
                _listWindWall.Add(new WindWallData("Sion", SpellSlots.E, 800, 80, 1));
                _listWindWall.Add(new WindWallData("Sivir", SpellSlots.Q, 1250, 90, 1));
                _listWindWall.Add(new WindWallData("Skarner", SpellSlots.E, 1000, 70, 1));
                _listWindWall.Add(new WindWallData("Sona", SpellSlots.Q, 0, 680, 3));
                _listWindWall.Add(new WindWallData("Sona", SpellSlots.R, 1000, 140, 1));
                _listWindWall.Add(new WindWallData("Soraka", SpellSlots.Q, 0, 300, 3));
                _listWindWall.Add(new WindWallData("Swain", SpellSlots.E, 0, 0, 0));
                _listWindWall.Add(new WindWallData("Syndra", SpellSlots.E, 950, 90, 1));
                _listWindWall.Add(new WindWallData("Syndra", SpellSlots.R, 0, 0, 0));
                _listWindWall.Add(new WindWallData("Talon", SpellSlots.W, 800, 56, 4));
                _listWindWall.Add(new WindWallData("Talon", SpellSlots.R, 0, 500, 3));
                _listWindWall.Add(new WindWallData("Taric", SpellSlots.E, 0, 0, 0));
                _listWindWall.Add(new WindWallData("Teemo", SpellSlots.Q, 0, 0, 0));
                _listWindWall.Add(new WindWallData("Thresh", SpellSlots.Q, 1100, 70, 1));
                _listWindWall.Add(new WindWallData("Thresh", SpellSlots.E, 540, 110, 6));
                _listWindWall.Add(new WindWallData("Tristana", SpellSlots.E, 0, 0, 0));
                _listWindWall.Add(new WindWallData("Tristana", SpellSlots.R, 0, 200, 3));
                _listWindWall.Add(new WindWallData("TwistedFate", SpellSlots.Q, 1450, 40, 1));
                _listWindWall.Add(new WindWallData("Twitch", SpellSlots.W, 0, 275, 3));
                _listWindWall.Add(new WindWallData("Urgot", SpellSlots.Q, 1000, 60, 1));
                _listWindWall.Add(new WindWallData("Urgot", SpellSlots.E, 0, 210, 3));
                _listWindWall.Add(new WindWallData("Varus", SpellSlots.Q, 1800, 70, 1));
                _listWindWall.Add(new WindWallData("Varus", SpellSlots.E, 0, 235, 3));
                _listWindWall.Add(new WindWallData("Varus", SpellSlots.R, 1200, 120, 2));
                _listWindWall.Add(new WindWallData("Vayne", SpellSlots.E, 0, 0, 0));
                _listWindWall.Add(new WindWallData("Veigar", SpellSlots.Q, 0, 0, 0));
                _listWindWall.Add(new WindWallData("Veigar", SpellSlots.R, 0, 0, 0));
                _listWindWall.Add(new WindWallData("Velkoz", SpellSlots.Q, 1100, 50, 2));
                _listWindWall.Add(new WindWallData("Velkoz", SpellSlots.W, 1200, 88, 1));
                _listWindWall.Add(new WindWallData("Viktor", SpellSlots.Q, 0, 0, 0));
                _listWindWall.Add(new WindWallData("Viktor", SpellSlots.E, 1500, 80, 1));
                _listWindWall.Add(new WindWallData("Vladimir", SpellSlots.E, 0, 610, 3));
                _listWindWall.Add(new WindWallData("Xerath", SpellSlots.E, 1150, 60, 2));
                _listWindWall.Add(new WindWallData("Yasuo", SpellSlots.Q, 1075, 90, 1));
                _listWindWall.Add(new WindWallData("Zed", SpellSlots.Q, 925, 50, 1));
                _listWindWall.Add(new WindWallData("Ziggs", SpellSlots.Q, 0, 140, 3));
                _listWindWall.Add(new WindWallData("Ziggs", SpellSlots.W, 0, 275, 3));
                _listWindWall.Add(new WindWallData("Ziggs", SpellSlots.E, 0, 235, 3));
                _listWindWall.Add(new WindWallData("Zyra", SpellSlots.E, 1150, 70, 1));
            }

            private Tuple<SpellSlots, int> GetSpellType(Obj_AI_Hero target, string spellName)
            {
                var slot = SpellSlots.None;
                var type = 1;
                if ((!target.IsMelee() || (target.ChampionName == "Kayle" && target.AttackRange > 200)) &&
                    Orbwalking.IsAutoAttack(spellName) && !spellName.ToLower().Contains("crit"))
                {
                    slot = SpellSlots.BAttack;
                }
                else if (!target.IsMelee() && spellName.ToLower().Contains("critattack"))
                {
                    slot = SpellSlots.CAttack;
                }
                else if (target.GetSpellSlot(spellName) == SpellSlot.Q || _spellsQ.Any(spellName.Contains))
                {
                    slot = SpellSlots.Q;
                }
                else if (target.GetSpellSlot(spellName) == SpellSlot.W || _spellsW.Any(spellName.Contains))
                {
                    slot = SpellSlots.W;
                }
                else if (target.GetSpellSlot(spellName) == SpellSlot.E || _spellsE.Any(spellName.Contains))
                {
                    slot = SpellSlots.E;
                }
                else if (target.GetSpellSlot(spellName) == SpellSlot.R || _spellsR.Any(spellName.Contains))
                {
                    slot = SpellSlots.R;
                }
                if (_castType2.Contains(spellName))
                {
                    type = 2;
                }
                if (_castType3.Contains(spellName))
                {
                    type = 3;
                }
                if (_castType4.Contains(spellName))
                {
                    type = 4;
                }
                if (_castType5.Contains(spellName))
                {
                    type = 5;
                }
                if (_castType6.Contains(spellName))
                {
                    type = 6;
                }
                return new Tuple<SpellSlots, int>(slot, type);
            }

            private enum SpellSlots
            {
                BAttack,
                CAttack,
                Q,
                W,
                E,
                R,
                None
            }

            private class WindWallData
            {
                public readonly string ChampName;
                public readonly float Distance;
                public readonly float Radius;
                public readonly SpellSlots Slot;
                public readonly int Type;

                public WindWallData(string name, SpellSlots slot, float dist, float radius, int type)
                {
                    ChampName = name;
                    Slot = slot;
                    Distance = dist;
                    Radius = radius;
                    Type = type;
                }
            }
        }
    }
}