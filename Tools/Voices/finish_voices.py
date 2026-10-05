# Takes the lines made by make_voices.py (out\) and puts them into the game (Assets/Resources/Audio/voice_*.wav):
# trims silence, evens out the volume, adds soft fades, and a ghostly echo to the father and to the ghost's thoughts.
# Run with:  C:\Users\Ben\FadingVoiceAI\venv\Scripts\python finish_voices.py
import json, os, wave
import numpy as np
import soundfile as sf
from scipy.signal import lfilter

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = r'C:/Users/Ben/Fading/Assets/Resources/Audio/'

def trim(a, sr, th=0.01):
    idx = np.where(np.abs(a) > th)[0]
    if len(idx) == 0: return a
    return a[max(0, idx[0] - int(0.04 * sr)): min(len(a), idx[-1] + int(0.15 * sr))]

def reverb(a, sr, mix, size):
    x = np.concatenate([a, np.zeros(int(sr * 1.3 * size))])
    wet = np.zeros_like(x)
    for ms, fb in [(29.7, 0.74), (37.1, 0.72), (41.1, 0.70), (43.7, 0.68)]:   # comb filters (fast, with scipy)
        d = int(sr * ms / 1000 * size)
        b = np.zeros(d + 1); b[0] = 1.0
        c = np.zeros(d + 1); c[0] = 1.0; c[d] = -fb
        wet += lfilter(b, c, x)
    wet /= 4.0
    for ms, g in [(5.0, 0.7), (1.7, 0.7)]:                                      # all-pass smoothing
        d = int(sr * ms / 1000)
        b = np.zeros(d + 1); b[0] = -g; b[d] = 1.0
        c = np.zeros(d + 1); c[0] = 1.0; c[d] = -g
        wet = lfilter(b, c, wet)
    return x * (1 - mix) + wet * mix

def soften(a, k):                                                             # gentle low-pass
    return lfilter([k], [1, -(1 - k)], a)

def fades(a, sr):
    n = min(len(a) // 4, int(0.02 * sr)); m = min(len(a) // 4, int(0.18 * sr))
    a[:n] *= np.linspace(0, 1, n); a[-m:] *= np.linspace(1, 0, m)
    return a

lines = json.load(open(os.path.join(HERE, 'lines.json'), encoding='utf-8'))
done = 0
for vid, who, text in lines:
    src = os.path.join(HERE, 'out', vid + '.wav')
    if not os.path.exists(src): continue
    a, sr = sf.read(src); a = a.astype(np.float64)
    a = trim(a, sr)
    if who == 'GHOST': a = reverb(soften(a, 0.5), sr, 0.42, 1.25); gain = 0.82
    elif who == 'FATHER': a = reverb(soften(a, 0.65), sr, 0.26, 1.0); gain = 0.88
    else: a = reverb(a, sr, 0.10, 0.7); gain = 0.92
    a = a / max(1e-4, np.max(np.abs(a))) * gain
    a = fades(trim(a, sr, 0.004), sr)
    sf.write(OUT + vid + '.wav', a.astype(np.float32), sr, subtype='PCM_16')
    done += 1
print('finished', done, 'lines into the game')
