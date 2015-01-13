using System;
using System.Linq;
using Color = System.Drawing.Color;

using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Orbwalk = MasterSeries.Common.M_Orbwalker;

namespace MasterSeries.Champions
{
    class LeeSin : Program
    {
        private Obj_AI_Base allyObj = null;
        private bool WardCasted = false, JumpCasted = false, KickCasted = false, FarmCasted = false, InsecJumpCasted = false, QCasted = false, WCasted = false, ECasted = false, RCasted = false;
        private enum HarassStage
        {
            Nothing,
            Doing,
            Finish
        }
        private HarassStage CurHarassStage = HarassStage.Nothing;
        private Vector3 HarassBackPos = default(Vector3), WardPlacePos = default(Vector3);
        private Spell Q2, E2;

        public LeeSin()
        {
            Q = new Spell(SpellSlot.Q, 1060);
            Q2 = new Spell(SpellSlot.Q, 1300);
            W = new Spell(SpellSlot.W, 750);
            E = new Spell(SpellSlot.E, 425);
            E2 = new Spell(SpellSlot.E, 575);
            R = new Spell(SpellSlot.R, 375);
            Q.SetSkillshot(0.5f, 60, 1800, true, SkillshotType.SkillshotLine);
            Q2.SetTargetted(0.5f, float.MaxValue);
            R.SetTargetted(0.5f, 1500);

            Config.SubMenu("OW").SubMenu("Mode").AddItem(new MenuItem("OWStarCombo", "Star Combo", true).SetValue(new KeyBind("X".ToCharArray()[0], KeyBindType.Press)));
            Config.SubMenu("OW").SubMenu("Mode").AddItem(new MenuItem("OWInsecCombo", "Insec", true).SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press)));
            Config.SubMenu("OW").SubMenu("Mode").AddItem(new MenuItem("OWKSMob", "Kill Steal Mob", true).SetValue(new KeyBind("Z".ToCharArray()[0], KeyBindType.Press)));
            var ChampMenu = new Menu("Plugin", Name + "Plugin");
            {
                var ComboMenu = new Menu("Combo", "Combo");
                {
                    ItemBool(ComboMenu, "Passive", "Use Passive", false);
                    ItemBool(ComboMenu, "Q", "Use Q");
                    ItemBool(ComboMenu, "W", "Use W");
                    ItemSlider(ComboMenu, "WUnder", "-> If Hp Under", 30);
                    ItemBool(ComboMenu, "E", "Use E");
                    ItemBool(ComboMenu, "R", "Use R If Killable");
                    ItemBool(ComboMenu, "Item", "Use Item");
                    ItemBool(ComboMenu, "Ignite", "Auto Ignite If Killable");
                    ChampMenu.AddSubMenu(ComboMenu);
                }
                var HarassMenu = new Menu("Harass", "Harass");
                {
                    ItemBool(HarassMenu, "Q", "Use Q");
                    ItemSlider(HarassMenu, "Q2Above", "-> Q2 If Hp Above", 20);
                    ItemBool(HarassMenu, "E", "Use E");
                    ItemBool(HarassMenu, "W", "Use W Jump Back");
                    ItemBool(HarassMenu, "WWard", "-> Ward Jump If No Ally Near", false);
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
                var InsecMenu = new Menu("Insec", "Insec");
                {
                    var InsecNearMenu = new Menu("Near Ally Config", "InsecNear");
                    {
                        ItemBool(InsecNearMenu, "ToChamp", "To Champion");
                        ItemSlider(InsecNearMenu, "ToChampHp", "-> If Hp Above", 20);
                        ItemSlider(InsecNearMenu, "ToChampR", "-> If In", 1100, 500, 1600);
                        ItemBool(InsecNearMenu, "DrawToChamp", "-> Draw Range", false);
                        ItemBool(InsecNearMenu, "ToTower", "To Tower");
                        ItemBool(InsecNearMenu, "ToMinion", "To Minion");
                        ItemSlider(InsecNearMenu, "ToMinionR", "-> If In", 1100, 500, 1600);
                        ItemBool(InsecNearMenu, "DrawToMinion", "-> Draw Range", false);
                        InsecMenu.AddSubMenu(InsecNearMenu);
                    }
                    ItemList(InsecMenu, "Mode", "Mode", new[] { "Near Ally", "Selected Ally", "Mouse Position" }, 2);
                    ItemBool(InsecMenu, "Flash", "Flash If Ward Jump Not Ready");
                    ItemBool(InsecMenu, "DrawLine", "Draw Insec Line");
                    ChampMenu.AddSubMenu(InsecMenu);
                }
                var UltiMenu = new Menu("Ultimate", "Ultimate");
                {
                    var KillableMenu = new Menu("Killable", "Killable");
                    {
                        foreach (var Obj in ObjectManager.Get<Obj_AI_Hero>().Where(i => i.IsEnemy)) ItemBool(KillableMenu, Obj.ChampionName, "Use R On " + Obj.ChampionName);
                        UltiMenu.AddSubMenu(KillableMenu);
                    }
                    var InterruptMenu = new Menu("Interrupt", "Interrupt");
                    {
                        foreach (var Obj in ObjectManager.Get<Obj_AI_Hero>().Where(i => i.IsEnemy))
                        {
                            foreach (var Spell in Interrupter.Spells.Where(i => i.ChampionName == Obj.ChampionName)) ItemBool(InterruptMenu, Obj.ChampionName + "_" + Spell.Slot.ToString(), "Spell " + Spell.Slot.ToString() + " Of " + Obj.ChampionName);
                        }
                        UltiMenu.AddSubMenu(InterruptMenu);
                    }
                    ChampMenu.AddSubMenu(UltiMenu);
                }
                var MiscMenu = new Menu("Misc", "Misc");
                {
                    ItemBool(MiscMenu, "WJPink", "Ward Jump Use Pink Ward", false);
                    ItemBool(MiscMenu, "QLastHit", "Use Q To Last Hit", false);
                    ItemBool(MiscMenu, "RInterrupt", "Use R To Interrupt");
                    ItemBool(MiscMenu, "InterruptGap", "-> Ward Jump If No Ally Near");
                    ItemBool(MiscMenu, "WSurvive", "Try Use W To Survive");
                    ItemBool(MiscMenu, "SmiteCol", "Auto Smite Collision");
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
            Game.OnWndProc += OnWndProc;
        }

        private void OnGameUpdate(EventArgs args)
        {
            if (Player.IsDead || MenuGUI.IsChatOpen || Player.IsChannelingImportantSpell() || Player.IsRecalling()) return;
            if (ItemList("Insec", "Mode") == 1)
            {
                if (R.IsReady())
                {
                    allyObj = (allyObj.IsValid && !allyObj.IsDead) ? allyObj : null;
                }
                else if (allyObj != null) allyObj = null;
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
                case Orbwalk.Mode.LastHit:
                    LastHit();
                    break;
                case Orbwalk.Mode.Flee:
                    WardJump(Game.CursorPos);
                    break;
            }
            if (Orbwalk.CurrentMode != Orbwalk.Mode.Harass) CurHarassStage = HarassStage.Nothing;
            if (ItemActive("StarCombo")) StarCombo();
            if (ItemActive("InsecCombo"))
            {
                InsecCombo();
            }
            else InsecJumpCasted = false;
            if (ItemActive("KSMob")) KillStealMob();
            if (ItemBool("Misc", "WSurvive") && W.IsReady() && W.Instance.Name == "BlindMonkWOne") TrySurvive(W.Slot);
        }

        private void OnDraw(EventArgs args)
        {
            if (Player.IsDead) return;
            if (ItemBool("Draw", "Q") && Q.Level > 0) Render.Circle.DrawCircle(Player.Position, Q.Instance.Name == "BlindMonkQOne" ? Q.Range : Q2.Range, Q.IsReady() ? Color.Green : Color.Red, 7);
            if (ItemBool("Draw", "W") && W.Level > 0) Render.Circle.DrawCircle(Player.Position, W.Instance.Name == "BlindMonkWOne" ? W.Range : 0, W.IsReady() ? Color.Green : Color.Red, 7);
            if (ItemBool("Draw", "E") && E.Level > 0) Render.Circle.DrawCircle(Player.Position, E.Instance.Name == "BlindMonkEOne" ? E.Range : E2.Range, E.IsReady() ? Color.Green : Color.Red, 7);
            if (ItemBool("Draw", "R") && R.Level > 0) Render.Circle.DrawCircle(Player.Position, R.Range, R.IsReady() ? Color.Green : Color.Red, 7);
            if (ItemBool("Insec", "DrawLine") && R.IsReady())
            {
                byte validTargets = 0;
                if (targetObj.IsValidTarget())
                {
                    Render.Circle.DrawCircle(targetObj.Position, 70, Color.FromArgb(0, 204, 0), 7);
                    validTargets += 1;
                }
                if (GetInsecPos(true) != default(Vector3))
                {
                    Render.Circle.DrawCircle(GetInsecPos(true), 70, Color.FromArgb(0, 204, 0), 7);
                    validTargets += 1;
                }
                if (validTargets == 2) Drawing.DrawLine(Drawing.WorldToScreen(targetObj.Position), Drawing.WorldToScreen(targetObj.Position.Extend(GetInsecPos(true), 600)), 1, Color.White);
            }
            if (ItemList("Insec", "Mode") == 0 && R.IsReady())
            {
                if (ItemBool("InsecNear", "ToChamp") && ItemBool("InsecNear", "DrawToChamp")) Render.Circle.DrawCircle(Player.Position, ItemSlider("InsecNear", "ToChampR"), Color.White, 7);
                if (ItemBool("InsecNear", "ToMinion") && ItemBool("InsecNear", "DrawToMinion")) Render.Circle.DrawCircle(Player.Position, ItemSlider("InsecNear", "ToMinionR"), Color.White, 7);
            }
        }

        private void OnPossibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            if (!ItemBool("Misc", "RInterrupt") || !R.IsReady() || !ItemBool("Interrupt", (unit as Obj_AI_Hero).ChampionName + "_" + spell.Slot.ToString()) || Player.IsDead) return;
            if (R.InRange(unit)) R.CastOnUnit(unit, PacketCast());
            if (!R.InRange(unit) && W.CanCast(unit) && W.Instance.Name == "BlindMonkWOne")
            {
                var nearObj = ObjectManager.Get<Obj_AI_Base>().Where(i => i.IsValidTarget(W.Range + i.BoundingRadius, false) && i.IsAlly && !i.IsMe && !(i is Obj_AI_Turret) && i.Distance3D(unit) <= R.Range).OrderBy(i => i.Distance3D(unit));
                if (nearObj.Count() > 0 && !JumpCasted)
                {
                    foreach (var Obj in nearObj) W.CastOnUnit(Obj, PacketCast());
                }
                else if (ItemBool("Misc", "InterruptGap") && (GetWardSlot() != null || WardCasted)) WardJump(unit.Position.Randomize(0, (int)R.Range / 2));
            }
        }

        private void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe) return;
            if (args.SData.Name == "BlindMonkQOne")
            {
                QCasted = true;
                Utility.DelayAction.Add(Orbwalk.CurrentMode == Orbwalk.Mode.LaneClear ? 2900 : 2000, () => QCasted = false);
            }
            if (args.SData.Name == "BlindMonkWOne")
            {
                WCasted = true;
                Utility.DelayAction.Add(Orbwalk.CurrentMode == Orbwalk.Mode.LaneClear ? 2900 : 1000, () => WCasted = false);
                JumpCasted = true;
                Utility.DelayAction.Add(1000, () => JumpCasted = false);
            }
            if (args.SData.Name == "BlindMonkEOne")
            {
                ECasted = true;
                Utility.DelayAction.Add(Orbwalk.CurrentMode == Orbwalk.Mode.LaneClear ? 2900 : 2000, () => ECasted = false);
            }
            if (args.SData.Name == "BlindMonkRKick")
            {
                RCasted = true;
                Utility.DelayAction.Add(700, () => RCasted = false);
                if (ItemActive("StarCombo") || ItemActive("InsecCombo"))
                {
                    KickCasted = true;
                    Utility.DelayAction.Add(1000, () => KickCasted = false);
                }
            }
        }

        private void OnWndProc(WndEventArgs args)
        {
            if (args.Msg != (uint)WindowsMessages.WM_LBUTTONDOWN || MenuGUI.IsChatOpen || ItemList("Insec", "Mode") != 1 || !R.IsReady()) return;
            allyObj = null;
            if (Player.IsDead) return;
            foreach (var Obj in ObjectManager.Get<Obj_AI_Base>().Where(i => !i.IsDead && i.IsAlly && !i.IsMe && i.Distance(Game.CursorPos) <= 200).OrderByDescending(i => i.Distance(Game.CursorPos))) allyObj = Obj;
        }

        private void NormalCombo()
        {
            if (!targetObj.IsValidTarget()) return;
            if (ItemBool("Combo", "Passive") && Player.HasBuff("BlindMonkFlurry") && Orbwalk.InAutoAttackRange(targetObj) && Orbwalk.CanAttack()) return;
            if (ItemBool("Combo", "Q") && Q.IsReady())
            {
                if (Q.Instance.Name == "BlindMonkQOne" && Q.InRange(targetObj))
                {
                    CastSkillShotSmite(Q, targetObj);
                }
                else if (targetObj.HasBuff("BlindMonkSonicWave") && Q2.InRange(targetObj) && (Player.Distance3D(targetObj) > Orbwalk.GetAutoAttackRange(Player, targetObj) + 50 || CanKill(targetObj, Q2, 1) || (targetObj.HasBuff("BlindMonkTempest") && E.InRange(targetObj) && !Orbwalk.InAutoAttackRange(targetObj)) || !QCasted)) Q.Cast(PacketCast());
            }
            if (ItemBool("Combo", "E") && E.IsReady())
            {
                if (E.Instance.Name == "BlindMonkEOne" && E.InRange(targetObj))
                {
                    E.Cast(PacketCast());
                }
                else if (targetObj.HasBuff("BlindMonkTempest") && E2.InRange(targetObj) && (Player.Distance3D(targetObj) > Orbwalk.GetAutoAttackRange(Player, targetObj) + 30 || !ECasted)) E.Cast(PacketCast());
            }
            if (ItemBool("Combo", "R") && ItemBool("Killable", targetObj.ChampionName) && R.CanCast(targetObj) && (CanKill(targetObj, R) || (CanKill(targetObj, R, R.GetDamage(targetObj), GetQ2Dmg(targetObj, R.GetDamage(targetObj))) && ItemBool("Combo", "Q") && Q.IsReady() && targetObj.HasBuff("BlindMonkSonicWave")))) R.CastOnUnit(targetObj, PacketCast());
            if (ItemBool("Combo", "W") && W.IsReady())
            {
                if (W.Instance.Name == "BlindMonkWOne")
                {
                    if (Orbwalk.InAutoAttackRange(targetObj) && Player.HealthPercentage() <= ItemList("Combo", "WUnder")) W.Cast(PacketCast());
                }
                else if (E.InRange(targetObj) && !Player.HasBuff("BlindMonkSafeguard") && !WCasted) W.Cast(PacketCast());
            }
            if (ItemBool("Combo", "Item")) UseItem(targetObj);
            if (ItemBool("Combo", "Ignite") && IgniteReady()) CastIgnite(targetObj);
        }

        private void Harass()
        {
            if (!targetObj.IsValidTarget())
            {
                CurHarassStage = HarassStage.Nothing;
                return;
            }
            switch (CurHarassStage)
            {
                case HarassStage.Nothing:
                    CurHarassStage = HarassStage.Doing;
                    break;
                case HarassStage.Doing:
                    if (ItemBool("Harass", "Q") && Q.IsReady())
                    {
                        if (Q.Instance.Name == "BlindMonkQOne" && Q.InRange(targetObj))
                        {
                            CastSkillShotSmite(Q, targetObj);
                        }
                        else if (targetObj.HasBuff("BlindMonkSonicWave") && Q2.InRange(targetObj) && (CanKill(targetObj, Q2, 1) || (W.IsReady() && W.Instance.Name == "BlindMonkWOne" && Player.Mana >= W.Instance.ManaCost + (ItemBool("Harass", "E") && E.IsReady() && E.Instance.Name == "BlindMonkEOne" ? Q.Instance.ManaCost + E.Instance.ManaCost : Q.Instance.ManaCost) && Player.HealthPercentage() >= ItemSlider("Harass", "Q2Above"))))
                        {
                            HarassBackPos = Player.ServerPosition;
                            Q.Cast(PacketCast());
                            Utility.DelayAction.Add((int)((Player.Distance3D(targetObj) + (ItemBool("Harass", "E") && E.IsReady() && E.Instance.Name == "BlindMonkEOne" ? E.Range : 0)) / Q.Speed * 1000 + Q.Delay), () => CurHarassStage = HarassStage.Finish);
                        }
                    }
                    if (ItemBool("Harass", "E") && E.IsReady())
                    {
                        if (E.Instance.Name == "BlindMonkEOne" && E.InRange(targetObj))
                        {
                            E.Cast(PacketCast());
                        }
                        else if (targetObj.HasBuff("BlindMonkTempest") && E2.InRange(targetObj)) CurHarassStage = HarassStage.Finish;
                    }
                    break;
                case HarassStage.Finish:
                    if (ItemBool("Harass", "W") && W.IsReady() && W.Instance.Name == "BlindMonkWOne")
                    {
                        var jumpObj = ObjectManager.Get<Obj_AI_Base>().Where(i => i.IsValidTarget(W.Range + i.BoundingRadius, false, Player.Position) && i.IsAlly && !i.IsMe && !(i is Obj_AI_Turret) && i.Distance3D(targetObj) >= 450).OrderByDescending(i => i.Distance3D(Player)).OrderBy(i => ObjectManager.Get<Obj_AI_Turret>().Where(a => !a.IsDead && a.IsAlly).OrderBy(a => a.Distance3D(Player)).FirstOrDefault().Distance3D(i));
                        if (jumpObj.Count() > 0 && !JumpCasted)
                        {
                            foreach (var Obj in jumpObj) W.CastOnUnit(Obj, PacketCast());
                        }
                        else if (ItemBool("Harass", "WWard") && (GetWardSlot() != null || WardCasted)) WardJump(HarassBackPos);
                    }
                    else
                    {
                        if (HarassBackPos != default(Vector3)) HarassBackPos = default(Vector3);
                        CurHarassStage = HarassStage.Nothing;
                    }
                    break;
            }
        }

        private void LaneJungClear()
        {
            var minionObj = MinionManager.GetMinions(Q2.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth);
            foreach (var Obj in minionObj)
            {
                if (Obj.Team == GameObjectTeam.Neutral && CanSmiteMob(Obj.Name)) CastSmite(Obj);
                var Passive = Player.HasBuff("BlindMonkFlurry");
                if (ItemBool("Clear", "Q") && Q.IsReady())
                {
                    if (Q.Instance.Name == "BlindMonkQOne" && Q.InRange(Obj))
                    {
                        Q.CastIfHitchanceEquals(Obj, HitChance.Medium, PacketCast());
                    }
                    else if (Obj.HasBuff("BlindMonkSonicWave") && (CanKill(Obj, Q2, GetQ2Dmg(Obj)) || Player.Distance3D(Obj) > Orbwalk.GetAutoAttackRange(Player, Obj) + 100 || !QCasted || !Passive)) Q.Cast(PacketCast());
                }
                if (ItemBool("Clear", "E") && E.IsReady())
                {
                    if (E.Instance.Name == "BlindMonkEOne" && !Passive && (minionObj.Count(i => E.InRange(i)) >= 2 || (Obj.MaxHealth >= 1200 && E.InRange(Obj))) && !FarmCasted)
                    {
                        E.Cast(PacketCast());
                        FarmCasted = true;
                        Utility.DelayAction.Add(300, () => FarmCasted = false);
                    }
                    else if (Obj.HasBuff("BlindMonkTempest") && E2.InRange(Obj) && (!ECasted || !Passive)) E.Cast(PacketCast());
                }
                if (ItemBool("Clear", "W") && W.IsReady())
                {
                    if (W.Instance.Name == "BlindMonkWOne")
                    {
                        if (!Passive && Orbwalk.InAutoAttackRange(Obj) && !FarmCasted)
                        {
                            W.Cast(PacketCast());
                            FarmCasted = true;
                            Utility.DelayAction.Add(300, () => FarmCasted = false);
                        }
                    }
                    else if (E.InRange(Obj) && (!WCasted || !Passive)) W.Cast(PacketCast());
                }
                if (ItemBool("Clear", "Item")) UseItem(Obj, true);
            }
        }

        private void LastHit()
        {
            if (!ItemBool("Misc", "QLastHit") || !Q.IsReady() || Q.Instance.Name != "BlindMonkQOne") return;
            foreach (var Obj in MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.NotAlly).Where(i => CanKill(i, Q)).OrderByDescending(i => i.Distance3D(Player))) Q.CastIfHitchanceEquals(Obj, HitChance.VeryHigh, PacketCast());
        }

        private bool WardJump(Vector3 Pos)
        {
            if (!W.IsReady() || W.Instance.Name != "BlindMonkWOne" || JumpCasted) return false;
            bool Casted = false;
            var JumpPos = Pos;
            if (GetWardSlot() != null && !WardCasted && Player.Distance(Pos) > GetWardRange()) JumpPos = Player.Position.Extend(Pos, GetWardRange());
            foreach (var Obj in ObjectManager.Get<Obj_AI_Base>().Where(i => i.IsValidTarget(W.Range + i.BoundingRadius, false) && i.IsAlly && !i.IsMe && !(i is Obj_AI_Turret) && i.Distance(WardCasted ? WardPlacePos : JumpPos) < 200 && (!ItemActive("InsecCombo") || (ItemActive("InsecCombo") && i.Name.EndsWith("Ward") && i is Obj_AI_Minion))).OrderBy(i => i.Distance(WardCasted ? WardPlacePos : JumpPos)))
            {
                W.CastOnUnit(Obj, PacketCast());
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

        private void StarCombo()
        {
            CustomOrbwalk(targetObj);
            if (!targetObj.IsValidTarget()) return;
            UseItem(targetObj);
            if (Q.IsReady())
            {
                if (Q.Instance.Name == "BlindMonkQOne" && Q.InRange(targetObj))
                {
                    CastSkillShotSmite(Q, targetObj);
                }
                else if (targetObj.HasBuff("BlindMonkSonicWave") && Q2.InRange(targetObj) && (CanKill(targetObj, Q2, 1) || (!R.IsReady() && !RCasted && KickCasted) || (!R.IsReady() && !RCasted && !KickCasted && (Player.Distance3D(targetObj) > Orbwalk.GetAutoAttackRange(Player, targetObj) + 100 || !QCasted)))) Q.Cast(PacketCast());
            }
            if (W.IsReady())
            {
                if (W.Instance.Name == "BlindMonkWOne")
                {
                    if (R.IsReady())
                    {
                        if (Q.IsReady() && targetObj.HasBuff("BlindMonkSonicWave") && !R.InRange(targetObj) && Player.Distance3D(targetObj) < W.Range + R.Range - 200) WardJump(targetObj.Position.Randomize(0, (int)R.Range / 2));
                    }
                    else if (Orbwalk.InAutoAttackRange(targetObj)) W.Cast(PacketCast());
                }
                else if (E.InRange(targetObj) && !Player.HasBuff("BlindMonkSafeguard") && !WCasted) W.Cast(PacketCast());
            }
            if (R.CanCast(targetObj) && Q.IsReady() && targetObj.HasBuff("BlindMonkSonicWave")) R.CastOnUnit(targetObj, PacketCast());
            if (E.IsReady())
            {
                if (E.Instance.Name == "BlindMonkEOne" && E.InRange(targetObj))
                {
                    E.Cast(PacketCast());
                }
                else if (targetObj.HasBuff("BlindMonkTempest") && E2.InRange(targetObj) && (Player.Distance3D(targetObj) > Orbwalk.GetAutoAttackRange(Player, targetObj) + 30 || !ECasted)) E.Cast(PacketCast());
            }
        }

        private void InsecCombo()
        {
            CustomOrbwalk(targetObj);
            if (!targetObj.IsValidTarget()) return;
            if (GetInsecPos() != default(Vector3))
            {
                if (R.CanCast(targetObj) && Player.Distance(GetInsecPos()) < 200)
                {
                    R.CastOnUnit(targetObj, PacketCast());
                    return;
                }
                else if (W.IsReady() && W.Instance.Name == "BlindMonkWOne" && (GetWardSlot() != null || WardCasted) && Player.Position.Distance(GetInsecPos()) < GetWardRange())
                {
                    if (WardJump(GetInsecPos())) Utility.DelayAction.Add(50, () => R.CastOnUnit(targetObj, PacketCast()));
                    if (ItemBool("Insec", "Flash")) InsecJumpCasted = true;
                    return;
                }
                else if (ItemBool("Insec", "Flash") && FlashReady() && !InsecJumpCasted && !WardCasted && Player.Position.Distance(GetInsecPos()) < 400)
                {
                    if (CastFlash(GetInsecPos())) Utility.DelayAction.Add(50, () => R.CastOnUnit(targetObj, PacketCast()));
                    return;
                }
            }
            if (Q.IsReady())
            {
                if (Q.Instance.Name == "BlindMonkQOne")
                {
                    if (Q.InRange(targetObj) && Q.GetPrediction(targetObj).Hitchance >= HitChance.Low)
                    {
                        CastSkillShotSmite(Q, targetObj);
                    }
                    else if (GetInsecPos() != default(Vector3) && Q.GetPrediction(targetObj).Hitchance == HitChance.Collision)
                    {
                        foreach (var Obj in Q.GetPrediction(targetObj, true).CollisionObjects.Where(i => i.Position.Distance(GetInsecPos()) < ((ItemBool("Insec", "Flash") && FlashReady() && !InsecJumpCasted && (!W.IsReady() || W.Instance.Name != "BlindMonkWOne" || GetWardSlot() == null || !WardCasted)) ? 400 : GetWardRange()) && !CanKill(i, Q)).OrderBy(i => i.Position.Distance(GetInsecPos()))) Q.CastIfHitchanceEquals(Obj, HitChance.VeryHigh, PacketCast());
                    }
                }
                else if (targetObj.HasBuff("BlindMonkSonicWave") && Q2.InRange(targetObj) && (CanKill(targetObj, Q2, 1) || (!R.IsReady() && !RCasted && KickCasted) || (!R.IsReady() && !RCasted && !KickCasted && (Player.Distance3D(targetObj) > Orbwalk.GetAutoAttackRange(Player, targetObj) + 100 || !QCasted)) || (GetInsecPos() != default(Vector3) && Player.Position.Distance(GetInsecPos()) > ((ItemBool("Insec", "Flash") && FlashReady() && !InsecJumpCasted && (!W.IsReady() || W.Instance.Name != "BlindMonkWOne" || GetWardSlot() == null || !WardCasted)) ? 400 : GetWardRange()))))
                {
                    Q.Cast(PacketCast());
                }
                else if (GetInsecPos() != default(Vector3) && ObjectManager.Get<Obj_AI_Base>().Any(i => i.HasBuff("BlindMonkSonicWave") && i.IsValidTarget(Q2.Range) && i.Position.Distance(GetInsecPos()) < ((ItemBool("Insec", "Flash") && FlashReady() && !InsecJumpCasted && (!W.IsReady() || W.Instance.Name != "BlindMonkWOne" || GetWardSlot() == null || !WardCasted)) ? 400 : GetWardRange()))) Q.Cast(PacketCast());
            }
        }

        private void KillStealMob()
        {
            var Mob = MinionManager.GetMinions(Q2.Range, MinionTypes.All, MinionTeam.Neutral).FirstOrDefault(i => new string[] { "SRU_Baron", "SRU_Dragon", "SRU_Blue", "SRU_Red" }.Any(a => i.Name.StartsWith(a) && !i.Name.StartsWith(a + "Mini")));
            CustomOrbwalk(Mob);
            if (Mob == null) return;
            if (SmiteReady()) CastSmite(Mob);
            if (Q.IsReady())
            {
                if (Q.Instance.Name == "BlindMonkQOne")
                {
                    if (Q.InRange(Mob) && CanKill(Mob, Q, Q.GetDamage(Mob) + (SmiteReady() ? Player.GetSummonerSpellDamage(Mob, Damage.SummonerSpell.Smite) : 0), GetQ2Dmg(Mob, Q.GetDamage(Mob) + (SmiteReady() ? Player.GetSummonerSpellDamage(Mob, Damage.SummonerSpell.Smite) : 0))) && Q.GetPrediction(Mob).Hitchance >= HitChance.VeryHigh)
                    {
                        Q.CastIfHitchanceEquals(Mob, HitChance.VeryHigh, PacketCast());
                    }
                    else if (SmiteReady() && CanKill(Mob, Q2, Player.GetSummonerSpellDamage(Mob, Damage.SummonerSpell.Smite)) && Q.GetPrediction(Mob).Hitchance <= HitChance.OutOfRange)
                    {
                        foreach (var Obj in Q.GetPrediction(Mob, true).CollisionObjects.Where(i => i.Distance3D(Mob) <= 760 && !CanKill(i, Q)).OrderBy(i => i.Distance3D(Mob))) Q.CastIfHitchanceEquals(Obj, HitChance.VeryHigh, PacketCast());
                    }
                }
                else if (Mob.HasBuff("BlindMonkSonicWave") && CanKill(Mob, Q2, SmiteReady() ? Player.GetSummonerSpellDamage(Mob, Damage.SummonerSpell.Smite) : 0, GetQ2Dmg(Mob, SmiteReady() ? Player.GetSummonerSpellDamage(Mob, Damage.SummonerSpell.Smite) : 0)))
                {
                    Q.Cast(PacketCast());
                    if (SmiteReady()) Utility.DelayAction.Add((int)((Player.Distance3D(Mob) - 200) / Q.Speed * 1000 + Q.Delay), () => CastSmite(Mob, false));
                }
                else if (ObjectManager.Get<Obj_AI_Base>().Any(i => i.HasBuff("BlindMonkSonicWave") && i.IsValidTarget(Q2.Range) && i.Distance3D(Mob) <= 760) && SmiteReady() && CanKill(Mob, Q2, Player.GetSummonerSpellDamage(Mob, Damage.SummonerSpell.Smite)))
                {
                    Q.Cast(PacketCast());
                    Utility.DelayAction.Add((int)((Player.Distance3D(Mob) - 760) / Q.Speed * 1000 + Q.Delay), () => CastSmite(Mob));
                }
            }
        }

        private Vector3 GetInsecPos(bool IsDraw = false)
        {
            if (!R.IsReady()) return default(Vector3);
            switch (ItemList("Insec", "Mode"))
            {
                case 0:
                    var ChampList = ObjectManager.Get<Obj_AI_Hero>().Where(i => i.IsValidTarget(ItemSlider("InsecNear", "ToChampR"), false) && i.IsAlly && !i.IsMe && i.HealthPercentage() >= ItemSlider("InsecNear", "ToChampHp"));
                    var TowerObj = ObjectManager.Get<Obj_AI_Turret>().Where(i => !i.IsDead && i.IsAlly).OrderBy(i => Player.Distance3D(i)).FirstOrDefault();
                    var MinionObj = targetObj.IsValidTarget() ? ObjectManager.Get<Obj_AI_Minion>().Where(i => i.IsValidTarget(ItemSlider("InsecNear", "ToMinionR"), false) && i.IsAlly && Player.Distance3D(TowerObj) > 1500 && i.Distance3D(targetObj) > 600 && !i.Name.EndsWith("Ward")).OrderByDescending(i => i.Distance3D(targetObj)).OrderBy(i => i.Distance3D(TowerObj)).FirstOrDefault() : null;
                    if (ChampList.Count() > 0 && ItemBool("InsecNear", "ToChamp"))
                    {
                        var Pos = default(Vector3);
                        foreach (var Obj in ChampList) Pos += Obj.Position;
                        Pos = new Vector2(Pos.X / ChampList.Count(), Pos.Y / ChampList.Count()).To3D();
                        return IsDraw ? Pos : targetObj.Position.Extend(Pos, -230);
                    }
                    if (MinionObj != null && ItemBool("InsecNear", "ToMinion")) return IsDraw ? MinionObj.Position : targetObj.Position.Extend(MinionObj.Position, -230);
                    if (TowerObj != null && ItemBool("InsecNear", "ToTower")) return IsDraw ? TowerObj.Position : targetObj.Position.Extend(TowerObj.Position, -230);
                    break;
                case 1:
                    if (allyObj != null) return IsDraw ? allyObj.Position : targetObj.Position.Extend(allyObj.Position, -230);
                    break;
                case 2:
                    return IsDraw ? Game.CursorPos : targetObj.Position.Extend(Game.CursorPos, -230);
            }
            return default(Vector3);
        }

        private void UseItem(Obj_AI_Base Target, bool IsFarm = false)
        {
            if (Bilgewater.IsReady() && !IsFarm) Bilgewater.Cast(Target);
            if (BladeRuined.IsReady() && !IsFarm) BladeRuined.Cast(Target);
            if (Tiamat.IsReady() && IsFarm ? Player.Distance3D(Target) <= Tiamat.Range : Player.CountEnemysInRange((int)Tiamat.Range) >= 1) Tiamat.Cast();
            if (Hydra.IsReady() && IsFarm ? Player.Distance3D(Target) <= Hydra.Range : (Player.CountEnemysInRange((int)Hydra.Range) >= 2 || (Player.GetAutoAttackDamage(Target, true) < Target.Health && Player.CountEnemysInRange((int)Hydra.Range) == 1))) Hydra.Cast();
            if (RanduinOmen.IsReady() && Player.CountEnemysInRange((int)RanduinOmen.Range) >= 1 && !IsFarm) RanduinOmen.Cast();
        }

        private double GetQ2Dmg(Obj_AI_Base Target, double Plus = 0)
        {
            var Dmg = Player.CalcDamage(Target, Damage.DamageType.Physical, new double[] { 50, 80, 110, 140, 170 }[Q.Level - 1] + 0.9 * Player.FlatPhysicalDamageMod + 0.08 * (Target.MaxHealth - Target.Health + Plus));
            return Target is Obj_AI_Minion && Dmg > 400 ? Player.CalcDamage(Target, Damage.DamageType.Physical, 400) : Dmg;
        }
    }
}