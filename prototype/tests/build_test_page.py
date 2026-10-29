"""Builds test.html from ../index.html with a test hook (window.__t) and checks JS syntax with node.

window.__t is a Proxy that resolves any top-level name of the game script (functions, consts, S, W ...).
window.__ss(v) sets the private `stroke` variable (needed to call stampManual headless).
"""
import os, re, subprocess, sys, tempfile

HERE = os.path.dirname(os.path.abspath(__file__))
SRC = os.path.join(HERE, '..', 'index.html')
OUT = os.path.join(HERE, 'test.html')

html = open(SRC, encoding='utf-8').read()
hook = "try{boot();}"
if html.count(hook) != 1:
    sys.exit('boot hook not found exactly once')
html = html.replace(hook, "window.__ss=v=>{stroke=v};window.__t=new Proxy({},{get(_,k){return eval(k)}});" + hook)
open(OUT, 'w', encoding='utf-8').write(html)

# syntax check of the inline game script (the last <script> without src)
scripts = re.findall(r'<script>(.*?)</script>', open(SRC, encoding='utf-8').read(), re.S)
if scripts:
    with tempfile.NamedTemporaryFile('w', suffix='.js', delete=False, encoding='utf-8') as f:
        f.write(scripts[-1])
    try:
        r = subprocess.run(['node', '--check', f.name], capture_output=True, text=True)
        print('node --check:', 'OK' if r.returncode == 0 else r.stderr)
        if r.returncode != 0:
            sys.exit(1)
    except FileNotFoundError:
        print('node not installed – syntax check skipped')
print('wrote', OUT)
