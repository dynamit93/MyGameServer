using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyGameServer.player
{
    public class Player
    {

        // Add foreign key property
        public int AccountId { get; set; }

        // Navigation property to represent the relationship to the Account entity
        [ForeignKey("AccountId")]
        public Account Account { get; set; }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PlayerId { get; set; }

        [Required]
        [MaxLength(100)]
        [Column(TypeName = "varchar(100)")]
        public string Name { get; set; }

        public int Level { get; set; } = 1;

        public long Balance { get; set; }
        public byte Blessings { get; set; }
        public int Cap { get; set; } = 400;
        public long Experience { get; set; }
        public int GroupId { get; set; } = 1;
        public int Health { get; set; } = 150;
        public int HealthMax { get; set; } = 150;
        public long LastLogin { get; set; }
        public long LastLogout { get; set; }
        public int LookAddons { get; set; }
        public int LookBody { get; set; }
        public int LookFeet { get; set; }
        public int LookHead { get; set; }
        public int LookLegs { get; set; }
        public int Mana { get; set; }
        public int ManaMax { get; set; }
        public long ManaSpent { get; set; }
        public int PosX { get; set; }
        public int PosY { get; set; }
        public int PosZ { get; set; }
        public byte Save { get; set; } = 1;
        public int Sex { get; set; }
        public int SkillAxe { get; set; } = 10;
        public long SkillAxeTries { get; set; }
        public int SkillClub { get; set; } = 10;
        public long SkillClubTries { get; set; }
        public int SkillDist { get; set; } = 10;
        public long SkillDistTries { get; set; }
        public int SkillFishing { get; set; } = 10;
        public long SkillFishingTries { get; set; }
        public int SkillFist { get; set; } = 10;
    }
}
