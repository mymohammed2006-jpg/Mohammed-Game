# page two of the co-op level (four rooms in the red storm), added after page one in data/team_P2.json
import json,sys
src=sys.argv[1]
L=json.load(open(src,encoding='utf-8'))
L['pages']=L['pages'][:1]
B=json.load(open(sys.argv[2]))[:125]          # 81.27 .. 179.05 s, beat 0 = the drum's bar
def room():
    g=[['.']*32 for _ in range(18)]
    for x in range(32): g[0][x]='#'; g[17][x]='#'
    for y in range(18): g[y][0]='#'; g[y][31]='#'
    return g
def fill(g,x0,x1,y0,y1,c='#'):
    for y in range(y0,y1+1):
        for x in range(x0,x1+1): g[y][x]=c
R,C,Y=0,1,2
pages=[]
# ---- room 1: each one has eyes (dark; clouds only their owner sees)
g=room()
fill(g,1,4,13,16); fill(g,5,30,16,16,'^'); fill(g,27,30,6,6)
g[12][1]='S'; g[5][29]='G'
th=[dict(k='vis',a=R,x=6,y=12,w=2),dict(k='vis',a=C,x=10,y=10,w=2),dict(k='vis',a=Y,x=14,y=12,w=2),
    dict(k='vis',a=R,x=18,y=11,w=2),dict(k='vis',a=C,x=22,y=9,w=2),dict(k='vis',a=Y,x=25,y=7.5,w=2),
    # dead ends, so following one of the others' clouds blindly is wrong
    dict(k='vis',a=R,x=11,y=6.5,w=2),dict(k='vis',a=Y,x=20,y=14,w=2),dict(k='vis',a=C,x=16,y=8,w=1)]
saws=[dict(x0=12,y0=8,x1=12,y1=14,beats=4,r=15,ph=0),dict(x0=19,y0=7,x1=23,y1=7,beats=4,r=15,ph=0.5)]
pages.append(dict(name='الغيمة الحمراء 1',map=[''.join(r) for r in g],things=th,saws=saws,loop=[B[0],B[-1]],beats=B,on=[False,False,True],pieces={},theme=1,dark=True,stack=True))
# ---- room 2: your move is the others' bridge; then up on a head to the button
g=room()
fill(g,1,6,14,16); fill(g,7,22,16,16,'^'); fill(g,23,30,14,16)
g[13][1]='S'; g[13][29]='G'
th=[dict(k='flip',a=R,x=8,y=14,w=2,b=0),dict(k='flip',a=C,x=12,y=13,w=2,b=1),
    dict(k='flip',a=Y,x=16,y=14,w=2,b=1),dict(k='flip',a=R,x=20,y=14,w=2,b=1),
    dict(k='cloud',x=23,y=10.125,w=4),dict(k='btn',id='b2',x=24,y=10.125,w=2,h=0.2),
    dict(k='door',x=27,y=10.2,w=1,h=3.8,need=['b2'])]
pages.append(dict(name='2',map=[''.join(r) for r in g],things=th,saws=[dict(x0=24,y0=8.5,x1=29,y1=8.5,beats=8,r=14,ph=0)],cont=True,pieces={},stack=True))
# ---- room 3: the lightning swaps; curtains only one walks through, a button only red presses
g=room()
fill(g,1,8,14,16); fill(g,9,16,12,12); fill(g,9,16,8,8); fill(g,17,21,12,12)
fill(g,9,21,16,16,'^'); fill(g,22,30,15,16); fill(g,22,30,8,8); fill(g,22,30,4,4)
g[13][1]='S'; g[7][28]='1'; g[14][30]='G'
th=[dict(k='cwall',a=Y,x=9,y=9,w=1,h=3),dict(k='cwall',a=C,x=16,y=9,w=1,h=3),
    dict(k='cbtn',id='rb',a=R,x=12,y=12,w=2,h=0.2),
    dict(k='bridge',x=9,y=15,w=13,h=0.3,need=['rb']),
    dict(k='cloud',x=20,y=9.5,w=2),
    dict(k='cwall',a=C,x=22,y=5,w=1,h=3)]
pages.append(dict(name='3',map=[''.join(r) for r in g],things=th,saws=[],cont=True,pieces={'1':0},stack=True,swap=True))
# ---- room 4: chained, the red wall behind, Assala's voice up on the ledge
g=room()
fill(g,1,5,15,16); fill(g,6,13,16,16,'^'); fill(g,14,21,15,16); fill(g,22,30,15,16)
fill(g,25,26,15,15,'^')
g[14][1]='S'; g[10][20]='2'; g[14][30]='G'
th=[dict(k='flip',a=R,x=7,y=13,w=2,b=1),dict(k='flip',a=C,x=10.5,y=12,w=2,b=1),
    dict(k='cloud',x=18,y=11.125,w=4),dict(k='btn',id='b4',x=18,y=11.125,w=2,h=0.2),
    dict(k='door',x=22,y=11.2,w=1,h=3.8,need=['b4']),
    dict(k='chase',x=-1,a=1.0,c=4)]
pages.append(dict(name='4',map=[''.join(r) for r in g],things=th,saws=[dict(x0=27,y0=10,x1=27,y1=14,beats=2,r=14,ph=0)],cont=True,pieces={'2':1},stack=True,swap=True,tether=280))
L['pages']+=pages
json.dump(L,open(src,'w',encoding='utf-8'),ensure_ascii=False,indent=1)
for p in pages: print('\n'.join(p['map'])); print()
