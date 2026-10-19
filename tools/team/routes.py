# the three bots' routes for page two, one block per room (times from that room's start)
import sys
R={}
R[1]=[[(0.3,'@190'),(0.9,'J @280'),(1.25,'@280'),(1.7,'J @440'),(2.05,'@440'),(2.6,'J @600'),(2.95,'@600'),(3.5,'J @760'),(3.85,'@760'),(4.4,'J @920'),(4.75,'@920'),(5.3,'J @1040'),(5.65,'@1040'),(6.2,'J @1180'),(6.55,'@1180')]]*3
R[1]=[[(t+off,c) for t,c in R[1][0]] for off in (0,1.58,3.16)]
R[2]=[
 # red: jumps for gold (D on), crosses alone on its own jumps, bottom of the stack, jumps for cyan (D on), then the gate
 [(3.5,'J'),(3.8,''),(5.0,'@262'),(5.7,'J @360'),(6.0,'@360'),(6.5,'J @520'),(6.8,'@520'),(7.3,'J @680'),(7.6,'@680'),(8.1,'J @840'),(8.4,'@840'),(8.9,'J @1000'),(9.2,'@1000'),(12.4,'J @1000'),(12.7,'@1000'),(15.6,'@1180')],
 # cyan: dashes up for B, crosses last, lands on red's head, up to the button, then over the door to the gate
 [(1.0,'U X'),(1.25,''),(9.5,'@262'),(10.2,'J @360'),(10.5,'@360'),(11.0,'J @520'),(11.3,'@520'),(11.8,'J @680'),(12.1,'@680'),(12.9,'J @840'),(13.2,'@840'),(13.7,'J @1000'),(14.0,'@1000'),(14.5,'J @1000'),(14.8,'@1000'),(17.0,'@1100'),(17.4,'@1180')],
 # gold: first across: A, B (cyan's), C (its own, on from the start's rest), D (red's jump); keeps moving so C stays
 [(1.2,'@262'),(1.65,'J @360'),(1.95,'@360'),(2.4,'J @520'),(2.7,'@520'),(3.2,'J @680'),(3.5,'@680'),(4.1,'J @840'),(4.4,'@840'),(4.8,'J @1050'),(5.1,'@1060')]+[(5.7+0.6*i,'@1040' if i%2==0 else '@1060') for i in range(17)]+[(15.6,'@1180')],
]
R[3]=[
 # red: waits on the left; the swap puts it on its button in the chamber (bridge), the next one puts it across
 [(0.3,'@300'),(3.95,''),(13.4,'@1220')],
 # cyan: crosses the bridge; swapped into the chamber, walks out its curtain; after its next swap: up for the drums, then the gate
 [(0.3,'@250'),(4.1,'@1050'),(7.2,'@760'),(7.9,'@1100'),(10.4,'@905'),(10.9,'J @780'),(11.2,'@780'),(11.6,'J @840'),(11.9,'@840'),(12.3,'J @960'),(12.6,'@1140'),(13.4,'@860'),(14.4,'@1220')],
 # gold: through its curtain onto red's button; swapped back left, crosses the bridge, waits, the gate
 [(0.3,'@330'),(1.0,'J @520'),(1.3,'@520'),(4.1,'@1150'),(13.4,'@1220')],
]
R[4]=[
 # red: its jump brings P, last off P (P goes), bottom of the stack, through the door, over the pit, the gate
 [(0.3,'@230'),(3.1,'J @320'),(3.4,'@320'),(6.6,'J @460'),(6.9,'@460'),(7.4,'@600'),(9.2,'@760'),(11.1,'@960'),(14.15,'J @1140'),(14.45,'@1220')],
 # cyan: onto P, dashes up in place (Q comes), to Q, down; climbs red to the button and the voice; through to the gate
 [(0.3,'@200'),(3.6,'J @300'),(3.9,'@300'),(4.3,'U X'),(4.5,''),(5.3,'J @430'),(5.6,'@430'),(7.2,'@620'),(9.2,'@700'),(9.9,'J @760'),(10.2,'@760'),(10.5,'J @790'),(10.8,'@800'),(12.15,'@975'),(14.35,'J @1140'),(14.65,'@1220')],
 # gold: onto P, to Q, down, through the door
 [(0.3,'@170'),(4.6,'J @350'),(4.9,'@350'),(6.1,'J @490'),(6.4,'@490'),(7.3,'@660'),(11.1,'@940'),(12.4,'@940'),(14.25,'J @1140'),(14.55,'@1220')],
]
for k in range(3):
    with open(f'q{k}.txt','w') as f:
        for room in sorted(R):
            f.write(f'ROOM {room}\n')
            for t,c in R[room][k]: f.write(f'{t:.2f} {c}\n')
