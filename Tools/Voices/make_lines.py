import re, json, os, glob
F = r'C:/Users/Ben/Fading/Assets/FadingScripts/'

def slug(line):
    s = re.sub(r'[^a-z0-9]+', '_', line.lower()).strip('_')
    return 'voice_line_' + s[:40].rstrip('_')

lines = []   # (id, speaker, text)

# story cutscene lines: Say("WHO", "text", seconds, "voice_id")
pat = re.compile(r'Say\("([A-Z]*)",\s*"((?:[^"\\]|\\.)*)",\s*[0-9.]+f,\s*"(voice_[a-z0-9_]+)"\)')
for f in glob.glob(F + 'Cutscenes/*.cs'):
    for who, text, vid in pat.findall(open(f, encoding='utf-8').read()):
        lines.append((vid, who or 'GHOST', text.replace('\\"', '"')))

# in-game lines (FamilyLife.Say): Mom's clue reactions + her other lines + the ghost's thoughts while she cries
mom = open(F + 'Core/MomLife.cs', encoding='utf-8').read()
lines_src = open(F + 'Core/FamilyLines.cs', encoding='utf-8').read()
mom_table = lines_src[lines_src.index('string[,] Mom'):lines_src.index('string[,] Luna')]
mom_lines = [m.replace('\\"', '"') for _, m in re.findall(r'\{\s*"(\w+)",\s*"((?:[^"\\]|\\.)*)"\s*\}', mom_table)]
for arr in ['NoticeLines', 'SighLines']:
    body = re.search(arr + r'\s*=\s*\{(.*?)\};', mom, re.S).group(1)
    mom_lines += re.findall(r'"((?:[^"\\]|\\.)*)"', body)
mom_lines += ["Sleep, little one. Mommy's here.", "Luna?! I'm coming, baby!", "Who's there?! Stop it!", "Stop... please, stop."]
for t in mom_lines: lines.append((slug(t), 'MOM', t))
cry = re.findall(r'"((?:[^"\\]|\\.)*)"', re.search(r'CryLines\s*=\s*\{(.*?)\};', mom, re.S).group(1))
for t in cry: lines.append((slug(t), 'GHOST', t))

seen = set(); out = []
for l in lines:
    if l[0] in seen: continue
    seen.add(l[0]); out.append(l)
json.dump(out, open(r'C:/Users/Ben/AppData/Local/Temp/claude/C--Users-Ben-Fading/9065ce2a-6279-49e9-a878-9f3fbc00fb40/scratchpad/lines.json', 'w', encoding='utf-8'), indent=1, ensure_ascii=False)
from collections import Counter
print(len(out), Counter(w for _, w, _ in out))
