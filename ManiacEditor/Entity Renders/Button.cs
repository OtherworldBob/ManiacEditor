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
    public class Button : EntityRenderer
    {

        public override void Draw(DevicePanel d, SceneEntity entity, EditorEntity e, int x, int y, int Transparency)
        {
            int type = (int)entity.attributesMap["type"].ValueVar;
            bool fliph = false;
            bool flipv = false;
            int animID = 0;
            if (type == 0 || type == 1)
            {
                animID = 0;
            }
            if (type == 2 || type == 3)
            {
                animID = 1;
            }
            if (type == 3)
            {
                fliph = true;
            }
            if (type == 1)
            {
                flipv = true;
            }
            var editorAnim = e.LoadAnimation2("Button", d, animID, -1, fliph, flipv, false);
            if (editorAnim != null && editorAnim.Frames.Count != 0 && animID >= 0)
            {
                var frame = editorAnim.Frames[e.index];

                d.DrawBitmap(frame.Texture,
                    x + frame.Frame.CenterX - (fliph ? (frame.Frame.Width - editorAnim.Frames[0].Frame.Width + 10) : 0),
                    y + frame.Frame.CenterY + (flipv ? (frame.Frame.Height - editorAnim.Frames[0].Frame.Height + 10) : 0),
                    frame.Frame.Width, frame.Frame.Height, false, Transparency);
            }
        }

        public override string GetObjectName()
        {
            return "Button";
        }
    }
}
