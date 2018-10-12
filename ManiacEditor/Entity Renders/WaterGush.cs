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
    public class WaterGush : EntityRenderer
    {

        public override void Draw(DevicePanel d, SceneEntity entity, EditorEntity e, int x, int y, int Transparency)
        {
            var length = (int)(entity.attributesMap["length"].ValueUInt32);
            var editorAnim = e.LoadAnimation2("WaterGush", d, 0, -1, false, false, false);
            if (editorAnim != null && editorAnim.Frames.Count != 0)
            {
                var frame = editorAnim.Frames[e.index];
                e.ProcessAnimation(frame.Entry.FrameSpeed, frame.Entry.Frames.Count, frame.Frame.Duration);
                for (int i = -length + 1; i <= 0; ++i)
                {
                    d.DrawBitmap(frame.Texture, x + frame.Frame.CenterX, y + frame.Frame.CenterY + i * frame.Frame.Height,
                        frame.Frame.Width, frame.Frame.Height, false, Transparency);
                }
            }
        }

        public override string GetObjectName()
        {
            return "WaterGush";
        }
    }
}
