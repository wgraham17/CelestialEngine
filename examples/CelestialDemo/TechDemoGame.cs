// -----------------------------------------------------------------------
// <copyright file="TechDemoGame.cs" company="">
// Copyright (C) Matt Razza
// </copyright>
// -----------------------------------------------------------------------

namespace CelestialEngine.TechDemo
{
    using CelestialEngine.Core;
    using CelestialEngine.Core.PostProcess;
    using CelestialEngine.Game;
    using CelestialEngine.Game.PostProcess.Lights;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Microsoft.Xna.Framework.Input;
    using Microsoft.Xna.Framework.Media;
    using System;

    /// <summary>
    /// Entry point for our lighting and shadow demo
    /// </summary>
    public class TechDemoGame : BaseGame
    {
        public bool IsDebug;
        private Random prng;
        private TiledSprite background;
        private AmbientLight amLight;
        private SimulatedLight mouseLight;
        private float secFrames;
        private double elapsed;
        private double lastFramesPerSec;
        private SpriteFont consoleFont;
        private int lightCount;
        private Song song;

        /// <summary>
        /// Initializes a new instance of the <see cref="TechDemoGame"/> class.
        /// </summary>
        public TechDemoGame()
        {
            Window.Title = "Celestial Engine Tech Demo";
        }

        /// <summary>
        /// Called after the Game and GraphicsDevice are created, but before LoadContent.  Reference page contains code sample.
        /// </summary>
        protected override void Initialize()
        {
            this.prng = new Random();
            this.GameCamera.Position = this.GameWorld.GetWorldFromPixel(new Vector2(this.GraphicsDevice.Viewport.Width / 2.0f, this.GraphicsDevice.Viewport.Height / 2.0f) / this.GameCamera.CameraZoom);

            this.InputManager.AddConditionalBinding((s) => { return s.IsKeyDown(Keys.Escape); }, (s) => this.Exit());
            this.InputManager.AddConditionalBinding((s) => { return s.IsLeftMouseClick() && s.IsKeyDown(Keys.LeftShift); }, SpawnNewStaticLight);
            this.InputManager.AddConditionalBinding((s) => { return s.IsLeftMouseClick() && !s.IsKeyDown(Keys.LeftShift); }, SpawnNewLight); 
            this.InputManager.AddConditionalBinding((s) => { return s.IsRightMouseClick(); }, SpawnNewRotatingLight);
            this.InputManager.AddConditionalBinding((s) => { return s.IsScrollWheelChanged(); }, PerformZoom);
            this.InputManager.AddConditionalBinding((s) => { return s.IsFirstKeyPress(Keys.B); }, (s) => mouseLight.MinShadowBlurDistance = mouseLight.MinShadowBlurDistance == float.PositiveInfinity ? 0.5f / 64.0f : float.PositiveInfinity);
            this.InputManager.AddBinding(UpdateCursorPosition);
            this.InputManager.AddConditionalBinding((s) => { return (s.IsKeyDown(Keys.LeftControl) || s.IsKeyDown(Keys.RightControl)) && s.IsLeftMouseDown(); }, SpawnNewSmallLight);
            this.IsMouseVisible = true;

            base.Initialize();
        }

