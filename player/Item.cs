using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyGameServer.player
{
    public class Player_Items
    {
        [Key]
        public int Sid { get; set; } // Assuming Sid is the primary key

        public byte[] Attributes { get; set; } // Blob type
        public short Count { get; set; } // SmallInt type
        public short ItemType { get; set; } // SmallInt type
        public int Pid { get; set; } // Assuming this is a regular int field

        [ForeignKey("Player")]
        public int PlayerId { get; set; } // Foreign key to Player table
        public Player Player { get; set; } // Navigation property
    }
}

