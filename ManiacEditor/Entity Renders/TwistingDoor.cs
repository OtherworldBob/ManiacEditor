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
    public class TwistingDoor : EntityRenderer
    {

        public void Draw(DevicePanel d, SceneEntity entity, EditorEntity e, int x, int y, int Transparency)
        {
            int type = (int)entity.attributesMap["type"].ValueVar;
            int direction = (int)entity.attributesMap["direction"].ValueUInt8;
            bool fliph = false;
            bool flipv = false;
            int animID = 0;
            switch (type)
            {
                case 0:
                    animID = 0;
                    break;
                case 1:
                    animID = 1;
                    break;
                case 2:
                    animID = 2;
                    break;
                case 3:
                    animID = 3;
                    break;

            }
            switch (direction)
            {
                case 0:
                    break;
                case 1:
                    fliph = true;
                    break;
                default:
                    break;
            }

            var editorAnim = e.LoadAnimation2("TwistingDoor", d, animID, -1, fliph, flipv, false);
            if (editorAnim != null && editorAnim.Frames.Count != 0 && animID >= 0)
            {
                var frame = editorAnim.Frames[e.index];

                e.ProcessAnimation(frame.Entry.FrameSpeed, frame.Entry.Frames.Count, frame.Frame.Duration);

                d.DrawBitmap(frame.Texture,
                    x + frame.Frame.CenterX - (fliph ? (frame.Frame.Width - editorAnim.Frames[0].Frame.Width + 10) : 0),
                    y + frame.Frame.CenterY + (flipv ? (frame.Frame.Height - editorAnim.Frames[0].Frame.Height + 10) : 0),
                    frame.Frame.Width, frame.Frame.Height, false, Transparency);
            }
        }

        public override string GetObjectName()
        {
            return "TwistingDoor";
        }
    }
}
