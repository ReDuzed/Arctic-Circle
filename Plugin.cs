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
    [ApiVersion(2, 1)]
    public class Plugin : TerrariaPlugin
    {
        public override string Author => "";
        public override string Name => "Arctic Circle";
        public override string Description => "A variety of plugins for the Arctic Circle PvP minigame.";
        public override Version Version => new Version(1, 0, 0, 0);
        public Plugin(Main game) : base (game)
        {
            Instance = this;
            Hooks.Instance = new Hooks();
            Delegates.Instance = new Delegates();
            ChatCommands.Instance = new ChatCommands();
        }   
        public static Plugin Instance;
        public static Ini classINI;
        public DataStore teamData;
        public DataStore item_data;
        public override void Initialize()
        {
            ChatCommands.Instance.AddCommands();
            teamData = new DataStore("config\\team_data");
            item_data = new DataStore("config\\item_data");
            classINI = new Ini()
            {
                path = "config\\class_data" + Ini.ext
            };
            Delegates.Instance.Reload(new CommandArgs(string.Empty, TSPlayer.Server, null));
            ServerApi.Hooks.ServerJoin.Register(this, Hooks.Instance.OnJoin);
            ServerApi.Hooks.ServerLeave.Register(this, Hooks.Instance.OnLeave);
            ServerApi.Hooks.GameUpdate.Register(this, Hooks.Instance.ItemClassGameUpdate);
            ServerApi.Hooks.NetGetData.Register(this, Hooks.Instance.OnGetData);

            Utils.SetDefaultStats();
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {   
                teamData.WriteToFile();
                item_data.WriteToFile();
                ServerApi.Hooks.ServerJoin.Deregister(this, Hooks.Instance.OnJoin);
                ServerApi.Hooks.ServerLeave.Deregister(this, Hooks.Instance.OnLeave);
                ServerApi.Hooks.GameUpdate.Deregister(this, Hooks.Instance.ItemClassGameUpdate);
                ServerApi.Hooks.NetGetData.Deregister(this, Hooks.Instance.OnGetData);
            }
        }
    }
}
