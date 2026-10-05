# Fading: audio list

Put every file in **`Assets/Resources/Audio/`** and name it **exactly** like the name below (`.wav`, `.mp3` or `.ogg`).
A missing file is fine: the game just plays nothing there. Music and ambience should loop cleanly.
Sounds with no file keep using the quiet generated ones (room hum, footsteps, a soft chime).

Crossed out = already in the game (5 of 59).

## 1. Most important (biggest effect, do these first)
| File name | What | Length |
|---|---|---|
| `music_night` | slow, lonely night music (piano / strings / music-box), loops | 1-3 min |
| `music_day` | softer, hollow daytime music, loops | 1-3 min |
| `music_menu` | the title screen theme (candle, quiet), loops | 1-2 min |
| `music_ending` | the ending theme, gentle and sad then peaceful, loops | 1-2 min |
| `MatchStrike` | match scratch + flare (plays when the menu candle is lit) | 1-2 s |
| `footstep_1` ... `footstep_4` | soft footsteps on a wooden floor (4 variations) | 0.2-0.4 s each |
| `door_locked` | a door handle rattling, locked | 0.5-1 s |
| ~~`room_ambience`~~ | ~~quiet house at night: faint wind, wood creaks, distant clock (loops)~~ **DONE: Distant cricket ambient sound.mp3** | 20-60 s |

## 2. Game feedback sounds
| File name | What | Length |
|---|---|---|
| `flash_swell` | a rising, warm swell as the candle's light floods the screen (after PLAY) | 4-5 s |
| `touch_chime` | a soft chime when the ghost touches something | 0.5-1 s |
| `hint_whoosh` | a candle-hint beam appearing (warm, airy whoosh) | 1 s |
| `candle_out` | a candle flame being snuffed (breath + sizzle) | 1 s |
| `candle_relight` | a candle catching again (small flare) | 1 s |
| `ability_lost` | a low, sinking sound when a sense fades (vision / speed / hearing) | 2-3 s |
| `baby_giggle` | a baby's soft giggle | 1-2 s |

## 3. Sounds of the touchable objects
These fill the sound slots of the objects in the house. The name must match exactly (it is case-sensitive).
| File name | What |
|---|---|
| `candleWhoosh` | the hallway memorial candle flaring |
| `photoTap` | tapping a photo frame |
| `calendarRustle` | a paper calendar rustling |
| `chairCreak` | Grandma's rocking chair creaking |
| ~~`lampBuzz`~~ | ~~Grandma's lamp buzzing / flickering~~ **DONE: mix_00s (lamp switch)** |
| ~~`albumPage`~~ | ~~a photo-album page turning~~ **DONE: page-turning** |
| ~~`clockChime`~~ | ~~Grandma's clock chiming~~ **DONE: clock-chiming-1** |
| ~~`radioSong`~~ | ~~a short old song from Mom's radio (music, can be 30-60 s)~~ **DONE: Cant_Help.mp3 (friend's radio)** |
| `mugSlide` | a mug sliding on a table |
| `fogSqueak` | a finger squeaking on a foggy window |
| `lullaby` | a music-box lullaby (loops) |
| `toyThump` | a stuffed toy thumping |
| `nightlightClick` | a nightlight switch click |
| `mobileChime` | a baby mobile chiming |
| `doorCreak` | a door creaking open |

