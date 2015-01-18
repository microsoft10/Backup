using System;
using SharpDX;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using LeagueSharp.Network.Packets;
using Color = System.Drawing.Color;
using Packet = LeagueSharp.Common.Packet;

namespace KurisuRiven
{
    internal class KurisuRiven
    {
        #region  Main

        public static Menu config;
        private static Obj_AI_Hero enemy;
        private static readonly Obj_AI_Hero me = ObjectManager.Player;
        private static Orbwalking.Orbwalker orbwalker;

        private static int etime;
        private static int qtime;
        private static int wtime;
        private static int rtime;
        private static int aatime;
        public static int cleavecount;

        private static double ritems;
        private static double ua, uq, uw;
        private static double ra, rq, rw, rr, ri;
        private static float truerange;
        private static bool focus;

        private static bool ultion, useblade, useautows;
        private static bool usecombo, useclear;
        private static int gaptime, wslash, wsneed, bladewhen;

        private static readonly Spell valor = new Spell(SpellSlot.E, 390f);
        private static readonly Spell wings = new Spell(SpellSlot.Q, 250f);
        private static readonly Spell kiburst = new Spell(SpellSlot.W, 150f);
        private static readonly Spell blade = new Spell(SpellSlot.R, 900f);

        private static float ee, ff;
        private static readonly int[] items = { 3144, 3153, 3142, 3112, 3131 };
        private static readonly int[] runicpassive =
        {
            20, 20, 25, 25, 25, 30, 30, 30, 35, 35, 35, 40, 40, 40, 45, 45, 45, 50, 50
        };

        public const string Revision = "1.0.0.8";
        private static readonly string[] jungleminions =
        {     
            "SRU_Razorbeak", "SRU_Krug", "Sru_Crab",
            "SRU_Baron", "SRU_Dragon", "SRU_Blue", "SRU_Red", "SRU_Murkwolf", "SRU_Gromp"
        };

        #endregion

        public KurisuRiven()
        {
            Console.WriteLine("KurisuRiven is loading..");
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }


