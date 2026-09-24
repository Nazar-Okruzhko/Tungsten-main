// =============================================================================
// TUNGSTEN ENGINE — Gameplay/FirstPersonController.cs
//
// The "built-in First Person Character Controller" Roblox Studio ships with.
// Responsibilities:
//   • WASD + Sprint movement relative to camera look direction
//   • Mouse-look (yaw on Entity, pitch only on Camera)
//   • Jump with gravity and ground detection
//   • Crouch (halves speed + capsule height)
//   • Head-bob animation cycle  (subtle vertical sine wave on camera position)
//   • Footstep audio cue timer
//
// USAGE — attach to an Entity that also has a CapsuleCollider:
//   var player = scene.AddEntity("Player");
//   player.AddComponent(new FirstPersonController());
// =============================================================================

using System;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework; // Keys enum

namespace Tungsten.Gameplay
{
    /// <summary>
    /// Roblox-style built-in first-person character controller.
    /// Mount this on the player Entity; point <see cref="CameraNode"/> at the
    /// head sub-entity to get the view camera.
    /// </summary>
    public class FirstPersonController : Core.Component
    {
        // ---------------------------------------------------------------------------
        // Tunable constants — tweak in the Properties panel
        // ---------------------------------------------------------------------------

        /// <summary>Normal walk speed in units/second.</summary>
        public float WalkSpeed    { get; set; } = 7.0f;

        /// <summary>Sprint multiplier applied while Shift is held.</summary>
        public float SprintMult   { get; set; } = 1.75f;

        /// <summary>Crouch multiplier applied while Ctrl is held.</summary>
        public float CrouchMult   { get; set; } = 0.45f;

        /// <summary>Vertical impulse on jump (units/second).</summary>
        public float JumpForce    { get; set; } = 10.0f;

        /// <summary>Downward acceleration in units/second².</summary>
        public float Gravity      { get; set; } = 24.0f;

        /// <summary>Mouse sensitivity (degrees per pixel).</summary>
        public float Sensitivity  { get; set; } = 0.15f;

        /// <summary>Pitch clamped to ±this many degrees.</summary>
        public float PitchLimit   { get; set; } = 89.0f;

        /// <summary>
        /// Head-bob amplitude in units.
        /// Set to 0 to disable the bob entirely.
        /// </summary>
        public float HeadBobAmp   { get; set; } = 0.06f;

        /// <summary>Head-bob cycles per second at walk speed.</summary>
        public float HeadBobFreq  { get; set; } = 2.0f;

        // ---------------------------------------------------------------------------
        // Runtime state — not serialised, recreated each play session
        // ---------------------------------------------------------------------------

        private float _yaw;           // Horizontal look angle (degrees) on player Entity
        private float _pitch;         // Vertical look angle (degrees) on camera only

        private float _velocityY;     // Vertical velocity component (gravity / jump)
        private bool  _isGrounded;    // True when character is standing on the floor
        private bool  _isCrouching;

        private float _bobTimer;      // Accumulates time for the sine wave head-bob
        private float _footstepTimer; // Seconds until next footstep sound cue

        // The camera entity is the "head" — a child of the player entity
        public Core.Entity? CameraNode { get; set; }

        // ---------------------------------------------------------------------------
        // Input snapshot (injected each frame by the Engine's input manager)
        // ---------------------------------------------------------------------------

        // These are set by the Engine before Update() so the controller doesn't need
        // direct access to GLFW, making it testable without an OpenGL window.

        public Vector2 MoveInput   { get; set; } // (forward, right) in [-1,1]
        public Vector2 MouseDelta  { get; set; } // raw pixel delta this frame
        public bool    JumpPressed { get; set; }
        public bool    SprintHeld  { get; set; }
        public bool    CrouchHeld  { get; set; }

        // ---------------------------------------------------------------------------
        // Lifecycle
        // ---------------------------------------------------------------------------

        public override void Start()
        {
            // Initialise yaw from whatever rotation the entity starts at
            _yaw = Transform.EulerAngles.Y;
        }

        public override void Update(float dt)
        {
            ApplyMouseLook();
            ApplyMovement(dt);
            ApplyGravityAndJump(dt);
            ApplyHeadBob(dt);
            HandleFootsteps(dt);
        }

        // ---------------------------------------------------------------------------
        // Step 1 – Mouse look
        // ---------------------------------------------------------------------------

