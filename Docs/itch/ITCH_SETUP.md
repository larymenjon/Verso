# Montando a página no itch.io

## Tamanhos das imagens

| Peça | Tamanho | Observação |
|---|---|---|
| Capa (Cover image) | **630 x 500** | proporção 315:250 (mínimo 315x250). Aparece nas listagens. Até 3 MB. |
| Capa animada (opcional) | 630 x 500 GIF | aparece quando passam o mouse na listagem |
| Banner (Edit theme > Banner) | **960 x 300** | a coluna da página tem 960 px; altura até ~400 |
| Fundo (Edit theme > Background) | **1920 x 1080** | máx. 3840x2160; fica atrás da coluna central |
| Screenshots | **1920 x 1080** | qualquer tamanho; aparecem com 347 px de largura na lateral. 3 a 5. Até 3 MB cada |
| Vídeo / trailer | link do YouTube | o itch só embute vídeo por link (YouTube/Vimeo) |

Prontos nesta pasta: `cover_630x500.png`, `banner_960x300.png` e `background_1920x1080.png`.
Os screenshots (`screenshot_01..05.png`) e o vídeo narrado (`verso_gameplay_en.mp4`) saem depois da gravação da demo.
O vídeo vai no YouTube e o link entra em "Gameplay video or trailer".

As artes editáveis estão no Figma, página **itch.io** (frames `itch/Cover 630x500`, `itch/Banner 960x300`,
`itch/Background 1920x1080`). Os retângulos laranja `Render/...` são o lugar dos personagens: selecione cada um e
arraste o PNG correspondente de `Docs/itch/renders` (Fill > Image > Fit). Os PNGs desta pasta já saem com eles aplicados.

O fundo fica atrás da coluna de 960 px da página, por isso os personagens estão nas laterais (x < 480 e x > 1440).

## Vídeo de gameplay

1. Game view em 16:9, 1920x1080. Não mexa no mouse/teclado durante a gravação.
2. **Verso > Gravar demo (vídeo + screenshots)**. O jogo se joga sozinho e grava em `Recordings/`.
3. **Verso > Baixar narração do vídeo (ElevenLabs)**. Baixa os 13 trechos em inglês para `Docs/itch/narration`.
   Os links valem cerca de 2 horas; se der erro 403, peça links novos.
4. Com o vídeo, o `demo_timeline.json` e a narração prontos, a montagem final (narração sincronizada e música abaixada) é feita com ffmpeg.

## Passo a passo

1. **Create new project** > Kind of project: **HTML** (WebGL) ou **Downloadable** (Windows).
2. Title: **Verso**. Short description: *Flip the world. Cross to the other side.*
3. Classification: Games. Genre: **Puzzle** / Platformer. Tags: `3d`, `puzzle-platformer`, `cute`, `halloween`, `low-poly`, `game-jam`.
4. Cover image: `cover_630x500.png`. Screenshots: os 5 PNGs.
5. Description: cole o texto de `ITCH_PAGE.md`.
6. Uploads: build WebGL zipada (marque "This file will be played in the browser", viewport 1280x720, fullscreen button)
   e/ou build Windows zipada.
7. Salve, abra **Edit theme**: banner, background, cores (fundo `#160F26`, texto `#FFF2D9`, links `#FFB347`, botões `#FF9E0E`).
8. Na jam: Submit your project e escreva o tema em uma linha: *Two worlds in the same place, and the other side is you.*
