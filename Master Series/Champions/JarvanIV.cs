using System;
using System.Linq;
using Color = System.Drawing.Color;

using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

using Orbwalk = MasterSeries.Common.M_Orbwalker;

namespace MasterSeries.Champions
{
    class JarvanIV : Program
    {
        private bool RCasted = false;
        private Vector3 FlagPos = default(Vector3);

        public JarvanIV()
        {
            Q = new Spell(SpellSlot.Q, 840);
            W = new Spell(SpellSlot.W, 505);
            E = new Spell(SpellSlot.E, 860);
            R = new Spell(SpellSlot.R, 650);
            Q.SetSkillshot(0.5f, 70, float.MaxValue, false, SkillshotType.SkillshotLine);
            E.SetSkillshot(0.5f, 175, 1450, false, SkillshotType.SkillshotCircle);
            R.SetTargetted(0.5f, float.MaxValue);

            var ChampMenu = new Menu("Plugin", Name + "Plugin");
            {
                var ComboMenu = new Menu("Combo", "Combo");
                {
                    ItemBool(ComboMenu, "Q", "Use Q");
                    ItemBool(ComboMenu, "W", "Use W");
                    ItemSlider(ComboMenu, "WUnder", "-> If Hp Under", 20);
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
                    ItemBool(HarassMenu, "Q", "Use Q");
                    ItemSlider(HarassMenu, "QAbove", "-> To Flag If Hp Above", 20);
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
                    ItemBool(ClearMenu, "Item", "Use Tiamat/Hydra Item");
                    ChampMenu.AddSubMenu(ClearMenu);
                }
                var MiscMenu = new Menu("Misc", "Misc");
                {
                    ItemBool(MiscMenu, "QLastHit", "Use Q To Last Hit");
                    ItemBool(MiscMenu, "QKillSteal", "Use Q To Kill Steal");
                    ItemBool(MiscMenu, "EQInterrupt", "Use EQ To Interrupt");
                    ItemBool(MiscMenu, "WSurvive", "Try Use W To Survive");
                    ItemSlider(MiscMenu, "CustomSkin", "Skin Changer", 5, 0, 6).ValueChanged += SkinChanger;
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
            Interrupter.OnPossibleToInterrupt += OnPossibleToInterrupt;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
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
            else if (Orbwalk.CurrentMode == Orbwalk.Mode.LastHit)
            {
                LastHit();
            }
            else if (Orbwalk.CurrentMode == Orbwalk.Mode.Flee) Flee();
            if (ItemBool("Misc", "QKillSteal")) KillSteal();
            if (ItemBool("Misc", "WSurvive") && W.IsReady()) TrySurvive(W.Slot);
        }

        private void OnDraw(EventArgs args)
        {
            if (Player.IsDead) return;
            if (ItemBool("Draw", "Q") && Q.Level > 0) Render.Circle.DrawCircle(Player.Position, Q.Range, Q.IsReady() ? Color.Green : Color.Red, 7);
            if (ItemBool("Draw", "W") && W.Level > 0) Render.Circle.DrawCircle(Player.Position, W.Range, W.IsReady() ? Color.Green : Color.Red, 7);
            if (ItemBool("Draw", "E") && E.Level > 0) Render.Circle.DrawCircle(Player.Position, E.Range, E.IsReady() ? Color.Green : Color.Red, 7);
            if (ItemBool("Draw", "R") && R.Level > 0) Render.Circle.DrawCircle(Player.Position, R.Range, R.IsReady() ? Color.Green : Color.Red, 7);
        }

        private void OnPossibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            if (!ItemBool("Misc", "EQInterrupt") || !Q.IsReady()) return;
            if (Q.InRange(unit) && E.IsReady() && Player.Mana >= Q.Instance.ManaCost + E.Instance.ManaCost) E.Cast(unit.Position.Extend(Player.Position, -100), PacketCast());
            if (FlagPos != default(Vector3) && (FlagPos.Distance(unit.Position) <= 60 || (Q.WillHit(unit.Position, FlagPos, 110) && Player.Distance3D(unit) > 50))) Q.Cast(FlagPos, PacketCast());
        }

        private void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe) return;
            if (args.SData.Name == "JarvanIVCataclysm" && ItemBool("Combo", "R"))
            {
                RCasted = true;
                Utility.DelayAction.Add(3500, () => RCasted = false);
            }
            if (args.SData.Name == "JarvanIVDemacianStandard")
            {
                FlagPos = args.End;
                Utility.DelayAction.Add(8050, () => FlagPos = default(Vector3));
            }
        }

