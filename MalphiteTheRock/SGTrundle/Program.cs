using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace Trundle
{
    class Program
    {
        public const string ChampionName = "Trundle";

        //Orbwalker instance
        public static Orbwalking.Orbwalker Orbwalker;
		//Spells
        public static List<Spell> SpellList = new List<Spell>();

        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;

        //Menu
        public static Menu menu;

        private static Obj_AI_Hero Player;
        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            //Thanks to Esk0r
            Player = ObjectManager.Player;

            //check to see if correct champ
            if (Player.BaseSkinName != ChampionName) return;

            //intalize spell
            Q = new Spell(SpellSlot.Q, 300);
            W = new Spell(SpellSlot.W, 750);
            E = new Spell(SpellSlot.E, 1000);
            R = new Spell(SpellSlot.R, 650);

			W.SetSkillshot(.25f, 300f, float.MaxValue, false, SkillshotType.SkillshotCircle);
			E.SetSkillshot(.25f, 1f, float.MaxValue, false, SkillshotType.SkillshotLine);
          
            SpellList.Add(Q);
            SpellList.Add(W);
            SpellList.Add(E);
            SpellList.Add(R);

            //Create the menu
            menu = new Menu(ChampionName, ChampionName, true);

            //Orbwalker submenu
            menu.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));

            //Target selector
            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            menu.AddSubMenu(targetSelectorMenu);

            //Orbwalk
            Orbwalker = new Orbwalking.Orbwalker(menu.SubMenu("Orbwalking"));

            //Combo menu:
            menu.AddSubMenu(new Menu("Combo", "Combo"));
            menu.SubMenu("Combo").AddItem(new MenuItem("UseQCombo", "Use Q").SetValue(true));
            menu.SubMenu("Combo").AddItem(new MenuItem("UseWCombo", "Use W").SetValue(true));
            menu.SubMenu("Combo").AddItem(new MenuItem("UseECombo", "Use E").SetValue(true));
            menu.SubMenu("Combo").AddItem(new MenuItem("UseRCombo", "Use R Finish").SetValue(true));
            menu.SubMenu("Combo").AddItem(new MenuItem("HPForR", "Use R if my HP < X%").SetValue(new Slider(50, 100, 0)));
			menu.SubMenu("Combo").AddItem(new MenuItem("eHPForR", "Use R if enemy HP >").SetValue(new Slider(3000, 5000, 0)));
            menu.SubMenu("Combo").AddItem(new MenuItem("ComboActive", "Combo!").SetValue(new KeyBind(menu.Item("Orbwalk").GetValue<KeyBind>().Key, KeyBindType.Press)));
			menu.SubMenu("Combo").AddItem(new MenuItem("escape", "Flee !!!").SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press)));

            //Harass menu:
            menu.AddSubMenu(new Menu("Harass", "Harass"));
            menu.SubMenu("Harass").AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(true));
            menu.SubMenu("Harass").AddItem(new MenuItem("UseWHarass", "Use W").SetValue(false));
            menu.SubMenu("Harass").AddItem(new MenuItem("UseEHarass", "Use E").SetValue(false));
            menu.SubMenu("Harass").AddItem(new MenuItem("HarassActive", "Harass!").SetValue(new KeyBind(menu.Item("Farm").GetValue<KeyBind>().Key, KeyBindType.Press)));
            menu.SubMenu("Harass").AddItem(new MenuItem("HarassActiveT", "Harass (toggle)!").SetValue(new KeyBind("Y".ToCharArray()[0], KeyBindType.Toggle)));

            //Farming menu:
          /*   menu.AddSubMenu(new Menu("Farm", "Farm"));
            menu.SubMenu("Farm").AddItem(new MenuItem("UseQFarm", "Use Q").SetValue(true));
            menu.SubMenu("Farm").AddItem(new MenuItem("UseWFarm", "Use W").SetValue(false));
            menu.SubMenu("Farm").AddItem(new MenuItem("UseEFarm", "Use E").SetValue(false));
            menu.SubMenu("Farm").AddItem(new MenuItem("LaneClearActive", "Farm!").SetValue(new KeyBind(menu.Item("LaneClear").GetValue<KeyBind>().Key, KeyBindType.Press)));
            menu.SubMenu("Farm").AddItem(new MenuItem("LastHitQQ", "Last hit with Q").SetValue(new KeyBind("A".ToCharArray()[0], KeyBindType.Press))); */

            //Misc Menu:
            menu.AddSubMenu(new Menu("Misc", "Misc"));
            menu.SubMenu("Misc").AddItem(new MenuItem("packet", "Use Packets").SetValue(true));
			menu.SubMenu("Misc").AddSubMenu(new Menu("Dont use R on", "DontR"));
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != Player.Team))
            menu.SubMenu("Misc").SubMenu("DontR").AddItem(new MenuItem("DontR" + enemy.BaseSkinName, enemy.BaseSkinName).SetValue(false));
				
				
              			
            //Damage after combo:
            var dmgAfterComboItem = new MenuItem("DamageAfterCombo", "Draw damage after combo").SetValue(true);
            Utility.HpBarDamageIndicator.DamageToUnit = GetComboDamage;
            Utility.HpBarDamageIndicator.Enabled = dmgAfterComboItem.GetValue<bool>();
            dmgAfterComboItem.ValueChanged += delegate(object sender, OnValueChangeEventArgs eventArgs)
            {
                Utility.HpBarDamageIndicator.Enabled = eventArgs.GetNewValue<bool>();
            };

            //Drawings menu:
            menu.AddSubMenu(new Menu("Drawings", "Drawings"));
            menu.SubMenu("Drawings")
                .AddItem(new MenuItem("QRange", "Q range").SetValue(new Circle(false, Color.FromArgb(100, 255, 0, 255))));
            menu.SubMenu("Drawings")
                .AddItem(new MenuItem("WRange", "W range").SetValue(new Circle(false, Color.FromArgb(100, 255, 0, 255))));
            menu.SubMenu("Drawings")
                .AddItem(new MenuItem("ERange", "E range").SetValue(new Circle(true, Color.FromArgb(100, 255, 0, 255))));
            menu.SubMenu("Drawings")
                .AddItem(new MenuItem("RRange", "R range").SetValue(new Circle(false, Color.FromArgb(100, 255, 0, 255))));
            menu.SubMenu("Drawings")
                .AddItem(dmgAfterComboItem);
            menu.AddToMainMenu();

            //Events
            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Game.PrintChat("SG" + ChampionName + " Loaded! ");
        }

        
        private static float GetComboDamage(Obj_AI_Base enemy)
        {
            var damage = 0d;

            if (Q.IsReady())
                damage += Player.GetSpellDamage(enemy, SpellSlot.Q);
          
            if (R.IsReady())
                damage += Player.GetSpellDamage(enemy, SpellSlot.R);

            return (float)damage ;
        }
		
				
        private static void Combo()
        {
            UseSpells(menu.Item("UseQCombo").GetValue<bool>(), menu.Item("UseWCombo").GetValue<bool>(),
                menu.Item("UseECombo").GetValue<bool>(), menu.Item("UseRCombo").GetValue<bool>());
        }

        private static void UseSpells(bool useQ, bool useW, bool useE, bool useR)
        {
	    	
			
            var qTarget = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
			var wTarget = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Magical);			
            var eTarget = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);
            var rTarget = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Magical);
          
            if (useQ && qTarget != null && Q.IsReady() && Player.Distance(qTarget) < Q.Range)
            {
                Q.CastOnUnit(Player);
                return;
            }

            if (useW && wTarget != null && W.IsReady() && Player.Distance(wTarget) < W.Range)
            {
                var pred = W.GetPrediction(wTarget);
                W.Cast(pred.CastPosition);
                return;
            }

            if (useE && eTarget != null && E.IsReady() && Player.Distance(eTarget) < E.Range)
            {
                CastE(eTarget);
                return;
            }
			//regular combo
            if (rTarget != null && R.IsReady() && menu.Item("HPForR").GetValue<Slider>().Value >= ((Player.Health / Player.MaxHealth) * 100) && menu.Item("DontR").GetValue<bool>() == false)
			{	
                R.CastOnUnit(rTarget, packets());
                return;
            }
			
			//regular combo
            if (rTarget != null && R.IsReady() && rTarget.MaxHealth >= menu.Item("eHPForR").GetValue<Slider>().Value && menu.Item("DontR").GetValue<bool>() == false)
			{	
                R.CastOnUnit(rTarget, packets());
                return;
            }

			//R finish
			if (useR && rTarget != null && R.IsReady() && GetComboDamage(rTarget) >= rTarget.Health + 100 && Player.Distance(rTarget) <= R.Range && menu.Item("DontR").GetValue<bool>() == false)
            {
                R.CastOnUnit(rTarget, packets());
                return;
            }
        }
        public static bool packets()
        {
            return menu.Item("packet").GetValue<bool>();
        }

        private static void Harass()
        {
            UseSpells(menu.Item("UseQHarass").GetValue<bool>(), menu.Item("UseWHarass").GetValue<bool>(),
                menu.Item("UseEHarass").GetValue<bool>(), false);
        }
		
			
		private static void CastE(Obj_AI_Hero target)
        {
            PredictionOutput pred = E.GetPrediction(target);
            var vec = new Vector3(pred.CastPosition.X - Player.ServerPosition.X, 0,
                pred.CastPosition.Z - Player.ServerPosition.Z);
            Vector3 castBehind = pred.CastPosition + Vector3.Normalize(vec) * 125;

            if (E.IsReady())
                E.Cast(castBehind, packets());
        }
		
		private static void CastEEscape(Obj_AI_Hero target)
        {
            PredictionOutput pred = E.GetPrediction(target);
            var vec = new Vector3(pred.CastPosition.X - Player.ServerPosition.X, 0,
                pred.CastPosition.Z - Player.ServerPosition.Z);
            Vector3 castBehind = pred.CastPosition - Vector3.Normalize(vec) * 125;

            if (E.IsReady())
                E.Cast(castBehind, packets());
        }
		
		private static void Escape()
        {
            Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);

            List<Obj_AI_Hero> enemy = (from champ in ObjectManager.Get<Obj_AI_Hero>() where champ.IsValidTarget(1000) select champ).ToList();

             if (W.IsReady())
            {
                W.Cast(Game.CursorPos);
            }

            if (E.IsReady() && Player.Distance(enemy.FirstOrDefault()) <= E.Range)
            {
                CastEEscape(enemy.FirstOrDefault());
            }
        }


        private static void Game_OnGameUpdate(EventArgs args)
        {
            //check if player is dead
            if (Player.IsDead) return;


           

            Orbwalker.SetAttack(true);

		    if (menu.Item("escape").GetValue<KeyBind>().Active)
            {
                Escape();
            }
            else if(menu.Item("ComboActive").GetValue<KeyBind>().Active)
            {
                Combo();
            }
            else
            {
                if (menu.Item("HarassActive").GetValue<KeyBind>().Active || menu.Item("HarassActiveT").GetValue<KeyBind>().Active)
                    Harass();
					
				
				

               /*  if (menu.Item("LastHitQQ").GetValue<KeyBind>().Active)
                    lastHit();

                if (menu.Item("LaneClearActive").GetValue<KeyBind>().Active)
                    Farm(); */

            }
        }

		
		
      /*   public static void lastHit()
        {
            if (!Orbwalking.CanMove(40)) return;

            var allMinions = MinionManager.GetMinions(Player.ServerPosition, Q.Range);

            if (Q.IsReady())
            {
                foreach (var minion in allMinions)
                {
                    if (minion.IsValidTarget() && HealthPrediction.GetHealthPrediction(minion, (int)(Player.Distance(minion) * 1000 / 1400)) < Damage.GetSpellDamage(Player, minion, SpellSlot.Q) - 10)
                    {
                        Q.CastOnUnit(minion);
                        return;
                    }
                }
            }
        }

        private static void Farm()
        {
            var allMinionsQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.NotAlly);
            var rangedMinionsE = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range + E.Width, MinionTypes.All);

            var useQ = menu.Item("UseQFarm").GetValue<bool>();
           

           if (useQ && Q.IsReady())
            {
                var minionsQ = MinionManager.GetMinions(Player.ServerPosition, Q.Range);

                foreach (var vMinion in from vMinion in minionsQ
                    let vMinionEDamage = Player.GetSpellDamage(vMinion, SpellSlot.Q)
                    where vMinion.Health <= vMinionEDamage && vMinion.Health > Player.GetAutoAttackDamage(vMinion)
                    select vMinion) 
                {
                    Q.CastOnUnit(Player);
                }
            }
        } */

       
        private static void Drawing_OnDraw(EventArgs args)
        {
            foreach (var spell in SpellList)
            {
                var menuItem = menu.Item(spell.Slot + "Range").GetValue<Circle>();
                if (menuItem.Active)
                    Utility.DrawCircle(Player.Position, spell.Range, menuItem.Color);
            }

        }

    }
}
