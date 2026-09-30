namespace Tungsten.Player
{
    /// <summary>
    /// Pure data describing one gun. Kept separate from GunSystem (the
    /// behaviour) so the Asset Store can offer many different Weapon presets
    /// (pistol, rifle, shotgun) that all run through the exact same
    /// GunSystem logic - "universal assets" for weapons.
    /// </summary>
    public class Weapon
    {
        public string Name = "Pistol";
        public int MagazineSize = 12;
        public float Damage = 18f;
        public float FireRate = 6f;        // rounds per second
        public float Range = 120f;
        public float ReloadTime = 1.4f;
        public float RecoilKick = 2.2f;
        public bool Automatic = false;

        public static Weapon Pistol() => new() { Name = "Pistol", MagazineSize = 12, Damage = 22, FireRate = 4, Range = 80, ReloadTime = 1.2f, RecoilKick = 1.8f, Automatic = false };
        public static Weapon Rifle() => new() { Name = "Rifle", MagazineSize = 30, Damage = 16, FireRate = 9, Range = 160, ReloadTime = 1.8f, RecoilKick = 1.1f, Automatic = true };
        public static Weapon Shotgun() => new() { Name = "Shotgun", MagazineSize = 6, Damage = 55, FireRate = 1.2f, Range = 25, ReloadTime = 2.2f, RecoilKick = 4.5f, Automatic = false };
    }
}
