using System;
using System.Linq;
using Color = System.Drawing.Color;

using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

using Orbwalk = MasterSeries.Common.M_Orbwalker;

namespace MasterSeries.Champions
{
    class Amumu : Program
    {
        public Amumu()
        {
            Q = new Spell(SpellSlot.Q, 1100);
            W = new Spell(SpellSlot.W, 300);
            E = new Spell(SpellSlot.E, 350);
            R = new Spell(SpellSlot.R, 550);
            Q.SetSkillshot(0.5f, 80, 2000, true, SkillshotType.SkillshotLine);
            R.SetSkillshot(0.5f, 550, float.MaxValue, false, SkillshotType.SkillshotCircle);

            var ChampMenu = new Menu("Plugin", Name + "Plugin");
            {
                var ComboMenu = new Menu("Combo", "Combo");
                {
                    ItemBool(ComboMenu, "Q", "Use Q");
                    ItemBool(ComboMenu, "W", "Use W");
                    ItemSlider(ComboMenu, "WAbove", "-> If Mp Above", 20);
                    ItemBool(ComboMenu, "E", "Use E");
                    ItemBool(ComboMenu, "R", "Use R");
                    ItemList(ComboMenu, "RMode", "-> Mode", new[] { "Killable", "# Enemy" });
                    ItemSlider(ComboMenu, "RAbove", "--> If Enemy Above", 2, 1, 4);
                    ItemBool(ComboMenu, "Item", "Use Item");
                    ItemBool(ComboMenu, "Ignite", "Auto Ignite If Killable");
                    ChampMenu.AddSubMenu(ComboMenu);
                }
                var HarassMenu = new Menu("Harass", "Harass");
                {
                    ItemBool(HarassMenu, "W", "Use W");
                    ItemSlider(HarassMenu, "WAbove", "-> If Mp Above", 20);
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
                    ItemSlider(ClearMenu, "WAbove", "-> If Mp Above", 20);
                    ItemBool(ClearMenu, "E", "Use E");
                    ChampMenu.AddSubMenu(ClearMenu);
                }
                var MiscMenu = new Menu("Misc", "Misc");
                {
                    ItemBool(MiscMenu, "QAntiGap", "Use Q To Anti Gap Closer");
                    ItemBool(MiscMenu, "QInterrupt", "Use Q To Interrupt");
                    ItemBool(MiscMenu, "SmiteCol", "Auto Smite Collision");
                    ItemSlider(MiscMenu, "CustomSkin", "Skin Changer", 6, 0, 7).ValueChanged += SkinChanger;
                    ChampMenu.AddSubMenu(MiscMenu);
                }
                var DrawMenu = new Menu("Draw", "Draw");
                {
                    ItemBool(DrawMenu, "Q", "Q Range", false);
                    ItemBool(DrawMenu, "W", "W Range", false);
                    ItemBool(DrawMenu, "E", "E Range", false);
                    ItemBool(DrawMenu, "R", "R Range", false);
                    ChampMenu.AddSubMenu(DrawMenu);
                }
                Config.AddSubMenu(ChampMenu);
            }
            Game.OnGameUpdate += OnGameUpdate;
            Drawing.OnDraw += OnDraw;
            AntiGapcloser.OnEnemyGapcloser += OnEnemyGapcloser;
            Interrupter.OnPossibleToInterrupt += OnPossibleToInterrupt;
        }

        private void OnGameUpdate(EventArgs args)
        {
            if (Player.IsDead || MenuGUI.IsChatOpen || Player.IsChannelingImportantSpell() || Player.IsRecalling()) return;
            if (Orbwalk.CurrentMode == Orbwalk.Mode.Combo || Orbwalk.CurrentMode == Orbwalk.Mode.Harass)
            {
                NormalCombo(Orbwalk.CurrentMode.ToString());
            }
            else if (Orbwalk.CurrentMode == Orbwalk.Mode.LaneClear) LaneJungClear();
        }

        private void OnDraw(EventArgs args)
        {
            if (Player.IsDead) return;
            if (ItemBool("Draw", "Q") && Q.Level > 0) Render.Circle.DrawCircle(Player.Position, Q.Range, Q.IsReady() ? Color.Green : Color.Red, 7);
            if (ItemBool("Draw", "W") && W.Level > 0) Render.Circle.DrawCircle(Player.Position, W.Range, W.IsReady() ? Color.Green : Color.Red, 7);
            if (ItemBool("Draw", "E") && E.Level > 0) Render.Circle.DrawCircle(Player.Position, E.Range, E.IsReady() ? Color.Green : Color.Red, 7);
            if (ItemBool("Draw", "R") && R.Level > 0) Render.Circle.DrawCircle(Player.Position, R.Range, R.IsReady() ? Color.Green : Color.Red, 7);
        }

