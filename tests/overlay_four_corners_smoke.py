"""FFmpeg-only smoke test of the 4-corner overlay chain used by RenderEngine.

Does not compile .NET and is not a substitute for running WinForms on Windows.
"""
from pathlib import Path
import subprocess
import tempfile
from PIL import Image


def main():
    with tempfile.TemporaryDirectory() as d:
        root = Path(d)
        colors = [(255, 0, 0), (0, 255, 0), (0, 0, 255), (255, 255, 0)]
        for i, color in enumerate(colors):
            Image.new('RGBA', (80, 80), (*color, 255)).save(root / f'corner{i}.png')
        out = root / 'test.mp4'
        cmd = [
            'ffmpeg', '-nostdin', '-y', '-v', 'error',
            '-f', 'lavfi', '-i', 'color=c=black:s=640x360:r=25:d=0.5',
        ]
        for i in range(4):
            cmd += ['-loop', '1', '-framerate', '25', '-i', str(root / f'corner{i}.png')]
        graph = ['[0:v]setpts=PTS-STARTPTS[v0]']
        positions = [('8','8'),('W-w-8','8'),('8','H-h-8'),('W-w-8','H-h-8')]
        for i, (x, y) in enumerate(positions):
            graph += [f'[{i+1}:v]scale=80:80:force_original_aspect_ratio=decrease,format=rgba[logo{i}]']
            graph += [f'[v{i}][logo{i}]overlay=x={x}:y={y}:eof_action=repeat:format=auto[v{i+1}]']
        graph += ['[v4]format=yuv420p[vout]']
        cmd += ['-filter_complex', ';'.join(graph), '-map', '[vout]', '-an',
                '-c:v', 'libx264', '-pix_fmt', 'yuv420p', '-t', '0.32', str(out)]
        subprocess.run(cmd, check=True, capture_output=True, text=True)
        frame = root / 'frame.png'
        subprocess.run(['ffmpeg', '-nostdin', '-y', '-v', 'error', '-i', str(out), '-frames:v', '1', str(frame)],
                       check=True, capture_output=True, text=True)
        with Image.open(frame) as im:
            sample = [im.convert('RGB').getpixel(pt) for pt in [(25,25),(615,25),(25,335),(615,335)]]
        assert all(abs(a-b) < 25 for got, want in zip(sample, colors) for a,b in zip(got,want)), (sample, colors)
        print('PASS: one MP4, 4 independent corner PNGs at expected pixel locations')

if __name__ == '__main__':
    main()
