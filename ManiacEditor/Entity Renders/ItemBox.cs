using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using ManiacEditor;
using Microsoft.Xna.Framework;
using RSDKv5;

namespace ManiacEditor.Entity_Renders
{
    public class ItemBox
    {

        public void Draw(DevicePanel d, SceneEntity entity, EditorEntity e, int x, int y, int Transparency)
        {
                var value = entity.attributesMap["type"];
                int direction = (int)entity.attributesMap["direction"].ValueUInt8;
                bool fliph = false;
                bool flipv = false;
                switch (direction)
                {
                    case 0:
                        break;
                    case 1:
                        flipv = true;
                        break;
                    default:
                        break;

                }
                var editorAnimBox = e.LoadAnimation2("ItemBox", d, 0, 0, fliph, flipv, false);
                var editorAnimEffect = e.LoadAnimation2("ItemBox", d, 2, (int)value.ValueVar, fliph, flipv, false);
                if (editorAnimBox != null && editorAnimEffect != null && editorAnimEffect.Frames.Count != 0)
                {
                    var frameBox = editorAnimBox.Frames[0];
                    var frameEffect = editorAnimEffect.Frames[0];
                    d.DrawBitmap(frameBox.Texture, x + frameBox.Frame.CenterX, y + frameBox.Frame.CenterY,
                        frameBox.Frame.Width, frameBox.Frame.Height, false, Transparency);
                    d.DrawBitmap(frameEffect.Texture, x + frameEffect.Frame.CenterX, y + frameEffect.Frame.CenterY - (flipv ? (-3) : 3),
                        frameEffect.Frame.Width, frameEffect.Frame.Height, false, Transparency);
                }
        }
    }
}
