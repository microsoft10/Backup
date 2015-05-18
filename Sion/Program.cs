using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace Sion
{
    class Program
    {
        private static Menu Config;

        public static Orbwalking.Orbwalker Orbwalker;
        public static Spell Q;
        public static Spell E;
        public static Spell W;
        public static Int32 lastSkinId = 0;
        public static Obj_AI_Hero Player = ObjectManager.Player;

        public static Vector2 QCastPos = new Vector2();
        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        static void Game_OnGameLoad(EventArgs args)
        {
            if (ObjectManager.Player.BaseSkinName != "Sion") return;

            //Spells
            Q = new Spell(SpellSlot.Q, 600);
            Q.SetSkillshot(0.6f, 100f, float.MaxValue, false, SkillshotType.SkillshotLine);
            Q.SetCharged("SionQ", "SionQ", 500, 720, 0.5f);

            W = new Spell(SpellSlot.W);

            E = new Spell(SpellSlot.E, 725);
            E.SetSkillshot(0.25f, 80f, 1500, false, SkillshotType.SkillshotLine);

            //Make the menu
            Config = new Menu("Sion The Meat Walker", "Sion The Meat Walker", true);

            //Orbwalker submenu
            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));

            //Add the target selector to the menu as submenu.
            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);

            //Load the orbwalker and add it to the menu as submenu.
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));

            //Combo menu:
            Config.AddSubMenu(new Menu("Combo", "Combo"));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseQCombo", "Use Q").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseWCombo", "Use W").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseECombo", "Use E").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("ComboActive", "Combo!").SetValue(new KeyBind(Config.Item("Orbwalk").GetValue<KeyBind>().Key, KeyBindType.Press)));

            //Harass menu:
            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseWHarass", "Use W").SetValue(false));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseEHarass", "Use E").SetValue(false));
            Config.SubMenu("Harass").AddItem(new MenuItem("HarassActive", "Harass!").SetValue(new KeyBind(Config.Item("Farm").GetValue<KeyBind>().Key, KeyBindType.Press)));

            //laneclear
            Config.AddSubMenu(new Menu("LaneClear", "LaneClear"));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseWLaneClear", "Use W").SetValue(false));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("LaneClearActive", "LaneClear!").SetValue(new KeyBind(Config.Item("LaneClear").GetValue<KeyBind>().Key, KeyBindType.Press)));


            //R
            Config.AddSubMenu(new Menu("R", "R"));
            Config.SubMenu("R").AddItem(new MenuItem("AntiCamLock", "Avoid locking camera").SetValue(true));
            Config.SubMenu("R").AddItem(new MenuItem("MoveToMouse", "Move to mouse (Exploit)").SetValue(false));//Disabled by default since its not legit Keepo
            
            //misc
            Config.AddSubMenu(new Menu("Misc Settings", "miscs"));
            Config.SubMenu("miscs").AddItem(new MenuItem("skin", "Use Custom Skin").SetValue(true));
            Config.SubMenu("miscs").AddItem(new MenuItem("skin1", "Skin Changer").SetValue(new Slider(0, 0, 4)));
            Config.SubMenu("miscs").AddItem(new MenuItem("packet", "Use Packets").SetValue(true));

            if (Config.Item("skin").GetValue<bool>())
            {
                Packet.S2C.UpdateModel.Encoded(new Packet.S2C.UpdateModel.Struct(Player.NetworkId, Config.Item("skin1").GetValue<Slider>().Value, Player.ChampionName)).Process();
                lastSkinId = Config.Item("skin1").GetValue<Slider>().Value;
            }
           

            Config.AddToMainMenu();
            // end menu

            Game.PrintChat("Sion Loaded! by iMeh Modify by nongnoobjung");
            Game.OnUpdate += Game_OnGameUpdate;
            Game.OnProcessPacket += Game_OnGameProcessPacket;
            Drawing.OnDraw += Drawing_OnDraw;
            Obj_AI_Hero.OnProcessSpellCast += ObjAiHeroOnOnProcessSpellCast;
        }

        public static bool packets()
        {
            return Config.Item("packet").GetValue<bool>();
        }


        private static void ObjAiHeroOnOnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe && args.SData.Name == "SionQ")
            {
                QCastPos = args.End.To2D();
            }
        }

        static void Drawing_OnDraw(EventArgs args)
        {
            Utility.DrawCircle(ObjectManager.Player.Position, Q.Range, System.Drawing.Color.White);
        }

        static void Game_OnGameProcessPacket(GamePacketEventArgs args)
        {
            if (args.PacketData[0] == 0xFE && Config.Item("AntiCamLock").GetValue<bool>())
            {
                var p = new GamePacket(args.PacketData);
                if (p.ReadInteger(1) == ObjectManager.Player.NetworkId && p.Size() > 9)
                {
                    args.Process = false;
                }
            }
        }

        static void harass()
        {
                var qTarget = TargetSelector.GetTarget(!Q.IsCharging ? Q.ChargedMaxRange / 2 : Q.ChargedMaxRange, TargetSelector.DamageType.Physical);

                var eTarget = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);

                if (qTarget != null && Config.Item("UseQHarass").GetValue<bool>())
                {
                    if (Q.IsCharging)
                    {
                        var start = ObjectManager.Player.ServerPosition.To2D();
                        var end = start.Extend(QCastPos, Q.Range);
                        var direction = (end - start).Normalized();
                        var normal = direction.Perpendicular();

                        var points = new List<Vector2>();
                        var hitBox = qTarget.BoundingRadius;
                        points.Add(start + normal * (Q.Width + hitBox));
                        points.Add(start - normal * (Q.Width + hitBox));
                        points.Add(end + Q.ChargedMaxRange * direction - normal * (Q.Width + hitBox));
                        points.Add(end + Q.ChargedMaxRange * direction + normal * (Q.Width + hitBox));

                        for (int i = 0; i <= points.Count - 1; i++)
                        {
                            var A = points[i];
                            var B = points[i == points.Count - 1 ? 0 : i + 1];

                            if (qTarget.ServerPosition.To2D().Distance(A, B, true, true) < 50 * 50)
                            {
                                Q.Cast(qTarget, true);
                            }
                        }
                        return;
                    }

                    if (Q.IsReady())
                    {
                        Q.StartCharging(qTarget.ServerPosition);
                    }
                }

                if (qTarget != null && Config.Item("UseWHarass").GetValue<bool>() && W.IsReady() )
                {
                    ObjectManager.Player.Spellbook.CastSpell(SpellSlot.W, ObjectManager.Player);
                }

                if (eTarget != null && Config.Item("UseEHarass").GetValue<bool>() && E.IsReady())
                {
                    E.Cast(eTarget,packets());
                }
        }

        static void combo()
        {
            var qTarget = TargetSelector.GetTarget(!Q.IsCharging ? Q.ChargedMaxRange / 2 : Q.ChargedMaxRange, TargetSelector.DamageType.Physical);

            var eTarget = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);

            if (qTarget != null && Config.Item("UseQCombo").GetValue<bool>())
            {
                if (Q.IsCharging)
                {
                    var start = ObjectManager.Player.ServerPosition.To2D();
                    var end = start.Extend(QCastPos, Q.Range);
                    var direction = (end - start).Normalized();
                    var normal = direction.Perpendicular();

                    var points = new List<Vector2>();
                    var hitBox = qTarget.BoundingRadius;
                    points.Add(start + normal * (Q.Width + hitBox));
                    points.Add(start - normal * (Q.Width + hitBox));
                    points.Add(end + Q.ChargedMaxRange * direction - normal * (Q.Width + hitBox));
                    points.Add(end + Q.ChargedMaxRange * direction + normal * (Q.Width + hitBox));

                    for (int i = 0; i <= points.Count - 1; i++)
                    {
                        var A = points[i];
                        var B = points[i == points.Count - 1 ? 0 : i + 1];

                        if (qTarget.ServerPosition.To2D().Distance(A, B, true, true) < 50 * 50)
                        {
                            Q.Cast(qTarget, true);
                        }
                    }
                    return;
                }

                if (Q.IsReady())
                {
                    Q.StartCharging(qTarget.ServerPosition);
                }
            }

            if (qTarget != null && Config.Item("UseWCombo").GetValue<bool>() && W.IsReady())
            {
                ObjectManager.Player.Spellbook.CastSpell(SpellSlot.W, ObjectManager.Player);
            }

            if (eTarget != null && Config.Item("UseECombo").GetValue<bool>() && E.IsReady())
            {
                E.Cast(eTarget,packets());
            }
        }

        static void laneclear()
        {
            //q
            var allMinions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, W.Range,
                MinionTypes.All, MinionTeam.NotAlly);



            //w
            if (Config.Item("UseWLaneClear").GetValue<bool>() && W.IsReady() && allMinions.Count > 0)
            {
                ObjectManager.Player.Spellbook.CastSpell(SpellSlot.W, ObjectManager.Player);
            }
        }

        static void Game_OnGameUpdate(EventArgs args)
        {



            //Casting R
            if (ObjectManager.Player.HasBuff("SionR"))
            {
                if (Config.Item("MoveToMouse").GetValue<bool>())
                {
                    var p = ObjectManager.Player.Position.To2D().Extend(Game.CursorPos.To2D(), 500);
                    ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, p.To3D());
                }
                return;
            }

            //harass
            if (Config.Item("HarassActive").GetValue<KeyBind>().Active)
            {
                harass();
            }

            //combo
            if (Config.Item("ComboActive").GetValue<KeyBind>().Active)
            {
                combo();
            }

            //combo
            if (Config.Item("LaneClearActive").GetValue<KeyBind>().Active)
            {
                laneclear();
            }

            //skin changer
            if (Config.Item("skin").GetValue<bool>() && Config.Item("skin1").GetValue<Slider>().Value != lastSkinId)
            {
                Packet.S2C.UpdateModel.Encoded(new Packet.S2C.UpdateModel.Struct(Player.NetworkId, Config.Item("skin1").GetValue<Slider>().Value, Player.ChampionName)).Process();
                lastSkinId = Config.Item("skin1").GetValue<Slider>().Value;
            }

        }
    }
}
