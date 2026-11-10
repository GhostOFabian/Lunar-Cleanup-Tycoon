import json, time
from module import *
from render import png
d=json.load(open('' + __import__('os').path.join(__import__('os').path.dirname(__file__), '../../unity/Assets/Resources/Data/gamedata.json') + ''))
ok=0
for key in d['layouts']:
    t=time.time(); P,msg=build(key)
    print(key, msg, '%.1fs'%(time.time()-t), flush=True)
    if msg.startswith('ok'): ok+=1
    if P and hasattr(P,'paths'): png(P, f'all/{key}.png', S=7)
print('OK', ok, 'von', len(d['layouts']))