        private void OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (!ItemBool("Misc", "QAntiGap") || Player.IsDead || !Q.CanCast(gapcloser.Sender) || Player.Distance3D(gapcloser.Sender) > 400) return;
            CastSkillShotSmite(Q, (Obj_AI_Hero)gapcloser.Sender);
        }

        private void OnPossibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            if (!ItemBool("Misc", "QInterrupt") || !Q.CanCast(unit)) return;
            CastSkillShotSmite(Q, (Obj_AI_Hero)unit);
        }

        private void NormalCombo(string Mode)
        {
            if (ItemBool(Mode, "W") && W.IsReady() && Player.HasBuff("AuraofDespair") && Player.CountEnemysInRange(500) == 0) W.Cast(PacketCast());
            if (!targetObj.IsValidTarget()) return;
            if (Mode == "Combo" && ItemBool(Mode, "Q") && Q.IsReady())
            {
                var nearObj = ObjectManager.Get<Obj_AI_Base>().Where(i => i.IsValidTarget(Q.Range) && !(i is Obj_AI_Turret) && i.CountEnemysInRange((int)R.Range - 20) >= ItemSlider(Mode, "RAbove") && Q.GetPrediction(i).Hitchance >= HitChance.Medium).OrderBy(i => i.CountEnemysInRange((int)R.Range));
                if (ItemBool(Mode, "R") && R.IsReady() && ItemList(Mode, "RMode") == 1 && nearObj.Count() > 0)
                {
                    foreach (var Obj in nearObj) Q.CastIfHitchanceEquals(Obj, HitChance.VeryHigh, PacketCast());
                }
                else if (Q.InRange(targetObj) && (CanKill(targetObj, Q) || !Orbwalk.InAutoAttackRange(targetObj))) CastSkillShotSmite(Q, targetObj);
            }
            if (ItemBool(Mode, "W") && W.IsReady())
            {
                if (Player.ManaPercentage() >= ItemSlider(Mode, "WAbove"))
                {
                    if (Player.Distance3D(targetObj) <= W.Range + 60)
                    {
                        if (!Player.HasBuff("AuraofDespair")) W.Cast(PacketCast());
                    }
                    else if (Player.HasBuff("AuraofDespair")) W.Cast(PacketCast());
                }
                else if (Player.HasBuff("AuraofDespair")) W.Cast(PacketCast());
            }
            if (ItemBool(Mode, "E") && E.CanCast(targetObj)) E.Cast(PacketCast());
            if (Mode == "Combo" && ItemBool(Mode, "R") && R.IsReady())
            {
                switch (ItemList(Mode, "RMode"))
                {
                    case 0:
                        if (R.InRange(targetObj) && CanKill(targetObj, R)) R.Cast(PacketCast());
                        break;
                    case 1:
                        var Obj = ObjectManager.Get<Obj_AI_Hero>().Where(i => i.IsValidTarget(R.Range));
                        if (Obj.Count() > 0 && (Obj.Count() >= ItemSlider(Mode, "RAbove") || (Obj.Count() >= 2 && Obj.Count(i => CanKill(i, R)) >= 1))) R.Cast(PacketCast());
                        break;
                }
            }
            if (Mode == "Combo" && ItemBool(Mode, "Item") && RanduinOmen.IsReady() && Player.CountEnemysInRange((int)RanduinOmen.Range) >= 1) RanduinOmen.Cast();
            if (Mode == "Combo" && ItemBool(Mode, "Ignite") && IgniteReady()) CastIgnite(targetObj);
        }

        private void LaneJungClear()
        {
            var minionObj = MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth);
            if (minionObj.Count() == 0 && ItemBool("Clear", "W") && W.IsReady() && Player.HasBuff("AuraofDespair")) W.Cast(PacketCast());
            foreach (var Obj in minionObj)
            {
                if (Obj.Team == GameObjectTeam.Neutral && CanSmiteMob(Obj.Name)) CastSmite(Obj);
                if (ItemBool("Clear", "E") && E.CanCast(Obj)) E.Cast(PacketCast());
                if (ItemBool("Clear", "W") && W.IsReady())
                {
                    if (Player.ManaPercentage() >= ItemSlider("Clear", "WAbove"))
                    {
                        if (minionObj.Count(i => Player.Distance3D(i) <= W.Range + 60) >= 2 || (Obj.MaxHealth >= 1200 && Player.Distance3D(Obj) <= W.Range + 60))
                        {
                            if (!Player.HasBuff("AuraofDespair")) W.Cast(PacketCast());
                        }
                        else if (Player.HasBuff("AuraofDespair")) W.Cast(PacketCast());
                    }
                    else if (Player.HasBuff("AuraofDespair")) W.Cast(PacketCast());
                }
                if (ItemBool("Clear", "Q") && Q.IsReady() && (CanKill(Obj, Q) || !Orbwalk.InAutoAttackRange(Obj))) Q.CastIfHitchanceEquals(Obj, HitChance.Medium, PacketCast());
            }
        }
    }
}