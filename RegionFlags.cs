using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TShockAPI;
using Terraria;

namespace RegionFlags
{
    [APIVersion(1,11)]
    public class RegionFlags : TerrariaPlugin
    {
        private Dictionary<string, PositionQueue> playerPos;
        private FlaggedRegionManager regions;
        
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
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        public override void Initialize()
        {
            TShockAPI.Commands.ChatCommands.Add(new Command("setflags", SetFlags, "rflags", "rf"));
        }

        private void SetFlags( CommandArgs args )
        {
            if( args.Parameters.Count < 2 )
            {
                
            }

            string regionname = args.Parameters[0];
            string flag = args.Parameters[1];
            Flags enumval;
            if (Flags.TryParse(flag.ToUpper(), out enumval))
            {
                FlaggedRegion reg = regions.getRegion(regionname);
                reg.setFlags(enumval);
            }


        }
    }
}
