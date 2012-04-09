using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TShockAPI;

namespace RegionFlags
{
    class RegionPlayer
    {
        private TSPlayer player;
        private bool forcedPVP = false;
        private PositionQueue positions;
        private FlaggedRegionManager regionManager;

        public RegionPlayer( TSPlayer ply, FlaggedRegionManager regionManager )
        {
            player = ply;
            positions = new PositionQueue();
            this.regionManager = regionManager;
        }

        private DateTime lastUpdate = DateTime.Now;
        public void Update()
        {
            DateTime now = DateTime.Now;
            if ((now - lastUpdate).Seconds != 0)
            {
                positions.enqueue(player.TPlayer.position);
                lastUpdate = now;
            }

            List<string> regions = TShock.Regions.InAreaRegionName(player.TileX, player.TileY);
            if( regions.Count > 0 )
            {
                foreach( string s in regions )
                {
                    FlaggedRegion reg = regionManager.getRegion(s);
                    if( reg != null )
                    {
                        if( reg.getFlags().Contains(Flags.PRIVATE) )
                        {
                            Vector2 pos = positions.getTP();
                            player.Teleport((int)pos.X / 16, (int)pos.Y / 16);
                        }
                    }
                }
            }
        }
    }
}
