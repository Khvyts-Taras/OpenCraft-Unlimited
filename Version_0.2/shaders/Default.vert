#version 330 core

layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec3 aNormal;
layout (location = 2) in vec2 aTexCoord;
layout (location = 3) in float aAO;

out vec2 TexCoord;
out vec3 Normal;
out float vAO;
out vec3 WorldPos;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

void main()
{
    vec4 world = model * vec4(aPosition, 1.0);
    WorldPos = world.xyz;

    gl_Position = projection * view * world;

    TexCoord = aTexCoord;
    Normal = aNormal;
    vAO = aAO;
}
