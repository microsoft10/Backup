using System;
using System.Linq;
using System.Collections.Generic;
using Color = System.Drawing.Color;

using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

using MasterSeries.Common;
using Orbwalk = MasterSeries.Common.M_Orbwalker;

namespace MasterSeries
{
    public class HtmlColor
    {
        public const string AliceBlue = "#F0F8FF";
        public const string AntiqueWhite = "#FAEBD7";
        public const string Aqua = "#00FFFF";
        public const string Aquamarine = "#7FFFD4";
        public const string Azure = "#F0FFFF";
        public const string Beige = "#F5F5DC";
        public const string Bisque = "#FFE4C4";
        public const string Black = "#000000";
        public const string BlanchedAlmond = "#FFEBCD";
        public const string Blue = "#0000FF";
        public const string BlueViolet = "#8A2BE2";
        public const string Brown = "#A52A2A";
        public const string BurlyWood = "#DEB887";
        public const string CadetBlue = "#5F9EA0";
        public const string Chartreuse = "#7FFF00";
        public const string Chocolate = "#D2691E";
        public const string Coral = "#FF7F50";
        public const string CornflowerBlue = "#6495ED";
        public const string Cornsilk = "#FFF8DC";
        public const string Crimson = "#DC143C";
        public const string Cyan = "#00FFFF";
        public const string DarkBlue = "#00008B";
        public const string DarkCyan = "#008B8B";
        public const string DarkGoldenRod = "#B8860B";
        public const string DarkGray = "#A9A9A9";
        public const string DarkGreen = "#006400";
        public const string DarkKhaki = "#BDB76B";
        public const string DarkMagenta = "#8B008B";
        public const string DarkOliveGreen = "#556B2F";
        public const string DarkOrange = "#FF8C00";
        public const string DarkOrchid = "#9932CC";
        public const string DarkRed = "#8B0000";
        public const string DarkSalmon = "#E9967A";
        public const string DarkSeaGreen = "#8FBC8F";
        public const string DarkSlateBlue = "#483D8B";
        public const string DarkSlateGray = "#2F4F4F";
        public const string DarkTurquoise = "#00CED1";
        public const string DarkViolet = "#9400D3";
        public const string DeepPink = "#FF1493";
        public const string DeepSkyBlue = "#00BFFF";
        public const string DimGray = "#696969";
        public const string DodgerBlue = "#1E90FF";
        public const string FireBrick = "#B22222";
        public const string FloralWhite = "#FFFAF0";
        public const string ForestGreen = "#228B22";
        public const string Fuchsia = "#FF00FF";
        public const string Gainsboro = "#DCDCDC";
        public const string GhostWhite = "#F8F8FF";
        public const string Gold = "#FFD700";
        public const string GoldenRod = "#DAA520";
        public const string Gray = "#808080";
        public const string Green = "#008000";
        public const string GreenYellow = "#ADFF2F";
        public const string HoneyDew = "#F0FFF0";
        public const string HotPink = "#FF69B4";
        public const string IndianRed = "#CD5C5C";
        public const string Indigo = "#4B0082";
        public const string Ivory = "#FFFFF0";
        public const string Khaki = "#F0E68C";
        public const string Lavender = "#E6E6FA";
        public const string LavenderBlush = "#FFF0F5";
        public const string LawnGreen = "#7CFC00";
        public const string LemonChiffon = "#FFFACD";
        public const string LightBlue = "#ADD8E6";
        public const string LightCoral = "#F08080";
        public const string LightCyan = "#E0FFFF";
        public const string LightGoldenRodYellow = "#FAFAD2";
        public const string LightGray = "#D3D3D3";
        public const string LightGreen = "#90EE90";
        public const string LightPink = "#FFB6C1";
        public const string LightSalmon = "#FFA07A";
        public const string LightSeaGreen = "#20B2AA";
        public const string LightSkyBlue = "#87CEFA";
        public const string LightSlateGray = "#778899";
        public const string LightSteelBlue = "#B0C4DE";
        public const string LightYellow = "#FFFFE0";
        public const string Lime = "#00FF00";
        public const string LimeGreen = "#32CD32";
        public const string Linen = "#FAF0E6";
        public const string Magenta = "#FF00FF";
        public const string Maroon = "#800000";
        public const string MediumAquaMarine = "#66CDAA";
        public const string MediumBlue = "#0000CD";
        public const string MediumOrchid = "#BA55D3";
        public const string MediumPurple = "#9370DB";
        public const string MediumSeaGreen = "#3CB371";
        public const string MediumSlateBlue = "#7B68EE";
        public const string MediumSpringGreen = "#00FA9A";
        public const string MediumTurquoise = "#48D1CC";
        public const string MediumVioletRed = "#C71585";
        public const string MidnightBlue = "#191970";
        public const string MintCream = "#F5FFFA";
        public const string MistyRose = "#FFE4E1";
        public const string Moccasin = "#FFE4B5";
        public const string NavajoWhite = "#FFDEAD";
        public const string Navy = "#000080";
        public const string OldLace = "#FDF5E6";
        public const string Olive = "#808000";
        public const string OliveDrab = "#6B8E23";
        public const string Orange = "#FFA500";
        public const string OrangeRed = "#FF4500";
        public const string Orchid = "#DA70D6";
        public const string PaleGoldenRod = "#EEE8AA";
        public const string PaleGreen = "#98FB98";
        public const string PaleTurquoise = "#AFEEEE";
        public const string PaleVioletRed = "#DB7093";
        public const string PapayaWhip = "#FFEFD5";
        public const string PeachPuff = "#FFDAB9";
        public const string Peru = "#CD853F";
        public const string Pink = "#FFC0CB";
        public const string Plum = "#DDA0DD";
        public const string PowderBlue = "#B0E0E6";
        public const string Purple = "#800080";
        public const string Red = "#FF0000";
        public const string RosyBrown = "#BC8F8F";
        public const string RoyalBlue = "#4169E1";
        public const string SaddleBrown = "#8B4513";
        public const string Salmon = "#FA8072";
        public const string SandyBrown = "#F4A460";
        public const string SeaGreen = "#2E8B57";
        public const string SeaShell = "#FFF5EE";
        public const string Sienna = "#A0522D";
        public const string Silver = "#C0C0C0";
        public const string SkyBlue = "#87CEEB";
        public const string SlateBlue = "#6A5ACD";
        public const string SlateGray = "#708090";
        public const string Snow = "#FFFAFA";
        public const string SpringGreen = "#00FF7F";
        public const string SteelBlue = "#4682B4";
        public const string Tan = "#D2B48C";
        public const string Teal = "#008080";
        public const string Thistle = "#D8BFD8";
        public const string Tomato = "#FF6347";
        public const string Turquoise = "#40E0D0";
        public const string Violet = "#EE82EE";
        public const string Wheat = "#F5DEB3";
        public const string White = "#FFFFFF";
        public const string WhiteSmoke = "#F5F5F5";
        public const string Yellow = "#FFFF00";
        public const string YellowGreen = "#9ACD32";
    }

