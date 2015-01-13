using System;
using System.Linq;
using Color = System.Drawing.Color;

using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

using Orbwalk = MasterSeries.Common.M_Orbwalker;

namespace MasterSeries.Champions
{
    class Lucian : Program
    {
        private bool QCasted = false, WCasted = false, ECasted = false, WillInAA = false;
        private Spell Q2;
        private Vector2 REndPos = default(Vector2);

        public Lucian()
        {
            Q = new Spell(SpellSlot.Q, 630);
            Q2 = new Spell(SpellSlot.Q, 1130);
            W = new Spell(SpellSlot.W, 1080);
            E = new Spell(SpellSlot.E, 445);
            R = new Spell(SpellSlot.R, 1460);
            Q.SetTargetted(0, 500);
            Q2.SetSkillshot(0, 65, 500, true, SkillshotType.SkillshotLine);
            W.SetSkillshot(0, 80, 500, true, SkillshotType.SkillshotLine);
            R.SetSkillshot(0, 120, 500, true, SkillshotType.SkillshotLine);

            var ChampMenu = new Menu("Plugin", Name + "Plugin");
            {
                var ComboMenu = new Menu("Combo", "Combo");
                {
                    ItemBool(ComboMenu, "Passive", "Use Passive");
                    ItemBool(ComboMenu, "Q", "Use Q");
                    ItemBool(ComboMenu, "W", "Use W");
                    ItemBool(ComboMenu, "E", "Use E");
                    ItemSlider(ComboMenu, "EDelay", "-> Stop All If E Will Ready In (ms)", 2000, 0, 4000);
                    ItemBool(ComboMenu, "R", "Use R If Killable");
                    ItemBool(ComboMenu, "CancelR", "-> Stop R For Kill Steal");
                    ItemBool(ComboMenu, "Item", "Use Item");
                    ItemBool(ComboMenu, "Ignite", "Auto Ignite If Killable");
                    ChampMenu.AddSubMenu(ComboMenu);
                }
                var HarassMenu = new Menu("Harass", "Harass");
                {
                    ItemBool(HarassMenu, "Passive", "Use Passive");
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
                    ItemSlider(ClearMenu, "EDelay", "-> Stop All If E Will Ready In (ms)", 2000, 0, 4000);
                    ChampMenu.AddSubMenu(ClearMenu);
                }
                var MiscMenu = new Menu("Misc", "Misc");
                {
                    ItemBool(MiscMenu, "QKillSteal", "Use Q To Kill Steal");
                    ItemSlider(MiscMenu, "CustomSkin", "Skin Changer", 2, 0, 2).ValueChanged += SkinChanger;
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
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            Orbwalk.AfterAttack += AfterAttack;
        }

        private void OnGameUpdate(EventArgs args)
        {
            if (Player.IsDead || MenuGUI.IsChatOpen || Player.IsRecalling()) return;
            if (ItemBool("Misc", "QKillSteal")) KillSteal();
            if (Player.IsChannelingImportantSpell())
            {
                if (ItemBool("Combo", "R"))
                {
                    if (Player.CountEnemysInRange((int)R.Range + 60) == 0) R.Cast(PacketCast());
                    if (targetObj.IsValidTarget()) LockROnTarget(targetObj);
                }
                return;
            }
            else REndPos = default(Vector2);
            if (Orbwalk.CurrentMode == Orbwalk.Mode.Combo || Orbwalk.CurrentMode == Orbwalk.Mode.Harass)
            {
                NormalCombo(Orbwalk.CurrentMode.ToString());
            }
            else if (Orbwalk.CurrentMode == Orbwalk.Mode.LaneClear) LaneJungClear();
            if (Orbwalk.CurrentMode != Orbwalk.Mode.Combo) WillInAA = false;
        }

        private void OnDraw(EventArgs args)
        {
            if (Player.IsDead) return;
            if (ItemBool("Draw", "Q") && Q.Level > 0) Render.Circle.DrawCircle(Player.Position, Q.Range, Q.IsReady() ? Color.Green : Color.Red, 7);
            if (ItemBool("Draw", "W") && W.Level > 0) Render.Circle.DrawCircle(Player.Position, W.Range, W.IsReady() ? Color.Green : Color.Red, 7);
            if (ItemBool("Draw", "E") && E.Level > 0) Render.Circle.DrawCircle(Player.Position, E.Range, E.IsReady() ? Color.Green : Color.Red, 7);
            if (ItemBool("Draw", "R") && R.Level > 0) Render.Circle.DrawCircle(Player.Position, R.Range, R.IsReady() ? Color.Green : Color.Red, 7);
        }

        private void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe) return;
            if (args.SData.Name == "LucianQ")
            {
                QCasted = true;
                Utility.DelayAction.Add(250, () => QCasted = false);
            }
            if (args.SData.Name == "LucianW")
            {
                WCasted = true;
                Utility.DelayAction.Add(350, () => WCasted = false);
            }
            if (args.SData.Name == "LucianE")
            {
                ECasted = true;
                Utility.DelayAction.Add(250, () => ECasted = false);
            }
        }

