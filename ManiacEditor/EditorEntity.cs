using ManiacEditor.Enums;
using RSDKv5;
using SharpDX.Direct3D9;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Windows.Forms;

namespace ManiacEditor
{
    [Serializable]
    public class EditorEntity : IDrawable
    {
        protected const int NAME_BOX_WIDTH  = 20;
        protected const int NAME_BOX_HEIGHT = 20;

        protected const int NAME_BOX_HALF_WIDTH  = NAME_BOX_WIDTH  / 2;
        protected const int NAME_BOX_HALF_HEIGHT = NAME_BOX_HEIGHT / 2;

        public bool Selected;

        private SceneEntity entity;
        private bool filteredOut;

        public static Dictionary<string, EditorAnimation> Animations = new Dictionary<string, EditorAnimation>();
        public static Dictionary<string, Bitmap> Sheets = new Dictionary<string, Bitmap>();
        public static string[] DataDirectoryList = null;
        public static bool Working = false;
        public Animation rsdkAnim;
        public DateTime lastFrametime;
        public int index = 0;
        public SceneEntity Entity { get { return entity; } }

        public EditorEntity(SceneEntity entity)
        {
            this.entity = entity;
            lastFrametime = DateTime.Now;
        }
 
        public void Draw(Graphics g)
        {
        }

        public bool ContainsPoint(Point point)
        {
            if (filteredOut) return false;

            return GetDimensions().Contains(point);
        }

        public bool IsInArea(Rectangle area)
        {
            if (filteredOut) return false;

            return GetDimensions().IntersectsWith(area);
        }

        public void Move(Point diff)
        {
            entity.Position.X.High += (short)diff.X;
            entity.Position.Y.High += (short)diff.Y;
            //Since the Editor can now update without the use of this render, I removed it
            //if (Properties.Settings.Default.AllowMoreRenderUpdates == true) Editor.Instance.UpdateRender();
            if (Editor.GameRunning)
            {
                // TODO: Find out if this is constent
                int ObjectStart = 0x00A5DCC0;
                int ObjectSize = 0x458;
                int ObbjectAddress = ObjectStart + (ObjectSize * entity.SlotID);
                Editor.GameMemory.WriteInt16(ObbjectAddress + 2, entity.Position.X.High);
                Editor.GameMemory.WriteInt16(ObbjectAddress + 6, entity.Position.Y.High);
            }
        }

        public void SnapToGrid(Point diff)
        {
            entity.Position.X.High = (short)((diff.X + 8) / 16 * 16);
            entity.Position.Y.High = (short)((diff.Y + 8) / 16 * 16);
            if (Editor.GameRunning)
            {
                // TODO: Find out if this is constent
                int ObjectStart = 0x00A5DCC0;
                int ObjectSize = 0x458;
                int ObbjectAddress = ObjectStart + (ObjectSize * entity.SlotID);
                Editor.GameMemory.WriteInt16(ObbjectAddress + 2, entity.Position.X.High);
                Editor.GameMemory.WriteInt16(ObbjectAddress + 6, entity.Position.Y.High);
            }
        }

        public Rectangle GetDimensions()
        {
            return new Rectangle(entity.Position.X.High, entity.Position.Y.High, NAME_BOX_WIDTH, NAME_BOX_HEIGHT);
        }

        public Bitmap CropImage(Bitmap source, Rectangle section, bool fliph, bool flipv)
        {
            Bitmap bmp2 = new Bitmap(section.Size.Width, section.Size.Height);
            using (Graphics gg = Graphics.FromImage(bmp2))
                gg.DrawImage(source, 0, 0, section, GraphicsUnit.Pixel);
            if (fliph && flipv)
                bmp2.RotateFlip(RotateFlipType.RotateNoneFlipXY);
            else if (fliph)
                bmp2.RotateFlip(RotateFlipType.RotateNoneFlipX);
            else if (flipv)
                bmp2.RotateFlip(RotateFlipType.RotateNoneFlipY);
            int size = ((section.Width > section.Height ? section.Width : section.Height) / 64) * 64;
            Bitmap bmp = new Bitmap(1024, 1024);
            using (Graphics g = Graphics.FromImage(bmp))
                g.DrawImage(bmp2, 0, 0, new Rectangle(0, 0, bmp2.Width, bmp2.Height), GraphicsUnit.Pixel);
            return bmp;
        }

        public Bitmap RemoveColourImage(Bitmap source, System.Drawing.Color colour, int width, int height)
        {
            for (int x = 0; x < width; ++x)
            {
                for (int y = 0; y < height; ++y)
                {
                    if (source.GetPixel(x, y) == colour)
                        source.SetPixel(x, y, System.Drawing.Color.Transparent);
                }
            }
            return source;
        }

        public List<LoadAnimationData> AnimsToLoad = new List<LoadAnimationData>();
        
        public void LoadNextAnimation()
        {
            if (AnimsToLoad.Count == 0)
                return;
            var val = AnimsToLoad[0];
            if (val.anim == null)
            {
                string key = $"{val.name}-{val.AnimId}-{val.frameId}-{val.fliph}-{val.flipv}-{val.rotate}";
                if (!Animations.ContainsKey(key))
                {
                    if (!Working)
                    {
                        try
                        {
                            LoadAnimation(val.name, val.d, val.AnimId, val.frameId, val.fliph, val.flipv, val.rotate, false);
                        }
                        catch (Exception)
                        {
                            // lots of changes introduced by Plus, just hide errors for now (evil I know!)
                            // Console.WriteLine($"Pop loading next animiation. {val.name}, {val.AnimId}, {val.frameId}, {val.fliph}, {val.flipv}, {val.rotate}", e);
                        }
                    }
                }
                else
                {
                    val.anim = Animations[key];
                }

            }
            if (val.anim == null)
                return;
            if (val.anim.Ready)
                AnimsToLoad.RemoveAt(0);
            else
            {
                if (val.anim.Frames.Count == 0)
                {
                    val.anim.Ready = true;
                    AnimsToLoad.RemoveAt(0);
                    return;
                }
                val.anim.Frames[val.anim.loadedFrames].Texture = TextureCreator.FromBitmap(val.d._device, val.anim.Frames[val.anim.loadedFrames]._Bitmap);
                val.anim.Frames[val.anim.loadedFrames]._Bitmap.Dispose();
                ++val.anim.loadedFrames;
                if (val.anim.loadedFrames == val.anim.Frames.Count)
                    val.anim.Ready = true;
            }
        }

        [Serializable]
        public class LoadAnimationData
        {
            public string name;
            public DevicePanel d;
            public int AnimId, frameId;
            public bool fliph, flipv, rotate;
            public EditorAnimation anim;
        }

        /// <summary>
        /// Loads / Gets the Sprite Animation
        /// NOTE: 
        /// </summary>
        /// <param name="name">The Name of the object</param>
        /// <param name="d">The DevicePanel</param>
        /// <param name="AnimId">The Animation ID (-1 to Load Normal)</param>
        /// <param name="frameId">The Frame ID for the specified Animation (-1 to load all frames)</param>
        /// <param name="fliph">Flip the Texture Horizontally</param>
        /// <param name="flipv">Flip the Texture Vertically</param>
        /// <returns>The fully loaded Animation</returns>
        public EditorAnimation LoadAnimation2(string name, DevicePanel d, int AnimId, int frameId, bool fliph, bool flipv, bool rotate)
        {
            string key = $"{name}-{AnimId}-{frameId}-{fliph}-{flipv}-{rotate}";
            if (Animations.ContainsKey(key))
            {
                if (Animations[key].Ready)
                {
                    // Use the already loaded Amination
                    return Animations[key];
                }
                else
                    return null;
            }
            var entry = new LoadAnimationData()
            {
                name = name,
                d = d,
                AnimId = AnimId,
                frameId = frameId,
                fliph = fliph,
                flipv = flipv,
                rotate = rotate
            };
            AnimsToLoad.Add(entry);
            return null;
        }

        /// <summary>
        /// Loads / Gets the Sprite Animation
        /// NOTE: 
        /// </summary>
        /// <param name="name">The Name of the object</param>
        /// <param name="d">The DevicePanel</param>
        /// <param name="AnimId">The Animation ID (-1 to Load Normal)</param>
        /// <param name="frameId">The Frame ID for the specified Animation (-1 to load all frames)</param>
        /// <param name="fliph">Flip the Texture Horizontally</param>
        /// <param name="flipv">Flip the Texture Vertically</param>
        /// <returns>The fully loaded Animation</returns>
        public EditorAnimation LoadAnimation(string name, DevicePanel d, int AnimId, int frameId, bool fliph, bool flipv, bool rotate, bool loadImageToDX = true)
        {

            string key = $"{name}-{AnimId}-{frameId}-{fliph}-{flipv}-{rotate}";
            var anim = new EditorAnimation();
            if (Animations.ContainsKey(key))
            {
                if (Animations[key].Ready)
                {
                    // Use the already loaded Amination
                    return Animations[key];
                }
                else
                {
                    return null;
                }
            }

            Animations.Add(key, anim);


            string path, path2;
            if (name == "EditorAssets")
            {
                path2 = Path.Combine(Environment.CurrentDirectory, "EditorAssets.bin");
                if (!File.Exists(path2))
                    return null;
            }
            else
            {
                if (DataDirectoryList == null)
                    DataDirectoryList = Directory.GetFiles(Path.Combine(Editor.DataDirectory, "Sprites"), $"*.bin", SearchOption.AllDirectories);

                // Checks Global frist
                path = Editor.Instance.SelectedZone + "\\" + name + ".bin";
                path2 = Path.Combine(Editor.DataDirectory, "sprites") + '\\' + path;
                if (!File.Exists(path2))
                {
                    // Checks without last character
                    path = path = Editor.Instance.SelectedZone.Substring(0, Editor.Instance.SelectedZone.Length - 1) + "\\" + name + ".bin";
                    path2 = Path.Combine(Editor.DataDirectory, "sprites") + '\\' + path;
                }
                if (!File.Exists(path2))
                {
                    // Checks Global
                    path = "Global\\" + name + ".bin";
                    path2 = Path.Combine(Editor.DataDirectory, "sprites") + '\\' + path;
                }
                if (!File.Exists(path2))
                {
                    // Checks the Stage folder 
                    foreach (string dir in Directory.GetDirectories(Path.Combine(Editor.DataDirectory, "Sprites"), $"*", SearchOption.TopDirectoryOnly))
                    {
                        path = Path.GetFileName(dir) + "\\" + name + ".bin";
                        path2 = Path.Combine(Editor.DataDirectory, "sprites") + '\\' + path;
                        if (File.Exists(path2))
                            break;
                    }
                }
                if (!File.Exists(path2))
                {
                    // Seaches all around the Data directory
                    var list = DataDirectoryList;
                    if (list.Any(t => Path.GetFileName(t.ToLower()).Contains(name.ToLower())))
                    {
                        list = list.Where(t => Path.GetFileName(t.ToLower()).Contains(name.ToLower())).ToArray();
                        if (list.Any(t => t.ToLower().Contains(Editor.Instance.SelectedZone)))
                            path2 = list.Where(t => t.ToLower().Contains(Editor.Instance.SelectedZone)).First();
                        else
                            path2 = list.First();
                    }
                }
                if (!File.Exists(path2))
                {
                    // No animation found
                    return null;
                }
            }

            using (var stream = File.OpenRead(path2))
            {
                rsdkAnim = new Animation();
                rsdkAnim.Load(new BinaryReader(stream));
            }
            if (AnimId == -1)
            {
                if (rsdkAnim.Animations.Any(t => t.AnimName.Contains("Normal")))
                    AnimId = rsdkAnim.Animations.FindIndex(t => t.AnimName.Contains("Normal"));
                else AnimId = 0;
                // Use Vertical Amination if one exists
                if (rotate && rsdkAnim.Animations.Any(t => t.AnimName.EndsWith(" V")))
                    AnimId = rsdkAnim.Animations.FindIndex(t => t.AnimName.EndsWith(" V"));
            }
            if (AnimId >= rsdkAnim.Animations.Count)
                AnimId = rsdkAnim.Animations.Count - 1;
            for (int i = 0; i < rsdkAnim.Animations[AnimId].Frames.Count; ++i)
            {
                // check we don't stray outside our loaded animations/frames
                // if user enters a value that would take us there, just show
                // a valid frame instead
                var animiation = rsdkAnim.Animations[AnimId];
                var frame = animiation.Frames[i];
                if (frameId >= 0 && frameId < animiation.Frames.Count)
                    frame = animiation.Frames[frameId];
                Bitmap map;
                if (!Sheets.ContainsKey(rsdkAnim.SpriteSheets[frame.SpriteSheet]))
                {
                    string targetFile;
                    if (name == "EditorAssets")
                        targetFile = Path.Combine(Environment.CurrentDirectory, "EditorAssets.gif");
                    else
                        targetFile = Path.Combine(Editor.DataDirectory, "sprites", rsdkAnim.SpriteSheets[frame.SpriteSheet].Replace('/', '\\'));
                    if (!File.Exists(targetFile))
                    {
                        map = null;
                        // add a Null to our lookup, so we can avoid looking again in the future
                        Sheets.Add(rsdkAnim.SpriteSheets[frame.SpriteSheet], map);
                    }
                    else
                    {
                        map = new Bitmap(targetFile);
                        Sheets.Add(rsdkAnim.SpriteSheets[frame.SpriteSheet], map);
                    }
                }
                else
                    map = Sheets[rsdkAnim.SpriteSheets[frame.SpriteSheet]];

                if (frame.Width == 0 || frame.Height == 0)
                    continue;

                // can't load the animation, it probably doesn't exist in the User's Sprites folder
                if (map == null) return null;

                // We are storing the first colour from the palette so we can use it to make sprites transparent
                var colour = map.Palette.Entries[0];
                // Slow
                map = CropImage(map, new Rectangle(frame.X, frame.Y, frame.Width, frame.Height), fliph, flipv);
                RemoveColourImage(map, colour, frame.Width, frame.Height);

                Texture texture = null;
                if (loadImageToDX)
                {
                    texture = TextureCreator.FromBitmap(d._device, map);
                }
                var editorFrame = new EditorAnimation.EditorFrame()
                {
                    Texture = texture,
                    Frame = frame,
                    Entry = rsdkAnim.Animations[AnimId]
                };
                if (!loadImageToDX)
                    editorFrame._Bitmap = map;
                anim.Frames.Add(editorFrame);
                if (frameId != -1)
                    break;
            }
            anim.ImageLoaded = true;
            if (loadImageToDX)
                anim.Ready = true;
            Working = false;
            return anim;

        }

        public bool SetFilter()
        {
            if (HasFilter())
            {
                int filter = entity.GetAttribute("filter").ValueUInt8;

                /**
                 * 1 or 5 = Both
                 * 2 = Mania
                 * 4 = Encore
                 */

                filteredOut =
                    ((filter == 1 || filter == 5) && !Properties.Settings.Default.showBothEntities) ||
                    (filter == 2 && !Properties.Settings.Default.showManiaEntities) ||
                    (filter == 4 && !Properties.Settings.Default.showEncoreEntities) ||
                    ((filter < 1 || filter == 3 || filter > 5) && !Properties.Settings.Default.showOtherEntities);

            }
            else
                filteredOut = !Properties.Settings.Default.showBothEntities;
            return filteredOut;
        }

        // allow derived types to override the draw
        public virtual void Draw(DevicePanel d)
        {
            if (filteredOut) return;


            if (Properties.Settings.Default.AlwaysRenderObjects == false)
            {
                //This causes some objects not to load ever, which is problamatic, so I made a toggle. It can also have some performance benifits
                if (!d.IsObjectOnScreen(entity.Position.X.High, entity.Position.Y.High, NAME_BOX_WIDTH, NAME_BOX_HEIGHT)) return;
            }
            System.Drawing.Color color = Selected ? System.Drawing.Color.MediumPurple : System.Drawing.Color.MediumTurquoise;
            System.Drawing.Color color2 = System.Drawing.Color.DarkBlue;
            int Transparency = (Editor.Instance.EditLayer == null) ? 0xff : 0x32;
            if (!Properties.Settings.Default.NeverLoadEntityTextures)
            {
                LoadNextAnimation();
            }
            int x = entity.Position.X.High;
            int y = entity.Position.Y.High;
            bool fliph = false;
            bool flipv = false;
            bool rotate = false;
            var offset = GetRotationFromAttributes(ref fliph, ref flipv, ref rotate);
            string name = entity.Object.Name.Name;
            var editorAnim = LoadAnimation2(name, d, -1, -1, fliph, flipv, rotate);
            if (name == "Spring" || name == "Player" || name == "Platform" ||
                name == "TimeAttackGate" || name == "Spikes" || name == "ItemBox" ||
                name == "Bridge" || name == "TeeterTotter" || name == "HUD" ||
                name == "Music" || name == "BoundsMarker" || name == "TitleCard" ||
                name == "CorkscrewPath" || name == "BGSwitch" || name == "ForceSpin" ||
                name == "UIControl" || name == "SignPost" || name == "UFO_Ring" ||
                name == "UFO_Sphere" || name == "UFO_Player" || name == "UFO_ItemBox" ||
                name == "UFO_Springboard" || name == "Decoration" || name == "WaterGush" ||
                name == "BreakBar" || name == "InvisibleBlock" || name == "ForceUnstick" ||
                name == "BreakableWall" || name == "CollapsingPlatform" || name == "PlaneSwitch" ||
                name == "ChemicalPool" || name == "Newtron" || name == "Chopper" || name == "TippingPlatform" || name == "Spiny" || name == "OneWayDoor"
                || name == "Syringe" || name == "StickyPlatform" || name == "TwistedTubes" || name == "ShopWindow")
            {
                if (!Properties.Settings.Default.NeverLoadEntityTextures)
                {
                    DrawOthers(d);
                }

            }
            else if (name == "DirectorChair" || name == "TVVan" || name == "FilmProjector" || name == "RockemSockem" || name == "Clapperboard" || name == "PopcornMachine" || name == "LEDPanel"
                || name == "SpinSign" || name == "EggTV" || name == "LottoMachine" || name == "Funnel" || name == "Letterboard" || name == "Ring" || name == "DNARiser" || name == "CaterkillerJr" || name == "Grabber" ||
                name == "SpinBooster" || name == "Water")
            {
                if (!Properties.Settings.Default.NeverLoadEntityTextures)
                {
                    DrawOthers(d);
                }

            }
            else if (editorAnim != null && editorAnim.Frames.Count > 0)
            {
                // Special cases that always display a set frame(?)
                if (Editor.Instance.ShowAnimations.Enabled == true)
                {
                    if (entity.Object.Name.Name == "StarPost")
                        index = 1;
                    //else if (entity.Object.Name.Name == "SpecialRing")
                    //    index = 9;
                }



                // Just incase
                if (index >= editorAnim.Frames.Count)
                    index = 0;
                var frame = editorAnim.Frames[index];

                if (entity.attributesMap.ContainsKey("frameID"))
                    frame = GetFrameFromAttribute(editorAnim, entity.attributesMap["frameID"]);
                
                if (frame != null)
                {
                    ProcessAnimation(frame.Entry.FrameSpeed, frame.Entry.Frames.Count, frame.Frame.Duration);
                    // Draw the normal filled Rectangle but Its visible if you have the entity selected
                        d.DrawBitmap(frame.Texture, x + frame.Frame.CenterX + ((int)offset.X * frame.Frame.Width), y + frame.Frame.CenterY + ((int)offset.Y * frame.Frame.Height),
                            frame.Frame.Width, frame.Frame.Height, false, Transparency);
                }
                else
                { // No frame to render
                    d.DrawRectangle(x, y, x + NAME_BOX_WIDTH, y + NAME_BOX_HEIGHT, System.Drawing.Color.FromArgb(Transparency, color));
                }
                // Draws the Special Objects
                //DrawOthers(d);
            }
            else
            {
                d.DrawRectangle(x, y, x + NAME_BOX_WIDTH, y + NAME_BOX_HEIGHT, System.Drawing.Color.FromArgb(Transparency, color));
            }

            d.DrawRectangle(x, y, x + NAME_BOX_WIDTH, y + NAME_BOX_HEIGHT, System.Drawing.Color.FromArgb(Selected ? 0x60 : 0x00, System.Drawing.Color.MediumPurple));
            d.DrawLine(x, y, x + NAME_BOX_WIDTH, y, System.Drawing.Color.FromArgb(Transparency, color2));
            d.DrawLine(x, y, x, y + NAME_BOX_HEIGHT, System.Drawing.Color.FromArgb(Transparency, color2));
            d.DrawLine(x, y + NAME_BOX_HEIGHT, x + NAME_BOX_WIDTH, y + NAME_BOX_HEIGHT, System.Drawing.Color.FromArgb(Transparency, color2));
            d.DrawLine(x + NAME_BOX_WIDTH, y, x + NAME_BOX_WIDTH, y + NAME_BOX_HEIGHT, System.Drawing.Color.FromArgb(Transparency, color2));
            if (Properties.Settings.Default.UseObjectRenderingImprovements == false)
                if (Editor.Instance.GetZoom() >= 1) d.DrawTextSmall(String.Format("{0} (ID: {1})", entity.Object.Name, entity.SlotID), x + 2, y + 2, NAME_BOX_WIDTH - 4, System.Drawing.Color.FromArgb(Transparency, System.Drawing.Color.Black), true);
        }

