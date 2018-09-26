using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;
using ManiacEditor;
using Microsoft.Xna.Framework;
using RSDKv5;

namespace ManiacEditor.Entity_Renders
{
    public class PimPom
    {

        public void Draw(DevicePanel d, SceneEntity entity, EditorEntity e, int x, int y, int Transparency)
        {
            int type = (int)entity.attributesMap["type"].ValueVar;
            int color = (int)entity.attributesMap["color"].ValueUInt8;
            int direction = (int)entity.attributesMap["direction"].ValueUInt8;
            int angle = (int)entity.attributesMap["angle"].ValueInt32;
            int length = (int)entity.attributesMap["length"].ValueUInt8;
            int gap = (int)entity.attributesMap["gap"].ValueUInt8;
            bool fliph = false;
            bool flipv = false;
            int animID;
            int frameID;
            switch (type)
            {
                case 0:
                    animID = 0;
                    switch (color)
                    {
                        case 0:
                            frameID = 0;
                            break;
                        case 1:
                            frameID = 1;
                            break;
                        case 2:
                            frameID = 2;
                            break;
                        default:
                            frameID = 0;
                            break;
                    }
                    break;
                case 1:
                    animID = 1;
                    frameID = 0;
                    break;
                case 2:
                    animID = 2;
                    frameID = 0;
                    break;
                case 3:
                    animID = 3;
                    frameID = 0;
                    break;
                default:
                    animID = 0;
                    frameID = 0;
                    break;

            }
            if (direction == 1)
            {
                fliph = true;
            }
            switch (angle)
            {
                case 0:
                    break;
                case 1:
                    animID = 2;
                    fliph = true;
                    break;
                case 2:
                    animID = 2;
                    flipv = true;
                    break;
                case 3:
                    animID = 2;
                    flipv = true;
                    fliph = true;
                    break;
                default:
                    break;
            }
            var editorAnim = e.LoadAnimation2("PimPom", d, animID, frameID, fliph, flipv, false);
            if (editorAnim != null && editorAnim.Frames.Count != 0 && animID >= 0)
            {
                var frame = editorAnim.Frames[e.index];

                //e.ProcessAnimation(frame.Entry.FrameSpeed, frame.Entry.Frames.Count, frame.Frame.Duration);


                    d.DrawBitmap(frame.Texture,
                        x + frame.Frame.CenterX - (fliph ? (frame.Frame.Width - editorAnim.Frames[0].Frame.Width) : 0),
                        y + frame.Frame.CenterY + (flipv ? (frame.Frame.Height - editorAnim.Frames[0].Frame.Height) : 0),
                        frame.Frame.Width, frame.Frame.Height, false, Transparency);

            }
        }
    }
}
