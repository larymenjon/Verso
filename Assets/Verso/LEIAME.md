# Verso · blockout de mecânicas

## Como gerar
1. Abra o projeto na Unity e espere compilar.
2. Menu **Verso > Gerar Projeto (cenas e prefabs)**.
3. Abre a cena `MainMenu`. Aperte Play.

O gerador cria layers (Player, WorldDay, WorldNight), materiais, prefabs e as cenas
`MainMenu`, `Island1`, `Island2`, `Island3`, `End` em `Assets/Verso`, e já coloca tudo no Build.
Rodar de novo sobrescreve as cenas geradas (os scripts não são tocados).

## Controles
WASD/analógico mover, mouse/analógico direito câmera, Espaço/A pular,
F ou clique direito/Y virar o mundo, R/Select reiniciar ilha, Esc/Start pausa.

## Scripts
| Script | Papel |
|---|---|
| WorldManager | Estado Dia/Noite, flip (0,6 s de transição, 0,5 s de cooldown), checagem de overlap antes de virar, cor/luz/névoa |
| WorldObject | Marca objeto como Dia, Noite ou Ambos. Muda layer e render. `ghostWhenInactive` mostra fantasma do outro lado |
| PlayerController | CharacterController, pulo com coyote/buffer, respawn ao cair (killY) |
| ThirdPersonCamera | Câmera orbital com colisão e tremor (no lugar do Cinemachine) |
| Collectible / IslandManager / PortalExit | Cristais, contagem e portal que libera com todos os cristais |
| Hazard | Espinho, respawn ao tocar no mundo ativo |
| EchoGhost | Repete o jogador com 1,5 s de atraso, sempre no mundo oposto |
| PressurePlate / Door | Placa pressionada por quem existe naquele mundo (jogador ou Eco); porta com latch ou fechamento com atraso |
| EndingSequence | Eco se aproxima, os dois se fundem, fade branco, cena final |
| GameUI | HUD (cristais, mundo atual), mensagens, pausa, fade |
| AudioManager | Música Dia/Noite com crossfade e SFX por tipo. Arraste os clips no prefab `Systems` |

## Trocar o visual depois
Os prefabs em `Assets/Verso/Prefabs` (Player, Crystal, Portal, Door, PressurePlate, Echo, Spikes, GameUI)
podem receber modelos e arte sem mudar os scripts. Nas cenas, os blocos do cenário são cubos com
`WorldObject`; troque o mesh/material e mantenha o componente.

## UI (Figma "Verso · UI")
Sprites em `Assets/Verso/UI/Sprites`, fontes Lilita One e Fredoka (OFL) em `Assets/Verso/UI/Fonts`.
Menu **Verso > Aplicar UI (Figma)** refaz o prefab GameUI e a interface das cenas MainMenu e End
sem mexer nas ilhas. `VersoUIBuilder` monta tudo; `UIButtonFx` dá o feedback dos botões e `UIFloat` faz o logo e as almas flutuarem.
Os mundos aparecem na UI como **VIVOS** (Dia) e **ALÉM** (Noite).

## Personagens 3D (Dia e Noite)
Modelos feitos no Blender em `Assets/Verso/Characters` (`Verso_Dia.fbx`, `Verso_Noite.fbx` e as paletas).
Menu **Verso > Aplicar Personagens 3D** configura a importação (Rig Generic, loops), cria os materiais
`M_Char_Dia`/`M_Char_Noite`, o Animator `AC_Verso` e troca as cápsulas dos prefabs Player e Echo pelos modelos.
`CharacterVisual` mostra o Dia ou a Noite conforme o mundo (o Eco sempre mostra o lado oposto) e alimenta o Animator:
Speed, Grounded, VSpeed, Jump (evento `Jumped`), Flip (evento `WorldChanged`) e Celebrate (`CharacterVisual.Celebrate(true)`).
Clipes: Idle, Run, Jump, Fall, Flip, Celebrate. **Gerar Projeto** já chama esse passo no final.

## Ícones novos (Figma, página Estilo e Componentes)
`icon_pumpkin`, `icon_soul` e `icon_moon` foram trocados pela arte nova. Entraram também `icon_soul_empty`
(fantasma apagado), `icon_grave` e `icon_cemetery`, prontos pra usar.

## Menu principal
Fundo novo (`menu_background.png`) e título novo (`logo.png`) vêm do componente `Art/Menu Background` do Figma.
Layout centralizado igual à tela "01 · Menu principal": título no topo, subtítulo e os botões Jogar/Sair embaixo.

## Chave do fim da ilha
Ao juntar todas as almas, a chave dourada (`Verso_Chave.fbx`) aparece girando no portal. O jogador pega a chave,
comemora e vai pra próxima ilha (no portal final, dispara o Eco). Fica no prefab `Portal`, campo `keyVisual` do `PortalExit`.
**Aplicar Personagens 3D** coloca a chave no prefab.

## Vidas e fim de jogo
3 vidas por partida (`Lives`). Cair ou encostar num perigo gasta uma vida e volta ao spawn; os 3 Lumes embaixo do contador mostram o que sobrou.
Sem vidas, abre a caixa "FIM DE JOGO" com Tentar de novo (reinicia a ilha com 3 vidas) e Menu principal. Jogar no menu também zera pra 3.
Reiniciar a ilha com R não gasta vida.

