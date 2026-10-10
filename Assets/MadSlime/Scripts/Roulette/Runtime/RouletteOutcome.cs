namespace Roulette
{
    public class RouletteOutcome
    {
        private int _slotIndex;
        private RoulettePrize _prize;

        public int SlotIndex => _slotIndex;
        public RoulettePrize Prize => _prize;

        public RouletteOutcome(int slotIndex, RoulettePrize prize)
        {
            _slotIndex = slotIndex;
            _prize = prize;
        }
    }
}