        #region  OnGameLoad
        private void Game_OnGameLoad(EventArgs args)
        {
            if (me.ChampionName != "Riven")
                return;

            config = new Menu("KurisuRiven", "kriven", true);

            // Target Selector
            var menuTS = new Menu("Selector", "tselect");
            TargetSelector.AddToMenu(menuTS);
            config.AddSubMenu(menuTS);

            // Orbwalker
            var menuOrb = new Menu("Orbwalker", "orbwalker");
            orbwalker = new Orbwalking.Orbwalker(menuOrb);
            config.AddSubMenu(menuOrb);

           

            // Draw settings
            var menuD = new Menu("Riven: Draw ", "dsettings");
            menuD.AddItem(new MenuItem("dsep1", "==== Drawing Settings"));
            menuD.AddItem(new MenuItem("drawrr", "Draw R range")).SetValue(false);
            menuD.AddItem(new MenuItem("drawqq", "Draw Q range")).SetValue(true);
            menuD.AddItem(new MenuItem("drawp", "Draw passive")).SetValue(true);
            menuD.AddItem(new MenuItem("drawengage", "Draw engage range")).SetValue(true);
            menuD.AddItem(new MenuItem("drawjumps", "Draw jump spots")).SetValue(false);
            menuD.AddItem(new MenuItem("drawkill", "Draw killable")).SetValue(true);
            menuD.AddItem(new MenuItem("drawtarg", "Draw target")).SetValue(true);
            config.AddSubMenu(menuD);

            var kMenu = new Menu("Riven: Keys", "rkkes");
            kMenu.AddItem(new MenuItem("combokey", "Combo Key")).SetValue(new KeyBind(32, KeyBindType.Press));
            kMenu.AddItem(new MenuItem("clearkey", "Clear Key")).SetValue(new KeyBind(86, KeyBindType.Press));
            kMenu.AddItem(new MenuItem("changemode", "Change Mode")).SetValue(new KeyBind(90, KeyBindType.Press));
            config.AddSubMenu(kMenu);

            // Combo Settings
            var menuC = new Menu("Riven: Combo ", "csettings");
            menuC.AddItem(new MenuItem("csep1", "==== Combo Settings"));
            menuC.AddItem(new MenuItem("usevalor", "Use E Logic")).SetValue(true);
            menuC.AddItem(new MenuItem("valorhealth", "Use E > AARange or Min HP %")).SetValue(new Slider(40));
            menuC.AddItem(new MenuItem("csep2", "==== Ultimate Settings"));
            menuC.AddItem(new MenuItem("useblade", "Use R Logic")).SetValue(true);
            menuC.AddItem(new MenuItem("bladewhen", "Use R When: "))
                .SetValue(new StringList(new[] { "Easy", "Normal", "Hard" }, 2));
            menuC.AddItem(new MenuItem("checkover", "Check R Overkill")).SetValue(false);

            menuC.AddItem(new MenuItem("csep3", "==== Cleave Settings"));       
            menuC.AddItem(new MenuItem("blockanim", "Block Q Animation (Visual)")).SetValue(false);
            menuC.AddItem(new MenuItem("qqdelay", "Gapclose Delay (Milliseconds): ")).SetValue(new Slider(1200, 1, 3000));
            
            config.AddSubMenu(menuC);

            // Extra Settings
            var menuO = new Menu("Riven: Extra ", "osettings");
            menuO.AddItem(new MenuItem("osep2", "==== Extra Settings"));
            menuO.AddItem(new MenuItem("useignote", "Use Ignite")).SetValue(true);
            menuO.AddItem(new MenuItem("enableAntiG", "Auto W Gapcloser")).SetValue(true);
            menuO.AddItem(new MenuItem("osep1", "==== Windslash Settings"));
            menuO.AddItem(new MenuItem("useautows", "Use Windslash")).SetValue(true);
            menuO.AddItem(new MenuItem("wslash", "Windslash: "))
                .SetValue(new StringList(new[] { "Only Kill", "Reckless" }));
            menuO.AddItem(new MenuItem("autows", "Windslash if damage dealt %")).SetValue(new Slider(65, 1));
            menuO.AddItem(new MenuItem("autows2", "Windslash if targets hit >=")).SetValue(new Slider(3, 2, 5));
            menuO.AddItem(new MenuItem("osep3", "==== Interrupt Settings"));
            menuO.AddItem(new MenuItem("interrupter", "Use Interrupter")).SetValue(true);
            menuO.AddItem(new MenuItem("interruptQ3", "Interrupt with 3rd Q")).SetValue(true);
            menuO.AddItem(new MenuItem("interruptW", "Interrupt with W")).SetValue(true);
            config.AddSubMenu(menuO);

            // Farm/Clear Settings
            var menuJ = new Menu("Riven: Farm ", "jsettings");
            menuJ.AddItem(new MenuItem("zzzz", "==== Farm Settings"));
            menuJ.AddItem(new MenuItem("farmE", "Use E (Farm)")).SetValue(true);
            menuJ.AddItem(new MenuItem("farmW", "Use W (Farm)")).SetValue(true);
            menuJ.AddItem(new MenuItem("farmQ", "Use Q (Farm)")).SetValue(true);
            menuJ.AddItem(new MenuItem("jungleE", "Use E (Jungle)")).SetValue(true);
            menuJ.AddItem(new MenuItem("jungleW", "Use W (Jungle)")).SetValue(true);
            menuJ.AddItem(new MenuItem("jungleQ", "Use Q (Jungle)")).SetValue(true);
            config.AddSubMenu(menuJ);

            var rivenD = new Menu("Riven: Debug", "therivend");
            rivenD.AddItem(new MenuItem("dsep2", "==== Debug Settings"));
            rivenD.AddItem(new MenuItem("debugdmg", "Debug combo damage")).SetValue(false);
            rivenD.AddItem(new MenuItem("debugtrue", "Debug true range")).SetValue(false);
            rivenD.AddItem(new MenuItem("exportjump", "Export Position")).SetValue(new KeyBind(73, KeyBindType.Press));
            rivenD.AddItem(new MenuItem("delay1", "Debug 1")).SetValue(new Slider(308, 0, 1000));
            rivenD.AddItem(new MenuItem("delay2", "Debug 2")).SetValue(new Slider(412, 0, 1000));
            config.AddSubMenu(rivenD);

            config.AddToMainMenu();

            wings.SetSkillshot(0.25f, 200f, 1700, false, SkillshotType.SkillshotCircle);
            blade.SetSkillshot(0.25f, 300f, 120f, false, SkillshotType.SkillshotCone);

            new KurisuLib();

            Drawing.OnDraw += Game_OnDraw;
            Game.OnGameUpdate += Game_OnGameUpdate;
            Game.OnGameProcessPacket += Game_OnGameProcessPacket;
            Interrupter.OnPossibleToInterrupt += Interrupter_OnPossibleToInterrupt;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Game.OnGameSendPacket += Game_OnGameSendPacket;

            var tcolor = config.Item("wslash").GetValue<StringList>().SelectedIndex == 0;
            var hex = tcolor ? "#7CFC00" : "#FF0080";

            Game.PrintChat("<font color='" + hex + "'>KurisuRiven</font> - Loaded");

        }

