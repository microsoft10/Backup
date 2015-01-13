using System;
using System.Linq;
using Color = System.Drawing.Color;

using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

using Orbwalk = MasterSeries.Common.M_Orbwalker;

namespace MasterSeries.Champions
{
    class Kennen : Program
    {
        public Kennen()
        {
            Q = new Spell(SpellSlot.Q, 1060.23f);
            W = new Spell(SpellSlot.W, 914.645f);
            E = new Spell(SpellSlot.E);
            R = new Spell(SpellSlot.R, 562.425f);
            Q.SetSkillshot(0.65f, 50, 1700, true, SkillshotType.SkillshotLine);
            W.SetSkillshot(0.5f, 914.645f, float.MaxValue, false, SkillshotType.SkillshotCircle);
            R.SetSkillshot(0.5f, 562.425f, 779.9f, false, SkillshotType.SkillshotCircle);

            var ChampMenu = new Menu("Plugin", Name + "Plugin");
            {
                var ComboMenu = new Menu("Combo", "Combo");
                {
                    ItemBool(ComboMenu, "Q", "Use Q");
                    ItemBool(ComboMenu, "W", "Use W");
                    ItemBool(ComboMenu, "R", "Use R");
                    ItemList(ComboMenu, "RMode", "-> Mode", new[] { "Always", "# Enemy" });
                    ItemSlider(ComboMenu, "RAbove", "--> If Enemy Above", 2, 1, 4);
                    ItemBool(ComboMenu, "Item", "Use Item");
                    ItemBool(ComboMenu, "Ignite", "Auto Ignite If Killable");
                    ChampMenu.AddSubMenu(ComboMenu);
                }
                var HarassMenu = new Menu("Harass", "Harass");
                {
                    ItemBool(HarassMenu, "Q", "Use Q");
                    ItemBool(HarassMenu, "W", "Use W");
                    ItemSlider(HarassMenu, "WAbove", "-> If Energy Above", 50, 0, 200);
                    ChampMenu.AddSubMenu(HarassMenu);
                }
                var ClearMenu = new Menu("Lane/Jungle Clear", "Clear");
                {
                    ItemBool(ClearMenu, "Q", "Use Q");
                    ItemBool(ClearMenu, "W", "Use W");
                    ChampMenu.AddSubMenu(ClearMenu);
                }
                var MiscMenu = new Menu("Misc", "Misc");
                {
                    ItemBool(MiscMenu, "QLastHit", "Use Q To Last Hit");
                    ItemBool(MiscMenu, "QKillSteal", "Use Q To Kill Steal");
                    ItemBool(MiscMenu, "WKillSteal", "Use W To Kill Steal");
                    ItemSlider(MiscMenu, "CustomSkin", "Skin Changer", 5, 0, 5).ValueChanged += SkinChanger;
                    ChampMenu.AddSubMenu(MiscMenu);
                }
                var DrawMenu = new Menu("Draw", "Draw");
                {
                    ItemBool(DrawMenu, "Q", "Q Range", false);
                    ItemBool(DrawMenu, "W", "W Range", false);
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
            else if (Orbwalk.CurrentMode == Orbwalk.Mode.LastHit)
            {
                LastHit();
            }
            else if (Orbwalk.CurrentMode == Orbwalk.Mode.Flee) Flee();
            KillSteal();
        }

        private void OnDraw(EventArgs args)
        {
            if (Player.IsDead) return;
            if (ItemBool("Draw", "Q") && Q.Level > 0) Render.Circle.DrawCircle(Player.Position, Q.Range, Q.IsReady() ? Color.Green : Color.Red, 7);
            if (ItemBool("Draw", "W") && W.Level > 0) Render.Circle.DrawCircle(Player.Position, W.Range, W.IsReady() ? Color.Green : Color.Red, 7);
            if (ItemBool("Draw", "R") && R.Level > 0) Render.Circle.DrawCircle(Player.Position, R.Range, R.IsReady() ? Color.Green : Color.Red, 7);
        }

        private void NormalCombo(string Mode)
        {
            if (!targetObj.IsValidTarget()) return;
            if (ItemBool(Mode, "Q") && Q.CanCast(targetObj)) Q.CastIfHitchanceEquals(targetObj, HitChance.VeryHigh, PacketCast());
            if (ItemBool(Mode, "W") && W.IsReady())
            {
                var Obj = ObjectManager.Get<Obj_AI_Hero>().Where(i => i.IsValidTarget(W.Range) && i.HasBuff("KennenMarkOfStorm"));
                if (Obj.Count() > 0 && (Mode == "Combo" || (Mode == "Harass" && Player.Mana >= ItemSlider(Mode, "WAbove"))) && ((Obj.Count() >= 2 && Obj.Count(i => CanKill(i, W, 1)) >= 1) || Obj.Count(i => i.Buffs.First(a => a.DisplayName == "KennenMarkOfStorm").Count == 2) >= 2 || Obj.Count() >= 3 || (Obj.Count(i => i.Buffs.First(a => a.DisplayName == "KennenMarkOfStorm").Count == 2) < 2 && Obj.Count(i => i.Buffs.First(a => a.DisplayName == "KennenMarkOfStorm").Count == 1) >= 1))) W.Cast(PacketCast());
            }
            if (Mode == "Combo" && ItemBool(Mode, "R") && R.IsReady())
            {
                switch (ItemList(Mode, "RMode"))
                {
                    case 0:
                        if (R.InRange(targetObj)) R.Cast(PacketCast());
                        break;
                    case 1:
                        var Obj = ObjectManager.Get<Obj_AI_Hero>().Where(i => i.IsValidTarget(R.Range));
                        if (Obj.Count() > 0 && (Obj.Count() >= ItemSlider(Mode, "RAbove") || (Obj.Count() >= 2 && Obj.Count(i => CanKill(i, R, GetRDmg(i))) >= 1))) R.Cast(PacketCast());
                        break;
                }
            }
            if (Mode == "Combo" && ItemBool(Mode, "Item")) UseItem(targetObj);
            if (Mode == "Combo" && ItemBool(Mode, "Ignite") && IgniteReady()) CastIgnite(targetObj);
        }

        private void LaneJungClear()
        {
            var minionObj = MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth);
            foreach (var Obj in minionObj)
            {
                if (ItemBool("Clear", "Q") && Q.IsReady() && (CanKill(Obj, Q) || Obj.MaxHealth >= 1200)) Q.CastIfHitchanceEquals(Obj, HitChance.Medium, PacketCast());
                if (ItemBool("Clear", "W") && W.IsReady() && minionObj.Count(i => W.InRange(i) && i.HasBuff("KennenMarkOfStorm")) >= 2) W.Cast(PacketCast());
            }
        }

        private void LastHit()
        {
            if (!ItemBool("Misc", "QLastHit") || !Q.IsReady()) return;
            foreach (var Obj in MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth).Where(i => CanKill(i, Q)).OrderByDescending(i => i.Distance3D(Player))) Q.CastIfHitchanceEquals(Obj, HitChance.VeryHigh, PacketCast());
        }

