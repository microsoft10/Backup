using System;
using System.Linq;
using Color = System.Drawing.Color;

using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

using Orbwalk = MasterSeries.Common.M_Orbwalker;

namespace MasterSeries.Champions
{
    class Ryze : Program
    {
        public Ryze()
        {
            Q = new Spell(SpellSlot.Q, 632.5f);
            W = new Spell(SpellSlot.W, 607.5f);
            E = new Spell(SpellSlot.E, 607.5f);
            R = new Spell(SpellSlot.R);
            Q.SetTargetted(0, 1400);
            W.SetTargetted(0, 500);
            E.SetTargetted(0, 1000);

            Config.SubMenu("OW").SubMenu("Mode").AddItem(new MenuItem("OWChase", "Chase", true).SetValue(new KeyBind("Z".ToCharArray()[0], KeyBindType.Press)));
            var ChampMenu = new Menu("Plugin", Name + "Plugin");
            {
                var ComboMenu = new Menu("Combo", "Combo");
                {
                    ItemBool(ComboMenu, "Q", "Use Q");
                    ItemSlider(ComboMenu, "QDelay", "-> Stop All If Q Will Ready In (ms)", 500, 300, 700);
                    ItemBool(ComboMenu, "W", "Use W");
                    ItemBool(ComboMenu, "E", "Use E");
                    ItemBool(ComboMenu, "R", "Use R");
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
                    ItemBool(ClearMenu, "Q", "Use Q");
                    ItemBool(ClearMenu, "W", "Use W");
                    ItemBool(ClearMenu, "E", "Use E");
                    ChampMenu.AddSubMenu(ClearMenu);
                }
                var MiscMenu = new Menu("Misc", "Misc");
                {
                    ItemBool(MiscMenu, "QLastHit", "Use Q To Last Hit");
                    ItemBool(MiscMenu, "Exploit", "-> Tear Exploit");
                    ItemBool(MiscMenu, "QKillSteal", "Use Q To Kill Steal");
                    ItemBool(MiscMenu, "WAntiGap", "Use W To Anti Gap Closer");
                    ItemBool(MiscMenu, "WInterrupt", "Use W To Interrupt");
                    ItemBool(MiscMenu, "SeraphSurvive", "Try Use Seraph's Embrace To Survive");
                    ItemSlider(MiscMenu, "CustomSkin", "Skin Changer", 7, 0, 8).ValueChanged += SkinChanger;
                    ChampMenu.AddSubMenu(MiscMenu);
                }
                var DrawMenu = new Menu("Draw", "Draw");
                {
                    ItemBool(DrawMenu, "Q", "Q Range", false);
                    ItemBool(DrawMenu, "W", "W Range", false);
                    ItemBool(DrawMenu, "E", "E Range", false);
                    ChampMenu.AddSubMenu(DrawMenu);
                }
                Config.AddSubMenu(ChampMenu);
            }
            Game.OnGameUpdate += OnGameUpdate;
            Drawing.OnDraw += OnDraw;
            AntiGapcloser.OnEnemyGapcloser += OnEnemyGapcloser;
            Interrupter.OnPossibleToInterrupt += OnPossibleToInterrupt;
            Obj_AI_Hero.OnProcessSpellCast += OnProcessSpellCast;
            Obj_AI_Base.OnProcessSpellCast += TrySurviveSpellCast;
            Orbwalk.BeforeAttack += BeforeAttack;
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
            else if (ItemActive("Chase")) NormalCombo("Chase");
            if (ItemBool("Misc", "QKillSteal")) KillSteal();
            if (ItemBool("Misc", "SeraphSurvive") && Items.CanUseItem(ItemData.Seraphs_Embrace.Id)) TrySurvive(ItemData.Seraphs_Embrace.Id);
        }

        private void OnDraw(EventArgs args)
        {
            if (Player.IsDead) return;
            if (ItemBool("Draw", "Q") && Q.Level > 0) Render.Circle.DrawCircle(Player.Position, Q.Range, Q.IsReady() ? Color.Green : Color.Red, 7);
            if (ItemBool("Draw", "W") && W.Level > 0) Render.Circle.DrawCircle(Player.Position, W.Range, W.IsReady() ? Color.Green : Color.Red, 7);
            if (ItemBool("Draw", "E") && E.Level > 0) Render.Circle.DrawCircle(Player.Position, E.Range, E.IsReady() ? Color.Green : Color.Red, 7);
        }

