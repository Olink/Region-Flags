using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TShockAPI.DB;

namespace RegionFlags
{
    class FlaggedRegion
    {
        private int flags = 0;
        private Region region;

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
            return f;
        }
    }
}
