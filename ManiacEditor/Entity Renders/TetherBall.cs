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
    public class TetherBall : EntityRenderer
    {
        //TODO: Get the Angle Calculations Correct
        public override void Draw(DevicePanel d, SceneEntity entity, EditorEntity e, int x, int y, int Transparency)
        {
            int type = (int)entity.attributesMap["type"].ValueUInt8;
            int angleStart = (int)entity.attributesMap["angleStart"].ValueVar;
            int angleEnd = (int)entity.attributesMap["angleEnd"].ValueVar;
            int chainCount = (int)entity.attributesMap["chainCount"].ValueUInt8;
            bool fliph = false;
            bool flipv = false;
            //bool drawType = true;
            int animID;
            switch (type)
            {
                case 0:
                    animID = 0;
                    break;
                case 1:
                    animID = 0;
                    flipv = true;
                    break;
                case 2:
                    animID = 1;
                    break;
                case 3:
                    animID = 1;
                    flipv = true;
                    break;
                default:
                    animID = 0;
                    //drawType = false;
                    break;

            }
            var editorAnim = e.LoadAnimation2("TetherBall", d, 0, animID, fliph, flipv, false);
            var editorAnim2 = e.LoadAnimation2("TetherBall", d, 0, 2, fliph, flipv, false);
            var editorAnim3 = e.LoadAnimation2("TetherBall", d, 0, 3, fliph, flipv, false);
            if (editorAnim != null && editorAnim.Frames.Count != 0 && editorAnim2 != null && editorAnim2.Frames.Count != 0 && editorAnim3 != null && editorAnim3.Frames.Count != 0)
            {
                var frame = editorAnim.Frames[e.index];
                var frame2 = editorAnim2.Frames[e.index];
                var frame3 = editorAnim3.Frames[e.index];

                int length = (frame2.Frame.Width * chainCount) - (frame2.Frame.Width/2 + 7);
                int[] processPoints;
                if (type == 2)
                {
                    processPoints = RotatePoints(x + length, y + length, x, y, angleStart - 32);
                }
                else if (type == 1)
                {
                    processPoints = RotatePoints(x + length, y + length, x, y, angleStart - 96);
                }
                else
                {
                    processPoints = RotatePoints(x + length, y + length, x, y, angleStart);
                }

                int segmentsX = processPoints[0] / chainCount;
                int segmentsY = processPoints[1] / chainCount;

                // TetherBall Center
                d.DrawBitmap(frame.Texture,
                    x + frame.Frame.CenterX - (fliph ? (frame2.Frame.Width - editorAnim2.Frames[0].Frame.Width) : 0),
                    y + frame.Frame.CenterY + (flipv ? (frame2.Frame.Height - editorAnim2.Frames[0].Frame.Height) : 0),
                    frame.Frame.Width, frame.Frame.Height, false, Transparency);

                // TetherBall Line
                /*
                for (int i = 1; i < chainCount+1; i++)
                {
                    d.DrawBitmap(frame2.Texture,
                        x + segmentsX*i + frame2.Frame.CenterX - (fliph ? (frame2.Frame.Width - editorAnim2.Frames[0].Frame.Width) : 0),
                        y + segmentsY*i + frame2.Frame.CenterY + (flipv ? (frame2.Frame.Height - editorAnim2.Frames[0].Frame.Height) : 0),
                        frame2.Frame.Width, frame2.Frame.Height, false, Transparency);
                }
                */


                //TetherBall Ball
                d.DrawBitmap(frame3.Texture,
                    processPoints[0] + frame3.Frame.CenterX - (fliph ? (frame3.Frame.Width - editorAnim3.Frames[0].Frame.Width) : 0),
                    processPoints[1] + frame3.Frame.CenterY + (flipv ? (frame3.Frame.Height - editorAnim3.Frames[0].Frame.Height) : 0),
                    frame3.Frame.Width, frame3.Frame.Height, false, Transparency);
            }
        }
        private static int[] RotatePoints(double initX, double initY, double centerX, double centerY, int angle)
        {
            initX -= centerX;
            initY -= centerY;

            if (initX == 0 && initY == 0)
            {
                int[] results2 = { (int)centerX, (int)centerY };
                return results2;
            }

            const double FACTOR = 40.743665431525205956834243423364;

            double hypo = Math.Sqrt(Math.Pow(initX, 2) + Math.Pow(initY, 2));
            double initAngle = Math.Acos(initX / hypo);
            if (initY < 0) initAngle = 2 * Math.PI - initAngle;
            double newAngle = initAngle - angle / FACTOR;
            double finalX = hypo * Math.Cos(newAngle) + centerX;
            double finalY = hypo * Math.Sin(newAngle) + centerY;

            int[] results = { (int)Math.Round(finalX), (int)Math.Round(finalY) };
            return results;
        }

        public override string GetObjectName()
        {
            return "TetherBall";
        }
    }
}
