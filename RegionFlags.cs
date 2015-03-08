using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Mono.Data.Sqlite;
using MySql.Data.MySqlClient;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.DB;
using Terraria;
using TShockAPI.Hooks;

namespace RegionFlags
{
    [ApiVersion(1,17)]
    public class RegionFlags : TerrariaPlugin
    {
        private FlaggedRegionManager regions;
        private RegionPlayer[] players;
        public static IDbConnection db;
        private NPCHooks npchooks;
        private PlayerHooks playerhooks;

        public override string Author
        {
            get { return "Olink"; }
        }

        public override string Description
        {
            get { return "Provides flags for regions."; }
        }

        public override string Name
        {
            get { return "Region flags."; }
        }

        public override Version Version
        {
            get { return new Version(0, 1); }
        }


        public RegionFlags( Main game ) : base( game )
        {
            Order = 3;
            regions = new FlaggedRegionManager();
            players = new RegionPlayer[255];
            npchooks = new NPCHooks( regions );
            playerhooks = new PlayerHooks( regions );
        }

        protected override void Dispose(bool disposing)
        {
            if( disposing )
            {
				ServerApi.Hooks.GameUpdate.Deregister(this, OnUpdate);
				ServerApi.Hooks.GamePostInitialize.Deregister(this, Import);
				GetDataHandlers.ItemDrop -= playerhooks.OnItemDrop;
				ServerApi.Hooks.NetGreetPlayer.Deregister(this, OnGreet);
				ServerApi.Hooks.ServerLeave.Deregister(this, OnLeave);
                GetDataHandlers.NPCStrike -= npchooks.OnNPCStrike;
                GetDataHandlers.PlayerDamage -= playerhooks.OnDamage;
				TShockAPI.Hooks.GeneralHooks.ReloadEvent -= OnReload;
	            TShockAPI.Hooks.PlayerHooks.PlayerPostLogin -= OnLogin;
            }
            base.Dispose(disposing);
        }

        public override void Initialize()
        {
            Commands.ChatCommands.Add(new Command("setflags", SetFlags, "rflags", "rf"));
            Commands.ChatCommands.Add(new Command("defineflag", DefineRegion, "dreg"));
            Commands.ChatCommands.Add(new Command("setflags", SetDPS, "regdamage", "rd"));
            Commands.ChatCommands.Add(new Command("setflags", SetHPS, "regheal", "rh"));
			Commands.ChatCommands.Add(new Command("setflags", SetTempGroup, "regtemp", "rt"));
			Commands.ChatCommands.Add(new Command("setflags", ViewFlags, "regview", "rv"));
            ServerApi.Hooks.GameUpdate.Register(this, OnUpdate);
            ServerApi.Hooks.GamePostInitialize.Register(this, Import, -1);
            GetDataHandlers.ItemDrop += playerhooks.OnItemDrop;
            ServerApi.Hooks.NetGreetPlayer.Register(this, OnGreet);
            ServerApi.Hooks.ServerLeave.Register(this, OnLeave);
            GetDataHandlers.NPCStrike += npchooks.OnNPCStrike;
            GetDataHandlers.PlayerDamage += playerhooks.OnDamage;
	        TShockAPI.Hooks.GeneralHooks.ReloadEvent += OnReload;
			TShockAPI.Hooks.PlayerHooks.PlayerPostLogin += OnLogin;
            Database();
        }

        private void Database()
        {
            if (TShock.Config.StorageType.ToLower() == "sqlite")
            {
                string sql = Path.Combine(TShock.SavePath, "region_flags.sqlite");
                db = new SqliteConnection(string.Format("uri=file://{0},Version=3", sql));
            }
            else if (TShock.Config.StorageType.ToLower() == "mysql")
            {
                try
                {
                    var hostport = TShock.Config.MySqlHost.Split(':');
                    db = new MySqlConnection();
                    db.ConnectionString =
                        String.Format("Server={0}; Port={1}; Database={2}; Uid={3}; Pwd={4};",
                                      hostport[0],
                                      hostport.Length > 1 ? hostport[1] : "3306",
                                      "region_flags",
                                      TShock.Config.MySqlUsername,
                                      TShock.Config.MySqlPassword
                            );
                }
                catch (MySqlException ex)
                {
                    Log.Error(ex.ToString());
                    throw new Exception("MySql not setup correctly");
                }
            }
            else
            {
                throw new Exception("Invalid storage type");
            }
            
            var table = new SqlTable("Regions",
                                     new SqlColumn("Name", MySqlDbType.VarChar, 56){ Length = 56, Primary = true},
                                     new SqlColumn("Flags", MySqlDbType.Int32){ DefaultValue = "0" },
                                     new SqlColumn("Damage", MySqlDbType.Int32) { DefaultValue = "0" },
                                     new SqlColumn("Heal", MySqlDbType.Int32) { DefaultValue = "0" },
									 new SqlColumn("BannedItems", MySqlDbType.Text) { DefaultValue = "" },
									 new SqlColumn("TempGroup", MySqlDbType.VarChar, 56)
                );
            var creator = new SqlTableCreator(db,
                                              db.GetSqlType() == SqlType.Sqlite
                                                ? (IQueryBuilder)new SqliteQueryCreator()
                                                : new MysqlQueryCreator());
            creator.EnsureExists(table);
        }

