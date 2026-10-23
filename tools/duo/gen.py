# The radio level (co-op for two): data/team_P3.json. Page 1 "السلك" (the cable).
#   python3 tools/duo/gen.py      (beats from tools/team/beats_radio.json, the song's own beats)
import json,os
here=os.path.dirname(os.path.abspath(__file__)); root=os.path.join(here,'..','..')
B=json.load(open(os.path.join(root,'tools/team/beats_radio.json')))
W,Hh=64,30
def grid():
    g=[['.']*W for _ in range(Hh)]
    for x in range(W): g[0][x]='#'; g[Hh-1][x]='#'
    for y in range(Hh): g[y][0]='#'; g[y][W-1]='#'
    return g
def fill(g,x0,x1,y0,y1,c='#'):
    for y in range(y0,y1+1):
        for x in range(x0,x1+1): g[y][x]=c
BL,GD=0,1      # blue, gold (a cloud of colour a is lit by the OTHER one's waves)
def echo(a,x,y,w): return dict(k='echo',a=a,x=x,y=y,w=w,h=0.4)

# ---------------- page 1: the cable (a try of his idea pick 20:44 10-23; the gate at the end as before) ----------------
# The two are tied by a cable (rope px). No dash on this level. The well: one holds at its edge, the other goes down, jumps off
# the cable into a niche for the sound, and is pulled back up. The swing pit (too wide to jump): one holds, the other swings
# across and jumps off the cable onto the far side, then holds there while the first one falls in and is pulled up.
W,Hh=60,32
g=grid()
fill(g,1,58,1,30)                                   # all rock, then carve
fill(g,1,58,9,17,'.')                               # the hall over the floor (row 18)
g[17][3]='S'
# the well, its niche on the right
fill(g,13,15,18,29,'.'); fill(g,13,15,30,30,'^'); fill(g,16,18,21,22,'.'); g[22][17]='1'
# the swing pit: seven wide, spikes far down
fill(g,21,27,18,29,'.'); fill(g,21,27,30,30,'^'); g[23][24]='2'
# the second well: a niche on the right, a sound hanging deep in it (only on the cable)
fill(g,44,46,18,29,'.'); fill(g,44,46,30,30,'^'); fill(g,47,49,21,22,'.'); g[25][44]='3'; g[22][48]='4'
g[17][56]='G'
th=[]
p1=dict(name='السلك',map=[''.join(r) for r in g],things=th,saws=[],loop=[B[0],B[64]],beats=B[:65],theme=3,zoom=1.6,murk=1.0,mstep=0.04,rope=300,nodash=True)

# ---------------- page 2: the keys (his yes 21:29 10-23) ----------------
# Two ways: gold's up top (floor row 13), blue's below (floor row 32); a curtain at each way's door lets only its own one in.
# Each one's buttons (only they press them) move the OTHER one's way: their clouds, doors and lift. Pink saws sweep over
# some buttons, so holding one means jumping the saw while the other one is out on what it holds.
W,Hh=72,34
g=grid()
fill(g,1,70,1,32)
fill(g,1,7,1,31,'.'); fill(g,1,4,17,17,'#'); g[16][2]='S'; fill(g,6,7,15,15,'#')    # the start: a ledge, a step up to gold's way
fill(g,8,62,1,12,'.')                               # gold's way (floor row 13)
fill(g,8,62,18,31,'.')                              # blue's way (floor row 32)
fill(g,63,70,1,31,'.')                              # the gate's hall: both ways end in it
fill(g,15,27,13,13,'^')                             # gold: the first pit (only blue's bridge goes over it)
fill(g,34,49,13,13,'^')                             # gold: the stepping pit
g[9][21]='1'; g[5][44]='2'
fill(g,37,62,22,31,'#')                             # blue: the high block (only the lift goes up)
fill(g,50,53,22,22,'^')                             # blue: a pit on top of it
g[20][44]='3'; g[19][51]='4'
g[31][67]='G'
BL,GD=0,1
def cb(id,a,x,y,w=1.6): return dict(k='cbtn',id=id,a=a,x=x,y=y,w=w)
th=[dict(k='cwall',a=GD,x=8,y=1,w=0.5,h=12),dict(k='cwall',a=BL,x=8,y=18,w=0.5,h=14),
    # 1: blue holds the bridge over gold's pit; gold opens blue's door
    cb('a1',BL,11.2,32),dict(k='path',need=['a1'],x=15,y=13,w=13,h=0.4),
    cb('b1',GD,30.2,13),dict(k='door',need=['b1'],x=16,y=18,w=0.6,h=14),
    # 2: blue's three buttons hold gold's stepping clouds (each one is held by two buttons, so they go along)
    cb('a2a',BL,20.2,32),cb('a2b',BL,25.2,32),cb('a2c',BL,30.2,32),
    dict(k='path',need=['a2a'],x=35,y=12.5,w=3,h=0.4),dict(k='path',need=['a2a'],alt=['a2b'],x=39.5,y=10.5,w=3,h=0.4),
    dict(k='path',need=['a2b'],alt=['a2c'],x=43.5,y=8.5,w=3,h=0.4),dict(k='path',need=['a2c'],x=47,y=10.5,w=2.5,h=0.4),
    # 3: gold on a button under a saw runs blue's lift up to the high ledge, and the bridge over blue's last pit
    cb('b2',GD,53.2,13),dict(k='mover',need=['b2'],x=34.2,y=32,x1=34.2,y1=22,w=2.6,h=0.4,b=4),
    dict(k='path',need=['b2'],x=50,y=22,w=4,h=0.4),
    # 4: the doors at the end: blue opens gold's, then gold (through it) opens blue's
    cb('a3',BL,55.2,22),dict(k='door',need=['a3'],x=58,y=1,w=0.6,h=12),
    cb('b3',GD,60.2,13),dict(k='door',need=['b3'],x=58.2,y=18,w=0.6,h=4)]