        #endregion

        #region Initialize

        private static void Game_OnGameSendPacket(GamePacketEventArgs args)
        {
            var packet = new GamePacket(args.PacketData);
            if (packet.Header == 0xDE && (usecombo || useclear))
            {
                var condition = cleavecount >= 2;

                var pkt = new PKT_NPC_CastSpellReq();
                pkt.Decode(args.PacketData);

                if (pkt.SpellSlot == (byte)SpellSlot.Q)
                {
                    var id = orbwalker.GetTarget() == null
                     ? (enemy.IsValidTarget() ? enemy.NetworkId : me.NetworkId)
                     : orbwalker.GetTarget().NetworkId;

                    var obj = ObjectManager.GetUnitByNetworkId<Obj_AI_Base>(id);

                    var movePos = new Vector3();
                    switch (obj.Type)
                    {
                        case GameObjectType.obj_Barracks:
                        case GameObjectType.obj_AI_Turret:
                        case GameObjectType.obj_AI_Minion:
                            movePos = obj.Position + Vector3.Normalize(me.Position -
                                                                              obj.ServerPosition) * (me.Distance(obj.ServerPosition) + 63);
                            break;
                        case GameObjectType.obj_AI_Hero:
                            movePos = enemy.ServerPosition + Vector3.Normalize(me.Position -
                                                                              enemy.ServerPosition) * (me.Distance(enemy.ServerPosition) + 57);
                            break;
                    }

                    if (obj.NetworkId != me.NetworkId && obj.Distance(me.ServerPosition) <= truerange + 30)
                    {
                        orbwalker.SetAttack(false);
                        orbwalker.SetMovement(false);

                        Utility.DelayAction.Add(Game.Ping + 328,
                            () =>
                                me.IssueOrder(GameObjectOrder.MoveTo, new Vector3(movePos.X, movePos.Y, movePos.Z)));


                        Utility.DelayAction.Add(condition ? Game.Ping + 450 : Game.Ping + 400, delegate
                        {
                            Orbwalking.LastAATick = 0;
                            orbwalker.SetAttack(true);
                        });

                        Utility.DelayAction.Add(condition ? 320 : 260,
                            () => orbwalker.SetMovement(true));
                    }
                    else
                    {
                        Orbwalking.LastAATick = 0;
                    }
                }
            }
        }

        #endregion

        #region  OnGameUpdate

        private static bool color;
        private void Game_OnGameUpdate(EventArgs args)
        {
            enemy = TargetSelector.GetSelectedTarget().IsValidTarget(1400) 
                ? TargetSelector.GetSelectedTarget() 
                : TargetSelector.GetTarget(900, TargetSelector.DamageType.Physical);


            if (me.IsDead)
                return;

            //if (Environment.TickCount - etime >= 300 &&
            //    Environment.TickCount - qtime >= Game.Ping + 450 &&
            //    Environment.TickCount - wtime >= 200 &&
            //    Environment.TickCount - rtime >= 200 && me.IsMoving)
            //{
            //    if (Environment.TickCount - aatime >= 1000 || !enemy.IsValidTarget(truerange + 20))
            //         Orbwalking.LastAATick = 0;
            //}

            if (config.Item("changemode").GetValue<KeyBind>().Active)
            {
                switch (wslash)
                {
                    case 1:
                        Utility.DelayAction.Add(300,
                            () => config.Item("wslash").SetValue(new StringList(new[] { "Only Kill", "Max Damage" }, 0)));
                        break;
                    case 0:
                        Utility.DelayAction.Add(300,
                            () => config.Item("wslash").SetValue(new StringList(new[] { "Only Kill", "Max Damage" }, 1)));
                        break;
                }
            }

            if (me.IsStunned)
                Orbwalking.LastAATick = 0;

            if (usecombo)
                CastCombo(enemy);

            foreach (var b in me.Buffs.Where(b => b.Name == "RivenTriCleave"))
                cleavecount = b.Count;

            if (!me.HasBuff("RivenTriCleave", true))
                Utility.DelayAction.Add(700, () => cleavecount = 0);

            Clear();
            Reqs();
            WindSlash();
        }

