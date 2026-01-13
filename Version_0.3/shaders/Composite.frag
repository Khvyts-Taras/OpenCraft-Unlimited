#version 330 core
in vec2 vUv;
out vec4 FragColor;

uniform sampler2D uBg;
uniform sampler2D uWorld;

void main()
{
    vec4 bg = texture(uBg, vUv);
    vec4 w  = texture(uWorld, vUv);

    vec3 rgb = mix(bg.rgb, w.rgb, w.a);

    FragColor = vec4(rgb, 1.0);
}
