using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using Color = System.Drawing.Color;

namespace KurisuBlitz
{
    //  _____ _ _ _                       _   
    // | __  | |_| |_ ___ ___ ___ ___ ___| |_ 
    // | __ -| | |  _|- _|  _|  _| .'|   | '_|
    // |_____|_|_|_| |___|___|_| |__,|_|_|_,_|
    //  Copyright © Kurisu Solutions 2015
   
    internal class Program
    {
        private static Menu _menu;
        private static Spell _q, _e, _w, _r;
        private static Orbwalking.Orbwalker _orbwalker;
        private static readonly Obj_AI_Hero Me = ObjectManager.Player;

        static void Main(string[] args)
        {
            Console.WriteLine("Blitzcrank injected...");
            CustomEvents.Game.OnGameLoad += BlitzOnLoad;
        }

        private static void BlitzOnLoad(EventArgs args)
        {
            if (Me.ChampionName != "Blitzcrank")
                return;
           
            // Set spells      
            _q = new Spell(SpellSlot.Q, 1000f);
            _q.SetSkillshot(0.25f, 70f, 1800f, true, SkillshotType.SkillshotLine);
			_w = new Spell(SpellSlot.W, 150f);
            _e = new Spell(SpellSlot.E, 150f);
            _r = new Spell(SpellSlot.R, 550f);

            // Load Menu
            _menu = new Menu("KurisuBlitz", "blitz", true);

            var blitzTs = new Menu("Selector", "tselect");
            TargetSelector.AddToMenu(blitzTs);
            _menu.AddSubMenu(blitzTs);

            var blitzOrb = new Menu("Orbwalker", "orbwalker");
            _orbwalker = new Orbwalking.Orbwalker(blitzOrb);
            _menu.AddSubMenu(blitzOrb);

            var menuD = new Menu("Drawings", "drawings");
            menuD.AddItem(new MenuItem("drawQ", "Draw Q")).SetValue(new Circle(true, Color.FromArgb(150, Color.White)));
            menuD.AddItem(new MenuItem("drawR", "Draw R")).SetValue(new Circle(true, Color.FromArgb(150, Color.White)));
            menuD.AddItem(new MenuItem("drawT", "Draw Target")).SetValue(true);
            _menu.AddSubMenu(menuD);

            var spellmenu = new Menu("Spells", "smenu");

            var menuQ = new Menu("Q Menu", "qmenu");
            menuQ.AddItem(new MenuItem("usecomboq", "Use in Combo")).SetValue(true);
            menuQ.AddItem(new MenuItem("qdashing", "Q on Dashing Enemies")).SetValue(true);
            menuQ.AddItem(new MenuItem("qimmobile", "Q on Immobile Enemies")).SetValue(true);
            menuQ.AddItem(new MenuItem("interruptq", "Use for Interrupt")).SetValue(true);
            menuQ.AddItem(new MenuItem("secureq", "Use for Killsteal")).SetValue(false);
            spellmenu.AddSubMenu(menuQ);
			
			var menuW = new Menu("W Menu", "wmenu");
            menuW.AddItem(new MenuItem("usecombow", "Use in Combo")).SetValue(true);
            menuW.AddItem(new MenuItem("wr", "Distance to W")).SetValue(new Slider(300, 0, (int)_q.Range));
			menuW.AddItem(new MenuItem("wmana", "Mana Save").SetValue(new Slider(40, 0, 100)));
            spellmenu.AddSubMenu(menuW);

            var menuE = new Menu("E Menu", "emenu");
            menuE.AddItem(new MenuItem("usecomboe", "Use in Combo")).SetValue(true);
            menuE.AddItem(new MenuItem("interrupte", "Use for Interrupt")).SetValue(true);
            menuE.AddItem(new MenuItem("securee", "Use for Killsteal")).SetValue(false);
            spellmenu.AddSubMenu(menuE);

            var menuR = new Menu("R Menu", "rmenu");
            menuR.AddItem(new MenuItem("usecombor", "Use in Combo")).SetValue(true);
            menuR.AddItem(new MenuItem("interruptr", "Use for Interrupt")).SetValue(true);
            menuR.AddItem(new MenuItem("securer", "Use for Killsteal")).SetValue(false);
            spellmenu.AddSubMenu(menuR);


            _menu.AddSubMenu(spellmenu);

            var menuM = new Menu("Misc", "bmisc");
            menuM.AddItem(new MenuItem("hitchanceq", "Q Hitchance 1-Low, 4-Very High")).SetValue(new Slider(3, 1, 4));
            menuM.AddItem(new MenuItem("dnd", "Mininum Distance to Q")).SetValue(new Slider(255, 0, (int)_q.Range));
            menuM.AddItem(new MenuItem("dnd2", "Maximum Distance to Q")).SetValue(new Slider((int)_q.Range, 0, (int)_q.Range));
            menuM.AddItem(new MenuItem("hnd", "Dont grab if below health %")).SetValue(new Slider(0));
            foreach (var obj in ObjectManager.Get<Obj_AI_Hero>().Where(obj => obj.Team != Me.Team))
            {
                menuM.AddItem(new MenuItem("dograb" + obj.ChampionName, obj.ChampionName))
                    .SetValue(new StringList(new[] { "Dont Grab ", "Normal Grab ", "Auto Grab" }, 1));
            }

            _menu.AddSubMenu(menuM);
            _menu.AddItem(new MenuItem("combokey", "Combo (active)")).SetValue(new KeyBind(32, KeyBindType.Press));

            _menu.AddToMainMenu();

            // events
            Drawing.OnDraw += BlitzOnDraw;
            Game.OnUpdate += BlitzOnUpdate;
            Interrupter.OnPossibleToInterrupt += BlitzOnInterruptableSpell;

            Game.PrintChat("<font color=\"#FF9900\"><b>KurisuBlitz:</b></font> Loaded");

        }