        private void OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (!ItemBool("Misc", "WAntiGap") || Player.IsDead || !W.CanCast(gapcloser.Sender)) return;
            if (Player.Distance3D(gapcloser.Sender) <= W.Range - 200) W.CastOnUnit(gapcloser.Sender, PacketCast());
        }

        private void OnPossibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            if (!ItemBool("Misc", "WInterrupt") || Player.IsDead || !W.CanCast(unit)) return;
            W.CastOnUnit(unit, PacketCast());
        }

        private void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe) return;
            if (ItemBool("Misc", "Exploit") && W.IsReady() && args.SData.Name == "Overload" && Orbwalk.CurrentMode == Orbwalk.Mode.LastHit && ItemBool("Misc", "QLastHit") && args.Target is Obj_AI_Minion) Utility.DelayAction.Add((int)((Player.Distance3D((Obj_AI_Minion)args.Target) - 50) / Q.Speed * 1000 + Q.Delay), () => W.CastOnUnit((Obj_AI_Minion)args.Target, PacketCast()));
        }

        private void BeforeAttack(Orbwalk.BeforeAttackEventArgs Args)
        {
            var Target = (Obj_AI_Base)Args.Target;
            if (Orbwalk.CurrentMode == Orbwalk.Mode.Combo || Orbwalk.CurrentMode == Orbwalk.Mode.Harass)
            {
                if ((ItemBool(Orbwalk.CurrentMode.ToString(), "Q") && Q.CanCast(Target)) || (ItemBool(Orbwalk.CurrentMode.ToString(), "W") && W.CanCast(Target)) || (ItemBool(Orbwalk.CurrentMode.ToString(), "E") && E.CanCast(Target))) Args.Process = false;
            }
            else if (Orbwalk.CurrentMode == Orbwalk.Mode.LaneClear)
            {
                if ((ItemBool("Clear", "Q") && Q.CanCast(Target)) || (ItemBool("Clear", "W") && W.CanCast(Target)) || (ItemBool("Clear", "E") && E.CanCast(Target))) Args.Process = false;
            }
            else if (Orbwalk.CurrentMode == Orbwalk.Mode.LastHit && ItemBool("Misc", "QLastHit") && Q.CanCast(Target) && CanKill(Target, Q)) Args.Process = false;
        }

        private void NormalCombo(string Mode)
        {
            if (Mode == "Chase") CustomOrbwalk(targetObj);
            if (!targetObj.IsValidTarget()) return;
            if ((Mode == "Chase" || (Mode != "Chase" && ItemBool(Mode, "Q"))) && Q.CanCast(targetObj) && CanKill(targetObj, Q)) Q.CastOnUnit(targetObj, PacketCast());
            if ((Mode == "Chase" || (Mode != "Chase" && ItemBool(Mode, "E"))) && E.CanCast(targetObj) && CanKill(targetObj, E)) E.CastOnUnit(targetObj, PacketCast());
            if ((Mode == "Chase" || (Mode != "Chase" && ItemBool(Mode, "W"))) && W.CanCast(targetObj) && (CanKill(targetObj, W) || (Player.Distance3D(targetObj) > W.Range - 20 && !targetObj.IsFacing(Player)))) W.CastOnUnit(targetObj, PacketCast());
            switch (Mode)
            {
                case "Harass":
                    if (ItemBool(Mode, "Q") && Q.CanCast(targetObj)) Q.CastOnUnit(targetObj, PacketCast());
                    if (ItemBool(Mode, "W") && W.CanCast(targetObj)) W.CastOnUnit(targetObj, PacketCast());
                    if (ItemBool(Mode, "E") && E.CanCast(targetObj)) E.CastOnUnit(targetObj, PacketCast());
                    break;
                case "Combo":
                    if (ItemBool(Mode, "Ignite") && IgniteReady()) CastIgnite(targetObj);
                    if (ItemBool(Mode, "Q") && Q.CanCast(targetObj)) Q.CastOnUnit(targetObj, PacketCast());
                    if (!ItemBool(Mode, "Q") || (ItemBool(Mode, "Q") && !Q.IsReady()))
                    {
                        if (ItemBool(Mode, "Q") && Q.IsReady(ItemSlider(Mode, "QDelay")) && Math.Abs(Player.PercentCooldownMod) >= 0.2) return;
                        if (ItemBool(Mode, "R") && R.IsReady() && (Math.Abs(Player.PercentCooldownMod) < 0.2 || (Math.Abs(Player.PercentCooldownMod) >= 0.2 && Player.LastCastedSpellName() == "Overload"))) R.Cast(PacketCast());
                        if ((!ItemBool(Mode, "R") || (ItemBool(Mode, "R") && !R.IsReady())) && ItemBool(Mode, "W") && W.CanCast(targetObj) && (Math.Abs(Player.PercentCooldownMod) < 0.2 || (Math.Abs(Player.PercentCooldownMod) >= 0.2 && (Player.LastCastedSpellName() == "Overload" || (ItemBool(Mode, "R") && !R.IsReady() && Player.LastCastedSpellName() == "DesperatePower" && Player.HasBuff("DesperatePower")))))) W.CastOnUnit(targetObj, PacketCast());
                        if ((!ItemBool(Mode, "R") || (ItemBool(Mode, "R") && !R.IsReady())) && (!ItemBool(Mode, "W") || (ItemBool(Mode, "W") && !W.IsReady())) && ItemBool(Mode, "E") && E.CanCast(targetObj) && (Math.Abs(Player.PercentCooldownMod) < 0.2 || (Math.Abs(Player.PercentCooldownMod) >= 0.2 && Player.LastCastedSpellName() == "Overload"))) E.CastOnUnit(targetObj, PacketCast());
                    }
                    break;
                case "Chase":
                    if (W.CanCast(targetObj)) W.CastOnUnit(targetObj, PacketCast());
                    if (!W.IsReady() || targetObj.HasBuff("Rune Prison"))
                    {
                        if (Q.CanCast(targetObj)) Q.CastOnUnit(targetObj, PacketCast());
                        if (R.IsReady() && (Math.Abs(Player.PercentCooldownMod) < 0.2 || (Math.Abs(Player.PercentCooldownMod) >= 0.2 && Player.LastCastedSpellName() == "Overload"))) R.Cast(PacketCast());
                        if (!R.IsReady() && E.CanCast(targetObj) && (Math.Abs(Player.PercentCooldownMod) < 0.2 || (Math.Abs(Player.PercentCooldownMod) >= 0.2 && (Player.LastCastedSpellName() == "Overload" || (!R.IsReady() && Player.LastCastedSpellName() == "DesperatePower" && Player.HasBuff("DesperatePower")))))) E.CastOnUnit(targetObj, PacketCast());
                    }
                    break;
            }
        }

        private void LaneJungClear()
        {
            foreach (var Obj in MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth))
            {
                if (ItemBool("Clear", "Q") && Q.IsReady() && (CanKill(Obj, Q) || Obj.MaxHealth >= 1200 || !CanKill(Obj, Q, Q.GetDamage(Obj) * 2))) Q.CastOnUnit(Obj, PacketCast());
                if (ItemBool("Clear", "W") && W.CanCast(Obj) && (CanKill(Obj, W) || Obj.MaxHealth >= 1200 || !CanKill(Obj, W, W.GetDamage(Obj) * 2))) W.CastOnUnit(Obj, PacketCast());
                if (ItemBool("Clear", "E") && E.CanCast(Obj) && (CanKill(Obj, E) || Obj.MaxHealth >= 1200 || !CanKill(Obj, E, E.GetDamage(Obj) * 2))) E.CastOnUnit(Obj, PacketCast());
            }
        }

        private void LastHit()
        {
            if (!ItemBool("Misc", "QLastHit") || !Q.IsReady()) return;
            foreach (var Obj in MinionManager.GetMinions((ItemBool("Misc", "Exploit") && W.IsReady()) ? W.Range : Q.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth).Where(i => CanKill(i, Q)).OrderByDescending(i => i.Distance3D(Player))) Q.CastOnUnit(Obj, PacketCast());
        }

        private void KillSteal()
        {
            if (!Q.IsReady()) return;
            foreach (var Obj in ObjectManager.Get<Obj_AI_Hero>().Where(i => i.IsValidTarget(Q.Range) && CanKill(i, Q) && i != targetObj).OrderBy(i => i.Health).OrderBy(i => i.Distance3D(Player))) Q.CastOnUnit(Obj, PacketCast());
        }
    }
}