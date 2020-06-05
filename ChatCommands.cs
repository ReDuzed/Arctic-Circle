using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OTAPI.Tile;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using TShockAPI;
using TerrariaApi.Server;
using RUDD.Dotnet;

using static ArcticCircle.Hooks;

namespace ArcticCircle
{
    public class ChatCommands
    {
        public ChatCommands()
        {
            ChatCommands.Instance = this;
        }
        public static ChatCommands Instance;
        private bool removeClass, canChoose = true;
        public List<TileData> copy = new List<TileData>();	
        public static bool loaded;
        public void AddCommands()
        {
            var DEL = Delegates.Instance;
            void add(Command cmd)
            {
                Commands.ChatCommands.Add(cmd);
            }
            #region Item Classes
            add(new Command("classes.user.choose", DEL.ChooseClass, "chooseclass") { HelpText = "" });
            add(new Command("classes.admin.add", DEL.AddClass, "addclass") { HelpText = "" });
            add(new Command("classes.admin.reset", DEL.ResetAll, "resetall") { HelpText = "" });
            add(new Command("classes.admin.reset.opt", DEL.ResetOption, "resetopt") { HelpText = "" });
            add(new Command("classes.admin.reload", DEL.Reload, "reload") { HelpText = "" });
            add(new Command("classes.admin.reset.logout", delegate(CommandArgs e)
            {
                removeClass = !removeClass;
                e.Player.SendSuccessMessage("Player that log out have their class type removed: [" + removeClass + "]");
            }, "classlogout") { HelpText = "" });
            add(new Command("classes.admin.opt", delegate(CommandArgs e)
            {
                canChoose = !canChoose;
                e.Player.SendSuccessMessage("Players able to choose classes: [" + canChoose + "]");
            }, "canchoose") { HelpText = "" });
            add(new Command("classes.admin.start", DEL.Start, "match"));
            add(new Command("classes.admin.help", delegate(CommandArgs e)
            {
                var list = PaginationTools.BuildLinesFromTerms(new List<string>() { "chooseclass", "resetall", "resetopt", "reload", "classlogout", "canchoose", "match" });
                if (e.Message.Contains(" "))
                {
                    int.TryParse(e.Message.Substring(e.Message.IndexOf(" ") + 1), out int page);
                    PaginationTools.SendPage(e.Player, page, list);
                }
                else
                {
                    PaginationTools.SendPage(e.Player, 1, list);
                }
            }, "classhelp"));
            #endregion

            


            #region Team Set
            add(new Command("teamset.admin.set", DEL.PlaceTeam, new string[] { "placeteam", "removeteam" })
            {
                HelpText = "For placing or removing players from teams.",
                AllowServer = false
            });
            add(new Command("teamset.admin", DEL.Reload, new string[] { "reload" })
            {
                HelpText = "Reloads settings."
            });
            add(new Command("teamset.admin.group", DEL.MakeGroups, new string[] { "teamgroups" })
            {
                HelpText = "Makes general groups for each team color."
            });
            add(new Command("teamset.admin.group", DEL.MakeGroups, new string[] { "teamset" })
            {
                HelpText = "Modifies the associated group for the specified team color."
            });
            add(new Command("teamset.join", DEL.JoinTeam, new string[] { "jointeam", "team" }) // "team" command only used to deny the PvPToggle plugin command /team.
            {
                HelpText = "Allows players to join a team if they aren't on one already.",
                AllowServer = false
            });
            add(new Command("teamset.tp", DEL.TeamSpawn, new string[] { "tspawn" })
            {
                HelpText = "Spawns player to their team-designated spawn point",
                AllowServer = false
            });
            add(new Command("teamset.admin.tp", DEL.SetSpawn, new string[] { "settspawn" })
            {
                HelpText = "For admins to set team spawns",
                AllowServer = false
            });
            add(new Command("teamset.admin", delegate(CommandArgs a)
            {
                DEL.teamSpawn = !DEL.teamSpawn;
                a.Player.SendSuccessMessage("Teams being permitted to use /tspawn is [" + DEL.teamSpawn + "].");
            }, "teamspawn")
            {
                HelpText = "Toggles whether players can use /tspawn to go to team spawn locations."
            });
            add(new Command("teamset.help", delegate(CommandArgs a)
            {
                a.Player.SendInfoMessage(string.Format("{0} <index | color>, {1} <name>\n{2}\n{3} automates team group creation parented to group default\n{4} <team color> <group>\n{5} <color | index>\n{6} team spawn switch\n{7} <color> places spawn at your current position\n{8} teleports to team spawn\n{9} switches player on leave being removed from team\n{10} <<1-5>,<1-5>,[<1-5>]...> use 2 or more team indices to autosort into said teams \n{11} <reset | init <#>> Useful for expanding the maximum number of players per team \n{12} <all | team | [username]> Teleport whole everyone, team, or single player to their team spawn \n{13} removes everyone from their teams",
                                        "/placeteam", "/removeteam", "/reload", "/teamgroups", "/teamset", "/jointeam", "/tspawn", "/settspawn", "/teamspawn", "/teamleavekick", "/autosort", "/database", "/tpteam", "/resetteams"));
            }, "teamscrip")
            {
                HelpText = "Toggles whether players can use /tspawn to go to team spawn locations."
            });
            //  Kicking a player via /kick removed them from their team with this flag set.
            add(new Command("teamset.admin", delegate(CommandArgs a)
            {
                DEL.kickOnLeave = !DEL.kickOnLeave;
                a.Player.SendSuccessMessage("Players that leave are removed from their designated team [" + DEL.kickOnLeave + "].");
            }, "teamleavekick")
            {
                HelpText = "Toggles whether players leaving should kick them off their teams."
            });
            add(new Command("teamset.superadmin.db", DEL.MakeDataBase, "database")
            {
                HelpText = "Makes the database with which to store maximum player per team only to be used after the INI file is manually set up"
            });
            add(new Command("teamset.admin.sort", DEL.AutoSort, "autosort")
            {
                HelpText = "Begins automatically sorting players into the teams that have the least players through use of team indices."
            });
            add(new Command("teamset.admin", DEL.TeleportTeam, "tpteam")
            {
                HelpText = "Teleport whole team or single player using /tpteam <all | team <color> | [username]>"
            });
            add(new Command("teamset.admin", delegate(CommandArgs a)
            {
                DEL.autoAssignGroup = !DEL.autoAssignGroup;
                a.Player.SendSuccessMessage("Automatically assigning members upon team join to group set to: [" + DEL.autoAssignGroup + "].");
            }, "autoassign")
            {
                HelpText = "Flag for automatically assigning members to configured groups upon team join"
            });
            add(new Command("teamset.admin.kick", DEL.KickAll, "resetteams")
            {
                HelpText = "Removes all server member's teams"
            });
            add(new Command("teamset.help", delegate(CommandArgs e)
            {
                var list = PaginationTools.BuildLinesFromTerms(new List<string>() { "placeteam", "removeteam", "reload", "teamgroups", "teamset", "jointeam", "team", "tspawn", "settspawn", "teamspawn", "teamscrip", "teamleavekick", "database", "autosort", "tpteam", "autoassign", "resetteams" });
                if (e.Message.Contains(" "))
                {
                    int.TryParse(e.Message.Substring(e.Message.IndexOf(" ") + 1), out int page);
                    PaginationTools.SendPage(e.Player, page, list);
                }
                else
                {
                    PaginationTools.SendPage(e.Player, 1, list);
                }
            }, "teamhelp")
            {
                HelpText = "Provides help pages for commands"
            });
            #endregion

            #region Item Tweak
            add(new Command("item.admin.add", DEL.ItemTweak, new string[] { "tweak" }));
            add(new Command("item.admin.get", DEL.ItemGet, new string[] { "giveitem" }));
            add(new Command("item.admin.add", DEL.ItemRestore, new string[] { "resetitem" }));
            #endregion

            // Safe Regions v2
            add(new Command("saferegion.admin.setup", DEL.Setup, new string[] { "region", "sr", "load" }));

            // Tile manager	
            add(new Command("tile.superadmin.replace", 	
            delegate(CommandArgs e)	
            {	
                var tiles = copy;
                int index = 0;
                TileData[] sprites = tiles.Where(t => 
                            (t.type == TileID.Explosives ||
                            t.type == TileID.PressurePlates || 
                            t.wall == WallID.RubyGemspark ||
                            t.wall == WallID.SapphireGemspark) &&
                            t.active == true).ToArray();	
                for (int m = 0; m < sprites.Length; m++)
                {
                    Utils.ReplaceTiles(sprites[m].i, sprites[m].j, sprites[m]);
                    index++;
                }
                for (int n = 0; n < modifiedTile.Count; n++)	
                {	
                    TileData[] list = tiles.Where(t => 	
                            t.i == modifiedTile[n].i && 	
                            t.j == modifiedTile[n].j &&	
                            t.type == modifiedTile[n].type &&	
                            t.active == modifiedTile[n].active).ToArray();	
                    
                    if (list.Length == 0)	
                        continue;	

                    TileData data = list[0];	
                    int i = data.i;	
                    int j = data.j;	
                    
                    if (data.active)	
                    {
                        Utils.ReplaceTiles(i, j, data);
                        index++;
                    }	
                    else	
                    {	
                        WorldGen.KillTile(i, j, false, false, true);	
                        TShockAPI.TSPlayer.All.SendData(PacketTypes.Tile, "", 0, i, j);	                        
                        index++;
                    }
                }	
                if (index > 0)
                {
                    modifiedTile.Clear();
                    e.Player.SendSuccessMessage("Replaced " + index + " tiles with their original versions.");
                }
                else
                {
                    e.Player.SendErrorMessage("There are no tiles to replace with their originals.");
                }
            }, "replaceworld"));	
            add(new Command("tile.superadmin.replace", 	
            delegate(CommandArgs e)	
            {	
                if (!loaded)	
                {	
                    for (int i = 0; i < Main.maxTilesX; i++)	
                    {	
                        for (int j = 0; j < Main.maxTilesY; j++)	
                        copy.Add(new TileData(){	
                            i = i,	
                            j = j,	
                            type = Main.tile[i, j].type,	
                            slope = Main.tile[i, j].slope(),	
                            active = Main.tile[i, j].active(),
                            halfBrick = Main.tile[i, j].halfBrick(),
                            wall = Main.tile[i, j].wall
                        });	
                    }	
                    loaded = true;	
                    e.Player.SendSuccessMessage("World tiles have been loaded into memory.");	
                }	
                else	
                {	
                    e.Player.SendErrorMessage("The world tiles have already been loaded into memory.");	
                }	
            }, "loadworld"));
        }
    }
}