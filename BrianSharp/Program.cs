using System;
using System.Linq;
using BrianSharp.Common;
using LeagueSharp;
using LeagueSharp.Common;
using System.Globalization;
using System.Linq;
using System.Reflection.Emit;
using LeagueSharp.Common.Data;
using Orbwalk = BrianSharp.Common.Orbwalker;

namespace BrianSharp
{
    internal class Program
    {
        public static Obj_AI_Hero Player;
        public static Spell Q, Q2, W, W2, E, E2, R;
        public static SpellSlot Flash, Smite, Ignite;
        public static Items.Item Tiamat, Hydra, Youmuu, Zhonya, Seraph, Sheen, Iceborn, Trinity;
        public static Menu MainMenu;
        public static String PlayerName;

        private static void Main(string[] args)
        {
            if (args == null)
            {
                return;
            }
            if (Game.Mode == GameMode.Running)
            {
                OnGameStart(new EventArgs());
            }
            Game.OnStart += OnGameStart;
        }

		public class DynamicActivator
    {
        public delegate object DynamicCreationDelegate(object[] arguments);

        private static DynamicMethod CreateDynamicMethod(Type returnType, Type[] types)
        {
            var constructor = returnType.GetConstructor(types);

            if (constructor == null)
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture,
                    "Could not find constructor matching signature {0}({1})", returnType.FullName,
                    string.Join(",", from argument in types select argument.FullName)));


            var constructorParams = constructor.GetParameters();
            var method = new DynamicMethod(string.Empty, returnType, new[] { typeof(object[]) }, constructor.DeclaringType);

            var il = method.GetILGenerator();
            il.Emit(OpCodes.Nop);
            for (var i = 0; i < constructorParams.Length; i++)
            {
                var paramType = constructorParams[i].ParameterType;
                il.Emit(OpCodes.Ldarg_0);
                switch (i)
                {
                    case 0:
                        il.Emit(OpCodes.Ldc_I4_0);
                        break;
                    case 1:
                        il.Emit(OpCodes.Ldc_I4_1);
                        break;
                    case 2:
                        il.Emit(OpCodes.Ldc_I4_2);
                        break;
                    case 3:
                        il.Emit(OpCodes.Ldc_I4_3);
                        break;
                    case 4:
                        il.Emit(OpCodes.Ldc_I4_4);
                        break;
                    case 5:
                        il.Emit(OpCodes.Ldc_I4_5);
                        break;
                    case 6:
                        il.Emit(OpCodes.Ldc_I4_6);
                        break;
                    case 7:
                        il.Emit(OpCodes.Ldc_I4_7);
                        break;
                    case 8:
                        il.Emit(OpCodes.Ldc_I4_8);
                        break;
                    default:
                        il.Emit(OpCodes.Ldc_I4_S, i);
                        break;
                }
                il.Emit(OpCodes.Ldelem_Ref);
                il.Emit(paramType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, paramType);
            }
            il.Emit(OpCodes.Newobj, constructor);
            il.Emit(OpCodes.Ret);
            return method;
        }

        public static object New(Type returnType, object[] args)
        {
            var types = args.Select(a => a.GetType()).ToArray();
            var creator = CreateDynamicMethod(returnType, types);
            return ((DynamicCreationDelegate)creator.CreateDelegate(typeof(DynamicCreationDelegate)))(args);
        }

        public static T New<T>(object[] args)
        {
            var returnType = typeof(T);
            var types = args.Select(a => a.GetType()).ToArray();
            var creator = CreateDynamicMethod(returnType, types);
            return (T)((DynamicCreationDelegate)creator.CreateDelegate(typeof(DynamicCreationDelegate)))(args);
        }
    }
	
        private static void OnGameStart(EventArgs args)
        {
            Player = ObjectManager.Player;
            PlayerName = Player.ChampionName;
            var plugin = Type.GetType("BrianSharp.Plugin." + PlayerName);
            if (plugin == null)
            {
                Helper.AddNotif(string.Format("[Brian Sharp] - {0}: Not support !", PlayerName), 5000);
                return;
            }
            MainMenu = new Menu("Brian Sharp", "BrianSharp", true);
            var infoMenu = new Menu("Info", "Info");
            {
                infoMenu.AddItem(new MenuItem("Author", "Author: Brian"));
                infoMenu.AddItem(new MenuItem("Paypal", "Paypal: dcbrian01@gmail.com"));
                MainMenu.AddSubMenu(infoMenu);
            }
            TargetSelector.AddToMenu(MainMenu.AddSubMenu(new Menu("Target Selector", "TS")));
            Orbwalk.AddToMainMenu(MainMenu);
            DynamicActivator.New(plugin, new object[0]);
            Helper.AddItem(MainMenu.SubMenu(PlayerName + "_Plugin").SubMenu("Misc"), "UsePacket", "Use Packet To Cast");
            Tiamat = ItemData.Tiamat_Melee_Only.GetItem();
            Hydra = ItemData.Ravenous_Hydra_Melee_Only.GetItem();
            Youmuu = ItemData.Youmuus_Ghostblade.GetItem();
            Zhonya = ItemData.Zhonyas_Hourglass.GetItem();
            Seraph = ItemData.Seraphs_Embrace.GetItem();
            Sheen = ItemData.Sheen.GetItem();
            Iceborn = ItemData.Iceborn_Gauntlet.GetItem();
            Trinity = ItemData.Trinity_Force.GetItem();
            Flash = Player.GetSpellSlot("summonerflash");
            foreach (var spell in
                Player.Spellbook.Spells.Where(
                    i =>
                        i.Name.ToLower().Contains("smite") &&
                        (i.Slot == SpellSlot.Summoner1 || i.Slot == SpellSlot.Summoner2)))
            {
                Smite = spell.Slot;
            }
            Ignite = Player.GetSpellSlot("summonerdot");
            MainMenu.AddToMainMenu();
            Helper.AddNotif(string.Format("[Brian Sharp] - {0}: Loaded !", PlayerName), 5000);
        }
    }
}