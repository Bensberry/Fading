Voices for Fading
=================
The game's spoken lines are Assets/Resources/Audio/voice_*.wav (found by name, see GameAudio / CutsceneContext.Say / FamilyLife.Say).

They were made with Chatterbox (free, open-source emotional text-to-speech, MIT licence) on an NVIDIA GPU:
  folder C:\Users\Ben\FadingVoiceAI  (Python venv with PyTorch cu128 + chatterbox-tts installed with --no-deps)
  lines.json        every line: file name, speaker (MOM / GRANDMA / FATHER / GHOST), text
  refs\*.wav        10-20 s of each character's voice (mom.wav, grandma.wav, father.wav); replace with real recordings and re-run
  make_voices.py    makes the lines (emotion per line: exaggeration / cfg), sentence by sentence with pauses
  finish_voices.py  trims, levels, adds the ghostly echo, copies into the game

To redo everything:  delete FadingVoiceAI\out\*.wav, then
  FadingVoiceAI\venv\Scripts\python make_voices.py
  FadingVoiceAI\venv\Scripts\python finish_voices.py
To redo one line:  FadingVoiceAI\venv\Scripts\python make_voices.py voice_mom_eyes   (then finish_voices.py)
(speak.ps1 / polish.py are the older Windows-voice version.)
