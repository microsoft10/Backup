using System;
using System.Linq;
using BrianSharp.Common;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;
using Orbwalk = BrianSharp.Common.Orbwalker;

namespace BrianSharp.Plugin
{
    internal class Jax : Helper
    {
        private bool _wardCasted;
        private Vector3 _wardPlacePos;

        public Jax()
        {
            Q = new Spell(SpellSlot.Q, 700);
            W = new Spell(SpellSlot.W, Orbwalk.GetAutoAttackRange());
            E = new Spell(SpellSlot.E, 375);
            R = new Spell(SpellSlot.R);
            Q.SetTargetted(0.5f, float.MaxValue);
            W.SetTargetted(0.2333f, float.MaxValue);

            var champMenu = new Menu("Plugin", PlayerName + "_Plugin");
            {
                var comboMenu = new Menu("Combo", "Combo");
                {
                    AddItem(comboMenu, "Q", "Use Q");
                    AddItem(comboMenu, "W", "Use W");
                    AddItem(comboMenu, "E", "Use E");
                    AddItem(comboMenu, "ECountA", "-> Cancel If Enemy Above", 2, 1, 5);
                    AddItem(comboMenu, "R", "Use R");
                    AddItem(comboMenu, "RHpU", "-> If Player Hp Under", 60);
                    AddItem(comboMenu, "RCountA", "-> If Enemy Above", 2, 1, 5);
                    champMenu.AddSubMenu(comboMenu);
                }
                var harassMenu = new Menu("Harass", "Harass");
                {
                    AddItem(harassMenu, "Q", "Use Q");
                    AddItem(harassMenu, "QHpA", "-> If Hp Above", 20);
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
                    AddItem(clearMenu, "Item", "Use Tiamat/Hydra");
                    champMenu.AddSubMenu(clearMenu);
                }
                var lastHitMenu = new Menu("Last Hit", "LastHit");
                {
                    AddItem(lastHitMenu, "W", "Use W");
                    champMenu.AddSubMenu(lastHitMenu);
                }
                var fleeMenu = new Menu("Flee", "Flee");
                {
                    AddItem(fleeMenu, "Q", "Use Q");
                    AddItem(fleeMenu, "PinkWard", "-> Ward Jump Use Pink Ward", false);
                    champMenu.AddSubMenu(fleeMenu);
                }
                var miscMenu = new Menu("Misc", "Misc");
                {
                    var killStealMenu = new Menu("Kill Steal", "KillSteal");
                    {
                        AddItem(killStealMenu, "Q", "Use Q");
                        AddItem(killStealMenu, "W", "Use W");
                        AddItem(killStealMenu, "Ignite", "Use Ignite");
                        AddItem(killStealMenu, "Smite", "Use Smite");
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
                case Orbwalker.Mode.Flee:
                    Flee(Game.CursorPos);
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
            E.Cast(PacketCast);
        }

        private void OnPossibleToInterrupt(Obj_AI_Hero unit, InterruptableSpell spell)
        {
            if (Player.IsDead || !GetValue<bool>("Interrupt", "E") ||
                !GetValue<bool>("Interrupt", unit.ChampionName + "_" + spell.Slot) || !E.IsReady())
            {
                return;
            }
            if (E.IsInRange(unit))
            {
                E.Cast(PacketCast);
            }
            else if (Q.CanCast(unit) &&
                     Player.Mana >= Q.Instance.ManaCost + (Player.HasBuff("JaxEvasion") ? 0 : E.Instance.ManaCost))
            {
                Q.CastOnUnit(unit, PacketCast);
            }
        }

        private void AfterAttack(AttackableUnit target)
        {
            if (!W.IsReady())
            {
                return;
            }
            if ((((Orbwalk.CurrentMode == Orbwalker.Mode.Combo || Orbwalk.CurrentMode == Orbwalker.Mode.Harass) &&
                  target is Obj_AI_Hero) || (Orbwalk.CurrentMode == Orbwalker.Mode.Clear && target is Obj_AI_Minion)) &&
                GetValue<bool>(Orbwalk.CurrentMode.ToString(), "W") && W.Cast(PacketCast))
            {
                Player.IssueOrder(GameObjectOrder.AttackUnit, target);
            }
        }

        private void Fight(string mode)
        {
            if (GetValue<bool>(mode, "E") && E.IsReady())
            {
                if (!Player.HasBuff("JaxEvasion"))
                {
                    if (GetValue<bool>(mode, "Q") && Q.IsReady() && E.GetTarget() == null)
                    {
                        var target = Q.GetTarget();
                        if (target != null && E.Cast(PacketCast) && Q.CastOnUnit(target, PacketCast))
                        {
                            return;
                        }
                    }
                    else if (E.GetTarget() != null && E.Cast(PacketCast))
                    {
                        return;
                    }
                }
                else
                {
                    var target = Orbwalk.GetBestHeroTarget();
                    if (target != null)
                    {
                        Player.IssueOrder(GameObjectOrder.AttackUnit, target);
                    }
                    if ((Player.CountEnemiesInRange(E.Range) >= GetValue<Slider>(mode, "ECountA").Value ||
                         (E.GetTarget() != null && Player.Distance(E.GetTarget()) > E.Range - 50)) && E.Cast(PacketCast))
                    {
                        return;
                    }
                }
            }
            if (GetValue<bool>(mode, "W") && W.IsReady() && GetValue<bool>(mode, "Q") && Q.IsReady() &&
                Player.Mana >= W.Instance.ManaCost + Q.Instance.ManaCost)
            {
                var target = Q.GetTarget();
                if (target != null && CanKill(target, Q, GetBonusDmg(target, Q.GetDamage(target))) && W.Cast(PacketCast) &&
                    Q.CastOnUnit(target, PacketCast))
                {
                    return;
                }
            }
            if (GetValue<bool>(mode, "Q") && Q.IsReady())
            {
                var target = Q.GetTarget();
                if (target != null)
                {
                    if (
                        CanKill(
                            target, Q, Q.GetDamage(target) + (Player.HasBuff("EmpowerTwo") ? GetBonusDmg(target) : 0)) &&
                        Q.CastOnUnit(target, PacketCast))
                    {
                        return;
                    }
                    if (mode == "Combo" || Player.HealthPercent >= GetValue<Slider>(mode, "QHpA").Value)
                    {
                        if ((!Orbwalk.InAutoAttackRange(target, 30) ||
                             (GetValue<bool>(mode, "E") && E.IsReady() && Player.HasBuff("JaxEvasion") &&
                              !E.IsInRange(target))) && Q.CastOnUnit(target, PacketCast))
                        {
                            return;
                        }
                    }
                }
            }
            if (mode == "Combo" && GetValue<bool>(mode, "R") && R.IsReady())
            {
                var rCount = GetValue<Slider>(mode, "RCountA").Value;
                if (((rCount > 1 && (Player.CountEnemiesInRange(Q.Range) >= rCount || Q.GetTarget() != null)) ||
                     (rCount == 1 && Q.GetTarget() != null)) &&
                    Player.HealthPercent < GetValue<Slider>(mode, "RHpU").Value)
                {
                    R.Cast(PacketCast);
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
                return;
            }
            if (GetValue<bool>("Clear", "E") && E.IsReady())
            {
                if (!Player.HasBuff("JaxEvasion"))
                {
                    if (GetValue<bool>("Clear", "Q") && Q.IsReady() && minionObj.Count(i => E.IsInRange(i)) == 0)
                    {
                        if (
                            minionObj.Any(
                                i =>
                                    minionObj.Count(a => a.Distance(i) <= E.Range) > 1 && E.Cast(PacketCast) &&
                                    Q.CastOnUnit(i, PacketCast)))
                        {
                            return;
                        }
                    }
                    else if ((minionObj.Count(i => i.MaxHealth >= 1200 && E.IsInRange(i)) > 0 ||
                              minionObj.Count(i => E.IsInRange(i)) > 2) && E.Cast(PacketCast))
                    {
                        return;
                    }
                }
                else
                {
                    var obj = Orbwalk.GetPossibleTarget();
                    if (obj.IsValidTarget())
                    {
                        Player.IssueOrder(GameObjectOrder.AttackUnit, obj);
                    }
                }
            }
            if (GetValue<bool>("Clear", "W") && W.IsReady() && GetValue<bool>("Clear", "Q") && Q.IsReady() &&
                Player.Mana >= W.Instance.ManaCost + Q.Instance.ManaCost)
            {
                var obj =
                    minionObj.Cast<Obj_AI_Minion>()
                        .Find(i => i.MaxHealth >= 1200 && CanKill(i, Q, GetBonusDmg(i, Q.GetDamage(i))));
                if (obj != null && W.Cast(PacketCast) && Q.CastOnUnit(obj, PacketCast))
                {
                    return;
                }
            }
            if (GetValue<bool>("Clear", "Q") && Q.IsReady())
            {
                var obj =
                    (Obj_AI_Base)
                        minionObj.Cast<Obj_AI_Minion>()
                            .Find(
                                i =>
                                    i.MaxHealth >= 1200 &&
                                    CanKill(i, Q, Q.GetDamage(i) + (Player.HasBuff("EmpowerTwo") ? GetBonusDmg(i) : 0)));
                if (obj == null &&
                    (minionObj.Count(i => Orbwalk.InAutoAttackRange(i, 40)) == 0 ||
                     (GetValue<bool>("Clear", "E") && E.IsReady() && Player.HasBuff("JaxEvasion") &&
                      minionObj.Count(i => E.IsInRange(i)) == 0)))
                {
                    obj = minionObj.MinOrDefault(i => i.Health);
                }
                if (obj != null && Q.CastOnUnit(obj, PacketCast))
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

        private void LastHit()
        {
            if (!GetValue<bool>("LastHit", "W") || (!W.IsReady() && !Player.HasBuff("EmpowerTwo")))
            {
                return;
            }
            var obj =
                MinionManager.GetMinions(W.Range + 100, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth)
                    .Cast<Obj_AI_Minion>()
                    .Where(i => Orbwalk.InAutoAttackRange(i))
                    .Find(i => CanKill(i, W, GetBonusDmg(i)));
            if (obj == null)
            {
                return;
            }
            if (!Player.HasBuff("EmpowerTwo"))
            {
                W.Cast(PacketCast);
            }
            Orbwalk.Move = false;
            Utility.DelayAction.Add(80, () => Orbwalk.Move = true);
            Player.IssueOrder(GameObjectOrder.AttackUnit, obj);
        }

        private void Flee(Vector3 pos)
        {
            if (!GetValue<bool>("Flee", "Q") || !Q.IsReady())
            {
                return;
            }
            Obj_AI_Base obj;
            Vector3[] jumpPos = { Player.Distance(pos) > Q.Range ? Player.ServerPosition.Extend(pos, Q.Range) : pos };
            if (_wardCasted && _wardPlacePos.IsValid())
            {
                obj =
                    ObjectManager.Get<Obj_AI_Minion>()
                        .Find(i => i.IsAlly && i.Distance(_wardPlacePos) < 200 && i.Name.ToLower().Contains("ward"));
            }
            else
            {
                obj =
                    HeroManager.AllHeroes.Where(
                        i =>
                            !i.IsMe && i.IsValidTarget(Q.Range + i.BoundingRadius, false) &&
                            i.Distance(jumpPos[0]) < 200).MinOrDefault(i => i.Distance(jumpPos[0])) ??
                    (Obj_AI_Base)
                        ObjectManager.Get<Obj_AI_Minion>()
                            .Where(
                                i => i.IsValidTarget(Q.Range + i.BoundingRadius, false) && i.Distance(jumpPos[0]) < 200)
                            .MinOrDefault(i => i.Distance(jumpPos[0]));
            }
            if (obj != null && Q.CastOnUnit(obj, PacketCast))
            {
                return;
            }
            jumpPos[0] = Player.Distance(pos) > GetWardRange() ? Player.ServerPosition.Extend(pos, GetWardRange()) : pos;
            if (GetWardSlot() != null && !_wardCasted && Player.Spellbook.CastSpell(GetWardSlot().SpellSlot, jumpPos[0]))
            {
                _wardPlacePos = jumpPos[0];
                _wardCasted = true;
                Utility.DelayAction.Add(
                    500, () =>
                    {
                        _wardPlacePos = new Vector3();
                        _wardCasted = false;
                    });
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
            if (GetValue<bool>("KillSteal", "W") && (W.IsReady() || Player.HasBuff("EmpowerTwo")))
            {
                var target = Orbwalk.GetBestHeroTarget();
                if (target != null && CanKill(target, W, GetBonusDmg(target)))
                {
                    if (!Player.HasBuff("EmpowerTwo"))
                    {
                        W.Cast(PacketCast);
                    }
                    Orbwalk.Move = false;
                    Utility.DelayAction.Add(80, () => Orbwalk.Move = true);
                    Player.IssueOrder(GameObjectOrder.AttackUnit, target);
                }
            }
            if (GetValue<bool>("KillSteal", "Q") && Q.IsReady())
            {
                var target = Q.GetTarget();
                if (target != null)
                {
                    if (W.IsReady() && Player.Mana >= W.Instance.ManaCost + Q.Instance.ManaCost &&
                        CanKill(target, Q, GetBonusDmg(target, Q.GetDamage(target))) && W.Cast(PacketCast) &&
                        Q.CastOnUnit(target, PacketCast))
                    {
                        return;
                    }
                    if (CanKill(
                        target, Q, Q.GetDamage(target) + (Player.HasBuff("EmpowerTwo") ? GetBonusDmg(target) : 0)))
                    {
                        Q.CastOnUnit(target, PacketCast);
                    }
                }
            }
        }

        private double GetBonusDmg(Obj_AI_Base target, double subDmg = 0)
        {
            double dmgItem = 0;
            if (Sheen.IsOwned() && (Sheen.IsReady() || Player.HasBuff("Sheen")) && Player.BaseAttackDamage > dmgItem)
            {
                dmgItem = Player.BaseAttackDamage;
            }
            if (Trinity.IsOwned() && (Trinity.IsReady() || Player.HasBuff("Sheen")) &&
                Player.BaseAttackDamage * 2 > dmgItem)
            {
                dmgItem = Player.BaseAttackDamage * 2;
            }
            return (W.IsReady() || Player.HasBuff("EmpowerTwo") ? W.GetDamage(target) : 0) +
                   Player.GetAutoAttackDamage(target, true) +
                   (dmgItem > 0 ? Player.CalcDamage(target, Damage.DamageType.Physical, dmgItem) : 0) + subDmg;
        }
    }
}