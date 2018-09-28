using ManiacEditor.Entity_Renders;
using ManiacEditor.Enums;
using RSDKv5;
using SharpDX.Direct3D9;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices.ComTypes;
using System.Windows.Forms;
using System.Diagnostics;

namespace ManiacEditor
{
    public class EditorEntity_ini
    {
        public static List<string> getEntityInternalList(int type)
        {
            if (type == 1) //For the list of objects with renders
            {
                var resourceName = "ManiacEditor.Resources.objectRenderList.ini";
                var assembly = Assembly.GetExecutingAssembly();
                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                using (StreamReader reader = new StreamReader(stream))
                {
                    return EnumerateLines(reader).ToList<string>();
                }
            }
            else //For On Screen Render Exlusions (So we can make our own rules in the object's render file)
            {
                var resourceName = "ManiacEditor.Resources.onScreenRenderExclusions.ini";
                var assembly = Assembly.GetExecutingAssembly();
                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                using (StreamReader reader = new StreamReader(stream))
                {
                    return EnumerateLines(reader).ToList<string>();
                }
            }

        }
        public static List<string> getEntityExternalList(int type)
        {
            if (type == 1) //For the list of objects with renders
            {
                var path = Path.Combine(Environment.CurrentDirectory, "Resources\\objectRenderList.ini");
                //Debug.Print(path.ToString());
                using (StreamReader reader = new StreamReader(path))
                {
                    return EnumerateLines(reader).ToList<string>();
                }
            }
            else //For On Screen Render Exlusions (So we can make our own rules in the object's render file)
            {
                var path = Path.Combine(Environment.CurrentDirectory, "Resources\\onScreenRenderExclusions.ini");
                //Debug.Print(path.ToString());
                using (StreamReader reader = new StreamReader(path))
                {
                    return EnumerateLines(reader).ToList<string>();
                }
            }

        }

        public static List<string> getSpecialRenderList(int type)
        {
            if (type == 1) //For the list of objects with renders
            {
                List<string> entityRenderListInternal = getEntityInternalList(1); // Get the list embeded in the editor
                List<string> entityRenderListExternal = getEntityExternalList(1); // Get the list the user is allowed to edit
                if (entityRenderListExternal != entityRenderListInternal)
                {
                    return entityRenderListExternal;
                }
                else
                {
                    return entityRenderListInternal;
                }
            }
            else //For On Screen Render Exlusions (So we can make our own rules in the object's render file)
            {
                List<string> entityRenderListInternal = getEntityInternalList(0); // Get the list embeded in the editor
                List<string> entityRenderListExternal = getEntityExternalList(0); // Get the list the user is allowed to edit
                if (entityRenderListExternal != entityRenderListInternal)
                {
                    return entityRenderListExternal;
                }
                else
                {
                    return entityRenderListInternal;
                }
            }



        }

        public static IEnumerable<string> EnumerateLines(TextReader reader)
        {
            string line;

            while ((line = reader.ReadLine()) != null)
            {
                yield return line;
            }
        }
    }
}
