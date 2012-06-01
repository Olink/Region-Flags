using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TShockAPI.DB;
using Terraria;

namespace RegionFlags
{
    class FlaggedRegion
    {
        private int flags = 0;
        private Region region;
        private int dps = 0;

        public FlaggedRegion(Region r )
        {
            region = r;
        }

        public FlaggedRegion(Region r, int f )
        {
            region = r;
            flags = f;
        }

        public Region getRegion()
        {
            return region;
        }

        public void setFlags(Flags f)
        {
            if (f == Flags.NONE)
                flags = 0;
            else 
                flags |= (int)f;
        }

        public void removeFlags(Flags f)
        {
            flags &= (int)(~f);
        }

        public List<Flags> getFlags()
        {
            List<Flags> f = new List<Flags>();
            if ((flags & (int)Flags.PRIVATE) == (int)Flags.PRIVATE)
            {
                f.Add(Flags.PRIVATE);
            }
            if ((flags & (int)Flags.DEATH) == (int)Flags.DEATH)
            {
                f.Add(Flags.DEATH);
            }
            if ((flags & (int)Flags.PVP) == (int)Flags.PVP)
            {
                f.Add(Flags.PVP);
            }
            if ((flags & (int)Flags.PVP) == (int)Flags.PVP)
            {
                f.Add(Flags.PVP);
            }
            if ((flags & (int)Flags.HURT) == (int)Flags.HURT)
            {
                f.Add(Flags.HURT);
            }
            if ((flags & (int)Flags.NOITEM) == (int)Flags.NOITEM)
            {
                f.Add(Flags.NOITEM);
            }
            return f;
        }

        public int getIntFlags()
        {
            return flags;
        }

        public int getDPS()
        {
            return dps;
        }

        public void setDPS( int s )
        {
            dps = s;
        }
    }
}
