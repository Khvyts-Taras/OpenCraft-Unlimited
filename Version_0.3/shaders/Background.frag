#version 330 core
in vec2 vUv;
out vec4 FragColor;

uniform vec3 uTopColor;
uniform vec3 uBottomColor;
uniform float uPitch; // -1..+1

void main()
{
    float y = vUv.y;

    float shift = uPitch;
    y += shift;

    float scale = 1.5 + abs(uPitch) * 0.35;
    y = (y - 0.5) / scale + 0.5;

    float t = clamp(y, 0.0, 1.0);

    vec3 col = mix(uBottomColor, uTopColor, t);
    FragColor = vec4(col, 1.0);
}
