using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terraria;

namespace RegionFlags
{
    class Utilities
    {
        public static void GiveItem(string name, int X, int Y, int width, int height, int type, int stack, int prefix, int id, Vector2 velocity)
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
            Main.item[itemid].noGrabDelay = 1;
            NetMessage.SendData((int)PacketTypes.ItemDrop, -1, -1, "", itemid, 0f, 0f, 0f);
            NetMessage.SendData((int)PacketTypes.ItemOwner, -1, -1, "", itemid, 0f, 0f, 0f);
        }
    }
}