        #endregion

        #region  Requisites
        private static void Reqs()
        {
            color = wslash == 0;
            CheckDamage(enemy);

            truerange = me.AttackRange + me.Distance(me.BBox.Minimum) + 1;
            focus = TargetSelector.GetSelectedTarget().IsValidTarget(truerange + 20);

            ee = (ff - Game.Time > 0) ? (ff - Game.Time) : 0;
            ultion = me.HasBuff("RivenFengShuiEngine", true);

            wsneed = config.Item("autows").GetValue<Slider>().Value;
            gaptime = config.Item("qqdelay").GetValue<Slider>().Value;

            usecombo = config.Item("combokey").GetValue<KeyBind>().Active;
            useclear = config.Item("clearkey").GetValue<KeyBind>().Active;
            useautows = config.Item("useautows").GetValue<bool>();

            bladewhen = config.Item("bladewhen").GetValue<StringList>().SelectedIndex;
            wslash = config.Item("wslash").GetValue<StringList>().SelectedIndex;

            useblade = config.Item("useblade").GetValue<bool>();
        }

        #endregion

        #region  On Draw
        private static void Game_OnDraw(EventArgs args)
        {
            if (config.Item("drawtarg").GetValue<bool>() && enemy.IsValidTarget(900))
            {
                Utility.DrawCircle(enemy.Position, wings.Range, Color.Red, 5, 1);
            }

            if (config.Item("drawqq").GetValue<bool>() && !me.IsDead)
            {
                Utility.DrawCircle(me.Position, wings.Range + 1, color ? Color.LawnGreen : Color.Fuchsia, 2, 1);
                Utility.DrawCircle(me.Position, wings.Range - 3, Color.White, 2, 1);
            }

            if (config.Item("drawrr").GetValue<bool>() && !me.IsDead)
            {
                Utility.DrawCircle(me.Position, blade.Range + 1, color ? Color.LawnGreen : Color.Fuchsia, 2, 1);
                Utility.DrawCircle(me.Position, blade.Range - 3, Color.White, 2, 1);
            }
            if (config.Item("drawp").GetValue<bool>() && !me.IsDead)
            {
                var wts = Drawing.WorldToScreen(me.Position);

                if (wslash == 0)
                    Drawing.DrawText(wts[0] - 55, wts[1] + 30, Color.LawnGreen, "Mode: Only Kill");
                else
                    Drawing.DrawText(wts[0] - 55, wts[1] + 30, Color.Fuchsia, "Mode: Reckless");
                if (me.Spellbook.CanUseSpell(SpellSlot.Q) == SpellState.NotLearned)
                    Drawing.DrawText(wts[0] - 35, wts[1] + 10, Color.White, "Q: Not Learned!");
                else if (ee <= 0)
                    Drawing.DrawText(wts[0] - 35, wts[1] + 10, Color.White, "Q: Ready");
                else
                    Drawing.DrawText(wts[0] - 35, wts[1] + 10, Color.White, "Q: " + ee.ToString("0.0"));

            }
            if (config.Item("debugtrue").GetValue<bool>())
            {
                if (!me.IsDead)
                {
                    Utility.DrawCircle(me.Position, truerange + 25, Color.Yellow, 2, 1);
                }
            }

            if (config.Item("drawengage").GetValue<bool>() && !me.IsDead)
            {
                Utility.DrawCircle(me.Position, valor.Range + me.AttackRange - 35, color ? Color.LawnGreen : Color.Fuchsia, 2, 1);
                Utility.DrawCircle(me.Position, valor.Range + me.AttackRange - 32, Color.White, 2, 1);
            }

            if (config.Item("drawkill").GetValue<bool>())
            {
                if (enemy != null && !enemy.IsDead && !me.IsDead)
                {
                    var ts = enemy;
                    var wts = Drawing.WorldToScreen(enemy.Position);
                    if ((float)(ra + rq * 2 + rw + ri + ritems) > ts.Health)
                        Drawing.DrawText(wts[0] - 20, wts[1] + 40, Color.OrangeRed, "Kill!");
                    else if ((float)(ra * 2 + rq * 2 + rw + ritems) > ts.Health)
                        Drawing.DrawText(wts[0] - 40, wts[1] + 40, Color.OrangeRed, "Easy Kill!");
                    else if ((float)(ua * 3 + uq * 2 + uw + ri + rr + ritems) > ts.Health)
                        Drawing.DrawText(wts[0] - 40, wts[1] + 40, Color.OrangeRed, "Full Combo Kill!");
                    else if ((float)(ua * 3 + uq * 3 + uw + rr + ri + ritems) > ts.Health)
                        Drawing.DrawText(wts[0] - 40, wts[1] + 40, Color.OrangeRed, "Full Combo Hard Kill!");
                    else if ((float)(ua * 3 + uq * 3 + uw + rr + ri + ritems) < ts.Health)
                        Drawing.DrawText(wts[0] - 40, wts[1] + 40, Color.OrangeRed, "Cant Kill!");

                }
            }

            if (config.Item("debugdmg").GetValue<bool>())
            {
                if (enemy != null && !enemy.IsDead && !me.IsDead)
                {
                    var wts = Drawing.WorldToScreen(enemy.Position);
                    if (!blade.IsReady())
                        Drawing.DrawText(wts[0] - 75, wts[1] + 60, Color.Orange,
                            "Combo Damage: " + (float)(ra * 3 + rq * 3 + rw + rr + ri + ritems));
                    else
                        Drawing.DrawText(wts[0] - 75, wts[1] + 60, Color.Orange,
                            "Combo Damage: " + (float)(ua * 3 + uq * 3 + uw + rr + ri + ritems));
                }
            }

            if (config.Item("drawjumps").GetValue<bool>())
            {
                var jumplist = KurisuLib.jumpList;
                if (jumplist.Any())
                {
                    foreach (var j in jumplist)
                    {
                        if (me.Distance(j.pointA) <= 800 || me.Distance(j.pointB) <= 800)
                        {
                            Utility.DrawCircle(j.pointA, 100, color ? Color.LawnGreen : Color.Fuchsia, 2, 1);
                            Utility.DrawCircle(j.pointB, 100, color ? Color.LawnGreen : Color.Fuchsia, 2, 1);
                        }
                    }
                }
            }
        }

