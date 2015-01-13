using System;
using System.Linq;
using Color = System.Drawing.Color;

using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

using Orbwalk = MasterSeries.Common.M_Orbwalker;

namespace MasterSeries.Champions
{
    class Maokai : Program
    {
        public Maokai()
        {
            Q = new Spell(SpellSlot.Q, 630);
            W = new Spell(SpellSlot.W, 525);
            E = new Spell(SpellSlot.E, 1115);
            R = new Spell(SpellSlot.R, 478);
            Q.SetSkillshot(0.3333f, 110, 1100, false, SkillshotType.SkillshotLine);
            W.SetTargetted(0.5f, 1000);
            E.SetSkillshot(0.25f, 225, 1750, false, SkillshotType.SkillshotCircle);
            R.SetSkillshot(0.25f, 478, float.MaxValue, false, SkillshotType.SkillshotCircle);

            var ChampMenu = new Menu("Plugin", Name + "Plugin");
            {
                var ComboMenu = new Menu("Combo", "Combo");
                {
                    ItemBool(ComboMenu, "Q", "Use Q");
                    ItemBool(ComboMenu, "W", "Use W");
                    ItemBool(ComboMenu, "E", "Use E");
                    ItemBool(ComboMenu, "R", "Use R");
                    ItemBool(ComboMenu, "RKill", "-> Cancel R If Killable");
                    ItemSlider(ComboMenu, "RAbove", "-> Cancel R2 If Mp Under", 20);
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
                    ItemBool(ClearMenu, "E", "Use E");
                    ChampMenu.AddSubMenu(ClearMenu);
                }
                var MiscMenu = new Menu("Misc", "Misc");
                {
                    ItemBool(MiscMenu, "QKillSteal", "Use Q To Kill Steal");
                    ItemBool(MiscMenu, "QAntiGap", "Use Q To Anti Gap Closer");
                    ItemBool(MiscMenu, "QInterrupt", "Use Q To Interrupt");
                    ItemBool(MiscMenu, "WUnderTower", "Use W If Enemy Under Tower");
                    ItemSlider(MiscMenu, "CustomSkin", "Skin Changer", 5, 0, 5).ValueChanged += SkinChanger;
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
            if (ItemBool("Misc", "QKillSteal")) KillSteal();
            if (ItemBool("Misc", "WUnderTower")) AutoWUnderTower();
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
            if (!ItemBool("Misc", "QAntiGap") || Player.IsDead || !Q.CanCast(gapcloser.Sender)) return;
            if (Player.Distance3D(gapcloser.Sender) <= 100) Q.Cast(gapcloser.Sender.Position, PacketCast());
        }

        private void OnPossibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            if (!ItemBool("Misc", "QInterrupt") || Player.IsDead || !Q.IsReady()) return;
            if (Player.Distance3D(unit) > 100 && W.CanCast(unit) && Player.Mana >= Q.Instance.ManaCost + W.Instance.ManaCost)
            {
                W.CastOnUnit(unit, PacketCast());
                return;
            }
            if (Q.InRange(unit) && Player.Distance3D(unit) <= 100) Q.Cast(unit.Position, PacketCast());
        }

        private void NormalCombo(string Mode)
        {
            if (!targetObj.IsValidTarget()) return;
            if (ItemBool(Mode, "E") && E.CanCast(targetObj)) E.Cast(targetObj.Position.Extend(Player.Position, Player.Distance3D(targetObj) <= E.Range - 100 ? -100 : 0), PacketCast());
            if (Mode == "Combo" && ItemBool(Mode, "R") && R.CanCast(targetObj))
            {
                if (ItemBool(Mode, "RKill") && Player.HasBuff("MaokaiDrain") && (CanKill(targetObj, R, GetRDmg(targetObj)) || ObjectManager.Get<Obj_AI_Hero>().Count(i => i.IsValidTarget(R.Range) && CanKill(i, R, GetRDmg(i)) && i != targetObj) >= 1)) R.Cast(PacketCast());
                if (Player.ManaPercentage() >= ItemSlider(Mode, "RAbove"))
                {
                    if (!Player.HasBuff("MaokaiDrain")) R.Cast(PacketCast());
                }
                else if (Player.HasBuff("MaokaiDrain")) R.Cast(PacketCast());
            }
            if (ItemBool(Mode, "W") && W.CanCast(targetObj) && (Mode == "Combo" || (Mode == "Harass" && Player.HealthPercentage() >= ItemSlider(Mode, "WAbove")))) W.CastOnUnit(targetObj, PacketCast());
            if (ItemBool(Mode, "Q") && Q.CanCast(targetObj)) Q.Cast(targetObj.Position, PacketCast());
            if (Mode == "Combo" && ItemBool(Mode, "Item") && RanduinOmen.IsReady() && Player.CountEnemysInRange((int)RanduinOmen.Range) >= 1) RanduinOmen.Cast();
            if (Mode == "Combo" && ItemBool(Mode, "Ignite") && IgniteReady()) CastIgnite(targetObj);
        }

        private void LaneJungClear()
        {
            var minionObj = MinionManager.GetMinions(E.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth);
            foreach (var Obj in minionObj)
            {
                if (Obj.Team == GameObjectTeam.Neutral && CanSmiteMob(Obj.Name)) CastSmite(Obj);
                if (ItemBool("Clear", "E") && E.IsReady() && (minionObj.Count >= 2 || Obj.MaxHealth >= 1200)) E.Cast(GetClearPos(minionObj, E), PacketCast());
                if (ItemBool("Clear", "W") && W.CanCast(Obj) && (CanKill(Obj, W, W.GetDamage(Obj) > 300 ? Player.CalcDamage(Obj, Damage.DamageType.Magical, 300) : W.GetDamage(Obj)) || Obj.MaxHealth >= 1200)) W.CastOnUnit(Obj, PacketCast());
                if (ItemBool("Clear", "Q") && Q.IsReady()) Q.Cast(GetClearPos(minionObj.Where(i => Q.InRange(i)).ToList(), Q), PacketCast());
            }
        }

        private void KillSteal()
        {
            if (!Q.IsReady()) return;
            foreach (var Obj in ObjectManager.Get<Obj_AI_Hero>().Where(i => i.IsValidTarget(Q.Range) && CanKill(i, Q) && i != targetObj).OrderBy(i => i.Health).OrderBy(i => i.Distance3D(Player))) Q.Cast(Obj.Position, PacketCast());
        }

        private void AutoWUnderTower()
        {
            if (Player.UnderTurret() || !W.IsReady()) return;
            foreach (var Obj in ObjectManager.Get<Obj_AI_Hero>().Where(i => i.IsValidTarget(W.Range)).OrderBy(i => i.Distance3D(Player)))
            {
                var TowerObj = ObjectManager.Get<Obj_AI_Turret>().FirstOrDefault(i => i.IsValidTarget(950, false) && i.IsAlly);
                if (TowerObj != null && Obj.Distance3D(TowerObj) <= 950) W.CastOnUnit(Obj, PacketCast());
            }
        }

        private double GetRDmg(Obj_AI_Base Target)
        {
            return Player.CalcDamage(Target, Damage.DamageType.Magical, new double[] { 100, 150, 200 }[R.Level - 1] + 0.5 * Player.FlatMagicDamageMod + R.Instance.Ammo);
        }
    }
}