using Tungsten.Core;
using Tungsten.Physics;

namespace Tungsten.Player
{
    /// <summary>
    /// The built-in Gun System the brief asked for. Any Weapon preset
    /// (pistol/rifle/shotgun, or a custom one dragged from the Asset Store)
    /// plugs into this same class - it only needs the Weapon's stats, so
    /// adding a new gun to the store never means writing new C#.
    /// </summary>
    public class GunSystem
    {
        public Weapon Weapon;
        public int AmmoInMagazine { get; private set; }
        public bool IsReloading { get; private set; }

        private float _fireCooldown;
        private float _reloadTimer;
        private readonly SimplePhysicsWorld _physics;

        public event System.Action<Tungsten.World.Entity, float>? OnHit; // (entity hit, damage)
        public event System.Action? OnFire;

        public GunSystem(Weapon weapon, SimplePhysicsWorld physics)
        {
            Weapon = weapon;
            _physics = physics;
            AmmoInMagazine = weapon.MagazineSize;
        }

        public void Update(FirstPersonController owner, bool wantsFire, bool wantsReload)
        {
            _fireCooldown -= Time.DeltaTime;

            if (IsReloading)
            {
                _reloadTimer -= Time.DeltaTime;
                if (_reloadTimer <= 0f)
                {
                    IsReloading = false;
                    AmmoInMagazine = Weapon.MagazineSize;
                    Logger.Info($"{Weapon.Name} reloaded.");
                }
                return;
            }

            if (wantsReload && AmmoInMagazine < Weapon.MagazineSize)
            {
                IsReloading = true;
                _reloadTimer = Weapon.ReloadTime;
                Logger.Info($"Reloading {Weapon.Name}...");
                return;
            }

            bool triggerHeld = Weapon.Automatic ? wantsFire : wantsFire;
            if (triggerHeld && _fireCooldown <= 0f)
            {
                if (AmmoInMagazine <= 0)
                {
                    Logger.Warn($"{Weapon.Name} click - out of ammo.");
                    _fireCooldown = 0.25f;
                    return;
                }
                Fire(owner);
                _fireCooldown = 1f / Weapon.FireRate;
            }
        }

        private void Fire(FirstPersonController owner)
        {
            AmmoInMagazine--;
            owner.Animator.ApplyRecoilKick(Weapon.RecoilKick);
            OnFire?.Invoke();

            bool didHit = _physics.Raycast(owner.Camera.Position, owner.Camera.Front, Weapon.Range, out var hit, out float dist);
            if (didHit && hit != null)
            {
                OnHit?.Invoke(hit, Weapon.Damage);
                Logger.Info($"{Weapon.Name} hit '{hit.Name}' at {dist:F1}m for {Weapon.Damage} dmg.");
            }
            else
            {
                Logger.Info($"{Weapon.Name} fired - no target in range.");
            }
        }
    }
}