        #endregion

        #region Clear
        private static void Clear()
        {
            if (!useclear)
                return;

            var target = orbwalker.GetTarget();
            if (target.IsValidTarget(kiburst.Range) && jungleminions.Any(name => target.Name.StartsWith(name)))
            {
                if (kiburst.IsReady() && config.Item("jungleW").GetValue<bool>())
                    kiburst.Cast();
            }

            else if (target.IsValidTarget(wings.Range) && target.Name.StartsWith("Minion"))
            {
                if (!valor.IsReady() || !config.Item("farmE").GetValue<bool>()) return;
                if (wings.IsReady() && cleavecount >= 1)
                    valor.Cast(Game.CursorPos);
            }

            if (kiburst.IsReady() && config.Item("farmW").GetValue<bool>())
            {
                var minions = ObjectManager.Get<Obj_AI_Minion>().Where(m => m.IsValidTarget(kiburst.Range)).ToList();

                if (minions.Count() > 2)
                {
                    if (Items.HasItem(3077) && Items.CanUseItem(3077))
                        Items.UseItem(3077);
                    if (Items.HasItem(3074) && Items.CanUseItem(3074))
                        Items.UseItem(3074);
                    kiburst.Cast();
                }
            }

        }

        #endregion

        #region  AntiGapcloser
        private void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (!config.Item("enableAntiG").GetValue<bool>())
                return;

            if (gapcloser.Sender.Type == me.Type && gapcloser.Sender.IsValid)
                if (gapcloser.Sender.Distance(me.Position) < kiburst.Range && kiburst.IsReady())
                    kiburst.Cast();
        }
        #endregion

        #region  Interrupter
        private void Interrupter_OnPossibleToInterrupt(Obj_AI_Base sender, InterruptableSpell spell)
        {
            if (!config.Item("interuppter").GetValue<bool>())
                return;

            if (sender.IsValidTarget(wings.Range))
            {
                if (wings.IsReady() && cleavecount == 2 && config.Item("interruptQ3").GetValue<bool>())
                    wings.Cast(sender.Position);
            }

            if (sender.IsValidTarget(kiburst.Range))
            {
                if (kiburst.IsReady() && config.Item("interruptW").GetValue<bool>())
                    kiburst.Cast();
            }
        }
        #endregion

        #region  OnProcessSpellCast

        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            var wrange = ultion ? 150 + 135 : 150;
            if (!sender.IsMe)
                return;

            if (args.SData.Name.Contains("BasicAttack"))
                aatime = Environment.TickCount;