        public EditorAnimation.EditorFrame GetFrameFromAttribute(EditorAnimation anim, AttributeValue attribute, string key = "frameID")
        {
            int frameID = -1;
            switch (attribute.Type)
            {
                case AttributeTypes.UINT8:
                    frameID = entity.attributesMap[key].ValueUInt8;
                    break;
                case AttributeTypes.INT8:
                    frameID = entity.attributesMap[key].ValueInt8;
                    break;
                case AttributeTypes.VAR:
                    frameID = (int)entity.attributesMap[key].ValueVar;
                    break;
            }
            if (frameID >= anim.Frames.Count)
                frameID = (anim.Frames.Count - 1) - (frameID % anim.Frames.Count + 1);
            if (frameID < 0)
                frameID = 0;
            if (frameID >= 0 && frameID < int.MaxValue)
                return anim.Frames[frameID % anim.Frames.Count];
            else
                return null; // Don't Render the Animation
        }

        /// <summary>
        /// Guesses the rotation of an entity
        /// </summary>
        /// <param name="attributes">List of all Attributes</param>
        /// <param name="fliph">Reference to fliph</param>
        /// <param name="flipv">Reference to flipv</param>
        /// <returns>AnimationID Offset</returns>
        public SharpDX.Vector2 GetRotationFromAttributes(ref bool fliph, ref bool flipv, ref bool rotate)
        {
            AttributeValue attribute = null;
            var attributes = entity.attributesMap;
            int dir = 0;
            var offset = new SharpDX.Vector2();
            if (attributes.ContainsKey("orientation"))
            {
                attribute = attributes["orientation"];
                switch (attribute.Type)
                {
                    case AttributeTypes.UINT8:
                        dir = attribute.ValueUInt8;
                        break;
                    case AttributeTypes.INT8:
                        dir = attribute.ValueInt8;
                        break;
                    case AttributeTypes.VAR:
                        dir = (int) attribute.ValueVar;
                        break;
                }
                if (dir == 0) // Up
                {
                }
                else if (dir == 1) // Down
                {
                    fliph = true;
                    offset.X = 1;
                    flipv = true;
                    offset.Y = 1;
                }
                else if (dir == 2) // Right
                {
                    flipv = true;
                    rotate = true;
                    offset.Y = 1;
                    //animID = 1;
                }
                else if (dir == 3) // Left
                {
                    flipv = true;
                    rotate = true;
                    offset.Y = 1;
                    //animID = 1;
                }
            }
            if (attributes.ContainsKey("direction") && dir == 0)
            {
                attribute = attributes["direction"];
                switch (attribute.Type)
                {
                    case AttributeTypes.UINT8:
                        dir = attribute.ValueUInt8;
                        break;
                    case AttributeTypes.INT8:
                        dir = attribute.ValueInt8;
                        break;
                    case AttributeTypes.VAR:
                        dir = (int)attribute.ValueVar;
                        break;
                }
                if (dir == 0) // Right
                {
                }
                else if (dir == 1) // left
                {
                    fliph = true;
                    offset.X = 0;
                    rotate = true;
                }
                else if (dir == 2) // Up
                {
                    flipv = true;
                }
                else if (dir == 3) // Down
                {
                    flipv = true;
                    //offset.Y = 1;
                }
            }
            return offset;
        }

        /// <summary>
        /// Handles animation timing
        /// </summary>
        /// <param name="speed">Speed</param>
        /// <param name="frameCount">The total amount of frames</param>
        public void ProcessAnimation(int speed, int frameCount, int duration)
        {
            // Playback
            if (Editor.Instance.ShowAnimations.Checked)
            {
                if (speed > 0)
                {
                    int speed1 = speed * 64 / (duration == 0 ? 256 : duration);
                    if (speed1 == 0)
                        speed1 = 1;
                    if ((DateTime.Now - lastFrametime).TotalMilliseconds > 1024 / speed1)
                    {
                        index++;
                        lastFrametime = DateTime.Now;
                    }
                }
            }
            else index = 0;
            if (index >= frameCount)
                index = 0;
        }

