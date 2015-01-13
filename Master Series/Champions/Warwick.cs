using System;
using System.Linq;
using Color = System.Drawing.Color;

using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

using Orbwalk = MasterSeries.Common.M_Orbwalker;

namespace MasterSeries.Champions
{
    class Warwick : Program
    {
        public Warwick()
        {
            Q = new Spell(SpellSlot.Q, 413.6f);
            W = new Spell(SpellSlot.W, 1250);
            R = new Spell(SpellSlot.R, 704.62f);
            Q.SetTargetted(0.5f, float.MaxValue);
            R.SetTargetted(0.5f, float.MaxValue);

            var ChampMenu = new Menu("Plugin", Name + "Plugin");
            {
                var ComboMenu = new Menu("Combo", "Combo");
                {
                    ItemBool(ComboMenu, "Q", "Use Q");
                    ItemBool(ComboMenu, "W", "Use W");
                    ItemList(ComboMenu, "WMode", "-> Mode", new[] { "Always", "# Ally" });
                    ItemSlider(ComboMenu, "WAbove", "--> If Ally Above", 2, 1, 4);
                    ItemBool(ComboMenu, "R", "Use R");
                    ItemBool(ComboMenu, "Smite", "Use Red Smite");
                    ItemBool(ComboMenu, "Item", "Use Item");
                    ItemBool(ComboMenu, "Ignite", "Auto Ignite If Killable");
                    ChampMenu.AddSubMenu(ComboMenu);
                }
                var HarassMenu = new Menu("Harass", "Harass");
                {
                    ItemBool(HarassMenu, "Q", "Use Q");
                    ItemBool(HarassMenu, "W", "Use W");
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
                    ChampMenu.AddSubMenu(ClearMenu);
                }
                var UltiMenu = new Menu("Ultimate", "Ultimate");
                {
                    foreach (var Obj in ObjectManager.Get<Obj_AI_Hero>().Where(i => i.IsEnemy)) ItemBool(UltiMenu, Obj.ChampionName, "Use R On " + Obj.ChampionName);
                    ChampMenu.AddSubMenu(UltiMenu);
                }
                var MiscMenu = new Menu("Misc", "Misc");
                {
                    ItemBool(MiscMenu, "QLastHit", "Use Q To Last Hit");
                    ItemBool(MiscMenu, "QKillSteal", "Use Q To Kill Steal");
                    ItemBool(MiscMenu, "RUnderTower", "Use R If Enemy Under Tower");
                    ItemSlider(MiscMenu, "CustomSkin", "Skin Changer", 5, 0, 7).ValueChanged += SkinChanger;
                    ChampMenu.AddSubMenu(MiscMenu);
                }
                var DrawMenu = new Menu("Draw", "Draw");
                {
                    ItemBool(DrawMenu, "Q", "Q Range", false);
                    ItemBool(DrawMenu, "R", "R Range", false);
                    ChampMenu.AddSubMenu(DrawMenu);
                }
                Config.AddSubMenu(ChampMenu);
            }
            Game.OnGameUpdate += OnGameUpdate;
            Drawing.OnDraw += OnDraw;
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
            else if (Orbwalk.CurrentMode == Orbwalk.Mode.LastHit) LastHit();
            if (ItemBool("Misc", "QKillSteal")) KillSteal();
            if (ItemBool("Misc", "RUnderTower")) AutoRUnderTower();
        }

        private void OnDraw(EventArgs args)
        {
            if (Player.IsDead) return;
            if (ItemBool("Draw", "Q") && Q.Level > 0) Render.Circle.DrawCircle(Player.Position, Q.Range, Q.IsReady() ? Color.Green : Color.Red, 7);
            if (ItemBool("Draw", "R") && R.Level > 0) Render.Circle.DrawCircle(Player.Position, R.Range, R.IsReady() ? Color.Green : Color.Red, 7);
        }

