using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OTAPI.Tile;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using TShockAPI;
using TShockAPI.DB;
using TerrariaApi.Server;
using RUDD.Dotnet;
using static TShockAPI.GetDataHandlers;

using ArcticCircle;
using static ArcticCircle.Utils;
using Microsoft.Xna.Framework;

namespace ArcticCircle
{
    public class Hooks
    {
        public Hooks()
        {
            Hooks.Instance = this;
        }
        public static Hooks Instance;
        public static int ticks;
        public static bool preMatchChoose;
        public bool[] hasChosenClass = new bool[256];
        public class TileData
        {
            public int i, j;
            public int type;
            public byte slope;
            public short frameX;
            public short frameY; 
            public ITile tile;
            public bool active;
        }
        public static List<TileData> modifiedTile = new List<TileData>();
        public void ItemClassGameUpdate(EventArgs e)
        {
            //  Item Classes
            if (preMatchChoose)
            {
                if ((int)Main.time % 60 == 0)
                    ticks--;
                if (ticks < 0)
                {
                    foreach (TSPlayer p in TShock.Players)
                    {
                        if (p != null && p.Active && !p.Dead)
                        {
                            Delegates.Instance.ChooseClass(new CommandArgs("chooseclass " + Utils.ClassID.Array[Main.rand.Next(0, Utils.ClassID.Array.Length - 1)], p, null));
                        }
                    }
                    preMatchChoose = false;
                }
            }
            if ((int)Main.time % 20 != 0)
                return;
            foreach (TSPlayer player in TShock.Players)
            {
                if (player != null && player.Active && !player.Dead)
                {
                    Region region = HighestAxis(player.TPlayer, out int z);
                    if (region == null)
                        continue;
                    if (Delegates.Instance.pvpRules.ContainsKey(region.Name.ToLower()))
                        TogglePvp(player.Index, Delegates.Instance.pvpRules[region.Name.ToLower()]);
                }
            }
        }
        public void OnJoin(JoinEventArgs e)
        {
            hasChosenClass[e.Who] = false;

            #region Team Set 
            Utils.SetTeam(e.Who, Utils.GetPlayerTeam(Main.player[e.Who].name));

            Block roster;
            if (!Plugin.Instance.teamData.BlockExists(Delegates.Roster))
            {
                roster = Plugin.Instance.teamData.NewBlock(new string[] { Delegates.Key }, Delegates.Roster);
            }
            else
            {
                roster = Plugin.Instance.teamData.GetBlock(Delegates.Roster);
            }
            string userName = TShock.Players[e.Who].Name;
            string list = roster.GetValue(Delegates.Key);
            if (!list.Contains(";"))
            {
                roster.WriteValue(Delegates.Key, userName + ";");
                return;
            }
            for (int i = 0; i < list.Length; i++)
            {
                if (list.Substring(i).StartsWith(userName))
                    return;
            }
            roster.AddValue(Delegates.Key, ';', userName);
            #endregion
        }

        public void OnLeave(LeaveEventArgs e)
        {
            //  Team Set
            if (Delegates.Instance.kickOnLeave)
            {
                Utils.RemoveFromTeam(TShock.Players[e.Who].Name);
            }
            //  Item Classes
            hasChosenClass[e.Who] = false;
        }

        public void OnGetData(GetDataEventArgs e)
        {
            if (!e.Handled)
            {
                using (BinaryReader br = new BinaryReader(new MemoryStream(e.Msg.readBuffer, e.Index, e.Length)))
                {
                    switch (e.MsgID)
                    {
                        case PacketTypes.PlayerTeam:
                            //  Team set
                            byte teamWho = br.ReadByte();
                            byte team = br.ReadByte();
                            int teamCheck = GetPlayerTeam(Main.player[teamWho].name);
                            Utils.SetTeam(teamWho, teamCheck);
                            if (Delegates.Instance.kickOnSwitch && team != teamCheck && team != 0)
                            {
                                TShock.Players[teamWho].Disconnect("Kicked for switching teams.");
                            }
                            break;
                        case PacketTypes.GemLockToggle:
                            e.Handled = true;
                            int lockPosX = br.ReadInt16();
                            int lockPosY = br.ReadInt16();
                            bool lockOn = br.ReadBoolean();
                            WorldGen.ToggleGemLock(lockPosX, lockPosY, lockOn);
                            break;
                        case PacketTypes.Tile:
                            byte flag = br.ReadByte();
                            int x = br.ReadInt16();
                            int y = br.ReadInt16();
                            //byte dmg = br.ReadByte();
                            
                            if (modifiedTile.Where(t => t.i == x && t.j == y).ToArray().Length != 0)
                                return;
                            
                            ITile tile = Main.tile[x, y];
                            modifiedTile.Add(new TileData()
                            {
                                i = x,
                                j = y,
                                type = tile.type,
                                slope = tile.slope(),
                                active = tile.active()
                            });
                            break;
                    }
                }
            }
        }

