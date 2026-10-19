using System;
using MG.Canvas;
public static class CanvasTest
{
	public static void Run(Ctx g)
	{
		g.fillStyle="#07040d";g.fillRect(0,0,1280,720);
		// 1 glowing pink squares like hazards
		g.save();g.shadowColor="#ff2a6d";g.shadowBlur=20;g.fillStyle="#ff2a6d";g.fillRect(40,40,60,60);g.shadowBlur=8;g.fillRect(130,50,30,30);g.shadowBlur=40;g.fillRect(190,30,80,20);g.restore();
		// 2 player square: rotated, squashed, white with cyan glow + stroke
		g.save();g.translate(340,80);g.rotate(0.35);g.scale(1.3,0.75);g.shadowColor="#00f0ff";g.shadowBlur=18;g.fillStyle="#ffffff";g.fillRect(-14,-14,28,28);g.shadowBlur=0;g.strokeStyle="#00f0ff";g.lineWidth=3;g.strokeRect(-14,-14,28,28);g.restore();
		// 3 circles and rings
		g.save();g.fillStyle="#ff2a6d";g.beginPath();g.arc(450,70,30,0,Math.PI*2);g.fill();g.strokeStyle="rgba(255,42,109,0.6)";g.lineWidth=4;g.beginPath();g.arc(530,70,30,0,Math.PI*2);g.stroke();g.lineWidth=1;g.strokeStyle="#fff";g.beginPath();g.arc(600,70,20,0.3,4.5);g.stroke();g.restore();
		// 4 joins and caps
		g.save();g.strokeStyle="#ffd23f";g.lineWidth=10;g.lineJoin="miter";g.beginPath();g.moveTo(660,110);g.lineTo(690,40);g.lineTo(720,110);g.stroke();g.lineJoin="round";g.lineCap="round";g.beginPath();g.moveTo(740,110);g.lineTo(770,40);g.lineTo(800,110);g.stroke();g.lineJoin="bevel";g.lineCap="square";g.beginPath();g.moveTo(820,110);g.lineTo(850,40);g.lineTo(880,110);g.stroke();g.restore();
		// 5 dashes
		g.save();g.strokeStyle="rgba(255,255,255,0.8)";g.lineWidth=2;g.setLineDash(new double[]{8,6});g.beginPath();g.moveTo(910,40);g.lineTo(1250,40);g.stroke();g.setLineDash(new double[]{3,9});g.lineDashOffset=4;g.lineWidth=4;g.beginPath();g.moveTo(910,70);g.lineTo(1250,100);g.stroke();g.setLineDash(new double[0]);g.restore();
		// 6 text
		g.save();g.fillStyle="#fff";g.font="800 34px \"Tajawal\",\"Segoe UI\",Tahoma,sans-serif";g.textAlign="center";g.textBaseline="middle";g.fillText("مرحلة جديدة",200,170);g.font="500 22px \"Tajawal\",\"Segoe UI\",Tahoma,sans-serif";g.textAlign="start";g.fillText("السرعة 1.5x",500,170);g.direction="ltr";g.font="28px Audiowide";g.textAlign="left";g.textBaseline="alphabetic";g.fillText("LEVEL 11 - 95%",620,180);g.textBaseline="top";g.font="800 18px Tajawal";g.fillText("Normal / Easy",950,160);g.restore();
		// 7 glowing text
		g.save();g.direction="ltr";g.shadowColor="#ff2a6d";g.shadowBlur=16;g.fillStyle="#ff2a6d";g.font="40px Audiowide";g.textAlign="center";g.textBaseline="middle";g.fillText("S",1180,170);g.restore();
		// 8 clip circle with stripes
		g.save();g.beginPath();g.arc(80,280,50,0,Math.PI*2);g.clip();g.strokeStyle="rgba(255,42,109,0.55)";g.lineWidth=5;g.beginPath();g.moveTo(10,330);g.lineTo(110,230);g.moveTo(36,330);g.lineTo(136,230);g.moveTo(62,330);g.lineTo(162,230);g.moveTo(-16,330);g.lineTo(84,230);g.stroke();g.restore();
		// 9 evenodd hole
		g.save();g.beginPath();g.rect(160,220,200,120);g.arc(260,280,45,0,Math.PI*2,true);g.fillStyle="rgba(255,42,109,0.5)";g.fill("evenodd");g.restore();
		// 10 gradients
		g.save();g.fillStyle="#222";g.fillRect(380,220,200,120);g.restore();
		var gr=g.createRadialGradient(440,280,10,440,280,60);gr.addColorStop(0,"rgba(255,255,255,0.6)");gr.addColorStop(1,"rgba(255,255,255,0)");g.fillStyle=gr;g.beginPath();g.arc(440,280,60,0,Math.PI*2);g.fill();
		var gl=g.createLinearGradient(500,230,570,330);gl.addColorStop(0,"#ff2436");gl.addColorStop(0.45,"#ff2436");gl.addColorStop(0.5,"#ffe0e4");gl.addColorStop(0.55,"#ff2436");gl.addColorStop(1,"#ff2436");g.fillStyle=gl;g.fillRect(500,230,70,100);
		// 11 curves
		g.save();g.strokeStyle="#7cff6b";g.lineWidth=3;g.beginPath();g.moveTo(600,330);g.quadraticCurveTo(650,200,700,330);g.bezierCurveTo(720,200,780,360,820,240);g.stroke();g.fillStyle="rgba(124,255,107,0.35)";g.beginPath();g.ellipse(900,280,60,30,0.4,0,Math.PI*2);g.fill();g.restore();
		// 12 global alpha + overlapping
		g.save();g.globalAlpha=0.5;g.fillStyle="#00f0ff";g.fillRect(1000,230,80,80);g.fillStyle="#ff2a6d";g.fillRect(1040,260,80,80);g.globalAlpha=1;g.restore();
		// 13 thin lines
		g.save();g.strokeStyle="#fff";g.lineWidth=0.5;g.beginPath();g.moveTo(1150,220);g.lineTo(1270,340);g.stroke();g.lineWidth=1;g.beginPath();g.moveTo(1150,240);g.lineTo(1270,240.5);g.stroke();g.lineWidth=1;g.beginPath();g.moveTo(1150.5,260);g.lineTo(1270.5,260);g.stroke();g.restore();
		// 14 star (self crossing) nonzero and evenodd
		g.save();g.fillStyle="#ffd23f";g.beginPath();g.moveTo(80,380);g.lineTo(110,480);g.lineTo(30,420);g.lineTo(130,420);g.lineTo(50,480);g.closePath();g.fill();g.beginPath();g.moveTo(230,380);g.lineTo(260,480);g.lineTo(180,420);g.lineTo(280,420);g.lineTo(200,480);g.closePath();g.fill("evenodd");g.restore();
		// 15 concave polygon with glow, laser beam
		g.save();g.shadowColor="#ff2a6d";g.shadowBlur=12;g.fillStyle="#ff2a6d";g.beginPath();g.moveTo(320,380);g.lineTo(420,380);g.lineTo(420,480);g.lineTo(380,480);g.lineTo(380,420);g.lineTo(320,420);g.closePath();g.fill();g.fillStyle="rgba(255,42,109,0.25)";g.fillRect(450,420,800,24);g.shadowBlur=24;g.fillStyle="#fff";g.fillRect(450,428,800,8);g.restore();
		// 16 round rect panel with stroke
		g.save();g.fillStyle="rgba(255,255,255,0.06)";g.strokeStyle="rgba(255,255,255,0.25)";g.lineWidth=2;g.beginPath();g.roundRect(40,520,300,150,16);g.fill();g.stroke();g.restore();
		// 17 big glowing ring
		g.save();g.shadowColor="#00f0ff";g.shadowBlur=30;g.strokeStyle="#00f0ff";g.lineWidth=6;g.beginPath();g.arc(480,600,70,0,Math.PI*2);g.stroke();g.restore();
		// 18 small dots / particles
		g.save();g.fillStyle="rgba(255,255,255,0.7)";g.fillRect(620,540,2,2);g.fillRect(630.5,545.5,3,3);g.beginPath();g.arc(650,550,1.5,0,Math.PI*2);g.fill();g.beginPath();g.arc(665,550,3,0,Math.PI*2);g.fill();g.restore();
		// 19 rotated thin rect outline
		g.save();g.translate(780,600);g.rotate(0.6);g.strokeStyle="#ff2a6d";g.lineWidth=2;g.strokeRect(-50,-30,100,60);g.restore();
		// 20 arabic right aligned paragraph and stroke text
		g.save();g.fillStyle="#fff";g.font="500 20px Tajawal";g.textAlign="right";g.textBaseline="middle";g.fillText("أفضل نتيجة: 98% • حرف S",1250,560);g.strokeStyle="rgba(200,200,220,0.18)";g.lineWidth=2;g.font="800 60px Tajawal";g.textAlign="center";g.strokeText("هلا",1050,640);g.restore();
	}
}
