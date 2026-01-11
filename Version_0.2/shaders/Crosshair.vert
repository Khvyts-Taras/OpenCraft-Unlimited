#version 330 core
layout (location = 0) in vec2 aPos;
uniform float uAspect;

void main()
{
    vec2 p = aPos;
    p.x /= uAspect;
    gl_Position = vec4(p, 0.0, 1.0);
}
