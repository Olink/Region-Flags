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
        }

        public void ImportRegion( string name, int flags, int d, int h, List<string> items )
        {
            var reg = TShock.Regions.GetRegionByName(name);
            if( reg == null )
            {
                Console.WriteLine( "{0} was not found in tshocks region list.", name);
                return;
            }
            FlaggedRegion f = new FlaggedRegion(reg, flags);
            f.setDPS( d );
            f.setHPS(h);
			f.setBannedItems(items);
            regions.Add( name, f );
        }

        public bool AddRegion( string name, int flags )
        {
            if( regions.ContainsKey( name ) )
            {
                return false;
            }
            var reg = TShock.Regions.GetRegionByName(name);
            FlaggedRegion f = new FlaggedRegion(reg, flags);
            f.setDPS(0);
            //todo:save to db
            RegionFlags.db.Query(
                    "INSERT INTO Regions (Name, Flags, Damage) VALUES (@0, @1, @2);",
                    name, flags, 0);
            regions.Add(name, f);

            return true;
        }

        public bool UpdateRegion( string name )
        {
            if( !regions.ContainsKey(name))
            {
                return false;
            }

            FlaggedRegion f = regions[name];

            RegionFlags.db.Query(
                    "UPDATE Regions SET Flags=@0, Damage=@1, Heal=@2 WHERE Name=@3", f.getIntFlags(), f.getDPS(), f.getHPS(), name);
            return true;
        }

        public FlaggedRegion getRegion( string region )
        {
            if( regions.ContainsKey(region))
            {
                return regions[region];
            }

            return null;
        }

        public List<FlaggedRegion> InRegions( int x, int y )
        {
            List<FlaggedRegion> ret = new List<FlaggedRegion>();
            foreach( FlaggedRegion reg in regions.Values )
            {
                if (reg.getRegion() != null)
                {
                    if (reg.getRegion().InArea(x, y))
                        ret.Add(reg);
                }
            }
            return ret;
        }

	    public void Clear()
	    {
		    regions.Clear();
	    }
    }
}
