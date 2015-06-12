using System;
using System.Collections.Generic;
using System.Linq;
using SharpDX;
using LeagueSharp;
using LeagueSharp.Common;
//using LSConsole = LeagueSharp.Console.Console;


namespace RLProjectJunglePlay
{
public class Program
{
    public static List<Obj_AI_Hero> GetEnemyList()//회피용 영웅탐지 추가
    {
        return ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsEnemy && x.IsValid && !x.IsDead && !x.IsInvulnerable).ToList();
    }
    public static List<Obj_AI_Turret> GetEnemyTurretList()//터렛 탐지 추가(just for test)
    {
        return ObjectManager.Get<Obj_AI_Turret>().Where(x => x.IsEnemy && x.IsValid && !x.IsDead && !x.IsInvulnerable).ToList();
    }
    public static List<Obj_AI_Turret> GetAllyTurretList()//터렛 탐지 추가(just for test)
    {
        return ObjectManager.Get<Obj_AI_Turret>().Where(x => !x.IsEnemy && x.IsValid && !x.IsDead && !x.IsInvulnerable).ToList();
    }
    public static List<Obj_AI_Minion> GetTMinionList()//근처아군미니언
    {
        return ObjectManager.Get<Obj_AI_Minion>().Where(x => x.IsAlly && x.IsValid && !x.IsDead && !x.IsInvulnerable).ToList();
    }
    public static List<Obj_AI_Minion> GetEMinionList()//내근처적미니온 수
    {
        return ObjectManager.Get<Obj_AI_Minion>().Where(x => !x.IsAlly && x.IsValid && !x.IsDead && !x.IsInvulnerable).ToList();
    }
    public static List<Obj_AI_Hero> GetAllyList()//회피용 영웅탐지 추가
    {
        return ObjectManager.Get<Obj_AI_Hero>().Where(x => !x.IsEnemy && x.IsValid && !x.IsDead && !x.IsInvulnerable).ToList();
    }
    public static float getHealthPercent(Obj_AI_Base unit)
    {
        return unit.Health / unit.MaxHealth * 100;
    }
    public static float getManaPercent(Obj_AI_Base unit)
    {
        return unit.Mana / unit.MaxMana * 100;
    }
    public static Obj_AI_Hero Player = ObjectManager.Player;
    private static Obj_AI_Hero Target = null; // 타겟 추
    public static Spell Q, W, E, R;
    private static Vector3 spawn;
    private static Vector3 enemy_spawn;
    public static Menu RLProjectAutoJungleMenu;
    public static Orbwalking.Orbwalker Orbwalker; //테스트추가 오브워
    public static Orbwalking.OrbwalkingMode ActiveMode { get; set; }
    public static float gamestart = 0, pastTime = 0, kiTime = 0, pastTimeAFK, afktime = 0;
    public static List<MonsterINFO> MonsterList = new List<MonsterINFO>();
    public static int now = 1, max = 20, num = 0;
    public static float NonRCHP = 73;
    public static float recallhp = 0;
    public static bool recall = false, IsOVER = false, IsAttackedByTurret = false, IsAttackStart = false,
    IsCastW = false;
    public static bool canBuyItems = true, IsBlueTeam, IsStart = true, IsFind = false;
    public static bool _cougarForm;
    public static SpellSlot smiteSlot = SpellSlot.Unknown;
    public static Spell smite;
    public static SpellDataInst Qdata = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.Q);
    public static SpellDataInst Wdata = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W);
    public static SpellDataInst Edata = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.E);
    public static SpellDataInst Rdata = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R);
    public static List<Spell> cast2mob = new List<Spell>();
    public static List<Spell> cast2hero = new List<Spell>();
    public static List<Spell> cast4laneclear = new List<Spell>();    
    //니달리를 위해서
    private static readonly Spell Javelin = new Spell(SpellSlot.Q, 1500f);
    private static readonly Spell Bushwack = new Spell(SpellSlot.W, 900f);
    private static readonly Spell Primalsurge = new Spell(SpellSlot.E, 650f);
    private static readonly Spell Takedown = new Spell(SpellSlot.Q, 200f);
    private static readonly Spell Pounce = new Spell(SpellSlot.W, 375f);
    private static readonly Spell Swipe = new Spell(SpellSlot.E, 275f);
    private static readonly Spell Aspectofcougar = new Spell(SpellSlot.R);
    private static readonly List<Spell> HumanSpellList = new List<Spell>();
    private static readonly List<Spell> CougarSpellList = new List<Spell>();
    public static int TRRange = 830;
    public class MonsterINFO
    {
        public Vector3 Position;
        public string ID;
        public string name;
        public int order;
        public int respawntime;
        public int Range = 300;
        public MonsterINFO()
        {
            MonsterList.Add(this);
        }
    }
    public class ItemToShop
    {
        public int Price, index;
        public ItemId item;
        public ItemId needItem;
        public ItemToShop()
        {
            num += 1;
        }
    }
    #region 몬스터
    public static MonsterINFO Baron = new MonsterINFO
    {
        ID = "Baron",
        Position = new Vector3(4910f, 10268f, -71.24f),
        name = "SRU_BaronSpawn",
        respawntime = 420
    };
    public static MonsterINFO Dragon = new MonsterINFO
    {
        ID = "Dragon",
        Position = new Vector3(9836f, 4408f, -71.24f),
        name = "SRU_Dragon",
        respawntime = 360
    };
    public static MonsterINFO top_crab = new MonsterINFO
    {
        ID = "top_crab",
        Position = new Vector3(4266f, 9634f, -67.87f),
        name = "noneuses",
        respawntime = 180,
        Range = 3000
    };
    public static MonsterINFO BLUE_MID = new MonsterINFO
    {
        ID = "blue_MID",
        Position = new Vector3(5294.531f, 5537.924f, 50.46155f),
        name = "noneuses",
        respawntime = 180,
        Range = 3000
    };
    public static MonsterINFO PURPLE_MID = new MonsterINFO
    {
        ID = "purple_MID",
        Position = new Vector3(9443.35f, 9339.06f, 53.30994f),
        name = "noneuses",
        respawntime = 180,
        Range = 3000
    };
    public static MonsterINFO down_crab = new MonsterINFO
    {
        ID = "down_crab",
        Position = new Vector3(10524f, 5116f, -62.81f),
        name = "noneuses",
        respawntime = 180,
        Range = 3000
    };
    public static MonsterINFO bteam_Razorbeak = new MonsterINFO { ID = "bteam_Razorbeak", Position = new Vector3(6974f, 5460f, 54f), name = "SRU_Razorbeak" };
    public static MonsterINFO bteam_Red = new MonsterINFO
    {
        ID = "bteam_Red",
        Position = new Vector3(7796f, 4028f, 54f),
        name = "SRU_Red",
        respawntime = 300
    };
    public static MonsterINFO bteam_Krug = new MonsterINFO { ID = "bteam_Krug", Position = new Vector3(8394f, 2750f, 50f), name = "SRU_Krug" };
    public static MonsterINFO bteam_Blue = new MonsterINFO
    {
        ID = "bteam_Blue",
        Position = new Vector3(3832f, 7996f, 52f),
        name = "SRU_Blue",
        respawntime = 300
    };
    public static MonsterINFO bteam_Gromp = new MonsterINFO { ID = "bteam_Gromp", Position = new Vector3(2112f, 8372f, 51.7f), name = "SRU_Gromp" };
    public static MonsterINFO bteam_Wolf = new MonsterINFO { ID = "bteam_Wolf", Position = new Vector3(3844f, 6474f, 52.46f), name = "SRU_Murkwolf" };
    public static MonsterINFO pteam_Razorbeak = new MonsterINFO { ID = "pteam_Razorbeak", Position = new Vector3(7856f, 9492f, 52.33f), name = "SRU_Razorbeak" };
    public static MonsterINFO pteam_Red = new MonsterINFO
    {
        ID = "pteam_Red",
        Position = new Vector3(7124f, 10856f, 56.34f),
        name = "SRU_Red",
        respawntime = 300
    };
    public static MonsterINFO pteam_Krug = new MonsterINFO { ID = "pteam_Krug", Position = new Vector3(6495f, 12227f, 56.47f), name = "SRU_Krug" };
    public static MonsterINFO pteam_Blue = new MonsterINFO
    {
        ID = "pteam_Blue",
        Position = new Vector3(10850f, 6938f, 51.72f),
        name = "SRU_Blue",
        respawntime = 300
    };
    public static MonsterINFO pteam_Gromp = new MonsterINFO { ID = "pteam_Gromp", Position = new Vector3(12766f, 6464f, 51.66f), name = "SRU_Gromp" };
    public static MonsterINFO pteam_Wolf = new MonsterINFO { ID = "pteam_Wolf", Position = new Vector3(10958f, 8286f, 62.46f), name = "SRU_Murkwolf" };
    #endregion
    #region 아이템
    #region ap
    public static List<ItemToShop> buyThings_AP = new List<ItemToShop>
{
new ItemToShop()
{
Price = 450,
needItem = ItemId.Hunters_Machete,
item = ItemId.Rangers_Trailblazer,
index = 1
},
new ItemToShop()
{
Price = 820,
needItem = ItemId.Rangers_Trailblazer,
item = ItemId.Fiendish_Codex,
index = 2
},
new ItemToShop()
{
Price = 580,
needItem = ItemId.Fiendish_Codex,
item = ItemId.Rangers_Trailblazer_Enchantment_Magus,
index = 3
},
new ItemToShop()
{
Price = 1100,
needItem = ItemId.Rangers_Trailblazer_Enchantment_Magus,
item = ItemId.Sorcerers_Shoes,
index = 4
},
new ItemToShop()
{
Price = 820,
needItem = ItemId.Sorcerers_Shoes,
item = ItemId.Fiendish_Codex,
index = 5
},
new ItemToShop()
{
Price = 600,
needItem = ItemId.Fiendish_Codex,
item = ItemId.Forbidden_Idol,
index = 6
},
new ItemToShop()
{
Price = 880,
needItem = ItemId.Forbidden_Idol,
item = ItemId.Morellonomicon,
index = 7
},
new ItemToShop()
{
Price = 1200,
needItem = ItemId.Morellonomicon,
item = ItemId.Seekers_Armguard,
index = 8
},
new ItemToShop()
{
Price = 1600,
needItem = ItemId.Seekers_Armguard,
item = ItemId.Needlessly_Large_Rod,
index = 9
},
new ItemToShop()
{
Price = 500,
needItem = ItemId.Needlessly_Large_Rod,
item = ItemId.Zhonyas_Hourglass,
index = 10
},
new ItemToShop()
{
Price = 860,
needItem = ItemId.Zhonyas_Hourglass,
item = ItemId.Blasting_Wand,
index = 11
},
new ItemToShop()
{
Price = 1600,
needItem = ItemId.Blasting_Wand,
item = ItemId.Needlessly_Large_Rod,
index = 12
},
new ItemToShop()
{
Price = 840,
needItem = ItemId.Needlessly_Large_Rod,
item = ItemId.Rabadons_Deathcap,
index = 13
},
new ItemToShop()
{
Price = 860,
needItem = ItemId.Rabadons_Deathcap,
item = ItemId.Blasting_Wand,
index = 14
},
new ItemToShop()
{
Price = 1435,
needItem = ItemId.Blasting_Wand,
item = ItemId.Void_Staff,
index = 15
},
new ItemToShop()
{
Price = 2750,
needItem = ItemId.Void_Staff,
item = ItemId.Banshees_Veil,
index = 16
}
};
    #endregion
    #region bap
    public static List<ItemToShop> buyThings_BAP = new List<ItemToShop>
{
new ItemToShop()
{
Price = 450,
needItem = ItemId.Hunters_Machete,
item = ItemId.Stalkers_Blade,
index = 1
},
new ItemToShop()
{
Price = 820,
needItem = ItemId.Stalkers_Blade,
item = ItemId.Fiendish_Codex,
index = 2
},
new ItemToShop()
{
Price = 580,
needItem = ItemId.Fiendish_Codex,
item = ItemId.Stalkers_Blade_Enchantment_Magus,
index = 3
},
new ItemToShop()
{
Price = 1100,
needItem = ItemId.Stalkers_Blade_Enchantment_Magus,
item = ItemId.Sorcerers_Shoes,
index = 4
},
new ItemToShop()
{
Price = 820,
needItem = ItemId.Sorcerers_Shoes,
item = ItemId.Fiendish_Codex,
index = 5
},
new ItemToShop()
{
Price = 600,
needItem = ItemId.Fiendish_Codex,
item = ItemId.Forbidden_Idol,
index = 6
},
new ItemToShop()
{
Price = 880,
needItem = ItemId.Forbidden_Idol,
item = ItemId.Morellonomicon,
index = 7
},
new ItemToShop()
{
Price = 1200,
needItem = ItemId.Morellonomicon,
item = ItemId.Seekers_Armguard,
index = 8
},
new ItemToShop()
{
Price = 1600,
needItem = ItemId.Seekers_Armguard,
item = ItemId.Needlessly_Large_Rod,
index = 9
},
new ItemToShop()
{
Price = 500,
needItem = ItemId.Needlessly_Large_Rod,
item = ItemId.Zhonyas_Hourglass,
index = 10
},
new ItemToShop()
{
Price = 860,
needItem = ItemId.Zhonyas_Hourglass,
item = ItemId.Blasting_Wand,
index = 11
},
new ItemToShop()
{
Price = 1600,
needItem = ItemId.Blasting_Wand,
item = ItemId.Needlessly_Large_Rod,
index = 12
},
new ItemToShop()
{
Price = 840,
needItem = ItemId.Needlessly_Large_Rod,
item = ItemId.Rabadons_Deathcap,
index = 13
},
new ItemToShop()
{
Price = 860,
needItem = ItemId.Rabadons_Deathcap,
item = ItemId.Blasting_Wand,
index = 14
},
new ItemToShop()
{
Price = 1435,
needItem = ItemId.Blasting_Wand,
item = ItemId.Void_Staff,
index = 15
},
new ItemToShop()
{
Price = 2750,
needItem = ItemId.Void_Staff,
item = ItemId.Banshees_Veil,
index = 16
}
};
    #endregion
    #region hi
    public static List<ItemToShop> buyThings_HI = new List<ItemToShop>
{
new ItemToShop()
{
Price = 450 + 820,
needItem = ItemId.Hunters_Machete,
item = ItemId.Stalkers_Blade,
index = 1
},
new ItemToShop()
{
Price = 820,
needItem = ItemId.Stalkers_Blade,
item = ItemId.Fiendish_Codex,
index = 2
},
new ItemToShop()
{
Price = 580 + 720,
needItem = ItemId.Fiendish_Codex,
item = ItemId.Stalkers_Blade_Enchantment_Magus,
index = 3
},
new ItemToShop()
{
Price = 360,
needItem = ItemId.Stalkers_Blade_Enchantment_Magus,
item = ItemId.Long_Sword,
index = 4
},
new ItemToShop()
{
Price = 360,
needItem = ItemId.Long_Sword,
item = ItemId.Long_Sword,
index = 5
},
new ItemToShop()
{
Price = 1100,
needItem = ItemId.Long_Sword,
item = ItemId.Sorcerers_Shoes,
index = 6
},
new ItemToShop()
{
Price = 680,
needItem = ItemId.Sorcerers_Shoes,
item = ItemId.Bilgewater_Cutlass,
index = 7
},
new ItemToShop()
{
Price = 1200,
needItem = ItemId.Bilgewater_Cutlass,
item = ItemId.Hextech_Revolver,
index = 8
},
new ItemToShop()
{
Price = 800,
needItem = ItemId.Hextech_Revolver,
item = ItemId.Hextech_Gunblade,
index = 9
},
new ItemToShop()
{
Price = 1200,
needItem = ItemId.Hextech_Gunblade,
item = ItemId.Seekers_Armguard,
index = 10
},
new ItemToShop()
{
Price = 1600,
needItem = ItemId.Seekers_Armguard,
item = ItemId.Needlessly_Large_Rod,
index = 11
},
new ItemToShop()
{
Price = 500,
needItem = ItemId.Needlessly_Large_Rod,
item = ItemId.Zhonyas_Hourglass,
index = 12
},
new ItemToShop()
{
Price = 860,
needItem = ItemId.Zhonyas_Hourglass,
item = ItemId.Blasting_Wand,
index = 13
},
new ItemToShop()
{
Price = 1600,
needItem = ItemId.Blasting_Wand,
item = ItemId.Needlessly_Large_Rod,
index = 14
},
new ItemToShop()
{
Price = 840,
needItem = ItemId.Needlessly_Large_Rod,
item = ItemId.Rabadons_Deathcap,
index = 15
},
new ItemToShop()
{
Price = 860,
needItem = ItemId.Rabadons_Deathcap,
item = ItemId.Blasting_Wand,
index = 16
},
new ItemToShop()
{
Price = 1435,
needItem = ItemId.Blasting_Wand,
item = ItemId.Void_Staff,
index = 17
}
};
    #endregion
    #region ad = default
    public static List<ItemToShop> buyThings = new List<ItemToShop>

