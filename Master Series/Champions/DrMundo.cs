using System;
using System.Linq;
using Color = System.Drawing.Color;

using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

using Orbwalk = MasterSeries.Common.M_Orbwalker;

namespace MasterSeries.Champions
{
    class DrMundo : Program
    {
        public DrMundo()
        {
            Q = new Spell(SpellSlot.Q, 1050);
            W = new Spell(SpellSlot.W, 325);
            E = new Spell(SpellSlot.E);
            R = new Spell(SpellSlot.R);
            Q.SetSkillshot(0.5f, 75, 1500, true, SkillshotType.SkillshotLine);

            var ChampMenu = new Menu("Plugin", Name + "Plugin");
            {
                var ComboMenu = new Menu("Combo", "Combo");
                {
                    ItemBool(ComboMenu, "Q", "Use Q");
                    ItemBool(ComboMenu, "W", "Use W");
                    ItemSlider(ComboMenu, "WAbove", "-> If Hp Above", 20);
                    ItemBool(ComboMenu, "E", "Use E");
                    ItemBool(ComboMenu, "Item", "Use Item");
                    ItemBool(ComboMenu, "Ignite", "Auto Ignite If Killable");
                    ChampMenu.AddSubMenu(ComboMenu);
                }
                var HarassMenu = new Menu("Harass", "Harass");
                {
                    ItemBool(HarassMenu, "Q", "Use Q");
                    ItemBool(HarassMenu, "W", "Use W");
                    ItemSlider(HarassMenu, "WAbove", "-> If Hp Above", 20);
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
                    ItemSlider(ClearMenu, "WAbove", "-> If Hp Above", 20);
                    ItemBool(ClearMenu, "E", "Use E");
                    ChampMenu.AddSubMenu(ClearMenu);
                }
                var UltiMenu = new Menu("Ultimate", "Ultimate");
                {
                    ItemBool(UltiMenu, "RSurvive", "Try Use R To Survive");
                    ItemSlider(UltiMenu, "RUnder", "-> If Hp Under", 30);
                    ChampMenu.AddSubMenu(UltiMenu);
                }
                var MiscMenu = new Menu("Misc", "Misc");
                {
                    ItemBool(MiscMenu, "QLastHit", "Use Q To Last Hit");
                    ItemBool(MiscMenu, "QKillSteal", "Use Q To Kill Steal");
                    ItemBool(MiscMenu, "SmiteCol", "Auto Smite Collision");
                    ItemSlider(MiscMenu, "CustomSkin", "Skin Changer", 7, 0, 7).ValueChanged += SkinChanger;
                    ChampMenu.AddSubMenu(MiscMenu);
                }
                var DrawMenu = new Menu("Draw", "Draw");
                {
                    ItemBool(DrawMenu, "Q", "Q Range", false);
                    ItemBool(DrawMenu, "W", "W Range", false);
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
            else if (Orbwalk.CurrentMode == Orbwalk.Mode.LastHit) LastHit();
            if (ItemBool("Ultimate", "RSurvive") && R.IsReady()) TrySurvive(R.Slot, ItemSlider("Ultimate", "RUnder"));
            if (ItemBool("Misc", "QKillSteal")) KillSteal();
        }

        private void OnDraw(EventArgs args)
        {
            if (Player.IsDead) return;
            if (ItemBool("Draw", "Q") && Q.Level > 0) Render.Circle.DrawCircle(Player.Position, Q.Range, Q.IsReady() ? Color.Green : Color.Red, 7);
            if (ItemBool("Draw", "W") && W.Level > 0) Render.Circle.DrawCircle(Player.Position, W.Range, W.IsReady() ? Color.Green : Color.Red, 7);
        }

        private void NormalCombo(string Mode)
        {
            if (ItemBool(Mode, "W") && W.IsReady() && Player.HasBuff("BurningAgony") && Player.CountEnemysInRange(500) == 0) W.Cast(PacketCast());
            if (!targetObj.IsValidTarget()) return;
            if (ItemBool(Mode, "W") && W.IsReady())
            {
                if (Player.HealthPercentage() >= ItemSlider(Mode, "WAbove"))
                {
                    if (Player.Distance3D(targetObj) <= W.Range + 60)
                    {
                        if (!Player.HasBuff("BurningAgony")) W.Cast(PacketCast());
                    }
                    else if (Player.HasBuff("BurningAgony")) W.Cast(PacketCast());
                }
                else if (Player.HasBuff("BurningAgony")) W.Cast(PacketCast());
            }
            if (ItemBool(Mode, "Q") && Q.CanCast(targetObj))
            {
                CastSkillShotSmite(Q, targetObj);
            }
            if (ItemBool(Mode, "E") && E.IsReady() && Orbwalk.InAutoAttackRange(targetObj)) E.Cast(PacketCast());
            if (Mode == "Combo" && ItemBool(Mode, "Item") && RanduinOmen.IsReady() && Player.CountEnemysInRange((int)RanduinOmen.Range) >= 1) RanduinOmen.Cast();
            if (Mode == "Combo" && ItemBool(Mode, "Ignite") && IgniteReady()) CastIgnite(targetObj);
        }

        private void LaneJungClear()
        {
            var minionObj = MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.NotAlly);
            if (minionObj.Count() == 0 && ItemBool("Clear", "W") && W.IsReady() && Player.HasBuff("BurningAgony")) W.Cast(PacketCast());
            foreach (var Obj in minionObj)
            {
                if (Obj.Team == GameObjectTeam.Neutral && CanSmiteMob(Obj.Name)) CastSmite(Obj);
                if (ItemBool("Clear", "E") && E.IsReady() && Orbwalk.InAutoAttackRange(Obj)) E.Cast(PacketCast());
                if (ItemBool("Clear", "W") && W.IsReady())
                {
                    if (Player.HealthPercentage() >= ItemSlider("Clear", "WAbove"))
                    {
                        if (minionObj.Count(i => Player.Distance3D(i) <= W.Range + 60) >= 2 || (Obj.MaxHealth >= 1200 && Player.Distance3D(Obj) <= W.Range + 60))
                        {
                            if (!Player.HasBuff("BurningAgony")) W.Cast(PacketCast());
                        }
                        else if (Player.HasBuff("BurningAgony")) W.Cast(PacketCast());
                    }
                    else if (Player.HasBuff("BurningAgony")) W.Cast(PacketCast());
                }
                if (ItemBool("Clear", "Q") && Q.IsReady() && (CanKill(Obj, Q) || Obj.MaxHealth >= 1200)) Q.CastIfHitchanceEquals(Obj, HitChance.Medium, PacketCast());
            }
        }

        private void LastHit()
        {
            if (!ItemBool("Misc", "QLastHit") || !Q.IsReady()) return;
            foreach (var Obj in MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.NotAlly).Where(i => CanKill(i, Q)).OrderByDescending(i => i.Distance3D(Player))) Q.CastIfHitchanceEquals(Obj, HitChance.VeryHigh, PacketCast());
        }

        private void KillSteal()
        {
            if (!Q.IsReady()) return;
            foreach (var Obj in ObjectManager.Get<Obj_AI_Hero>().Where(i => i.IsValidTarget(Q.Range) && CanKill(i, Q) && i != targetObj).OrderBy(i => i.Health).OrderBy(i => i.Distance3D(Player))) CastSkillShotSmite(Q, Obj);
        }
    }
}