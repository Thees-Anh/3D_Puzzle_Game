"""Generate lightweight original WAV placeholders for the Escape Room audio pass."""
import math, os, random, struct, wave

ROOT = os.path.dirname(os.path.abspath(__file__))
SR = 44100
random.seed(527)

def write(path, samples):
    full = os.path.join(ROOT, path)
    os.makedirs(os.path.dirname(full), exist_ok=True)
    peak = max(1.0, max(abs(x) for x in samples))
    data = b''.join(struct.pack('<h', int(max(-1, min(1, x / peak * .92)) * 32767)) for x in samples)
    with wave.open(full, 'wb') as f:
        f.setnchannels(1); f.setsampwidth(2); f.setframerate(SR); f.writeframes(data)

def synth(duration, voices=(), noise=0, attack=.01, release=.12, tremolo=0):
    n = int(duration * SR); out=[]
    for i in range(n):
        t=i/SR
        env=min(1,t/max(.001,attack))*min(1,(duration-t)/max(.001,release))
        s=sum(a*math.sin(2*math.pi*f*t+p) for f,a,p in voices)
        if noise: s += noise*(random.random()*2-1)
        if tremolo: s *= .78+.22*math.sin(2*math.pi*tremolo*t)
        out.append(s*max(0,env))
    return out

def sweep(duration, f0, f1, amp=.6, noise=0, release=.1):
    n=int(duration*SR); out=[]
    for i in range(n):
        t=i/SR; q=t/duration; f=f0+(f1-f0)*q; phase=2*math.pi*(f0*t+(f1-f0)*t*t/(2*duration))
        env=min(1,t/.01)*min(1,(duration-t)/release)
        out.append((amp*math.sin(phase)+noise*(random.random()*2-1))*max(0,env))
    return out

def concat(*parts):
    out=[]
    for p in parts: out.extend(p)
    return out

silence=lambda d:[0.0]*int(d*SR)

# UI and electronic feedback
write('SFX/UI/ui_hover.wav', synth(.07, [(920,.35,0)], release=.04))
write('SFX/UI/ui_click.wav', concat(synth(.045,[(520,.5,0)],noise=.08,release=.03), synth(.04,[(760,.25,0)],release=.03)))
write('SFX/UI/ui_open.wav', sweep(.18,360,720,.38,release=.08))
write('SFX/UI/ui_close.wav', sweep(.16,650,300,.35,release=.08))
write('SFX/UI/ui_pause.wav', concat(synth(.09,[(660,.35,0)]),silence(.03),synth(.12,[(440,.4,0)])))
write('SFX/UI/ui_resume.wav', concat(synth(.08,[(440,.35,0)]),silence(.02),synth(.12,[(660,.4,0)])))
write('SFX/UI/confirm.wav', concat(synth(.10,[(523,.38,0)]),synth(.16,[(784,.45,0)])))
write('SFX/UI/error.wav', concat(synth(.12,[(240,.45,0),(120,.18,0)]),silence(.035),synth(.18,[(190,.5,0),(95,.2,0)])))

# Physical interactions. These are deliberately dry, short wooden knocks rather
# than broad-band rustling, so they read as shoes striking wooden floorboards.
def wood_step(variation):
    duration=.18; n=int(duration*SR); out=[]
    f1=135+variation*13; f2=285+variation*17; f3=510+variation*23
    for i in range(n):
        t=i/SR
        body=(.72*math.sin(2*math.pi*f1*t)*math.exp(-27*t)+
              .34*math.sin(2*math.pi*f2*t+.4)*math.exp(-38*t)+
              .16*math.sin(2*math.pi*f3*t+.8)*math.exp(-52*t))
        transient=(random.random()*2-1)*.22*math.exp(-95*t)
        # A quiet heel/toe follow-up gives a compact voxel-like double clack.
        u=t-.055
        toe=0 if u<0 else (.24*math.sin(2*math.pi*(f2+35)*u)*math.exp(-48*u))
        out.append(body+transient+toe)
    return out
for i in range(3):
    write(f'SFX/Player/footstep_wood_{i+1}.wav', wood_step(i))
