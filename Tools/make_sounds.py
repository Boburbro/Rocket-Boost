#!/usr/bin/env python3
"""Rocket Boost o'yini uchun 3 ta ovozni sintez qiladi va Assets/Audio ichiga yozadi.

Ishga tushirish:  python3 Tools/make_sounds.py
Fayl nomlari o'zgarmaydi (Engine.wav, Crash.wav, Success.wav), shuning uchun
sahnalardagi ulanishlar joyida qoladi. Ovozni o'zgartirish uchun pastdagi
raqamlarni o'zgartirib, skriptni qayta ishga tushiring.
"""
import math
import os
import random
import struct
import wave

SR = 44100
OUT_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Assets", "Audio")


def lowpass(samples, cutoff):
    """Bir qutbli past chastota filtri. cutoff — son yoki har bir namuna uchun funksiya."""
    out, y = [], 0.0
    for i, x in enumerate(samples):
        fc = cutoff(i) if callable(cutoff) else cutoff
        a = 1.0 - math.exp(-2.0 * math.pi * fc / SR)
        y += a * (x - y)
        out.append(y)
    return out


def normalize(samples, peak):
    top = max(abs(s) for s in samples) or 1.0
    return [s * peak / top for s in samples]


def fade(samples, fade_in=0.0, fade_out=0.0):
    n = len(samples)
    a, b = int(fade_in * SR), int(fade_out * SR)
    for i in range(min(a, n)):
        samples[i] *= i / a
    for i in range(min(b, n)):
        samples[n - 1 - i] *= i / b
    return samples


def save(name, samples):
    path = os.path.join(OUT_DIR, name)
    with wave.open(path, "wb") as f:
        f.setnchannels(1)
        f.setsampwidth(2)
        f.setframerate(SR)
        f.writeframes(b"".join(
            struct.pack("<h", int(max(-1.0, min(1.0, s)) * 32767)) for s in samples))
    print("yozildi:", os.path.normpath(path), "(%.2f s)" % (len(samples) / SR))


def engine():
    """Dvigatel: past gurillash + shovqin. 2 soniyalik, uzluksiz takrorlanadigan bo'lak."""
    rng = random.Random(1)
    length, overlap = 2.0, 0.3
    n = int(length * SR) + int(overlap * SR)
    white = [rng.uniform(-1, 1) for _ in range(n)]

    rumble = normalize(lowpass(lowpass(white, 180), 180), 1.0)          # past gurillash
    mid = lowpass(white, 2600)
    low = lowpass(white, 500)
    hiss = normalize([m - l for m, l in zip(mid, low)], 1.0)            # alanga shovqini

    out = []
    for i in range(n):
        t = i / SR
        # Butun sonli davrlar: 2 soniyada aniq takrorlanadi
        flutter = 1.0 + 0.18 * math.sin(2 * math.pi * 7 * t) + 0.08 * math.sin(2 * math.pi * 19 * t)
        tone = (0.55 * math.sin(2 * math.pi * 55 * t)
                + 0.30 * math.sin(2 * math.pi * 82.5 * t)
                + 0.18 * math.sin(2 * math.pi * 110 * t))
        s = (0.9 * rumble[i] + 0.35 * hiss[i]) * flutter + 0.5 * tone
        out.append(math.tanh(1.6 * s))

    # Oxirini boshiga silliq qo'shamiz, shunda takrorlanganda "tiq" etmaydi
    body, k = int(length * SR), int(overlap * SR)
    loop = out[:body]
    for i in range(k):
        w = i / k
        loop[i] = out[i] * w + out[body + i] * (1.0 - w)
    return normalize(loop, 0.75)


def crash():
    """Urilish: past zarba + so'nib boruvchi portlash shovqini + mayda parchalar."""
    rng = random.Random(2)
    n = int(1.7 * SR)
    white = [rng.uniform(-1, 1) for _ in range(n)]
    # Portlash: filtr 7000 Hz dan 180 Hz gacha tushadi
    blast = lowpass(white, lambda i: 180 + 6800 * math.exp(-(i / SR) / 0.16))
    blast = normalize(blast, 1.0)

    # Parchalar: siyrak tasodifiy "chirs"lar
    crackle = [0.0] * n
    for _ in range(140):
        i = int(rng.uniform(0.05, 1.3) * SR)
        crackle[i] = rng.uniform(-1, 1)
    crackle = normalize(lowpass(crackle, 3500), 1.0)

    out, phase = [], 0.0
    for i in range(n):
        t = i / SR
        freq = 38 + 120 * math.exp(-t / 0.09)          # zarba: 158 Hz -> 38 Hz
        phase += 2 * math.pi * freq / SR
        thump = math.sin(phase) * math.exp(-t / 0.28)
        s = (1.1 * thump
             + 1.0 * blast[i] * math.exp(-t / 0.33)
             + 0.45 * crackle[i] * math.exp(-t / 0.6))
        out.append(math.tanh(2.2 * s))
    return fade(normalize(out, 0.9), 0.002, 0.25)


def success():
    """Finish: yuqoriga ko'tariluvchi qo'ng'iroqsimon ohang (C-E-G-C) va yengil aks-sado."""
    n = int(1.9 * SR)
    out = [0.0] * n
    notes = [(523.25, 0.00), (659.25, 0.11), (783.99, 0.22), (1046.50, 0.33), (1567.98, 0.46)]
    for idx, (freq, start) in enumerate(notes):
        last = idx >= 3
        decay = 0.75 if last else 0.35
        gain = 0.55 if idx == 4 else 1.0
        for i in range(int(start * SR), n):
            t = i / SR - start
            env = min(1.0, t / 0.006) * math.exp(-t / decay)
            wave_sum = (math.sin(2 * math.pi * freq * t)
                        + 0.5 * math.sin(2 * math.pi * freq * 1.004 * t)   # yengil "jimirlash"
                        + 0.28 * math.sin(2 * math.pi * freq * 2 * t)
                        + 0.10 * math.sin(2 * math.pi * freq * 3 * t))
            out[i] += gain * env * wave_sum

    # Aks-sado
    for delay, amount in ((0.17, 0.32), (0.34, 0.14)):
        d = int(delay * SR)
        for i in range(n - 1, d - 1, -1):
            out[i] += out[i - d] * amount
    return fade(normalize(out, 0.8), 0.0, 0.3)


if __name__ == "__main__":
    save("Engine.wav", engine())
    save("Crash.wav", crash())
    save("Success.wav", success())
