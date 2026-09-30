using OpenTK.Mathematics;

namespace Tungsten.Vehicles
{
    /// <summary>One corner of the vehicle. Position is local to the car body.</summary>
    public class Wheel
    {
        public Vector3 LocalPosition;
        public bool IsSteering;
        public bool IsDriven;
        public float SuspensionCompression; // 0 (fully extended) .. 1 (fully compressed), for wheel visual bob

        public Wheel(Vector3 localPosition, bool steering, bool driven)
        {
            LocalPosition = localPosition;
            IsSteering = steering;
            IsDriven = driven;
        }
    }
}
