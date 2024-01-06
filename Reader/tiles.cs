using System;
using System.Collections.Generic;

namespace MyGameServer.Reader
{
    public class Tile
    {
        public Position Position { get; }
        public List<Item> Items { get; }

        public Tile(Position position)
        {
            Position = position;
            Items = new List<Item>();
        }
    }
}
