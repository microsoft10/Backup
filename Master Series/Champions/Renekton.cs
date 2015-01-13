using System;
using System.Linq;
using Color = System.Drawing.Color;

using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

using Orbwalk = MasterSeries.Common.M_Orbwalker;

namespace MasterSeries.Champions
{
    class Renekton : Program
    {
        private Vector3 HarassBackPos = default(Vector3);
        private bool WCasted = false, ECasted = false;
        private int AACount = 0;

        public Renekton()
        {
            Q = new Spell(SpellSlot.Q, 370);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 550);
            R = new Spell(SpellSlot.R);
            Q.SetSkillshot(0.5f, 370, float.MaxValue, false, SkillshotType.SkillshotCircle);
            W.SetTargetted(0.2333f, float.MaxValue);
            E.SetSkillshot(0.5f, 50, float.MaxValue, false, SkillshotType.SkillshotLine);

            var ChampMenu = new Menu("Plugin", Name + "Plugin");
            {
                var ComboMenu = new Menu("Combo", "Combo");
                {
                    ItemBool(ComboMenu, "Q", "Use Q");
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
                    ItemSlider(HarassMenu, "EAbove", "-> If Hp Above", 20);
                    ChampMenu.AddSubMenu(HarassMenu);
                }
                var ClearMenu = new Menu("Lane/Jungle Clear", "Clear");
                {
                    ItemBool(ClearMenu, "Q", "Use Q");
                    ItemBool(ClearMenu, "W", "Use W");
                    ItemBool(ClearMenu, "E", "Use E");
                    ItemBool(ClearMenu, "Item", "Use Tiamat/Hydra");
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
                    ItemBool(MiscMenu, "WAntiGap", "Use W To Anti Gap Closer");
                    ItemBool(MiscMenu, "WInterrupt", "Use W To Interrupt");
                    ItemBool(MiscMenu, "WCancel", "Cancel W Animation");
                    ItemSlider(MiscMenu, "CustomSkin", "Skin Changer", 6, 0, 6).ValueChanged += SkinChanger;
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
            Obj_AI_Base.OnProcessSpellCast += TrySurviveSpellCast;
            Orbwalk.OnAttack += OnAttack;
            Orbwalk.AfterAttack += AfterAttack;
        }

        private void OnGameUpdate(EventArgs args)
        {
            if (Player.IsDead || MenuGUI.IsChatOpen || Player.IsChannelingImportantSpell() || Player.IsRecalling())
            {
                if (Player.IsDead) AACount = 0;
                return;
            }
            switch (Orbwalk.CurrentMode)
            {
                case Orbwalk.Mode.Combo:
                    NormalCombo();
                    break;
                case Orbwalk.Mode.Harass:
                    Harass();
                    break;
                case Orbwalk.Mode.LaneClear:
                    LaneJungClear();
                    break;
                case Orbwalk.Mode.Flee:
                    if (E.IsReady()) E.Cast(Game.CursorPos, PacketCast());
                    break;
                case Orbwalk.Mode.None:
                    AACount = 0;
                    break;
            }
            if (ItemBool("Ultimate", "RSurvive") && R.IsReady()) TrySurvive(R.Slot, ItemSlider("Ultimate", "RUnder"));
        }

        private void OnDraw(EventArgs args)
        {
            if (Player.IsDead) return;
            if (ItemBool("Draw", "Q") && Q.Level > 0) Render.Circle.DrawCircle(Player.Position, Q.Range, Q.IsReady() ? Color.Green : Color.Red, 7);
            if (ItemBool("Draw", "E") && E.Level > 0) Render.Circle.DrawCircle(Player.Position, E.Range, E.IsReady() ? Color.Green : Color.Red, 7);
        }

        private void OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (!ItemBool("Misc", "WAntiGap") || Player.IsDead || !W.IsReady() || !Player.HasBuff("RenektonExecuteReady")) return;
            if (Player.Distance3D(gapcloser.Sender) <= Orbwalk.GetAutoAttackRange(Player, gapcloser.Sender) + 50)
            {
                if (W.IsReady()) W.Cast(PacketCast());
                if (Player.HasBuff("RenektonExecuteReady")) Player.IssueOrder(GameObjectOrder.AttackUnit, gapcloser.Sender);
            }
        }

