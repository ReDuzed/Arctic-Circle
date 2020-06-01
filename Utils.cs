using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using TShockAPI;
using TerrariaApi.Server;
using RUDD.Dotnet;

namespace ArcticCircle
{
    public class Utils
    {
        public static List<NetItem> StarterItems = new List<NetItem>();
        public static int startHealth, startMana;

        public static string[] Parameters = new string[] { "width", "height", "damage", "crit", "knockback", "prefix", "reusedelay", "shoot", "shootspeed", "useammo", "usetime", "autoreuse", "ammo", "scale" };
        public const int Width = 0, Height = 1, Damage = 2, Crit = 3, KB = 4, Prefix = 5, ReuseDelay = 6, Shoot = 7, ShootSpeed = 8, UseAmmo = 9, UseTime = 10, AutoReuse = 11, Ammo = 12, Scale = 13;

        public static class ClassID
        {
            public const string None = "None", Ranged = "Ranged", Melee = "Melee", Mage = "Mage";
            public static string[] Array = new string[4];
        }

        public class ClassItem
        {
            public int id;
            public int stack;
            public int prefix;
        }

        public static string Class(int index)
        {
            return ClassID.Array[index].ToLower();
        }

        public static int ClassSet(string param)
        {
            if (ClassID.Array == null || ClassID.Array.Length == 0)
                return -1;
            for (int i = 0; i < ClassID.Array.Length; i++)
            {
                if (param.ToLower() == ClassID.Array[i].ToLower())
                    return i;
            }
            return -1;
        }

        public static int IndexTotal()
        {
            return NetItem.InventorySlots + NetItem.MiscDyeSlots + NetItem.MiscEquipSlots + NetItem.ArmorSlots + NetItem.DyeSlots + NetItem.TrashSlots;
        }

        public static void UpdateItem(Item item, int slot, int who, bool remove = false, int stack = 1)
        {
            if (remove)
            {
                item.active = false;
                item.type = 0;
                item.netDefaults(0);
            }
            item.stack = stack;
            NetMessage.SendData((int)PacketTypes.PlayerSlot, -1, -1, NetworkText.FromLiteral(item.Name), who, slot, item.prefix);
            NetMessage.SendData((int)PacketTypes.PlayerSlot, who, -1, NetworkText.FromLiteral(item.Name), who, slot, item.prefix);
        }

        #region Reset Players
        public static void SetDefaultStats()
        {
            if (Main.ServerSideCharacter)
            {
                StarterItems = TShock.ServerSideCharacterConfig.StartingInventory;

                if (TShock.ServerSideCharacterConfig.StartingHealth > 500)
                    startHealth = 500;
                else if (TShock.ServerSideCharacterConfig.StartingHealth < 100)
                    startHealth = 100;
                else
                    startHealth = TShock.ServerSideCharacterConfig.StartingHealth;

                if (TShock.ServerSideCharacterConfig.StartingMana > 200)
                    startMana = 200;
                else if (TShock.ServerSideCharacterConfig.StartingMana < 20)
                    startMana = 20;
                else
                    startMana = TShock.ServerSideCharacterConfig.StartingMana;
            }
        }

        // Credit to: https://tshock.co/xf/index.php?resources/character-reset-ssc.4/
        public static void ResetPlayer(TSPlayer player)
        {
            if (Main.ServerSideCharacter)
            {
                try
                {
                    ResetStats(player);
                    ResetInventory(player);
                    ResetQuests(player);
                    ResetBanks(player);
                }
                catch (Exception ex)
                {
                    TShock.Log.ConsoleError("An error occurred while resetting a player!");
                    TShock.Log.ConsoleError(ex.ToString());
                }
            }
            else
            {
                TShock.Log.ConsoleError("The ResetPlayer method was called but SSC isn't enabled on this server!");
            }
        }

        public static void ResetStats(TSPlayer player)
        {
            player.TPlayer.statLife = startHealth;
            player.TPlayer.statLifeMax = startHealth;
            player.TPlayer.statMana = startMana;
            player.TPlayer.statManaMax = startMana;

            NetMessage.SendData(4, -1, -1, NetworkText.FromLiteral(player.Name), player.Index, 0f, 0f, 0f, 0);
            NetMessage.SendData(42, -1, -1, NetworkText.Empty, player.Index, 0f, 0f, 0f, 0);
            NetMessage.SendData(16, -1, -1, NetworkText.Empty, player.Index, 0f, 0f, 0f, 0);
            NetMessage.SendData(50, -1, -1, NetworkText.Empty, player.Index, 0f, 0f, 0f, 0);

            NetMessage.SendData(4, player.Index, -1, NetworkText.FromLiteral(player.Name), player.Index, 0f, 0f, 0f, 0);
            NetMessage.SendData(42, player.Index, -1, NetworkText.Empty, player.Index, 0f, 0f, 0f, 0);
            NetMessage.SendData(16, player.Index, -1, NetworkText.Empty, player.Index, 0f, 0f, 0f, 0);
            NetMessage.SendData(50, player.Index, -1, NetworkText.Empty, player.Index, 0f, 0f, 0f, 0);
        }