	    private void OnReload(ReloadEventArgs args)
	    {
		    regions.Clear();
			Import(new EventArgs());
	    }

        private void Import(EventArgs args)
        {
            String query = "SELECT * FROM Regions";

            using (var reader = db.QueryReader(query))
            {
                while (reader.Read())
                {
                    string name = reader.Get<string>("Name");
                    int flags = reader.Get<int>("Flags");
                    int damage = reader.Get<int>("Damage");
                    int heal = reader.Get<int>("Heal");
	                string bannedItems = reader.Get<string>("BannedItems") ?? "";
	                string tempGroup = reader.Get<string>("TempGroup") ?? "";
	                Group g = TShock.Groups.GetGroupByName(tempGroup);
	                List<string> bannedItemsList = new List<string>(bannedItems.Split(',').ToList().Select(s => s.Trim()));
					regions.ImportRegion(name, flags, damage, heal, bannedItemsList, g);
                }
            }
        }

        private void OnGreet(GreetPlayerEventArgs args)
        {
            lock (players)
            {
				players[args.Who] = new RegionPlayer(TShock.Players[args.Who], regions);
            }
        }

        private void OnLeave(LeaveEventArgs args)
        {
            lock (players)
            {
                players[args.Who] = null;
            }
        }

	    private void OnLogin(PlayerPostLoginEventArgs args)
	    {
		    lock(players)
		    {
			    players[args.Player.Index].OriginalGroup = args.Player.Group;
		    }
	    }

        private DateTime lastUpdate = DateTime.Now;
        private void OnUpdate(EventArgs args)
        {
            lock( players )
            {
                foreach( RegionPlayer ply in players )
                {
                    if( ply != null )
                    {
                        ply.Update();
                    }
                }
            }

            DateTime now = DateTime.Now;
            if( (now - lastUpdate).TotalSeconds > 0 )
            {
                lastUpdate = now;
                lock (Main.npc)
                {
                    foreach (NPC npc in Main.npc)
                    {
                        if (!npc.active)
                            continue;

                        Region r = TShock.Regions.GetTopRegion(
                            TShock.Regions.InAreaRegion((int) npc.position.X/16, (int) npc.position.Y/16));
                        if (r != null)
                        {
                            FlaggedRegion reg = regions.getRegion(r.Name);
                            if (reg != null)
                            {
                                List<Flags> flags = reg.getFlags();
                                if (flags.Contains(Flags.MOBKILL))
                                {
                                    npc.StrikeNPC(9999, 0f, 0);
                                    NetMessage.SendData(23, -1, -1, "", npc.whoAmI, 0f, 0f, 0f, 0);
                                }
                                else if (flags.Contains(Flags.NOMOB))
                                {
                                    npc.active = false;
                                    NetMessage.SendData(23, -1, -1, "", npc.whoAmI, 0f, 0f, 0f, 0);
                                }
                            }
                        }
                    }
                }
            }
        }

	    private void ViewFlags(CommandArgs args)
	    {
			if (args.Parameters.Count == 1)
			{
				FlaggedRegion region = regions.getRegion(args.Parameters[0]);
				if (region == null)
				{
					args.Player.SendErrorMessage("Region '{0}' is not a region flag defined region.", args.Parameters[0]);
					return;
				}

				string flags = string.Join(", ", region.getFlags().Select(f => f.ToString()));
				args.Player.SendInfoMessage("Flags for '{0} are: {1}", region.getRegion().Name, flags);
				return;
			}

			args.Player.SendErrorMessage("Usage: /regview[/rv] [region name] - Views the flags on a given region.");
	    }

