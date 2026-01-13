using StbImageSharp;

using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

using OpenTK.Windowing.Common.Input;

namespace OpenCraft
{
    internal class Game: GameWindow
    {
        private string version = "v0.3";

        int Width, Height;
        private bool mouseCaptured = false;

        static World GameWorld = null!;
        static Camera MainCamera = null!;
        static GameObject Player = null!;
        private VoxelRaycaster _raycaster = null!;

        private WorldRenderTarget _worldRt = null!;

        byte selectedBlock = 6;

        private double fpsTime;
        private int fpsFrames;
        private int fps;
        private int fpsLimit;

        float globalTime = 0f;
        float timeOfDay = 0f;
        float dayDuration = 60f * 4f;

        string worldFolder = "saves/world";

    public Game(int width, int height, int fps) : base(GameWindowSettings.Default, new NativeWindowSettings {NumberOfSamples = 4})
        {
            Width = width;
            Height = height;
            fpsLimit = fps;

            using var s = File.OpenRead("textures/cobblestone.png");
            ImageResult img = ImageResult.FromStream(s, ColorComponents.RedGreenBlueAlpha);

            this.Icon = new WindowIcon(new OpenTK.Windowing.Common.Input.Image(img.Width, img.Height, img.Data));
            Title = $"OpenCraft {version}";

            CenterWindow(new Vector2i(width, height));
        }


        protected override void OnLoad()
        {
            base.OnLoad();

            UpdateFrequency = fpsLimit;

            Player = new GameObject(new Vector3(0, 8, 0), new Vector3(0.8f, 1.8f, 0.8f));
            MainCamera = new Camera(new Vector3(16, 30, 60), new Vector2(-20f, -90f), Width, Height, 90f);
            

            GameWorld = new World(new Shader("Default.vert", "Default.frag"), -1);

            _raycaster = new VoxelRaycaster(GameWorld.Chunks);
            _worldRt = new WorldRenderTarget(Width, Height);
            Compositor.Load();

            Player.SetWorld(GameWorld);
        
            Background.Load(Width, Height);

            Crosshair.LoadCrosshair();

            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(TriangleFace.Back);
            GL.Enable(EnableCap.Multisample);

            chunkTask = ChunkGenerator.GenerateChunksAsync(MainCamera, GameWorld);

        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);

            fpsTime += args.Time;
            fpsFrames++;

            if (fpsTime >= 1.0)
            {
                fps = fpsFrames;
                fpsFrames = 0;
                fpsTime -= 1.0;

                Title = $"OpenCraft {version} | FPS: {fps}";
            }

            GL.ClearColor(0.53f, 0.81f, 0.92f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            Background.Draw(MainCamera.pitch, (int)MainCamera.screenWidth, (int)MainCamera.screenHeight);

            _worldRt.Begin();
            GameWorld.Draw(MainCamera, timeOfDay);
            _worldRt.End();

            Compositor.DrawToScreen(
                fullscreenVao: Background._vao,
                bgTexId: Background.TextureId,
                worldTexId: _worldRt.TextureId,
                screenWidth: Size.X,
                screenHeight: Size.Y
            );

            Crosshair.DrawCrosshair(MainCamera.screenWidth, MainCamera.screenHeight);

            SwapBuffers();
        }

        static void PlayerControllerUpdate(KeyboardState input, float dt)
        {
            Vector3 forwardFlat = new Vector3(MainCamera.front.X, 0f, MainCamera.front.Z);

            float speed_x = 1.0f;
            float playerSpeed = 50f;
            float jumpPower = 15f;

            if (input.IsKeyDown(Keys.LeftControl))
                speed_x = 3.0f;

            if (input.IsKeyDown(Keys.LeftShift))
                speed_x = 0.3f;

            if (forwardFlat.LengthSquared > 0f)
                forwardFlat = Vector3.Normalize(forwardFlat);

            if (input.IsKeyDown(Keys.W))
                Player.Velocity += playerSpeed * speed_x * forwardFlat * dt;

            if (input.IsKeyDown(Keys.S))
                Player.Velocity += -playerSpeed * speed_x * forwardFlat * dt;

            if (input.IsKeyDown(Keys.A))
                Player.Velocity += -playerSpeed * speed_x * MainCamera.right * dt;

            if (input.IsKeyDown(Keys.D))
                Player.Velocity += playerSpeed * speed_x * MainCamera.right * dt;

            if (input.IsKeyDown(Keys.Space))
            {
                Vector3 probePos = new Vector3(Player.Position.X, Player.Position.Y-0.1f, Player.Position.Z);
                Vector3 probeSize = new Vector3(Player.Size.X, 0.1f, Player.Size.Z);

                if (Player.CheckCollision(probePos, probeSize).Count > 0 && Player.Velocity.Y == 0f)
                {
                    Player.Velocity.Y += jumpPower;
                    if (input.IsKeyDown(Keys.W) && input.IsKeyDown(Keys.LeftShift))
                    {
                        Player.Velocity += 30f * forwardFlat;
                    }
                }
            }
        }