            switch (args.SData.Name)    
            {
                case "RivenTriCleave":
                    qtime = Environment.TickCount;
                    if (cleavecount < 1)
                        ff = Game.Time + (13 + (13 * me.PercentCooldownMod));
                    if (wslash == 1 && cleavecount == 1 && enemy.Health < rr + ra)
                        blade.Cast(enemy.ServerPosition);

                    break;
                case "RivenMartyr":
                    wtime = Environment.TickCount;
                    Orbwalking.LastAATick = 0;            
                    if (wings.IsReady() && usecombo)
                        Utility.DelayAction.Add(Game.Ping + 50, () => wings.Cast(enemy.ServerPosition));

                    break;
                case "ItemTiamatCleave":
                    Orbwalking.LastAATick = 0;

                    if (enemy.IsValidTarget(wrange) && usecombo)
                        if (wings.IsReady() && !kiburst.IsReady())
                            wings.Cast(enemy.ServerPosition);

                    if (wslash == 1 && blade.IsReady())
                        if (ultion && cleavecount >= 2)
                            blade.Cast(enemy.ServerPosition);

                    break;
                case "RivenFeint":
                    etime = Environment.TickCount;
                    Orbwalking.LastAATick = 0;

                    if (!useblade || !useautows)
                        return;

                    if (usecombo)
                        castitems(enemy);

                    if (ultion && wslash == 1 && cleavecount > 1)
                       blade.Cast(enemy.ServerPosition);
                    break;
                case "RivenFengShuiEngine":
                    rtime = Environment.TickCount;
                    Orbwalking.LastAATick = 0;

                    if (!usecombo)
                        return;

                    castitems(enemy);
                    if (enemy.IsValidTarget(285))
                        if (kiburst.IsReady())
                            kiburst.Cast();
                    break;
                case "rivenizunablade":
                    if (wings.IsReady())
                        wings.Cast(enemy.ServerPosition);
                    break;
            }
        }

        #endregion

        #region  OnProcessPacket
        private static void Game_OnGameProcessPacket(GamePacketEventArgs args)
        {
            var packet = new GamePacket(args.PacketData);
            if (packet.Header == 0x9a)
            {
                packet.Position = 2;
                var sourceId = packet.ReadInteger();
                if (sourceId != me.NetworkId)
                    return;

                if (config.Item("blockanim").GetValue<bool>())
                    args.Process = false;
            }
         
            if (packet.Header == 0x23 && wings.IsReady())
            {
                var trees = Packet.S2C.Damage.Decoded(args.PacketData);

                var targetId = trees.TargetNetworkId;
                var damageType = trees.Type;

                var targ =
                    ObjectManager.
                        GetUnitByNetworkId<Obj_AI_Base>
                            (targetId);

                if (targ.NetworkId == me.NetworkId)
                    return;

                if (damageType.ToString() != "54")
                    return;

                if (useclear && targ.Type == me.Type || usecombo && targ.Type != me.Type)
                    return;

                if ((usecombo || useclear) && !valor.IsReady())
                {
                    if (Items.HasItem(3077) && Items.CanUseItem(3077))
                        Items.UseItem(3077);
                    if (Items.HasItem(3074) && Items.CanUseItem(3074))
                        Items.UseItem(3074);
                }

                switch (targ.Type)
                {
                    case GameObjectType.obj_Barracks:
                    case GameObjectType.obj_AI_Turret:
                        if (!config.Item("clearkey").GetValue<KeyBind>().Active)
                            return;
                        castitems(targ);
                        wings.Cast(targ.ServerPosition, true);
                        break;
                    case GameObjectType.obj_AI_Hero:
                        if (!config.Item("combokey").GetValue<KeyBind>().Active)
                            return;         
                            castitems(targ);
                            wings.Cast(focus ? enemy.ServerPosition : targ.ServerPosition, true);
                        break;
                    case GameObjectType.obj_AI_Minion:
                        if (!config.Item("clearkey").GetValue<KeyBind>().Active)
                            return;
                        if (jungleminions.Any(name => targ.Name.StartsWith(name)) && !targ.Name.Contains("Mini") &&
                            wings.IsReady() && config.Item("jungleQ").GetValue<bool>())
                            wings.Cast(targ.ServerPosition, true);
                        if (targ.Name.StartsWith("Minion") && config.Item("farmQ").GetValue<bool>())
                            wings.Cast(targ.ServerPosition, true);
                        if (valor.IsReady() && cleavecount >= 1 && config.Item("jungleE").GetValue<bool>())
                            valor.Cast(targ.ServerPosition, true);
                        break;
                }
            }
        }

        #endregion

        #region  Combo Logic

        private static void CastCombo(Obj_AI_Base target)
        {
            if (!target.IsValidTarget(950))
                return;

            var wrange = ultion ? 310 : 250;
            var aHealthPercent = (int)((me.Health/me.MaxHealth)*100);

            if (me.Distance(target.ServerPosition) > truerange + 25 || 
                aHealthPercent <= config.Item("valorhealth").GetValue<Slider>().Value)
            {
                castitems(target);
                if (valor.IsReady() && config.Item("usevalor").GetValue<bool>())
                    valor.Cast(target.ServerPosition, true);
                if (wings.IsReady() && cleavecount <= 1)
                    CheckR(target);
            }

            if (wings.IsReady() && (valor.IsReady() || kiburst.IsReady())
                && me.Distance(target.Position) <= wrange)
            {
                if (cleavecount <= 1)
                    CheckR(target);
            }

            if (blade.IsReady() && valor.IsReady() && ultion)
            {
                if (cleavecount == 2 && wslash == 1)
                    valor.Cast(target.ServerPosition, true);
            }

            var hasitem = (Items.HasItem(3074) && Items.CanUseItem(3074) ||
                           Items.HasItem(3077) && Items.CanUseItem(3077));

            if (kiburst.IsReady())
            {
                var irange = hasitem ? wrange + 100 : wrange;
                if (me.Distance(target.ServerPosition) <= irange)
                {
                    if (Items.HasItem(3077) && Items.CanUseItem(3077))
                        Items.UseItem(3077);
                    if (Items.HasItem(3074) && Items.CanUseItem(3074))
                        Items.UseItem(3074);

                    if (hasitem && !ultion)
                    {
                        Utility.DelayAction.Add(Game.Ping + 120, () => kiburst.Cast());
                    }
                    else if (hasitem && ultion)
                    {
                        Utility.DelayAction.Add(Game.Ping + 165, () => kiburst.Cast());
                    }
                    else
                    {
                        kiburst.Cast();
                    }
                }
            }

            var eready = me.Spellbook.CanUseSpell(valor.Slot) == SpellState.Ready;

            if ((!eready || !config.Item("usevalor").GetValue<bool>()) &&
                wings.IsReady()  && me.Distance(target.ServerPosition) > truerange + 20)
            {
                if (Environment.TickCount - qtime >= gaptime && Environment.TickCount - etime >= 300)
                {
                    wings.Cast(target.ServerPosition, true);
                }
            }

        }

        #endregion

        #region  Windlsash
        private static void WindSlash()
        {
            if (!ultion)
                return;

            foreach (var e in ObjectManager.Get<Obj_AI_Hero>().Where(e => e.IsValidTarget(blade.Range)))
            {
                var hitcount = config.Item("autows2").GetValue<Slider>().Value;

                var prediction = blade.GetPrediction(e, true);
                if (blade.IsReady() && useautows)
                {
                    if (wslash == 1 && prediction.AoeTargetsHitCount >= hitcount)
                        blade.Cast(prediction.CastPosition);

                    if (wslash == 1 && prediction.Hitchance >= HitChance.Medium)
                    {
                        if (rr / e.MaxHealth * 100 >= e.Health / e.MaxHealth * wsneed)
                            blade.Cast(prediction.CastPosition);

                        else if (e.Health < rr + ua * 1 + uq * 1 && enemy.Distance(me.Position) < wings.Range + 50)
                        {
                            blade.Cast(prediction.CastPosition);
                        }
                        else if (enemy.Distance(me.Position) <= blade.Range && e.Health <= rr)
                        {
                            blade.Cast(prediction.CastPosition);
                        }
                    }
                    else if (wslash == 0)
                    {
                        if (prediction.Hitchance >= HitChance.Medium && e.Health <= rr-100)
                            blade.Cast(prediction.CastPosition);
                    }
                }
            }
        }

        #endregion

        #region  Item Handler
        private static void castitems(Obj_AI_Base target)
        {
            foreach (var i in items.Where(i => Items.CanUseItem(i) && Items.HasItem(i)))
            {
                if (target.IsValidTarget(valor.Range + blade.Range))
                    Items.UseItem(i);
            }
        }
        #endregion

        #region  Damage Handler
        private static void CheckDamage(Obj_AI_Base target)
        {
            if (target == null) return;

            var ignite = me.GetSpellSlot("summonerdot");
            var aaa = me.GetAutoAttackDamage(target);

            var tmt = Items.HasItem(3077) && Items.CanUseItem(3077) ? me.GetItemDamage(target, Damage.DamageItems.Tiamat) : 0;
            var hyd = Items.HasItem(3074) && Items.CanUseItem(3074) ? me.GetItemDamage(target, Damage.DamageItems.Hydra) : 0;
            var bwc = Items.HasItem(3144) && Items.CanUseItem(3144) ? me.GetItemDamage(target, Damage.DamageItems.Bilgewater) : 0;
            var brk = Items.HasItem(3153) && Items.CanUseItem(3153) ? me.GetItemDamage(target, Damage.DamageItems.Botrk) : 0;

            rr = me.GetSpellDamage(target, SpellSlot.R);
            ra = aaa + (aaa * (runicpassive[me.Level] / 100));
            rq = wings.IsReady() ? DamageQ(target) : 0;
            rw = kiburst.IsReady() ? me.GetSpellDamage(target, SpellSlot.W) : 0;
            ri = me.Spellbook.CanUseSpell(ignite) == SpellState.Ready ? me.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite) : 0;

            ritems = tmt + hyd + bwc + brk;

            ua = blade.IsReady()
                ? ra +
                  me.CalcDamage(target, Damage.DamageType.Physical,
                      me.BaseAttackDamage + me.FlatPhysicalDamageMod * 0.2)
                : ua;

            uq = blade.IsReady()
                ? rq +
                  me.CalcDamage(target, Damage.DamageType.Physical,
                      me.BaseAttackDamage + me.FlatPhysicalDamageMod * 0.2 * 0.7)
                : uq;

            uw = blade.IsReady()
                ? rw +
                  me.CalcDamage(target, Damage.DamageType.Physical,
                      me.BaseAttackDamage + me.FlatPhysicalDamageMod * 0.2 * 1)
                : uw;

            rr = blade.IsReady()
                ? rr +
                  me.CalcDamage(target, Damage.DamageType.Physical,
                      me.BaseAttackDamage + me.FlatPhysicalDamageMod * 0.2)
                : rr;
        }

        public static float DamageQ(Obj_AI_Base target)
        {
            double dmg = 0;
            if (wings.IsReady())
            {
                dmg += me.CalcDamage(target, Damage.DamageType.Physical,
                    -10 + (wings.Level * 20) +
                    (0.35 + (wings.Level * 0.05)) * (me.FlatPhysicalDamageMod + me.BaseAttackDamage));
            }

            return (float)dmg;
        }

        #endregion

        #region  Ultimate Handler
        private static void CheckR(Obj_AI_Base target)
        {
            if (useblade && usecombo)
            {
                switch (bladewhen)
                {
                    case 2:
                        if ((float)(ua * 3 + uq * 3 + uw + rr + ri + ritems) >= target.Health && !ultion)
                        {
                            if (target.Health <= (float)(ra * 2 + rq * 2 + ri + ritems))
                            {
                                if (config.Item("checkover").GetValue<bool>())
                                    return;
                            }

                            blade.Cast();

                            if (config.Item("useignote").GetValue<bool>())
                            {
                                if (cleavecount <= 1 && ultion)
                                    CastIgnite(target);

                            }

                        }
                        break;
                    case 1:
                        if ((float)(ra * 3 + rq * 3 + rw + rr + ri + ritems) >= target.Health && !ultion)
                        {

                            if (target.Health <= (float)(ra * 2 + rq * 2 + ri + ritems))
                            {
                                if (config.Item("checkover").GetValue<bool>())
                                    return;
                            }

                            blade.Cast();

                            if (config.Item("useignote").GetValue<bool>())
                            {
                                if (cleavecount <= 1 && ultion)
                                    CastIgnite(target);
                            }

                        }
                        break;
                    case 0:
                        if ((float)(ra * 2 + rq * 2 + rw + rr + ri + ritems) >= target.Health && !ultion)
                        {
                            blade.Cast();
                        }
                        break;
                }
            }
        }

        #endregion

        #region  Ignote Handler
        private static void CastIgnite(Obj_AI_Base target)
        {
            if (target.IsValidTarget(600))
            {
                var ignote = me.GetSpellSlot("summonerdot");
                if (me.Spellbook.CanUseSpell(ignote) == SpellState.Ready)
                {
                    me.Spellbook.CastSpell(ignote, target);
                }
            }
        }
        #endregion
    }
}
