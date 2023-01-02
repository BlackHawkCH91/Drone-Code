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

        public static Vector3D ConvertToWorldDirection(this Vector3D LocalDirection, MatrixD WorldMatrix)
        {
            return Vector3D.TransformNormal(LocalDirection, WorldMatrix);
        }

        public static Vector3D ConvertToLocalDirection(this Vector3D WorldDirection, MatrixD WorldMatrix)
        {
            return Vector3D.TransformNormal(WorldDirection, MatrixD.Transpose(WorldMatrix));
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

        public static Vector3D ConvertToWorldPosition(this Vector3D LocalPosition, MatrixD WorldMatrix)
        {
            return Vector3D.Transform(LocalPosition, WorldMatrix);
        }

        public static Vector3D ConvertToLocalPosition(this Vector3D WorldPosition, MatrixD WorldMatrix)
        {
            Vector3D referenceWorldPosition = WorldMatrix.Translation; // block.WorldMatrix.Translation is the same as block.GetPosition() btw
            // Convert worldPosition into a world direction
            Vector3D worldDirection = WorldPosition - referenceWorldPosition; // This is a vector starting at the reference block pointing at your desired position
            // Convert worldDirection into a local direction
            return Vector3D.TransformNormal(worldDirection, MatrixD.Transpose(WorldMatrix));
        }


        // -- GENERAL USE METHODS --
        public static Vector3D UnitVector(this Vector3D vector)
        {
            if (vector.LengthSquared() != 0)
            {
                return vector / vector.Length();
            }
            else
            {
                return Vector3D.Zero;
            }
        }

        public static Vector3D ProjectOnPlane(this Vector3D vector, Vector3D normal)
        {
            return Vector3D.ProjectOnPlane(ref vector, ref normal);
        }

        public static Vector3D RoundToDp(this Vector3D vector, int dp)
        {
            return new Vector3D(vector.X.RoundToDp(dp), vector.Y.RoundToDp(dp), vector.Z.RoundToDp(dp));
        }

        public static Vector3D Sign(this Vector3D vector)
        {
            return new Vector3D(Math.Sign(vector.X), Math.Sign(vector.Y), Math.Sign(vector.Z));
        }

        public static Vector3D Pow(this Vector3D vector, int power)
        {
            Vector3D powVct = vector;
            for(int i = 0; i < power; i++)
            {
                powVct *= vector;
            }
            return powVct;
        }

        public static Vector3D ComponentAbs(this Vector3D vector)
        {
            return vector * vector.Sign();
        }

        public static Vector3D ClampComponents(this Vector3D vector, double min, double max)
        {
            return new Vector3D(
                MathHelper.Clamp(vector.X, min, max),
                MathHelper.Clamp(vector.Y, min, max),
                MathHelper.Clamp(vector.Z, min, max)
                );
        }
    }
    public static class Vector3Extensions
    {
        // -- METHODS FOR DIRECTION VECTORS --
        public static Vector3 ConvertToWorldDirection(this Vector3 LocalDirection, IMyTerminalBlock ReferenceBlock)
        {
            return Vector3.TransformNormal(LocalDirection, ReferenceBlock.WorldMatrix);
        }

        public static Vector3 ConvertToLocalDirection(this Vector3 WorldDirection, IMyTerminalBlock ReferenceBlock)
        {
            return Vector3.TransformNormal(WorldDirection, MatrixD.Transpose(ReferenceBlock.WorldMatrix));
        }

        public static Vector3 ConvertToWorldDirection(this Vector3 LocalDirection, MatrixD WorldMatrix)
        {
            return Vector3.TransformNormal(LocalDirection, WorldMatrix);
        }

        public static Vector3 ConvertToLocalDirection(this Vector3 WorldDirection, MatrixD WorldMatrix)
        {
            return Vector3.TransformNormal(WorldDirection, MatrixD.Transpose(WorldMatrix));
        }



        // -- METHODS FOR POSITION VECTORS --
        public static Vector3 ConvertToWorldPosition(this Vector3 LocalPosition, IMyTerminalBlock ReferenceBlock)
        {
            return Vector3.Transform(LocalPosition, ReferenceBlock.WorldMatrix);
        }

        public static Vector3 ConvertToLocalPosition(this Vector3 WorldPosition, IMyTerminalBlock ReferenceBlock)
        {
            Vector3 referenceWorldPosition = ReferenceBlock.WorldMatrix.Translation; // block.WorldMatrix.Translation is the same as block.GetPosition() btw
            // Convert worldPosition into a world direction
            Vector3 worldDirection = WorldPosition - referenceWorldPosition; // This is a vector starting at the reference block pointing at your desired position
            // Convert worldDirection into a local direction
            return Vector3.TransformNormal(worldDirection, MatrixD.Transpose(ReferenceBlock.WorldMatrix));
        }

        public static Vector3 ConvertToWorldPosition(this Vector3 LocalPosition, MatrixD WorldMatrix)
        {
            return Vector3.Transform(LocalPosition, WorldMatrix);
        }

        public static Vector3 ConvertToLocalPosition(this Vector3 WorldPosition, MatrixD WorldMatrix)
        {
            Vector3 referenceWorldPosition = WorldMatrix.Translation; // block.WorldMatrix.Translation is the same as block.GetPosition() btw
            // Convert worldPosition into a world direction
            Vector3 worldDirection = WorldPosition - referenceWorldPosition; // This is a vector starting at the reference block pointing at your desired position
            // Convert worldDirection into a local direction
            return Vector3.TransformNormal(worldDirection, MatrixD.Transpose(WorldMatrix));
        }


        // -- GENERAL USE METHODS --
        public static Vector3 UnitVector(this Vector3 vector)
        {
            if(vector.LengthSquared() != 0)
            {
                return vector / vector.Length();
            }
            else
            {
                return Vector3.Zero;
            }
        }

        public static Vector3 ProjectOnPlane(this Vector3 vector, Vector3 normal)
        {
            return Vector3.ProjectOnPlane(ref vector, ref normal);
        }

        public static Vector3 RoundToDp(this Vector3 vector, int dp)
        {
            return new Vector3(vector.X.RoundToDp(dp), vector.Y.RoundToDp(dp), vector.Z.RoundToDp(dp));
        }

        public static Vector3 Sign(this Vector3 vector)
        {
            return new Vector3(Math.Sign(vector.X), Math.Sign(vector.Y), Math.Sign(vector.Z));
        }

        public static Vector3 Pow(this Vector3 vector, int power)
        {
            Vector3 powVct = vector;
            for (int i = 0; i < power; i++)
            {
                powVct *= vector;
            }
            return powVct;
        }

        public static Vector3 ComponentAbs(this Vector3 vector)
        {
            return vector * vector.Sign();
        }

        public static Vector3 ClampComponents(this Vector3 vector, double min, double max)
        {
            return new Vector3(
                MathHelper.Clamp(vector.X, min, max),
                MathHelper.Clamp(vector.Y, min, max),
                MathHelper.Clamp(vector.Z, min, max)
                );
        }
    }
}
