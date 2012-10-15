#region Credits
//http://www.dhpoware.com/index.html	
// This file FirstPersonCamera.cs, was built off of a demo explaining how to
// properly handle a gun using a first person camera.
// 
//Welcome to dhpoware.
//This site is dedicated to real-time 2D/3D graphics and games 
//programming using OpenGL, Direct3D, and XNA. We maintain a repository 
//of source code that you can use in your own projects. Occasionally we 
//release demos showcasing particular graphics and games programming techniques.
#endregion

#region Using Statements
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
#endregion

namespace BookCode
{
    public class FirstPersonCamera : GameComponent
    {
        public enum Actions
        {
            Forward,
            Backwards,
            Right,
            Left,
        }
        #region Fields
        //
        public const float DEFAULT_FOVX = 90.0f;
        public const float DEFAULT_ZNEAR = 0.1f;
        public const float DEFAULT_ZFAR = 1000.0f;
        //
        private static Vector3 worldx = new Vector3(1.0f, 0.0f, 0.0f);
        private static Vector3 worldy = new Vector3(0.0f, 1.0f, 0.0f);
        private static Vector3 worldz = new Vector3(0.0f, 0.0f, 1.0f);
        //
        private const float DEFAULT_MOUSE_SMOOTHING_SENSITIVITY = 0.5f;
        private const float DEFAULT_SPEED_ROTATION = 0.3f;
        private const int MOUSE_SMOOTHING_CACHE_SIZE = 10;
        //
        private float fovx;
        private float aspectRatio;
        private float znear;
        private float zfar;
        //
        private float accumHeadingDegrees;
        private float accumPitchDegrees;
        private float eyeHeight;
        private Vector3 eye;
        private Vector3 target;
        private Vector3 xAxis;
        private Vector3 yAxis;
        private Vector3 zAxis;
        private Vector3 viewDir;
        //
        private Vector3 acceleration;
        private Vector3 currentVelocity;
        private Vector3 velocity;
        private Vector3 velocityWalking;
        //
        private Quaternion orientation;
        private Matrix viewMatrix;
        private Matrix projMatrix;
        //
        private bool forwardsPressed;
        private bool backwardsPressed;
        private bool strafeRightPressed;
        private bool strafeLeftPressed;
        //
        private float rotationSpeed;
        private Vector2[] mouseMovement;
        private Vector2 smoothedMouseMovement;
        private MouseState currentMouseState;
        private MouseState previousMouseState;
        private KeyboardState currentKeyboardState;
        private KeyboardState previousKeyboardState;
        private Dictionary<Actions, Keys> actionKeys;
        #endregion

        #region Public Methods

        public FirstPersonCamera(Game game) : base(game)
        {
            UpdateOrder = 1;
            // Initialize camera state.
            fovx = DEFAULT_FOVX;
            znear = DEFAULT_ZNEAR;
            zfar = DEFAULT_ZFAR;
            accumHeadingDegrees = 0.0f;
            accumPitchDegrees = 0.0f;
            eyeHeight = 0.0f;
            eye = Vector3.Zero;
            target = Vector3.Zero;
            xAxis = Vector3.UnitX;
            yAxis = Vector3.UnitY;
            zAxis = Vector3.UnitZ;
            viewDir = Vector3.Forward;
            acceleration = new Vector3(100.0f, 100.0f, 100.0f);
            velocityWalking = new Vector3(10.0f, 10.0f, 10.0f);
            velocity = new Vector3(10.0f, 10.0f, 10.0f);
            orientation = Quaternion.Identity;
            viewMatrix = Matrix.Identity;
            // Initialize mouse and keyboard input.
            rotationSpeed = DEFAULT_SPEED_ROTATION;
            mouseMovement = new Vector2[2];
            mouseMovement[0].X = 0.0f;
            mouseMovement[0].Y = 0.0f;
            mouseMovement[1].X = 0.0f;
            mouseMovement[1].Y = 0.0f;
            // Setup default action key bindings.
            actionKeys = new Dictionary<Actions, Keys>();
            actionKeys.Add(Actions.Forward, Keys.W);
            actionKeys.Add(Actions.Backwards, Keys.S);
            actionKeys.Add(Actions.Right, Keys.D);
            actionKeys.Add(Actions.Left, Keys.A);
            // Get initial keyboard and mouse states.
            currentKeyboardState = Keyboard.GetState();
            currentMouseState = Mouse.GetState();
            // Setup perspective projection matrix.
            Rectangle clientBounds = game.Window.ClientBounds;
            float aspect = (float)clientBounds.Width / (float)clientBounds.Height;
            Perspective(fovx, aspect, znear, zfar);
        }

