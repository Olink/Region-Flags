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
                       args.Handled = true;
                       Main.npc[args.ID].life = Main.npc[args.ID].lifeMax;
                       NetMessage.SendData(23, -1, -1, "", args.ID, 0f, 0f, 0f, 0);
                   }
               }
            }
        }
    }
}
