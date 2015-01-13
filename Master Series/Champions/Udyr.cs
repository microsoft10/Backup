using System;
using System.Linq;

using LeagueSharp;
using LeagueSharp.Common;

using Orbwalk = MasterSeries.Common.M_Orbwalker;

namespace MasterSeries.Champions
{
    class Udyr : Program
    {
        private enum Stance
        {
            Tiger,
            Turtle,
            Bear,
            Phoenix
        }
        private Stance CurStance;
        private int AACount = 0;
        private bool PhoenixActive = false;

        public Udyr()
        {
            Q = new Spell(SpellSlot.Q);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E);
            R = new Spell(SpellSlot.R);

            Config.SubMenu("OW").SubMenu("Mode").AddItem(new MenuItem("OWStunCycle", "Stun Cycle", true).SetValue(new KeyBind("Z".ToCharArray()[0], KeyBindType.Press)));
            var ChampMenu = new Menu("Plugin", Name + "Plugin");
            {
                var ComboMenu = new Menu("Combo", "Combo");
                {
                    ItemBool(ComboMenu, "Q", "Use Q");
                    ItemBool(ComboMenu, "W", "Use W");
                    ItemBool(ComboMenu, "E", "Use E");
                    ItemBool(ComboMenu, "R", "Use R");
                    ItemBool(ComboMenu, "Item", "Use Item");
                    ItemBool(ComboMenu, "Ignite", "Auto Ignite If Killable");
                    ChampMenu.AddSubMenu(ComboMenu);
                }
                var HarassMenu = new Menu("Harass", "Harass");
                {
                    ItemBool(HarassMenu, "Q", "Use Q");
                    ItemBool(HarassMenu, "W", "Use W");
                    ItemBool(HarassMenu, "E", "Use E");
                    ItemBool(HarassMenu, "R", "Use R");
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
                    ItemBool(ClearMenu, "R", "Use R");
                    ItemBool(ClearMenu, "Item", "Use Tiamat/Hydra");
                    ChampMenu.AddSubMenu(ClearMenu);
                }
                var MiscMenu = new Menu("Misc", "Misc");
                {
                    ItemBool(MiscMenu, "EAntiGap", "Use E To Anti Gap Closer");
                    ItemBool(MiscMenu, "EInterrupt", "Use E To Interrupt");
                    ItemBool(MiscMenu, "WSurvive", "Try Use W To Survive");
                    ItemSlider(MiscMenu, "CustomSkin", "Skin Changer", 3, 0, 3).ValueChanged += SkinChanger;
                    ChampMenu.AddSubMenu(MiscMenu);
                }
                Config.AddSubMenu(ChampMenu);
            }
            Game.OnGameUpdate += OnGameUpdate;
            AntiGapcloser.OnEnemyGapcloser += OnEnemyGapcloser;
            Interrupter.OnPossibleToInterrupt += OnPossibleToInterrupt;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            Obj_AI_Base.OnProcessSpellCast += TrySurviveSpellCast;
            Orbwalk.AfterAttack += AfterAttack;
        }

        private void OnGameUpdate(EventArgs args)
        {
            if (Player.IsDead || MenuGUI.IsChatOpen || Player.IsChannelingImportantSpell() || Player.IsRecalling())
            {
                if (Player.IsDead) AACount = 0;
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
            else if (Orbwalk.CurrentMode == Orbwalk.Mode.Flee) Flee();
            if (ItemActive("StunCycle")) StunCycle();
            if (ItemBool("Misc", "WSurvive") && W.IsReady()) TrySurvive(W.Slot);
        }

        private void OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (!ItemBool("Misc", "EAntiGap") || Player.IsDead || CurStance != Stance.Bear || (!E.IsReady() && CurStance != Stance.Bear)) return;
            if (Player.Distance3D(gapcloser.Sender) <= Orbwalk.GetAutoAttackRange(Player, gapcloser.Sender) + 50 && !gapcloser.Sender.HasBuff("UdyrBearStunCheck"))
            {
                if (CurStance != Stance.Bear && E.IsReady()) E.Cast(PacketCast());
                if (CurStance == Stance.Bear) Player.IssueOrder(GameObjectOrder.AttackUnit, gapcloser.Sender);
            }
        }

