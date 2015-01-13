using System;
using System.Linq;
using Color = System.Drawing.Color;

using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

using Orbwalk = MasterSeries.Common.M_Orbwalker;

namespace MasterSeries.Champions
{
    class Garen : Program
    {
        public Garen()
        {
            Q = new Spell(SpellSlot.Q);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 325);
            R = new Spell(SpellSlot.R, 400);
            Q.SetTargetted(0.2333f, float.MaxValue);
            E.SetSkillshot(0, 325, 700, false, SkillshotType.SkillshotCircle);
            R.SetTargetted(0.13f, 900);

            var ChampMenu = new Menu("Plugin", Name + "Plugin");
            {
                var ComboMenu = new Menu("Combo", "Combo");
                {
                    ItemBool(ComboMenu, "Q", "Use Q");
                    ItemBool(ComboMenu, "W", "Use W");
                    ItemSlider(ComboMenu, "WUnder", "-> If Hp Under", 60);
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
                    ItemSlider(HarassMenu, "WUnder", "-> If Hp Under", 60);
                    ItemBool(HarassMenu, "E", "Use E");
                    ChampMenu.AddSubMenu(HarassMenu);
                }
                var ClearMenu = new Menu("Lane/Jungle Clear", "Clear");
                {
                    ItemBool(ClearMenu, "Q", "Use Q");
                    ItemList(ClearMenu, "QMode", "-> Mode", new[] { "Always", "Killable" });
                    ItemBool(ClearMenu, "E", "Use E");
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
            Obj_AI_Base.OnProcessSpellCast += TrySurviveSpellCast;
            Orbwalk.OnAttack += OnAttack;
            Orbwalk.AfterAttack += AfterAttack;
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
            else if (Orbwalk.CurrentMode == Orbwalk.Mode.LastHit)
            {
                LastHit();
            }
            else if (Orbwalk.CurrentMode == Orbwalk.Mode.Flee && Q.IsReady()) Q.Cast(PacketCast());
            if (ItemBool("Misc", "WSurvive") && W.IsReady()) TrySurvive(W.Slot);
        }

        private void OnDraw(EventArgs args)
        {
            if (Player.IsDead) return;
            if (ItemBool("Draw", "E") && E.Level > 0) Render.Circle.DrawCircle(Player.Position, E.Range, E.IsReady() ? Color.Green : Color.Red, 7);
            if (ItemBool("Draw", "R") && R.Level > 0) Render.Circle.DrawCircle(Player.Position, R.Range, R.IsReady() ? Color.Green : Color.Red, 7);
        }

        private void OnAttack(AttackableUnit Target)
        {
            if (Target.IsValidTarget(Orbwalk.GetAutoAttackRange(Player, Target) + 20) && Q.IsReady())
            {
                if (Orbwalk.CurrentMode == Orbwalk.Mode.Harass && ItemBool("Harass", "Q") && Target is Obj_AI_Hero)
                {
                    Q.Cast(PacketCast());
                }
                else if (Target is Obj_AI_Minion && CanKill((Obj_AI_Minion)Target, Q) && ((Orbwalk.CurrentMode == Orbwalk.Mode.LastHit && ItemBool("Misc", "QLastHit")) || (Orbwalk.CurrentMode == Orbwalk.Mode.LaneClear && ItemBool("Clear", "Q") && ItemList("Clear", "QMode") == 1))) Q.Cast(PacketCast());
            }
        }

        private void AfterAttack(AttackableUnit Target)
        {
            if (Orbwalk.CurrentMode == Orbwalk.Mode.Combo && ItemBool("Combo", "Q") && Q.IsReady() && Target.IsValidTarget(Orbwalk.GetAutoAttackRange(Player, Target) + 20) && Target is Obj_AI_Hero) Q.Cast(PacketCast());
        }

        private void NormalCombo(string Mode)
        {
            if (!targetObj.IsValidTarget()) return;
            if (ItemBool(Mode, "Q") && Q.IsReady() && Player.Distance3D(targetObj) <= ((Mode == "Harass") ? Orbwalk.GetAutoAttackRange(Player, targetObj) + 20 : 800) && (Mode == "Harass" || (Mode == "Combo" && !Orbwalk.InAutoAttackRange(targetObj))))
            {
                if (Mode == "Harass")
                {
                    Orbwalk.SetAttack(false);
                    Player.IssueOrder(GameObjectOrder.AttackUnit, targetObj);
                    Orbwalk.SetAttack(true);
                }
                else Q.Cast(PacketCast());
            }
            if (ItemBool(Mode, "E") && E.CanCast(targetObj) && !Player.HasBuff("GarenE") && !Player.HasBuff("GarenQBuff")) E.Cast(PacketCast());
            if (ItemBool(Mode, "W") && W.IsReady() && Orbwalk.InAutoAttackRange(targetObj) && Player.HealthPercentage() <= ItemSlider(Mode, "WUnder")) W.Cast(PacketCast());
            if (Mode == "Combo" && ItemBool(Mode, "R") && ItemBool("Ultimate", targetObj.ChampionName) && R.CanCast(targetObj) && CanKill(targetObj, R)) R.CastOnUnit(targetObj, PacketCast());
            if (Mode == "Combo" && ItemBool(Mode, "Item") && RanduinOmen.IsReady() && Player.CountEnemysInRange((int)RanduinOmen.Range) >= 1) RanduinOmen.Cast();
            if (Mode == "Combo" && ItemBool(Mode, "Ignite") && IgniteReady()) CastIgnite(targetObj);
        }

        private void LaneJungClear()
        {
            foreach (var Obj in MinionManager.GetMinions(700, MinionTypes.All, MinionTeam.NotAlly))
            {
                if (ItemBool("Clear", "Q") && Q.IsReady())
                {
                    switch (ItemList("Clear", "QMode"))
                    {
                        case 0:
                            Q.Cast(PacketCast());
                            break;
                        case 1:
                            if (CanKill(Obj, Q) && Player.Distance3D(Obj) <= Orbwalk.GetAutoAttackRange(Player, Obj) + 20)
                            {
                                Orbwalk.SetAttack(false);
                                Player.IssueOrder(GameObjectOrder.AttackUnit, Obj);
                                Orbwalk.SetAttack(true);
                                break;
                            }
                            break;
                    }
                }
                if (ItemBool("Clear", "E") && E.CanCast(Obj) && !Player.HasBuff("GarenE") && !Player.HasBuff("GarenQBuff")) E.Cast(PacketCast());
            }
        }

        private void LastHit()
        {
            if (!ItemBool("Misc", "QLastHit") || !Q.IsReady()) return;
            foreach (var Obj in MinionManager.GetMinions(Orbwalk.GetAutoAttackRange() + 100, MinionTypes.All, MinionTeam.NotAlly).Where(i => CanKill(i, Q)).OrderByDescending(i => i.Distance3D(Player)))
            {
                Orbwalk.SetAttack(false);
                Player.IssueOrder(GameObjectOrder.AttackUnit, Obj);
                Orbwalk.SetAttack(true);
                break;
            }
        }
    }
}