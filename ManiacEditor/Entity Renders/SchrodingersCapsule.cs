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
    public class SchrodingersCapsule : EntityRenderer
    {
        SchrodingersCapsulePart2 part2 = new SchrodingersCapsulePart2();
        SchrodingersCapsulePart1 part1 = new SchrodingersCapsulePart1();

        public override void Draw(DevicePanel d, SceneEntity entity, EditorEntity e, int x, int y, int Transparency)
        {
            bool fliph = false;
            bool flipv = false;
            var editorAnim = e.LoadAnimation2("SchrodingersCapsule", d, 0, 0, fliph, flipv, false);
            var editorAnimInside = e.LoadAnimation2("SchrodingersCapsule", d, 0, 2, fliph, flipv, false);
            var editorAnimExclamation = e.LoadAnimation2("SchrodingersCapsule", d, 0, 3, fliph, flipv, false);
            var editorAnimButton = e.LoadAnimation2("SchrodingersCapsule", d, 1, -1, fliph, flipv, false);

            if (editorAnim != null && editorAnim.Frames.Count != 0 && editorAnimButton != null && editorAnimButton.Frames.Count != 0 && editorAnimInside != null && editorAnimInside.Frames.Count != 0 && editorAnimExclamation != null && editorAnimExclamation.Frames.Count != 0)
            {
                var frame = editorAnim.Frames[e.index];
                var insideFrame = editorAnimInside.Frames[e.index];
                var exclamationFrame = editorAnimExclamation.Frames[e.index];
                var buttonFrame = editorAnimButton.Frames[e.index];

                part1.Draw(d, entity, e, x, y, Transparency);
                part2.Draw(d, entity, e, x, y, Transparency);


                d.DrawBitmap(buttonFrame.Texture,
                    x + buttonFrame.Frame.CenterX - (fliph ? (buttonFrame.Frame.Width - editorAnimButton.Frames[0].Frame.Width) : 0),
                    y + exclamationFrame.Frame.CenterY + (flipv ? (buttonFrame.Frame.Height - editorAnimButton.Frames[0].Frame.Height) : 0),
                    buttonFrame.Frame.Width, buttonFrame.Frame.Height, false, Transparency);
                d.DrawBitmap(exclamationFrame.Texture,
                    x + exclamationFrame.Frame.CenterX - (fliph ? (exclamationFrame.Frame.Width - editorAnimExclamation.Frames[3].Frame.Width) : 0),
                    y + exclamationFrame.Frame.CenterY + (flipv ? (exclamationFrame.Frame.Height - editorAnimExclamation.Frames[3].Frame.Height) : 0),
                    exclamationFrame.Frame.Width, exclamationFrame.Frame.Height, false, Transparency);
                d.DrawBitmap(insideFrame.Texture,
                    x + insideFrame.Frame.CenterX - (fliph ? (insideFrame.Frame.Width - editorAnimInside.Frames[2].Frame.Width) : 0),
                    y + insideFrame.Frame.CenterY + (flipv ? (insideFrame.Frame.Height - editorAnimInside.Frames[2].Frame.Height) : 0),
                    insideFrame.Frame.Width, insideFrame.Frame.Height, false, Transparency);

                d.DrawBitmap(frame.Texture,
                    x + frame.Frame.CenterX - (fliph ? (frame.Frame.Width - editorAnim.Frames[0].Frame.Width) : 0),
                    y + frame.Frame.CenterY + (flipv ? (frame.Frame.Height - editorAnim.Frames[0].Frame.Height) : 0),
                    frame.Frame.Width, frame.Frame.Height, false, Transparency);
                d.DrawBitmap(frame.Texture,
                    x + frame.Frame.CenterX - (fliph ? (frame.Frame.Width - editorAnim.Frames[0].Frame.Width) : 0),
                    y + frame.Frame.CenterY + (flipv ? (frame.Frame.Height - editorAnim.Frames[0].Frame.Height) : 0),
                    frame.Frame.Width, frame.Frame.Height, false, Transparency);
            }

        }

        public override string GetObjectName()
        {
            return "SchrodingersCapsule";
        }
    }
}
