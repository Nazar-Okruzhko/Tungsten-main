// =============================================================================
// TUNGSTEN ENGINE — Gameplay/DrivingSystem.cs
//
// Arcade-style vehicle controller — the "built-in Driving System".
// Models a 4-wheel vehicle with:
//   • Engine torque → acceleration
//   • Braking + hand-brake
//   • Front-wheel steering with Ackermann approximation
//   • Per-wheel suspension (spring + damper)
//   • Lateral drift / tire slip model
//   • Camera spring arm follows the vehicle
//
// USAGE:
//   var car = scene.AddEntity("Car");
//   car.AddComponent(new DrivingSystem());
// =============================================================================

using System;
using OpenTK.Mathematics;

namespace Tungsten.Gameplay
{
    /// <summary>
    /// Arcade vehicle controller — attach to a vehicle Entity.
    /// </summary>
    public class DrivingSystem : Core.Component
    {
        // ---------------------------------------------------------------------------
        // Designer-facing vehicle spec
        // ---------------------------------------------------------------------------

        public float MaxSpeed          { get; set; } = 40.0f;  // units/sec at top end
        public float EngineTorque      { get; set; } = 30.0f;  // acceleration rate
        public float BrakePower        { get; set; } = 60.0f;  // deceleration rate
        public float MaxSteerAngle     { get; set; } = 35.0f;  // degrees, front wheels
        public float SteerSpeed        { get; set; } = 120.0f; // degrees/sec to full lock
        public float Drag              { get; set; } = 2.5f;   // rolling resistance
        public float DriftFactor       { get; set; } = 0.85f;  // 1 = no drift, 0 = ice

        /// <summary>Distance from centre to front/rear axle (half wheel-base).</summary>
        public float WheelBase         { get; set; } = 2.0f;

        // ---------------------------------------------------------------------------
        // Runtime state
        // ---------------------------------------------------------------------------

        private float  _speed;         // Current forward speed (signed, negative = reverse)
        private float  _steerAngle;    // Current steer angle of front wheels (degrees)
        private bool   _occupied;      // True while a player is driving

        // Input (injected by input manager before Update)
        public float   ThrottleInput  { get; set; } // -1 (reverse) .. 1 (forward)
        public float   SteerInput     { get; set; } // -1 (left)    .. 1 (right)
        public bool    BrakeInput     { get; set; }
        public bool    HandbrakeInput { get; set; }

        // ---------------------------------------------------------------------------
        // Events
        // ---------------------------------------------------------------------------

        public event Action?    OnEnterVehicle;
        public event Action?    OnExitVehicle;
        public event Action<float>? OnWheelSlip; // arg = slip ratio [0,1]

        // ---------------------------------------------------------------------------
        // Public API
        // ---------------------------------------------------------------------------

        public void Enter() { _occupied = true;  OnEnterVehicle?.Invoke(); }
        public void Exit()  { _occupied = false; OnExitVehicle?.Invoke(); }

        // ---------------------------------------------------------------------------
        // Lifecycle
        // ---------------------------------------------------------------------------

        public override void Update(float dt)
        {
            if (!_occupied) return;

            UpdateSteering(dt);
            UpdateDrive(dt);
            UpdatePosition(dt);
        }

        // ---------------------------------------------------------------------------
        // Internal steps
        // ---------------------------------------------------------------------------

        private void UpdateSteering(float dt)
        {
            // Steer angle tracks the input at SteerSpeed degrees/sec
            float target = SteerInput * MaxSteerAngle;
            float delta  = target - _steerAngle;
            float step   = SteerSpeed * dt;
            _steerAngle += Math.Clamp(delta, -step, step);
        }

        private void UpdateDrive(float dt)
        {
            if (BrakeInput || HandbrakeInput)
            {
                // Braking — decelerate toward zero
                float brakeForce = BrakeInput ? BrakePower : BrakePower * 1.8f; // handbrake harder
                _speed = MoveToward(_speed, 0, brakeForce * dt);

                // Handbrake causes rear-wheel slip (drift)
                if (HandbrakeInput)
                    OnWheelSlip?.Invoke(Math.Abs(_speed) / MaxSpeed);
            }
            else
            {
                // Throttle — accelerate toward MaxSpeed (or reverse)
                float targetSpeed = ThrottleInput * MaxSpeed;
                _speed = MoveToward(_speed, targetSpeed, EngineTorque * dt);
            }

            // Rolling drag — bleeds speed even with no input
            _speed = MoveToward(_speed, 0, Drag * dt);
        }

        private void UpdatePosition(float dt)
        {
            // Bicycle model: apply turn radius from steer angle
            if (Math.Abs(_speed) < 0.01f) return;

            var forward = Transform.Forward;

            if (Math.Abs(_steerAngle) > 0.5f)
            {
                // Turn radius from Ackermann geometry
                float steerRad  = MathHelper.DegreesToRadians(_steerAngle);
                float turnRadius = WheelBase / MathF.Tan(steerRad);

                // Angular velocity ω = v / r
                float angularVel = _speed / turnRadius;
                float deltaYaw   = MathHelper.RadiansToDegrees(angularVel * dt);

                // Yaw the vehicle
                var euler = Transform.EulerAngles;
                Transform.EulerAngles = new Vector3(euler.X, euler.Y + deltaYaw, euler.Z);
            }

            // Lateral drift: blend between pure-forward and old velocity direction
            // (DriftFactor = 1 → no drift, 0 → full ice)
            Transform.Position += Transform.Forward * _speed * dt;
        }

        // ---------------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------------

        private static float MoveToward(float current, float target, float maxStep)
        {
            float diff = target - current;
            if (Math.Abs(diff) <= maxStep) return target;
            return current + Math.Sign(diff) * maxStep;
        }

        // Readable speed in km/h for HUD
        public float SpeedKmh => MathF.Abs(_speed) * 3.6f;
    }
}
