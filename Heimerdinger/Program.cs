#region

using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

#endregion

namespace Heimerdinger
{
    internal class Program
    {
        public const string ChampName = "Heimerdinger";
        public static Orbwalking.Orbwalker Orbwalker;
        public static Obj_AI_Hero Player;
		public static List<int> LevelUps;
        public static List<Spell> SpellList = new List<Spell>();
        public static Spell H_Q, H_W, H_E, H_R;
        public static SpellDataInst Ignite;
        public static Menu Menu;
		public static int qOff = 0, wOff = 0, eOff = 0, rOff = 0;
        public static readonly StringList HitchanceList = new StringList(new[] { "Low", "Medium", "High", "Very High" });

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            Player = ObjectManager.Player;

            if (Player.BaseSkinName != ChampName)
            {
                return;
            }
			LevelUps = new List<int>() { 1,3,2,1,1,4,1,2,1,2,4,2,2,1,1,4,1,1 };
            H_Q = new Spell(SpellSlot.Q, 250f);
            H_W = new Spell(SpellSlot.W,1100f);
            H_E = new Spell(SpellSlot.E, 925f);
			H_R = new Spell(SpellSlot.R);

			H_Q.SetSkillshot(0.283f, 0f, 1750f, false, SkillshotType.SkillshotCircle);
            H_W.SetSkillshot(0.283f, 0f, 1750f, true, SkillshotType.SkillshotLine);
            H_E.SetSkillshot(0.283f, 0f, 1750f, false, SkillshotType.SkillshotCircle);

            SpellList.Add(H_Q);
            SpellList.Add(H_W);
            SpellList.Add(H_E);
            SpellList.Add(H_R);
					

            Menu = new Menu("Trees " + ChampName, ChampName, true);

            Menu.AddSubMenu(new Menu("Orbwalker", "Orbwalker"));
            Orbwalker = new Orbwalking.Orbwalker(Menu.SubMenu("Orbwalker"));

            var ts = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(ts);
            Menu.AddSubMenu(ts);

            Menu.AddSubMenu(new Menu("Combo", "Combo"));
            Menu.SubMenu("Combo").AddItem(new MenuItem("ComboQ", "Use Q").SetValue(true));
            Menu.SubMenu("Combo").AddItem(new MenuItem("ComboW", "Use W").SetValue(true));
            Menu.SubMenu("Combo").AddItem(new MenuItem("ComboE", "Use E").SetValue(true));
						Menu.SubMenu("Combo").AddItem(new MenuItem("ComboR", "Use R").SetValue(true));
            Menu.SubMenu("Combo").AddItem(new MenuItem("ComboEChance", "E HitChance").SetValue(HitchanceList));
            Menu.SubMenu("Combo")
                .AddItem(new MenuItem("ComboActive", "Combo").SetValue(new KeyBind(32, KeyBindType.Press)));

            Menu.AddSubMenu(new Menu("Harass", "Harass"));
            Menu.SubMenu("Harass").AddItem(new MenuItem("HarassW", "Use W").SetValue(true));
            Menu.SubMenu("Harass").AddItem(new MenuItem("HarassE", "Use E").SetValue(true));
            Menu.SubMenu("Harass").AddItem(new MenuItem("HarassEChance", "E HitChance").SetValue(HitchanceList));
            Menu.SubMenu("Harass")
                .AddItem(new MenuItem("HarassActive", "Harass").SetValue(new KeyBind((byte) 'C', KeyBindType.Press)));

            Menu.AddSubMenu(new Menu("LaneClear", "LaneClear"));
            Menu.SubMenu("LaneClear").AddItem(new MenuItem("LaneClearW", "Use W").SetValue(true));
            Menu.SubMenu("LaneClear")
                .AddItem(
                    new MenuItem("LaneClearManaPercent", "Minimum Mana Percent").SetValue(new Slider(30, 0, 100)));
            Menu.SubMenu("LaneClear")
                .AddItem(
                    new MenuItem("LaneClearActive", "LaneClear").SetValue(new KeyBind((byte) 'S', KeyBindType.Press)));

            Menu.AddSubMenu(new Menu("Drawings", "Drawings"));
            Menu.SubMenu("Drawings")
                .AddItem(new MenuItem("QQRange", "Q").SetValue(new Circle(false, Color.Blue, H_Q.Range)));
            Menu.SubMenu("Drawings")
                .AddItem(new MenuItem("ERange", "E").SetValue(new Circle(false, Color.Blue, H_E.Range)));
            Menu.SubMenu("Drawings")
                 .AddItem(new MenuItem("WRange", "W").SetValue(new Circle(false, Color.Orange, H_W.Range)));

            Menu.AddToMainMenu();

            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnGameUpdate += Game_OnGameUpdate;

            Game.PrintChat("SG " + ChampName + " loaded!");
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (Player.IsDead)
            {
                return;
            }
						
