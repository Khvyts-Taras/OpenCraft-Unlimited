namespace OpenCraft
{
	public static class WorldGenerator
	{
        private static FastNoiseLite noise = new FastNoiseLite();
        private static Random random = new Random();

        public const int AIR_ID = 0;
        public const int DEFAULT_GROUND_ID = 1;

        static int HeightScale;
        static int Border;
        public static int Seed = 1;

        static float mountainsPower;
        static float valleysPower;
        static float valleyScale;

        private static (int blockId, int min, int max)[] layers = null!;

		public static void Init(int seed, float frequency, int heightScale, int border,
		    float mPower, float vPower, float vScale, (int blockId, int min, int max)[] genLayers)
		{
            Seed = seed;
	        noise.SetSeed(seed);
	        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
	        noise.SetFrequency(frequency);

	        HeightScale = heightScale;
	        Border = border;

	        mountainsPower = mPower;
			valleysPower = vPower;
			valleyScale = vScale;

			layers = genLayers;
        }

        public static int GetBlockId(int blockX, int blockY, int blockZ, int surfaceHeight)
        {
            if (blockY > surfaceHeight)
                return AIR_ID;

            int randY = blockY + random.Next(-Border, Border+1);
            foreach (var layer in layers)
            {
                if (randY >= layer.min && randY <= layer.max)
                    return layer.blockId;
            }

            return DEFAULT_GROUND_ID;
        }

        public static byte GenerateBlock(int blockX, int blockY, int blockZ)
        {
            float n = noise.GetNoise(blockX, blockZ);
            float h;

            if (n >= 0f)
                h = MathF.Pow(n, mountainsPower);
            else
                h = -MathF.Pow(-n, valleysPower) * valleyScale;

            float surfaceHeight = h * HeightScale;
            return (byte)GetBlockId(blockX, blockY, blockZ, (int)surfaceHeight);
        }

        public static int GetH(int blockX, int blockZ)
        {
            float n = noise.GetNoise(blockX, blockZ);
            float h;

            if (n >= 0f)
                h = MathF.Pow(n, mountainsPower);
            else
                h = -MathF.Pow(-n, valleysPower) * valleyScale;

            return (int)(h * HeightScale);
        }
	}
}