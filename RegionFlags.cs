using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using Hooks;
using Mono.Data.Sqlite;
using MySql.Data.MySqlClient;
using TShockAPI;
using TShockAPI.DB;
using Terraria;

namespace RegionFlags
{
    [APIVersion(1,12)]
    public class RegionFlags : TerrariaPlugin
    {
        private FlaggedRegionManager regions;
        private RegionPlayer[] players;
        public static IDbConnection db;
        private NPCHooks npchooks;
        private PlayerHooks playerhooks;

        public override string Author
        {
            get { return "Zack Piispanen"; }
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
                GameHooks.Update -= OnUpdate;
                GameHooks.PostInitialize -= Import;
                NetHooks.GreetPlayer -= OnGreet;
                GetDataHandlers.NPCStrike -= npchooks.OnNPCStrike;
                GetDataHandlers.PlayerDamage -= playerhooks.OnDamage;
            }
            base.Dispose(disposing);
        }

        public override void Initialize()
        {
            TShockAPI.Commands.ChatCommands.Add(new Command("setflags", SetFlags, "rflags", "rf"));
            TShockAPI.Commands.ChatCommands.Add(new Command("defineflag", DefineRegion, "dreg"));
            TShockAPI.Commands.ChatCommands.Add(new Command("setflags", SetDPS, "regdamage", "rd"));
            TShockAPI.Commands.ChatCommands.Add(new Command("setflags", SetHPS, "regheal", "rh"));
            GameHooks.Update += OnUpdate;
            GameHooks.PostInitialize += Import  ;
            TShockAPI.GetDataHandlers.ItemDrop += OnItemDrop;
            NetHooks.GreetPlayer += OnGreet;
            ServerHooks.Leave += OnLeave;
            GetDataHandlers.NPCStrike += npchooks.OnNPCStrike;
            GetDataHandlers.PlayerDamage += playerhooks.OnDamage;
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
                                     new SqlColumn("Heal", MySqlDbType.Int32) { DefaultValue = "0" }
                );
            var creator = new SqlTableCreator(db,
                                              db.GetSqlType() == SqlType.Sqlite
                                                ? (IQueryBuilder)new SqliteQueryCreator()
                                                : new MysqlQueryCreator());
            creator.EnsureExists(table);
        }

        private void Import()
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
                    regions.ImportRegion(name, flags, damage, heal);
                }
            }
        }

        private void OnItemDrop( object sender, TShockAPI.GetDataHandlers.ItemDropEventArgs args )
        {
            var reg =
                TShock.Regions.GetTopRegion(TShock.Regions.InAreaRegion((int) args.Position.X/16, (int) args.Position.Y/16));
            if( reg != null )
            {
                var freg = regions.getRegion(reg.Name);
                if( freg != null && freg.getFlags().Contains(Flags.NOITEM))
                {
                    Main.item[args.ID].SetDefaults(0);
                    args.Handled = true;
                }
            }
        }

        private void OnGreet( int id, HandledEventArgs args)
        {
            lock (players)
            {
                players[id] = new RegionPlayer(TShock.Players[id], regions);
            }
        }

        private void OnLeave(int id)
        {
            lock (players)
            {
                players[id] = null;
            }
        }

        private DateTime lastUpdate = DateTime.Now;
        private void OnUpdate()
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

        private void SetFlags( CommandArgs args )
        {
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
            Console.WriteLine(String.Join(", ", reg.getFlags()));
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
    }
}
