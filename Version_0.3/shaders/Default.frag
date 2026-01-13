#version 330 core

in vec2 TexCoord;
in vec3 Normal;
in float vAO;
in vec3 WorldPos;
in vec3 LocalPos;

out vec4 FragColor;

uniform sampler2D texture0;
uniform float timeOfDay;

const float PI = 3.14159265359;
float angle = timeOfDay * 2.0 * PI;

vec3 lightDir = normalize(vec3(
    cos(angle),
    sin(angle),
    0.5
));

void main()
{
    vec4 tex = texture(texture0, TexCoord);
    vec3 color = tex.rgb;
    float alpha = tex.a;

    if (alpha < 0.5)
        discard;

    float dayFactor = clamp(lightDir.y * 0.5 + 0.5, 0.0, 1.0);

    vec3 shadowColor = vec3(0.53, 0.81, 0.92);
    float ambientBias = 4.5;
    vec3 ambientLight = normalize(vec3(ambientBias) + shadowColor) * pow(vAO, 0.8);

    float light = max(dot(normalize(Normal), lightDir), 0.0);


    vec3 litColor = color * (light*0.1 + ambientLight * mix(0.3, 1.5, dayFactor));

    float d = length(LocalPos);

    

    FragColor = vec4(litColor, 1.0);
}
