"""Turns a Claude-artifact style page (no doctype/head) into a standalone HTML file for GitHub Pages.
Usage: python tools/standalone.py site/page.html index.html"""
import sys
HEAD = ('<!doctype html>\n<html lang="de">\n<head>\n<meta charset="utf-8">\n'
        '<meta name="viewport" content="width=device-width, initial-scale=1, viewport-fit=cover">\n'
        '<style>[hidden]{display:none!important}:root{padding-top:env(safe-area-inset-top,0px);padding-bottom:env(safe-area-inset-bottom,0px)}</style>\n')
src, dst = sys.argv[1], sys.argv[2]
s = open(src, encoding='utf-8').read()
if s.lstrip().lower().startswith('<!doctype'):
    sys.exit('already standalone')
open(dst, 'w', encoding='utf-8').write(HEAD + s)
print('wrote', dst)
