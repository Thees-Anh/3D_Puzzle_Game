# Escape Room Audio Mixer setup

The runtime audio system works without a mixer and already exposes category volume methods.
To route it through a Unity AudioMixer:

1. Create `Assets/Audio/Mixers/EscapeRoomAudioMixer.mixer`.
2. Keep the Master group and add child groups: `MUSIC`, `SFX`, `AMBIENCE`, `UI`.
3. Expose each group attenuation as: `MasterVolume`, `MusicVolume`, `SFXVolume`, `AmbienceVolume`, `UIVolume`.
4. Select `AUDIO_System_v1` and assign the mixer and four child groups to AudioManager.

Future Settings sliders can call `SetMasterVolume`, `SetMusicVolume`, `SetSfxVolume`,
`SetAmbienceVolume`, and `SetUIVolume` with values from 0 to 1.
