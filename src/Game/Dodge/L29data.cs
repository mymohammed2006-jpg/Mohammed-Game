namespace MG;
// Trouble (main level 29): the song's hits in beats (beat 0 = 0:06.371 of the song), made by tools/level29/hits.py from songs/SONG_L29.mp3
// (onsets of the low beatbox (kick), the high noise (snare) and the voice, each against its own loudest in 16 beats around it)
public static partial class G
{
	static readonly double[] TB_S1 = { 36, 37, 37.5, 45, 45.5, 46, 52, 52.5, 53, 54, 57, 58, 61, 62, 65.5, 66, 69, 69.5, 72, 73, 73.5, 77, 77.5, 78 };
	static readonly double[] TB_KB = { 104, 107.75, 110.25, 113.5, 115.5, 118.75, 122.75, 124.25, 126.25, 128, 129.25, 131.5, 133.5 };
	static readonly double[] TB_SX = { 233.5, 235, 236, 237.5, 239, 240, 243, 245, 245.5, 247, 248, 252, 255, 256, 257.5, 259, 260, 261 };
	// the hype 152-184 (1:05.6-1:18.1): kicks, snares and the shouts' peaks
	static readonly double[] TB_HK = { 152, 154.25, 155, 157.5, 159, 160, 161.5, 163, 168, 171.5, 172, 175.5, 179.5, 180.5, 181.5 };
	static readonly double[] TB_HS = { 152, 155, 156, 157.5, 158.5, 159, 162, 163.5, 165.5, 166.5, 168, 173.5, 174, 178, 180.75, 181.5, 182 };
	static readonly double[] TB_HV = { 166, 168, 172, 175, 178, 179, 180.75 };
	static readonly double[] TB_K3 = { 292, 293.5, 296, 299.5, 303, 303.5, 307.5, 308.75, 309.5, 312, 315, 315.5, 319, 323.5, 324, 324.75, 325.5 };
}
