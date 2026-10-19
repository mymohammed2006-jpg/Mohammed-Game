# Builds the UI check's inputs: fakenet.json (fixed Firebase answers) and script.json (the key script and the frames to capture).
# python3 make_ui.py <mg folder>
import json, sys, base64, os
MG = sys.argv[1]; OUT = os.path.dirname(os.path.abspath(__file__))
DOC = 'projects/mohammed-game-f7563/databases/(default)/documents'
IDS = ['L%d' % i for i in range(1, 12)] + ['F%d' % i for i in range(1, 9)]
def pl(uid, name, n, e, t='2026-10-1%dT10:00:00Z'):
    f = {'name': {'stringValue': name}}
    for k, m in (('n', n), ('e', e)):
        f[k] = {'mapValue': {'fields': {i: {'stringValue': r} for i, r in m.items()}}}
        f[k + 't'] = {'mapValue': {'fields': {i: {'timestampValue': t % (j % 9)} for j, i in enumerate(m)}}}
    return {'name': DOC + '/players/' + uid, 'fields': f}
R = 'SABC'
players = [pl('uMe', 'Mohammed', {'L1': 'S', 'L2': 'A'}, {'L1': 'B'})]
for k in range(13):
    n = {IDS[(k * 3 + j) % 19]: R[(k + j) % 4] for j in range(k % 7 + 1)}
    e = {IDS[(k * 5 + j) % 19]: R[(k * 2 + j) % 4] for j in range(k % 4)}
    players.append(pl('u%02d' % k, ['Gampet', 'ixGha', 'Nice', 'Nightmare', 'Twesii', 'A3D', 'AJF', 'Link', 'سالم', 'Fahad99', 'ابو_علي', 'zz_top', 'Kh4led'][k], n, e))
players.append(pl('u99', 'ixN1X', {'L1': 'S'}, {}))
bot = json.load(open(os.path.join(MG, 'data/bot.json')))['L1n']; s = bot['segs'][0]
raw = base64.b64decode(s['d'])
def seg(cp, seed, t0, n, b): return {'mapValue': {'fields': {'cp': {'integerValue': str(cp)}, 'seed': {'integerValue': str(seed)}, 't0': {'doubleValue': t0}, 'n': {'integerValue': str(n)}, 'd': {'stringValue': base64.b64encode(b).decode()}}}}
rpdoc = {'name': DOC + '/replays/u00_L1_n', 'fields': {'uid': {'stringValue': 'u00'}, 'lvl': {'stringValue': 'L1'}, 'm': {'stringValue': 'n'}, 'r': {'stringValue': 'S'}, 'v': {'integerValue': '3'}, 'hz': {'integerValue': '60'},
    'segs': {'arrayValue': {'values': [seg(0, s['seed'], s['t0'], 2000, raw[:10000]), seg(0, s['seed'], s['t0'], s['n'], raw)]}}}}
rules = [
    # [method, url contains, status, body]
    ['POST', 'accounts:signInWithPassword', 0, None],
    ['GET', '/players?pageSize=300', 200, {'documents': players}],
    ['GET', '/players/uMe', 200, players[0]],
    ['POST', ':runQuery', 200, [{'document': {'name': DOC + '/replays/u00_L1_n', 'fields': {'lvl': {'stringValue': 'L1'}, 'm': {'stringValue': 'n'}, 'r': {'stringValue': 'S'}}}, 'readTime': 'x'}, {'readTime': 'x'}]],
    ['GET', '/replays/', 200, rpdoc],
    ['POST', ':commit', 200, {'writeResults': [{}], 'commitTime': '2026-10-18T00:00:00Z'}],
]
json.dump({'rules': rules, 'signin_bad': {'error': {'code': 400, 'message': 'INVALID_LOGIN_CREDENTIALS', 'status': 'INVALID_ARGUMENT'}},
           'signin_ok': {'localId': 'uMe', 'idToken': 'tokA', 'refreshToken': 'rtA', 'expiresIn': '3600'}}, open(os.path.join(OUT, 'fakenet.json'), 'w'), ensure_ascii=False)