        public static void ResetInventory(TSPlayer player)
        {
            ClearInventory(player);

            int slot = 0;
            Item give;
            foreach (NetItem item in StarterItems)
            {
                give = TShock.Utils.GetItemById(item.NetId);
                give.stack = item.Stack;
                give.prefix = item.PrefixId;

                if (player.InventorySlotAvailable)
                {
                    player.TPlayer.inventory[slot] = give;
                    NetMessage.SendData((int)PacketTypes.PlayerSlot, -1, -1, NetworkText.Empty, player.Index, slot);
                    slot++;
                }
            }
        }

        private static void ClearInventory(TSPlayer player) //The inventory clearing method from ClearInvSSC
        {
            for (int i = 0; i < NetItem.MaxInventory; i++)
            {
                if (i < NetItem.InventorySlots) //Main Inventory
                {
                    player.TPlayer.inventory[i].netDefaults(0);
                }
                else if (i < NetItem.InventorySlots + NetItem.ArmorSlots) //Armor&Accessory slots
                {
                    var index = i - NetItem.InventorySlots;
                    player.TPlayer.armor[index].netDefaults(0);
                }
                else if (i < NetItem.InventorySlots + NetItem.ArmorSlots + NetItem.DyeSlots) //Dye Slots
                {
                    var index = i - (NetItem.InventorySlots + NetItem.ArmorSlots);
                    player.TPlayer.dye[index].netDefaults(0);
                }
                else if (i < NetItem.InventorySlots + NetItem.ArmorSlots + NetItem.DyeSlots + NetItem.MiscEquipSlots) //Misc Equip slots
                {
                    var index = i - (NetItem.InventorySlots + NetItem.ArmorSlots + NetItem.DyeSlots);
                    player.TPlayer.miscEquips[index].netDefaults(0);
                }
                else if (i < NetItem.InventorySlots + NetItem.ArmorSlots + NetItem.DyeSlots + NetItem.MiscEquipSlots + NetItem.MiscDyeSlots)
                {
                    var index = i - (NetItem.InventorySlots + NetItem.ArmorSlots + NetItem.DyeSlots + NetItem.MiscEquipSlots);
                    player.TPlayer.miscDyes[index].netDefaults(0);
                }
                else if (i < NetItem.InventorySlots + NetItem.ArmorSlots + NetItem.DyeSlots + NetItem.MiscEquipSlots + NetItem.MiscDyeSlots + NetItem.PiggySlots) //piggy Bank
                {
                    //var index = i - (NetItem.InventorySlots + NetItem.ArmorSlots + NetItem.DyeSlots + NetItem.MiscEquipSlots + NetItem.MiscDyeSlots);
                    //player.TPlayer.bank.item[index].netDefaults(0);
                }
                else if (i < NetItem.InventorySlots + NetItem.ArmorSlots + NetItem.DyeSlots + NetItem.MiscEquipSlots + NetItem.MiscDyeSlots + NetItem.PiggySlots + NetItem.SafeSlots) //safe Bank
                {
                    //var index = i - (NetItem.InventorySlots + NetItem.ArmorSlots + NetItem.DyeSlots + NetItem.MiscEquipSlots + NetItem.MiscDyeSlots + NetItem.PiggySlots);
                    //player.TPlayer.bank2.item[index].netDefaults(0);
                }
                else if (i < NetItem.InventorySlots + NetItem.ArmorSlots + NetItem.DyeSlots + NetItem.MiscEquipSlots + NetItem.MiscDyeSlots + NetItem.PiggySlots + NetItem.SafeSlots + NetItem.ForgeSlots) //Defender's Forge
                {
                    //var index = i - (NetItem.InventorySlots + NetItem.ArmorSlots + NetItem.DyeSlots + NetItem.MiscEquipSlots + NetItem.MiscDyeSlots + NetItem.PiggySlots + NetItem.SafeSlots);
                    //player.TPlayer.bank3.item[index].netDefaults(0);
                }
                else
                {
                    player.TPlayer.trashItem.netDefaults(0);
                }
            }

            for (int k = 0; k < NetItem.MaxInventory - (NetItem.SafeSlots + NetItem.PiggySlots + NetItem.ForgeSlots); k++) //clear all slots excluding bank slots, bank slots cleared in ResetBanks method
            {
                NetMessage.SendData((int)PacketTypes.PlayerSlot, -1, -1, NetworkText.Empty, player.Index, (float)k, 0f, 0f, 0);
            }

            var trashSlot = NetItem.InventorySlots + NetItem.ArmorSlots + NetItem.DyeSlots + NetItem.MiscEquipSlots + NetItem.MiscDyeSlots + NetItem.PiggySlots + NetItem.SafeSlots;
            NetMessage.SendData((int)PacketTypes.PlayerSlot, -1, -1, NetworkText.Empty, player.Index, (float)trashSlot, 0f, 0f, 0); //trash slot

            for (int k = 0; k < Player.maxBuffs; k++)
            {
                player.TPlayer.buffType[k] = 0;
            }

            NetMessage.SendData((int)PacketTypes.PlayerInfo, -1, -1, NetworkText.FromLiteral(player.Name), player.Index, 0f, 0f, 0f, 0);
            NetMessage.SendData((int)PacketTypes.PlayerMana, -1, -1, NetworkText.Empty, player.Index, 0f, 0f, 0f, 0);
            NetMessage.SendData((int)PacketTypes.PlayerHp, -1, -1, NetworkText.Empty, player.Index, 0f, 0f, 0f, 0);
            NetMessage.SendData((int)PacketTypes.PlayerBuff, -1, -1, NetworkText.Empty, player.Index, 0f, 0f, 0f, 0);

            for (int k = 0; k < NetItem.MaxInventory - (NetItem.SafeSlots + NetItem.PiggySlots); k++)
            {
                NetMessage.SendData((int)PacketTypes.PlayerSlot, player.Index, -1, NetworkText.Empty, player.Index, (float)k, 0f, 0f, 0);
            }
            NetMessage.SendData((int)PacketTypes.PlayerSlot, player.Index, -1, NetworkText.Empty, player.Index, (float)trashSlot, 0f, 0f, 0);

            for (int k = 0; k < Player.maxBuffs; k++)
            {
                player.TPlayer.buffType[k] = 0;
            }

            NetMessage.SendData((int)PacketTypes.PlayerInfo, player.Index, -1, NetworkText.FromLiteral(player.Name), player.Index, 0f, 0f, 0f, 0);
            NetMessage.SendData((int)PacketTypes.PlayerMana, player.Index, -1, NetworkText.Empty, player.Index, 0f, 0f, 0f, 0);
            NetMessage.SendData((int)PacketTypes.PlayerHp, player.Index, -1, NetworkText.Empty, player.Index, 0f, 0f, 0f, 0);
            NetMessage.SendData((int)PacketTypes.PlayerBuff, player.Index, -1, NetworkText.Empty, player.Index, 0f, 0f, 0f, 0);
        }

