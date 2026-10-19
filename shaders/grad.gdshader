shader_type canvas_item;
render_mode unshaded;

// canvas linear / radial (two point conical) gradients; positions come in device pixels
uniform vec4 inv_abcd;
uniform vec2 inv_ef;
uniform int kind;
uniform vec3 p0;
uniform vec3 p1;
uniform int n;
uniform float offs[8];
uniform vec4 cols[8];
varying vec2 dp;

void vertex() {
	dp = VERTEX;
}

float param(vec2 p, out bool ok) {
	ok = true;
	if (kind == 0) {
		vec2 d = p1.xy - p0.xy;
		float l2 = dot(d, d);
		if (l2 <= 0.0) { ok = false; return 0.0; }
		return dot(p - p0.xy, d) / l2;
	}
	vec2 cd = p1.xy - p0.xy;
	float dr = p1.z - p0.z;
	vec2 pd = p - p0.xy;
	float a = dot(cd, cd) - dr * dr;
	float b = dot(pd, cd) + p0.z * dr;
	float c = dot(pd, pd) - p0.z * p0.z;
	if (abs(a) < 1e-6) {
		if (abs(b) < 1e-9) { ok = false; return 0.0; }
		float t0 = c / (2.0 * b);
		ok = p0.z + t0 * dr >= 0.0;
		return t0;
	}
	float disc = b * b - a * c;
	if (disc < 0.0) { ok = false; return 0.0; }
	float s = sqrt(disc);
	float t1 = (b + s) / a, t2 = (b - s) / a;
	float tt = max(t1, t2);
	if (p0.z + tt * dr >= 0.0) return tt;
	tt = min(t1, t2);
	ok = p0.z + tt * dr >= 0.0;
	return tt;
}

void fragment() {
	vec2 p = vec2(inv_abcd.x * dp.x + inv_abcd.z * dp.y + inv_ef.x, inv_abcd.y * dp.x + inv_abcd.w * dp.y + inv_ef.y);
	bool ok;
	float t = param(p, ok);
	vec4 c = vec4(0.0);
	if (ok) {
		c = cols[0];
		if (t > offs[0]) {
			for (int i = 1; i < 8; i++) {
				if (i >= n) break;
				if (t <= offs[i]) {
					float span = offs[i] - offs[i - 1];
					float k = span <= 0.0 ? 1.0 : (t - offs[i - 1]) / span;
					c = mix(cols[i - 1], cols[i], k);
					break;
				}
				c = cols[i];
			}
		}
	}
	COLOR = vec4(c.a > 0.0 ? c.rgb / c.a : vec3(0.0), c.a * COLOR.a);
}
