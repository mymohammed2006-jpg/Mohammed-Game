# Barracuda (L21): hit times per layer from the Demucs stems (w_drums/bass/other/vocals.wav of the song 0:00-1:52, 44100) -> hits.json; then mkdata.py -> src/Game/Dodge/L21data.cs
import numpy as np, soundfile as sf, json
from scipy.signal import butter,sosfilt,find_peaks
P=60/129.7; OFF=0.581
def B(t): return (t-OFF)/P
def load(n): x,sr=sf.read(f'w_{n}.wav'); return x,sr
def onsets(x,sr,lo,hi,rel=0.3,dist=0.1,floor=-45):
    if lo: x=sosfilt(butter(4,[lo,hi],'band',fs=sr,output='sos'),x)
    hop=int(sr*0.002); n=len(x)//hop; e=np.sqrt((x[:n*hop].reshape(n,hop)**2).mean(1)); db=20*np.log10(e+1e-6)
    on=np.maximum(0,np.diff(db,prepend=db[0]))
    # smooth
    k=np.ones(5)/5; on=np.convolve(on,k,'same')
    out=[]
    # local normalisation per 4 s window
    pk,_=find_peaks(on,distance=int(dist/0.002))
    for i in pk:
        a=max(0,i-1000); b=min(len(on),i+1000); m=np.percentile(on[a:b],99.5)
        if on[i]>rel*m and db[min(len(db)-1,i+15)]>floor: out.append((i*0.002, float(on[i]/m), float(db[min(len(db)-1,i+15)])))
    return out
d,sr=load('drums')
res={}
res['kick']=onsets(d,sr,35,110,0.45,0.15)
res['snare']=onsets(d,sr,170,500,0.45,0.12)
v,_=load('vocals'); res['vox']=onsets(v,sr,None,None,0.35,0.12,-32)
o,_=load('other'); res['mel']=onsets(o,sr,None,None,0.35,0.1,-30)
NN=['C','C#','D','D#','E','F','F#','G','G#','A','A#','B']
def pitch(x,t):
    s=x[int(t*sr)+200:int((t+0.12)*sr)]
    if len(s)<500: return None
    F=np.abs(np.fft.rfft(s*np.hanning(len(s)),8*len(s))); f=np.fft.rfftfreq(8*len(s),1/sr); k=(f>80)&(f<2500)
    fr=f[k][np.argmax(F[k])]; return int(round(12*np.log2(fr/440)))+69
J={}
for k,L in res.items():
    rows=[]
    for t,s,db in L:
        b=round(B(t)*4)/4
        if b<0 or b>226: continue
        r=dict(b=b,s=round(s,2))
        if k in('mel','vox'): r['p']=pitch(o if k=='mel' else v,t)
        rows.append(r)
    J[k]=rows
json.dump(J,open('hits.json','w'))
for k in J:
    print(k,len(J[k]))
    for a,b in [(0,32),(32,64),(64,80),(80,96),(96,160),(160,224)]:
        print('  ',a,' '.join(('%g'%r['b'])+((':'+NN[r['p']%12]) if r.get('p') else '') for r in J[k] if a<=r['b']<b)[:900])