# ---- the key script: [frame, action, arg] ----
S = []; f = 0; caps = []
def at(fr): global f; f = fr
def key(k, gap=20): global f; S.append([f, 'key', k]); f += gap
def act(a, arg='', gap=20): global f; S.append([f, a, arg]); f += gap
def cap(*d): caps.extend(f + x for x in d)
# gate -> splash (auto ends at the drop, ~342 frames)
caps.extend([2, 60, 150, 250, 330, 344, 350, 356, 365, 380, 420, 470])
at(480); key('ArrowLeft'); cap(0, 5, 15, 30); f += 30
key('ArrowLeft'); cap(5, 30); f += 30
key('ArrowRight'); cap(5); f += 30
key('ArrowLeft'); f += 20
key('Enter'); cap(2, 8, 20, 40, 70); f += 80                    # news -> main
key('ArrowDown'); cap(2); key('ArrowUp')
key('Enter'); cap(2, 8, 15, 40, 60); f += 40                     # levels
for i in range(4): key('ArrowLeft', 12)
cap(2, 10, 40); f += 40
for i in range(4): key('ArrowRight', 12)
f += 30
key('KeyT'); cap(2, 10, 30); f += 30                             # practice (L1)
key('ArrowRight'); key('ArrowRight'); cap(2)
key('ArrowDown'); key('ArrowLeft'); cap(2)
key('ArrowDown'); key('Enter'); key('Enter'); cap(2)
key('ArrowDown'); key('ArrowLeft'); key('ArrowLeft'); cap(2)
key('ArrowDown'); cap(2); key('ArrowDown'); cap(2)
key('Enter', 5); cap(2, 10, 60, 120); f += 130                  # practice play
key('Escape'); cap(2, 10)
key('ArrowDown'); key('ArrowDown'); key('Enter'); cap(2, 10)   # back to the practice menu
key('Backspace'); cap(2, 15)
key('Enter', 5); cap(3, 40, 120, 160, 200, 230); f += 240          # L1 with the level intro
key('Escape'); cap(2)
for i in range(4): key('ArrowDown', 12)
key('Enter'); cap(2, 10, 30)
key('Backspace'); cap(2, 10, 30)                               # main
key('ArrowDown'); key('Enter'); cap(2, 15, 40)                  # friends
for i in range(5): key('ArrowLeft', 12)
cap(2, 30); f += 30
key('Backspace')
key('ArrowDown'); key('ArrowDown'); key('Enter'); cap(2, 20)    # how to play
key('Enter')
key('ArrowDown'); key('Enter'); cap(2, 20)                      # settings
key('ArrowLeft'); cap(2)
for i in range(5): key('ArrowDown'); cap(2)
key('ArrowDown'); key('ArrowRight'); key('Backspace')            # volume back up, main
key('ArrowRight'); key('Enter'); cap(2, 20); key('Enter')        # points
key('ArrowUp'); key('Enter'); cap(2, 20); key('Enter'); cap(2)   # Easy on, off
key('ArrowLeft'); key('Enter'); cap(2, 20)                       # account
act('type', 'Moh'); cap(2, 31)
key('ArrowDown'); act('type', 'wrongpw1'); key('ArrowDown'); cap(2)
key('Enter'); cap(1, 3, 10)
key('ArrowUp'); cap(2)
for i in range(8): act('bs', '', 3)
act('type', 'secret12'); key('ArrowDown'); key('Enter'); cap(1, 3, 6, 10, 30, 80); f += 80
key('ArrowDown'); key('ArrowDown'); key('ArrowDown'); key('Enter'); cap(1, 3, 6, 20)   # board
key('ArrowDown'); key('ArrowDown'); cap(2)
key('KeyR'); cap(2); key('KeyR'); key('KeyR'); cap(2)
key('ArrowLeft'); cap(2, 15)
key('Enter'); cap(1, 3, 6, 20)                                   # profile of the top player
key('ArrowDown'); key('ArrowLeft'); key('Enter'); cap(2, 20); key('ArrowRight'); key('ArrowUp')
for i in range(12): key('ArrowDown', 8)
cap(2, 20)
for i in range(12): key('ArrowUp', 8)
key('Enter'); cap(1, 3, 6, 10, 60, 200, 260); f += 260           # the replay
key('Enter'); cap(2, 30); key('ArrowRight'); cap(2, 10); key('ArrowUp'); key('ArrowUp'); cap(2)
key('KeyR'); cap(2); key('Enter'); cap(2, 60); f += 60
key('Backspace'); cap(2, 20); key('Backspace'); cap(2); key('Backspace'); cap(2, 20)
act('rpwin', '', 5); cap(1, 10, 30, 120, 200); f += 200            # saving a replay + the corner message
key('ArrowLeft'); key('Enter'); cap(2, 20)                       # account (logged in)
key('Enter'); cap(2); key('Enter'); cap(2, 20)                   # logout
key('Backspace'); key('ArrowRight'); key('Enter'); cap(2, 20); key('Enter')
S.append([f + 30, 'end', ''])
json.dump({'steps': S, 'caps': sorted(set(caps))}, open(os.path.join(OUT, 'script.json'), 'w'))
print('frames', f + 30, 'captures', len(set(caps)))