        /// <summary>
        /// Loads the content.
        /// </summary>
        protected override void LoadContent()
        {
            this.consoleFont = this.Content.Load<SpriteFont>("Content\\LucidaConsole");

            // Create Tiled Sprites
            RectangleF bounds = this.RenderSystem.GetCameraRenderBounds();
            this.background = new TiledSprite(this.GameWorld, "Content/floortile", "Content/floortilenormal", null, bounds.Position, Vector2.Zero, bounds.AreaBounds)
            {
                RenderScale = new Vector2(0.4f, 0.4f),
                RenderOptions = SpriteRenderOptions.IsLit
            };

            SimpleSprite test = new SimpleSprite(this.GameWorld, "Content/box", null, false)
            {
                Position = new Vector2(8, 7),
                RenderScale = new Vector2(0.4f, 0.4f),
                RenderOptions = SpriteRenderOptions.CastsShadows | SpriteRenderOptions.IsLit,
                SpecularReflectivity = 0,
                LayerDepth = 1
            };
            test.Body.BodyType = FarseerPhysics.Dynamics.BodyType.Static;
            test.Body.CollisionCategories = FarseerPhysics.Dynamics.Category.Cat1;
            test.Body.Friction = 0;
            test.Body.Restitution = 1.0f;

            if (this.IsDebug)
            {
                var x = new DebugSprite(this.GameWorld, test);
            }

            SimpleSprite test2 = new SimpleSprite(this.GameWorld, "Content/box", null, false)
            {
                Position = new Vector2(10, 8),
                Rotation = -MathHelper.PiOver2,
                RenderScale = new Vector2(0.4f, 0.4f),
                RenderOptions = SpriteRenderOptions.CastsShadows | SpriteRenderOptions.IsLit,
                SpecularReflectivity = 0,
                LayerDepth = 2
            };
            test2.Body.BodyType = FarseerPhysics.Dynamics.BodyType.Static;
            test2.Body.CollisionCategories = FarseerPhysics.Dynamics.Category.Cat1;
            test2.Body.Friction = 0;
            test2.Body.Restitution = 1.0f;

            if (this.IsDebug)
            {
                var x = new DebugSprite(this.GameWorld, test2);
            }

            SimpleSprite test3 = new SimpleSprite(this.GameWorld, "Content/gear", null, true)
            {
                Position = new Vector2(20, 8),
                Rotation = -MathHelper.PiOver2,
                RenderScale = new Vector2(0.4f, 0.4f),
                RenderOptions = SpriteRenderOptions.CastsShadows | SpriteRenderOptions.IsLit,
                SpecularReflectivity = 0,
                LayerDepth = 2
            };
            test3.Body.BodyType = FarseerPhysics.Dynamics.BodyType.Static;
            test3.Body.CollisionCategories = FarseerPhysics.Dynamics.Category.Cat1;
            test3.Body.Friction = 0;
            test3.Body.Restitution = 1.0f;

            if (this.IsDebug)
            {
                var x = new DebugSprite(this.GameWorld, test3);
            }

            this.amLight = new AmbientLight(Color.White, 0.02f, true, 1);
            this.RenderSystem.AddPostProcessEffect(this.amLight);

            mouseLight = new PointLight(this.GameWorld)
            {
                Color = Color.White,
                Power = 1f,
                Range = 8,
                SpecularStrength = 4.75f,
                CastsShadows = true,
                MinShadowBlurDistance = .5f,
                MaxShadowBlurDistance = 1.5f
            };
            lightCount++;

            if (this.IsDebug)
            {
                DebugSimulatedPostProcess y = new DebugSimulatedPostProcess(this.GameWorld, mouseLight);
            }

            this.RenderSystem.AddPostProcessEffect(mouseLight);

            // https://www.youtube.com/watch?v=BExTagcymo0
            // George Ellinas - Pulse (George Ellinas Remix) (Free - Creative Commons MP3)
            // http://creativecommons.org/licenses/by/3.0/
            this.song = Content.Load<Song>("Content/George_Ellinas_-_Pulse_(George_Ellinas_remix)_LoopEdit");
            MediaPlayer.Volume = 0.1f;
            MediaPlayer.Play(song);


            base.LoadContent();
        }

        /// <summary>
        /// Draws the specified game time.
        /// </summary>
        /// <param name="gameTime">The game time.</param>
        protected override void Draw(GameTime gameTime)
        {
            Window.Title = String.Format("Celestial Engine Tech Demo [{0} lights @ {1} FPS(real), {2} FPS(avg)]", lightCount, Math.Round(1000.0f / gameTime.ElapsedGameTime.TotalMilliseconds), lastFramesPerSec);

            base.Draw(gameTime);

            secFrames++;
            elapsed += gameTime.ElapsedGameTime.TotalSeconds;

            if (elapsed > 1.0)
            {
                lastFramesPerSec = Math.Round(secFrames / elapsed);
                elapsed = 0;
                secFrames = 0;
            }
        }

        /// <summary>
        /// Performs the zoom.
        /// </summary>
        /// <param name="state">The state.</param>
        private void PerformZoom(InputState state)
        {
            this.GameCamera.CameraZoom = state.CurrentMouseState.ScrollWheelValue / 120.0f;
            RectangleF bounds = this.RenderSystem.GetCameraRenderBounds();
            this.background.Position = bounds.Position;
            this.background.TileArea = bounds.AreaBounds;
        }

        /// <summary>
        /// Updates the cursor position.
        /// </summary>
        /// <param name="state">The state.</param>
        private void UpdateCursorPosition(InputState state)
        {
            if (mouseLight != null)
                mouseLight.Position = new Vector3(this.RenderSystem.GetWorldFromScreen(new Vector2(state.CurrentMouseState.X, state.CurrentMouseState.Y)), 0.15f);
        }

        /// <summary>
        /// Spawns the new rotating light.
        /// </summary>
        /// <param name="state">The state.</param>
        private void SpawnNewRotatingLight(InputState state)
        {
            BouncyConeLight newLight = new BouncyConeLight(this.GameWorld)
            {
                Position = new Vector3(this.RenderSystem.GetWorldFromScreen(new Vector2(state.CurrentMouseState.X, state.CurrentMouseState.Y)), 0.15f),
                Velocity = this.GetRandomVelocity(),
                Color = this.GetRandomColor(),
                Power = 0.5f,
                Range = this.prng.Next(400, 600) / 100.0f,
                SpecularStrength = 4.75f,
                AngularVelocity = this.prng.Next(40, 90) / 100.0f,
                LightAngle = MathHelper.PiOver2,
                MinShadowBlurDistance = .5f,
                MaxShadowBlurDistance = 2f,
                CastsShadows = true
            };
            lightCount++;

            if (this.IsDebug)
            {
                var x = new DebugSimulatedPostProcess(this.GameWorld, newLight);
            }

            this.RenderSystem.AddPostProcessEffect(newLight);
        }

