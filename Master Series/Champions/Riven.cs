using System;
using System.Linq;
using Color = System.Drawing.Color;

using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

using Orbwalk = MasterSeries.Common.M_Orbwalker;

namespace MasterSeries.Champion
{
    class Riven : Program
    {
        private int AACount = 0;

        public Riven()
        {
            Q = new Spell(SpellSlot.Q, 295);
            W = new Spell(SpellSlot.W, 260);
            E = new Spell(SpellSlot.E, 250);
            R = new Spell(SpellSlot.R, 900);
            Q.SetTargetted(0.5f, float.MaxValue);
            W.SetSkillshot(0, 260, 1500, false, SkillshotType.SkillshotCircle);
            R.SetSkillshot(0.3f, 200, 2200, false, SkillshotType.SkillshotCone);

            Game.OnGameUpdate += OnGameUpdate;
            Obj_AI_Base.OnPlayAnimation += OnPlayAnimation;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            Orbwalk.AfterAttack += AfterAttack;
        }

        private void AfterAttack(AttackableUnit Target)
        {
            if (Orbwalk.CurrentMode == Orbwalk.Mode.Combo)
            {
                AACount += 1;
            }
        }

        private void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe || Orbwalk.CurrentMode != Orbwalk.Mode.Combo) return;
            var Slot = Player.GetSpellSlot(args.SData.Name);
            if (Slot == SpellSlot.Q || Slot == SpellSlot.W)
            {
                AACount = 0;
                if (Slot == SpellSlot.Q) Orbwalk.ResetAutoAttack();
                if (Tiamat.IsReady() && Player.CountEnemysInRange((int)Tiamat.Range) >= 1) Tiamat.Cast();
                if (Hydra.IsReady() && (Player.CountEnemysInRange((int)Hydra.Range) >= 2 || (Player.GetAutoAttackDamage(targetObj, true) < targetObj.Health && Player.CountEnemysInRange((int)Hydra.Range) == 1))) Hydra.Cast();
            }
        }

        private void OnPlayAnimation(GameObject sender, GameObjectPlayAnimationEventArgs args)
        {
            if (!sender.IsMe) return;
            //if (args.Animation.Contains("Spell") && Orbwalk.CurrentMode == Orbwalk.Mode.Combo) Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
        }

        private void OnGameUpdate(EventArgs args)
        {
            if (Player.IsDead || MenuGUI.IsChatOpen || Player.IsChannelingImportantSpell() || Player.IsRecalling()) return;
            if (Orbwalk.CurrentMode == Orbwalk.Mode.Combo) NormalCombo();
        }

        private void NormalCombo()
        {
            if (!targetObj.IsValidTarget()) return;
            if (R.CanCast(targetObj) && R.Instance.Name != "RivenFengShuiEngine" && CanKill(targetObj, R)) R.Cast(targetObj.Position, PacketCast());
            if (Player.Distance3D(targetObj) <= E.Range + Orbwalk.GetAutoAttackRange(Player, targetObj) - 30)
            {
                if (Player.Distance3D(targetObj) <= Orbwalk.GetAutoAttackRange(Player, targetObj) + 30)
                {
                    //Player.IssueOrder(GameObjectOrder.AttackTo, targetObj.Position);
                    if (AACount == 0) return;
                    if (R.IsReady() && R.Instance.Name == "RivenFengShuiEngine") R.Cast(PacketCast());
                    if (E.IsReady()) E.Cast(targetObj.Position.Extend(Player.Position, -30), PacketCast());
                    if (W.CanCast(targetObj)) W.Cast(PacketCast());
                    if (Q.IsReady())
                    {
                        int QState = 1;
                        if (Player.HasBuff("riventricleavesoundone", true)) QState = 2;
                        if (Player.HasBuff("riventricleavesoundtwo", true)) QState = 3;
                        if (R.CanCast(targetObj) && R.Instance.Name != "RivenFengShuiEngine" && QState == 3) R.Cast(targetObj.Position, PacketCast());
                        if (QState == 1 || QState == 2 || (!R.IsReady() && QState == 3)) Q.Cast(targetObj.Position.Extend(Player.Position, -20), PacketCast());
                    }
                }
                else
                {
                    if (R.IsReady() && R.Instance.Name == "RivenFengShuiEngine") R.Cast(PacketCast());
                    if (E.IsReady()) E.Cast(targetObj.Position, PacketCast());
                    if (Q.IsReady() && (!E.IsReady() || Player.LastCastedSpellName() == "RivenFeint")) Q.Cast(targetObj.Position, PacketCast());
                }
            }
        }
    }
}