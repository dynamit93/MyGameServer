﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Drawing;

namespace MyGameServer.player
{

    public class ClientCreature
    {
        public string CreatureName { get; set; }
        public int CreatureID { get; set; }
        public string Name { get; set; }

        // Add other properties that are relevant to Creature
        // These properties can be similar to those in Player
    }

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
        public int SkillAxe { get; set; }
        public int SkillAxeTries { get; set; }
        public int SkillClub { get; set; }
        public int SkillClubTries { get; set; }
        public int SkillDist { get; set; }
        public int SkillDistTries { get; set; }
        public int SkillFishing { get; set; }
        public int SkillFishingTries { get; set; }
        public int SkillFist { get; set; }


    }
    public class Skills
    {
        public int SkillAxe { get; set; }
        public int SkillAxeTries { get; set; }
        public int SkillClub { get; set; }
        public int SkillClubTries { get; set; }
        public int SkillDist { get; set; }
        public int SkillDistTries { get; set; }
        public int SkillFishing { get; set; }
        public int SkillFishingTries { get; set; }
        public int SkillFist { get; set; }
    }
    public class ClientSkill
    {
        public string LongName { get; set; }
        public int Value { get; set; }
        public int Percent { get; set; } // Add this if needed

        public ClientSkill(string longName, int value = 0, int percent = 0)
        {
            LongName = longName;
            Value = value;
            Percent = percent;
        }
    }

    public class CustomPlayer
    {
        public string Type { get; set; }
        public int PlayerId { get; set; }
        public int AccountId { get; set; }
        public string Name { get; set; }
        public ClientSkill Level { get; set; }
        public long Balance { get; set; }
        public byte Blessings { get; set; }
        public int Cap { get; set; }
        public long Experience { get; set; }
        public int GroupId { get; set; }
        public int Health { get; set; }
        public int HealthMax { get; set; }
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
        public byte Save { get; set; }
        public int Sex { get; set; }
        public Dictionary<string, ClientSkill> Skills { get; set; }

   

        // Constructor
        public CustomPlayer()
        {

            Skills = new Dictionary<string, ClientSkill>();
        }
    }


}