        private void ApplyMouseLook()
        {
            // Horizontal rotation: yaw the whole player Entity around Y axis
            _yaw -= MouseDelta.X * Sensitivity;

            // Vertical rotation: pitch only the camera child node, clamped
            _pitch -= MouseDelta.Y * Sensitivity;
            _pitch = Math.Clamp(_pitch, -PitchLimit, PitchLimit);

            // Apply yaw to player entity
            Transform.EulerAngles = new Vector3(0, _yaw, 0);

            // Apply pitch to camera node (if assigned)
            if (CameraNode != null)
                CameraNode.Transform.EulerAngles = new Vector3(_pitch, 0, 0);
        }

        // ---------------------------------------------------------------------------
        // Step 2 – Horizontal movement (WASD)
        // ---------------------------------------------------------------------------

        private void ApplyMovement(float dt)
        {
            // Derive directions from the player's yaw (ignore pitch for movement)
            var yawRad  = MathHelper.DegreesToRadians(_yaw);
            var forward = new Vector3(-MathF.Sin(yawRad), 0,  -MathF.Cos(yawRad));
            var right   = new Vector3( MathF.Cos(yawRad), 0,  -MathF.Sin(yawRad));

            // Build movement vector from input axes
            var moveDir = forward * MoveInput.X + right * MoveInput.Y;
            if (moveDir.LengthSquared > 1.0f)
                moveDir.Normalize(); // Prevent diagonal speed boost

            // Apply speed modifiers
            float speed = WalkSpeed;
            if (SprintHeld && !_isCrouching) speed *= SprintMult;
            if (CrouchHeld)                  speed *= CrouchMult;

            _isCrouching = CrouchHeld;

            // Translate position (simple kinematic — no physics engine needed for XZ)
            Transform.Position += moveDir * speed * dt;
        }

        // ---------------------------------------------------------------------------
        // Step 3 – Gravity + Jump (simple kinematic vertical axis)
        // ---------------------------------------------------------------------------

        private void ApplyGravityAndJump(float dt)
        {
            // ---- Ground check (flat-floor assumption; override with ray-cast later)
            const float groundLevel = 0.9f; // half-height of capsule
            _isGrounded = Transform.Position.Y <= groundLevel + 0.01f;

            if (_isGrounded)
            {
                Transform.Position = new Vector3(
                    Transform.Position.X,
                    groundLevel,
                    Transform.Position.Z);

                _velocityY = 0;

                if (JumpPressed)
                    _velocityY = JumpForce; // Launch upward
            }
            else
            {
                // Accumulate gravity while airborne
                _velocityY -= Gravity * dt;
            }

            // Apply vertical velocity
            Transform.Position += new Vector3(0, _velocityY * dt, 0);
        }

        // ---------------------------------------------------------------------------
        // Step 4 – Head-bob (sine wave on camera Y when moving)
        // ---------------------------------------------------------------------------

        private void ApplyHeadBob(float dt)
        {
            if (CameraNode == null || HeadBobAmp <= 0) return;

            bool isMoving = MoveInput.LengthSquared > 0.01f && _isGrounded;

            if (isMoving)
            {
                // Advance bob timer at a rate proportional to speed
                float freq = HeadBobFreq * (SprintHeld ? SprintMult : 1.0f);
                _bobTimer += dt * freq * MathF.Tau;

                float bob = MathF.Sin(_bobTimer) * HeadBobAmp;

                // Offset only the camera node, not the whole entity
                var camPos = CameraNode.Transform.Position;
                CameraNode.Transform.Position = new Vector3(camPos.X, bob, camPos.Z);
            }
            else
            {
                // Lerp the bob back to zero when idle
                _bobTimer = 0;
                var camPos = CameraNode.Transform.Position;
                CameraNode.Transform.Position = new Vector3(
                    camPos.X,
                    MathHelper.Lerp(camPos.Y, 0, 0.2f),
                    camPos.Z);
            }
        }

        // ---------------------------------------------------------------------------
        // Step 5 – Footstep audio cue (fire event every N seconds while walking)
        // ---------------------------------------------------------------------------

        public event Action? OnFootstep; // Studio or audio system subscribes here

        private void HandleFootsteps(float dt)
        {
            bool isMoving = MoveInput.LengthSquared > 0.01f && _isGrounded;
            if (!isMoving) return;

            float interval = SprintHeld ? 0.30f : 0.50f;
            _footstepTimer -= dt;
            if (_footstepTimer <= 0)
            {
                _footstepTimer = interval;
                OnFootstep?.Invoke(); // Play footstep sound in subscriber
            }
        }
    }
}
