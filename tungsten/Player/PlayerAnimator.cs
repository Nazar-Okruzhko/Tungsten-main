using System;
using OpenTK.Mathematics;
using Tungsten.Core;

namespace Tungsten.Player
{
    public enum PlayerAnimState { Idle, Walk, Run, Jump, Fall, Crouch }

    /// <summary>
    /// Procedural (not skeletal-mesh) animator: rather than blending imported
    /// animation clips - which would need a real content pipeline we don't
    /// have here - this drives view-bob/sway/recoil-recovery math directly
    /// off player state. It plugs into the same slot a clip-based Animator
    /// would occupy later (FirstPersonController.Animator), so swapping in
    /// real skeletal animation only touches this one file.
    /// </summary>
    public class PlayerAnimator
    {
        public PlayerAnimState State { get; private set; } = PlayerAnimState.Idle;

        private float _bobTimer;
        public Vector3 ViewBobOffset { get; private set; }
        public float LeanAngle { get; private set; }

        public void SetState(PlayerAnimState state) => State = state;

        public void Update(float speedFraction, bool grounded)
        {
            float bobSpeed = State switch
            {
                PlayerAnimState.Run => 14f,
                PlayerAnimState.Walk => 9f,
                _ => 0f,
            };
            float bobAmount = State switch
            {
                PlayerAnimState.Run => 0.08f,
                PlayerAnimState.Walk => 0.045f,
                _ => 0.015f, // idle breathing sway
            };

            _bobTimer += Time.DeltaTime * bobSpeed * (grounded ? 1f : 0f);
            float bobY = MathF.Abs(MathF.Sin(_bobTimer)) * bobAmount;
            float bobX = MathF.Sin(_bobTimer * 0.5f) * bobAmount * 0.5f;
            ViewBobOffset = new Vector3(bobX, bobY, 0f);

            LeanAngle = MathHelper.Lerp(LeanAngle, 0f, Time.DeltaTime * 6f);
        }

        public void ApplyRecoilKick(float amount) => LeanAngle -= amount;
    }
}
