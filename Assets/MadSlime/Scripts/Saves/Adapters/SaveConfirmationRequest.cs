using Cysharp.Threading.Tasks;

namespace Adapters
{
    public class SaveConfirmationRequest
    {
        private int _id;
        private string _json;
        private UniTaskCompletionSource _completion = new UniTaskCompletionSource();

        public int Id => _id;
        public string Json => _json;
        public UniTaskCompletionSource Completion => _completion;

        public SaveConfirmationRequest(int id, string json)
        {
            _id = id;
            _json = json;
        }
    }
}
