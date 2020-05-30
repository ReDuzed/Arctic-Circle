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
        public static class ClassID
        {
            public const string None = "None", Ranged = "Ranged", Melee = "Melee", Mage = "Mage";
            public static string[] Array = new string[4];
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
    }   
}