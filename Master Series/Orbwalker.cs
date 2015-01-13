using System;
using System.Linq;
using System.Collections.Generic;
using Color = System.Drawing.Color;

using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace MasterSeries.Common
{
    class M_Orbwalker
    {
        private static Menu Config;
        private static Obj_AI_Hero Player = ObjectManager.Player;
        public static Obj_AI_Base ForcedTarget = null;
        public enum Mode
        {
            Combo,
            Harass,
            LaneClear,
            LastHit,
            Flee,
            None
        }
        private static bool Attack = true;
        private static bool Move = true;
        private static bool DisableNextAttack;
        private const float ClearWaitTimeMod = 2f;
        private static int LastAttack;
        private static AttackableUnit LastTarget;
        private static Obj_AI_Minion PrevMinion;
        private static Spell MovePrediction;
        private static int LastMove;
        private static int WindUp;
        private static int LastRealAttack;

        public class BeforeAttackEventArgs
        {
            public AttackableUnit Target;
            private bool Value = true;
            public bool Process
            {
                get
                {
                    return Value;
                }
                set
                {
                    DisableNextAttack = !value;
                    Value = value;
                }
            }
        }
        public delegate void BeforeAttackEvenH(BeforeAttackEventArgs Args);
        public delegate void OnAttackEvenH(AttackableUnit Target);
        public delegate void AfterAttackEvenH(AttackableUnit Target);
        public delegate void OnTargetChangeH(AttackableUnit OldTarget, AttackableUnit NewTarget);
        public delegate void OnNonKillableMinionH(AttackableUnit Minion);

        public static event BeforeAttackEvenH BeforeAttack;
        public static event OnAttackEvenH OnAttack;
        public static event AfterAttackEvenH AfterAttack;
        public static event OnTargetChangeH OnTargetChange;
        public static event OnNonKillableMinionH OnNonKillableMinion;

        public static void AddToMenu(Menu MainMenu)
        {
            var OWMenu = new Menu("Orbwalker", "OW");
            {
                var DrawMenu = new Menu("Draw", "Draw");
                {
                    DrawMenu.AddItem(new MenuItem("OW_Draw_AARange", "AA Circle").SetValue(new Circle(false, Color.FloralWhite)));
                    DrawMenu.AddItem(new MenuItem("OW_Draw_AARangeEnemy", "AA Circle Enemy").SetValue(new Circle(false, Color.Pink)));
                    DrawMenu.AddItem(new MenuItem("OW_Draw_HoldZone", "Hold Zone").SetValue(new Circle(false, Color.FloralWhite)));
                    DrawMenu.AddItem(new MenuItem("OW_Draw_LastHit", "Minion Last Hit").SetValue(new Circle(false, Color.Lime)));
                    DrawMenu.AddItem(new MenuItem("OW_Draw_NearKill", "Minion Near Kill").SetValue(new Circle(false, Color.Gold)));
                    OWMenu.AddSubMenu(DrawMenu);
                }
                var MiscMenu = new Menu("Misc", "Misc");
                {
                    MiscMenu.AddItem(new MenuItem("OW_Misc_HoldZone", "Hold Zone").SetValue(new Slider(50, 0, 150)));
                    MiscMenu.AddItem(new MenuItem("OW_Misc_FarmDelay", "Farm Delay").SetValue(new Slider(0, 0, 200)));
                    MiscMenu.AddItem(new MenuItem("OW_Misc_MoveDelay", "Movement Delay").SetValue(new Slider(80, 0, 150)));
                    MiscMenu.AddItem(new MenuItem("OW_Misc_ExtraWindUp", "Extra WindUp Time").SetValue(new Slider(80, 0, 200)));
                    MiscMenu.AddItem(new MenuItem("OW_Misc_AutoWindUp", "Auto WindUp").SetValue(true));
                    MiscMenu.AddItem(new MenuItem("OW_Misc_PriorityUnit", "Priority Unit").SetValue(new StringList(new[] { "Minion", "Hero" })));
                    MiscMenu.AddItem(new MenuItem("OW_Misc_MeleePrediction", "Melee Movement Prediction").SetValue(false));
                    MiscMenu.AddItem(new MenuItem("OW_Misc_AllMovementDisabled", "Disable All Movement").SetValue(false));
                    MiscMenu.AddItem(new MenuItem("OW_Misc_AllAttackDisabled", "Disable All Attack").SetValue(false));
                    OWMenu.AddSubMenu(MiscMenu);
                }
                var ModeMenu = new Menu("Mode", "Mode");
                {
                    var ComboMenu = new Menu("Combo", "Mode_Combo");
                    {
                        ComboMenu.AddItem(new MenuItem("OW_Combo_Key", "Key").SetValue(new KeyBind(32, KeyBindType.Press)));
                        ComboMenu.AddItem(new MenuItem("OW_Combo_Move", "Movement").SetValue(true));
                        ComboMenu.AddItem(new MenuItem("OW_Combo_Attack", "Attack").SetValue(true));
                        ModeMenu.AddSubMenu(ComboMenu);
                    }
                    var HarassMenu = new Menu("Harass", "Mode_Harass");
                    {
                        HarassMenu.AddItem(new MenuItem("OW_Harass_Key", "Key").SetValue(new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));
                        HarassMenu.AddItem(new MenuItem("OW_Harass_Move", "Movement").SetValue(true));
                        HarassMenu.AddItem(new MenuItem("OW_Harass_Attack", "Attack").SetValue(true));
                        HarassMenu.AddItem(new MenuItem("OW_Harass_LastHit", "Last Hit Minions").SetValue(true));
                        ModeMenu.AddSubMenu(HarassMenu);
                    }
                    var ClearMenu = new Menu("Lane/Jungle Clear", "Mode_Clear");
                    {
                        ClearMenu.AddItem(new MenuItem("OW_Clear_Key", "Key").SetValue(new KeyBind("G".ToCharArray()[0], KeyBindType.Press)));
                        ClearMenu.AddItem(new MenuItem("OW_Clear_Move", "Movement").SetValue(true));
                        ClearMenu.AddItem(new MenuItem("OW_Clear_Attack", "Attack").SetValue(true));
                        ModeMenu.AddSubMenu(ClearMenu);
                    }
                    var LastHitMenu = new Menu("Last Hit", "Mode_LastHit");
                    {
                        LastHitMenu.AddItem(new MenuItem("OW_LastHit_Key", "Key").SetValue(new KeyBind(17, KeyBindType.Press)));
                        LastHitMenu.AddItem(new MenuItem("OW_LastHit_Move", "Movement").SetValue(true));
                        LastHitMenu.AddItem(new MenuItem("OW_LastHit_Attack", "Attack").SetValue(true));
                        ModeMenu.AddSubMenu(LastHitMenu);
                    }
                    var FleeMenu = new Menu("Flee", "Mode_Flee");
                    {
                        FleeMenu.AddItem(new MenuItem("OW_Flee_Key", "Key").SetValue(new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));
                        ModeMenu.AddSubMenu(FleeMenu);
                    }
                    OWMenu.AddSubMenu(ModeMenu);
                }
                OWMenu.AddItem(new MenuItem("OW_Info", "Credits: xSLx & Esk0r"));
                Config = OWMenu;
                MainMenu.AddSubMenu(OWMenu);
            }
            MovePrediction = new Spell(SpellSlot.Unknown, GetAutoAttackRange());
            MovePrediction.SetTargetted(Player.BasicAttack.SpellCastTime, Player.BasicAttack.MissileSpeed);
            Game.OnGameUpdate += OnGameUpdate;
            Drawing.OnDraw += OnDraw;
            Obj_AI_Hero.OnInstantStopAttack += OnInstantStopAttack;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            GameObject.OnCreate += OnCreateObjMissile;
        }

        private static void OnGameUpdate(EventArgs args)
        {
            if (Player.IsDead || CurrentMode == Mode.None || MenuGUI.IsChatOpen || Player.IsChannelingImportantSpell() || Player.IsRecalling()) return;
            CheckAutoWindUp();
            Orbwalk(Game.CursorPos, CurrentMode == Mode.Flee ? null : GetPossibleTarget());
        }

        private static void OnDraw(EventArgs args)
        {
            if (Player.IsDead) return;
            if (Config.Item("OW_Draw_AARange").GetValue<Circle>().Active) Render.Circle.DrawCircle(Player.Position, GetAutoAttackRange(), Config.Item("OW_Draw_AARange").GetValue<Circle>().Color, 7);
            if (Config.Item("OW_Draw_AARangeEnemy").GetValue<Circle>().Active)
            {
                foreach (var Obj in ObjectManager.Get<Obj_AI_Hero>().Where(i => i.IsValidTarget(1300))) Render.Circle.DrawCircle(Obj.Position, GetAutoAttackRange(Obj, Player), Config.Item("OW_Draw_AARangeEnemy").GetValue<Circle>().Color, 7);
            }
            if (Config.Item("OW_Draw_HoldZone").GetValue<Circle>().Active) Render.Circle.DrawCircle(Player.Position, Config.Item("OW_Misc_HoldZone").GetValue<Slider>().Value, Config.Item("OW_Draw_HoldZone").GetValue<Circle>().Color, 7);
            if (Config.Item("OW_Draw_LastHit").GetValue<Circle>().Active || Config.Item("OW_Draw_NearKill").GetValue<Circle>().Active)
            {
                foreach (var Obj in MinionManager.GetMinions(GetAutoAttackRange() + 500, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth))
                {
                    if (Config.Item("OW_Draw_LastHit").GetValue<Circle>().Active && Obj.Health <= Player.GetAutoAttackDamage(Obj, true))
                    {
                        Render.Circle.DrawCircle(Obj.Position, 70, Config.Item("OW_Draw_LastHit").GetValue<Circle>().Color, 7);
                    }
                    else if (Config.Item("OW_Draw_NearKill").GetValue<Circle>().Active && Obj.Health <= Player.GetAutoAttackDamage(Obj, true) * 2) Render.Circle.DrawCircle(Obj.Position, 70, Config.Item("OW_Draw_NearKill").GetValue<Circle>().Color, 7);
                }
            }
        }

        private static void OnInstantStopAttack(Obj_AI_Base sender, GameObjectInstantStopAttackEventArgs args)
        {
            if (sender.IsMe && (args.BitData & 1) == 0 && ((args.BitData >> 4) & 1) == 1) ResetAutoAttack();
        }

        private static void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (Orbwalking.IsAutoAttackReset(args.SData.Name) && sender.IsMe) Utility.DelayAction.Add(100, ResetAutoAttack);
            if (!args.SData.IsAutoAttack()) return;
            if (sender.IsMe && args.Target is AttackableUnit)
            {
                LastAttack = Environment.TickCount - Game.Ping / 2;
                if (args.Target.IsValid)
                {
                    FireOnTargetSwitch((AttackableUnit)args.Target);
                    LastTarget = (AttackableUnit)args.Target;
                }
                if (sender.IsMelee()) Utility.DelayAction.Add((int)(sender.AttackCastDelay * 1000 + Game.Ping * 0.5 + 50), () => FireAfterAttack(LastTarget));
                FireOnAttack(LastTarget);
            }
        }

        private static void OnCreateObjMissile(GameObject sender, EventArgs args)
        {
            if (!sender.IsValid<Obj_SpellMissile>()) return;
            var missile = (Obj_SpellMissile)sender;
            if (!missile.SData.IsAutoAttack()) return;
            if (missile.SpellCaster.IsMe && !missile.SpellCaster.IsMelee())
            {
                FireAfterAttack(LastTarget);
                LastRealAttack = Environment.TickCount;
            }
        }

        private static readonly Random RandomPos = new Random(DateTime.Now.Millisecond);
        private static void MoveTo(Vector3 Pos)
        {
            if (Environment.TickCount - LastMove < Config.Item("OW_Misc_MoveDelay").GetValue<Slider>().Value) return;
            LastMove = Environment.TickCount;
            if (Player.Distance(Pos) < Config.Item("OW_Misc_HoldZone").GetValue<Slider>().Value)
            {
                if (Player.Path.Count() > 1) Player.IssueOrder(GameObjectOrder.HoldPosition, Player.ServerPosition);
                return;
            }
            Player.IssueOrder(GameObjectOrder.MoveTo, Player.ServerPosition.Extend(Pos, (RandomPos.NextFloat(0.6f, 1) + 0.2f) * 300));
        }

        public static void Orbwalk(Vector3 Pos, AttackableUnit Target)
        {
            if (Target.IsValidTarget() && (CanAttack() || HaveCancled()) && IsAllowedToAttack())
            {
                DisableNextAttack = false;
                FireBeforeAttack(Target);
                if (!DisableNextAttack)
                {
                    Player.IssueOrder(GameObjectOrder.AttackUnit, Target);
                    if (LastTarget != null && LastTarget.IsValid && LastTarget != Target) LastAttack = Environment.TickCount + Game.Ping / 2;
                    LastTarget = Target;
                    return;
                }
            }
            if (!CanMove() || !IsAllowedToMove()) return;
            if (Player.IsMelee() && Target.IsValidTarget() && InAutoAttackRange(Target) && Config.Item("OW_Misc_MeleePrediction").GetValue<bool>() && Target is Obj_AI_Hero && Game.CursorPos.Distance(Target.Position) < 300)
            {
                MovePrediction.Delay = Player.BasicAttack.SpellCastTime;
                MovePrediction.Speed = Player.BasicAttack.MissileSpeed;
                MoveTo(MovePrediction.GetPrediction((Obj_AI_Hero)Target).UnitPosition);
            }
            else MoveTo(Pos);
        }

        public static void ResetAutoAttack()
        {
            LastAttack = 0;
        }

        private static bool IsAllowedToAttack()
        {
            if (!Attack || Config.Item("OW_Misc_AllAttackDisabled").GetValue<bool>()) return false;
            if (CurrentMode == Mode.Combo && !Config.Item("OW_Combo_Attack").GetValue<bool>()) return false;
            if (CurrentMode == Mode.Harass && !Config.Item("OW_Harass_Attack").GetValue<bool>()) return false;
            if (CurrentMode == Mode.LaneClear && !Config.Item("OW_Clear_Attack").GetValue<bool>()) return false;
            return CurrentMode != Mode.LastHit || Config.Item("OW_LastHit_Attack").GetValue<bool>();
        }

        private static bool IsAllowedToMove()
        {
            if (!Move || Config.Item("OW_Misc_AllMovementDisabled").GetValue<bool>()) return false;
            if (CurrentMode == Mode.Combo && !Config.Item("OW_Combo_Move").GetValue<bool>()) return false;
            if (CurrentMode == Mode.Harass && !Config.Item("OW_Harass_Move").GetValue<bool>()) return false;
            if (CurrentMode == Mode.LaneClear && !Config.Item("OW_Clear_Move").GetValue<bool>()) return false;
            return CurrentMode != Mode.LastHit || Config.Item("OW_LastHit_Move").GetValue<bool>();
        }

        private static void CheckAutoWindUp()
        {
            if (!Config.Item("OW_Misc_AutoWindUp").GetValue<bool>())
            {
                WindUp = GetCurrentWindupTime();
                return;
            }
            var additional = 0;
            if (Game.Ping >= 100)
            {
                additional = Game.Ping / 100 * 5;
            }
            else if (Game.Ping > 40 && Game.Ping < 100)
            {
                additional = Game.Ping / 100 * 10;
            }
            else if (Game.Ping <= 40) additional = 20;
            var windUp = Game.Ping + additional;
            if (windUp < 40) windUp = 40;
            Config.Item("OW_Misc_ExtraWindUp").SetValue(windUp < 200 ? new Slider(windUp, 0, 200) : new Slider(200, 0, 200));
            WindUp = windUp;
        }

        private static int GetCurrentWindupTime()
        {
            return Config.Item("OW_Misc_ExtraWindUp").GetValue<Slider>().Value;
        }

        public static float GetAutoAttackRange(Obj_AI_Base Source = null, AttackableUnit Target = null)
        {
            if (Source == null) Source = Player;
            var Result = Source.AttackRange + Source.BoundingRadius;
            if (Target.IsValidTarget()) Result += Target.BoundingRadius;
            return Result;
        }

        public static bool InAutoAttackRange(AttackableUnit Target)
        {
            if (!Target.IsValidTarget()) return false;
            return Player.Distance((Target is Obj_AI_Base) ? (Target as Obj_AI_Base).ServerPosition : Target.Position) <= GetAutoAttackRange(Player, Target);
        }

        public static bool CanAttack()
        {
            if (LastAttack <= Environment.TickCount) return Environment.TickCount + Game.Ping / 2 + 25 >= LastAttack + Player.AttackDelay * 1000 && Attack;
            return false;
        }

        private static bool HaveCancled()
        {
            if (LastAttack - Environment.TickCount > Player.AttackCastDelay * 1000 + 25) return LastRealAttack < LastAttack;
            return false;
        }

        public static bool CanMove()
        {
            if (LastAttack <= Environment.TickCount) return Environment.TickCount + Game.Ping / 2 >= LastAttack + Player.AttackCastDelay * 1000 + WindUp && Move;
            return false;
        }

        private static int GetCurrentFarmDelay()
        {
            return Config.Item("OW_Misc_FarmDelay").GetValue<Slider>().Value;
        }

        public static Mode CurrentMode
        {
            get
            {
                if (Config.Item("OW_Combo_Key").GetValue<KeyBind>().Active) return Mode.Combo;
                if (Config.Item("OW_Harass_Key").GetValue<KeyBind>().Active) return Mode.Harass;
                if (Config.Item("OW_Clear_Key").GetValue<KeyBind>().Active) return Mode.LaneClear;
                if (Config.Item("OW_LastHit_Key").GetValue<KeyBind>().Active) return Mode.LastHit;
                return Config.Item("OW_Flee_Key").GetValue<KeyBind>().Active ? Mode.Flee : Mode.None;
            }
        }

        public static void SetAttack(bool Value)
        {
            Attack = Value;
        }

        public static void SetMovement(bool Value)
        {
            Move = Value;
        }

        private static bool ShouldWait()
        {
            return ObjectManager.Get<Obj_AI_Minion>().Any(i => i.IsValidTarget() && i.Team != GameObjectTeam.Neutral && InAutoAttackRange(i) && HealthPrediction.LaneClearHealthPrediction(i, (int)(Player.AttackDelay * 1000 * ClearWaitTimeMod), GetCurrentFarmDelay()) <= Player.GetAutoAttackDamage(i));
        }

        private static AttackableUnit GetPossibleTarget()
        {
            AttackableUnit Target = null;
            var R = float.MaxValue;
            if (Config.Item("OW_Misc_PriorityUnit").GetValue<StringList>().SelectedIndex == 1 && (CurrentMode == Mode.Harass || CurrentMode == Mode.LaneClear))
            {
                var Obj = GetBestHeroTarget();
                if (Obj.IsValidTarget()) return Obj;
            }
            if (CurrentMode == Mode.Harass || CurrentMode == Mode.LaneClear || CurrentMode == Mode.LastHit)
            {
                foreach (var Obj in ObjectManager.Get<Obj_AI_Minion>().Where(i => i.IsValidTarget() && i.Name != "Beacon" && InAutoAttackRange(i) && i.Team != GameObjectTeam.Neutral))
                {
                    var Time = (int)(Player.AttackCastDelay * 1000) - 100 + Game.Ping / 2 + 1000 * (int)(Player.Distance(Obj.ServerPosition) / Orbwalking.GetMyProjectileSpeed());
                    var predHp = HealthPrediction.GetHealthPrediction(Obj, Time, GetCurrentFarmDelay());
                    if (predHp <= 0) FireOnNonKillableMinion(Obj);
                    if (predHp > 0 && predHp <= Player.GetAutoAttackDamage(Obj, true)) return Obj;
                }
            }
            if (ForcedTarget.IsValidTarget() && InAutoAttackRange(ForcedTarget)) return ForcedTarget;
            if (CurrentMode == Mode.LaneClear)
            {
                foreach (var Obj in ObjectManager.Get<Obj_AI_Turret>().Where(i => i.IsValidTarget() && InAutoAttackRange(i))) return Obj;
                foreach (var Obj in ObjectManager.Get<Obj_BarracksDampener>().Where(i => i.IsValidTarget() && InAutoAttackRange(i))) return Obj;
                foreach (var Obj in ObjectManager.Get<Obj_HQ>().Where(i => i.IsValidTarget() && InAutoAttackRange(i))) return Obj;
            }
            if (CurrentMode != Mode.LastHit)
            {
                var Obj = GetBestHeroTarget();
                if (Obj.IsValidTarget()) return Obj;
            }
            if (CurrentMode == Mode.Harass || CurrentMode == Mode.LaneClear)
            {
                foreach (var Obj in ObjectManager.Get<Obj_AI_Minion>().Where(i => i.IsValidTarget() && i.Name != "Beacon" && InAutoAttackRange(i) && i.Team == GameObjectTeam.Neutral && (i.MaxHealth >= R || Math.Abs(R - float.MaxValue) < float.Epsilon)))
                {
                    Target = Obj;
                    R = Obj.MaxHealth;
                }
            }
            if (CurrentMode == Mode.LaneClear && !ShouldWait())
            {
                if (PrevMinion.IsValidTarget() && InAutoAttackRange(PrevMinion))
                {
                    var predHp = HealthPrediction.LaneClearHealthPrediction(PrevMinion, (int)(Player.AttackDelay * 1000 * ClearWaitTimeMod), GetCurrentFarmDelay());
                    if (predHp >= 2 * Player.GetAutoAttackDamage(PrevMinion, true) || Math.Abs(predHp - PrevMinion.Health) < float.Epsilon) return PrevMinion;
                }
                foreach (var Obj in ObjectManager.Get<Obj_AI_Minion>().Where(i => i.IsValidTarget() && i.Name != "Beacon" && InAutoAttackRange(i)))
                {
                    var predHp = HealthPrediction.LaneClearHealthPrediction(Obj, (int)(Player.AttackDelay * 1000 * ClearWaitTimeMod), GetCurrentFarmDelay());
                    if ((predHp >= 2 * Player.GetAutoAttackDamage(Obj, true) || Math.Abs(predHp - Obj.Health) < float.Epsilon) && (Obj.Health >= R || Math.Abs(R - float.MaxValue) < float.Epsilon))
                    {
                        Target = Obj;
                        R = Obj.MaxHealth;
                        PrevMinion = Obj;
                    }
                }
            }
            return Target;
        }

        private static Obj_AI_Hero GetBestHeroTarget()
        {
            Obj_AI_Hero KillableObj = null;
            var HitsToKill = double.MaxValue;
            foreach (var Obj in ObjectManager.Get<Obj_AI_Hero>().Where(i => i.IsValidTarget() && InAutoAttackRange(i)))
            {
                var KillHits = Obj.Health / Player.GetAutoAttackDamage(Obj, true);
                if (KillableObj.IsValidTarget() && (!(KillHits < HitsToKill) || Obj.HasBuffOfType(BuffType.Invulnerability))) continue;
                KillableObj = Obj;
                HitsToKill = KillHits;
            }
            return HitsToKill <= 3 ? KillableObj : TargetSelector.GetTarget(GetAutoAttackRange(), TargetSelector.DamageType.Physical);
        }

        private static void FireBeforeAttack(AttackableUnit Target)
        {
            if (BeforeAttack != null)
            {
                BeforeAttack(new BeforeAttackEventArgs { Target = Target });
            }
            else DisableNextAttack = false;
        }

        private static void FireOnAttack(AttackableUnit Target)
        {
            if (OnAttack != null) OnAttack(Target);
        }

        private static void FireAfterAttack(AttackableUnit Target)
        {
            if (AfterAttack != null) AfterAttack(Target);
        }

        private static void FireOnTargetSwitch(AttackableUnit NewTarget)
        {
            if (OnTargetChange != null && (!LastTarget.IsValidTarget() || LastTarget != NewTarget)) OnTargetChange(LastTarget, NewTarget);
        }

        private static void FireOnNonKillableMinion(AttackableUnit Minion)
        {
            if (OnNonKillableMinion != null) OnNonKillableMinion(Minion);
        }
    }
}