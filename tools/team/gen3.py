# page three of the co-op level (the photo, a clear night): three rooms after page two's in data/team_P2.json
import json,sys
src=sys.argv[1]
L=json.load(open(src,encoding='utf-8'))
L['pages']=L['pages'][:5]
B=json.load(open(sys.argv[2]))[:77]           # 179.05 .. 238.98 s; then the song plays out once the photo is whole
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
# ---- room 1: the flash. Whoever works the camera snaps the other two in the air; each stands on their own picture
g=room()
fill(g,1,8,16,16); fill(g,9,21,16,16,'^'); fill(g,22,26,16,16); fill(g,27,30,5,16)
g[15][1]='S'; g[4][28]='1'; g[4][30]='G'
th=[dict(k='cam',a=R,x=5,y=16,w=2,h=0.2),dict(k='cam',a=C,x=23,y=16,w=1.5,h=0.2)]
pages.append(dict(name='الصورة 1',map=[''.join(r) for r in g],things=th,saws=[],loop=[B[0],B[-1]],beats=B,on=[False,False,True],pieces={'1':0},theme=2))
# ---- room 2: the mirror. Buttons and spikes are only in the reflection; a reflection hit is your hit
g=room()
fill(g,1,14,16,16); fill(g,1,8,8,8); fill(g,9,9,1,4); fill(g,9,9,8,11)
for y in range(18):
    for x in range(1,15): g[y][31-x]=g[y][x]
fill(g,15,16,1,15,'I')
g[15][1]='S'; g[7][2]='G'
g[15][26]='^'                                   # the floor of the first room, under left tile 5
g[15][18]='^'                                   # B's floor under left tile 13
g[10][21]='^'; g[12][17]='^'                    # half of c2, and c1's right end
g[7][24]='^'; g[7][28]='^'                      # the top floor, under left tiles 7 and 3
g[5][26]='2'                                    # Assala's voice, only a reflection reaches it
th=[dict(k='btn',id='mA',a=1,x=28,y=16,w=1,h=0.2),dict(k='btn',id='mB',a=1,x=25,y=16,w=1,h=0.2),
    dict(k='btn',id='mC',a=1,x=19,y=16,w=1,h=0.2),
    dict(k='door',x=9,y=12,w=1,h=4,need=['mA','mB'],alt=['mC']),
    dict(k='cloud',x=12,y=13,w=3),dict(k='cloud',x=10,y=11,w=2),dict(k='cloud',x=12,y=8,w=3)]
saws=[dict(x0=18.5,y0=9,x1=18.5,y1=13.5,beats=4,r=14,ph=0),dict(x0=23,y0=6.2,x1=27,y1=6.2,beats=4,r=13,ph=0.25)]
pages.append(dict(name='2',map=[''.join(r) for r in g],things=th,saws=saws,cont=True,pieces={'2':1},mirror=640))
# ---- room 3: the photo. Each carries their own piece (no dash, a lower jump) to its place in the big frame
g=room()
fill(g,1,30,16,16); fill(g,7,11,16,16,'^'); fill(g,23,27,16,16,'^'); fill(g,1,4,10,10); fill(g,12,19,4,4)
g[15][13]='S'
th=[dict(k='pic',a=R,x=2,y=9),dict(k='pic',a=C,x=29,y=15),dict(k='pic',a=Y,x=15.5,y=11),
    dict(k='cloud',x=15,y=12,w=2),
    dict(k='mover',x=7,y=14,x1=10,y1=14,w=2,h=0.4,b=4,c=0),dict(k='mover',x=23,y=14,x1=26,y1=14,w=2,h=0.4,b=4,c=2),
    dict(k='btn',id='bL',x=1,y=16,w=2,h=0.2),dict(k='btn',id='bT',x=12,y=4,w=0.8,h=0.2),
    dict(k='mover',x=20,y=16,x1=20,y1=4,w=2,h=0.4,b=6,c=0,need=['bL'],alt=['bT']),
    dict(k='slot',a=R,x=13.5,y=2.6),dict(k='slot',a=C,x=15.25,y=2.6),dict(k='slot',a=Y,x=17,y=2.6)]
pages.append(dict(name='3',map=[''.join(r) for r in g],things=th,saws=[],cont=True,pieces={},outro=B[-1]))
L['pages']+=pages
json.dump(L,open(src,'w',encoding='utf-8'),ensure_ascii=False,indent=1)
for p in pages: print('\n'.join(p['map'])); print()