        private void OnPossibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            if (!ItemBool("Misc", "EInterrupt") || Player.IsDead || CurStance != Stance.Bear || (!E.IsReady() && CurStance != Stance.Bear)) return;
            if (Player.Distance3D(unit) <= 600 && !unit.HasBuff("UdyrBearStunCheck"))
            {
                if (CurStance != Stance.Bear && E.IsReady()) E.Cast(PacketCast());
                if (CurStance == Stance.Bear) Player.IssueOrder(GameObjectOrder.AttackUnit, unit);
            }
        }

        private void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe) return;
            if (args.SData.Name == "UdyrTigerStance")
            {
                CurStance = Stance.Tiger;
                AACount = 0;
            }
            if (args.SData.Name == "UdyrTurtleStance")
            {
                CurStance = Stance.Turtle;
                AACount = 0;
            }
            if (args.SData.Name == "UdyrBearStance")
            {
                CurStance = Stance.Bear;
                AACount = 0;
            }
            if (args.SData.Name == "UdyrPhoenixStance")
            {
                CurStance = Stance.Phoenix;
                AACount = 0;
            }
        }

        private void AfterAttack(AttackableUnit Target)
        {
            if (CurStance == Stance.Tiger || CurStance == Stance.Phoenix) AACount += 1;
            if (CurStance == Stance.Phoenix && Player.Buffs.First(i => i.DisplayName == "UdyrPhoenixStance").Count == 1)
            {
                PhoenixActive = true;
                Utility.DelayAction.Add(50, () => PhoenixActive = false);
            }
        }

        private void NormalCombo(string Mode)
        {
            if (!targetObj.IsValidTarget()) return;
            if (ItemBool(Mode, "E") && E.IsReady() && !targetObj.HasBuff("UdyrBearStunCheck") && Player.Distance3D(targetObj) <= ((Mode == "Combo") ? 800 : Orbwalk.GetAutoAttackRange(Player, targetObj) + 80)) E.Cast(PacketCast());
            if (Player.Distance3D(targetObj) <= Orbwalk.GetAutoAttackRange(Player, targetObj) + 50 && (!ItemBool(Mode, "E") || (ItemBool(Mode, "E") && (E.Level == 0 || targetObj.HasBuff("UdyrBearStunCheck")))))
            {
                if (ItemBool(Mode, "Q") && Q.IsReady()) Q.Cast(PacketCast());
                if (ItemBool(Mode, "R") && R.IsReady() && (!ItemBool(Mode, "Q") || (ItemBool(Mode, "Q") && (Q.Level == 0 || (CurStance == Stance.Tiger && AACount >= 2))))) R.Cast(PacketCast());
                if (ItemBool(Mode, "W") && W.IsReady())
                {
                    if ((CurStance == Stance.Tiger && AACount >= 2) || (CurStance == Stance.Phoenix && (AACount > 3 || PhoenixActive)))
                    {
                        W.Cast(PacketCast());
                    }
                    else if (Q.Level == 0 && R.Level == 0) W.Cast(PacketCast());
                }
            }
            if (Mode == "Combo" && ItemBool(Mode, "Item")) UseItem(targetObj);
            if (Mode == "Combo" && ItemBool(Mode, "Ignite") && IgniteReady()) CastIgnite(targetObj);
        }

        private void LaneJungClear()
        {
            foreach (var Obj in MinionManager.GetMinions(800, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth))
            {
                if (Obj.Team == GameObjectTeam.Neutral && CanSmiteMob(Obj.Name)) CastSmite(Obj);
                if (ItemBool("Clear", "E") && E.IsReady() && !Obj.HasBuff("UdyrBearStunCheck") && !Obj.Name.StartsWith("SRU_Baron") && !Obj.Name.StartsWith("SRU_Dragon")) E.Cast(PacketCast());
                if (Player.Distance3D(Obj) <= Orbwalk.GetAutoAttackRange(Player, Obj) + 50 && (!ItemBool("Clear", "E") || (ItemBool("Clear", "E") && (E.Level == 0 || Obj.HasBuff("UdyrBearStunCheck") || Obj.Name.StartsWith("SRU_Baron") || Obj.Name.StartsWith("SRU_Dragon")))))
                {
                    if (ItemBool("Clear", "Q") && Q.IsReady()) Q.Cast(PacketCast());
                    if (ItemBool("Clear", "R") && R.IsReady() && (!ItemBool("Clear", "Q") || (ItemBool("Clear", "Q") && (Q.Level == 0 || (CurStance == Stance.Tiger && AACount >= 2))))) R.Cast(PacketCast());
                    if (ItemBool("Clear", "W") && W.IsReady())
                    {
                        if ((CurStance == Stance.Tiger && AACount >= 2) || (CurStance == Stance.Phoenix && (AACount > 3 || PhoenixActive)))
                        {
                            W.Cast(PacketCast());
                        }
                        else if (Q.Level == 0 && R.Level == 0) W.Cast(PacketCast());
                    }
                }
                if (ItemBool("Clear", "Item")) UseItem(Obj, true);
            }
        }

        private void Flee()
        {
            var Passive = Player.Buffs.FirstOrDefault(i => i.DisplayName == "UdyrMonkeyAgilityBuff");
            if (E.IsReady()) E.Cast(PacketCast());
            if (Passive != null && Passive.Count < 3)
            {
                if (Q.IsReady() && (Q.Level > W.Level || Q.Level > R.Level || (Q.Level == W.Level && Q.Level > R.Level) || (Q.Level == R.Level && Q.Level > W.Level) || (Q.Level == W.Level && Q.Level == R.Level)))
                {
                    Q.Cast(PacketCast());
                }
                else if (W.IsReady() && (W.Level > Q.Level || W.Level > R.Level || (W.Level == Q.Level && W.Level > R.Level) || (W.Level == R.Level && W.Level > Q.Level) || (W.Level == Q.Level && W.Level == R.Level)))
                {
                    W.Cast(PacketCast());
                }
                else if (R.IsReady() && (R.Level > Q.Level || R.Level > W.Level || (R.Level == Q.Level && R.Level > W.Level) || (R.Level == W.Level && R.Level > Q.Level) || (R.Level == Q.Level && R.Level == W.Level))) R.Cast(PacketCast());
            }
        }

        private void StunCycle()
        {
            var Obj = ObjectManager.Get<Obj_AI_Hero>().Where(i => i.IsValidTarget(800) && !i.HasBuff("UdyrBearStunCheck")).OrderBy(i => i.Distance3D(Player)).FirstOrDefault();
            CustomOrbwalk(Obj);
            if (Obj != null && E.IsReady()) E.Cast(PacketCast());
        }

        private void UseItem(Obj_AI_Base Target, bool IsFarm = false)
        {
            if (Bilgewater.IsReady() && !IsFarm) Bilgewater.Cast(Target);
            if (BladeRuined.IsReady() && !IsFarm) BladeRuined.Cast(Target);
            if (Tiamat.IsReady() && IsFarm ? Player.Distance3D(Target) <= Tiamat.Range : Player.CountEnemysInRange((int)Tiamat.Range) >= 1) Tiamat.Cast();
            if (Hydra.IsReady() && IsFarm ? Player.Distance3D(Target) <= Hydra.Range : (Player.CountEnemysInRange((int)Hydra.Range) >= 2 || (Player.GetAutoAttackDamage(Target, true) < Target.Health && Player.CountEnemysInRange((int)Hydra.Range) == 1))) Hydra.Cast();
            if (RanduinOmen.IsReady() && Player.CountEnemysInRange((int)RanduinOmen.Range) >= 1 && !IsFarm) RanduinOmen.Cast();
        }
    }
}