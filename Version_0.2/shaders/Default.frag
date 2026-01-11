#version 330 core

in vec2 TexCoord;
in vec3 Normal;
in float vAO;
in vec3 WorldPos;
in vec3 LocalPos;

out vec4 FragColor;

uniform sampler2D texture0;


void main()
{
    vec3 shadowColor = vec3(0.53, 0.81, 0.92);
    float ambientBias = 4.5;
    vec3 ambientLight = normalize(vec3(ambientBias) + shadowColor) * pow(vAO, 0.7);

    vec3 lightDir = normalize(vec3(0.3, 1.0, 0.5));

    float light = max(dot(normalize(Normal), lightDir), 0.0);
    vec3 color = texture(texture0, TexCoord).rgb;

    vec3 litColor = color * (light * 0.3 + ambientLight * 1.2);

    float d = length(LocalPos);

    FragColor = vec4(litColor, 1.0);
}