        private void AfterAttack(AttackableUnit Target)
        {
            if (!E.IsReady()) return;
            if ((Orbwalk.CurrentMode == Orbwalk.Mode.LaneClear && ItemBool("Clear", "E") && !HavePassive() && Target is Obj_AI_Minion) || ((Orbwalk.CurrentMode == Orbwalk.Mode.Combo || (Orbwalk.CurrentMode == Orbwalk.Mode.Harass && Player.HealthPercentage() >= ItemSlider("Harass", "EAbove"))) && ItemBool(Orbwalk.CurrentMode.ToString(), "E") && !HavePassive(Orbwalk.CurrentMode.ToString()) && Target is Obj_AI_Hero))
            {
                var Pos = (Player.Position.Distance(Game.CursorPos) <= E.Range && Player.Position.Distance(Game.CursorPos) > 100) ? Game.CursorPos : Player.Position.Extend(Game.CursorPos, E.Range);
                if (Target.Position.Distance(Pos) <= Orbwalk.GetAutoAttackRange(Player, Target))
                {
                    E.Cast(Pos, PacketCast());
                    if (Orbwalk.CurrentMode == Orbwalk.Mode.Combo) WillInAA = true;
                }
                WillInAA = false;
            }
        }

        private void NormalCombo(string Mode)
        {
            if (!targetObj.IsValidTarget() || Player.IsDashing()) return;
            if (ItemBool(Mode, "Q") && Q.IsReady() && CanKill(targetObj, Q))
            {
                if (Q.InRange(targetObj))
                {
                    Q.CastOnUnit(targetObj, PacketCast());
                }
                else if (Q2.InRange(targetObj)) foreach (var Obj in Q2.GetPrediction(targetObj).CollisionObjects.Where(i => Q.InRange(i) && Q2.WillHit(i.Position, targetObj.Position))) Q.CastOnUnit(Obj, PacketCast());
            }
            if (ItemBool(Mode, "W") && W.CanCast(targetObj) && CanKill(targetObj, W))
            {
                if (W.GetPrediction(targetObj).Hitchance >= HitChance.Low)
                {
                    W.CastIfHitchanceEquals(targetObj, HitChance.Low, PacketCast());
                }
                else if (W.GetPrediction(targetObj).Hitchance == HitChance.Collision)
                {
                    foreach (var Obj in W.GetPrediction(targetObj, true).CollisionObjects.Where(i => i.Distance3D(targetObj) <= W.Width && W.GetPrediction(i).Hitchance >= HitChance.Low)) W.Cast(Obj.Position, PacketCast());
                }
            }
            if (Mode == "Combo" && ItemBool(Mode, "R") && R.CanCast(targetObj) && CanKill(targetObj, R, GetRDmg(targetObj)))
            {
                if (Player.Distance3D(targetObj) > 500 && Player.Distance3D(targetObj) <= 800 && (!ItemBool(Mode, "Q") || (ItemBool(Mode, "Q") && !Q.IsReady())) && (!ItemBool(Mode, "W") || (ItemBool(Mode, "W") && !W.IsReady())) && (!ItemBool(Mode, "E") || (ItemBool(Mode, "E") && !E.IsReady())))
                {
                    R.Cast(targetObj, PacketCast());
                    REndPos = (Player.Position - targetObj.Position).To2D().Normalized();
                }
                else if (Player.Distance3D(targetObj) > 800 && Player.Distance3D(targetObj) <= 1075)
                {
                    R.Cast(targetObj, PacketCast());
                    REndPos = (Player.Position - targetObj.Position).To2D().Normalized();
                }
            }
            if (Mode == "Combo" && ItemBool(Mode, "E") && E.IsReady() && !Orbwalk.InAutoAttackRange(targetObj) && targetObj.Position.Distance(Player.Position.Extend(Game.CursorPos, E.Range)) + 30 <= Orbwalk.GetAutoAttackRange(Player, targetObj)) E.Cast(Game.CursorPos, PacketCast());
            if (Mode == "Combo" && ItemBool(Mode, "Item")) UseItem(targetObj);
            if (Mode == "Combo" && ItemBool(Mode, "Ignite") && IgniteReady()) CastIgnite(targetObj);
            if (!ItemBool(Mode, "E") || (ItemBool(Mode, "E") && (!E.IsReady() || (Mode == "Combo" && E.IsReady() && !WillInAA))))
            {
                if (Mode == "Combo" && ItemBool(Mode, "E") && E.IsReady(ItemSlider(Mode, "EDelay"))) return;
                if (ItemBool(Mode, "Q") && Q.IsReady())
                {
                    if ((Orbwalk.InAutoAttackRange(targetObj) && !HavePassive(Mode)) || (Player.Distance3D(targetObj) > Orbwalk.GetAutoAttackRange(Player, targetObj) + 50 && Q.InRange(targetObj)))
                    {
                        Q.CastOnUnit(targetObj, PacketCast());
                    }
                    else if (!Q.InRange(targetObj) && Q2.InRange(targetObj))
                    {
                        foreach (var Obj in Q2.GetPrediction(targetObj).CollisionObjects.Where(i => Q.InRange(i) && Q2.WillHit(i.Position, Q2.GetPrediction(targetObj).CastPosition))) Q.CastOnUnit(Obj, PacketCast());
                    }
                }
                if ((!ItemBool(Mode, "Q") || (ItemBool(Mode, "Q") && !Q.IsReady())) && ItemBool(Mode, "W") && W.IsReady() && ((Orbwalk.InAutoAttackRange(targetObj) && !HavePassive(Mode)) || (Player.Distance3D(targetObj) > Orbwalk.GetAutoAttackRange(Player, targetObj) + 50 && W.InRange(targetObj))))
                {
                    if (W.GetPrediction(targetObj).Hitchance >= HitChance.Low)
                    {
                        W.CastIfHitchanceEquals(targetObj, HitChance.Low, PacketCast());
                    }
                    else if (W.GetPrediction(targetObj).Hitchance == HitChance.Collision)
                    {
                        foreach (var Obj in W.GetPrediction(targetObj, true).CollisionObjects.Where(i => i.Distance3D(targetObj) <= W.Width && W.GetPrediction(i).Hitchance >= HitChance.Low)) W.Cast(Obj.Position, PacketCast());
                    }
                }
            }
        }

