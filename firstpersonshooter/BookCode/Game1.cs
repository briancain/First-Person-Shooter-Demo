using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;

namespace BookCode
{
    public class Shell
    {
        public Vector3 Position;
        public Vector3 Direction;
    }

    public class Game1 : Microsoft.Xna.Framework.Game
    {
        #region Fields
        private GraphicsDeviceManager graphics;
        private GraphicsDevice device;
        private FirstPersonCamera camera;
        private CoordCross cCross;
        private FPS fps;
        private Texture2D crosshair;
        private SpriteBatch spriteBatch;
        private Terrain terrain;
        //
        private int windowWidth;
        private int windowHeight;
        //
        private const float weaponScale = 0.03f;
        private const float weaponX = 0.45f;
        private const float weaponY = -0.75f;
        private const float weaponZ = 2.0f;
        private Vector3 weaponVec = new Vector3(weaponX, weaponY, weaponZ);
        //
        private const float CAMERA_FOVX = 85.0f;
        private const float CAMERA_ZNEAR = 0.01f;
        private const float CAMERA_ZFAR = 1024.0f * 2.0f;
        //
        private Model weapon;
        private Matrix[] weaponTransforms;
        private Matrix weaponWorldMatrix;
        //
        private Model dino;
        const float dinoScale = 0.01f;
        private Matrix scale = Matrix.CreateScale(dinoScale);
        private Matrix orientation = Matrix.Identity;
        private Vector3 dinoPosition;
        private float dinofacingDirection;

        List<Shell> shells;
        int cooldown;
        SpherePrimitive spherePrimitive;
        int reloadCoolDown;
        private int Ammo;
        private int Clips;
        #endregion

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
#if !DEBUG
            Window.Title = "Super Mars Battle 2010 Demo";
#endif
            // Add FPS to game window
            fps = new FPS(this, true, true, this.TargetElapsedTime);
            Components.Add(fps);

            Vector3 orient = dinoPosition - Vector3.Zero;
            dinofacingDirection = (float)Math.Atan(orient.X / orient.Z) + MathHelper.Pi;
        }
        
        protected override void Initialize()
        {
            // Setup the window to be a quarter the size of the desktop.
            windowWidth = GraphicsDevice.DisplayMode.Width;// / 2;
            windowHeight = GraphicsDevice.DisplayMode.Height;// / 2;
            // Setup frame buffer.
            graphics.SynchronizeWithVerticalRetrace = false;
            graphics.PreferredBackBufferWidth = windowWidth;
            graphics.PreferredBackBufferHeight = windowHeight;
            graphics.PreferMultiSampling = true;
            graphics.ApplyChanges();

            //fpsCam = new QuakeCamera(graphics.GraphicsDevice.Viewport, new Vector3(1,15,-1), 0, 0);
            camera = new FirstPersonCamera(this);
            Components.Add(camera);

            camera.EyeHeightStanding = 110.0f;
            camera.Acceleration = new Vector3(100.0f, 100.0f, 100.0f);
            camera.Velocity = new Vector3(10.0f, 10.0f, 10.0f);
            camera.Perspective(CAMERA_FOVX, (float)windowWidth / (float)windowHeight, CAMERA_ZNEAR, CAMERA_ZFAR);

            shells = new List<Shell>();
            cooldown = 0;
            Ammo = 50;
            Clips = 5;
            reloadCoolDown = 0;
            
            base.Initialize();
        }

        protected override void LoadContent()
        {
            
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // Load our fonts into the static Font class
            Fonts.LoadContent(Content);

            device = graphics.GraphicsDevice;
            cCross = new CoordCross(device);

            weapon = Content.Load<Model>("weapon");
            crosshair = Content.Load<Texture2D>("crosshair1");
            // Initialize the weapon matrices.
            weaponTransforms = new Matrix[weapon.Bones.Count];
            weaponWorldMatrix = Matrix.Identity;

            terrain = new Terrain(device, Content);
            dino = Content.Load<Model>("dino");
            spherePrimitive = new SpherePrimitive(GraphicsDevice, 0.5f, 12);

        } 
     
        protected override void UnloadContent()
        {
           
        }
    
        protected override void Update(GameTime gameTime)
        {
            dinoPosition.X = 247.0f;
            dinoPosition.Z = -270.0f;
            dinoPosition.Y = 20.0f;

            GamePadState gamePadState = GamePad.GetState(PlayerIndex.One);
            if (gamePadState.Buttons.Back == ButtonState.Pressed)
                this.Exit();

            MouseState mouseState = Mouse.GetState();
            KeyboardState keyState = Keyboard.GetState();

            camera.Update(gameTime);

            float treshold = 3.0f;
            float terrainHeight = terrain.GetExactHeightAt(camera.Position.X, -camera.Position.Z);            
            Vector3 newPos = camera.Position;
            newPos.Y = terrainHeight + treshold;
            camera.Position = newPos;

            ProcessKeyboard(keyState, mouseState, gameTime);

            UpdateWeapon();
            base.Update(gameTime);
        }


