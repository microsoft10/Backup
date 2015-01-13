using System;
using System.Linq;
using Color = System.Drawing.Color;

using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

using Orbwalk = MasterSeries.Common.M_Orbwalker;

namespace MasterSeries.Champions
{
    class XinZhao : Program
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

            var ChampMenu = new Menu("Plugin", Name + "Plugin");
            {
                var ComboMenu = new Menu("Combo", "Combo");
                {
                    ItemBool(ComboMenu, "Q", "Use Q");
                    ItemBool(ComboMenu, "W", "Use W");
                    ItemBool(ComboMenu, "E", "Use E");
                    ItemBool(ComboMenu, "R", "Use R If Killable");
                    ItemBool(ComboMenu, "Item", "Use Item");
                    ItemBool(ComboMenu, "Ignite", "Auto Ignite If Killable");
                    ChampMenu.AddSubMenu(ComboMenu);
                }
                var HarassMenu = new Menu("Harass", "Harass");
                {
                    ItemBool(HarassMenu, "Q", "Use Q");
                    ItemBool(HarassMenu, "W", "Use W");
                    ItemBool(HarassMenu, "E", "Use E");
                    ChampMenu.AddSubMenu(HarassMenu);
                }
                var ClearMenu = new Menu("Lane/Jungle Clear", "Clear");
                {
                    var SmiteMob = new Menu("Smite Mob If Killable", "SmiteMob");
                    {
                        ItemBool(SmiteMob, "Baron", "Baron Nashor");
                        ItemBool(SmiteMob, "Dragon", "Dragon");
                        ItemBool(SmiteMob, "Red", "Red Brambleback");
                        ItemBool(SmiteMob, "Blue", "Blue Sentinel");
                        ItemBool(SmiteMob, "Krug", "Ancient Krug");
                        ItemBool(SmiteMob, "Gromp", "Gromp");
                        ItemBool(SmiteMob, "Raptor", "Crimson Raptor");
                        ItemBool(SmiteMob, "Wolf", "Greater Murk Wolf");
                        ClearMenu.AddSubMenu(SmiteMob);
                    }
                    ItemBool(ClearMenu, "Q", "Use Q");
                    ItemBool(ClearMenu, "W", "Use W");
                    ItemBool(ClearMenu, "E", "Use E");
                    ItemBool(ClearMenu, "Item", "Use Tiamat/Hydra");
                    ChampMenu.AddSubMenu(ClearMenu);
                }
                var UltiMenu = new Menu("Ultimate", "Ultimate");
                {
                    var KillableMenu = new Menu("Killable", "Killable");
                    {
                        foreach (var Obj in ObjectManager.Get<Obj_AI_Hero>().Where(i => i.IsEnemy)) ItemBool(KillableMenu, Obj.ChampionName, "Use R On " + Obj.ChampionName);
                        UltiMenu.AddSubMenu(KillableMenu);
                    }
                    var InterruptMenu = new Menu("Interrupt", "Interrupt");
                    {
                        foreach (var Obj in ObjectManager.Get<Obj_AI_Hero>().Where(i => i.IsEnemy))
                        {
                            foreach (var Spell in Interrupter.Spells.Where(i => i.ChampionName == Obj.ChampionName)) ItemBool(InterruptMenu, Obj.ChampionName + "_" + Spell.Slot.ToString(), "Spell " + Spell.Slot.ToString() + " Of " + Obj.ChampionName);
                        }
                        UltiMenu.AddSubMenu(InterruptMenu);
                    }
                    ChampMenu.AddSubMenu(UltiMenu);
                }
                var MiscMenu = new Menu("Misc", "Misc");
                {
                    ItemBool(MiscMenu, "EKillSteal", "Use E To Kill Steal");
                    ItemBool(MiscMenu, "RInterrupt", "Use R To Interrupt");
                    ItemSlider(MiscMenu, "CustomSkin", "Skin Changer", 5, 0, 5).ValueChanged += SkinChanger;
                    ChampMenu.AddSubMenu(MiscMenu);
                }
                var DrawMenu = new Menu("Draw", "Draw");
                {
                    ItemBool(DrawMenu, "E", "E Range", false);
                    ItemBool(DrawMenu, "R", "R Range", false);
                    ChampMenu.AddSubMenu(DrawMenu);
                }
                Config.AddSubMenu(ChampMenu);
            }
            Game.OnGameUpdate += OnGameUpdate;
            Drawing.OnDraw += OnDraw;
            Interrupter.OnPossibleToInterrupt += OnPossibleToInterrupt;
            Orbwalk.AfterAttack += AfterAttack;
        }

        private void OnGameUpdate(EventArgs args)
        {
            if (Player.IsDead || MenuGUI.IsChatOpen || Player.IsChannelingImportantSpell() || Player.IsRecalling()) return;
            if (Orbwalk.CurrentMode == Orbwalk.Mode.Combo || Orbwalk.CurrentMode == Orbwalk.Mode.Harass)
            {
                NormalCombo(Orbwalk.CurrentMode.ToString());
            }
            else if (Orbwalk.CurrentMode == Orbwalk.Mode.LaneClear) LaneJungClear();
            if (ItemBool("Misc", "EKillSteal")) KillSteal();
        }

        private void OnDraw(EventArgs args)
        {
            if (Player.IsDead) return;
            if (ItemBool("Draw", "E") && E.Level > 0) Render.Circle.DrawCircle(Player.Position, E.Range, E.IsReady() ? Color.Green : Color.Red, 7);
            if (ItemBool("Draw", "R") && R.Level > 0) Render.Circle.DrawCircle(Player.Position, R.Range, R.IsReady() ? Color.Green : Color.Red, 7);
        }

        private void OnPossibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            if (!ItemBool("Misc", "RInterrupt") || !R.IsReady() || !ItemBool("Interrupt", (unit as Obj_AI_Hero).ChampionName + "_" + spell.Slot.ToString()) || Player.IsDead || unit.HasBuff("XinZhaoIntimidate")) return;
            if (!R.InRange(unit) && E.IsReady() && Player.Mana >= E.Instance.ManaCost + R.Instance.ManaCost)
            {
                foreach (var Obj in ObjectManager.Get<Obj_AI_Base>().Where(i => i.IsValidTarget(E.Range) && !(i is Obj_AI_Turret) && i != unit && i.Distance3D(unit) <= R.Range - 20)) E.CastOnUnit(Obj, PacketCast());
            }
            if (R.InRange(unit)) R.Cast(PacketCast());
        }

        private void AfterAttack(AttackableUnit Target)
        {
            if ((Orbwalk.CurrentMode == Orbwalk.Mode.Combo || Orbwalk.CurrentMode == Orbwalk.Mode.Harass) && ItemBool(Orbwalk.CurrentMode.ToString(), "Q") && Q.IsReady() && Target is Obj_AI_Hero && Target.IsValidTarget(Orbwalk.GetAutoAttackRange(Player, Target) + 20)) Q.Cast(PacketCast());
        }

        private void NormalCombo(string Mode)
        {
            if (!targetObj.IsValidTarget()) return;
            if (ItemBool(Mode, "Q") && (Player.HasBuff("XenZhaoComboTarget") || Player.HasBuff("XenZhaoAutoCombo") || Player.HasBuff("XenZhaoAutoComboFinish")) && Orbwalk.InAutoAttackRange(targetObj)) Player.IssueOrder(GameObjectOrder.AttackUnit, targetObj);
            if (Mode == "Combo" && ItemBool(Mode, "R") && ItemBool("Killable", targetObj.ChampionName) && R.CanCast(targetObj))
            {
                if (CanKill(targetObj, R))
                {
                    R.Cast(PacketCast());
                }
                else if (CanKill(targetObj, R, R.GetDamage(targetObj), E.GetDamage(targetObj) + Player.GetAutoAttackDamage(targetObj, true) + ((ItemBool(Mode, "Q") && Q.IsReady()) ? Q.GetDamage(targetObj) * 3 : 0)) && ItemBool(Mode, "E") && E.IsReady() && Player.Mana >= R.Instance.ManaCost + E.Instance.ManaCost + ((ItemBool(Mode, "Q") && Q.IsReady()) ? Q.Instance.ManaCost : 0)) R.Cast(PacketCast());
            }
            if (ItemBool(Mode, "E") && E.CanCast(targetObj) && (CanKill(targetObj, E) || Player.Distance3D(targetObj) > Orbwalk.GetAutoAttackRange(Player, targetObj) + 30 || (Mode == "Combo" && Player.HealthPercentage() < targetObj.HealthPercentage()))) E.CastOnUnit(targetObj, PacketCast());
            if (ItemBool(Mode, "W") && W.IsReady() && Orbwalk.InAutoAttackRange(targetObj)) W.Cast(PacketCast());
            if (Mode == "Combo" && ItemBool(Mode, "Item")) UseItem(targetObj);
            if (Mode == "Combo" && ItemBool(Mode, "Ignite") && IgniteReady()) CastIgnite(targetObj);
        }

        private void LaneJungClear()
        {
            foreach (var Obj in MinionManager.GetMinions(E.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth))
            {
                if (Obj.Team == GameObjectTeam.Neutral && CanSmiteMob(Obj.Name)) CastSmite(Obj);
                if (ItemBool("Clear", "E") && E.IsReady() && (Player.Distance3D(Obj) > Orbwalk.GetAutoAttackRange(Player, Obj) + 30 || CanKill(Obj, E) || Obj.MaxHealth >= 1200)) E.CastOnUnit(Obj, PacketCast());
                if (ItemBool("Clear", "W") && W.IsReady() && Orbwalk.InAutoAttackRange(Obj)) W.Cast(PacketCast());
                if (ItemBool("Clear", "Q"))
                {
                    if (Q.IsReady() && Player.Distance3D(Obj) <= Orbwalk.GetAutoAttackRange(Player, Obj) + 20)
                    {
                        Q.Cast(PacketCast());
                    }
                    else if ((Player.HasBuff("XenZhaoComboTarget") || Player.HasBuff("XenZhaoAutoCombo") || Player.HasBuff("XenZhaoAutoComboFinish")) && Orbwalk.InAutoAttackRange(Obj)) Player.IssueOrder(GameObjectOrder.AttackUnit, Obj);
                }
                if (ItemBool("Clear", "Item")) UseItem(Obj, true);
            }
        }

        private void KillSteal()
        {
            if (!E.IsReady()) return;
            foreach (var Obj in ObjectManager.Get<Obj_AI_Hero>().Where(i => i.IsValidTarget(E.Range) && CanKill(i, E) && i != targetObj).OrderBy(i => i.Health).OrderBy(i => i.Distance3D(Player))) E.CastOnUnit(Obj, PacketCast());
        }

        private void UseItem(Obj_AI_Base Target, bool IsFarm = false)
        {
            if (Bilgewater.IsReady() && !IsFarm) Bilgewater.Cast(Target);
            if (BladeRuined.IsReady() && !IsFarm) BladeRuined.Cast(Target);
            if (Tiamat.IsReady() && IsFarm ? Player.Distance3D(Target) <= Tiamat.Range : Player.CountEnemysInRange((int)Tiamat.Range) >= 1) Tiamat.Cast();
            if (Hydra.IsReady() && IsFarm ? Player.Distance3D(Target) <= Hydra.Range : (Player.CountEnemysInRange((int)Hydra.Range) >= 2 || (Player.GetAutoAttackDamage(Target, true) < Target.Health && Player.CountEnemysInRange((int)Hydra.Range) == 1))) Hydra.Cast();
            if (RanduinOmen.IsReady() && Player.CountEnemysInRange((int)RanduinOmen.Range) >= 1 && !IsFarm) RanduinOmen.Cast();
            if (Youmuu.IsReady() && Player.CountEnemysInRange((int)Orbwalk.GetAutoAttackRange()) >= 1 && !IsFarm) Youmuu.Cast();
        }
    }
}