    class Program
    {
        public static Obj_AI_Hero Player = null, targetObj = null;
        public static Spell Q, W, E, R;
        private static SpellSlot FlashSlot, SmiteSlot, IgniteSlot;
        public static Items.Item Tiamat, Hydra, Bilgewater, HexGun, BladeRuined, RanduinOmen, Youmuu, Deathfire, Blackfire, Sheen, Iceborn, Trinity;
        public static Menu Config;
        public static String Name;
        private static M_TargetSelector TS;
        private static string[] ChampMultiSkin = { "Rammus", "Udyr" };
        private static bool SurviveHit = false;
        private static float SurviveHitDmg = 0;

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += OnGameLoad;
        }

        private static void OnGameLoad(EventArgs args)
        {
            Player = ObjectManager.Player;
            Name = Player.ChampionName;
            Game.PrintChat("<font color = \'{0}'>Master Series</font>", HtmlColor.Lime);
            Config = new Menu("Master Series", "MasterSeries", true);
            Config.AddSubMenu(new Menu("Info", "Info"));
            Config.SubMenu("Info").AddItem(new MenuItem("Author", "Credits: Brian"));
            Config.SubMenu("Info").AddItem(new MenuItem("Paypal", "Donate: dcbrian01@gmail.com"));
            TS = new M_TargetSelector(Config);
            Orbwalk.AddToMenu(Config);
            try
            {
                if (Activator.CreateInstance(null, "MasterSeries.Champions." + Name) != null)
                {
                    Tiamat = ItemData.Tiamat_Melee_Only.GetItem();
                    Hydra = ItemData.Ravenous_Hydra_Melee_Only.GetItem();
                    Bilgewater = ItemData.Bilgewater_Cutlass.GetItem();
                    HexGun = ItemData.Hextech_Gunblade.GetItem();
                    BladeRuined = ItemData.Blade_of_the_Ruined_King.GetItem();
                    RanduinOmen = ItemData.Randuins_Omen.GetItem();
                    Youmuu = ItemData.Youmuus_Ghostblade.GetItem();
                    Deathfire = ItemData.Deathfire_Grasp.GetItem();
                    Blackfire = ItemData.Blackfire_Torch.GetItem();
                    Sheen = ItemData.Sheen.GetItem();
                    Iceborn = ItemData.Iceborn_Gauntlet.GetItem();
                    Trinity = ItemData.Trinity_Force.GetItem();
                    ItemBool(Config.SubMenu(Name + "Plugin").SubMenu("Misc"), "UsePacket", "Use Packet To Cast");
                    FlashSlot = Player.GetSpellSlot("summonerflash");
                    foreach (var Smite in Player.Spellbook.Spells.Where(i => i.Name.ToLower().Contains("smite") && (i.Slot == SpellSlot.Summoner1 || i.Slot == SpellSlot.Summoner2))) SmiteSlot = Smite.Slot;
                    IgniteSlot = Player.GetSpellSlot("summonerdot");
                    SkinChanger(null, null);
                    Game.PrintChat("<font color = \'{0}'>-></font> <font color = \'{1}'>Master Of {2}</font>: <font color = \'{3}'>Loaded !</font>", HtmlColor.BlueViolet, HtmlColor.Gold, Name, HtmlColor.Cyan);
                    Game.OnGameUpdate += OnGameUpdate;
                    //Game.OnGameProcessPacket += OnGameProcessPacket;
                }
            }
            catch
            {
                Game.PrintChat("<font color = \'{0}'>-></font> <font color = \'{1}'>{2}</font>: <font color = \'{3}'>Currently not supported !</font>", HtmlColor.BlueViolet, HtmlColor.Gold, Name, HtmlColor.Cyan);
            }
            Config.AddToMainMenu();
        }

