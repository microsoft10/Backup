using System;
using System.Linq;
using Color = System.Drawing.Color;

using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

using Orbwalk = MasterSeries.Common.M_Orbwalker;

namespace MasterSeries.Champions
{
    class Jax : Program
    {
        private bool WardCasted = false, ECasted = false;
        private int RCount = 0;
        private Vector3 WardPlacePos = default(Vector3);

        public Jax()
        {
            Q = new Spell(SpellSlot.Q, 700);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 375);
            R = new Spell(SpellSlot.R);
            Q.SetTargetted(0.5f, float.MaxValue);
            W.SetTargetted(0.2333f, float.MaxValue);
            E.SetSkillshot(0.3f, 375, 1450, false, SkillshotType.SkillshotCircle);

            var ChampMenu = new Menu("Plugin", Name + "Plugin");
            {
                var ComboMenu = new Menu("Combo", "Combo");
                {
                    ItemBool(ComboMenu, "Q", "Use Q");
                    ItemBool(ComboMenu, "W", "Use W");
                    ItemBool(ComboMenu, "E", "Use E");
                    ItemBool(ComboMenu, "R", "Use R");
                    ItemList(ComboMenu, "RMode", "-> Mode", new[] { "Player Hp", "# Enemy" });
                    ItemSlider(ComboMenu, "RUnder", "--> If Hp Under", 40);
                    ItemSlider(ComboMenu, "RCount", "--> If Enemy Above", 2, 1, 4);
                    ItemBool(ComboMenu, "Item", "Use Item");
                    ItemBool(ComboMenu, "Ignite", "Auto Ignite If Killable");
                    ChampMenu.AddSubMenu(ComboMenu);
                }
                var HarassMenu = new Menu("Harass", "Harass");
                {
                    ItemBool(HarassMenu, "Q", "Use Q");
                    ItemSlider(HarassMenu, "QAbove", "-> If Hp Above", 20);
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
                    ItemBool(ClearMenu, "W", "Use W");
                    ItemBool(ClearMenu, "E", "Use E");
                    ItemBool(ClearMenu, "Item", "Use Tiamat/Hydra");
                    ChampMenu.AddSubMenu(ClearMenu);
                }
                var MiscMenu = new Menu("Misc", "Misc");
                {
                    ItemBool(MiscMenu, "WJPink", "Ward Jump Use Pink Ward", false);
                    ItemBool(MiscMenu, "WLastHit", "Use W To Last Hit");
                    ItemBool(MiscMenu, "QKillSteal", "Use Q To Kill Steal");
                    ItemBool(MiscMenu, "EAntiGap", "Use E To Anti Gap Closer");
                    ItemBool(MiscMenu, "EInterrupt", "Use E To Interrupt");
                    ItemSlider(MiscMenu, "CustomSkin", "Skin Changer", 8, 0, 8).ValueChanged += SkinChanger;
                    ChampMenu.AddSubMenu(MiscMenu);
                }
                var DrawMenu = new Menu("Draw", "Draw");
                {
                    ItemBool(DrawMenu, "Q", "Q Range", false);
                    ItemBool(DrawMenu, "E", "E Range", false);
                    ChampMenu.AddSubMenu(DrawMenu);
                }
                Config.AddSubMenu(ChampMenu);
            }
            Game.OnGameUpdate += OnGameUpdate;
            Drawing.OnDraw += OnDraw;
            AntiGapcloser.OnEnemyGapcloser += OnEnemyGapcloser;
            Interrupter.OnPossibleToInterrupt += OnPossibleToInterrupt;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            Orbwalk.BeforeAttack += BeforeAttack;
            Orbwalk.AfterAttack += AfterAttack;
        }

        private void OnGameUpdate(EventArgs args)
        {
            if (Player.IsDead || MenuGUI.IsChatOpen || Player.IsChannelingImportantSpell() || Player.IsRecalling())
            {
                if (Player.IsDead) RCount = 0;
                return;
            }
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
            else if (Orbwalk.CurrentMode == Orbwalk.Mode.Flee) WardJump(Game.CursorPos);
            if (ItemBool("Misc", "QKillSteal")) KillSteal();
        }

