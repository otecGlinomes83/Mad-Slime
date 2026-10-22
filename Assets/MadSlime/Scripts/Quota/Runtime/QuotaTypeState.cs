namespace Quota
{
    internal class QuotaTypeState
    {
        private QuotaEntry _entry;
        private bool _isHighlighted;
        private bool _isDirty;
        private bool _hasChanged;

        public QuotaEntry Entry => _entry;
        public bool IsHighlighted => _isHighlighted;
        public bool IsDirty => _isDirty;
        public bool HasChanged => _hasChanged;

        public QuotaTypeState(QuotaEntry entry, bool isHighlighted)
        {
            _entry = entry;
            _isHighlighted = isHighlighted;
        }

        public void MarkDirty()
        {
            _isDirty = true;
        }

        public void UpdateHighlight(bool isHighlighted)
        {
            _hasChanged = _isHighlighted != isHighlighted;
            _isHighlighted = isHighlighted;
            _isDirty = false;
        }

        public void ClearChanges()
        {
            _hasChanged = false;
        }
    }
}
