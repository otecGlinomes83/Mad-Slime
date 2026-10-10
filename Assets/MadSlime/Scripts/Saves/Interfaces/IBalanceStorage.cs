namespace Saves
{
    public interface IBalanceStorage
    {
        int Balance { get; }

        void SetBalance(int balance);
    }
}
