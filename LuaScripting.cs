using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLua;
using System.Xml.Linq;

namespace MyGameServer
{


    public class LuaScripting
    {
        private Lua luaState;
        public static List<Monster> AllMonsters = new List<Monster>();
        public LuaScripting()
        {
            luaState = new Lua();
            RegisterFunctions();
        }

        public void LoadScript(string path)
        {
            luaState.DoFile(path);

        }


        private void RegisterFunctions()
        {
            // Register a C# method so it can be called from Lua
            luaState.RegisterFunction("CalculateDamage", this, this.GetType().GetMethod("CalculateDamage"));
        }

        public double CalculateDamage(double baseDamage, double attack, double defense)
        {
            // Placeholder calculation logic; replace with your own logic
            return baseDamage + (attack * 0.5) - (defense * 0.3);
        }


        public void LoadMonsterData(string xmlPath)
        {
            var monsterPaths = new List<Monsterpath>();
            
            
            try
            {
                var doc = XDocument.Load(xmlPath);
                var monsters = doc.Descendants("monster").Select(m => new
                {
                    Name = m.Attribute("name")?.Value,
                    File = m.Attribute("file")?.Value
                }).Where(m => m.Name != null && m.File != null);

                foreach (var monster in monsters)
                {
                    try
                    {
                        luaState["monsterName"] = monster.Name;

                        string fixingmonsterpath = $"Data\\Monster\\{monster.File}";
                        var monsterInstance = new Monster(fixingmonsterpath);
                        AllMonsters.Add(monsterInstance);
                    }
                    catch (Exception ex)
                    {
                        // Handle errors related to Lua file loading
                        Console.WriteLine($"Error loading Lua file for monster {monster.Name}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle XML parsing errors
                Console.WriteLine($"Error loading monster data from XML: {ex.Message}");
            }
        }

    }

}