        public static void ResetQuests(TSPlayer player)
        {
            player.TPlayer.anglerQuestsFinished = 0;

            NetMessage.SendData((int)PacketTypes.NumberOfAnglerQuestsCompleted, -1, -1, NetworkText.Empty, player.Index);
            NetMessage.SendData((int)PacketTypes.NumberOfAnglerQuestsCompleted, player.Index, -1, NetworkText.Empty, player.Index);
        }

        public static void ResetBanks(TSPlayer player)
        {
            for (int k = 0; k < NetItem.PiggySlots; k++)
            {
                player.TPlayer.bank.item[k].netDefaults(0);
            }
            for (int k = 0; k < NetItem.SafeSlots; k++)
            {
                player.TPlayer.bank2.item[k].netDefaults(0);
            }
            for (int k = 0; k < NetItem.ForgeSlots; k++)
            {
                player.TPlayer.bank3.item[k].netDefaults(0);
            }

            for (int k = NetItem.MaxInventory - (NetItem.PiggySlots + NetItem.SafeSlots + NetItem.ForgeSlots) - 1; k < NetItem.MaxInventory; k++)
            {
                NetMessage.SendData((int)PacketTypes.PlayerSlot, -1, -1, NetworkText.Empty, player.Index, (float)k, 0f, 0f, 0);
            }
            for (int k = NetItem.MaxInventory - (NetItem.PiggySlots + NetItem.SafeSlots + NetItem.ForgeSlots) - 1; k < NetItem.MaxInventory; k++)
            {
                NetMessage.SendData((int)PacketTypes.PlayerSlot, player.Index, -1, NetworkText.Empty, player.Index, (float)k, 0f, 0f, 0);
            }
        }
        #endregion

