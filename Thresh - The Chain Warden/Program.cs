using System;
using System.Collections;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using System.Drawing;
using Color = System.Drawing.Color;
using System.Collections.Generic;
using System.Threading;

namespace Thresh___The_Chain_Warden
{
  class Program
  {
    private static Orbwalking.Orbwalker Orbwalker;

    private static Spell Q, Q2, W, E, R; //Same declaration as every new line, null object variable

    private static SpellSlot FlashSlot;

    public static float FlashRange = 450f;

    private static float CheckInterval = 50f;
    private static readonly Dictionary<int, List<Vector2>> _waypoints = new Dictionary<int, List<Vector2>>();
    private static float _lastCheck = Environment.TickCount;
    private static List<Spell> SpellList = new List<Spell>() { Q, Q2, W, E, R }; //Instead of SpellList.Add();

    private static Menu Config;
    public static Vector2 oWp;
    public static Vector2 nWp;
    public static Obj_AI_Hero Player = ObjectManager.Player;
	public static Boolean Q1 = true;

    static void Main(string[] args)
    {
      CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
    }
    static void Game_OnGameLoad(EventArgs args)
    {
      if (Player.ChampionName != "Thresh") return;
      Notifications.AddNotification("Thresh - The Chain Warden by DanZ Loaded!", 1000);
      FlashSlot = Player.GetSpellSlot("SummonerFlash");

      Q = new Spell(SpellSlot.Q, 1100);
      Q2 = new Spell(SpellSlot.Q, 1400);
      W = new Spell(SpellSlot.W, 950);
      E = new Spell(SpellSlot.E, 390);
      R = new Spell(SpellSlot.R, 450);

      /* Q.SetSkillshot(0.500f, 70, 1900f, true, SkillshotType.SkillshotLine);
      Q2.SetSkillshot(0.500f, 70, 1900f, true, SkillshotType.SkillshotLine); */
	   Q.SetSkillshot(0.35f, 60, 1200, true, SkillshotType.SkillshotLine);
	   Q2.SetSkillshot(0.35f, 60, 1200, true, SkillshotType.SkillshotLine);
       W.SetSkillshot(0.25f, 300, 1750, false, SkillshotType.SkillshotCircle);
       E.SetSkillshot(1, 110, 2000, false, SkillshotType.SkillshotLine);

      Config = new Menu("Thresh", "thresh_menu", true);
      var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
      TargetSelector.AddToMenu(targetSelectorMenu);
      Config.AddSubMenu(targetSelectorMenu);
      Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
      Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));

      Config.AddSubMenu(new Menu("Combo", "Combo"));
      Config.SubMenu("Combo").AddItem(new MenuItem("UseQCombo", "Use Q")).SetValue(true);
/*       Config.SubMenu("Combo").AddItem(new MenuItem("UseWCombo", "Use W")).SetValue(true); */
      Config.SubMenu("Combo").AddItem(new MenuItem("UseECombo", "Use E")).SetValue(true);
      Config.SubMenu("Combo").AddItem(new MenuItem("UseRCombo", "Use R")).SetValue(true);
      Config.SubMenu("Combo").AddItem(new MenuItem("EPush", "E Push/Pull(on/off)")).SetValue(true);//get used to check out commit
	  
	   var lantMenu = new Menu("Slutty Lantern Settings", "lantern");
            {
                foreach (var hero in 
                    ObjectManager.Get<Obj_AI_Hero>()
                        .Where(x => x.IsAlly))
                {
                    {
                        lantMenu.AddItem(new MenuItem("healop" + hero.ChampionName, hero.ChampionName))
                            .SetValue(new StringList(new[] {"Lantern", "No Lantern"}));

                        lantMenu.AddItem(
                            new MenuItem("hpsettings" + hero.ChampionName, "Lantern When %HP <").SetValue(
                                new Slider(20)));
                    }

                }
                lantMenu.AddItem(new MenuItem("manalant", "%Mana for lantern").SetValue(new Slider(50)));
                lantMenu.AddItem(new MenuItem("autolantern", "Auto Lantern Ally When Q hits").SetValue(false));
            }
		Config.AddSubMenu(lantMenu);	

      Config.AddSubMenu(new Menu("Harass", "Harass"));
      Config.SubMenu("Harass").AddItem(new MenuItem("UseQHarass", "Use Q")).SetValue(true);
      Config.SubMenu("Harass").AddItem(new MenuItem("UseEHarass", "Use E")).SetValue(true);