        // TODO: Fix issue where the player can dodge the falling item by dashing or moving fast using wings.

        public void OnItemDrop(object sender, ItemDropEventArgs e)
        {
            // Check if the player is picking up an item (E.g. from /chooseclass).
            if (e.ID != 400)
            {
                return;
            }

            e.Handled = true;

            TSPlayer tSPlayer = e.Player;
            Player player = tSPlayer.TPlayer;
            
            string itemName = TShock.Utils.GetItemById(e.Type).Name;

            int index = Item.NewItem(player.position, new Vector2(32, 48), e.Type, e.Stacks);
            Item item = Main.item[index];

            var data = Plugin.Instance.item_data;
            if (data.BlockExists(itemName))
            {
                Block block = data.GetBlock(itemName);

                int.TryParse(block.GetValue(Parameters[Damage].TrimEnd(':', '0')), out item.damage);
                int.TryParse(block.GetValue(Parameters[Crit].TrimEnd(':', '0')), out item.crit);
                float.TryParse(block.GetValue(Parameters[KB].TrimEnd(':', '0')), out item.knockBack);
                byte.TryParse(block.GetValue(Parameters[Prefix].TrimEnd(':', '0')), out item.prefix);
                int.TryParse(block.GetValue(Parameters[ReuseDelay].TrimEnd(':', '0')), out item.reuseDelay);
                int.TryParse(block.GetValue(Parameters[Shoot].TrimEnd(':', '0')), out item.shoot);
                float.TryParse(block.GetValue(Parameters[ShootSpeed].TrimEnd(':', '0')), out item.shootSpeed);
                int.TryParse(block.GetValue(Parameters[UseAmmo].TrimEnd(':', '0')), out item.useAmmo);
                int.TryParse(block.GetValue(Parameters[UseTime].TrimEnd(':', '0')), out item.useTime);
                int.TryParse(block.GetValue(Parameters[Width].TrimEnd(':', '0')), out item.width);
                int.TryParse(block.GetValue(Parameters[Height].TrimEnd(':', '0')), out item.height);
                bool.TryParse(block.GetValue(Parameters[AutoReuse].TrimEnd(':', '0')), out item.autoReuse);
                int.TryParse(block.GetValue(Parameters[Ammo].TrimEnd(':', '0')), out item.ammo);
                float.TryParse(block.GetValue(Parameters[Scale].TrimEnd(':', '0')), out item.scale);

                TSPlayer.All.SendData(PacketTypes.TweakItem, "", index, 255, 63);
            }
        }

        public void OnChestItemChange(object sender, ChestItemEventArgs e)
        {
            TSPlayer tSPlayer = e.Player;
            Player player = tSPlayer.TPlayer;

            e.Handled = true;
            Main.chest[e.ID].item[e.Slot] = new Item();
            tSPlayer.SendData(PacketTypes.ChestItem, "", e.ID, e.Slot, e.Stacks, e.Prefix, e.Type);

            string itemName = TShock.Utils.GetItemById(e.Type).Name;

            int index = Item.NewItem(player.position, new Vector2(32, 48), e.Type, e.Stacks);
            Item item = Main.item[index];

            var data = Plugin.Instance.item_data;
            if (data.BlockExists(itemName))
            {
                Block block = data.GetBlock(itemName);

                int.TryParse(block.GetValue(Parameters[Damage].TrimEnd(':', '0')), out item.damage);
                int.TryParse(block.GetValue(Parameters[Crit].TrimEnd(':', '0')), out item.crit);
                float.TryParse(block.GetValue(Parameters[KB].TrimEnd(':', '0')), out item.knockBack);
                byte.TryParse(block.GetValue(Parameters[Prefix].TrimEnd(':', '0')), out item.prefix);
                int.TryParse(block.GetValue(Parameters[ReuseDelay].TrimEnd(':', '0')), out item.reuseDelay);
                int.TryParse(block.GetValue(Parameters[Shoot].TrimEnd(':', '0')), out item.shoot);
                float.TryParse(block.GetValue(Parameters[ShootSpeed].TrimEnd(':', '0')), out item.shootSpeed);
                int.TryParse(block.GetValue(Parameters[UseAmmo].TrimEnd(':', '0')), out item.useAmmo);
                int.TryParse(block.GetValue(Parameters[UseTime].TrimEnd(':', '0')), out item.useTime);
                int.TryParse(block.GetValue(Parameters[Width].TrimEnd(':', '0')), out item.width);
                int.TryParse(block.GetValue(Parameters[Height].TrimEnd(':', '0')), out item.height);
                bool.TryParse(block.GetValue(Parameters[AutoReuse].TrimEnd(':', '0')), out item.autoReuse);
                int.TryParse(block.GetValue(Parameters[Ammo].TrimEnd(':', '0')), out item.ammo);
                float.TryParse(block.GetValue(Parameters[Scale].TrimEnd(':', '0')), out item.scale);

                TSPlayer.All.SendData(PacketTypes.TweakItem, "", index, 255, 63);
            }
        }
    }
}