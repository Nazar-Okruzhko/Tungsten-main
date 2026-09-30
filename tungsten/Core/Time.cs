namespace Tungsten.Core
{
    /// <summary>
    /// Holds the current frame's timing info. Updated once per frame by
    /// EngineHost.Update(). Any system (controller, physics, animator) reads
    /// from here instead of taking its own stopwatch, so everything stays in
    /// lock-step with a single authoritative clock - important once we add
    /// networked play or a fixed physics tick later.
    /// </summary>
    public static class Time
    {
        /// <summary>Seconds since the previous frame ("Delta Time").</summary>
        public static float DeltaTime { get; private set; }

        /// <summary>Total seconds since the engine started.</summary>
        public static float Elapsed { get; private set; }

        /// <summary>Fixed timestep used for physics integration (60 Hz).</summary>
        public const float FixedDeltaTime = 1f / 60f;

        // Physics runs on a fixed step even if rendering framerate varies;
        // this accumulator implements the classic "spiral of death"-safe loop.
        private static float _accumulator;

        public static void Tick(float deltaSeconds)
        {
            DeltaTime = deltaSeconds;
            Elapsed += deltaSeconds;
            _accumulator += deltaSeconds;
        }

        /// <summary>
        /// Call in a while-loop each frame: while (Time.ConsumeFixedStep()) PhysicsStep();
        /// Caps catch-up steps per frame so a debugger breakpoint (or a slow
        /// frame) doesn't cause a huge burst of physics steps all at once -
        /// the classic "spiral of death" guard.
        /// </summary>
        private const int MaxStepsPerFrame = 5;
        private static int _stepsThisFrame;

        public static bool ConsumeFixedStep()
        {
            if (_accumulator < FixedDeltaTime) { _stepsThisFrame = 0; return false; }
            if (_stepsThisFrame >= MaxStepsPerFrame)
            {
                // Drop the remainder rather than spiralling - the sim will
                // just look briefly slow-motion instead of hanging.
                _accumulator = 0f;
                _stepsThisFrame = 0;
                return false;
            }
            _accumulator -= FixedDeltaTime;
            _stepsThisFrame++;
            return true;
        }
    }
}
