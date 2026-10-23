# The radio level (co-op for two): data/team_P3.json. Page 1 "الصدى" (the echo).
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

# ---------------- page 1: the echo ----------------
g=grid()
fill(g,1,62,1,28)                                   # all rock, then carve
# segment 1: the start ledge, a pit of spikes, the far ledge
fill(g,1,16,17,25,'.'); fill(g,7,14,26,27,'.'); fill(g,7,14,28,28,'^')
g[25][2]='S'; g[22][11]='1'
# segment 2: the shaft (both climb one zig-zag; each step is lit by the other one)
fill(g,17,29,2,27,'.'); fill(g,17,29,28,28,'^'); fill(g,15,16,26,28,'#')
fill(g,17,17,2,21,'#')                              # the wall between the start and the shaft (open at the bottom)
fill(g,23,29,6,7,'#')                               # the upper lane's floor reaches over the shaft's right half
# the alcove and the lower lane (blue's way, right through the shaft's wall)
fill(g,30,58,8,13,'.'); fill(g,30,58,14,14,'^')
# the upper lane (gold's way, above it)
fill(g,23,58,2,5,'.'); fill(g,18,22,2,7,'.')
fill(g,34,37,6,7,'.'); fill(g,46,49,6,7,'.')        # holes in its floor: through them you fall to the lane below
# the chute on the right, down to the landing; the bottom room going back left
fill(g,59,62,2,26,'.'); fill(g,31,58,16,26,'.'); fill(g,33,49,27,27,'^'); fill(g,31,34,25,26,'#')
g[26][61]='G'; g[2][52]='3'; g[24][33]='4'; g[9][37]='2'
th=[echo(BL,7.2,26,2.8),echo(BL,11.6,26,3.4),        # the pit: blue's clouds, then gold's a little higher
    echo(GD,7.0,24.4,3.0),echo(GD,11.4,24.4,3.0)]
# the shaft: two stairs side by side, gold's on the left and blue's on the right; each step lights from the other's jump
for y in [23.5,20.5,17.5,14.5,11.5,8.5]: th.append(echo(GD,18.5,y,3))
th.append(echo(BL,19.5,25.5,3))                   # blue's first step, low under gold's
for y in [22.5,19.5,16.5,13.5]: th.append(echo(BL,25,y,3))
th+=[echo(GD,34,6,4),echo(GD,46,6,4)]                # the holes in the upper lane's floor
for x0 in [30,35,39.5,44,48.5,53]: th.append(echo(BL,x0,11,3.5 if x0!=30 else 4))   # the lower lane: blue's clouds over spikes
# the bottom room: blue's clouds low, gold's high, staggered, out to the last sound and back
for x0 in [46.5,42.5,38.5]: th.append(echo(BL,x0,25,2))
for x0 in [47.5,43.5,39.5]: th.append(echo(GD,x0,24,2))
p1=dict(name='الصدى',map=[''.join(r) for r in g],things=th,saws=[],loop=[B[0],B[64]],beats=B[:65],theme=3,zoom=1.6,murk=1.0,mstep=0.04)

L=dict(id='P3',name='المرحلة الثالثة',song='زينة عماد - صوتك أقوى',stems=['PL_RADIO'],stemNames=['الأغنية'],on=[True],spb=0.6453,players=2,cols=[1,2],pages=[p1])
json.dump(L,open(os.path.join(root,'data/team_P3.json'),'w',encoding='utf-8'),ensure_ascii=False,indent=1)
for r in p1['map']: print(r)