        /// <summary>
        /// Spawns the new light.
        /// </summary>
        /// <param name="state">The state.</param>
        private void SpawnNewLight(InputState state)
        {
            PointLight newLight = new PointLight(this.GameWorld)
            {
                Position = new Vector3(this.RenderSystem.GetWorldFromScreen(new Vector2(state.CurrentMouseState.X, state.CurrentMouseState.Y)), 0.15f),
                Velocity = this.GetRandomVelocity(),
                Color = this.GetRandomColor(),
                Range = 8,
                Power = .5f,
                SpecularStrength = 4.75f,
                CastsShadows = true,
                MinShadowBlurDistance = .5f,
                MaxShadowBlurDistance = 2f
            };
            lightCount++;
            newLight.Body.BodyType = FarseerPhysics.Dynamics.BodyType.Dynamic;
            newLight.Body.CollidesWith = FarseerPhysics.Dynamics.Category.Cat1;
            newLight.Body.CollisionCategories = FarseerPhysics.Dynamics.Category.Cat2;
            newLight.Body.Friction = 0;
            newLight.Body.Restitution = 1.0f;

            if (this.IsDebug)
            {
                DebugSimulatedPostProcess z = new DebugSimulatedPostProcess(this.GameWorld, newLight);
            }

            this.RenderSystem.AddPostProcessEffect(newLight);
        }

        /// <summary>
        /// Spawns the new static light.
        /// </summary>
        /// <param name="state">The state.</param>
        private void SpawnNewStaticLight(InputState state)
        {
            PointLight newLight = new PointLight(this.GameWorld)
            {
                Position = new Vector3(this.RenderSystem.GetWorldFromScreen(new Vector2(state.CurrentMouseState.X, state.CurrentMouseState.Y)), 0.15f),
                Color = this.GetRandomColor(),
                Range = 8,
                Power = .5f,
                SpecularStrength = 4.75f,
                CastsShadows = true,
                MinShadowBlurDistance = .5f,
                MaxShadowBlurDistance = 2f
            };
            lightCount++;
            newLight.Body.BodyType = FarseerPhysics.Dynamics.BodyType.Dynamic;
            newLight.Body.CollidesWith = FarseerPhysics.Dynamics.Category.Cat1;
            newLight.Body.CollisionCategories = FarseerPhysics.Dynamics.Category.Cat2;
            newLight.Body.Friction = 0;
            newLight.Body.Restitution = 1.0f;

            if (this.IsDebug)
            {
                DebugSimulatedPostProcess z = new DebugSimulatedPostProcess(this.GameWorld, newLight);
            }

            this.RenderSystem.AddPostProcessEffect(newLight);
        }

        /// <summary>
        /// Spawns the new small light.
        /// </summary>
        /// <param name="state">The state.</param>
        private void SpawnNewSmallLight(InputState state)
        {
            BouncyPointLight newLight = new BouncyPointLight(this.GameWorld)
            {
                Position = new Vector3(this.RenderSystem.GetWorldFromScreen(new Vector2(state.CurrentMouseState.X, state.CurrentMouseState.Y)), 0.15f),
                Velocity = this.GetRandomVelocity(),
                Color = this.GetRandomColor(),
                Power = 0.25f,
                Range = this.prng.Next(300, 500) / 100.0f,
                SpecularStrength = 2.75f,
                MinShadowBlurDistance = .5f,
                MaxShadowBlurDistance = 2f,
                CastsShadows = true
            };
            lightCount++;

            if (this.IsDebug)
            {
                DebugSimulatedPostProcess l = new DebugSimulatedPostProcess(this.GameWorld, newLight);
            }

            this.RenderSystem.AddPostProcessEffect(newLight);
        }

        /// <summary>
        /// Gets a random color.
        /// </summary>
        /// <returns>A random color within our constraints of brightness.</returns>
        private Color GetRandomColor()
        {
            int f = 0;
            int s = 0;
            int t = 0;

            while (Math.Sqrt(s * s + f * f + t * t) < 250)
            {
                f = this.prng.Next(0, 255);
                s = this.prng.Next(0, 255);
                t = this.prng.Next(0, 255);
            }

            return new Color(f, s, t);
        }

        /// <summary>
        /// Gets a random velocity.
        /// </summary>
        /// <returns>A random velocity within our constraints of speed.</returns>
        private Vector3 GetRandomVelocity()
        {
            Vector3 currVal;

            do
            {
                currVal = new Vector3(this.prng.Next(-400, 400) / 100.0f, this.prng.Next(-400, 400) / 100.0f, 0);
            } while (currVal.Length() < 1.5f);

            return currVal;
        }
    }
}
