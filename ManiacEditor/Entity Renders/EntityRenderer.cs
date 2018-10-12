using RSDKv5;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManiacEditor.Entity_Renders
{
    public abstract class EntityRenderer
    {

        public abstract string GetObjectName();

        public virtual void Draw(DevicePanel d, SceneEntity entity, EditorEntity e, int x, int y, int Transparency)
        {

        }


    }
}
