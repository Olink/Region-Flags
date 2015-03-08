using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TShockAPI;
using TShockAPI.DB;
using Terraria;

namespace RegionFlags
{
    class PlayerHooks
    {
        private FlaggedRegionManager regionManager;

        public PlayerHooks(FlaggedRegionManager region)
        {
            regionManager = region;
        }

		public void OnItemDrop(object sender, TShockAPI.GetDataHandlers.ItemDropEventArgs args)
		{
			var reg =
				TShock.Regions.GetTopRegion(TShock.Regions.InAreaRegion((int)args.Position.X / 16, (int)args.Position.Y / 16));
			if (reg != null)
			{
				var freg = regionManager.getRegion(reg.Name);
				if (freg != null && freg.getFlags().Contains(Flags.NOITEM))
				{
					Main.item[args.ID].SetDefaults(0);
					args.Handled = true;
				}
			}
		}

       public void OnDamage( object sender, TShockAPI.GetDataHandlers.PlayerDamageEventArgs args )
        {
            Region r = TShock.Regions.GetTopRegion(
                TShock.Regions.InAreaRegion((int)Main.player[args.ID].position.X / 16, (int)Main.player[args.ID].position.Y / 16));
            if( r != null )
            {
               FlaggedRegion reg = regionManager.getRegion(r.Name);
               if (reg != null)
               {
                   List<Flags> flags = reg.getFlags();
                   if( flags.Contains( Flags.HEALONDAMAGE ) )
                   {
                       int heal = 0;
                       int damage = Math.Max(args.Damage*(args.Critical ? 2 : 1) -
                                    (int)(Math.Round(Main.player[args.ID].statDefense * .5)), 1);

                       var items = TShock.Utils.GetItemByIdOrName("heart");
                       while(heal < damage)
                       {
                           Utilities.GiveItem(items[0].name, (int)Main.player[args.ID].position.X, (int)Main.player[args.ID].position.Y, items[0].width,
                                items[0].height, items[0].type, 1, items[0].prefix, args.ID, Main.player[args.ID].velocity);
                           heal += 20;
                       }
                   }
               }
            }
        }
    }
}
