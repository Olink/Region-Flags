using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Hooks;
using TShockAPI;
using Terraria;

namespace RegionFlags
{
    [APIVersion(1,11)]
    public class RegionFlags : TerrariaPlugin
    {
        private Dictionary<string, PositionQueue> playerPos;
        private FlaggedRegionManager regions;
        private RegionPlayer[] players;
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
