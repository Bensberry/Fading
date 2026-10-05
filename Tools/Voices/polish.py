import json, wave, numpy as np, os
D = r'C:/Users/Ben/AppData/Local/Temp/claude/C--Users-Ben-Fading/9065ce2a-6279-49e9-a878-9f3fbc00fb40/scratchpad/'
OUT = r'C:/Users/Ben/Fading/Assets/Resources/Audio/'
RATE = 22050

def read(p):
    w = wave.open(p, 'rb')
    a = np.frombuffer(w.readframes(w.getnframes()), dtype=np.int16).astype(np.float32) / 32768.0
    return a

def write(p, a):
    a = np.clip(a, -1, 1)
    w = wave.open(p, 'wb'); w.setnchannels(1); w.setsampwidth(2); w.setframerate(RATE)
    w.writeframes((a * 32767).astype(np.int16).tobytes()); w.close()

def trim(a, th=0.008):
    idx = np.where(np.abs(a) > th)[0]
    if len(idx) == 0: return a
    s = max(0, idx[0] - int(0.03 * RATE)); e = min(len(a), idx[-1] + int(0.12 * RATE))
    return a[s:e]

def lowpass(a, k):            # simple one-pole low-pass (k small = softer)
    out = np.empty_like(a); y = 0.0
    for i, x in enumerate(a):
        y += k * (x - y); out[i] = y
    return out

def reverb(a, mix, size):
    tail = int(RATE * 1.2 * size)
    x = np.concatenate([a, np.zeros(tail, dtype=np.float32)])
    wet = np.zeros_like(x)
    for delay_ms, fb in [(29.7, 0.72), (37.1, 0.70), (41.1, 0.68), (43.7, 0.66)]:
        d = int(RATE * delay_ms / 1000 * size)
        buf = np.zeros_like(x)
        for i in range(len(x)):                   # feedback comb filter
            buf[i] = x[i] + (fb * buf[i - d] if i >= d else 0.0)
        wet += buf
    wet /= 4.0
    for delay_ms, g in [(5.0, 0.7), (1.7, 0.7)]:  # two all-pass filters to smooth it
        d = int(RATE * delay_ms / 1000)
        y = np.zeros_like(wet)
        for i in range(len(wet)):
            xd = wet[i - d] if i >= d else 0.0
            yd = y[i - d] if i >= d else 0.0
            y[i] = -g * wet[i] + xd + g * yd
        wet = y
    return x * (1 - mix) + wet * mix

def fades(a):
    n = min(len(a) // 4, int(0.02 * RATE)); m = min(len(a) // 4, int(0.15 * RATE))
    a[:n] *= np.linspace(0, 1, n); a[-m:] *= np.linspace(1, 0, m)
    return a

lines = json.load(open(D + 'lines.json', encoding='utf-8'))
for vid, who, text in lines:
    a = trim(read(D + 'raw/' + vid + '.wav'))
    if who == 'GHOST':
        a = lowpass(a, 0.45); a = reverb(a, 0.42, 1.25); gain = 0.8
    elif who == 'FATHER':
        a = lowpass(a, 0.6); a = reverb(a, 0.28, 1.0); gain = 0.85
    else:
        a = reverb(a, 0.12, 0.7); gain = 0.9
    a = a / max(1e-4, np.max(np.abs(a))) * gain
    a = trim(a, 0.004)
    write(OUT + vid + '.wav', fades(a))
print('polished', len(lines))
