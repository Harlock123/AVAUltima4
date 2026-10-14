#!/usr/bin/env python3
"""Synthesises AVAUltima4's sound effects and tunes into ../src/AVAUltima4/Sounds as 16-bit mono WAV.

Nothing is sampled from any other game: every effect is built here from square, triangle and
noise voices, and the tunes are new compositions. Run it from anywhere:
    python3 tools/make_sounds.py
"""
import math
import os
import random
import struct
import wave

RATE = 22050
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "src", "AVAUltima4", "Sounds")
RNG = random.Random(1985)


def write(name, samples, volume=0.7):
    peak = max(1e-9, max(abs(s) for s in samples))
    with wave.open(os.path.join(OUT, name), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(b"".join(struct.pack("<h", int(s / peak * volume * 32767)) for s in samples))


def voice(kind, phase):
    p = phase % 1.0
    if kind == "square":
        return 1.0 if p < 0.5 else -1.0
    if kind == "pulse":
        return 1.0 if p < 0.25 else -1.0
    if kind == "tri":
        return 4 * p - 1 if p < 0.5 else 3 - 4 * p
    if kind == "saw":
        return 2 * p - 1
    if kind == "sine":
        return math.sin(2 * math.pi * p)
    return RNG.uniform(-1, 1)


def tone(seconds, f0, f1=None, kind="square", attack=0.004, release=1.0, vibrato=0.0, vib_rate=8.0, gain=1.0):
    """A note gliding from f0 to f1 (exponentially), with a short attack and a shaped decay."""
    n = int(RATE * seconds)
    f1 = f1 or f0
    out, phase, held = [], 0.0, 0.0
    for i in range(n):
        t = i / RATE
        f = f0 * (f1 / f0) ** (i / max(1, n - 1))
        if vibrato:
            f *= 1 + vibrato * math.sin(2 * math.pi * vib_rate * t)
        phase += f / RATE
        if kind == "noise":
            # Noise re-sampled at the voice's rate: a lower 'pitch' sounds rougher.
            if int(phase) != int(phase - f / RATE):
                held = RNG.uniform(-1, 1)
            s = held
        else:
            s = voice(kind, phase)
        env = min(1.0, t / attack) * (1 - i / n) ** release
        out.append(s * env * gain)
    return out


def silence(seconds):
    return [0.0] * int(RATE * seconds)


def cat(*parts):
    out = []
    for p in parts:
        out.extend(p)
    return out


def mix(*parts):
    n = max(len(p) for p in parts)
    out = [0.0] * n
    for p in parts:
        for i, v in enumerate(p):
            out[i] += v
    return out


NOTES = {"C": 0, "D": 2, "E": 4, "F": 5, "G": 7, "A": 9, "B": 11}


def hz(note):
    n = NOTES[note[0]]
    rest = note[1:]
    while rest and rest[0] in "#b":
        n += 1 if rest[0] == "#" else -1
        rest = rest[1:]
    return 440.0 * 2 ** ((12 * (int(rest) + 1) + n - 69) / 12)


def tune(bpm, lead, bass, kind="pulse"):
    """Two voices: a lead and a bass line, as (note, beats) lists; '-' is a rest."""
    beat = 60.0 / bpm

    def line(notes, k, gain, release):
        out = []
        for note, beats in notes:
            d = beats * beat
            out.extend(silence(d) if note == "-" else tone(d, hz(note), kind=k, release=release, gain=gain))
        return out

    return mix(line(lead, kind, 0.55, 0.6), line(bass, "tri", 0.6, 0.3))


def main():
    os.makedirs(OUT, exist_ok=True)
    # Walking, walls, doors, ladders, water.
    write("Step.wav", tone(0.05, 120, 70, "noise", release=1.5), 0.2)
    write("Bump.wav", mix(tone(0.09, 90, 60, "square", release=1.2), tone(0.08, 300, 100, "noise", release=1.5)), 0.35)
    write("Door.wav", cat(tone(0.12, 140, 110, "saw", release=0.5, vibrato=0.06, vib_rate=30), tone(0.12, 160, 90, "noise", release=1.0)), 0.38)
    write("Ladder.wav", cat(*[cat(tone(0.04, 400 - i * 40, 300 - i * 40, "noise", release=1.2), silence(0.05)) for i in range(4)]), 0.35)
    write("Splash.wav", mix(tone(0.35, 1200, 300, "noise", release=0.8), tone(0.3, 220, 140, "sine", release=0.9, gain=0.4)), 0.3)
    write("Trap.wav", mix(tone(0.5, 600, 60, "noise", release=0.5), tone(0.4, 200, 50, "square", release=0.8)), 0.5)
    # Fighting.
    write("Hit.wav", mix(tone(0.08, 500, 120, "noise", release=1.0), tone(0.07, 180, 90, "square", release=1.2)), 0.42)
    write("Miss.wav", tone(0.12, 900, 300, "noise", release=0.7, gain=0.6), 0.25)
    write("Shoot.wav", cat(tone(0.03, 300, 900, "tri", release=0.4), tone(0.12, 1200, 400, "noise", release=1.0)), 0.3)
    write("Kill.wav", mix(tone(0.3, 400, 50, "noise", release=0.6), tone(0.28, 220, 55, "square", release=0.7)), 0.45)
    write("Hurt.wav", tone(0.12, 200, 120, "saw", release=0.8), 0.35)
    write("Fall.wav", cat(*[tone(0.12, hz(n), kind="square", release=0.6) for n in ["E4", "C4", "A3"]], tone(0.3, hz("E3"), kind="tri", release=0.5)), 0.45)
    write("Spell.wav", mix(tone(0.55, 400, 1600, "sine", release=0.5, vibrato=0.08, vib_rate=14), tone(0.55, 800, 3200, "tri", release=0.7, gain=0.4)), 0.42)
    write("Fire.wav", mix(tone(0.7, 300, 80, "noise", release=0.5), tone(0.6, 160, 60, "saw", release=0.6, vibrato=0.2, vib_rate=11)), 0.5)
    write("Heal.wav", cat(*[tone(0.09, hz(n), kind="sine", release=0.3) for n in ["C5", "E5", "G5", "C6", "E6"]]), 0.42)
    write("Sleep.wav", cat(*[tone(0.18, hz(n), kind="sine", release=0.5, vibrato=0.02) for n in ["G5", "E5", "C5", "G4"]]), 0.4)
    write("Cannon.wav", mix(tone(0.6, 160, 30, "noise", release=0.6), tone(0.5, 90, 35, "square", release=0.8), tone(0.08, 2000, 400, "noise", release=1.0, gain=0.5)), 0.6)
    # Magic, the moongates, and the stirring of a virtue.
    write("Mix.wav", cat(*[tone(0.06, 500 + RNG.randint(0, 600), 300, "sine", release=0.6) for _ in range(7)]), 0.35)
    write("Gate.wav", mix(tone(1.0, 300, 1200, "sine", release=0.4, vibrato=0.05, vib_rate=7), tone(1.0, 1200, 300, "tri", release=0.4, gain=0.5), tone(1.0, 2400, 2400, "noise", release=0.3, gain=0.08)), 0.45)
    write("Chime.wav", mix(*[cat(silence(0.07 * i), tone(0.9, hz(n), kind="sine", release=0.6)) for i, n in enumerate(["E5", "G#5", "B5", "E6"])]), 0.4)
    write("Shame.wav", cat(tone(0.25, hz("A3"), kind="tri", release=0.4), tone(0.4, hz("G#3"), kind="tri", release=0.6, vibrato=0.03, vib_rate=5)), 0.4)
    # Gold, menus, levels.
    write("Coin.wav", cat(tone(0.05, hz("B5"), kind="square", release=0.5), tone(0.18, hz("E6"), kind="square", release=0.7)), 0.3)
    write("Menu.wav", tone(0.03, 1200, kind="tri", release=0.8), 0.2)
    write("Pick.wav", cat(tone(0.03, 900, kind="tri", release=0.6), tone(0.05, 1350, kind="tri", release=0.8)), 0.25)
    write("LevelUp.wav", tune(220, [("D5", 1), ("F#5", 1), ("A5", 1), ("D6", 1), ("A5", 1), ("D6", 3)], [("D3", 4), ("A2", 2), ("D3", 2)]), 0.5)
    write("Victory.wav", tune(150,
        [("A4", 0.5), ("D5", 0.5), ("F#5", 0.5), ("A5", 1), ("F#5", 0.5), ("A5", 2.5)],
        [("D3", 1.5), ("A2", 1), ("D3", 3)]), 0.55)
    write("Defeat.wav", tune(66,
        [("E5", 1), ("D5", 1), ("C5", 1), ("B4", 1), ("A4", 1), ("G#4", 1), ("A4", 3)],
        [("A2", 2), ("F2", 2), ("E2", 2), ("A2", 3)], kind="tri"), 0.55)

    # The title: a slow call in E minor over open fifths, rising to the relative major (new). Loops.
    write("TitleTune.wav", tune(84,
        [("E4", 1), ("B4", 1), ("E5", 1.5), ("F#5", 0.5), ("G5", 1), ("F#5", 1), ("D5", 2),
         ("E5", 1), ("B4", 1), ("A4", 1.5), ("B4", 0.5), ("C5", 1), ("D5", 1), ("B4", 2),
         ("G5", 1), ("A5", 1), ("B5", 1.5), ("A5", 0.5), ("G5", 1), ("F#5", 1), ("D5", 2),
         ("E5", 1), ("D5", 1), ("B4", 1), ("C5", 1), ("B4", 4)],
        [(n, 2) for n in ["E3", "E3", "D3", "D3", "A2", "A2", "B2", "B2", "G2", "G2", "D3", "D3", "C3", "C3", "B2", "B2"]], kind="tri"), 0.55)
    # The open land: a walking tune in G, steady and hopeful (new). Loops.
    write("WorldMusic.wav", tune(104,
        [("D5", 1), ("G5", 1), ("B5", 1), ("A5", 0.5), ("G5", 0.5), ("E5", 1), ("D5", 1), ("B4", 2),
         ("C5", 1), ("E5", 1), ("G5", 1), ("F#5", 0.5), ("E5", 0.5), ("D5", 1), ("A4", 1), ("D5", 2),
         ("D5", 1), ("G5", 1), ("B5", 1), ("C6", 0.5), ("B5", 0.5), ("A5", 1), ("G5", 1), ("E5", 2),
         ("C5", 1), ("D5", 1), ("E5", 1), ("F#5", 1), ("G5", 4)],
        [(n, 1) for n in ["G2", "D3", "G2", "D3", "E2", "B2", "G2", "D3",
                          "C3", "G3", "C3", "G3", "D3", "A2", "D3", "D3",
                          "G2", "D3", "G2", "D3", "C3", "G2", "E2", "B2",
                          "C3", "G2", "D3", "A2", "G2", "D3", "G2", "G2"]], kind="pulse"), 0.34)
    # Towns: a gentle round in F, plucked on a triangle voice (new). Loops.
    write("TownMusic.wav", tune(112,
        [("F4", 0.5), ("A4", 0.5), ("C5", 1), ("A4", 0.5), ("Bb4", 0.5), ("C5", 1), ("D5", 1), ("C5", 2),
         ("Bb4", 0.5), ("A4", 0.5), ("G4", 1), ("A4", 0.5), ("Bb4", 0.5), ("A4", 1), ("G4", 1), ("F4", 2),
         ("F4", 0.5), ("A4", 0.5), ("C5", 1), ("F5", 1), ("E5", 0.5), ("D5", 0.5), ("C5", 1), ("A4", 2),
         ("Bb4", 1), ("G4", 1), ("A4", 0.5), ("G4", 0.5), ("F4", 1), ("F4", 3)],
        [(n, 1) for n in ["F2", "C3", "F2", "C3", "Bb2", "F3", "C3", "C3",
                          "G2", "D3", "C3", "G2", "C3", "G2", "F2", "F2",
                          "F2", "C3", "F2", "C3", "C3", "G2", "F2", "C3",
                          "Bb2", "C3", "F2", "C3", "F2", "F2"]], kind="tri"), 0.36)
    # Dungeons: a slow, uneasy figure in D minor over a dragging bass (new). Loops.
    write("DungeonMusic.wav", tune(70,
        [("D4", 2), ("F4", 1), ("E4", 1), ("A3", 2), ("-", 1), ("Bb3", 1),
         ("D4", 2), ("C#4", 1), ("E4", 1), ("D4", 3), ("-", 1),
         ("A4", 2), ("G4", 1), ("F4", 1), ("E4", 2), ("D4", 1), ("C#4", 1), ("D4", 4)],
        [(n, 2) for n in ["D2", "D2", "Bb1", "A1", "D2", "A1", "D2", "D2", "F2", "C2", "Bb1", "A1", "D2", "D2"]], kind="tri"), 0.34)
    # At sea: a rolling six-eight in A minor (new). Loops.
    write("SeaMusic.wav", tune(132,
        [("A4", 1), ("C5", 0.5), ("E5", 1), ("D5", 0.5), ("C5", 1), ("B4", 0.5), ("A4", 1.5),
         ("G4", 1), ("B4", 0.5), ("D5", 1), ("C5", 0.5), ("B4", 1), ("A4", 0.5), ("G4", 1.5),
         ("A4", 1), ("C5", 0.5), ("E5", 1), ("G5", 0.5), ("F5", 1), ("E5", 0.5), ("D5", 1.5),
         ("E5", 1), ("C5", 0.5), ("B4", 1), ("G#4", 0.5), ("A4", 3)],
        [(n, 1.5) for n in ["A2", "E3", "A2", "E3", "G2", "D3", "G2", "D3", "F2", "C3", "D3", "A2", "E2", "E2", "A2", "A2"]], kind="pulse"), 0.33)
    # Battle: a driving riff in B minor (new). Loops.
    lead = [("B4", 0.5), ("B4", 0.25), ("B4", 0.25), ("D5", 0.5), ("B4", 0.5), ("A4", 0.5), ("F#4", 0.5), ("G4", 0.5), ("A4", 0.5),
            ("B4", 0.5), ("B4", 0.25), ("B4", 0.25), ("E5", 0.5), ("D5", 0.5), ("C#5", 0.5), ("A4", 0.5), ("B4", 1),
            ("F#5", 0.5), ("F#5", 0.25), ("F#5", 0.25), ("A5", 0.5), ("F#5", 0.5), ("E5", 0.5), ("C#5", 0.5), ("D5", 0.5), ("E5", 0.5),
            ("F#5", 0.5), ("G5", 0.5), ("F#5", 0.5), ("E5", 0.5), ("D5", 0.5), ("C#5", 0.5), ("B4", 1)]
    bass = [(n, 0.5) for n in ["B1", "B2"] * 8 + ["F#1", "F#2"] * 4 + ["B1", "B2"] * 4]
    write("BattleMusic.wav", tune(150, lead, bass), 0.4)
    # The ending: the title's call, in E major and at peace (new). Loops.
    write("Ending.wav", tune(78,
        [("E4", 1), ("B4", 1), ("E5", 1.5), ("F#5", 0.5), ("G#5", 1), ("F#5", 1), ("E5", 2),
         ("C#5", 1), ("E5", 1), ("A5", 1.5), ("G#5", 0.5), ("F#5", 1), ("D#5", 1), ("B4", 2),
         ("E5", 1), ("G#5", 1), ("B5", 1.5), ("A5", 0.5), ("G#5", 1), ("F#5", 1), ("E5", 1), ("C#5", 1),
         ("B4", 1), ("C#5", 1), ("D#5", 1), ("F#5", 1), ("E5", 4)],
        [(n, 2) for n in ["E3", "E3", "A2", "B2", "A2", "A2", "B2", "B2", "E3", "E3", "A2", "A2", "B2", "B2", "E3", "E3"]], kind="tri"), 0.5)


if __name__ == "__main__":
    main()
