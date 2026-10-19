shader_type canvas_item;
render_mode blend_disabled, unshaded;

// one direction of a Gaussian blur over the shadow atlas; sigma (atlas pixels) comes in COLOR.r * 4
uniform sampler2D src : filter_nearest;
uniform vec2 dir;
varying float st;

float erf_(float x) {
	// Abramowitz-Stegun 7.1.26
	float s = sign(x); x = abs(x);
	float t = 1.0 / (1.0 + 0.3275911 * x);
	float y = 1.0 - (((((1.061405429 * t - 1.453152027) * t) + 1.421413741) * t - 0.284496736) * t + 0.254829592) * t * exp(-x * x);
	return s * y;
}

void vertex() {
	st = COLOR.r * 4.0;
}

void fragment() {
	ivec2 p = ivec2(FRAGCOORD.xy);
	ivec2 d = ivec2(dir);
	float acc = 0.0;
	float ws = 0.0;
	if (st < 0.05) {
		acc = texelFetch(src, p, 0).a;
		ws = 1.0;
	} else {
		int r = int(ceil(st * 3.0));
		float k2 = 1.0 / (st * 1.41421356);
		for (int k = -13; k <= 13; k++) {
			if (abs(k) > r) continue;
			// the Gaussian integrated over the whole pixel
			float w = 0.5 * (erf_((float(k) + 0.5) * k2) - erf_((float(k) - 0.5) * k2));
			acc += w * texelFetch(src, p + d * k, 0).a;
			ws += w;
		}
	}
	COLOR = vec4(1.0, 1.0, 1.0, acc / ws);
}
