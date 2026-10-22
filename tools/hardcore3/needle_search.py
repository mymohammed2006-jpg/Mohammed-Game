import itertools, math
W,H=1280,720; SPB=60/112; V=370
WS=[0,0.75,1.5]; WD=0.5  # beats
LINES=[]
for x in [320,430,640,850,960]: LINES+= [('V%d'%x,(x,0,x,H)),('v%d'%x,(x,H,x,0))]
for y in [180,240,360,480,540]: LINES+= [('H%d'%y,(0,y,W,y)),('h%d'%y,(W,y,0,y))]
LINES+=[('D1',(0,0,W,H)),('d1',(W,H,0,0)),('D2',(0,H,W,0)),('d2',(W,0,0,H)),('D3',(0,120,W,600)),('D4',(0,600,W,120))]
def side(l,p): x0,y0,x1,y1=l; return (x1-x0)*(p[1]-y0)-(y1-y0)*(p[0]-x0)
def inter(l,a,b):
    da,db=side(l,a),side(l,b)
    if da*db>=0: return None
    u=da/(da-db); return (a[0]+(b[0]-a[0])*u,a[1]+(b[1]-a[1])*u)
def needle_at(l,p):  # fraction along wall
    x0,y0,x1,y1=l; L2=(x1-x0)**2+(y1-y0)**2; return ((p[0]-x0)*(x1-x0)+(p[1]-y0)*(y1-y0))/L2
def room_ok(walls,s):
    # distance from s to every wall >= 70 and to screen edges >= 70
    for l in walls:
        x0,y0,x1,y1=l; d=abs(side(l,s))/math.hypot(x1-x0,y1-y0)
        if d<75: return False
    return 70<=s[0]<=W-70 and 70<=s[1]<=H-70
def ok(prev,walls,s):
    d=math.hypot(s[0]-prev[0],s[1]-prev[1])
    if d<220 or d>380: return False
    if not room_ok(walls,s): return False
    for i,l in enumerate(walls):
        c=inter(l,prev,s)
        if c is None: continue
        tp=math.hypot(c[0]-prev[0],c[1]-prev[1])/V + 0.25
        tn=(WS[i]+WD*needle_at(l,c))*SPB
        if tp>tn-0.12: return False
    return True
S_CAND=[(x,y) for x in range(160,1200,80) for y in range(120,660,60)]
def kind(n): return n[0].upper()
def search(prev,used_kinds):
    for combo in itertools.permutations(range(len(LINES)),3):
        names=[LINES[i][0] for i in combo]
        if len({n[1:] if n[0] in 'VvHh' else n for n in names})<3: continue
        ks=''.join(sorted(kind(n) for n in names))
        if ks in used_kinds: continue
        walls=[LINES[i][1] for i in combo]
        for s in S_CAND:
            if ok(prev,walls,s): return names,walls,s,ks
    return None
prev=(640,360); seq=[]; used=[]
for k in range(8):
    r=search(prev,used[-2:])
    print(k,r[0],r[2]); seq.append(r); used.append(r[3]); prev=r[2]
