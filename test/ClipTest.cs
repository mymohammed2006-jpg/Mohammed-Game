using System;
using MG.Canvas;

namespace MG;

// nested clips (and the bold fallback glyph), compared with Chrome (scratch cliptest/clip.js)
public static class ClipTest
{
	public static void Run(Ctx g)
	{
		g.fillStyle="#07040d";g.fillRect(0,0,1280,720);
		g.save();g.beginPath();g.rect(0,0,1280,720);g.clip();
		// A: arc clip inside the frame clip (L3 eye)
		g.save();g.beginPath();g.arc(150,150,100,0,Math.PI*2);g.clip();g.fillStyle="#ff2a6d";g.beginPath();g.arc(130,150,36,0,Math.PI*2);g.fill();g.fillStyle="rgba(0,240,255,0.6)";g.fillRect(40,40,300,60);g.restore();
		// B: UI repro
		g.save();g.beginPath();g.rect(300,60,300,200);g.clip();g.fillStyle="rgba(255,0,0,0.5)";g.fillRect(250,10,500,500);g.fillStyle="#0f0";g.fillRect(350,110,50,50);g.restore();
		// C: evenodd fence (L7)
		g.save();g.beginPath();g.rect(650,40,300,240);g.arc(800,160,70,0,Math.PI*2);g.clip("evenodd");g.fillStyle="rgba(255,42,109,0.7)";g.fillRect(600,20,400,300);g.restore();
		// D: two clipped halves in a row, then drawing after (L8)
		g.save();g.beginPath();g.rect(0,320,640,180);g.clip();g.strokeStyle="#ffd23f";g.lineWidth=6;g.beginPath();g.moveTo(400,330);g.lineTo(900,490);g.stroke();g.restore();
		g.save();g.beginPath();g.rect(640,320,640,180);g.clip();g.strokeStyle="#00f0ff";g.lineWidth=6;g.beginPath();g.moveTo(400,490);g.lineTo(900,330);g.stroke();g.restore();
		g.fillStyle="#fff";g.fillRect(1000,340,60,60);
		// E: clip inside a non-rect clip (intersection)
		g.save();g.beginPath();g.arc(200,600,90,0,Math.PI*2);g.clip();g.save();g.beginPath();g.rect(150,480,200,100);g.clip();g.fillStyle="#ff2a6d";g.fillRect(0,400,400,320);g.restore();g.fillStyle="rgba(0,240,255,0.5)";g.fillRect(100,620,300,40);g.restore();
		// F: order: draw inside inner clip, then outer drawing on top after restore
		g.save();g.beginPath();g.arc(500,600,90,0,Math.PI*2);g.clip();g.fillStyle="#333";g.fillRect(380,480,240,240);g.save();g.beginPath();g.rect(420,540,160,120);g.clip();g.fillStyle="#ff2a6d";g.fillRect(380,480,240,240);g.restore();g.fillStyle="#0f0";g.fillRect(470,570,60,60);g.restore();
		// G: glow inside a nested clip
		g.save();g.beginPath();g.rect(700,520,300,160);g.clip();g.shadowColor="#ff2a6d";g.shadowBlur=20;g.fillStyle="#ff2a6d";g.fillRect(680,580,80,40);g.fillRect(900,560,60,60);g.restore();
		g.restore();
		g.fillStyle="#fff";g.fillRect(1100,600,40,40);
		g.fillStyle="#00f0ff";g.font="800 30px Tajawal";g.textAlign="center";g.textBaseline="middle";g.direction="rtl";g.fillText("✓ تم الحفظ",1150,250);
		g.font="500 30px Tajawal";g.fillText("✓ تم الحفظ",1150,300);
		g.fillStyle="rgba(255,210,63,0.8)";g.beginPath();g.rect(1000,420,200,140);g.arc(1100,490,50,0,Math.PI*2);g.fill("evenodd");
	}
}