saws=[dict(x0=51,y0=12.4,x1=56,y1=12.4,beats=3,r=15),      # over gold's lift button
      dict(x0=22,y0=31.4,x1=28.5,y1=31.4,beats=2,r=15)]     # over blue's middle buttons
p2=dict(name='المفاتيح',map=[''.join(r) for r in g],things=th,saws=saws,loop=[B[64],B[145]],beats=B[64:146],theme=3,zoom=1.6,murk=0.84,mstep=0.04)

# ---------------- page 3: the shield (his yes 22:11 10-23, 100% harder than page 2) ----------------
# Speakers send pink sound waves along the floor; one of the two carries the shield (blue at the start): the rows of a wave
# that reach it stop there, so the one behind it is safe too. The one without it takes it by holding down on the other.
# A corridor with a speaker ahead, a hall with speakers on both sides by turns, then two lanes with cover blocks and the
# speakers firing down them, and the gate right under those speakers.
W,Hh=88,26
g=grid()
fill(g,1,86,1,24)
fill(g,1,24,16,21,'.'); g[21][2]='S'
fill(g,9,10,22,22,'^'); fill(g,15,17,22,22,'^'); g[18][13]='1'
fill(g,25,27,16,18,'.')                              # over the first speaker's block
fill(g,26,50,10,21,'.'); fill(g,33,35,22,22,'^'); fill(g,43,45,22,22,'^')
g[18][44]='2'
fill(g,51,53,11,18,'.')                              # over the second hall speaker
fill(g,54,80,11,15,'.'); fill(g,54,80,18,21,'.')     # the two lanes
for x in (60,66,72): fill(g,x,x,14,15,'#')
for x in (62,68,74): fill(g,x,x,20,21,'#')
g[12][63]='4'; g[21][71]='3'
fill(g,81,84,11,21,'.'); g[21][82]='G'
def spk(x,y,h,a,b,c,x1,v=420): return dict(k='spk',x=x,y=y,h=h,a=a,b=b,c=c,x1=x1,v=v)
th=[spk(25,19,3,-1,2,1,0),
    spk(25,19,3,1,4,0,51),spk(51,19,3,-1,4,2,25),
    spk(85,12,4,-1,3,0,53),spk(85,18,4,-1,3,1.5,53)]
saws=[dict(x0=34,y0=16.5,x1=34,y1=21.4,beats=2,r=15),      # up and down over the hall's first spike pit
      dict(x0=61,y0=15.4,x1=65,y1=15.4,beats=2,r=15),      # on the upper lane's floor, under sound 4
      dict(x0=69,y0=21.4,x1=72,y1=21.4,beats=2,r=15)]      # on the lower lane's floor, over sound 3
p3=dict(name='الدرع',map=[''.join(r) for r in g],things=th,saws=saws,loop=[B[145],B[205]],beats=B[145:206],theme=3,zoom=1.6,murk=0.68,mstep=0.04)

L=dict(id='P3',name='المرحلة الثالثة',song='زينة عماد - صوتك أقوى',stems=['PL_RADIO'],stemNames=['الأغنية'],on=[True],spb=0.6453,players=2,cols=[1,2],pages=[p1,p2,p3])
json.dump(L,open(os.path.join(root,'data/team_P3.json'),'w',encoding='utf-8'),ensure_ascii=False,indent=1)
for r in p3['map']: print(r)
