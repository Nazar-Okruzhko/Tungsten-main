using System;
using OpenTK.Mathematics;
using Tungsten.World;

namespace Tungsten.Physics
{
    /// <summary>
    /// A hand-rolled, dependency-free physics stand-in: gravity + swept AABB
    /// vs static-entity collision, plus a ray-vs-AABB test for hitscan
    /// weapons. This intentionally is NOT a full rigid-body engine (no
    /// rotational dynamics, no constraint solver) - it exists so the
    /// FirstPersonController and VehicleController have solid ground to stand
    /// on and something to shoot at, out of the box, with zero external
    /// dependencies. Swap this out for Bullet/PhysX/BepuPhysics later without
    /// touching gameplay code, since gameplay only calls the methods below.
    /// </summary>
    public class SimplePhysicsWorld
    {
        public Vector3 Gravity = new(0, -18f, 0);
        public readonly Scene Scene;

        public SimplePhysicsWorld(Scene scene) => Scene = scene;

        /// <summary>
        /// Resolves a moving AABB (the player capsule approximated as a box,
        /// or a vehicle body) against every static entity in the scene.
        /// Axis-separated resolution: cheap, stable, good enough for a
        /// blocky first-pass level.
        /// </summary>
        public Vector3 ResolveMove(Vector3 position, Vector3 halfExtents, Vector3 desiredDelta, out bool grounded)
        {
            grounded = false;
            Vector3 pos = position;

            // Resolve one axis at a time so sliding along walls/floors works.
            pos.X += desiredDelta.X;
            ResolveAxisCollision(ref pos, halfExtents, Axis.X);

            pos.Y += desiredDelta.Y;
            bool hitBelow = ResolveAxisCollision(ref pos, halfExtents, Axis.Y, desiredDelta.Y <= 0f);
            grounded = hitBelow;

            pos.Z += desiredDelta.Z;
            ResolveAxisCollision(ref pos, halfExtents, Axis.Z);

            return pos;
        }

        private enum Axis { X, Y, Z }

        private bool ResolveAxisCollision(ref Vector3 pos, Vector3 half, Axis axis, bool movingDown = false)
        {
            bool collided = false;
            foreach (Entity e in Scene.Entities)
            {
                if (!e.IsStatic) continue;
                Vector3 otherHalf = e.ColliderHalfExtents * e.Transform.Scale;
                Vector3 otherPos = e.Transform.Position;

                bool overlapX = MathF.Abs(pos.X - otherPos.X) < half.X + otherHalf.X;
                bool overlapY = MathF.Abs(pos.Y - otherPos.Y) < half.Y + otherHalf.Y;
                bool overlapZ = MathF.Abs(pos.Z - otherPos.Z) < half.Z + otherHalf.Z;
                if (!(overlapX && overlapY && overlapZ)) continue;

                collided = true;
                switch (axis)
                {
                    case Axis.X:
                        pos.X = pos.X > otherPos.X ? otherPos.X + otherHalf.X + half.X : otherPos.X - otherHalf.X - half.X;
                        break;
                    case Axis.Y:
                        pos.Y = pos.Y > otherPos.Y ? otherPos.Y + otherHalf.Y + half.Y : otherPos.Y - otherHalf.Y - half.Y;
                        break;
                    case Axis.Z:
                        pos.Z = pos.Z > otherPos.Z ? otherPos.Z + otherHalf.Z + half.Z : otherPos.Z - otherHalf.Z - half.Z;
                        break;
                }
            }
            return collided && axis == Axis.Y && movingDown;
        }

        /// <summary>Ray vs every entity's world-space AABB. Used by GunSystem for hitscan weapons.</summary>
        public bool Raycast(Vector3 origin, Vector3 direction, float maxDistance, out Entity? hit, out float distance)
        {
            hit = null;
            distance = maxDistance;
            direction = direction.Normalized();

            foreach (Entity e in Scene.Entities)
            {
                Vector3 half = e.ColliderHalfExtents * e.Transform.Scale;
                Vector3 min = e.Transform.Position - half;
                Vector3 max = e.Transform.Position + half;

                if (RayAabb(origin, direction, min, max, out float t) && t < distance)
                {
                    distance = t;
                    hit = e;
                }
            }
            return hit != null;
        }

        private static bool RayAabb(Vector3 origin, Vector3 dir, Vector3 min, Vector3 max, out float tHit)
        {
            float tmin = 0f, tmax = float.MaxValue;
            tHit = 0f;
            for (int i = 0; i < 3; i++)
            {
                float o = origin[i], d = dir[i], mn = min[i], mx = max[i];
                if (MathF.Abs(d) < 1e-8f)
                {
                    if (o < mn || o > mx) return false;
                }
                else
                {
                    float t1 = (mn - o) / d, t2 = (mx - o) / d;
                    if (t1 > t2) (t1, t2) = (t2, t1);
                    tmin = MathF.Max(tmin, t1);
                    tmax = MathF.Min(tmax, t2);
                    if (tmin > tmax) return false;
                }
            }
            tHit = tmin;
            return true;
        }
    }
}