        private void SetFlags( CommandArgs args )
        {
            if (args.Parameters.Count == 1 && args.Parameters[0] == "flags")
            {
                string flags = string.Join(", ", Enum.GetNames(typeof (Flags)));
                args.Player.SendInfoMessage("Available flags: {0}", flags);
                return;
            }
            if( args.Parameters.Count < 3 )
            {
                args.Player.SendMessage("Invalid usage: /rflags(/rf) set|rem [region name] [flag]", Color.Red);
                return;
            }

            string regionname = args.Parameters[1];
            string flag = args.Parameters[2];
            FlaggedRegion reg = regions.getRegion(regionname);
            if( reg == null )
            {
                args.Player.SendMessage("Invalid region", Color.Red);
                return;
            }
            switch(  args.Parameters[0] )
            {
                case "set":
				case "add":
                {
                    
                    Flags enumval;
                    if (Flags.TryParse(flag.ToUpper(), out enumval))
                    {
                        reg.setFlags(enumval);
                        if (!regions.UpdateRegion(regionname) )
                        {
                            args.Player.SendMessage("Region doesn't exist.", Color.Red);
                        }
                        args.Player.SendMessage("Region now has flag.", Color.Green);
                    }
                    else
                    {
                        args.Player.SendMessage("Invalid flag", Color.Red);
                    }
                    break;
                }
                case "rem":
                case "remove":
                {
                    Flags enumval;
                    if (Flags.TryParse(flag.ToUpper(), out enumval))
                    {
                        reg.removeFlags(enumval);
                        if (!regions.UpdateRegion(regionname))
                        {
                            args.Player.SendMessage("Region doesn't exist.", Color.Red);
                        }
                        args.Player.SendMessage("Flag has been removed from region.", Color.Green);
                    }
                    else
                    {
                        args.Player.SendMessage("Invalid flag", Color.Red);
                    }
                    break;
                }
            }
        }

        private void DefineRegion( CommandArgs args )
        {
            if( args.Parameters.Count < 1 )
            {
                args.Player.SendMessage("Invalid usage: /dreg [region name]", Color.Red);
            }
            else
            {                                                       
                string region = args.Parameters[0];
                if( TShock.Regions.GetRegionByName(region) == null )
                {
                    args.Player.SendMessage("Region does not exist.", Color.Red);
                    return;
                }
                if( regions.AddRegion(region, (int)Flags.NONE) )
                    args.Player.SendMessage("Region has been defined.", Color.Green);
                else
                {
                    args.Player.SendMessage("Region already defined.", Color.Red);
                }
            }
        }

        private void SetDPS( CommandArgs args)
        {
            if (args.Parameters.Count < 2)
            {
                args.Player.SendMessage("Invalid usage: /regdamage[/rd] [region name] [damage]", Color.Red);
            }
            else
            {
                string region = args.Parameters[0];
                int damage = 0;
                if( !int.TryParse(args.Parameters[1], out damage ) )
                {
                    args.Player.SendMessage("You must specify damage as a number.", Color.Red);
                    return;
                }

                FlaggedRegion reg = regions.getRegion(region);
                if (reg == null)
                {
                    args.Player.SendMessage("Invalid region", Color.Red);
                    return;
                }

                args.Player.SendMessage(String.Format("DPS for {0} is now {1}", region, damage), Color.Green);
                reg.setDPS(damage);
                regions.UpdateRegion(reg.getRegion().Name);
            }
        }

        private void SetHPS(CommandArgs args)
        {
            if (args.Parameters.Count < 2)
            {
                args.Player.SendMessage("Invalid usage: /regheal[/rh] [region name] [heal]", Color.Red);
            }
            else
            {
                string region = args.Parameters[0];
                int health = 0;
                if (!int.TryParse(args.Parameters[1], out health))
                {
                    args.Player.SendMessage("You must specify health as a number of seconds between heart drops.", Color.Red);
                    return;
                }

                FlaggedRegion reg = regions.getRegion(region);
                if (reg == null)
                {
                    args.Player.SendMessage("Invalid region", Color.Red);
                    return;
                }

                args.Player.SendMessage(String.Format("HPS for {0} is now {1}", region, health), Color.Green);
                reg.setHPS(health);
                regions.UpdateRegion(reg.getRegion().Name);
            }
        }

		private void SetTempGroup(CommandArgs args)
		{
			if (args.Parameters.Count < 2)
			{
				args.Player.SendMessage("Invalid usage: /regtemp[/rt] [region name] [group name]", Color.Red);
			}
			else
			{
				string region = args.Parameters[0];
				string group = args.Parameters[1];
				Group g = TShock.Groups.GetGroupByName(group);
				if (g == null)
				{
					args.Player.SendErrorMessage("Group '{0}' does not exist.", group);
					return;
				}

				FlaggedRegion reg = regions.getRegion(region);
				if (reg == null)
				{
					args.Player.SendMessage("Invalid region", Color.Red);
					return;
				}

				args.Player.SendMessage(String.Format("Temp Group for {0} is now {1}", region, g.Name), Color.Green);
				reg.setTempGroup(g);
				regions.UpdateRegion(reg.getRegion().Name);
			}
		}
    }
}
