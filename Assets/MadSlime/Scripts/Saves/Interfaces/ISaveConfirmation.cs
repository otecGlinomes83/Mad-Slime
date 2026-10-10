using System.Threading;
using Cysharp.Threading.Tasks;

namespace Saves
{
    public interface ISaveConfirmation
    {
        UniTask ConfirmSavedAsync(CancellationToken cancellationToken);
    }
}
