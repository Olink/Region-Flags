using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TShockAPI;
using TShockAPI.DB;
using Terraria;

namespace RegionFlags
{
    class NPCHooks
    {
        private FlaggedRegionManager regionManager;

        public NPCHooks( FlaggedRegionManager region )
        {
            regionManager = region;
        }

       public void OnNPCStrike( object sender, TShockAPI.GetDataHandlers.NPCStrikeEventArgs args )
        {
           Console.WriteLine("mob took damage");
            Region r = TShock.Regions.GetTopRegion( 
                TShock.Regions.InAreaRegion((int)Main.npc[args.ID].position.X / 16, (int)Main.npc[args.ID].position.Y / 16) );
            if( r != null )
            {
               FlaggedRegion reg = regionManager.getRegion(r.Name);
               if (reg != null)
               {
                   List<Flags> flags = reg.getFlags();
                   if( flags.Contains( Flags.GODMOB ) )
                   {
                       Console.WriteLine( "we prevented it");
                       args.Handled = true;
                       TSPlayer.All.SendData(PacketTypes.NpcUpdate, "", -1, );
                       return;
                   }
               }
            }
        }
    }
}
