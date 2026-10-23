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

L=dict(id='P3',name='المرحلة الثالثة',song='زينة عماد - صوتك أقوى',stems=['PL_RADIO'],stemNames=['الأغنية'],on=[True],spb=0.6453,players=2,cols=[1,2],pages=[p1])
json.dump(L,open(os.path.join(root,'data/team_P3.json'),'w',encoding='utf-8'),ensure_ascii=False,indent=1)
for r in p1['map']: print(r)