        public override void Initialize()
        {
            base.Initialize();

            Rectangle clientBounds = Game.Window.ClientBounds;
            Mouse.SetPosition((clientBounds.Width / 2), (clientBounds.Height / 2));
        }

        /// <summary>
        /// Binds an action to a keyboard key.
        /// </summary>
        /// <param name="action">The action to bind.</param>
        /// <param name="key">The key to map the action to.</param>
        public void MapActionToKey(Actions action, Keys key)
        {
            actionKeys[action] = key;
        }

        /// <summary>
        /// Moves the camera by dx world units to the left or right; dy
        /// world units upwards or downwards; and dz world units forwards
        /// or backwards.
        /// </summary>
        /// <param name="dx">Distance to move left or right.</param>
        /// <param name="dy">Distance to move up or down.</param>
        /// <param name="dz">Distance to move forwards or backwards.</param>
        public void Move(float dx, float dy, float dz)
        {
            // Calculate the forwards direction. Can't just use the
            // camera's view direction as doing so will cause the camera to
            // move more slowly as the camera's view approaches 90 degrees
            // straight up and down.

            Vector3 forwards = Vector3.Normalize(Vector3.Cross(worldy, xAxis));
            
            eye += xAxis * dx;
            eye += worldy * dy;
            eye += forwards * dz;

            Position = eye;
        }

        public void Perspective(float fovx, float aspect, float znear, float zfar)
        {
            this.fovx = fovx;
            this.aspectRatio = aspect;
            this.znear = znear;
            this.zfar = zfar;

            float aspectInv = 1.0f / aspect;
            float e = 1.0f / (float)Math.Tan(MathHelper.ToRadians(fovx) / 2.0f);
            float fovy = 2.0f * (float)Math.Atan(aspectInv / e);
            float xScale = 1.0f / (float)Math.Tan(0.5f * fovy);
            float yScale = xScale / aspectInv;

            projMatrix.M11 = xScale;
            projMatrix.M12 = 0.0f;
            projMatrix.M13 = 0.0f;
            projMatrix.M14 = 0.0f;

            projMatrix.M21 = 0.0f;
            projMatrix.M22 = yScale;
            projMatrix.M23 = 0.0f;
            projMatrix.M24 = 0.0f;

            projMatrix.M31 = 0.0f;
            projMatrix.M32 = 0.0f;
            projMatrix.M33 = (zfar + znear) / (znear - zfar);
            projMatrix.M34 = -1.0f;

            projMatrix.M41 = 0.0f;
            projMatrix.M42 = 0.0f;
            projMatrix.M43 = (2.0f * zfar * znear) / (znear - zfar);
            projMatrix.M44 = 0.0f;
        }

        public void Rotate(float headingDegrees, float pitchDegrees)
        {
            headingDegrees = -headingDegrees;
            pitchDegrees = -pitchDegrees;
            
            accumPitchDegrees += pitchDegrees;

            if (accumPitchDegrees > 90.0f)
            {
                pitchDegrees = 90.0f - (accumPitchDegrees - pitchDegrees);
                accumPitchDegrees = 90.0f;
            }

            if (accumPitchDegrees < -90.0f)
            {
                pitchDegrees = -90.0f - (accumPitchDegrees - pitchDegrees);
                accumPitchDegrees = -90.0f;
            }

            accumHeadingDegrees += headingDegrees;

            if (accumHeadingDegrees > 360.0f)
                accumHeadingDegrees -= 360.0f;

            if (accumHeadingDegrees < -360.0f)
                accumHeadingDegrees += 360.0f;

            float heading = MathHelper.ToRadians(headingDegrees);
            float pitch = MathHelper.ToRadians(pitchDegrees);
            Quaternion rotation = Quaternion.Identity;

            // Rotate the camera about the world Y axis.
            if (heading != 0.0f)
            {
                Quaternion.CreateFromAxisAngle(ref worldy, heading, out rotation);
                Quaternion.Concatenate(ref rotation, ref orientation, out orientation);
            }

            // Rotate the camera about its local X axis.
            if (pitch != 0.0f)
            {
                Quaternion.CreateFromAxisAngle(ref worldx, pitch, out rotation);
                Quaternion.Concatenate(ref orientation, ref rotation, out orientation);
            }

            UpdateViewMatrix();
        }

