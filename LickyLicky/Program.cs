using System;
using System.Collections.Generic;
using System.Drawing.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using SharpDX;
using System.Text;
using LeagueSharp;
using LeagueSharp.Common;
using Menu = LeagueSharp.Common.Menu;
using MenuItem = LeagueSharp.Common.MenuItem;

namespace LickyLicky
{
    class Program
    {
        static Menu mainMenu = new Menu("Tahm", "Tahm", true);
        private static Obj_AI_Hero Player;
        private static Spell Q, W, E, R;

        
        static void Main(string[] args)
        {
            try
            {
                CustomEvents.Game.OnGameLoad += OnLoad;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        static void OnLoad(EventArgs args)
        {
            Player = LeagueSharp.ObjectManager.Player;
            if (!Player.CharData.BaseSkinName.ToLower().Contains("tahm"))
                return;
            Q = new Spell(SpellSlot.Q, 790);
            Q.SetSkillshot(.1f, 75, 2000, true, SkillshotType.SkillshotLine);
            W = new Spell(SpellSlot.W, 250);
            E = new Spell(SpellSlot.E);
            R = new Spell(SpellSlot.R);

            PopulateMenu();
            Game.OnUpdate += OnUpdate;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpell; 
            Interrupter2.OnInterruptableTarget += OnInterruptableSpell;
            Drawing.OnDraw += Drawing_OnDraw;
			// AntiGapcloser.OnEnemyGapcloser += OnEnemyGapcloser;

        }

        static void PopulateMenu()
        {
			Menu tsMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(tsMenu);
            mainMenu.AddSubMenu(tsMenu);
				
			Menu comboMenu = new Menu("Combo", "Combo");
            comboMenu.AddItem(new MenuItem("Combo", "Combo").SetValue(new KeyBind(' ', KeyBindType.Press)));
            comboMenu.AddItem(new MenuItem("Use Q", "Use Q").SetValue(true));
            comboMenu.AddItem(new MenuItem("Use W", "Use W (auto)").SetValue(true));
			comboMenu.AddItem(new MenuItem("Use E", "Use E (auto)").SetValue(true));			
			comboMenu.AddItem(new MenuItem("EHP", "E % HP").SetValue(true)).SetValue(new Slider(20, 0, 100));

			
            Menu harassMenu = new Menu("Harass", "Harass");
            harassMenu.AddItem(new MenuItem("Harass", "Harass").SetValue(new KeyBind('C', KeyBindType.Press)));
            harassMenu.AddItem(new MenuItem("Use Q", "Use Q").SetValue(true));
            harassMenu.AddItem(new MenuItem("Harass Mana Percent", "Harass Mana Percent").SetValue(new Slider(30)));

			Menu saveMenu = new Menu("Save", "Save");
		 	saveMenu.AddItem(new MenuItem("Wsave", "HP Save ally")).SetValue(true);
			saveMenu.AddItem(new MenuItem("useWSafeHP", "%HP Save")).SetValue(new Slider(10, 0, 100));
            saveMenu.AddItem(new MenuItem("shieldTargeted", "Shield targeted skills")).SetValue(true);
            saveMenu.AddItem(new MenuItem("shieldifXHpAmount", "...if ally looses X % of maxHP")).SetValue(new Slider(20));
            saveMenu.AddItem(new MenuItem("eatCCdAllies", "W cc'ed allies")).SetValue(true);
			saveMenu.AddItem(new MenuItem("eatCCdAlliesHP", "%HP W cc'ed allies")).SetValue(new Slider(50, 0, 100));
		

            Menu fleeMenu = new Menu("Flee", "Flee");
            fleeMenu.AddItem(new MenuItem("Flee", "Flee").SetValue(new KeyBind('Z', KeyBindType.Press)));
            fleeMenu.AddItem(new MenuItem("Use Q", "Use Q").SetValue(true));
            fleeMenu.AddItem(new MenuItem("W Ally", "W Ally").SetValue(true));
			fleeMenu.AddItem(new MenuItem("WAHP", "W Ally HP%")).SetValue(new Slider(20, 0, 100));
			

            Menu miscMenu = new Menu("Misc", "Misc");
            miscMenu.AddItem(new MenuItem("Interrupt With Q", "Interrupt With Q").SetValue(true));
            miscMenu.AddItem(new MenuItem("Interrupt with W", "Interrupt With W").SetValue(true));
			// miscMenu.AddItem(new MenuItem("Gapcloser With Q", "Gapcloser With Q").SetValue(true));
			
            Menu orbwalkingMenu = new Menu("Orbwalking", "Orbwalking");
            Orbwalking.Orbwalker walker = new Orbwalking.Orbwalker(orbwalkingMenu);

            Menu drawingMenu = new Menu("Drawing","Drawing");
            drawingMenu.AddItem(new MenuItem("Draw Q", "Draw Q").SetValue(true));
			drawingMenu.AddItem(new MenuItem("Draw R", "Draw R").SetValue(true));

            mainMenu.AddSubMenu(orbwalkingMenu);
			mainMenu.AddSubMenu(comboMenu);
            mainMenu.AddSubMenu(harassMenu);
			mainMenu.AddSubMenu(saveMenu);
            mainMenu.AddSubMenu(fleeMenu);
            mainMenu.AddSubMenu(miscMenu);
            mainMenu.AddSubMenu(drawingMenu);

            mainMenu.AddToMainMenu();
        }
        static void OnUpdate(EventArgs args)
        {
            if (Player.IsDead)
                return;
			
	
			
			if (mainMenu.SubMenu("Combo").Item("Use W").IsActive() && W.IsReady())
			 {
                var target = TargetSelector.GetTarget(300, TargetSelector.DamageType.Magical);
		    	var t = (((3 * W.Level) + 17) + (0.02 * Player.FlatMagicDamageMod));
						
                if (target != null && target.HasBuff("tahmkenchpdevourable", true) && !Player.HasBuff("TahmKenchWHasDevouredTarget", true) && target.HealthPercent > t)
				{
					W.CastOnUnit(target);
				}
		
			    else if (target != null && target.HasBuff("tahmkenchpdevourable", true) && !Player.HasBuff("TahmKenchWHasDevouredTarget", true) && target.HealthPercent <= t)
				{
				W.CastOnUnit(target);
				Utility.DelayAction.Add(
                    1000, () =>
                    {
                        if (Player.HasBuff("TahmKenchWHasDevouredTarget", true))
                        {
                           W.Cast(Game.CursorPos);
                        }
                    });
                }
			 }
			
		
			 			
			if (mainMenu.SubMenu("Combo").Item("Use E").IsActive() && E.IsReady())
			{
				var target = TargetSelector.GetTarget(1000, TargetSelector.DamageType.Magical);
				if (target != null && Player.HealthPercent <= mainMenu.SubMenu("Combo").Item("EHP").GetValue<Slider>().Value)
				E.Cast();
			}
			
			if (mainMenu.SubMenu("Combo").Item("Combo").IsActive())
            {
                var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
						
                if (target != null && Q.IsReady() && mainMenu.SubMenu("Combo").Item("Use Q").IsActive() && Player.Distance(target) >=100 && !target.HasBuff("TahmKenchWHasDevouredTarget", true))
				{
				Q.CastIfHitchanceEquals(target, HitChance.Low, true);
				}
			
				
            }
			
            else if (mainMenu.SubMenu("Harass").Item("Harass").IsActive())
            {

                if (Player.ManaPercent >= mainMenu.SubMenu("Harass").Item("Harass Mana Percent").GetValue<Slider>().Value)
                {
                    Obj_AI_Hero target =
                        ObjectManager.Get<Obj_AI_Hero>().Where(x => x.Distance(Player) <= Q.Range && x.Distance(Player) >= 100 && x.IsEnemy && x.IsTargetable && !x.IsDead)
                            .FirstOrDefault();
                    
                        if (mainMenu.SubMenu("Harass").Item("Use Q").IsActive())
                        {		
						Q.CastIfHitchanceEquals(target, HitChance.Low, true);
						}                   
                    
                }
            }
            else if (mainMenu.SubMenu("Flee").Item("Flee").IsActive())
            {

                try
                {
                    if (mainMenu.SubMenu("Flee").Item("Use Q").IsActive())
                    {
                        Obj_AI_Hero target =
                            ObjectManager.Get<Obj_AI_Hero>().Where(
                                x => x.Distance(Player) <= Q.Range && x.IsEnemy && x.IsTargetable && !x.IsDead)
                                .FirstOrDefault();
                        if (target != null)
                            Q.Cast(target);
                    }
                }
                catch { }

                if (mainMenu.SubMenu("Flee").Item("W Ally").IsActive() && !Player.HasBuff("TahmKenchWHasDevouredTarget", true))
                {

                    try
                    {
                        var Ally =
                        ObjectManager.Get<Obj_AI_Hero>().FirstOrDefault(
                            x => x.HealthPercent < mainMenu.SubMenu("Flee").Item("WAHP").GetValue<Slider>().Value
                                && x.IsAlly && Player.Distance(x) <= 500
                                && !x.IsDead);
						if (Ally != null)
                            W.CastOnUnit(Ally);

                    }
					catch { }
                }
                Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);

            }

			CheckCCdAlly();
			
			HPsave();
        }
		
		static void OnProcessSpell(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!mainMenu.SubMenu("Save").Item("shieldTargeted").GetValue<bool>() || !W.IsReady() || Player.HasBuff("TahmKenchWHasDevouredTarget", true))
                return;

            if (sender.IsEnemy && args.SData.TargettingType == SpellDataTargetType.Unit 
                && !args.Target.IsMe && !args.Target.IsEnemy)
            {
                var target = (Obj_AI_Hero)args.Target;
				
                var spell = sender.Spellbook.Spells.FirstOrDefault(x => args.SData.Name.Contains(x.Name));

                if (args.Target.Position.Distance(ObjectManager.Player.Position) <= W.Range)
                {
                    if (Damage.GetSpellDamage(sender, target, spell.Name) >= 
                        target.MaxHealth * (mainMenu.SubMenu("Save").Item("shieldifXHpAmount").GetValue<Slider>().Value / 100))
                        W.CastOnUnit(target);
                }    
            }
        }
		