        Task<Chunk[]> chunkTask = null!;

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);

            MouseState mouse = MouseState;
            KeyboardState input = KeyboardState;

            float dt = Math.Min((float)args.Time, 0.1f);
            globalTime += dt;
            timeOfDay = (globalTime % dayDuration) / dayDuration;
            //Console.WriteLine(dt);

            if (chunkTask != null && chunkTask.IsCompleted)
            {
                Chunk[] newChunks = chunkTask.Result;
                ChunkGenerator.ApplyMeshInMainTHread(newChunks, GameWorld);
                chunkTask = ChunkGenerator.GenerateChunksAsync(MainCamera, GameWorld);
            }

            if (mouseCaptured)
                PlayerControllerUpdate(input, dt);

            Player.Update(dt);

            if (input.IsKeyDown(Keys.LeftShift))
                MainCamera.Position = Player.Position + new Vector3(0.4f, 1.2f, 0.4f);
            else
                MainCamera.Position = Player.Position + new Vector3(0.4f, 1.5f, 0.4f);


            var origin = MainCamera.Position;
            var dir = MainCamera.front.Normalized();
            float reach = 8f;

            if (_raycaster.RaycastBlock(origin, dir, reach, out var hit, out var normal))
            {
                if (MouseState.IsButtonPressed(MouseButton.Left))
                    if (mouseCaptured) GameWorld.SetBlockWorld(hit, 0);

                if (MouseState.IsButtonPressed(MouseButton.Right))
                {
                    var placePos = hit + normal;
                    var touched = Player.GetBlocksPos();

                    if (!touched.Contains(placePos))
                        if (mouseCaptured) GameWorld.SetBlockWorld(placePos, selectedBlock);
                }
                if (input.IsKeyPressed(Keys.Q)) {Console.WriteLine($"Block position: {hit + normal}");}
            }


            if (input.IsKeyPressed(Keys.D1)) selectedBlock = 1;
            if (input.IsKeyPressed(Keys.D2)) selectedBlock = 2;
            if (input.IsKeyPressed(Keys.D3)) selectedBlock = 3;
            if (input.IsKeyPressed(Keys.D4)) selectedBlock = 4;
            if (input.IsKeyPressed(Keys.D5)) selectedBlock = 5;
            if (input.IsKeyPressed(Keys.D6)) selectedBlock = 6;
            if (input.IsKeyPressed(Keys.D7)) selectedBlock = 7;
            if (input.IsKeyPressed(Keys.D8)) selectedBlock = 8;
            if (input.IsKeyPressed(Keys.D9)) selectedBlock = 9;

            //Save
            if (input.IsKeyPressed(Keys.F))
            {
                WorldStorage.SaveSeed($"{worldFolder}/seed.txt", WorldGenerator.Seed);
                WorldStorage.SaveChanges($"{worldFolder}/chunks.bin", GameWorld.Changes);

                Console.WriteLine("Seed saved");
            }

            //Load
            if (input.IsKeyPressed(Keys.L))
            {
                GameWorld = new World(new Shader("Default.vert", "Default.frag"), WorldStorage.LoadSeed($"{worldFolder}/seed.txt"));
                _raycaster = new VoxelRaycaster(GameWorld.Chunks);
                Player.SetWorld(GameWorld);
                Player.Position = new Vector3(0, 8, 0);

                GameWorld.Changes = WorldStorage.LoadChanges($"{worldFolder}/chunks.bin");
                Console.WriteLine($"Loaded chunks: {GameWorld.Changes.Count}");
                WorldStorage.ApplyChangesToWorld(GameWorld, GameWorld.Changes);
            }

            //Reegenerate
            if (input.IsKeyPressed(Keys.R))
            {
                GameWorld = new World(new Shader("Default.vert", "Default.frag"), -1);
                _raycaster = new VoxelRaycaster(GameWorld.Chunks);
                Player.SetWorld(GameWorld);
                Player.Position = new Vector3(0, 8, 0);
            }



            if (KeyboardState.IsKeyPressed(Keys.Escape))
                ReleaseMouse();
            if (MouseState.IsButtonPressed(MouseButton.Left))
                CaptureMouse();

            if (mouseCaptured)
                MainCamera.Update(input, mouse, dt);
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            Width = e.Width;
            Height = e.Height;

            MainCamera?.Resize(Width, Height);
            _worldRt?.Resize(Width, Height);

            GL.Viewport(0, 0, e.Width, e.Height);
        }

        protected override void OnUnload()
        {
            base.OnUnload();
            GameWorld.Dispose();
        }

        private void CaptureMouse()
        {
            mouseCaptured = true;
            CursorState = CursorState.Grabbed;
            MainCamera.ResetMouse();
        }

        private void ReleaseMouse()
        {
            mouseCaptured = false;
            CursorState = CursorState.Normal;
        }
    }
}