        public override void Update(GameTime gameTime)
        {
#if DEBUG
            Game.Window.Title = "Position<x,y,z>: <" + Position.X + " , " + Position.Y + " , " + Position.Z + ">";
#endif
            base.Update(gameTime);
            UpdateInput();
            UpdateCamera(gameTime);
        }

        /// <summary>
        /// Calculates the world transformation matrix for the weapon attached
        /// to the FirstPersonCamera. The weapon moves along with the camera.
        /// The offsets are to ensure the weapon is slightly in front of the
        /// camera and to one side.
        /// </summary>
        /// <param name="xOffset">How far to position the weapon left or right.</param>
        /// <param name="yOffset">How far to position the weapon up or down.</param>
        /// <param name="zOffset">How far to position the weapon in front or behind.</param>
        /// <param name="scale">How much to scale the weapon.</param>
        /// <returns>The weapon world transformation matrix.</returns>
        public Matrix WeaponWorldMatrix(float xOffset, float yOffset, float zOffset, float scale)
        {
            Vector3 weaponPos = eye;

            weaponPos += viewDir * zOffset;
            weaponPos += yAxis * yOffset;
            weaponPos += xAxis * xOffset;

            return Matrix.CreateScale(scale) * Matrix.CreateRotationX(MathHelper.ToRadians(PitchDegrees)) * Matrix.CreateRotationY(MathHelper.ToRadians(HeadingDegrees)) * Matrix.CreateTranslation(weaponPos);
        }

    #endregion

    #region Private Methods

        /// <summary>
        /// Determines which way to move the camera based on player input.
        /// The returned values are in the range [-1,1].
        /// </summary>
        /// <param name="direction">The movement direction.</param>
        private void GetMovementDirection(out Vector3 direction)
        {
            direction.X = 0.0f;
            direction.Y = 0.0f;
            direction.Z = 0.0f;

            if (currentKeyboardState.IsKeyDown(actionKeys[Actions.Forward]))
            {
                if (!forwardsPressed)
                {
                    forwardsPressed = true;
                    currentVelocity.Z = 0.0f;
                }

                direction.Z += 1.0f;
            }
            else
            {
                forwardsPressed = false;
            }

            if (currentKeyboardState.IsKeyDown(actionKeys[Actions.Backwards]))
            {
                if (!backwardsPressed)
                {
                    backwardsPressed = true;
                    currentVelocity.Z = 0.0f;
                }

                direction.Z -= 1.0f;
            }
            else
            {
                backwardsPressed = false;
            }

            if (currentKeyboardState.IsKeyDown(actionKeys[Actions.Right]))
            {
                if (!strafeRightPressed)
                {
                    strafeRightPressed = true;
                    currentVelocity.X = 0.0f;
                }

                direction.X += 1.0f;
            }
            else
            {
                strafeRightPressed = false;
            }

            if (currentKeyboardState.IsKeyDown(actionKeys[Actions.Left]))
            {
                if (!strafeLeftPressed)
                {
                    strafeLeftPressed = true;
                    currentVelocity.X = 0.0f;
                }

                direction.X -= 1.0f;
            }
            else
            {
                strafeLeftPressed = false;
            }
        }

        /// <summary>
        /// Dampens the rotation by applying the rotation speed to it.
        /// </summary>
        /// <param name="headingDegrees">Y axis rotation in degrees.</param>
        /// <param name="pitchDegrees">X axis rotation in degrees.</param>
        private void RotateSmoothly(float headingDegrees, float pitchDegrees)
        {
            headingDegrees *= rotationSpeed;
            pitchDegrees *= rotationSpeed;

            Rotate(headingDegrees, pitchDegrees);
        }

