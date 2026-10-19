# Builds harness.html from the original game: no song decoding, a fake audio clock, a hand-driven performance.now,
# local fonts, and window.__mg(code) to evaluate code inside the game's closure.
import sys, re, os
src, out = sys.argv[1], sys.argv[2]
fonts = sys.argv[3]
s = open(os.path.join(src, 'index.html'), encoding='utf-8').read()
s = re.sub(r'<link[^>]*fonts\.googleapis\.com/css2[^>]*>', '<style>' + open(fonts).read() + '</style>', s)
s = re.sub(r'<link rel="preconnect"[^>]*>\n?', '', s)
# keep only bot.js; the songs become empty strings (decoding fails quietly, levels just have no song)
s = re.sub(r'<script src="songs/(?!bot\.js)[^"]*"></script>\n?', '', s)
names = sorted(set(re.findall(r'\b(SONG_[A-Z0-9]+)\b', s)))
stub = '''<script>
window.__NOW=100000;window.__T=0;performance.now=()=>window.__NOW;window.requestAnimationFrame=()=>0;
%s
(function(){const P=()=>({value:1,setValueAtTime(){},exponentialRampToValueAtTime(){},setTargetAtTime(){},linearRampToValueAtTime(){},cancelScheduledValues(){}});
const node=()=>({connect(){},disconnect(){}});
class AC{constructor(){this.state='running';this.sampleRate=44100;this.destination=node();}
 get currentTime(){return window.__T;}
 createGain(){return Object.assign(node(),{gain:P()});}
 createBiquadFilter(){return Object.assign(node(),{type:'',frequency:P(),Q:P()});}
 createOscillator(){return Object.assign(node(),{type:'',frequency:P(),start(){},stop(){}});}
 createBuffer(c,n){return{duration:n/44100,getChannelData:()=>new Float32Array(n)};}
 createBufferSource(){return Object.assign(node(),{buffer:null,loop:false,loopStart:0,loopEnd:0,playbackRate:P(),start(){},stop(){}});}
 decodeAudioData(ab,res,rej){setTimeout(()=>rej(new Error('x')),0);return new Promise(()=>{});}
 resume(){this.state='running';return Promise.resolve();} suspend(){return Promise.resolve();}}
window.AudioContext=AC;window.webkitAudioContext=AC;})();
</script>
''' % ''.join("window.%s='';" % n for n in names)
i = s.index('<script')
s = s[:i] + stub + s[i:]
a = "requestAnimationFrame(frame);\n})();"
assert s.count(a) == 1
s = s.replace(a, "requestAnimationFrame(frame);\nwindow.__mg=c=>eval(c);\n})();")
open(out, 'w', encoding='utf-8').write(s)
print('ok', out, len(names), 'song names stubbed')
