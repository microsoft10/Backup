// This file is part of LeagueSharp.Common.
// 
// LeagueSharp.Common is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// LeagueSharp.Common is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with LeagueSharp.Common.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace FuckingAwesomeLeeSin
{
    internal static class Program
    {
        #region Params

        private const string ChampName = "LeeSin";
        private static Orbwalking.Orbwalker Orbwalker;

        private static readonly Obj_AI_Hero Player = ObjectManager.Player;
        // Instead of typing ObjectManager.Player you can just type Player

        public static Spell Q, W, E, R, R2;
        public static Spellbook SBook;
        public static Items.Item Dfg;
        public static Vector2 JumpPos;
        public static Vector3 mouse = Game.CursorPos;
        public static SpellSlot smiteSlot;
        public static SpellSlot flashSlot;
        public static Menu Menu;
        public static bool CastQAgain;
        public static bool CastWardAgain = true;
        public static bool reCheckWard = true;
        public static bool wardJumped;
        public static Obj_AI_Base minionerimo;
        public static bool checkSmite;
        public static bool delayW;
        public static Vector2 insecLinePos;
        public static float TimeOffset;
        public static Vector3 lastWardPos;
        public static float lastPlaced;
        public static int passiveStacks;
        public static float passiveTimer;
        public static bool waitforjungle;
        public static bool waitingForQ2;
        public static bool q2Done;
        public static float q2Timer;
        public static int clickCount;
        public static Vector3 insecClickPos;
        public static float resetTime;
        public static bool clicksecEnabled;
        public static float doubleClickReset;
        public static Vector3 lastClickPos;
        public static bool lastClickBool;


        public static bool textRendered;
        private static readonly string[] epics = { "SRU_Baron", "SRU_Dragon" };
        private static readonly string[] buffs = { "SRU_Red", "SRU_Blue" };
        private static readonly string[] buffandepics = { "SRU_Red", "SRU_Blue", "SRU_Dragon", "SRU_Baron" };

        private static readonly string[] bigjungleminions =
        {
            "SRU_Red", "SRU_Blue", "SRU_Dragon", "SRU_Baron",
            "SRU_Murkwolf", "SRU_Razorbeak", "SRU_Gromp", "SRU_Krug"
        };

        private static readonly string[] spells =
        {
            "BlindMonkQOne", "BlindMonkWOne", "BlindMonkEOne", "blindmonkwtwo",
            "blindmonkqtwo", "blindmonketwo", "BlindMonkRKick"
        };

        // ReSharper disable once UnusedParameter.Local
        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnWndProc(WndEventArgs args)
        {
            if (args.Msg != (uint) WindowsMessages.WM_LBUTTONDOWN || !ParamBool("clickInsec"))
            {
                return;
            }
            var asec =
                ObjectManager.Get<Obj_AI_Hero>()
                    .Where(a => a.IsEnemy && a.Distance(Game.CursorPos) < 200 && a.IsValid && !a.IsDead);
            if (asec.Any())
            {
                return;
            }
            if (!lastClickBool || clickCount == 0)
            {
                clickCount++;
                lastClickPos = Game.CursorPos;
                lastClickBool = true;
                doubleClickReset = Environment.TickCount + 600;
                return;
            }
            if (lastClickBool && lastClickPos.Distance(Game.CursorPos) < 200)
            {
                clickCount++;
                lastClickBool = false;
            }
        }

        private static void Orbwalking_AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (unit.IsMe && passiveStacks > 0)
            {
                passiveStacks = passiveStacks - 1;
            }
        }

        public static SpellSlot IgniteSlot;

        #endregion

        public static void GameObject_OnDelete(GameObject sender, EventArgs args)
        {
            if (!(sender is Obj_GeneralParticleEmitter))
            {
                return;
            }
            if (sender.Name.Contains("blindMonk_Q_resonatingStrike") && waitingForQ2)
            {
                waitingForQ2 = false;
                q2Done = true;
                q2Timer = Environment.TickCount + 800;
            }
        }

        #region OnLoad

        private static void Game_OnGameLoad(EventArgs args)
        {
            if (Player.ChampionName != ChampName)
            {
                return;
            }
            IgniteSlot = Player.GetSpellSlot("SummonerDot");
            flashSlot = Player.GetSpellSlot("summonerflash");

            Q = new Spell(SpellSlot.Q, 1000);
            W = new Spell(SpellSlot.W, 700);
            E = new Spell(SpellSlot.E, 340);
            R = new Spell(SpellSlot.R, 375);
			 R2 = new Spell(SpellSlot.R, 800);

            /* Q.SetSkillshot(
                Q.Instance.SData.SpellCastTime, Q.Instance.SData.LineWidth, Q.Instance.SData.MissileSpeed, true,
                SkillshotType.SkillshotLine); */
				Q.SetSkillshot(0.25f, 65, 1800, true, SkillshotType.SkillshotLine);
				 R2.SetSkillshot(0.25f, 100, 1500, false, SkillshotType.SkillshotLine);

            //Base menu
            Menu = new Menu("FALeeSin", ChampName, true);
            //Orbwalker and menu
            Menu.AddSubMenu(new Menu("Orbwalker", "Orbwalker"));
            Orbwalker = new Orbwalking.Orbwalker(Menu.SubMenu("Orbwalker"));
            //Target selector and menu
            var ts = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(ts);
            Menu.AddSubMenu(ts);
            //Combo menu
            Menu.AddSubMenu(new Menu("Combo", "Combo"));
            Menu.SubMenu("Combo").AddItem(new MenuItem("useQ", "Use Q").SetValue(true));
            Menu.SubMenu("Combo").AddItem(new MenuItem("useQ2", "Use Q2").SetValue(true));
			Menu.SubMenu("Combo").AddItem(new MenuItem("Wcombo", "%HP W combo").SetValue(new Slider(40, 0, 100)));
			/* Menu.SubMenu("Combo").AddItem(new MenuItem("Wsave", "%HP W save").SetValue(new Slider(15, 0, 100))); */
            Menu.SubMenu("Combo").AddItem(new MenuItem("useW", "Wardjump in combo").SetValue(false));
            Menu.SubMenu("Combo").AddItem(new MenuItem("dsjk", "Wardjump if: "));
            Menu.SubMenu("Combo").AddItem(new MenuItem("wMode", "> Q Range").SetValue(true));
            Menu.SubMenu("Combo").AddItem(new MenuItem("useE", "Use E").SetValue(true));
            Menu.SubMenu("Combo").AddItem(new MenuItem("useR", "Use R").SetValue(false));
			Menu.SubMenu("Combo").AddItem(new MenuItem("RCount", "-> Hit Bonus Enemy >= (5 off)").SetValue(new Slider( 2, 1, 5)));
            Menu.SubMenu("Combo").AddItem(new MenuItem("ksR", "KS R").SetValue(false));
            Menu.SubMenu("Combo")
                .AddItem(
                    new MenuItem("starCombo", "Star Combo").SetValue(
                        new KeyBind("T".ToCharArray()[0], KeyBindType.Press)));
            Menu.SubMenu("Combo").AddItem(new MenuItem("random2ejwej", "W->Q->R->Q2"));
            Menu.SubMenu("Combo").AddItem(new MenuItem("aaStacks", "Wait for Passive").SetValue(false));

            var harassMenu = new Menu("Harass", "Harass");
            harassMenu.AddItem(new MenuItem("q1H", "Use Q1").SetValue(true));
            harassMenu.AddItem(new MenuItem("q2H", "Use Q2").SetValue(true));
			harassMenu.AddItem(new MenuItem("q2hp", "%HP Q2").SetValue(new Slider(25, 0, 100)));
            harassMenu.AddItem(new MenuItem("wH", "Wardjump/Minion Jump away").SetValue(true));
            harassMenu.AddItem(new MenuItem("eH", "Use E1").SetValue(false));
            Menu.AddSubMenu(harassMenu);

            //Jung/Wave Clear
            var waveclearMenu = new Menu("Wave/Jung Clear", "wjClear");
            waveclearMenu.AddItem(new MenuItem("sjasjsdsjs", "WaveClear"));
            waveclearMenu.AddItem(new MenuItem("useQClear", "Use Q").SetValue(true));
            waveclearMenu.AddItem(new MenuItem("useEClear", "Use E").SetValue(true));
            waveclearMenu.AddItem(new MenuItem("sjasjjs", "Jungle"));
            waveclearMenu.AddItem(
                new MenuItem("jungActive", "Jungle Clear Active").SetValue(
                    new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));
			waveclearMenu.AddItem(new MenuItem("Passive", "Ignore Stack PS").SetValue(new Slider(0, 0, 2)));
            waveclearMenu.AddItem(new MenuItem("Qjng", "Use Q").SetValue(true));
            waveclearMenu.AddItem(new MenuItem("Wjng", "Use W").SetValue(true));
            waveclearMenu.AddItem(new MenuItem("Ejng", "Use E").SetValue(true));
            Menu.AddSubMenu(waveclearMenu);

            //InsecMenu
            var insecMenu = new Menu("Insec", "Insec");
            insecMenu.AddItem(
                new MenuItem("InsecEnabled", "Enabled").SetValue(new KeyBind("Y".ToCharArray()[0], KeyBindType.Press)));
            insecMenu.AddItem(new MenuItem("rnshsasdhjk", "Insec Mode:"));
            insecMenu.AddItem(new MenuItem("insecMode", "Left Click [on] TS [off]").SetValue(true));
            insecMenu.AddItem(new MenuItem("insecOrbwalk", "Orbwalking").SetValue(true));
            insecMenu.AddItem(new MenuItem("flashInsec", "Flash insec").SetValue(false));
            insecMenu.AddItem(new MenuItem("waitForQBuff", "Wait For Q Buff to go").SetValue(false));
            insecMenu.AddItem(new MenuItem("22222222222222", "(Faster off more dmg on)"));
            insecMenu.AddItem(new MenuItem("clickInsec", "Click Insec").SetValue(true));
            var lM = insecMenu.AddSubMenu(new Menu("Click Insec Instructions", "clickInstruct"));
            lM.AddItem(new MenuItem("1223342334", "Firstly Click the point you want to"));
            lM.AddItem(new MenuItem("122334233", "Two Times. Then Click your target and insec"));
            insecMenu.AddItem(new MenuItem("insec2champs", "Insec to allies").SetValue(true));
            insecMenu.AddItem(new MenuItem("bonusRangeA", "Ally Bonus Range").SetValue(new Slider(1000, 0, 1000)));
            insecMenu.AddItem(new MenuItem("insec2tower", "Insec to towers").SetValue(true));
            insecMenu.AddItem(new MenuItem("bonusRangeT", "Towers Bonus Range").SetValue(new Slider(1000, 0, 1000)));
            insecMenu.AddItem(new MenuItem("insec2orig", "Insec to original pos").SetValue(true));
            insecMenu.AddItem(new MenuItem("22222222222", "--"));
            insecMenu.AddItem(new MenuItem("instaFlashInsec1", "Cast R Manually"));
            insecMenu.AddItem(new MenuItem("instaFlashInsec2", "And it will flash to insec pos"));
            insecMenu.AddItem(
                new MenuItem("instaFlashInsec", "Enabled").SetValue(
                    new KeyBind("P".ToCharArray()[0], KeyBindType.Toggle)));
            Menu.AddSubMenu(insecMenu);

            var autoSmiteSettings = new Menu("Smite Settings", "Auto Smite Settings");
            autoSmiteSettings.AddItem(
                new MenuItem("smiteEnabled", "Enabled").SetValue(new KeyBind("M".ToCharArray()[0], KeyBindType.Toggle)));
            var itemSelMenu = autoSmiteSettings.AddSubMenu(new Menu("Selected Smite Targets", "sst"));
            itemSelMenu.AddItem(new MenuItem("SRU_Red", "Red Buff").SetValue(true));
            itemSelMenu.AddItem(new MenuItem("SRU_Blue", "Blue Buff").SetValue(true));
            itemSelMenu.AddItem(new MenuItem("SRU_Dragon", "Dragon").SetValue(true));
            itemSelMenu.AddItem(new MenuItem("SRU_Baron", "B'ron").SetValue(true));
            autoSmiteSettings.AddItem(new MenuItem("qqSmite", "Q->Smite->Q").SetValue(true));
            autoSmiteSettings.AddItem(new MenuItem("normSmite", "Normal Smite").SetValue(true));
            autoSmiteSettings.AddItem(new MenuItem("drawSmite", "Draw Smite Range").SetValue(true));
            Menu.AddSubMenu(autoSmiteSettings);

            //SaveMe Menu
            var SaveMeMenu = new Menu("Smite Save Settings", "Smite Save Settings");
            SaveMeMenu.AddItem(new MenuItem("smiteSave", "Smite Save Active").SetValue(true));
            SaveMeMenu.AddItem(new MenuItem("hpPercentSM", "WWSmite on x%").SetValue(new Slider(10, 1)));
            SaveMeMenu.AddItem(new MenuItem("param1", "Dont Smite if near and hp = x%")); // TBC
            SaveMeMenu.AddItem(new MenuItem("dBuffs", "Buffs").SetValue(true)); // TBC
            SaveMeMenu.AddItem(new MenuItem("hpBuffs", "HP %").SetValue(new Slider(30, 1))); // TBC
            SaveMeMenu.AddItem(new MenuItem("dEpics", "Epics").SetValue(true)); // TBC
            SaveMeMenu.AddItem(new MenuItem("hpEpics", "HP %").SetValue(new Slider(10, 1))); // TBC
            Menu.AddSubMenu(SaveMeMenu);
            //Wardjump menu
            var wardjumpMenu = new Menu("Wardjump", "Wardjump");
            wardjumpMenu.AddItem(
                new MenuItem("wjump", "Wardjump key").SetValue(new KeyBind("G".ToCharArray()[0], KeyBindType.Press)));
            wardjumpMenu.AddItem(new MenuItem("m2m", "Move to mouse").SetValue(true));
            wardjumpMenu.AddItem(new MenuItem("j2m", "Jump to minions").SetValue(true));
            wardjumpMenu.AddItem(new MenuItem("j2c", "Jump to champions").SetValue(true));
            Menu.AddSubMenu(wardjumpMenu);

            var drawMenu = new Menu("Drawing", "Drawing");
            drawMenu.AddItem(new MenuItem("DrawEnabled", "Draw Enabled").SetValue(false));
            drawMenu.AddItem(new MenuItem("drawST", "Draw Smite Text").SetValue(true));
            drawMenu.AddItem(new MenuItem("drawOutLineST", "Draw Outline").SetValue(true));
            drawMenu.AddItem(new MenuItem("insecDraw", "Draw INSEC").SetValue(true));
            drawMenu.AddItem(new MenuItem("WJDraw", "Draw WardJump").SetValue(true));
            drawMenu.AddItem(new MenuItem("drawQ", "Draw Q").SetValue(true));
            drawMenu.AddItem(new MenuItem("drawW", "Draw W").SetValue(true));
            drawMenu.AddItem(new MenuItem("drawE", "Draw E").SetValue(true));
            drawMenu.AddItem(new MenuItem("drawR", "Draw R").SetValue(true));
            Menu.AddSubMenu(drawMenu);

            var miscMenu = new Menu("Misc", "Misc");
            miscMenu.AddItem(new MenuItem("NFE", "Use Packets?").SetValue(true));
            miscMenu.AddItem(
                new MenuItem("QHC", "Q Hitchance").SetValue(
                    new StringList(new[] { "LOW", "MEDIUM", "HIGH", "VERY HIGH" }, 1)));
            miscMenu.AddItem(new MenuItem("IGNks", "Use Ignite?").SetValue(true));
            miscMenu.AddItem(new MenuItem("qSmite", "Smite Q!").SetValue(true));
            Menu.AddSubMenu(miscMenu);

            Menu.AddToMainMenu();

            Drawing.OnDraw += Drawing_OnDraw; // Add onDraw
            Game.OnUpdate += Game_OnGameUpdate; // adds OnGameUpdate (Same as onTick in bol)
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            GameObject.OnCreate += GameObject_OnCreate;
            Orbwalking.AfterAttack += Orbwalking_AfterAttack;
            GameObject.OnDelete += GameObject_OnDelete;
            Game.OnWndProc += Game_OnWndProc;
			CustomEvents.Unit.OnDash += Unit_OnDash;

            PrintMessage("Loaded!");
        }

        #endregion

        #region Harass

        public static void Harass()
        {
            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
            var q = ParamBool("q1H");
            var q2 = ParamBool("q2H");
            var e = ParamBool("eH");
            var w = ParamBool("wH");
			var abc =
                    ObjectManager.Get<Obj_AI_Minion>()
                        .Where(a => a.IsAlly && a.Distance(target) <= W.Range)
                        .OrderByDescending(a => a.Distance(target))
                        .FirstOrDefault();

            if (q && Q.IsReady() && Q.Instance.Name == "BlindMonkQOne" && target.IsValidTarget(Q.Range) && q)
            {
                CastQ1(target);
            }
            if (q2 && Q.IsReady() &&
                (target.HasBuff("BlindMonkQOne", true) || target.HasBuff("blindmonkqonechaos", true)) && abc != null && !UnderTower(target.ServerPosition) && Player.HealthPercent > Menu.Item("q2hp").GetValue<Slider>().Value && W.IsReady() && q2)
            {
                if (CastQAgain || !target.IsValidTarget(Orbwalking.GetRealAutoAttackRange(Player)))
                {
                    Q.Cast();
                }
            }
            if (e && E.IsReady() && target.IsValidTarget(E.Range) && E.Instance.Name == "BlindMonkEOne" && e)
            {
                E.Cast();
            }
            if (w && Player.Distance(target) < 150 &&
                !(target.HasBuff("BlindMonkQOne", true) && !target.HasBuff("blindmonkqonechaos", true)) &&
                (E.Instance.Name == "blindmonketwo" || !E.IsReady() && e) &&
                (Q.Instance.Name == "blindmonkqtwo" || !Q.IsReady() && q))
            {
                var min =
                    ObjectManager.Get<Obj_AI_Minion>()
                        .Where(a => a.IsAlly && a.Distance(Player) <= W.Range)
                        .OrderByDescending(a => a.Distance(target))
                        .FirstOrDefault();
                W.CastOnUnit(min);
            }
        }

        #endregion

        #region Insec

        public static bool isNullInsecPos = true;
        public static Vector3 insecPos;

        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe)
            {
                return;
            }
            if (spells.Contains(args.SData.Name))
            {
                passiveStacks = 2;
                passiveTimer = Environment.TickCount + 3000;
            }
            if (args.SData.Name == "BlindMonkQOne")
            {
                CastQAgain = false;
                Utility.DelayAction.Add(2900, () => { CastQAgain = true; });
            }
            if (Menu.Item("instaFlashInsec").GetValue<KeyBind>().Active && args.SData.Name == "BlindMonkRKick")
            {
                Player.Spellbook.CastSpell(flashSlot, getInsecPos((Obj_AI_Hero) (args.Target)));
            }
            if (args.SData.Name == "summonerflash" && InsecComboStep != InsecComboStepSelect.NONE)
            {
                Obj_AI_Hero target = ParamBool("insecMode")
                    ? TargetSelector.GetSelectedTarget()
                    : TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
                InsecComboStep = InsecComboStepSelect.PRESSR;
                Utility.DelayAction.Add(80, () => R.CastOnUnit(target, true));
            }
            if (args.SData.Name == "blindmonkqtwo")
            {
                waitingForQ2 = true;
                Utility.DelayAction.Add(3000, () => { waitingForQ2 = false; });
            }
            if (args.SData.Name == "BlindMonkRKick")
            {
                InsecComboStep = InsecComboStepSelect.NONE;
            }
        }

        public static Vector3 getInsecPos(Obj_AI_Hero target)
        {
            if (clicksecEnabled && ParamBool("clickInsec"))
            {
                insecLinePos = Drawing.WorldToScreen(insecClickPos);
                return V2E(insecClickPos, target.Position, target.Distance(insecClickPos) + 230).To3D();
            }
            if (isNullInsecPos)
            {
                isNullInsecPos = false;
                insecPos = Player.Position;
            }
            var turrets = (from tower in ObjectManager.Get<Obj_Turret>()
                where
                    tower.IsAlly && !tower.IsDead &&
                    target.Distance(tower.Position) < 1500 + Menu.Item("bonusRangeT").GetValue<Slider>().Value &&
                    tower.Health > 0
                select tower).ToList();
            if (GetAllyHeroes(target, 2000 + Menu.Item("bonusRangeA").GetValue<Slider>().Value).Count > 0 &&
                ParamBool("insec2champs"))
            {
                Vector3 insecPosition =
                    InterceptionPoint(
                        GetAllyInsec(GetAllyHeroes(target, 2000 + Menu.Item("bonusRangeA").GetValue<Slider>().Value)));
                insecLinePos = Drawing.WorldToScreen(insecPosition);
                return V2E(insecPosition, target.Position, target.Distance(insecPosition) + 230).To3D();
            }
            if (turrets.Any() && ParamBool("insec2tower"))
            {
                insecLinePos = Drawing.WorldToScreen(turrets[0].Position);
                return V2E(turrets[0].Position, target.Position, target.Distance(turrets[0].Position) + 230).To3D();
            }
            if (ParamBool("insec2orig"))
            {
                insecLinePos = Drawing.WorldToScreen(insecPos);
                return V2E(insecPos, target.Position, target.Distance(insecPos) + 230).To3D();
            }
            return new Vector3();
        }

        private enum InsecComboStepSelect
        {
            NONE,
            QGAPCLOSE,
            WGAPCLOSE,
            PRESSR
        };

        private static InsecComboStepSelect InsecComboStep;

        private static void InsecCombo(Obj_AI_Hero target)
        {
            if (target != null && target.IsVisible)
            {
                if (Player.Distance(getInsecPos(target)) < 200)
                {
                    InsecComboStep = InsecComboStepSelect.PRESSR;
                }
                else if (InsecComboStep == InsecComboStepSelect.NONE &&
                         getInsecPos(target).Distance(Player.Position) < 600)
                {
                    InsecComboStep = InsecComboStepSelect.WGAPCLOSE;
                }
                else if (InsecComboStep == InsecComboStepSelect.NONE && target.Distance(Player) < Q.Range)
                {
                    InsecComboStep = InsecComboStepSelect.QGAPCLOSE;
                }

                switch (InsecComboStep)
                {
                    case InsecComboStepSelect.QGAPCLOSE:
                        if (!(target.HasBuff("BlindMonkQOne", true) || target.HasBuff("blindmonkqonechaos", true)) &&
                            Q.Instance.Name == "BlindMonkQOne")
                        {
                            CastQ1(target);
                        }
                        else if ((target.HasBuff("BlindMonkQOne", true) || target.HasBuff("blindmonkqonechaos", true)))
                        {
                            Q.Cast();
                            InsecComboStep = InsecComboStepSelect.WGAPCLOSE;
                        }
                        else
                        {
                            if (Q.Instance.Name == "blindmonkqtwo" && ReturnQBuff().Distance(target) <= 600)
                            {
                                Q.Cast();
                            }
                        }
                        break;
                    case InsecComboStepSelect.WGAPCLOSE:
                        if (FindBestWardItem() != null && W.IsReady() && W.Instance.Name == "BlindMonkWOne" &&
                            (ParamBool("waitForQBuff") &&
                             (Q.Instance.Name == "BlindMonkQOne" ||
                              (!Q.IsReady() || Q.Instance.Name == "blindmonkqtwo") && q2Done)) ||
                            !ParamBool("waitForQBuff"))
                        {
                            WardJump(getInsecPos(target), false, false, true);
                            wardJumped = true;
                        }
                        else if (Player.Spellbook.CanUseSpell(flashSlot) == SpellState.Ready && ParamBool("flashInsec") &&
                                 !wardJumped && Player.Distance(insecPos) < 400 ||
                                 Player.Spellbook.CanUseSpell(flashSlot) == SpellState.Ready && ParamBool("flashInsec") &&
                                 !wardJumped && Player.Distance(insecPos) < 400 && FindBestWardItem() == null)
                        {
                            Player.Spellbook.CastSpell(flashSlot, getInsecPos(target));
                            Utility.DelayAction.Add(50, () => R.CastOnUnit(target, true));
                        }
                        break;
                    case InsecComboStepSelect.PRESSR:
                        R.CastOnUnit(target, true);
                        break;
                }
            }
        }

        private static Vector3 InterceptionPoint(List<Obj_AI_Hero> heroes)
        {
            Vector3 result = new Vector3();
            foreach (Obj_AI_Hero hero in heroes)
            {
                result += hero.Position;
            }
            result.X /= heroes.Count;
            result.Y /= heroes.Count;
            return result;
        }

        private static List<Obj_AI_Hero> GetAllyInsec(List<Obj_AI_Hero> heroes)
        {
            byte alliesAround = 0;
            Obj_AI_Hero tempObject = new Obj_AI_Hero();
            foreach (Obj_AI_Hero hero in heroes)
            {
                int localTemp = GetAllyHeroes(hero, 500 + Menu.Item("bonusRangeA").GetValue<Slider>().Value).Count;
                if (localTemp > alliesAround)
                {
                    tempObject = hero;
                    alliesAround = (byte) localTemp;
                }
            }
            return GetAllyHeroes(tempObject, 500 + Menu.Item("bonusRangeA").GetValue<Slider>().Value);
        }

        private static List<Obj_AI_Hero> GetAllyHeroes(Obj_AI_Hero position, int range)
        {
            return
                ObjectManager.Get<Obj_AI_Hero>()
                    .Where(hero => hero.IsAlly && !hero.IsMe && hero.Distance(position) < range)
                    .ToList();
        }

        private static Vector2 V2E(Vector3 from, Vector3 direction, float distance)
        {
            return from.To2D() + distance * Vector3.Normalize(direction - from).To2D();
        }

        #endregion

        #region SmiteSaver

        public static void SaveMe()
        {
            if ((Player.Health / Player.MaxHealth * 100) > Menu.Item("hpPercentSM").GetValue<Slider>().Value ||
                Player.Spellbook.CanUseSpell(smiteSlot) != SpellState.Ready)
            {
                return;
            }
            var epicSafe = false;
            var buffSafe = false;
            foreach (Obj_AI_Base minion in
                MinionManager.GetMinions(
                    Player.Position, 1100f, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.None))
            {
                if (
                    epics.Any(
                        minionName =>
                            minion.BaseSkinName == minionName && HpLowerParam(minion, "hpEpics") && ParamBool("dEpics")))
                {
                    epicSafe = true;
                }
                if (
                    buffs.Any(
                        minionName =>
                            minion.BaseSkinName == minionName && HpLowerParam(minion, "hpBuffs") && ParamBool("dBuffs")))
                {
                    buffSafe = true;
                }
            }

            if (epicSafe || buffSafe)
            {
                return;
            }

            foreach (var minion in
                MinionManager.GetMinions(
                    Player.Position, 700f, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth)
                    .TakeWhile(
                        minion =>
                            (W.IsReady() || Player.HasBuff("BlindMonkIronWill")) && smiteSlot != SpellSlot.Unknown &&
                            (smiteSlot == SpellSlot.Unknown ||
                             Player.Spellbook.CanUseSpell(smiteSlot) == SpellState.Ready)))
            {
                if (minion.Name.ToLower().Contains("ward"))
                {
                    return;
                }
                if (W.Instance.Name != "blindmonkwtwo")
                {
                    W.Cast();
                    W.Cast();
                }
                if (Player.HasBuff("BlindMonkIronWill"))
                {
                    Player.Spellbook.CastSpell(smiteSlot, minion);
                }
            }
        }

        #endregion

        #region Tick Tasks

        private static void Game_OnGameUpdate(EventArgs args)
        {
            smiteSlot = Player.GetSpellSlot(GetSmiteName());

            if (doubleClickReset <= Environment.TickCount && clickCount != 0)
            {
                doubleClickReset = float.MaxValue;
                clickCount = 0;
            }

            if (clickCount >= 2 && ParamBool("clickInsec"))
            {
                resetTime = Environment.TickCount + 3000;
                clicksecEnabled = true;
                insecClickPos = Game.CursorPos;
                clickCount = 0;
            }

            if (passiveTimer <= Environment.TickCount)
            {
                passiveStacks = 0;
            }

            if (resetTime <= Environment.TickCount && !Menu.Item("InsecEnabled").GetValue<KeyBind>().Active &&
                clicksecEnabled)
            {
                clicksecEnabled = false;
            }

            if (q2Timer <= Environment.TickCount)
            {
                q2Done = false;
            }

            if (Player.IsDead)
            {
                return;
            }

            if (Menu.Item("jungActive").GetValue<KeyBind>().Active)
            {
                JungleClear();
            }

            if ((ParamBool("insecMode")
                ? TargetSelector.GetSelectedTarget()
                : TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical)) == null)
            {
                InsecComboStep = InsecComboStepSelect.NONE;
            }

            if (Menu.Item("smiteEnabled").GetValue<KeyBind>().Active)
            {
                smiter();
            }

            if (Menu.Item("starCombo").GetValue<KeyBind>().Active)
            {
                WardCombo();
            }

            if (ParamBool("smiteSave"))
            {
                SaveMe();
            }

            if (ParamBool("IGNks"))
            {
                Obj_AI_Hero NewTarget = TargetSelector.GetTarget(600, TargetSelector.DamageType.True);

                if (NewTarget != null && IgniteSlot != SpellSlot.Unknown &&
                    Player.Spellbook.CanUseSpell(IgniteSlot) == SpellState.Ready &&
                    ObjectManager.Player.GetSummonerSpellDamage(NewTarget, Damage.SummonerSpell.Ignite) >
                    NewTarget.Health)
                {
                    Player.Spellbook.CastSpell(IgniteSlot, NewTarget);
                }
            }

            if (Menu.Item("InsecEnabled").GetValue<KeyBind>().Active)
            {
                if (ParamBool("insecOrbwalk"))
                {
                    Orbwalk(Game.CursorPos);
                }
                Obj_AI_Hero newTarget = ParamBool("insecMode")
                    ? TargetSelector.GetSelectedTarget()
                    : TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);

                if (newTarget != null)
                {
                    InsecCombo(newTarget);
                }
            }
            else
            {
                isNullInsecPos = true;
                wardJumped = false;
            }

            if (Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.Combo)
            {
                InsecComboStep = InsecComboStepSelect.NONE;
            }

            switch (Orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    StarCombo();
                    break;
                case Orbwalking.OrbwalkingMode.LaneClear:
                    AllClear();
                    break;
                case Orbwalking.OrbwalkingMode.Mixed:
                    Harass();
                    break;
            }

            if (Menu.Item("wjump").GetValue<KeyBind>().Active)
            {
                wardjumpToMouse();
            }
        }

        #endregion

        #region Draw

        private static void Drawing_OnDraw(EventArgs args)
        {
            Obj_AI_Hero newTarget = ParamBool("insecMode")
                ? TargetSelector.GetSelectedTarget()
                : TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
            if (clicksecEnabled)
            {
                Utility.DrawCircle(insecClickPos, 100, Color.White);
            }
            if (Menu.Item("instaFlashInsec").GetValue<KeyBind>().Active)
            {
                Drawing.DrawText(960, 340, Color.Red, "FLASH INSEC ENABLED");
            }
            if (newTarget != null && newTarget.IsVisible && Player.Distance(newTarget) < 3000 && ParamBool("insecDraw"))
            {
                Vector2 targetPos = Drawing.WorldToScreen(newTarget.Position);
                Drawing.DrawLine(insecLinePos.X, insecLinePos.Y, targetPos.X, targetPos.Y, 3, Color.White);
                Utility.DrawCircle(getInsecPos(newTarget), 100, Color.White);
            }
            if (!ParamBool("DrawEnabled"))
            {
                return;
            }
            foreach (var t in ObjectManager.Get<Obj_AI_Hero>())
            {
                if (t.HasBuff("BlindMonkQOne", true) || t.HasBuff("blindmonkqonechaos", true))
                {
                    Drawing.DrawCircle(t.Position, 200, Color.Red);
                }
            }
            if (Menu.Item("smiteEnabled").GetValue<KeyBind>().Active && ParamBool("drawSmite"))
            {
                Utility.DrawCircle(Player.Position, 700, Color.White);
            }
            if (Menu.Item("wjump").GetValue<KeyBind>().Active && ParamBool("WJDraw"))
            {
                Utility.DrawCircle(JumpPos.To3D(), 20, Color.Red);
                Utility.DrawCircle(Player.Position, 600, Color.Red);
            }
            if (ParamBool("drawQ"))
            {
                Utility.DrawCircle(Player.Position, Q.Range - 80, Q.IsReady() ? Color.LightSkyBlue : Color.Tomato);
            }
            if (ParamBool("drawW"))
            {
                Utility.DrawCircle(Player.Position, W.Range - 80, W.IsReady() ? Color.LightSkyBlue : Color.Tomato);
            }
            if (ParamBool("drawE"))
            {
                Utility.DrawCircle(Player.Position, E.Range - 80, E.IsReady() ? Color.LightSkyBlue : Color.Tomato);
            }
            if (ParamBool("drawR"))
            {
                Utility.DrawCircle(Player.Position, R.Range - 80, R.IsReady() ? Color.LightSkyBlue : Color.Tomato);
            }
        }

        #endregion

        #region Autosmite

        public static void smiter()
        {
            var minion =
                ObjectManager.Get<Obj_AI_Minion>()
                    .FirstOrDefault(a => buffandepics.Contains(a.BaseSkinName) && a.Distance(Player) <= 1300);
            if (minion != null)
            {
                if (Menu.Item(minion.BaseSkinName).GetValue<bool>())
                {
                    minionerimo = minion;
                    if (GetSmiteDamage() > minion.Health && minion.IsValidTarget(780) && ParamBool("normSmite"))
                    {
                        Player.Spellbook.CastSpell(smiteSlot, minion);
                    }
                    if (minion.Distance(Player) < 100 && checkSmite)
                    {
                        checkSmite = false;
                        Player.Spellbook.CastSpell(smiteSlot, minion);
                    }
                    if (!Q.IsReady() || !ParamBool("qqSmite"))
                    {
                        return;
                    }
                    if (Q2Damage(minion, ((float) GetSmiteDamage() + Q.GetDamage(minion)), true) + GetSmiteDamage() >
                        minion.Health &&
                        !(minion.HasBuff("BlindMonkQOne", true) || minion.HasBuff("blindmonkqonechaos", true)))
                    {
                        Q.Cast(minion, true);
                    }
                    if ((Q2Damage(minion, (float) GetSmiteDamage(), true) + GetSmiteDamage()) > minion.Health &&
                        (minion.HasBuff("BlindMonkQOne", true) || minion.HasBuff("blindmonkqonechaos", true)))
                    {
                        Q.CastOnUnit(Player, true);
                        checkSmite = true;
                    }
                    if ((minion.HasBuff("BlindMonkQOne", true) || minion.HasBuff("blindmonkqonechaos", true)) &&
                        CastQAgain ||
                        (minion.HasBuff("BlindMonkQOne", true) || minion.HasBuff("blindmonkqonechaos", true)) &&
                        Q2Damage(minion, 0, true) > minion.Health)
                    {
                        Q.CastOnUnit(Player, true);
                    }
                }
            }
        }

        #endregion

        #region WaveClear

        public static void JungleClear()
        {
            var minion =
                MinionManager.GetMinions(
                    Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth)
                    .FirstOrDefault();
            if (minion == null)
            {
                return;
            }
            var passiveIsActive = passiveStacks > Menu.Item("Passive").GetValue<Slider>().Value;
            useClearItems(minion);
            if (Q.IsReady() && ParamBool("Qjng"))
            {
                if ((minion.HasBuff("BlindMonkQOne", true) || minion.HasBuff("blindmonkqonechaos", true)) &&
                    (CastQAgain) || Q.GetDamage(minion, 1) > minion.Health)
                {
                    Q.Cast(packets());
                }
            }
            if (passiveIsActive || waitforjungle)
            {
                return;
            }
            if (ParamBool("Qjng") &&
                Q2Damage(
                    minion, Q.Instance.Name == "BlindMonkQOne" ? minion.Health - Q.GetDamage(minion) : minion.Health,
                    true) > minion.Health && Q.IsReady())
            {
                if (Q.Instance.Name == "BlindMonkQOne")
                {
                    Q.Cast(minion, packets());
                    waiter();
                    return;
                }
                Q.Cast(packets());
                waiter();
                return;
            }
            if (ParamBool("Wjng") && W.IsReady() &&
                minion.IsValidTarget(Orbwalking.GetRealAutoAttackRange(Player) + 200))
            {
                if (W.Instance.Name == "BlindMonkWOne")
                {
                    W.Cast(packets());
                    waiter();
                    return;
                }
                W.Cast(packets());
                waiter();
                return;
            }
            if (ParamBool("Qjng") && Q.IsReady() && minion.IsValidTarget(Q.Range))
            {
                if (Q.Instance.Name == "BlindMonkQOne")
                {
                    Q.Cast(minion, packets());
                    waiter();
                    return;
                }
                if ((minion.HasBuff("BlindMonkQOne", true) || minion.HasBuff("blindmonkqonechaos", true)))
                {
                    Q.Cast(packets());
                    waiter();
                    return;
                }
            }
            if (ParamBool("Ejng") && E.IsReady() && minion.IsValidTarget(E.Range))
            {
                E.Cast(packets());
                waiter();
            }
        }

        public static void waiter()
        {
            waitforjungle = true;
            Utility.DelayAction.Add(300, () => waitforjungle = false);
        }

        private static void AllClear()
        {
            Obj_AI_Base minion = MinionManager.GetMinions(Player.ServerPosition, Q.Range).FirstOrDefault();
            useClearItems(minion);
            if (minion == null || minion.Name.ToLower().Contains("ward"))
            {
                return;
            }
            if (Menu.Item("useQClear").GetValue<bool>() && Q.IsReady())
            {
                if (Q.Instance.Name == "BlindMonkQOne")
                {
                    Q.Cast(minion, true);
                }
                else if ((minion.HasBuff("BlindMonkQOne", true) || minion.HasBuff("blindmonkqonechaos", true)) &&
                         (Q.IsKillable(minion, 1)) || Player.Distance(minion) > 500)
                {
                    Q.Cast();
                }
            }
            if (Menu.Item("useEClear").GetValue<bool>() && E.IsReady())
            {
                if (E.Instance.Name == "BlindMonkEOne" && minion.IsValidTarget(E.Range) && !delayW)
                {
                    E.Cast();
                    delayW = true;
                    Utility.DelayAction.Add(300, () => delayW = false);
                }
                else if (minion.HasBuff("BlindMonkEOne", true) && (Player.Distance(minion) > 450))
                {
                    E.Cast();
                }
            }
        }

        #endregion

        #region Wardjump

        private static void wardjumpToMouse()
        {
            WardJump(Game.CursorPos, ParamBool("m2m"), false, false, ParamBool("j2m"), ParamBool("j2c"));
        }

        private static void WardJump(Vector3 pos,
            bool m2m = true,
            bool maxRange = false,
            bool reqinMaxRange = false,
            bool minions = true,
            bool champions = true)
        {
            var basePos = Player.Position.To2D();
            var newPos = (pos.To2D() - Player.Position.To2D());

            if (JumpPos == new Vector2())
            {
                if (reqinMaxRange)
                {
                    JumpPos = pos.To2D();
                }
                else if (maxRange || Player.Distance(pos) > 590)
                {
                    JumpPos = basePos + (newPos.Normalized() * (590));
                }
                else
                {
                    JumpPos = basePos + (newPos.Normalized() * (Player.Distance(pos)));
                }
            }
            if (JumpPos != new Vector2() && reCheckWard)
            {
                reCheckWard = false;
                Utility.DelayAction.Add(
                    20, () =>
                    {
                        if (JumpPos != new Vector2())
                        {
                            JumpPos = new Vector2();
                            reCheckWard = true;
                        }
                    });
            }
            if (m2m)
            {
                Orbwalk(pos);
            }
            if (!W.IsReady() || W.Instance.Name == "blindmonkwtwo" || reqinMaxRange && Player.Distance(pos) > W.Range)
            {
                return;
            }
            if (minions || champions)
            {
                if (champions)
                {
                    var champs = (from champ in ObjectManager.Get<Obj_AI_Hero>()
                        where
                            champ.IsAlly && champ.Distance(Player) < W.Range && champ.Distance(pos) < 200 && !champ.IsMe
                        select champ).ToList();
                    if (champs.Count > 0)
                    {
                        W.CastOnUnit(champs[0], true);
                        return;
                    }
                }
                if (minions)
                {
                    var minion2 = (from minion in ObjectManager.Get<Obj_AI_Minion>()
                        where
                            minion.IsAlly && minion.Distance(Player) < W.Range && minion.Distance(pos) < 200 &&
                            !minion.Name.ToLower().Contains("ward")
                        select minion).ToList();
                    if (minion2.Count > 0)
                    {
                        W.CastOnUnit(minion2[0], true);
                        return;
                    }
                }
            }
            var isWard = false;
            foreach (var ward in ObjectManager.Get<Obj_AI_Minion>())
            {
                if (ward.IsAlly && ward.Name.ToLower().Contains("ward") && ward.Distance(JumpPos) < 200)
                {
                    isWard = true;
                    W.CastOnUnit(ward, true);
                }
            }
            if (!isWard && CastWardAgain)
            {
                var ward = FindBestWardItem();
                if (ward == null)
                {
                    return;
                }
                Player.Spellbook.CastSpell(ward.SpellSlot, JumpPos.To3D());
                CastWardAgain = false;
                lastWardPos = JumpPos.To3D();
                lastPlaced = Environment.TickCount;
                Utility.DelayAction.Add(500, () => CastWardAgain = true);
            }
        }

        //Thanks to xSallice the gumbo
        private static InventorySlot FindBestWardItem()
        {
            InventorySlot slot = Items.GetWardSlot();
            if (slot == default(InventorySlot))
            {
                return null;
            }

            SpellDataInst sdi = GetItemSpell(slot);

            if (sdi != default(SpellDataInst) && sdi.State == SpellState.Ready)
            {
                return slot;
            }
            return slot;
        }

        private static void GameObject_OnCreate(GameObject sender, EventArgs args)
        {
            if (Environment.TickCount < lastPlaced + 300)
            {
                var ward = (Obj_AI_Minion) sender;
                if (ward.Name.ToLower().Contains("ward") && ward.Distance(lastWardPos) < 500 && E.IsReady())
                {
                    W.Cast(ward);
                }
            }
        }

        #endregion

        #region Combo
		private static void Unit_OnDash(Obj_AI_Base sender, Dash.DashItem args)
        {

            /* var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);

            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);


            if (!sender.IsEnemy)
                return;

            if (sender.NetworkId == target.NetworkId)
            {
                if (Q.IsReady()
                   && Q.IsInRange(sender.ServerPosition))
                {
                    Q.CastIfHitchanceEquals(target, HitChance.Dashing, true);
                }

            } */
			if (sender != null && sender.IsEnemy && sender.IsValidTarget(2000) && sender.Type == Player.Type)
            {
				if (args.EndPos.Distance(Player.Position) < 1000 && !sender.HasBuff("BlindMonkQOne", true) && !sender.IsZombie && !Q.GetPrediction(sender).CollisionObjects.Any(h => h.IsEnemy && !h.IsDead && h is Obj_AI_Minion))
				{
				Q.Cast(args.EndPos + 100);
				}


            }
        }

        private static void WardCombo()
        {
            Obj_AI_Hero target = TargetSelector.GetTarget(1500, TargetSelector.DamageType.Physical);

            Orbwalking.Orbwalk(
                target, Game.CursorPos, Menu.Item("ExtraWindup").GetValue<Slider>().Value,
                Menu.Item("HoldPosRadius").GetValue<Slider>().Value);

            if (target == null)
            {
                return;
            }
            UseItems(target);
            if ((target.HasBuff("BlindMonkQOne", true) || target.HasBuff("blindmonkqonechaos", true)))
            {
                if (CastQAgain || target.HasBuffOfType(BuffType.Knockup) && !Player.IsValidTarget(300) && !R.IsReady() ||
                    !target.IsValidTarget(Orbwalking.GetRealAutoAttackRange(Player)) && !R.IsReady())
                {
                    Q.Cast();
                }
            }
            if (target.Distance(Player) > R.Range && target.Distance(Player) < R.Range + 580 &&
                (target.HasBuff("BlindMonkQOne", true) || target.HasBuff("blindmonkqonechaos", true)))
            {
                WardJump(target.Position, false);
            }
            if (E.IsReady() && E.Instance.Name == "BlindMonkEOne" && target.IsValidTarget(E.Range))
            {
                E.Cast();
            }

            if (E.IsReady() && E.Instance.Name != "BlindMonkEOne" &&
                !target.IsValidTarget(Orbwalking.GetRealAutoAttackRange(Player)))
            {
                E.Cast();
            }

            if (Q.IsReady() && Q.Instance.Name == "BlindMonkQOne")
            {
                CastQ1(target);
            }

            if (R.IsReady() && Q.IsReady() &&
                ((target.HasBuff("BlindMonkQOne", true) || target.HasBuff("blindmonkqonechaos", true))))
            {
                R.CastOnUnit(target, packets());
            }
			
			if (R.IsReady())
            {
                foreach (var enemy in
                        HeroManager.Enemies.Where(i => i.IsValidTarget(R.Range) && !R.IsKillable(i)))
                    {
                        R2.UpdateSourcePosition(enemy.ServerPosition, enemy.ServerPosition);
                        var enemyBehind =
                            HeroManager.Enemies.Where(
                                i =>
                                    i.IsValidTarget(R2.Range) && i.NetworkId != enemy.NetworkId &&
                                    R2.WillHit(
                                        i, enemy.ServerPosition.Extend(Player.ServerPosition, -R2.Range),
                                        (int) enemy.BoundingRadius)).ToList();
                        if (enemyBehind.Count >= Menu.Item("RCount").GetValue<Slider>().Value &&
                            R.CastOnUnit(enemy, packets()))
                        {
                            break;
                        }
                    }
            }
			
        }

        private static void StarCombo()
        {
            Obj_AI_Hero target = TargetSelector.GetTarget(1500f, TargetSelector.DamageType.Physical);
            if (target == null)
            {
                return;
            }
            if ((target.HasBuff("BlindMonkQOne", true) || target.HasBuff("blindmonkqonechaos", true)) &&
                ParamBool("useQ2"))
            {
                if (CastQAgain || target.HasBuffOfType(BuffType.Knockup) && !Player.IsValidTarget(300) && !R.IsReady() ||
                    !target.IsValidTarget(Orbwalking.GetRealAutoAttackRange(Player)) ||
                    Q.GetDamage(target, 1) > target.Health ||
                    ReturnQBuff().Distance(target) < Player.Distance(target) &&
                    !target.IsValidTarget(Orbwalking.GetRealAutoAttackRange(Player)))
                {
                    Q.Cast();
                }
            }
            UseItems(target);
            if (R.GetDamage(target) >= target.Health && ParamBool("ksR") && target.IsValidTarget())
            {
                R.Cast(target, packets());
            }
            if (ParamBool("aaStacks") && passiveStacks > 0 &&
                target.IsValidTarget(Orbwalking.GetRealAutoAttackRange(Player) + 100))
            {
                return;
            }
            if (ParamBool("useW"))
            {
                if (ParamBool("wMode") && Player.Distance(target) > Q.Range && R.GetDamage(target) >= target.Health)
                {
                    WardJump(target.Position, false, true);
                }
            }
            if (E.IsReady() && E.Instance.Name == "BlindMonkEOne" && target.IsValidTarget(E.Range) && ParamBool("useE"))
            {
                E.Cast();
            }

            if (E.IsReady() && E.Instance.Name != "BlindMonkEOne" && target.IsValidTarget(480) && !target.IsValidTarget(Orbwalking.GetRealAutoAttackRange(Player) + 50) && ParamBool("useE"))
            {
                E.Cast();
            }

            if (Q.IsReady() && Q.Instance.Name == "BlindMonkQOne" && ParamBool("useQ"))
            {
                CastQ1(target);
            }
			
			if (W.IsReady() && target.IsValidTarget(250) && Player.HealthPercent <= Menu.Item("Wcombo").GetValue<Slider>().Value)
            {
                W.Cast();
            }
			
			/* if (W.IsReady() && target.IsValidTarget(1000) && !Player.HasBuff("BlindMonkSafeguard") && Player.HealthPercent <= Menu.Item("Wsave").GetValue<Slider>().Value)
            {
                W.Cast();
            } */
				
            if (R.IsReady() && Q.IsReady() &&
                ((target.HasBuff("BlindMonkQOne", true) || target.HasBuff("blindmonkqonechaos", true))) &&
                ParamBool("useR"))
            {
                R.CastOnUnit(target, packets());
            }
        }

        public static void CastQ1(Obj_AI_Hero target)
       /*  {
            var Qpred = Q.GetPrediction(target);
			HitChance minChance = GetHitChance(Menu.Item("QHC").GetValue<StringList>());
            if ((Qpred.CollisionObjects.Where(a => a.IsValidTarget() && a.IsMinion).ToList().Count) == 1 &&
                smiteSlot.IsReady() && ParamBool("qSmite") && Qpred.CollisionObjects[0].IsValidTarget(780))
            {
                Player.Spellbook.CastSpell(smiteSlot, Qpred.CollisionObjects[0]);
                Utility.DelayAction.Add(Game.Ping / 2, () => 
				{
				if (Qpred.Hitchance >= minChance)
					 Q.Cast(Qpred.CastPosition, packets());                
				}
				);
            }
            else if (Qpred.CollisionObjects.Count == 0)
            {
				if (Qpred.Hitchance >= minChance)
					 Q.Cast(Qpred.CastPosition, packets());        


            }
        }
		 */
		/*  {
            var qpred = Q.GetPrediction(target);
            if ((qpred.CollisionObjects.Where(a => a.IsValidTarget() && a.IsMinion).ToList().Count) == 1
                && smiteSlot.IsReady() && ParamBool("qSmite") && qpred.CollisionObjects[0].IsValidTarget(780))
            {
                Player.Spellbook.CastSpell(smiteSlot, qpred.CollisionObjects[0]);
                Utility.DelayAction.Add(Game.Ping / 2, () => Q.Cast(qpred.CastPosition));
            }
            else if (qpred.CollisionObjects.Count == 0)
            {
                if (qpred.Hitchance >= HitChance.VeryHigh)
                {
                    Q.Cast(target);
                }

            }
        } */
		
		{
            var Qprediction = Q.GetPrediction(target);

        if (Qprediction.Hitchance >= HitChance.VeryHigh && !Qprediction.CollisionObjects.Any(h => h.IsValidTarget() && h.IsEnemy && !h.IsDead && h is Obj_AI_Minion))
			{
				Q.Cast(Qprediction.CastPosition);
			}
		else if (Qprediction.Hitchance >= HitChance.VeryHigh && Qprediction.CollisionObjects.Where(h => h.IsValidTarget() && h.IsEnemy && !h.IsDead && h is Obj_AI_Minion).ToList().Count == 1 && smiteSlot.IsReady() && ParamBool("qSmite") && Qprediction.CollisionObjects[0].IsValidTarget(500))
			{
				Player.Spellbook.CastSpell(smiteSlot, Qprediction.CollisionObjects[0]);
                Utility.DelayAction.Add(Game.Ping / 2, () => Q.Cast(Qprediction.CastPosition));
			}
        }

        #endregion

        #region Utility

        //Start Credits to Kurisu
        public static readonly int[] SmitePurple = { 3713, 3726, 3725, 3726, 3723 };
        public static readonly int[] SmiteGrey = { 3711, 3722, 3721, 3720, 3719 };
        public static readonly int[] SmiteRed = { 3715, 3718, 3717, 3716, 3714 };
        public static readonly int[] SmiteBlue = { 3706, 3710, 3709, 3708, 3707 };

        private static string GetSmiteName()
        {
            if (SmiteBlue.Any(a => Items.HasItem(a)))
            {
                return "s5_summonersmiteplayerganker";
            }
            if (SmiteRed.Any(a => Items.HasItem(a)))
            {
                return "s5_summonersmiteduel";
            }
            if (SmiteGrey.Any(a => Items.HasItem(a)))
            {
                return "s5_summonersmitequick";
            }
            if (SmitePurple.Any(a => Items.HasItem(a)))
            {
                return "itemsmiteaoe";
            }
            return "summonersmite";
        }

        //End credits

        public static float Q2Damage(Obj_AI_Base target, float subHP = 0, bool monster = false)
        {
            var damage = (50 + (Q.Level * 30)) + (0.09 * Player.FlatPhysicalDamageMod) +
                         ((target.MaxHealth - (target.Health - subHP)) * 0.08);
            if (monster && damage > 400)
            {
                return (float) Player.CalcDamage(target, Damage.DamageType.Physical, 400);
            }
            return (float) Player.CalcDamage(target, Damage.DamageType.Physical, damage);
        }

        public static void PrintMessage(string msg) // Credits to ChewyMoon, and his Brain.exe
        {
            Game.PrintChat("<font color=\"#6699ff\"><b>FALeeSin:</b></font> <font color=\"#FFFFFF\">" + msg + "</font>");
        }

        public static void Orbwalk(Vector3 pos, Obj_AI_Hero target = null)
        {
            Player.IssueOrder(GameObjectOrder.MoveTo, pos);
        }

        private static SpellDataInst GetItemSpell(InventorySlot invSlot)
        {
            return Player.Spellbook.Spells.FirstOrDefault(spell => (int) spell.Slot == invSlot.Slot + 4);
        }

        public static bool packets()
        {
            return Menu.Item("NFE").GetValue<bool>();
        }

		private static bool UnderTower(Vector3 pos)
        {
            return
                ObjectManager.Get<Obj_AI_Turret>()
                    .Any(i => i.IsEnemy && !i.IsDead && i.Distance(pos) < 850 + Player.BoundingRadius);
        }
		
        private static Obj_AI_Base ReturnQBuff()
        {
            return
                ObjectManager.Get<Obj_AI_Base>()
                    .Where(a => a.IsValidTarget(1300))
                    .FirstOrDefault(
                        unit => unit.HasBuff("BlindMonkQOne", true) || unit.HasBuff("blindmonkqonechaos", true));
        }

        public static void UseItems(Obj_AI_Hero enemy)
        {
            if (Items.CanUseItem(3142) && Player.Distance(enemy) <= 600)
            {
                Items.UseItem(3142);
            }
            if (Items.CanUseItem(3144) && Player.Distance(enemy) <= 450)
            {
                Items.UseItem(3144, enemy);
            }
            if (Items.CanUseItem(3153) && Player.Distance(enemy) <= 450)
            {
                Items.UseItem(3153, enemy);
            }
            if (Items.CanUseItem(3077) && Utility.CountEnemiesInRange(350) >= 1)
            {
                Items.UseItem(3077);
            }
            if (Items.CanUseItem(3074) && Utility.CountEnemiesInRange(350) >= 1)
            {
                Items.UseItem(3074);
            }
            if (Items.CanUseItem(3143) && Utility.CountEnemiesInRange(450) >= 1)
            {
                Items.UseItem(3143);
            }
        }

        public static double GetSmiteDamage()
        {
            int[] dmg =
            {
                20 * Player.Level + 370, 30 * Player.Level + 330, 40 * +Player.Level + 240,
                50 * Player.Level + 100
            };
            return Player.Spellbook.CanUseSpell(smiteSlot) == SpellState.Ready ? dmg.Max() : 0;
        }

        public static void useClearItems(Obj_AI_Base enemy)
        {
            if (Items.CanUseItem(3077) && Player.Distance(enemy) < 350)
            {
                Items.UseItem(3077);
            }
            if (Items.CanUseItem(3074) && Player.Distance(enemy) < 350)
            {
                Items.UseItem(3074);
            }
        }

        public static bool ParamBool(String paramName)
        {
            return Menu.Item(paramName).GetValue<bool>();
        }

        public static HitChance GetHitChance(StringList stringList)
        {
            switch (stringList.SelectedIndex)
            {
                case 0:
                    return HitChance.Low;
                case 1:
                    return HitChance.Medium;
                case 2:
                    return HitChance.High;
                case 3:
                    return HitChance.VeryHigh;
                default:
                    return HitChance.High;
            }
        }

        public static bool HpLowerParam(Obj_AI_Base obj, String paramName)
        {
            return ((obj.Health / obj.MaxHealth) * 100) <= Menu.Item(paramName).GetValue<Slider>().Value;
        }

        #endregion
    }
}
