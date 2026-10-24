using System;
using System.Collections.Generic;

namespace Verso
{
    /// <summary>
    /// Traduções. O texto-base (chave) é o português que já está nas cenas e prefabs;
    /// em inglês, Loc.Tr devolve a versão em inglês. Texto sem tradução passa direto.
    /// Pra traduzir algo novo, é só acrescentar uma linha em Table.
    /// </summary>
    public static class Loc
    {
        public const string CreditsPt =
            "<size=96><b>VERSO</b></size>\n\n\n" +
            "<color=#FFB347><b>Criação, arte e programação</b></color>\nLaryssa Aguiar Menjon\n\n" +
            "<color=#FFB347><b>Música</b></color>\nHAI Soundworks\nhaisoundworks.com\n\n" +
            "<color=#FFB347><b>Efeitos sonoros (Pixabay)</b></color>\nPulo · Aryan Raikundalia\nAlma · Universfield\nChave · Xmer™\nPassou de fase · CRYUG CEET\nFim de jogo · Hanifi Şahin\nClique · 666HeroHero\nHover · u_uev3l0tolc\n\n" +
            "<color=#FFB347><b>Céu</b></color>\nPSX Sky Sampler · Munchkin Gaming\n\n" +
            "<color=#FFB347><b>Vozes</b></color>\nElevenLabs\n\n" +
            "<color=#FFB347><b>Fontes</b></color>\nLilita One e Fredoka\nSIL Open Font License\n\n" +
            "\n<b>Obrigada por jogar!</b>";

        public const string CreditsEn =
            "<size=96><b>VERSO</b></size>\n\n\n" +
            "<color=#FFB347><b>Created, art and code by</b></color>\nLaryssa Aguiar Menjon\n\n" +
            "<color=#FFB347><b>Music by</b></color>\nHAI Soundworks\nhaisoundworks.com\n\n" +
            "<color=#FFB347><b>Sound effects (Pixabay)</b></color>\nJump · Aryan Raikundalia\nSoul · Universfield\nKey · Xmer™\nLevel clear · CRYUG CEET\nGame over · Hanifi Şahin\nClick · 666HeroHero\nHover · u_uev3l0tolc\n\n" +
            "<color=#FFB347><b>Sky</b></color>\nPSX Sky Sampler · Munchkin Gaming\n\n" +
            "<color=#FFB347><b>Voices</b></color>\nElevenLabs\n\n" +
            "<color=#FFB347><b>Fonts</b></color>\nLilita One and Fredoka\nSIL Open Font License\n\n" +
            "\n<b>Thanks for playing!</b>";

        private static readonly Dictionary<string, string> Table = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            // Menu
            { "Vire o mundo. Cruze para o outro lado.", "Flip the world. Cross to the other side." },
            { "Jogar", "Play" },
            { "Sair", "Quit" },
            { "Créditos", "Credits" },
            { "Voltar", "Back" },
            { CreditsPt, CreditsEn },
            // Configurações
            { "Configurações", "Settings" },
            { "Idioma", "Language" },
            { "Música", "Music" },
            { "Efeitos", "Effects" },
            // HUD
            { "VIVOS", "LIVING" },
            { "ALÉM", "BEYOND" },
            { "virar", "flip" },
            { "reiniciar", "restart" },
            { "pausa", "pause" },
            // Ilhas
            { "Ilha 1 · Despertar", "Island 1 · Awakening" },
            { "Ilha 2 · Travessia", "Island 2 · Crossing" },
            { "Ilha 3 · Salto", "Island 3 · Leap" },
            { "Ilha 4 · Ritmo", "Island 4 · Rhythm" },
            { "Ilha 5 · Eco", "Island 5 · Echo" },
            { "Existe um lado que você nunca viu.", "There's a side you've never seen." },
            { "A chave apareceu no portal!", "The key appeared at the portal!" },
            { "Você pegou a chave!", "You got the key!" },
            { "Cuidado! Só resta 1 vida.", "Careful! Only 1 life left." },
            { "Ops! Restam {0} vidas.", "Oops! {0} lives left." },
            // Pausa e fim de jogo
            { "PAUSA", "PAUSED" },
            { "Continuar", "Resume" },
            { "Reiniciar ilha", "Restart island" },
            { "Menu principal", "Main menu" },
            { "Fim de jogo", "Game over" },
            { "Suas 3 vidas acabaram.", "You're out of lives." },
            { "Tentar de novo", "Try again" },
            // Intro e final
            { "Espaço ou clique para continuar", "Space or click to continue" },
            { "Pular", "Skip" },
            { "Alminha", "Wisp" },
            { "O outro lado sempre foi você.", "The other side was always you." },
            { "Voltar ao menu", "Back to menu" },
        };

        private static Dictionary<string, string> upper;
        private static Dictionary<string, string> Upper
        {
            get
            {
                if (upper != null) return upper;
                upper = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (var kv in Table)
                {
                    string k = kv.Key.ToUpperInvariant();
                    if (!upper.ContainsKey(k)) upper.Add(k, kv.Value);
                }
                return upper;
            }
        }

        /// <summary>Traduz o texto em português para o idioma atual.</summary>
        public static string Tr(string pt)
        {
            if (string.IsNullOrEmpty(pt) || !GameSettings.IsEnglish) return pt;
            if (Table.TryGetValue(pt, out string en)) return en;
            // texto em CAIXA ALTA (títulos): procura sem diferenciar maiúsculas e devolve em caixa alta
            bool upper = pt == pt.ToUpperInvariant() && pt != pt.ToLowerInvariant();
            if (upper && Upper.TryGetValue(pt, out en)) return en.ToUpperInvariant();
            return pt;
        }

        public static string Format(string ptTemplate, params object[] args)
        {
            return string.Format(Tr(ptTemplate), args);
        }
    }
}
