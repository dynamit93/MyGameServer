using OpenTibiaCommons.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyGameServer
{
    public class Tile
    {
        public int X { get; set; }
        public int Y { get; set; }

        public int Z { get; set; }
        // Other tile properties...
        public int id { get; set; }

        public bool BlockProjectile { get; set; }
        public string TileName { get; set; }
    }



    public class MapTileData
    {
        public MapLocation Location { get; set; }
        public List<MapItem> Items { get; set; }
        public MapGround Ground { get; set; }
    }

    public class MapLocation
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
    }

    public class MapItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class MapGround
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }


    public class ItemData
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public OtItemType Type { get; set; }
    }

    public class GroundData
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class TileData
    {
        public MapLocation Location { get; set; }
        public List<ItemData> Items { get; set; } = new List<ItemData>();
        public GroundData Ground { get; set; }
    }


}
