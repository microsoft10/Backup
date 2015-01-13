using System;
using System.Linq;
using Color = System.Drawing.Color;

using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

using Orbwalk = MasterSeries.Common.M_Orbwalker;

namespace MasterSeries.Champions
{
    class Nasus : Program
    {
        public Nasus()
        {
            Q = new Spell(SpellSlot.Q);
            W = new Spell(SpellSlot.W, 600);
            E = new Spell(SpellSlot.E, 650);
            R = new Spell(SpellSlot.R);
            Q.SetTargetted(0.2333f, float.MaxValue);
            E.SetSkillshot(0.5f, 380, float.MaxValue, false, SkillshotType.SkillshotCircle);

            var ChampMenu = new Menu("Plugin", Name + "Plugin");
            {
                var ComboMenu = new Menu("Combo", "Combo");
                {
                    ItemBool(ComboMenu, "Q", "Use Q");
                    ItemList(ComboMenu, "QMode", "-> Mode", new[] { "Always", "Smart" }, 1);
                    ItemBool(ComboMenu, "W", "Use W");
                    ItemBool(ComboMenu, "E", "Use E");
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
                    ItemBool(MiscMenu, "EKillSteal", "Use E To Kill Steal");
                    ItemSlider(MiscMenu, "CustomSkin", "Skin Changer", 5, 0, 5).ValueChanged += SkinChanger;
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
            Orbwalk.OnAttack += OnAttack;
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
            if (ItemBool("Misc", "EKillSteal")) KillSteal();
        }

        private void OnDraw(EventArgs args)
        {
            if (Player.IsDead) return;
            if (ItemBool("Draw", "W") && W.Level > 0) Render.Circle.DrawCircle(Player.Position, W.Range, W.IsReady() ? Color.Green : Color.Red, 7);
            if (ItemBool("Draw", "E") && E.Level > 0) Render.Circle.DrawCircle(Player.Position, E.Range, E.IsReady() ? Color.Green : Color.Red, 7);
        }

        private void OnAttack(AttackableUnit Target)
        {
            if (Target.IsValidTarget(Orbwalk.GetAutoAttackRange(Player, Target) + 20) && Q.IsReady())
            {
                var Obj = (Obj_AI_Base)Target;
                var DmgAA = Player.GetAutoAttackDamage(Obj) * Math.Floor(Q.Instance.Cooldown / 1 / Player.AttackDelay);
                if (Orbwalk.CurrentMode == Orbwalk.Mode.LastHit && ItemBool("Misc", "QLastHit") && CanKill(Obj, Q, GetBonusDmg(Obj)) && Target is Obj_AI_Minion)
                {
                    Q.Cast(PacketCast());
                }
                else if (((Orbwalk.CurrentMode == Orbwalk.Mode.LaneClear && ItemBool("Clear", "Q") && (Target is Obj_AI_Minion || Target is Obj_AI_Turret)) || (Orbwalk.CurrentMode == Orbwalk.Mode.Combo && ItemBool("Combo", "Q") && ItemList("Combo", "QMode") == 1 && Target is Obj_AI_Hero)) && (CanKill(Obj, Q, GetBonusDmg(Obj)) || (!(Target is Obj_AI_Turret) && !CanKill(Obj, Q, GetBonusDmg(Obj) + DmgAA))))
                {
                    Q.Cast(PacketCast());
                }
                else if ((Orbwalk.CurrentMode == Orbwalk.Mode.Harass || (Orbwalk.CurrentMode == Orbwalk.Mode.Combo && ItemList("Combo", "QMode") == 0)) && ItemBool(Orbwalk.CurrentMode.ToString(), "Q") && Target is Obj_AI_Hero) Q.Cast(PacketCast());
            }
        }

        private void NormalCombo(string Mode)
        {
            if (!targetObj.IsValidTarget()) return;
            if (ItemBool(Mode, "Q") && Q.IsReady() && Player.Distance3D(targetObj) <= Orbwalk.GetAutoAttackRange(Player, targetObj) + 20)
            {
                var DmgAA = Player.GetAutoAttackDamage(targetObj) * Math.Floor(Q.Instance.Cooldown / 1 / Player.AttackDelay);
                if (Mode == "Harass" || (Mode == "Combo" && (ItemList(Mode, "QMode") == 0 || (ItemList(Mode, "QMode") == 1 && (CanKill(targetObj, Q, GetBonusDmg(targetObj)) || !CanKill(targetObj, Q, GetBonusDmg(targetObj) + DmgAA))))))
                {
                    Orbwalk.SetAttack(false);
                    Player.IssueOrder(GameObjectOrder.AttackUnit, targetObj);
                    Orbwalk.SetAttack(true);
                }
            }
            if (ItemBool(Mode, "W") && W.CanCast(targetObj) && (Mode == "Combo" || (Mode == "Harass" && Player.Distance3D(targetObj) <= Orbwalk.GetAutoAttackRange(Player, targetObj) + 100))) W.CastOnUnit(targetObj, PacketCast());
            if (ItemBool(Mode, "E") && E.CanCast(targetObj) && (Mode == "Combo" || (Mode == "Harass" && Player.Distance3D(targetObj) <= Orbwalk.GetAutoAttackRange(Player, targetObj) + 100))) E.Cast(targetObj.Position.Randomize(0, (int)E.Width / 2), PacketCast());
            if (Mode == "Combo" && ItemBool(Mode, "Item") && RanduinOmen.IsReady() && Player.CountEnemysInRange((int)RanduinOmen.Range) >= 1) RanduinOmen.Cast();
            if (Mode == "Combo" && ItemBool(Mode, "Ignite") && IgniteReady()) CastIgnite(targetObj);
        }

        private void LaneJungClear()
        {
            var towerObj = ObjectManager.Get<Obj_AI_Turret>().FirstOrDefault(i => i.IsValidTarget(Orbwalk.GetAutoAttackRange(Player, i) + 20) && CanKill(i, Q, GetBonusDmg(i)));
            if (towerObj != null && Q.IsReady())
            {
                Orbwalk.SetAttack(false);
                Player.IssueOrder(GameObjectOrder.AttackUnit, towerObj);
                Orbwalk.SetAttack(true);
            }
            var minionObj = MinionManager.GetMinions(E.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth);
            foreach (var Obj in minionObj)
            {
                if (Obj.Team == GameObjectTeam.Neutral && CanSmiteMob(Obj.Name)) CastSmite(Obj);
                if (ItemBool("Clear", "Q") && Q.IsReady() && Player.Distance3D(Obj) <= Orbwalk.GetAutoAttackRange(Player, Obj) + 20)
                {
                    var DmgAA = Player.GetAutoAttackDamage(Obj) * Math.Floor(Q.Instance.Cooldown / 1 / Player.AttackDelay);
                    if (CanKill(targetObj, Q, GetBonusDmg(targetObj)) || !CanKill(Obj, Q, GetBonusDmg(Obj) + DmgAA))
                    {
                        Orbwalk.SetAttack(false);
                        Player.IssueOrder(GameObjectOrder.AttackUnit, Obj);
                        Orbwalk.SetAttack(true);
                        break;
                    }
                }
                if (ItemBool("Clear", "E") && E.IsReady() && (minionObj.Count >= 2 || Obj.MaxHealth >= 1200)) E.Cast(GetClearPos(minionObj, E), PacketCast());
            }
        }

        private void LastHit()
        {
            if (!ItemBool("Misc", "QLastHit") || !Q.IsReady()) return;
            foreach (var Obj in MinionManager.GetMinions(Orbwalk.GetAutoAttackRange() + 100, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth).Where(i => CanKill(i, Q, GetBonusDmg(i))).OrderByDescending(i => i.Distance3D(Player)))
            {
                Orbwalk.SetAttack(false);
                Player.IssueOrder(GameObjectOrder.AttackUnit, Obj);
                Orbwalk.SetAttack(true);
                break;
            }
        }

        private void KillSteal()
        {
            if (!E.IsReady()) return;
            foreach (var Obj in ObjectManager.Get<Obj_AI_Hero>().Where(i => i.IsValidTarget(E.Range) && CanKill(i, E) && i != targetObj).OrderBy(i => i.Health).OrderByDescending(i => i.Distance3D(Player))) E.Cast(Obj.Position.Randomize(0, (int)E.Width / 2), PacketCast());
        }

        private double GetBonusDmg(Obj_AI_Base Target)
        {
            double DmgItem = 0;
            if (Sheen.IsOwned() && ((Sheen.IsReady() && Q.IsReady()) || Player.HasBuff("Sheen")) && Player.BaseAttackDamage > DmgItem) DmgItem = Player.BaseAttackDamage;
            if (Iceborn.IsOwned() && ((Iceborn.IsReady() && Q.IsReady()) || Player.HasBuff("ItemFrozenFist")) && Player.BaseAttackDamage * 1.25 > DmgItem) DmgItem = Player.BaseAttackDamage * 1.25;
            if (Trinity.IsOwned() && ((Trinity.IsReady() && Q.IsReady()) || Player.HasBuff("Sheen")) && Player.BaseAttackDamage * 2 > DmgItem) DmgItem = Player.BaseAttackDamage * 2;
            return (Q.IsReady() ? Q.GetDamage(Target) : 0) + Player.GetAutoAttackDamage(Target, Q.IsReady() ? false : true) + Player.CalcDamage(Target, Damage.DamageType.Physical, DmgItem);
        }
    }
}