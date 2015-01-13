using System;
using System.Linq;
using Color = System.Drawing.Color;

using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

using Orbwalk = MasterSeries.Common.M_Orbwalker;

namespace MasterSeries.Champions
{
    class Rammus : Program
    {
        public Rammus()
        {
            Q = new Spell(SpellSlot.Q);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 325);
            R = new Spell(SpellSlot.R, 375);
            E.SetTargetted(0.5f, float.MaxValue);
            R.SetSkillshot(0.5f, 375, float.MaxValue, false, SkillshotType.SkillshotCircle);

            var ChampMenu = new Menu("Plugin", Name + "Plugin");
            {
                var ComboMenu = new Menu("Combo", "Combo");
                {
                    ItemBool(ComboMenu, "Q", "Use Q");
                    ItemBool(ComboMenu, "W", "Use W");
                    ItemBool(ComboMenu, "E", "Use E");
                    ItemList(ComboMenu, "EMode", "-> Mode", new[] { "Always", "Have W" });
                    ItemBool(ComboMenu, "R", "Use R");
                    ItemList(ComboMenu, "RMode", "-> Mode", new[] { "Always", "# Enemy" });
                    ItemSlider(ComboMenu, "RCount", "--> If Enemy Above", 2, 1, 4);
                    ItemBool(ComboMenu, "Item", "Use Item");
                    ItemBool(ComboMenu, "Ignite", "Auto Ignite If Killable");
                    ChampMenu.AddSubMenu(ComboMenu);
                }
                var HarassMenu = new Menu("Harass", "Harass");
                {
                    ItemBool(HarassMenu, "Q", "Use Q");
                    ItemBool(HarassMenu, "W", "Use W");
                    ItemBool(HarassMenu, "E", "Use E");
                    ItemList(HarassMenu, "EMode", "-> Mode", new[] { "Always", "Have W" });
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
                    ItemList(ClearMenu, "EMode", "-> Mode", new[] { "Always", "Have W" });
                    ChampMenu.AddSubMenu(ClearMenu);
                }
                var MiscMenu = new Menu("Misc", "Misc");
                {
                    ItemBool(MiscMenu, "QAntiGap", "Use Q To Anti Gap Closer");
                    ItemBool(MiscMenu, "EInterrupt", "Use E To Interrupt");
                    ItemBool(MiscMenu, "WSurvive", "Try Use W To Survive");
                    ItemSlider(MiscMenu, "CustomSkin", "Skin Changer", 6, 0, 6).ValueChanged += SkinChanger;
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
            AntiGapcloser.OnEnemyGapcloser += OnEnemyGapcloser;
            Interrupter.OnPossibleToInterrupt += OnPossibleToInterrupt;
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
            else if (Orbwalk.CurrentMode == Orbwalk.Mode.Flee && Q.IsReady() && !Player.HasBuff("PowerBall")) Q.Cast(PacketCast());
            if (ItemBool("Misc", "WSurvive") && W.IsReady()) TrySurvive(W.Slot);
        }

        private void OnDraw(EventArgs args)
        {
            if (Player.IsDead) return;
            if (ItemBool("Draw", "E") && E.Level > 0) Render.Circle.DrawCircle(Player.Position, E.Range, E.IsReady() ? Color.Green : Color.Red, 7);
            if (ItemBool("Draw", "R") && R.Level > 0) Render.Circle.DrawCircle(Player.Position, R.Range, R.IsReady() ? Color.Green : Color.Red, 7);
        }

        private void OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (!ItemBool("Misc", "QAntiGap") || Player.IsDead || !Q.IsReady()) return;
            if (Player.Distance3D(gapcloser.Sender) <= Orbwalk.GetAutoAttackRange(Player, gapcloser.Sender) + 30 && !Player.HasBuff("PowerBall")) Q.Cast(PacketCast());
        }

        private void OnPossibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            if (!ItemBool("Misc", "EInterrupt") || Player.IsDead || !E.CanCast(unit) || !Player.HasBuff("PowerBall")) return;
            E.CastOnUnit(unit, PacketCast());
        }

        private void NormalCombo(string Mode)
        {
            if (!targetObj.IsValidTarget()) return;
            if (ItemBool(Mode, "Q") && Q.IsReady() && Player.Distance3D(targetObj) <= ((Mode == "Combo") ? 800 : Orbwalk.GetAutoAttackRange(Player, targetObj) + 30) && !Player.HasBuff("PowerBall"))
            {
                if ((ItemBool(Mode, "E") && E.IsReady() && !E.InRange(targetObj)) || !Player.HasBuff("DefensiveBallCurl")) Q.Cast(PacketCast());
            }
            if (ItemBool(Mode, "W") && W.IsReady() && Orbwalk.InAutoAttackRange(targetObj) && !Player.HasBuff("PowerBall")) W.Cast(PacketCast());
            if (ItemBool(Mode, "E") && E.CanCast(targetObj) && !Player.HasBuff("PowerBall"))
            {
                switch (ItemList(Mode, "EMode"))
                {
                    case 0:
                        E.CastOnUnit(targetObj, PacketCast());
                        break;
                    case 1:
                        if (!ItemBool(Mode, "W") || (ItemBool(Mode, "W") && Player.HasBuff("DefensiveBallCurl"))) E.CastOnUnit(targetObj, PacketCast());
                        break;
                }
            }
            if (Mode == "Combo" && ItemBool(Mode, "R") && R.IsReady())
            {
                switch (ItemList(Mode, "RMode"))
                {
                    case 0:
                        if (R.InRange(targetObj)) R.Cast(PacketCast());
                        break;
                    case 1:
                        if (Player.CountEnemysInRange((int)R.Range) >= ItemSlider(Mode, "RCount")) R.Cast(PacketCast());
                        break;
                }
            }
            if (Mode == "Combo" && ItemBool(Mode, "Item") && RanduinOmen.IsReady() && Player.CountEnemysInRange((int)RanduinOmen.Range) >= 1) RanduinOmen.Cast();
            if (Mode == "Combo" && ItemBool(Mode, "Ignite") && IgniteReady()) CastIgnite(targetObj);
        }

        private void LaneJungClear()
        {
            foreach (var Obj in MinionManager.GetMinions(800, MinionTypes.All, MinionTeam.NotAlly))
            {
                if (Obj.Team == GameObjectTeam.Neutral && CanSmiteMob(Obj.Name)) CastSmite(Obj);
                if (ItemBool("Clear", "Q") && Q.IsReady() && !Player.HasBuff("PowerBall"))
                {
                    if ((ItemBool("Clear", "E") && E.IsReady() && !E.InRange(Obj)) || !Player.HasBuff("DefensiveBallCurl")) Q.Cast(PacketCast());
                }
                if (ItemBool("Clear", "W") && W.IsReady() && Orbwalk.InAutoAttackRange(Obj) && !Player.HasBuff("PowerBall")) W.Cast(PacketCast());
                if (ItemBool("Clear", "E") && E.CanCast(Obj) && !Player.HasBuff("PowerBall") && Obj.Team == GameObjectTeam.Neutral)
                {
                    switch (ItemList("Clear", "EMode"))
                    {
                        case 0:
                            E.CastOnUnit(Obj, PacketCast());
                            break;
                        case 1:
                            if (!ItemBool("Clear", "W") || (ItemBool("Clear", "W") && Player.HasBuff("DefensiveBallCurl"))) E.CastOnUnit(Obj, PacketCast());
                            break;
                    }
                }
            }
        }
    }
}