						int qL = Player.Spellbook.GetSpell(SpellSlot.Q).Level + qOff;
						int wL = Player.Spellbook.GetSpell(SpellSlot.W).Level + wOff;
						int eL = Player.Spellbook.GetSpell(SpellSlot.E).Level + eOff;
						int rL = Player.Spellbook.GetSpell(SpellSlot.R).Level + rOff;
						if (qL + wL + eL + rL < ObjectManager.Player.Level)
						{
							int[] level = new int[] { 0, 0, 0, 0 };
							for (int i = 0; i < ObjectManager.Player.Level; i++)
							{
								level[LevelUps[i] - 1] = level[LevelUps[i] - 1] + 1;
							}
									
							if (qL < level[0]) ObjectManager.Player.Spellbook.LevelUpSpell(SpellSlot.Q);
							if (wL < level[1]) ObjectManager.Player.Spellbook.LevelUpSpell(SpellSlot.W);
							if (eL < level[2]) ObjectManager.Player.Spellbook.LevelUpSpell(SpellSlot.E);
							if (rL < level[3]) ObjectManager.Player.Spellbook.LevelUpSpell(SpellSlot.R);
						}

            if (Menu.Item("LaneClearActive").GetValue<KeyBind>().Active)
            {
                LaneClear();
                return;
            }


            CastLogic();
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            Circle[] Draw = { Menu.Item("QQRange").GetValue<Circle>(), Menu.Item("ERange").GetValue<Circle>(), Menu.Item("WRange").GetValue<Circle>() };

            foreach (var circle in Draw.Where(circle => circle.Active))
            {
                Utility.DrawCircle(Player.Position, circle.Radius, circle.Color);
            }
        }

        private static void LaneClear()
        {
            if (!H_W.IsReady())
            {
                return;
            }
            var unit =
                ObjectManager.Get<Obj_AI_Minion>()
                    .First(
                        minion =>
                            minion.IsValid && minion.IsVisible && !minion.IsDead &&
                            minion.IsValidTarget(H_W.Range, true, Player.ServerPosition) &&
                            minion.Health < Player.GetDamageSpell(minion, SpellSlot.W).CalculatedDamage);

            CastH_W(unit, "LaneClear");
        }


        private static void CastLogic()
        {
            
            var target = TargetSelector.GetTarget(H_W.Range, TargetSelector.DamageType.Magical);
			var close_range = TargetSelector.GetTarget(450, TargetSelector.DamageType.Magical);
            if (target == null ||
                (!Menu.Item("ComboActive").GetValue<KeyBind>().Active &&
                 !Menu.Item("HarassActive").GetValue<KeyBind>().Active))
            {                                                                                           
                return;
            }
						
            var mode = Menu.Item("ComboActive").GetValue<KeyBind>().Active ? "Combo" : "Harass";
						if(H_R.IsReady() && Menu.Item(mode + "R").GetValue<bool>() )
						{
						
							if(H_Q.IsReady() && Menu.Item(mode + "Q").GetValue<bool>() && close_range != null){
								H_R.Cast();
								H_Q.Cast(Player);
							}

                            if (H_W.IsReady() && Menu.Item(mode + "W").GetValue<bool>() && !H_Q.IsReady() && close_range != null)
							{
                                H_R.Cast();
                                H_W.Cast(target);
                            }
							
						}
            if (H_Q.IsReady() && Menu.Item(mode + "Q").GetValue<bool>() && close_range != null)
            {
                H_Q.Cast(Player);
            }
            CastH_E(TargetSelector.GetTarget(H_E.Range, TargetSelector.DamageType.Magical), mode);
            CastH_W(target, mode);
        }

        private static void SmartQ()
        {
            if (!H_Q.IsReady() || !Menu.Item("AutoQ").GetValue<bool>())
            {
                return;
            }
            foreach (var obj in
                ObjectManager.Get<GameObject>()
                    .Where(
                        obj =>
                            obj is Obj_AI_Hero &&
                            ((Obj_AI_Base) obj).IsValidTarget(H_Q.Range, true, Player.ServerPosition) &&
                            ((Obj_AI_Base) obj).HasBuff("UrgotPlasmaGrenadeBoom",true)))
            {
                H_W.Cast();
                H_Q.Cast(obj.Position);
            }
        }

        private static void CastH_W(Obj_AI_Base target, string mode)
        {
            if (H_W.IsReady() && Menu.Item(mode + "W").GetValue<bool>() && target.IsValidTarget(H_W.Range))
            {
                H_W.Cast(target);
            }
        }

        private static void CastH_E(Obj_AI_Base target, string mode)
        {
            if (!H_E.IsReady() || !Menu.Item(mode + "E").GetValue<bool>())
            {
                return;
            }

            var hitchance = (HitChance) (Menu.Item(mode + "EChance").GetValue<StringList>().SelectedIndex + 3);

            if (target.IsValidTarget(H_E.Range))
            {
                H_E.CastIfHitchanceEquals(target, hitchance);
            }
            else
            {
                H_E.CastIfHitchanceEquals(TargetSelector.GetTarget(H_E.Range, TargetSelector.DamageType.Magical), HitChance.High);
            }
        }
    }
}