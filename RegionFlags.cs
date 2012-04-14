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
    [APIVersion(1,11)]
    public class RegionFlags : TerrariaPlugin
    {
        private Dictionary<string, PositionQueue> playerPos;
        private FlaggedRegionManager regions;
        private RegionPlayer[] players;
        static IDbConnection db;

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
        }

        protected override void Dispose(bool disposing)
        {
            if( disposing )
            {
                GameHooks.Update -= OnUpdate;
                NetHooks.GreetPlayer -= OnGreet;
            }
            base.Dispose(disposing);
        }

        public override void Initialize()
        {
            TShockAPI.Commands.ChatCommands.Add(new Command("setflags", SetFlags, "rflags", "rf"));
            GameHooks.Update += OnUpdate;
            NetHooks.GreetPlayer += OnGreet;
            ServerHooks.Leave += OnLeave;
            Database();
            Import();
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
                                     new SqlColumn("Flags", MySqlDbType.Int32){ DefaultValue = "0" }
                );
            var creator = new SqlTableCreator(db,
                                              db.GetSqlType() == SqlType.Sqlite
                                                ? (IQueryBuilder)new SqliteQueryCreator()
                                                : new MysqlQueryCreator());
            creator.EnsureExists(table);
        }

        private void Import()
        {
            String query = "SELECT * FROM Blocked_NPC";

            using (var reader = db.QueryReader(query))
            {
                while (reader.Read())
                {
                    string name = reader.Get<string>("Name");
                    int flags = reader.Get<int>("Flags");
                    regions.ImportRegion(name, flags);
                }
            }
    }

        private void OnGreet( int id, HandledEventArgs args)
        {
            if (args.Handled)
                return;

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

        
        private void OnUpdate()
        {
            DateTime now = DateTime.Now;
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
        }

        private void SetFlags( CommandArgs args )
        {
            if( args.Parameters.Count < 3 )
            {
                args.Player.SendMessage("Invalid usage", Color.Red);
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
    }
}
