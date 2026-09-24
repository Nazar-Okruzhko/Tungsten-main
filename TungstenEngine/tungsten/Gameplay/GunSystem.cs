// =============================================================================
// TUNGSTEN ENGINE — Gameplay/GunSystem.cs
//
// Universal gun component covering:
//   • Hitscan ray-cast shooting (instant bullet-trace, like most FPS rifles)
//   • Projectile mode (spawns a bullet Entity that travels in an arc)
//   • Magazine / ammo management with reload cycle
//   • Recoil impulse fed back to FirstPersonController's pitch/yaw
//   • Muzzle-flash event (let visual system subscribe)
//   • Spread / accuracy model (expands on spray, recovers over time)
//
// USAGE:
//   var gun = player.AddComponent(new GunSystem { Mode = GunMode.Hitscan });
//   gun.OnFire += () => PlayMuzzleEffect();
//   gun.OnHit  += (hit) => ApplyDamage(hit.Entity, gun.Damage);
// =============================================================================

using System;
using OpenTK.Mathematics;

namespace Tungsten.Gameplay
{
    // ── Enum: gun behaviour mode ─────────────────────────────────────────────

    public enum GunMode
    {
        Hitscan,   // Instant ray, no travel time — rifles, pistols
        Projectile // Spawns a moving bullet entity — RPGs, grenades, bows
    }

    // ── Hit-result passed to OnHit subscribers ───────────────────────────────

    public struct RayHit
    {
        public Core.Entity? Entity;  // Entity that was hit (null = terrain)
        public Vector3      Point;   // World-space contact point
        public Vector3      Normal;  // Surface normal at contact
        public float        Distance;
    }

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Attach to the player (or an NPC) to give it a weapon.
    /// </summary>
    public class GunSystem : Core.Component
    {
        // ---------------------------------------------------------------------------
        // Designer-facing properties (editable in Properties panel)
        // ---------------------------------------------------------------------------

        public GunMode  Mode              { get; set; } = GunMode.Hitscan;

        /// <summary>Damage dealt per bullet hit.</summary>
        public float    Damage            { get; set; } = 25.0f;

        /// <summary>Magazine capacity before a reload is required.</summary>
        public int      MagazineSize      { get; set; } = 30;

        /// <summary>Total reserve ammo (fill to desired pool).</summary>
        public int      ReserveAmmo       { get; set; } = 90;

        /// <summary>Seconds between consecutive shots (1/FireRate).</summary>
        public float    FireRate          { get; set; } = 0.1f; // 10 rounds/sec

        /// <summary>Reload duration in seconds.</summary>
        public float    ReloadTime        { get; set; } = 2.0f;

        /// <summary>Maximum bullet range for hitscan mode.</summary>
        public float    MaxRange          { get; set; } = 500.0f;

        /// <summary>Initial bullet speed for projectile mode (units/sec).</summary>
        public float    ProjectileSpeed   { get; set; } = 80.0f;

        /// <summary>Base spread radius in degrees at maximum accuracy.</summary>
        public float    BaseSpread        { get; set; } = 0.5f;

        /// <summary>How much each shot increases spread (accuracy penalty).</summary>
        public float    SpreadPerShot     { get; set; } = 0.3f;

        /// <summary>How fast spread recovers per second (degrees/sec).</summary>
        public float    SpreadRecovery    { get; set; } = 3.0f;

        /// <summary>Upward recoil pitch kick per shot (degrees).</summary>
        public float    RecoilPitch       { get; set; } = 1.2f;

        // ---------------------------------------------------------------------------
        // Runtime state
        // ---------------------------------------------------------------------------

        private int   _currentAmmo;
        private bool  _isReloading;
        private float _fireCooldown;   // Seconds until next shot is allowed
        private float _reloadTimer;    // Counts down during reload
        private float _currentSpread;  // Current accuracy bloom in degrees

        // ---------------------------------------------------------------------------
        // Events — subscribe for VFX, SFX, UI updates
        // ---------------------------------------------------------------------------

        public event Action?          OnFire;     // Triggered every shot
        public event Action?          OnReload;   // Triggered at reload start
        public event Action?          OnEmpty;    // Triggered on dry-fire
        public event Action<RayHit>?  OnHit;      // Triggered on successful hit

        // ---------------------------------------------------------------------------
        // Lifecycle
        // ---------------------------------------------------------------------------

        public override void Start()
        {
            _currentAmmo   = MagazineSize;
            _currentSpread = BaseSpread;
        }

        public override void Update(float dt)
        {
            // Cool down fire-rate timer
            _fireCooldown = MathF.Max(0, _fireCooldown - dt);

            // Progress reload
            if (_isReloading)
            {
                _reloadTimer -= dt;
                if (_reloadTimer <= 0) FinishReload();
            }

            // Recover accuracy spread over time
            _currentSpread = MathF.Max(BaseSpread, _currentSpread - SpreadRecovery * dt);
        }

        // ---------------------------------------------------------------------------
        // Public API
        // ---------------------------------------------------------------------------