        private void LaneJungClear()
        {
            if (Player.IsDashing()) return;
            var minionObj = MinionManager.GetMinions(Q2.Range, MinionTypes.All, MinionTeam.NotAlly);
            foreach (var Obj in minionObj)
            {
                if (!ItemBool("Clear", "E") || (ItemBool("Clear", "E") && !E.IsReady()))
                {
                    if (ItemBool("Clear", "E") && E.IsReady(ItemSlider("Clear", "EDelay"))) return;
                    if (ItemBool("Clear", "W") && W.IsReady() && !HavePassive())
                    {
                        if (W.InRange(Obj) && Obj.Team == GameObjectTeam.Neutral && Obj.MaxHealth >= 1200)
                        {
                            W.CastIfHitchanceEquals(Obj, HitChance.Medium, PacketCast());
                        }
                        else
                        {
                            var BestW = 0;
                            var BestWPos = default(Vector3);
                            foreach (var Sub in minionObj.Where(i => W.InRange(i) && W.GetPrediction(i).Hitchance >= HitChance.Low))
                            {
                                var Hit = W.GetPrediction(Sub, true).CollisionObjects.Count(i => i.Distance3D(Sub) <= W.Width);
                                if (Hit > BestW || BestWPos == default(Vector3))
                                {
                                    BestW = Hit;
                                    BestWPos = Sub.Position;
                                }
                            }
                            if (BestWPos != default(Vector3)) W.Cast(BestWPos, PacketCast());
                        }
                    }
                    if ((!ItemBool("Clear", "W") || (ItemBool("Clear", "W") && !W.IsReady())) && ItemBool("Clear", "Q") && Q.IsReady() && !HavePassive())
                    {
                        if (Q.InRange(Obj) && Obj.Team == GameObjectTeam.Neutral && Obj.MaxHealth >= 1200)
                        {
                            Q.CastOnUnit(Obj, PacketCast());
                        }
                        else
                        {
                            var BestQ = 0;
                            Obj_AI_Base BestQTarget = null;
                            foreach (var Sub in minionObj.OrderByDescending(i => i.Distance3D(Player)))
                            {
                                var Hit = Q2.GetPrediction(Sub).CollisionObjects.Count(i => Q2.WillHit(i.Position, Q2.GetPrediction(Sub).CastPosition));
                                if (Hit > BestQ || BestQTarget == null)
                                {
                                    BestQ = Hit;
                                    BestQTarget = Sub;
                                }
                            }
                            if (BestQTarget != null) Q.CastOnUnit(BestQTarget, PacketCast());
                        }
                    }
                }
            }
        }

