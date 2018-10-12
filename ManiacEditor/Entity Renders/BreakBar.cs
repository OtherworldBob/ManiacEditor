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
    public class BreakBar : EntityRenderer
    {

        public override void Draw(DevicePanel d, SceneEntity entity, EditorEntity e, int x, int y, int Transparency)
        {
            var length = (short)(entity.attributesMap["length"].ValueUInt16);
            var orientation = entity.attributesMap["orientation"].ValueUInt8;
            if (orientation > 1)
            {
                orientation = 0;
            }
            var editorAnim = e.LoadAnimation2("BreakBar", d, orientation, -1, false, false, false);
            if (editorAnim != null && editorAnim.Frames.Count != 0)
            {
                var frameTop = editorAnim.Frames[0];
                var frameBase = editorAnim.Frames[1];
                var frameBottom = editorAnim.Frames[2];
                for (int i = -length / 2; i <= length / 2; ++i)
                {
                    d.DrawBitmap(frameBase.Texture, x + frameBase.Frame.CenterX,
                        y + frameBase.Frame.CenterY + i * frameBase.Frame.Height,
                        frameBase.Frame.Width, frameBase.Frame.Height, false, Transparency);
                }
                d.DrawBitmap(frameTop.Texture, x + frameTop.Frame.CenterX,
                    y + frameTop.Frame.CenterY - (length / 2 + 1) * frameBase.Frame.Height,
                    frameTop.Frame.Width, frameTop.Frame.Height, false, Transparency);
                d.DrawBitmap(frameBottom.Texture, x + frameBottom.Frame.CenterX,
                    y + frameBottom.Frame.CenterY + (length / 2 + 1) * frameBottom.Frame.Height,
                    frameBottom.Frame.Width, frameBottom.Frame.Height, false, Transparency);

            }
        }

        public override string GetObjectName()
        {
            return "BreakBar";
        }
    }
}
