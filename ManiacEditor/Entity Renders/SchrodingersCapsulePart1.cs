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
    public class SchrodingersCapsulePart1
    {

        public void Draw(DevicePanel d, SceneEntity entity, EditorEntity e, int x, int y, int Transparency)
        {
            bool fliph = false;
            bool flipv = false;
            var mightyAnim = e.LoadAnimation2("SchrodingersCapsule", d, 3, -1, fliph, flipv, false);
            var rayAnim = e.LoadAnimation2("SchrodingersCapsule", d, 4, -1, fliph, flipv, false);

                if (mightyAnim != null && mightyAnim.Frames.Count != 0 && rayAnim != null && rayAnim.Frames.Count != 0)
                {
                    var mightyFrame = mightyAnim.Frames[e.index];
                    var rayFrame = rayAnim.Frames[e.index];

                    //e.ProcessAnimation(rayFrame.Entry.FrameSpeed, rayFrame.Entry.Frames.Count, rayFrame.Frame.Duration);
                    //e.ProcessAnimation(mightyFrame.Entry.FrameSpeed, mightyFrame.Entry.Frames.Count, mightyFrame.Frame.Duration);

                    d.DrawBitmap(mightyFrame.Texture,
                        x + mightyFrame.Frame.CenterX - (fliph ? (mightyFrame.Frame.Width - mightyAnim.Frames[0].Frame.Width) : 0) + 15,
                        y + mightyFrame.Frame.CenterY + (flipv ? (mightyFrame.Frame.Height - mightyAnim.Frames[0].Frame.Height) : 0),
                        mightyFrame.Frame.Width, mightyFrame.Frame.Height, false, Transparency);
                    d.DrawBitmap(rayFrame.Texture,
                        x + rayFrame.Frame.CenterX - (fliph ? (rayFrame.Frame.Width - rayAnim.Frames[0].Frame.Width) : 0) - 15,
                        y + rayFrame.Frame.CenterY + (flipv ? (rayFrame.Frame.Height - rayAnim.Frames[0].Frame.Height) : 0),
                        rayFrame.Frame.Width, rayFrame.Frame.Height, false, Transparency);
                }


        }
    }
}
