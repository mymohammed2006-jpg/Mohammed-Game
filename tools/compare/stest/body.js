g.fillStyle="#07040d";g.fillRect(0,0,1280,720);
var T='"Audiowide","Segoe UI",sans-serif';
var ps=[-0.2,0.1,0.35,0.6,0.9];
for(var i=0;i<5;i++){var now=1700*(ps[i]+0.3)/1.6+5*1700;
g.save();g.translate(140+250*i,200);g.scale(1.1,1.1);g.font="150px "+T;g.textAlign="center";g.textBaseline="middle";g.direction="ltr";sShine("S",0,0,110,now,30);g.restore();
g.font="32px "+T;g.textAlign="center";g.textBaseline="middle";g.direction="ltr";sShine("S",140+250*i,450,26,now,12);}
g.font="150px "+T;g.fillStyle="#ff2436";g.shadowColor="#ff2436";g.shadowBlur=30;g.fillText("A",640,600);g.shadowBlur=0;