write('SFX/Interaction/pickup_uv.wav', concat(synth(.07,[(410,.22,0)],noise=.22),synth(.13,[(690,.32,0)])))
write('SFX/Interaction/pickup_key.wav', concat(synth(.05,[(1800,.28,0),(2700,.14,0)]),silence(.025),synth(.14,[(2200,.22,0)],release=.12)))
write('SFX/Interaction/pickup_fuse.wav', concat(synth(.055,[(900,.3,0)],noise=.18),synth(.12,[(1250,.24,0)])))
write('SFX/Interaction/switch_on.wav', concat(synth(.045,[(700,.3,0)],noise=.28),sweep(.16,480,1050,.2)))
write('SFX/Interaction/switch_off.wav', concat(synth(.045,[(620,.3,0)],noise=.28),sweep(.13,760,330,.18)))
write('SFX/Door/door_locked.wav', concat(synth(.06,[(120,.35,0)],noise=.38),silence(.05),synth(.09,[(105,.3,0)],noise=.28)))
write('SFX/Door/door_open.wav', sweep(.85,115,68,.34,noise=.16,release=.25))
write('SFX/Door/door_close.wav', concat(sweep(.55,90,58,.3,noise=.13),synth(.13,[(72,.6,0)],noise=.3)))
write('SFX/Door/lock_release.wav', concat(synth(.09,[(420,.35,0)],noise=.32),silence(.04),synth(.18,[(680,.3,0),(1020,.12,0)])))
write('SFX/Cabinet/cabinet_open.wav', sweep(.62,150,82,.28,noise=.22,release=.2))
write('SFX/Cabinet/key_unlock.wav', concat(synth(.08,[(1500,.25,0)],noise=.18),silence(.05),synth(.15,[(900,.3,0),(1800,.12,0)])))
write('SFX/Hanoi/book_select.wav', synth(.18,[(180,.18,0)],noise=.35,release=.14))
write('SFX/Hanoi/book_place.wav', concat(synth(.045,[(110,.42,0)],noise=.38),synth(.13,[(72,.25,0)],release=.12)))
write('SFX/Hanoi/drawer_open.wav', sweep(.85,105,58,.3,noise=.24,release=.22))
write('SFX/Hanoi/mechanism_unlock.wav', concat(synth(.10,[(310,.3,0)],noise=.35),silence(.06),synth(.12,[(470,.35,0)],noise=.25),silence(.05),synth(.28,[(700,.3,0)])))

# Safe and electrical puzzle
write('SFX/Safe/keypad.wav', synth(.075,[(980,.38,0),(1960,.12,0)],release=.055))
write('SFX/Safe/safe_unlock.wav', concat(synth(.12,[(135,.38,0)],noise=.3),silence(.05),synth(.22,[(420,.32,0)],noise=.12)))
write('SFX/Safe/safe_door.wav', sweep(1.0,95,48,.42,noise=.2,release=.3))
for i,f in enumerate((620,710,800),1):
    write(f'SFX/Electrical/circuit_click_{i}.wav', concat(synth(.035,[(f,.34,0)],noise=.18),synth(.07,[(f*1.5,.2,0)])))
write('SFX/Electrical/fuse_insert.wav', concat(synth(.06,[(560,.3,0)],noise=.3),silence(.03),synth(.16,[(1100,.3,0),(2200,.1,0)])))
write('SFX/Electrical/grid_complete.wav', concat(sweep(.3,280,900,.32),synth(.45,[(440,.24,0),(660,.2,0),(880,.16,0)])))
write('SFX/Electrical/output_activate.wav', concat(synth(.08,[(520,.26,0)]),sweep(.18,600,980,.28)))
write('SFX/Electrical/power_restored.wav', concat(sweep(.85,55,240,.42,noise=.12,release=.2),synth(1.2,[(120,.24,0),(240,.16,0),(480,.08,0)],tremolo=6,release=.5)))
write('SFX/Symbols/symbol_press.wav', concat(synth(.045,[(260,.35,0)],noise=.25),synth(.11,[(520,.22,0)])))
write('SFX/Symbols/symbol_complete.wav', concat(synth(.18,[(330,.28,0)]),synth(.18,[(494,.3,0)]),synth(.45,[(740,.36,0),(1110,.12,0)])))

# Loops use integer-related frequencies for clean boundaries.
def ambience(duration, powered=False):
    n=int(duration*SR); out=[]
    for i in range(n):
        t=i/SR
        base=.08*math.sin(2*math.pi*(60 if powered else 40)*t)
        air=.025*(random.random()*2-1)
        pulse=(.018 if powered else .012)*math.sin(2*math.pi*.2*t)*math.sin(2*math.pi*120*t)
        out.append(base+air+pulse)
    return out
write('Ambience/room_dark.wav', ambience(20,False))
write('Ambience/room_powered.wav', ambience(20,True))

def music(duration, victory=False):
    notes = ([261.63,329.63,392.0,523.25] if victory else [110,130.81,164.81,196])
    n=int(duration*SR); out=[]
    beat=1.0 if victory else 3.0
    for i in range(n):
        t=i/SR; idx=int(t/beat)%len(notes); f=notes[idx]
        q=(t%beat)/beat; env=(math.sin(math.pi*q)**2)*(.24 if victory else .10)
        s=env*(math.sin(2*math.pi*f*t)+.35*math.sin(2*math.pi*2*f*t))
        if not victory: s += .035*math.sin(2*math.pi*55*t)
        out.append(s)
    return out
write('Music/Gameplay/mystery_loop.wav', music(24,False))
write('Music/Victory/victory_theme.wav', music(6,True))

print('Generated Escape Room audio assets in', ROOT)