## Caixa de pausa
`pause_panel.png` (a caixa nova, sem os botões desenhados) substitui `panel.png` + `banner.png`. Título na tábua de cima,
botões de verdade nos mesmos lugares dos desenhados. A tela de fim de jogo usa a mesma caixa.

## Música
- Menu: `Audio/Music/menu_frankensteins_lab.ogg`, tocando num AudioSource no MenuUI (volume 0,55).
- Ilhas: arraste as músicas em `Music Day` e `Music Night` do `AudioManager` no prefab `Systems` (ainda vazios).
- Intro: AudioSource `Music` dentro do IntroUI, volume 0,12. Arraste o clip nele.

## Vozes da intro (ElevenLabs)
Lume: Wesley Bessa · Alminha: Bianca · ???: Cassian (sussurrando). Modelo eleven_v4.
Menu **Verso > Baixar vozes da intro (ElevenLabs)** baixa os 14 mp3 de `Audio/Voice/vozes_intro.json` e refaz a cena Intro com as vozes ligadas.
Os links expiram cerca de 2 h depois de gerados; se der 403, peça links novos.

## Créditos
Botão pequeno "Créditos" embaixo do menu abre a caixa (mesma arte da pausa). Texto em `VersoUIBuilder.CreditsText`.

## Céu (skybox PSX)
`Sky/Sky_Vivos_SunsetOrange.png` (Vivos) e `Sky/Sky_Alem_FullMoonNight.png` (Além), do pacote PSX Sky Sampler (Munchkin Gaming).
Materiais `M_Sky_Vivos`/`M_Sky_Alem` (Skybox/Panoramic, filtro Point). O `WorldManager` troca o céu no meio da virada, com uma escurecida,
e a névoa usa a cor do horizonte de cada céu. Menu **Verso > Aplicar Céu e Músicas**.

## Músicas e efeitos por momento
Música: Boo & Breakfast (HAI Soundworks), em `Audio/Music`:
- Ilha nos Vivos: `ilha_vivos.mp3` (14 · Pancakes for Poltergeists)
- Ilha no Além: `ilha_alem.mp3` fica guardada mas não toca: ao virar o mundo a música continua a mesma (`keepMusicOnFlip` no AudioManager)
- Diálogo da intro: `intro_dialogo.mp3` (15 · The Lonely Little Lantern), volume 0,12
- Tela final: `final.mp3` (22 · Farewell Hollowgate)
- Menu: `menu_frankensteins_lab.ogg`

Efeitos (Pixabay), em `Audio/SFX`: `sfx_pulo.mp3` (pulo, Aryan Raikundalia), `sfx_alma.mp3` (alma, Universfield), `sfx_coleta.mp3` (chave, Xmer™, cortado em 0,7 s),
`sfx_fim_de_jogo.mp3` (toca uma vez no fim de jogo) e `sfx_passou_de_fase.mp3` (ao entrar no portal, CRYUG CEET).
**Verso > Aplicar Céu e Músicas** liga tudo no `AudioManager` (prefab Systems) e nas cenas Intro e End.

## Idioma e volume (engrenagem)
O jogo abre em **inglês**. A engrenagem (canto de cima do menu e da pausa) abre as Configurações:
idioma English/Português, volume da Música e dos Efeitos (as vozes seguem os Efeitos). Tudo salvo em PlayerPrefs (`GameSettings`).
- Textos: `Loc.cs` tem a tabela português → inglês. Todo texto fixo criado pelo builder ganha `LocalizedText` sozinho;
  texto novo é só acrescentar uma linha na tabela.
- Intro: cada fala tem `text` (PT) e `textEn`; vozes em `voices` (PT) e `voicesEn`. Em inglês a Alminha vira **Wisp**.
- Vozes em inglês (ElevenLabs): Lume = VALF, Wisp = Veda Sky, ??? = Cassian. Arquivos `intro_en_01..14.mp3`.
- Volume: `AudioManager` (músicas das ilhas e efeitos), `VolumeBinder` (música do menu e do final), `IntroDialogue` (música e voz da intro).

## Sons dos botões
`Resources/UI/ui_click.mp3` (clique, 666HeroHero) e `ui_hover.mp3` (passar o mouse ou navegar pelo teclado, u_uev3l0tolc).
Tocados pelo `UIAudio` a partir do `UIButtonFx`, em qualquer cena, com o volume de Efeitos.

## Ilhas (5, cada vez mais difíceis)
1. **Despertar**: ensina o flip (caminho só da Noite, ponte e parede só do Dia).
2. **Travessia**: degraus que trocam de lado (pular e virar no ar) e espinhos do Além.
3. **Salto**: o bloco do outro lado ocupa o lugar do jogador; virar parado é recusado, então pula e vira no ar pra cair em cima.
4. **Ritmo**: degraus menores e mais longe trocando de lado + corredor com espinhos dos dois mundos alternados.
5. **Eco** (final): o Eco aperta as placas do outro lado; portal final dispara o encontro.

Cada ilha começa com dicas da Alminha (caixa embaixo com o retrato dela, PT/EN), só na primeira vez que a ilha abre na partida.
As falas ficam em `tipsPt`/`tipsEn` do `IslandManager` (definidas no `VersoSceneBuilder`). O jogador fica parado até fechar.
Fim de jogo > Tentar de novo volta pra Ilha 1 com 3 vidas.
