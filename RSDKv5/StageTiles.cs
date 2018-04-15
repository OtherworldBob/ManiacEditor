using System;
using System.IO;

namespace RSDKv5
{
    public class StageTiles : IDisposable
    {
        public readonly GIF Image;
        public readonly TilesConfig Config;
        private string _tileConfigPath;

        public StageTiles(string stage_directory)
        {
            Image = new GIF(Path.Combine(stage_directory, "16x16Tiles.gif"));
            _tileConfigPath = Path.Combine(stage_directory, "TileConfig.bin");
            if (File.Exists(_tileConfigPath))
                Config = new TilesConfig(_tileConfigPath);
        }

        public void Dispose()
        {
            Image.Dispose();
        }

        public void DisposeTextures()
        {
            Image.DisposeTextures();
        }

        public void Write()
        {
            Config?.Write(_tileConfigPath);
        }
    }
}
