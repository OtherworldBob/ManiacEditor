using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;
using ManiacEditor;
using Microsoft.Xna.Framework;
using RSDKv5;

namespace ManiacEditor.Entity_Renders
{
    public class HangPoint
    {

        public void Draw(DevicePanel d, SceneEntity entity, EditorEntity e, int x, int y, int Transparency)
        {
            int length = (int)entity.attributesMap["length"].ValueVar;
            bool fliph = false;
            bool flipv = false;
            var editorAnim = e.LoadAnimation2("HangPoint", d, 0, 0, fliph, flipv, false);
            var editorAnim2 = e.LoadAnimation2("HangPoint", d, 0, 1, fliph, flipv, false);
            if (editorAnim != null && editorAnim.Frames.Count != 0 && editorAnim2 != null && editorAnim2.Frames.Count != 0)
            {
                var frame = editorAnim.Frames[0];
                var frame2 = editorAnim2.Frames[0];
                int i = 0;
                int lengthMemory = length;

                //e.ProcessAnimation(frame.Entry.FrameSpeed, frame.Entry.Frames.Count, frame.Frame.Duration);
                /* if (length >= 1)
                    {
                    int repeat = 0;

                    for (bool a = false; a == true;)
                    {
                        if (lengthMemory > 256)
                        {
                            repeat++;
                        }
                        else
                        {
                            a = true;
                        }
                    }
                    for (i = 0; i < repeat; i++)
                    {
                        d.DrawBitmap(frame2.Texture,
                            x + frame2.Frame.CenterX - (fliph ? (frame2.Frame.Width - editorAnim2.Frames[0].Frame.Width) : 0),
                            y + (256*i) + frame2.Frame.CenterY + (flipv ? (frame2.Frame.Height - editorAnim2.Frames[0].Frame.Height) : 0),
                            frame2.Frame.Width, (256 * i), false, Transparency);
                    }
                    if (lengthMemory <= 256 && lengthMemory != 0)
                    {
                        d.DrawBitmap(frame2.Texture,
                            x + frame2.Frame.CenterX - (fliph ? (frame2.Frame.Width - editorAnim2.Frames[0].Frame.Width) : 0),
                            y + (i * 256) + frame2.Frame.CenterY + (flipv ? (frame2.Frame.Height - editorAnim2.Frames[0].Frame.Height) : 0),
                            frame2.Frame.Width, lengthMemory, false, Transparency);
                    }

                }*/

                d.DrawBitmap(frame.Texture,
                    x + frame.Frame.CenterX - (fliph ? (frame.Frame.Width - editorAnim.Frames[0].Frame.Width) : 0),
                    y + /*(256*i) - lengthMemory +*/ frame.Frame.CenterY + (flipv ? (frame.Frame.Height - editorAnim.Frames[0].Frame.Height) : 0),
                    frame.Frame.Width, frame.Frame.Height, false, Transparency);
            }
        }
    }
}
