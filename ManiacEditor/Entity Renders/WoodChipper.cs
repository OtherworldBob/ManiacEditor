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
    public class WoodChipper : EntityRenderer
    {

        public override void Draw(DevicePanel d, SceneEntity entity, EditorEntity e, int x, int y, int Transparency)
        {
            bool fliph = false;
            bool flipv = false;
            var editorAnim = e.LoadAnimation2("WoodChipper", d, 0, -1, fliph, flipv, false);
            if (editorAnim != null && editorAnim.Frames.Count != 0)
            {
                var frame = editorAnim.Frames[0];
                var frame2 = editorAnim.Frames[1];
                var frame3 = editorAnim.Frames[2];
                var frame4 = editorAnim.Frames[6];

                //e.ProcessAnimation(frame.Entry.FrameSpeed, frame.Entry.Frames.Count, frame.Frame.Duration);

                d.DrawBitmap(frame.Texture,
                    x + frame.Frame.CenterX - (fliph ? (frame.Frame.Width - editorAnim.Frames[0].Frame.Width) : 0),
                    y + frame.Frame.CenterY + (flipv ? (frame.Frame.Height - editorAnim.Frames[0].Frame.Height) : 0),
                    frame.Frame.Width, frame.Frame.Height, false, Transparency);
                d.DrawBitmap(frame2.Texture,
                    x + frame2.Frame.CenterX - (fliph ? (frame2.Frame.Width - editorAnim.Frames[1].Frame.Width) : 0),
                    y + frame2.Frame.CenterY + (flipv ? (frame2.Frame.Height - editorAnim.Frames[1].Frame.Height) : 0),
                    frame2.Frame.Width, frame2.Frame.Height, false, Transparency);
                d.DrawBitmap(frame3.Texture,
                    x + frame3.Frame.CenterX - (fliph ? (frame3.Frame.Width - editorAnim.Frames[2].Frame.Width) : 0),
                    y + frame3.Frame.CenterY + (flipv ? (frame3.Frame.Height - editorAnim.Frames[2].Frame.Height) : 0),
                    frame3.Frame.Width, frame3.Frame.Height, false, Transparency);
                d.DrawBitmap(frame4.Texture,
                    x + frame4.Frame.CenterX - (fliph ? (frame4.Frame.Width - editorAnim.Frames[6].Frame.Width) : 0),
                    y + frame4.Frame.CenterY + (flipv ? (frame4.Frame.Height - editorAnim.Frames[6].Frame.Height) : 0),
                    frame4.Frame.Width, frame4.Frame.Height, false, Transparency);
            }
        }

        public override string GetObjectName()
        {
            return "WoodChipper";
        }
    }
}
