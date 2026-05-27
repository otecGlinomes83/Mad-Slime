using System;
using System.Collections.Generic;
using Collectables;
using Quota;
using TMPro;
using UnityEngine;

namespace UI
{
    public class QuotaViewer : MonoBehaviour
    {
        [SerializeField] private QuotaTreker _quotaTreker;
        [SerializeField] private List<QuotaTab> _tabs;

        private void OnEnable()
        {
            _quotaTreker.QuotaChanged += OnQuotaChanged;
        }

        private void OnDisable()
        {
            _quotaTreker.QuotaChanged -= OnQuotaChanged;
        }

        private void OnQuotaChanged(int remaining, QuotaEntry entry)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class QuotaTab
    {
        [SerializeField] private QuotaEntry _entry;
        [SerializeField] private int _remainingCount;
        [SerializeField] private TMP_Text _text;
    }
}