using System;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Text.RegularExpressions;
using System.Xml;
using System.Reflection.PortableExecutable;
using TiledSharp;
using ZstdNet;
using MyGameServer;
using MyGameServer.player;
using OpenTibiaCommons.Domain;
using OpenTibiaCommons.IO;
using Newtonsoft.Json;
using System.Drawing;
using SharpTibiaProxy.Domain;
using ClientCreature = MyGameServer.player.ClientCreature;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using MyGameServer;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;

class Program
{
    //public static List<PlayerGame> players = new List<PlayerGame>(); // Declare and initialize the players list



     

    public static OtMap LoadMap()
    {
        // Load items definitions
        OtItems items = new OtItems();
        items.Load("items.otb");

        // Initialize OtMap
        OtMap map = new OtMap(items);
        var tileLocations = map.Tiles.Select(t => t.Location.ToIndex()).ToHashSet();
        string spawnFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test-spawn.xml");

        string otbmFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test.otbm");
        map.Load(otbmFilePath, replaceTiles: true);


        map.LoadSpawn(spawnFilePath, tileLocations);
      

        return map;
    }




    //Console.WriteLine(creature.Location.X + spawn.Location.X);
    //            Console.WriteLine(creature.Location.Y + spawn.Location.Y);

    public static void Main(string[] args)
    {
        LuaScripting luaScripting = new LuaScripting();

        string monsterxmlstring = @"Data\Monster\monster.xml";

        luaScripting.LoadMonsterData(monsterxmlstring);


        var dbContext = new GameContext();
        OtMap map = LoadMap();  // LoadMap now also includes spawn loading


        // Print details of all creatures
       // PrintAllCreatureLocations(map);

        // Continue with server startup
        SimpleTcpServer server = new SimpleTcpServer(1300, dbContext, map);
        server.Start();
    }

private static byte[] DecompressBase64ZstdData(string base64CompressedData)
    {
        byte[] compressedData = Convert.FromBase64String(base64CompressedData);

        using (var decompressor = new Decompressor())
        {
            return decompressor.Unwrap(compressedData);
        }
    }



}