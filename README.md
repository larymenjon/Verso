# Verso

> Flip the world. Cross to the other side. Find out the other side was always you.

**Verso** is a cozy-spooky 3D puzzle platformer made in **Unity 6 (URP)** for a 2-day solo game jam with the theme **"The Other Side"**.
Every floating island exists twice, in the world of the **Living** and in the **Beyond**. One button flips between them:
bridges vanish, platforms appear, spikes move to the other side. Gather the lost souls, grab the key and reach the portal.


## Features

- **One mechanic, explored to the end:** flip between two overlapping worlds with a single key.
- **5 islands** that ramp up in difficulty: flip basics, mid-air flips, flip rhythm through spikes, and the Echo puzzle.
- **The Echo:** a ghost that repeats your moves 1.5 s later, on the opposite side, and presses plates you can't reach.
- **3 lives** per run, with a Game Over screen.
- **Voiced intro** (English and Brazilian Portuguese) and in-game tips from Wisp, a lost soul who guides you.
- **Settings:** language (English / Português), music and effects volume, saved between sessions.
- Hand-made characters in Blender (Day, Night, the soul and the key), PSX-style skies, and a cute spooky soundtrack.

## Controls

| Action | Keyboard / Mouse | Gamepad |
|---|---|---|
| Move | WASD / arrows | Left stick |
| Camera | Mouse | Right stick |
| Jump | Space | A / Cross |
| Flip the world | F or right click | Y / Triangle |
| Restart island | R | Select |
| Pause | Esc | Start |

## How to open

1. Clone the repo and open the folder in **Unity 6000.6** (URP).
2. Wait for scripts to compile.
3. Menu **Verso > Gerar Projeto (cenas e prefabs)** builds layers, materials, prefabs, all scenes and the Build list.
4. Open `Assets/Verso/Scenes/MainMenu.unity` and press Play.

Other editor menus under **Verso**:

| Menu | What it does |
|---|---|
| Aplicar UI (Figma) | Rebuilds the HUD/pause/game over prefab, main menu, credits and end screen |
| Aplicar Personagens 3D | Imports the Blender FBX characters, Animator, soul and key |
| Criar Intro (diálogo) | Rebuilds the dialogue scene before Island 1 |
| Baixar vozes da intro (ElevenLabs) | Downloads the intro voice lines |
| Aplicar Céu e Músicas | PSX skyboxes, island music, intro/end music and sound effects |
| Gravar demo (vídeo + screenshots) | Autopilot playthrough recorded to `Recordings/` |

## Project structure

```
Assets/Verso/
  Audio/        Music, SFX, voice lines
  Characters/   FBX models (Day, Night, Soul, Key) + palette textures
  Prefabs/      Player, Echo, Crystal (soul), Portal, Door, Plate, Spikes, GameUI, Systems
  Scenes/       MainMenu, Intro, Island1..5, End
  Scripts/
    Core/       SceneFlow, GameInput, WorldType, Lives, GameSettings, Loc
    World/      WorldManager (flip), WorldObject (Day/Night/Both)
    Player/     PlayerController, ThirdPersonCamera, CharacterVisual
    Gameplay/   Collectible, IslandManager, PortalExit, Hazard
    Echo/       EchoGhost, PressurePlate, Door, EndingSequence
    UI/         GameUI, MainMenuUI, IntroDialogue, SettingsUI, CreditsRoll, LocalizedText
    Editor/     Builders that generate scenes, UI and assets
  Sky/          PSX skybox panoramas
  UI/           Sprites and fonts (from the Figma file)
```

## How the flip works

`WorldManager` keeps the current world. Every level object has a `WorldObject` (Day, Night or Both) that sets its physics layer
and rendering. Before flipping, the manager tests the player's capsule against the solids of the target world; if it would end up
inside something, the flip is refused with a shake. That rule is what makes mid-air flips a mechanic: jump above a platform that
only exists on the other side, flip, and land on top of it.

## Credits

- **Design, art and code:** Laryssa Aguiar Menjon
- **Music:** HAI Soundworks, [haisoundworks.com](https://haisoundworks.com) (Boo & Breakfast)
- **Sound effects (Pixabay):** Aryan Raikundalia (jump), Universfield (soul), Xmer™ (key), CRYUG CEET (level clear),
  Hanifi Şahin (game over), 666HeroHero (click), u_uev3l0tolc (hover)
- **Skies:** PSX Sky Sampler by Munchkin Gaming
- **Voices:** ElevenLabs
- **Fonts:** Lilita One and Fredoka (SIL Open Font License)

Third-party audio and skies are used under their own licenses and are not covered by this repository's license.