        private void NormalCombo(string Mode)
        {
            if (!targetObj.IsValidTarget()) return;
            if (ItemBool(Mode, "Q") && Q.CanCast(targetObj)) Q.CastOnUnit(targetObj, PacketCast());
            if (ItemBool(Mode, "W") && W.IsReady() && ((Mode == "Harass" && Orbwalk.InAutoAttackRange(targetObj)) || (Mode == "Combo" && ((ItemList(Mode, "WMode") == 0 && Orbwalk.InAutoAttackRange(targetObj)) || (ItemList(Mode, "WMode") == 1 && ObjectManager.Get<Obj_AI_Hero>().Count(i => i.IsValidTarget(W.Range, false) && i.IsAlly && !i.IsMe) >= ItemSlider(Mode, "WAbove")))))) W.Cast(PacketCast());
            if (Mode == "Combo" && ItemBool(Mode, "Item")) UseItem(targetObj);
            if (Mode == "Combo" && ItemBool(Mode, "Smite") && SmiteReady() && Player.GetSpellSlot("s5_summonersmiteduel") != SpellSlot.Unknown) CastSmite(targetObj, false);
            if (Mode == "Combo" && ItemBool(Mode, "R") && ItemBool("Ultimate", targetObj.ChampionName) && R.CanCast(targetObj)) R.CastOnUnit(targetObj, PacketCast());
            if (Mode == "Combo" && ItemBool(Mode, "Ignite") && IgniteReady()) CastIgnite(targetObj);
        }

        private void LaneJungClear()
        {
            foreach (var Obj in MinionManager.GetMinions(R.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth))
            {
                if (Obj.Team == GameObjectTeam.Neutral && CanSmiteMob(Obj.Name)) CastSmite(Obj);
                if (ItemBool("Clear", "Q") && Q.CanCast(Obj) && (CanKill(Obj, Q) || Obj.MaxHealth >= 1200 || Obj.HealthPercentage() > 50)) Q.CastOnUnit(Obj, PacketCast());
                if (ItemBool("Clear", "W") && W.IsReady() && Orbwalk.InAutoAttackRange(Obj)) W.Cast(PacketCast());
            }
        }

        private void LastHit()
        {
            if (!ItemBool("Misc", "QLastHit") || !Q.IsReady()) return;
            foreach (var Obj in MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.NotAlly).Where(i => CanKill(i, Q)).OrderByDescending(i => i.Distance3D(Player))) Q.CastOnUnit(Obj, PacketCast());
        }

        private void KillSteal()
        {
            if (!Q.IsReady()) return;
            foreach (var Obj in ObjectManager.Get<Obj_AI_Hero>().Where(i => i.IsValidTarget(Q.Range) && CanKill(i, Q) && i != targetObj).OrderBy(i => i.Health).OrderBy(i => i.Distance3D(Player))) Q.CastOnUnit(Obj, PacketCast());
        }

        private void AutoRUnderTower()
        {
            if (Player.UnderTurret() || !R.IsReady()) return;
            foreach (var Obj in ObjectManager.Get<Obj_AI_Hero>().Where(i => i.IsValidTarget(R.Range)).OrderBy(i => i.Distance3D(Player)))
            {
                var TowerObj = ObjectManager.Get<Obj_AI_Turret>().FirstOrDefault(i => i.IsValidTarget(950, false) && i.IsAlly);
                if (TowerObj != null && Obj.Distance3D(TowerObj) <= 950) R.CastOnUnit(Obj, PacketCast());
            }
        }

        private void UseItem(Obj_AI_Base Target)
        {
            if (Bilgewater.IsReady()) Bilgewater.Cast(Target);
            if (BladeRuined.IsReady()) BladeRuined.Cast(Target);
            if (RanduinOmen.IsReady() && Player.CountEnemysInRange((int)RanduinOmen.Range) >= 1) RanduinOmen.Cast();
        }
    }
}