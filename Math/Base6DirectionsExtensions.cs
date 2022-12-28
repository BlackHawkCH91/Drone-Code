using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
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
    static class Base6DirectionsExtensions
    {
        public static Base6Directions.Direction ConvertToLocal(this Base6Directions.Direction direction, IMyCubeBlock referenceBlock)
        {
            return referenceBlock.Orientation.TransformDirection(direction);
        }

        public static Base6Directions.Direction ConvertToWorld(this Base6Directions.Direction direction, IMyCubeBlock referenceBlock)
        {
            return referenceBlock.Orientation.TransformDirectionInverse(direction);
        }

        public static Base6Directions.Direction OppositeDirection(this Base6Directions.Direction direction)
        {
            return Base6Directions.GetOppositeDirection(direction);
        }
    }
}
