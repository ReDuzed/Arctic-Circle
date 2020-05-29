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

using ItemClasses;
using TeamSetQueue;

namespace ArcticCircle
{
    public class Plugin : TerrariaPlugin
    {
        public override string Author => "";
        public override string Name => "";
        public override string Description => "";
        public override Version Version => new Version(1, 0, 0, 0);
        public Plugin(Main game) : base (game)
        {

        }
        public ItemClassesV2 Classes;
        public TeamSetV2 TeamSet; 
        public override void Initialize()
        {
            Classes = new ItemClassesV2(this);
            TeamSet = new TeamSetV2(this);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {   
                Classes.Dispose(disposing, this);
                TeamSet.Dispose(disposing, this);
            }
        }
    }
}