        private void NormalCombo(string Mode)
        {
            if (Mode == "Combo" && ItemBool(Mode, "R") && ItemList(Mode, "RMode") == 0 && R.IsReady() && RCasted && Player.CountEnemysInRange(325) == 0) R.Cast(PacketCast());
            if (!targetObj.IsValidTarget()) return;
            if (ItemBool(Mode, "E") && E.CanCast(targetObj)) E.Cast((Player.Distance3D(targetObj) > 450 && !targetObj.IsFacing(Player)) ? targetObj.Position.Extend(Player.Position, Player.Distance3D(targetObj) <= E.Range - 100 ? -100 : 0) : targetObj.Position, PacketCast());
            if ((!ItemBool(Mode, "E") || (ItemBool(Mode, "E") && !E.IsReady())) && ItemBool(Mode, "Q") && Q.IsReady())
            {
                if (ItemBool(Mode, "E") && FlagPos != default(Vector3))
                {
                    if ((FlagPos.Distance(targetObj.Position) <= 60 || (Q.WillHit(targetObj.Position, FlagPos, 110) && Player.Distance3D(targetObj) > 50)) && Q.InRange(FlagPos))
                    {
                        if (Mode == "Combo" || (Mode == "Harass" && Player.HealthPercentage() >= ItemSlider(Mode, "QAbove"))) Q.Cast(FlagPos, PacketCast());
                    }
                    else if (Q.InRange(targetObj)) Q.Cast(targetObj.Position, PacketCast());
                }
                else if ((!ItemBool(Mode, "E") || (ItemBool(Mode, "E") && FlagPos == default(Vector3))) && Q.InRange(targetObj)) Q.Cast(targetObj.Position, PacketCast());
            }
            if (Mode == "Combo" && ItemBool(Mode, "R") && R.IsReady())
            {
                if (!RCasted)
                {
                    switch (ItemList(Mode, "RMode"))
                    {
                        case 0:
                            if (R.InRange(targetObj) && CanKill(targetObj, R)) R.CastOnUnit(targetObj, PacketCast());
                            break;
                        case 1:
                            var UltiObj = ObjectManager.Get<Obj_AI_Hero>().FirstOrDefault(i => i.IsValidTarget(R.Range) && (i.CountEnemysInRange(325) >= ItemSlider(Mode, "RAbove") || (CanKill(i, R) && i.CountEnemysInRange(325) >= 1)));
                            if (UltiObj != null) R.CastOnUnit(UltiObj, PacketCast());
                            break;
                    }
                }
                else if (Player.CountEnemysInRange(325) == 0) R.Cast(PacketCast());
            }
            if (Mode == "Combo" && ItemBool(Mode, "W") && W.CanCast(targetObj) && Player.HealthPercentage() <= ItemSlider(Mode, "WUnder")) W.Cast(PacketCast());
            if (Mode == "Combo" && ItemBool(Mode, "Item")) UseItem(targetObj);
            if (Mode == "Combo" && ItemBool(Mode, "Ignite") && IgniteReady()) CastIgnite(targetObj);
        }

        private void LaneJungClear()
        {
            var minionObj = MinionManager.GetMinions(E.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth);
            foreach (var Obj in minionObj)
            {
                if (Obj.Team == GameObjectTeam.Neutral && CanSmiteMob(Obj.Name)) CastSmite(Obj);
                if (ItemBool("Clear", "E") && E.IsReady() && (minionObj.Count >= 2 || Obj.MaxHealth >= 1200)) E.Cast(GetClearPos(minionObj, E), PacketCast());
                if (ItemBool("Clear", "Q") && Q.IsReady())
                {
                    if (ItemBool("Clear", "E") && FlagPos != default(Vector3))
                    {
                        if ((minionObj.Count(i => FlagPos.Distance(i.Position) <= 60) >= 2 || minionObj.Where(i => Q.InRange(i)).Count(i => Q.WillHit(i.Position, FlagPos, 110)) >= 2) && Q.InRange(FlagPos))
                        {
                            Q.Cast(FlagPos, PacketCast());
                        }
                        else Q.Cast(GetClearPos(minionObj.Where(i => Q.InRange(i)).ToList(), Q), PacketCast());
                    }
                    else if (!ItemBool("Clear", "E") || (ItemBool("Clear", "E") && FlagPos == default(Vector3))) Q.Cast(GetClearPos(minionObj.Where(i => Q.InRange(i)).ToList(), Q), PacketCast());
                }
                if (ItemBool("Clear", "Item")) UseItem(Obj, true);
            }
        }

        private void LastHit()
        {
            if (!ItemBool("Misc", "QLastHit") || !Q.IsReady()) return;
            foreach (var Obj in MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.NotAlly).Where(i => CanKill(i, Q)).OrderByDescending(i => i.Distance3D(Player))) Q.Cast(Obj.Position, PacketCast());
        }

        private void Flee()
        {
            if (!Q.IsReady()) return;
            if (E.IsReady() && Player.Mana >= Q.Instance.ManaCost + E.Instance.ManaCost) E.Cast(Game.CursorPos, PacketCast());
            if (Player.LastCastedSpellName() == "JarvanIVDemacianStandard") Q.Cast(Game.CursorPos, PacketCast());
        }

        private void KillSteal()
        {
            if (!Q.IsReady()) return;
            foreach (var Obj in ObjectManager.Get<Obj_AI_Hero>().Where(i => i.IsValidTarget(Q.Range) && CanKill(i, Q) && i != targetObj).OrderBy(i => i.Health).OrderBy(i => i.Distance3D(Player))) Q.Cast(Obj.Position, PacketCast());
        }

        private void UseItem(Obj_AI_Base Target, bool IsFarm = false)
        {
            if (Tiamat.IsReady() && IsFarm ? Player.Distance3D(Target) <= Tiamat.Range : Player.CountEnemysInRange((int)Tiamat.Range) >= 1) Tiamat.Cast();
            if (Hydra.IsReady() && IsFarm ? Player.Distance3D(Target) <= Hydra.Range : (Player.CountEnemysInRange((int)Hydra.Range) >= 2 || (Player.GetAutoAttackDamage(Target, true) < Target.Health && Player.CountEnemysInRange((int)Hydra.Range) == 1))) Hydra.Cast();
            if (RanduinOmen.IsReady() && Player.CountEnemysInRange((int)RanduinOmen.Range) >= 1 && !IsFarm) RanduinOmen.Cast();
        }
    }
}