## 4. Voices (optional; the subtitles show the text anyway)
Record them yourselves or use text-to-speech. Each file is one line.
| File name | Who | Line |
|---|---|---|
| `voice_grandma_candle` | Grandma | "They say the light shows you the way home. Just until it burns out." |
| `voice_ghost_calling` | the ghost (whisper) | "A light in the dark. Someone is calling me home." |
| `voice_grandma_isthatyou` | Grandma | "...Is that you?" |
| `voice_grandma_latehome` | Grandma | "You always came home late." |
| `voice_grandma_son` | Grandma | "I know you're here, son." |
| `voice_grandma_gotothem` | Grandma | "Go see them. They need you more than I do." |
| `voice_grandma_lookafter` | Grandma | "Look after them, wherever you are." |
| `voice_mom_eyes` | Mom | "She has your eyes. I keep waiting for you to walk through that door." |
| `voice_ghost_mobile` | the ghost | "The mobile turns, though nobody touched it." |
| `voice_ghost_luna` | the ghost | "Luna... you can see me?" |
| `voice_ghost_giggles` | the ghost | "Luna giggles and reaches for something I can't hold." |
| `voice_mom_whosmiling` | Mom | "Who are you smiling at, baby?" |
| `voice_mom_isityou` | Mom | "...Is it you?" |
| ~~`voice_mom_okay`~~ (not needed any more: the endings changed) | Mom (ending 1) | "It's okay. We're going to be okay." |
| ~~`voice_mom_yougonow`~~ (not needed any more: the endings changed) | Mom (ending 1) | "You can go now." |
| ~~`voice_mom_feelyou`~~ (not needed any more: the endings changed) | Mom (ending 2) | "Sometimes I feel you here. I don't know why." |
| ~~`voice_mom_goodbye`~~ (not needed any more: the endings changed) | Mom (ending 2) | "Goodbye, anyway." |
| ~~`voice_mom_letsgo`~~ (not needed any more: the endings changed) | Mom (ending 3) | "This house is so cold now. Come on, Luna. Let's go." |
| ~~`voice_ghost_nobody`~~ (not needed any more: the endings changed) | the ghost (ending 4) | "Nobody noticed. Nobody turned around." |

## 5. Family life and scares (new)
| File name | What | Length |
|---|---|---|
| `baby_cry` | the baby starting to cry (when the ghost frightens her) | 3-6 s |
| `mom_cry` | a woman crying softly (Mom on her bed at night) | 6-12 s |
| `mom_sigh` | a tired, sad sigh | 1-2 s |
| `mom_gasp` | a frightened gasp (the ghost scared Mom) | 1 s |
| `scare_sting` | a short uneasy sting when the family is frightened (the bar turns red) | 1-2 s |
| ~~`voice_ghost_smile`~~ (not needed any more: the endings changed) | the ghost (ending 1): "She smiles at me. She always knew I was here." | 4 s |
| ~~`voice_ghost_seesme`~~ (not needed any more: the endings changed) | the ghost (ending 2): "For a moment, she sees me." | 3 s |

## 6. Dreams, the yard, the new endings (new)
| File name | What |
|---|---|
| `music_dream` | soft floating dream music (loops) |
| `swing_creak` | the back yard swings creaking (loops) |
| `notice_chime` / `memory_collect` / `dream_swell` | real versions of the made-up chimes (noticed / memory light / dream) |
| `voice_dream_mom1_a` ... `voice_dream_mom3_c` | Mom's three dreams (3 lines each, see the Word doc for the text) |
| `voice_dream_luna1_a` ... `voice_dream_luna3_b` | Luna's three dreams (the ghost speaks, 5 lines) |
| `voice_end1_mom` ... `voice_end4_ghost` | the four new endings (9 lines) |

## 7. Sounds of the new clues (new)
`tea_clink`, `flowers_rustle`, `fan_whir`, `globe_spin`, `clock_ring`, `ceiling_fan`, `glass_ring`, `doll_thump`, `duck_squeak`,
`airplane_whoosh`, `piggy_rattle`, `present_shake` (one short sound each, see the Word doc).

## The four endings (picked by the two bars at the end of the last night)
1. **THE FLIGHT** - nobody felt him; his desperate signs frightened them and Mom flees the house with Luna
2. **THE SMILE** - only Mom felt him; she smiles and speaks to him
3. **THE CRADLE** - only Luna felt him; she looks up from her cradle and smiles
4. **HOME** - both felt him