        public static void SkinChanger(object sender, OnValueChangeEventArgs e)
        {
            //Utility.DelayAction.Add(35, () => Packet.S2C.UpdateModel.Encoded(new Packet.S2C.UpdateModel.Struct(Player.NetworkId, ItemSlider("Misc", "CustomSkin"), ChampMultiSkin.Contains(Name) ? Player.SkinName : Player.BaseSkinName)).Process());
        }

        private static void OnGameUpdate(EventArgs args)
        {
            Utility.DelayAction.Add(100, () => targetObj = TS.Target);
        }

        private static void OnGameProcessPacket(GamePacketEventArgs args)
        {
            if (args.Channel == PacketChannel.S2C && args.PacketData[0] == Packet.S2C.UpdateModel.Header)
            {
                if (Packet.S2C.UpdateModel.Decoded(args.PacketData).NetworkId == Player.NetworkId)
                {
                    args.Process = false;
                    SkinChanger(null, null);
                }
            }
        }

        public static void TrySurviveSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (Player.IsDead || !sender.IsEnemy || !args.Target.IsMe) return;
            double Dmg = 0;
            if (sender is Obj_AI_Hero)
            {
                var Caster = (Obj_AI_Hero)sender;
                var Slot = Caster.GetSpellSlot(args.SData.Name);
                if ((Slot == SpellSlot.Summoner1 || Slot == SpellSlot.Summoner2) && args.SData.Name == "summonerdot")
                {
                    Dmg = Caster.GetSummonerSpellDamage(Player, Damage.SummonerSpell.Ignite);
                }
                else if (Slot == SpellSlot.Item1 || Slot == SpellSlot.Item2 || Slot == SpellSlot.Item3 || Slot == SpellSlot.Item4 || Slot == SpellSlot.Item5 || Slot == SpellSlot.Item6)
                {
                    switch (args.SData.Name)
                    {
                        case "ItemSwordOfFeastAndFamine":
                            Dmg = Caster.GetItemDamage(Player, Damage.DamageItems.Botrk);
                            break;
                        case "BilgewaterCutlass":
                            Dmg = Caster.GetItemDamage(Player, Damage.DamageItems.Bilgewater);
                            break;
                        case "DeathfireGrasp":
                            Dmg = Caster.GetItemDamage(Player, Damage.DamageItems.Dfg);
                            break;
                        case "Hextech Gunblade":
                            Dmg = Caster.GetItemDamage(Player, Damage.DamageItems.Hexgun);
                            break;
                    }
                }
                else if (Slot == SpellSlot.Q || Slot == SpellSlot.W || Slot == SpellSlot.E || Slot == SpellSlot.R)
                {
                    Dmg = Caster.GetSpellDamage(Player, Slot);
                }
                else if (args.SData.IsAutoAttack()) Dmg = Caster.GetAutoAttackDamage(Player, true);
            }
            else if ((sender is Obj_AI_Minion || sender is Obj_AI_Turret) && args.SData.IsAutoAttack()) Dmg = sender.CalcDamage(Player, Damage.DamageType.Physical, sender.BaseAttackDamage + sender.FlatPhysicalDamageMod);
            if (Dmg > 0)
            {
                SurviveHitDmg = (float)Dmg;
                SurviveHit = true;
            }
        }

        public static void TrySurvivePacket(GamePacketEventArgs args)
        {
            if (Player.IsDead || args.PacketData[0] != Packet.S2C.Damage.Header) return;
            var DmgPacket = Packet.S2C.Damage.Decoded(args.PacketData);
            if (DmgPacket.TargetNetworkId == Player.NetworkId && DmgPacket.DamageAmount > 0)
            {
                Game.PrintChat(DmgPacket.Type.ToString() + " / " + DmgPacket.DamageAmount.ToString());
                SurviveHitDmg = DmgPacket.DamageAmount;
                SurviveHit = true;
            }
        }

        #region CreateMenu
        public static MenuItem ItemActive(Menu SubMenu, string Item, string Display, string Key, bool State = false)
        {
            return SubMenu.AddItem(new MenuItem(SubMenu.Name + Item, Display, true).SetValue(new KeyBind(Key.ToCharArray()[0], KeyBindType.Press, State)));
        }

        public static MenuItem ItemBool(Menu SubMenu, string Item, string Display, bool State = true)
        {
            return SubMenu.AddItem(new MenuItem(SubMenu.Name + Item, Display, true).SetValue(State));
        }

        public static MenuItem ItemSlider(Menu SubMenu, string Item, string Display, int Cur, int Min = 1, int Max = 100)
        {
            return SubMenu.AddItem(new MenuItem(SubMenu.Name + Item, Display, true).SetValue(new Slider(Cur, Min, Max)));
        }

        public static MenuItem ItemList(Menu SubMenu, string Item, string Display, string[] Text, int DefaultIndex = 0)
        {
            return SubMenu.AddItem(new MenuItem(SubMenu.Name + Item, Display, true).SetValue(new StringList(Text, DefaultIndex)));
        }

        public static bool ItemActive(string Item)
        {
            return Config.SubMenu("OW").SubMenu("Mode").Item("OW" + Item, true).GetValue<KeyBind>().Active;
        }

        public static bool ItemActive(string SubMenu, string Item)
        {
            return Config.SubMenu(Name + "Plugin").Item(SubMenu + Item, true).GetValue<KeyBind>().Active;
        }

        public static bool ItemBool(string SubMenu, string Item)
        {
            return Config.SubMenu(Name + "Plugin").Item(SubMenu + Item, true).GetValue<bool>();
        }

        public static int ItemSlider(string SubMenu, string Item)
        {
            return Config.SubMenu(Name + "Plugin").Item(SubMenu + Item, true).GetValue<Slider>().Value;
        }

        public static int ItemList(string SubMenu, string Item)
        {
            return Config.SubMenu(Name + "Plugin").Item(SubMenu + Item, true).GetValue<StringList>().SelectedIndex;
        }
        #endregion

        public static bool PacketCast()
        {
            return ItemBool("Misc", "UsePacket");
        }

        public static void CustomOrbwalk(Obj_AI_Base Target)
        {
            Orbwalk.Orbwalk(Game.CursorPos, Orbwalk.InAutoAttackRange(Target) ? Target : null);
        }

        public static bool CanKill(Obj_AI_Base Target, Spell Skill, double Health, double SubDmg)
        {
            return Skill.GetHealthPrediction(Target) - Health + 5 <= SubDmg;
        }

        public static bool CanKill(Obj_AI_Base Target, Spell Skill, double SubDmg)
        {
            return CanKill(Target, Skill, 0, SubDmg);
        }

        public static bool CanKill(Obj_AI_Base Target, Spell Skill, int Stage = 0, double SubDmg = 0)
        {
            return Skill.GetHealthPrediction(Target) + 5 <= (SubDmg > 0 ? SubDmg : Skill.GetDamage(Target, Stage));
        }

        public static bool FlashReady()
        {
            return FlashSlot != SpellSlot.Unknown && FlashSlot.IsReady();
        }

        public static bool SmiteReady()
        {
            return SmiteSlot != SpellSlot.Unknown && SmiteSlot.IsReady();
        }

        public static bool IgniteReady()
        {
            return IgniteSlot != SpellSlot.Unknown && IgniteSlot.IsReady();
        }

        public static bool CastFlash(Vector3 Pos)
        {
            if (!FlashReady()) return false;
            return Player.Spellbook.CastSpell(FlashSlot, Pos);
        }

        public static bool CastSmite(Obj_AI_Base Target, bool Killable = true)
        {
            if (!SmiteReady() || !Target.IsValidTarget(760) || (Killable && Target.Health > Player.GetSummonerSpellDamage(Target, Damage.SummonerSpell.Smite))) return false;
            return Player.Spellbook.CastSpell(SmiteSlot, Target);
        }

        public static bool CastIgnite(Obj_AI_Hero Target)
        {
            if (!IgniteReady() || !Target.IsValidTarget(600) || Target.Health + 5 > Player.GetSummonerSpellDamage(Target, Damage.SummonerSpell.Ignite)) return false;
            return Player.Spellbook.CastSpell(IgniteSlot, Target);
        }

        public static InventorySlot GetWardSlot()
        {
            InventorySlot Ward = null;
            int[] WardPink = { 3362, 2043 };
            int[] WardGreen = { 3340, 3361, 2049, 2045, 2044 };
            if (ItemBool("Misc", "WJPink")) Ward = Player.InventoryItems.FirstOrDefault(i => i.Id == (ItemId)WardPink.FirstOrDefault(a => Items.CanUseItem(a)));
            foreach (var Id in WardGreen.Where(i => Items.CanUseItem(i))) Ward = Player.InventoryItems.First(i => i.Id == (ItemId)Id);
            return Ward;
        }

        public static float GetWardRange()
        {
            int[] TricketWard = { 3340, 3361, 3362 };
            return 600 * ((Player.Masteries.Any(i => i.Page == MasteryPage.Utility && i.Id == 68 && i.Points == 1) && GetWardSlot() != null && TricketWard.Contains((int)GetWardSlot().Id)) ? 1.15f : 1);
        }

        public static void TrySurvive(int Id, int AtHpPer = 0)
        {
            TrySurvive(Player.InventoryItems.First(i => i.Id == (ItemId)Id).SpellSlot, AtHpPer);
        }

        public static void TrySurvive(SpellSlot Slot, int AtHpPer = 0)
        {
            if (!SurviveHit) return;
            var HpPerAfterHit = (Player.Health - (int)SurviveHitDmg) / Player.MaxHealth * 100;
            if ((AtHpPer == 0 && HpPerAfterHit <= 10) || (AtHpPer > 0 && HpPerAfterHit <= AtHpPer))
            {
                Player.Spellbook.CastSpell(Slot, Player);
                SurviveHit = false;
            }
        }

        public static Vector3 GetClearPos(List<Obj_AI_Base> Object, Spell Skill)
        {
            var Best = 0;
            var BestPos = default(Vector3);
            foreach (var Obj in Object)
            {
                var Hit = Skill.CountHits(Object, Obj.Position);
                if (Hit > Best || BestPos == default(Vector3))
                {
                    Best = Hit;
                    BestPos = Obj.Position;
                }
            }
            return BestPos;
        }

        public static bool CanSmiteMob(string Name)
        {
            if (!SmiteReady() || Name.Contains("Mini")) return false;
            if (ItemBool("SmiteMob", "Baron") && Name.StartsWith("SRU_Baron")) return true;
            if (ItemBool("SmiteMob", "Dragon") && Name.StartsWith("SRU_Dragon")) return true;
            if (ItemBool("SmiteMob", "Red") && Name.StartsWith("SRU_Red")) return true;
            if (ItemBool("SmiteMob", "Blue") && Name.StartsWith("SRU_Blue")) return true;
            if (ItemBool("SmiteMob", "Krug") && Name.StartsWith("SRU_Krug")) return true;
            if (ItemBool("SmiteMob", "Gromp") && Name.StartsWith("SRU_Gromp")) return true;
            if (ItemBool("SmiteMob", "Raptor") && Name.StartsWith("SRU_Razorbeak")) return true;
            if (ItemBool("SmiteMob", "Wolf") && Name.StartsWith("SRU_Murkwolf")) return true;
            return false;
        }

        public static void CastSkillShotSmite(Spell Skill, Obj_AI_Hero Target)
        {
            var Pred = Skill.GetPrediction(Target);
            if (ItemBool("Misc", "SmiteCol") && Pred.CollisionObjects.Count == 1 && Q.MinHitChance == HitChance.High && CastSmite(Pred.CollisionObjects.First()))
            {
                Q.Cast(Pred.CastPosition, PacketCast());
            }
            else Q.CastIfHitchanceEquals(Target, HitChance.VeryHigh, PacketCast());
        }
    }
}