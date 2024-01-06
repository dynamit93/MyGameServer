using System;
using System.IO;
using System.Collections.Generic;

namespace MyGameServer.Reader
{
    public class OTBMReader
    {
        // Constants
        private const int ESCAPE_CHAR = 0xFD;
        private const int START_CHAR = 0xFE;
        private const int END_CHAR = 0xFF;
        private const byte OTBM_MAP_DATA = 0x01;
        private const byte OTBM_TILE_AREA = 0x02;
        // Define other constants for different node types if needed

        private string fileName;

        // Define the OTBM header struct
        public struct OTBMRootHeader
        {
            public uint version;
            public ushort width;
            public ushort height;
            public uint majorVersionItems;
            public uint minorVersionItems;
        }

        // Define the OTBM map header node struct
        public struct OTBMMapHeaderNode
        {
            public byte nodeId;         // OTBM_MAP_DATA
            public byte[] attributes;   // OTBM_ATTR_DESCRIPTION || OTBM_ATTR_EXT_SPAWN_FILE || OTBM_ATTR_EXT_HOUSE_FILE
            public byte nullByte;       // 0
        }

        public struct OTBMTileArea
        {
            public byte node_id;
            public ushort base_x;
            public ushort base_y;
            public byte base_z;
        }

        // Define similar structs for other node types (OTBM_TILE_AREA, OTBM_TILE, OTBM_HOUSETILE, etc.)

        public OTBMReader(string fileName)
        {
            this.fileName = fileName;
        }

        public void ReadOTBMFile()
        {
            try
            {
                byte[] fileContents = File.ReadAllBytes(fileName);
                int currentPosition = 0;

                // Check if the file has the correct header
                if (fileContents.Length < 4 || !ValidateHeader(fileContents, ref currentPosition))
                {
                    throw new InvalidOTBFormatException();
                }

                // Read the header fields
                OTBMRootHeader header = ReadHeader(fileContents, ref currentPosition);

                // Check if the root node is correctly terminated
                if (fileContents[currentPosition] != END_CHAR)
                {
                    throw new InvalidOTBFormatException();
                }

                // Read the root node, providing the root type (OTBM_ROOT) as the third argument
                Node rootNode = ReadNode(fileContents, ref currentPosition, 0x02); // Replace 0x00 with the appropriate type value



                // Process the root node or access its properties
                // You can implement your logic here
                ProcessNode(rootNode);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading '{fileName}': {ex.Message}");
            }
        }


        private bool ValidateHeader(byte[] fileContents, ref int currentPosition)
        {
            // Check if there are enough bytes in the array to validate the header
            if (currentPosition + 4 <= fileContents.Length &&
                fileContents[currentPosition] == (byte)'O' &&
                fileContents[currentPosition + 1] == (byte)'T' &&
                fileContents[currentPosition + 2] == (byte)'B' &&
                fileContents[currentPosition + 3] == (byte)'M')
            {
                currentPosition += 4; // Move past the header
                Console.WriteLine("Header validation successful.");
                return true;
            }
            else
            {
                Console.WriteLine("Header validation failed.");
            }

            return false;
        }

        private OTBMRootHeader ReadHeader(byte[] fileContents, ref int currentPosition)
        {
            OTBMRootHeader header = new OTBMRootHeader
            {
                version = BitConverter.ToUInt32(fileContents, currentPosition),
                width = BitConverter.ToUInt16(fileContents, currentPosition + 4),
                height = BitConverter.ToUInt16(fileContents, currentPosition + 6),
                majorVersionItems = BitConverter.ToUInt32(fileContents, currentPosition + 8),
                minorVersionItems = BitConverter.ToUInt32(fileContents, currentPosition + 12)
            };

            currentPosition += 16; // Move past the header
            return header;
        }

        private Node ReadNode(byte[] fileContents, ref int currentPosition, byte type)
        {
            Node node = new Node(type);

            while (currentPosition < fileContents.Length)
            {
                byte childType = fileContents[currentPosition];
                currentPosition++;

                if (childType == END_CHAR)
                {
                    break;
                }

                if (childType == ESCAPE_CHAR)
                {
                    childType = fileContents[currentPosition];
                    currentPosition++;
                }

                node.Children.Add(ReadNode(fileContents, ref currentPosition, childType));
            }

            return node;
        }

        private OTBMMapHeaderNode ReadMapHeader(byte[] fileContents, ref int currentPosition)
        {
            // Implement code to read and return OTBMMapHeaderNode
            // ...

            return new OTBMMapHeaderNode(); // Replace with actual reading logic
        }

        private OTBMTileArea ReadTileArea(byte[] fileContents, ref int currentPosition)
        {
            // Implement code to read and return OTBMTileArea
            // ...

            return new OTBMTileArea(); // Replace with actual reading logic
        }

        private void ProcessNode(Node node)
        {
            // Process the node or access its properties
            // Implement your logic here
            Console.WriteLine($"Node Type: {node.Type}");

            foreach (var childNode in node.Children)
            {
                ProcessNode(childNode);
            }
        }
    }

    public class Node
    {
        public List<Node> Children { get; set; } = new List<Node>();
        public byte Type { get; set; }

        public Node(byte type)
        {
            Type = type;
        }
    }


    public class InvalidOTBFormatException : Exception
    {
        public InvalidOTBFormatException() : base("Invalid OTBM file format") { }
    }
}
