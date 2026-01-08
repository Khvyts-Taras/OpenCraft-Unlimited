#version 330 core

in vec2 TexCoord;
in vec3 Normal;

out vec4 FragColor;

uniform sampler2D texture0;

void main()
{
	vec3 shadowColor = vec3(0.53, 0.81, 0.92);
	float ambientBias = 4.5;
	vec3 ambientLight = normalize(vec3(ambientBias) + shadowColor);

	vec3 lightPos = normalize(vec3(0.3, 1.0, 0.5));

	float light = max(dot(Normal, lightPos), 0.0);
	vec3 color = texture(texture0, TexCoord).rgb;

	

	FragColor = vec4(color * (light * 0.3 + ambientLight * 1.2), 1.0);

	//FragColor = vec4(Normal, 1);
	//float depth = pow(gl_FragCoord.z, 400);

}