        private void OnDraw(EventArgs args)
        {
            if (Player.IsDead) return;
            if (ItemBool("Draw", "Q") && Q.Level > 0) Render.Circle.DrawCircle(Player.Position, Q.Range, Q.IsReady() ? Color.Green : Color.Red, 7);
            if (ItemBool("Draw", "E") && E.Level > 0) Render.Circle.DrawCircle(Player.Position, E.Range, E.IsReady() ? Color.Green : Color.Red, 7);
        }

        private void OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (!ItemBool("Misc", "EAntiGap") || Player.IsDead || !E.CanCast(gapcloser.Sender)) return;
            E.Cast(PacketCast());
        }

        private void OnPossibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            if (!ItemBool("Misc", "EInterrupt") || Player.IsDead || !E.IsReady()) return;
            if (!E.InRange(unit) && Q.CanCast(unit) && Player.Mana >= Q.Instance.ManaCost + E.Instance.ManaCost) Q.CastOnUnit(unit, PacketCast());
            if (E.InRange(unit)) E.Cast(PacketCast());
        }

        private void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe) return;
            if (args.SData.Name == "JaxCounterStrike")
            {
                ECasted = true;
                Utility.DelayAction.Add(1800, () => ECasted = false);
            }
            if (args.SData.Name == "jaxrelentlessattack") RCount = 0;
        }

        private void BeforeAttack(Orbwalk.BeforeAttackEventArgs Args)
        {
            if (Args.Target.IsValidTarget(Orbwalk.GetAutoAttackRange(Player, Args.Target) + 20) && ItemBool("Misc", "WLastHit") && W.IsReady() && Orbwalk.CurrentMode == Orbwalk.Mode.LastHit && CanKill((Obj_AI_Minion)Args.Target, W, GetBonusDmg((Obj_AI_Minion)Args.Target)) && Args.Target is Obj_AI_Minion)
            {
                Args.Process = false;
                W.Cast(PacketCast());
            }
        }

        private void AfterAttack(AttackableUnit Target)
        {
            if (R.Level > 0) RCount += 1;
            if (W.IsReady() && Target.IsValidTarget(Orbwalk.GetAutoAttackRange(Player, Target) + 20) && Target is Obj_AI_Base)
            {
                switch (Orbwalk.CurrentMode)
                {
                    case Orbwalk.Mode.Combo:
                        if (ItemBool("Combo", "W")) W.Cast(PacketCast());
                        break;
                    case Orbwalk.Mode.Harass:
                        if (ItemBool("Harass", "W") && (!ItemBool("Harass", "Q") || (ItemBool("Harass", "Q") && !Q.IsReady()))) W.Cast(PacketCast());
                        break;
                    case Orbwalk.Mode.LaneClear:
                        if (ItemBool("Clear", "W")) W.Cast(PacketCast());
                        break;
                }
            }
            if (!W.IsReady() && !Player.HasBuff("JaxEmpowerTwo") && Orbwalk.CurrentMode == Orbwalk.Mode.Combo && ItemBool("Combo", "W") && ItemBool("Combo", "Item") && Target is Obj_AI_Hero) UseItem((Obj_AI_Hero)Target, true);
        }

        private void NormalCombo(string Mode)
        {
            if (!targetObj.IsValidTarget()) return;
            if (ItemBool(Mode, "E") && E.IsReady())
            {
                if (!Player.HasBuff("JaxEvasion"))
                {
                    if ((ItemBool(Mode, "Q") && Q.CanCast(targetObj)) || E.InRange(targetObj)) E.Cast(PacketCast());
                }
                else if (E.InRange(targetObj) && Player.Distance3D(targetObj) >= E.Range - 20) E.Cast(PacketCast());
            }
            if (ItemBool(Mode, "W") && W.IsReady() && ItemBool(Mode, "Q") && Q.CanCast(targetObj) && CanKill(targetObj, Q, Q.GetDamage(targetObj) + GetBonusDmg(targetObj))) W.Cast(PacketCast());
            if (ItemBool(Mode, "Q") && Q.CanCast(targetObj) && (CanKill(targetObj, Q) || (Player.HasBuff("JaxEmpowerTwo") && CanKill(targetObj, Q, Q.GetDamage(targetObj) + GetBonusDmg(targetObj))) || ((Mode == "Combo" || (Mode == "Harass" && Player.HealthPercentage() >= ItemList(Mode, "QAbove"))) && ((ItemBool(Mode, "E") && E.IsReady() && Player.HasBuff("JaxEvasion") && !E.InRange(targetObj)) || Player.Distance3D(targetObj) > Orbwalk.GetAutoAttackRange(Player, targetObj) + 40)))) Q.CastOnUnit(targetObj, PacketCast());
            if (Mode == "Combo" && ItemBool(Mode, "R") && R.IsReady())
            {
                switch (ItemList(Mode, "RMode"))
                {
                    case 0:
                        if (Player.HealthPercentage() <= ItemSlider(Mode, "RUnder") && Player.CountEnemysInRange((int)Q.Range + 200) >= 1) R.Cast(PacketCast());
                        break;
                    case 1:
                        if (Player.CountEnemysInRange((int)Q.Range + 200) >= ItemSlider(Mode, "RCount")) R.Cast(PacketCast());
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
                if (Obj.Team == GameObjectTeam.Neutral && CanSmiteMob(Obj.Name)) CastSmite(Obj);
                if (ItemBool("Clear", "E") && E.IsReady() && (Obj.MaxHealth >= 1200 || minionObj.Count(i => i.Distance3D(Obj) <= E.Range) >= 2))
                {
                    if (!Player.HasBuff("JaxEvasion"))
                    {
                        if ((ItemBool("Clear", "Q") && Q.IsReady()) || E.InRange(Obj)) E.Cast(PacketCast());
                    }
                    else if (E.InRange(Obj) && !ECasted) E.Cast(PacketCast());
                }
                if (ItemBool("Clear", "W") && W.IsReady() && ItemBool("Clear", "Q") && Q.IsReady() && CanKill(Obj, Q, Q.GetDamage(Obj) + GetBonusDmg(Obj))) W.Cast(PacketCast());
                if (ItemBool("Clear", "Q") && Q.IsReady() && (CanKill(Obj, Q) || (Player.HasBuff("JaxEmpowerTwo") && CanKill(Obj, Q, Q.GetDamage(Obj) + GetBonusDmg(Obj))) || (ItemBool("Clear", "E") && E.IsReady() && Player.HasBuff("JaxEvasion") && !E.InRange(Obj)) || Player.Distance3D(Obj) > Orbwalk.GetAutoAttackRange(Player, targetObj) + 40)) Q.CastOnUnit(Obj, PacketCast());
                if (ItemBool("Clear", "Item")) UseItem(Obj, true);
            }
        }

        private void LastHit()
        {
            if (!ItemBool("Misc", "WLastHit") || !W.IsReady() || !Player.HasBuff("JaxEmpowerTwo")) return;
            foreach (var Obj in MinionManager.GetMinions(Orbwalk.GetAutoAttackRange() + 100, MinionTypes.All, MinionTeam.NotAlly).Where(i => CanKill(i, W, GetBonusDmg(i))).OrderByDescending(i => i.Distance3D(Player)))
            {
                Orbwalk.SetAttack(false);
                Player.IssueOrder(GameObjectOrder.AttackUnit, Obj);
                Orbwalk.SetAttack(true);
                break;
            }
        }

        private bool WardJump(Vector3 Pos)
        {
            if (!Q.IsReady()) return false;
            bool Casted = false;
            var JumpPos = Pos;
            if (GetWardSlot() != null && !WardCasted && Player.Distance(Pos) > GetWardRange()) JumpPos = Player.Position.Extend(Pos, GetWardRange());
            foreach (var Obj in ObjectManager.Get<Obj_AI_Base>().Where(i => i.IsValidTarget(Q.Range + i.BoundingRadius, false) && !i.IsMe && !(i is Obj_AI_Turret) && i.Distance(WardCasted ? WardPlacePos : JumpPos) < 200).OrderBy(i => i.Distance(WardCasted ? WardPlacePos : JumpPos)))
            {
                Q.CastOnUnit(Obj, PacketCast());
                Casted = true;
                return true;
            }
            if (!Casted && GetWardSlot() != null && !WardCasted)
            {
                Player.Spellbook.CastSpell(GetWardSlot().SpellSlot, JumpPos);
                WardPlacePos = JumpPos;
                Utility.DelayAction.Add(800, () => WardPlacePos = default(Vector3));
                WardCasted = true;
                Utility.DelayAction.Add(800, () => WardCasted = false);
            }
            return false;
        }

        private void KillSteal()
        {
            if (!Q.IsReady()) return;
            foreach (var Obj in ObjectManager.Get<Obj_AI_Hero>().Where(i => i.IsValidTarget(Q.Range) && CanKill(i, Q, Q.GetDamage(i) + (((W.IsReady() || Player.HasBuff("JaxEmpowerTwo")) && !CanKill(i, Q)) ? GetBonusDmg(i) : 0)) && i != targetObj).OrderBy(i => i.Health).OrderBy(i => i.Distance3D(Player)))
            {
                if (W.IsReady() && !CanKill(Obj, Q)) W.Cast(PacketCast());
                if (Q.IsReady() && ((!CanKill(Obj, Q) && Player.HasBuff("JaxEmpowerTwo")) || CanKill(Obj, Q))) Q.CastOnUnit(Obj, PacketCast());
            }
        }

        private void UseItem(Obj_AI_Base Target, bool IsFarm = false)
        {
            if (Bilgewater.IsReady() && !IsFarm) Bilgewater.Cast(Target);
            if (HexGun.IsReady() && !IsFarm) HexGun.Cast(Target);
            if (BladeRuined.IsReady() && !IsFarm) BladeRuined.Cast(Target);
            if (Tiamat.IsReady() && IsFarm ? Player.Distance3D(Target) <= Tiamat.Range : Player.CountEnemysInRange((int)Tiamat.Range) >= 1) Tiamat.Cast();
            if (Hydra.IsReady() && IsFarm ? Player.Distance3D(Target) <= Hydra.Range : (Player.CountEnemysInRange((int)Hydra.Range) >= 2 || (Player.GetAutoAttackDamage(Target, true) < Target.Health && Player.CountEnemysInRange((int)Hydra.Range) == 1))) Hydra.Cast();
            if (RanduinOmen.IsReady() && Player.CountEnemysInRange((int)RanduinOmen.Range) >= 1 && !IsFarm) RanduinOmen.Cast();
        }

        private double GetBonusDmg(Obj_AI_Base Target)
        {
            double DmgItem = 0;
            if (Sheen.IsOwned() && ((Sheen.IsReady() && W.IsReady()) || Player.HasBuff("Sheen")) && Player.BaseAttackDamage > DmgItem) DmgItem = Player.BaseAttackDamage;
            if (Trinity.IsOwned() && ((Trinity.IsReady() && W.IsReady()) || Player.HasBuff("Sheen")) && Player.BaseAttackDamage * 2 > DmgItem) DmgItem = Player.BaseAttackDamage * 2;
            return ((W.IsReady() || Player.HasBuff("JaxEmpowerTwo")) ? W.GetDamage(Target) : 0) + (RCount >= 2 ? R.GetDamage(Target) : 0) + Player.GetAutoAttackDamage(Target, true) + Player.CalcDamage(Target, Damage.DamageType.Physical, DmgItem);
        }
    }
}