        private void KillSteal()
        {
            if (!Q.IsReady() || Player.IsDashing() || ((!ItemBool("Combo", "R") || (ItemBool("Combo", "R") && !ItemBool("Combo", "CancelR"))) && Player.IsChannelingImportantSpell())) return;
            var CancelR = ItemBool("Combo", "R") && ItemBool("Combo", "CancelR") && Player.IsChannelingImportantSpell();
            foreach (var Obj in ObjectManager.Get<Obj_AI_Hero>().Where(i => i.IsValidTarget(Q2.Range) && CanKill(i, Q) && i != targetObj).OrderBy(i => i.Health).OrderBy(i => i.Distance3D(Player)))
            {
                if (Q.InRange(Obj))
                {
                    if (CancelR) R.Cast(PacketCast());
                    Q.CastOnUnit(Obj, PacketCast());
                }
                else
                {
                    foreach (var Col in Q2.GetPrediction(Obj).CollisionObjects.Where(i => Q.InRange(i) && Q2.WillHit(i.Position, Q2.GetPrediction(Obj).CastPosition)))
                    {
                        if (CancelR) R.Cast(PacketCast());
                        Q.CastOnUnit(Col, PacketCast());
                    }
                }
            }
        }

        private void UseItem(Obj_AI_Base Target)
        {
            if (Bilgewater.IsReady()) Bilgewater.Cast(Target);
            if (BladeRuined.IsReady()) BladeRuined.Cast(Target);
            if (Youmuu.IsReady() && Player.CountEnemysInRange((int)Orbwalk.GetAutoAttackRange()) >= 1) Youmuu.Cast();
        }

        private bool HavePassive(string Mode = "Clear")
        {
            if (Mode != "Clear" && !ItemBool(Mode, "Passive")) return false;
            if (QCasted || WCasted || ECasted || Player.HasBuff("LucianPassiveBuff")) return true;
            return false;
        }

        private double GetRDmg(Obj_AI_Hero Target)
        {
            var Shot = (int)(7.5 + new double[] { 7.5, 9, 10.5 }[R.Level - 1] * 1 / Player.AttackDelay);
            var MaxShot = new int[] { 26, 30, 33 }[R.Level - 1];
            return Player.CalcDamage(Target, Damage.DamageType.Physical, (new double[] { 40, 50, 60 }[R.Level - 1] + 0.25 * Player.FlatPhysicalDamageMod + 0.1 * Player.FlatMagicDamageMod) * (Shot > MaxShot ? MaxShot : Shot));
        }

        private void LockROnTarget(Obj_AI_Hero Target)
        {
            var PredR = R.GetPrediction(Target).CastPosition;
            var Pos = new Vector2(PredR.X + REndPos.X * R.Range * 0.98f, PredR.Y + REndPos.Y * R.Range * 0.98f).To3D();
            var ClosePos = Player.Position.To2D().Closest(new Vector2[] { PredR.To2D(), Pos.To2D() }.ToList()).To3D();
            if (ClosePos.IsValid() && !ClosePos.IsWall() && PredR.Distance(ClosePos) > E.Range)
            {
                Player.IssueOrder(GameObjectOrder.MoveTo, ClosePos);
            }
            else if (Pos.IsValid() && !Pos.IsWall() && PredR.Distance(Pos) < R.Range && PredR.Distance(Pos) > 100)
            {
                Player.IssueOrder(GameObjectOrder.MoveTo, Pos);
            }
            else Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
        }
    }
}