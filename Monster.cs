using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace MyGameServer
{

    public class Monsterpath
    {
        public string path { get; set; }
    }

    public class Monster
    {
        public string Name { get; private set; }
        public string Description { get; private set; }
        public string Race { get; private set; }
        public int Experience { get; private set; }
        public int Speed { get; private set; }
        public int ManaCost { get; private set; }
        public int HealthNow { get; private set; }
        public int HealthMax { get; private set; }
        public List<MonsterAttack> Attacks { get; private set; } = new List<MonsterAttack>();
        public List<MonsterVoice> Voices { get; private set; } = new List<MonsterVoice>();
        // Add more properties as needed based on XML structure


        // Look properties
        public int LookType { get; private set; }
        public int HeadColor { get; private set; }
        public int BodyColor { get; private set; }
        public int LegsColor { get; private set; }
        public int FeetColor { get; private set; }
        public int CorpseSpriteId { get; private set; }


        // Flags
        public bool Summonable { get; private set; }
        public bool Canbecometame { get; private set; }
        public bool Attackable { get; private set; }
        public bool pushable { get; private set; }
        public bool canpushitems { get; private set; }
        public bool canpushcreatures { get; private set; }
        public bool canbeparalyzed { get; private set; }
        public int targetdistance { get; private set; }
        public int runonhealth { get; private set; }

        // Target Change (simplified for demonstration)
        public int TargetChangeInterval { get; private set; }
        public int TargetChangeChance { get; private set; }

        public Monster(string xmlPath)
        {
            LoadData(xmlPath);
        }

        private void LoadData(string xmlPath)
        {
            XDocument doc = XDocument.Load(xmlPath);
            XElement monsterElement = doc.Element("monster");
            XElement lookElement = monsterElement.Element("look");
            if (lookElement != null)
            {
                LookType = Convert.ToInt32(lookElement.Attribute("type")?.Value);
                HeadColor = Convert.ToInt32(lookElement.Attribute("head")?.Value);
                BodyColor = Convert.ToInt32(lookElement.Attribute("body")?.Value);
                LegsColor = Convert.ToInt32(lookElement.Attribute("legs")?.Value);
                FeetColor = Convert.ToInt32(lookElement.Attribute("feet")?.Value);
                CorpseSpriteId = Convert.ToInt32(lookElement.Attribute("corpse")?.Value);
            }

            // Parsing target change
            XElement targetChangeElement = monsterElement.Element("targetchange");
            if (targetChangeElement != null)
            {
                TargetChangeInterval = Convert.ToInt32(targetChangeElement.Attribute("interval")?.Value);
                TargetChangeChance = Convert.ToInt32(targetChangeElement.Attribute("chance")?.Value);
            }

            // Parsing flags
            XElement flagsElement = monsterElement.Element("flags");
            if (flagsElement != null)
            {
                Summonable = Convert.ToBoolean(Convert.ToInt32(flagsElement.Element("flag").Attribute("summonable")?.Value));
                Canbecometame = Convert.ToBoolean(Convert.ToInt32(flagsElement.Element("flag").Attribute("canbecometame")?.Value));
                Attackable = Convert.ToBoolean(Convert.ToInt32(flagsElement.Element("flag").Attribute("attackable")?.Value));
                pushable = Convert.ToBoolean(Convert.ToInt32(flagsElement.Element("flag").Attribute("pushable")?.Value));
                canpushitems = Convert.ToBoolean(Convert.ToInt32(flagsElement.Element("flag").Attribute("canpushitems")?.Value));
                canpushcreatures = Convert.ToBoolean(Convert.ToInt32(flagsElement.Element("flag").Attribute("canpushcreatures")?.Value));
                targetdistance = Convert.ToInt32(flagsElement.Element("flag").Attribute("targetdistance")?.Value);
                runonhealth = Convert.ToInt32(flagsElement.Element("flag").Attribute("runonhealth")?.Value);
                canbeparalyzed = Convert.ToBoolean(Convert.ToInt32(flagsElement.Element("flag").Attribute("canbeparalyzed")?.Value));
                // Other flags...
            }

            if (monsterElement != null)
            {
                Name = monsterElement.Attribute("name")?.Value;
                Description = monsterElement.Attribute("nameDescription")?.Value;
                Race = monsterElement.Attribute("race")?.Value;
                Experience = Convert.ToInt32(monsterElement.Attribute("experience")?.Value);
                Speed = Convert.ToInt32(monsterElement.Attribute("speed")?.Value);
                ManaCost = Convert.ToInt32(monsterElement.Attribute("manacost")?.Value);

                XElement healthElement = monsterElement.Element("health");
                if (healthElement != null)
                {
                    HealthNow = Convert.ToInt32(healthElement.Attribute("now")?.Value);
                    HealthMax = Convert.ToInt32(healthElement.Attribute("max")?.Value);
                }

                // Parse attacks
                var attackElements = monsterElement.Element("attacks")?.Elements("attack");
                if (attackElements != null)
                {
                    foreach (var attackElement in attackElements)
                    {
                        Attacks.Add(new MonsterAttack
                        {
                            Name = attackElement.Attribute("name")?.Value,
                            Interval = Convert.ToInt32(attackElement.Attribute("interval")?.Value),
                            MinDamage = Convert.ToInt32(attackElement.Attribute("min")?.Value),
                            MaxDamage = Convert.ToInt32(attackElement.Attribute("max")?.Value),
                            // Add more attributes as necessary
                        });
                    }
                }

                // Parse voices
                var voiceElements = monsterElement.Element("voices")?.Elements("voice");
                if (voiceElements != null)
                {
                    foreach (var voiceElement in voiceElements)
                    {
                        Voices.Add(new MonsterVoice
                        {
                            Sentence = voiceElement.Attribute("sentence")?.Value
                            // Add more attributes as necessary
                        });
                    }
                }
            }
        }
    }

    public class MonsterAttack
    {
        public string Name { get; set; }
        public int Interval { get; set; }
        public int MinDamage { get; set; }
        public int MaxDamage { get; set; }
        // Add more properties as necessary
    }

    public class MonsterVoice
    {
        public string Sentence { get; set; }
        // Add more properties as necessary
    }
}
