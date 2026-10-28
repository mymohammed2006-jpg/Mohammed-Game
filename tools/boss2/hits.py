# La Danse Macabre (B2): own onset map with numpy (no outside tools). beat n = 0.046 + n*60/128
import numpy as np, json, sys
sr=22050; x=np.fromfile(sys.argv[1],dtype=np.float32)
OFF=0.046; P=60/128
hop=128; n=2048; fps=sr/hop
win=np.hanning(n); fr=(len(x)-n)//hop
S=np.empty((fr,n//2+1),np.float32)
for i in range(fr): S[i]=np.abs(np.fft.rfft(x[i*hop:i*hop+n]*win))
fb=sr/n
L=np.log1p(20*S)
def flux(f0,f1):
    a,b=int(f0/fb),int(f1/fb); d=np.maximum(0,np.diff(L[:,a:b],axis=0)).sum(1); return np.concatenate([[0],d])
def peaks(env,thr_rel,mind):
    m=np.convolve(env,np.ones(int(fps*0.5))/int(fps*0.5),'same')
    out=[]; last=-9
    for i in range(1,len(env)-1):
        if env[i]>=env[i-1] and env[i]>=env[i+1] and env[i]>m[i]*thr_rel and env[i]>np.percentile(env,60) and (i-last)/fps>mind:
            out.append(i); last=i
    return out
res={}
for name,(f0,f1,rel,mind) in {'kick':(30,130,1.8,0.12),'snare':(1800,6000,1.8,0.1),'lead':(500,2200,1.6,0.08),'bass':(40,250,1.6,0.1)}.items():
    env=flux(f0,f1); pk=peaks(env,rel,mind)
    bs=[]; 
    for i in pk:
        b=((i*hop+n/2)/sr-OFF)/P; q=round(b*4)/4
        if abs(b-q)<0.12 and q>=0: bs.append(q)
    bs=sorted(set(bs)); res[name]=bs
    print(name,len(bs))
json.dump(res,open(sys.argv[2],'w'))
# print per 8 beats a grid of 32 quarter steps for kick/snare/lead
for b0 in range(0,248,8):
    line=f'{b0:3d} '
    for nm in ('kick','snare','lead'):
        s=set(res[nm]); line+=nm[0]+':'+''.join('#' if b0+k/4 in s else '.' for k in range(32))+' '
    print(line)
# pitch of each lead onset (strongest bin 200-2000 Hz in the 70 ms after it) as a midi number
pit=[]
for q in res['lead']:
    t=OFF+q*P; i0=int(t*fps); i1=i0+int(0.07*fps)
    a,b=int(200/fb),int(2000/fb); sp=S[i0:i1,a:b].mean(0); k=np.argmax(sp)+a; f=k*fb
    pit.append(round(69+12*np.log2(f/440),1))
res['leadPitch']=pit
json.dump(res,open(sys.argv[2],'w'))
import collections
for b0 in range(0,248,16):
    ps=[p for q,p in zip(res['lead'],pit) if b0<=q<b0+16]
    print(b0, ' '.join(str(int(round(p))) for p in ps[:40]))
