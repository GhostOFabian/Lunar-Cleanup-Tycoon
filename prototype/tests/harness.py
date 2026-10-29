"""Shared Playwright setup: opens test.html headless with three.js replaced by a no-op mock."""
import os, json
from contextlib import contextmanager
from playwright.sync_api import sync_playwright

HERE = os.path.dirname(os.path.abspath(__file__))


@contextmanager
def game(start=True):
    errs = []
    with sync_playwright() as p:
        b = p.chromium.launch()
        pg = b.new_page(viewport={'width': 390, 'height': 844}, has_touch=True)
        pg.on('pageerror', lambda e: errs.append('PAGEERR ' + str(e)))
        pg.on('console', lambda m: errs.append('CONSOLE ' + m.text) if m.type == 'error' and 'Failed to load' not in m.text else None)
        pg.route('**/three.min.js', lambda r: r.fulfill(path=os.path.join(HERE, 'three-mock.js'), content_type='application/javascript'))
        pg.route('https://fonts.**', lambda r: r.abort())
        pg.goto('file://' + os.path.join(HERE, 'test.html'))
        pg.wait_for_timeout(800)
        if start:
            pg.click('#go')
        try:
            yield pg, errs
        finally:
            b.close()


def show(label, x):
    print(label, json.dumps(x, ensure_ascii=False)[:900])
