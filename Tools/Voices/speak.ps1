Add-Type -AssemblyName System.Speech
$dir = "C:\Users\Ben\AppData\Local\Temp\claude\C--Users-Ben-Fading\9065ce2a-6279-49e9-a878-9f3fbc00fb40\scratchpad"
$raw = Join-Path $dir "raw"
New-Item -ItemType Directory -Force $raw | Out-Null
$lines = Get-Content (Join-Path $dir "lines.json") -Raw -Encoding UTF8 | ConvertFrom-Json

# who -> voice, speaking rate, pitch
$style = @{
  "MOM"     = @("Microsoft Zira Desktop",  "-10%", "-2st");
  "GRANDMA" = @("Microsoft Zira Desktop",  "-25%", "-5st");
  "FATHER"  = @("Microsoft David Desktop", "-12%", "-2st");
  "GHOST"   = @("Microsoft David Desktop", "-20%", "-3st")
}
$format = New-Object System.Speech.AudioFormat.SpeechAudioFormatInfo(22050, [System.Speech.AudioFormat.AudioBitsPerSample]::Sixteen, [System.Speech.AudioFormat.AudioChannel]::Mono)
$synth = New-Object System.Speech.Synthesis.SpeechSynthesizer
$n = 0
foreach ($l in $lines) {
  $id = $l[0]; $who = $l[1]; $text = $l[2]
  $s = $style[$who]
  $safe = [System.Security.SecurityElement]::Escape($text)
  $ssml = "<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='en-US'><voice name='$($s[0])'><prosody rate='$($s[1])' pitch='$($s[2])'>$safe</prosody></voice></speak>"
  $synth.SetOutputToWaveFile((Join-Path $raw "$id.wav"), $format)
  $synth.SpeakSsml($ssml)
  $n++
}
$synth.SetOutputToNull()
$synth.Dispose()
"made $n files"
