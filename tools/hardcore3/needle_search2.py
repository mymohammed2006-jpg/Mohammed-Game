# The needle patterns of gen.py (tighter version): 3 walls, stitched at 0 / 0.7 / 1.4 beats (0.5 beat each), fill at 2.1 beats.
# From the last button to the next one: 240-400 px, reachable before the fill, and the way crosses only walls the needle reaches
# at least 0.1 s after you can be there. The room that stays is 4-14% of the screen. Seeded order, so it always gives the same list.
import random, math, itertools
src=open(__file__.replace('needle_search2.py','needle_search.py')).read().split("prev=(640,360)")[0]
src=src.replace("WS=[0,0.75,1.5]","WS=[0,0.7,1.4]").replace("if d<220 or d>380: return False","if d<240 or d>400: return False").replace("tp=math.hypot(c[0]-prev[0],c[1]-prev[1])/V + 0.25","tp=math.hypot(c[0]-prev[0],c[1]-prev[1])/V + 0.22")
exec(src)
def ok2(prev,walls,s): return ok(prev,walls,s) and any(inter(l,prev,s) is not None for l in walls) and math.hypot(s[0]-prev[0],s[1]-prev[1])/V + 0.15 < 2.1*SPB
def area_frac(walls,s):
    n=tot=0
    for x in range(20,W,40):
        for y in range(20,H,40):
            tot+=1; n+= all((side(l,(x,y))>0)==(side(l,s)>0) for l in walls)
    return n/tot
rnd=random.Random(11)
combos=list(itertools.permutations(range(len(LINES)),3)); rnd.shuffle(combos)
S=S_CAND[:]; rnd.shuffle(S)
prev=(640,360); used=[]; sets=[]; out=[]
for k in range(8):
    found=None
    for strict in (True, False):
        for combo in combos:
            names=[LINES[i][0] for i in combo]
            base=[n[1:] if n[0] in 'VvHh' else n.lower() for n in names]
            if len(set(base))<3 or frozenset(base) in sets: continue
            ks=''.join(sorted(n[0].upper() for n in names))
            if len(set(ks))<2 or ks in used[-2:]: continue
            walls=[LINES[i][1] for i in combo]
            for s in S:
                if strict and (k%2==1) != (s[0]>640): continue
                if ok2(prev,walls,s) and 0.04<area_frac(walls,s)<0.14: found=(names,walls,s,ks,base);break
            if found: break
        if found: break
    out.append(found); used.append(found[3]); sets.append(frozenset(found[4])); prev=found[2]
print([ (f[1],f[2]) for f in out])
