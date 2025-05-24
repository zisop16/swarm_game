// NOTE: Shader automatically converted from Godot Engine 4.4.1.stable's StandardMaterial3D.

shader_type spatial;
render_mode blend_mix, depth_draw_opaque, cull_disabled, depth_prepass_alpha;

uniform vec3 tint : source_color = vec3(1., 1., 1.);
uniform sampler2D texture_albedo : source_color, filter_linear_mipmap, repeat_disable;
uniform float scale_decrease : hint_range(1, 10) = 1.;
uniform sampler2D relative_wind_strength : filter_linear, repeat_disable;
uniform sampler2D wind_noise;
uniform float wind_strength : hint_range(0, 10, .01) = 2.;
uniform float wind_frequency : hint_range(0, 10, .01) = 2.;
uniform float wind_turbulence : hint_range(0, .1, .001) = .1;
uniform float positional_wind : hint_range(0, 1, .001) = .1;

float hash( vec2 f )
{   uvec2
          x = floatBitsToUint(f),
          q = 1103515245U * ( x>>1U ^ x.yx    );
    return float( 1103515245U * (q.x ^ q.y>>3U) ) / float(0xffffffffU);
}

vec2 wind_direction_vector(vec2 world_pos) {
	float wind_angle = 2. * PI * texture(wind_noise, vec2(TIME * wind_turbulence) + world_pos * positional_wind).r;
	return vec2(cos(wind_angle), sin(wind_angle));
}

vec2 xz_offset(vec3 vert, vec2 uv, vec2 world_pos) {
	float y = 1. - uv.y;
	float current_wind_strength = wind_strength * sin(TIME * wind_frequency);
	float relative_strength = texture(relative_wind_strength, vec2(y, y)).r;
	vec2 wind = wind_direction_vector(world_pos) * vec2(current_wind_strength * relative_strength);
	return wind * y;
}

void vertex() {
	vec2 world_pos = NODE_POSITION_WORLD.xz;
	vec2 offset = xz_offset(VERTEX, UV, world_pos);
    // VERTEX.xz = normalize(INV_VIEW_MATRIX[0].xz) * VERTEX.x + normalize(INV_VIEW_MATRIX[2].xz) * VERTEX.z;
	VERTEX.xz += offset;
	VERTEX.xyz /= scale_decrease;
}

void fragment() {
	vec2 base_uv = UV;

	vec4 albedo_tex = texture(texture_albedo, base_uv);
	ALBEDO = albedo_tex.rgb * tint;

	ALPHA = albedo_tex.a;
}
