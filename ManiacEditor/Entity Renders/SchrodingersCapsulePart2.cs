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
    public class SchrodingersCapsulePart2
    {

        public void Draw(DevicePanel d, SceneEntity entity, EditorEntity e, int x, int y, int Transparency)
        {
            bool fliph = false;
            bool flipv = false;
            var editorAnimGlass = e.LoadAnimation2("SchrodingersCapsule", d, 2, -1, fliph, flipv, false);

                if (editorAnimGlass != null && editorAnimGlass.Frames.Count != 0)
                {
                    var glassFrame = editorAnimGlass.Frames[e.index];

                    e.ProcessAnimation(glassFrame.Entry.FrameSpeed, glassFrame.Entry.Frames.Count, glassFrame.Frame.Duration);

                    d.DrawBitmap(glassFrame.Texture,
                        x + glassFrame.Frame.CenterX - (fliph ? (glassFrame.Frame.Width - editorAnimGlass.Frames[0].Frame.Width) : 0),
                        y + glassFrame.Frame.CenterY + (flipv ? (glassFrame.Frame.Height - editorAnimGlass.Frames[0].Frame.Height) : 0),
                        glassFrame.Frame.Width, glassFrame.Frame.Height, false, 50);
                }
        }
    }
}
