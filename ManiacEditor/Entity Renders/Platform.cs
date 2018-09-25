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
    public class Platform
    {

        public void Draw(DevicePanel d, SceneEntity entity, EditorEntity e, int x, int y, int Transparency)
        {
            int frameID = 0;
            int targetFrameID = -1;
            var attribute = entity.attributesMap["frameID"];
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
                if (editorAnim.Frames[0].Entry.FrameSpeed > 0)
                    frame = editorAnim.Frames[e.index];
                else
                    frame = editorAnim.Frames[frameID > 0 ? frameID : 0];
                e.ProcessAnimation(frame.Entry.FrameSpeed, frame.Entry.Frames.Count, frame.Frame.Duration);
                d.DrawBitmap(frame.Texture, x + frame.Frame.CenterX, y + frame.Frame.CenterY,
                    frame.Frame.Width, frame.Frame.Height, false, Transparency);
            }
        }
    }
}
