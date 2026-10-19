import json,sys
B=json.load(open('beats1.json'))
W,H=32,18
g=[['.']*W for _ in range(H)]
def put(x,y,c): g[y][x]=c
for x in range(W): put(x,0,'#'); put(x,17,'#')
for y in range(H): put(0,y,'#'); put(31,y,'#')
# bottom floor (row 16): start x1-9, pit x10-16, door column x17, ledge x18, alley spikes x19-30
for x in range(1,10): put(x,16,'#')
for x in range(10,17): put(x,16,'^')
put(17,16,'#'); put(18,16,'#')
for x in range(19,31): put(x,16,'^')
# wall between the scale's shaft and the start (the shaft is entered from the floor only)
for y in range(9,14): put(3,y,'#')
# the middle floor (row 8): open over the shaft (hatch), the pan L slot and the hole
for x in range(3,25): put(x,8,'#')
put(27,8,'#'); put(30,8,'#')
for y in (9,10): put(17,y,'#')
# spikes on the sky field
for x in range(5,20): put(x,7,'^')
# the wall between the top room and the sky field
for y in range(3,8): put(24,y,'#')
put(4,15,'S'); put(27,5,'1'); put(21,6,'2'); put(22,7,'G')
th=[
 dict(k='sink',id='cA',x=5,y=13,w=3,a=3,b=25,c=3),
 dict(k='sink',id='cB',x=7,y=11,w=3,a=3,b=25,c=3),
 dict(k='stone',id='sA',of='cA',x=6.5),
 dict(k='stone',id='sB',of='cB',x=8.5),
 dict(k='bridge',x=10,y=16,w=7,h=0.3,need=['sA','sB']),
 dict(k='door',x=17,y=11,w=1,h=5,need=['sA','sB']),
 dict(k='fall',x=20,y=15,w=2,a=1,b=4),
 dict(k='fall',x=24,y=13,w=2,a=1,b=4),
 dict(k='fall',x=28,y=11,w=2,a=1,b=4),
 dict(k='wind',x=18,y=9,w=13,h=7,a=230,b=2,c=0.5),
 dict(k='hail',x=19,y=9,w=12,h=7,a=470),
 dict(k='scale',id='sc',x=25,y=8,w=2,x1=1,y1=16,a=3,b=5,c=9,need=['bt']),
 dict(k='hatch',of='sc',x=1,y=8,w=2,h=1),
 dict(k='btn',id='bt',x=3,y=8,w=2,h=0.2),
 dict(k='path',x=5,y=3,w=19,h=0.3,need=['bt']),
 dict(k='ride',id='rd',x=5,y=6.75,w=4,a=48,b=22,x1=16),
 dict(k='bolt',of='rd',x=5,y=1,w=15,h=6),
]
saws=[dict(x0=13,y0=11,x1=13,y1=15,beats=4,r=16,ph=0),dict(x0=21,y0=10,x1=27,y1=10,beats=8,r=15,ph=0.25)]
page=dict(name='الصفحة الأولى: الأرض غيوم',map=[''.join(r) for r in g],things=th,saws=saws,loop=[B[0],B[-1]],beats=B,pieces={'1':2,'2':1})
L=dict(id='P2',name='المرحلة الثانية',song='Assala - الصورة',stems=['PL_SOURA_DRUMS','PL_SOURA_VOX','PL_SOURA_MUSIC'],stemNames=['الإيقاع','صوت أصالة','الموسيقى'],on=[True,False,False],spb=float(sys.argv[3]),pages=[page])
json.dump(L,open(sys.argv[4],'w',encoding='utf-8'),ensure_ascii=False,indent=1)
print('\n'.join(page['map']))
