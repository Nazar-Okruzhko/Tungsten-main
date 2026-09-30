using OpenTK.Mathematics;
using Tungsten.Assets;
using Tungsten.Physics;
using Tungsten.Player;
using Tungsten.Rendering;
using Tungsten.World;

namespace Tungsten.Core
{
    /// <summary>
    /// Composition root for the Tungsten runtime. The Studio editor creates
    /// exactly one of these and hosts it inside the 3D viewport panel - in
    /// "Edit" mode the Engine just renders the Scene under a free camera; in
    /// "Play" mode it also steps physics and the FirstPersonController, the
    /// same edit/play split Roblox Studio and Unity both use.
    /// </summary>
    public class Engine
    {
        public Scene Scene { get; } = new();
        public Renderer Renderer { get; }
        public SimplePhysicsWorld Physics { get; }
        public AssetDatabase Assets { get; } = new();
        public FirstPersonController Player { get; }
        public bool IsPlaying { get; private set; }

        public Engine(string sharedAssetsRoot)
        {
            Renderer = new Renderer();
            Physics = new SimplePhysicsWorld(Scene);
            Player = new FirstPersonController(Physics, new Vector3(0, 1f, 5f));
            Player.EquippedGun = new GunSystem(Weapon.Rifle(), Physics);

            Assets.LoadBuiltins();
            Assets.ScanSharedAudio(sharedAssetsRoot);

            BuildDefaultLevel();
            Logger.Info("Tungsten engine initialized.");
        }

        /// <summary>Spawns a small default level so the viewport isn't empty on first launch.</summary>
        private void BuildDefaultLevel()
        {
            var ground = Assets.Prefabs.Find(p => p.Name == "Ground Plane");
            ground?.Instantiate(Scene, Renderer, Vector3.Zero);

            var crate = Assets.Prefabs.Find(p => p.Name == "Crate");
            crate?.Instantiate(Scene, Renderer, new Vector3(2, 0.5f, -2));
            crate?.Instantiate(Scene, Renderer, new Vector3(-2, 0.5f, -3));

            var car = Assets.Prefabs.Find(p => p.Name == "Car");
            car?.Instantiate(Scene, Renderer, new Vector3(4, 0.6f, 2));
        }

        public void SetPlaying(bool playing) => IsPlaying = playing;

        public void Update(Input input)
        {
            if (IsPlaying)
                Player.Update(input);
        }

        public void Render(Camera activeCamera, int width, int height) =>
            Renderer.RenderScene(Scene, activeCamera, width, height);
    }
}
