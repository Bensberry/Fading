# Makes every spoken line of Fading with Chatterbox (emotional, offline text-to-speech on the GPU).
# Run with:  C:\Users\Ben\FadingVoiceAI\venv\Scripts\python make_voices.py  [optional: a line id to make just that one]
# Voices: put ~10 s recordings in refs\  ->  mom.wav, grandma.wav, father.wav  (missing = Chatterbox's own voice).
# Emotion: 'exaggeration' (0.3 calm ... 1.2 very emotional) and 'cfg' (lower = slower, more deliberate delivery).
import json, os, sys, wave
import numpy as np
import torch, torchaudio
import chatterbox.models.t3.llama_configs as llama_configs
for _cfg in vars(llama_configs).values():                 # newer libraries: the end-of-sentence check needs 'eager' attention
    if isinstance(_cfg, dict) and 'attn_implementation' in _cfg: _cfg['attn_implementation'] = 'eager'
from chatterbox.tts import ChatterboxTTS

HERE = os.path.dirname(os.path.abspath(__file__))
LINES = os.path.join(HERE, 'lines.json')
OUT = r'C:/Users/Ben/Fading/Assets/Resources/Audio/'
REFS = {'MOM': 'mom.wav', 'GRANDMA': 'grandma.wav', 'FATHER': 'father.wav', 'GHOST': 'father.wav'}

# Default feeling per speaker, then special lines.
BASE = {'MOM': (0.65, 0.4), 'GRANDMA': (0.6, 0.35), 'FATHER': (0.6, 0.35), 'GHOST': (0.45, 0.35)}
SPECIAL = {
    # tearful / heartbroken
    'voice_mom_eyes': (0.9, 0.3), 'voice_dream_mom1_c': (0.9, 0.3), 'voice_dream_mom2_c': (0.85, 0.3),
    'voice_dream_mom3_a': (0.8, 0.3), 'voice_dream_mom3_c': (0.7, 0.3), 'voice_end2_mom_a': (0.95, 0.3),
    'voice_end2_mom_b': (0.85, 0.3), 'voice_end4_mom_b': (0.8, 0.3),
    # frightened / panicked
    'voice_end1_mom': (1.15, 0.35), 'voice_line_who_s_there_stop_it': (1.3, 0.45), 'voice_line_stop_please_stop': (1.1, 0.3),
    'voice_line_luna_i_m_coming_baby': (1.2, 0.45), 'voice_line_who_s_there': (0.9, 0.4),
    # warm / smiling / teasing
    'voice_dream_mom1_a': (0.75, 0.4), 'voice_dream_mom2_a': (0.75, 0.4), 'voice_end4_mom_a': (0.7, 0.35),
    'voice_mom_whosmiling': (0.7, 0.4), 'voice_line_sleep_little_one_mommy_s_here': (0.6, 0.3),
    # the father: tender, proud
    'voice_dream_luna2_b': (0.75, 0.35), 'voice_dream_luna3_b': (0.8, 0.3), 'voice_ghost_luna': (0.85, 0.3),
    # Grandma at the end of chapter 0
    'voice_grandma_gotothem': (0.75, 0.3), 'voice_grandma_isthatyou': (0.7, 0.3),
    # the ghost at his lowest
    'voice_end1_ghost': (0.7, 0.3), 'voice_end4_ghost': (0.6, 0.3),
}

def pieces(text):
    # Split a line into sentences; remember the pause after each ("..." = long, "." "?" "!" = short).
    import re
    parts = re.findall(r'.+?(?:[.][.][.]|[.?!]+|$)', text.strip())
    out = []
    for p in parts:
        p = p.strip()
        if not p: continue
        pause = 0.65 if p.endswith('...') else 0.3
        if p.startswith('...'): p = p[3:].strip()
        if p: out.append((p, pause))
    merged = []                                       # a 1-2 word bit with a short pause joins the next one ("Luna?! I'm coming")
    for p, pause in out:
        if merged and len(merged[-1][0].split()) <= 2 and merged[-1][1] < 0.5:
            merged[-1] = (merged[-1][0] + ' ' + p, pause)
        else:
            merged.append((p, pause))
    return merged or [(text, 0.3)]

SLOW = {'MOM': 0.92, 'GRANDMA': 0.88, 'FATHER': 0.9, 'GHOST': 0.85}    # play speed (1 = as made, lower = slower)

def save(path, chunks, sr, who):
    import librosa
    a = np.concatenate(chunks).astype(np.float32)
    a = librosa.effects.time_stretch(a, rate=SLOW[who])
    a = a / max(1e-4, np.max(np.abs(a))) * 0.9
    w = wave.open(path, 'wb'); w.setnchannels(1); w.setsampwidth(2); w.setframerate(sr)
    w.writeframes((np.clip(a, -1, 1) * 32767).astype(np.int16).tobytes()); w.close()

def main():
    only = sys.argv[1] if len(sys.argv) > 1 else None
    model = ChatterboxTTS.from_pretrained(device='cuda' if torch.cuda.is_available() else 'cpu')
    lines = json.load(open(LINES, encoding='utf-8'))
    os.makedirs(os.path.join(HERE, 'out'), exist_ok=True)
    for vid, who, text in lines:
        if only and vid != only: continue
        if not only and os.path.exists(os.path.join(HERE, 'out', vid + '.wav')): continue   # already made (delete it to redo)
        ex, cfg = SPECIAL.get(vid, BASE[who])
        ref = os.path.join(HERE, 'refs', REFS[who])
        kwargs = {'exaggeration': ex, 'cfg_weight': cfg}
        if os.path.exists(ref): kwargs['audio_prompt_path'] = ref
        chunks = []
        for piece, pause in pieces(text):                         # sentence by sentence, with real pauses between
            wav = model.generate(piece, **kwargs)
            chunks.append(wav.squeeze().cpu().numpy())
            chunks.append(np.zeros(int(model.sr * pause), dtype=np.float32))
        save(os.path.join(HERE, 'out', vid + '.wav'), chunks[:-1], model.sr, who)
        print('made', vid, who, ex, cfg, flush=True)

if __name__ == '__main__':
    main()
