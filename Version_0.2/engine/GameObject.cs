using OpenTK.Mathematics;

namespace OpenCraft
{
    class GameObject
    {
        public Vector3 Position;
        public Vector3 Size;
        public Vector3 Velocity;

        public Dictionary<(int, int, int), Chunk> Chunks = new();

        public GameObject(Vector3 startPosition, Vector3 objectSize)
        {
            Position = startPosition;
            Size = objectSize;
            Velocity = Vector3.Zero;
        }

        public void SetWorld(World world)
        {
        	Chunks = world.Chunks;
        }

		public void Update(float dt)
		{
		    Velocity += new Vector3(0f, -35f, 0f)*dt;
		    Velocity *= new Vector3(MathF.Pow(0.9f, dt*100), MathF.Pow(0.99f, dt*100), MathF.Pow(0.9f, dt*100));

		    Position.X += Velocity.X * dt;
		    ResolveAxisCollisions(axis: 0);

		    Position.Y += Velocity.Y * dt;
		    ResolveAxisCollisions(axis: 1);

		    Position.Z += Velocity.Z * dt;
		    ResolveAxisCollisions(axis: 2);
		}


		private void ResolveAxisCollisions(int axis)
		{
		    List<Vector3i> hits = CheckCollision();
		    if (hits.Count == 0)
		        return;

		    float v = axis == 0 ? Velocity.X : axis == 1 ? Velocity.Y : Velocity.Z;
		    if (v == 0f)
		        return;

		    int chosenCoord = v > 0f ? int.MaxValue : int.MinValue;

		    foreach (var b in hits)
		    {
		        int c = axis == 0 ? b.X : axis == 1 ? b.Y : b.Z;
		        if (v > 0f) {if (c < chosenCoord) chosenCoord = c;}
		        else {if (c > chosenCoord) chosenCoord = c;}
		    }

		    if (axis == 0)
		    {
		        if (v > 0f)
		            Position.X = chosenCoord - Size.X;
		        else
		            Position.X = chosenCoord + 1f;
		       

		        Velocity.X = 0f;
		    }
		    else if (axis == 1)
		    {
		        if (v > 0f)
		            Position.Y = chosenCoord - Size.Y;
		        else
		            Position.Y = chosenCoord + 1f;

		        Velocity.Y = 0f;
		    }
		    else
		    {
		        if (v > 0f)
		            Position.Z = chosenCoord - Size.Z;
		        else
		            Position.Z = chosenCoord + 1f;

		        Velocity.Z = 0f;
		    }
		}


		public List<Vector3i> CheckCollision()
		{
		    return CheckCollision(this.Position, this.Size);
		}

		public List<Vector3i> CheckCollision(Vector3 position, Vector3 size)
		{
		    int S = Chunk.Size;
		    List<Vector3i> touchedSolidBlocks = new List<Vector3i>();

		    foreach (Vector3i pos in GetBlocksPos(position, size))
		    {
				int x = pos.X;
				int y = pos.Y;
				int z = pos.Z;

		        int chunkX = (int)MathF.Floor(x / (float)S);
		        int chunkY = (int)MathF.Floor(y / (float)S);
		        int chunkZ = (int)MathF.Floor(z / (float)S);

		        int localX = x - chunkX * S;
		        int localY = y - chunkY * S;
		        int localZ = z - chunkZ * S;

		        if (!Chunks.TryGetValue((chunkX, chunkY, chunkZ), out Chunk? chunk))
		            continue;

		        byte block = chunk.Blocks[localX, localY, localZ];
		        if (block == 0)
		            continue;

		        touchedSolidBlocks.Add(new Vector3i(x, y, z));
		    }
		    return touchedSolidBlocks;
		}

		public List<Vector3i> GetBlocksPos()
		{
		    return GetBlocksPos(this.Position, this.Size);
		}

		float Snap(float v, int digits = 5)
		{
		    return MathF.Round(v, digits, MidpointRounding.AwayFromZero);
		}

		public List<Vector3i> GetBlocksPos(Vector3 position, Vector3 size)
		{
		    int minX = (int)MathF.Floor(Snap(position.X));
		    int minY = (int)MathF.Floor(Snap(position.Y));
		    int minZ = (int)MathF.Floor(Snap(position.Z));

		    int maxX = (int)MathF.Ceiling(Snap(position.X + size.X));
		    int maxY = (int)MathF.Ceiling(Snap(position.Y + size.Y));
		    int maxZ = (int)MathF.Ceiling(Snap(position.Z + size.Z));

		    List<Vector3i> touchedBlocksPos = new List<Vector3i>();

		    for (int x = minX; x < maxX; x++)
		    for (int y = minY; y < maxY; y++)
		    for (int z = minZ; z < maxZ; z++)
		    {
		    	touchedBlocksPos.Add(new Vector3i(x, y, z));
		    }

		    return touchedBlocksPos;
		}
    }
}
