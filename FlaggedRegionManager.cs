using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TShockAPI;
using TShockAPI.DB;

namespace RegionFlags
{
    class FlaggedRegionManager
    {
        private Dictionary<string, FlaggedRegion> regions;
        
        public FlaggedRegionManager()
        {
            regions = new Dictionary<string, FlaggedRegion>();
            regions.Add("test", new FlaggedRegion( null ));
        }

        public void ImportRegion( string name, int flags )
        {
            var reg = TShock.Regions.GetRegionByName(name);
            FlaggedRegion f = new FlaggedRegion(reg, flags);
        }

        public void AddRegion( string name, int flags )
        {
            var reg = TShock.Regions.GetRegionByName(name);
            FlaggedRegion f = new FlaggedRegion(reg, flags);
            //todo:save to db
            
        }

        public FlaggedRegion getRegion( string region )
        {
            if( regions.ContainsKey(region))
            {
                return regions[region];
            }

            return null;
        }
    }
}