/*new ItemToShop()
{
Price = 1850,
needItem = ItemId.Hunters_Machete,
item = ItemId.Stalkers_Blade_Enchantment_Devourer,
index = 1
},*/
{ //우르프 모드를 위해 제거했음 임시로
new ItemToShop()
{
Price = 450,
needItem = ItemId.Hunters_Machete,
item = ItemId.Stalkers_Blade,
index = 1
},
new ItemToShop()
{
Price = 450,
needItem = ItemId.Stalkers_Blade,
item = ItemId.Dagger,
index = 2
},
new ItemToShop()
{
Price = 950,
needItem = ItemId.Dagger,
item = ItemId.Stalkers_Blade_Enchantment_Devourer,
index = 3
},
new ItemToShop()
{
Price = 325,
needItem = ItemId.Stalkers_Blade_Enchantment_Devourer,
item = ItemId.Boots_of_Speed,
index = 4
},
new ItemToShop()
{
Price = 675,
needItem = ItemId.Boots_of_Speed,
item = ItemId.Berserkers_Greaves,
index = 5
},
new ItemToShop()
{
Price = 1475 - 325 - 675,
needItem = ItemId.Stalkers_Blade_Enchantment_Devourer,
item = ItemId.Berserkers_Greaves_Enchantment_Furor,
index = 6
},
new ItemToShop()
{
Price = 875,
needItem = ItemId.Berserkers_Greaves_Enchantment_Furor,
item = ItemId.Pickaxe,
index = 7
},
new ItemToShop()
{
Price = 1025,
needItem = ItemId.Pickaxe,
item = ItemId.Tiamat_Melee_Only,
index = 8
},
new ItemToShop()
{
Price = 1400,
needItem = ItemId.Tiamat_Melee_Only,
item = ItemId.Bilgewater_Cutlass,
index = 9
},
new ItemToShop()
{
Price = 1400,
needItem = ItemId.Bilgewater_Cutlass,
item = ItemId.Ravenous_Hydra_Melee_Only,
index = 10
},
new ItemToShop()
{
Price = 1100,
needItem = ItemId.Ravenous_Hydra_Melee_Only,
item = ItemId.Zeal,
index = 11
},
new ItemToShop()
{
Price = 1700,
needItem = ItemId.Zeal,
item = ItemId.Phantom_Dancer,
index = 12
},
new ItemToShop()
{
Price = 1800,
needItem = ItemId.Phantom_Dancer,
item = ItemId.Blade_of_the_Ruined_King,
index = 13
},
new ItemToShop()
{
Price = 1550,
needItem = ItemId.Blade_of_the_Ruined_King,
item = ItemId.B_F_Sword,
index = 14
},
new ItemToShop()
{
Price = 2250,
needItem = ItemId.B_F_Sword,
item = ItemId.Infinity_Edge,
index = 15
}
};
    #endregion
    #region adc
    public static List<ItemToShop> buyThings_ADC = new List<ItemToShop>
{
new ItemToShop()
{
Price = 450,
needItem = ItemId.Hunters_Machete,
item = ItemId.Rangers_Trailblazer,
index = 1
},
new ItemToShop()
{
Price = 450,
needItem = ItemId.Rangers_Trailblazer,
item = ItemId.Dagger,
index = 2
},
new ItemToShop()
{
Price = 950,
needItem = ItemId.Dagger,
item = ItemId.Rangers_Trailblazer_Enchantment_Devourer,
index = 3
},
new ItemToShop()
{
Price = 1475,
needItem = ItemId.Rangers_Trailblazer_Enchantment_Devourer,
item = ItemId.Berserkers_Greaves_Enchantment_Homeguard,
index = 4
},
new ItemToShop()
{
Price = 875,
needItem = ItemId.Berserkers_Greaves_Enchantment_Homeguard,
item = ItemId.Pickaxe,
index = 5
},
new ItemToShop()
{
Price = 1550,
needItem = ItemId.Pickaxe,
item = ItemId.B_F_Sword,
index = 6
},
new ItemToShop()
{
Price = 875,
needItem = ItemId.B_F_Sword,
item = ItemId.Pickaxe,
index = 7
},
new ItemToShop()
{
Price = 2250 - 875,
needItem = ItemId.Pickaxe,
item = ItemId.Infinity_Edge,
index = 8
},
new ItemToShop()
{
Price = 1100,
needItem = ItemId.Infinity_Edge,
item = ItemId.Vampiric_Scepter,
index = 9
},
new ItemToShop()
{
Price = 1100,
needItem = ItemId.Vampiric_Scepter,
item = ItemId.Zeal,
index = 10
},
new ItemToShop()
{
Price = 1700,
needItem = ItemId.Zeal,
item = ItemId.Phantom_Dancer,
index = 11
},
new ItemToShop()
{
Price = 875,
needItem = ItemId.Phantom_Dancer,
item = ItemId.Pickaxe,
index = 12
},
new ItemToShop()
{
Price = 2300 - 875,
needItem = ItemId.Pickaxe,
item = ItemId.Last_Whisper,
index = 13
},
new ItemToShop()
{
Price = 3500 - 800,
needItem = ItemId.Last_Whisper,
item = ItemId.The_Bloodthirster,
index = 14
}
};
    #endregion
    #region as
    public static List<ItemToShop> buyThings_AS = new List<ItemToShop>
{
new ItemToShop()
{
Price = 450,
needItem = ItemId.Hunters_Machete,
item = ItemId.Rangers_Trailblazer,
index = 1
},
new ItemToShop()
{
Price = 450,
needItem = ItemId.Rangers_Trailblazer,
item = ItemId.Dagger,
index = 2
},
new ItemToShop()
{
Price = 950,
needItem = ItemId.Dagger,
item = ItemId.Rangers_Trailblazer_Enchantment_Devourer,
index = 3
},
new ItemToShop()
{
Price = 1475,
needItem = ItemId.Rangers_Trailblazer_Enchantment_Devourer,
/*item = ItemId.Boots_of_Speed,
index = 4
},
new ItemToShop()
{
Price = 675,
needItem = ItemId.Boots_of_Speed,
*/item = ItemId.Berserkers_Greaves_Enchantment_Homeguard,
index = 4
},
new ItemToShop()
{
Price = 1400,
needItem = ItemId.Berserkers_Greaves_Enchantment_Homeguard,
item = ItemId.Bilgewater_Cutlass,
index = 5
},
new ItemToShop()
{
Price = 1800,
needItem = ItemId.Bilgewater_Cutlass,
item = ItemId.Blade_of_the_Ruined_King,
index = 6
},
new ItemToShop()
{
Price = 900,
needItem = ItemId.Blade_of_the_Ruined_King,
item = ItemId.Recurve_Bow,
index = 7
},
new ItemToShop()
{
Price = 500+750+450,
needItem = ItemId.Recurve_Bow,
item = ItemId.Wits_End,
index = 8
},
new ItemToShop()
{
Price = 1900,
needItem = ItemId.Wits_End,
item = ItemId.Tiamat_Melee_Only,
index = 9
},
new ItemToShop()
{
Price = 800+600,
needItem = ItemId.Tiamat_Melee_Only,
item = ItemId.Ravenous_Hydra_Melee_Only,
index = 10
},
new ItemToShop()
{
Price = 2900,
needItem = ItemId.Ravenous_Hydra_Melee_Only,
item = ItemId.Last_Whisper,
index = 11
}
};
    #endregion
    #region tanky
    public static List<ItemToShop> buyThings_TANK = new List<ItemToShop>
{
new ItemToShop()
{
Price = 450,
needItem = ItemId.Hunters_Machete,
item = ItemId.Rangers_Trailblazer,
index = 1
},
new ItemToShop()
{
Price = 450,
needItem = ItemId.Rangers_Trailblazer,
item = ItemId.Dagger,
index = 2
},
new ItemToShop()
{
Price = 950,
needItem = ItemId.Dagger,
item = ItemId.Rangers_Trailblazer_Enchantment_Devourer,
index = 3
},
new ItemToShop()
{
Price = 1000,
needItem = ItemId.Rangers_Trailblazer_Enchantment_Devourer,
item = ItemId.Ionian_Boots_of_Lucidity,
index = 4
},
new ItemToShop()
{
Price = 500,
needItem = ItemId.Ionian_Boots_of_Lucidity,
item = ItemId.Null_Magic_Mantle,
index = 5
},
new ItemToShop()
{
Price = 900,
needItem = ItemId.Null_Magic_Mantle,
item = ItemId.Recurve_Bow,
index = 6
},
new ItemToShop()
{
Price = 1200,
needItem = ItemId.Recurve_Bow,
item = ItemId.Wits_End,
index = 7
},
new ItemToShop()
{
Price = 950,
needItem = ItemId.Wits_End,
item = ItemId.Glacial_Shroud,
index = 8
},
new ItemToShop()
{
Price = 1050+450,
needItem = ItemId.Glacial_Shroud,
item = ItemId.Frozen_Heart,
index = 9
},
new ItemToShop()
{
Price = 500,
needItem = ItemId.Frozen_Heart,
item = ItemId.Null_Magic_Mantle,
index = 10
},
new ItemToShop()
{
Price = 400+1150,
needItem = ItemId.Null_Magic_Mantle,
item = ItemId.Banshees_Veil,
index = 11
},
new ItemToShop()
{
Price = 1200,
needItem = ItemId.Banshees_Veil,
item = ItemId.Sheen,
index = 12
},
new ItemToShop()
{
Price = 1325,
needItem = ItemId.Sheen,
item = ItemId.Phage,
index = 13
},
new ItemToShop()
{
Price = 1178,
needItem = ItemId.Phage,
item = ItemId.Trinity_Force,
index = 14
},
};
    #endregion
    #endregion
    private static void Main(string[] args)
    {
        CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
    }

    private static void Game_OnGameLoad(EventArgs args)
    {
        /*////////////////////customizing////////////////// 브로킹됨!!!!!!
        var dir = new DirectoryInfo(Config.AppDataDirectory.ToString() + @"\RLProjectAutoJungle");
        var setFile = new FileInfo(dir + "/" + Player.ChampionName + ".ini");
        #region File Stream
        try
        {
            if (!dir.Exists)
                dir.Create();
            if (!setFile.Exists)
            {
                Readini.Setini(setFile.FullName);
                GamePrintChat("Something Wrong. Try Again");
            }
        }
        catch
        { }
        #endregion
        ////////////////////////////////////////////////*/
        RLProjectAutoJungleMenu = new Menu("RLProjectAutoJungle", "RLProjectAutoJungle", true);
        RLProjectAutoJungleMenu.AddItem(new MenuItem("isActive", "Activate")).SetValue(true);
        RLProjectAutoJungleMenu.AddItem(new MenuItem("maxstacks", "Max Stacks").SetValue(new Slider(9, 1, 150)));
        RLProjectAutoJungleMenu.AddItem(new MenuItem("maxlv", "Max level").SetValue(new Slider(9, 1, 18)));
        RLProjectAutoJungleMenu.AddItem(new MenuItem("autorecallheal", "Recall[for heal]")).SetValue(true);
        RLProjectAutoJungleMenu.AddItem(new MenuItem("hpper", "Recall on HP(%)").SetValue(new Slider(50, 0, 100)));
        RLProjectAutoJungleMenu.AddItem(new MenuItem("ehhro", "Enemy in Range").SetValue(new Slider(1, 1, 5)));
        RLProjectAutoJungleMenu.AddItem(new MenuItem("ehhro2", "Enemy in Far Range").SetValue(new Slider(2, 1, 5)));
        RLProjectAutoJungleMenu.AddItem(new MenuItem("autorecallitem", "Recall[for item]")).SetValue(true);
        RLProjectAutoJungleMenu.AddItem(new MenuItem("evading", "Detect TurretAttack")).SetValue(true);
        RLProjectAutoJungleMenu.AddItem(new MenuItem("Invade", "InvadeEnemyJungle?")).SetValue(true);
        RLProjectAutoJungleMenu.AddItem(new MenuItem("k_dragon", "Add Dragon to Route on Lv").SetValue(new Slider(10, 1, 18)));
        if (Player.ChampionName == "MasterYi")
            RLProjectAutoJungleMenu.AddItem(new MenuItem("yi_W", "Cast MasterYi-W(%)").SetValue(new Slider(58, 0, 100)));
        Orbwalker = new Orbwalking.Orbwalker(RLProjectAutoJungleMenu.AddSubMenu(new Menu(Player.ChampionName + ": Orbwalker", "Orbwalker")));
        TargetSelector.AddToMenu(RLProjectAutoJungleMenu.AddSubMenu(new Menu(Player.ChampionName + ": Target Selector", "Target Selector")));
            RLProjectAutoJungleMenu.AddToMainMenu();
            //메뉴
            setSmiteSlot();
        if (Player.ChampionName == "Nidalee")//for 니달리
        {
        // Add drawing skill list
        CougarSpellList.AddRange(new[] { Takedown, Pounce, Swipe });
        HumanSpellList.AddRange(new[] { Javelin, Bushwack, Primalsurge });
        // Set skillshot prediction (i has rito decode now)
        Javelin.SetSkillshot(0.125f, 40f, 1300f, true, SkillshotType.SkillshotLine);
        Bushwack.SetSkillshot(0.50f, 100f, 1500f, false, SkillshotType.SkillshotCircle);
        Swipe.SetSkillshot(0.50f, 375f, 1500f, false, SkillshotType.SkillshotCone);
        Pounce.SetSkillshot(0.50f, 400f, 1500f, false, SkillshotType.SkillshotCone);
        }
        
        #region 스펠설정
        Q = new Spell(SpellSlot.Q, GetSpellRange(Qdata));
        W = new Spell(SpellSlot.W, GetSpellRange(Wdata));
        E = new Spell(SpellSlot.E, GetSpellRange(Edata));
        R = new Spell(SpellSlot.R, GetSpellRange(Rdata));
        #endregion
        #region 지점 설정
        if (Player.Team.ToString() == "Chaos")
        {
            spawn = new Vector3(14318f, 14354, 171.97f);
            enemy_spawn = new Vector3(415.33f, 453.38f, 182.66f);
            GamePrintChat("Set PurpleTeam Spawn");
            IsBlueTeam = false;
            MonsterList.First(temp => temp.ID == pteam_Krug.ID).order = 1;
            MonsterList.First(temp => temp.ID == pteam_Red.ID).order = 2;
            MonsterList.First(temp => temp.ID == pteam_Razorbeak.ID).order = 3;
            MonsterList.First(temp => temp.ID == bteam_Gromp.ID).order = 4;
            MonsterList.First(temp => temp.ID == bteam_Blue.ID).order = 5;
            MonsterList.First(temp => temp.ID == bteam_Wolf.ID).order = 6;
            MonsterList.First(temp => temp.ID == top_crab.ID).order = 7;
            MonsterList.First(temp => temp.ID == PURPLE_MID.ID).order = 8;
            MonsterList.First(temp => temp.ID == pteam_Wolf.ID).order = 9;
            MonsterList.First(temp => temp.ID == pteam_Blue.ID).order = 10;
            MonsterList.First(temp => temp.ID == pteam_Gromp.ID).order = 11;
            MonsterList.First(temp => temp.ID == bteam_Razorbeak.ID).order = 12;
            MonsterList.First(temp => temp.ID == bteam_Red.ID).order = 13;
            MonsterList.First(temp => temp.ID == bteam_Krug.ID).order = 14;
            MonsterList.First(temp => temp.ID == down_crab.ID).order = 15;
        }
        else
        {
            spawn = new Vector3(415.33f, 453.38f, 182.66f);
            enemy_spawn = new Vector3(14318f, 14354, 171.97f);
            GamePrintChat("Set BlueTeam Spawn");
            IsBlueTeam = true;
            MonsterList.First(temp => temp.ID == bteam_Gromp.ID).order = 1;
            MonsterList.First(temp => temp.ID == bteam_Blue.ID).order = 2;
            MonsterList.First(temp => temp.ID == bteam_Wolf.ID).order = 3;
            MonsterList.First(temp => temp.ID == bteam_Razorbeak.ID).order = 8;
            MonsterList.First(temp => temp.ID == bteam_Red.ID).order = 9;
            MonsterList.First(temp => temp.ID == bteam_Krug.ID).order = 10;
            MonsterList.First(temp => temp.ID == pteam_Razorbeak.ID).order = 6;
            MonsterList.First(temp => temp.ID == pteam_Red.ID).order = 5;
            MonsterList.First(temp => temp.ID == pteam_Krug.ID).order = 4;
            MonsterList.First(temp => temp.ID == top_crab.ID).order = 7;
            MonsterList.First(temp => temp.ID == BLUE_MID.ID).order = 15;
            MonsterList.First(temp => temp.ID == down_crab.ID).order = 14;
            MonsterList.First(temp => temp.ID == pteam_Gromp.ID).order = 11;
            MonsterList.First(temp => temp.ID == pteam_Blue.ID).order = 12;
            MonsterList.First(temp => temp.ID == pteam_Wolf.ID).order = 13;
        }
        max = MonsterList.OrderByDescending(h => h.order).First().order;
        #endregion
/*        #region 챔피언 설정 원래 값
        if (Player.ChampionName.ToUpper() == "NUNU")
        {
            GetItemTree(setFile);
            GamePrintChat("NUNU BOT ACTIVE");
            Readini.GetSpelltree(setFile.FullName);
        }
        else if (Player.ChampionName.ToUpper() == "WARWICK")
        {
            GetItemTree(setFile);
            GamePrintChat("WARWICK BOT ACTIVE");
            Readini.GetSpelltree(setFile.FullName);
        }
        else if (Player.ChampionName.ToUpper() == "MASTERYI")
        {
            GetItemTree(setFile);
            GamePrintChat("MASTER YI BOT ACTIVE");
            Readini.GetSpelltree(setFile.FullName);
        }
        else if (Player.ChampionName.ToUpper() == "CHOGATH")
        {
            GetItemTree(setFile);
            GamePrintChat("CHOGATH BOT ACTIVE");
            Readini.GetSpelltree(setFile.FullName);
        }
        else if (Player.ChampionName.ToUpper() == "MAOKAI")
        {
            GetItemTree(setFile);
            GamePrintChat("MAOKAI BOT ACTIVE");
            Readini.GetSpelltree(setFile.FullName);
        }
        else if (Player.ChampionName.ToUpper() == "NASUS")
        {
            GetItemTree(setFile);
            GamePrintChat("NASUS BOT ACTIVE");
            Readini.GetSpelltree(setFile.FullName);
        }
        else if (Player.ChampionName.ToUpper() == "XINZHAO")
        {
            GetItemTree(setFile);
            GamePrintChat("XINZHAO is now going to Chawchaw");
            Readini.GetSpelltree(setFile.FullName);
        }
        else if (Player.ChampionName.ToUpper() == "NIDALEE")
        {
            GetItemTree(setFile);
            GamePrintChat("NIDALEE CARRY IP");
            Readini.GetSpelltree(setFile.FullName);
        }
        else if (Player.ChampionName.ToUpper() == "JINX")
        {
            GetItemTree(setFile);
            GamePrintChat("Get Jinxed!!");
            Readini.GetSpelltree(setFile.FullName);
        }
        else
        {
            #region Read ini
            GamePrintChat("Read ini file");
            Readini.GetSpelltree(setFile.FullName);
            GetItemTree(setFile);
            Readini.GetSpells(setFile.FullName, ref cast2mob, ref cast2hero, ref cast4laneclear);
            #endregion readini
        }
        #endregion
*/
        #region 챔피언 설정
        if (Player.ChampionName.ToUpper() == "NUNU")
        {
            GetItemTree("TANK");
            GamePrintChat("NUNU BOT ACTIVE");
            Readini.GetSpelltree(new int[] { 1, 3, 2, 1, 1, 4, 1, 3, 1, 3, 4, 2, 2, 2, 2, 4, 3, 3 });
        }
        else if (Player.ChampionName.ToUpper() == "WARWICK")
        {
            GetItemTree("AS");
            GamePrintChat("WARWICK BOT ACTIVE");
            Readini.GetSpelltree(new int[] { 1, 2, 3, 1, 1, 4, 1, 2, 1, 2, 4, 3, 2, 3, 2, 4, 3, 3 });
        }
        else if (Player.ChampionName.ToUpper() == "MASTERYI")
        {
            GetItemTree("AD");
            GamePrintChat("MASTER YI BOT ACTIVE");
            Readini.GetSpelltree(new int[] { 1, 2, 3, 1, 1, 4, 1, 3, 1, 3, 4, 2, 3, 2, 3, 4, 2, 2 });
        }
        else if (Player.ChampionName.ToUpper() == "CHOGATH")
        {
            GetItemTree("TANK");
            GamePrintChat("CHOGATH BOT ACTIVE");
            Readini.GetSpelltree(new[] { 1, 3, 2, 1, 1, 4, 1, 3, 1, 3, 4, 2, 2, 2, 2, 4, 3, 3 });
        }
        else if (Player.ChampionName.ToUpper() == "MAOKAI")
        {
            GetItemTree("TANK");
            GamePrintChat("MAOKAI BOT ACTIVE");
            Readini.GetSpelltree(new[] { 1, 3, 2, 1, 1, 4, 1, 3, 1, 3, 4, 2, 2, 2, 2, 4, 3, 3 });
        }
        else if (Player.ChampionName.ToUpper() == "NASUS")
        {
            GetItemTree("TANK");
            GamePrintChat("NASUS BOT ACTIVE");
            Readini.GetSpelltree(new[] { 1, 3, 2, 1, 1, 4, 1, 3, 1, 3, 4, 2, 2, 2, 2, 4, 3, 3 });
        }
        else if (Player.ChampionName.ToUpper() == "XINZHAO")
        {
            GetItemTree("AD");
            GamePrintChat("XINZHAO is now going to Chawchaw");
            Readini.GetSpelltree(new int[] { 1, 2, 3, 1, 1, 4, 1, 3, 1, 3, 4, 2, 3, 2, 3, 4, 2, 2 });
        }
        else if (Player.ChampionName.ToUpper() == "NIDALEE")
        {
            GetItemTree("BAP");
            GamePrintChat("NIDALEE CARRY IP");
            Readini.GetSpelltree(new int[] { 2, 2, 1, 3, 1, 1, 4, 1, 3, 1, 3, 4, 2, 3, 2, 3, 4, 2, 2 });
        }
        else if (Player.ChampionName.ToUpper() == "JINX")
        {
            GetItemTree("ADC");
            GamePrintChat("Get Jinxed!!");
            Readini.GetSpelltree(new int[] { 1, 2, 3, 1, 1, 4, 1, 3, 1, 3, 4, 2, 3, 2, 3, 4, 2, 2 });
        }
        else if (Player.ChampionName.ToUpper() == "VAYNE")
        {
            GetItemTree("ADC");
            GamePrintChat("VAAAAAYYYYYYNNNNNNNEEEEEE!!");
            Readini.GetSpelltree(new int[] { 1, 2, 3, 1, 1, 4, 1, 2, 1, 2, 4, 3, 2, 3, 2, 4, 3, 3 });
        }
        else
        {
            #region Read ini
            GamePrintChat("Read ini file");
            Readini.GetSpelltree(new int[] { 1, 2, 3, 1, 1, 4, 1, 3, 1, 3, 4, 2, 3, 2, 3, 4, 2, 2 });
            GetItemTree("AD");
            //Readini.GetSpells(setFile.FullName, ref cast2mob, ref cast2hero, ref cast4laneclear);
            #endregion readini
        }
        #endregion

        #region 현재 아이템 단계 설정 - 도중 리로드시 필요
        if (buyThings.Any(h => Items.HasItem((int)(h.needItem))))
        {
            if (buyThings.First().needItem != buyThings.Last(h => Items.HasItem((int)(h.needItem))).needItem)
            {
                var lastitem = buyThings.Last(h => Items.HasItem((int)(h.needItem)));
                GamePrintChat("Find new ItemList");
                List<ItemToShop> newlist = buyThings.Where(t => t.index >= lastitem.index).ToList();
                buyThings.Clear();
                buyThings = newlist;
            }
        }
        #endregion
        gamestart = Game.Time; // 시작시간 설정
        Game.OnUpdate += Game_OnUpdate;
        GameObject.OnCreate += OnCreate;
        Obj_AI_Base.OnProcessSpellCast += OnSpell;
        AddOn.Load();
        if (smiteSlot == SpellSlot.Unknown)
            GamePrintChat("YOU ARE NOT JUNGLER(NO SMITE)");
    }
    private static Obj_AI_Hero GetTarget()
    {
        Obj_AI_Hero Target = null;
        /*if (ChoosedTarget == null)
        {*/
            Target = TargetSelector.GetTarget((Player.AttackRange / 3) + 900 , TargetSelector.DamageType.Physical);
/*            }
        else
        {
            Target = ChoosedTarget;
        }*/
        return Target;
    }
    public static bool CB()
    {
        bool b = false;
        if (Target != null)
        b = true;
        return b;
    }
    private static void Game_OnUpdate(EventArgs args) 
    {
    int maxlv = RLProjectAutoJungleMenu.Item("maxlv").GetValue<Slider>().Value;
    int level = Player.Level;    
        Orbwalker.SetMovement(false);
        setSmiteSlot();
        Target = GetTarget();
        if (Player.ChampionName == "Nidalee")
        _cougarForm = Player.Spellbook.GetSpell(SpellSlot.Q).Name != "JavelinToss";
        if (Player.Spellbook.IsChanneling) // 마스터이w라던가 피들 w라던가..!!!
            return;
        if (!RLProjectAutoJungleMenu.Item("isActive").GetValue<Boolean>() || smiteSlot == SpellSlot.Unknown)
            return;
        #region detect afk
        if (Game.Time - pastTimeAFK >= 1 && !Player.IsDead && !Player.IsRecalling() && IsOVER && IsAttackStart && false == recall) //어짜피 IsOVER일때 말곤 그럴일없음.
        {
            afktime += 1;
            if (afktime > 5) // 잠수 5초 경과
            {
                var turret = ObjectManager.Get<Obj_AI_Turret>().OrderBy(t => t.Distance(Player.Position)).First(t => t.IsEnemy);
            int turretcount = GetEnemyTurretList().Where(x => x.Distance(Player.Position) <= 20000).Count();
                if (Player.InShop() && getHealthPercent(Player) > NonRCHP)
                {
                //if(turretcount >= 1)
                //    Player.IssueOrder(GameObjectOrder.AttackTo, turret.Position.Extend(Player.Position, 10));
                //else
                    Player.IssueOrder(GameObjectOrder.AttackTo, enemy_spawn);
                }
                else if(!Player.InShop())
                    {
                        if(IsOVER && IsAttackStart && getHealthPercent(Player) > 50)
                            Player.IssueOrder(GameObjectOrder.AttackTo, enemy_spawn);
                        else
                            Player.Spellbook.CastSpell(SpellSlot.Recall);
                    }
                afktime = 0;
            }
            pastTimeAFK = Game.Time;
        }
        #endregion
        #region 0.5초마다 발동 // 오류 없애줌
        if (Environment.TickCount - pastTime <= 500) return;
        pastTime = Environment.TickCount;
        #endregion
        #region 카이팅 타임
        if (Environment.TickCount - kiTime < 80) return;
        kiTime = Environment.TickCount;
        #endregion
        #region InvadeEnemyJungle
        if (!IsBlueTeam)
        {
            if (!RLProjectAutoJungleMenu.Item("Invade").GetValue<Boolean>())
            {
                MonsterList.First(temp => temp.ID == bteam_Gromp.ID).order = 0;
                MonsterList.First(temp => temp.ID == bteam_Blue.ID).order = 0;
                MonsterList.First(temp => temp.ID == bteam_Wolf.ID).order = 0;
                MonsterList.First(temp => temp.ID == top_crab.ID).order = 0;
                MonsterList.First(temp => temp.ID == PURPLE_MID.ID).order = 0;
                MonsterList.First(temp => temp.ID == down_crab.ID).order = 0;
                MonsterList.First(temp => temp.ID == bteam_Razorbeak.ID).order = 0;
                MonsterList.First(temp => temp.ID == bteam_Red.ID).order = 0;
                MonsterList.First(temp => temp.ID == bteam_Krug.ID).order = 0;
                MonsterList.First(temp => temp.ID == pteam_Krug.ID).order = 1;
                MonsterList.First(temp => temp.ID == pteam_Red.ID).order = 2;
                MonsterList.First(temp => temp.ID == pteam_Razorbeak.ID).order = 3;
                MonsterList.First(temp => temp.ID == pteam_Wolf.ID).order = 4;
                MonsterList.First(temp => temp.ID == pteam_Blue.ID).order = 5;
                MonsterList.First(temp => temp.ID == pteam_Gromp.ID).order = 6;
            }
            else
            {
                MonsterList.First(temp => temp.ID == pteam_Krug.ID).order = 1;
                MonsterList.First(temp => temp.ID == pteam_Red.ID).order = 2;
                MonsterList.First(temp => temp.ID == pteam_Razorbeak.ID).order = 3;
                MonsterList.First(temp => temp.ID == bteam_Gromp.ID).order = 4;
                MonsterList.First(temp => temp.ID == bteam_Blue.ID).order = 5;
                MonsterList.First(temp => temp.ID == bteam_Wolf.ID).order = 6;
                MonsterList.First(temp => temp.ID == top_crab.ID).order = 7;
                MonsterList.First(temp => temp.ID == PURPLE_MID.ID).order = 8;
                MonsterList.First(temp => temp.ID == pteam_Wolf.ID).order = 9;
                MonsterList.First(temp => temp.ID == pteam_Blue.ID).order = 10;
                MonsterList.First(temp => temp.ID == pteam_Gromp.ID).order = 11;
                MonsterList.First(temp => temp.ID == bteam_Razorbeak.ID).order = 12;
                MonsterList.First(temp => temp.ID == bteam_Red.ID).order = 13;
                MonsterList.First(temp => temp.ID == bteam_Krug.ID).order = 14;
                MonsterList.First(temp => temp.ID == down_crab.ID).order = 15;
            }
        }
        else
        {
            if (!RLProjectAutoJungleMenu.Item("Invade").GetValue<Boolean>())
            {
                MonsterList.First(temp => temp.ID == pteam_Razorbeak.ID).order = 0;
                MonsterList.First(temp => temp.ID == pteam_Red.ID).order = 0;
                MonsterList.First(temp => temp.ID == pteam_Krug.ID).order = 0;
                MonsterList.First(temp => temp.ID == top_crab.ID).order = 0;
                MonsterList.First(temp => temp.ID == BLUE_MID.ID).order = 0;
                MonsterList.First(temp => temp.ID == down_crab.ID).order = 0;
                MonsterList.First(temp => temp.ID == pteam_Gromp.ID).order = 0;
                MonsterList.First(temp => temp.ID == pteam_Blue.ID).order = 0;
                MonsterList.First(temp => temp.ID == pteam_Wolf.ID).order = 0;
                MonsterList.First(temp => temp.ID == bteam_Gromp.ID).order = 1;
                MonsterList.First(temp => temp.ID == bteam_Blue.ID).order = 2;
                MonsterList.First(temp => temp.ID == bteam_Wolf.ID).order = 3;
                MonsterList.First(temp => temp.ID == bteam_Razorbeak.ID).order = 4;
                MonsterList.First(temp => temp.ID == bteam_Red.ID).order = 5;
                MonsterList.First(temp => temp.ID == bteam_Krug.ID).order = 6;
            }
            else
            {
                MonsterList.First(temp => temp.ID == bteam_Gromp.ID).order = 1;
                MonsterList.First(temp => temp.ID == bteam_Blue.ID).order = 2;
                MonsterList.First(temp => temp.ID == bteam_Wolf.ID).order = 3;
                MonsterList.First(temp => temp.ID == bteam_Razorbeak.ID).order = 8;
                MonsterList.First(temp => temp.ID == bteam_Red.ID).order = 9;
                MonsterList.First(temp => temp.ID == bteam_Krug.ID).order = 10;
                MonsterList.First(temp => temp.ID == pteam_Razorbeak.ID).order = 6;
                MonsterList.First(temp => temp.ID == pteam_Red.ID).order = 5;
                MonsterList.First(temp => temp.ID == pteam_Krug.ID).order = 4;
                MonsterList.First(temp => temp.ID == top_crab.ID).order = 7;
                MonsterList.First(temp => temp.ID == BLUE_MID.ID).order = 15;
                MonsterList.First(temp => temp.ID == down_crab.ID).order = 14;
                MonsterList.First(temp => temp.ID == pteam_Gromp.ID).order = 11;
                MonsterList.First(temp => temp.ID == pteam_Blue.ID).order = 12;
                MonsterList.First(temp => temp.ID == pteam_Wolf.ID).order = 13;
            }
        }
        max = MonsterList.OrderByDescending(h => h.order).First().order;
        #endregion
        #region detect reload
        if (IsStart && Player.Level > 1)
        {
            GamePrintChat("You did reload");
            IsStart = false;
        }
        #endregion
        #region check somethings about dragon
        if (Player.Level > RLProjectAutoJungleMenu.Item("k_dragon").GetValue<Slider>().Value)
        {
            if (MonsterList.First(temp => temp.ID == down_crab.ID).order == 14)
            {
                MonsterList.First(temp => temp.ID == down_crab.ID).order = 0;
                MonsterList.First(temp => temp.ID == Dragon.ID).order = 14;
            }
            if (MonsterList.First(temp => temp.ID == down_crab.ID).order == 15)
            {
                MonsterList.First(temp => temp.ID == down_crab.ID).order = 0;
                MonsterList.First(temp => temp.ID == Dragon.ID).order = 15;
            }
        }
        #endregion
        #region 오토 플레이 - auto play
        if (Player.IsMoving)
            afktime = 0;
        if (!IsOVER)
        {
            var ANEXUS = ObjectManager.Get<Obj_HQ>().Where(t => !t.IsEnemy); // 넥서승
            if (IsStart) // start
            {
                if (Game.Time - gamestart >= 0)
                {
                    if(Items.HasItem((int)(ItemId.Scrying_Orb_Trinket)))
                    {
                    Player.IssueOrder(GameObjectOrder.MoveTo, MonsterList.First(t => t.order == 1).Position);
                            if (Player.ChampionName == "Nidalee")
                            {
                                if (Player.Position.Distance(MonsterList.First(t => t.order == 1).Position) > 500)
                                {
                                    if(!_cougarForm && Aspectofcougar.IsReady())
                                    {
                                    Aspectofcougar.Cast();
                                    }
                                    if(_cougarForm && Pounce.IsReady())
                                    {
                                    Pounce.Cast(MonsterList.First(t => t.order == 1).Position);
                                    }
                                }
                            }
                    afktime = 0;
                    }
                    afktime = 0;
                }
                if (Player.Distance(MonsterList.First(t => t.order == 1).Position) <= 100)
                {
                    if (CheckMonster(MonsterList.First(t => t.order == 1).name, MonsterList.First(t => t.order == 1).Position, MonsterList.First(t => t.order == 1).Range))
                    {
                        IsStart = false;
                        now = 1;
                        GamePrintChat("START!");
                    }
                    else
                    {
                        Game.SendEmote(Emote.Laugh); //시작할때 계속 웃기
                        Console.WriteLine("Laugh");
                    }

                }
            }
            else
            {
                if (Player.IsDead && now >= 7 && now <= 9)
                    now = 5;
                if (Player.IsDead && now > 12)
                    now = 12;
                MonsterINFO target = MonsterList.First(t => t.order == now);
                if (Player.Position.Distance(target.Position) >= 1000)
                {
                    if (!recall)
                    {
                        //DoCast_Hero();
                        if (!Player.InShop() && getHealthPercent(Player) < RLProjectAutoJungleMenu.Item("hpper").GetValue<Slider>().Value && !Player.IsDead//hpper
                        && RLProjectAutoJungleMenu.Item("autorecallheal").GetValue<Boolean>()) // HP LESS THAN 25%
                        {
                            GamePrintChat("YOUR HP IS SO LOW. RECALL!");
                            recall = true;
                            Player.Spellbook.CastSpell(SpellSlot.Recall);
                            recallhp = Player.Health;
                            afktime = 0;
                        }
                        else if (!Player.InShop() && Player.Gold > buyThings.First().Price
                        && RLProjectAutoJungleMenu.Item("autorecallitem").GetValue<Boolean>()
                        && Player.InventoryItems.Length < 9) // HP LESS THAN 25%
                        {
                            GamePrintChat("CAN BUY " + buyThings.First().item.ToString() + ". RECALL!");
                            recall = true;
                            Player.Spellbook.CastSpell(SpellSlot.Recall);
                            recallhp = Player.Health;
                            afktime = 0;
                        }
                        else if (Player.Position.Distance(target.Position) > Player.AttackRange)
                        {
                            if(Player.InShop() && getHealthPercent(Player) > NonRCHP || !Player.InShop())
                            {
                                Player.IssueOrder(GameObjectOrder.MoveTo, target.Position);

                                if (Player.ChampionName == "Nidalee")
                                {
                                    if(!_cougarForm && Aspectofcougar.IsReady())
                                    {
                                    Aspectofcougar.Cast();
                                    }
                                    if(Pounce.IsReady())
                                    {
                                    Pounce.Cast(target.Position);
                                    }
                                }
                            afktime = 0;
                            }
                        }
                    }
                }
                else if (Player.Position.Distance(target.Position) <= 700 && Player.Position.Distance(target.Position) > 250)
                {
                    if(!recall)
                    {if (CheckMonster(target.name, target.Position, 800)) //해당지점에 몬스터가 있는지
                    {
                        DoCast();
                        Player.IssueOrder(GameObjectOrder.AttackUnit, GetNearest(Player.Position));
                        afktime = 0;
                        if (smite.Slot != SpellSlot.Unknown && smite.IsReady())
                            DoSmite();
                    }
                    else
                    {
                            Player.IssueOrder(GameObjectOrder.MoveTo, target.Position);
                            afktime = 0;
                    }}
                }
                else if (Player.Position.Distance(target.Position) <= 250)
                {
                    if(!recall)
                    {if (CheckMonster(target.name, target.Position, 500)) //해당지점에 몬스터가 있는지
                    {
                        DoCast();
                        Player.IssueOrder(GameObjectOrder.AttackUnit, GetNearest(Player.Position));
                        afktime = 0;
                        if (smite.Slot != SpellSlot.Unknown && smite.IsReady())
                            DoSmite();
                    }
                    else
                    {
                        now += 1;
                        if (now > max)
                            now = 1;
                    }}
                }
            }
            if (Player.InShop())
            {
                if(RLProjectAutoJungleMenu.Item("Invade").GetValue<Boolean>())
                {
                    if(!IsBlueTeam && now == 7 || !IsBlueTeam && now == 15 ||
                        IsBlueTeam && now == 7 || IsBlueTeam && now == 14) //정글 게 뻘짓 줄이기
                    now += 1;
                    if(now == 6)
                    now = 8;
                    else if(IsBlueTeam && now == 13 || !IsBlueTeam && now == 14)
                    now = 1;
                }
                recall = false;
            }
            if (level >= maxlv || Items.HasItem((int)(ItemId.Sorcerers_Shoes)))
            {
                IsOVER = true;
                GamePrintChat("You're level is" + level + ". Now Going to be offense.");
            }
        }
        else
        {
            if (level < maxlv && !Items.HasItem((int)(ItemId.Stalkers_Blade_Enchantment_Devourer)) && !Items.HasItem((int)(ItemId.Rangers_Trailblazer_Enchantment_Devourer)) && !Items.HasItem((int)(ItemId.Sorcerers_Shoes)))
            {
                GamePrintChat("You're under " + maxlv + "lv. Going back to farm.");
                IsOVER = false;
                IsAttackStart = false;
            }
        }
        #endregion
        #region 스택이 넘는지 체크 - check ur stacks
        foreach (var buff in Player.Buffs.Where(b => b.DisplayName == "Enchantment_Slayer_Stacks"))
        {
            int maxstacks = RLProjectAutoJungleMenu.Item("maxstacks").GetValue<Slider>().Value;
            if (buff.Count >= maxstacks && !IsOVER || level >= maxlv)// || Items.HasItem((int)(ItemId.Rangers_Trailblazer_Enchantment_Magus)) || Items.HasItem((int)(ItemId.Stalkers_Blade_Enchantment_Magus))) //--테스트
            {
                IsOVER = true;
                GamePrintChat("Your Stack Is  " + buff.Count + ". Now Going to be offense.");
            }
            if (buff.Count < maxstacks && IsOVER && level < maxlv && !Items.HasItem((int)(ItemId.Rangers_Trailblazer_Enchantment_Magus)) && !Items.HasItem((int)(ItemId.Stalkers_Blade_Enchantment_Magus))) // MaGUS
            {
                GamePrintChat("Stacks under " + maxstacks + ". Going back to farm.");
                IsOVER = false;
                IsAttackStart = false;
            }
        }

        
        #endregion
        if(recall && Player.InShop()) //리콜은 ㄴㄴ
        recall = false;
        #region 공격 모드 - offensive mode
        if (IsOVER)
        {
            var ehero = ObjectManager.Get<Obj_AI_Hero>().OrderBy(t => t.Distance(Player.Position)).First(t => t.IsEnemy & !t.IsDead);
//                var eheros = GetEnemyList().Where(x => x.IsValid && x.IsEnemy && !x.IsDead && Player.Distance(x.Position) <= 2000);                    
//                Obj_AI_Hero ehro = eheros.FirstOrDefault();   Obj_HQ
            var turrett = ObjectManager.Get<Obj_AI_Turret>().OrderBy(t => t.Distance(Player.Position)).First(t => t.IsEnemy);
            var ANEXUS = ObjectManager.Get<Obj_HQ>().Where(t => !t.IsEnemy); // 넥서승
            var emini = ObjectManager.Get<Obj_AI_Minion>().OrderBy(t => t.Distance(Player.Position)).First(t => t.IsEnemy);
            var s_ehro = RLProjectAutoJungleMenu.Item("ehhro").GetValue<Slider>().Value;
            var s_ehro2 = RLProjectAutoJungleMenu.Item("ehhro2").GetValue<Slider>().Value;
            var aturret = ObjectManager.Get<Obj_AI_Turret>().OrderBy(t => t.Distance(Player.Position)).First(t => !t.IsEnemy);
            int faceat = GetAllyTurretList().Where(x => x.Distance(ehero.Position) <= TRRange).Count();
            int face_ehro2 = GetEnemyList().Where(x => x.Distance(Player.Position) <= 2100 && getHealthPercent(x) > 35).Count();
            int face_ehro2LH = GetEnemyList().Where(x => x.Distance(Player.Position) <= 2100 && getHealthPercent(x) < 50).Count();
            int face_ehro = GetEnemyList().Where(x => x.Distance(Player.Position) <= 900 && getHealthPercent(x) > 35).Count();                
            int face_ally = GetAllyList().Where(x => (x.Distance(Player.Position) <= 900 || x.Distance(ehero.Position) <= 800) && !x.IsMe).Count() + faceat;
            int face_allye = GetAllyList().Where(x => x.Distance(ehero.Position) <= 800 && !x.IsMe).Count();
            int tminic = GetTMinionList().Where(x => x.Distance(turrett.Position) <= 900).Count();
            int CM = GetTMinionList().Where(x => x.Distance(Player.Position) <= 350).Count();
            int turretcount = GetEnemyTurretList().Where(x => x.Distance(Player.Position) <= 20000).Count();                
            //if(Player.InShop() && Player.HealthPercent < NonRCHP) //자동귀환취소.
            //    Player.IssueOrder(GameObjectOrder.MoveTo, Player.ServerPosition);
            if (!IsAttackStart)
            {
                /*if (!ObjectManager.Get<Obj_AI_Turret>().Any(t => t.Name == "Turret_T2_C_05_A") && IsBlueTeam || !ObjectManager.Get<Obj_AI_Turret>().Any(t => t.Name == "Turret_T2_C_05_A") && IsBlueTeam)
                    IsAttackStart = true;
                else if (!ObjectManager.Get<Obj_AI_Turret>().Any(t => t.Name == "Turret_T1_C_05_A") && !IsBlueTeam || !ObjectManager.Get<Obj_AI_Turret>().Any(t => t.Name == "Turret_T1_C_05_A") && !IsBlueTeam)
                    IsAttackStart = true;
                else
                {*/
                if(!Player.InShop())
                {
//                    if (!ObjectManager.Get<Obj_AI_Turret>().Any(t => t.Name == "Turret_T2_C_05_A") && IsBlueTeam || !ObjectManager.Get<Obj_AI_Turret>().Any(t => t.Name == "Turret_T1_C_05_A") && !IsBlueTeam)
//                    {
                        if(Player.Distance(emini.Position) < 650)
                        {
                            Player.IssueOrder(GameObjectOrder.AttackUnit, GetNearest(Player.Position));
                            afktime = 0;
                        }
                        else
                        {
                            afktime = 0;
                            if(false == recall)
                            {
                                Player.Spellbook.CastSpell(SpellSlot.Recall);
                                recall = true;
                                //IsAttackStart = true;
                            }
                        }
                        if(Player.Distance(aturret.Position) <= TRRange + 100)
                        IsAttackStart = true;
                        
                        if(recall && !Player.IsRecalling())//리콜
                        recall = false;
//                    }
/*                    else
                    {
                        if (IsBlueTeam)
                        {
                            Player.IssueOrder(GameObjectOrder.MoveTo, BLUE_MID.Position);
                                if (Player.ChampionName == "Nidalee")
                                {
                                    if(!_cougarForm && Aspectofcougar.IsReady())
                                    {
                                    Aspectofcougar.Cast();
                                    }
                                    if(Pounce.IsReady())
                                    {
                                    Pounce.Cast(BLUE_MID.Position);
                                    }
                                }
                            if (Player.Distance(BLUE_MID.Position) <= 400)
                                IsAttackStart = true;
                        }
                        else
                        {
                            Player.IssueOrder(GameObjectOrder.MoveTo, PURPLE_MID.Position);
                                if (Player.ChampionName == "Nidalee")
                                {
                                    if(!_cougarForm && Aspectofcougar.IsReady())
                                    {
                                    Aspectofcougar.Cast();
                                    }
                                    if(Pounce.IsReady())
                                    {
                                    Pounce.Cast(PURPLE_MID.Position);
                                    }
                                }

                            if (Player.Distance(PURPLE_MID.Position) <= 400)
                                IsAttackStart = true;
                        }
                    }*/
                }
                else
                {
                    IsAttackStart = true;
                    afktime = 0;
                    if(recall)
                    recall = false;
                }
                //}
            }
            else
            {
                var turret = ObjectManager.Get<Obj_AI_Turret>().OrderBy(t => t.Distance(Player.Position)).First(t => t.IsEnemy);
                var amini = ObjectManager.Get<Obj_AI_Minion>().OrderBy(t => t.Distance(turret.Position)).First(t => t.IsAlly);

                //                var am = ObjectManager.Get<Obj_AI_Base>().Where(t => t.Distance(Player.Position)).First(t => t.IsEnemy);
                if(false == recall && !Player.InShop() && Player.Gold > 2500 && face_ehro2 == 0 && turret.Distance(Player.Position) > TRRange + 100 && emini.Distance(Player.Position) > 1000)//템사러집가자
                {
                    GamePrintChat("Time To Recall Yeah!");
                    Player.Spellbook.CastSpell(SpellSlot.Recall);
                    recall = true;
                    afktime = 0;
                }
                
                if(recall && !Player.IsRecalling())//리콜
                recall = false;
                
                if (IsOVER && !IsAttackedByTurret && getHealthPercent(Player) >= 35)
                {
                    if(Player.InShop() && getHealthPercent(Player) >= NonRCHP || !Player.InShop())
                    {
                        if ((turret.Distance(Player.Position) > TRRange + 100 && face_ehro <= s_ehro && face_ehro2 <= s_ehro2 && getHealthPercent(Player) >= 35 || turret.Distance(Player.Position) > TRRange + 100 && face_ehro2 - face_ally <= s_ehro2 && (face_ehro2LH > 1 || face_allye > 0) && getHealthPercent(Player) >= 35) 
                        && (CM > 1 || ehero.Distance(turret.Position) > TRRange && ehero.Distance(Player.Position) <= TRRange * 3 / 2 || face_ally > 0 || tminic > 1))
                        {
                            //if(turretcount <= 1)
                            //{
                            
                            Player.IssueOrder(GameObjectOrder.AttackTo, enemy_spawn);
                            if (Player.ChampionName == "Nidalee")
                            {
                                if(face_ehro2 < 1 && turret.Distance(Player.Position) > TRRange + 150)
                                {
                                    if(!_cougarForm && Aspectofcougar.IsReady())
                                    {
                                    Aspectofcougar.Cast();
                                    }
                                    if(Pounce.IsReady())
                                    {
                                    Pounce.Cast(enemy_spawn);
                                    }
                                }
                            }
                        
                        //}
                        /*else
                        {
                        Player.IssueOrder(GameObjectOrder.AttackTo, turret.Position.Extend(Player.Position, 10));
                            if (Player.ChampionName == "Nidalee")
                            {
                                if(face_ehro2 < 1 && turret.Distance(Player.Position) > TRRange + 150)
                                {
                                    if(!_cougarForm && Aspectofcougar.IsReady())
                                    {
                                    Aspectofcougar.Cast();
                                    }
                                    if(Pounce.IsReady())
                                    {
                                    Pounce.Cast(turret.Position);
                                    }
                                }
                            }
                        }*/
                            if(ehero.Distance(turret.Position) > TRRange)
                            {
                                DoCast_Hero();
                                DoLaneClear();
                            }
                            
                        }
                        
                        else if (tminic > 1 && turret.Distance(Player.Position) <= TRRange && turret.Distance(amini.Position) <= TRRange + 50 && !IsAttackedByTurret && face_ehro == 0 && face_ehro2 <= 1 && getHealthPercent(Player) >= 35 || tminic > 1 && turret.Distance(Player.Position) <= TRRange && turret.Distance(amini.Position) <= TRRange && !IsAttackedByTurret && face_ehro == 0 && face_ehro2 - face_ally <= s_ehro2 && getHealthPercent(Player) >= 35)
                        {
                            //if(turretcount <= 1)
                            //{
                            Player.IssueOrder(GameObjectOrder.AttackTo, enemy_spawn);
                            if(tminic > 2 && turret.Distance(Player.Position) <= TRRange)
                            {
                                DoLaneClear();
                            }
                        }
                        
                        else
                        {
                            Player.IssueOrder(GameObjectOrder.MoveTo, Player.Position.Extend(spawn, 855));
                                if (Player.ChampionName == "Nidalee")
                                {
                                    if(!_cougarForm && Aspectofcougar.IsReady())
                                    {
                                    Aspectofcougar.Cast();
                                    }
                                    if(Pounce.IsReady())
                                    {
                                    Pounce.Cast(spawn);
                                    }
                                }
                        }
                        
                    afktime = 0;
                    }
                }
                
                if (!(CM > 1 || ehero.Distance(turret.Position) > TRRange && ehero.Distance(Player.Position) <= TRRange * 3 / 2 || face_ally > 0 || tminic > 0))
                    Player.IssueOrder(GameObjectOrder.MoveTo, Player.Position.Extend(spawn, 855));
                
                if (tminic < 1 && turret.Distance(Player.Position) <= TRRange + 250 && turret.Distance(Player.Position) > TRRange - 200)
                    Player.IssueOrder(GameObjectOrder.MoveTo, Player.Position.Extend(spawn, 855));
                    
                if (Player.Distance(enemy_spawn) <= TRRange + 600)
                    Player.IssueOrder(GameObjectOrder.MoveTo, Player.Position.Extend(spawn, 855));

                if (turret.Distance(Player.Position) > TRRange + 200)
                    IsAttackedByTurret = false;
                if (Player.IsDead)
                    IsAttackedByTurret = false; //터렛앞에서 깝죽 ㄴㄴ
                if(turret.Distance(ehero.Position) <= TRRange - 50 && Player.Distance(ehero.Position) <= ehero.AttackRange / 2 + 700 && ehero.AttackRange > 50)
                Player.IssueOrder(GameObjectOrder.MoveTo, Player.Position.Extend(spawn, 855));
                afktime = 0;
                

            }
//도망가기용
            if ((getHealthPercent(Player) < 33 && !Player.IsDead && ehero.Distance(Player.Position) <= 1400//hpper
            && RLProjectAutoJungleMenu.Item("autorecallheal").GetValue<Boolean>() ||
            getHealthPercent(Player) < 33 && !Player.IsDead && emini.Distance(Player.Position) <= 900//hpper
            && RLProjectAutoJungleMenu.Item("autorecallheal").GetValue<Boolean>() ||
            turrett.Distance(Player.Position) <= TRRange && getHealthPercent(Player) < 33
            && RLProjectAutoJungleMenu.Item("autorecallheal").GetValue<Boolean>()) && false == recall) // HP LESS THAN 25%  //도망가!!!!!
            {
                GamePrintChat("YOUR HP IS SO LOW. Back to RECALL!");
                Player.IssueOrder(GameObjectOrder.MoveTo, spawn);
                if (Player.Distance(ehero.Position) <= 700 &&
                    (Items.HasItem((int)(ItemId.Stalkers_Blade)) ||
                    Items.HasItem((int)(ItemId.Stalkers_Blade_Enchantment_Magus)) ||
                    Items.HasItem((int)(ItemId.Stalkers_Blade_Enchantment_Devourer))))
                {
                    smite.CastOnUnit(ehero);
                }
                if (Player.ChampionName == "Akali")
                {
                    if(W.IsReady())
                    W.Cast(Player.Position);
                }
                else if (Player.ChampionName == "MasterYi")
                {
                    if(R.IsReady())
                    R.Cast();
                }
                else if (Player.ChampionName == "Nidalee")
                {
                    if(!_cougarForm && Aspectofcougar.IsReady())
                    {
                    Aspectofcougar.Cast();
                    }
                    if(Pounce.IsReady())
                    {
                    Pounce.Cast(spawn);
                    }
                }
                else if (Player.ChampionName == "Jinx")
                {
                    var epred = E.GetPrediction(ehero, true);
                    var wpred = W.GetPrediction(ehero, true);
                    if (E.IsReady() && epred.Hitchance >= HitChance.High)
                    E.Cast(epred.CastPosition);
                    if (W.IsReady() && wpred.Hitchance >= HitChance.High && ehero.Distance(Player.Position) > 900)
                    W.Cast(wpred.CastPosition);
                }
                afktime = 0;
            }
            if (false == recall && !Player.InShop() && getHealthPercent(Player) < 35 && !Player.IsDead && ehero.Distance(Player.Position) > 2500//hpper
            && turrett.Distance(Player.Position) > 2250 && emini.Distance(Player.Position) > 1500
            && RLProjectAutoJungleMenu.Item("autorecallheal").GetValue<Boolean>()) // HP LESS THAN 25%
            {
                GamePrintChat("Time To Recall Yeah!");
                Player.Spellbook.CastSpell(SpellSlot.Recall);
                recall = true;
                recallhp = Player.Health;
                afktime = 0;
            }
        }
        else
        {
        var turret = ObjectManager.Get<Obj_AI_Turret>().OrderBy(t => t.Distance(Player.Position)).First(t => t.IsEnemy);
            if(!IsOVER && turret.Distance(Player.Position) < TRRange)
            {
                Player.IssueOrder(GameObjectOrder.MoveTo, Player.Position.Extend(spawn, 855));
                afktime = 0;
                    if (Player.ChampionName == "Nidalee")
                    {
                        if(!_cougarForm && Aspectofcougar.IsReady())
                        {
                        Aspectofcougar.Cast();
                        }
                        if(Pounce.IsReady())
                        {
                        Pounce.Cast(spawn);
                        }
                    }
            }
        }
        
        #endregion
        #region 상점이용가능할때 // when you are in shop range or dead
        #region 시작아이템 사기 // startup
        if (Player.InShop() || Player.IsDead)
        {
            if (!(Items.HasItem((int)ItemId.Hunters_Machete) ||
            Items.HasItem((int)ItemId.Rangers_Trailblazer) ||
            Items.HasItem((int)(ItemId.Rangers_Trailblazer_Enchantment_Devourer)) ||
            Items.HasItem((int)(ItemId.Rangers_Trailblazer_Enchantment_Magus)) ||
            Items.HasItem((int)(ItemId.Stalkers_Blade)) ||
            Items.HasItem((int)(ItemId.Stalkers_Blade_Enchantment_Magus)) ||
            Items.HasItem((int)(ItemId.Stalkers_Blade_Enchantment_Devourer))
            ))
            {
                if (smiteSlot != SpellSlot.Unknown)
                {
                    Player.BuyItem((ItemId)1039);
                    Player.BuyItem((ItemId)3342);
                }
            }
            if(recall)
            recall = false;
        #endregion
            //GamePrintChat("Gold:" + Player.Gold);
            //GamePrintChat("NeedItem:" + buyThings.First().needItem.ToString());
            //GamePrintChat("BuyItem:" + buyThings.First().item.ToString());
            #region 아이템트리 올리기 // item build up
            if (buyThings.Any(t => t.item != ItemId.Unknown))
            {
                if (Items.HasItem((int)(buyThings.First().needItem)))
                {
                    if (Player.Gold > buyThings.First().Price)
                    {
                        Player.BuyItem((ItemId)(int)(buyThings.First().item));
                        buyThings.Remove(buyThings.First());
                    }
                }
            }
            #endregion
            #region 포션 구매 - buy potions
            if (Player.Gold > 35f && !IsOVER && !Player.InventoryItems.Any(t => t.Id == ItemId.Health_Potion) && Player.Level <= 6)
                Player.BuyItem((ItemId)2003);
            if (Player.InventoryItems.Any(t => t.Id == ItemId.Health_Potion))
            {
                if (Player.InventoryItems.First(t => t.Id == ItemId.Health_Potion).Stacks <= 2 && Player.Level <= 6)
                    Player.BuyItem((ItemId)2003);
                if (Player.Level > 6)
                    Player.SellItem(Player.InventoryItems.First(t => t.Id == ItemId.Health_Potion).Slot);
            }
            if (Player.Level > 6 && Items.HasItem(2010))
                Player.SellItem(Player.InventoryItems.First(t => Convert.ToInt32(t.Id) == 2010).Slot);
            #endregion
            #region 분노의 영약 구매.
            if (Player.Gold > 400f && IsOVER && !Player.HasBuff("ElixirOfWrath") && !Player.InventoryItems.Any(t => t.Id == ItemId.Elixir_of_Wrath) && Player.Level >= 12)
                Player.BuyItem((ItemId)ItemId.Elixir_of_Wrath);
            #endregion
            if(Player.HealthPercent < NonRCHP)//리코오오올
            Player.IssueOrder(GameObjectOrder.MoveTo, spawn);
        }
        #endregion
        #region 자동포션사용 - auto use potions
        if (getHealthPercent(Player) <= 60 && !Player.InShop())
        {
            ItemId item = ItemId.Health_Potion;
            if (Player.InventoryItems.Any(t => Convert.ToInt32(t.Id) == 2010))
                item = ItemId.Unknown;
            if (Player.InventoryItems.Any(t => (t.Id == ItemId.Health_Potion || Convert.ToInt32(t.Id) == 2010)))
            {
                if (!Player.HasBuff("ItemMiniRegenPotion") && item == ItemId.Unknown)
                    Player.Spellbook.CastSpell(Player.InventoryItems.First(t => Convert.ToInt32(t.Id) == 2010).SpellSlot);
                if (!Player.HasBuff("Health Potion") && item == ItemId.Health_Potion)
                    Player.Spellbook.CastSpell(Player.InventoryItems.First(t => t.Id == ItemId.Health_Potion).SpellSlot);
            }
        }
        #endregion
        AutoLevel.Enabled(true);
    }
    public static void GamePrintChat(string message)//신
    { //아 미친랙때문에 지웁니다.
        Console.WriteLine(message);
        //LSConsole.WriteLine(message);
    }
    private static void OnCreate(GameObject sender, EventArgs args)
    {
        if (sender.IsValid<Obj_SpellMissile>())
        {
            var m = (Obj_SpellMissile)sender;
            if (m.SpellCaster.IsValid<Obj_AI_Turret>() && m.SpellCaster.IsEnemy &&
            m.Target.IsValid<Obj_AI_Hero>() && m.Target.IsMe && RLProjectAutoJungleMenu.Item("evading").GetValue<Boolean>())
            {
                Player.IssueOrder(GameObjectOrder.MoveTo, spawn);
                            if (Player.ChampionName == "Nidalee")
                            {
                                if(!_cougarForm && Aspectofcougar.IsReady())
                                {
                                Aspectofcougar.Cast();
                                }
                                if(Pounce.IsReady())
                                {
                                Pounce.Cast(spawn);
                                }
                            }
                GamePrintChat("OOPS YOU ARE ATTACKED BY TURRET!");
                Player.IssueOrder(GameObjectOrder.MoveTo, Player.Position.Extend(spawn, 855));
                IsAttackedByTurret = true;
            }
        }
    }
    private static void OnSpell(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs spell)
    {
        if (spell.Target.IsValid<Obj_AI_Hero>())
        {
            if (spell.Target.IsMe && sender.IsEnemy)
            {
                string[] turrest =
                    {
                    "Turret_T2_C_01_A",
                    "Turret_T2_C_02_A",
                    "Turret_T2_L_01_A",
                    "Turret_T2_C_03_A",
                    "Turret_T2_R_01_A",
                    "Turret_T1_C_01_A",
                    "Turret_T1_C_02_A",
                    "Turret_T1_C_06_A",
                    "Turret_T1_C_03_A",
                    "Turret_T1_C_07_A"
                    };
                if (turrest.Contains(sender.Name) && RLProjectAutoJungleMenu.Item("evading").GetValue<Boolean>())
                {
                    Player.IssueOrder(GameObjectOrder.MoveTo, spawn);
                            if (Player.ChampionName == "Nidalee")
                            {
                                if(!_cougarForm && Aspectofcougar.IsReady())
                                {
                                Aspectofcougar.Cast();
                                }
                                if(Pounce.IsReady())
                                {
                                Pounce.Cast(spawn);
                                }
                            }
                    GamePrintChat("OOPS YOU ARE ATTACKED BY INHIBIT TURRET!");
                    Player.IssueOrder(GameObjectOrder.MoveTo, Player.Position.Extend(spawn, 855));
                    IsAttackedByTurret = true;
                }
            }
        }
    }
    #region getminions around turret
    public static int GetMinions(Obj_AI_Turret Turret)
    {
        int i = 0;
        foreach (var minion in ObjectManager.Get<Obj_AI_Minion>().Where(t => t.Name.Contains("Minion") && t.Distance(Turret.Position) <= 855 && !t.IsEnemy))
        {
            i++;
        }
        return i;
    }
    #endregion
    #region spell methods
    public static void DoSmite()//스마이트
    {
        var mob1 = GetNearest_big(Player.Position);
        int badally = GetAllyList().Where(x => x.Distance(Player.Position) <= 500 && !x.IsMe).Count(); //나쁜아군
        double smdmg = setSmiteDamage();
        if (!IsStart && Player.Level > 1 && mob1.Health < smdmg && (mob1.Health > (float)Player.GetAutoAttackDamage2(mob1, true)*3 || badally > 0))
        {
        if (mob1.IsValid)
            smite.CastOnUnit(mob1);
        }
        else if(Player.Level == 1)
        {
        if (mob1.IsValid)
            smite.CastOnUnit(mob1);
        }
    }
    public static void DoLaneClear()
    {
        var mob1 = ObjectManager.Get<Obj_AI_Minion>().OrderBy(t => Player.Distance(t.Position)).First(t => t.IsEnemy & !t.IsDead);
        if (Player.ChampionName.ToUpper() == "NUNU" && Q.IsReady()) // 누누 Q버그수정 - Fix nunu Q bug
            Player.IssueOrder(GameObjectOrder.MoveTo, mob1.ServerPosition.Extend(Player.ServerPosition, 10));
        if (!ObjectManager.Get<Obj_AI_Hero>().Any(t => t.IsEnemy & !t.IsDead && Player.Distance(t.Position) <= 1500) && ObjectManager.Get<Obj_AI_Minion>().Any(t => t.IsMinion && Player.Distance(t.Position) <= 500) && ObjectManager.Get<Obj_AI_Turret>().Any(t => t.IsEnemy && Player.Distance(t.Position) > TRRange) && getManaPercent(Player) > 75)
            castspell_laneclear(mob1);
    }
    public static void DoCast()
    {
        var mob1 = ObjectManager.Get<Obj_AI_Minion>().OrderBy(t => Player.Distance(t.Position)).First(t => t.IsEnemy & !t.IsDead);
        if (Player.ChampionName.ToUpper() == "NUNU" && Q.IsReady()) // 누누 Q버그수정 - Fix nunu Q bug
            Player.IssueOrder(GameObjectOrder.MoveTo, mob1.ServerPosition.Extend(Player.ServerPosition, 10));
        if (!ObjectManager.Get<Obj_AI_Hero>().Any(t => t.IsEnemy & !t.IsDead && !t.IsInvulnerable && Player.Distance(t.Position) <= 1000) && ObjectManager.Get<Obj_AI_Minion>().Any(t => !t.IsMinion && Player.Distance(t.Position) <= 500) && ObjectManager.Get<Obj_AI_Turret>().Any(t => t.IsEnemy && Player.Distance(t.Position) > TRRange))
            castspell(mob1);
    }
    public static void DoCast_Hero()
    {
        if (IsOVER && ObjectManager.Get<Obj_AI_Hero>().Any(t => t.IsEnemy && !t.IsDead && !t.IsInvulnerable && Player.Distance(t.Position) <= 1000) && Target != null)
        {
            var tarrr = ObjectManager.Get<Obj_AI_Hero>().OrderBy(t => t.Distance(Player.Position)).
            Where(x => x.IsEnemy && !x.IsMe && !x.IsDead && !x.IsInvulnerable && x.Distance(Player.ServerPosition) <= 2000).First(); // 플레이어와 가장 가까운타겟
            var turrr = ObjectManager.Get<Obj_AI_Turret>().OrderBy(t => t.Distance(tarrr.Position)).
            Where(x => x.IsEnemy && !x.IsDead).First(); // 타겟과 가장 가까운터렛
            var tp = turrr != null ? turrr.ServerPosition : enemy_spawn;
            if(tarrr == null)
            return;
            
            if (Player.AttackRange < 200 && !IsAttackedByTurret && tp.Distance(Player.Position) > TRRange && Target.Distance(tp) > TRRange) // 터렛 사정거리 밖에있어야만 공격함.
            {
                castspell_hero(Target);
                //if(Environment.TickCount - kiTime < 70)
                Player.IssueOrder(GameObjectOrder.MoveTo, Target.ServerPosition.Extend(Player.ServerPosition, -50));
                //Player.IssueOrder(GameObjectOrder.AttackUnit, Target); 오브워커로 대체
            }
            else if (Player.AttackRange >= 200 && tp.Distance(Player.Position) > TRRange && !IsAttackedByTurret)
            {
                castspell_hero(Target);
                //if(Environment.TickCount - kiTime < 70)
                Player.IssueOrder(GameObjectOrder.MoveTo, Target.ServerPosition.Extend(Player.ServerPosition, (Player.AttackRange + 450) / 2 - 100));            
                //Player.IssueOrder(GameObjectOrder.AttackUnit, Target); 오브워커로 대체
            }
/*                else if (tp.Distance(tarrr.Position) <= 850)
            {
                Player.IssueOrder(GameObjectOrder.MoveTo, Player.Position.Extend(spawn, 855));
                if (Player.ChampionName == "Nidalee")
                {
                    if(!_cougarForm && Aspectofcougar.IsReady())
                    {
                    Aspectofcougar.Cast();
                    }
                    if(Pounce.IsReady())
                    {
                    Pounce.Cast(spawn);
                    }
                }
            }
*/                
            else
            {
                IsAttackStart = true;
                IsOVER = true;
            }
        }
        if(Target == null)
        Player.IssueOrder(GameObjectOrder.AttackTo, enemy_spawn);

    }
    public static void castspell(Obj_AI_Base mob1)
    {
            int eminic = GetEMinionList().Where(x => x.Distance(Player.Position) <= 900).Count();
            if (Player.ChampionName.ToUpper() == "NUNU")
        {
            if (Q.IsReady())
                Q.CastOnUnit(mob1);
            if (E.IsReady())
                E.CastOnUnit(mob1);
            if (W.IsReady())
                W.Cast();
        }
        else if (Player.ChampionName.ToUpper() == "AKALI")
        {
            if (Q.IsReady() && Player.Position.Distance(mob1.Position) <= 600)
                Q.CastOnUnit(mob1);
            if (E.IsReady() && Player.Position.Distance(mob1.Position) <= 325)
                E.CastOnUnit(mob1);
            if (R.IsReady() && Player.Position.Distance(mob1.Position) <= 700 && Player.Position.Distance(mob1.Position) > 0)
                R.CastOnUnit(mob1);
        }
        else if (Player.ChampionName.ToUpper() == "NIDALEE" && Player.Position.Distance(mob1.Position) <= 600)
        {
            if (Javelin.IsReady())
                Javelin.Cast(mob1.Position);
            if (Bushwack.IsReady())
                Bushwack.Cast(mob1.Position);
            if (Takedown.IsReady())
                Takedown.CastOnUnit(mob1);
            if (Pounce.IsReady())
                Pounce.Cast(mob1.Position);
            if (Swipe.IsReady())
                Swipe.Cast(mob1.Position);
            if (R.IsReady())
                R.Cast();
        }
        else if (Player.ChampionName.ToUpper() == "CHOGATH")
        {
            if (Q.IsReady())
                Q.Cast(mob1.Position);
            if (W.IsReady())
                W.Cast(mob1.Position);
            if (R.IsReady() && R.GetDamage(mob1) >= mob1.Health)
                R.CastOnUnit(mob1);
        }
        else if (Player.ChampionName.ToUpper() == "WARWICK")
        {
            if (Q.IsReady())
                Q.CastOnUnit(mob1);
            if (W.IsReady())
                W.Cast();
            if (R.IsReady())
                R.CastOnUnit(mob1);
        }
        else if (Player.ChampionName.ToUpper() == "MASTERYI")
        {
            if (Q.IsReady())
                Q.CastOnUnit(mob1);
            if (W.IsReady() && getHealthPercent(Player) < RLProjectAutoJungleMenu.Item("yi_W").GetValue<Slider>().Value)
                W.Cast();
            if (E.IsReady())
                E.Cast();
            if (R.IsReady())
                R.Cast();
        }
        else if (Player.ChampionName.ToUpper() == "MAOKAI")
        {
            if (Q.IsReady())
                Q.Cast(mob1.Position);
            if (E.IsReady())
                E.Cast(mob1.Position);
            if (W.IsReady())
                W.CastOnUnit(mob1);
        }
        else if (Player.ChampionName.ToUpper() == "NASUS")
        {
            if (Q.IsReady() && CheckNasusQDamage(mob1))
                Q.Cast();
            if (W.IsReady() && mob1.IsValid<Obj_AI_Hero>())
                W.CastOnUnit(mob1);
            if (E.IsReady())
                E.Cast(mob1.Position);
        }
        else if (Player.ChampionName.ToUpper() == "JINX")
        {
            if (Q.IsReady() && Player.Level >= 3 && ((Player.AttackRange > 550 && eminic <= 2|| eminic > 2 && Player.AttackRange <= 550)))
                Q.Cast();
        }
        else if (Player.ChampionName.ToUpper() == "VAYNE")
        {
            if (Q.IsReady())
                Q.Cast(Player.ServerPosition.Extend(mob1.Position, 50)); // For Vayne
            if (E.IsReady())
                E.Cast(mob1);
        }
        else
        {
            foreach (var spell in cast2mob)
            {
                if (spell.IsReady())
                    spell.CastOnUnit(mob1);
                if (spell.IsReady())
                    spell.Cast();
                if (spell.IsReady())
                    spell.Cast(mob1.Position);
            }
        }
    }
    public static void castspell_hero(Obj_AI_Base mob1)
    {
        if (Player.ChampionName.ToUpper() == "NUNU")
        {
            if (Q.IsReady())
                Q.CastOnUnit(mob1);
            if (E.IsReady())
                E.CastOnUnit(mob1);
            if (W.IsReady())
                W.Cast();
        }
        else if (Player.ChampionName.ToUpper() == "AKALI")
        {
            if (Q.IsReady() && Player.Position.Distance(mob1.Position) <= 600)
                Q.CastOnUnit(mob1);
            if (E.IsReady() && Player.Position.Distance(mob1.Position) <= 325)
                E.CastOnUnit(mob1);
            if (R.IsReady() && Player.Position.Distance(mob1.Position) <= 700 && Player.Position.Distance(mob1.Position) > 1)
                R.CastOnUnit(mob1);
        }
        else if (Player.ChampionName.ToUpper() == "NIDALEE" && Player.Position.Distance(mob1.Position) <= 1000)
        {
            if (Javelin.IsReady())
                Javelin.Cast(mob1.Position);
            if (Bushwack.IsReady())
                Bushwack.Cast(mob1.Position);
            if (Takedown.IsReady())
                Takedown.CastOnUnit(mob1);
            if (Pounce.IsReady())
                Pounce.Cast(mob1.Position);
            if (Swipe.IsReady())
                Swipe.Cast(mob1.Position);
            if (R.IsReady())
                R.Cast();
        }
        else if (Player.ChampionName.ToUpper() == "CHOGATH")
        {
            if (Q.IsReady())
                Q.Cast(mob1.Position);
            if (W.IsReady())
                W.Cast(mob1.Position);
            if (R.IsReady() && R.GetDamage(mob1) >= mob1.Health)
                R.CastOnUnit(mob1);
        }
        else if (Player.ChampionName.ToUpper() == "WARWICK")
        {
            if (Q.IsReady())
                Q.CastOnUnit(mob1);
            if (W.IsReady())
                W.Cast();
            if (R.IsReady())
                R.CastOnUnit(mob1);
        }
        else if (Player.ChampionName.ToUpper() == "MASTERYI")
        {
            if (Q.IsReady() && (Player.Distance(mob1.ServerPosition) >= 180 || Player.HealthPercent < 50))
                Q.CastOnUnit(mob1);
//                if (W.IsReady() && getHealthPercent(Player) < 35
//                    W.Cast();
            if (E.IsReady())
                E.Cast();
            if (R.IsReady())
                R.Cast();
        }
        else if (Player.ChampionName.ToUpper() == "MAOKAI")
        {
            if (Q.IsReady())
                Q.Cast(mob1.Position);
            if (E.IsReady())
                E.Cast(mob1.Position);
            if (W.IsReady())
                W.CastOnUnit(mob1);
        }
        else if (Player.ChampionName.ToUpper() == "NASUS")
        {
            if (Q.IsReady())
                Q.Cast();
            if (W.IsReady() && mob1.IsValid<Obj_AI_Hero>())
                W.CastOnUnit(mob1);
            if (E.IsReady())
                E.Cast(mob1.Position);
            if (R.IsReady())
                R.Cast();
        }
        else if (Player.ChampionName.ToUpper() == "JINX")
        {
        var rpred = R.GetPrediction(mob1, true);
        var epred = E.GetPrediction(mob1, true);
        var wpred = W.GetPrediction(mob1, true);
            if (Q.IsReady() && (mob1.Distance(Player.Position) > Player.AttackRange - 50 && mob1.Distance(Player.Position) <= Player.AttackRange + 200 && Player.AttackRange <= 550) || Player.AttackRange > 550 && mob1.Distance(Player.Position) <= 500)
                Q.Cast();
            if (E.IsReady() && epred.Hitchance >= HitChance.High)
        E.Cast(epred.CastPosition);
            if (W.IsReady() && wpred.Hitchance >= HitChance.High && mob1.Distance(Player.Position) > 900)
        W.Cast(wpred.CastPosition);
            if (R.IsReady() && Player.Distance(mob1.Position) > 800 && rpred.Hitchance >= HitChance.High && getHealthPercent(mob1) < 20/*R.GetDamage(mob1) > mob1.Health*/)
            R.Cast(rpred.CastPosition);
        }
        else if (Player.ChampionName.ToUpper() == "VAYNE")
        {
            if (Q.IsReady())
                Q.Cast(Player.ServerPosition.Extend(mob1.Position, 40)); // For Vayne
            if (E.IsReady())
                E.Cast(mob1);
            if (R.IsReady() && Player.HealthPercent > 40 && mob1.HealthPercent > 40)
                R.Cast();
        }
        else
        {
            foreach (var spell in cast2hero)
            {
                if (spell.IsReady())
                    spell.CastOnUnit(mob1);
                if (spell.IsReady())
                    spell.Cast();
                if (spell.IsReady())
                    spell.Cast(mob1.Position);
            }
        }
    }
    public static void castspell_laneclear(Obj_AI_Base mob1)
    {
        int eminic = GetEMinionList().Where(x => x.Distance(Player.Position) <= 900).Count();
        if (Player.ChampionName.ToUpper() == "NUNU")
        {
            if (Q.IsReady())
                Q.CastOnUnit(mob1);
            if (E.IsReady())
                E.CastOnUnit(mob1);
            if (W.IsReady())
                W.Cast();
        }
        else if (Player.ChampionName.ToUpper() == "AKALI")
        {
            if (Q.IsReady() && Player.Position.Distance(mob1.Position) <= 600)
                Q.CastOnUnit(mob1);
            if (E.IsReady() && Player.Position.Distance(mob1.Position) <= 325)
                E.CastOnUnit(mob1);
            if (R.IsReady() && Player.Position.Distance(mob1.Position) <= 700 && Player.Position.Distance(mob1.Position) > 0 && getHealthPercent(Player) < 40)
                R.CastOnUnit(mob1);
        }
        else if (Player.ChampionName.ToUpper() == "NIDALEE" && Player.Position.Distance(mob1.Position) <= 400)
        {
            if (Takedown.IsReady())
                Takedown.CastOnUnit(mob1);
    //        if (Pounce.IsReady())
    //            Pounce.Cast(mob1.Position);
            if (Swipe.IsReady())
                Swipe.Cast(mob1.Position);
            if (R.IsReady())
                R.Cast();
        }
        else if (Player.ChampionName.ToUpper() == "CHOGATH")
        {
            if (Q.IsReady())
                Q.Cast(mob1.Position);
            if (W.IsReady())
                W.Cast(mob1.Position);
            if (R.IsReady() && R.GetDamage(mob1) >= mob1.Health)
                R.CastOnUnit(mob1);
        }
        else if (Player.ChampionName.ToUpper() == "WARWICK")
        {
            if (W.IsReady())
                W.Cast();
        }
        else if (Player.ChampionName.ToUpper() == "MASTERYI")
        {
            if (Q.IsReady())
                Q.CastOnUnit(mob1);
//                if (E.IsReady())
//                    E.Cast();
        }
        else if (Player.ChampionName.ToUpper() == "MAOKAI")
        {
            if (Q.IsReady())
                Q.Cast(mob1.Position);
            if (E.IsReady())
                E.Cast(mob1.Position);
        }
        else if (Player.ChampionName.ToUpper() == "NASUS")
        {
            if (Q.IsReady() && CheckNasusQDamage(mob1))
                Q.Cast();
            if (W.IsReady() && mob1.IsValid<Obj_AI_Hero>())
                W.CastOnUnit(mob1);
            if (E.IsReady())
                E.Cast(mob1.Position);
        }
        else if (Player.ChampionName.ToUpper() == "JINX")
        {
            if (Q.IsReady() && ((Player.AttackRange > 550  && eminic <= 5|| eminic > 5 && Player.AttackRange <= 550)))
                Q.Cast();
        }
        else if (Player.ChampionName.ToUpper() == "VAYNE")
        {
            if (Q.IsReady())
                Q.Cast(Player.ServerPosition.Extend(mob1.Position, 50)); // For Vayne
        }
        else
        {
            foreach (var spell in cast4laneclear)
            {
                if (spell.IsReady())
                    spell.CastOnUnit(mob1);
                if (spell.IsReady())
                    spell.Cast();
                if (spell.IsReady())
                    spell.Cast(mob1.Position);
            }
        }
    }
    public static bool CheckNasusQDamage(Obj_AI_Base target)
    {
        float QDmg = Convert.ToSingle(Q.GetDamage(target) + Player.CalcDamage(target, Damage.DamageType.Physical, Player.BaseAttackDamage + Player.FlatPhysicalDamageMod));
        if (QDmg >= target.Health)
            return true;
        else
            return false;
    }
    public static float GetSpellRange(SpellDataInst targetSpell, bool IsChargedSkill = false)
    {
        if (targetSpell.SData.CastRangeDisplayOverride <= 0)
        {
            if (targetSpell.SData.CastRange <= 0)
            {
                return
                Player.AttackRange;//targetSpell.SData.CastRadius;
            }
            else
            {
                if (!IsChargedSkill)
                    return
                    targetSpell.SData.CastRange;
                else
                    return
                    targetSpell.SData.CastRadius;
            }
        }
        else
            return
            targetSpell.SData.CastRangeDisplayOverride;
    }
    #endregion spell methods
    #region 스마이트함수 - Smite Function
    public static readonly int[] SmitePurple = { 3713, 3726, 3725, 3726, 3723 };
    public static readonly int[] SmiteGrey = { 3711, 3722, 3721, 3720, 3719 };
    public static readonly int[] SmiteRed = { 3715, 3718, 3717, 3716, 3714 };
    public static readonly int[] SmiteBlue = { 3706, 3710, 3709, 3708, 3707 };
    private static readonly string[] MinionNames =
{
"SRU_Blue", "SRU_Gromp", "SRU_Murkwolf", "SRU_Razorbeak", "SRU_Red", "SRU_Krug", "SRU_Dragon", "SRU_BaronSpawn"
};
    public static void setSmiteSlot()
    {
        foreach (var spell in Player.Spellbook.Spells.Where(spell => String.Equals(spell.Name, smitetype(), StringComparison.CurrentCultureIgnoreCase)))
        {
            smiteSlot = spell.Slot;
            smite = new Spell(smiteSlot, 550);
            return;
        }
    }
    public static string smitetype()
    {
        if (Player.InventoryItems.Any(item => SmiteBlue.Any(t => t == (int)(item.Id))))
        {
            return "s5_summonersmiteplayerganker";
        }
        if (Player.InventoryItems.Any(item => SmiteRed.Any(t => t == (int)(item.Id))))
        {
            return "s5_summonersmiteduel";
        }
        if (Player.InventoryItems.Any(item => SmiteGrey.Any(t => t == (int)(item.Id))))
        {
            return "s5_summonersmitequick";
        }
        if (Player.InventoryItems.Any(item => SmitePurple.Any(t => t == (int)(item.Id))))
        {
            return "itemsmiteaoe";
        }
        return "summonersmite";
    }
    public static double setSmiteDamage() //스마이트 데미지
    {
        int level = Player.Level;
        int[] damage =
{
20*level + 370,
30*level + 330,
40*level + 240,
50*level + 100
};
        return damage.Max();
    }
    public static Obj_AI_Base GetNearest(Vector3 pos)
    {
    if(Player.Level > 0)
        {
        var Mobs = MinionManager.GetMinions(700, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
        return Mobs[0];
        }
    else
        {
            var minions =
            ObjectManager.Get<Obj_AI_Minion>()
            .Where(minion => minion.IsValid && minion.IsEnemy && !minion.IsDead && MinionNames.Any(name => minion.Name.StartsWith(name)
            && Player.Distance(minion.Position) <= 1000));
            var objAiMinions = minions as Obj_AI_Minion[] ?? minions.ToArray();
            Obj_AI_Minion sMinion = objAiMinions.FirstOrDefault();
            double? nearest = null;
            foreach (Obj_AI_Minion minion in objAiMinions)
            {
                double distance = Vector3.Distance(pos, minion.Position);
                if (nearest == null || nearest > distance)
                {
                    nearest = distance;
                    sMinion = minion;
                }
            }
            return sMinion;
        }
    }
    public static Obj_AI_Minion GetNearest_big(Vector3 pos)
    {
        var minions =
        ObjectManager.Get<Obj_AI_Minion>()
        .Where(minion => minion.IsValid && minion.IsEnemy && !minion.IsDead && MinionNames.Any(name => minion.Name.StartsWith(name)) && !MinionNames.Any(name => minion.Name.Contains("Mini")
        && Player.Distance(minion.Position) <= 1000));
        var objAiMinions = minions as Obj_AI_Minion[] ?? minions.ToArray();
        Obj_AI_Minion sMinion = objAiMinions.FirstOrDefault();
        double? nearest = null;
        foreach (Obj_AI_Minion minion in objAiMinions)
        {
            double distance = Vector3.Distance(pos, minion.Position);
            if (nearest == null || nearest > distance)
            {
                nearest = distance;
                sMinion = minion;
            }
        }
        return sMinion;
    }
    public static bool CheckMonster(String TargetName, Vector3 BasePosition, int Range = 300)
    {
        var minions = ObjectManager.Get<Obj_AI_Minion>()
        .Where(minion => minion.IsValid && !minion.IsDead && minion.Name.StartsWith(TargetName));
        var objAiMinions = minions as Obj_AI_Minion[] ?? minions.ToArray();
        if (!objAiMinions.Any(m => m.Distance(BasePosition) < Range))
        {
            return false;
        }
        else
        {
            return true;
        }
    }
    #endregion
    /*
    #region GetItemTree
    public static void GetItemTree(FileInfo setFile)
    {
        if (Readini.GetItemTreetype(setFile.FullName) == "AP")
        {
            buyThings.Clear();
            buyThings = buyThings_AP;
            GamePrintChat("Set ItemTree for AP - Finished");
        }
        else if (Readini.GetItemTreetype(setFile.FullName) == "BAP")
        {
            buyThings.Clear();
            buyThings = buyThings_BAP;
            GamePrintChat("Set ItemTree for BAP with Blue Smite Fin.");
        }
        else if (Readini.GetItemTreetype(setFile.FullName) == "AS")
        {
            buyThings.Clear();
            buyThings = buyThings_AS;
            GamePrintChat("Set ItemTree for AS - Finished");
        }
        else if (Readini.GetItemTreetype(setFile.FullName) == "TANK")
        {
            buyThings.Clear();
            buyThings = buyThings_TANK;
            GamePrintChat("Set ItemTree for TANK - Finished");
        }
        else if (Readini.GetItemTreetype(setFile.FullName) == "HI")
        {
            buyThings.Clear();
            buyThings = buyThings_HI;
            GamePrintChat("Set ItemTree for AP+AD - Finished");
        }
        else if (Readini.GetItemTreetype(setFile.FullName) == "ADC")
        {
            buyThings.Clear();
            buyThings = buyThings_ADC;
            GamePrintChat("ADC Itemtree Loaded. Time to carry.");
        }
        else if (Readini.GetItemTreetype(setFile.FullName) == "X")
        {
            GamePrintChat("PLZ TYPE VALID VALUE, SET AD ITEMTREE - ERROR");
        }
        else
        {
            GamePrintChat("Set ItemTree for AD - Finished");
        }
    }
    #endregion
    */
        #region GetItemTree
    public static void GetItemTree(string type)
    {
        if (type == "AP")
        {
            buyThings.Clear();
            buyThings = buyThings_AP;
            GamePrintChat("Set ItemTree for AP - Finished");
        }
        else if (type == "BAP")
        {
            buyThings.Clear();
            buyThings = buyThings_BAP;
            GamePrintChat("Set ItemTree for BAP with Blue Smite Fin.");
        }
        else if (type == "AS")
        {
            buyThings.Clear();
            buyThings = buyThings_AS;
            GamePrintChat("Set ItemTree for AS - Finished");
        }
        else if (type == "TANK")
        {
            buyThings.Clear();
            buyThings = buyThings_TANK;
            GamePrintChat("Set ItemTree for TANK - Finished");
        }
        else if (type == "HI")
        {
            buyThings.Clear();
            buyThings = buyThings_HI;
            GamePrintChat("Set ItemTree for AP+AD - Finished");
        }
        else if (type == "ADC")
        {
            buyThings.Clear();
            buyThings = buyThings_ADC;
            GamePrintChat("ADC Itemtree Loaded. Time to carry.");
        }
        else if (type == "AD")
        {
            GamePrintChat("AAADDDDD");
        }
        else
        {
            GamePrintChat("Set ItemTree for AD - Finished");
        }
    }
    #endregion
}
}