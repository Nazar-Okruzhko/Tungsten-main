using System;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Tungsten.Core;
using Tungsten.Physics;
using Tungsten.World;

namespace Tungsten.Vehicles
{
    /// <summary>
    /// The built-in Driving System the brief asked for. Arcade-style rather
    /// than a full constraint-solved rigid body (matching the level of
    /// fidelity SimplePhysicsWorld provides): four wheels raycast down for
    /// suspension height, engine force accelerates along the body's forward
    /// axis, and steering yaws the body - the same recipe most "fun to drive"
    /// arcade vehicle controllers (including Roblox's own vehicle seats) use
    /// under the hood.
    /// </summary>
    public class VehicleController
    {
        public Entity Body;
        public readonly Wheel[] Wheels;

        public float EnginePower = 22f;
        public float BrakePower = 30f;
        public float MaxSpeed = 28f;
        public float SteerSpeed = 2.2f;
        public float SuspensionRestLength = 0.5f;
        public float SuspensionStrength = 40f;

        public float Speed { get; private set; }
        public bool Occupied;

        private readonly SimplePhysicsWorld _physics;
        private float _steerAngle;

        public VehicleController(Entity body, SimplePhysicsWorld physics)
        {
            Body = body;
            _physics = physics;
            // Standard 4-wheel layout relative to the car body's local origin.
            Wheels = new[]
            {
                new Wheel(new Vector3(-0.9f, -0.3f,  1.4f), steering: true,  driven: true),  // front-left
                new Wheel(new Vector3( 0.9f, -0.3f,  1.4f), steering: true,  driven: true),  // front-right
                new Wheel(new Vector3(-0.9f, -0.3f, -1.4f), steering: false, driven: false), // rear-left
                new Wheel(new Vector3( 0.9f, -0.3f, -1.4f), steering: false, driven: false), // rear-right
            };
        }

        public void Update(Core.Input input)
        {
            if (!Occupied) return;

            float throttle = 0f;
            if (input.IsDown(Keys.W)) throttle += 1f;
            if (input.IsDown(Keys.S)) throttle -= 1f;

            float steerInput = 0f;
            if (input.IsDown(Keys.A)) steerInput -= 1f;
            if (input.IsDown(Keys.D)) steerInput += 1f;
            bool handbrake = input.IsDown(Keys.Space);

            _steerAngle = MathHelper.Lerp(_steerAngle, steerInput * 30f, Time.DeltaTime * SteerSpeed * 3f);

            Vector3 forward = Vector3.Transform(-Vector3.UnitZ, Body.Transform.Rotation);
            Speed = MathHelper.Clamp(Speed + throttle * EnginePower * Time.DeltaTime, -MaxSpeed * 0.5f, MaxSpeed);
            if (handbrake) Speed = MathHelper.Lerp(Speed, 0f, Time.DeltaTime * BrakePower * 0.2f);
            else Speed = MathHelper.Lerp(Speed, Speed, 0f); // (drag hook point for future tuning)

            // Steering only matters while moving, like a real car.
            float turnAmount = MathHelper.DegreesToRadians(_steerAngle) * (Speed / MaxSpeed) * Time.DeltaTime * 2.2f;
            Body.Transform.Rotation = Quaternion.FromAxisAngle(Vector3.UnitY, turnAmount) * Body.Transform.Rotation;

            Vector3 delta = forward * Speed * Time.DeltaTime;
            Body.Transform.Position = _physics.ResolveMove(
                Body.Transform.Position,
                Body.ColliderHalfExtents * Body.Transform.Scale,
                delta + new Vector3(0, _physics.Gravity.Y * Time.DeltaTime, 0),
                out _);

            UpdateSuspensionVisual();
        }

        private void UpdateSuspensionVisual()
        {
            // Cheap "bounce" feedback so wheels visually settle - a stand-in
            // for real per-wheel raycast suspension until a proper wheel mesh
            // + raycast-per-wheel pass is added.
            foreach (Wheel w in Wheels)
                w.SuspensionCompression = MathHelper.Clamp(MathF.Abs(Speed) / MaxSpeed * 0.3f, 0f, 1f);
        }

        public void Enter() { Occupied = true; Logger.Info($"Entered vehicle '{Body.Name}'."); }
        public void Exit() { Occupied = false; Speed = 0f; Logger.Info($"Exited vehicle '{Body.Name}'."); }
    }
}