        private void Flee()
        {
            if (E.IsReady() && E.Instance.Name == "KennenLightningRush") E.Cast(PacketCast());
            if (W.IsReady() && ObjectManager.Get<Obj_AI_Hero>().Count(i => i.IsValidTarget(W.Range) && i.HasBuff("KennenMarkOfStorm") && i.Buffs.First(a => a.DisplayName == "KennenMarkOfStorm").Count == 2) > 0) W.Cast(PacketCast());
        }

        private void KillSteal()
        {
            if (ItemBool("Misc", "QKillSteal") && Q.IsReady())
            {
                foreach (var Obj in ObjectManager.Get<Obj_AI_Hero>().Where(i => i.IsValidTarget(Q.Range) && CanKill(i, Q) && i != targetObj).OrderBy(i => i.Health).OrderBy(i => i.Distance3D(Player))) Q.CastIfHitchanceEquals(Obj, HitChance.VeryHigh, PacketCast());
            }
            if (ItemBool("Misc", "WKillSteal") && W.IsReady() && ObjectManager.Get<Obj_AI_Hero>().Count(i => i.IsValidTarget(W.Range) && CanKill(i, W, 1) && i.HasBuff("KennenMarkOfStorm") && i != targetObj) >= 1) W.Cast(PacketCast());
        }

        private void UseItem(Obj_AI_Base Target)
        {
            if (Deathfire.IsReady()) Deathfire.Cast(targetObj);
            if (Blackfire.IsReady()) Blackfire.Cast(targetObj);
            if (Bilgewater.IsReady()) Bilgewater.Cast(Target);
            if (HexGun.IsReady()) HexGun.Cast(Target);
            if (BladeRuined.IsReady()) BladeRuined.Cast(Target);
        }

        private double GetRDmg(Obj_AI_Hero Target)
        {
            return Player.CalcDamage(Target, Damage.DamageType.Magical, (new double[] { 80, 145, 210 }[R.Level - 1] + 0.4 * Player.FlatMagicDamageMod) * 3);
        }
    }
}