      Config.AddSubMenu(new Menu("Flay", "Flay"));
      Config.SubMenu("Flay").AddItem(new MenuItem("Push", "Push")).SetValue(new KeyBind('I', KeyBindType.Press));
      Config.SubMenu("Flay").AddItem(new MenuItem("Pull", "Pull")).SetValue(new KeyBind('U', KeyBindType.Press));


		
      Config.AddSubMenu(new Menu("Flash Hook", "Fhook"));
      Config.SubMenu("Fhook").AddItem(new MenuItem("FlashQCombo", "Flash + Hook").SetValue(new KeyBind('G', KeyBindType.Press)));

      Config.AddSubMenu(new Menu("Interrupts", "Interrupts"));
      Config.SubMenu("Interrupts").AddItem(new MenuItem("EInterrupt", "Interrupt Spells with E").SetValue(true));

      Config.AddSubMenu(new Menu("Gap Closers", "GapClosers"));
      Config.SubMenu("GapClosers").AddItem(new MenuItem("EGapCloser", "Auto use E away on Gap Closers").SetValue(true));
      Config.SubMenu("GapClosers").AddItem(new MenuItem("RGapCloser", "Auto use R on Gap Closers").SetValue(false));

      Config.AddSubMenu(new Menu("Lantern Settings", "LanternSettings"));
      Config.SubMenu("LanternSettings").AddItem(new MenuItem("ThrowLantern", "Throw Lantern to Ally")).SetValue(new KeyBind('T', KeyBindType.Press));
      /* Config.SubMenu("LanternSettings").AddItem(new MenuItem("ThrowLanternNear", "Prioritize Nearest Ally")).SetValue(true);
      Config.SubMenu("LanternSettings").AddItem(new MenuItem("ThrowLanternLife", "Prioritize Low Ally")).SetValue(false);
 */
      Config.AddSubMenu(new Menu("Drawings", "Drawings"));
      Config.SubMenu("Drawings").AddItem(new MenuItem("drawEnable", "Enable Drawing")).SetValue(true);
      Config.SubMenu("Drawings").AddItem(new MenuItem("drawQpred", "Draw Q line prediction")).SetValue(true);
      Config.SubMenu("Drawings").AddItem(new MenuItem("drawQ", "Draw Q")).SetValue(true);
      Config.SubMenu("Drawings").AddItem(new MenuItem("drawW", "Draw W")).SetValue(true);
      Config.SubMenu("Drawings").AddItem(new MenuItem("drawE", "Draw E")).SetValue(true);
      Config.SubMenu("Drawings").AddItem(new MenuItem("drawR", "Draw R")).SetValue(true);

      Config.AddSubMenu(new Menu("Debug", "Debug"));
      Config.SubMenu("Debug").AddItem(new MenuItem("debugE", "Debug E")).SetValue(false);
      Config.SubMenu("Debug").AddItem(new MenuItem("debugFlash", "Debug flash+hook")).SetValue(false);

      Config.AddToMainMenu();

