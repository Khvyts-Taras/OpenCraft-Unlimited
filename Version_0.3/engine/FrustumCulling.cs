using System;
using OpenTK.Mathematics;

namespace OpenCraft
{
    public static class Frustum
    {
        public static bool ChunkInFrustum(Camera camera, Vector3 chunkPos)
        {
            int s = Chunk.Size;
            float r = MathF.Sqrt(3f) * (s * 0.5f);

            Vector3 chunkCenter = new Vector3(
                (chunkPos.X + 0.5f) * s,
                (chunkPos.Y + 0.5f) * s,
                (chunkPos.Z + 0.5f) * s
            );

            Vector3 sphereVec = chunkCenter - camera.Position;

            // Важливо: front/right/up мають бути нормалізовані й ортогональні
            Vector3 front = camera.front.Normalized();
            Vector3 right = camera.right.Normalized();
            Vector3 up    = camera.up.Normalized();

            float sz = Vector3.Dot(sphereVec, front);

            float maxRenderDist = camera.Far + r;
            if (sz < camera.Near - r || sz > maxRenderDist) return false;

            float vFOV = MathHelper.DegreesToRadians(camera.FOV);
            float aspect = camera.screenWidth / (float)camera.screenHeight;

            // Горизонтальний FOV з вертикального:
            float hFOV = 2f * MathF.Atan(MathF.Tan(vFOV * 0.5f) * aspect);

            float half_x = hFOV * 0.5f;
            float half_y = vFOV * 0.5f;

            float tan_x = MathF.Tan(half_x);
            float tan_y = MathF.Tan(half_y);

            // Для сфери: “роздуваємо” межі фрустума на r / cos(halfFov)
            float factorX = 1.0f / MathF.Cos(half_x);
            float factorY = 1.0f / MathF.Cos(half_y);

            // Проєкції на осі right/up
            float sx = Vector3.Dot(sphereVec, right);
            float sy = Vector3.Dot(sphereVec, up);

            // Межі по X/Y на відстані sz, плюс запас під радіус сфери
            float limitX = sz * tan_x + r * factorX;
            float limitY = sz * tan_y + r * factorY;

            if (sx >  limitX) return false; // праворуч за фрустумом
            if (sx < -limitX) return false; // ліворуч за фрустумом
            if (sy >  limitY) return false; // вище
            if (sy < -limitY) return false; // нижче

            return true;
        }
    }
}
