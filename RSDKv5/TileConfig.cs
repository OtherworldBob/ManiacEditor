using System.Linq;
using System.IO;

namespace RSDKv5
{
    /// <summary>
    /// The definition of a single Tile in Sonic Mania
    /// </summary>
    public class TileConfig
    {
        /// <summary>
        /// Collision position for each pixel
        /// </summary>
        private byte[] _collision;

        private byte[] _rawHasCollision;

        /// <summary>
        /// If has collision
        /// </summary>
        private bool[] _hasCollision;

        /// <summary>
        /// If is ceiling, the collision is from below
        /// </summary>
        private bool _isCeiling;

        // Unknow 5 bytes config
        private byte _magic1;
        private byte _magic2;
        private byte _magic3;
        private byte _magic4;
        private byte _specialBehaviour;

        public byte[] Collision { get => _collision; set => _collision = value; }
        public bool[] HasCollision { get => _hasCollision; set => _hasCollision = value; }
        public bool IsCeiling { get => _isCeiling; set => _isCeiling = value; }
        public byte Magic1 { get => _magic1; set => _magic1 = value; }
        public byte Magic2 { get => _magic2; set => _magic2 = value; }
        public byte Magic3 { get => _magic3; set => _magic3 = value; }
        public byte Magic4 { get => _magic4; set => _magic4 = value; }
        public byte SpecialBehaviour { get => _specialBehaviour; set => _specialBehaviour = value; }

        public TileConfig(Stream stream) : this(new Reader(stream)) { }

        internal TileConfig(Reader reader)
        {
            _collision = reader.ReadBytes(16);
            _rawHasCollision = reader.ReadBytes(16);
            _hasCollision = _rawHasCollision.Select(x => x != 0).ToArray();
            _isCeiling = reader.ReadBoolean();
            _magic1 = reader.ReadByte();
            _magic2 = reader.ReadByte();
            _magic3 = reader.ReadByte();
            _magic4 = reader.ReadByte();
            _specialBehaviour = reader.ReadByte();
        }

        /// <summary>
        /// These fields are all compressed together in a large group
        /// </summary>
        /// <param name="writer"></param>
        internal void Write(BinaryWriter writer)
        {
            writer.Write(_collision);
            writer.Write(_rawHasCollision);
            writer.Write(_isCeiling);
            writer.Write(_magic1);
            writer.Write(_magic2);
            writer.Write(_magic3);
            writer.Write(_magic4);
            writer.Write(_specialBehaviour);
        }
    }
}