        #region Team Set
        public const string Empty = "0";
        public static void TeamTeleport(string name, int whoAmI)
        {
            string team = Delegates.informal[GetPlayerTeam(name)];
            string[] s = Delegates.Instance.spawn.GetValue(team).Split('x');
            string sX = s[0];
            string sY = s[1];
            float x, y;
            float.TryParse(sX, out x);
            float.TryParse(sY, out y);
            TShock.Players[whoAmI].Teleport(x, y);
            TShock.Players[whoAmI].SendSuccessMessage(string.Format("You have been sent to {0}'s spawn at {1}:{2}.", team, x, y));
        }
        public static int GetTeamIndex(string team)
        {
            for (int j = 0; j < Delegates.Teams.Length; j++)
            {
                if (Delegates.Teams[j].ToLower().Contains(team.ToLower()))
                    return j;
            }
            return 0;
        }
        public static void SetTeam(int who, int team)
        {
            Main.player[who].team = team;
            //TShock.Players[who].SetTeam(team);
            NetMessage.SendData((int)PacketTypes.PlayerTeam, -1, -1, null, who, team);
            NetMessage.SendData((int)PacketTypes.PlayerTeam, who, -1, null, who, team);
            //TShock.Players[who].SendData(PacketTypes.PlayerTeam, "", who, team);
        }
        public static int GetPlayerTeam(string name)
        {
            for (int i = 0; i < Delegates.Teams.Length; i++)
            {
                var block = Plugin.Instance.teamData.GetBlock(Delegates.Teams[i]);
                foreach (string t in block.Contents)
                {
                    if (!string.IsNullOrWhiteSpace(t))
                    {
                        if (block.Value(t).ToLower() == name.ToLower())
                        {
                            return i;
                        }
                    }
                }
            }
            return 0;
        }
        public static bool SetPlayerTeam(string name, int team)
        {
            var block = Plugin.Instance.teamData.GetBlock(Delegates.Teams[team]);
            foreach (string t in block.Contents)
            {
                if (!string.IsNullOrWhiteSpace(t))
                {
                    if (block.Value(t) == Empty)
                    {
                        RemoveFromTeam(name);
                        block.WriteValue(block.Key(t), name);
                        SetTeam(FromName(name).whoAmI, team);
                        return true;
                    }
                }
            }
            block = Plugin.Instance.teamData.GetBlock(Delegates.Teams[team]);
            if (Delegates.Instance.overflow)
            {
                RemoveFromTeam(name);
                block.AddItem("players" + block.Contents.Length + 1, name);
                SetTeam(FromName(name).whoAmI, team);
                return true;
            }
            return false;
        }
        public static bool RemoveFromTeam(string name)
        {
            for (int i = 0; i < Delegates.Teams.Length; i++)
            {
                var block = Plugin.Instance.teamData.GetBlock(Delegates.Teams[i]);
                foreach (string t in block.Contents)
                {
                    if (!string.IsNullOrWhiteSpace(t))
                    {
                        if (block.Value(t).ToLower() == name.ToLower())
                        {
                            block.WriteValue(block.Key(t), Empty);
                            SetTeam(FromName(name).whoAmI, 0);
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        public static Player FromName(string name)
        {
            for (int i = 0; i < Main.player.Length; i++)
            {
                if (Main.player[i].name.ToLower() == name.ToLower())
                    return Main.player[i];
            }
            return Main.player[255];
        }
        public static int TeamCount(int index)
        {
            int count = 0;
            Block block = Plugin.Instance.teamData.GetBlock(Delegates.Teams[index]);
            for (int i = 1; i <= Delegates.Instance.total; i++)
            {
                if (block.GetValue("players" + i) != "0")
                {
                    count++;
                }
            }
            return count;
        }
        #endregion

        #region Item Tweak
        public static int GetParamIndex(string key)
        {
            var Parameters = Delegates.Parameters;
            int choose = 0;
            int index = -1;
            for (int i = 0; i < Parameters.Length; i++)
            {
                int same = 0;
                for (int k = 0; k < Parameters[i].Length - 1; k++)
                {
                    if (k < key.Length - 1)
                    {
                        if (Parameters[i].ToLower().Substring(k, 1) == key.ToLower().Substring(k, 1))
                        {
                            if (same++ > choose)
                                index = i;
                        }
                    }
                }
                choose = same;
            }
            return index;
        }
        #endregion
    }   
}