        private void ProcessKeyboard(KeyboardState keyState, MouseState mouseState, GameTime gameTime)
        {
            if (keyState.IsKeyDown(Keys.Escape)) Exit();

            cooldown -= gameTime.ElapsedGameTime.Milliseconds;
            if (mouseState.LeftButton == ButtonState.Pressed && cooldown < 0 && Ammo > 0)
            {
                Ammo--;
                Matrix shoot = weaponTransforms[0];
                Shell shell = new Shell();
                shell.Position = Vector3.Transform(Vector3.Zero, weaponWorldMatrix);
                shell.Direction = camera.ViewDirection;
                shells.Add(shell);
                cooldown = 100;
            }
            reloadCoolDown -= gameTime.ElapsedGameTime.Milliseconds;
            if (keyState.IsKeyDown(Keys.R) && reloadCoolDown < 0 && Ammo < 50)
            {
                if (Clips > 0)
                {
                    Ammo = 50;
                    Clips--;
                    reloadCoolDown = 1000;
                }
            }

            foreach (Shell shell in shells)
            {
                shell.Position += shell.Direction * 5;
            }

            if (keyState.IsKeyDown(Keys.Y))
            {
                Ammo = 50;
                Clips = 5;
            }
        }
            
        protected override void Draw(GameTime gameTime)
        {
            graphics.GraphicsDevice.Clear(Color.Beige);
            //
            //draw coordcross
            cCross.Draw(camera.ViewMatrix, camera.ProjectionMatrix);

            terrain.Draw(Matrix.Identity, camera.ViewMatrix, camera.ProjectionMatrix);

            UpdateDino();
            // Draw the weapon.
            foreach (Shell shell in shells)
            {
                spherePrimitive.Draw(Matrix.CreateTranslation(shell.Position), camera.ViewMatrix, camera.ProjectionMatrix, Color.Gray);
            }
            string ammoString = Ammo + "/" + (Clips * 50);

            Rectangle clientBounds = Window.ClientBounds;
            Vector2 crosshairPlace = new Vector2((clientBounds.Width / 2) - (crosshair.Width / 2), (clientBounds.Height / 2) - (crosshair.Height / 2));
            float textSpace = clientBounds.Height - 100;
            float textSpaceNumber = clientBounds.Height - 65;

            spriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Texture, SaveStateMode.SaveState);
            spriteBatch.Draw(crosshair, crosshairPlace, Color.Green);
            if (Ammo > 0)
            {
                spriteBatch.DrawString(Fonts.MenuFont, "Ammo", new Vector2(20, textSpace), Fonts.PlayerColor);
            }
            else if (Ammo >= 0 && Clips > 0)
            {
                spriteBatch.DrawString(Fonts.MenuFont, "RELOAD", new Vector2(20, textSpace), Color.Yellow);
            }
            else
            {
                spriteBatch.DrawString(Fonts.MenuFont, "EMPTY", new Vector2(20, textSpace), Color.Yellow);
            }
            spriteBatch.DrawString(Fonts.MenuFont, ammoString, new Vector2(20, textSpaceNumber), Fonts.PlayerColor);
            spriteBatch.End();
            ResetRenderstateFor3D();

            foreach (ModelMesh m in weapon.Meshes)
            {
                foreach (BasicEffect e in m.Effects)
                {
                    e.EnableDefaultLighting();
                    e.World = weaponTransforms[m.ParentBone.Index] * weaponWorldMatrix;
                    e.View = camera.ViewMatrix;
                    e.Projection = camera.ProjectionMatrix;
                }

                m.Draw();
            }

            base.Draw(gameTime);
        }

        private void UpdateDino()
        {
            // TODO: Add your drawing code here
            float height = terrain.GetClippedHeightAt(0, 0);
            Matrix worldMatrix = Matrix.Identity * scale;
            worldMatrix = Matrix.CreateScale(50.0f, 50.0f, 50.0f);
            worldMatrix *= Matrix.CreateTranslation(dinoPosition);
            foreach (ModelMesh mesh in dino.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.EnableDefaultLighting();
                    effect.World = worldMatrix;
                    effect.View = camera.ViewMatrix;
                    effect.Projection = camera.ProjectionMatrix;
                }
                mesh.Draw();
            }
        }

        private void UpdateWeapon()
        {
            weapon.CopyAbsoluteBoneTransformsTo(weaponTransforms);

            weaponWorldMatrix = camera.WeaponWorldMatrix(weaponX, weaponY, weaponZ, weaponScale);
        }
        /// <summary>
        /// When we call SpriteBatch.Draw(), the SpriteBatch modifes the RenderState
        /// of the GraphicsDevice in order to do 2D rendering, but some of the values
        /// muck with 3D rendering.  We could save and restore the entire RenderState 
        /// every time we switch between 2D and 3D rendering, but that is an expensive
        /// operation.  Instead we'll reset these three RenderState options manually,
        /// which will be much more efficient and should make our 3D rendering look 
        /// normal.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        private void ResetRenderstateFor3D()
        {
            GraphicsDevice.RenderState.DepthBufferEnable = true;
            GraphicsDevice.RenderState.AlphaBlendEnable = false;
            GraphicsDevice.RenderState.AlphaTestEnable = false;
        }
    }
}