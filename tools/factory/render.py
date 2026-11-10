from PIL import Image, ImageDraw
from planner import *
def png(P, fn, S=9):
    W=N*S; im=Image.new('RGB',(W,W),(17,17,20)); d=ImageDraw.Draw(im)
    X=lambda x:(x+EXT)*S+S/2; Y=lambda y:(EXT-y)*S+S/2
    if P.mask:
        for yy in range(-44,45):
            for xx in range(-44,45):
                if P.inb(xx,yy): d.rectangle([X(xx)-S/2,Y(yy)-S/2,X(xx)+S/2,Y(yy)+S/2],fill=(28,28,34))
    else:
        r=P.area_r*S; d.ellipse([X(0)-r,Y(0)-r,X(0)+r,Y(0)+r],fill=(28,28,34))
    for b in P.B.values():
        d.rectangle([X(b.x-b.hs)-S/2,Y(b.y+b.hs)-S/2,X(b.x+b.hs)+S/2,Y(b.y-b.hs)+S/2],fill=(40,60,85),outline=(100,140,180))
        d.text((X(b.x)-12,Y(b.y)-5),b.id,fill=(230,230,230))
    for (x0,y0,x1,y1) in P.docks: d.rectangle([X(x0)-S/2,Y(y1)-S/2,X(x1)+S/2,Y(y0)+S/2],outline=(120,80,40))
    w=int(S*.84)
    cols=[(80,255,200),(255,100,170),(255,255,100),(100,170,255),(255,140,60),(170,130,255),(130,255,130),(255,200,130),(100,255,255),(255,100,100),(200,200,200)]
    for i,(lid,p) in enumerate(P.paths.items()):
        if not p: continue
        pts=[(X(x),Y(y)) for x,y in p]
        if lid in P.merge: m=P.merge[lid][1]; pts.append((X(m[0]),Y(m[1])))
        c=cols[i%len(cols)]; d.line(pts,fill=c,width=w,joint='curve')
        mx,my=pts[len(pts)//2]; d.text((mx+4,my-12),lid,fill=(255,255,255))
    im.save(fn)
