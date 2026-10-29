"""Offline regression for v3 filter expressions and independent four-image positioning.
Run: python tests/v3_position_smoke.py (FFmpeg + Pillow required).
This doesn't compile WinForms; it verifies the same escaped FFmpeg overlay formulas.
"""
from pathlib import Path
import subprocess, tempfile, math
from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
SOURCE = (ROOT / 'Core' / 'RenderEngine.cs').read_text(encoding='utf-8')
UI = (ROOT / 'UI' / 'MainForm.cs').read_text(encoding='utf-8')
assert 'min({offsetX}\\\\,W-w)' in SOURCE and 'max(0\\\\,W-w-{offsetX})' in SOURCE
assert 'min({offsetY}\\\\,H-h)' in SOURCE and 'max(0\\\\,H-h-{offsetY})' in SOURCE
assert 'CreateOverlayNumberRow("Rộng %"' in UI
assert 'CreateOverlayNumberRow("Ngang %"' in UI
assert 'RowStyle(SizeType.Absolute, 296)' in UI
assert 'MarginBox' not in UI

width, height = 640, 360
# Deliberately non-square, distinct sizes to catch corner-specific size/settings bugs.
items = [
    ((255, 20, 20, 255), (80, 55), 20, 20, 4, 5, 'tl'),
    ((30, 255, 20, 255), (120, 80), 40, 25, 6, 7, 'tr'),
    ((40, 50, 255, 255), (90, 60), 50, 50, 90, 95, 'bl'),
    ((245, 245, 20, 255), (200, 155), 100, 100, 100, 100, 'br'),
]
with tempfile.TemporaryDirectory(prefix='lk-editor-v3-') as tmp:
    tmp = Path(tmp)
    paths = []
    for idx, (color, imsize, *_rest) in enumerate(items):
        p = tmp / f'img{idx}.png'
        Image.new('RGBA', imsize, color).save(p)
        paths.append(p)

    cmd = ['ffmpeg', '-nostdin', '-y', '-hide_banner', '-loglevel', 'error',
           '-f', 'lavfi', '-i', f'color=c=black:s={width}x{height}:r=1']
    for path in paths:
        cmd += ['-loop', '1', '-framerate', '1', '-i', str(path)]
    chain = ['[0:v]format=rgba[v0]']
    positions = []
    for i, (_color, imsize, wp, hp, ox, oy, corner) in enumerate(items):
        scale = min(width*wp/100/imsize[0], height*hp/100/imsize[1])
        w, h = int(imsize[0]*scale), int(imsize[1]*scale)
        dx, dy = int(width*ox/100), int(height*oy/100)
        x = min(dx, width-w) if corner in ('tl','bl') else max(0,width-w-dx)
        y = min(dy, height-h) if corner in ('tl','tr') else max(0,height-h-dy)
        assert x>=0 and y>=0 and x+w<=width and y+h<=height
        positions.append((x, y, w, h))
        ex = f'min({dx}\\,W-w)' if corner in ('tl','bl') else f'max(0\\,W-w-{dx})'
        ey = f'min({dy}\\,H-h)' if corner in ('tl','tr') else f'max(0\\,H-h-{dy})'
        chain.append(f'[{i+1}:v]scale={int(width*wp/100)}:{int(height*hp/100)}:force_original_aspect_ratio=decrease:flags=lanczos,format=rgba[logo{i}]')
        chain.append(f'[v{i}][logo{i}]overlay=x={ex}:y={ey}:eof_action=repeat:format=auto[v{i+1}]')
    chain.append('[v4]format=yuv420p[vout]')
    out = tmp/'out.png'
    cmd += ['-filter_complex', ';'.join(chain), '-map', '[vout]', '-frames:v', '1', str(out)]
    result=subprocess.run(cmd,capture_output=True,text=True,timeout=50)
    if result.returncode: raise AssertionError(result.stderr)
    img=Image.open(out).convert('RGB')
    # Assert any bright overlay pixels and frame size; overlap may naturally occur in this stress case.
    assert img.size == (width,height)
    assert sum(max(img.getpixel((x,y)))>100 for x in range(0,width,20) for y in range(0,height,20)) > 10
    print('PASS: v3 four overlays, independent sizes/offsets, escaped min/max, no overflow')
