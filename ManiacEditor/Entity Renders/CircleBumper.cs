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
    public class CircleBumper
    {

        public void Draw(DevicePanel d, SceneEntity entity, EditorEntity e, int x, int y, int Transparency)
        {
            //int type = (int)entity.attributesMap["type"].ValueInt32;
            int angle = (int)entity.attributesMap["angle"].ValueInt32;
            int amplitudeX = (int)entity.attributesMap["amplitude"].ValuePosition.X.High;
            int amplitudeY = (int)entity.attributesMap["amplitude"].ValuePosition.Y.High;
            int angleStateX = 0;
            int angleStateY = 0;
            bool fliph = false;
            bool flipv = false;
            int animID = 0;
            var editorAnim = e.LoadAnimation2("CircleBumper", d, animID, -1, fliph, flipv, false);
            if (editorAnim != null && editorAnim.Frames.Count != 0 && animID >= 0)
            {

                var frame = editorAnim.Frames[e.index];
                if (amplitudeX != 0 || amplitudeY != 0)
                {
                    angleStateX = (int)((frame.Frame.CenterX + amplitudeX) * Math.Cos(Math.PI * angle / 128) + (frame.Frame.CenterY + amplitudeY) * Math.Sin(Math.PI * angle / 128));
                    angleStateY = (int)((frame.Frame.CenterX + amplitudeX) * Math.Sin(Math.PI * angle / 128) - (frame.Frame.CenterY + amplitudeY) * Math.Cos(Math.PI * angle / 128));
                }

                //e.ProcessAnimation(frame.Entry.FrameSpeed, frame.Entry.Frames.Count, frame.Frame.Duration);

                d.DrawBitmap(frame.Texture,
                    x  + frame.Frame.CenterX + (angleStateX),
                    y  + frame.Frame.CenterY - (angleStateY),
                    frame.Frame.Width, frame.Frame.Height, false, Transparency);
            }
        }
    }
}
