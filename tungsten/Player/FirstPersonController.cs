using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Tungsten.Core;
using Tungsten.Physics;
using Tungsten.Rendering;

namespace Tungsten.Player
{
    /// <summary>
    /// The built-in First Person Character Controller the brief asked for -
    /// this is the one piece of gameplay every level gets "for free", the
    /// same way a fresh Roblox baseplate already has a walking/jumping
    /// character without any scripting.
    ///
    /// Owns: movement + collision (via SimplePhysicsWorld), mouse-look camera,
    /// jumping/gravity, crouch, sprint, and drives PlayerAnimator + GunSystem.
    /// </summary>
    public class FirstPersonController
    {
        public Camera Camera { get; }
        public PlayerAnimator Animator { get; } = new();
        public GunSystem? EquippedGun;

        public Vector3 Position;
        public Vector3 Velocity;
        public bool Grounded { get; private set; }
        public bool Crouching { get; private set; }

        public float WalkSpeed = 4.5f;
        public float SprintSpeed = 8.0f;
        public float CrouchSpeed = 2.2f;
        public float JumpVelocity = 6.5f;
        public float MouseSensitivity = 0.12f;
        public float EyeHeight = 1.7f;
        public float CrouchHeight = 1.1f;

        private readonly Vector3 _halfExtents = new(0.35f, 0.9f, 0.35f);
        private readonly SimplePhysicsWorld _physics;

        public FirstPersonController(SimplePhysicsWorld physics, Vector3 spawnPosition)
        {
            _physics = physics;
            Position = spawnPosition;
            Camera = new Camera(spawnPosition + new Vector3(0, EyeHeight, 0));
        }

        public void Update(Input input)
        {
            // --- Look ---
            if (input.MouseCaptured)
                Camera.AddLook(input.MouseDelta.X * MouseSensitivity, -input.MouseDelta.Y * MouseSensitivity);

            // --- Move intent (camera-relative, flattened to the ground plane) ---
            Vector3 flatForward = new Vector3(Camera.Front.X, 0, Camera.Front.Z).Normalized();
            Vector3 flatRight = new Vector3(Camera.Right.X, 0, Camera.Right.Z).Normalized();

            Vector3 wish = Vector3.Zero;
            if (input.IsDown(Keys.W)) wish += flatForward;
            if (input.IsDown(Keys.S)) wish -= flatForward;
            if (input.IsDown(Keys.D)) wish += flatRight;
            if (input.IsDown(Keys.A)) wish -= flatRight;
            if (wish.LengthSquared > 0f) wish.Normalize();

            Crouching = input.IsDown(Keys.LeftControl);
            bool sprinting = input.IsDown(Keys.LeftShift) && !Crouching;
            float speed = Crouching ? CrouchSpeed : sprinting ? SprintSpeed : WalkSpeed;

            Velocity.X = wish.X * speed;
            Velocity.Z = wish.Z * speed;

            // --- Jump / gravity ---
            if (Grounded && input.WasPressed(Keys.Space))
                Velocity.Y = JumpVelocity;
            Velocity.Y += _physics.Gravity.Y * Time.DeltaTime;

            Vector3 delta = Velocity * Time.DeltaTime;
            Position = _physics.ResolveMove(Position, _halfExtents, delta, out bool grounded);
            Grounded = grounded;
            if (Grounded && Velocity.Y < 0f) Velocity.Y = 0f;

            float eye = Crouching ? CrouchHeight : EyeHeight;
            Camera.Position = Position + new Vector3(0, eye, 0) + Animator.ViewBobOffset;

            // --- Animation state ---
            float speedFraction = new Vector2(Velocity.X, Velocity.Z).Length / SprintSpeed;
            Animator.SetState(!Grounded
                ? (Velocity.Y > 0 ? PlayerAnimState.Jump : PlayerAnimState.Fall)
                : Crouching
                    ? PlayerAnimState.Crouch
                    : speedFraction > 0.6f ? PlayerAnimState.Run
                    : speedFraction > 0.05f ? PlayerAnimState.Walk
                    : PlayerAnimState.Idle);
            Animator.Update(speedFraction, Grounded);

            // --- Gun ---
            if (EquippedGun != null)
            {
                bool wantsFire = input.IsMouseDown(MouseButton.Left);
                bool wantsReload = input.WasPressed(Keys.R);
                EquippedGun.Update(this, wantsFire, wantsReload);
            }
        }
    }
}