        private void UpdateCamera(GameTime gameTime)
        {
            float elapsedTimeSec = 0.0f;

            if (Game.IsFixedTimeStep)
                elapsedTimeSec = (float)gameTime.ElapsedGameTime.TotalSeconds;
            else
                elapsedTimeSec = (float)gameTime.ElapsedRealTime.TotalSeconds;

            Vector3 direction = new Vector3();

            velocity = velocityWalking;

            GetMovementDirection(out direction);
                                    
            RotateSmoothly(smoothedMouseMovement.X, smoothedMouseMovement.Y);
            UpdatePosition(ref direction, elapsedTimeSec);
        }

        private void UpdateInput()
        {
            previousKeyboardState = currentKeyboardState;
            currentKeyboardState = Keyboard.GetState();

            previousMouseState = currentMouseState;
            currentMouseState = Mouse.GetState();

            Rectangle clientBounds = Game.Window.ClientBounds;

            int centerX = clientBounds.Width / 2;
            int centerY = clientBounds.Height / 2;
            int deltaX = centerX - currentMouseState.X;
            int deltaY = centerY - currentMouseState.Y;

            Mouse.SetPosition(centerX, centerY);

            smoothedMouseMovement.X = (float)deltaX;
            smoothedMouseMovement.Y = (float)deltaY;
        }

        /// <summary>
        /// Moves the camera based on player input.
        /// </summary>
        /// <param name="direction">Direction moved.</param>
        /// <param name="elapsedTimeSec">Elapsed game time.</param>
        private void UpdatePosition(ref Vector3 direction, float elapsedTimeSec)
        {
            if (currentVelocity.LengthSquared() != 0.0f)
            {
                // Only move the camera if the velocity vector is not of zero
                // length. Doing this guards against the camera slowly creeping
                // around due to floating point rounding errors.

                Vector3 displacement = (currentVelocity * elapsedTimeSec) +
                    (0.5f * acceleration * elapsedTimeSec * elapsedTimeSec);

                // Floating point rounding errors will slowly accumulate and
                // cause the camera to move along each axis. To prevent any
                // unintended movement the displacement vector is clamped to
                // zero for each direction that the camera isn't moving in.
                // Note that the UpdateVelocity() method will slowly decelerate
                // the camera's velocity back to a stationary state when the
                // camera is no longer moving along that direction. To account
                // for this the camera's current velocity is also checked.

                if (direction.X == 0.0f && (float)Math.Abs(currentVelocity.X) < 1e-6f)
                    displacement.X = 0.0f;

                if (direction.Y == 0.0f && (float)Math.Abs(currentVelocity.Y) < 1e-6f)
                    displacement.Y = 0.0f;

                if (direction.Z == 0.0f && (float)Math.Abs(currentVelocity.Z) < 1e-6f)
                    displacement.Z = 0.0f;

                Move(displacement.X, displacement.Y, displacement.Z);

                
            }

            // Continuously update the camera's velocity vector even if the
            // camera hasn't moved during this call. When the camera is no
            // longer being moved the camera is decelerating back to its
            // stationary state.

            UpdateVelocity(ref direction, elapsedTimeSec);
        }

