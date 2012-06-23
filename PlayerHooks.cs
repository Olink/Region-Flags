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
                   if( flags.Contains( Flags.HEAL ) )
                   {
                       var items = TShock.Utils.GetItemByIdOrName("heart");
                       GiveItem(items[0].name, (int)Main.player[args.ID].position.X, (int)Main.player[args.ID].position.Y, items[0].width,
                           items[0].height, items[0].type, 10, items[0].prefix, args.ID, Main.player[args.ID].velocity);
                   }
               }
            }
        }

        private void GiveItem( string name, int X, int Y, int width, int height, int type, int stack, int prefix, int id, Vector2 velocity )
        {
            int itemid = Item.NewItem((int)X, (int)Y, width, height, type, stack, true, prefix);

            // This is for special pickaxe/hammers/swords etc
            Main.item[itemid].SetDefaults(name);
            // The set default overrides the wet and stack set by NewItem
            Main.item[itemid].wet = Collision.WetCollision(Main.item[itemid].position, Main.item[itemid].width,
                                                           Main.item[itemid].height);
            Main.item[itemid].stack = stack;
            Main.item[itemid].owner = id;
            Main.item[itemid].prefix = (byte)prefix;
            Main.item[itemid].velocity = velocity;
            NetMessage.SendData((int)PacketTypes.ItemDrop, -1, -1, "", itemid, 0f, 0f, 0f);
            NetMessage.SendData((int)PacketTypes.ItemOwner, -1, -1, "", itemid, 0f, 0f, 0f);
        }
    }
}
