using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Adapters
{
    public class SaveConfirmation : MonoBehaviour
    {
        private const int TimeoutSeconds = 15;

        private Queue<SaveConfirmationRequest> _requests = new Queue<SaveConfirmationRequest>();
        private SaveConfirmationRequest _activeRequest;
        private int _nextId;
        private bool _isProcessing;

#if UNITY_WEBGL && UNITY_EDITOR == false
        [DllImport("__Internal")]
        private static extern void MadSlimeConfirmSave(string target, int requestId, string json);
#endif

        public UniTask ConfirmAsync(string json, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _nextId = checked(_nextId + 1);
            SaveConfirmationRequest request = new SaveConfirmationRequest(_nextId, json);
            _requests.Enqueue(request);

            if (_isProcessing == false)
            {
                ProcessRequestsAsync().Forget();
            }

            return request.Completion.Task.AttachExternalCancellation(cancellationToken);
        }

        private async UniTaskVoid ProcessRequestsAsync()
        {
            _isProcessing = true;

            while (_requests.Count > 0)
            {
                _activeRequest = _requests.Dequeue();
                SaveConfirmationRequest request = _activeRequest;

                try
                {
#if UNITY_WEBGL && UNITY_EDITOR == false
                    MadSlimeConfirmSave(gameObject.name, request.Id, request.Json);
#else
                    request.Completion.TrySetResult();
#endif
                    await request.Completion.Task.Timeout(TimeSpan.FromSeconds(TimeoutSeconds), DelayType.Realtime);
                }
                catch (Exception exception)
                {
                    request.Completion.TrySetException(exception);
                }

                _activeRequest = null;
            }

            _isProcessing = false;
        }

        public void OnSaveConfirmed(string requestId)
        {
            if (_activeRequest == null || int.TryParse(requestId, out int id) == false || id != _activeRequest.Id)
            {
                return;
            }

            _activeRequest.Completion.TrySetResult();
        }

        public void OnSaveFailed(string result)
        {
            int separatorIndex = result.IndexOf('|');

            if (_activeRequest == null || separatorIndex < 0
                || int.TryParse(result.Substring(0, separatorIndex), out int id) == false || id != _activeRequest.Id)
            {
                return;
            }

            string reason = result.Substring(separatorIndex + 1);
            _activeRequest.Completion.TrySetException(new InvalidOperationException("Cloud save was not confirmed: " + reason));
        }

        private void OnDestroy()
        {
            OperationCanceledException exception = new OperationCanceledException("Save confirmation was destroyed.");

            if (_activeRequest != null)
            {
                _activeRequest.Completion.TrySetException(exception);
            }

            while (_requests.Count > 0)
            {
                _requests.Dequeue().Completion.TrySetException(exception);
            }
        }
    }
}