        /// <summary>
        /// Updates the camera's velocity based on the supplied movement
        /// direction and the elapsed time (since this method was last
        /// called). The movement direction is the in the range [-1,1].
        /// </summary>
        /// <param name="direction">Direction moved.</param>
        /// <param name="elapsedTimeSec">Elapsed game time.</param>
        private void UpdateVelocity(ref Vector3 direction, float elapsedTimeSec)
        {
            if (direction.X != 0.0f)
            {
                // Camera is moving along the x axis.
                // Linearly accelerate up to the camera's max speed.

                currentVelocity.X += direction.X * acceleration.X * elapsedTimeSec;

                if (currentVelocity.X > velocity.X)
                    currentVelocity.X = velocity.X;
                else if (currentVelocity.X < -velocity.X)
                    currentVelocity.X = -velocity.X;
            }
            else
            {
                // Camera is no longer moving along the x axis.
                // Linearly decelerate back to stationary state.

                if (currentVelocity.X > 0.0f)
                {
                    if ((currentVelocity.X -= acceleration.X * elapsedTimeSec) < 0.0f)
                        currentVelocity.X = 0.0f;
                }
                else
                {
                    if ((currentVelocity.X += acceleration.X * elapsedTimeSec) > 0.0f)
                        currentVelocity.X = 0.0f;
                }
            }

            if (direction.Y != 0.0f)
            {
                currentVelocity.Y += direction.Y * acceleration.Y * elapsedTimeSec;

                if (currentVelocity.Y > velocity.Y)
                    currentVelocity.Y = velocity.Y;
                else if (currentVelocity.Y < -velocity.Y)
                    currentVelocity.Y = -velocity.Y;
            }
            else
            {
                // Camera is no longer moving along the y axis.
                // Linearly decelerate back to stationary state.

                if (currentVelocity.Y > 0.0f)
                {
                    if ((currentVelocity.Y -= acceleration.Y * elapsedTimeSec) < 0.0f)
                        currentVelocity.Y = 0.0f;
                }
                else
                {
                    if ((currentVelocity.Y += acceleration.Y * elapsedTimeSec) > 0.0f)
                        currentVelocity.Y = 0.0f;
                }
            }

            if (direction.Z != 0.0f)
            {
                // Camera is moving along the z axis.
                // Linearly accelerate up to the camera's max speed.

                currentVelocity.Z += direction.Z * acceleration.Z * elapsedTimeSec;

                if (currentVelocity.Z > velocity.Z)
                    currentVelocity.Z = velocity.Z;
                else if (currentVelocity.Z < -velocity.Z)
                    currentVelocity.Z = -velocity.Z;
            }
            else
            {
                // Camera is no longer moving along the z axis.
                // Linearly decelerate back to stationary state.

                if (currentVelocity.Z > 0.0f)
                {
                    if ((currentVelocity.Z -= acceleration.Z * elapsedTimeSec) < 0.0f)
                        currentVelocity.Z = 0.0f;
                }
                else
                {
                    if ((currentVelocity.Z += acceleration.Z * elapsedTimeSec) > 0.0f)
                        currentVelocity.Z = 0.0f;
                }
            }
        }

        private void UpdateViewMatrix()
        {
            Matrix.CreateFromQuaternion(ref orientation, out viewMatrix);

            xAxis.X = viewMatrix.M11;
            xAxis.Y = viewMatrix.M21;
            xAxis.Z = viewMatrix.M31;

            yAxis.X = viewMatrix.M12;
            yAxis.Y = viewMatrix.M22;
            yAxis.Z = viewMatrix.M32;

            zAxis.X = viewMatrix.M13;
            zAxis.Y = viewMatrix.M23;
            zAxis.Z = viewMatrix.M33;

            viewMatrix.M41 = -Vector3.Dot(xAxis, eye);
            viewMatrix.M42 = -Vector3.Dot(yAxis, eye);
            viewMatrix.M43 = -Vector3.Dot(zAxis, eye);

            viewDir.X = -zAxis.X;
            viewDir.Y = -zAxis.Y;
            viewDir.Z = -zAxis.Z;
        }

    #endregion

    #region Properties

        public Vector3 Acceleration
        {
            get { return acceleration; }
            set { acceleration = value; }
        }

        public Vector3 CurrentVelocity
        {
            get { return currentVelocity; }
        }

        public float EyeHeightStanding
        {
            get { return eyeHeight; }

            set
            {
                eyeHeight = value;
                eye.Y = eyeHeight;
                UpdateViewMatrix();
            }
        }

        public float HeadingDegrees
        {
            get { return -accumHeadingDegrees; }
        }

        public Quaternion Orientation
        {
            get { return orientation; }
        }

        public float PitchDegrees
        {
            get { return -accumPitchDegrees; }
        }

        public Vector3 Position
        {
            get { return eye; }

            set
            {
                eye = value;
                UpdateViewMatrix();
            }
        }
                
        public Matrix ProjectionMatrix
        {
            get { return projMatrix; }
        }

        public float RotationSpeed
        {
            get { return rotationSpeed; }
            set { rotationSpeed = value; }
        }

        public Vector3 Velocity
        {
            get { return velocity; }
            set { velocity = value; }
        }

        public Vector3 ViewDirection
        {
            get { return viewDir; }
        }

        public Matrix ViewMatrix
        {
            get { return viewMatrix; }
        }

        public Matrix ViewProjectionMatrix
        {
            get { return viewMatrix * projMatrix; }
        }

        //public Vector3 XAxis
        //{
        //    get { return xAxis; }
        //}

        //public Vector3 YAxis
        //{
        //    get { return yAxis; }
        //}

        //public Vector3 ZAxis
        //{
        //    get { return zAxis; }
        //}

    #endregion
    }
}