		private static void HPsave()
		{
			if (!mainMenu.SubMenu("Save").Item("Wsave").GetValue<bool>() || Player.HasBuff("TahmKenchWHasDevouredTarget", true)) return;
		var Ally =
                        ObjectManager.Get<Obj_AI_Hero>().FirstOrDefault(
                            x => x.HealthPercent < mainMenu.SubMenu("Save").Item("useWSafeHP").GetValue<Slider>().Value
                                && x.IsAlly && Player.Distance(x) <= 500
                                && !x.IsDead);
		var target =
                    ObjectManager.Get<Obj_AI_Hero>().FirstOrDefault(
                            x => x.IsEnemy && x.Distance(Ally) <= 1000 && !x.IsDead);
						 if (W.IsReady() && Ally != null && target != null)
                {
                    W.CastOnUnit(Ally);
                }
		}
		
		private static void CheckCCdAlly()
        {
            if (!mainMenu.SubMenu("Save").Item("eatCCdAllies").GetValue<bool>() || Player.HasBuff("TahmKenchWHasDevouredTarget", true)) return;

			var Ally =
                        ObjectManager.Get<Obj_AI_Hero>().FirstOrDefault(
                            x => x.HealthPercent < mainMenu.SubMenu("Save").Item("eatCCdAlliesHP").GetValue<Slider>().Value
                                && x.IsAlly && Player.Distance(x) <= 500
                                && !x.IsDead);
								
            if (Ally.HasBuffOfType(BuffType.Taunt) || Ally.HasBuffOfType(BuffType.Suppression) || Ally.HasBuffOfType(BuffType.Stun) || Ally.HasBuffOfType(BuffType.Snare) ||
                Ally.HasBuffOfType(BuffType.Polymorph) || Ally.HasBuffOfType(BuffType.Blind) || Ally.HasBuffOfType(BuffType.Fear))
            {
                if (W.IsReady() && !Player.HasBuff("TahmKenchWHasDevouredTarget", true))
                {
                    W.CastOnUnit(Ally);
                }
            }


        }
		

