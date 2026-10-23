# Bot routes for the radio level with times on the song's beats:  python3 tools/duo/mkroute.py <in.txt> <out.txt> [page_first_beat]
# A line "b12-0.3 J! @800" becomes "<page time of beat 12 minus 0.3> J! @800"; plain times pass as they are.
import json,sys,os,re
B=json.load(open(os.path.join(os.path.dirname(os.path.abspath(__file__)),'..','team','beats_radio.json')))
b0=int(sys.argv[3]) if len(sys.argv)>3 else 0
out=[]
for l in open(sys.argv[1]):
    l=l.rstrip('\n'); m=re.match(r'^b(\d+)([+-][\d.]+)?(\s.*)?$',l)
    if m:
        n=int(m.group(1)); t=B[b0+n]-B[b0]+float(m.group(2) or 0); l=f"{t:.3f}{m.group(3) or ''}"
    out.append(l)
open(sys.argv[2],'w').write('\n'.join(out)+'\n')