        private static void BlitzOnInterruptableSpell(Obj_AI_Base unit, InterruptableSpell spell)
        {
            if (_menu.Item("interruptq").GetValue<bool>())
            {
                var prediction = _q.GetPrediction(unit);
                if (prediction.Hitchance >= HitChance.Low)
                {
                    _q.Cast(prediction.CastPosition);
                }
            }

            if (_menu.Item("interruptr").GetValue<bool>())
            {
                if (unit.Distance(Me.ServerPosition, true) <= _r.RangeSqr)
                {
                    _r.Cast();
                }
            }

            if (_menu.Item("interrupte").GetValue<bool>())
            {
                if (unit.Distance(Me.ServerPosition, true) <= _e.RangeSqr)
                {
                    _e.CastOnUnit(Me);
                    Me.IssueOrder(GameObjectOrder.AttackUnit, unit);
                }
            }
        }

        private static void BlitzOnDraw(EventArgs args)
        {
            var target = TargetSelector.GetTarget(_q.Range * 2, TargetSelector.DamageType.Physical);

            if (!Me.IsDead)
            {
                var rcircle = _menu.Item("drawR").GetValue<Circle>();
                var qcircle = _menu.Item("drawQ").GetValue<Circle>();

                if (qcircle.Active)
                    Render.Circle.DrawCircle(Me.Position, _q.Range, qcircle.Color);

                if (rcircle.Active)
                    Render.Circle.DrawCircle(Me.Position, _r.Range, qcircle.Color);

                if (target.IsValidTarget(_q.Range * 2) && _menu.Item("drawT").GetValue<bool>())
                    Render.Circle.DrawCircle(target.Position, target.BoundingRadius - 30, Color.Yellow, 3);
            }
        }

        private static void BlitzOnUpdate(EventArgs args)
        {
            // kill secure
            Secure(_menu.Item("secureq").GetValue<bool>(), _menu.Item("securee").GetValue<bool>(),
                   _menu.Item("securer").GetValue<bool>());

            // auto grab
            AutoCast(_menu.Item("qdashing").GetValue<bool>(),
                     _menu.Item("qimmobile").GetValue<bool>());

            if ((int) (Me.Health/Me.MaxHealth*100) >= _menu.Item("hnd").GetValue<Slider>().Value)
            {
                if (_menu.Item("combokey").GetValue<KeyBind>().Active)
                {
                    Combo(_menu.Item("usecomboq").GetValue<bool>(),
						  _menu.Item("usecombow").GetValue<bool>(),
                          _menu.Item("usecomboe").GetValue<bool>());
                }
            }
        }

