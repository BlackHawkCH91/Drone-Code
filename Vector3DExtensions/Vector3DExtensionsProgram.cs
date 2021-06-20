using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    public static class Vector3DExtensions
    {

        // -- METHODS FOR DIRECTION VECTORS --
        public static Vector3D ConvertToWorldDirection(this Vector3D LocalDirection, IMyTerminalBlock ReferenceBlock)
        {

            return Vector3D.TransformNormal(LocalDirection, ReferenceBlock.WorldMatrix);

        }

        public static Vector3D ConvertToLocalDirection(this Vector3D WorldDirection, IMyTerminalBlock ReferenceBlock)
        {

            return Vector3D.TransformNormal(WorldDirection, MatrixD.Transpose(ReferenceBlock.WorldMatrix));

        }



        // -- METHODS FOR POSITION VECTORS --
        public static Vector3D ConvertToWorldPosition(this Vector3D LocalPosition, IMyTerminalBlock ReferenceBlock)
        {

            return Vector3D.Transform(LocalPosition, ReferenceBlock.WorldMatrix);

        }

        public static Vector3D ConvertToLocalPosition(this Vector3D WorldPosition, IMyTerminalBlock ReferenceBlock)
        {

            Vector3D referenceWorldPosition = ReferenceBlock.WorldMatrix.Translation; // block.WorldMatrix.Translation is the same as block.GetPosition() btw

            // Convert worldPosition into a world direction
            Vector3D worldDirection = WorldPosition - referenceWorldPosition; // This is a vector starting at the reference block pointing at your desired position

            // Convert worldDirection into a local direction
            return Vector3D.TransformNormal(worldDirection, MatrixD.Transpose(ReferenceBlock.WorldMatrix));

        }

    }
}