        static void OnInterruptableSpell(object enemy, Interrupter2.InterruptableTargetEventArgs args)
        {
            try
            {
                Obj_AI_Hero sender = (Obj_AI_Hero)enemy;
                if (sender.IsAlly)
                    return;
                float distance = sender.Distance(Player);
                if (sender.HasBuff("tahmkenchpdevourable", true))
                {
                    if (distance <= W.Range && mainMenu.SubMenu("Misc").Item("Interrupt With W").IsActive() && !Player.HasBuff("TahmKenchWHasDevouredTarget", true))
                        W.CastOnUnit(sender);
                    else if (distance <= Q.Range && mainMenu.SubMenu("misc").Item("Interrupt With Q").IsActive())
                        Q.Cast(sender);
                }
                else
                {
                    if (distance <= 250)
                        Player.IssueOrder(GameObjectOrder.AttackUnit, sender);
                    if (distance <= Q.Range)
                        Q.Cast(sender);
                }


            }
            catch{}
        }
		
	/* 	static void OnEnemyGapcloser(object enemy, ActiveGapcloser args)
        {
			if (gapcloser.Sender.IsAlly || !mainMenu.SubMenu("misc").Item("Gapcloser With Q").IsActive())   return;
              
			if (gapcloser.Sender.IsValidTarget(Q.Range) && Q.IsReady()) 
			  Q.Cast(gapcloser.Start);
        
        } */
			
			/* private static void OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (mainMenu.SubMenu("misc").Item("Gapcloser With Q").IsActive())
            {           
                if (gapcloser.Sender.IsValidTarget(Q.Range) && Q.IsReady()) Q.Cast(gapcloser.Start);
            }
        } */
		

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (Player.IsDead)
                return;
            if(mainMenu.SubMenu("Drawing").Item("Draw Q").IsActive())
                Render.Circle.DrawCircle(Player.Position, Q.Range, System.Drawing.Color.Red);
			if(R.Level == 1 && mainMenu.SubMenu("Drawing").Item("Draw R").IsActive())
                Render.Circle.DrawCircle(Player.Position, 4000, System.Drawing.Color.Red);
			if(R.Level == 2 && mainMenu.SubMenu("Drawing").Item("Draw R").IsActive())
                Render.Circle.DrawCircle(Player.Position, 5000, System.Drawing.Color.Red);
			if(R.Level == 3 && mainMenu.SubMenu("Drawing").Item("Draw R").IsActive())
                Render.Circle.DrawCircle(Player.Position, 6000, System.Drawing.Color.Red);
        }
    }
}