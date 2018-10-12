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
    public class Bridge : EntityRenderer
    {

        public override void Draw(DevicePanel d, SceneEntity entity, EditorEntity e, int x, int y, int Transparency)
        {
            var value = entity.attributesMap["length"].ValueUInt8;
            var editorAnim = e.LoadAnimation2("Bridge", d, 0, 0, false, false, false);
            if (editorAnim != null && editorAnim.Frames.Count != 0)
            {
                var frame = editorAnim.Frames[0];
                bool wEven = value % 2 == 0;
                for (int xx = 0; xx <= value; ++xx)
                {
                    d.DrawBitmap(frame.Texture,
                        x + (wEven ? frame.Frame.CenterX : -frame.Frame.Width) + (-value / 2 + xx) * frame.Frame.Width,
                        y + frame.Frame.CenterY,
                        frame.Frame.Width, frame.Frame.Height, false, Transparency);
                }
            }
        }

        public override string GetObjectName()
        {
            return "Bridge";
        }
    }
}