      /* OnBeforeAttack();  *///You can use OnBeforeAttack event here instead of declaring new delegate in function
      Game.OnUpdate += OnGameUpdate;
      Drawing.OnDraw += OnDraw;
      AntiGapcloser.OnEnemyGapcloser += OnEnemyGapcloser;
      Interrupter2.OnInterruptableTarget += OnPossibleToInterrupt;
      CustomEvents.Unit.OnDash += Unit_OnDash;
    }


	private static Obj_AI_Base HookedTarget
        {
            get
            {
                return ObjectManager.Get<Obj_AI_Base>().FirstOrDefault(x => x.IsEnemy && x.HasBuff("ThreshQ"));
            }
        }


    private static void OnDraw(EventArgs args)
    {
      var myPos = Drawing.WorldToScreen(Player.Position);

      if (Config.Item("drawEnable").GetValue<bool>())
      {
        if (Config.Item("drawQ").GetValue<bool>())
        {

          Render.Circle.DrawCircle(ObjectManager.Player.Position, Q.Range, Color.Aqua, 1);
        }

        if (Config.Item("drawW").GetValue<bool>())
        {

          Render.Circle.DrawCircle(ObjectManager.Player.Position, W.Range, Color.Aqua, 1);
        }
        if (Config.Item("drawE").GetValue<bool>())
        {

          Render.Circle.DrawCircle(ObjectManager.Player.Position, E.Range, Color.Aqua, 1);
        }
        if (Config.Item("drawR").GetValue<bool>())
        {

          Render.Circle.DrawCircle(ObjectManager.Player.Position, R.Range, Color.Aqua, 1);
        }

      }
      var enemy = TargetSelector.GetTarget(1500, TargetSelector.DamageType.Magical);
      List<Vector2> waypoints = enemy.GetWaypoints();
      for (int i = 0; i < waypoints.Count - 1; i++)
      {


        oWp = Drawing.WorldToScreen(waypoints[i].To3D());
        nWp = Drawing.WorldToScreen(waypoints[i + 1].To3D());
        if (!waypoints[i].IsOnScreen() && !waypoints[i + 1].IsOnScreen())
        {
          continue;
        }
        //Drawing.DrawLine(oWp[0], oWp[1], nWp[0], nWp[1], 3, System.Drawing.Color.Red);


        //var pos = Player.Position + Vector3.Normalize(enemy.Position - Player.Position) * 100;
        //pos = Player.Position + Vector3.Normalize(enemy.Position - Player.Position) * Player.Distance3D(enemy);
        //var ePos = Drawing.WorldToScreen(pos);


        if (Config.Item("drawQpred").GetValue<bool>())
        {
          Drawing.DrawLine(myPos.X - 25, myPos.Y - 25, nWp[0] - 10, nWp[1] - 25, 1, Color.Red);
          Drawing.DrawLine(myPos.X + 25, myPos.Y + 25, nWp[0] + 10, nWp[1] + 25, 1, Color.Red);
        }

        if (Config.Item("debugFlash").GetValue<bool>())
        {
          Q2.UpdateSourcePosition(V2E(ObjectManager.Player.Position, enemy.Position, 400).To3D());
          var predPos = Q2.GetPrediction(enemy);
          Render.Circle.DrawCircle(V2E(ObjectManager.Player.Position, enemy.Position, 400).To3D(), 100, Color.Aqua, 1);
          Drawing.DrawLine(Drawing.WorldToScreen(V2E(ObjectManager.Player.Position, enemy.Position, 400).To3D()), Drawing.WorldToScreen(predPos.CastPosition), 2, Color.Aqua);
          var toScreen = Drawing.WorldToScreen(enemy.Position);
          Drawing.DrawText(toScreen.X + 70, toScreen.Y, Color.Aqua, predPos.Hitchance.ToString());
        }

        if (Config.Item("debugE").GetValue<bool>())
        {
          var target2 = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);
          if (!Config.Item("EPush").GetValue<bool>())
          {
            Render.Circle.DrawCircle(V2E(target2.Position, Player.Position, Player.Distance(target2.Position) + 400).To3D(), 100, Color.Red, 1);
          }
          else
          {
            Render.Circle.DrawCircle(target2.Position, 100, Color.Red, 1);
          }
        }
      }
    }



    private static void DrawLine(float x, float y, float x2, float y2, float thickness, System.Drawing.Color color)
    {
    }

	 private static void wcast()
        {
			 var target = TargetSelector.GetTarget(1500, TargetSelector.DamageType.Magical);
            if (Player.ManaPercent < Config.Item("manalant").GetValue<Slider>().Value || target == null)
                return;
           // Obj_AI_Hero target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
            foreach (var hero in
                HeroManager.Allies.Where(x => !x.IsDead
                                              && x.Distance(Player) <= W.Range))
            {
                if (hero.HealthPercent <= Config.Item("hpsettings" + hero.ChampionName).GetValue<Slider>().Value)
                {
                    if (Config.Item("healop" + hero.ChampionName).GetValue<StringList>().SelectedIndex == 0)
                                W.Cast(hero.Position - 100);
                }
            }

        }
		
		
		
		private static void wcast2()
        {
             var target = TargetSelector.GetTarget(1500, TargetSelector.DamageType.Magical);
		   if (HookedTarget != null && Config.Item("autolantern").GetValue<bool>()
                && W.IsReady())
            /* if ((args.SData.Name == "threshqinternal" || args.SData.Name == "ThreshQ")
                && Config.Item("autolantern").GetValue<bool>()
                && W.IsReady()) */
            {
                foreach (var heros in
                    HeroManager.Allies.Where(x => !x.IsMe
                                                  && x.Distance(Player) <= W.Range))
                    {
                        Utility.DelayAction.Add(400, () => W.Cast(heros.Position - 100));
                    }
            }
        }

		
	static void Unit_OnDash(Obj_AI_Base sender, Dash.DashItem args)
        {
            /* var target = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);

            if (!sender.IsEnemy)
                return;

            if (sender.NetworkId == target.NetworkId)
            {
                if (E.IsReady()
                   && E.IsInRange(sender.ServerPosition))
                {
                    E.Cast(Player.Position.Extend(sender.Position, 400));
                }
            }
			Q.CastIfHitchanceEquals(target, HitChance.Dashing, true); */
			if (sender != null && sender.IsEnemy && Q1 != false && HookedTarget == null && sender.IsValidTarget(2000) && sender.Type == Player.Type && !sender.IsZombie)
            {
				if (args.EndPos.Distance(Player.Position) < 1000 && !sender.HasBuff("ThreshQ") && !Q.GetPrediction(sender).CollisionObjects.Any(h => h.IsEnemy && !h.IsDead/*  && h is Obj_AI_Minion */))
				{
                /* Q.Cast(args.EndPos); */
				Q.Cast(args.EndPos + 100);
				Q1 = false;
				Utility.DelayAction.Add(3000, () => Q1 = true);
				}
            }
			if (sender != null && sender.IsEnemy && sender.IsValidTarget(1500) && sender.Type == Player.Type && !sender.IsZombie)
            {
				if (args.EndPos.Distance(Player.Position) < 370 || args.StartPos.Distance(Player.Position) < 370)
				{
                 E.Cast(sender.Position.Extend(Player.Position, Vector3.Distance(sender.Position, Player.Position) + 400));
				}
            }
        }
		
		

    private static void ThrowLantern()
    {
      if (W.IsReady())
      {
        var NearAllies = Player.GetAlliesInRange(W.Range) //W.Range instead of 1200, also there is no "On most damaged"
                        .Where(x => !x.IsMe)
                        .Where(x => !x.IsDead)
                        .Where(x => x.Distance(Player.Position) <= W.Range + 250)
						.OrderBy(x => x.HealthPercentage())
                        .FirstOrDefault();

        if (NearAllies == null) return;

        W.Cast(NearAllies.Position);


      }

    }

    private static void OnGameUpdate(EventArgs args)
    {
      var targetz = TargetSelector.GetTarget(5000, TargetSelector.DamageType.Magical, true);
      DrawLine(Player.Position.X, Player.Position.Y, targetz.Position.X, targetz.Position.Y, 2, Color.Red);

      if (Config.Item("Push").GetValue<KeyBind>().Active)
      {
        Push();
      }
      if (Config.Item("Pull").GetValue<KeyBind>().Active)
      {
        Pull();
      }
      if (Config.Item("FlashQCombo").GetValue<KeyBind>().Active)
      {
        FlashQCombo();
      }
      if (Config.Item("ThrowLantern").GetValue<KeyBind>().Active)
      {
        ThrowLantern();
      }
      switch (Orbwalker.ActiveMode)
      {
        case Orbwalking.OrbwalkingMode.Combo:
          Combo();
          break;
        case Orbwalking.OrbwalkingMode.Mixed:
          Harass();
          break;
      }
	  
	
	  wcast();
		wcast2();
    }

    private static void OnPossibleToInterrupt(Obj_AI_Hero target, Interrupter2.InterruptableTargetEventArgs args)
    {
      if (Config.Item("EInterrupt").GetValue<bool>() && E.IsReady() && E.IsInRange(target))
      {
        E.Cast(target.ServerPosition);
      }
    }
    private static void OnEnemyGapcloser(ActiveGapcloser gapcloser)
    {
      if (gapcloser.Sender.IsAlly)
      {
        return;
      }

      if (Config.Item("EGapCloser").GetValue<bool>() && E.IsReady() && E.IsInRange(gapcloser.Start))
      {
        E.Cast(Player.Position.Extend(gapcloser.Sender.Position, 250));
      }
      if (Config.Item("RGapCloser").GetValue<bool>() && R.IsReady() && R.IsInRange(gapcloser.Start))
       {
		  R.Cast();
      }
    }
		
	

    /* private static void OnBeforeAttack()
    {
      Orbwalking.BeforeAttack += args =>
      {
        try
        {
          if (args.Target.IsValid<Obj_AI_Minion>() && Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
          {
            args.Process = false;
          }
        }
        catch (Exception e)
        {
          Console.WriteLine(e);
        }
      };
    } */

    static Vector2 V2E(Vector3 from, Vector3 direction, float distance)
    {
      return from.To2D() + distance * Vector3.Normalize(direction - from).To2D();
    }

    private static void Pull()
    {
      var target = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);

      if (E.IsReady() && Player.Distance(target.Position) < E.Range)
      {
        E.Cast(target.Position.Extend(Player.Position, Vector3.Distance(target.Position, Player.Position) + 400));
      }
    }

    private static void Push()
    {
      var target = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);
      if (E.IsReady() && Player.Distance(target.Position) < E.Range)
      {
        E.Cast(target.Position);
      }
    }

    private static void Harass()
    {
      var target = TargetSelector.GetTarget(1000, TargetSelector.DamageType.Magical);

      if (Q.IsReady() && Config.Item("UseQHarass").GetValue<bool>())
      {
        var Qprediction = Q.GetPrediction(target);

        if (Qprediction.Hitchance >= HitChance.High && Q1 != false && HookedTarget == null && !Qprediction.CollisionObjects.Any(h => h.IsValidTarget() && h.IsEnemy && !h.IsDead && h is Obj_AI_Minion))
        {
          Q.Cast(Qprediction.CastPosition);
		  Q1 = false;
		  Utility.DelayAction.Add(3000, () => Q1 = true);
        }

      }

      if (E.IsReady() && Config.Item("UseEHarass").GetValue<bool>() && Player.Distance(target.Position) < E.Range)
      {
        E.Cast(V2E(target.Position, Player.Position, Player.Distance(target.Position) + 400));
      }
    }
    private static void Combo()
    {
      var target = TargetSelector.GetTarget(1000, TargetSelector.DamageType.Magical);
      if (Q.IsReady() && (Config.Item("UseQCombo").GetValue<bool>()))
      {
        var Qprediction = Q.GetPrediction(target);
        if ((Qprediction.Hitchance >= HitChance.VeryHigh || Qprediction.Hitchance == HitChance.Immobile) && Q1 != false && HookedTarget == null && !Qprediction.CollisionObjects.Any(h => h.IsValidTarget() && h.IsEnemy && !h.IsDead && h is Obj_AI_Minion))
        {
          Q.Cast(Qprediction.CastPosition);
		  Q1 = false;
		  Utility.DelayAction.Add(3000, () => Q1 = true);
        }
      }

      if (E.IsReady() && Config.Item("UseECombo").GetValue<bool>() && Vector3.Distance(target.Position, Player.Position) < E.Range)
      {
        if (!Config.Item("EPush").GetValue<bool>())
        {
          E.Cast(target.Position.Extend(Player.Position, Vector3.Distance(target.Position, Player.Position) + 400));
        }
        else
        {
          E.Cast(target.Position);
        }
      }

      if (R.IsReady() && (Config.Item("UseRCombo").GetValue<bool>()) && Player.CountEnemiesInRange(380) >= 2)
      {
        R.Cast();
      }


    }
   private static void FlashQCombo()
    {
      Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
      var target = TargetSelector.GetTarget(Q2.Range, TargetSelector.DamageType.Magical);

      if (Player.Distance3D(target) > Q.Range)
      {
        if (FlashSlot != SpellSlot.Unknown && Player.Spellbook.CanUseSpell(FlashSlot) == SpellState.Ready && Q.IsReady())
        {
          Q2.UpdateSourcePosition(V2E(ObjectManager.Player.Position, target.Position, FlashRange).To3D());
          var predPos = Q2.GetPrediction(target);
          if (predPos.Hitchance != HitChance.VeryHigh || Q1 == false || HookedTarget == null) //What does "Madlife" mean?
            return;
          Player.Spellbook.CastSpell(FlashSlot, predPos.CastPosition);
          Q.Cast(predPos.CastPosition);
		  Q1 = false;
		  Utility.DelayAction.Add(3000, () => Q1 = true);

        }
      }
	}
  }

}
