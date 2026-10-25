import json
J=json.load(open('hits.json'))
def sel(k,a,b,gap=0,smin=0):
    out=[];last=-9
    for r in sorted(J[k],key=lambda r:r['b']):
        if a<=r['b']<b and r['s']>=smin and r['b']-last>=gap: out.append(r); last=r['b']
    return out
pul=[r['b'] for r in sel('kick',0,7,0.5)]+[r['b'] for r in sel('snare',7,32,0.5)]
pul=sorted(set(pul))
vox=sel('vox',13,32,0.25)
vp=[];prev=None
for r in vox:
    p=r['p'] or 60
    d=0 if prev is None else (1 if p>prev else -1 if p<prev else 0); prev=p
    if d: vp.append((r['b'],d))
arp=sel('mel',64,79,0.25)
ap=[(r['b'],(r['p'] or 60)%12) for r in arp]
net=[r['b'] for r in sel('kick',80,95.5,0.25)]
v2=[r['b'] for r in sel('vox',160,220,0.5)]
def arr(n,v): return f'\tstatic readonly double[] {n} = {{ {", ".join("%g"%x for x in v)} }};\n'
s='namespace MG;\n// Barracuda (main level 21): the song\'s hits in beats (Demucs stems + onsets), made by tools/level21/mkdata.py\npublic static partial class G\n{\n'
s+=arr('BR_PUL',pul)+arr('BR_VOXB',[a for a,_ in vp])+arr('BR_VOXD',[d for _,d in vp])+arr('BR_ARPB',[a for a,_ in ap])+arr('BR_ARPP',[p for _,p in ap])+arr('BR_NET',net)+arr('BR_VOX2',v2)
# the reef heaves on these bass hits (picked by ear from the bass stem)
s+=arr('BR_REEF',[32,33.5,36,37.5,38.5,40,41.5,44,45.5,46.5,47.75,49.5,52,53.5,54.5,55.5,57.5,60])
s+='}\n'
open('L21data.cs','w').write(s); print(s)