        /// <summary>
        /// Pull the trigger.  Call this from the input handler each frame
        /// <paramref name="triggerHeld"/> = true when the fire button is held.
        /// <paramref name="cameraForward"/> = look direction for the hitscan ray.
        /// </summary>
        public void Shoot(bool triggerHeld, Vector3 cameraForward, Core.Scene scene)
        {
            if (!triggerHeld || _fireCooldown > 0 || _isReloading) return;

            if (_currentAmmo <= 0) { OnEmpty?.Invoke(); return; }

            // ── Apply spread to camera forward ────────────────────────────────
            var direction = ApplySpread(cameraForward);

            // ── Fire ──────────────────────────────────────────────────────────
            if (Mode == GunMode.Hitscan)
                DoHitscan(direction, scene);
            else
                SpawnProjectile(direction, scene);

            // ── Post-shot state ───────────────────────────────────────────────
            _currentAmmo--;
            _fireCooldown  = FireRate;
            _currentSpread += SpreadPerShot; // Accuracy degrades with each shot
            OnFire?.Invoke();

            // Auto-reload when magazine is empty
            if (_currentAmmo <= 0 && ReserveAmmo > 0)
                StartReload();
        }

        /// <summary>Start the reload cycle (also called by R key in input handler).</summary>
        public void StartReload()
        {
            if (_isReloading || _currentAmmo == MagazineSize || ReserveAmmo <= 0) return;
            _isReloading = true;
            _reloadTimer = ReloadTime;
            OnReload?.Invoke();
        }

        // ---------------------------------------------------------------------------
        // Internal helpers
        // ---------------------------------------------------------------------------

        private Vector3 ApplySpread(Vector3 forward)
        {
            // Rotate the direction vector by a random angle within the spread cone
            float spreadRad  = MathHelper.DegreesToRadians(_currentSpread);
            float randomYaw  = (float)(new Random().NextDouble() * 2 - 1) * spreadRad;
            float randomPitch= (float)(new Random().NextDouble() * 2 - 1) * spreadRad;

            var rot = Quaternion.FromEulerAngles(randomPitch, randomYaw, 0);
            return (rot * forward).Normalized();
        }

        private void DoHitscan(Vector3 direction, Core.Scene scene)
        {
            // ── Simple broad-phase AABB ray test ─────────────────────────────
            // A full engine would use a BVH / physics engine here (e.g. BEPUphysics).
            // For now we iterate entities and check bounding sphere overlap.

            var origin   = Owner.Transform.Position + new Vector3(0, 1.6f, 0); // eye level
            RayHit? best = null;

            foreach (var entity in scene.Entities)
            {
                if (entity == Owner) continue;

                // Rough sphere test: use entity position + radius 1.0
                const float radius = 1.0f;
                var toEntity = entity.Transform.Position - origin;
                float proj = Vector3.Dot(toEntity, direction);
                if (proj < 0 || proj > MaxRange) continue;

                var closest = origin + direction * proj;
                if ((closest - entity.Transform.Position).Length <= radius)
                {
                    var hit = new RayHit
                    {
                        Entity   = entity,
                        Point    = closest,
                        Normal   = (closest - entity.Transform.Position).Normalized(),
                        Distance = proj
                    };
                    if (best == null || proj < best.Value.Distance)
                        best = hit;
                }
            }

            if (best.HasValue) OnHit?.Invoke(best.Value);
        }

        private void SpawnProjectile(Vector3 direction, Core.Scene scene)
        {
            // Spawn a bullet entity that moves along 'direction' each frame.
            // Projectile lifetime / gravity handled by BulletProjectile component.
            var bullet = scene.AddEntity("Bullet");
            bullet.Transform.Position = Owner.Transform.Position + new Vector3(0, 1.6f, 0);

            var proj = new BulletProjectile
            {
                Velocity = direction * ProjectileSpeed,
                Damage   = Damage,
                Source   = Owner
            };
            bullet.AddComponent(proj);
        }

        private void FinishReload()
        {
            int needed  = MagazineSize - _currentAmmo;
            int taken   = Math.Min(needed, ReserveAmmo);
            _currentAmmo  += taken;
            ReserveAmmo   -= taken;
            _isReloading   = false;
        }

        // ---------------------------------------------------------------------------
        // UI-readable ammo string (e.g. "28 / 60")
        // ---------------------------------------------------------------------------

        public string AmmoDisplay => $"{_currentAmmo} / {ReserveAmmo}";
    }

    // =============================================================================
    // BulletProjectile — lightweight component riding on spawned bullet entities
    // =============================================================================

    internal class BulletProjectile : Core.Component
    {
        public Vector3      Velocity { get; set; }
        public float        Damage   { get; set; }
        public Core.Entity? Source   { get; set; }

        private float _lifetime = 5.0f; // Self-destructs after N seconds

        // Gravity in units/sec² applied to projectile arc
        private const float BulletGravity = 9.8f;

        public override void Update(float dt)
        {
            // Arc trajectory — gravity bends the bullet downward over time
            Velocity -= new Vector3(0, BulletGravity * dt, 0);
            Owner.Transform.Position += Velocity * dt;

            // Simple floor collision
            if (Owner.Transform.Position.Y < 0)
                RemoveSelf();

            _lifetime -= dt;
            if (_lifetime <= 0) RemoveSelf();
        }

        private void RemoveSelf()
        {
            // The Engine's deferred removal queue would handle this safely;
            // simplified here as a flag the Engine polls.
            Owner.IsVisible = false;
        }
    }
}
