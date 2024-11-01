#ifdef GL_ES
precision highp float;
#else
#define highp 
#define mediump 
#define lowp 
#endif

attribute vec4 a_pos;

#ifdef SUPPORTED_GL_OES_30
attribute mat4 a_model;
#else
uniform mat4 u_model;
#define a_model u_model
#endif

uniform mat4 u_projectionView;

uniform vec3 u_corner0;
uniform vec3 u_corner1;
uniform vec3 u_corner2;
uniform vec3 u_corner3;

void main(void)
{
	vec3 pos = u_corner0 * a_pos.x + u_corner1 * a_pos.y + u_corner2 * a_pos.z + u_corner3 * a_pos.w;

	gl_Position = u_projectionView * vec4(pos, 1.0);
}
