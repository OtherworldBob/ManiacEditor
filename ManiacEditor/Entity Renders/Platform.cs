using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using ManiacEditor;
using Microsoft.Xna.Framework;
using RSDKv5;
using System.Windows.Forms;
using System.Drawing;

namespace ManiacEditor.Entity_Renders
{
    public class Platform
    {
        
        public void Draw(DevicePanel d, SceneEntity entity, EditorEntity e, int x, int y, int Transparency)
        {
            int frameID = 0;
            int targetFrameID = -1;
            var attribute = entity.attributesMap["frameID"];
            int angle = (int)entity.attributesMap["angle"].ValueInt32;
            int angleRotate = (int)entity.attributesMap["angle"].ValueInt32;
            int type = (int)entity.attributesMap["type"].ValueVar;
            int amplitudeX = (int)entity.attributesMap["amplitude"].ValuePosition.X.High;
            int amplitudeY = (int)entity.attributesMap["amplitude"].ValuePosition.Y.High;
            int angleStateX = 0;
            int angleStateY = 0;
            switch (attribute.Type)
            {
                case AttributeTypes.UINT8:
                    targetFrameID = attribute.ValueUInt8;
                    break;
                case AttributeTypes.INT8:
                    targetFrameID = attribute.ValueInt8;
                    break;
                case AttributeTypes.VAR:
                    targetFrameID = (int)attribute.ValueVar;
                    break;
            }

            int aminID = 0;
            EditorEntity.EditorAnimation editorAnim = null;
            while (true)
            {

                try
                {
                    editorAnim = e.LoadAnimation("Platform", d, aminID, -1, false, false, false);

                    if (editorAnim == null) return; // no animation, bail out

                    frameID += editorAnim.Frames.Count;
                    if (targetFrameID < frameID)
                    {
                        int aminStart = (frameID - editorAnim.Frames.Count);
                        frameID = targetFrameID - aminStart;
                        break;
                    }
                    aminID++;
                }
                catch (Exception i)
                {
                    throw new ApplicationException($"Pop Loading Platforms! {aminID}", i);
                }
            }
            if (editorAnim.Frames.Count != 0)
            {
                EditorEntity.EditorAnimation.EditorFrame frame = null;
                EditorEntity.EditorAnimation.EditorFrame frame2 = null;
                if (editorAnim.Frames[0].Entry.FrameSpeed > 0)
                {
                    frame = editorAnim.Frames[e.index];
                    frame2 = editorAnim.Frames[0];
                }
                else
                {
                    frame = editorAnim.Frames[frameID > 0 ? frameID : 0];
                    frame2 = editorAnim.Frames[0];
                }

                e.ProcessAnimation(frame.Entry.FrameSpeed, frame.Entry.Frames.Count, frame.Frame.Duration);

                if ((amplitudeX != 0 || amplitudeY != 0) && type == 2 && e.Selected)
                {
                    d.DrawBitmap(frame.Texture, x + frame.Frame.CenterX + amplitudeX, y + frame.Frame.CenterY + amplitudeY,
                        frame.Frame.Width, frame.Frame.Height, false, 125);
                    d.DrawBitmap(frame.Texture, x + frame.Frame.CenterX - amplitudeX, y + frame.Frame.CenterY - amplitudeY,
                        frame.Frame.Width, frame.Frame.Height, false, 125);
                    d.DrawBitmap(frame.Texture, x + frame.Frame.CenterX, y + frame.Frame.CenterY,
                        frame.Frame.Width, frame.Frame.Height, false, Transparency);
                }
                /*if ((amplitudeX != 0 || amplitudeY != 0) && type == 2)
                {
                    e.ProcessMovingPlatform2(amplitudeX,amplitudeY);
                    d.DrawBitmap(frame.Texture, x + frame.Frame.CenterX + e.positionX, y + frame.Frame.CenterY - e.positionY,
                        frame.Frame.Width, frame.Frame.Height, false, Transparency);
                }*/
                else if ((amplitudeX != 0 || amplitudeY != 0) && type == 3)
                {
                    e.ProcessMovingPlatform(angle);
                    angle = e.angle;
                    double xd = x;
                    double yd = y;
                    double x2 = x + amplitudeX - amplitudeX / 3.7;
                    double y2 = y + amplitudeY - amplitudeY / 3.7;
                    double radius = Math.Pow(x2 - xd, 2) + Math.Pow(y2 - yd, 2);
                    int radiusInt = (int)Math.Sqrt(radius);
                    int newX = (int)(radiusInt * Math.Cos(Math.PI * angle / 128));
                    int newY = (int)(radiusInt * Math.Sin(Math.PI * angle / 128));
                    d.DrawBitmap(frame.Texture, (x + newX) + frame.Frame.CenterX, (y - newY) + frame.Frame.CenterY,
                       frame.Frame.Width, frame.Frame.Height, false, Transparency);
                }
                else
                {

                    d.DrawBitmap(frame.Texture, x + frame.Frame.CenterX + angleStateX, y + frame.Frame.CenterY - angleStateY,
                        frame.Frame.Width, frame.Frame.Height, false, Transparency);
                }

            }


        }
    }
}
