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

using ArcticCircle;

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
                if (e.MsgID == PacketTypes.PlayerTeam)
                {
                    using (BinaryReader br = new BinaryReader(new MemoryStream(e.Msg.readBuffer, e.Index, e.Length)))
                    {
                        //  Team set
                        byte who = br.ReadByte();
                        byte team = br.ReadByte();
                        int check = Utils.GetPlayerTeam(Main.player[who].name);
                         Utils.SetTeam(who, check);
                        if (Delegates.Instance.kickOnSwitch && team != check && team != 0)
                        {
                            TShock.Players[who].Disconnect("Kicked for switching teams.");
                        }
                    }
                }
            }
        }
    }
}