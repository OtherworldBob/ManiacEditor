using System;
using System.IO;
using System.Linq;

namespace RSDKv5
{
    /// <summary>
    /// The definition of all the Tiles in a stage of Sonic Mania
    /// Multiple acts/scenes, may use the same stage of Tiles.
    /// </summary>
    public class TilesConfig
    {
        public static readonly byte[] MAGIC = new byte[] { (byte)'T', (byte)'I', (byte)'L', (byte)'\0' };

        const int TILES_COUNT = 0x400;

        TileConfig[] _collisionPath1 = new TileConfig[TILES_COUNT];
        TileConfig[] _collisionPath2 = new TileConfig[TILES_COUNT];

        public TileConfig[] CollisionPath1 { get => _collisionPath1; set => _collisionPath1 = value; }
        public TileConfig[] CollisionPath2 { get => _collisionPath2; set => _collisionPath2 = value; }

        public TilesConfig(string filename) : this(new Reader(filename)) { }

        public TilesConfig(Stream stream) : this(new Reader(stream)) { }

        private TilesConfig(Reader reader)
        {
            if (!reader.ReadBytes(4).SequenceEqual(MAGIC))
                throw new Exception("Invalid tiles config file header magic");

            using (Reader creader = reader.GetCompressedStream())
            {
                for (int i = 0; i < TILES_COUNT; ++i)
                    _collisionPath1[i] = new TileConfig(creader);
                for (int i = 0; i < TILES_COUNT; ++i)
                    _collisionPath2[i] = new TileConfig(creader);
            }
        }

        public void Write(string fileName)
        {
            using (Writer writer = new Writer(fileName))
                Write(writer);
        }

        internal void Write(Writer writer)
        {
            writer.Write(MAGIC);
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter binaryWriter = new BinaryWriter(ms))
            {
                WriteAllTilesConfig(_collisionPath1, binaryWriter);
                WriteAllTilesConfig(_collisionPath2, binaryWriter);

                binaryWriter.Flush();
                writer.WriteCompressed(ms.ToArray());
            }
        }

        private void WriteAllTilesConfig(TileConfig[] setOfTilesConfig, BinaryWriter writer)
        {
            foreach (var tileConfig in setOfTilesConfig)
            {
                tileConfig.Write(writer);
            }
        }
    }
}
