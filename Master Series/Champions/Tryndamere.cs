using System;
using System.Linq;
using Color = System.Drawing.Color;

using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

using Orbwalk = MasterSeries.Common.M_Orbwalker;

namespace MasterSeries.Champions
{
    class Tryndamere : Program
    {
        public Tryndamere()
        {
            Q = new Spell(SpellSlot.Q);
            W = new Spell(SpellSlot.W, 830);
            E = new Spell(SpellSlot.E, 830);
            R = new Spell(SpellSlot.R);
            E.SetSkillshot(0, 160, 700, false, SkillshotType.SkillshotLine);

            var ChampMenu = new Menu("Plugin", Name + "Plugin");
            {
                var ComboMenu = new Menu("Combo", "Combo");
                {
                    ItemBool(ComboMenu, "Q", "Use Q");
                    ItemSlider(ComboMenu, "QUnder", "-> If Hp Under", 40);
                    ItemBool(ComboMenu, "W", "Use W");
                    ItemBool(ComboMenu, "E", "Use E");
                    ItemBool(ComboMenu, "Item", "Use Item");
                    ItemBool(ComboMenu, "Ignite", "Auto Ignite If Killable");
                    ChampMenu.AddSubMenu(ComboMenu);
                }
                var HarassMenu = new Menu("Harass", "Harass");
                {
                    ItemBool(HarassMenu, "W", "Use W");
                    ItemBool(HarassMenu, "E", "Use E");
                    ItemSlider(HarassMenu, "EAbove", "-> If Hp Above", 20);
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
                    ItemSlider(ClearMenu, "QUnder", "-> If Hp Under", 40);
                    ItemBool(ClearMenu, "E", "Use E");
                    ItemBool(ClearMenu, "Item", "Use Tiamat/Hydra");
                    ChampMenu.AddSubMenu(ClearMenu);
                }
                var MiscMenu = new Menu("Misc", "Misc");
                {
                    ItemBool(MiscMenu, "EKillSteal", "Use E To Kill Steal");
                    ItemBool(MiscMenu, "QSurvive", "Try Use Q To Survive");
                    ItemBool(MiscMenu, "RSurvive", "Try Use R To Survive");
                    ItemSlider(MiscMenu, "CustomSkin", "Skin Changer", 4, 0, 6).ValueChanged += SkinChanger;
                    ChampMenu.AddSubMenu(MiscMenu);
                }
                var DrawMenu = new Menu("Draw", "Draw");
                {
                    ItemBool(DrawMenu, "W", "W Range", false);
                    ItemBool(DrawMenu, "E", "E Range", false);
                    ChampMenu.AddSubMenu(DrawMenu);
                }
                Config.AddSubMenu(ChampMenu);
            }
            Game.OnGameUpdate += OnGameUpdate;
            Drawing.OnDraw += OnDraw;
            Obj_AI_Base.OnProcessSpellCast += TrySurviveSpellCast;
        }

        private void OnGameUpdate(EventArgs args)
        {
            if (Player.IsDead || MenuGUI.IsChatOpen || Player.IsChannelingImportantSpell() || Player.IsRecalling()) return;
            if (Orbwalk.CurrentMode == Orbwalk.Mode.Combo || Orbwalk.CurrentMode == Orbwalk.Mode.Harass)
            {
                NormalCombo(Orbwalk.CurrentMode.ToString());
            }
            else if (Orbwalk.CurrentMode == Orbwalk.Mode.LaneClear)
            {
                LaneJungClear();
            }
            else if (Orbwalk.CurrentMode == Orbwalk.Mode.Flee && E.IsReady()) E.Cast(Game.CursorPos, PacketCast());
            if (ItemBool("Misc", "EKillSteal")) KillSteal();
            if (ItemBool("Misc", "QSurvive") && Q.IsReady()) TrySurvive(Q.Slot);
            if (ItemBool("Misc", "RSurvive") && R.IsReady()) TrySurvive(R.Slot);
        }

        private void OnDraw(EventArgs args)
        {
            if (Player.IsDead) return;
            if (ItemBool("Draw", "W") && W.Level > 0) Render.Circle.DrawCircle(Player.Position, W.Range, W.IsReady() ? Color.Green : Color.Red, 7);
            if (ItemBool("Draw", "E") && E.Level > 0) Render.Circle.DrawCircle(Player.Position, E.Range, E.IsReady() ? Color.Green : Color.Red, 7);
        }

        private void NormalCombo(string Mode)
        {
            if (!targetObj.IsValidTarget()) return;
            if (Mode == "Combo" && ItemBool(Mode, "Q") && Q.IsReady() && Player.HealthPercentage() <= ItemSlider(Mode, "QUnder") && Player.CountEnemysInRange(800) >= 1) Q.Cast(PacketCast());
            if (ItemBool(Mode, "W") && W.CanCast(targetObj))
            {
                if (targetObj.IsFacing(Player) && Orbwalk.InAutoAttackRange(targetObj) && (Player.GetAutoAttackDamage(targetObj, true) < targetObj.GetAutoAttackDamage(Player, true) || Player.Health < targetObj.Health))
                {
                    W.Cast(PacketCast());
                }
                else if (!targetObj.IsFacing(Player) && !Orbwalk.InAutoAttackRange(targetObj)) W.Cast(PacketCast());
            }
            if (ItemBool(Mode, "E") && E.CanCast(targetObj) && (CanKill(targetObj, E) || (Player.Distance3D(targetObj) > Orbwalk.GetAutoAttackRange(Player, targetObj) + 20 && (Mode == "Combo" || (Mode == "Harass" && Player.HealthPercentage() >= ItemSlider(Mode, "EAbove")))))) E.Cast(targetObj.Position.Extend(Player.Position, Player.Distance3D(targetObj) <= E.Range - 100 ? -100 : 0), PacketCast());
            if (Mode == "Combo" && ItemBool(Mode, "Item")) UseItem(targetObj);
            if (Mode == "Combo" && ItemBool(Mode, "Ignite") && IgniteReady()) CastIgnite(targetObj);
        }

        private void LaneJungClear()
        {
            var minionObj = MinionManager.GetMinions(E.Range, MinionTypes.All, MinionTeam.NotAlly);
            foreach (var Obj in minionObj)
            {
                if (Obj.Team == GameObjectTeam.Neutral && CanSmiteMob(Obj.Name)) CastSmite(Obj);
                if (ItemBool("Clear", "Q") && Q.IsReady() && Player.HealthPercentage() <= ItemSlider("Clear", "QUnder") && (minionObj.Count(i => Orbwalk.InAutoAttackRange(i)) >= 2 || (Obj.MaxHealth >= 1200 && Orbwalk.InAutoAttackRange(Obj)))) Q.Cast(PacketCast());
                if (ItemBool("Clear", "E") && E.IsReady()) E.Cast(GetClearPos(minionObj, E), PacketCast());
                if (ItemBool("Clear", "Item")) UseItem(Obj, true);
            }
        }

        private void KillSteal()
        {
            if (!E.IsReady()) return;
            foreach (var Obj in ObjectManager.Get<Obj_AI_Hero>().Where(i => i.IsValidTarget(E.Range) && CanKill(i, E) && i != targetObj).OrderBy(i => i.Health).OrderBy(i => i.Distance3D(Player))) E.Cast(Obj.Position.Extend(Player.Position, Player.Distance3D(Obj) <= E.Range - 100 ? -100 : 0), PacketCast());
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