        private void OnPossibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            if (!ItemBool("Misc", "WInterrupt") || Player.IsDead || !W.IsReady() || !Player.HasBuff("RenektonExecuteReady")) return;
            if (!Orbwalk.InAutoAttackRange(unit) && E.CanCast(unit)) E.Cast(unit.Position.Randomize(0, (int)Orbwalk.GetAutoAttackRange(Player, unit) / 2), PacketCast());
            if (Player.Distance3D(unit) <= Orbwalk.GetAutoAttackRange(Player, unit) + 20)
            {
                if (W.IsReady()) W.Cast(PacketCast());
                if (Player.HasBuff("RenektonExecuteReady")) Player.IssueOrder(GameObjectOrder.AttackUnit, unit);
            }
        }

        private void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe) return;
            if (args.SData.Name == "RenektonCleave") AACount = 0;
            if (args.SData.Name == "RenektonPreExecute") AACount = 0;
            if (args.SData.Name == "RenektonExecute")
            {
                WCasted = true;
                Utility.DelayAction.Add(200, () => WCasted = false);
                AACount = 0;
            }
            if (args.SData.Name == "RenektonSliceAndDice")
            {
                ECasted = true;
                Utility.DelayAction.Add(Orbwalk.CurrentMode == Orbwalk.Mode.LaneClear ? 3000 : 2400, () => ECasted = false);
                AACount = 0;
            }
            if (args.SData.Name == "renektondice") AACount = 0;
        }

        private void OnAttack(AttackableUnit Target)
        {
            if (Target.IsValidTarget(Orbwalk.GetAutoAttackRange(Player, Target) + 20) && W.IsReady())
            {
                var Obj = (Obj_AI_Base)Target;
                if (Orbwalk.CurrentMode == Orbwalk.Mode.LaneClear && ItemBool("Clear", "W") && Target is Obj_AI_Minion && (CanKill(Obj, W, Player.Mana >= 50 ? 1 : 0) || Obj.MaxHealth >= 1200))
                {
                    W.Cast(PacketCast());
                }
                else if ((Orbwalk.CurrentMode == Orbwalk.Mode.Combo || Orbwalk.CurrentMode == Orbwalk.Mode.Harass) && ItemBool(Orbwalk.CurrentMode.ToString(), "W") && Target is Obj_AI_Hero) W.Cast(PacketCast());
            }
        }

        private void AfterAttack(AttackableUnit Target)
        {
            if (!WCasted && ((Orbwalk.CurrentMode == Orbwalk.Mode.Harass && Target is Obj_AI_Hero) || (Orbwalk.CurrentMode == Orbwalk.Mode.LaneClear && Target is Obj_AI_Minion))) AACount += 1;
            if ((Orbwalk.CurrentMode == Orbwalk.Mode.Combo || Orbwalk.CurrentMode == Orbwalk.Mode.Harass) && ItemBool(Orbwalk.CurrentMode.ToString(), "W") && ItemBool("Misc", "WCancel") && ((Obj_AI_Hero)Target).Buffs.Any(i => i.SourceName == Name && i.DisplayName == "Stun") && Target is Obj_AI_Hero) UseItem((Obj_AI_Hero)Target, true);
        }

        private void NormalCombo()
        {
            if (!targetObj.IsValidTarget() || Player.IsDashing()) return;
            if (ItemBool("Combo", "E") && E.IsReady())
            {
                if (E.Instance.Name == "RenektonSliceAndDice")
                {
                    if (E.InRange(targetObj))
                    {
                        E.Cast(targetObj.Position.Extend(Player.Position, Player.Distance3D(targetObj) <= E.Range - 100 ? -100 : 0), PacketCast());
                    }
                    else if (Player.Distance3D(targetObj) > E.Range + 30 && Player.Distance3D(targetObj) <= E.Range * 2 - 20)
                    {
                        foreach (var Obj in ObjectManager.Get<Obj_AI_Base>().Where(i => i.IsValidTarget(E.Range) && !(i is Obj_AI_Turret) && i.Distance3D(targetObj) <= E.Range - 20).OrderBy(i => i.Distance3D(targetObj))) E.Cast(Obj.Position.Extend(Player.Position, Player.Distance3D(Obj) <= E.Range - 100 ? -100 : 0), PacketCast());
                    }
                }
                else if (!ECasted || Player.Distance3D(targetObj) > E.Range - 30 || CanKill(targetObj, E, Player.Mana >= 50 ? 1 : 0)) E.Cast(targetObj.Position.Extend(Player.Position, Player.Distance3D(targetObj) <= E.Range - 100 ? -100 : 0), PacketCast());
            }
            if (ItemBool("Combo", "Q") && Q.CanCast(targetObj)) Q.Cast(PacketCast());
            if (ItemBool("Combo", "W") && (W.IsReady() || Player.HasBuff("RenektonExecuteReady")) && Orbwalk.InAutoAttackRange(targetObj))
            {
                Orbwalk.SetAttack(false);
                Player.IssueOrder(GameObjectOrder.AttackUnit, targetObj);
                Orbwalk.SetAttack(true);
            }
            if (ItemBool("Combo", "Item")) UseItem(targetObj);
            if (ItemBool("Combo", "Ignite") && IgniteReady()) CastIgnite(targetObj);
        }

        private void Harass()
        {
            if (!targetObj.IsValidTarget() || Player.IsDashing()) return;
            if (ItemBool("Harass", "E"))
            {
                if (E.IsReady())
                {
                    if (E.Instance.Name == "RenektonSliceAndDice")
                    {
                        if (E.InRange(targetObj) && Player.HealthPercentage() >= ItemSlider("Harass", "EAbove"))
                        {
                            HarassBackPos = Player.ServerPosition;
                            E.Cast(targetObj.Position.Extend(Player.Position, Player.Distance3D(targetObj) <= E.Range - 100 ? -100 : 0), PacketCast());
                        }
                    }
                    else if (!ECasted || AACount >= 2) E.Cast(HarassBackPos, PacketCast());
                }
                else if (HarassBackPos != default(Vector3)) HarassBackPos = default(Vector3);
            }
            if (ItemBool("Harass", "Q") && Q.CanCast(targetObj) && (AACount >= 2 || (E.IsReady() && E.Instance.Name != "RenektonSliceAndDice"))) Q.Cast(PacketCast());
            if (ItemBool("Harass", "W") && (W.IsReady() || Player.HasBuff("RenektonExecuteReady")) && (AACount >= 1 || (ItemBool("Harass", "E") && E.IsReady() && E.Instance.Name != "RenektonSliceAndDice")) && Orbwalk.InAutoAttackRange(targetObj))
            {
                Orbwalk.SetAttack(false);
                Player.IssueOrder(GameObjectOrder.AttackUnit, targetObj);
                Orbwalk.SetAttack(true);
            }
        }

        private void LaneJungClear()
        {
            if (Player.IsDashing()) return;
            var minionObj = MinionManager.GetMinions(E.Range, MinionTypes.All, MinionTeam.NotAlly);
            foreach (var Obj in minionObj)
            {
                if (ItemBool("Clear", "E") && E.IsReady())
                {
                    if (E.Instance.Name == "RenektonSliceAndDice")
                    {
                        E.Cast(GetClearPos(minionObj, E), PacketCast());
                    }
                    else if (!ECasted || AACount >= 2) E.Cast(GetClearPos(minionObj, E), PacketCast());
                }
                if (ItemBool("Clear", "Q") && Q.IsReady() && (AACount >= 2 || (E.IsReady() && E.Instance.Name != "RenektonSliceAndDice")) && (minionObj.Count(i => Q.InRange(i)) >= 2 || (Obj.MaxHealth >= 1200 && Q.InRange(Obj)))) Q.Cast(PacketCast());
                if (ItemBool("Clear", "W") && (W.IsReady() || Player.HasBuff("RenektonExecuteReady")) && AACount >= 1 && Orbwalk.InAutoAttackRange(Obj) && (CanKill(Obj, W, Player.Mana >= 50 ? 1 : 0) || Obj.MaxHealth >= 1200))
                {
                    Orbwalk.SetAttack(false);
                    Player.IssueOrder(GameObjectOrder.AttackUnit, Obj);
                    Orbwalk.SetAttack(true);
                    break;
                }
                if (ItemBool("Clear", "Item") && AACount >= 1) UseItem(Obj, true);
            }
        }

        private void UseItem(Obj_AI_Base Target, bool IsFarm = false)
        {
            if (Tiamat.IsReady() && IsFarm ? Player.Distance3D(Target) <= Tiamat.Range : Player.CountEnemysInRange((int)Tiamat.Range) >= 1) Tiamat.Cast();
            if (Hydra.IsReady() && IsFarm ? Player.Distance3D(Target) <= Hydra.Range : (Player.CountEnemysInRange((int)Hydra.Range) >= 2 || (Player.GetAutoAttackDamage(Target, true) < Target.Health && Player.CountEnemysInRange((int)Hydra.Range) == 1))) Hydra.Cast();
            if (RanduinOmen.IsReady() && Player.CountEnemysInRange((int)RanduinOmen.Range) >= 1 && !IsFarm) RanduinOmen.Cast();
        }
    }
}