        // These are special
        public void DrawOthers(DevicePanel d)
        {
            int x = entity.Position.X.High;
            int y = entity.Position.Y.High;
            int Transparency = (Editor.Instance.EditLayer == null) ? 0xff : 0x32;
            if (entity.Object.Name.Name == "ItemBox")
            {
                var value = entity.attributesMap["type"];
                int direction = (int)entity.attributesMap["direction"].ValueUInt8;
                bool fliph = false;
                bool flipv = false;
                switch (direction)
                {
                    case 0:
                        break;
                    case 1:
                        flipv = true;
                        break;
                    default:
                        break;

                }
                var editorAnimBox = LoadAnimation2("ItemBox", d, 0, 0, fliph, flipv, false);
                var editorAnimEffect = LoadAnimation2("ItemBox", d, 2, (int)value.ValueVar, fliph, flipv, false);
                if (editorAnimBox != null && editorAnimEffect != null && editorAnimEffect.Frames.Count != 0)
                {
                    var frameBox = editorAnimBox.Frames[0];
                    var frameEffect = editorAnimEffect.Frames[0];
                    d.DrawBitmap(frameBox.Texture, x + frameBox.Frame.CenterX, y + frameBox.Frame.CenterY,
                        frameBox.Frame.Width, frameBox.Frame.Height, false, Transparency);
                    d.DrawBitmap(frameEffect.Texture, x + frameEffect.Frame.CenterX, y + frameEffect.Frame.CenterY - (flipv ? (-3) : 3),
                        frameEffect.Frame.Width, frameEffect.Frame.Height, false, Transparency);
                }
            }
            else if (entity.Object.Name.Name == "Bridge")
            {
                var value = entity.attributesMap["length"].ValueUInt8;
                var editorAnim = LoadAnimation2("Bridge", d, 0, 0, false, false, false);
                if (editorAnim != null && editorAnim.Frames.Count != 0)
                {
                    var frame = editorAnim.Frames[0];
                    bool wEven = value % 2 == 0;
                    for (int xx = 0; xx <= value; ++xx)
                    {
                        d.DrawBitmap(frame.Texture,
                            x + (wEven ? frame.Frame.CenterX : -frame.Frame.Width) + (-value / 2 + xx) * frame.Frame.Width,
                            y + frame.Frame.CenterY,
                            frame.Frame.Width, frame.Frame.Height, false, Transparency);
                    }
                }
            }
            else if (entity.Object.Name.Name == "Newtron")
            {
                int type = (int)entity.attributesMap["type"].ValueUInt8;
                int direction = (int)entity.attributesMap["direction"].ValueUInt8;
                bool fliph = false;
                bool flipv = false;
                int animID;
                if (type == 1)
                {
                    animID = 2;
                }
                else
                {
                    animID = 0;
                }
                if (direction == 1)
                {
                    fliph = true;
                }
                if (direction == 1)
                {
                    fliph = true;
                }
                var editorAnim = LoadAnimation2("Newtron", d, animID, -1, fliph, flipv, false);
                if (editorAnim != null && editorAnim.Frames.Count != 0 && animID >= 0)
                {
                    var frame = editorAnim.Frames[index];

                    ProcessAnimation(frame.Entry.FrameSpeed, frame.Entry.Frames.Count, frame.Frame.Duration);

                    d.DrawBitmap(frame.Texture,
                        x + frame.Frame.CenterX - (fliph ? (frame.Frame.Width - editorAnim.Frames[0].Frame.Width) : 0),
                        y + frame.Frame.CenterY + (flipv ? (frame.Frame.Height - editorAnim.Frames[0].Frame.Height) : 0),
                        frame.Frame.Width, frame.Frame.Height, false, Transparency);
                }
            }
            else if (entity.Object.Name.Name == "Ring")
            {
                int type = (int)entity.attributesMap["type"].ValueVar;
                bool fliph = false;
                bool flipv = false;
                int animID;
                switch (type) {
                    case 0:
                        animID = 0;
                        break;
                    case 1:
                        animID = 1;
                        break;
                    case 2:
                        animID = 2;
                        break;
                    default:
                        animID = 0;
                        break;
                }

                var editorAnim = LoadAnimation2("Ring", d, animID, -1, fliph, flipv, false);
                if (editorAnim != null && editorAnim.Frames.Count != 0 && animID >= 0)
                {
                    var frame = editorAnim.Frames[0];
                    if (type != 2)
                    {
                        frame = editorAnim.Frames[index];
                        ProcessAnimation(frame.Entry.FrameSpeed, frame.Entry.Frames.Count, frame.Frame.Duration);
                    }




                    d.DrawBitmap(frame.Texture,
                        x + frame.Frame.CenterX - (fliph ? (frame.Frame.Width - editorAnim.Frames[0].Frame.Width) : 0),
                        y + frame.Frame.CenterY + (flipv ? (frame.Frame.Height - editorAnim.Frames[0].Frame.Height) : 0),
                        frame.Frame.Width, frame.Frame.Height, false, Transparency);
                }
            }
            else if (entity.Object.Name.Name == "Chopper")
            {
                int type = (int)entity.attributesMap["type"].ValueUInt8;
                int direction = (int)entity.attributesMap["direction"].ValueUInt8;
                bool charge = entity.attributesMap["charge"].ValueBool;
                bool fliph = false;
                bool flipv = false;
                int animID;
                if (type == 1)
                {
                    if (charge == true) {
                        animID = 3;
                    }
                    else {
                        animID = 1;
                    }

                }
                else
                {
                    animID = 0;
                }
                if (direction == 1)
                {
                    fliph = true;
                }
                if (direction == 1)
                {
                    fliph = true;
                }
                var editorAnim = LoadAnimation2("Chopper", d, animID, -1, fliph, flipv, false);
                if (editorAnim != null && editorAnim.Frames.Count != 0 && animID >= 0)
                {
                    var frame = editorAnim.Frames[index];

                    ProcessAnimation(frame.Entry.FrameSpeed, frame.Entry.Frames.Count, frame.Frame.Duration);

                    d.DrawBitmap(frame.Texture,
                        x + frame.Frame.CenterX - (fliph ? (frame.Frame.Width - editorAnim.Frames[0].Frame.Width) : 0),
                        y + frame.Frame.CenterY + (flipv ? (frame.Frame.Height - editorAnim.Frames[0].Frame.Height) : 0),
                        frame.Frame.Width, frame.Frame.Height, false, Transparency);
                }
            }
            else if (entity.Object.Name.Name == "TippingPlatform")
            {
                bool fliph = false;
                bool flipv = false;
                int animID = 0;
                var editorAnim = LoadAnimation2("Platform", d, 1, -1, fliph, flipv, false);
                if (editorAnim != null && editorAnim.Frames.Count != 0 && animID >= 0)
                {
                    var frame = editorAnim.Frames[index];

                    ProcessAnimation(frame.Entry.FrameSpeed, frame.Entry.Frames.Count, frame.Frame.Duration);

                    d.DrawBitmap(frame.Texture,
                        x + frame.Frame.CenterX - (fliph ? (frame.Frame.Width - editorAnim.Frames[0].Frame.Width) : 0),
                        y + frame.Frame.CenterY + (flipv ? (frame.Frame.Height - editorAnim.Frames[0].Frame.Height) : 0),
                        frame.Frame.Width, frame.Frame.Height, false, Transparency);
                }
            }
            else if (entity.Object.Name.Name == "Spiny")
            {
                int type = (int)entity.attributesMap["type"].ValueUInt8;
                int direction = (int)entity.attributesMap["direction"].ValueUInt8;
                bool fliph = false;
                bool flipv = false;
                int animID = 0;
                if (type == 1)
                {
                    animID = 2;
                    if (direction == 3)
                    {
                        fliph = true;
                    }
                }
                else
                {
                    animID = 0;
                    if (direction == 3)
                    {
                        flipv = true;
                    }

                }
                var editorAnim = LoadAnimation2("Spiny", d, animID, -1, fliph, flipv, false);
                if (editorAnim != null && editorAnim.Frames.Count != 0 && animID >= 0)
                {
                    var frame = editorAnim.Frames[index];

                    ProcessAnimation(frame.Entry.FrameSpeed, frame.Entry.Frames.Count, frame.Frame.Duration);

                    d.DrawBitmap(frame.Texture,
                        x + frame.Frame.CenterX - (fliph ? (frame.Frame.Width - editorAnim.Frames[0].Frame.Width) : 0),
                        y + frame.Frame.CenterY + (flipv ? (frame.Frame.Height - editorAnim.Frames[0].Frame.Height) : 0),
                        frame.Frame.Width, frame.Frame.Height, false, Transparency);
                }
            }
            else if (entity.Object.Name.Name == "OneWayDoor")
            {
                //int type = (int)entity.attributesMap["type"].ValueUInt8;
                int direction = (int)entity.attributesMap["direction"].ValueUInt8;
                bool fliph = false;
                bool flipv = false;
                int animID = 0;
                var editorAnim = LoadAnimation2("OneWayDoor", d, animID, -1, fliph, flipv, false);
                if (editorAnim != null && editorAnim.Frames.Count != 0 && animID >= 0)
                {
                    var frame = editorAnim.Frames[index];

                    ProcessAnimation(frame.Entry.FrameSpeed, frame.Entry.Frames.Count, frame.Frame.Duration);

                    d.DrawBitmap(frame.Texture,
                        x + frame.Frame.CenterX - (fliph ? (frame.Frame.Width - editorAnim.Frames[0].Frame.Width) : 0),
                        y + frame.Frame.CenterY + (flipv ? (frame.Frame.Height - editorAnim.Frames[0].Frame.Height) : 0),
                        frame.Frame.Width, frame.Frame.Height, false, Transparency);
                    d.DrawBitmap(frame.Texture,
                        x + frame.Frame.CenterX - (fliph ? (frame.Frame.Width - editorAnim.Frames[0].Frame.Width) : 0),
                        y + 32 + frame.Frame.CenterY + (flipv ? (frame.Frame.Height - editorAnim.Frames[0].Frame.Height) : 0),
                        frame.Frame.Width, frame.Frame.Height, false, Transparency);
                }
            }
            else if (entity.Object.Name.Name == "Syringe")
            {
                //int type = (int)entity.attributesMap["type"].ValueUInt8;
                //int direction = (int)entity.attributesMap["direction"].ValueUInt8;
                bool fliph = false;
                bool flipv = false;
                int animID = 0;
                var editorAnim = LoadAnimation2("Syringe", d, animID, -1, fliph, flipv, false);
                if (editorAnim != null && editorAnim.Frames.Count != 0 && animID >= 0)
                {
                    var frame = editorAnim.Frames[0];
                    var frame2 = editorAnim.Frames[1];

                    //ProcessAnimation(frame.Entry.FrameSpeed, frame.Entry.Frames.Count, frame.Frame.Duration);

                    d.DrawBitmap(frame.Texture,
                        x + frame.Frame.CenterX - (fliph ? (frame.Frame.Width - editorAnim.Frames[0].Frame.Width) : 0),
                        y + frame.Frame.CenterY + (flipv ? (frame.Frame.Height - editorAnim.Frames[0].Frame.Height) : 0),
                        frame.Frame.Width, frame.Frame.Height, false, Transparency);
                    d.DrawBitmap(frame2.Texture,
                        x + frame2.Frame.CenterX - (fliph ? (frame2.Frame.Width - editorAnim.Frames[0].Frame.Width) : 0),
                        y + frame2.Frame.CenterY + (flipv ? (frame2.Frame.Height - editorAnim.Frames[0].Frame.Height) : 0),
                        frame2.Frame.Width, frame2.Frame.Height, false, Transparency);
                }
            }
            else if (entity.Object.Name.Name == "StickyPlatform")
            {
                int type = (int)entity.attributesMap["type"].ValueVar;
                //int direction = (int)entity.attributesMap["direction"].ValueUInt8;
                bool fliph = false;
                bool flipv = false;
                int animID = 0;
                switch (type) {
                    case 0:
                        break;
                    case 1:
                        flipv = true;
                        break;
                    case 2:
                        animID = 3;
                        break;
                    case 3:
                        animID = 3;
                        fliph = true;
                        break;
                    default:
                        break;
                }
                var editorAnim = LoadAnimation2("StickyPlatform", d, animID, -1, fliph, flipv, false);
                if (editorAnim != null && editorAnim.Frames.Count != 0 && animID >= 0)
                {
                    var frame = editorAnim.Frames[index];

                    ProcessAnimation(frame.Entry.FrameSpeed, frame.Entry.Frames.Count, frame.Frame.Duration);

                    d.DrawBitmap(frame.Texture,
                        x + frame.Frame.CenterX - (fliph ? (frame.Frame.Width - editorAnim.Frames[0].Frame.Width) : 0),
                        y + frame.Frame.CenterY + (flipv ? (frame.Frame.Height - editorAnim.Frames[0].Frame.Height) : 0),
                        frame.Frame.Width, frame.Frame.Height, false, Transparency);
                }
            }
            else if (entity.Object.Name.Name == "TwistedTubes")
            {
                bool fliph = false;
                bool flipv = false;
                int height = (int)entity.attributesMap["height"].ValueUInt8;
                var editorAnim = LoadAnimation2("TwistedTubes", d, 0, 1, fliph, flipv, false);
                var editorAnim2 = LoadAnimation2("TwistedTubes", d, 0, 0, fliph, flipv, false);
                if (editorAnim != null && editorAnim.Frames.Count != 0 && editorAnim2 != null && editorAnim2.Frames.Count != 0)
                {
                    var frame = editorAnim.Frames[index];
                    var frame2 = editorAnim2.Frames[index];


                    //ProcessAnimation(frame.Entry.FrameSpeed, frame.Entry.Frames.Count, frame.Frame.Duration);

                    d.DrawBitmap(frame2.Texture,
                        x + frame2.Frame.CenterX - (fliph ? (frame2.Frame.Width - editorAnim2.Frames[0].Frame.Width) : 0),
                        y + frame2.Frame.CenterY + (flipv ? (frame2.Frame.Height - editorAnim2.Frames[0].Frame.Height) : 0),
                        frame2.Frame.Width, frame.Frame.Height, false, Transparency);
                    for (int i = 0; i < height * 2; i++)
                    {
                        d.DrawBitmap(frame.Texture,
                            x + frame.Frame.CenterX - (fliph ? (frame.Frame.Width - editorAnim.Frames[1].Frame.Width) : 0),
                            y + (i * 32) + frame.Frame.CenterY + 64 + (flipv ? (frame.Frame.Height - editorAnim.Frames[1].Frame.Height) : 0),
                            frame.Frame.Width, frame.Frame.Height, false, Transparency);
                    }

                }
            }
            else if (entity.Object.Name.Name == "ShopWindow")
            {
                var widthPixels = (int)(entity.attributesMap["size"].ValuePosition.X.High) * 2;
                var heightPixels = (int)(entity.attributesMap["size"].ValuePosition.Y.High) * 2;
                var width = (int)widthPixels / 16;
                var height = (int)heightPixels / 16;

                EditorAnimation editorAnim;

                if (width != 0 && height != 0)
                {
                    // draw corners
                    for (int i = 0; i < 4; i++)
                    {
                        bool right = (i & 1) > 0;
                        bool bottom = (i & 2) > 0;

                        editorAnim = LoadAnimation2("EditorAssets", d, 0, 0, right, bottom, false);
                        if (editorAnim != null && editorAnim.Frames.Count != 0)
                        {
                            var frame = editorAnim.Frames[index];
                            ProcessAnimation(frame.Entry.FrameSpeed, frame.Entry.Frames.Count, frame.Frame.Duration);
                            d.DrawBitmap(frame.Texture,
                                (x + widthPixels / (right ? 2 : -2)) - (right ? frame.Frame.Width : 0),
                                (y + heightPixels / (bottom ? 2 : -2) - (bottom ? frame.Frame.Height : 0)),
                                frame.Frame.Width, frame.Frame.Height, false, Transparency);

                        }
                    }

                    // draw top and bottom
                    for (int i = 0; i < 2; i++)
                    {
                        bool bottom = (i & 1) > 0;

                        editorAnim = LoadAnimation2("EditorAssets", d, 0, 1, false, bottom, false);
                        if (editorAnim != null && editorAnim.Frames.Count != 0)
                        {
                            var frame = editorAnim.Frames[index];
                            ProcessAnimation(frame.Entry.FrameSpeed, frame.Entry.Frames.Count, frame.Frame.Duration);
                            bool wEven = width % 2 == 0;
                            for (int j = 1; j < width; j++)
                                d.DrawBitmap(frame.Texture,
                                    (x + (wEven ? frame.Frame.CenterX : -frame.Frame.Width) + (-width / 2 + j) * frame.Frame.Width),
                                    (y + heightPixels / (bottom ? 2 : -2) - (bottom ? frame.Frame.Height : 0)),
                                    frame.Frame.Width, frame.Frame.Height, false, Transparency);
                        }
                    }

                    // draw sides
                    for (int i = 0; i < 2; i++)
                    {
                        bool right = (i & 1) > 0;

                        editorAnim = LoadAnimation2("EditorAssets", d, 0, 2, right, false, false);
                        if (editorAnim != null && editorAnim.Frames.Count != 0)
                        {
                            var frame = editorAnim.Frames[index];
                            ProcessAnimation(frame.Entry.FrameSpeed, frame.Entry.Frames.Count, frame.Frame.Duration);
                            bool hEven = height % 2 == 0;
                            for (int j = 1; j < height; j++)
                                d.DrawBitmap(frame.Texture,
                                    (x + widthPixels / (right ? 2 : -2)) - (right ? frame.Frame.Width : 0),
                                    (y + (hEven ? frame.Frame.CenterY : -frame.Frame.Height) + (-height / 2 + j) * frame.Frame.Height),
                                    frame.Frame.Width, frame.Frame.Height, false, Transparency);
                        }
                    }
                }
            }
            else if (entity.Object.Name.Name == "DirectorChair")
            {
                //int type = (int)entity.attributesMap["type"].ValueUInt8;
                int size = (int)entity.attributesMap["size"].ValueVar;
                bool fliph = false;
                bool flipv = false;
                var editorAnim = LoadAnimation2("DirectorChair", d, 1, 0, fliph, flipv, false);
                var editorAnim2 = LoadAnimation2("DirectorChair", d, 1, 1, fliph, flipv, false);
                var editorAnim3 = LoadAnimation2("DirectorChair", d, 1, 2, fliph, flipv, false);
                if (editorAnim != null && editorAnim.Frames.Count != 0 && editorAnim2 != null && editorAnim2.Frames.Count != 0 && editorAnim3 != null && editorAnim3.Frames.Count != 0)
                {
                    var frame = editorAnim.Frames[index];
                    var frame2 = editorAnim2.Frames[index];
                    var frame3 = editorAnim3.Frames[index];

                    ProcessAnimation(frame.Entry.FrameSpeed, frame.Entry.Frames.Count, frame.Frame.Duration);

                    d.DrawBitmap(frame.Texture,
                        x + frame.Frame.CenterX - (fliph ? (frame.Frame.Width - editorAnim.Frames[0].Frame.Width) : 0),
                        y + frame.Frame.CenterY + (flipv ? (frame.Frame.Height - editorAnim.Frames[0].Frame.Height) : 0),
                        frame.Frame.Width, frame.Frame.Height, false, Transparency);
                    d.DrawBitmap(frame.Texture,
                        x + frame.Frame.CenterX - (fliph ? (frame.Frame.Width - editorAnim.Frames[0].Frame.Width) : 0),
                        y - (size * 5 + 8) + frame.Frame.CenterY + (flipv ? (frame.Frame.Height - editorAnim.Frames[0].Frame.Height) : 0),
                        frame.Frame.Width, frame.Frame.Height, false, Transparency);
                    d.DrawBitmap(frame2.Texture,
                        x + frame2.Frame.CenterX - (fliph ? (frame2.Frame.Width - editorAnim2.Frames[0].Frame.Width) : 0),
                        y - (size * 5 + 8) + frame2.Frame.CenterY + (flipv ? (frame2.Frame.Height - editorAnim2.Frames[0].Frame.Height) : 0),
                        frame2.Frame.Width, frame2.Frame.Height, false, Transparency);
                    d.DrawBitmap(frame3.Texture,
                        x + frame3.Frame.CenterX - (fliph ? (frame3.Frame.Width - editorAnim3.Frames[0].Frame.Width) : 0),
                        y - (size * 5 + 8) + frame3.Frame.CenterY + (flipv ? (frame3.Frame.Height - editorAnim3.Frames[0].Frame.Height) : 0),
                        frame3.Frame.Width, frame3.Frame.Height, false, Transparency);
                }
            }
            else if (entity.Object.Name.Name == "TVVan")
            {
                int type = (int)entity.attributesMap["type"].ValueUInt8;
                bool allowToRender = false;
                int objType = 0;
                bool fliph = false;
                bool flipv = false;
                switch (type)
                {
                    case 0:
                        objType = 0;
                        break;
                    case 1:
                        objType = 1;
                        fliph = true;
                        break;
                    case 2:
                        objType = 2;
                        break;
                    case 3:
                        objType = 3;
                        break;
                    case 4:
                        objType = 4;
                        break;
                    case 5:
                        objType = 5;
                        break;
                    case 6:
                        objType = 6;
                        break;
                    case 7:
                        objType = 7;
                        break;
                    case 8:
                        objType = 8;
                        break;
                    case 9:
                        objType = 9;
                        break;
                    case 10:
                        objType = 10;
                        break;
                    case 11:
                        objType = 11;
                        break;
                    case 12:
                        objType = 12;
                        break;
                    case 13:
                        objType = 13;
                        break;
                    case 14:
                        objType = 14;
                        break;
                    default:
                        objType = 14;
                        break;

                }
                var editorAnim = LoadAnimation2("TVVan", d, 0, -1, fliph, flipv, false);
                var editorAnim2 = LoadAnimation2("TVVan", d, 0, -1, true, flipv, false);
                var editorAnim10 = LoadAnimation2("TVVan", d, 0, 9, fliph, flipv, false);
                var editorAnim11 = LoadAnimation2("TVVan", d, 3, 3, fliph, flipv, false);
                var editorAnim12 = LoadAnimation2("TVVan", d, 4, 3, fliph, flipv, false);
                var editorAnim13 = LoadAnimation2("TVVan", d, 6, 0, fliph, flipv, false);
                var editorAnim14 = LoadAnimation2("TVVan", d, 6, 1, fliph, flipv, false);
                var editorAnim15 = LoadAnimation2("TVVan", d, 6, 2, fliph, flipv, false);
                var editorAnim16 = LoadAnimation2("TVVan", d, 15, 0, fliph, flipv, false);
                var normalSataliteReversedHV = LoadAnimation2("TVVan", d, 6, 0, true, true, false);
                var normalSataliteReversedV = LoadAnimation2("TVVan", d, 6, 0, false, true, false);
                var normalSataliteReversedH = LoadAnimation2("TVVan", d, 6, 0, true, false, false);
                var downwardsSatalite = LoadAnimation2("TVVan", d, 6, 1, fliph, true, false);

                if (editorAnim != null && editorAnim.Frames.Count != 0)
                {
                    if (true)
                    {
                        if (true)
                        {
                            if (true)
                            {
                                if (true && editorAnim10 != null && editorAnim10.Frames.Count != 0)
                                {
                                    if (editorAnim11 != null && editorAnim11.Frames.Count != 0 && editorAnim12 != null && editorAnim12.Frames.Count != 0)
                                    {
                                        if (editorAnim13 != null && editorAnim13.Frames.Count != 0 && editorAnim14 != null && editorAnim14.Frames.Count != 0)
                                        {
                                            if (editorAnim15 != null && editorAnim15.Frames.Count != 0 && editorAnim16 != null && editorAnim16.Frames.Count != 0)
                                            {
                                                if (normalSataliteReversedHV != null && normalSataliteReversedHV.Frames.Count != 0 && normalSataliteReversedV != null && normalSataliteReversedV.Frames.Count != 0 && normalSataliteReversedH != null && normalSataliteReversedH.Frames.Count != 0)
                                                {
                                                    if (downwardsSatalite != null && downwardsSatalite.Frames.Count != 0)
                                                    {
                                                        allowToRender = true;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                if (allowToRender == true)

                {
                    var TVVan = editorAnim.Frames[0];
                    var HighLabel = editorAnim.Frames[1];
                    var InsideTVs = editorAnim.Frames[2];
                    var ramp = editorAnim.Frames[3];
                    var backsideTire = editorAnim.Frames[4];
                    var frontTireL = editorAnim.Frames[5];
                    var frontTireR = editorAnim.Frames[6];
                    var Window = editorAnim.Frames[7];
                    var VanSatalite = editorAnim.Frames[8];
                    var frame10 = editorAnim10.Frames[index];
                    var frame11 = editorAnim11.Frames[index];
                    var frame12 = editorAnim12.Frames[index];
                    var normalSatalite = editorAnim13.Frames[index];
                    var upwardsSatalite = editorAnim14.Frames[index];
                    var sataliteHook = editorAnim15.Frames[index];
                    var frame16 = editorAnim16.Frames[index];
                    var normalSatalite2 = normalSataliteReversedHV.Frames[index];
                    var normalSataliteH = normalSataliteReversedH.Frames[index];
                    var normalSataliteV = normalSataliteReversedV.Frames[index];
                    var downwardsFaceSatalite = downwardsSatalite.Frames[index];

                    //ProcessAnimation(frame16.Entry.FrameSpeed, frame16.Entry.Frames.Count, frame16.Frame.Duration);

                    if (objType == 0 || objType == 12 || objType == 13) // Normal (TV Van)
                    {
                        d.DrawBitmap(Window.Texture,
                        x + Window.Frame.CenterX - (fliph ? (Window.Frame.Width - editorAnim.Frames[0].Frame.Width) : 0),
                        y + Window.Frame.CenterY + (flipv ? (Window.Frame.Height - editorAnim.Frames[0].Frame.Height) : 0),
                        Window.Frame.Width, Window.Frame.Height, false, Transparency);
                        d.DrawBitmap(backsideTire.Texture,
                        x + backsideTire.Frame.CenterX - (fliph ? (backsideTire.Frame.Width - editorAnim.Frames[0].Frame.Width) : 0),
                        y + backsideTire.Frame.CenterY + (flipv ? (backsideTire.Frame.Height - editorAnim.Frames[0].Frame.Height) : 0),
                        backsideTire.Frame.Width, backsideTire.Frame.Height, false, Transparency);
                        d.DrawBitmap(TVVan.Texture,
                        x + TVVan.Frame.CenterX - (fliph ? (TVVan.Frame.Width - editorAnim.Frames[0].Frame.Width) : 0),
                        y + TVVan.Frame.CenterY + (flipv ? (TVVan.Frame.Height - editorAnim.Frames[0].Frame.Height) : 0),
                        TVVan.Frame.Width, TVVan.Frame.Height, false, Transparency);
                        d.DrawBitmap(HighLabel.Texture,
                        x + HighLabel.Frame.CenterX - (fliph ? (HighLabel.Frame.Width - editorAnim.Frames[0].Frame.Width) : 0),
                        y + HighLabel.Frame.CenterY + (flipv ? (HighLabel.Frame.Height - editorAnim.Frames[0].Frame.Height) : 0),
                        HighLabel.Frame.Width, HighLabel.Frame.Height, false, Transparency);
                        d.DrawBitmap(InsideTVs.Texture,
                        x + InsideTVs.Frame.CenterX - (fliph ? (InsideTVs.Frame.Width - editorAnim.Frames[0].Frame.Width) : 0),
                        y + InsideTVs.Frame.CenterY + (flipv ? (InsideTVs.Frame.Height - editorAnim.Frames[0].Frame.Height) : 0),
                        InsideTVs.Frame.Width, InsideTVs.Frame.Height, false, Transparency);
                        d.DrawBitmap(ramp.Texture,
                        x + ramp.Frame.CenterX - (fliph ? (ramp.Frame.Width - editorAnim.Frames[0].Frame.Width) : 0),
                        y + ramp.Frame.CenterY + (flipv ? (ramp.Frame.Height - editorAnim.Frames[0].Frame.Height) : 0),
                        ramp.Frame.Width, ramp.Frame.Height, false, Transparency);
                        d.DrawBitmap(frontTireL.Texture,
                        x + frontTireL.Frame.CenterX - (fliph ? (frontTireL.Frame.Width - editorAnim.Frames[0].Frame.Width) : 0),
                        y + frontTireL.Frame.CenterY + (flipv ? (frontTireL.Frame.Height - editorAnim.Frames[0].Frame.Height) : 0),
                        frontTireL.Frame.Width, frontTireL.Frame.Height, false, Transparency);
                        d.DrawBitmap(frontTireR.Texture,
                        x + frontTireR.Frame.CenterX - (fliph ? (frontTireR.Frame.Width - editorAnim.Frames[0].Frame.Width) : 0),
                        y + frontTireR.Frame.CenterY + (flipv ? (frontTireR.Frame.Height - editorAnim.Frames[0].Frame.Height) : 0),
                        frontTireR.Frame.Width, frontTireR.Frame.Height, false, Transparency);
                        d.DrawBitmap(VanSatalite.Texture,
                        x + VanSatalite.Frame.CenterX - (fliph ? (VanSatalite.Frame.Width - editorAnim.Frames[0].Frame.Width) : 0),
                        y + VanSatalite.Frame.CenterY + (flipv ? (VanSatalite.Frame.Height - editorAnim.Frames[0].Frame.Height) : 0),
                        VanSatalite.Frame.Width, VanSatalite.Frame.Height, false, Transparency);
                        d.DrawBitmap(frame11.Texture,
                        x + frame11.Frame.CenterX - (fliph ? (frame11.Frame.Width - editorAnim11.Frames[0].Frame.Width) : 0),
                        y + frame11.Frame.CenterY + (flipv ? (frame11.Frame.Height - editorAnim11.Frames[0].Frame.Height) : 0),
                        frame11.Frame.Width, frame11.Frame.Height, false, Transparency);
                        d.DrawBitmap(frame12.Texture,
                        x + frame12.Frame.CenterX - (fliph ? (frame12.Frame.Width - editorAnim12.Frames[0].Frame.Width) : 0),
                        y + frame12.Frame.CenterY + (flipv ? (frame12.Frame.Height - editorAnim12.Frames[0].Frame.Height) : 0),
                        frame12.Frame.Width, frame12.Frame.Height, false, Transparency);
                    }

                    if (objType == 1) // Reverse (TV Van)
                    {
                        d.DrawBitmap(Window.Texture,
                        x - Window.Frame.CenterX - Window.Frame.Width - (false ? (Window.Frame.Width - editorAnim.Frames[0].Frame.Width) : 0),
                        y + Window.Frame.CenterY + (flipv ? (Window.Frame.Height - editorAnim.Frames[0].Frame.Height) : 0),
                        Window.Frame.Width, Window.Frame.Height, false, Transparency);
                        d.DrawBitmap(backsideTire.Texture,
                        x - backsideTire.Frame.CenterX - backsideTire.Frame.Width - (false ? (backsideTire.Frame.Width - editorAnim.Frames[0].Frame.Width) : 0),
                        y + backsideTire.Frame.CenterY + (flipv ? (backsideTire.Frame.Height - editorAnim.Frames[0].Frame.Height) : 0),
                        backsideTire.Frame.Width, backsideTire.Frame.Height, false, Transparency);
                        d.DrawBitmap(TVVan.Texture,
                        x + TVVan.Frame.CenterX + (true ? (TVVan.Frame.Width - editorAnim.Frames[0].Frame.Width) : 0),
                        y + TVVan.Frame.CenterY + (flipv ? (TVVan.Frame.Height - editorAnim.Frames[0].Frame.Height) : 0),
                        TVVan.Frame.Width, TVVan.Frame.Height, false, Transparency);
                        d.DrawBitmap(HighLabel.Texture,
                        x + HighLabel.Frame.CenterX + HighLabel.Frame.Width*2 - (true ? (HighLabel.Frame.Width - editorAnim.Frames[0].Frame.Width) : 0),
                        y + HighLabel.Frame.CenterY + (flipv ? (HighLabel.Frame.Height - editorAnim.Frames[0].Frame.Height) : 0),
                        HighLabel.Frame.Width, HighLabel.Frame.Height, false, Transparency);
                        d.DrawBitmap(InsideTVs.Texture,
                        x + InsideTVs.Frame.CenterX + InsideTVs.Frame.Width - (true ? (InsideTVs.Frame.Width - editorAnim.Frames[0].Frame.Width) : 0),
                        y + InsideTVs.Frame.CenterY + (flipv ? (InsideTVs.Frame.Height - editorAnim.Frames[0].Frame.Height) : 0),
                        InsideTVs.Frame.Width, InsideTVs.Frame.Height, false, Transparency);
                        d.DrawBitmap(ramp.Texture,
                        x - ramp.Frame.CenterX - ramp.Frame.Width - (false ? (ramp.Frame.Width - editorAnim.Frames[0].Frame.Width) : 0),
                        y + ramp.Frame.CenterY + (flipv ? (ramp.Frame.Height - editorAnim.Frames[0].Frame.Height) : 0),
                        ramp.Frame.Width, ramp.Frame.Height, false, Transparency);
                        d.DrawBitmap(frontTireL.Texture,
                        x - frontTireL.Frame.CenterX - frontTireL.Frame.Width - (false ? (frontTireL.Frame.Width - editorAnim.Frames[0].Frame.Width) : 0),
                        y + frontTireL.Frame.CenterY + (flipv ? (frontTireL.Frame.Height - editorAnim.Frames[0].Frame.Height) : 0),
                        frontTireL.Frame.Width, frontTireL.Frame.Height, false, Transparency);
                        d.DrawBitmap(frontTireR.Texture,
                        x - frontTireR.Frame.CenterX - frontTireR.Frame.Width  - (false ? (frontTireR.Frame.Width - editorAnim.Frames[0].Frame.Width) : 0),
                        y + frontTireR.Frame.CenterY + (flipv ? (frontTireR.Frame.Height - editorAnim.Frames[0].Frame.Height) : 0),
                        frontTireR.Frame.Width, frontTireR.Frame.Height, false, Transparency);
                        d.DrawBitmap(VanSatalite.Texture,
                        x - VanSatalite.Frame.CenterX - VanSatalite.Frame.Width - (false ? (VanSatalite.Frame.Width - editorAnim.Frames[0].Frame.Width) : 0),
                        y + VanSatalite.Frame.CenterY + (flipv ? (VanSatalite.Frame.Height - editorAnim.Frames[0].Frame.Height) : 0),
                        VanSatalite.Frame.Width, VanSatalite.Frame.Height, false, Transparency);
                        d.DrawBitmap(frame11.Texture,
                        x - frame11.Frame.CenterX - frame11.Frame.Width - (true ? (frame11.Frame.Width - editorAnim11.Frames[0].Frame.Width) : 0),
                        y + frame11.Frame.CenterY + (flipv ? (frame11.Frame.Height - editorAnim11.Frames[0].Frame.Height) : 0),
                        frame11.Frame.Width, frame11.Frame.Height, false, Transparency);
                        d.DrawBitmap(frame12.Texture,
                        x - frame12.Frame.CenterX - frame12.Frame.Width - (true ? (frame12.Frame.Width - editorAnim12.Frames[0].Frame.Width) : 0),
                        y + frame12.Frame.CenterY + (flipv ? (frame12.Frame.Height - editorAnim12.Frames[0].Frame.Height) : 0),
                        frame12.Frame.Width, frame12.Frame.Height, false, Transparency);
                    }

                    if (objType >= 14) //Game Gear TV
                    {
                        d.DrawBitmap(frame16.Texture,
                        x + frame16.Frame.CenterX - (fliph ? (frame16.Frame.Width - editorAnim16.Frames[0].Frame.Width) : 0),
                        y + frame16.Frame.CenterY + (flipv ? (frame16.Frame.Height - editorAnim16.Frames[0].Frame.Height) : 0),
                        frame16.Frame.Width, frame16.Frame.Height, false, Transparency);
                        d.DrawBitmap(frame10.Texture,
                        x + frame10.Frame.CenterX - (fliph ? (frame10.Frame.Width - editorAnim10.Frames[0].Frame.Width) : 0),
                        y + frame10.Frame.CenterY + (flipv ? (frame10.Frame.Height - editorAnim10.Frames[0].Frame.Height) : 0),
                        frame10.Frame.Width, frame10.Frame.Height, false, Transparency);
                    }

                    if (objType == 2 || objType == 5 || objType == 6 || objType == 11) //Satalite Normal
                    {
                        d.DrawBitmap(normalSatalite.Texture,
                        x + normalSatalite.Frame.CenterX - (fliph ? (normalSatalite.Frame.Width - editorAnim13.Frames[0].Frame.Width) : 0),
                        y + normalSatalite.Frame.CenterY + (flipv ? (normalSatalite.Frame.Height - editorAnim13.Frames[0].Frame.Height) : 0),
                        normalSatalite.Frame.Width, normalSatalite.Frame.Height, false, Transparency);
                    }
                    if (objType == 3 || objType == 4 || objType == 6 || objType == 9) //Satalite Flipped H
                    {
                        d.DrawBitmap(normalSataliteH.Texture,
                        x - normalSataliteH.Frame.CenterX - normalSataliteH.Frame.Width - (true ? (normalSataliteH.Frame.Width - normalSataliteReversedH.Frames[0].Frame.Width) : 0),
                        y + normalSataliteH.Frame.CenterY + (false ? (normalSataliteH.Frame.Height - normalSataliteReversedH.Frames[0].Frame.Height) : 0),
                        normalSataliteH.Frame.Width, normalSataliteH.Frame.Height, false, Transparency);
                    }
                    if (objType == 3 || objType == 5 || objType == 7 || objType == 10) //Satalite Flipped V
                    {
                        d.DrawBitmap(normalSataliteV.Texture,
x + normalSataliteV.Frame.CenterX - (false ? (normalSataliteV.Frame.Width - normalSataliteReversedV.Frames[0].Frame.Width) : 0),
y + normalSataliteV.Frame.CenterY + normalSataliteV.Frame.Height + (true ? (normalSataliteV.Frame.Height - normalSataliteReversedV.Frames[0].Frame.Height) : 0),
normalSataliteV.Frame.Width, normalSataliteV.Frame.Height, false, Transparency);
                    }
                    if (objType == 2 || objType == 4 || objType == 7 || objType == 8) //Satalite Flipped VH
                    {
                        d.DrawBitmap(normalSatalite2.Texture,
x - normalSatalite2.Frame.CenterX - normalSatalite2.Frame.Width - (true ? (normalSatalite2.Frame.Width - editorAnim13.Frames[0].Frame.Width) : 0),
y + normalSatalite2.Frame.CenterY + normalSatalite2.Frame.Height + (true ? (normalSatalite2.Frame.Height - editorAnim13.Frames[0].Frame.Height) : 0),
normalSatalite2.Frame.Width, normalSatalite2.Frame.Height, false, Transparency);
                    }
                    if (objType == 8 || objType == 10) //Satalite Upward
                    {
                        d.DrawBitmap(upwardsSatalite.Texture,
                        x + upwardsSatalite.Frame.CenterX - (fliph ? (upwardsSatalite.Frame.Width - editorAnim14.Frames[0].Frame.Width) : 0),
                        y + upwardsSatalite.Frame.CenterY + (flipv ? (upwardsSatalite.Frame.Height - editorAnim14.Frames[0].Frame.Height) : 0),
                        upwardsSatalite.Frame.Width, upwardsSatalite.Frame.Height, false, Transparency);
                    }
                    if (objType == 9 || objType == 11) //Satalite Downward
                    {
                        d.DrawBitmap(downwardsFaceSatalite.Texture,
                        x + downwardsFaceSatalite.Frame.CenterX - (fliph ? (downwardsFaceSatalite.Frame.Width - downwardsSatalite.Frames[0].Frame.Width) : 0),
                        y + downwardsFaceSatalite.Frame.CenterY + downwardsFaceSatalite.Frame.Width + sataliteHook.Frame.Height/2 +  (true ? (downwardsFaceSatalite.Frame.Height - downwardsSatalite.Frames[0].Frame.Height) : 0),
                        downwardsFaceSatalite.Frame.Width, downwardsFaceSatalite.Frame.Height, false, Transparency);
                    }
                    if (objType <= 11 && objType >= 2) //Satalite Center
                    {
                        d.DrawBitmap(sataliteHook.Texture,
x + sataliteHook.Frame.CenterX - (fliph ? (sataliteHook.Frame.Width - editorAnim15.Frames[0].Frame.Width) : 0),
y + sataliteHook.Frame.CenterY + (flipv ? (sataliteHook.Frame.Height - editorAnim15.Frames[0].Frame.Height) : 0),
sataliteHook.Frame.Width, sataliteHook.Frame.Height, false, Transparency);
                    }
                }
            }
            else if (entity.Object.Name.Name == "FilmProjector")
            {
                bool fliph = false;
                bool flipv = false;
                var editorAnim = LoadAnimation2("FilmProjector", d, 0, 0, fliph, flipv, false);
                var editorAnim2 = LoadAnimation2("FilmProjector", d, 0, 1, fliph, flipv, false);
                var editorAnim3 = LoadAnimation2("FilmProjector", d, 3, -1, fliph, flipv, false);
                var editorAnim4 = LoadAnimation2("FilmProjector", d, 1, 0, fliph, flipv, false);
                var editorAnim5 = LoadAnimation2("FilmProjector", d, 2, 0, fliph, flipv, false);
                var editorAnim6 = LoadAnimation2("FilmProjector", d, 1, 0, fliph, flipv, false);
                var editorAnim7 = LoadAnimation2("FilmProjector", d, 2, 0, fliph, flipv, false);
                if (editorAnim != null && editorAnim.Frames.Count != 0 && editorAnim2 != null && editorAnim2.Frames.Count != 0 && editorAnim3 != null && editorAnim3.Frames.Count != 0 && editorAnim4 != null && editorAnim4.Frames.Count != 0 && editorAnim5 != null && editorAnim5.Frames.Count != 0 && editorAnim6 != null && editorAnim6.Frames.Count != 0 && editorAnim7 != null && editorAnim7.Frames.Count != 0)
                {
                    var frame = editorAnim.Frames[index];
                    var frame2 = editorAnim2.Frames[index];
                    var frame3 = editorAnim3.Frames[index];
                    var frame4 = editorAnim4.Frames[index];
                    var frame5 = editorAnim5.Frames[index];
                    var frame6 = editorAnim4.Frames[index];
                    var frame7 = editorAnim5.Frames[index];

                    //ProcessAnimation(frame3.Entry.FrameSpeed, frame3.Entry.Frames.Count, frame3.Frame.Duration);

                    d.DrawBitmap(frame5.Texture,
                        x + 42 + frame5.Frame.CenterX - (fliph ? (frame5.Frame.Width - editorAnim5.Frames[0].Frame.Width) : 0),
                        y - 68 + frame5.Frame.CenterY + (flipv ? (frame5.Frame.Height - editorAnim5.Frames[0].Frame.Height) : 0),
                        frame5.Frame.Width, frame5.Frame.Height, false, Transparency);
                    d.DrawBitmap(frame4.Texture,
                        x + 42 + frame4.Frame.CenterX - (fliph ? (frame4.Frame.Width - editorAnim4.Frames[0].Frame.Width) : 0),
                        y - 68 + frame4.Frame.CenterY + (flipv ? (frame3.Frame.Height - editorAnim4.Frames[0].Frame.Height) : 0),
                        frame4.Frame.Width, frame4.Frame.Height, false, Transparency);


                    d.DrawBitmap(frame7.Texture,
                        x - 60 + frame7.Frame.CenterX - (fliph ? (frame7.Frame.Width - editorAnim5.Frames[0].Frame.Width) : 0),
                        y - 68 + frame7.Frame.CenterY + (flipv ? (frame7.Frame.Height - editorAnim5.Frames[0].Frame.Height) : 0),
                        frame7.Frame.Width, frame7.Frame.Height, false, Transparency);
                    d.DrawBitmap(frame6.Texture,
                        x - 60 + frame6.Frame.CenterX - (fliph ? (frame6.Frame.Width - editorAnim4.Frames[0].Frame.Width) : 0),
                        y - 68 + frame6.Frame.CenterY + (flipv ? (frame3.Frame.Height - editorAnim4.Frames[0].Frame.Height) : 0),
                        frame6.Frame.Width, frame6.Frame.Height, false, Transparency);


                    d.DrawBitmap(frame.Texture,
                        x + frame.Frame.CenterX - (fliph ? (frame.Frame.Width - editorAnim.Frames[0].Frame.Width) : 0),
                        y + frame.Frame.CenterY + (flipv ? (frame.Frame.Height - editorAnim.Frames[0].Frame.Height) : 0),
                        frame.Frame.Width, frame.Frame.Height, false, Transparency);
                    d.DrawBitmap(frame2.Texture,
                        x + 185 + frame2.Frame.CenterX - (fliph ? (frame2.Frame.Width - editorAnim2.Frames[0].Frame.Width) : 0),
                        y + frame2.Frame.CenterY + (flipv ? (frame2.Frame.Height - editorAnim2.Frames[0].Frame.Height) : 0),
                        frame2.Frame.Width, frame2.Frame.Height, false, Transparency);
                    d.DrawBitmap(frame3.Texture,
                        x + 185 + frame3.Frame.CenterX - (fliph ? (frame3.Frame.Width - editorAnim2.Frames[0].Frame.Width) : 0),
                        y + frame3.Frame.CenterY + (flipv ? (frame3.Frame.Height - editorAnim2.Frames[0].Frame.Height) : 0),
                        frame3.Frame.Width, frame3.Frame.Height, false, Transparency);

                }
            }
            else if (entity.Object.Name.Name == "RockemSockem")
            {
                //int type = (int)entity.attributesMap["type"].ValueUInt8;
                //int direction = (int)entity.attributesMap["direction"].ValueUInt8;
                bool fliph = false;
                bool flipv = false;
                var editorAnim = LoadAnimation2("RockemSockem", d, 0, -1, fliph, flipv, false);
                var editorAnim2 = LoadAnimation2("RockemSockem", d, 1, -1, fliph, flipv, false);
                var editorAnim3 = LoadAnimation2("RockemSockem", d, 2, -1, fliph, flipv, false);
                if (editorAnim != null && editorAnim.Frames.Count != 0 && editorAnim2 != null && editorAnim2.Frames.Count != 0 && editorAnim3 != null && editorAnim3.Frames.Count != 0)
                {
                    var frame = editorAnim.Frames[index];
                    var frame2 = editorAnim2.Frames[index];
                    var frame3 = editorAnim3.Frames[index];

                    //ProcessAnimation(frame.Entry.FrameSpeed, frame.Entry.Frames.Count, frame.Frame.Duration);

                    d.DrawBitmap(frame.Texture,
                        x + frame.Frame.CenterX - (fliph ? (frame.Frame.Width - editorAnim.Frames[0].Frame.Width) : 0),
                        y + frame.Frame.CenterY + (flipv ? (frame.Frame.Height - editorAnim.Frames[0].Frame.Height) : 0),
                        frame.Frame.Width, frame.Frame.Height, false, Transparency);

                    for (int i = 0; i < 6; i++)
                    {
                        d.DrawBitmap(frame2.Texture,
                            x + frame2.Frame.CenterX - (fliph ? (frame2.Frame.Width - editorAnim2.Frames[0].Frame.Width) : 0),
                            y - 8 - (i * 4) + frame2.Frame.CenterY + (flipv ? (frame2.Frame.Height - editorAnim2.Frames[0].Frame.Height) : 0),
                            frame2.Frame.Width, frame2.Frame.Height, false, Transparency);
                    }
                    d.DrawBitmap(frame3.Texture,
                        x + frame3.Frame.CenterX - (fliph ? (frame3.Frame.Width - editorAnim3.Frames[0].Frame.Width) : 0),
                        y - 44 + frame3.Frame.CenterY + (flipv ? (frame3.Frame.Height - editorAnim3.Frames[0].Frame.Height) : 0),
                        frame3.Frame.Width, frame3.Frame.Height, false, Transparency);
                }
            }
            else if (entity.Object.Name.Name == "Clapperboard")
            {
                //int type = (int)entity.attributesMap["type"].ValueUInt8;
                int direction = (int)entity.attributesMap["direction"].ValueUInt8;
                bool fliph = false;
                bool flipv = false;
                int flipState = 0;
                if (direction == 1)
                {
                    fliph = true;
                    flipState = 3;
                }
                else
                {
                    fliph = false;
                    flipState = 2;
                }
                var editorAnim = LoadAnimation2("Clapperboard", d, 0, 0, fliph, flipv, false);
                var editorAnim2 = LoadAnimation2("Clapperboard", d, 0, 1, fliph, flipv, false);
                var editorAnim3 = LoadAnimation2("Clapperboard", d, 0, flipState, false, false, false);
                if (editorAnim != null && editorAnim.Frames.Count != 0 && editorAnim2 != null && editorAnim2.Frames.Count != 0 && editorAnim3 != null && editorAnim3.Frames.Count != 0)
                {
                    var frame = editorAnim.Frames[index];
                    var frame2 = editorAnim2.Frames[index];
                    var frame3 = editorAnim3.Frames[index];

                    //ProcessAnimation(frame.Entry.FrameSpeed, frame.Entry.Frames.Count, frame.Frame.Duration);

                    if (direction == 1)
                    {
                        d.DrawBitmap(frame.Texture,
                            x - 104 + frame.Frame.CenterX - (fliph ? (frame.Frame.Width - editorAnim.Frames[0].Frame.Width) : 0),
                            y + frame.Frame.CenterY + (flipv ? (frame.Frame.Height - editorAnim.Frames[0].Frame.Height) : 0),
                            frame.Frame.Width, frame.Frame.Height, false, Transparency);
                        d.DrawBitmap(frame2.Texture,
                                x - 104 + frame2.Frame.CenterX - (fliph ? (frame2.Frame.Width - editorAnim2.Frames[0].Frame.Width) : 0),
                                y + frame2.Frame.CenterY + (flipv ? (frame2.Frame.Height - editorAnim2.Frames[0].Frame.Height) : 0),
                                frame2.Frame.Width, frame2.Frame.Height, false, Transparency);
                        d.DrawBitmap(frame3.Texture,
                            x + frame3.Frame.CenterX - (fliph ? (frame3.Frame.Width - editorAnim3.Frames[0].Frame.Width) : 0),
                            y + frame3.Frame.CenterY + (flipv ? (frame3.Frame.Height - editorAnim3.Frames[0].Frame.Height) : 0),
                            frame3.Frame.Width, frame3.Frame.Height, false, Transparency);
                    }
                    else
                    {
                        d.DrawBitmap(frame.Texture,
                            x + frame.Frame.CenterX - (fliph ? (frame.Frame.Width - editorAnim.Frames[0].Frame.Width) : 0),
                            y + frame.Frame.CenterY + (flipv ? (frame.Frame.Height - editorAnim.Frames[0].Frame.Height) : 0),
                            frame.Frame.Width, frame.Frame.Height, false, Transparency);
                        d.DrawBitmap(frame2.Texture,
                                x + frame2.Frame.CenterX - (fliph ? (frame2.Frame.Width - editorAnim2.Frames[0].Frame.Width) : 0),
                                y + frame2.Frame.CenterY + (flipv ? (frame2.Frame.Height - editorAnim2.Frames[0].Frame.Height) : 0),
                                frame2.Frame.Width, frame2.Frame.Height, false, Transparency);
                        d.DrawBitmap(frame3.Texture,
                            x + frame3.Frame.CenterX - (fliph ? (frame3.Frame.Width - editorAnim3.Frames[0].Frame.Width) : 0),
                            y + frame3.Frame.CenterY + (flipv ? (frame3.Frame.Height - editorAnim3.Frames[0].Frame.Height) : 0),
                            frame3.Frame.Width, frame3.Frame.Height, false, Transparency);
                    }

                }
            }
            else if (entity.Object.Name.Name == "PopcornMachine")
            {
                //int type = (int)entity.attributesMap["type"].ValueUInt8;
                int type = (int)entity.attributesMap["type"].ValueUInt8;
                int height = (int)entity.attributesMap["height"].ValueUInt8;
                bool fliph = false;
                bool flipv = false;
                var editorAnim = LoadAnimation2("PopcornMachine", d, 0, 0, fliph, flipv, false);
                var editorAnim2 = LoadAnimation2("PopcornMachine", d, 0, 1, fliph, flipv, false);
                var editorAnim3 = LoadAnimation2("PopcornMachine", d, 0, 2, false, false, false);
                var editorAnim4 = LoadAnimation2("PopcornMachine", d, 0, 3, false, false, false);
                var editorAnim5 = LoadAnimation2("PopcornMachine", d, 0, 6, false, false, false);
                var editorAnim6 = LoadAnimation2("PopcornMachine", d, 0, 7, false, false, false);
                var editorAnim7 = LoadAnimation2("PopcornMachine", d, 0, 4, false, false, false);
                var editorAnim8 = LoadAnimation2("PopcornMachine", d, 0, 5, false, false, false);
                var editorAnim9 = LoadAnimation2("PopcornMachine", d, 0, 9, false, false, false);
                var editorAnim10 = LoadAnimation2("PopcornMachine", d, 0, 10, false, false, false);
                if (editorAnim != null && editorAnim.Frames.Count != 0 && editorAnim2 != null && editorAnim2.Frames.Count != 0 && editorAnim3 != null && editorAnim3.Frames.Count != 0 && editorAnim4 != null && editorAnim4.Frames.Count != 0 && editorAnim5 != null && editorAnim5.Frames.Count != 0 && editorAnim6 != null && editorAnim6.Frames.Count != 0 && editorAnim7 != null && editorAnim7.Frames.Count != 0 && editorAnim8 != null && editorAnim8.Frames.Count != 0 && editorAnim9 != null && editorAnim9.Frames.Count != 0 && editorAnim10 != null && editorAnim10.Frames.Count != 0)
                {
                    var frame = editorAnim.Frames[index];
                    var frame2 = editorAnim2.Frames[index];
                    var frame3 = editorAnim3.Frames[index];
                    var frame4 = editorAnim4.Frames[index];
                    var frame5 = editorAnim5.Frames[index];
                    var frame6 = editorAnim6.Frames[index];
                    var frame7 = editorAnim7.Frames[index];
                    var frame8 = editorAnim8.Frames[index];
                    var frame9 = editorAnim9.Frames[index];
                    var frame10 = editorAnim10.Frames[index];

                    //ProcessAnimation(frame.Entry.FrameSpeed, frame.Entry.Frames.Count, frame.Frame.Duration);
                    d.DrawBitmap(frame.Texture,
                            x + frame.Frame.CenterX - (fliph ? (frame.Frame.Width - editorAnim.Frames[0].Frame.Width) : 0),
                            y + frame.Frame.CenterY + (flipv ? (frame.Frame.Height - editorAnim.Frames[0].Frame.Height) : 0),
                            frame.Frame.Width, frame.Frame.Height, false, Transparency);
                        d.DrawBitmap(frame2.Texture,
                                x + frame2.Frame.CenterX - (fliph ? (frame2.Frame.Width - editorAnim2.Frames[0].Frame.Width) : 0),
                                y + frame2.Frame.CenterY + (flipv ? (frame2.Frame.Height - editorAnim2.Frames[0].Frame.Height) : 0),
                                frame2.Frame.Width, frame2.Frame.Height, false, Transparency);
                    if (type == 0 || type == 2)
                    {
                        d.DrawBitmap(frame3.Texture,
                            x + frame3.Frame.CenterX - (fliph ? (frame3.Frame.Width - editorAnim3.Frames[0].Frame.Width) : 0),
                            y + frame3.Frame.CenterY + (flipv ? (frame3.Frame.Height - editorAnim3.Frames[0].Frame.Height) : 0),
                            frame3.Frame.Width, frame3.Frame.Height, false, Transparency);
                    }
                    if (type == 1 || type == 2)
                    {
                        d.DrawBitmap(frame4.Texture,
                            x + frame4.Frame.CenterX - (fliph ? (frame4.Frame.Width - editorAnim3.Frames[0].Frame.Width) : 0),
                            y + frame4.Frame.CenterY + (flipv ? (frame4.Frame.Height - editorAnim3.Frames[0].Frame.Height) : 0),
                            frame4.Frame.Width, frame4.Frame.Height, false, Transparency);
                    }
                    for (int i = 0; i <= height; i++)
                    {
                        d.DrawBitmap(frame5.Texture,
                            x + frame5.Frame.CenterX - (fliph ? (frame5.Frame.Width - editorAnim3.Frames[0].Frame.Width) : 0),
                            y - 208 - (i * 160) + frame5.Frame.CenterY + (flipv ? (frame5.Frame.Height - editorAnim3.Frames[0].Frame.Height) : 0),
                            frame5.Frame.Width, frame5.Frame.Height, false, Transparency);
                        d.DrawBitmap(frame6.Texture,
                            x + frame6.Frame.CenterX - (fliph ? (frame6.Frame.Width - editorAnim3.Frames[0].Frame.Width) : 0),
                            y - 208 - (i * 160) + frame6.Frame.CenterY + (flipv ? (frame6.Frame.Height - editorAnim3.Frames[0].Frame.Height) : 0),
                            frame6.Frame.Width, frame6.Frame.Height, false, Transparency);
                    }
                    d.DrawBitmap(frame7.Texture,
                        x + frame7.Frame.CenterX - (fliph ? (frame7.Frame.Width - editorAnim3.Frames[0].Frame.Width) : 0),
                        y - 208 - (height * 160) + frame7.Frame.CenterY + (flipv ? (frame7.Frame.Height - editorAnim3.Frames[0].Frame.Height) : 0),
                        frame7.Frame.Width, frame7.Frame.Height, false, Transparency);
                    d.DrawBitmap(frame8.Texture,
                        x + frame8.Frame.CenterX - (fliph ? (frame8.Frame.Width - editorAnim3.Frames[0].Frame.Width) : 0),
                        y - 208 - (height * 160) + frame8.Frame.CenterY + (flipv ? (frame8.Frame.Height - editorAnim3.Frames[0].Frame.Height) : 0),
                        frame8.Frame.Width, frame8.Frame.Height, false, Transparency);
                    d.DrawBitmap(frame9.Texture,
                        x + frame9.Frame.CenterX - (fliph ? (frame9.Frame.Width - editorAnim9.Frames[0].Frame.Width) : 0),
                        y - 95 + frame9.Frame.CenterY + (flipv ? (frame9.Frame.Height - editorAnim9.Frames[0].Frame.Height) : 0),
                        frame9.Frame.Width, frame9.Frame.Height, false, Transparency);
                    d.DrawBitmap(frame10.Texture,
                        x + frame10.Frame.CenterX - (fliph ? (frame10.Frame.Width - editorAnim10.Frames[0].Frame.Width) : 0),
                        y - 64 + frame10.Frame.CenterY + (flipv ? (frame10.Frame.Height - editorAnim10.Frames[0].Frame.Height) : 0),
                        frame10.Frame.Width, frame10.Frame.Height, false, Transparency);




                }
            }
            else if (entity.Object.Name.Name == "LEDPanel")
            {
                var widthPixels = (int)(entity.attributesMap["size"].ValuePosition.X.High) - 16;
                var heightPixels = (int)(entity.attributesMap["size"].ValuePosition.Y.High);
                var width = (int)widthPixels / 16;
                var height = (int)heightPixels / 16;

                EditorAnimation editorAnim;

                if (width != 0 && height != 0)
                {
                    // draw corners
                    for (int i = 0; i < 4; i++)
                    {
                        bool right = (i & 1) > 0;
                        bool bottom = (i & 2) > 0;

                        editorAnim = LoadAnimation2("EditorAssets", d, 0, 0, right, bottom, false);
                        if (editorAnim != null && editorAnim.Frames.Count != 0)
                        {
                            var frame = editorAnim.Frames[index];
                            ProcessAnimation(frame.Entry.FrameSpeed, frame.Entry.Frames.Count, frame.Frame.Duration);
                            d.DrawBitmap(frame.Texture,
                                (x + widthPixels / (right ? 2 : -2)) - (right ? frame.Frame.Width : 0),
                                (y + heightPixels / (bottom ? 2 : -2) - (bottom ? frame.Frame.Height : 0)),
                                frame.Frame.Width, frame.Frame.Height, false, Transparency);

                        }
                    }

                    // draw top and bottom
                    for (int i = 0; i < 2; i++)
                    {
                        bool bottom = (i & 1) > 0;

                        editorAnim = LoadAnimation2("EditorAssets", d, 0, 1, false, bottom, false);
                        if (editorAnim != null && editorAnim.Frames.Count != 0)
                        {
                            var frame = editorAnim.Frames[index];
                            ProcessAnimation(frame.Entry.FrameSpeed, frame.Entry.Frames.Count, frame.Frame.Duration);
                            bool wEven = width % 2 == 0;
                            for (int j = 1; j < width; j++)
                                d.DrawBitmap(frame.Texture,
                                    (x + (wEven ? frame.Frame.CenterX : -frame.Frame.Width) + (-width / 2 + j) * frame.Frame.Width),
                                    (y + heightPixels / (bottom ? 2 : -2) - (bottom ? frame.Frame.Height : 0)),
                                    frame.Frame.Width, frame.Frame.Height, false, Transparency);
                        }
                    }

                    // draw sides
                    for (int i = 0; i < 2; i++)
                    {
                        bool right = (i & 1) > 0;

                        editorAnim = LoadAnimation2("EditorAssets", d, 0, 2, right, false, false);
                        if (editorAnim != null && editorAnim.Frames.Count != 0)
                        {
                            var frame = editorAnim.Frames[index];
                            ProcessAnimation(frame.Entry.FrameSpeed, frame.Entry.Frames.Count, frame.Frame.Duration);
                            bool hEven = height % 2 == 0;
                            for (int j = 1; j < height; j++)
                                d.DrawBitmap(frame.Texture,
                                    (x + widthPixels / (right ? 2 : -2)) - (right ? frame.Frame.Width : 0),
                                    (y + (hEven ? frame.Frame.CenterY : -frame.Frame.Height) + (-height / 2 + j) * frame.Frame.Height),
                                    frame.Frame.Width, frame.Frame.Height, false, Transparency);
                        }
                    }
                }
            }
            else if (entity.Object.Name.Name == "SpinSign")
            {
                int type = (int)entity.attributesMap["type"].ValueUInt8;
                bool fliph = false;
                bool flipv = false;
                int animID;
                int frameID = -1;
                switch (type)
                {
                    case 0:
                        animID = 0;
                        break;
                    case 1:
                        animID = 1;
                        break;
                    case 2:
                        animID = 2;
                        break;
                    case 3:
                        animID = 5;
                        frameID = 1;
                        break;
                    default:
                        animID = 3;
                        break;
                }
                var editorAnim = LoadAnimation2("SpinSign", d, animID, frameID, fliph, flipv, false);
                if (editorAnim != null && editorAnim.Frames.Count != 0 && animID >= 0)
                {
                    var frame = editorAnim.Frames[index];

                    ProcessAnimation(frame.Entry.FrameSpeed, frame.Entry.Frames.Count, frame.Frame.Duration);

                    d.DrawBitmap(frame.Texture,
                        x + frame.Frame.CenterX - (fliph ? (frame.Frame.Width - editorAnim.Frames[0].Frame.Width) : 0),
                        y + frame.Frame.CenterY + (flipv ? (frame.Frame.Height - editorAnim.Frames[0].Frame.Height) : 0),
                        frame.Frame.Width, frame.Frame.Height, false, Transparency);
                }
            }
            else if (entity.Object.Name.Name == "EggTV")
            {
                var widthPixels = (int)(entity.attributesMap["size"].ValuePosition.X.High);
                var heightPixels = (int)(entity.attributesMap["size"].ValuePosition.Y.High);
                var width = (int)widthPixels / 16;
                var height = (int)heightPixels / 16;

                EditorAnimation editorAnim;

                if (width != 0 && height != 0)
                {
                    // draw corners
                    for (int i = 0; i < 4; i++)
                    {
                        bool right = (i & 1) > 0;
                        bool bottom = (i & 2) > 0;

                        editorAnim = LoadAnimation2("EditorAssets", d, 0, 0, right, bottom, false);
                        if (editorAnim != null && editorAnim.Frames.Count != 0)
                        {
                            var frame = editorAnim.Frames[index];
                            ProcessAnimation(frame.Entry.FrameSpeed, frame.Entry.Frames.Count, frame.Frame.Duration);
                            d.DrawBitmap(frame.Texture,
                                (x + widthPixels / (right ? 2 : -2)) - (right ? frame.Frame.Width : 0),
                                (y + heightPixels / (bottom ? 2 : -2) - (bottom ? frame.Frame.Height : 0)),
                                frame.Frame.Width, frame.Frame.Height, false, Transparency);

                        }
                    }

                    // draw top and bottom
                    for (int i = 0; i < 2; i++)
                    {
                        bool bottom = (i & 1) > 0;

                        editorAnim = LoadAnimation2("EditorAssets", d, 0, 1, false, bottom, false);
                        if (editorAnim != null && editorAnim.Frames.Count != 0)
                        {
                            var frame = editorAnim.Frames[index];
                            ProcessAnimation(frame.Entry.FrameSpeed, frame.Entry.Frames.Count, frame.Frame.Duration);
                            bool wEven = width % 2 == 0;
                            for (int j = 1; j < width; j++)
                                d.DrawBitmap(frame.Texture,
                                    (x + (wEven ? frame.Frame.CenterX : -frame.Frame.Width) + (-width / 2 + j) * frame.Frame.Width),
                                    (y + heightPixels / (bottom ? 2 : -2) - (bottom ? frame.Frame.Height : 0)),
                                    frame.Frame.Width, frame.Frame.Height, false, Transparency);
                        }
                    }

                    // draw sides
                    for (int i = 0; i < 2; i++)
                    {
                        bool right = (i & 1) > 0;

                        editorAnim = LoadAnimation2("EditorAssets", d, 0, 2, right, false, false);
                        if (editorAnim != null && editorAnim.Frames.Count != 0)
                        {
                            var frame = editorAnim.Frames[index];
                            ProcessAnimation(frame.Entry.FrameSpeed, frame.Entry.Frames.Count, frame.Frame.Duration);
                            bool hEven = height % 2 == 0;
                            for (int j = 1; j < height; j++)
                                d.DrawBitmap(frame.Texture,
                                    (x + widthPixels / (right ? 2 : -2)) - (right ? frame.Frame.Width : 0),
                                    (y + (hEven ? frame.Frame.CenterY : -frame.Frame.Height) + (-height / 2 + j) * frame.Frame.Height),
                                    frame.Frame.Width, frame.Frame.Height, false, Transparency);
                        }
                    }
                }
            }
            else if (entity.Object.Name.Name == "LottoMachine")
            {
                //int type = (int)entity.attributesMap["type"].ValueUInt8;
                //int type = (int)entity.attributesMap["type"].ValueUInt8;
                //int height = (int)entity.attributesMap["height"].ValueUInt8;
                bool fliph = false;
                bool flipv = false;
                bool allowToRender = false;
                var editorAnim = LoadAnimation2("LottoMachine", d, 0, 0, fliph, flipv, false);
                var editorAnim2 = LoadAnimation2("LottoMachine", d, 0, 1, fliph, flipv, false);
                var editorAnim3 = LoadAnimation2("LottoMachine", d, 0, 2, fliph, flipv, false);
                var editorAnim4 = LoadAnimation2("LottoMachine", d, 0, 3, fliph, flipv, false);
                var editorAnim5 = LoadAnimation2("LottoMachine", d, 0, 4, fliph, flipv, false);
                var editorAnim3_2 = LoadAnimation2("LottoMachine", d, 0, 2, true, flipv, false);
                var editorAnim4_2 = LoadAnimation2("LottoMachine", d, 0, 3, true, flipv, false);
                var editorAnim5_2 = LoadAnimation2("LottoMachine", d, 0, 4, true, flipv, false);
                var editorAnim6 = LoadAnimation2("LottoMachine", d, 0, 5, fliph, flipv, false);
                var editorAnim7 = LoadAnimation2("LottoMachine", d, 0, 6, fliph, flipv, false);
                var editorAnim8 = LoadAnimation2("LottoMachine", d, 0, 7, fliph, flipv, false);
                var editorAnim9 = LoadAnimation2("LottoMachine", d, 0, 8, fliph, flipv, false);
                var editorAnim10 = LoadAnimation2("LottoMachine", d, 0, 9, fliph, flipv, false);
                var editorAnim11 = LoadAnimation2("LottoMachine", d, 0, 8, true, false, false);
                var editorAnim12 = LoadAnimation2("LottoMachine", d, 0, 8, true, true, false);
                var editorAnim13 = LoadAnimation2("LottoMachine", d, 0, 8, false, true, false);
                var editorAnim14 = LoadAnimation2("LottoMachine", d, 0, 1, fliph, flipv, false);
                var editorAnim15 = LoadAnimation2("LottoMachine", d, 0, 2, fliph, flipv, false);
                var editorAnim16 = LoadAnimation2("LottoMachine", d, 0, 0, fliph, flipv, false);
                var editorAnim17 = LoadAnimation2("LottoMachine", d, 2, 0, fliph, flipv, false);
                var editorAnim18 = LoadAnimation2("LottoMachine", d, 0, 1, fliph, flipv, false);

                if (editorAnim != null && editorAnim.Frames.Count != 0 && editorAnim2 != null && editorAnim2.Frames.Count != 0)
                {
                    if (editorAnim3 != null && editorAnim3.Frames.Count != 0 && editorAnim4 != null && editorAnim4.Frames.Count != 0)
                    {
                        if (editorAnim5 != null && editorAnim5.Frames.Count != 0 && editorAnim6 != null && editorAnim6.Frames.Count != 0)
                        {
                            if (editorAnim7 != null && editorAnim7.Frames.Count != 0 && editorAnim8 != null && editorAnim8.Frames.Count != 0)
                            {
                                if (editorAnim9 != null && editorAnim9.Frames.Count != 0 && editorAnim10 != null && editorAnim10.Frames.Count != 0)
                                {
                                    if (editorAnim11 != null && editorAnim11.Frames.Count != 0 && editorAnim12 != null && editorAnim12.Frames.Count != 0)
                                    {
                                        if (editorAnim13 != null && editorAnim13.Frames.Count != 0 && editorAnim14 != null && editorAnim14.Frames.Count != 0)
                                        {
                                            if (editorAnim15 != null && editorAnim15.Frames.Count != 0 && editorAnim16 != null && editorAnim16.Frames.Count != 0)
                                            {
                                                if (editorAnim17 != null && editorAnim17.Frames.Count != 0 && editorAnim18 != null && editorAnim18.Frames.Count != 0)
                                                {
                                                    if (editorAnim3_2 != null && editorAnim3_2.Frames.Count != 0 && editorAnim4_2 != null && editorAnim4_2.Frames.Count != 0)
                                                    {
                                                        if (editorAnim5_2 != null && editorAnim5_2.Frames.Count != 0)
                                                        {
                                                            allowToRender = true;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                if (allowToRender == true)
                {
                    var frame = editorAnim.Frames[index];
                    var dispenser = editorAnim2.Frames[index];
                    var ballslot1 = editorAnim3.Frames[index];
                    var ballslot2 = editorAnim4.Frames[index];
                    var ballslot3 = editorAnim5.Frames[index];
                    var frame6 = editorAnim6.Frames[index];
                    var frame7 = editorAnim7.Frames[index];
                    var galloplogo = editorAnim8.Frames[index];
                    var frame9 = editorAnim9.Frames[index];
                    var frame10 = editorAnim10.Frames[index];
                    var frame11 = editorAnim11.Frames[index];
                    var frame12 = editorAnim12.Frames[index];
                    var frame13 = editorAnim13.Frames[index];
                    var frame14 = editorAnim14.Frames[index];
                    var frame15 = editorAnim15.Frames[index];
                    var frame16 = editorAnim16.Frames[index];
                    var chute = editorAnim17.Frames[index];
                    var ballslot1_2 = editorAnim3_2.Frames[index];
                    var ballslot2_2 = editorAnim4_2.Frames[index];
                    var ballslot3_2 = editorAnim5_2.Frames[index];

                    //ProcessAnimation(frame.Entry.FrameSpeed, frame.Entry.Frames.Count, frame.Frame.Duration);
                    d.DrawBitmap(frame9.Texture,
    x + frame9.Frame.CenterX - (fliph ? (frame9.Frame.Width - editorAnim9.Frames[0].Frame.Width) : 0),
    y + frame9.Frame.CenterY + (flipv ? (frame9.Frame.Height - editorAnim9.Frames[0].Frame.Height) : 0),
    frame9.Frame.Width, frame9.Frame.Height, false, Transparency);
                    d.DrawBitmap(frame10.Texture,
                        x + frame10.Frame.CenterX - (fliph ? (frame10.Frame.Width - editorAnim10.Frames[0].Frame.Width) : 0),
                        y + frame10.Frame.CenterY + (flipv ? (frame10.Frame.Height - editorAnim10.Frames[0].Frame.Height) : 0),
                        frame10.Frame.Width, frame10.Frame.Height, false, Transparency);
                    d.DrawBitmap(frame11.Texture,
    x + frame11.Frame.Width + 32 + frame11.Frame.CenterX - (true ? (frame11.Frame.Width - editorAnim11.Frames[0].Frame.Width) : 0),
    y + frame11.Frame.CenterY + (false ? (frame11.Frame.Height - editorAnim11.Frames[0].Frame.Height) : 0),
    frame11.Frame.Width, frame11.Frame.Height, false, Transparency);
                    d.DrawBitmap(frame12.Texture,
x + frame12.Frame.Width + 32 + frame12.Frame.CenterX - (true ? (frame12.Frame.Width - editorAnim12.Frames[0].Frame.Width) : 0),
y + frame12.Frame.Height + frame12.Frame.CenterY + (true ? (frame12.Frame.Height - editorAnim12.Frames[0].Frame.Height) : 0),
frame12.Frame.Width, frame12.Frame.Height, false, Transparency);
                    d.DrawBitmap(frame13.Texture,
x + frame13.Frame.CenterX - (false ? (frame13.Frame.Width - editorAnim13.Frames[0].Frame.Width) : 0),
y + frame13.Frame.Height + frame13.Frame.CenterY + (true ? (frame13.Frame.Height - editorAnim13.Frames[0].Frame.Height) : 0),
frame13.Frame.Width, frame13.Frame.Height, false, Transparency);
                        d.DrawBitmap(ballslot1.Texture,
                            x + ballslot1.Frame.CenterX - (fliph ? (ballslot1.Frame.Width - editorAnim3.Frames[0].Frame.Width) : 0),
                            y + ballslot1.Frame.CenterY + (flipv ? (ballslot1.Frame.Height - editorAnim3.Frames[0].Frame.Height) : 0),
                            ballslot1.Frame.Width, ballslot1.Frame.Height, false, Transparency);
                        d.DrawBitmap(ballslot2.Texture,
                            x + ballslot2.Frame.CenterX - (fliph ? (ballslot2.Frame.Width - editorAnim4.Frames[0].Frame.Width) : 0),
                            y + ballslot2.Frame.CenterY + (flipv ? (ballslot2.Frame.Height - editorAnim4.Frames[0].Frame.Height) : 0),
                            ballslot2.Frame.Width, ballslot2.Frame.Height, false, Transparency);
                    d.DrawBitmap(ballslot3.Texture,
    x + ballslot3.Frame.CenterX - (fliph ? (ballslot3.Frame.Width - editorAnim5.Frames[0].Frame.Width) : 0),
    y + ballslot3.Frame.CenterY + (flipv ? (ballslot3.Frame.Height - editorAnim5.Frames[0].Frame.Height) : 0),
    ballslot3.Frame.Width, ballslot3.Frame.Height, false, Transparency);
                                            d.DrawBitmap(ballslot1_2.Texture,
                            x - ballslot1_2.Frame.Width - ballslot1_2.Frame.CenterX + (fliph ? (ballslot1_2.Frame.Width + editorAnim3.Frames[0].Frame.Width) : 0),
                            y + ballslot1_2.Frame.CenterY + (flipv ? (ballslot1_2.Frame.Height - editorAnim3.Frames[0].Frame.Height) : 0),
                            ballslot1_2.Frame.Width, ballslot1_2.Frame.Height, false, Transparency);
                        d.DrawBitmap(ballslot2_2.Texture,
                            x - ballslot2_2.Frame.Width - ballslot2_2.Frame.CenterX + (fliph ? (ballslot2_2.Frame.Width + editorAnim4.Frames[0].Frame.Width) : 0),
                            y + ballslot2_2.Frame.CenterY + (flipv ? (ballslot2_2.Frame.Height - editorAnim4.Frames[0].Frame.Height) : 0),
                            ballslot2_2.Frame.Width, ballslot2_2.Frame.Height, false, Transparency);
                    d.DrawBitmap(ballslot3_2.Texture,
    x - ballslot3_2.Frame.Width - ballslot3_2.Frame.CenterX + (fliph ? (ballslot3_2.Frame.Width + editorAnim5.Frames[0].Frame.Width) : 0),
    y + ballslot3_2.Frame.CenterY + (flipv ? (ballslot3_2.Frame.Height - editorAnim5.Frames[0].Frame.Height) : 0),
    ballslot3_2.Frame.Width, ballslot3_2.Frame.Height, false, Transparency);
                    d.DrawBitmap(galloplogo.Texture,
                        x + galloplogo.Frame.CenterX - (fliph ? (galloplogo.Frame.Width - editorAnim8.Frames[0].Frame.Width) : 0),
                        y + galloplogo.Frame.CenterY + (flipv ? (galloplogo.Frame.Height - editorAnim8.Frames[0].Frame.Height) : 0),
                        galloplogo.Frame.Width, galloplogo.Frame.Height, false, Transparency);
                    d.DrawBitmap(chute.Texture,
x + chute.Frame.CenterX - (fliph ? (chute.Frame.Width - editorAnim17.Frames[0].Frame.Width) : 0),
y + chute.Frame.CenterY + 132 + (flipv ? (chute.Frame.Height - editorAnim17.Frames[0].Frame.Height) : 0),
chute.Frame.Width, chute.Frame.Height, false, Transparency);
                    d.DrawBitmap(dispenser.Texture,
        x + dispenser.Frame.CenterX - (fliph ? (dispenser.Frame.Width - editorAnim2.Frames[0].Frame.Width) : 0),
        y + dispenser.Frame.CenterY + (flipv ? (dispenser.Frame.Height - editorAnim2.Frames[0].Frame.Height) : 0),
        dispenser.Frame.Width, dispenser.Frame.Height, false, Transparency);




                }
            }
            else if (entity.Object.Name.Name == "Funnel")
            {
                //int type = (int)entity.attributesMap["type"].ValueUInt8;
                //int direction = (int)entity.attributesMap["direction"].ValueUInt8;
                bool fliph = false;
                bool flipv = false;
                int animID = 0;
                var editorAnim = LoadAnimation2("Funnel", d, animID, -1, fliph, flipv, false);
                if (editorAnim != null && editorAnim.Frames.Count != 0 && animID >= 0)
                {
                    var frame = editorAnim.Frames[0];
                    var frame2 = editorAnim.Frames[1];

                    //ProcessAnimation(frame.Entry.FrameSpeed, frame.Entry.Frames.Count, frame.Frame.Duration);

                    d.DrawBitmap(frame.Texture,
                        x + frame.Frame.CenterX - (fliph ? (frame.Frame.Width - editorAnim.Frames[0].Frame.Width) : 0),
                        y + frame.Frame.CenterY + (flipv ? (frame.Frame.Height - editorAnim.Frames[0].Frame.Height) : 0),
                        frame.Frame.Width, frame.Frame.Height, false, Transparency);
                    d.DrawBitmap(frame2.Texture,
                        x + frame2.Frame.CenterX - (fliph ? (frame2.Frame.Width - editorAnim.Frames[0].Frame.Width) : 0),
                        y + frame2.Frame.CenterY + (flipv ? (frame2.Frame.Height - editorAnim.Frames[0].Frame.Height) : 0),
                        frame2.Frame.Width, frame2.Frame.Height, false, Transparency);
                }
            }
            else if (entity.Object.Name.Name == "DNARiser")
            {
                //int type = (int)entity.attributesMap["type"].ValueUInt8;
                //int direction = (int)entity.attributesMap["direction"].ValueUInt8;
                bool fliph = false;
                bool flipv = false;
                var editorAnim = LoadAnimation2("DNARiser", d, 0, -1, fliph, flipv, false);
                var editorAnim2 = LoadAnimation2("DNARiser", d, 2, -1, fliph, flipv, false);
                var editorAnim3 = LoadAnimation2("DNARiser", d, 4, -1, fliph, flipv, false);
                if (editorAnim != null && editorAnim.Frames.Count != 0 && editorAnim2 != null && editorAnim2.Frames.Count != 0 && editorAnim3 != null && editorAnim3.Frames.Count != 0)
                {
                    var frame = editorAnim.Frames[0];
                    var frame2 = editorAnim2.Frames[0];
                    var frame3 = editorAnim3.Frames[0];

                    //ProcessAnimation(frame.Entry.FrameSpeed, frame.Entry.Frames.Count, frame.Frame.Duration);

                    d.DrawBitmap(frame.Texture,
                        x + frame.Frame.CenterX - (fliph ? (frame.Frame.Width - editorAnim.Frames[0].Frame.Width) : 0),
                        y + frame.Frame.CenterY + (flipv ? (frame.Frame.Height - editorAnim.Frames[0].Frame.Height) : 0),
                        frame.Frame.Width, frame.Frame.Height, false, Transparency);
                    d.DrawBitmap(frame2.Texture,
                        x + frame2.Frame.CenterX - 24 - (fliph ? (frame2.Frame.Width - editorAnim.Frames[0].Frame.Width) : 0),
                        y + frame2.Frame.CenterY + (flipv ? (frame2.Frame.Height - editorAnim.Frames[0].Frame.Height) : 0),
                        frame2.Frame.Width, frame2.Frame.Height, false, Transparency);
                    d.DrawBitmap(frame3.Texture,
                        x + frame3.Frame.CenterX + 24 + (fliph ? (frame3.Frame.Width - editorAnim.Frames[0].Frame.Width) : 0),
                        y + frame3.Frame.CenterY + (flipv ? (frame3.Frame.Height - editorAnim.Frames[0].Frame.Height) : 0),
                        frame3.Frame.Width, frame3.Frame.Height, false, Transparency);
                }
            }
            else if (entity.Object.Name.Name == "CaterkillerJr")
            {
                //int type = (int)entity.attributesMap["type"].ValueUInt8;
                //int direction = (int)entity.attributesMap["direction"].ValueUInt8;
                bool fliph = false;
                bool flipv = false;
                var editorAnim = LoadAnimation2("CaterkillerJr", d, 0, -1, true, flipv, false);
                var editorAnim2 = LoadAnimation2("CaterkillerJr", d, 1, -1, fliph, flipv, false);
                var editorAnim3 = LoadAnimation2("CaterkillerJr", d, 2, -1, fliph, flipv, false);
                var editorAnim4 = LoadAnimation2("CaterkillerJr", d, 3, -1, fliph, flipv, false);
                if (editorAnim != null && editorAnim.Frames.Count != 0 && editorAnim2 != null && editorAnim2.Frames.Count != 0 && editorAnim3 != null && editorAnim3.Frames.Count != 0 && editorAnim4 != null && editorAnim4.Frames.Count != 0)
                {
                    var frame = editorAnim.Frames[0];
                    var frame2 = editorAnim2.Frames[0];
                    var frame3 = editorAnim3.Frames[0];
                    var frame4 = editorAnim4.Frames[0];

                    //ProcessAnimation(frame.Entry.FrameSpeed, frame.Entry.Frames.Count, frame.Frame.Duration);

                    d.DrawBitmap(frame.Texture,
                        x + frame.Frame.CenterX - (fliph ? (frame.Frame.Width - editorAnim.Frames[0].Frame.Width) : 0),
                        y + frame.Frame.CenterY + (flipv ? (frame.Frame.Height - editorAnim.Frames[0].Frame.Height) : 0),
                        frame.Frame.Width, frame.Frame.Height, false, Transparency);
                    for (int i = 0; i <= 6; i++)
                    {
                        if (i <= 3 && i >= 1)
                        {
                            d.DrawBitmap(frame2.Texture,
                                x + frame2.Frame.CenterX + (i*frame2.Frame.Width) + (fliph ? (frame2.Frame.Width - editorAnim2.Frames[0].Frame.Width) : 0),
                                y + frame2.Frame.CenterY + (flipv ? (frame2.Frame.Height - editorAnim2.Frames[0].Frame.Height) : 0),
                                frame2.Frame.Width, frame2.Frame.Height, false, Transparency);
                        }
                        if (i == 4)
                        {
                            d.DrawBitmap(frame3.Texture,
                                x + frame3.Frame.CenterX + (i*frame3.Frame.Width) + (frame2.Frame.Width - 5) + (fliph ? (frame3.Frame.Width - editorAnim3.Frames[0].Frame.Width) : 0),
                                y + frame3.Frame.CenterY + (flipv ? (frame3.Frame.Height - editorAnim3.Frames[0].Frame.Height) : 0),
                                frame3.Frame.Width, frame3.Frame.Height, false, Transparency);
                        }
                        if (i >= 5)
                        {
                            d.DrawBitmap(frame4.Texture,
                                x + frame4.Frame.CenterX + (i*frame4.Frame.Width) + frame2.Frame.Width + frame3.Frame.Width + (fliph ? (frame4.Frame.Width - editorAnim4.Frames[0].Frame.Width) : 0),
                                y + frame4.Frame.CenterY + (flipv ? (frame4.Frame.Height - editorAnim4.Frames[0].Frame.Height) : 0),
                                frame4.Frame.Width, frame4.Frame.Height, false, Transparency);
                        }
                    }


                }
            }
            else if (entity.Object.Name.Name == "Grabber")
            {
                //int type = (int)entity.attributesMap["type"].ValueUInt8;
                int direction = (int)entity.attributesMap["direction"].ValueUInt8;
                bool fliph = false;
                bool flipv = false;
                if (direction == 1)
                {
                    fliph = true;
                }
                var editorAnim = LoadAnimation2("Grabber", d, 0, -1, fliph, flipv, false);
                var editorAnim2 = LoadAnimation2("Grabber", d, 1, -1, fliph, flipv, false);
                if (editorAnim != null && editorAnim.Frames.Count != 0 && editorAnim2 != null && editorAnim2.Frames.Count != 0)
                {
                    var frame = editorAnim.Frames[index];
                    var frame2 = editorAnim2.Frames[index];

                    ProcessAnimation(frame.Entry.FrameSpeed, frame.Entry.Frames.Count, frame.Frame.Duration);
                    ProcessAnimation(frame2.Entry.FrameSpeed, frame2.Entry.Frames.Count, frame2.Frame.Duration);

                    d.DrawBitmap(frame.Texture,
                        x + frame.Frame.CenterX - (fliph ? (frame.Frame.Width - editorAnim.Frames[0].Frame.Width) : 0),
                        y + frame.Frame.CenterY + (flipv ? (frame.Frame.Height - editorAnim.Frames[0].Frame.Height) : 0),
                        frame.Frame.Width, frame.Frame.Height, false, Transparency);
                    d.DrawBitmap(frame2.Texture,
                        x + frame2.Frame.CenterX - (fliph ? (frame2.Frame.Width - 2) : 0),
                        y + frame2.Frame.CenterY + (flipv ? (frame2.Frame.Height - editorAnim2.Frames[0].Frame.Height) : 0),
                        frame2.Frame.Width, frame2.Frame.Height, false, Transparency);
                }
            }
            else if (entity.Object.Name.Name == "Letterboard")
            {
                int letterID = (int)entity.attributesMap["letterID"].ValueUInt8;
                bool controller = entity.attributesMap["controller"].ValueBool;
                bool fliph = false;
                bool flipv = false;
                var editorAnim = LoadAnimation2("Letterboard", d, 0, -1, fliph, flipv, false);
                var editorAnim2 = LoadAnimation2("Letterboard", d, 1, letterID - 1, fliph, flipv, false);
                if (editorAnim != null && editorAnim.Frames.Count != 0 && editorAnim2 != null && editorAnim2.Frames.Count != 0)
                {
                    var frame = editorAnim.Frames[0];
                    var frame2 = editorAnim2.Frames[index];

                    //ProcessAnimation(frame.Entry.FrameSpeed, frame.Entry.Frames.Count, frame.Frame.Duration);

                    if (letterID == 0 || controller == true)
                    {
                        d.DrawBitmap(frame.Texture,
                            x + frame.Frame.CenterX - (fliph ? (frame.Frame.Width - editorAnim.Frames[0].Frame.Width) : 0),
                            y + frame.Frame.CenterY + (flipv ? (frame.Frame.Height - editorAnim.Frames[0].Frame.Height) : 0),
                            frame.Frame.Width, frame.Frame.Height, false, Transparency);
                    }
                    else
                    {
                        d.DrawBitmap(frame2.Texture,
                            x + frame.Frame.CenterX - (fliph ? (frame.Frame.Width - editorAnim.Frames[letterID].Frame.Width) : 0),
                            y + frame.Frame.CenterY + (flipv ? (frame.Frame.Height - editorAnim.Frames[letterID].Frame.Height) : 0),
                            frame.Frame.Width, frame.Frame.Height, false, Transparency);
                    }

                }
            }
            else if (entity.Object.Name.Name == "Water")
            {
                int type = (int)entity.attributesMap["type"].ValueVar;
                var widthPixels = (int)(entity.attributesMap["size"].ValuePosition.X.High);
                var heightPixels = (int)(entity.attributesMap["size"].ValuePosition.Y.High);
                var width = (int)widthPixels / 16;
                var height = (int)heightPixels / 16;
                bool fliph = false;
                bool flipv = false;
                int animID;
                if (type == 2)
                {
                    animID = 2;
                }
                else
                {
                    animID = 0;
                }
                var editorAnim = LoadAnimation2("Water", d, animID, -1, fliph, flipv, false);
                if (editorAnim != null && editorAnim.Frames.Count != 0 && animID >= 0 && type != 1)
                {
                    var frame = editorAnim.Frames[index];

                    ProcessAnimation(frame.Entry.FrameSpeed, frame.Entry.Frames.Count, frame.Frame.Duration);

                        d.DrawBitmap(frame.Texture,
    x + frame.Frame.CenterX - (fliph ? (frame.Frame.Width - editorAnim.Frames[0].Frame.Width) : 0),
    y + frame.Frame.CenterY + (flipv ? (frame.Frame.Height - editorAnim.Frames[0].Frame.Height) : 0),
    frame.Frame.Width, frame.Frame.Height, false, Transparency);

                }
                else if (width != 0 && height != 0 && type == 1)
                {
                    // draw corners
                    for (int i = 0; i < 4; i++)
                    {
                        bool right = (i & 1) > 0;
                        bool bottom = (i & 2) > 0;

                        editorAnim = LoadAnimation2("EditorAssets", d, 0, 0, right, bottom, false);
                        if (editorAnim != null && editorAnim.Frames.Count != 0)
                        {
                            var frame = editorAnim.Frames[index];
                            ProcessAnimation(frame.Entry.FrameSpeed, frame.Entry.Frames.Count, frame.Frame.Duration);
                            d.DrawBitmap(frame.Texture,
                                (x + widthPixels / (right ? 2 : -2)) - (right ? frame.Frame.Width : 0),
                                (y + heightPixels / (bottom ? 2 : -2) - (bottom ? frame.Frame.Height : 0)),
                                frame.Frame.Width, frame.Frame.Height, false, Transparency);

                        }
                    }

                    // draw top and bottom
                    for (int i = 0; i < 2; i++)
                    {
                        bool bottom = (i & 1) > 0;

                        editorAnim = LoadAnimation2("EditorAssets", d, 0, 1, false, bottom, false);
                        if (editorAnim != null && editorAnim.Frames.Count != 0)
                        {
                            var frame = editorAnim.Frames[index];
                            ProcessAnimation(frame.Entry.FrameSpeed, frame.Entry.Frames.Count, frame.Frame.Duration);
                            bool wEven = width % 2 == 0;
                            for (int j = 1; j < width; j++)
                                d.DrawBitmap(frame.Texture,
                                    (x + (wEven ? frame.Frame.CenterX : -frame.Frame.Width) + (-width / 2 + j) * frame.Frame.Width),
                                    (y + heightPixels / (bottom ? 2 : -2) - (bottom ? frame.Frame.Height : 0)),
                                    frame.Frame.Width, frame.Frame.Height, false, Transparency);
                        }
                    }

                    // draw sides
                    for (int i = 0; i < 2; i++)
                    {
                        bool right = (i & 1) > 0;

                        editorAnim = LoadAnimation2("EditorAssets", d, 0, 2, right, false, false);
                        if (editorAnim != null && editorAnim.Frames.Count != 0)
                        {
                            var frame = editorAnim.Frames[index];
                            ProcessAnimation(frame.Entry.FrameSpeed, frame.Entry.Frames.Count, frame.Frame.Duration);
                            bool hEven = height % 2 == 0;
                            for (int j = 1; j < height; j++)
                                d.DrawBitmap(frame.Texture,
                                    (x + widthPixels / (right ? 2 : -2)) - (right ? frame.Frame.Width : 0),
                                    (y + (hEven ? frame.Frame.CenterY : -frame.Frame.Height) + (-height / 2 + j) * frame.Frame.Height),
                                    frame.Frame.Width, frame.Frame.Height, false, Transparency);
                        }
                    }
                }
            }





            else if (entity.Object.Name.Name == "TeeterTotter")

            {
                var value = entity.attributesMap["length"].ValueUInt32 / 2;
                var editorAnim = LoadAnimation2("TeeterTotter", d, 0, 0, false, false, false);
                if (editorAnim != null && editorAnim.Frames.Count != 0)
                {
                    var frame = editorAnim.Frames[0];
                    for (int i = -(int)value; i < value + 1; ++i)
                    {
                        d.DrawBitmap(frame.Texture, x + frame.Frame.CenterX + (i * (frame.Frame.Width + 2)),
                            y + frame.Frame.CenterY, frame.Frame.Width, frame.Frame.Height, false, Transparency);
                    }
                }
            }
            else if (entity.Object.Name.Name == "Spikes")
            {
                var value = entity.attributesMap["type"];
                bool fliph = false;
                bool flipv = false;
                int animID = 0;

                // Down
                if (value.ValueVar == 1)
                {
                    flipv = true;
                    animID = 0;
                }
                // Right
                if (value.ValueVar == 2)
                {
                    animID = 1;
                }
                // Left
                if (value.ValueVar == 3)
                {
                    fliph = true;
                    animID = 1;
                }

                int count = (entity.attributesMap.ContainsKey("count") ? (int)entity.attributesMap["count"].ValueUInt8 : 2);

                // Is it a value that defaults to 2?
                if (count < 2)
                    count = 2;

                int offset1 = 0, offset2 = 0;
                bool extra = false;
                count *= 2; // I made all this with an incorrect assumption so here's a cheap fix
                int count2 = count >> 2;
                var editorAnim = LoadAnimation2("Spikes", d, animID, 0, fliph, flipv, false);
                if (editorAnim != null && editorAnim.Frames.Count != 0)
                {
                    var frame = editorAnim.Frames[0];

                    if (value.ValueVar == 0 || value.ValueVar == 1)
                    {
                        // Is count indivisible by 4?
                        if (count % 4 != 0)
                        {
                            offset1 = frame.Frame.Width / 4;
                            count -= 2;
                            extra = true;
                        }

                        // Is count divisible by 8?
                        if (count % 8 == 0)
                        {
                            offset2 = frame.Frame.Width / 2;
                        }

                        // Draw each set of spikes
                        int max = (count2 + 1) / 2;
                        for (int i = -count2 / 2; i < max; ++i)
                        {
                            d.DrawBitmap(frame.Texture, x + frame.Frame.CenterX + (i * (frame.Frame.Width)) - offset1 + offset2, y + frame.Frame.CenterY,
                                frame.Frame.Width, frame.Frame.Height, false, Transparency);
                        }

                        // Draw one more overlapping if needed
                        if (extra)
                        {
                            d.DrawBitmap(frame.Texture, x + frame.Frame.CenterX + ((max - 1) * (frame.Frame.Width)) + offset1 + offset2, y + frame.Frame.CenterY,
                                frame.Frame.Width, frame.Frame.Height, false, Transparency);
                        }
                    }
                    else if (value.ValueVar == 2 || value.ValueVar == 3)
                    {
                        // Is count indivisible by 4?
                        if (count % 4 != 0)
                        {
                            offset1 = frame.Frame.Height / 4;
                            count -= 2;
                            extra = true;
                        }

                        // Is count divisible by 8?
                        if (count % 8 == 0)
                        {
                            offset2 = frame.Frame.Height / 2;
                        }

                        // Draw each set of spikes
                        int max = (count2 + 1) / 2;
                        for (int i = -count2 / 2; i < max; ++i)
                        {
                            d.DrawBitmap(frame.Texture, x + frame.Frame.CenterX, y + frame.Frame.CenterY + (i * (frame.Frame.Height)) - offset1 + offset2,
                                frame.Frame.Width, frame.Frame.Height, false, Transparency);
                        }

                        // Draw one more overlapping if needed
                        if (extra)
                        {
                            d.DrawBitmap(frame.Texture, x + frame.Frame.CenterX, y + frame.Frame.CenterY + ((max - 1) * (frame.Frame.Height)) + offset1 + offset2,
                                frame.Frame.Width, frame.Frame.Height, false, Transparency);
                        }
                    }

                }

            }
            else if (entity.Object.Name.Name == "Spring")
            {
                int animID = (int)entity.attributesMap["type"].ValueVar;
                var flipFlag = entity.attributesMap["flipFlag"].ValueVar;
                bool fliph = false;
                bool flipv = false;

                // Handle springs being flipped in both planes
                // Down
                if ((flipFlag & 0x02) == 0x02)
                    flipv = true;
                // Left
                if ((flipFlag & 0x01) == 0x01)
                    fliph = true;

                var editorAnim = LoadAnimation2("Springs", d, animID % 6, -1, fliph, flipv, false);
                if (editorAnim != null && editorAnim.Frames.Count != 0 && animID >= 0)
                {
                    var frame = editorAnim.Frames[index];

                    ProcessAnimation(frame.Entry.FrameSpeed, frame.Entry.Frames.Count, frame.Frame.Duration);

                    d.DrawBitmap(frame.Texture,
                        x + frame.Frame.CenterX - (fliph ? (frame.Frame.Width - editorAnim.Frames[0].Frame.Width) : 0),
                        y + frame.Frame.CenterY + (flipv ? (frame.Frame.Height - editorAnim.Frames[0].Frame.Height) : 0),
                        frame.Frame.Width, frame.Frame.Height, false, Transparency);
                }
            }
            else if (entity.Object.Name.Name == "Player")
            {
                int id = (int)entity.attributesMap["characterID"].ValueVar;
                if (id > 7)
                {
                    entity.attributesMap["characterID"].ValueVar = 7u;
                }
                var editorAnim = LoadAnimation2("PlayerIcons", d, 0, id, false, false, false);
                if (editorAnim != null && editorAnim.Frames.Count != 0)
                {
                    var frame = editorAnim.Frames[index];
                    d.DrawBitmap(frame.Texture, x + frame.Frame.CenterX, y + frame.Frame.CenterY,
                        frame.Frame.Width, frame.Frame.Height, false, Transparency);
                }
            }
            else if (entity.Object.Name.Name == "SignPost")
            {
                var editorAnim = LoadAnimation2("SignPost", d, 0, -1, false, false, false);
                if (editorAnim != null && editorAnim.Frames.Count != 0)
                {
                    var frame = editorAnim.Frames[index];
                    ProcessAnimation(1, frame.Entry.Frames.Count, frame.Frame.Duration);
                    d.DrawBitmap(frame.Texture, x + frame.Frame.CenterX, y + frame.Frame.CenterY,
                        frame.Frame.Width, frame.Frame.Height, false, Transparency);
                }
                editorAnim = LoadAnimation2("SignPost", d, 4, -1, false, false, false);
                if (editorAnim != null && editorAnim.Frames.Count != 0)
                {
                    for (int i = 0; i < editorAnim.Frames.Count; ++i)
                    {
                        if (i == 1)
                            continue;
                        var frame = editorAnim.Frames[i];
                        d.DrawBitmap(frame.Texture, x + frame.Frame.CenterX, y + frame.Frame.CenterY,
                            frame.Frame.Width, frame.Frame.Height, false, Transparency);
                    }
                }
            }
            else if (entity.Object.Name.Name == "TimeAttackGate")
            {
                bool finish = entity.attributesMap["finishLine"].ValueBool;
                var editorAnimBase = LoadAnimation2("SpeedGate", d, 0, 0, false, false, false);
                var editorAnimTop = LoadAnimation2("SpeedGate", d, 1, 0, false, false, false);
                var editorAnimFins = LoadAnimation2("SpeedGate", d, finish ? 4 : 3, -1, false, false, false);
                if (editorAnimBase != null && editorAnimTop != null && editorAnimFins != null)
                {
                    var frameBase = editorAnimBase.Frames[0];
                    var frameTop = editorAnimTop.Frames[0];
                    d.DrawBitmap(frameBase.Texture, x + frameBase.Frame.CenterX, y + frameBase.Frame.CenterY,
                        frameBase.Frame.Width, frameBase.Frame.Height, false, Transparency);
                    d.DrawBitmap(frameTop.Texture, x + frameTop.Frame.CenterX, y + frameTop.Frame.CenterY,
                        frameTop.Frame.Width, frameTop.Frame.Height, false, Transparency);
                    for (int i = 0; i < editorAnimFins.Frames.Count; ++i)
                    {
                        var frame = editorAnimFins.Frames[i];
                        d.DrawBitmap(frame.Texture, x + frame.Frame.CenterX, y + frame.Frame.CenterY,
                            frame.Frame.Width, frame.Frame.Height, false, Transparency);
                    }
                }
            }
            else if (entity.Object.Name.Name == "HUD")
            {
                var editorAnim = LoadAnimation2("EditorIcons", d, 0, 0, false, false, false);
                if (editorAnim != null && editorAnim.Frames.Count != 0)
                {
                    var frame = editorAnim.Frames[index];
                    ProcessAnimation(frame.Entry.FrameSpeed, frame.Entry.Frames.Count, frame.Frame.Duration);
                    d.DrawBitmap(frame.Texture, x + frame.Frame.CenterX, y + frame.Frame.CenterY,
                        frame.Frame.Width, frame.Frame.Height, false, Transparency);
                }
            }
            else if (entity.Object.Name.Name == "Music")
            {
                var editorAnim = LoadAnimation2("EditorIcons", d, 0, 1, false, false, false);
                if (editorAnim != null && editorAnim.Frames.Count != 0)
                {
                    var frame = editorAnim.Frames[index];
                    ProcessAnimation(frame.Entry.FrameSpeed, frame.Entry.Frames.Count, frame.Frame.Duration);
                    d.DrawBitmap(frame.Texture, x + frame.Frame.CenterX, y + frame.Frame.CenterY,
                        frame.Frame.Width, frame.Frame.Height, false, Transparency);
                }
            }
            else if (entity.Object.Name.Name == "BoundsMarker")
            {
                var editorAnim = LoadAnimation2("EditorIcons", d, 0, 2, false, false, false);
                if (editorAnim != null && editorAnim.Frames.Count != 0)
                {
                    var frame = editorAnim.Frames[index];
                    ProcessAnimation(frame.Entry.FrameSpeed, frame.Entry.Frames.Count, frame.Frame.Duration);
                    d.DrawBitmap(frame.Texture, x + frame.Frame.CenterX, y + frame.Frame.CenterY,
                        frame.Frame.Width, frame.Frame.Height, false, Transparency);
                }
            }
            else if (entity.Object.Name.Name == "TitleCard")
            {
                var editorAnim = LoadAnimation2("EditorIcons", d, 0, 3, false, false, false);
                if (editorAnim != null && editorAnim.Frames.Count != 0)
                {
                    var frame = editorAnim.Frames[index];
                    ProcessAnimation(frame.Entry.FrameSpeed, frame.Entry.Frames.Count, frame.Frame.Duration);
                    d.DrawBitmap(frame.Texture, x + frame.Frame.CenterX, y + frame.Frame.CenterY,
                        frame.Frame.Width, frame.Frame.Height, false, Transparency);
                }
            }
            else if (entity.Object.Name.Name == "CorkscrewPath")
            {
                var editorAnim = LoadAnimation2("EditorIcons", d, 0, 4, false, false, false);
                if (editorAnim != null && editorAnim.Frames.Count != 0)
                {
                    var frame = editorAnim.Frames[index];
                    ProcessAnimation(frame.Entry.FrameSpeed, frame.Entry.Frames.Count, frame.Frame.Duration);
                    d.DrawBitmap(frame.Texture, x + frame.Frame.CenterX, y + frame.Frame.CenterY,
                        frame.Frame.Width, frame.Frame.Height, false, Transparency);
                }
            }
            else if (entity.Object.Name.Name == "BGSwitch")
            {
                var editorAnim = LoadAnimation2("EditorIcons", d, 0, 5, false, false, false);
                if (editorAnim != null && editorAnim.Frames.Count != 0)
                {
                    var frame = editorAnim.Frames[index];
                    ProcessAnimation(frame.Entry.FrameSpeed, frame.Entry.Frames.Count, frame.Frame.Duration);
                    d.DrawBitmap(frame.Texture, x + frame.Frame.CenterX, y + frame.Frame.CenterY,
                        frame.Frame.Width, frame.Frame.Height, false, Transparency);
                }
            }
            else if (entity.Object.Name.Name == "ForceSpin")
            {

                var size = (int)(entity.attributesMap["size"].ValueVar) - 1;
                var angle = entity.attributesMap["angle"].ValueInt32;

                EditorAnimation editorAnim = LoadAnimation2("PlaneSwitch", d, 0, 4, true, false, false);

                const int pivotOffsetX = -8, pivotOffsetY = 0;
                const int drawOffsetX = 0, drawOffsetY = -8;

                if (editorAnim != null && editorAnim.Frames.Count != 0)
                {
                    var frame = editorAnim.Frames[index];
                    ProcessAnimation(frame.Entry.FrameSpeed, frame.Entry.Frames.Count, frame.Frame.Duration);
                    bool hEven = size % 2 == 0;
                    for (int yy = 0; yy <= size; ++yy)
                    {
                        int[] drawCoords = RotatePoints(
                            x - frame.Frame.Width/2,
                            (y + (hEven ? frame.Frame.CenterY : -frame.Frame.Height) + (-size / 2 + yy) * frame.Frame.Height),
                            x + pivotOffsetX, y + pivotOffsetY, angle);

                        d.DrawBitmap(frame.Texture, drawCoords[0] + drawOffsetX, drawCoords[1] + drawOffsetY, frame.Frame.Width, frame.Frame.Height, false, Transparency);
                    }
                }
            }
            /*else if (entity.Object.Name.Name == "SpinBooster2")
            {

                var size = (int)(entity.attributesMap["size"].ValueVar) - 1;
                //var angle = entity.attributesMap["angle"].ValueInt32;

                EditorAnimation editorAnim = LoadAnimation2("PlaneSwitch", d, 0, 4, true, false, false);

                const int pivotOffsetX = -8, pivotOffsetY = 0;
                const int drawOffsetX = 0, drawOffsetY = -8;

                if (editorAnim != null && editorAnim.Frames.Count != 0)
                {
                    var frame = editorAnim.Frames[index];
                    ProcessAnimation(frame.Entry.FrameSpeed, frame.Entry.Frames.Count, frame.Frame.Duration);
                    bool hEven = size % 2 == 0;
                    for (int yy = 0; yy <= size; ++yy)
                    {
                        int[] drawCoords = RotatePoints(
                            x - frame.Frame.Width / 2,
                            (y + (hEven ? frame.Frame.CenterY : -frame.Frame.Height) + (-size / 2 + yy) * frame.Frame.Height),
                            x + pivotOffsetX, y + pivotOffsetY, angle);

                        d.DrawBitmap(frame.Texture, drawCoords[0] + drawOffsetX, drawCoords[1] + drawOffsetY, frame.Frame.Width, frame.Frame.Height, false, Transparency);
                    }
                }
            }*/
            else if (entity.Object.Name.Name == "UIControl")
            {
                var editorAnim = LoadAnimation2("EditorIcons", d, 0, 7, false, false, false);
                if (editorAnim != null && editorAnim.Frames.Count != 0)
                {
                    var frame = editorAnim.Frames[index];
                    ProcessAnimation(frame.Entry.FrameSpeed, frame.Entry.Frames.Count, frame.Frame.Duration);
                    d.DrawBitmap(frame.Texture, x + frame.Frame.CenterX, y + frame.Frame.CenterY,
                        frame.Frame.Width, frame.Frame.Height, false, Transparency);
                }
            }
            else if (entity.Object.Name.Name == "WaterGush")
            {
                var length = (int)(entity.attributesMap["length"].ValueUInt32);
                var editorAnim = LoadAnimation2("WaterGush", d, 0, -1, false, false, false);
                if (editorAnim != null && editorAnim.Frames.Count != 0)
                {
                    var frame = editorAnim.Frames[index];
                    ProcessAnimation(frame.Entry.FrameSpeed, frame.Entry.Frames.Count, frame.Frame.Duration);
                    for (int i = -length + 1; i <= 0; ++i)
                    {
                        d.DrawBitmap(frame.Texture, x + frame.Frame.CenterX, y + frame.Frame.CenterY + i * frame.Frame.Height,
                            frame.Frame.Width, frame.Frame.Height, false, Transparency);
                    }
                }
            }
            else if (entity.Object.Name.Name == "InvisibleBlock")
            {
                var width = (int)(entity.attributesMap["width"].ValueUInt8);
                var height = (int)(entity.attributesMap["height"].ValueUInt8);
                var editorAnim = LoadAnimation2("ItemBox", d, 2, 10, false, false, false);
                if (editorAnim != null && editorAnim.Frames.Count != 0)
                {
                    var frame = editorAnim.Frames[index];
                    ProcessAnimation(frame.Entry.FrameSpeed, frame.Entry.Frames.Count, frame.Frame.Duration);
                    bool wEven = width % 2 == 0;
                    bool hEven = height % 2 == 0;
                    for (int xx = 0; xx <= width; ++xx)
                    {
                        for (int yy = 0; yy <= height; ++yy)
                        {
                            d.DrawBitmap(frame.Texture,
                                x + (wEven ? frame.Frame.CenterX : -frame.Frame.Width) + (-width / 2 + xx) * frame.Frame.Width,
                                y + (hEven ? frame.Frame.CenterY : -frame.Frame.Height) + (-height / 2 + yy) * frame.Frame.Height,
                                frame.Frame.Width, frame.Frame.Height, false, Transparency);
                        }
                    }
                }
            }
            else if (entity.Object.Name.Name == "ForceUnstick")
            {
                var width = (int)(entity.attributesMap["width"].ValueUInt8);
                var height = (int)(entity.attributesMap["height"].ValueUInt8);
                bool breakClimb = entity.attributesMap["breakClimb"].ValueBool;
                int type;
                if (breakClimb)
                    type = 9;
                else
                    type = 6;
                var editorAnim = LoadAnimation2("ItemBox", d, 2, type, false, false, false);
                if (editorAnim != null && editorAnim.Frames.Count != 0)
                {
                    var frame = editorAnim.Frames[index];
                    ProcessAnimation(frame.Entry.FrameSpeed, frame.Entry.Frames.Count, frame.Frame.Duration);
                    bool wEven = width % 2 == 0;
                    bool hEven = height % 2 == 0;
                    for (int xx = 0; xx <= width; ++xx)
                    {
                        for (int yy = 0; yy <= height; ++yy)
                        {
                            d.DrawBitmap(frame.Texture,
                                x + (wEven ? frame.Frame.CenterX : -frame.Frame.Width) + (-width / 2 + xx) * frame.Frame.Width,
                                y + (hEven ? frame.Frame.CenterY : -frame.Frame.Height) + (-height / 2 + yy) * frame.Frame.Height,
                                frame.Frame.Width, frame.Frame.Height, false, Transparency);
                        }
                    }
                }
            }
            else if (entity.Object.Name.Name == "BreakableWall")
            {
                var type = entity.attributesMap["type"].ValueUInt8;
                var width = (int)(entity.attributesMap["size"].ValuePosition.X.High) - 1;
                var height = (int)(entity.attributesMap["size"].ValuePosition.Y.High) - 1;

                EditorAnimation editorAnim;

                if (width != -1 && height != -1)
                {
                    // draw corners
                    for (int i = 0; i < 4; i++)
                    {
                        bool right = (i & 1) > 0;
                        bool bottom = (i & 2) > 0;

                        editorAnim = LoadAnimation2("EditorAssets", d, 0, 0, right, bottom, false);
                        if (editorAnim != null && editorAnim.Frames.Count != 0)
                        {
                            var frame = editorAnim.Frames[index];
                            ProcessAnimation(frame.Entry.FrameSpeed, frame.Entry.Frames.Count, frame.Frame.Duration);
                            bool wEven = width % 2 == 0;
                            bool hEven = height % 2 == 0;
                            d.DrawBitmap(frame.Texture,
                                (x + (wEven ? frame.Frame.CenterX : -frame.Frame.Width) + (-width / 2 + (right ? width : 0)) * frame.Frame.Width),
                                (y + (hEven ? frame.Frame.CenterY : -frame.Frame.Height) + (-height / 2 + (bottom ? height : 0)) * frame.Frame.Height),
                                frame.Frame.Width, frame.Frame.Height, false, Transparency);

                        }
                    }

                    // draw top and bottom
                    for (int i = 0; i < 2; i++)
                    {
                        bool bottom = (i & 1) > 0;

                        editorAnim = LoadAnimation2("EditorAssets", d, 0, 1, false, bottom, false);
                        if (editorAnim != null && editorAnim.Frames.Count != 0)
                        {
                            var frame = editorAnim.Frames[index];
                            ProcessAnimation(frame.Entry.FrameSpeed, frame.Entry.Frames.Count, frame.Frame.Duration);
                            bool wEven = width % 2 == 0;
                            bool hEven = height % 2 == 0;
                            for (int j = 1; j < width; j++)
                                d.DrawBitmap(frame.Texture,
                                    (x + (wEven ? frame.Frame.CenterX : -frame.Frame.Width) + (-width / 2 + j) * frame.Frame.Width),
                                    (y + (hEven ? frame.Frame.CenterY : -frame.Frame.Height) + (-height / 2 + (bottom ? height : 0)) * frame.Frame.Height),
                                    frame.Frame.Width, frame.Frame.Height, false, Transparency);
                        }
                    }

                    // draw sides
                    for (int i = 0; i < 2; i++)
                    {
                        bool right = (i & 1) > 0;

                        editorAnim = LoadAnimation2("EditorAssets", d, 0, 2, right, false, false);
                        if (editorAnim != null && editorAnim.Frames.Count != 0)
                        {
                            var frame = editorAnim.Frames[index];
                            ProcessAnimation(frame.Entry.FrameSpeed, frame.Entry.Frames.Count, frame.Frame.Duration);
                            bool wEven = width % 2 == 0;
                            bool hEven = height % 2 == 0;
                            for (int j = 1; j < height; j++)
                                d.DrawBitmap(frame.Texture,
                                    (x + (wEven ? frame.Frame.CenterX : -frame.Frame.Width) + (-width / 2 + (right ? width : 0)) * frame.Frame.Width),
                                    (y + (hEven ? frame.Frame.CenterY : -frame.Frame.Height) + (-height / 2 + j) * frame.Frame.Height),
                                    frame.Frame.Width, frame.Frame.Height, false, Transparency);
                        }
                    }
                }

                bool knux = entity.attributesMap["onlyKnux"].ValueBool;
                bool mighty = entity.attributesMap.ContainsKey("onlyMighty") && entity.attributesMap["onlyMighty"].ValueBool;

                // draw Knuckles icon
                if (knux)
                {
                    editorAnim = LoadAnimation2("HUD", d, 2, 2, false, false, false);
                    if (editorAnim != null && editorAnim.Frames.Count != 0)
                    {
                        var frame = editorAnim.Frames[index];
                        ProcessAnimation(frame.Entry.FrameSpeed, frame.Entry.Frames.Count, frame.Frame.Duration);
                        d.DrawBitmap(frame.Texture, x - frame.Frame.Width / (mighty ? 1 : 2), y - frame.Frame.Height / 2, frame.Frame.Width, frame.Frame.Height, false, Transparency);
                    }
                }

                // draw Mighty icon
                if (mighty)
                {
                    editorAnim = LoadAnimation2("HUD", d, 2, 3, false, false, false);
                    if (editorAnim != null && editorAnim.Frames.Count != 0)
                    {
                        var frame = editorAnim.Frames[index];
                        ProcessAnimation(frame.Entry.FrameSpeed, frame.Entry.Frames.Count, frame.Frame.Duration);
                        d.DrawBitmap(frame.Texture, x - (knux ? 0 : frame.Frame.Width / 2), y - frame.Frame.Height / 2, frame.Frame.Width, frame.Frame.Height, false, Transparency);
                    }
                }
            }
            else if (entity.Object.Name.Name == "CollapsingPlatform")
            {
                var widthPixels = (int)(entity.attributesMap["size"].ValuePosition.X.High);
                var heightPixels = (int)(entity.attributesMap["size"].ValuePosition.Y.High);
                var width = (int)widthPixels / 16;
                var height = (int)heightPixels / 16;

                EditorAnimation editorAnim;

                if (width != 0 && height != 0)
                {
                    // draw corners
                    for (int i = 0; i < 4; i++)
                    {
                        bool right = (i & 1) > 0;
                        bool bottom = (i & 2) > 0;

                        editorAnim = LoadAnimation2("EditorAssets", d, 0, 0, right, bottom, false);
                        if (editorAnim != null && editorAnim.Frames.Count != 0)
                        {
                            var frame = editorAnim.Frames[index];
                            ProcessAnimation(frame.Entry.FrameSpeed, frame.Entry.Frames.Count, frame.Frame.Duration);
                            d.DrawBitmap(frame.Texture,
                                (x + widthPixels / (right ? 2 : -2) ) - (right ? frame.Frame.Width : 0),
                                (y + heightPixels / (bottom ? 2 : -2) - (bottom ? frame.Frame.Height : 0)),
                                frame.Frame.Width, frame.Frame.Height, false, Transparency);

                        }
                    }

                    // draw top and bottom
                    for (int i = 0; i < 2; i++)
                    {
                        bool bottom = (i & 1) > 0;

                        editorAnim = LoadAnimation2("EditorAssets", d, 0, 1, false, bottom, false);
                        if (editorAnim != null && editorAnim.Frames.Count != 0)
                        {
                            var frame = editorAnim.Frames[index];
                            ProcessAnimation(frame.Entry.FrameSpeed, frame.Entry.Frames.Count, frame.Frame.Duration);
                            bool wEven = width % 2 == 0;
                            for (int j = 1; j < width; j++)
                                d.DrawBitmap(frame.Texture,
                                    (x + (wEven ? frame.Frame.CenterX : -frame.Frame.Width) + (-width / 2 + j) * frame.Frame.Width),
                                    (y + heightPixels / (bottom ? 2 : -2) - (bottom ? frame.Frame.Height : 0)),
                                    frame.Frame.Width, frame.Frame.Height, false, Transparency);
                        }
                    }

                    // draw sides
                    for (int i = 0; i < 2; i++)
                    {
                        bool right = (i & 1) > 0;

                        editorAnim = LoadAnimation2("EditorAssets", d, 0, 2, right, false, false);
                        if (editorAnim != null && editorAnim.Frames.Count != 0)
                        {
                            var frame = editorAnim.Frames[index];
                            ProcessAnimation(frame.Entry.FrameSpeed, frame.Entry.Frames.Count, frame.Frame.Duration);
                            bool hEven = height % 2 == 0;
                            for (int j = 1; j < height; j++)
                                d.DrawBitmap(frame.Texture,
                                    (x + widthPixels / (right ? 2 : -2)) - (right ? frame.Frame.Width : 0),
                                    (y + (hEven ? frame.Frame.CenterY : -frame.Frame.Height) + (-height / 2 + j) * frame.Frame.Height),
                                    frame.Frame.Width, frame.Frame.Height, false, Transparency);
                        }
                    }
                }
            }
            else if (entity.Object.Name.Name == "ChemicalPool")
            {
                var type = entity.attributesMap["type"].ValueVar;
                var widthPixels = (int)(entity.attributesMap["size"].ValuePosition.X.High);
                var heightPixels = (int)(entity.attributesMap["size"].ValuePosition.Y.High);
                var width = (int)widthPixels / 16 - 1;
                var height = (int)heightPixels / 16 - 1;

                EditorAnimation editorAnim;

                if (width != -1 && height != -1)
                {
                    // draw inside
                    // TODO this is really heavy on resources, so maybe switch to just drawing a rectangle??
                    for (int i = 0; i <= height; i++)
                    {
                        editorAnim = LoadAnimation2("EditorAssets", d, 1, 1 + (int)type * 2, false, false, false);
                        if (editorAnim != null && editorAnim.Frames.Count != 0)
                        {
                            var frame = editorAnim.Frames[index];
                            ProcessAnimation(frame.Entry.FrameSpeed, frame.Entry.Frames.Count, frame.Frame.Duration);
                            bool wEven = width % 2 == 0;
                            bool hEven = height % 2 == 0;
                            for (int j = 0; j <= width; j++)
                                d.DrawBitmap(frame.Texture,
                                    (((width + 1) * 16) - widthPixels) / 2 + (x + (wEven ? frame.Frame.CenterX : -frame.Frame.Width) + (-width / 2 + j) * frame.Frame.Width),
                                    y + (hEven ? frame.Frame.CenterY : -frame.Frame.Height) + (-height / 2 + i) * frame.Frame.Height,
                                    frame.Frame.Width, frame.Frame.Height, false, Transparency);
                        }
                    }

                    // draw top and botton
                    for (int i = 0; i < 2; i++)
                    {
                        bool bottom = !((i & 1) > 0);

                        editorAnim = LoadAnimation2("EditorAssets", d, 1, (bottom ? 1 : 0) + (int)type * 2, false, false, false);
                        if (editorAnim != null && editorAnim.Frames.Count != 0)
                        {
                            var frame = editorAnim.Frames[index];
                            ProcessAnimation(frame.Entry.FrameSpeed, frame.Entry.Frames.Count, frame.Frame.Duration);
                            bool wEven = width % 2 == 0;
                            bool hEven = height % 2 == 0;
                            for (int j = 0; j <= width; j++)
                                d.DrawBitmap(frame.Texture,
                                    (((width+1) * 16) - widthPixels)/2 + (x + (wEven ? frame.Frame.CenterX : -frame.Frame.Width) + (-width / 2 + j) * frame.Frame.Width),
                                    (y + heightPixels / (bottom ? 2 : -2) - (bottom ? frame.Frame.Height : 0)),
                                    frame.Frame.Width, frame.Frame.Height, false, Transparency);
                        }
                    }
                }
            }
            else if (entity.Object.Name.Name == "Decoration")
            {
                var type = entity.attributesMap["type"].ValueUInt8;
                var editorAnim = LoadAnimation2("Decoration", d, type, -1, false, false, false);
                if (editorAnim != null && editorAnim.Frames.Count != 0)
                {
                    if (index >= editorAnim.Frames.Count)
                        index = 0;
                    var frame = editorAnim.Frames[index];
                    ProcessAnimation(frame.Entry.FrameSpeed, frame.Entry.Frames.Count, frame.Frame.Duration);
                    d.DrawBitmap(frame.Texture, x + frame.Frame.CenterX, y + frame.Frame.CenterY,
                        frame.Frame.Width, frame.Frame.Height, false, Transparency);
                }
            }
            else if (entity.Object.Name.Name == "BreakBar")
            {
                var length = (short)(entity.attributesMap["length"].ValueUInt16);
                var orientation = entity.attributesMap["orientation"].ValueUInt8;
                if (orientation > 1)
                {
                    orientation = 0;
                }
                var editorAnim = LoadAnimation2("BreakBar", d, orientation, -1, false, false, false);
                if (editorAnim != null && editorAnim.Frames.Count != 0)
                {
                    var frameTop = editorAnim.Frames[0];
                    var frameBase = editorAnim.Frames[1];
                    var frameBottom = editorAnim.Frames[2];
                    for (int i = -length/2; i <= length / 2; ++i)
                    {
                        d.DrawBitmap(frameBase.Texture, x + frameBase.Frame.CenterX,
                            y + frameBase.Frame.CenterY + i * frameBase.Frame.Height,
                            frameBase.Frame.Width, frameBase.Frame.Height, false, Transparency);
                    }
                    d.DrawBitmap(frameTop.Texture, x + frameTop.Frame.CenterX,
                        y + frameTop.Frame.CenterY - (length / 2 + 1) * frameBase.Frame.Height,
                        frameTop.Frame.Width, frameTop.Frame.Height, false, Transparency);
                    d.DrawBitmap(frameBottom.Texture, x + frameBottom.Frame.CenterX,
                        y + frameBottom.Frame.CenterY + (length / 2 + 1) * frameBottom.Frame.Height,
                        frameBottom.Frame.Width, frameBottom.Frame.Height, false, Transparency);

                }
            }
            else if (entity.Object.Name.Name == "UFO_Ring")
            {
                var editorAnim = LoadAnimation2("Ring", d, 0, -1, false, false, false);
                if (editorAnim != null && editorAnim.Frames.Count != 0)
                {
                    var frame = editorAnim.Frames[index];
                    ProcessAnimation(frame.Entry.FrameSpeed, frame.Entry.Frames.Count, frame.Frame.Duration);
                    d.DrawBitmap(frame.Texture, x + frame.Frame.CenterX, y + frame.Frame.CenterY,
                        frame.Frame.Width, frame.Frame.Height, false, Transparency);
                }
            }
            else if (entity.Object.Name.Name == "UFO_Springboard")
            {
                var editorAnim = LoadAnimation2("Spring", d, 0, -1, false, false, false);
                if (editorAnim != null && editorAnim.Frames.Count != 0)
                {
                    var frame = editorAnim.Frames[index];
                    ProcessAnimation(50, frame.Entry.Frames.Count, frame.Frame.Duration);
                    d.DrawBitmap(frame.Texture, x + frame.Frame.CenterX, y + frame.Frame.CenterY,
                        frame.Frame.Width, frame.Frame.Height, false, Transparency);
                }
            }
            else if (entity.Object.Name.Name == "UFO_Sphere")
            {
                int id = (int)entity.attributesMap["type"].ValueVar;
                if (id > 4)
                {
                    entity.attributesMap["type"].ValueVar = 4u;
                }
                var editorAnim = LoadAnimation2("Spheres", d, id, -1, false, false, false);
                if (editorAnim != null && editorAnim.Frames.Count != 0)
                {
                    var frame = editorAnim.Frames[index];
                    ProcessAnimation(frame.Entry.FrameSpeed, frame.Entry.Frames.Count, frame.Frame.Duration);
                    d.DrawBitmap(frame.Texture, x + frame.Frame.CenterX, y + frame.Frame.CenterY,
                        frame.Frame.Width, frame.Frame.Height, false, Transparency);
                }
            }
            else if (entity.Object.Name.Name == "UFO_Player")
            {
               var editorAnim = LoadAnimation2("PlayerIcons", d, 0, 7, false, false, false);
                if (editorAnim != null && editorAnim.Frames.Count != 0)
                {
                    var frame = editorAnim.Frames[index];
                    d.DrawBitmap(frame.Texture, x + frame.Frame.CenterX, y + frame.Frame.CenterY,
                        frame.Frame.Width, frame.Frame.Height, false, Transparency);
                }
            }
            else if (entity.Object.Name.Name == "UFO_ItemBox")
            {
                int id = (int)entity.attributesMap["type"].ValueVar;
                if (id > 2)
                {
                    entity.attributesMap["type"].ValueVar = 2u;
                }
                var editorAnim = LoadAnimation2("Items", d, 0, id, false, false, false);
                if (editorAnim != null && editorAnim.Frames.Count != 0)
                {
                    var frame = editorAnim.Frames[index];
                    d.DrawBitmap(frame.Texture, x + frame.Frame.CenterX, y + frame.Frame.CenterY,
                        frame.Frame.Width, frame.Frame.Height, false, Transparency);
                }
            }
            else if (entity.Object.Name.Name == "Platform")
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
                EditorAnimation editorAnim = null;
                while (true)
                {
                    try
                    {
                        editorAnim = LoadAnimation("Platform", d, aminID, -1, false, false, false);

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
                    catch (Exception e)
                    {
                        throw new ApplicationException($"Pop Loading Platforms! {aminID}", e);
                    }
                }
                if (editorAnim.Frames.Count != 0)
                {
                    EditorAnimation.EditorFrame frame = null;
                    if (editorAnim.Frames[0].Entry.FrameSpeed > 0)
                        frame = editorAnim.Frames[index];
                    else
                        frame = editorAnim.Frames[frameID > 0 ? frameID : 0];
                    ProcessAnimation(frame.Entry.FrameSpeed, frame.Entry.Frames.Count, frame.Frame.Duration);
                    d.DrawBitmap(frame.Texture, x + frame.Frame.CenterX, y + frame.Frame.CenterY,
                        frame.Frame.Width, frame.Frame.Height, false, Transparency);
                }
            }
            else if (entity.Object.Name.Name == "PlaneSwitch")
            {
                const int LeftDist = 1,
                          LeftPlane = 2,
                          RightDist = 4,
                          RightPlane = 8;

                var flags = (int)entity.attributesMap["flags"].ValueVar;
                var size = (int)(entity.attributesMap["size"].ValueVar) - 1;
                var angle = entity.attributesMap["angle"].ValueInt32;

                int frameDist = (flags & LeftDist) > 0 ? 1 : 0;
                int framePlane = (flags & LeftPlane) > 0 ? 2 : 0;
                EditorAnimation editorAnim = LoadAnimation2("PlaneSwitch", d, 0, frameDist + framePlane, false, false, false);

                const int pivotOffsetX = -8, pivotOffsetY = 0;
                const int drawOffsetX = 0, drawOffsetY = -8;

                if (editorAnim != null && editorAnim.Frames.Count != 0)
                {
                    var frame = editorAnim.Frames[index];
                    ProcessAnimation(frame.Entry.FrameSpeed, frame.Entry.Frames.Count, frame.Frame.Duration);
                    bool hEven = size % 2 == 0;
                    for (int yy = 0; yy <= size; ++yy)
                    {
                        int[] drawCoords = RotatePoints(
                            x - frame.Frame.Width,
                            (y + (hEven ? frame.Frame.CenterY : -frame.Frame.Height) + (-size / 2 + yy) * frame.Frame.Height),
                            x + pivotOffsetX, y + pivotOffsetY, angle);

                        d.DrawBitmap(frame.Texture, drawCoords[0] + drawOffsetX, drawCoords[1] + drawOffsetY, frame.Frame.Width, frame.Frame.Height, false, Transparency);
                    }
                }

                frameDist = (flags & RightDist) > 0 ? 1 : 0;
                framePlane = (flags & RightPlane) > 0 ? 2 : 0;
                editorAnim = LoadAnimation2("PlaneSwitch", d, 0, frameDist + framePlane, false, false, false);

                if (editorAnim != null && editorAnim.Frames.Count != 0)
                {
                    var frame = editorAnim.Frames[index];
                    ProcessAnimation(frame.Entry.FrameSpeed, frame.Entry.Frames.Count, frame.Frame.Duration);
                    bool hEven = size % 2 == 0;
                    for (int yy = 0; yy <= size; ++yy)
                    {
                        int[] drawCoords = RotatePoints(
                            x,
                            (y + (hEven ? frame.Frame.CenterY : -frame.Frame.Height) + (-size / 2 + yy) * frame.Frame.Height),
                            x + pivotOffsetX, y + pivotOffsetY, angle);

                        d.DrawBitmap(frame.Texture, drawCoords[0] + drawOffsetX, drawCoords[1] + drawOffsetY, frame.Frame.Width, frame.Frame.Height, false, Transparency);
                    }
                }
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

        public bool HasFilter()
        {
            return entity.attributesMap.ContainsKey("filter") && entity.attributesMap["filter"].Type == AttributeTypes.UINT8;
        }

        internal void Flip(FlipDirection flipDirection)
        {
            if (entity.attributesMap.ContainsKey("flipFlag"))
            {
                if (flipDirection == FlipDirection.Horizontal)
                {
                    entity.attributesMap["flipFlag"].ValueVar ^= 0x01;
                }
                else
                {
                    entity.attributesMap["flipFlag"].ValueVar ^= 0x02;
                }
            }
        }

        public static void ReleaseResources()
        {
            DataDirectoryList = null;

            foreach (var pair in Sheets)
                pair.Value?.Dispose();
            Sheets.Clear();

            foreach (var pair in Animations)
                foreach (var pair2 in pair.Value.Frames)
                    pair2.Texture?.Dispose();

            Animations.Clear();
        }

        public class EditorAnimation
        {
            public int loadedFrames = 0;
            public bool Ready = false;
            public bool ImageLoaded = false;
            public List<EditorFrame> Frames = new List<EditorFrame>();
            public class EditorFrame
            {
                public Texture Texture;
                public Animation.Frame Frame;
                public Animation.AnimationEntry Entry;
                public Bitmap _Bitmap;
            }
        }
    }
}
