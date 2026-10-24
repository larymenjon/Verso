namespace Verso
{
    /// <summary>
    /// Vidas da partida. Cair ou encostar num perigo custa uma vida.
    /// Ao perder as 3, é fim de jogo. Volta pra 3 ao começar um jogo novo ou tentar de novo.
    /// </summary>
    public static class Lives
    {
        public const int Max = 3;

        public static int Current { get; private set; } = Max;
        public static bool IsGameOver => Current <= 0;

        public static event System.Action<int> Changed;

        public static void Reset()
        {
            Current = Max;
            Changed?.Invoke(Current);
        }

        /// <summary>Tira uma vida. Devolve true se acabou o jogo.</summary>
        public static bool LoseLife()
        {
            if (Current > 0) Current--;
            Changed?.Invoke(Current);
            return IsGameOver;
        }
    }
}