        private static void AutoCast(bool dashing, bool immobile)
        {
            if (_q.IsReady())
            {
                var itarget =
                    ObjectManager.Get<Obj_AI_Hero>()
                        .FirstOrDefault(h => h.IsEnemy && h.Distance(Me.ServerPosition, true) <= _q.RangeSqr);

                if (itarget.IsValidTarget(_q.Range))
                {
                    if (dashing && _menu.Item("dograb" + itarget.ChampionName).GetValue<StringList>().SelectedIndex == 2)
                        if (itarget.Distance(Me.ServerPosition) > _menu.Item("dnd").GetValue<Slider>().Value)
                            _q.CastIfHitchanceEquals(itarget, HitChance.Dashing);

                    if (immobile && _menu.Item("dograb" + itarget.ChampionName).GetValue<StringList>().SelectedIndex == 2)
                        if (itarget.Distance(Me.ServerPosition) > _menu.Item("dnd").GetValue<Slider>().Value)
                         _q.CastIfHitchanceEquals(itarget, HitChance.Immobile);
                }
            }

            if (_r.IsReady())
            {
                var rtarget =
                    ObjectManager.Get<Obj_AI_Hero>()
                    .FirstOrDefault(h => h.IsEnemy && h.Distance(Me.ServerPosition, true) <= _r.RangeSqr);

                if (rtarget.IsValidTarget(_r.Range) && _menu.Item("usecombor").GetValue<bool>())
                {
                    if (!_e.IsReady() && rtarget.HasBuffOfType(BuffType.Knockup))
                        _r.Cast();
                }            
            }
        }

        private static void Combo(bool useq, bool usew, bool usee)
        {
            if (useq && _q.IsReady())
            {
                var qtarget = TargetSelector.GetTargetNoCollision(_q);
                if (qtarget.IsValidTarget(_q.Range))
                {
                    var poutput = _q.GetPrediction(qtarget);
                    if (poutput.Hitchance >= (HitChance) _menu.Item("hitchanceq").GetValue<Slider>().Value + 2)
                    {
                        if (qtarget.Distance(Me.ServerPosition) > _menu.Item("dnd").GetValue<Slider>().Value)
                        {
                            if (_menu.Item("dograb" + qtarget.ChampionName).GetValue<StringList>().SelectedIndex != 0) 
                                _q.Cast(poutput.CastPosition);
                        }
                    }
                }
            }
			
			if (usew && _w.IsReady())
            {
                var wtarget = TargetSelector.GetTarget(1000, TargetSelector.DamageType.Physical);
				if (wtarget.IsValidTarget( _menu.Item("wr").GetValue<Slider>().Value))
                {
                    if (_menu.Item("usecombow").GetValue<bool>() && Me.ManaPercent >= _menu.Item("wmana").GetValue<Slider>().Value)
                        _w.CastOnUnit(Me);                   
                }
            }

            if (usee && _e.IsReady())
            {
                var etarget = TargetSelector.GetTarget(250, TargetSelector.DamageType.Physical);
                if (etarget.IsValidTarget(_e.Range + 100))
                {
                    if (_menu.Item("usecomboe").GetValue<bool>() && !_q.IsReady())
                        _e.CastOnUnit(Me);                   
                }
            }
        }

        private static void Secure(bool useq, bool usee, bool user)
        {
            if (user && _r.IsReady())
            {
                var rtarget = ObjectManager.Get<Obj_AI_Hero>().FirstOrDefault(h => h.IsEnemy);
                if (rtarget.IsValidTarget(_r.Range))
                {
                    if (Me.GetSpellDamage(rtarget, SpellSlot.R) >= rtarget.Health)
                        _r.Cast();
                }
            }
			
			

            if (usee && _e.IsReady())
            {
                var etarget = ObjectManager.Get<Obj_AI_Hero>().FirstOrDefault(h => h.IsEnemy);
                if (etarget.IsValidTarget(_e.Range))
                {
                    if (Me.GetSpellDamage(etarget, SpellSlot.E) >= etarget.Health)
                        _e.CastOnUnit(Me);
                }
            }

            if (useq && _q.IsReady())
            {
                var qtarget = ObjectManager.Get<Obj_AI_Hero>().FirstOrDefault(h => h.IsEnemy);
                if (qtarget.IsValidTarget(_q.Range))
                {
                    if (Me.GetSpellDamage(qtarget, SpellSlot.Q) >= qtarget.Health)
                    {
                        var poutput = _q.GetPrediction(qtarget);
                        if (poutput.Hitchance >= HitChance.Medium)
                        {
                            if (qtarget.Distance(Me.ServerPosition, true) >
                                Math.Pow(_menu.Item("dnd2").GetValue<Slider>().Value, 2))
                            {
                                _q.Cast(poutput.CastPosition);
                            }
                        }
                    }
                }
            }
        }
    }
}
